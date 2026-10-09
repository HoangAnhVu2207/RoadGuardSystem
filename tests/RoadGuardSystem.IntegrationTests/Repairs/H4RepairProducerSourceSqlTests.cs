using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Repairs;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Implementations.Defects;
using RoadGuardSystem.Repositories.Models.Huy01;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Repairs;

// Native source integration: genuine Reporter lineage; it does not assert survey accuracy or FT eligibility.
public sealed class H4RepairProducerSourceSqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly System.Text.Json.JsonSerializerOptions SourceJson = new(System.Text.Json.JsonSerializerDefaults.Web);
    [Theory]
    [InlineData(RepairMode.Normal, "NORMAL")]
    [InlineData(RepairMode.FastTrack, "CONDITIONAL_FT")]
    public async Task GenuineReporterDefectCreatesNativeRepairTaskWithoutSurveyOrMeasureOnlyRelabel(RepairMode mode, string expected)
    {
        await using var db = sql.CreateDbContext(); var scope = await Seed(db);
        var produced = await NativeTask(db, scope, mode);
        db.ChangeTracker.Clear(); var task = await db.Set<FieldInspectionTask>().SingleAsync(row => row.Id == produced.Task.Id);
        Assert.Equal(expected, task.TaskMode); Assert.Equal(produced.Item.Id, task.RepairItemId);
        Assert.Equal("REPORTER", task.SourceKind); Assert.Null(task.SurveyId);
        Assert.Equal(scope.Defect, task.DefectId); Assert.Equal(FieldInspectionPurpose.PostRepair, task.Purpose);
        Assert.False(await db.Set<RepairExecutionStart>().AnyAsync(row => row.ItemId == produced.Item.Id));
        Assert.False(await db.Set<RepairMeasurementAssessment>().AnyAsync(row => row.ItemId == produced.Item.Id));
    }

    [Theory]
    [InlineData(RepairMode.Normal, "NORMAL")]
    [InlineData(RepairMode.FastTrack, "CONDITIONAL_FT")]
    public async Task VerifiedReporterDefectAllowsOnlyBoundPostRepairTaskModes(RepairMode mode, string expected)
    {
        await using var db = sql.CreateDbContext(); var scope = await Seed(db);
        var defect = await db.Defects.SingleAsync(row => row.Id == scope.Defect);
        db.Entry(defect).Property(row => row.Status).CurrentValue = DefectStatus.Verified;
        await db.SaveChangesAsync();

        var produced = await NativeTask(db, scope, mode);

        db.ChangeTracker.Clear();
        var task = await db.FieldInspectionTasks.SingleAsync(row => row.Id == produced.Task.Id);
        Assert.Equal(expected, task.TaskMode);
        Assert.Equal(FieldInspectionPurpose.PostRepair, task.Purpose);
        Assert.Equal(produced.Item.Id, task.RepairItemId);
    }

    [Fact]
    public async Task VerifiedReporterDefectStillRejectsUnboundMeasureOnlyTask()
    {
        await using var db = sql.CreateDbContext(); var scope = await Seed(db);
        var defect = await db.Defects.SingleAsync(row => row.Id == scope.Defect);
        db.Entry(defect).Property(row => row.Status).CurrentValue = DefectStatus.Verified;
        await db.SaveChangesAsync();
        var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), "verified-unbound-" + Guid.NewGuid().ToString("N"),
            scope.Project, scope.Defect, null, "REPORTER", scope.Route, scope.Set, null, null,
            FieldInspectionPurpose.PostRepair, 1, "{}", null, DateTimeOffset.UtcNow.AddDays(1), scope.Pm);
        db.Add(task);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(51133, Assert.IsType<Microsoft.Data.SqlClient.SqlException>(exception.InnerException).Number);
    }

    [Fact]
    public async Task VerifiedReporterDefectStillRejectsBoundRepairTaskWithNonPostRepairPurpose()
    {
        await using var db = sql.CreateDbContext(); var scope = await Seed(db);
        var defect = await db.Defects.SingleAsync(row => row.Id == scope.Defect);
        db.Entry(defect).Property(row => row.Status).CurrentValue = DefectStatus.Verified;
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            NativeTask(db, scope, RepairMode.Normal, FieldInspectionPurpose.Verification));

        Assert.Equal(51133, Assert.IsType<Microsoft.Data.SqlClient.SqlException>(exception.InnerException).Number);
    }

    [Fact]
    public async Task VerifiedReporterDefectRejectsTaskPinnedToAnotherDefectsRepairItem()
    {
        await using var db = sql.CreateDbContext(); var scope = await Seed(db);
        var defect = await db.Defects.SingleAsync(row => row.Id == scope.Defect);
        db.Entry(defect).Property(row => row.Status).CurrentValue = DefectStatus.Verified;
        await db.SaveChangesAsync();
        var valid = await NativeTask(db, scope, RepairMode.Normal);

        var otherDefect = Defect.Create(Guid.NewGuid(), scope.Project, scope.Route, null,
            defect.DefectTypeCode, null, DefectSeverity.Low, DefectStatus.Open,
            new GeometryFactory(new PrecisionModel(), 32648).CreatePoint(new Coordinate(1, 0)), DateTimeOffset.UtcNow);
        db.Add(otherDefect);
        await db.SaveChangesAsync();
        var now = DateTimeOffset.UtcNow;
        var otherObligation = RepairObligation.Create(Guid.NewGuid(), scope.Project, otherDefect.Id,
            RepairObligationKind.FormalRepair, true,
            RepairActualScope.Create(Guid.NewGuid(), scope.Road, "source-route:" + scope.Route, "actual road", 1, 2, 0, 1));
        var otherPackage = RepairPackage.Create(Guid.NewGuid(), scope.Project, otherDefect.Id, [otherObligation]);
        var otherItem = RepairItem.ProposeWithPlan(Guid.NewGuid(), otherObligation, RepairMode.Normal,
            scope.Pm, UserRoleCode.ProjectManager, now, new("actual proposed plan", "checklist-v1"));
        otherItem.Approve(scope.Supervisor, UserRoleCode.Supervisor, now);
        otherItem.Assign(scope.Crew, scope.Pm, UserRoleCode.ProjectManager, now);
        otherPackage.AddItem(otherItem);
        db.Add(otherPackage);
        await db.SaveChangesAsync();

        var forged = FieldInspectionTask.CreateRepair(Guid.NewGuid(), "wrong-defect-" + Guid.NewGuid().ToString("N"),
            valid.Item, null, "REPORTER", scope.Route, scope.Set, null, null,
            FieldInspectionPurpose.PostRepair, 1, "{}", null, now.AddDays(1), scope.Pm);
        db.Add(forged);
        db.Entry(forged).Property(row => row.RepairItemId).CurrentValue = otherItem.Id;

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(51321, Assert.IsType<Microsoft.Data.SqlClient.SqlException>(exception.InnerException).Number);
    }

    [Fact]
    public async Task ActualNormalBindingAllowsExplicitPreExecutionSessionWithoutFormalIntakeOrCompletedRepair()
    {
        await using var db = sql.CreateDbContext(); var scope = await Seed(db);
        var produced = await NativeTask(db, scope, RepairMode.Normal);
        var now = DateTimeOffset.UtcNow;
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), produced.Task.Id, scope.Crew, scope.Pm,
            now, null, FieldInspectionAssignmentStatus.Active, null);
        db.Add(assignment); await db.SaveChangesAsync();
        var binding = new RepairFieldTaskBinding(Guid.NewGuid(), scope.Project, scope.Defect, produced.Item.ObligationId,
            produced.Item.Id, produced.Task.Id, assignment.Id, scope.Crew, RepairMode.Normal, null, null, null,
            produced.Item.ProposalPlanHash!, produced.Item.ChecklistVersion!, scope.Route, scope.Set, null, null, null, null,
            "source-route:" + scope.Route, scope.Pm, now, "actual initial normal assignment",
            Convert.ToBase64String(produced.Task.RowVersion), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
                {
                    assignment.Id,
                    assignment.FieldInspectionTaskId,
                    assignment.AssignedToUserId,
                    assignment.AssignedByUserId,
                    assignment.AssignedAt
                },
                    SourceJson))).ToLowerInvariant());
        db.Add(binding); await db.SaveChangesAsync();
        db.Entry(produced.Item).Property(row => row.CurrentBindingId).CurrentValue = binding.Id;
        produced.Task.Transition(FieldInspectionTaskStatus.Accepted); await db.SaveChangesAsync();
        var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.PreMeasurement, produced.Task.Id,
            scope.Project, scope.Route, null, "pre-execution", scope.Crew, "actual assigned Crew", now,
            null, "actual preliminary capture", FieldInspectionSessionStatus.Completed, null);
        db.Add(session); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Assert.Equal(FieldInspectionPurpose.PreMeasurement,
            (await db.Set<FieldInspectionSession>().SingleAsync(row => row.Id == session.Id)).Purpose);
        Assert.Equal(FieldInspectionPurpose.PostRepair,
            (await db.Set<FieldInspectionTask>().SingleAsync(row => row.Id == produced.Task.Id)).Purpose);
        Assert.False(await db.Set<FieldInspectionSubmission>().AnyAsync(row => row.TaskId == produced.Task.Id));
    }

    private static async Task<(RepairItem Item, FieldInspectionTask Task)> NativeTask(RoadGuardDbContext db, Source scope,
        RepairMode mode, FieldInspectionPurpose purpose = FieldInspectionPurpose.PostRepair)
    {
        var now = DateTimeOffset.UtcNow;
        var obligation = RepairObligation.Create(Guid.NewGuid(), scope.Project, scope.Defect, RepairObligationKind.FormalRepair,
            true, RepairActualScope.Create(Guid.NewGuid(), scope.Road, "source-route:" + scope.Route, "actual road", 1, 2, 0, 1));
        var package = RepairPackage.Create(Guid.NewGuid(), scope.Project, scope.Defect, [obligation]);
        var item = RepairItem.ProposeWithPlan(Guid.NewGuid(), obligation, mode, scope.Pm, UserRoleCode.ProjectManager,
            now, new("actual proposed plan", "checklist-v1"));
        if (mode == RepairMode.Normal) item.Approve(scope.Supervisor, UserRoleCode.Supervisor, now);
        item.Assign(scope.Crew, scope.Pm, UserRoleCode.ProjectManager, now); package.AddItem(item);
        db.Add(package); await db.SaveChangesAsync();
        var task = FieldInspectionTask.CreateRepair(Guid.NewGuid(), "repair-" + Guid.NewGuid().ToString("N"), item,
            null, "REPORTER", scope.Route, scope.Set, null, null, purpose, 1, "{}", null,
            now.AddDays(1), scope.Pm);
        db.Add(task); await db.SaveChangesAsync(); return (item, task);
    }

    private async Task<Source> Seed(RoadGuardDbContext db)
    {
        await db.Database.MigrateAsync(); await sql.SeedRolesAsync(db); var now = DateTimeOffset.UtcNow;
        ApplicationUser User(UserRoleCode role)
        {
            var name = Guid.NewGuid().ToString(); return new()
            {
                Id = Guid.NewGuid(),
                UserName = name,
                NormalizedUserName = name.ToUpperInvariant(),
                DisplayName = "H4 genuine source fixture",
                PasswordHash = "fixture",
                RoleCode = role,
                Status = UserStatus.Active,
                CreatedAt = now
            };
        }
        var pm = User(UserRoleCode.ProjectManager); var crew = User(UserRoleCode.RepairCrew);
        var supervisor = User(UserRoleCode.Supervisor); var reporter = User(UserRoleCode.Reporter);
        var project = Project.Create(Guid.NewGuid(), Guid.NewGuid().ToString(), "H4 genuine source", null, null, null, null, now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, "actual road");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new(0, 0), new(20, 0)]);
        var route = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now,
            "controlled coordinate fixture; no accuracy or eligibility assertion");
        var set = RoadSegmentSet.Create(Guid.NewGuid(), route.Id);
        var segment = RoadSegment.Create(Guid.NewGuid(), set.Id, route.Id, 1); segment.SetGeometry(0, 20, 0, geometry);
        var type = DefectType.Create("H4" + Guid.NewGuid().ToString("N"), "H4 genuine Reporter source");
        db.AddRange(pm, crew, supervisor, reporter, project, road, route, set, segment, type,
            ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, pm.Id, new(2000, 1, 1)),
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = crew.Id,
                RoleCode = UserRoleCode.RepairCrew,
                ValidFrom = new(2000, 1, 1),
                Status = ProjectMemberStatus.Active
            },
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = supervisor.Id,
                RoleCode = UserRoleCode.Supervisor,
                ValidFrom = new(2000, 1, 1),
                Status = ProjectMemberStatus.Active
            });
        await db.SaveChangesAsync();
        var file = StoredFile.Create(Guid.NewGuid(), "private/h4-reporter-" + Guid.NewGuid().ToString("N"), "source.jpg",
            "image/jpeg", 4, new string('b', 64), reporter.Id, now, null);
        db.AddRange(file, FileScope.CreatePrivate(Guid.NewGuid(), file.Id, reporter.Id, now)); await db.SaveChangesAsync();
        var upload = UploadSession.Create(Guid.NewGuid(), file.Id, reporter.Id, file.StorageUri, "REPORT_PHOTO", "image/jpeg", 4,
            new string('b', 64), 8388608, now.AddHours(24)); upload.StartUploading("fixture", now);
        db.Add(upload); await db.SaveChangesAsync(); upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now);
        await db.SaveChangesAsync(); upload.MarkVerified(); await db.SaveChangesAsync();
        var report = Report.Create(Guid.NewGuid(), reporter.Id, "genuine Reporter without Survey", now,
            [VerifiedEvidenceReference.Create(Guid.NewGuid(), file.Id, Convert.ToBase64String(upload.RowVersion), reporter.Id)]);
        var incident = IncidentCase.CreateUnassigned(Guid.NewGuid(), report.Id, now);
        incident.Triage(project.Id, CaseVerificationMethod.ExistingEvidence, "actual retained source", now);
        db.AddRange(report, incident); db.Set<HuyCaseReportLink>().Add(new()
        {
            Id = Guid.NewGuid(),
            CaseId = incident.Id,
            ReportId = report.Id,
            StartedAt = now
        }); await db.SaveChangesAsync();
        var source = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, report.Id,
            "fixture-source"), project.Id, "fixture-geometry");
        var accepted = await new CandidateDecisionRepository(db).SaveAcceptedAsync(pm.Id, source, CandidateDecisionKind.KeepNew,
            CandidateClassification.Create(route.Id, type.Code, null, DefectSeverity.Low, null), null, null, null,
            "genuine keep-new source", null, default);
        var defect = await db.Defects.SingleAsync(row => row.Id == accepted.DefectId);
        return new(pm.Id, crew.Id, supervisor.Id, project.Id, road.Id, route.Id, set.Id, defect.Id);
    }
    private sealed record Source(Guid Pm, Guid Crew, Guid Supervisor, Guid Project, Guid Road, Guid Route, Guid Set, Guid Defect);
}
