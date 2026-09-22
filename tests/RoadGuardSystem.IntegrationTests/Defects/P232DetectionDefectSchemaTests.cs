using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Spatial;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Defects;

[Trait("TaskId", "P2-32")]
public sealed class P232DetectionDefectSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly GeometryFactory GeometryFactory = new(
        new PrecisionModel(),
        SpatialConstants.UtmZone48NSrid);
    private readonly IdentitySqlServerFixture _fixture;

    public P232DetectionDefectSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-32: detection, defect, task and verification log round-trip through SQL Server")]
    public async Task DetectionDefectTaskAndLog_ValidRecords_RoundTrip()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var geometry = GeometryFactory.CreatePoint(new Coordinate(588550, 2325050));
        var now = DateTimeOffset.UtcNow;
        var detection = AIDetection.Create(
            Guid.NewGuid(), scope.ProcessingJobId, scope.ModelVersionId, scope.RoadSectionVersionId,
            geometry, scope.DefectTypeCode, 0.875m, 1.2m, 3.4m, "{\"source\":\"mock\"}");
        var defect = Defect.Create(
            Guid.NewGuid(), scope.ProjectId, scope.RoadSectionVersionId, detection.Id,
            scope.DefectTypeCode, scope.CauseCategoryCode, DefectSeverity.High, DefectStatus.Open,
            geometry, now);
        var task = FieldInspectionTask.Create(
            Guid.NewGuid(), "P232-TASK-001", scope.ProjectId, defect.Id, scope.SurveyId,
            scope.RoadSectionVersionId, 1, "{\"points\":[1,2]}", "Use calibrated gauge.", null,
            now.AddDays(2), FieldInspectionTaskStatus.NewAssigned, scope.UserId, null, null, null, null);
        var log = DefectVerificationLog.Create(
            Guid.NewGuid(), null, detection.Id, DefectVerificationAction.PreliminaryKeep,
            "{\"status\":\"new\"}", "{\"status\":\"kept\"}", null, null, scope.UserId,
            "Retained for PM review.");

        context.AddRange(detection, defect, task, log);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await context.Set<AIDetection>().AsNoTracking().SingleAsync(item => item.Id == detection.Id))
            .RawPayload.Should().Be("{\"source\":\"mock\"}");
        (await context.Set<Defect>().AsNoTracking().SingleAsync(item => item.Id == defect.Id))
            .Status.Should().Be(DefectStatus.Open);
        (await context.Set<FieldInspectionTask>().AsNoTracking().SingleAsync(item => item.Id == task.Id))
            .TaskCode.Should().Be("P232-TASK-001");
        (await context.Set<DefectVerificationLog>().AsNoTracking().SingleAsync(item => item.Id == log.Id))
            .AIDetectionId.Should().Be(detection.Id);
    }

    [Fact(DisplayName = "P2-32: SQL Server exposes schema backstops and rejects invalid values")]
    public async Task P232Schema_InvalidState_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var tableCount = await context.Database.SqlQueryRaw<int>(
            """
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.tables
            WHERE [name] IN ('Defects', 'AIDetections', 'DefectVerificationLogs', 'FieldInspectionTasks')
            """).SingleAsync();
        tableCount.Should().Be(4);

        var checkCount = await context.Database.SqlQueryRaw<int>(
            """
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.check_constraints
            WHERE [name] IN (
                'CK_AIDetections_Confidence', 'CK_AIDetections_RawPayload_Json',
                'CK_Defects_Status', 'CK_DefectVerificationLogs_ExactlyOneTarget',
                'CK_FieldInspectionTasks_Status', 'CK_FieldInspectionTasks_MeasurementScope_Json')
            """).SingleAsync();
        checkCount.Should().Be(6);

        var triggerCount = await context.Database.SqlQueryRaw<int>(
            """
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.triggers
            WHERE [name] IN ('TR_AIDetections_Immutable', 'TR_DefectVerificationLogs_AppendOnly')
            """).SingleAsync();
        triggerCount.Should().Be(2);

        var invalidConfidence = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [AIDetections]
                ([Id], [ProcessingJobId], [ModelVersionId], [Confidence], [RawPayload])
            VALUES ({Guid.NewGuid()}, {scope.ProcessingJobId}, {scope.ModelVersionId}, {2.0m}, {"{}"})
            """);
        await invalidConfidence.Should().ThrowAsync<SqlException>();

        var invalidTaskStatus = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [FieldInspectionTasks]
                ([Id], [TaskCode], [ProjectId], [DefectId], [SurveyId], [RoadSectionVersionId],
                 [RequiredMeasurementType], [MeasurementScope], [DueAt], [Status], [AssignedByUserId])
            VALUES ({Guid.NewGuid()}, {"P232-INVALID"}, {scope.ProjectId}, {scope.DefectId}, {scope.SurveyId},
                {scope.RoadSectionVersionId}, {1}, {"{}"}, {DateTimeOffset.UtcNow}, {99}, {scope.UserId})
            """);
        await invalidTaskStatus.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-32: retained detection key is unique and raw payload is immutable")]
    public async Task RetainedDetection_UniqueAndImmutable_IsEnforcedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var detection = AIDetection.Create(
            Guid.NewGuid(), scope.ProcessingJobId, scope.ModelVersionId, scope.RoadSectionVersionId,
            null, scope.DefectTypeCode, 0.7m, null, null, "{\"source\":\"mock\"}");
        var defect = Defect.Create(
            Guid.NewGuid(), scope.ProjectId, scope.RoadSectionVersionId, detection.Id,
            scope.DefectTypeCode, scope.CauseCategoryCode, DefectSeverity.Medium, DefectStatus.Open,
            GeometryFactory.CreatePoint(new Coordinate(588550, 2325050)), DateTimeOffset.UtcNow);
        context.AddRange(detection, defect);
        await context.SaveChangesAsync();

        var mutatePayload = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [AIDetections] SET [RawPayload] = {"{\"changed\":true}"}
            WHERE [Id] = {detection.Id}
            """);
        await mutatePayload.Should().ThrowAsync<SqlException>();

        var duplicateRetained = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [Defects]
                ([Id], [ProjectId], [RoadSectionVersionId], [SourceAIDetectionId], [DefectTypeCode], [Severity], [Status])
            VALUES ({Guid.NewGuid()}, {scope.ProjectId}, {scope.RoadSectionVersionId}, {detection.Id},
                {scope.DefectTypeCode}, {(byte)DefectSeverity.Medium}, {(byte)DefectStatus.Open})
            """);
        await duplicateRetained.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-32: repository commits the review set and replays the same idempotency key")]
    public async Task Repository_CommitAndReplay_IsAtomicAndIdempotent()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var request = CreatePersistenceRequest(scope, "p232-replay-key", new string('a', 64));
        var repository = new DetectionReviewPersistenceService(context, new IdempotencyOperationService(context));

        var first = await repository.PersistAsync(request);
        var replay = await repository.PersistAsync(request);

        first.Status.Should().Be(DetectionReviewPersistenceStatus.Executed);
        replay.Status.Should().Be(DetectionReviewPersistenceStatus.Replayed);
        (await context.AIDetections.CountAsync(item => item.ProcessingJobId == scope.ProcessingJobId)).Should().Be(1);
        (await context.Defects.CountAsync(item => item.ProjectId == scope.ProjectId)).Should().Be(1);
        (await context.FieldInspectionTasks.CountAsync(item => item.ProjectId == scope.ProjectId)).Should().Be(1);
        (await context.DefectVerificationLogs.CountAsync(item => item.DefectId == request.Defect.Id)).Should().Be(1);
        (await context.OutboxMessages.CountAsync(message => message.CorrelationId == request.CorrelationId)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-32: changed idempotency payload returns conflict without a second write")]
    public async Task Repository_ChangedFingerprint_ReturnsConflict()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var repository = new DetectionReviewPersistenceService(context, new IdempotencyOperationService(context));
        var firstRequest = CreatePersistenceRequest(scope, "p232-conflict-key", new string('b', 64));
        var first = await repository.PersistAsync(firstRequest);
        var conflict = await repository.PersistAsync(CreatePersistenceRequest(scope, "p232-conflict-key", new string('c', 64)));

        first.Status.Should().Be(DetectionReviewPersistenceStatus.Executed);
        conflict.Status.Should().Be(DetectionReviewPersistenceStatus.IdempotencyConflict);
        (await context.AIDetections.CountAsync(item => item.ProcessingJobId == scope.ProcessingJobId)).Should().Be(1);
        (await context.OutboxMessages.CountAsync(message => message.CorrelationId == firstRequest.CorrelationId)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-32: duplicate retained detection rolls back the complete second set")]
    public async Task Repository_DuplicateRetainedDetection_RollsBackWithoutOrphans()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var repository = new DetectionReviewPersistenceService(context, new IdempotencyOperationService(context));
        var first = CreatePersistenceRequest(scope, "p232-duplicate-a", new string('d', 64));
        await repository.PersistAsync(first);
        var duplicate = CreatePersistenceRequest(scope, "p232-duplicate-b", new string('e', 64), first.Defect.SourceAIDetectionId!.Value);

        var result = await repository.PersistAsync(duplicate);

        result.Status.Should().Be(DetectionReviewPersistenceStatus.DuplicateRetainedDetection);
        (await context.AIDetections.CountAsync(item => item.ProcessingJobId == scope.ProcessingJobId)).Should().Be(1);
        (await context.Defects.CountAsync(item => item.ProjectId == scope.ProjectId)).Should().Be(1);
        (await context.FieldInspectionTasks.CountAsync(item => item.ProjectId == scope.ProjectId)).Should().Be(1);
        (await context.OutboxMessages.CountAsync(message => message.CorrelationId == first.CorrelationId)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-32: missing verification actor rolls back all aggregate writes")]
    public async Task Repository_InvalidReference_RollsBackAllWrites()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var request = CreatePersistenceRequest(scope, "p232-invalid-reference", new string('f', 64), verifiedByUserId: Guid.NewGuid());
        var repository = new DetectionReviewPersistenceService(context, new IdempotencyOperationService(context));

        var result = await repository.PersistAsync(request);

        result.Status.Should().Be(DetectionReviewPersistenceStatus.InvalidReference);
        (await context.AIDetections.CountAsync(item => item.ProcessingJobId == scope.ProcessingJobId)).Should().Be(0);
        (await context.Defects.CountAsync(item => item.ProjectId == scope.ProjectId)).Should().Be(0);
        (await context.FieldInspectionTasks.CountAsync(item => item.ProjectId == scope.ProjectId)).Should().Be(0);
        (await context.DefectVerificationLogs.CountAsync(item => item.DefectId == request.Defect.Id)).Should().Be(0);
        (await context.OutboxMessages.CountAsync(message => message.CorrelationId == request.CorrelationId)).Should().Be(0);
    }

    [Fact(DisplayName = "P2-32: migration downgrades and reapplies the detection schema")]
    public async Task MigrationLifecycle_DowngradesAndReapplies()
    {
        await using var context = _fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync("20260921182227_P231ProcessingAndOutboxDelivery");
        (await CountP232TablesAsync(context)).Should().Be(0);

        await context.Database.MigrateAsync();
        (await CountP232TablesAsync(context)).Should().Be(3);
    }

    private async Task<P232Scope> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P232-{Guid.NewGuid():N}",
            Name = "P2-32 detection fixture",
            EngineeringUtmSrid = SpatialConstants.UtmZone48NSrid,
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p232_user_{Guid.NewGuid():N}",
            DisplayName = "P2-32 fixture user",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"P232-ROAD-{Guid.NewGuid():N}");
        var roadVersion = RoadSectionVersion.Create(
            Guid.NewGuid(), road.Id, 1, true,
            GeometryFactory.CreateLineString(new[]
            {
                new Coordinate(588500, 2325000),
                new Coordinate(588600, 2325100)
            }),
            DateTimeOffset.UtcNow,
            "P2-32 fixture version");
        var survey = Survey.Create(
            Guid.NewGuid(), null, project.Id, roadVersion.Id, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var dataVersion = SurveyDataVersion.Create(
            Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.Draft,
            SurveyDataIntegrityStatus.Pending, null, null, "[]");
        var block = ProcessingBlock.Create(Guid.NewGuid(), dataVersion.Id, 1, "{\"startFrame\":1}");
        var model = AIModelVersion.Create(
            Guid.NewGuid(), "roadguard-mock", $"P232-{Guid.NewGuid():N}", "file:///models/mock",
            null, null, AIModelVersionStatus.Draft, null, null);
        var job = ProcessingJob.Create(
            Guid.NewGuid(), block.Id, model.Id, ProcessingJobStatus.Completed,
            DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow, null, null);
        var defectType = DefectType.Create($"P232-DT-{Guid.NewGuid():N}", "P2-32 crack");
        var causeCategory = CauseCategory.Create($"P232-CC-{Guid.NewGuid():N}", "P2-32 weather");

        context.AddRange(project, user, road, roadVersion, survey, dataVersion, block, model, job, defectType, causeCategory);
        await context.SaveChangesAsync();
        return new(
            project.Id, roadVersion.Id, survey.Id, dataVersion.Id, block.Id, job.Id, model.Id, user.Id,
            defectType.Code, causeCategory.Code, Guid.NewGuid());
    }

    private static Task<int> CountP232TablesAsync(RoadGuardDbContext context)
        => context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('AIDetections', 'DefectVerificationLogs', 'FieldInspectionTasks')
                """)
            .SingleAsync();

    private static DetectionReviewPersistenceRequest CreatePersistenceRequest(
        P232Scope scope,
        string idempotencyKey,
        string requestFingerprint,
        Guid? retainedDetectionId = null,
        Guid? verifiedByUserId = null)
    {
        var detectionId = retainedDetectionId ?? Guid.NewGuid();
        var defectId = Guid.NewGuid();
        var geometry = GeometryFactory.CreatePoint(new Coordinate(588550, 2325050));
        var detection = AIDetection.Create(
            detectionId, scope.ProcessingJobId, scope.ModelVersionId, scope.RoadSectionVersionId,
            geometry, scope.DefectTypeCode, 0.8m, null, null, "{\"source\":\"mock\"}");
        var defect = Defect.Create(
            defectId, scope.ProjectId, scope.RoadSectionVersionId, detection.Id,
            scope.DefectTypeCode, scope.CauseCategoryCode, DefectSeverity.High, DefectStatus.Open,
            geometry, DateTimeOffset.UtcNow);
        var task = FieldInspectionTask.Create(
            Guid.NewGuid(), $"P232-{Guid.NewGuid():N}", scope.ProjectId, defectId, scope.SurveyId,
            scope.RoadSectionVersionId, 1, "{\"points\":[1]}", null, null, DateTimeOffset.UtcNow.AddDays(1),
            FieldInspectionTaskStatus.NewAssigned, scope.UserId, null, null, null, null);
        var log = DefectVerificationLog.Create(
            Guid.NewGuid(), defectId, null, DefectVerificationAction.PreliminaryKeep,
            null, "{\"status\":\"open\"}", null, null, verifiedByUserId ?? scope.UserId, "Retained for field review.");
        return new(
            detection, defect, task, log, scope.UserId, scope.ProjectId, idempotencyKey, requestFingerprint,
            Guid.NewGuid(), "{\"schemaVersion\":1}", "Detection review persisted.");
    }

    private sealed record P232Scope(
        Guid ProjectId,
        Guid RoadSectionVersionId,
        Guid SurveyId,
        Guid SurveyDataVersionId,
        Guid ProcessingBlockId,
        Guid ProcessingJobId,
        Guid ModelVersionId,
        Guid UserId,
        string DefectTypeCode,
        string CauseCategoryCode,
        Guid DefectId);
}
