using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Spatial;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Repositories.Transactions;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P2-23")]
public sealed class P223SurveyAssignmentSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly GeometryFactory Srid32648Factory = new(
        new PrecisionModel(),
        SpatialConstants.UtmZone48NSrid);
    private readonly IdentitySqlServerFixture _fixture;

    public P223SurveyAssignmentSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-23: survey and assignment round-trip through SQL Server")]
    public async Task SurveyAndAssignment_ValidRecords_RoundTrip()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var request = CreateRequest(scope);
        context.SurveyRequests.Add(request);
        await context.SaveChangesAsync();

        var survey = Survey.Create(
            Guid.NewGuid(),
            request.Id,
            scope.ProjectId,
            scope.RoadSectionVersionId,
            SurveyType.Periodic,
            SurveyStatus.Draft,
            false,
            null,
            null);
        var assignment = SurveyAssignment.Create(
            Guid.NewGuid(),
            request.Id,
            scope.OperatorUserId,
            scope.ProjectManagerUserId,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null);
        context.AddRange(survey, assignment);
        await context.SaveChangesAsync();

        (await context.Surveys.AsNoTracking().SingleAsync(candidate => candidate.Id == survey.Id))
            .RoadSectionVersionId.Should().Be(scope.RoadSectionVersionId);
        (await context.SurveyAssignments.AsNoTracking().SingleAsync(candidate => candidate.Id == assignment.Id))
            .OperatorUserId.Should().Be(scope.OperatorUserId);
    }

    [Fact(DisplayName = "P2-23: SQL Server rejects invalid survey status and duplicate active assignment")]
    public async Task SurveyStatusAndActiveAssignment_InvalidValues_AreRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var request = CreateRequest(scope);
        context.SurveyRequests.Add(request);
        var activeAssignment = SurveyAssignment.Create(
            Guid.NewGuid(),
            request.Id,
            scope.OperatorUserId,
            scope.ProjectManagerUserId,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null);
        context.SurveyAssignments.Add(activeAssignment);
        await context.SaveChangesAsync();
        var unassignedRequest = CreateRequest(scope);
        context.SurveyRequests.Add(unassignedRequest);
        await context.SaveChangesAsync();

        var invalidStatus = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [Surveys]
                ([Id], [SurveyRequestId], [ProjectId], [RoadSectionVersionId], [SurveyType], [IsBaselineConfirmed], [Status])
            VALUES ({Guid.NewGuid()}, {request.Id}, {scope.ProjectId}, {scope.RoadSectionVersionId}, {1}, {0}, {99})
            """);
        var duplicateActive = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyAssignments]
                ([Id], [SurveyRequestId], [OperatorUserId], [AssignedByUserId], [AssignedAt])
            VALUES ({Guid.NewGuid()}, {request.Id}, {scope.OperatorUserId}, {scope.ProjectManagerUserId}, {DateTimeOffset.UtcNow})
            """);
        var activeAssignmentWithReassignmentReason = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyAssignments]
                ([Id], [SurveyRequestId], [OperatorUserId], [AssignedByUserId], [AssignedAt], [ReassignmentReason])
            VALUES ({Guid.NewGuid()}, {unassignedRequest.Id}, {scope.OperatorUserId}, {scope.ProjectManagerUserId},
                {DateTimeOffset.UtcNow}, {"Active assignments cannot have reassignment history"})
            """);
        var rejectedAssignmentWithReassignmentReason = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [SurveyAssignments]
                ([Id], [SurveyRequestId], [OperatorUserId], [AssignedByUserId], [AssignedAt], [RejectedAt],
                    [RejectionReason], [ReassignmentReason], [EndedAt])
            VALUES ({Guid.NewGuid()}, {unassignedRequest.Id}, {scope.OperatorUserId}, {scope.ProjectManagerUserId},
                {DateTimeOffset.UtcNow.AddMinutes(-2)}, {DateTimeOffset.UtcNow.AddMinutes(-1)}, {"Weather restriction"},
                {"Replacement operator assigned"}, {DateTimeOffset.UtcNow})
            """);

        await invalidStatus.Should().ThrowAsync<SqlException>();
        await duplicateActive.Should().ThrowAsync<SqlException>();
        await activeAssignmentWithReassignmentReason.Should().ThrowAsync<SqlException>();
        await rejectedAssignmentWithReassignmentReason.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-23: SQL Server rejects survey request and version scope mismatches")]
    public async Task Survey_RequestAndVersionScopeMismatch_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var firstScope = await CreateScopeAsync(context);
        var secondScope = await CreateScopeAsync(context);
        var otherRoadSection = RoadSection.Create(
            Guid.NewGuid(),
            firstScope.ProjectId,
            $"P223-OTHER-{Guid.NewGuid():N}");
        var otherRoadSectionVersion = RoadSectionVersion.Create(
            Guid.NewGuid(),
            otherRoadSection.Id,
            1,
            true,
            Srid32648Factory.CreateLineString(new[]
            {
                new Coordinate(588700, 2325200),
                new Coordinate(588800, 2325300)
            }),
            SpatialConstants.UtmZone48NSrid,
            DateTimeOffset.UtcNow,
            "Alternate survey scope");
        context.AddRange(otherRoadSection, otherRoadSectionVersion);
        await context.SaveChangesAsync();
        var request = CreateRequest(firstScope);
        context.SurveyRequests.Add(request);
        await context.SaveChangesAsync();

        var mismatchedSurvey = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [Surveys]
                ([Id], [SurveyRequestId], [ProjectId], [RoadSectionVersionId], [SurveyType], [IsBaselineConfirmed], [Status])
            VALUES ({Guid.NewGuid()}, {request.Id}, {secondScope.ProjectId}, {secondScope.RoadSectionVersionId}, {1}, {0}, {1})
            """);
        var mismatchedRoadSection = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [Surveys]
                ([Id], [SurveyRequestId], [ProjectId], [RoadSectionVersionId], [SurveyType], [IsBaselineConfirmed], [Status])
            VALUES ({Guid.NewGuid()}, {request.Id}, {firstScope.ProjectId}, {otherRoadSectionVersion.Id}, {1}, {0}, {1})
            """);

        await mismatchedSurvey.Should().ThrowAsync<SqlException>();
        await mismatchedRoadSection.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-23: reassignment preserves history and emits one notification effect")]
    public async Task Reassignment_PreservesHistoryAndEmitsOneNotificationEffect()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var replacementOperator = CreateUser(UserRoleCode.DroneOperator, "replacement_operator");
        var request = CreateRequest(scope);
        var initialAssignment = SurveyAssignment.Create(
            Guid.NewGuid(),
            request.Id,
            scope.OperatorUserId,
            scope.ProjectManagerUserId,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            null,
            null,
            null,
            null,
            null);
        context.AddRange(replacementOperator, request, initialAssignment);
        await context.SaveChangesAsync();

        var replacement = SurveyAssignment.Create(
            Guid.NewGuid(),
            request.Id,
            replacementOperator.Id,
            scope.ProjectManagerUserId,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null);
        var service = new SurveyAssignmentPersistenceService(
            context,
            new IdempotencyOperationService(context));

        var result = await service.ReassignAsync(
            request.Id,
            replacement,
            scope.ProjectManagerUserId,
            DateTimeOffset.UtcNow,
            "Operator unavailable",
            "p223-reassign-key",
            new string('a', 64));
        var replay = await service.ReassignAsync(
            request.Id,
            replacement,
            scope.ProjectManagerUserId,
            DateTimeOffset.UtcNow,
            "Operator unavailable",
            "p223-reassign-key",
            new string('a', 64));

        context.ChangeTracker.Clear();
        var historical = await context.SurveyAssignments.SingleAsync(item => item.Id == initialAssignment.Id);
        historical.EndedAt.Should().NotBeNull();
        historical.ReassignmentReason.Should().Be("Operator unavailable");
        (await context.SurveyAssignments.CountAsync(item => item.SurveyRequestId == request.Id && item.EndedAt == null))
            .Should().Be(1);
        (await context.AuditLogs.CountAsync(item => item.EntityId == request.Id && item.EventType == "survey_request.reassigned"))
            .Should().Be(1);
        (await context.OutboxMessages.CountAsync(item => item.Id == result.OutboxMessageId)).Should().Be(1);
        replay.Status.Should().Be(IdempotencyOperationStatus.Replayed);
        replay.OutboxMessageId.Should().Be(result.OutboxMessageId);

        var notification = Notification.Create(
            Guid.NewGuid(),
            replacementOperator.Id,
            "SurveyRequest",
            request.Id,
            "survey_request.reassigned",
            "Survey request reassigned",
            "A survey request has been reassigned to you.");
        var consumer = new NotificationOutboxConsumer(context, new ConsumerEffectService(context));
        await consumer.ConsumeAsync(result.OutboxMessageId, notification);
        await consumer.ConsumeAsync(result.OutboxMessageId, notification);

        (await context.Notifications.CountAsync(item => item.Id == notification.Id)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-23: unknown commit reassignment returns the durable replay")]
    public async Task Reassignment_PostCommitAcknowledgmentFailure_ReturnsDurableReplay()
    {
        SurveyScope scope;
        SurveyRequest request;
        ApplicationUser replacementOperator;
        await using (var setup = _fixture.CreateDbContext())
        {
            scope = await CreateScopeAsync(setup);
            replacementOperator = CreateUser(UserRoleCode.DroneOperator, "unknown_commit_operator");
            request = CreateRequest(scope);
            var initialAssignment = SurveyAssignment.Create(
                Guid.NewGuid(),
                request.Id,
                scope.OperatorUserId,
                scope.ProjectManagerUserId,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                null,
                null,
                null,
                null,
                null);
            setup.AddRange(replacementOperator, request, initialAssignment);
            await setup.SaveChangesAsync();
        }

        var interceptor = new FailFirstCommittedInterceptor();
        await using var context = _fixture.CreateRetryingDbContext(interceptor);
        var replacement = SurveyAssignment.Create(
            Guid.NewGuid(),
            request.Id,
            replacementOperator.Id,
            scope.ProjectManagerUserId,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null);
        var service = new SurveyAssignmentPersistenceService(context, new IdempotencyOperationService(context));

        var result = await service.ReassignAsync(
            request.Id,
            replacement,
            scope.ProjectManagerUserId,
            DateTimeOffset.UtcNow,
            "Operator unavailable",
            "p223-unknown-commit-key",
            new string('b', 64));

        interceptor.FailureCount.Should().Be(1);
        result.Status.Should().Be(IdempotencyOperationStatus.Replayed);
        await using var verification = _fixture.CreateDbContext();
        (await verification.SurveyAssignments.CountAsync(item => item.SurveyRequestId == request.Id)).Should().Be(2);
        (await verification.AuditLogs.CountAsync(item => item.EntityId == request.Id && item.EventType == "survey_request.reassigned"))
            .Should().Be(1);
        (await verification.OutboxMessages.CountAsync(item => item.CorrelationId == request.Id)).Should().Be(1);
    }

    [Fact(DisplayName = "P2-23: failed cancellation persistence rolls back the request, audit, and outbox")]
    public async Task CancellationPersistence_FailedAudit_RollsBackAllWrites()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var request = CreateRequest(scope);
        context.SurveyRequests.Add(request);
        await context.SaveChangesAsync();
        var transactionService = new RoadGuardTransactionService(context);

        var cancel = () => transactionService.ExecuteAsync(
            async cancellationToken =>
            {
                var cancelledAt = DateTimeOffset.UtcNow;
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [SurveyRequests]
                    SET [Status] = {(byte)SurveyRequestStatus.Cancelled}, [CancelledAt] = {cancelledAt},
                        [CancellationReason] = {"Weather closure"}
                    WHERE [Id] = {request.Id}
                    """, cancellationToken);
                context.AuditLogs.Add(AuditLog.Create(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    cancelledAt,
                    "survey_request.cancelled",
                    "SurveyRequest",
                    request.Id,
                    null,
                    null,
                    null,
                    "p2-23.persistence",
                    request.Id));
                context.OutboxMessages.Add(OutboxMessage.Create(
                    Guid.NewGuid(),
                    "survey_request.cancelled",
                    cancelledAt,
                    request.Id,
                    "{}"));
            });

        await cancel.Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();
        var persisted = await context.SurveyRequests.AsNoTracking().SingleAsync(item => item.Id == request.Id);
        persisted.Status.Should().Be(SurveyRequestStatus.NewAssigned);
        persisted.CancelledAt.Should().BeNull();
        persisted.CancellationReason.Should().BeNull();
        (await context.AuditLogs.CountAsync(item => item.EntityId == request.Id)).Should().Be(0);
        (await context.OutboxMessages.CountAsync(item => item.CorrelationId == request.Id)).Should().Be(0);
    }

    [Fact(DisplayName = "P2-23: migration downgrades to P2-22 and reapplies survey assignment schema")]
    public async Task MigrationLifecycle_DowngradesToP222AndReapplies()
    {
        await using var context = _fixture.CreateDbContext();
        (await CountP223TablesAsync(context)).Should().Be(2);

        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260920172407_AddSurveyPlanningSchema");
        (await CountP223TablesAsync(context)).Should().Be(0);

        await context.Database.MigrateAsync();
        (await CountP223TablesAsync(context)).Should().Be(2);
    }

    private async Task<SurveyScope> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P223-{Guid.NewGuid():N}",
            Name = "P2-23 fixture project",
            EngineeringUtmSrid = SpatialConstants.UtmZone48NSrid,
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var projectManager = CreateUser(UserRoleCode.ProjectManager, "pm");
        var operatorUser = CreateUser(UserRoleCode.DroneOperator, "operator");
        var roadSection = RoadSection.Create(Guid.NewGuid(), project.Id, $"P223-ROAD-{Guid.NewGuid():N}");
        var version = RoadSectionVersion.Create(
            Guid.NewGuid(),
            roadSection.Id,
            1,
            true,
            Srid32648Factory.CreateLineString(new[]
            {
                new Coordinate(588500, 2325000),
                new Coordinate(588600, 2325100)
            }),
            SpatialConstants.UtmZone48NSrid,
            DateTimeOffset.UtcNow,
            "Initial survey scope");
        context.AddRange(project, projectManager, operatorUser, roadSection, version);
        await context.SaveChangesAsync();
        return new SurveyScope(project.Id, roadSection.Id, version.Id, projectManager.Id, operatorUser.Id);
    }

    private static SurveyRequest CreateRequest(SurveyScope scope)
        => SurveyRequest.Create(
            Guid.NewGuid(),
            scope.ProjectId,
            scope.RoadSectionId,
            null,
            scope.ProjectManagerUserId,
            SurveyType.Periodic,
            SurveyRequestStatus.NewAssigned,
            DateTimeOffset.UtcNow);

    private static ApplicationUser CreateUser(UserRoleCode roleCode, string roleName)
        => new()
        {
            Id = Guid.NewGuid(),
            UserName = $"p223_{roleName}_{Guid.NewGuid():N}",
            DisplayName = $"P2-23 {roleName}",
            PasswordHash = "fixture-password-hash",
            RoleCode = roleCode,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static Task<int> CountP223TablesAsync(RoadGuardDbContext context)
        => context.Database.SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM sys.tables
                WHERE [name] IN ('Surveys', 'SurveyAssignments')
                """)
            .SingleAsync();

    private sealed record SurveyScope(
        Guid ProjectId,
        Guid RoadSectionId,
        Guid RoadSectionVersionId,
        Guid ProjectManagerUserId,
        Guid OperatorUserId);
}
