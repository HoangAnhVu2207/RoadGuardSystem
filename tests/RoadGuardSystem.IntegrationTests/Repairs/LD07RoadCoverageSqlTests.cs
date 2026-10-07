using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Implementations.Retention;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Implementations.Offline;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Offline;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class LD07RoadCoverageSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task TestOnlyWarrantyPinsActualScopeAndEligibilityWithoutExecutionAndRetainsHistory()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var repository = new RoadCoverageRepository(db, TimeProvider.System);
        var before = await repository.ReadAsync(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, default);
        Assert.Equal(200, before.Status);
        var input = Input(before.Value!, state, "WARRANTY");
        var command = new RoadCoverageCommand(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, input, Guid.NewGuid().ToString(), before.Value!.Version);
        Assert.Equal(403, (await repository.ConfirmAsync(command with { ActorId = state.Source.Pm, Role = UserRoleCode.ProjectManager }, default)).Status);
        Assert.Equal(409, (await repository.ConfirmAsync(command with { Input = input with { SourceVersion = new string('0', 64) } }, default)).Status);
        Assert.Equal(409, (await repository.ConfirmAsync(command with { Input = input with { ExpectedScopeHash = new string('0', 64) } }, default)).Status);
        var confirmed = await repository.ConfirmAsync(command, default); Assert.Equal(201, confirmed.Status);
        Assert.Equal(200, (await repository.ConfirmAsync(command, default)).Status);
        Assert.Equal(409, (await repository.ConfirmAsync(command with { Input = input with { Reason = "different" } }, default)).Status);
        var mapping = Assert.Single(confirmed.Value!.History);
        var read = await new RepairEligibilityRepository(db, TimeProvider.System).ReadAsync(new(state.Source.Pm, UserRoleCode.ProjectManager,
            state.Source.Project, state.Package, state.Item), default);
        Assert.Equal(200, read.Status); Assert.Equal("TEST_ONLY_MAPPING", read.Value!.Sources.SourceMapping);
        Assert.Equal(mapping.Id, read.Value.Sources.Mapping!.Id);
        Assert.Contains("TEST_ONLY_SOURCE_NOT_EXECUTABLE", read.Value.MissingReasons);
        var device = Guid.NewGuid(); using var keys = OfflineDeviceKeys.Generate(state.Source.Crew, device.ToString("D"));
        var registration = OfflineDeviceRegistration.Register(Guid.NewGuid(), state.Source.Project, state.Source.Crew, device,
            1, UserRoleCode.RepairCrew, keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey, DateTimeOffset.UtcNow);
        db.Add(registration); await db.SaveChangesAsync();
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System, validator, validator);
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var offline = new OfflineWorkflowRepository(db, TimeProvider.System, field, validator, repairs);
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        async Task<bool> Current(CancellationToken token) => await guard.AuthorizeAsync(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project, token) is not null;
        var snapshot = await offline.ExecuteAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            "snapshot-create", new OfflineSnapshotData(registration.Id, state.Binding.TaskId), null, Guid.NewGuid().ToString(), null),
            OfflineContractMapping.RepositoryAlgorithms, Current, default);
        Assert.Equal(201, snapshot.Status);
        var snapshotId = JsonSerializer.SerializeToElement(snapshot.Value, Json).GetProperty("id").GetGuid();
        var storedSnapshot = await db.Set<OfflineTaskSnapshot>().AsNoTracking().SingleAsync(row => row.Id == snapshotId);
        using var payload = JsonDocument.Parse(storedSnapshot.SnapshotJson);
        var repairPayload = payload.RootElement.GetProperty("repair");
        Assert.Equal("TEST_ONLY_MAPPING", repairPayload.GetProperty("eligibility").GetString());
        Assert.Equal(mapping.Id, repairPayload.GetProperty("coverageMapping").GetProperty("id").GetGuid());
        Assert.Equal(mapping.SourceVersion, repairPayload.GetProperty("coverageMapping").GetProperty("sourceVersion").GetString());
        var version = Convert.ToBase64String(await db.RepairItems.Where(row => row.Id == state.Item).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var assessment = await repairs.AssessAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project, state.Package,
            state.Item, state.Binding.TaskId, new(Guid.NewGuid(), state.FirstStart.Id, null, null, null), Guid.NewGuid().ToString(), version), default);
        Assert.Equal(201, assessment.Status);
        var denied = await repairs.StartExecutionAsync(new(state.Source.Crew, UserRoleCode.RepairCrew, state.Source.Project,
            state.Package, state.Item, state.Binding.TaskId, new(Guid.NewGuid(), state.FirstStart.Id, DateTimeOffset.UtcNow,
                Assert.IsType<RepairAssessmentFact>(assessment.Value).Id), Guid.NewGuid().ToString(), assessment.Version!), default);
        Assert.Equal(409, denied.Status); Assert.Equal("test_only_source_not_executable", denied.Code);
        Assert.False(await db.Set<RepairExecutionStart>().AnyAsync(row => row.ItemId == state.Item));
        var fresh = (await repository.ReadAsync(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, default)).Value!;
        var superseded = await repository.ConfirmAsync(command with
        {
            Key = Guid.NewGuid().ToString(),
            ExpectedVersion = fresh.Version,
            Input = input with { SupersedesId = mapping.Id, ApplicableToUtc = input.ApplicableToUtc.AddDays(1) }
        }, default);
        Assert.Equal(201, superseded.Status);
        Assert.Equal(2, superseded.Value!.History.Length);
        Assert.Equal(storedSnapshot.ContentHash, (await db.Set<OfflineTaskSnapshot>().AsNoTracking().SingleAsync(row => row.Id == snapshotId)).ContentHash);
        Assert.Equal(storedSnapshot.SnapshotJson, (await db.Set<OfflineTaskSnapshot>().AsNoTracking().SingleAsync(row => row.Id == snapshotId)).SnapshotJson);
        var updated = await new RepairEligibilityRepository(db, TimeProvider.System).ReadAsync(new(state.Source.Pm, UserRoleCode.ProjectManager,
            state.Source.Project, state.Package, state.Item), default);
        Assert.NotEqual(mapping.Id, updated.Value!.Sources.Mapping!.Id);
        var retention = new Huy02InspectionRetentionContributor(db);
        Assert.Contains(state.SourceFile, await retention.KnownProjectFilesAsync(state.Source.Project, default));
        Assert.Contains((await retention.ReadAsync(state.SourceFile, default)).References, row => row.Kind == "ROAD_COVERAGE_MAPPING" && row.Id == mapping.Id);
        var immutable = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RoadCoverageMappings SET Reason='rewrite' WHERE Id={mapping.Id}"));
        Assert.Equal(51700, immutable.Number);
        var downgrade = await Assert.ThrowsAsync<SqlException>(() => db.GetService<IMigrator>().MigrateAsync("20261007115552_OwnerLifecycleActivation"));
        Assert.Equal(51790, downgrade.Number);
        await db.ProjectMembers.Where(row => row.ProjectId == state.Source.Project && row.UserId == state.Source.Supervisor)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.Status, ProjectMemberStatus.Ended));
        Assert.Equal(403, (await repository.ConfirmAsync(command, default)).Status);
    }

    [Fact]
    public async Task EmptyCoverageDowngradeAndReapplyRetainPopulatedRepairAndVerifiedSource()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        await db.GetService<IMigrator>().MigrateAsync("20261007115552_OwnerLifecycleActivation");
        Assert.True(await db.RepairItems.AsNoTracking().AnyAsync(row => row.Id == state.Item));
        Assert.True(await db.Files.AsNoTracking().AnyAsync(row => row.Id == state.SourceFile));
        Assert.True(await db.Set<RepairExecutionAuthorization>().AsNoTracking().AnyAsync(row => row.Id == state.Binding.AuthorizationId && row.FirstStartOriginId == state.FirstStart.Id));
        await db.Database.MigrateAsync();
        var read = await new RoadCoverageRepository(db, TimeProvider.System).ReadAsync(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, default);
        Assert.Equal(200, read.Status); Assert.Empty(read.Value!.History);
        Assert.Contains(read.Value.Scopes, row => row.ObligationId == state.Binding.ObligationId);
        Assert.Contains(read.Value.Sources, row => row.Kind == "WARRANTY" && row.Id == state.Warranty);
    }

    [Fact]
    public async Task ConcurrentSameKeyConfirmsOneMappingAndReceiptFailureRollsBackAllEffects()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var repo = new RoadCoverageRepository(db, TimeProvider.System);
        var view = (await repo.ReadAsync(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, default)).Value!;
        var command = new RoadCoverageCommand(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project,
            Input(view, state, "WARRANTY"), "TEST_ONLY_ROLLBACK_" + Guid.NewGuid(), view.Version);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER TR_TEST_LD07_ReceiptRollback ON IdempotencyRecords AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Operation='ld07.road-coverage.confirm.v1' AND IdempotencyKey LIKE 'TEST_ONLY_ROLLBACK_%') THROW 51799, 'TEST_ONLY receipt failure', 1; END");
        try { await Assert.ThrowsAsync<DbUpdateException>(() => repo.ConfirmAsync(command, default)); }
        finally { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER TR_TEST_LD07_ReceiptRollback"); db.ChangeTracker.Clear(); }
        Assert.False(await db.Set<RoadCoverageMapping>().AnyAsync(row => row.ProjectId == state.Source.Project));
        Assert.False(await db.IdempotencyRecords.AnyAsync(row => row.IdempotencyKey == command.Key));
        Assert.False(await db.AuditLogs.AnyAsync(row => row.EventType == "road_coverage_confirmed" && row.ActorUserId == state.Source.Supervisor));
        command = command with { Key = Guid.NewGuid().ToString() };
        async Task<RoadCoverageResult> Confirm()
        {
            await using var concurrent = sql.CreateDbContext();
            return await new RoadCoverageRepository(concurrent, TimeProvider.System).ConfirmAsync(command, default);
        }
        var results = await Task.WhenAll(Confirm(), Confirm());
        Assert.Equal(new[] { 200, 201 }, results.Select(row => row.Status).Order().ToArray());
        var mapping = await db.Set<RoadCoverageMapping>().SingleAsync(row => row.ProjectId == state.Source.Project);
        Assert.Equal(1, await db.OutboxMessages.CountAsync(row => row.Id == mapping.Id));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(row => row.IdempotencyKey == command.Key));
    }

    [Fact]
    public async Task SourceTimeMismatchPartialCoverageStaleEvidenceAndSyntheticPromotionCannotGrantPermission()
    {
        await using var db = sql.CreateDbContext(); var state = await Seed(db);
        var repo = new RoadCoverageRepository(db, TimeProvider.System);
        var view = (await repo.ReadAsync(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, default)).Value!;
        var input = Input(view, state, "MAINTENANCE_BASIS");
        var command = new RoadCoverageCommand(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, input, Guid.NewGuid().ToString(), view.Version);
        Assert.Equal(409, (await repo.ConfirmAsync(command with { Input = input with { ApplicableFromUtc = DateTimeOffset.UtcNow.AddYears(-2) } }, default)).Status);
        Assert.Equal(409, (await repo.ConfirmAsync(command with { Input = input with { Provenance = "REAL_SOURCE" } }, default)).Status);
        Assert.Equal(201, (await repo.ConfirmAsync(command, default)).Status);
        var scope = (await db.RepairObligations.AsNoTracking().Include(row => row.Scope).SingleAsync(row => row.Id == state.Binding.ObligationId)).Scope;
        // A sourced small scope must not be stretched, combined or proportionally allocated.
        var larger = RepairActualScope.Create(Guid.NewGuid(), scope.PhysicalRoadId, scope.LocationVersion, scope.RouteLabel,
            scope.From, scope.To + 1, scope.OffsetFrom, scope.OffsetTo);
        var resolved = await Resolve(db, state.Source.Project, larger, DateTimeOffset.UtcNow);
        Assert.Equal("COVERAGE_PARTIAL_OR_MISMATCH", resolved.State); Assert.False(resolved.PermitsExecution);
        var outside = await Resolve(db, state.Source.Project, scope, input.ApplicableToUtc);
        Assert.Equal("COVERAGE_PARTIAL_OR_MISMATCH", outside.State);
        var fresh = (await repo.ReadAsync(state.Source.Supervisor, UserRoleCode.Supervisor, state.Source.Project, default)).Value!;
        Assert.Equal(409, (await repo.ConfirmAsync(command with
        {
            Key = Guid.NewGuid().ToString(),
            ExpectedVersion = fresh.Version,
            Input = input with { Provenance = "REAL_SOURCE" }
        }, default)).Status);
        var upload = await db.UploadSessions.SingleAsync(row => row.FileId == state.SourceFile);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET Status={(byte)UploadSessionStatus.Failed} WHERE Id={upload.Id}");
        db.ChangeTracker.Clear();
        Assert.Equal(409, (await repo.ConfirmAsync(command, default)).Status);
        Assert.Equal("SOURCE_STALE", (await Resolve(db, state.Source.Project, scope, DateTimeOffset.UtcNow)).State);
        Assert.Single(await db.Set<RoadCoverageMapping>().Where(row => row.ProjectId == state.Source.Project).ToArrayAsync());
    }

    private static async Task<RoadCoverageResolution> Resolve(RoadGuardDbContext db, Guid project, RepairActualScope scope, DateTimeOffset at)
    {
        // Exercise the public eligibility consumer with the mapped item's scope in other assertions.
        // This internal resolver is the same production predicate used at fresh execution admission.
        var type = typeof(RoadCoverageRepository).Assembly.GetType("RoadGuardSystem.Repositories.Implementations.Repairs.RoadCoverageResolver")!;
        var method = type.GetMethod("Resolve", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        return await (Task<RoadCoverageResolution>)method.Invoke(null, [db, project, scope, at, CancellationToken.None])!;
    }
    private static RoadCoverageInputFact Input(RoadCoverageReadFact view, State state, string kind)
    {
        var handover = view.Sources.Single(row => row.Kind == "HANDOVER" && row.Id == state.Handover);
        var source = view.Sources.Single(row => row.Kind == kind && row.Id == (kind == "WARRANTY" ? state.Warranty : state.SourceFile));
        return new(state.Binding.ObligationId, view.Scopes.Single(row => row.ObligationId == state.Binding.ObligationId).ScopeHash,
            state.Handover, handover.Version, kind, source.Id, source.Version, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddDays(7),
            "TEST_ONLY", "TEST_ONLY source-linked confirmation; no real location or data verification");
    }
    private sealed record State(H4GenuineRepairSource.Source Source, Guid Package, Guid Item, RepairFieldTaskBinding Binding,
        FieldTaskStartOrigin FirstStart, Guid SourceFile, Guid Handover, Guid Warranty);
    private async Task<State> Seed(RoadGuardDbContext db)
    {
        var source = await H4GenuineRepairSource.Seed(db, sql);
        var defect = await db.Defects.AsNoTracking().SingleAsync(row => row.Id == source.Defect);
        var policies = new RepairPolicyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var draft = await policies.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, "create", null,
            new(defect.DefectTypeCode, "TEST_ONLY-checklist", [new("DepressionDepth", "mm", 0, 3)], [], "TEST_ONLY pinned policy"),
            null, Guid.NewGuid().ToString(), null), default); Assert.Equal(201, draft.Status);
        var published = await policies.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, "publish", draft.Value!.Id,
            null, "TEST_ONLY publication", Guid.NewGuid().ToString(), draft.Value.Version), default); Assert.Equal(201, published.Status);
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var version = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await repairs.CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(source.Defect, version, [new("FORMAL_REPAIR", true, new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "TEST_ONLY scope")], "TEST_ONLY package"),
            Guid.NewGuid().ToString(), version), default); Assert.Equal(201, created.Status);
        var package = Assert.IsType<RepairPackageFact>(created.Value).Id;
        var obligation = await db.RepairObligations.Where(row => row.DefectId == source.Defect).Select(row => row.Id).SingleAsync();
        var proposed = await repairs.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package,
            new(obligation, "FAST_TRACK", "TEST_ONLY conditional plan", "TEST_ONLY-checklist", "TEST_ONLY propose"), Guid.NewGuid().ToString(), created.Version!), default);
        Assert.Equal(201, proposed.Status); var item = Assert.IsType<RepairItemFact>(proposed.Value).Id;
        var assigned = await repairs.AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package, item,
            new(new(source.Defect, version, null, "REPORTER", source.Route, source.Set, null, null, "POST_REPAIR", 1, "{}", null,
                source.Crew, DateTimeOffset.UtcNow.AddDays(1)), published.Value!.Id, "TEST_ONLY assign"), Guid.NewGuid().ToString(), proposed.Version!), default);
        Assert.Equal(201, assigned.Status);
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.ItemId == item);
        var native = await db.FieldInspectionTasks.SingleAsync(row => row.Id == binding.TaskId);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var admission = new FieldAdmissionContext(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", true);
        var accepted = await field.ExecuteAsync(new(source.Project, native.Id, "accept",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("TEST_ONLY accept"), Guid.NewGuid().ToString(),
            Convert.ToBase64String(native.RowVersion), admission), _ => Task.FromResult(true), default); Assert.Equal(201, accepted.Status);
        var acceptedVersion = Convert.ToBase64String(await db.FieldInspectionTasks.AsNoTracking().Where(row => row.Id == native.Id).Select(row => row.RowVersion).SingleAsync());
        var started = await field.ExecuteAsync(new(source.Project, native.Id, "start",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact(Guid.NewGuid(), DateTimeOffset.UtcNow), Guid.NewGuid().ToString(),
            acceptedVersion, admission), _ => Task.FromResult(true), default); Assert.Equal(201, started.Status);
        var first = await db.Set<FieldTaskStartOrigin>().SingleAsync(row => row.TaskId == native.Id);
        var now = DateTimeOffset.UtcNow;
        var file = StoredFile.Create(Guid.NewGuid(), "TEST_ONLY/ld07/" + Guid.NewGuid(), "coverage.pdf", "application/pdf", 4, new string('e', 64), source.Supervisor, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, source.Supervisor, file.StorageUri, "TEST_ONLY_COVERAGE", file.MimeType, 4, file.Checksum, 8388608, now.AddHours(24));
        upload.StartUploading("TEST_ONLY", now); db.AddRange(file, upload, FileScope.Create(Guid.NewGuid(), file.Id, source.Project, source.Project,
            source.Supervisor, "TEST_ONLY_COVERAGE", now)); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var day = DateOnly.FromDateTime(now.UtcDateTime);
        var handover = HandoverDocument.Create(Guid.NewGuid(), source.Project, "TEST_ONLY-" + Guid.NewGuid(), day.AddDays(-2), source.Supervisor, file.Id, "TEST_ONLY source");
        db.Add(handover); await db.SaveChangesAsync();
        var warranty = Warranty.Create(Guid.NewGuid(), source.Project, source.Road, handover.Id, day.AddDays(-2), day.AddDays(-1), day.AddDays(30),
            null, WarrantyScope.RoadSection, "TEST_ONLY", file.Id, WarrantyStatus.Active); db.Add(warranty); await db.SaveChangesAsync();
        return new(source, package, item, binding, first, file.Id, handover.Id, warranty.Id);
    }
}
