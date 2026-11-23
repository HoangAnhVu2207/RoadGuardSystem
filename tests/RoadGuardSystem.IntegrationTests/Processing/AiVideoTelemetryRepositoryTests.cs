using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Processing;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

// Always fixture-owned SQL; never consumes a configured shared server.
public sealed class AiVideoTelemetryRepositoryTests : IAsyncLifetime
{
    private readonly IdentitySqlServerFixture _fixture = new(new SqlServerTestFixture(_ => null, createSpatialProbeSchema: false));
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly string[] Labels = ["alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack"];
    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    private static AiVideoTelemetryAppend Request(AiVideoWorkItem work) => new(Guid.NewGuid(), Guid.Empty,
        work.CurrentAttemptId, new string('a',64), "OPERATOR_ESTIMATE",
        "[{\"startMs\":0,\"endMs\":1000,\"latitude\":10,\"longitude\":106}]", null, null, null, 0,
        "fixture estimate", new string('b',64), new string('c',64), null, false);

    [Theory]
    [InlineData("discover")][InlineData("scope")][InlineData("list")][InlineData("append")]
    public async Task Telemetry_boundaries_support_the_configured_retry_strategy(string operation)
    {
        await using var db = _fixture.CreateRetryingDbContext();
        var repo = new AiVideoTelemetryRepository(db, TimeProvider.System);
        var actor = Guid.NewGuid(); var project = Guid.NewGuid(); var work = Guid.NewGuid();
        await Expect(404, async () => {
            if (operation == "discover") await repo.DiscoverAsync(actor, UserRoleCode.ProjectManager, project, Guid.NewGuid(), 0, 10, Ct);
            else if (operation == "scope") await repo.ReadScopeAsync(actor, UserRoleCode.ProjectManager, project, work, Ct);
            else if (operation == "list") await repo.ListAsync(actor, UserRoleCode.ProjectManager, project, work, Ct);
            else await repo.AppendAsync(actor, UserRoleCode.DroneOperator, project, work, null!, Ct);
        });
    }

