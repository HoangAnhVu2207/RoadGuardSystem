using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoadGuardSystem.Repositories.Storage;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Devices;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

using Microsoft.Extensions.Hosting;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.Services.Processing.Anh02;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.ApiTests.Processing;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Anh02AiHttpTests(AuthenticationSqlServerFixture sql)
{
    [Fact]
    public async Task Real_synthetic_upload_manifest_worker_provenance_replay_and_revocation()
    {
        var supervisor = await sql.CreateUserAsync($"anh-super-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync($"anh-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var first = await sql.CreateUserAsync($"anh-op-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var receiptRevocation = new Anh02ReceiptRevocationInterceptor(async token =>
        {
            await using var revoke = sql.CreateDbContext();
            await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status=2 WHERE Id={manager.Id}", token);
        });
        var second = await sql.CreateUserAsync($"anh-op2-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var media = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Anh02Fixtures", "synthetic-road-v1.mp4"));
        var checksum = Convert.ToHexString(SHA256.HashData(media)).ToLowerInvariant();
        var artifacts = new ArtifactFixture { LoseFirstAcknowledgement=true };
        var sourceStorage=new SurveyVideoStorage(media,checksum);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IHostedService>(); services.Configure<Anh02AiOptions>(o => o.MockEnabled = true);
          services.RemoveAll<IAnh02ArtifactStore>(); services.AddSingleton<IAnh02ArtifactStore>(artifacts);
          services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(sourceStorage);
          services.RemoveAll<RoadGuardSystem.Repositories.RoadGuardDbContext>();
          services.AddScoped(sp => new RoadGuardSystem.Repositories.RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardSystem.Repositories.RoadGuardDbContext>(sp.GetRequiredService<DbContextOptions<RoadGuardSystem.Repositories.RoadGuardDbContext>>()).AddInterceptors(receiptRevocation).Options)); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        var created = await client.PostAsJsonAsync("/api/v1/projects", new { projectCode = $"ANH-{Guid.NewGuid():N}", name = "DEMO survey evidence",
            engineeringUtmSrid = 32648, startDate = "2026-09-01", endDate = "2027-09-01", primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = "DEMO", handoverDate = "2026-08-31" }, operationId = Guid.NewGuid() });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await Body(created)).GetProperty("projectId").GetGuid();
        await Login(client, manager.UserName!);
        var draft = await Command(client, $"/api/v1/projects/{project}/road-geometry-drafts", new { sourceKind = "COORDINATES", sourceCrs = 32648,
            stationOriginMeters = 0d, changeReason = "DEMO alignment", coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500200d, y = 1100000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 200d, widthMeters = 8d } }, surveyWidthMeters = 12d, roadCode = "DEMO" });
        draft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, supervisor.UserName!);
        var road = await Command(client, draft.Headers.Location!.OriginalString + "/confirm", new { expectedCurrentVersionId = (Guid?)null,
            effectiveFrom = "2026-10-02T00:00:00Z", reason = "Supervisor DEMO approval" }, draft.Headers.ETag!.Tag);
        road.StatusCode.Should().Be(HttpStatusCode.Created);
        var route = (await Body(road)).GetProperty("routeVersionId").GetGuid();
        var section = (await Body(road)).GetProperty("roadSectionId").GetGuid();
        await Login(client, manager.UserName!);
        var setsPath = $"/api/v1/projects/{project}/road-sections/{section}/versions/{route}/segment-sets";
        var segmentSet = await Command(client, setsPath, new { targetLengthMeters = 100d, remainderMode = "KEEP" });
        segmentSet.StatusCode.Should().Be(HttpStatusCode.Created);
        var set = (await Body(segmentSet)).GetProperty("id").GetGuid();
        var segment = (await Body(segmentSet)).GetProperty("segments")[0].GetProperty("id").GetGuid();
        var published = await Command(client, setsPath + $"/{set}/publish", new { expectedPublishedSetId = (Guid?)null, reason = "PM DEMO publication" }, segmentSet.Headers.ETag!.Tag);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        var device = Guid.NewGuid();
        // Registry and project membership provisioning are fixture setup; there is no assigned device allocation API.
        await using (var db = sql.CreateDbContext())
        {
            db.DroneDevices.Add(DroneDevice.Create(device, $"DEMO-{device:N}", DroneDeviceStatus.Active));
            foreach (var actor in new[] { first.Id, second.Id })
                db.ProjectMembers.Add(new ProjectMember { Id = Guid.NewGuid(), ProjectId = project, UserId = actor, RoleCode = UserRoleCode.DroneOperator,
                    ValidFrom = new DateOnly(2026, 1, 1), Status = ProjectMemberStatus.Active });
            await db.SaveChangesAsync();
        }
        object Scope(string band) => new { routeVersionId = route, segmentSetId = set, segmentIds = new[] { segment }, targetBand = band };
        var scopes = new[] { Scope("SURFACE"), Scope("RIGHT_EDGE") };
        await Login(client, manager.UserName!);
        var plan = await Command(client, $"/api/v1/projects/{project}/survey-plans", new { scope = scopes, plannedAt = "2026-10-02T08:00:00Z", surveyType = "BASELINE" });
        plan.StatusCode.Should().Be(HttpStatusCode.Created);
        var planId = (await Body(plan)).GetProperty("id").GetGuid();
        (await client.GetAsync(plan.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var taskResponse = await Command(client, $"/api/v1/projects/{project}/survey-tasks", new { scope = scopes, surveyType = "BASELINE", operatorId = first.Id,
            dueAt = "2026-10-03T08:00:00Z", accessPoint = (object?)null, planId });
        taskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = (await Body(taskResponse)).GetProperty("id").GetGuid();
        await Login(client, first.UserName!);
        var work = await client.GetAsync($"/api/v1/survey-tasks/{task}/work-package");
        work.StatusCode.Should().Be(HttpStatusCode.OK);
        var workBody = await Body(work);
        workBody.GetProperty("schemaVersion").GetString().Should().Be("anh01.survey-work.v1");
        workBody.GetProperty("geometryRefs").GetArrayLength().Should().Be(1);
        workBody.GetProperty("geometryRefs")[0].TryGetProperty("targetBand", out _).Should().BeFalse();
        var accepted = await Command(client, $"/api/v1/survey-tasks/{task}/accept", null, taskResponse.Headers.ETag!.Tag);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadCreated = await Command(client, "/api/v1/uploads", new { purpose = "SURVEY_VIDEO", projectId = project, targetId = task,
            fileName = "synthetic.mp4", mediaType = "video/mp4", sizeBytes = media.Length, checksumSha256 = checksum });
        uploadCreated.StatusCode.Should().Be(HttpStatusCode.Created);
        var upload = (await Body(uploadCreated)).GetProperty("id").GetGuid();
        var video = (await Body(uploadCreated)).GetProperty("fileId").GetGuid();
        (await Command(client, $"/api/v1/uploads/{upload}/part-urls", new { partNumbers = new[] { 1 } })).StatusCode.Should().Be(HttpStatusCode.OK);
        var uploadRead = await client.GetAsync($"/api/v1/uploads/{upload}");
        (await Command(client, $"/api/v1/uploads/{upload}/complete", new { parts = new[] { new { partNumber = 1, eTag = "fixture-1" } }, checksumSha256 = checksum }, uploadRead.Headers.ETag!.Tag))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);
        using (var verification = factory.Services.CreateScope())
            await verification.ServiceProvider.GetRequiredService<RoadGuardSystem.Services.Files.IUploadService>().ProcessOneVerificationAsync();
        var fileMetadata = await client.GetAsync($"/api/v1/files/{video}");
        (await Body(fileMetadata)).GetProperty("status").GetString().Should().Be("VERIFIED");
        var submit = new { videoFileIds = new[] { video }, telemetryFileIds = Array.Empty<Guid>(), recordedAt = "2026-10-02T08:00:00Z",
            deviceId = device, scope = scopes, pairs = new[] { new { videoFileId = video, telemetryFileId = (Guid?)null, timeOffsetMilliseconds = 0L } } };
        var submitted = await Command(client, $"/api/v1/survey-tasks/{task}/datasets", submit, accepted.Headers.ETag!.Tag);
        submitted.StatusCode.Should().Be(HttpStatusCode.Created);
        var dataset = (await Body(submitted)).GetProperty("id").GetGuid();
        var detail = await client.GetAsync(submitted.Headers.Location);
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(detail)).GetProperty("submittedBy").GetGuid().Should().Be(first.Id);
        await Login(client, manager.UserName!);

        var model = Guid.NewGuid();
        await using (var db = sql.CreateDbContext())
        {
            db.AIModelVersions.Add(AIModelVersion.Create(model, "Synthetic fixture", "synthetic-road-v1", "fixture://local", null, null, AIModelVersionStatus.Released, DateTimeOffset.UtcNow, supervisor.Id));
            await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS(SELECT 1 FROM DefectTypes WHERE Code='CRACK') INSERT INTO DefectTypes(Code,Name,IsActive) VALUES ('CRACK','Crack',1)");
            await db.SaveChangesAsync();
        }
        string geometryVersion;
        using (var capture = factory.Services.CreateScope())
        {
            var geometry = await capture.ServiceProvider.GetRequiredService<IAnhHuyProducerService>().ResolveGeometryAsync(manager.Id, UserRoleCode.ProjectManager, project, route, set);
            Assert.Equal(AnhHuyProducerStatus.Ready, geometry.Status); geometryVersion = geometry.Facts!.Version;
        }
        var input = new RoadGuardSystem.DTOs.Processing.CreateAiMockRunRequest(dataset, new(route,set,[segment],"SURFACE"), model,"fixture-preprocess.v1","fixture-config.v1","synthetic-road-v1","VIDEO_ANALYSIS",geometryVersion);
        var path = $"/api/v1/projects/{project}/ai-mock-runs"; var key = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.UnprocessableEntity,(await Command(client,path,input,key:new string('k',201))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,(await Command(client,path,input with{ModelVersionId=Guid.NewGuid()})).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,(await Command(client,path,input with{Scope=input.Scope with{SegmentIds=[Guid.NewGuid()]}})).StatusCode);
        var admitted = await Command(client,path,input,key:key); admitted.StatusCode.Should().Be(HttpStatusCode.Accepted,await admitted.Content.ReadAsStringAsync());
        var run = (await Body(admitted)).GetProperty("id").GetGuid();
        Assert.Equal(run,(await Body(await Command(client,path,input,key:key))).GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Conflict,(await Command(client,path,input with { FixtureVersion="synthetic-empty-v1" },key:key)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.GetAsync(path+$"/{run}/result")).StatusCode);
        using (var worker = factory.Services.CreateScope()) Assert.True(await worker.ServiceProvider.GetRequiredService<IAnh02AiService>().ProcessOneAsync(default));
        Assert.Equal("RUNNING",(await Body(await client.GetAsync(path+$"/{run}"))).GetProperty("status").GetString());
        await using (var pending = sql.CreateDbContext())
        {
            var pendingRun = await pending.Set<AiMockRun>().SingleAsync(r => r.Id == run);
            Assert.Equal(ProcessingJobStatus.Queued, (await pending.ProcessingJobs.SingleAsync(j => j.Id == pendingRun.ProcessingJobId)).Status);
            Assert.Null((await pending.ProcessingAttempts.SingleAsync(a => a.Id == pendingRun.AttemptId)).EndedAt);
            Assert.Equal(1, await pending.ProcessingAttempts.CountAsync(a => a.ProcessingJobId == pendingRun.ProcessingJobId));
        }
        await using(var db=sql.CreateDbContext()) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Anh02AiMockRuns SET LeaseUntil={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={run}");
        using(var restarted=factory.Services.CreateScope()) Assert.True(await restarted.ServiceProvider.GetRequiredService<IAnh02AiService>().ProcessOneAsync(default));
        Assert.Equal(1,artifacts.Writes);
        var resultResponse = await client.GetAsync(path+$"/{run}/result"); Assert.Equal(HttpStatusCode.OK,resultResponse.StatusCode);
        var result = await Body(resultResponse); Assert.Equal("MOCK",result.GetProperty("mode").GetString());
        var detection = result.GetProperty("detections")[0]; var detectionId = detection.GetProperty("detectionId").GetGuid();
        Assert.Equal(video,detection.GetProperty("sourceVideoFileId").GetGuid()); Assert.Equal(500,detection.GetProperty("timestampMs").GetInt64());
        Assert.Equal("UNKNOWN",detection.GetProperty("positionStatus").GetString());
        await using(var db=sql.CreateDbContext())
        {
            var row=await db.Set<AiMockRun>().SingleAsync(r=>r.Id==run); Assert.Equal("SUCCEEDED",row.Status);
            var completedJob = await db.ProcessingJobs.SingleAsync(j => j.Id == row.ProcessingJobId);
            Assert.Equal(ProcessingJobStatus.Completed, completedJob.Status);
            var completedAttempt = await db.ProcessingAttempts.SingleAsync(a => a.Id == row.AttemptId);
            Assert.Equal(row.CompletedAt, completedAttempt.EndedAt);
            Assert.Equal(ProcessingAttemptErrorType.None, completedAttempt.ErrorType);
            var legacyJobId = Guid.NewGuid(); var legacyAttemptId = Guid.NewGuid(); var legacyStart = DateTimeOffset.UtcNow.AddMinutes(-1);
            db.ProcessingJobs.Add(ProcessingJob.CreateQueued(legacyJobId, completedJob.ProcessingBlockId, completedJob.ModelVersionId, project, new string('a', 64), "{}", "REAL"));
            db.ProcessingAttempts.Add(ProcessingAttempt.Create(legacyAttemptId, legacyJobId, 1, legacyStart, null, null, "legacy.fixture"));
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProcessingAttempts SET EndedAt={DateTimeOffset.UtcNow}, ErrorType=2 WHERE Id={legacyAttemptId}"));
            Assert.Null((await db.ProcessingAttempts.AsNoTracking().SingleAsync(a => a.Id == legacyAttemptId)).EndedAt);
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProcessingAttempts SET EndedAt={DateTimeOffset.UtcNow} WHERE Id={row.AttemptId}"));
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ProcessingAttempts WHERE Id={row.AttemptId}"));
            Assert.Equal(row.ManifestHash,AiManifestCanonicalizer.Hash(System.Text.Encoding.UTF8.GetBytes(row.CanonicalManifest)));
            var raw=await db.Set<AiResultProvenance>().SingleAsync(r=>r.RunId==run); Assert.Equal(raw.ResultHash,AiManifestCanonicalizer.Hash(System.Text.Encoding.UTF8.GetBytes(raw.CanonicalResult)));
            Assert.Equal(row.AttemptId,raw.AttemptId); Assert.Equal(row.ProcessingJobId,raw.ProcessingJobId);
            Assert.Single(await db.AIDetections.Where(d=>d.ProcessingJobId==row.ProcessingJobId).ToListAsync());
            Assert.Single(await db.Set<AiDetectionProvenance>().Where(p=>p.RunId==run).ToListAsync());
            var frame=await db.Files.SingleAsync(f=>f.Id==detection.GetProperty("frameFileId").GetGuid()); Assert.Equal(SyntheticAiFixture.FrameHash,frame.Checksum);
            Assert.False(await db.UploadSessions.AnyAsync(u=>u.FileId==frame.Id));
            Assert.False(await db.OutboxMessages.AnyAsync(o=>o.CorrelationId==row.ProcessingJobId));
            await Assert.ThrowsAnyAsync<Exception>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Anh02AiResultProvenance SET ResultHash={new string('b',64)} WHERE RunId={run}"));
        }
        var candidate=await client.GetAsync($"/api/v1/projects/{project}/ai-candidates/{detectionId}"); Assert.Equal(HttpStatusCode.OK,candidate.StatusCode);
        var proof=await Body(candidate); Assert.Equal("MOCK/SYNTHETIC",proof.GetProperty("mode").GetString());
        // Actual persisted job provenance must agree with the run/result, not
        // merely with the detection's FK. Only this disposable fixture is mutated.
        await using (var drift = sql.CreateDbContext())
        {
            var jobId = result.GetProperty("jobId").GetGuid();
            await drift.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProcessingJobs SET Mode='LIVE' WHERE Id={jobId}");
            try
            {
                var denied = await client.GetAsync($"/api/v1/projects/{project}/ai-candidates/{detectionId}");
                Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
                Assert.Equal("source_not_ready", (await Body(denied)).GetProperty("code").GetString());
            }
            finally { await drift.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProcessingJobs SET Mode='MOCK' WHERE Id={jobId}"); }
        }
        using(var capture=factory.Services.CreateScope())
        {
            var reader=capture.ServiceProvider.GetRequiredService<IAiCandidateFactsReader>();
            Assert.Equal(AnhHuyProducerStatus.StaleSource,(await reader.ResolveAsync(manager.Id,UserRoleCode.ProjectManager,project,detectionId,"stale")).Status);
            Assert.Equal(AnhHuyProducerStatus.StaleGeometry,(await reader.ResolveAsync(manager.Id,UserRoleCode.ProjectManager,project,detectionId,expectedGeometryVersion:"stale")).Status);
            Assert.Equal(AnhHuyProducerStatus.StaleDisposition,(await reader.ResolveAsync(manager.Id,UserRoleCode.ProjectManager,project,detectionId,expectedDispositionVersion:"stale")).Status);
        }
        var matching=input with{Stage="DUPLICATE_MATCHING",AnalysisRunId=run,CandidateSnapshotId=Guid.NewGuid()};
        var unavailable=await Command(client,path,matching); Assert.Equal(HttpStatusCode.Conflict,unavailable.StatusCode); Assert.Equal("source_not_ready",(await Body(unavailable)).GetProperty("code").GetString());
        // This reader is explicitly a fixture adapter contract check, not a real Huy integration.
        var matchingReader=new MatchingFixtureReader();
        matchingReader.Snapshot=new(matching.CandidateSnapshotId!.Value,new string('c',64),project,route,set,geometryVersion,DateTimeOffset.UtcNow,
            [new(Guid.NewGuid(),"synthetic-candidate.v1",segment,route)]);
        await using(var adapterFactory=new AuthenticationWebApplicationFactory(sql.ConnectionString,configureTestServices:services=>
        {
            services.RemoveAll<IHostedService>();services.Configure<Anh02AiOptions>(o=>o.MockEnabled=true);
            services.RemoveAll<IUploadObjectStorage>();services.AddSingleton<IUploadObjectStorage>(sourceStorage);
            services.RemoveAll<IAnh02ArtifactStore>();services.AddSingleton<IAnh02ArtifactStore>(artifacts);
            services.AddSingleton<IMatchingCandidateSnapshotReader>(matchingReader);
        }))
        {
            using var adapter=adapterFactory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});await Login(adapter,manager.UserName!);
            var admittedMatch=await Command(adapter,path,matching);admittedMatch.StatusCode.Should().Be(HttpStatusCode.Accepted,await admittedMatch.Content.ReadAsStringAsync());
            var matchRun=(await Body(admittedMatch)).GetProperty("id").GetGuid();
            using(var worker=adapterFactory.Services.CreateScope())Assert.True(await worker.ServiceProvider.GetRequiredService<IAnh02AiService>().ProcessOneAsync(default));
            var matchResult=await Body(await adapter.GetAsync(path+$"/{matchRun}/result"));Assert.Equal("DUPLICATE_MATCHING",matchResult.GetProperty("stage").GetString());
            Assert.Equal(result.GetProperty("jobId").GetGuid(),matchResult.GetProperty("jobId").GetGuid());Assert.Equal(result.GetProperty("attemptId").GetGuid(),matchResult.GetProperty("attemptId").GetGuid());
            Assert.Equal(detectionId,matchResult.GetProperty("matches")[0].GetProperty("detectionId").GetGuid());Assert.Empty(matchResult.GetProperty("detections").EnumerateArray());
            matchingReader.StaleAtCompletion=true;matchingReader.Calls=0;
            var stale=await Command(adapter,path,matching);Assert.Equal(HttpStatusCode.Accepted,stale.StatusCode);var staleId=(await Body(stale)).GetProperty("id").GetGuid();
            using(var worker=adapterFactory.Services.CreateScope())Assert.True(await worker.ServiceProvider.GetRequiredService<IAnh02AiService>().ProcessOneAsync(default));
            var staleView=await Body(await adapter.GetAsync(path+$"/{staleId}"));Assert.Equal("FAILED",staleView.GetProperty("status").GetString());Assert.Equal("candidate_stale",staleView.GetProperty("errorCode").GetString());
            await using(var db=sql.CreateDbContext())
            {
                var analysisJob=result.GetProperty("jobId").GetGuid();Assert.Equal(1,await db.ProcessingAttempts.CountAsync(a=>a.ProcessingJobId==analysisJob));
                Assert.Equal(ProcessingJobStatus.Completed, (await db.ProcessingJobs.SingleAsync(j => j.Id == analysisJob)).Status);
                Assert.Equal(ProcessingAttemptErrorType.None, (await db.ProcessingAttempts.SingleAsync(a => a.ProcessingJobId == analysisJob)).ErrorType);
                Assert.Equal((await db.Set<AiMockRun>().SingleAsync(r => r.Id == run)).CompletedAt, (await db.ProcessingAttempts.SingleAsync(a => a.ProcessingJobId == analysisJob)).EndedAt);
                Assert.Equal(1,await db.AIDetections.CountAsync(d=>d.ProcessingJobId==analysisJob));Assert.False(await db.Set<AiResultProvenance>().AnyAsync(r=>r.RunId==staleId));
                Assert.False(await db.Defects.AnyAsync(d=>d.SourceAIDetectionId==detectionId));
            }
        }
        var empty=await Command(client,path,input with{FixtureVersion="synthetic-empty-v1"}); Assert.Equal(HttpStatusCode.Accepted,empty.StatusCode);
        var emptyRun=(await Body(empty)).GetProperty("id").GetGuid();
        using(var worker=factory.Services.CreateScope()) Assert.True(await worker.ServiceProvider.GetRequiredService<IAnh02AiService>().ProcessOneAsync(default));
        Assert.Empty((await Body(await client.GetAsync(path+$"/{emptyRun}/result"))).GetProperty("detections").EnumerateArray());
        var contested=await Command(client,path,input);Assert.Equal(HttpStatusCode.Accepted,contested.StatusCode);
        var contestedId=(await Body(contested)).GetProperty("id").GetGuid();
        var start=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<AiMockRun?> Claim()
        { using var scope=factory.Services.CreateScope();await start.Task;return await scope.ServiceProvider.GetRequiredService<IAnh02AiRepository>().ClaimAsync(Guid.NewGuid(),DateTimeOffset.UtcNow,default); }
        var claimA=Claim();var claimB=Claim();start.SetResult();var claims=await Task.WhenAll(claimA,claimB);Assert.Single(claims.Where(c=>c is not null));
        Assert.Equal(contestedId,claims.Single(c=>c is not null)!.Id);
        // Failure between the run/job/audit write and attempt closure must roll everything back.
        await using (var faulted = new RoadGuardSystem.Repositories.RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardSystem.Repositories.RoadGuardDbContext>()
            .UseSqlServer(sql.ConnectionString, o => o.UseNetTopologySuite()).AddInterceptors(new AttemptCloseFailure()).Options))
        {
            var repo = new Anh02AiRepository(faulted, new(faulted), new RoadGuardSystem.Repositories.Projects.ProjectMembershipReadModel(faulted), TimeProvider.System);
            await Assert.ThrowsAsync<IOException>(() => repo.FailAsync(contestedId, claims.Single(c => c is not null)!.LeaseOwner!.Value, "source_not_ready", default));
        }
        await using (var rolledBack = sql.CreateDbContext())
        {
            var active = await rolledBack.Set<AiMockRun>().SingleAsync(r => r.Id == contestedId);
            Assert.Equal("RUNNING", active.Status); Assert.Null(active.CompletedAt); Assert.Null(active.ErrorCode);
            Assert.Equal(ProcessingJobStatus.Queued, (await rolledBack.ProcessingJobs.SingleAsync(j => j.Id == active.ProcessingJobId)).Status);
            Assert.Null((await rolledBack.ProcessingAttempts.SingleAsync(a => a.Id == active.AttemptId)).EndedAt);
            Assert.False(await rolledBack.AuditLogs.AnyAsync(a => a.EntityId == contestedId && a.EventType == "anh02_ai_mock_failed"));
        }
        await using(var db=sql.CreateDbContext()) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Anh02AiMockRuns SET LeaseUntil={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={contestedId}");
        using(var expired=factory.Services.CreateScope()) await expired.ServiceProvider.GetRequiredService<IAnh02AiRepository>().FailAsync(contestedId,claims.Single(c=>c is not null)!.LeaseOwner!.Value,"expired_worker",default);
        Assert.Equal("RUNNING",(await Body(await client.GetAsync(path+$"/{contestedId}"))).GetProperty("status").GetString());
        sourceStorage.Corrupt=true;
        using(var worker=factory.Services.CreateScope()) Assert.True(await worker.ServiceProvider.GetRequiredService<IAnh02AiService>().ProcessOneAsync(default));
        var failed=await Body(await client.GetAsync(path+$"/{contestedId}"));Assert.Equal("FAILED",failed.GetProperty("status").GetString());Assert.Equal("mock_fixture_source_mismatch",failed.GetProperty("errorCode").GetString());
        await using(var db=sql.CreateDbContext())
        {
            var failedRun = await db.Set<AiMockRun>().SingleAsync(r => r.Id == contestedId);
            var failedJob = await db.ProcessingJobs.SingleAsync(j => j.Id == failedRun.ProcessingJobId);
            Assert.Equal(ProcessingJobStatus.DataFailure, failedJob.Status);
            Assert.Equal(failedRun.ErrorCode, failedJob.ErrorCode);
            Assert.Equal(failedRun.CompletedAt, failedJob.CompletedAt);
            var failedAttempt = await db.ProcessingAttempts.SingleAsync(a => a.Id == failedRun.AttemptId);
            Assert.Equal(failedRun.CompletedAt, failedAttempt.EndedAt);
            Assert.Equal(ProcessingAttemptErrorType.Data, failedAttempt.ErrorType);
            Assert.False(await db.Set<AiResultProvenance>().AnyAsync(r=>r.RunId==contestedId));
            Assert.False(await db.AIDetections.AnyAsync(d => d.ProcessingJobId == failedRun.ProcessingJobId));
            Assert.False(await db.Set<AiDetectionProvenance>().AnyAsync(p => p.RunId == contestedId));
            Assert.Single(await db.AuditLogs.Where(a => a.EntityId == contestedId && a.EventType == "anh02_ai_mock_failed").ToArrayAsync());
        }
        var legacyId=Guid.NewGuid();var unrelatedProject=Guid.NewGuid();
        await using(var db=sql.CreateDbContext())
        {
            // Explicit historical fixture: legacy detection cannot gain proof just from a completed job.
            db.AIDetections.Add(AIDetection.Create(legacyId,result.GetProperty("jobId").GetGuid(),model,route,null,"CRACK",.5m,null,null,"{}"));
            db.Projects.Add(Project.Create(unrelatedProject,unrelatedProject.ToString(),"Cross-project hidden fixture",null,null,null,null,DateTimeOffset.UtcNow));await db.SaveChangesAsync();
        }
        var historical=await client.GetAsync($"/api/v1/projects/{project}/ai-candidates/{legacyId}");Assert.Equal(HttpStatusCode.Conflict,historical.StatusCode);Assert.Equal("source_not_ready",(await Body(historical)).GetProperty("code").GetString());
        await Login(client,supervisor.UserName!);Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/projects/{unrelatedProject}/ai-candidates/{detectionId}")).StatusCode);
        await Login(client,first.UserName!); Assert.Equal(HttpStatusCode.Forbidden,(await Command(client,path,input,key:key)).StatusCode);
        await Login(client,manager.UserName!);
        foreach (var replayInput in new[] { input, input with { FixtureVersion = "synthetic-empty-v1" } })
        {
            await using var before = sql.CreateDbContext();
            var receipts = await before.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().CountAsync(x => x.ActorUserId == manager.Id);
            var runs = await before.Set<AiMockRun>().CountAsync(x => x.ProjectId == project);
            var audits = await before.AuditLogs.CountAsync(x => x.ActorUserId == manager.Id);
            receiptRevocation.Armed = true;
            var denied = await Command(client, path, replayInput, key: key);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal("access_forbidden", (await Body(denied)).GetProperty("code").GetString());
            Assert.Null(denied.Headers.ETag); Assert.Null(denied.Headers.Location);
            Assert.Equal(receipts, await before.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().CountAsync(x => x.ActorUserId == manager.Id));
            Assert.Equal(runs, await before.Set<AiMockRun>().CountAsync(x => x.ProjectId == project));
            Assert.Equal(audits, await before.AuditLogs.CountAsync(x => x.ActorUserId == manager.Id));
            await before.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status=1 WHERE Id={manager.Id}");
        }
        Assert.Equal(2, receiptRevocation.Calls);
        await using(var db=sql.CreateDbContext()) { var member=await db.ProjectMembers.SingleAsync(m=>m.ProjectId==project&&m.UserId==manager.Id);member.Status=ProjectMemberStatus.Ended;await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.Forbidden,(await Command(client,path,input,key:key)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(path+$"/{run}/result")).StatusCode);
    }
    private sealed class MatchingFixtureReader:IMatchingCandidateSnapshotReader
    {
        public MatchingCandidateSnapshotV1 Snapshot{get;set;}=null!;public bool StaleAtCompletion{get;set;}public int Calls{get;set;}
        public Task<MatchingCandidateSnapshotV1?> CaptureAsync(Guid actorId,UserRoleCode role,Guid projectId,Guid routeVersionId,Guid segmentSetId,string geometryVersion,Guid[] detectionIds,Guid requestedSnapshotId,CancellationToken cancellationToken=default)
        {
            Assert.Equal(Snapshot.ProjectId,projectId);Assert.Equal(Snapshot.RouteVersionId,routeVersionId);Assert.Equal(Snapshot.SegmentSetId,segmentSetId);
            Assert.Equal(Snapshot.GeometryVersion,geometryVersion);Assert.Equal(Snapshot.SnapshotId,requestedSnapshotId);Assert.Single(detectionIds);
            Calls++;return Task.FromResult<MatchingCandidateSnapshotV1?>(StaleAtCompletion&&Calls>=3 ? Snapshot with{Hash=new string('d',64)} : Snapshot);
        }
    }
    private sealed class AttemptCloseFailure : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> NonQueryExecutingAsync(System.Data.Common.DbCommand command,
            Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("UPDATE [ProcessingAttempts]", StringComparison.Ordinal)) throw new IOException("Injected closure failure after terminal writes");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class ArtifactFixture : IAnh02ArtifactStore
    {
        public readonly Dictionary<string,byte[]> Objects=[];
        public bool LoseFirstAcknowledgement{get;init;} public int Writes{get;private set;}
        public async Task<Anh02ArtifactMetadata> WriteAsync(string key,Stream content,long? sizeBytes,string mediaType,CancellationToken cancellationToken=default)
        { using var copy=new MemoryStream();await content.CopyToAsync(copy,cancellationToken);var bytes=copy.ToArray();var added=Objects.TryAdd(key,bytes);if(added)Writes++;Assert.Equal(Objects[key],bytes);if(added&&LoseFirstAcknowledgement)throw new IOException("Lost durable acknowledgement fixture");return new(key,bytes.Length,AiManifestCanonicalizer.Hash(bytes),mediaType); }
        public Task<Anh02ArtifactRead> OpenReadAsync(string key,CancellationToken cancellationToken=default)
        { var bytes=Objects[key];return Task.FromResult(new Anh02ArtifactRead(new MemoryStream(bytes,false),new(key,bytes.Length,AiManifestCanonicalizer.Hash(bytes),"image/png"))); }
    }
    private static async Task Login(HttpClient client, string username)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = AuthenticationSqlServerFixture.EmailFor(username), password = "Current1!" });
        login.EnsureSuccessStatusCode(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await Body(login)).GetProperty("accessToken").GetString());
    }
    private static async Task<HttpResponseMessage> Command(HttpClient client, string url, object? body, string? version = null, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString("N"));
        if (version is not null) request.Headers.TryAddWithoutValidation("If-Match", version);
        return await client.SendAsync(request);
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
    private sealed class SurveyVideoStorage(byte[] media, string checksum) : IUploadObjectStorage
    {
        public bool Corrupt{get;set;}
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => Task.FromResult("fixture-upload");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers,
            DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(n => new PresignedUploadPart(n, $"https://storage.test/{n}", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts,
            CancellationToken cancellationToken = default) => Task.FromResult(new UploadObjectVerification(media.Length, checksum, "video/mp4"));
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(Corrupt ? new byte[media.Length] : media, false));
    }
}
