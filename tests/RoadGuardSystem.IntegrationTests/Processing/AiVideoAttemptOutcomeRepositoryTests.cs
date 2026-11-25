using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

// Always fixture-owned SQL; no configured shared server connection is eligible.
public sealed class AiVideoAttemptOutcomeRepositoryTests : IAsyncLifetime
{
    private readonly IdentitySqlServerFixture _fixture = new(new SqlServerTestFixture(_ => null, createSpatialProbeSchema: false));
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly string[] Labels = ["alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack"];
    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Theory]
    [InlineData(AiVideoWorkState.RetryableFailed, "WORKER_UNAVAILABLE")]
    [InlineData(AiVideoWorkState.TerminalFailed, "INVALID_RESULT")]
    [InlineData(AiVideoWorkState.Cancelled, null)]
    public async Task TerminalChangeAppendsImmutableAttemptEvidence(AiVideoWorkState state, string? error)
    {
        var seed = await SeedAsync(); var work = await ReadyAsync(seed);
        await using var db = _fixture.CreateDbContext();
        var now = work.UpdatedAt.AddSeconds(1);
        await new AiVideoWorkRepository(db).ChangeAsync(work.Id, "worker", work.CurrentAttemptId, state, error, now, now, Ct);
        var outcome = await db.AiVideoAttemptOutcomes.AsNoTracking().SingleAsync();
        outcome.AttemptId.Should().Be(work.CurrentAttemptId); outcome.WorkId.Should().Be(work.Id);
        outcome.ProcessingJobId.Should().Be(work.ProcessingJobId); outcome.State.Should().Be(state); outcome.ErrorCode.Should().Be(error);
        outcome.AdmissionHash.Should().Be(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(work.AdmissionJson))).ToLowerInvariant());
        outcome.OccurredAt.Should().Be(now);
        await FluentActions.Awaiting(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE AiVideoAttemptOutcomes SET AdmissionHash={new string('b', 64)} WHERE AttemptId={outcome.AttemptId}"))
            .Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>();
        await FluentActions.Awaiting(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM AiVideoAttemptOutcomes WHERE AttemptId={outcome.AttemptId}"))
            .Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>();
        (await db.ProcessingAttempts.SingleAsync()).EndedAt.Should().BeNull("ProcessingAttempt is not rewritten");
        if (state == AiVideoWorkState.RetryableFailed)
        {
            var ledger = await db.AiVideoWorkItems.SingleAsync();
            var oldAdmission = ledger.AdmissionJson;
            var retryId = Guid.NewGuid();
            var admission = System.Text.Json.Nodes.JsonNode.Parse(oldAdmission)!.AsObject(); admission["attemptId"] = retryId;
            ledger.AdvanceAttempt(retryId, admission.ToJsonString(), now.AddSeconds(1));
            db.ProcessingAttempts.Add(ProcessingAttempt.Create(retryId, work.ProcessingJobId, 2, now.AddSeconds(1), null, null, null));
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var retained = await db.AiVideoAttemptOutcomes.SingleAsync();
            retained.AttemptId.Should().Be(outcome.AttemptId); retained.ErrorCode.Should().Be(error); retained.AdmissionHash.Should().Be(outcome.AdmissionHash);
            var retried = await db.AiVideoWorkItems.SingleAsync();
            retried.CurrentAttemptId.Should().Be(retryId); retried.ErrorCode.Should().BeNull(); retried.AdmissionJson.Should().NotBe(oldAdmission);
        }
    }

    [Fact]
    public async Task IngestionCommitsOutcomeAlongsideResultArtifacts()
    {
        var seed = await SeedAsync(); var work = await ReadyAsync(seed);
        await using var db = _fixture.CreateDbContext();
        var now = work.UpdatedAt.AddSeconds(1);
        await new AiVideoWorkRepository(db).CompleteAsync(work.Id, "worker", work.CurrentAttemptId,
            [new AiRegisteredArtifact(Guid.NewGuid(), new string('b', 64), 20, "application/json", "RAW_RESULT", "fixture/result.json", null, null)], [], seed.Map, now, Ct);
        var outcome = await db.AiVideoAttemptOutcomes.SingleAsync();
        outcome.State.Should().Be(AiVideoWorkState.Ingested); outcome.ErrorCode.Should().BeNull();
        (await db.AiVideoWorkItems.SingleAsync()).State.Should().Be(AiVideoWorkState.Ingested);
        (await db.AiWorkerArtifacts.CountAsync()).Should().Be(1);
        db.Remove(outcome);
        await FluentActions.Awaiting(() => db.SaveChangesAsync()).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StaleAttemptDoesNotAppendOutcomeOrChangeLedger()
    {
        var seed = await SeedAsync(); var work = await ReadyAsync(seed);
        await using var db = _fixture.CreateDbContext();
        var now = work.UpdatedAt.AddSeconds(1);
        await FluentActions.Awaiting(() => new AiVideoWorkRepository(db).ChangeAsync(work.Id, "worker", Guid.NewGuid(),
            AiVideoWorkState.TerminalFailed, "INVALID_RESULT", now, now, Ct)).Should().ThrowAsync<InvalidOperationException>();
        (await db.AiVideoAttemptOutcomes.CountAsync()).Should().Be(0);
        (await db.AiVideoWorkItems.SingleAsync()).State.Should().Be(AiVideoWorkState.ArtifactsReady);
    }

    [Fact]
    public async Task OutcomeCollisionRollsBackTerminalJobAndWorkMutation()
    {
        var seed = await SeedAsync(); var work = await ReadyAsync(seed);
        await using var db = _fixture.CreateDbContext();
        var now = work.UpdatedAt.AddSeconds(1);
        // Adversarial pre-existing fact: an attempt outcome can never be
        // overwritten, and failure to append must roll back the whole change.
        db.AiVideoAttemptOutcomes.Add(AiVideoAttemptOutcome.Create(work.CurrentAttemptId, work.Id, work.ProcessingJobId,
            AiVideoWorkState.Cancelled, null, new string('a', 64), now));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await FluentActions.Awaiting(() => new AiVideoWorkRepository(db).ChangeAsync(work.Id, "worker", work.CurrentAttemptId,
            AiVideoWorkState.TerminalFailed, "INVALID_RESULT", now, now, Ct)).Should().ThrowAsync<DbUpdateException>();
        await using var verify = _fixture.CreateDbContext();
        (await verify.AiVideoWorkItems.SingleAsync()).State.Should().Be(AiVideoWorkState.ArtifactsReady);
        (await verify.ProcessingJobs.SingleAsync()).Status.Should().Be(ProcessingJobStatus.Queued);
        (await verify.AiVideoAttemptOutcomes.SingleAsync()).State.Should().Be(AiVideoWorkState.Cancelled);
    }

    private async Task<AiVideoWorkItem> ReadyAsync(Seed seed)
    {
        await using var db = _fixture.CreateDbContext();
        var repo = new AiVideoWorkRepository(db);
        var source = (await repo.NextSourceAsync("config", Ct))!;
        await repo.PrepareAsync(source, Prepared(source, seed.ModelId), seed.Map, Ct);
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var work = (await repo.ClaimAsync("worker", now, TimeSpan.FromMinutes(1), Ct))!;
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.Admitted, now);
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.ArtifactsReady, now);
        await db.SaveChangesAsync();
        return work;
    }

    private static OutboxMessage Intent(Guid id) => OutboxMessage.Create(Guid.NewGuid(), "ai.real.dataset_submitted.v1", DateTimeOffset.UtcNow, id, "{}");
    private static AiPreparedVideo Prepared(AiVideoSource source, Guid model)
    {
        var manifest = source.SourceVideoVersion is null ? "{\"source\":\"verified-video\"}" : JsonSerializer.Serialize(new { source = "verified-video", sourceVideoVersion = source.SourceVideoVersion });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        var job = Guid.NewGuid(); var attempt = Guid.NewGuid();
        var admission = JsonSerializer.Serialize(new
        {
            projectId = source.ProjectId,
            datasetId = source.DatasetId,
            sourceVideoFileId = source.FileId,
            jobId = job,
            attemptId = attempt,
            modelVersionId = model,
            inputSha256 = source.Sha256,
            inputBytes = source.Bytes,
            manifestHash = hash
        });
        return new(job, attempt, model, "config", admission, manifest, hash);
    }

    private sealed record Seed(AiVideoSource[] Sources, Guid ModelId, Dictionary<string, string> Map);
    private async Task<Seed> SeedAsync(int count = 1, bool intent = true, bool malformed = false)
    {
        await using var db = _fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        await _fixture.SeedRolesAsync(db);
        var owner = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = owner, UserName = $"worker-{owner:N}", NormalizedUserName = $"worker-{owner:N}".ToUpperInvariant(), DisplayName = "Worker fixture", PasswordHash = "fixture-password-hash", RoleCode = UserRoleCode.DroneOperator, Status = UserStatus.Active, CreatedAt = now });
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = $"WORKER-{Guid.NewGuid():N}", Name = "Isolated worker repository", EngineeringUtmSrid = 32648, Status = ProjectStatus.Active, CreatedAt = now };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"ROAD-{Guid.NewGuid():N}");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var files = Enumerable.Range(0, count).Select(_ => StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}", "video.mp4", "video/mp4", 10, new string('a', 64), owner, now, null)).ToArray();
        var manifest = JsonSerializer.Serialize(files.Select(f => new { fileId = f.Id, checksumSha256 = f.Checksum, sizeBytes = f.SizeBytes, purpose = "SURVEY_VIDEO" }));
        if (malformed) manifest = JsonSerializer.Serialize(new object[] { new { purpose = "SURVEY_VIDEO", fileId = "invalid" }, new { purpose = "SURVEY_VIDEO", fileId = files[1].Id.ToString("D"), checksumSha256 = files[1].Checksum, sizeBytes = "invalid" }, new { purpose = "SURVEY_VIDEO", fileId = files[1].Id.ToString("D"), checksumSha256 = files[1].Checksum, sizeBytes = files[1].SizeBytes } });
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, manifest);
        var model = AIModelVersion.Create(Guid.NewGuid(), "FT95", Guid.NewGuid().ToString("N"), "fixture/model", null, null, AIModelVersionStatus.Released, null, null);
        var map = Labels.Select((label, i) => (label, code: $"FT95_{i}")).ToDictionary(x => x.label, x => x.code);
        db.AddRange(project, road, version, survey, dataset, model);
        foreach (var code in map.Values) db.DefectTypes.Add(DefectType.Create(code, code));
        foreach (var file in files)
        {
            var upload = UploadSession.Create(Guid.NewGuid(), file.Id, owner, file.StorageUri, "SURVEY_VIDEO", "video/mp4", 10, file.Checksum, 10, now.AddHours(1));
            db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, project.Id, null, owner, "SURVEY_VIDEO", now), upload);
            await db.SaveChangesAsync();
            upload.StartUploading("fixture", now); await db.SaveChangesAsync();
            upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync();
            upload.MarkVerified(); await db.SaveChangesAsync();
        }
        if (intent) { db.OutboxMessages.Add(Intent(dataset.Id)); await db.SaveChangesAsync(); }
        return new(files.Select(f => new AiVideoSource(dataset.Id, project.Id, f.Id, owner, f.StorageUri, f.Checksum, f.SizeBytes)).ToArray(), model.Id, map);
    }
}
