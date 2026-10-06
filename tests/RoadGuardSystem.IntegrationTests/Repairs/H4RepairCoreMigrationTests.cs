using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Services.Exports;
using RoadGuardSystem.Services.Reporting;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Implementations.Reporting;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

public sealed class H4RepairCoreMigrationTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);
    public Task InitializeAsync() => fixture.InitializeAsync();
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>()
        .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);

    [Fact]
    public async Task LegacyGlobalSupervisorReportRemainsAvailableWithoutNewRepairSourcePermission()
    {
        await using var db=Db();await db.Database.MigrateAsync();var now=DateTimeOffset.UtcNow;
        var project=Project.Create(Guid.NewGuid(),Guid.NewGuid().ToString(),"Legacy global reporting scope",null,null,null,null,now);
        if(!await db.Roles.AnyAsync(role=>role.Code==UserRoleCode.Supervisor)) db.Add(new ApplicationRole(UserRoleCode.Supervisor,"Supervisor"));
        var name=Guid.NewGuid().ToString();var actor=new ApplicationUser {Id=Guid.NewGuid(),UserName=name,NormalizedUserName=name.ToUpperInvariant(),PasswordHash="fixture",DisplayName="legacy reporting fixture",RoleCode=UserRoleCode.Supervisor,CreatedAt=now};
        db.AddRange(project,actor);await db.SaveChangesAsync();
        var identity=new IdentityRepository(db);var guard=new ProjectScopeGuard(new ProjectMembershipReadModel(db),TimeProvider.System);
        var legacy=new ReportingService(new ReportingRepository(db),identity,guard,TimeProvider.System);
        Assert.Equal("success",(await legacy.CaptureAsync(actor.Id,project.Id,new(),default)).Code);
        var reader=new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db,TimeProvider.System));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>reader.CaptureAsync(actor.Id,project.Id,new(),default));
        var integrated=new ReportingService(new ReportingRepository(db),identity,guard,TimeProvider.System,currentRepairReaders:[reader]);
        var result=await integrated.CaptureAsync(actor.Id,project.Id,new(),default);
        Assert.Equal("success",result.Code);
        var metric=Assert.Single(result.Value!.Summary.Metrics.Where(row=>row.Code=="repairItemsByStatus"));
        Assert.Equal("UNAVAILABLE",metric.Availability);Assert.Null(metric.Value);
        Assert.Empty(metric.SourceRefs);
    }

    [Fact]
    public async Task PublicationMustPinExactDraftMeasurementRules()
    {
        await using var db=Db(); await db.Database.MigrateAsync(); var now=DateTimeOffset.UtcNow;
        var project=Project.Create(Guid.NewGuid(),Guid.NewGuid().ToString(),"Controlled policy source",null,null,null,null,now);
        if(!await db.Roles.AnyAsync(role=>role.Code==UserRoleCode.ProjectManager)) db.Add(new ApplicationRole(UserRoleCode.ProjectManager,"PM"));
        var name=Guid.NewGuid().ToString(); var actor=new ApplicationUser {Id=Guid.NewGuid(),UserName=name,NormalizedUserName=name.ToUpperInvariant(),PasswordHash="fixture",DisplayName="policy fixture",RoleCode=UserRoleCode.ProjectManager,CreatedAt=now};
        db.AddRange(project,actor);await db.SaveChangesAsync();
        var draft=Guid.NewGuid();var change=Guid.NewGuid();var wrong=Guid.NewGuid();
        // Controlled technical source pins, not an owner-approved real project threshold.
        const string measurements="[{\"Code\":\"depth\",\"Unit\":\"mm\",\"Minimum\":0,\"Maximum\":2}]";
        const string stops="[\"OBSERVATION_UNAVAILABLE\"]";
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairPolicyDrafts (Id,ProjectId) VALUES ({draft},{project.Id})");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairPolicyDraftChanges (Id,ActorId,At,Reason,DefectTypeCode,ChecklistVersion,Measurements,StopConditions,DraftId) VALUES ({change},{actor.Id},{now},'controlled fixture','CONTROLLED','check-v1',{measurements},{stops},{draft})");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairPolicyDrafts SET CurrentChangeId={change} WHERE Id={draft}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairPolicyRevisions (Id,ProjectId,Revision,PublishedBy,PublishedAt,DefectTypeCode,ChecklistVersion,StopConditions) VALUES ({wrong},{project.Id},1,{actor.Id},{now},'CONTROLLED','check-v1',{stops})");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairPolicyMeasurementRules (Code,PolicyRevisionId,Unit,Minimum,Maximum) VALUES ('depth',{wrong},'mm',0,99)");
        var error=await Assert.ThrowsAsync<SqlException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairPolicyDrafts SET PublishedRevisionId={wrong} WHERE Id={draft}"));
        Assert.Equal(51222,error.Number);
        var right=Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairPolicyRevisions (Id,ProjectId,Revision,PublishedBy,PublishedAt,DefectTypeCode,ChecklistVersion,StopConditions) VALUES ({right},{project.Id},2,{actor.Id},{now},'CONTROLLED','check-v1',{stops})");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairPolicyMeasurementRules (Code,PolicyRevisionId,Unit,Minimum,Maximum) VALUES ('depth',{right},'mm',0,2)");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairPolicyDrafts SET PublishedRevisionId={right} WHERE Id={draft}");
        Assert.Equal(right,await db.Set<RepairPolicyDraft>().Where(row=>row.Id==draft).Select(row=>row.PublishedRevisionId).SingleAsync());
        var lateRule=await Assert.ThrowsAsync<SqlException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairPolicyMeasurementRules (Code,PolicyRevisionId,Unit,Minimum,Maximum) VALUES ('late-rule',{right},'mm',0,2)"));
        Assert.Equal(51209,lateRule.Number);
    }

    [Fact]
    public async Task NullLegacyProjectCannotBecomeRepairScopeAuthority()
    {
        await using var db = Db(); await db.Database.MigrateAsync();
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Scope fixture", null, null, null, null, DateTimeOffset.UtcNow);
        var type = DefectType.Create("H4" + Guid.NewGuid().ToString("N"), "Legacy source");
        var defect = Defect.Create(Guid.NewGuid(), type.Code);
        db.AddRange(project, type, defect); await db.SaveChangesAsync();
        var failure = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO RepairPackages (Id,ProjectId,DefectId,MutationRevision) VALUES ({Guid.NewGuid()},{project.Id},{defect.Id},0)"));
        Assert.Equal(51210, failure.Number);
    }

    [Fact]
    public async Task DecisionHeadsCorrectionRollbackImmutableHistoryAndPopulatedDowngrade()
    {
        await using var db = Db(); await db.Database.MigrateAsync();
        var now = DateTimeOffset.UtcNow; var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Correction SQL fixture", null, null, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "R");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry.CreateLineString([new(0,0), new(10,0)]), now, "sampleOnly SQL identity; no CRS accuracy claim");
        var type = DefectType.Create("H4" + Guid.NewGuid().ToString("N"), "Repair source");
        var defect = Defect.Create(Guid.NewGuid(), project.Id, route.Id, null, type.Code, null, DefectSeverity.Low, DefectStatus.Open, geometry.CreatePoint(new Coordinate(5,0)), now);
        if (!await db.Set<ApplicationRole>().AnyAsync(role => role.Code == UserRoleCode.ProjectManager))
            db.Add(new ApplicationRole(UserRoleCode.ProjectManager, "PM"));
        var actor = new ApplicationUser { Id=Guid.NewGuid(), UserName=Guid.NewGuid().ToString(), PasswordHash="fixture", DisplayName="SQL fixture actor", RoleCode=UserRoleCode.ProjectManager, CreatedAt=now };
        db.AddRange(project, road, route, type, defect, actor); await db.SaveChangesAsync();
        var membership=ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(),project.Id,actor.Id,DateOnly.FromDateTime(now.UtcDateTime));
        db.Add(membership); await db.SaveChangesAsync();
        var obligation = RepairObligation.Create(Guid.NewGuid(), project.Id, defect.Id, RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(),road.Id,"source-frame-v1","R",1,2,0,1));
        var package = RepairPackage.Create(Guid.NewGuid(),project.Id,defect.Id,[obligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(),obligation,RepairMode.FastTrack,actor.Id,UserRoleCode.ProjectManager,now,new("actual repair plan","checklist-v1"));
        package.AddItem(item); db.Add(package); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var original=Guid.NewGuid();
        // These raw immutable facts test database invariants, not production command authorization.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairDecisions (Id,ItemId,ObligationId,DefectId,Mode,ActorId,Role,Reason,At,Result) VALUES ({original},{item.Id},{obligation.Id},{defect.Id},2,{actor.Id},2,'fixture acceptance',{now},3)");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET EffectiveDecisionId={original},State=7 WHERE Id={item.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET EffectiveResolutionHeadDecisionId={original},EffectiveResolutionDecisionId={original} WHERE Id={obligation.Id}");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairObligationResolutionEvents (DecisionId,Accepted,At,ObligationId) VALUES ({original},1,{now},{obligation.Id})");
        var reader=new CurrentRepairFactsReader(new CurrentRepairFactsRepository(db,TimeProvider.System));
        var beforeFacts=await reader.CaptureAsync(actor.Id,project.Id,new(),default);
        Assert.Empty(beforeFacts.MissingReasons); Assert.Equal("CONFIRMED",Assert.Single(beforeFacts.Items).Presentation);
        var reporting=new ReportingService(new ReportingRepository(db),new IdentityRepository(db),
            new ProjectScopeGuard(new ProjectMembershipReadModel(db),TimeProvider.System),TimeProvider.System,currentRepairReaders:[reader]);
        var beforeCapture=(await reporting.CaptureAsync(actor.Id,project.Id,new(),default)).Value!;
        Assert.Equal("CONFIRMED",Assert.Single(beforeCapture.Summary.Metrics.Where(metric=>metric.Code=="repairItemsByStatus")).Dimensions.Status);
        // Admission and immutable SQL snapshots are real. Rendering/storage/worker delivery are not exercised here.
        var exports=new ExportService(new ExportRepository(db,new IdempotencyOperationService(db),TimeProvider.System),
            new IdentityRepository(db),new ProjectScopeGuard(new ProjectMembershipReadModel(db),TimeProvider.System),
            reporting,[],[],null!,null!,null!,TimeProvider.System,null!);
        var oldExport=await exports.CreateAsync(actor.Id,project.Id,new("DOSSIER","ZIP"),"before-correction",null,default);
        Assert.Equal("success",oldExport.Code);
        var oldSnapshot=await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row=>row.Id==oldExport.Value!.SnapshotId);
        var originalSnapshotJson=oldSnapshot.PayloadJson;
        var correction=Guid.NewGuid(); var correctedAt=now.AddMinutes(1);
        async Task AppendCorrection()
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairDecisions (Id,ItemId,ObligationId,DefectId,Mode,ActorId,Role,Reason,At,SupersedesDecisionId,Result,Basis_Text) VALUES ({correction},{item.Id},{obligation.Id},{defect.Id},2,{actor.Id},2,'mistaken acceptance',{correctedAt},{original},1,'independent inspection basis')");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET EffectiveDecisionId={correction},State=9 WHERE Id={item.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairObligations SET EffectiveResolutionHeadDecisionId={correction},EffectiveResolutionDecisionId=NULL WHERE Id={obligation.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RepairObligationResolutionEvents (DecisionId,Accepted,SupersedesDecisionId,At,ObligationId) VALUES ({correction},0,{original},{correctedAt},{obligation.Id})");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairPackages SET MutationRevision=MutationRevision+1 WHERE Id={package.Id}");
        }
        await using (var transaction=await db.Database.BeginTransactionAsync()) { await AppendCorrection(); await transaction.RollbackAsync(); }
        Assert.False(await db.Set<RepairDecision>().AnyAsync(decision => decision.Id==correction));
        Assert.Equal(original,await db.Set<RepairObligation>().Where(row=>row.Id==obligation.Id).Select(row=>row.EffectiveResolutionDecisionId).SingleAsync());
        await using (var transaction=await db.Database.BeginTransactionAsync()) { await AppendCorrection(); await transaction.CommitAsync(); }
        var hydrated=await db.Set<RepairPackage>().AsNoTracking().Include(row=>row.Obligations).ThenInclude(row=>row.ResolutionHistory).Include(row=>row.Items).ThenInclude(row=>row.Decisions).SingleAsync(row=>row.Id==package.Id);
        Assert.False(hydrated.IsComplete); Assert.Equal(correction,Assert.Single(hydrated.Items).EffectiveDecisionId);
        Assert.Equal(2,Assert.Single(hydrated.Obligations).ResolutionHistory.Count); Assert.Null(Assert.Single(hydrated.Obligations).EffectiveResolutionDecisionId);
        Assert.Equal(RepairPresentationState.Confirmed,await db.Set<RepairDecision>().Where(row=>row.Id==original).Select(row=>row.Result).SingleAsync());
        var rewrite=await Assert.ThrowsAsync<SqlException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairDecisions SET Reason='rewrite' WHERE Id={original}")); Assert.Equal(51200,rewrite.Number);
        var stale=await Assert.ThrowsAsync<SqlException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE RepairItems SET EffectiveDecisionId={original},State=7 WHERE Id={item.Id}")); Assert.Equal(51217,stale.Number);
        var downgrade=await Assert.ThrowsAsync<SqlException>(()=>db.GetService<IMigrator>().MigrateAsync("20261006035334_H3FieldLifecycleAndIntake")); Assert.Equal(51290,downgrade.Number);
        Assert.True(await db.Set<RepairDecision>().AnyAsync(row=>row.Id==correction));
        // Empty later migrations can legitimately complete Down before populated core refuses.
        // Restore the current schema before exercising today's reader and export consumers.
        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();
        Assert.True(await db.Set<RepairDecision>().AnyAsync(row=>row.Id==correction));
        var lateFile=StoredFile.Create(Guid.NewGuid(),"controlled/late-evidence","fixture.jpg","image/jpeg",4,new string('a',64),actor.Id,now,null);
        db.Add(lateFile);await db.SaveChangesAsync();
        // A valid file FK cannot authorize additions to an already published original decision.
        var late=await Assert.ThrowsAsync<SqlException>(()=>db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO RepairCorrectionEvidence (FileId,DecisionId,FileVersion,Hash,Purpose,Verified,Related,SourceKind,SourceId,ReusedSource) VALUES ({lateFile.Id},{original},'controlled-version',{new string('a',64)},1,1,1,'CONTROLLED',{Guid.NewGuid()},0)"));
        Assert.Equal(51208,late.Number);
        var afterFacts=await reader.CaptureAsync(actor.Id,project.Id,new(),default);
        Assert.Empty(afterFacts.MissingReasons); Assert.Equal("UNREPAIRED",Assert.Single(afterFacts.Items).Presentation);
        Assert.Equal(correction,Assert.Single(afterFacts.Items).DecisionId);
        Assert.Equal("CONFIRMED",Assert.Single(beforeFacts.Items).Presentation);
        var afterCapture=(await reporting.CaptureAsync(actor.Id,project.Id,new(),default)).Value!;
        Assert.Equal("UNREPAIRED",Assert.Single(afterCapture.Summary.Metrics.Where(metric=>metric.Code=="repairItemsByStatus")).Dimensions.Status);
        Assert.Equal("CONFIRMED",Assert.Single(beforeCapture.Summary.Metrics.Where(metric=>metric.Code=="repairItemsByStatus")).Dimensions.Status);
        var newExport=await exports.CreateAsync(actor.Id,project.Id,new("DOSSIER","ZIP"),"after-correction",null,default);
        Assert.Equal("success",newExport.Code);
        var newSnapshot=await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row=>row.Id==newExport.Value!.SnapshotId);
        var newPayload=ExportSerialization.Read(newSnapshot);
        Assert.Equal("UNREPAIRED",Assert.Single(newPayload.Dossier!.Summary.Metrics.Where(metric=>metric.Code=="repairItemsByStatus")).Dimensions.Status);
        Assert.Contains(newPayload.Manifest.SourceRevisions,revision=>revision.Id==correction && revision.Kind=="RepairDecision");
        Assert.Equal(originalSnapshotJson,(await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row=>row.Id==oldSnapshot.Id)).PayloadJson);
        Assert.Equal(oldSnapshot.Hash,(await db.Set<ExportSnapshot>().AsNoTracking().SingleAsync(row=>row.Id==oldSnapshot.Id)).Hash);
        // Immutable snapshots retain old decisions, but they never retain an actor's revoked source permission.
        if(!await db.Roles.AnyAsync(role=>role.Code==UserRoleCode.Supervisor)) db.Add(new ApplicationRole(UserRoleCode.Supervisor,"Supervisor"));
        var supervisor=new ApplicationUser {Id=Guid.NewGuid(),UserName=Guid.NewGuid().ToString(),PasswordHash="fixture",DisplayName="private snapshot reader",RoleCode=UserRoleCode.Supervisor,CreatedAt=now};
        db.Add(supervisor);await db.SaveChangesAsync();
        Assert.Equal("forbidden",(await exports.GetAsync(supervisor.Id,project.Id,oldExport.Value!.Id,default)).Code);
        Assert.Equal("forbidden",(await exports.ManifestAsync(supervisor.Id,project.Id,oldExport.Value.Id,default)).Code);
        Assert.Equal("forbidden",(await exports.ContentAsync(supervisor.Id,project.Id,oldExport.Value.Id,default)).Code);
        var supervisorMember=new ProjectMember {Id=Guid.NewGuid(),ProjectId=project.Id,UserId=supervisor.Id,RoleCode=UserRoleCode.Supervisor,Status=ProjectMemberStatus.Active,ValidFrom=DateOnly.FromDateTime(now.UtcDateTime)};
        db.Add(supervisorMember);await db.SaveChangesAsync();
        Assert.Equal("success",(await exports.GetAsync(supervisor.Id,project.Id,oldExport.Value.Id,default)).Code);
        var retained=(await exports.ManifestAsync(supervisor.Id,project.Id,oldExport.Value.Id,default)).Value!;
        Assert.Contains(retained.SourceRevisions,revision=>revision.Kind=="RepairDecision" && revision.Id==original);
        Assert.Equal("success",(await exports.CreateAsync(supervisor.Id,project.Id,new("DOSSIER","ZIP"),"private-supervisor-snapshot",null,default)).Code);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status={(byte)ProjectMemberStatus.Ended} WHERE Id={supervisorMember.Id}");
        Assert.Equal("forbidden",(await exports.ManifestAsync(supervisor.Id,project.Id,oldExport.Value.Id,default)).Code);
        Assert.Equal("forbidden",(await exports.CreateAsync(supervisor.Id,project.Id,new("DOSSIER","ZIP"),"private-supervisor-snapshot",null,default)).Code);
        Assert.Equal("forbidden",(await exports.CreateAsync(supervisor.Id,project.Id,new("DOSSIER","PDF"),"private-supervisor-snapshot",null,default)).Code);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status={(byte)ProjectMemberStatus.Ended} WHERE Id={membership.Id}");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>reader.CaptureAsync(actor.Id,project.Id,new(),default));
    }

    [Fact]
    public async Task FreshCoreGuardsAndEmptyDowngradeRetainPopulatedH3Project()
    {
        await using var db = Db();
        const string baseline = "20261006035334_H3FieldLifecycleAndIntake";
        var target = Assert.Single(db.Database.GetMigrations().Where(name => name.EndsWith("_H4RepairCore", StringComparison.Ordinal)));
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(baseline);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "Populated H3 migration fixture", null, null, null, null, DateTimeOffset.UtcNow);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var history = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        await migrator.MigrateAsync(target);
        Assert.Equal(history.Append(target), await db.Database.GetAppliedMigrationsAsync());
        var guards = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE name LIKE 'TR_Repair%' AND is_disabled=0").SingleAsync();
        Assert.Equal(19, guards);
        Assert.False(db.Database.HasPendingModelChanges());
        await migrator.MigrateAsync(baseline);
        Assert.Equal(project.Name, await db.Projects.Where(row => row.Id == project.Id).Select(row => row.Name).SingleAsync());
        await migrator.MigrateAsync(target);
        Assert.Equal(19, await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.triggers WHERE name LIKE 'TR_Repair%' AND is_disabled=0").SingleAsync());
    }
}
