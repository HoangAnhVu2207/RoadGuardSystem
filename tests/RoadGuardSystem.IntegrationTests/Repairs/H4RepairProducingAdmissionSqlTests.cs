using RoadGuardSystem.Repositories.Repairs;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Repairs;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class H4RepairProducingAdmissionSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = new(System.Text.Json.JsonSerializerDefaults.Web);
    [Fact]
    public async Task ActualCurrentPmCreatesAnchoredPackageAndMandatoryPhysicalScopeFromGenuineReporterDefect()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var input = await PackageInput(db, source);
        var result = await Repo(db).CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            input, Guid.NewGuid().ToString(), input.DefectVersion), default);
        Assert.Equal(201, result.Status); var view = Assert.IsType<RepairPackageFact>(result.Value);
        db.ChangeTracker.Clear(); var package = await db.Set<RepairPackage>().Include(row => row.Obligations).SingleAsync(row => row.Id == view.Id);
        Assert.Equal(DefectStatus.Open, package.DefectStatusAtAnchor); Assert.False(package.IsComplete);
        Assert.Equal(source.Road, Assert.Single(package.Obligations).Scope.PhysicalRoadId);
        Assert.False(string.IsNullOrWhiteSpace(package.Obligations[0].Scope.LocationVersion));
        Assert.Equal(input.Obligations[0].Scope.FromMeters, package.Obligations[0].Scope.From);
    }

    [Fact]
    public async Task BodyPhysicalRoadCannotPartitionAnActualDefectIntoAnotherRoadIdentity()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var input = await PackageInput(db, source); var declared = input.Obligations[0];
        input = input with { Obligations = [declared with { Scope = declared.Scope with { PhysicalRoadId = Guid.NewGuid() } }] };
        var result = await Repo(db).CreatePackageAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            input, Guid.NewGuid().ToString(), input.DefectVersion), default);
        Assert.Equal(409, result.Status); Assert.False(await db.Set<RepairPackage>().AnyAsync(row => row.DefectId == source.Defect));
    }

    [Fact]
    public async Task CurrentCrewCannotUsePackageCreationToAppointItsOwnRepairAuthority()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var input = await PackageInput(db, source);
        var result = await Repo(db).CreatePackageAsync(new(source.Crew, UserRoleCode.RepairCrew, source.Project,
            input, Guid.NewGuid().ToString(), input.DefectVersion), default);
        Assert.Equal(403, result.Status); Assert.False(await db.Set<RepairPackage>().AnyAsync(row => row.DefectId == source.Defect));
    }

    [Fact]
    public async Task CurrentPmProposalPersistsExactPlanAndAwaitsActualNormalSupervisorApproval()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var package = await BarePackage(db, source); var input = new RepairItemProposeData(package.Obligations[0].Id,
            "NORMAL", "actual plan", "checklist-v1", "normal proposal");
        var result = await Repo(db).ProposeItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project,
            package.Id, input, Guid.NewGuid().ToString(), Version(db, package)), default);
        Assert.Equal(201, result.Status); var view = Assert.IsType<RepairItemFact>(result.Value);
        db.ChangeTracker.Clear(); var item = await db.Set<RepairItem>().SingleAsync(row => row.Id == view.Id);
        Assert.Equal(RepairItemState.AwaitingApproval, item.State); Assert.Equal(input.RepairPlan, item.RepairPlan);
        Assert.Equal(input.ChecklistVersion, item.ChecklistVersion); Assert.Null(item.ApprovedPlanHash);
        Assert.Equal(item.Id, (await db.Set<RepairObligation>().SingleAsync(row => row.Id == item.ObligationId)).CurrentRepairItemId);
        Assert.False(await db.Set<RepairFieldTaskBinding>().AnyAsync(row => row.ItemId == item.Id));
    }

    [Fact]
    public async Task ActualCurrentSupervisorApprovesPinnedNormalPlanWithoutCreatingCrewExecution()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var (package, item) = await StagedProposal(db, source, false);
        var result = await Repo(db).ApproveItemAsync(new(source.Supervisor, UserRoleCode.Supervisor, source.Project,
            package.Id, item.Id, new("actual Supervisor approval"), Guid.NewGuid().ToString(), Version(db, item)), default);
        Assert.Equal(201, result.Status); db.ChangeTracker.Clear(); var saved = await db.Set<RepairItem>().SingleAsync(row => row.Id == item.Id);
        Assert.Equal(RepairItemState.ReadyToAssign, saved.State); Assert.Equal(saved.ProposalPlanHash, saved.ApprovedPlanHash);
        Assert.Equal(source.Supervisor, saved.ApprovedBy); Assert.False(await db.Set<RepairFieldTaskBinding>().AnyAsync(row => row.ItemId == item.Id));
    }

    [Fact]
    public async Task ActualCurrentPmAssignsApprovedNormalItemToCurrentCrewAndCompleteNativeBinding()
    {
        await using var db = sql.CreateDbContext(); var source = await H4GenuineRepairSource.Seed(db, sql);
        var (package, item) = await StagedProposal(db, source, true);
        var defectVersion = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        var task = new RepairFieldTaskData(source.Defect, defectVersion, null, "REPORTER", source.Route, source.Set,
            null, null, "POST_REPAIR", 1, "{}", null, source.Crew, DateTimeOffset.UtcNow.AddDays(1));
        var result = await Repo(db).AssignItemAsync(new(source.Pm, UserRoleCode.ProjectManager, source.Project, package.Id,
            item.Id, new(task, null, "actual initial assignment"), Guid.NewGuid().ToString(), Version(db, item)), default);
        Assert.Equal(201, result.Status); var view = Assert.IsType<RepairTaskBindingFact>(result.Value);
        db.ChangeTracker.Clear(); var binding = await db.Set<RepairFieldTaskBinding>().SingleAsync(row => row.ItemId == item.Id);
        Assert.Equal(view.TaskId, binding.TaskId); Assert.Equal(view.AssignmentId, binding.AssignmentId);
        Assert.Null(binding.AuthorizationId); Assert.Null(binding.PolicyRevisionId);
        Assert.Equal(binding.Id, (await db.Set<RepairItem>().SingleAsync(row => row.Id == item.Id)).CurrentBindingId);
        var native = await db.FieldInspectionTasks.SingleAsync(row => row.Id == binding.TaskId);
        Assert.Equal("NORMAL", native.TaskMode); Assert.Equal(item.Id, native.RepairItemId);
        Assert.Equal(source.Crew, (await db.FieldInspectionAssignments.SingleAsync(row => row.Id == binding.AssignmentId)).AssignedToUserId);
    }

    private static RepairWorkflowRepository Repo(RoadGuardDbContext db) => new(db, new IdempotencyOperationService(db), TimeProvider.System);
    private static string Version<T>(RoadGuardDbContext db, T entity) where T : class
        => Convert.ToBase64String(db.Entry(entity).Property<byte[]>("RowVersion").CurrentValue!);
    private static async Task<RepairPackageCreateData> PackageInput(RoadGuardDbContext db, H4GenuineRepairSource.Source source)
    {
        var version = Convert.ToBase64String(await db.Defects.Where(row => row.Id == source.Defect)
            .Select(row => EF.Property<byte[]>(row, "RowVersion")).SingleAsync());
        return new(source.Defect, version, [new("FORMAL_REPAIR", true,
            new(source.Road, source.Route, source.Set, null, null, 1, 2, 0, 1), "mandatory repair")], "actual package proposal");
    }
    private static async Task<RepairPackage> BarePackage(RoadGuardDbContext db, H4GenuineRepairSource.Source source)
    {
        var obligation = RepairObligation.Create(Guid.NewGuid(), source.Project, source.Defect, RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), source.Road, "h4-frame-v1:" + Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
                { routeVersionId = source.Route, segmentSetId = (Guid?)source.Set, layoutRevisionId = (Guid?)null,
                    slabId = (string?)null, crsProfileRevisionId = (Guid?)null },
                    Json))).ToLowerInvariant(),
                "actual road", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), source.Project, source.Defect, [obligation]);
        db.Add(package); db.Entry(package).Property(row => row.DefectStatusAtAnchor).CurrentValue = DefectStatus.Open;
        await db.SaveChangesAsync(); return package;
    }
    private static async Task<(RepairPackage Package, RepairItem Item)> StagedProposal(RoadGuardDbContext db,
        H4GenuineRepairSource.Source source, bool approved)
    {
        var package = await BarePackage(db, source); var now = DateTimeOffset.UtcNow;
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), package.Obligations[0], RepairMode.Normal, source.Pm,
            UserRoleCode.ProjectManager, now, new("actual plan", "checklist-v1"));
        if (approved) item.Approve(source.Supervisor, UserRoleCode.Supervisor, now);
        package.AddItem(item); db.Entry(package).Property<long>("MutationRevision").CurrentValue++;
        await db.SaveChangesAsync(); db.Entry(package.Obligations[0]).Property(row => row.CurrentRepairItemId).CurrentValue = item.Id;
        await db.SaveChangesAsync(); return (package, item);
    }
}
