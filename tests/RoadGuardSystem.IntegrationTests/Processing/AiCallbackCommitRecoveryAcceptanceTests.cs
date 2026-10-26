using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

public sealed class AiCallbackCommitRecoveryAcceptanceTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public AiCallbackCommitRecoveryAcceptanceTests(IdentitySqlServerFixture fixture) => _fixture = fixture;


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DurableCallbackRecoveryRechecksCurrentAttemptAfterLostAcknowledgement(bool supersede)
    {
        var (scope, jobId, initial) = await NewJobAsync();
        var callback = Callback(scope, jobId, initial, initial.Attempts.Single().Id, "callback-1");
        Snapshot? committed = null;
        var fault = new LostAcknowledgement(async () =>
        {
            committed = await ReadAsync(jobId);
            committed.Status.Should().Be(ProcessingJobStatus.Completed);
            committed.Detections.Should().ContainSingle();
            if (supersede)
            {
                // Explicit controlled failure prerequisite; actual RetryAsync creates attempt2.
                await MarkFailureAsync(jobId);
                var failed = await ReadAsync(jobId);
                (await ExecuteAsync(r => r.RetryAsync(new(scope.ActorId, jobId, "recovery fencing", failed.Version,
                    "retry-1", Hash("retry-1"), null)))).Status.Should().Be(ProcessingJobPersistenceStatus.Success);
            }
        });
        await using (var context = _fixture.CreateDbContext(fault))
        {
            var repository = new ProcessingV2PersistenceService(context, new IdempotencyOperationService(context));
            var recovered = await repository.ReceiveResultAsync(callback);
            recovered.Status.Should().Be(supersede ? ProcessingJobPersistenceStatus.IdempotentConflict : ProcessingJobPersistenceStatus.Replayed);
            if (!supersede)
            {
                recovered.Job!.Id.Should().Be(jobId);
                recovered.Job.Version.Should().Be(committed!.Version);
                recovered.Job.ResultId.Should().Be(committed.Detections.Single().Id);
            }
        }
        fault.Fired.Should().BeTrue(); committed.Should().NotBeNull();
        var after = await ReadAsync(jobId);
        after.Detections.Should().Equal(committed!.Detections);
        committed.ReceiptKeys.Should().HaveCount(2).And.ContainSingle(key => key == "callback-1");
        after.ReceiptKeys.Should().Equal(supersede
            ? committed.ReceiptKeys.Append("retry-1").OrderBy(key => key, StringComparer.Ordinal)
            : committed.ReceiptKeys);
        after.AuditEvents.Should().HaveCount(supersede ? 2 : 1);
        after.OutboxTypes.Should().HaveCount(supersede ? 2 : 1);
        after.Attempts.Should().HaveCount(supersede ? 2 : 1);
        after.Status.Should().Be(supersede ? ProcessingJobStatus.Queued : ProcessingJobStatus.Completed);
        if (!supersede) after.Should().BeEquivalentTo(committed);
        (await ExecuteAsync(r => r.ReceiveResultAsync(callback))).Status.Should().Be(
            supersede ? ProcessingJobPersistenceStatus.IdempotentConflict : ProcessingJobPersistenceStatus.Replayed);
        (await ReadAsync(jobId)).Should().BeEquivalentTo(after);
    }

    private sealed class LostAcknowledgement(Func<Task> afterCommit) : DbTransactionInterceptor
    {
        public bool Fired { get; private set; }
        public override async Task TransactionCommittedAsync(System.Data.Common.DbTransaction transaction,
            TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Fired) return;
            Fired = true;
            await afterCommit();
            throw new IOException("Injected acknowledgement loss after durable callback commit.");
        }
    }

    private async Task<ProcessingJobPersistenceResult> ExecuteAsync(
        Func<ProcessingV2PersistenceService, Task<ProcessingJobPersistenceResult>> action)
    {
        await using var context = _fixture.CreateDbContext();
        return await action(new ProcessingV2PersistenceService(context, new IdempotencyOperationService(context)));
    }

    private async Task<(Scope Scope, Guid JobId, Snapshot Initial)> NewJobAsync()
    {
        var scope = await CreateScopeAsync();
        var key = Guid.NewGuid().ToString("N");
        var created = await ExecuteAsync(r => r.CreateAsync(new(scope.ActorId, scope.DatasetId, "{}", scope.ModelId,
            "pre-v1", "config-v1", "MOCK", key, Hash(key), null)));
        created.Status.Should().Be(ProcessingJobPersistenceStatus.Success);
        return (scope, created.Job!.Id, await ReadAsync(created.Job.Id));
    }

    private async Task MarkFailureAsync(Guid jobId)
    {
        // Controlled state fixture, not evidence of a production failure callback contract.
        await using var context = _fixture.CreateDbContext();
        await context.ProcessingJobs.Where(j => j.Id == jobId).ExecuteUpdateAsync(u =>
            u.SetProperty(j => j.Status, ProcessingJobStatus.RetryableFailure));
    }

    private static ProcessingAiResultRequest Callback(Scope scope, Guid jobId, Snapshot snapshot, Guid attemptId, string key)
        => new(jobId, attemptId, snapshot.ManifestHash, scope.ModelId, "MOCK", scope.RawFileId, scope.Checksum,
            $"[{{\"detectionId\":\"{Guid.NewGuid()}\",\"typeCode\":\"POTHOLE\",\"confidence\":0.9}}]", key);

    private async Task<Snapshot> ReadAsync(Guid jobId)
    {
        await using var context = _fixture.CreateDbContext();
        var job = await context.ProcessingJobs.AsNoTracking().SingleAsync(value => value.Id == jobId);
        var attempts = await context.ProcessingAttempts.AsNoTracking()
            .Where(value => value.ProcessingJobId == jobId).OrderBy(value => value.AttemptNo)
            .Select(value => new Attempt(value.Id, value.AttemptNo, value.EndedAt, value.ErrorType)).ToListAsync();
        var detections = await context.AIDetections.AsNoTracking()
            .Where(value => value.ProcessingJobId == jobId)
            .Select(value => new Detection(value.Id, value.ModelVersionId)).ToListAsync();
        var audit = await context.AuditLogs.AsNoTracking()
            .Where(value => value.EntityType == "ProcessingJob" && value.EntityId == jobId)
            .OrderBy(value => value.OccurredAtUtc).Select(value => value.EventType).ToListAsync();
        var outbox = await context.OutboxMessages.AsNoTracking()
            .Where(value => value.CorrelationId == jobId).OrderBy(value => value.OccurredAtUtc)
            .Select(value => value.MessageType).ToListAsync();
        var receipts = await context.IdempotencyRecords.AsNoTracking()
            .Where(value => value.OperationId == jobId || value.ProjectId == job.ProjectId)
            .OrderBy(value => value.IdempotencyKey).Select(value => value.IdempotencyKey).ToListAsync();
        return new Snapshot(job.Status, Convert.ToBase64String(job.RowVersion), job.ManifestHash,
            attempts, detections, audit, outbox, receipts);
    }

    private async Task<Scope> CreateScopeAsync()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var now = DateTimeOffset.UtcNow;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"RF1005-{Guid.NewGuid():N}",
            Name = "RF-10-05 isolated processing",
            EngineeringUtmSrid = 32648,
            Status = ProjectStatus.Active,
            CreatedAt = now
        };
        var actor = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"rf1005_{Guid.NewGuid():N}",
            DisplayName = "RF-10-05 actor",
            PasswordHash = "fixture-hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = now
        };
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, $"RF1005-ROAD-{Guid.NewGuid():N}");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([
            new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, geometry, now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed,
            SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "fixture-model", $"v1-{Guid.NewGuid():N}", "file:///fixture-model",
            null, null, AIModelVersionStatus.Released, now, actor.Id);
        var checksum = new string('a', 64);
        var file = StoredFile.Create(Guid.NewGuid(), $"objects/rf1005/{Guid.NewGuid():N}", "result.json",
            "application/json", 32, checksum, actor.Id, now, null);
        var fileScope = FileScope.Create(Guid.NewGuid(), file.Id, project.Id, null, actor.Id, "AI_RESULT", now);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor.Id, file.StorageUri,
            "AI_RESULT", "application/json", 32, checksum, 32, now.AddHours(1));
        context.AddRange(project, actor, section, version, survey, dataset, model, file, fileScope, upload);
        if (!await context.DefectTypes.AnyAsync(type => type.Code == "POTHOLE"))
            context.Add(DefectType.Create("POTHOLE", "Pothole"));
        await context.SaveChangesAsync();
        upload.StartUploading("fixture-upload", now);
        await context.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await context.SaveChangesAsync();
        upload.MarkVerified();
        await context.SaveChangesAsync();
        return new Scope(actor.Id, dataset.Id, model.Id, file.Id, checksum);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record Scope(Guid ActorId, Guid DatasetId, Guid ModelId, Guid RawFileId, string Checksum);
    private sealed record Attempt(Guid Id, int AttemptNo, DateTimeOffset? EndedAt, ProcessingAttemptErrorType? ErrorType);
    private sealed record Detection(Guid Id, Guid ModelVersionId);
    private sealed record Snapshot(ProcessingJobStatus Status, string Version, string ManifestHash,
        IReadOnlyList<Attempt> Attempts, IReadOnlyList<Detection> Detections, IReadOnlyList<string> AuditEvents,
        IReadOnlyList<string> OutboxTypes, IReadOnlyList<string> ReceiptKeys);
}
