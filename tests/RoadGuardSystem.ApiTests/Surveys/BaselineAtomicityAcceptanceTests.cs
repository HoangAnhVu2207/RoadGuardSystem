using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Repositories.Implementations.Surveys;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Factories;
using RoadGuardSystem.Services.Options;
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
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.ApiTests.Surveys;

[Collection(AuthenticationApiFixture.Name)]
public sealed class BaselineAtomicityAcceptanceTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task GenuineBaselineSelectionRollbackAndLostAcknowledgement(bool afterCommit, bool replace)
    {
        var supervisor = await sql.CreateUserAsync($"anh-super-{Guid.NewGuid():N}", "Current1!", UserRoleCode.Supervisor);
        var manager = await sql.CreateUserAsync($"anh-pm-{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        var first = await sql.CreateUserAsync($"anh-op-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var second = await sql.CreateUserAsync($"anh-op2-{Guid.NewGuid():N}", "Current1!", UserRoleCode.DroneOperator);
        var media = new byte[100];
        var checksum = Convert.ToHexString(SHA256.HashData(media)).ToLowerInvariant();
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString, configureTestServices: services =>
        { services.RemoveAll<IUploadObjectStorage>(); services.AddSingleton<IUploadObjectStorage>(new SurveyVideoStorage(media, checksum)); });
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        await Login(client, supervisor.UserName!);
        var created = await client.PostAsJsonAsync("/api/v1/projects", new
        {
            projectCode = $"ANH-{Guid.NewGuid():N}",
            name = "DEMO survey evidence",
            engineeringUtmSrid = 32648,
            startDate = "2026-09-01",
            endDate = "2027-09-01",
            primaryProjectManagerUserId = manager.Id,
            handover = new { documentNo = "DEMO", handoverDate = "2026-08-31" },
            operationId = Guid.NewGuid()
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await Body(created)).GetProperty("projectId").GetGuid();
        await Login(client, manager.UserName!);
        var draft = await Command(client, $"/api/v1/projects/{project}/road-geometry-drafts", new
        {
            sourceKind = "COORDINATES",
            sourceCrs = 32648,
            stationOriginMeters = 0d,
            changeReason = "DEMO alignment",
            coordinates = new[] { new { x = 500000d, y = 1100000d }, new { x = 500200d, y = 1100000d } },
            widthProfile = new[] { new { fromOffsetMeters = 0d, toOffsetMeters = 200d, widthMeters = 8d } },
            surveyWidthMeters = 12d,
            roadCode = "DEMO"
        });
        draft.StatusCode.Should().Be(HttpStatusCode.Created);
        await Login(client, supervisor.UserName!);
        var road = await Command(client, draft.Headers.Location!.OriginalString + "/confirm", new
        {
            expectedCurrentVersionId = (Guid?)null,
            effectiveFrom = "2026-10-02T00:00:00Z",
            reason = "Supervisor DEMO approval"
        }, draft.Headers.ETag!.Tag);
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
                db.ProjectMembers.Add(new ProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project,
                    UserId = actor,
                    RoleCode = UserRoleCode.DroneOperator,
                    ValidFrom = new DateOnly(2026, 1, 1),
                    Status = ProjectMemberStatus.Active
                });
            await db.SaveChangesAsync();
        }
        object Scope(string band) => new { routeVersionId = route, segmentSetId = set, segmentIds = new[] { segment }, targetBand = band };
        var scopes = new[] { Scope("SURFACE"), Scope("RIGHT_EDGE") };
        await Login(client, manager.UserName!);
        var plan = await Command(client, $"/api/v1/projects/{project}/survey-plans", new { scope = scopes, plannedAt = "2026-10-02T08:00:00Z", surveyType = "BASELINE" });
        plan.StatusCode.Should().Be(HttpStatusCode.Created);
        var planId = (await Body(plan)).GetProperty("id").GetGuid();
        (await client.GetAsync(plan.Headers.Location)).StatusCode.Should().Be(HttpStatusCode.OK);
        var taskResponse = await Command(client, $"/api/v1/projects/{project}/survey-tasks", new
        {
            scope = scopes,
            surveyType = "BASELINE",
            operatorId = first.Id,
            dueAt = "2026-10-03T08:00:00Z",
            accessPoint = (object?)null,
            planId
        });
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
        var uploadCreated = await Command(client, "/api/v1/uploads", new
        {
            purpose = "SURVEY_VIDEO",
            projectId = project,
            targetId = task,
            fileName = "synthetic.mp4",
            mediaType = "video/mp4",
            sizeBytes = media.Length,
            checksumSha256 = checksum
        });
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
        var submit = new
        {
            videoFileIds = new[] { video },
            telemetryFileIds = Array.Empty<Guid>(),
            recordedAt = "2026-10-02T08:00:00Z",
            deviceId = device,
            scope = scopes,
            pairs = new[] { new { videoFileId = video, telemetryFileId = (Guid?)null, timeOffsetMilliseconds = 0L } }
        };
        var submitted = await Command(client, $"/api/v1/survey-tasks/{task}/datasets", submit, accepted.Headers.ETag!.Tag);
        submitted.StatusCode.Should().Be(HttpStatusCode.Created);
        var dataset = (await Body(submitted)).GetProperty("id").GetGuid();
        var detail = await client.GetAsync(submitted.Headers.Location);
        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Body(detail)).GetProperty("submittedBy").GetGuid().Should().Be(first.Id);
        await Login(client, manager.UserName!);


        var assessed = await Command(client, $"/api/v1/datasets/{dataset}/assessments", new
        {
            methodVersion = "pm-evidence-review.v1",
            items = new[] { new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = "SURFACE", positionStatus = "PASS", qualityStatus = "PASS", coverageStatus = "PASS", reason = "Verified video", evidence = new[] { new { fileId = video, fromMilliseconds = 0, toMilliseconds = 1000 } } } }
        }, submitted.Headers.ETag!.Tag);
        assessed.StatusCode.Should().Be(HttpStatusCode.Created);
        var assessment = (await Body(assessed)).GetProperty("id").GetGuid();

        Guid? expected = null;
        if (replace)
        {
            using var selected = await Command(client, $"/api/v1/projects/{project}/baseline-selections", new { items = new[] { new { routeVersionId = route, segmentSetId = set, segmentId = segment, targetBand = "SURFACE", datasetId = dataset, assessmentId = assessment, expectedBaselineSelectionId = (Guid?)null } }, reason = "Initial baseline" });
            selected.StatusCode.Should().Be(HttpStatusCode.Created);
            expected = (await Body(selected)).GetProperty("items")[0].GetProperty("selectionId").GetGuid();
        }
        var key = Guid.NewGuid().ToString("N"); var correlation = Guid.NewGuid();
        BaselineItem[] items = [new(route, set, segment, "SURFACE", dataset, assessment, expected)];
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                selections = await db.Set<BaselineSelection>().AsNoTracking().Where(b => b.ProjectId == project).OrderBy(b => b.Id).ToArrayAsync(),
                entries = await (from i in db.Set<BaselineSelectionItem>().AsNoTracking() join b in db.Set<BaselineSelection>() on i.BaselineSelectionId equals b.Id where b.ProjectId == project orderby i.Id select i).ToArrayAsync(),
                pointers = await db.Set<BaselineCurrentPointer>().AsNoTracking().Where(b => b.ProjectId == project).OrderBy(b => b.Id).ToArrayAsync(),
                receipts = await db.IdempotencyRecords.AsNoTracking().Where(r => r.ProjectId == project && r.Operation == "BaselineSelected").OrderBy(r => r.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(a => a.ActorUserId == manager.Id && a.EventType == "baseline_selected").OrderBy(a => a.Id).ToArrayAsync()
            });
        }
        var before = await Snapshot();
        var probe = new BaselineCommitFailure(key, afterCommit);
        await using var faultDb = CommitFailureDbContext.Create(sql.ConnectionString, probe);
        var repository = new SurveyAssessmentRepository(faultDb, new IdempotencyOperationService(faultDb));
        if (!afterCommit)
        {
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException>(() => repository.SelectAsync(manager.Id, project, items, "Fault probe", key, new string('a', 64), correlation, default));
            probe.Failures.Should().Be(3); (await Snapshot()).Should().Be(before);
        }
        else
        {
            var result = await repository.SelectAsync(manager.Id, project, items, "Fault probe", key, new string('a', 64), correlation, default);
            probe.Failures.Should().Be(1); result.Code.Should().Be("success"); result.Baseline.Should().NotBeNull();
            var saved = await Snapshot();
            await using var replayDb = sql.CreateDbContext();
            var replay = await new SurveyAssessmentRepository(replayDb, new IdempotencyOperationService(replayDb)).SelectAsync(manager.Id, project, items, "Fault probe", key, new string('a', 64), correlation, default);
            JsonSerializer.Serialize(replay).Should().Be(JsonSerializer.Serialize(result)); (await Snapshot()).Should().Be(saved);
            await using var db = sql.CreateDbContext();
            var row = await db.Set<BaselineSelection>().SingleAsync(b => b.Id == result.Baseline!.Id);
            row.ProjectId.Should().Be(project); row.SelectedBy.Should().Be(manager.Id); row.Reason.Should().Be("Fault probe");
            (await db.Set<BaselineSelection>().CountAsync(b => b.ProjectId == project)).Should().Be(replace ? 2 : 1);
            var entry = await db.Set<BaselineSelectionItem>().SingleAsync(i => i.BaselineSelectionId == row.Id);
            entry.DatasetId.Should().Be(dataset); entry.AssessmentId.Should().Be(assessment);
            var pointer = await db.Set<BaselineCurrentPointer>().SingleAsync(p => p.ProjectId == project);
            pointer.SelectionId.Should().Be(entry.Id);
            var receipt = await db.IdempotencyRecords.SingleAsync(r => r.ProjectId == project && r.Operation == "BaselineSelected" && r.IdempotencyKey == key);
            receipt.ActorUserId.Should().Be(manager.Id); receipt.OperationId.Should().Be(row.Id);
            var audit = await db.AuditLogs.SingleAsync(a => a.EntityId == row.Id && a.EventType == "baseline_selected");
            audit.ActorUserId.Should().Be(manager.Id); audit.CorrelationId.Should().Be(correlation); audit.EntityType.Should().Be("SurveyAssessment");
            audit.BeforeSnapshot.Should().BeNull(); audit.AfterSnapshot.Should().Be("{}"); audit.Source.Should().Be("anh01"); audit.Reason.Should().Be("Manual survey evidence review");
            if (expected.HasValue)
                (await db.Set<BaselineSelectionItem>().AnyAsync(i => i.Id == expected.Value)).Should().BeTrue("replacement retains historical entry");
        }
    }
    private sealed class BaselineCommitFailure(string key, bool afterCommit) : DbTransactionInterceptor
    {
        public int Failures { get; private set; }
        private bool HasReceipt(DbContext? context) => context?.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().Local.Any(r => r.IdempotencyKey == key) == true;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!afterCommit && HasReceipt(eventData.Context)) { Failures++; throw new CommitFailureTransientException("Baseline precommit rollback probe."); }
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (afterCommit && Failures == 0 && HasReceipt(eventData.Context)) { Failures++; throw new CommitFailureTransientException("Baseline lost acknowledgement probe."); }
            return Task.CompletedTask;
        }
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
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => Task.FromResult("fixture-upload");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers,
            DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(n => new PresignedUploadPart(n, $"https://storage.test/{n}", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts,
            CancellationToken cancellationToken = default) => Task.FromResult(new UploadObjectVerification(media.Length, checksum, "video/mp4"));
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(media, false));
    }
}
