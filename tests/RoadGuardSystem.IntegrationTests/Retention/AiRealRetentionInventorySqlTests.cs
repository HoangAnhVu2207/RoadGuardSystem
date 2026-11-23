using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Retention;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Retention;

public sealed class AiRealRetentionInventorySqlTests : IAsyncLifetime
{
    private readonly IdentitySqlServerFixture _fixture = new(new SqlServerTestFixture(_ => null, createSpatialProbeSchema: false));
    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Theory]
    [InlineData("RAW_RESULT")]
    [InlineData("FRAME")]
    [InlineData("TELEMETRY_SRT")]
    public async Task RetainsRegisteredRealArtifactsAndSourceAcrossRetriesAndTerminalFailure(string kind)
    {
        var work = await SeedAsync();
        await using var db = _fixture.CreateDbContext();
        var file = StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}", "artifact.bin", "application/octet-stream", 20, new string('b', 64), null, work.CreatedAt, null);
        var artifact = AiWorkerArtifact.Create(Guid.NewGuid(), work.Id, work.CurrentAttemptId, Guid.NewGuid().ToString("D"), file.Checksum, 20, kind,
            kind == "FRAME" ? 100 : null, kind == "FRAME" ? 100 : null);
        artifact.MarkRegistered(file.Id);
        db.AddRange(file, artifact); await db.SaveChangesAsync();
        var current = await db.AiVideoWorkItems.SingleAsync(x => x.Id == work.Id);
        var now = current.CreatedAt.AddMinutes(1);
        current.AcquireLease("fixture", now, now.AddMinutes(1));
        current.Transition("fixture", current.CurrentAttemptId, AiVideoWorkState.RetryableFailed, now, errorCode: "RETRY");
        var retry = ProcessingAttempt.Create(Guid.NewGuid(), current.ProcessingJobId, 2, now, null, null, null);
        db.ProcessingAttempts.Add(retry); await db.SaveChangesAsync();
        current.AdvanceAttempt(retry.Id, "{}", now);
        current.AcquireLease("fixture", now, now.AddMinutes(1));
        current.Transition("fixture", retry.Id, AiVideoWorkState.TerminalFailed, now, errorCode: "FAILED");
        await db.SaveChangesAsync();
        var inventory = new AiRetentionInventoryContributor(db);
        var result = await inventory.ReadAsync(file.Id, default);
        Assert.True(result.Complete);
        Assert.Contains(result.References, x => x.Kind == "AI_WORKER_ARTIFACT" && x.Id == artifact.Id && x.ProjectId == work.ProjectId);
        Assert.Contains((await inventory.ReadAsync(work.SourceVideoFileId, default)).References,
            x => x.Kind == "AI_VIDEO_SOURCE" && x.Id == work.Id && x.ProjectId == work.ProjectId);
        var files = await inventory.KnownProjectFilesAsync(work.ProjectId, default);
        Assert.Contains(file.Id, files); Assert.Contains(work.SourceVideoFileId, files);
        Assert.Empty(await inventory.KnownProjectFilesAsync(Guid.NewGuid(), default));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("hash")]
    [InlineData("case")]
    [InlineData("bytes")]
    [InlineData("valid")]
    public async Task ExternalSrtRequiresUniqueExactStoredFileEvidence(string mapping)
    {
        var work = await SeedAsync();
        await using var db = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(db);
        var actor = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = actor, UserName = $"retention-{actor:N}", NormalizedUserName = $"retention-{actor:N}".ToUpperInvariant(), DisplayName = "Retention fixture", PasswordHash = "fixture", RoleCode = UserRoleCode.DroneOperator, Status = UserStatus.Active, CreatedAt = work.CreatedAt });
        var key = $"fixture/{Guid.NewGuid():N}/original.srt";
        StoredFile? file = null;
        if (mapping != "missing")
        {
            file = StoredFile.Create(Guid.NewGuid(), mapping == "case" ? key.ToUpperInvariant() : key, "original.srt", "application/x-subrip", mapping == "bytes" ? 21 : 20, new string(mapping == "hash" ? 'c' : 'b',64), null, work.CreatedAt, null);
            db.Files.Add(file);
        }
        var revision = AiVideoTelemetryRevision.Create(Guid.NewGuid(), work.Id, 1, null, actor, work.CreatedAt, new string('a',64), "EXTERNAL_SRT",
            "[{\"startMs\":0,\"endMs\":1000,\"latitude\":10,\"longitude\":106}]", 1000, key, new string('b',64), 20, 0, null, new string('c',64), new string('d',64));
        db.AiVideoTelemetryRevisions.Add(revision); await db.SaveChangesAsync();
        var inventory = new AiRetentionInventoryContributor(db);
        var result = await inventory.ReadAsync(work.SourceVideoFileId, default);
        Assert.Equal(mapping == "valid", result.Complete);
        if (mapping == "valid")
        {
            Assert.Contains((await inventory.ReadAsync(file!.Id, default)).References,
                x => x.Kind == "AI_EXTERNAL_SRT" && x.Id == revision.Id && x.ProjectId == work.ProjectId);
            Assert.Contains(file.Id, await inventory.KnownProjectFilesAsync(work.ProjectId, default));
        }
        else Assert.Contains("AI_EXTERNAL_SRT_FILE_UNRESOLVED", result.ReasonCodes);
    }
    private async Task<AiVideoWorkItem> SeedAsync()
    {
        await using var context = _fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = $"WORKER-{Guid.NewGuid():N}",
            Name = "Isolated worker ledger", EngineeringUtmSrid = 32648, Status = ProjectStatus.Active, CreatedAt = now };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"ROAD-{Guid.NewGuid():N}");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([
            new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed,
            SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "worker-fixture", Guid.NewGuid().ToString("N"),
            "fixture/model", null, null, AIModelVersionStatus.Draft, null, null);
        var file = StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}", "video.mp4", "video/mp4",
            10, new string('a', 64), null, now, null);
        var block = ProcessingBlock.Create(Guid.NewGuid(), dataset.Id, 1, "{}");
        var job = ProcessingJob.CreateQueued(Guid.NewGuid(), block.Id, model.Id, project.Id, new string('a', 64), "{}", "REAL");
        var attempt = ProcessingAttempt.Create(Guid.NewGuid(), job.Id, 1, now, null, null, null);
        var work = AiVideoWorkItem.Create(Guid.NewGuid(), dataset.Id, file.Id, project.Id, job.Id,
            attempt.Id, "model-v1/config-v1", "{\"version\":1}", now);
        context.AddRange(project, road, version, survey, dataset, model, file, block, job, attempt, work);
        await context.SaveChangesAsync();
        return work;
    }
}



\n