using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Repositories.Models.Huy01;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Notifications;

public sealed class H6FieldSourceSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task DuplicatePersistedTransportUsesTheSameActualSourceOrigin()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext();
        var original = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == seed.Event);
        var originalPlan = H6NotificationCatalog.Parse(original.Id, original.MessageType, original.OccurredAtUtc, original.PayloadJson);
        var source = originalPlan.Source with { EventId = Guid.NewGuid() };
        var duplicate = OutboxMessage.Create(source.EventId, original.MessageType, original.OccurredAtUtc, null, JsonSerializer.Serialize(source, Json));
        db.OutboxMessages.Add(duplicate); await db.SaveChangesAsync();
        var plan = H6NotificationCatalog.Parse(duplicate.Id, duplicate.MessageType, duplicate.OccurredAtUtc, duplicate.PayloadJson);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var proof = await new H6FieldNotificationSourceAdapter(db).ResolveAsync(plan, default);
        Assert.Equal("VERIFIED", proof.Status); Assert.Equal(seed.Task, proof.TaskId); Assert.Equal(seed.Crew, proof.ResponsibleUserId);
        await tx.CommitAsync();
    }
    [Fact]
    public async Task CallerCannotInventTransportRowOrSubstituteItsStoredEnvelope()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext();
        var original = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == seed.Event);
        var plan = H6NotificationCatalog.Parse(original.Id, original.MessageType, original.OccurredAtUtc, original.PayloadJson);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var adapter = new H6FieldNotificationSourceAdapter(db);
        Assert.Equal("REJECTED", (await adapter.ResolveAsync(plan with { Source = plan.Source with { EventId = Guid.NewGuid() } }, default)).Status);
        Assert.Equal("REJECTED", (await adapter.ResolveAsync(plan with { Source = plan.Source with { SchemaVersion = 2 } }, default)).Status);
        await tx.CommitAsync();
    }
    [Fact]
    public async Task RealFieldAssignmentProducerProvesPersistedSourceWithoutExposingReporterIdentity()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext();
        var message = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == seed.Event);
        var plan = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var resolution = await new H6FieldNotificationSourceAdapter(db).ResolveAsync(plan, default);
        Assert.Equal("VERIFIED", resolution.Status); Assert.Equal(seed.Crew, resolution.ResponsibleUserId);
        Assert.Equal(seed.Task, resolution.TaskId); Assert.DoesNotContain(seed.Reporter.ToString(), plan.Body, StringComparison.Ordinal);
        var notification = Notification.Create(Guid.NewGuid(), seed.Crew, "FieldTask", seed.Task, "field.task.assigned.v1", "FIELD task", "Protected task", message.OccurredAtUtc);
        db.Notifications.Add(notification); await db.SaveChangesAsync();
        var inbox = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System, [new H6FieldNotificationSourceAdapter(db)]);
        Assert.NotNull(await inbox.GetAsync(seed.Crew, notification.Id));
        Assert.Equal(notification.Id, Assert.Single((await inbox.ListAsync(seed.Crew, null, null, 1)).Items).Id);
        await tx.CommitAsync();
    }
    [Fact]
    public async Task ActualSourceRowRejectsPayloadThatClaimsDifferentProjectOrResponsibleCrew()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext();
        var message = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == seed.Event);
        var plan = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var adapter = new H6FieldNotificationSourceAdapter(db);
        Assert.Equal("REJECTED", (await adapter.ResolveAsync(plan with { Source = plan.Source with { ProjectId = Guid.NewGuid() } }, default)).Status);
        Assert.Equal("REJECTED", (await adapter.ResolveAsync(plan with { Source = plan.Source with { ResponsibleUserId = Guid.NewGuid() } }, default)).Status);
        await tx.CommitAsync();
    }
    [Fact]
    public async Task HistoricalAssignmentEventSurvivesEndButCannotGrantCurrentCrewScope()
    {
        var seed = await Seed(); await using var db = sql.CreateDbContext();
        var assignment = await db.FieldInspectionAssignments.SingleAsync(row => row.FieldInspectionTaskId == seed.Task);
        assignment.End(DateTimeOffset.UtcNow, "actual handover source fixture"); await db.SaveChangesAsync();
        var message = await db.OutboxMessages.AsNoTracking().SingleAsync(row => row.Id == seed.Event);
        var plan = H6NotificationCatalog.Parse(message.Id, message.MessageType, message.OccurredAtUtc, message.PayloadJson);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); var adapter = new H6FieldNotificationSourceAdapter(db);
        Assert.Equal("VERIFIED", (await adapter.ResolveAsync(plan, default)).Status);
        Assert.False(await adapter.ScopeQuery().AnyAsync(row => row.SourceId == seed.Task && row.AssignedUserId == seed.Crew));
        var notification = Notification.Create(Guid.NewGuid(), seed.Crew, "FieldTask", seed.Task, "field.task.assigned.v1", "Historical assignment", "Protected task", message.OccurredAtUtc);
        db.Notifications.Add(notification); await db.SaveChangesAsync();
        var inbox = new H6ProtectedNotificationRepository(db, new IdempotencyOperationService(db), TimeProvider.System, [adapter]);
        Assert.Null(await inbox.GetAsync(seed.Crew, notification.Id));
        Assert.Empty((await inbox.ListAsync(seed.Crew, null, null, 1)).Items);
        await tx.CommitAsync();
    }
    internal async Task<(Guid Task, Guid Event, Guid Crew, Guid Reporter)> Seed()
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db); var now = DateTimeOffset.UtcNow;
        ApplicationUser User(UserRoleCode role)
        {
            var name = Guid.NewGuid().ToString("N");
            return new() { Id = Guid.NewGuid(), UserName = name, NormalizedUserName = name.ToUpperInvariant(), DisplayName = "H6 actual-source fixture", PasswordHash = "fixture", RoleCode = role, Status = UserStatus.Active, CreatedAt = now };
        }
        var pm = User(UserRoleCode.ProjectManager); var crew = User(UserRoleCode.RepairCrew); var reporter = User(UserRoleCode.Reporter);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "H6 actual FIELD source", null, null, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "source fixture");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new(0, 0), new(20, 0)]);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now, "legacy sample"); var set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id);
        var type = DefectType.Create("N" + Guid.NewGuid().ToString("N"), "Source proof fixture");
        db.AddRange(pm, crew, reporter, project, road, route, set, type,
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, new(2000, 1, 1)),
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = project.Id, UserId = crew.Id, RoleCode = UserRoleCode.RepairCrew, ValidFrom = new(2000, 1, 1), Status = ProjectMemberStatus.Active });
        await db.SaveChangesAsync();
        var file = StoredFile.Create(Guid.NewGuid(), "private/h6-source-" + Guid.NewGuid().ToString("N"), "source.jpg", "image/jpeg", 4, new string('b', 64), reporter.Id, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, reporter.Id, file.StorageUri, "REPORT_PHOTO", "image/jpeg", 4, new string('b', 64), 8388608, now.AddHours(24)); upload.StartUploading("fixture", now);
        db.AddRange(file, FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter.Id, now), upload); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var report = Report.Create(Guid.NewGuid(), reporter.Id, "private source", now, [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, Convert.ToBase64String(upload.RowVersion), reporter.Id)]);
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now); incident.Triage(project.Id, CaseVerificationMethod.ExistingEvidence, "actual source", now);
        db.AddRange(report, incident); db.Set<HuyCaseReportLink>().Add(new() { Id = Guid.NewGuid(), CaseId = incident.Id, ReportId = report.Id, StartedAt = now }); await db.SaveChangesAsync();
        var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(pm.Id,
            CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, report.Id, "source-fixture"), project.Id, "geometry-fixture"),
            CandidateDecisionKind.KeepNew, CandidateClassification.Create(route.Id, type.Code, null, DefectSeverity.Low, null), null, null, null, "actual retained source", null, default);
        var defectVersion = await db.Defects.Where(row => row.Id == accepted.DefectId).Select(row => Convert.ToBase64String(EF.Property<byte[]>(row, "RowVersion"))).SingleAsync();
        var result = await new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System).ExecuteAsync(
            new(project.Id, null, "create", new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskCreateInputFact(accepted.DefectId!.Value, defectVersion, null, "REPORTER", route.Id, set.Id, null, null, "PRE_MEASUREMENT", 1, "{}", "actual FIELD assignment", crew.Id, now.AddDays(1)),
                Guid.NewGuid().ToString("N"), null, new(pm.Id, UserRoleCode.ProjectManager, pm.Id, "DIRECT", true)),
            async token => await new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System)
                .AuthorizeAsync(pm.Id, UserRoleCode.ProjectManager, project.Id, token) is not null, default);
        Assert.Equal(201, result.Status);
        var sourceEvent = await db.FieldInspectionTaskEvents.AsNoTracking().SingleAsync(row => row.ProjectId == project.Id && row.Kind == "ASSIGNED");
        return (sourceEvent.TaskId, sourceEvent.Id, crew.Id, reporter.Id);
    }
}
