using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

// Always use a fixture-owned container, ignoring any configured shared SQL server.
public sealed class WorkerLedgerPersistenceTests : IAsyncLifetime
{
    private readonly IdentitySqlServerFixture _fixture = new(
        new SqlServerTestFixture(_ => null, createSpatialProbeSchema: false));

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task Migration_DownUpPreservesExistingProcessingFacts()
    {
        var work = await SeedAsync();
        await using var context = _fixture.CreateDbContext();
        var migrations = context.Database.GetMigrations().ToArray();
        var ledgerIndex = Array.FindIndex(migrations, x => x.EndsWith("_AddAiWorkerTransportLedger", StringComparison.Ordinal));
        ledgerIndex.Should().BeGreaterThan(0);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(migrations[ledgerIndex - 1]);
        (await context.ProcessingJobs.CountAsync(x => x.Id == work.ProcessingJobId)).Should().Be(1);
        (await context.ProcessingAttempts.CountAsync(x => x.Id == work.CurrentAttemptId)).Should().Be(1);
        await migrator.MigrateAsync();
        (await context.AiVideoWorkItems.CountAsync()).Should().Be(0);
        (await context.AiWorkerArtifacts.CountAsync()).Should().Be(0);
        (await context.ProcessingAttempts.CountAsync(x => x.Id == work.CurrentAttemptId)).Should().Be(1);
    }

    [Fact]
    public async Task FreshMigration_RoundTripsLedgerAndExplicitRetryWithoutRewritingPriorArtifacts()
    {
        var work = await SeedAsync();
        var artifact = Artifact(work);
        await using (var context = _fixture.CreateDbContext())
        {
            context.AiWorkerArtifacts.Add(artifact);
            await context.SaveChangesAsync(); // HasTrigger must avoid incompatible SQL OUTPUT.
        }

        var retryId = Guid.NewGuid();
        var at = work.CreatedAt.AddSeconds(1);
        await using (var context = _fixture.CreateDbContext())
        {
            var restored = await context.AiVideoWorkItems.SingleAsync(x => x.Id == work.Id);
            restored.DispatchKey.Should().Be(work.DispatchKey);
            restored.AdmissionJson.Should().Be("{\"version\":1}");
            restored.AcquireLease("worker", at, at.AddMinutes(1));
            restored.Transition("worker", restored.CurrentAttemptId, AiVideoWorkState.RetryableFailed, at, errorCode: "busy");
            await context.SaveChangesAsync();
            context.ProcessingAttempts.Add(ProcessingAttempt.Create(retryId, work.ProcessingJobId, 2, at, null, null, null));
            await context.SaveChangesAsync();
            restored.AdvanceAttempt(retryId, "{\"version\":2}", at);
            await context.SaveChangesAsync();
        }
        await using var verify = _fixture.CreateDbContext();
        var persisted = await verify.AiVideoWorkItems.AsNoTracking().SingleAsync(x => x.Id == work.Id);
        persisted.CurrentAttemptId.Should().Be(retryId);
        persisted.AdmissionJson.Should().Be("{\"version\":2}");
        persisted.State.Should().Be(AiVideoWorkState.Pending);
        persisted.LeaseOwner.Should().BeNull();
        (await verify.AiWorkerArtifacts.SingleAsync(x => x.Id == artifact.Id)).AttemptId.Should().Be(work.CurrentAttemptId);
        (await verify.ProcessingAttempts.CountAsync(x => x.ProcessingJobId == work.ProcessingJobId)).Should().Be(2);
    }

    [Fact]
    public async Task RowVersion_RejectsStaleLeaseWriter()
    {
        var work = await SeedAsync();
        await using var first = _fixture.CreateDbContext();
        await using var second = _fixture.CreateDbContext();
        var left = await first.AiVideoWorkItems.SingleAsync(x => x.Id == work.Id);
        var right = await second.AiVideoWorkItems.SingleAsync(x => x.Id == work.Id);
        var now = work.CreatedAt.AddSeconds(1);
        left.AcquireLease("first", now, now.AddMinutes(1));
        right.AcquireLease("second", now, now.AddMinutes(1));
        await first.SaveChangesAsync();
        await FluentActions.Awaiting(() => second.SaveChangesAsync()).Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var verify = _fixture.CreateDbContext();
        (await verify.AiVideoWorkItems.SingleAsync(x => x.Id == work.Id)).LeaseOwner.Should().Be("first");
    }

