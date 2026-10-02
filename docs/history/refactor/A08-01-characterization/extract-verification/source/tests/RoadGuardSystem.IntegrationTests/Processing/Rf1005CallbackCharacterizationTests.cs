using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

public sealed class Rf1005CallbackCharacterizationTests : IClassFixture<IdentitySqlServerFixture>
{
    private readonly IdentitySqlServerFixture _fixture;

    public Rf1005CallbackCharacterizationTests(IdentitySqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CurrentAttempt_CallbackReplayAndRetryFromQueued_PreserveObservedSqlEffects()
    {
        var scope = await CreateScopeAsync();
        var create = new ProcessingJobCreateRequest(scope.ActorId, scope.DatasetId, "{}", scope.ModelId,
            "pre-v1", "config-v1", "MOCK", "create-1", Hash("create-1"), null);
        var created = await ExecuteAsync(repository => repository.CreateAsync(create));
        created.Status.Should().Be(ProcessingJobPersistenceStatus.Success);
        var jobId = created.Job!.Id;
        var initial = await ReadAsync(jobId);
        initial.Status.Should().Be(ProcessingJobStatus.Queued);
        initial.Attempts.Should().ContainSingle().Which.AttemptNo.Should().Be(1);
        initial.Detections.Should().BeEmpty();
        initial.AuditEvents.Should().Equal("processing_job_created");
        initial.OutboxTypes.Should().Equal("processing_job.dispatch");
        initial.ReceiptKeys.Should().Equal("create-1");

        var retry = await ExecuteAsync(repository => repository.RetryAsync(new ProcessingJobRetryRequest(
            scope.ActorId, jobId, "temporary failure", initial.Version, "retry-1", Hash("retry-1"), null)));
        retry.Status.Should().Be(ProcessingJobPersistenceStatus.Conflict);
        var afterRetry = await ReadAsync(jobId);
        afterRetry.Status.Should().Be(ProcessingJobStatus.Queued);
        afterRetry.Attempts.Should().Equal(initial.Attempts);
        afterRetry.Detections.Should().BeEmpty();
        afterRetry.OutboxTypes.Should().Equal(initial.OutboxTypes);
        afterRetry.AuditEvents.Should().Equal(initial.AuditEvents);
        afterRetry.ReceiptKeys.Should().Equal("create-1", "retry-1");

        var attemptId = initial.Attempts.Single().Id;
        var detectionId = Guid.NewGuid();
        var detections = $"[{{\"detectionId\":\"{detectionId}\",\"frameFileId\":\"{scope.RawFileId}\",\"timestampMs\":0,\"typeCode\":\"POTHOLE\",\"confidence\":0.9,\"bbox\":[0,0,0.1,0.1]}}]";
        var callback = new ProcessingAiResultRequest(jobId, attemptId, initial.ManifestHash, scope.ModelId,
            "MOCK", scope.RawFileId, scope.Checksum, detections, "callback-1");
        var wrongManifest = callback with { ManifestHash = new string('b', 64), IdempotencyKey = "wrong-manifest" };
        (await ExecuteAsync(repository => repository.ReceiveResultAsync(wrongManifest))).Status
            .Should().Be(ProcessingJobPersistenceStatus.Conflict);
        var afterManifest = await ReadAsync(jobId);
        afterManifest.Status.Should().Be(ProcessingJobStatus.Queued);
        afterManifest.Detections.Should().BeEmpty();
        afterManifest.Attempts.Should().Equal(initial.Attempts);
        afterManifest.AuditEvents.Should().Equal(initial.AuditEvents);
        afterManifest.OutboxTypes.Should().Equal(initial.OutboxTypes);

        var wrongModel = callback with { ModelVersionId = Guid.NewGuid(), IdempotencyKey = "wrong-model" };
        (await ExecuteAsync(repository => repository.ReceiveResultAsync(wrongModel))).Status
            .Should().Be(ProcessingJobPersistenceStatus.Conflict);
        var afterModel = await ReadAsync(jobId);
        afterModel.Status.Should().Be(ProcessingJobStatus.Queued);
        afterModel.Detections.Should().BeEmpty();
        afterModel.Attempts.Should().Equal(initial.Attempts);
        afterModel.ReceiptKeys.Should().Equal("create-1", "retry-1", "wrong-manifest", "wrong-model");

        var wrongSource = callback with { RawResultFileId = Guid.NewGuid(), IdempotencyKey = "wrong-source" };
        (await ExecuteAsync(repository => repository.ReceiveResultAsync(wrongSource))).Status
            .Should().Be(ProcessingJobPersistenceStatus.Conflict);
        var afterSource = await ReadAsync(jobId);
        afterSource.Status.Should().Be(ProcessingJobStatus.Queued);
        afterSource.Detections.Should().BeEmpty();
        afterSource.Attempts.Should().Equal(initial.Attempts);
        afterSource.AuditEvents.Should().Equal(initial.AuditEvents);
        afterSource.OutboxTypes.Should().Equal(initial.OutboxTypes);
        afterSource.ReceiptKeys.Should().Equal("create-1", "retry-1", "wrong-manifest", "wrong-model", "wrong-source");

        var received = await ExecuteAsync(repository => repository.ReceiveResultAsync(callback));
        received.Status.Should().Be(ProcessingJobPersistenceStatus.Success);
        received.Job!.AttemptNumber.Should().Be(1);
        var completed = await ReadAsync(jobId);
        completed.Status.Should().Be(ProcessingJobStatus.Completed);
        completed.Attempts.Should().Equal(initial.Attempts);
        completed.Detections.Should().Equal(new Detection(detectionId, scope.ModelId));
        completed.AuditEvents.Should().Equal(initial.AuditEvents);
        completed.OutboxTypes.Should().Equal(initial.OutboxTypes);
        completed.ReceiptKeys.Should().Equal("callback-1", "create-1", "retry-1", "wrong-manifest", "wrong-model", "wrong-source");

        var replay = await ExecuteAsync(repository => repository.ReceiveResultAsync(callback));
        replay.Status.Should().Be(ProcessingJobPersistenceStatus.Replayed);
        replay.Job!.AttemptNumber.Should().Be(1);
        (await ReadAsync(jobId)).Should().BeEquivalentTo(completed);

        var newKey = await ExecuteAsync(repository => repository.ReceiveResultAsync(
            callback with { IdempotencyKey = "callback-2" }));
        newKey.Status.Should().Be(ProcessingJobPersistenceStatus.InvalidInput);
        (await ReadAsync(jobId)).Should().BeEquivalentTo(completed);
    }

    private async Task<ProcessingJobPersistenceResult> ExecuteAsync(
        Func<ProcessingV2PersistenceService, Task<ProcessingJobPersistenceResult>> action)
    {
        await using var context = _fixture.CreateDbContext();
        return await action(new ProcessingV2PersistenceService(context, new IdempotencyOperationService(context)));
    }

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
            .Where(value => value.IdempotencyKey == "create-1" || value.ProjectId == job.ProjectId &&
                (value.IdempotencyKey == "retry-1" || value.IdempotencyKey == "wrong-manifest" ||
                 value.IdempotencyKey == "wrong-model" || value.IdempotencyKey == "wrong-source" || value.IdempotencyKey == "callback-1" ||
                 value.IdempotencyKey == "callback-2"))
            .OrderBy(value => value.IdempotencyKey).Select(value => value.IdempotencyKey).ToListAsync();
        return new Snapshot(job.Status, Convert.ToBase64String(job.RowVersion), job.ManifestHash,
            attempts, detections, audit, outbox, receipts);
    }

