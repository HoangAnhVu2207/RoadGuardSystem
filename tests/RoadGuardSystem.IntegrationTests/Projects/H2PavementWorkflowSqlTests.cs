using System.Text.Json;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Repositories.Retention;
using RoadGuardSystem.Repositories.Implementations.Retention;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Projects;

public sealed class H2PavementWorkflowSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    [Fact]
    public async Task Plan_sample_publication_counts_cursor_and_unknown_asbuilt_are_durable()
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext(); var service = Service(db);
        var plan = await Plan(service, scope); var layoutId = plan.GetProperty("id").GetGuid(); var hash = plan.GetProperty("contentHash").GetString()!;
        var publication = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "publish",
            Input: new GeometryMapPublishInput(layoutId, "SAMPLE", "candidate fixture"), Key: Guid.NewGuid().ToString(), ExpectedContentHash: hash));
        Assert.Equal(201, publication.Status); var manifest = (JsonElement)publication.Value!;
        Assert.True(manifest.GetProperty("sampleOnly").GetBoolean()); Assert.Equal("LEGACY_UNKNOWN", manifest.GetProperty("profileStatus").GetString());
        var publicationId = manifest.GetProperty("publicationId").GetGuid(); var mapHash = manifest.GetProperty("contentHash").GetString()!;
        var page = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "page", ResourceId: publicationId,
            Input: new PavementLayerQuery("slabs", 1, null, null), ExpectedContentHash: mapHash));
        Assert.Equal(200, page.Status); var first = (GeometryMapPage)page.Value!; Assert.Single(first.Features); Assert.NotNull(first.NextCursor);
        var mixed = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "page", ResourceId: publicationId,
            Input: new PavementLayerQuery("slabs", 1, null, first.NextCursor), ExpectedContentHash: new string('a', 64)));
        Assert.Equal(409, mixed.Status);
        var built = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "asbuilt-create",
            Input: new AsBuiltLayoutInput(layoutId, [new("custom", 1, [new(0, 0), new(5, 0), new(5, 4), new(0, 4), new(0, 0)], null, 4, "length unknown", "field capture")], "observed footprint"),
            Key: Guid.NewGuid().ToString(), ExpectedContentHash: hash));
        Assert.Equal(201, built.Status);
        var builtJson = (JsonElement)built.Value!; Assert.Equal(JsonValueKind.Null, builtJson.GetProperty("geometry").GetProperty("slabs")[0].GetProperty("lengthMeters").ValueKind);
        Assert.True(builtJson.GetProperty("geometry").GetProperty("renderedGapAreaSquareMeters").GetDouble() > 0);
        Assert.Equal(hash, (await db.Set<PavementLayoutRevision>().AsNoTracking().SingleAsync(x => x.Id == layoutId)).ContentHash);
        Assert.Equal(3, await db.OutboxMessages.CountAsync(x => x.PayloadJson.Contains(scope.Project.ToString())));
    }
    [Fact]
    public async Task Revoked_membership_blocks_same_key_receipt_and_conflict_without_new_effect()
    {
        var scope = await Seed(); var key = Guid.NewGuid().ToString();
        await using (var db = sql.CreateDbContext()) { await Plan(Service(db), scope, key); }
        await using (var revoke = sql.CreateDbContext()) { await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={scope.Project} AND UserId={scope.Actor}"); }
        await using var replay = sql.CreateDbContext();
        var result = await Service(replay).ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, key)); Assert.Equal(403, result.Status);
        var conflict = await Service(replay).ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, key) with { Input = new PavementPlanCreateInput(new(6, 10, [new(0, 20, 12)]), .1) });
        Assert.Equal(403, conflict.Status);
        Assert.Equal(1, await replay.Set<PavementLayoutRevision>().CountAsync(x => x.ProjectId == scope.Project));
    }
    [Fact]
    public async Task Concurrent_same_key_has_one_plan_audit_outbox_and_durable_receipt()
    {
        var scope = await Seed(); var key = Guid.NewGuid().ToString();
        async Task<GeometryWorkflowResult> Create()
        { await using var db = sql.CreateDbContext(); return await Service(db).ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, key)); }
        var results = await Task.WhenAll(Create(), Create());
        Assert.All(results, x => Assert.True(x.Status is 200 or 201));
        Assert.Equal(((JsonElement)results[0].Value!).GetProperty("id").GetGuid(), ((JsonElement)results[1].Value!).GetProperty("id").GetGuid());
        await using var check = sql.CreateDbContext(); Assert.Equal(1, await check.Set<PavementLayoutRevision>().CountAsync(x => x.ProjectId == scope.Project));
        Assert.Equal(1, await check.OutboxMessages.CountAsync(x => x.PayloadJson.Contains(scope.Project.ToString())));
    }
    [Fact]
    public async Task Mixed_set_official_mode_and_raw_snapshot_rewrite_are_rejected()
    {
        var scope = await Seed(); var other = await Seed(); await using var db = sql.CreateDbContext(); var service = Service(db);
        Assert.Equal(409, (await service.ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, Guid.NewGuid().ToString()) with { SegmentSetId = other.Set })).Status);
        var plan = await Plan(service, scope); var id = plan.GetProperty("id").GetGuid(); var hash = plan.GetProperty("contentHash").GetString()!;
        Assert.Equal(409, (await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "publish",
            Input: new GeometryMapPublishInput(id, "OFFICIAL", "cannot adopt guessed CRS"), Key: Guid.NewGuid().ToString(), ExpectedContentHash: hash))).Status);
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE PavementLayoutRevisions SET SnapshotJson='{{}}' WHERE Id={id}"));
        Assert.Equal(0, await db.Set<GeometryMapPublication>().CountAsync(x => x.ProjectId == scope.Project));
    }
    [Fact]
    public async Task Source_file_is_retained_with_immutable_capture_and_checksum_mismatch_fails_closed()
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext(); var service = Service(db);
        var plan = await Plan(service, scope); var id = plan.GetProperty("id").GetGuid(); var hash = plan.GetProperty("contentHash").GetString()!;
        var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "fixture/document", "footprint.json", "application/json", 4, new string('a', 64), scope.Actor, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, scope.Actor, "fixture/document", "DOCUMENT", "application/json", 4, new string('a', 64), 8388608, now.AddHours(24));
        upload.StartUploading("fixture", now);
        db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, scope.Project, null, scope.Actor, "DOCUMENT", now), upload); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var result = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "asbuilt-create",
            Input: new AsBuiltLayoutInput(id, [new("evidence", 1, [new(0, 0), new(5, 0), new(5, 4), new(0, 4), new(0, 0)], 5, 4, null, "field document", file.Id)], "captured custom polygon"),
            Key: Guid.NewGuid().ToString(), ExpectedContentHash: hash)); Assert.Equal(201, result.Status);
        var built = (JsonElement)result.Value!;
        Assert.Equal(201, (await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "publish",
            Input: new GeometryMapPublishInput(built.GetProperty("id").GetGuid(), "SAMPLE", "capture publication"), Key: Guid.NewGuid().ToString(), ExpectedContentHash: built.GetProperty("contentHash").GetString()))).Status);
        var contributor = new PavementRetentionContributor(db);
        Assert.Contains(file.Id, await contributor.KnownProjectFilesAsync(scope.Project, CancellationToken.None));
        var first = await contributor.ReadAsync(file.Id, CancellationToken.None); var second = await contributor.ReadAsync(file.Id, CancellationToken.None);
        Assert.True(first.Complete); Assert.Single(first.References); Assert.Equal(first.References[0].SourceVersion, second.References[0].SourceVersion);
        var composite = new RetentionInventoryRepository(db, [contributor, new Huy01RetentionInventoryContributor(db)]);
        Assert.Contains("HUY_REPAIR_REFERENCE_UNAVAILABLE", (await composite.ReadAsync(file.Id, CancellationToken.None))!.ReasonCodes);
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Files SET Checksum={new string('b', 64)} WHERE Id={file.Id}"));
        // This fixture owns a disposable database. Deliberately corrupt one row to exercise
        // inventory's fail-closed diagnostic, then restore the production immutability trigger.
        await db.Database.ExecuteSqlRawAsync("DISABLE TRIGGER [TR_Files_Immutable] ON [Files]");
        try { await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Files SET Checksum={new string('b', 64)} WHERE Id={file.Id}"); }
        finally { await db.Database.ExecuteSqlRawAsync("ENABLE TRIGGER [TR_Files_Immutable] ON [Files]"); }
        var changed = await contributor.ReadAsync(file.Id, CancellationToken.None);
        Assert.False(changed.Complete); Assert.Contains("PAVEMENT_SOURCE_CONTENT_VERSION_UNRESOLVED", changed.ReasonCodes);
        Assert.Equal(first.References[0].SourceVersion, changed.References[0].SourceVersion);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Committed_ack_loss_recovers_one_effect_only_under_current_authority(bool revoke)
    {
        var scope = await Seed(); var key = Guid.NewGuid().ToString();
        var interceptor = new AckLoss(async () =>
        {
            await using var check = sql.CreateDbContext();
            if (!await check.Set<PavementLayoutRevision>().AnyAsync(x => x.ProjectId == scope.Project)) return false;
            if (revoke) await check.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status=2 WHERE ProjectId={scope.Project} AND UserId={scope.Actor}");
            return true;
        });
        await using var db = sql.CreateRetryingDbContext(interceptor);
        var result = await Service(db).ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, key));
        Assert.True(interceptor.Fired); Assert.Equal(revoke ? 403 : 200, result.Status);
        await using var verify = sql.CreateDbContext(); Assert.Equal(1, await verify.Set<PavementLayoutRevision>().CountAsync(x => x.ProjectId == scope.Project));
        Assert.Equal(1, await verify.OutboxMessages.CountAsync(x => x.PayloadJson.Contains(scope.Project.ToString())));
    }
    private sealed class AckLoss(Func<Task<bool>> committed) : DbTransactionInterceptor
    {
        public bool Fired;
        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { if (Fired || !await committed()) return; Fired = true; throw new TestTransientException("Injected pavement commit ACK loss."); }
    }
    [Fact]
    public async Task Actual_field_impact_records_decision_without_moving_task_or_resetting_history()
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext();
        var route = await db.RoadSectionVersions.SingleAsync(x => x.Id == scope.Route);
        var replacement = RoadSectionVersion.Create(Guid.NewGuid(), route.RoadSectionId, 2, false, route.Geometry, DateTimeOffset.UtcNow, "proposed corrected location");
        var survey = Survey.Create(Guid.NewGuid(), null, scope.Project, scope.Route, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var type = DefectType.Create($"h2-{Guid.NewGuid():N}", "field fixture defect");
        var defect = Defect.Create(Guid.NewGuid(), scope.Project, scope.Route, null, type.Code, null, DefectSeverity.Medium, DefectStatus.Open,
            new GeometryFactory(new PrecisionModel(), 32648).CreatePoint(new Coordinate(5, 0)), DateTimeOffset.UtcNow);
        var task = FieldInspectionTask.Create(Guid.NewGuid(), $"H2-{Guid.NewGuid():N}", scope.Project, defect.Id, survey.Id, scope.Route, 1, "{}", null, null, DateTimeOffset.UtcNow.AddDays(1), FieldInspectionTaskStatus.Accepted, scope.Actor, null, null, null, null);
        db.AddRange(replacement, survey, type, defect, task); await db.SaveChangesAsync(); var oldVersion = task.RowVersion.ToArray();
        var service = Service(db);
        var result = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "impact-create", Input: new GeometryImpactInput(scope.Route, replacement.Id, "corrected geometry requires review"), Key: Guid.NewGuid().ToString()));
        Assert.Equal(201, result.Status); var impact = (JsonElement)result.Value!;
        var reference = impact.GetProperty("references").EnumerateArray().Single(x => x.GetProperty("kind").GetString() == "FIELD_TASK");
        Assert.Equal(task.Id, reference.GetProperty("id").GetGuid()); Assert.Equal(JsonValueKind.Null, reference.GetProperty("segmentSetId").ValueKind);
        var decision = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "impact-decide", ResourceId: impact.GetProperty("id").GetGuid(), Input: new GeometryImpactDecisionInput(task.Id, "REASSIGN", "decision pending actual H3 task command"), Key: Guid.NewGuid().ToString()));
        Assert.Equal(201, decision.Status); Assert.Equal("RECORDED_ONLY", ((JsonElement)decision.Value!).GetProperty("executionStatus").GetString());
        db.ChangeTracker.Clear(); var unchanged = await db.FieldInspectionTasks.AsNoTracking().SingleAsync(x => x.Id == task.Id);
        Assert.Equal(scope.Route, unchanged.RoadSectionVersionId); Assert.Equal(oldVersion, unchanged.RowVersion); Assert.Equal(FieldInspectionTaskStatus.Accepted, unchanged.Status);
        var foreignDecision = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "impact-decide", ResourceId: impact.GetProperty("id").GetGuid(), Input: new GeometryImpactDecisionInput(Guid.NewGuid(), "STOP", "not in inventory"), Key: Guid.NewGuid().ToString())); Assert.Equal(409, foreignDecision.Status);
    }
    [Fact]
    public async Task Closed_project_keeps_current_authorized_history_and_receipts_but_denies_new_mutation()
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext(); var key = Guid.NewGuid().ToString(); var service = Service(db);
        var plan = await Plan(service, scope, key); var project = await db.Projects.SingleAsync(x => x.Id == scope.Project);
        project.Status = ProjectStatus.Closed; await db.SaveChangesAsync();
        Assert.Equal(200, (await service.ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, key))).Status);
        Assert.Equal(200, (await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "layout-get", ResourceId: plan.GetProperty("id").GetGuid()))).Status);
        Assert.Equal(409, (await service.ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, Guid.NewGuid().ToString()))).Status);
        Assert.Equal(1, await db.Set<PavementLayoutRevision>().CountAsync(x => x.ProjectId == scope.Project));
    }
    [Fact]
    public async Task Receipt_save_failure_rolls_back_layout_audit_and_outbox()
    {
        var scope = await Seed();
        await using (var failed = sql.CreateDbContext(new ReceiptSaveFailure()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => Service(failed).ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, Guid.NewGuid().ToString())));
        await using var check = sql.CreateDbContext();
        Assert.Equal(0, await check.Set<PavementLayoutRevision>().CountAsync(x => x.ProjectId == scope.Project));
        Assert.Equal(0, await check.OutboxMessages.CountAsync(x => x.PayloadJson.Contains(scope.Project.ToString())));
        Assert.Equal(0, await check.AuditLogs.CountAsync(x => x.ActorUserId == scope.Actor && x.EventType.StartsWith("pavement_")));
        Assert.Equal(0, await check.IdempotencyRecords.CountAsync(x => x.ProjectId == scope.Project));
    }
    private sealed class ReceiptSaveFailure : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<IdempotencyRecord>().Any(x => x.State == EntityState.Added && x.Entity.Operation.StartsWith("h2.pavement.", StringComparison.Ordinal)))
                throw new InvalidOperationException("Injected receipt save failure after business SaveChanges.");
            return ValueTask.FromResult(result);
        }
    }
    [Fact]
    public async Task Read_reuses_caller_transaction_without_committing_it()
    {
        var scope = await Seed(); await using var db = sql.CreateDbContext(); var service = Service(db); var plan = await Plan(service, scope);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var result = await service.ExecuteAsync(UserRoleCode.ProjectManager, new(scope.Actor, scope.Project, "layout-get", ResourceId: plan.GetProperty("id").GetGuid()));
        Assert.Equal(200, result.Status); Assert.Same(transaction, db.Database.CurrentTransaction);
        await transaction.RollbackAsync();
    }
    private PavementWorkflowService Service(RoadGuardDbContext db) => new(new PavementWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System),
        new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System));
    private static PavementWorkflowCommand PlanCommand(Scope s, string key) => new(s.Actor, s.Project, "plan-create", s.Route, s.Set,
        Input: new PavementPlanCreateInput(new(4, 10, [new(0, 20, 12)]), .1), Key: key);
    private static async Task<JsonElement> Plan(PavementWorkflowService service, Scope scope, string? key = null)
    { var result = await service.ExecuteAsync(UserRoleCode.ProjectManager, PlanCommand(scope, key ?? Guid.NewGuid().ToString())); Assert.Equal(201, result.Status); return (JsonElement)result.Value!; }
    private async Task<Scope> Seed()
    {
        await using var db = sql.CreateDbContext(); await sql.SeedRolesAsync(db);
        var actor = new ApplicationUser { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), DisplayName = "Pavement PM", PasswordHash = "fixture", RoleCode = UserRoleCode.ProjectManager, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow };
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "pavement sample", null, null, null, null, DateTimeOffset.UtcNow);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "sample");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new(0, 0), new(20, 0)]);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, DateTimeOffset.UtcNow, "legacy fixture");
        var set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id);
        var segment = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, 1); segment.SetGeometry(0, 20, 0, geometry);
        db.AddRange(actor, project, ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor.Id, new(2000, 1, 1)), road, route, set, segment);
        await db.SaveChangesAsync(); return new(actor.Id, project.Id, route.Id, set.Id);
    }
    private sealed record Scope(Guid Actor, Guid Project, Guid Route, Guid Set);
}
