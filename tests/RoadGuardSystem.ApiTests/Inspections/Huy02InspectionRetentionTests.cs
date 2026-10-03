using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Implementations.Retention;
using RoadGuardSystem.Repositories.Retention;
using Xunit;

namespace RoadGuardSystem.ApiTests.Inspections;

[Collection(AuthenticationApiFixture.Name)]
public sealed class Huy02InspectionRetentionTests(AuthenticationSqlServerFixture fixture)
{
    [Fact]
    public async Task SessionAndMeasurementReferencesWithoutFileScopeReachRealComposite()
    {
        var actor = await fixture.CreateUserAsync($"huy02_ret_{Guid.NewGuid():N}", "Current1!");
        await using var db = fixture.CreateDbContext();
        var file = AddFile(db, actor.Id);
        var first = AddScope(db, file.Id, FieldInspectionSessionStatus.Completed);
        db.FieldInspectionSessions.Add(FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation,
            null, first.Project.Id, first.Session.RoadSectionVersionId, null, $"S-{Guid.NewGuid():N}", null,
            "Researcher", first.Session.ConductedAt, null, "Gauge", FieldInspectionSessionStatus.Completed, file.Id));
        var second = AddScope(db, null, FieldInspectionSessionStatus.Imported);
        var measurement = AddMeasurement(db, second.Session, file.Id);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var contributor = Contributor(db);
        var result = await contributor.ReadAsync(file.Id, default);
        Assert.True(result.Complete);
        Assert.Equal("HUY02_INSPECTION", result.Name);
        Assert.Contains(result.References, x => x.Kind == "FIELD_INSPECTION_SESSION" && x.Id == first.Session.Id && x.ProjectId == first.Project.Id);
        Assert.Contains(result.References, x => x.Kind == "GROUND_TRUTH_MEASUREMENT" && x.Id == measurement.Id && x.ProjectId == second.Project.Id);
        Assert.Equal(result.References, (await contributor.ReadAsync(file.Id, default)).References);
        Assert.Equal(new[] { file.Id }, await contributor.KnownProjectFilesAsync(first.Project.Id, default));
        Assert.Equal(new[] { file.Id }, await contributor.KnownProjectFilesAsync(second.Project.Id, default));
        Assert.Empty(await contributor.KnownProjectFilesAsync(Guid.NewGuid(), default));
        Assert.Empty(db.ChangeTracker.Entries());
        var inventory = await new RetentionInventoryRepository(db,
            [new Huy01RetentionInventoryContributor(db), contributor]).ReadAsync(file.Id, default);
        Assert.NotNull(inventory);
        Assert.False(inventory.Complete);
        Assert.Contains("HUY_REPAIR_REFERENCE_UNAVAILABLE", inventory.ReasonCodes);
        Assert.Contains(inventory.References, x => x.ProjectId == first.Project.Id);
        Assert.Contains(inventory.References, x => x.ProjectId == second.Project.Id);
    }

    [Fact]
    public async Task DefectVerificationProvenanceAndEndedAssignmentRetainHistoricalEvidence()
    {
        var crew = await fixture.CreateUserAsync($"h02_retcrew_{Guid.NewGuid():N}", "Current1!", UserRoleCode.RepairCrew);
        var pm = await fixture.CreateUserAsync($"h02_retpm_{Guid.NewGuid():N}", "Current1!", UserRoleCode.ProjectManager);
        await using var db = fixture.CreateDbContext();
        var file = AddFile(db, crew.Id);
        var scope = AddScope(db, null, FieldInspectionSessionStatus.Draft);
        db.Entry(scope.Session).State = EntityState.Detached;
        var now = scope.Session.ConductedAt;
        var versionId = scope.Session.RoadSectionVersionId;
        var survey = Survey.Create(Guid.NewGuid(), null, scope.Project.Id, versionId, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var type = DefectType.Create($"H02-DT-{Guid.NewGuid():N}", "Test defect");
        var cause = CauseCategory.Create($"H02-CC-{Guid.NewGuid():N}", "Test cause");
        var defect = Defect.Create(Guid.NewGuid(), scope.Project.Id, versionId, null, type.Code, cause.Code,
            DefectSeverity.Medium, DefectStatus.Open,
            new GeometryFactory(new PrecisionModel(), 32648).CreatePoint(new Coordinate(500050, 1100050)), now);
        var task = FieldInspectionTask.Create(Guid.NewGuid(), $"T-{Guid.NewGuid():N}", scope.Project.Id,
            defect.Id, survey.Id, versionId, 1, "{}", null, null, now.AddDays(1), FieldInspectionTaskStatus.Accepted,
            pm.Id, null, null, null, null);
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, crew.Id, pm.Id, now,
            null, FieldInspectionAssignmentStatus.Active, null);
        var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.DefectVerification,
            task.Id, scope.Project.Id, versionId, survey.Id, $"S-{Guid.NewGuid():N}", crew.Id,
            "Crew", now, null, "Gauge", FieldInspectionSessionStatus.Locked, file.Id);
        var measurement = GroundTruthMeasurement.Create(Guid.NewGuid(), session.Id, "sample", versionId,
            survey.Id, defect.Id, MeasurementType.DepressionDepth, 2, "mm",
            new GeometryFactory(new PrecisionModel(), 4326).CreatePoint(new Coordinate(106.7, 10.8)),
            "Gauge", null, "Depth", "Crew", now, file.Id, null);
        db.AddRange(survey, type, cause, defect, task, assignment, session, measurement);
        await db.SaveChangesAsync();
        var reader = Contributor(db);
        var initial = await reader.ReadAsync(file.Id, default);
        Assert.True(initial.Complete);
        Assert.Equal(2, initial.References.Count);
        db.Entry(assignment).Property(x => x.Status).CurrentValue = FieldInspectionAssignmentStatus.Ended;
        db.Entry(assignment).Property(x => x.EndedAt).CurrentValue = now.AddHours(1);
        db.Entry(assignment).Property(x => x.Reason).CurrentValue = "Historical assignment";
        await db.SaveChangesAsync();
        Assert.Equal(initial.References, (await reader.ReadAsync(file.Id, default)).References);
        Assert.Equal(new[] { file.Id }, await reader.KnownProjectFilesAsync(scope.Project.Id, default));
    }

    [Theory]
    [InlineData(FieldInspectionSessionStatus.Draft)]
    [InlineData(FieldInspectionSessionStatus.Completed)]
    [InlineData(FieldInspectionSessionStatus.Imported)]
    [InlineData(FieldInspectionSessionStatus.Locked)]
    public async Task ResearchAndHistoricalRowsRemainReferences(FieldInspectionSessionStatus status)
    {
        var actor = await fixture.CreateUserAsync($"huy02_hist_{Guid.NewGuid():N}", "Current1!");
        await using var db = fixture.CreateDbContext();
        var file = AddFile(db, actor.Id);
        var scope = AddScope(db, file.Id, status);
        AddMeasurement(db, scope.Session, file.Id);
        await db.SaveChangesAsync();
        var result = await Contributor(db).ReadAsync(file.Id, default);
        Assert.True(result.Complete);
        Assert.Equal(2, result.References.Count);
        Assert.All(result.References, x => Assert.Equal(scope.Project.Id, x.ProjectId));
    }

    [Fact]
    public async Task DraftChangeAndAdditiveReferenceChangeDeterministicVersion()
    {
        var actor = await fixture.CreateUserAsync($"huy02_draft_{Guid.NewGuid():N}", "Current1!");
        await using var db = fixture.CreateDbContext();
        var file = AddFile(db, actor.Id);
        var scope = AddScope(db, file.Id, FieldInspectionSessionStatus.Draft);
        await db.SaveChangesAsync();
        var reader = Contributor(db);
        var first = await reader.ReadAsync(file.Id, default);
        db.Entry(scope.Session).Property(x => x.ConductedAt).CurrentValue = scope.Session.ConductedAt.AddHours(1);
        await db.SaveChangesAsync();
        var changed = await reader.ReadAsync(file.Id, default);
        Assert.NotEqual(first.References.Single().SourceVersion, changed.References.Single().SourceVersion);
        var composite = new RetentionInventoryRepository(db, [new Huy01RetentionInventoryContributor(db), reader]);
        var priorVersion = (await composite.ReadAsync(file.Id, default))!.Version;
        AddMeasurement(db, scope.Session, file.Id);
        await db.SaveChangesAsync();
        Assert.Equal(2, (await reader.ReadAsync(file.Id, default)).References.Count);
        Assert.NotEqual(priorVersion, (await composite.ReadAsync(file.Id, default))!.Version);
        Assert.Equal((await reader.ReadAsync(file.Id, default)).References, (await reader.ReadAsync(file.Id, default)).References);
    }

    [Fact]
    public async Task CallerTransactionSeesAddedReferencesAndRollbackRemovesThem()
    {
        var actor = await fixture.CreateUserAsync($"huy02_tx_{Guid.NewGuid():N}", "Current1!");
        Guid fileId;
        Guid sessionId;
        await using (var db = fixture.CreateDbContext())
        {
            var file = AddFile(db, actor.Id);
            var scope = AddScope(db, null, FieldInspectionSessionStatus.Draft);
            await db.SaveChangesAsync();
            fileId = file.Id;
            sessionId = scope.Session.Id;
        }
        await using (var db = fixture.CreateDbContext())
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var session = await db.FieldInspectionSessions.SingleAsync(x => x.Id == sessionId);
            AddMeasurement(db, session, fileId);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var composite = new RetentionInventoryRepository(db, [new Huy01RetentionInventoryContributor(db), Contributor(db)]);
            Assert.Contains((await composite.ReadAsync(fileId, default))!.References, x => x.Kind == "GROUND_TRUTH_MEASUREMENT");
            Assert.Empty(db.ChangeTracker.Entries());
            await transaction.RollbackAsync();
        }
        await using var verify = fixture.CreateDbContext();
        Assert.Empty((await Contributor(verify).ReadAsync(fileId, default)).References);
        Assert.False(verify.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task CrossProjectRoadProvenanceFailsClosedRatherThanEmptyComplete()
    {
        var actor = await fixture.CreateUserAsync($"huy02_prov_{Guid.NewGuid():N}", "Current1!");
        await using var db = fixture.CreateDbContext();
        var file = AddFile(db, actor.Id);
        var first = AddScope(db, file.Id, FieldInspectionSessionStatus.Draft);
        var second = AddScope(db, null, FieldInspectionSessionStatus.Draft);
        db.Entry(first.Session).Property(x => x.RoadSectionVersionId).CurrentValue = second.Session.RoadSectionVersionId;
        await db.SaveChangesAsync();
        var result = await Contributor(db).ReadAsync(file.Id, default);
        Assert.False(result.Complete);
        Assert.NotEmpty(result.ReasonCodes);
        Assert.NotEmpty(result.References);
        var empty = await Contributor(db).ReadAsync(Guid.NewGuid(), default);
        Assert.True(empty.Complete); // Covered inspection tables only.
        Assert.Empty(empty.References);
    }

    private static Huy02InspectionRetentionContributor Contributor(RoadGuardDbContext db)
        => new Huy02InspectionRetentionContributor(db);

    private static StoredFile AddFile(RoadGuardDbContext db, Guid owner)
    {
        var file = StoredFile.Create(Guid.NewGuid(), $"test/{Guid.NewGuid():N}.jpg", "test.jpg", "image/jpeg",
            4, new string('a', 64), owner, DateTimeOffset.UtcNow, null);
        db.Files.Add(file);
        return file;
    }

    private static (Project Project, FieldInspectionSession Session) AddScope(
        RoadGuardDbContext db, Guid? file, FieldInspectionSessionStatus status)
    {
        var now = DateTimeOffset.UtcNow;
        var project = Project.Create(Guid.NewGuid(), $"H02-{Guid.NewGuid():N}", "Inspection retention", null,
            32648, new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), now);
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"R-{Guid.NewGuid():N}");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648)
            .CreateLineString([new Coordinate(500000, 1100000), new Coordinate(500100, 1100100)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now, "Test");
        var session = FieldInspectionSession.Create(Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation,
            null, project.Id, version.Id, null, $"S-{Guid.NewGuid():N}", null, "Researcher", now, null,
            "depth gauge", status, file);
        db.AddRange(project, road, version, session);
        return (project, session);
    }

    private static GroundTruthMeasurement AddMeasurement(RoadGuardDbContext db, FieldInspectionSession session, Guid file)
    {
        var point = new GeometryFactory(new PrecisionModel(), 4326).CreatePoint(new Coordinate(106.7, 10.8));
        var measurement = GroundTruthMeasurement.Create(Guid.NewGuid(), session.Id, $"M-{Guid.NewGuid():N}",
            session.RoadSectionVersionId, null, null, MeasurementType.DepressionDepth, 2, "mm", point,
            "Gauge", null, "Depth", "Researcher", session.ConductedAt, file, null);
        db.GroundTruthMeasurements.Add(measurement);
        return measurement;
    }
}