    private async Task<Scope> CreateScopeAsync()
    {
        await using var context = _fixture.CreateDbContext();
        await _fixture.SeedRolesAsync(context);
        var now = DateTimeOffset.UtcNow;
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = $"RF1005-{Guid.NewGuid():N}",
            Name = "RF-10-05 isolated processing", EngineeringUtmSrid = 32648,
            Status = ProjectStatus.Active, CreatedAt = now };
        var actor = new ApplicationUser { Id = Guid.NewGuid(), UserName = $"rf1005_{Guid.NewGuid():N}",
            DisplayName = "RF-10-05 actor", PasswordHash = "fixture-hash", RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active, CreatedAt = now };
        var section = RoadSection.Create(Guid.NewGuid(), project.Id, $"RF1005-ROAD-{Guid.NewGuid():N}");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([
            new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), section.Id, 1, true, geometry, now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed,
            SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "fixture-model", "v1", "file:///fixture-model",
            null, null, AIModelVersionStatus.Released, now, actor.Id);
        var checksum = new string('a', 64);
        var file = StoredFile.Create(Guid.NewGuid(), $"objects/rf1005/{Guid.NewGuid():N}", "result.json",
            "application/json", 32, checksum, actor.Id, now, null);
        var fileScope = FileScope.Create(Guid.NewGuid(), file.Id, project.Id, null, actor.Id, "AI_RESULT", now);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor.Id, file.StorageUri,
            "AI_RESULT", "application/json", 32, checksum, 32, now.AddHours(1));
        context.AddRange(project, actor, section, version, survey, dataset, model, file, fileScope, upload,
            DefectType.Create("POTHOLE", "Pothole"));
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
