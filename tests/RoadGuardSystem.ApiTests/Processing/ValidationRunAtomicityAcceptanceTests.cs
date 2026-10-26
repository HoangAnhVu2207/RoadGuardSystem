using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Geometries;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Processing;

namespace RoadGuardSystem.ApiTests.Processing;

// Keep queued validation work separate from the shared authentication host's worker.
public sealed class ValidationRunAtomicityAcceptanceTests(AuthenticationSqlServerFixture sql) : IClassFixture<AuthenticationSqlServerFixture>
{
    [Theory]
    [InlineData("rollback")]
    [InlineData("acknowledgement")]
    [InlineData("concurrent")]
    public async Task ValidationReceiptSampleAuditAndDispatchAreAtomic(string mode)
    {
        var manager = await sql.CreateUserAsync("validation-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var outsider = await sql.CreateUserAsync("validation-other-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var supervisor = await sql.CreateUserAsync("validation-super-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var crew = await sql.CreateUserAsync("validation-op-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.DroneOperator);
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), "VAL-" + Guid.NewGuid().ToString("N"), "Isolated validation HTTP", null, 32648, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "ROAD-" + Guid.NewGuid().ToString("N"));
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true,
            new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(1, 1), new Coordinate(2, 2)]), now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var data = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, "[]");
        var model = AIModelVersion.Create(Guid.NewGuid(), "validation-fixture", Guid.NewGuid().ToString("N"), "fixture://validation", null, null, AIModelVersionStatus.Released, now, supervisor.Id);
        // Synthetic measurement provenance is fixture setup, not an executed field-inspection journey.
        var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, null, project.Id, version.Id, null, Guid.NewGuid().ToString("N"), null, "fixture", now, null, "fixture", FieldInspectionSessionStatus.Imported, null);
        var truth = GroundTruthMeasurement.Create(Guid.NewGuid(), session.Id, "sample-1", version.Id, null, null, MeasurementType.DepressionDepth, 10m, "mm",
            new GeometryFactory(new PrecisionModel(), 4326).CreatePoint(new Coordinate(106, 10)), "gauge", null, "fixture", "fixture", now, null, "fixture");
        var derived = DerivedMeasurement.Create(Guid.NewGuid(), data.Id, version.Id, "sample-1", MeasurementType.DepressionDepth, 11m, "mm", null, DerivedMeasurementSourceType.ManualDerived, "fixture", now, DerivedMeasurementStatus.Published);
        var member = new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = manager.Id, RoleCode = UserRoleCode.ProjectManager, Status = ProjectMemberStatus.Active, ValidFrom = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1)) };
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(project, road, version, survey, data, model, session, truth, derived, member);
            await db.SaveChangesAsync();
        }

        var key = Guid.NewGuid().ToString("N"); var correlation = Guid.NewGuid(); var split = Guid.NewGuid();
        var pairs = JsonSerializer.Serialize(new[] { new { groundTruthId = truth.Id, derivedMeasurementId = derived.Id } });
        var request = new ValidationRunCreateRequest(manager.Id, project.Id, model.Id, split.ToString(), "DEPRESSION_DEPTH", "mm", pairs, key, new string('a', 64), correlation);
        async Task<string> Snapshot()
        {
            await using var db = sql.CreateDbContext();
            return JsonSerializer.Serialize(new
            {
                runs = await db.ValidationRuns.AsNoTracking().Where(x => x.ProjectId == project.Id).OrderBy(x => x.Id).ToArrayAsync(),
                samples = await db.MeasurementValidationSamples.AsNoTracking().Where(x => x.GroundTruthMeasurementId == truth.Id).OrderBy(x => x.Id).ToArrayAsync(),
                receipts = await db.IdempotencyRecords.AsNoTracking().Where(x => x.ProjectId == project.Id).OrderBy(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().Where(x => x.ActorUserId == manager.Id).OrderBy(x => x.Id).ToArrayAsync(),
                dispatch = await db.OutboxMessages.AsNoTracking().Where(x => x.MessageType == "validation_run.dispatch" && x.PayloadJson.Contains(model.Id.ToString())).OrderBy(x => x.Id).ToArrayAsync()
            });
        }
        async Task<ValidationRunPersistenceResult> Execute()
        {
            await using var db = sql.CreateDbContext();
            return await new ProcessingV2PersistenceService(db, new IdempotencyOperationService(db)).CreateValidationAsync(request);
        }
        var before = await Snapshot();
        ValidationRunPersistenceResult result;
        if (mode == "concurrent")
        {
            var results = await Task.WhenAll(Execute(), Execute());
            Assert.Equal(1, results.Count(x => x.Status == ProcessingJobPersistenceStatus.Success));
            Assert.Equal(1, results.Count(x => x.Status == ProcessingJobPersistenceStatus.Replayed));
            Assert.Equal(results[0].Run, results[1].Run); result = results[0];
        }
        else
        {
            var probe = new ValidationCommitFailure(key, mode == "acknowledgement");
            await using var db = CommitFailureDbContext.Create(sql.ConnectionString, probe);
            var repo = new ProcessingV2PersistenceService(db, new IdempotencyOperationService(db));
            if (mode == "rollback")
            {
                await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException>(() => repo.CreateValidationAsync(request));
                Assert.Equal(3, probe.Failures); Assert.Equal(before, await Snapshot()); return;
            }
            result = await repo.CreateValidationAsync(request);
            Assert.Equal(1, probe.Failures); Assert.Equal(ProcessingJobPersistenceStatus.Replayed, result.Status);
        }
        Assert.NotNull(result.Run);
        var committed = await Snapshot();
        var replay = await Execute(); Assert.Equal(ProcessingJobPersistenceStatus.Replayed, replay.Status); Assert.Equal(result.Run, replay.Run);
        Assert.Equal(committed, await Snapshot());
        await using var verify = sql.CreateDbContext(); var run = Assert.Single(await verify.ValidationRuns.AsNoTracking().Where(x => x.ProjectId == project.Id).ToArrayAsync());
        Assert.Equal(result.Run.Id, run.Id); Assert.Equal(ValidationRunStatus.Queued, run.Status);
        Assert.Equal(Convert.ToBase64String(run.RowVersion), result.Run.Version);
        var sample = Assert.Single(await verify.MeasurementValidationSamples.Where(x => x.ValidationRunId == run.Id).ToArrayAsync());
        Assert.Equal(truth.Id, sample.GroundTruthMeasurementId); Assert.Equal(derived.Id, sample.DerivedMeasurementId); Assert.Equal(1m, sample.SignedError);
        var receipt = Assert.Single(await verify.IdempotencyRecords.Where(x => x.ProjectId == project.Id).ToArrayAsync());
        Assert.Equal(run.Id, receipt.OperationId); Assert.Equal(manager.Id, receipt.ActorUserId); Assert.Equal("ValidationRunCreated", receipt.Operation); Assert.Equal(key, receipt.IdempotencyKey); Assert.Equal(request.RequestFingerprint, receipt.RequestFingerprint);
        var audit = Assert.Single(await verify.AuditLogs.Where(x => x.EntityId == run.Id).ToArrayAsync());
        Assert.Equal("validation_run_created", audit.EventType); Assert.Equal(manager.Id, audit.ActorUserId); Assert.Equal(correlation, audit.CorrelationId); Assert.Equal("ValidationRun", audit.EntityType); Assert.Equal("p2-034", audit.Source); Assert.Equal("{}", audit.AfterSnapshot); Assert.Null(audit.BeforeSnapshot);
        var message = Assert.Single(await verify.OutboxMessages.Where(x => x.CorrelationId == run.Id && x.MessageType == "validation_run.dispatch").ToArrayAsync());
        using var dispatch = JsonDocument.Parse(message.PayloadJson);
        Assert.Equal(new[] { "DatasetSplitId", "ModelVersionId", "validationRunId" }.Order(), dispatch.RootElement.EnumerateObject().Select(x => x.Name).Order());
        Assert.Equal(run.Id, dispatch.RootElement.GetProperty("validationRunId").GetGuid()); Assert.Equal(model.Id, dispatch.RootElement.GetProperty("ModelVersionId").GetGuid()); Assert.Equal(split.ToString(), dispatch.RootElement.GetProperty("DatasetSplitId").GetString());
    }

    private sealed class ValidationCommitFailure(string key, bool afterCommit) : DbTransactionInterceptor
    {
        public int Failures { get; private set; }
        private bool HasReceipt(DbContext? context) => context?.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().Local.Any(r => r.IdempotencyKey == key) == true;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!afterCommit && HasReceipt(eventData.Context)) { Failures++; throw new CommitFailureTransientException("Validation precommit rollback probe."); }
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (afterCommit && Failures == 0 && HasReceipt(eventData.Context)) { Failures++; throw new CommitFailureTransientException("Validation lost acknowledgement probe."); }
            return Task.CompletedTask;
        }
    }
}
