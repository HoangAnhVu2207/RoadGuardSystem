using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Inspections;
using RoadGuardSystem.Repositories.Implementations.Offline;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Offline;
using Xunit;


namespace RoadGuardSystem.TestFixtures;

internal static class ND01OriginalWindowFixture
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    internal sealed record Source(Guid Pm, Guid Crew, Guid Supervisor, Guid Project, Guid Road, Guid Route, Guid Set, Guid Defect);
    internal sealed record State(Source Source, Guid Package, Guid Item, Guid Authorization,
        FieldTaskStartOrigin First, DateTimeOffset Expiry, OfflineDeviceKeys Keys, OfflineDeviceRegistration Device,
        OfflineOperationInput Operation, OfflineSignedBatchInput Batch);
    internal static OfflineWorkflowRepository Offline(RoadGuardDbContext db, TimeProvider clock)
    {
        var validator = new OfflineAdmissionValidator(db, clock);
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), clock);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), clock, validator, validator);
        return new(db, clock, field, validator, repairs);
    }
    internal static async Task<State> Prepare(RoadGuardDbContext db, Source source, string condition = "valid")
    {
        async Task<bool> Current(CancellationToken token) => await new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System)
            .AuthorizeAsync(source.Crew, UserRoleCode.RepairCrew, source.Project, token) is not null;
        var policies = new RepairPolicyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var defect = await db.Defects.AsNoTracking().SingleAsync(row => row.Id == source.Defect);
        var draft = await policies.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, "create", null,
            new(defect.DefectTypeCode, "nd01-checklist", [new("DepressionDepth", "mm", 0, 3)], ["DANGER"], "original-window policy"),
            null, Guid.NewGuid().ToString(), null), default); Assert.Equal(201, draft.Status);
        var published = await policies.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, "publish", draft.Value!.Id,
            null, "publish", Guid.NewGuid().ToString(), draft.Value.Version), default); Assert.Equal(201, published.Status);
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var version = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await repairs.CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(source.Defect, version, [new("FORMAL_REPAIR", true, new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "exact scope")], "package"),
            Guid.NewGuid().ToString(), version), default); Assert.Equal(201, created.Status);
        var package = Assert.IsType<RepairPackageFact>(created.Value).Id;
        var obligation = await db.RepairObligations.SingleAsync(row => row.DefectId == source.Defect);
        var proposed = await repairs.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package,
            new(obligation.Id, "FAST_TRACK", "conditional plan", "nd01-checklist", "propose"), Guid.NewGuid().ToString(), created.Version!), default);
        Assert.Equal(201, proposed.Status); var item = Assert.IsType<RepairItemFact>(proposed.Value).Id;
        var assigned = await repairs.AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package, item,
            new(new(source.Defect, version, null, "REPORTER", source.Route, source.Set, null, null, "POST_REPAIR", 1, "{}", null,
                source.Crew, DateTimeOffset.UtcNow.AddDays(2)), published.Value!.Id, "assign"), Guid.NewGuid().ToString(), proposed.Version!), default);
        Assert.Equal(201, assigned.Status);
        var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.ItemId == item);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var direct = new FieldAdmissionContext(source.Crew, UserRoleCode.RepairCrew, source.Crew, "DIRECT", condition != "unverified-first");
        var taskVersion = Convert.ToBase64String(await db.FieldInspectionTasks.Where(row => row.Id == binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        var accepted = await field.ExecuteAsync(new(source.Project, binding.TaskId, "accept",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldTaskActionInputFact("accept"), Guid.NewGuid().ToString(), taskVersion, direct), Current, default);
        Assert.Equal(201, accepted.Status);
        taskVersion = Convert.ToBase64String(await db.FieldInspectionTasks.Where(row => row.Id == binding.TaskId).Select(row => row.RowVersion).SingleAsync());
        var firstResult = await field.ExecuteAsync(new(source.Project, binding.TaskId, "start",
            new RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections.FieldStartInputFact(Guid.NewGuid(), DateTimeOffset.UtcNow), Guid.NewGuid().ToString(), taskVersion, direct), Current, default);
        Assert.Equal(201, firstResult.Status);
        var first = await db.Set<FieldTaskStartOrigin>().AsNoTracking().SingleAsync(row => row.TaskId == binding.TaskId);
        var before = await VerifiedFile(db, source.Project, binding.TaskId, source.Crew, "BEFORE", "image/jpeg");
        var itemVersion = Convert.ToBase64String(await db.RepairItems.Where(row => row.Id == item).Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var assessed = await repairs.AssessAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project, package, item, binding.TaskId,
            new(Guid.NewGuid(), first.Id, [new("depth", "DepressionDepth", 1, "KNOWN", null, "LENGTH", "mm", null, null, "on road", "FIELD", "measured", "measured")],
                [new(Guid.NewGuid(), before.Id, "BEFORE", before.Checksum, before.MimeType, DateTimeOffset.UtcNow, "position and depth")],
                new("POSITION_CHECKLIST", "ROUTE_CHAINAGE_MARKINGS_CONFIRMED", null, null, null, null, source.Route, 1),
                StopConditions: new() { ["DANGER"] = false }), Guid.NewGuid().ToString(), itemVersion), default);
        Assert.Equal(201, assessed.Status); var assessment = Assert.IsType<RepairAssessmentFact>(assessed.Value); Assert.Equal("READY", assessment.Readiness);
        var file = await VerifiedFile(db, source.Project, source.Project, source.Supervisor, "HANDOVER_DOCUMENT", "application/pdf");
        var day = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var handover = HandoverDocument.Create(Guid.NewGuid(), source.Project, Guid.NewGuid().ToString(), day.AddDays(-2), source.Supervisor, file.Id, "accepted road");
        db.Add(handover); await db.SaveChangesAsync();
        var warranty = Warranty.Create(Guid.NewGuid(), source.Project, source.Road, handover.Id, day.AddDays(-2), day.AddDays(-1), day.AddDays(30),
            null, WarrantyScope.RoadSection, "road scope", file.Id, WarrantyStatus.Active); db.Add(warranty); await db.SaveChangesAsync();
        var coverage = new RoadCoverageRepository(db, TimeProvider.System);
        var read = (await coverage.ReadAsync(source.Supervisor, UserRoleCode.Supervisor, source.Project, default)).Value!;
        var confirmed = condition == "missing" ? null : await coverage.ConfirmAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            new(obligation.Id, read.Scopes.Single(row => row.ObligationId == obligation.Id).ScopeHash, handover.Id,
                read.Sources.Single(row => row.Id == handover.Id).Version, "WARRANTY", warranty.Id, read.Sources.Single(row => row.Id == warranty.Id).Version,
                DateTimeOffset.UtcNow.AddHours(condition == "mismatched" ? 1 : -1), DateTimeOffset.UtcNow.AddDays(7), condition == "test-only" ? "TEST_ONLY" : "REAL_SOURCE", "actual source-linked acceptance"), Guid.NewGuid().ToString(), read.Version), default);
        if (confirmed is not null) Assert.Equal(201, confirmed.Status);
        var eligible = await new RepairEligibilityRepository(db, TimeProvider.System).ReadAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project, package, item), default);
        Assert.Equal(200, eligible.Status); if (condition == "valid") Assert.Empty(eligible.Value!.MissingReasons);
        var deviceId = Guid.NewGuid(); var keys = OfflineDeviceKeys.Generate(source.Crew, deviceId.ToString("D"));
        var device = OfflineDeviceRegistration.Register(Guid.NewGuid(), source.Project, source.Crew, deviceId, 1, UserRoleCode.RepairCrew,
            keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey, DateTimeOffset.UtcNow);
        db.Add(device); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var snapshot = await Offline(db, TimeProvider.System).ExecuteAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project,
            "snapshot-create", new OfflineSnapshotData(device.Id, binding.TaskId), null, Guid.NewGuid().ToString(), null),
            OfflineContractMapping.RepositoryAlgorithms, Current, default); Assert.Equal(201, snapshot.Status);
        var view = JsonSerializer.SerializeToElement(snapshot.Value, Json); var origin = Guid.NewGuid();
        var payload = new OfflineRepairPayload(item, "execution-start", Start: new(origin, first.Id, DateTimeOffset.UtcNow, assessment.Id, deviceId));
        var operation = new OfflineOperationInput(1, origin, origin, "REPAIR_EXECUTION_START", source.Crew, deviceId,
            view.GetProperty("id").GetGuid(), binding.TaskId, binding.AssignmentId, view.GetProperty("taskVersion").GetString()!,
            RepairCoreHash.ExecutionStart(binding.TaskId, item, source.Crew, payload.Start!), [], null, Repair: payload);
        var batchId = Guid.NewGuid();
        var manifest = OfflineWorkflowEngine.CanonicalManifest(source.Project, batchId, device.Id, [OfflineWorkflowEngine.Describe(operation)]);
        var batch = new OfflineSignedBatchInput(batchId, device.Id, [operation], OfflinePackageAuthentication.SignClaim(manifest, keys));
        var authorization = await db.Set<RepairExecutionAuthorization>().AsNoTracking().SingleAsync(row => row.Id == binding.AuthorizationId);
        return new(source, package, item, authorization.Id, first, authorization.ExpiresAt ?? first.ServerReceivedAt.AddHours(24), keys, device, operation, batch);
    }
    private static async Task<StoredFile> VerifiedFile(RoadGuardDbContext db, Guid project, Guid target, Guid actor, string purpose, string mime)
    {
        // Controlled backend fixture sources use the retained upload state machine and real source validation.
        // This asserts backend admission, not external file contents or field accuracy; no TEST_ONLY source is promoted.
        var now = DateTimeOffset.UtcNow; var file = StoredFile.Create(Guid.NewGuid(), "private/nd01/" + Guid.NewGuid(), "source", mime, 4, new string('e', 64), actor, now, null);
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, actor, file.StorageUri, purpose, mime, 4, file.Checksum, 8388608, now.AddHours(24));
        upload.StartUploading("fixture", now); db.AddRange(file, upload, FileScope.Create(Guid.NewGuid(), file.Id, project, target, actor, purpose, now)); await db.SaveChangesAsync();
        upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        return file;
    }
}
