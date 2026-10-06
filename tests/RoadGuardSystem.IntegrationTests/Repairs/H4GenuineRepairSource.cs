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

internal static class H4GenuineRepairSource
{
    internal static async Task<Source> Seed(RoadGuardDbContext db, IdentitySqlServerFixture sql)
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
    internal sealed record Source(Guid Pm, Guid Crew, Guid Supervisor, Guid Project, Guid Road, Guid Route, Guid Set, Guid Defect);
}
