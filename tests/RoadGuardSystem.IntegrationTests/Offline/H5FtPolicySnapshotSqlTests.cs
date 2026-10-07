using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Clocks;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
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

namespace RoadGuardSystem.IntegrationTests.Offline;

public sealed class H5FtPolicySnapshotSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublishedTestOnlyFtPolicyIsPinnedAndDownloadedForCurrentCrewWithoutExecution()
    {
        await using var db = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(db, sql);
        var defect = await db.Defects.AsNoTracking().SingleAsync(row => row.Id == source.Defect);
        var policies = new RepairPolicyRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var definition = new RepairPolicyDefinition(defect.DefectTypeCode, "TEST_ONLY-ft-checklist-v1",
            [new("TEST_ONLY-depth", "mm", 0, 3)], ["TEST_ONLY-unstable"], "TEST_ONLY policy fixture");
        var draft = await policies.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            "create", null, definition, null, Guid.NewGuid().ToString(), null), default);
        Assert.Equal(201, draft.Status);
        var published = await policies.ExecuteAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            "publish", draft.Value!.Id, null, "TEST_ONLY publish", Guid.NewGuid().ToString(), draft.Value.Version), default);
        Assert.Equal(201, published.Status);
        var policyId = published.Value!.Id;
        var policy = await db.Set<RepairPolicyRevision>().AsNoTracking().Include(row => row.Measurements)
            .SingleAsync(row => row.Id == policyId);
        var repairs = new RepairWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System);
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var created = await repairs.CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            new(source.Defect, defectVersion, [new("FORMAL_REPAIR", true,
                new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "TEST_ONLY repair")], "TEST_ONLY package"),
            Guid.NewGuid().ToString(), defectVersion), default);
        Assert.Equal(201, created.Status);
        var package = await db.Set<RepairPackage>().Include(row => row.Obligations)
            .SingleAsync(row => row.Id == Assert.IsType<RepairPackageFact>(created.Value).Id);
        var proposed = await repairs.ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            package.Id, new(package.Obligations[0].Id, "FAST_TRACK", "TEST_ONLY conditional plan",
                definition.ChecklistVersion, "TEST_ONLY proposal"), Guid.NewGuid().ToString(), created.Version!), default);
        Assert.Equal(201, proposed.Status);
        var itemId = Assert.IsType<RepairItemFact>(proposed.Value).Id;
        var assigned = await repairs.AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            package.Id, itemId, new(new(source.Defect, defectVersion, null, "REPORTER", source.Route, source.Set,
                null, null, "POST_REPAIR", 1, "{}", null, source.Crew, DateTimeOffset.UtcNow.AddDays(1)),
                policyId, "TEST_ONLY conditional assignment"), Guid.NewGuid().ToString(), proposed.Version!), default);
        Assert.Equal(201, assigned.Status);
        var task = Assert.IsType<RepairTaskBindingFact>(assigned.Value);
        var binding = await db.Set<RepairFieldTaskBinding>().AsNoTracking().SingleAsync(row => row.ItemId == itemId);
        Assert.Equal(policyId, binding.PolicyRevisionId);
        var expectedPolicyHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            policy.Id,
            policy.ProjectId,
            policy.Revision,
            policy.DefectTypeCode,
            policy.ChecklistVersion,
            policy.PublishedAt,
            policy.PublishedBy,
            measurements = policy.Measurements.OrderBy(row => row.Code, StringComparer.Ordinal).ToArray(),
            stopConditions = policy.StopConditions.Order(StringComparer.Ordinal).ToArray()
        }, Json))).ToLowerInvariant();
        Assert.Equal(expectedPolicyHash, binding.PolicyContentHash);
        var deviceId = Guid.NewGuid();
        using var keys = OfflineDeviceKeys.Generate(source.Crew, deviceId.ToString("D"));
        var registration = OfflineDeviceRegistration.Register(Guid.NewGuid(), source.Project, source.Crew, deviceId,
            1, UserRoleCode.RepairCrew, keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey,
            DateTimeOffset.UtcNow);
        db.Add(registration); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var validator = new OfflineAdmissionValidator(db, TimeProvider.System);
        var field = new FieldInspectionWorkflowRepository(db, new IdempotencyOperationService(db), TimeProvider.System,
            validator, validator);
        var offline = new OfflineWorkflowRepository(db, TimeProvider.System, field, validator, repairs);
        var guard = new ProjectScopeGuard(new ProjectMembershipReadModel(db), TimeProvider.System);
        async Task<bool> Current(CancellationToken token) =>
            await guard.AuthorizeAsync(source.Crew, UserRoleCode.RepairCrew, source.Project, token) is not null;
        var captured = await offline.ExecuteAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project,
            "snapshot-create", new OfflineSnapshotData(registration.Id, task.TaskId), null,
            Guid.NewGuid().ToString(), null), OfflineContractMapping.RepositoryAlgorithms, Current, default);
        Assert.Equal(201, captured.Status);
        var id = JsonSerializer.SerializeToElement(captured.Value, Json).GetProperty("id").GetGuid();
        var persisted = await db.Set<OfflineTaskSnapshot>().AsNoTracking().SingleAsync(row => row.Id == id);
        Assert.Equal(binding.AssignmentId, persisted.AssignmentId);
        Assert.Equal(source.Crew, persisted.OriginalActorId);
        var persistedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(persisted.SnapshotJson))).ToLowerInvariant();
        Assert.Equal(persistedHash, persisted.ContentHash);
        using var payload = JsonDocument.Parse(persisted.SnapshotJson);
        var repair = payload.RootElement.GetProperty("repair");
        Assert.Equal(itemId, repair.GetProperty("itemId").GetGuid());
        Assert.Equal(binding.PolicyRevisionId, repair.GetProperty("policyRevisionId").GetGuid());
        Assert.Equal(expectedPolicyHash, repair.GetProperty("policyContentHash").GetString());
        var body = repair.GetProperty("policy");
        Assert.Equal(policyId, body.GetProperty("id").GetGuid());
        Assert.Equal(policy.Revision, body.GetProperty("revision").GetInt32());
        Assert.Equal(defect.DefectTypeCode, body.GetProperty("defectTypeCode").GetString());
        Assert.Equal(definition.ChecklistVersion, body.GetProperty("checklistVersion").GetString());
        var rule = Assert.Single(body.GetProperty("measurements").EnumerateArray());
        Assert.Equal("TEST_ONLY-depth", rule.GetProperty("code").GetString());
        Assert.Equal("mm", rule.GetProperty("unit").GetString());
        Assert.Equal(3m, rule.GetProperty("maximum").GetDecimal());
        Assert.Equal("TEST_ONLY-unstable", Assert.Single(body.GetProperty("stopConditions").EnumerateArray()).GetString());
        Assert.Equal("UNKNOWN_OWNER_MAPPING", repair.GetProperty("eligibility").GetString());
        Assert.False(await db.Set<RepairExecutionStart>().AnyAsync(row => row.ItemId == itemId));
        Assert.False(await db.Set<DeadlineClock>().AnyAsync(row => row.TargetId == itemId));
        var read = new OfflineWorkflowCommand(source.Crew, UserRoleCode.RepairCrew, source.Project,
            "snapshot-get", null, id, null, null);
        var downloaded = await offline.ExecuteAsync(read, OfflineContractMapping.RepositoryAlgorithms, Current, default);
        Assert.Equal(200, downloaded.Status);
        var view = JsonSerializer.SerializeToElement(downloaded.Value, Json);
        Assert.Equal(persisted.ContentHash, view.GetProperty("contentHash").GetString());
        Assert.Equal(expectedPolicyHash, view.GetProperty("snapshot").GetProperty("repair")
            .GetProperty("policyContentHash").GetString());
        async Task<bool> CurrentPm(CancellationToken token) =>
            await guard.AuthorizeAsync(source.Pm, UserRoleCode.ProjectManager, source.Project, token) is not null;
        Assert.Equal(403, (await offline.ExecuteAsync(read with { ActorId = source.Pm, Role = UserRoleCode.ProjectManager },
            OfflineContractMapping.RepositoryAlgorithms, CurrentPm, default)).Status);
        (await db.FieldInspectionAssignments.SingleAsync(row => row.Id == binding.AssignmentId))
            .End(DateTimeOffset.UtcNow, "TEST_ONLY stale assignment");
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(403, (await offline.ExecuteAsync(read, OfflineContractMapping.RepositoryAlgorithms, Current, default)).Status);
    }
}