    [Fact]
    public async Task UniqueConstraints_RejectDuplicateDispatchAndAttemptArtifact()
    {
        var work = await SeedAsync();
        var secondJobId = Guid.NewGuid();
        var secondAttemptId = Guid.NewGuid();
        await using (var setup = _fixture.CreateDbContext())
        {
            var sourceJob = await setup.ProcessingJobs.SingleAsync(x => x.Id == work.ProcessingJobId);
            setup.AddRange(ProcessingJob.CreateQueued(secondJobId, sourceJob.ProcessingBlockId,
                    sourceJob.ModelVersionId, work.ProjectId, new string('a', 64), "{}", "REAL"),
                ProcessingAttempt.Create(secondAttemptId, secondJobId, 1, work.CreatedAt, null, null, null));
            await setup.SaveChangesAsync();
        }
        await using (var duplicate = _fixture.CreateDbContext())
        {
            duplicate.AiVideoWorkItems.Add(AiVideoWorkItem.Create(Guid.NewGuid(), work.DatasetId, work.SourceVideoFileId,
                work.ProjectId, secondJobId, secondAttemptId, work.LogicalConfigurationIdentity, "{}", work.CreatedAt));
            await FluentActions.Awaiting(() => duplicate.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
        }
        await using (var duplicateJob = _fixture.CreateDbContext())
        {
            duplicateJob.AiVideoWorkItems.Add(AiVideoWorkItem.Create(Guid.NewGuid(), work.DatasetId, work.SourceVideoFileId,
                work.ProjectId, work.ProcessingJobId, work.CurrentAttemptId, "different-config", "{}", work.CreatedAt));
            await FluentActions.Awaiting(() => duplicateJob.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
        }
        var artifact = Artifact(work);
        await using (var context = _fixture.CreateDbContext())
        {
            context.AiWorkerArtifacts.Add(artifact);
            await context.SaveChangesAsync();
        }
        await using var again = _fixture.CreateDbContext();
        again.AiWorkerArtifacts.Add(AiWorkerArtifact.Create(Guid.NewGuid(), work.Id, work.CurrentAttemptId,
            artifact.ArtifactId, new string('b', 64), 10, "RAW_RESULT"));
        await FluentActions.Awaiting(() => again.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ArtifactGuard_AllowsFirstBindingAndRejectsRemapUnbindMutationAndDeletion()
    {
        var work = await SeedAsync();
        var artifact = Artifact(work);
        var other = StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}", "result.json", "application/json",
            10, new string('a', 64), null, work.CreatedAt, null);
        await using (var context = _fixture.CreateDbContext())
        {
            context.AddRange(artifact, other);
            await context.SaveChangesAsync();
            artifact.MarkRegistered(other.Id);
            await context.SaveChangesAsync();
        }
        await using var sql = _fixture.CreateDbContext();
        await sql.Database.ExecuteSqlInterpolatedAsync($"UPDATE AiWorkerArtifacts SET FileId = {other.Id} WHERE Id = {artifact.Id}");
        await FluentActions.Awaiting(() => sql.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AiWorkerArtifacts SET FileId = {work.SourceVideoFileId} WHERE Id = {artifact.Id}"))
            .Should().ThrowAsync<SqlException>();
        await FluentActions.Awaiting(() => sql.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AiWorkerArtifacts SET FileId = NULL WHERE Id = {artifact.Id}"))
            .Should().ThrowAsync<SqlException>();
        await FluentActions.Awaiting(() => sql.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AiWorkerArtifacts SET Bytes = Bytes + 1 WHERE Id = {artifact.Id}"))
            .Should().ThrowAsync<SqlException>();
        await FluentActions.Awaiting(() => sql.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE AiWorkerArtifacts SET Kind = 'RAW_RESULT ' WHERE Id = {artifact.Id}"))
            .Should().ThrowAsync<SqlException>();
        await FluentActions.Awaiting(() => sql.AiWorkerArtifacts.Where(x => x.Id == artifact.Id).ExecuteDeleteAsync())
            .Should().ThrowAsync<SqlException>();
        var persisted = await sql.AiWorkerArtifacts.AsNoTracking().SingleAsync(x => x.Id == artifact.Id);
        persisted.FileId.Should().Be(other.Id);
        persisted.Bytes.Should().Be(10);
    }

    [Fact]
    public async Task ForeignKeys_RejectMissingAttemptAndFile()
    {
        var work = await SeedAsync();
        await using var context = _fixture.CreateDbContext();
        context.AiWorkerArtifacts.Add(AiWorkerArtifact.Create(Guid.NewGuid(), work.Id, Guid.NewGuid(),
            Guid.NewGuid().ToString("D"), new string('a', 64), 10, "RAW_RESULT"));
        await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();
        var artifact = Artifact(work);
        artifact.MarkRegistered(Guid.NewGuid());
        context.AiWorkerArtifacts.Add(artifact);
        await FluentActions.Awaiting(() => context.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();
    }

    private static AiWorkerArtifact Artifact(AiVideoWorkItem work) => AiWorkerArtifact.Create(
        Guid.NewGuid(), work.Id, work.CurrentAttemptId, Guid.NewGuid().ToString("D"), new string('a', 64), 10, "RAW_RESULT");

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