    private sealed class FailFirstRevisionSave : SaveChangesInterceptor
    {
        public int Attempts { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AiVideoTelemetryRevision>().Any(x => x.State == EntityState.Added) && ++Attempts == 1)
                throw new TestTransientException("Simulated telemetry insert retry.");
            return ValueTask.FromResult(result);
        }
    }
    [Theory]
    [InlineData(false, false)][InlineData(true, false)]
    [InlineData(false, true)][InlineData(true, true)]
    public async Task AppendReplaysBeforeHeadCheckAndEnforcesImmutableSqlHistory(bool retry, bool external)
    {
        var seed = await SeedAsync(); var source = seed.Sources[0];
        await using var db = _fixture.CreateDbContext();
        var transport = new AiVideoWorkRepository(db);
        var workId = await transport.PrepareAsync(source, Prepared(source, seed.ModelId), seed.Map, Ct);
        var work = (await transport.ClaimAsync("worker", DateTimeOffset.UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), Ct))!;
        var now = work.UpdatedAt;
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.Admitted, now);
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.ArtifactsReady, now);
        await db.SaveChangesAsync();
        await transport.CompleteAsync(workId, "worker", work.CurrentAttemptId,
            [new AiRegisteredArtifact(Guid.NewGuid(), new string('d',64), 20, "application/json", "RAW_RESULT", "fixture/result.json", null, null)], [], seed.Map, now.AddSeconds(1), Ct);
        var transient = new FailFirstRevisionSave();
        await using var telemetryDb = retry ? _fixture.CreateRetryingDbContext(transient) : _fixture.CreateDbContext();
        var repo = new AiVideoTelemetryRepository(telemetryDb, TimeProvider.System);
        var request = Request(work);
        if (external) request = request with { Kind = "EXTERNAL_SRT", OriginalSrtObjectKey = $"fixture/{Guid.NewGuid():N}.srt", OriginalSrtSha256 = new string('9', 64), OriginalSrtBytes = 42 };
        var first = await repo.AppendAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, workId, request, Ct);
        var second = await repo.AppendAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, workId,
            request with { RevisionId = Guid.NewGuid(), ExpectedHeadId = first.Id, IdempotencyKeyHash = new string('e',64), PayloadFingerprint = new string('f',64) }, Ct);
        second.PreviousId.Should().Be(first.Id); second.Number.Should().Be(2);
        if (external)
        {
            var files = await telemetryDb.Files.AsNoTracking().Where(x => x.StorageUri == request.OriginalSrtObjectKey).ToListAsync();
            files.Should().ContainSingle();
            files[0].Checksum.Should().Be(request.OriginalSrtSha256);
            files[0].SizeBytes.Should().Be(42);
            files[0].UploadedByUserId.Should().Be(source.OwnerId);
        }
        if (retry) transient.Attempts.Should().Be(3);
        (await repo.AppendAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, workId, request, Ct)).Id.Should().Be(first.Id);
        await Expect(409, () => repo.AppendAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, workId, request with { PayloadFingerprint = new string('f',64) }, Ct));
        await Expect(412, () => repo.AppendAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, workId, request with { IdempotencyKeyHash = new string('1',64) }, Ct));
        await Expect(403, () => repo.AppendAsync(source.OwnerId, UserRoleCode.ProjectManager, source.ProjectId, workId, request, Ct));
        await FluentActions.Awaiting(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE AiVideoTelemetryRevisions SET Note='changed' WHERE Id={first.Id}"))
            .Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>();
        await FluentActions.Awaiting(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM AiVideoTelemetryRevisions WHERE Id={second.Id}"))
            .Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>();
        (await repo.ListAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, workId, Ct)).Count.Should().Be(2);
        (await db.AIDetections.CountAsync()).Should().Be(0);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SurveyAssignments SET EndedAt={DateTimeOffset.UtcNow.AddMinutes(1)}, ReassignmentReason='fixture' WHERE OperatorUserId={source.OwnerId}");
        await Expect(403, () => repo.ReadScopeAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, workId, Ct));
    }
    private static async Task Expect(int status, Func<Task> action)
        => (await FluentActions.Awaiting(action).Should().ThrowAsync<AiRequestException>()).Which.Status.Should().Be(status);

    [Theory]
    [InlineData("attempt", 409)]
    [InlineData("source", 409)]
    [InlineData("password", 403)]
    [InlineData("embedded", 409)]
    [InlineData("artifact-hash", 409)]
    public async Task AppendRechecksCurrentAuthorityAttemptSourceAndEmbeddedEvidence(string mutation, int status)
    {
        var seed = await SeedAsync(); var source = seed.Sources[0];
        await using var db = _fixture.CreateDbContext();
        var transport = new AiVideoWorkRepository(db);
        var workId = await transport.PrepareAsync(source, Prepared(source, seed.ModelId), seed.Map, Ct);
        var work = (await transport.ClaimAsync("worker", DateTimeOffset.UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), Ct))!;
        var now = work.UpdatedAt;
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.Admitted, now);
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.ArtifactsReady, now);
        await db.SaveChangesAsync();
        await transport.CompleteAsync(workId, "worker", work.CurrentAttemptId,
            [new AiRegisteredArtifact(Guid.NewGuid(), new string('d',64), 20, "application/json", "RAW_RESULT", "fixture/result.json", null, null)], [], seed.Map, now.AddSeconds(1), Ct);
        var request = Request(work);
        if (mutation == "attempt") db.ProcessingAttempts.Add(ProcessingAttempt.Create(Guid.NewGuid(), work.ProcessingJobId, 2, now.AddSeconds(2), null, null, null));
        if (mutation == "source") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET Status={(byte)UploadSessionStatus.Uploading} WHERE FileId={source.FileId}");
        if (mutation == "password") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET MustChangePassword=1 WHERE Id={source.OwnerId}");
        if (mutation is "embedded" or "artifact-hash")
        {
            var file = StoredFile.Create(Guid.NewGuid(), "fixture/embedded.srt", "embedded.srt", "application/x-subrip", 40, new string('e',64), null, now, null);
            var artifact = AiWorkerArtifact.Create(Guid.NewGuid(), work.Id, work.CurrentAttemptId, Guid.NewGuid().ToString("D"),
                new string(mutation == "artifact-hash" ? 'f' : 'e',64), 40, "TELEMETRY_SRT");
            artifact.MarkRegistered(file.Id); db.AddRange(file, artifact);
            request = request with { ExpectedEmbeddedSrtFileId = file.Id, EmbeddedSrtIsValid = true };
        }
        await db.SaveChangesAsync();
        var repo = new AiVideoTelemetryRepository(db, TimeProvider.System);
        await Expect(status, () => repo.AppendAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, workId, request, Ct));
        (await db.AiVideoTelemetryRevisions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ReadAllowsCurrentManagerAndSupervisorWhileWriteStaysOperatorOnly()
    {
        var seed = await SeedAsync(); var source = seed.Sources[0];
        await using var db = _fixture.CreateDbContext();
        var transport = new AiVideoWorkRepository(db);
        var workId = await transport.PrepareAsync(source, Prepared(source, seed.ModelId), seed.Map, Ct);
        var work = (await transport.ClaimAsync("worker", DateTimeOffset.UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), Ct))!;
        var now = work.UpdatedAt;
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.Admitted, now);
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.ArtifactsReady, now);
        await db.SaveChangesAsync();
        await transport.CompleteAsync(workId, "worker", work.CurrentAttemptId,
            [new AiRegisteredArtifact(Guid.NewGuid(), new string('d',64), 20, "application/json", "RAW_RESULT", "fixture/result.json", null, null)], [], seed.Map, now.AddSeconds(1), Ct);
        var repo = new AiVideoTelemetryRepository(db, TimeProvider.System);
        foreach (var role in new[] { UserRoleCode.ProjectManager, UserRoleCode.Supervisor })
        {
            var roleCode = role.ToDbCode();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET RoleCode={roleCode} WHERE Id={source.OwnerId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET RoleCode={roleCode} WHERE UserId={source.OwnerId}");
            (await repo.ReadScopeAsync(source.OwnerId, role, source.ProjectId, workId, Ct)).WorkId.Should().Be(workId);
            var page = await repo.DiscoverAsync(source.OwnerId, role, source.ProjectId, source.DatasetId, 0, 50, Ct);
            page.Items.Should().ContainSingle(x => x.WorkId == workId && x.State == "Ingested");
            await Expect(403, () => repo.AppendAsync(source.OwnerId, role, source.ProjectId, workId, Request(work), Ct));
        }
    }

    private static OutboxMessage Intent(Guid id) => OutboxMessage.Create(Guid.NewGuid(), "ai.real.dataset_submitted.v1", DateTimeOffset.UtcNow, id, "{}");

    [Fact]
    public async Task DiscoveryPagesPendingAndFailedVideosWithoutDetectionsAndChecksCurrentAssignment()
    {
        var seed = await SeedAsync(3); var source = seed.Sources[0];
        await using var db = _fixture.CreateDbContext(); var transport = new AiVideoWorkRepository(db);
        foreach (var video in seed.Sources) await transport.PrepareAsync(video, Prepared(video, seed.ModelId), seed.Map, Ct);
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var claimed = (await transport.ClaimAsync("worker", now, TimeSpan.FromMinutes(1), Ct))!;
        await transport.ChangeAsync(claimed.Id, "worker", claimed.CurrentAttemptId, AiVideoWorkState.TerminalFailed, "VIDEO_INVALID", now, now, Ct);
        var repo = new AiVideoTelemetryRepository(db, TimeProvider.System);
        var first = await repo.DiscoverAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, source.DatasetId, 0, 2, Ct);
        var second = await repo.DiscoverAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, source.DatasetId, 2, 2, Ct);
        first.Items.Should().HaveCount(2); first.HasMore.Should().BeTrue(); second.Items.Should().ContainSingle(); second.HasMore.Should().BeFalse();
        var all = first.Items.Concat(second.Items).ToArray();
        all.Select(x => x.WorkId).Distinct().Should().HaveCount(3);
        all.Select(x => x.SourceVideoFileId).Should().BeEquivalentTo(seed.Sources.Select(x => x.FileId));
        all.Should().Contain(x => x.State == "Pending"); all.Should().Contain(x => x.State == "TerminalFailed" && x.ErrorCode == "VIDEO_INVALID");
        JsonSerializer.Serialize(first).Should().NotContain("ObjectKey").And.NotContain("AdmissionJson");
        (await db.AIDetections.CountAsync()).Should().Be(0);
        await Expect(404, () => repo.DiscoverAsync(source.OwnerId, UserRoleCode.DroneOperator, Guid.NewGuid(), source.DatasetId, 0, 50, Ct));
        await Expect(422, () => repo.DiscoverAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, source.DatasetId, -1, 50, Ct));
        await Expect(422, () => repo.DiscoverAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, source.DatasetId, 0, 101, Ct));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SurveyAssignments SET EndedAt={DateTimeOffset.UtcNow.AddMinutes(1)}, ReassignmentReason='fixture' WHERE OperatorUserId={source.OwnerId}");
        await Expect(403, () => repo.DiscoverAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, source.DatasetId, 0, 50, Ct));
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.Supervisor)]
    public async Task DiscoveryAuthorizesCurrentManagerAndSupervisorEvenBeforeWorkExists(UserRoleCode role)
    {
        var seed = await SeedAsync(); var source = seed.Sources[0];
        await using var db = _fixture.CreateDbContext(); var code = role.ToDbCode();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET RoleCode={code} WHERE Id={source.OwnerId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET RoleCode={code} WHERE UserId={source.OwnerId}");
        var repo = new AiVideoTelemetryRepository(db, TimeProvider.System);
        (await repo.DiscoverAsync(source.OwnerId, role, source.ProjectId, source.DatasetId, 0, 50, Ct)).Items.Should().BeEmpty();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET MustChangePassword=1 WHERE Id={source.OwnerId}");
        await Expect(403, () => repo.DiscoverAsync(source.OwnerId, role, source.ProjectId, source.DatasetId, 0, 50, Ct));
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("unverified")]
    [InlineData("checksum")]
    public async Task PairedTelemetryReadsVerifiedOffsetAndRejectsBrokenUploadScope(string mutation)
    {
        var seed = await SeedAsync(paired: true, invalidPairedScope: mutation == "scope"); var source = seed.Sources[0];
        await using var db = _fixture.CreateDbContext(); var transport = new AiVideoWorkRepository(db);
        var id = await transport.PrepareAsync(source, Prepared(source, seed.ModelId), seed.Map, Ct);
        var work = (await transport.ClaimAsync("worker", DateTimeOffset.UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), Ct))!;
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.Admitted, work.UpdatedAt);
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.ArtifactsReady, work.UpdatedAt);
        await db.SaveChangesAsync();
        await transport.CompleteAsync(id, "worker", work.CurrentAttemptId,
            [new AiRegisteredArtifact(Guid.NewGuid(), new string('d',64), 20, "application/json", "RAW_RESULT", "fixture/result.json", null, null)], [], seed.Map, work.UpdatedAt.AddSeconds(1), Ct);
        var repo = new AiVideoTelemetryRepository(db, TimeProvider.System);
        if (mutation == "scope")
        {
            await Expect(409, () => repo.ReadScopeAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, id, Ct));
            return;
        }
        var scope = await repo.ReadScopeAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, id, Ct);
        scope.PairedSrt.Should().NotBeNull(); scope.PairedSrt!.OffsetMs.Should().Be(-250);
        var fileId = scope.PairedSrt.File.FileId;
        scope.PairedSrt.File.Sha256.Should().Be(new string('f',64)); scope.PairedSrt.File.Bytes.Should().Be(30);
        if (mutation == "unverified") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET Status={(byte)UploadSessionStatus.Uploading} WHERE FileId={fileId}");
        if (mutation == "checksum") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET ExpectedChecksumSha256={new string('e',64)} WHERE FileId={fileId}");
        await Expect(409, () => repo.ReadScopeAsync(source.OwnerId, UserRoleCode.DroneOperator, source.ProjectId, id, Ct));
    }
    private static AiPreparedVideo Prepared(AiVideoSource source, Guid model)
    {
        var manifest = source.SourceVideoVersion is null ? "{\"source\":\"verified-video\"}" : JsonSerializer.Serialize(new { source = "verified-video", sourceVideoVersion = source.SourceVideoVersion });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        var job = Guid.NewGuid(); var attempt = Guid.NewGuid();
        var admission = JsonSerializer.Serialize(new { projectId = source.ProjectId, datasetId = source.DatasetId, sourceVideoFileId = source.FileId,
            jobId = job, attemptId = attempt, modelVersionId = model, inputSha256 = source.Sha256, inputBytes = source.Bytes, inputDurationMs = 2000L, manifestHash = hash });
        return new(job, attempt, model, "config", admission, manifest, hash);
    }

    private sealed record Seed(AiVideoSource[] Sources, Guid ModelId, Dictionary<string, string> Map);
    private async Task<Seed> SeedAsync(int count = 1, bool intent = true, bool malformed = false, bool paired = false, bool invalidPairedScope = false)
    {
        await using var db = _fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        await _fixture.SeedRolesAsync(db);
        var owner = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = owner, UserName = $"worker-{owner:N}", NormalizedUserName = $"worker-{owner:N}".ToUpperInvariant(), DisplayName = "Worker fixture", PasswordHash = "fixture-password-hash", RoleCode = UserRoleCode.DroneOperator, Status = UserStatus.Active, CreatedAt = now });
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = $"WORKER-{Guid.NewGuid():N}", Name = "Isolated worker repository", EngineeringUtmSrid = 32648, Status = ProjectStatus.Active, CreatedAt = now };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"ROAD-{Guid.NewGuid():N}");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(500000,1200000),new Coordinate(500100,1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now, "fixture");
        var request = SurveyRequest.Create(Guid.NewGuid(), project.Id, road.Id, null, owner, SurveyType.Periodic, SurveyRequestStatus.Completed, now);
        var survey = Survey.Create(Guid.NewGuid(), request.Id, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var files = Enumerable.Range(0, count).Select(_ => StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}", "video.mp4", "video/mp4", 10, new string('a',64), owner, now, null)).ToArray();
        var manifest = JsonSerializer.Serialize(files.Select(f => new { fileId = f.Id, checksumSha256 = f.Checksum, sizeBytes = f.SizeBytes, purpose = "SURVEY_VIDEO" }));
        var telemetry = paired ? StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}.srt", "paired.srt", "application/x-subrip", 30, new string('f',64), owner, now, null) : null;
        if (telemetry is not null) manifest = JsonSerializer.Serialize(files.Append(telemetry).Select(f => new { fileId = f.Id, checksumSha256 = f.Checksum, sizeBytes = f.SizeBytes, purpose = f.Id == telemetry.Id ? "TELEMETRY" : "SURVEY_VIDEO" }));
        if (malformed) manifest = JsonSerializer.Serialize(new object[] { new { purpose = "SURVEY_VIDEO", fileId = "invalid" }, new { purpose = "SURVEY_VIDEO", fileId = files[1].Id.ToString("D"), checksumSha256 = files[1].Checksum, sizeBytes = "invalid" }, new { purpose = "SURVEY_VIDEO", fileId = files[1].Id.ToString("D"), checksumSha256 = files[1].Checksum, sizeBytes = files[1].SizeBytes } });
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, manifest);
        if (telemetry is not null) dataset.SetSubmissionProvenance(owner, JsonSerializer.Serialize(new[] { new { videoFileId = files[0].Id, telemetryFileId = telemetry.Id, timeOffsetMilliseconds = -250 } }));
        var model = AIModelVersion.Create(Guid.NewGuid(), "FT95", Guid.NewGuid().ToString("N"), "fixture/model", null, null, AIModelVersionStatus.Released, null, null);
        var map = Labels.Select((label, i) => (label, code: $"FT95_{i}")).ToDictionary(x => x.label, x => x.code);
        db.AddRange(project, road, version, request, survey, dataset, model); db.Add(SurveyAssignment.Create(Guid.NewGuid(), request.Id, owner, owner, now, now, null, null, null, null)); db.Add(new ProjectMember { Id=Guid.NewGuid(), ProjectId=project.Id, UserId=owner, RoleCode=UserRoleCode.DroneOperator, ValidFrom=DateOnly.FromDateTime(now.UtcDateTime), Status=ProjectMemberStatus.Active });
        foreach (var code in map.Values) db.DefectTypes.Add(DefectType.Create(code, code));
        foreach (var file in telemetry is null ? files : files.Append(telemetry))
        {
            var purpose = file.Id == telemetry?.Id ? "TELEMETRY" : "SURVEY_VIDEO";
            var upload = UploadSession.Create(Guid.NewGuid(), file.Id, owner, file.StorageUri, purpose, file.MimeType, file.SizeBytes, file.Checksum, checked((int)file.SizeBytes), now.AddHours(1));
            var scopeTarget = invalidPairedScope && purpose == "TELEMETRY" ? Guid.NewGuid() : request.Id;
            db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, project.Id, scopeTarget, owner, purpose, now), upload);
            await db.SaveChangesAsync();
            upload.StartUploading("fixture", now); await db.SaveChangesAsync();
            upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync();
            upload.MarkVerified(); await db.SaveChangesAsync();
        }
        if (intent) { db.OutboxMessages.Add(Intent(dataset.Id)); await db.SaveChangesAsync(); }
        return new(files.Select(f => new AiVideoSource(dataset.Id, project.Id, f.Id, owner, f.StorageUri, f.Checksum, f.SizeBytes)).ToArray(), model.Id, map);
    }
}
