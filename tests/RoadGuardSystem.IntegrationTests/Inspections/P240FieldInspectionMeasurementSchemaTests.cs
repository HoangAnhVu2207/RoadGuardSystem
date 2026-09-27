using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Inspections;

[Trait("TaskId", "P2-40")]
public sealed class P240FieldInspectionMeasurementSchemaTests : IClassFixture<IdentitySqlServerFixture>
{
    private static readonly GeometryFactory EngineeringGeometryFactory = new(new PrecisionModel(), 32648);
    private static readonly GeometryFactory GpsGeometryFactory = new(new PrecisionModel(), 4326);
    private readonly IdentitySqlServerFixture _fixture;

    public P240FieldInspectionMeasurementSchemaTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "P2-40: product and research sessions round-trip without operational writes")]
    public async Task ProductAndResearchSessions_RoundTripWithoutDefectMutation()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);

        var productSession = FieldInspectionSession.Create(
            Guid.NewGuid(), FieldInspectionPurpose.DefectVerification, scope.TaskId, scope.ProjectId,
            scope.RoadSectionVersionId, scope.SurveyId, $"P240-P-{Guid.NewGuid():N}", scope.CrewId,
            "Repair Crew", DateTimeOffset.UtcNow, "dry", "depth gauge", FieldInspectionSessionStatus.Completed,
            scope.FileId);
        var productMeasurement = GroundTruthMeasurement.Create(
            Guid.NewGuid(), productSession.Id, "sample-product-1", scope.RoadSectionVersionId, scope.SurveyId,
            scope.DefectId, MeasurementType.DepressionDepth, 12.5m, "mm", GpsGeometryFactory.CreatePoint(new Coordinate(106.7, 10.8)),
            "depth gauge", "DG-1", "straightedge baseline", "Repair Crew", DateTimeOffset.UtcNow,
            scope.FileId, null);
        var researchSession = FieldInspectionSession.Create(
            Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, null, scope.ProjectId,
            scope.RoadSectionVersionId, null, $"P240-R-{Guid.NewGuid():N}", null, "Research Engineer",
            DateTimeOffset.UtcNow, null, "calibrated gauge", FieldInspectionSessionStatus.Imported, null);
        var researchMeasurement = GroundTruthMeasurement.Create(
            Guid.NewGuid(), researchSession.Id, "sample-research-1", scope.RoadSectionVersionId, null, null,
            MeasurementType.SlabFaultingHeight, 2.2m, "mm", GpsGeometryFactory.CreatePoint(new Coordinate(106.71, 10.81)),
            "straightedge", null, "calibrated field procedure", "Research Engineer", DateTimeOffset.UtcNow,
            null, "Imported from controlled research fixture.");

        context.AddRange(productSession, productMeasurement, researchSession, researchMeasurement);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        (await context.FieldInspectionSessions.AsNoTracking().SingleAsync(item => item.Id == productSession.Id))
            .Purpose.Should().Be(FieldInspectionPurpose.DefectVerification);
        (await context.GroundTruthMeasurements.AsNoTracking().SingleAsync(item => item.Id == productMeasurement.Id))
            .Location.SRID.Should().Be(4326);
        (await context.FieldInspectionSessions.AsNoTracking().SingleAsync(item => item.Id == researchSession.Id))
            .FieldInspectionTaskId.Should().BeNull();
        (await context.GroundTruthMeasurements.AsNoTracking().SingleAsync(item => item.Id == researchMeasurement.Id))
            .DefectId.Should().BeNull();
        (await context.Defects.AsNoTracking().SingleAsync(item => item.Id == scope.DefectId))
            .Status.Should().Be(DefectStatus.Open);
    }

    [Fact(DisplayName = "P2-40: purpose gates reject task-bearing research sessions")]
    public void ResearchValidationSession_WithTask_IsRejectedByDomainInvariant()
    {
        var create = () => FieldInspectionSession.Create(
            Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, "invalid-research", null, "Research Engineer", DateTimeOffset.UtcNow,
            null, "research method", FieldInspectionSessionStatus.Draft, null);

        create.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "P2-40: active assignment and sample identities are unique")]
    public async Task ActiveAssignmentAndSampleIdentity_DuplicatesAreRejected()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var duplicateAssignment = FieldInspectionAssignment.Create(
            Guid.NewGuid(), scope.TaskId, scope.CrewId, scope.ProjectManagerId, DateTimeOffset.UtcNow,
            null, FieldInspectionAssignmentStatus.Active, null);

        var duplicateAssignmentWrite = () =>
        {
            context.Add(duplicateAssignment);
            return context.SaveChangesAsync();
        };
        await duplicateAssignmentWrite.Should().ThrowAsync<DbUpdateException>();

        await using var secondContext = _fixture.CreateDbContext();
        var secondScope = await CreateScopeAsync(secondContext);
        var session = FieldInspectionSession.Create(
            Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, null, secondScope.ProjectId,
            secondScope.RoadSectionVersionId, null, $"P240-D-{Guid.NewGuid():N}", null, "Research Engineer",
            DateTimeOffset.UtcNow, null, "research method", FieldInspectionSessionStatus.Imported, null);
        var first = GroundTruthMeasurement.Create(
            Guid.NewGuid(), session.Id, "duplicate-sample", secondScope.RoadSectionVersionId, null, null,
            MeasurementType.DepressionDepth, 1m, "mm", GpsGeometryFactory.CreatePoint(new Coordinate(106.72, 10.82)),
            "gauge", null, "method", "Research Engineer", DateTimeOffset.UtcNow, null, "No file available.");
        var second = GroundTruthMeasurement.Create(
            Guid.NewGuid(), session.Id, "duplicate-sample", secondScope.RoadSectionVersionId, null, null,
            MeasurementType.DepressionDepth, 2m, "mm", GpsGeometryFactory.CreatePoint(new Coordinate(106.73, 10.83)),
            "gauge", null, "method", "Research Engineer", DateTimeOffset.UtcNow, null, "No file available.");
        secondContext.AddRange(session, first, second);

        var duplicateSampleWrite = () => secondContext.SaveChangesAsync();
        await duplicateSampleWrite.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact(DisplayName = "P2-40: submitted measurement is immutable")]
    public async Task SubmittedMeasurement_Update_IsRejectedBySqlServer()
    {
        await using var context = _fixture.CreateDbContext();
        var scope = await CreateScopeAsync(context);
        var session = FieldInspectionSession.Create(
            Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, null, scope.ProjectId,
            scope.RoadSectionVersionId, null, $"P240-I-{Guid.NewGuid():N}", null, "Research Engineer",
            DateTimeOffset.UtcNow, null, "research method", FieldInspectionSessionStatus.Locked, null);
        var measurement = GroundTruthMeasurement.Create(
            Guid.NewGuid(), session.Id, "immutable-sample", scope.RoadSectionVersionId, null, null,
            MeasurementType.DepressionDepth, 3m, "mm", GpsGeometryFactory.CreatePoint(new Coordinate(106.74, 10.84)),
            "gauge", null, "method", "Research Engineer", DateTimeOffset.UtcNow, null, "No file available.");
        context.AddRange(session, measurement);
        await context.SaveChangesAsync();

        var update = () => context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [GroundTruthMeasurements]
            SET [Value] = {9m}
            WHERE [Id] = {measurement.Id}
            """);

        await update.Should().ThrowAsync<SqlException>();
    }

    [Fact(DisplayName = "P2-40: measurement rejects invalid units, values and SRID")]
    public void Measurement_InvalidUnitValueOrSrid_IsRejectedByDomainInvariant()
    {
        var invalidUnit = () => GroundTruthMeasurement.Create(
            Guid.NewGuid(), Guid.NewGuid(), "sample", Guid.NewGuid(), null, null,
            MeasurementType.DepressionDepth, 1m, "inch", GpsGeometryFactory.CreatePoint(new Coordinate(1, 1)),
            "gauge", null, "method", "observer", DateTimeOffset.UtcNow, null, "No file available.");
        var invalidValue = () => GroundTruthMeasurement.Create(
            Guid.NewGuid(), Guid.NewGuid(), "sample", Guid.NewGuid(), null, null,
            MeasurementType.DepressionDepth, -1m, "mm", GpsGeometryFactory.CreatePoint(new Coordinate(1, 1)),
            "gauge", null, "method", "observer", DateTimeOffset.UtcNow, null, "No file available.");
        var invalidSrid = () => GroundTruthMeasurement.Create(
            Guid.NewGuid(), Guid.NewGuid(), "sample", Guid.NewGuid(), null, null,
            MeasurementType.DepressionDepth, 1m, "mm", EngineeringGeometryFactory.CreatePoint(new Coordinate(1, 1)),
            "gauge", null, "method", "observer", DateTimeOffset.UtcNow, null, "No file available.");

        invalidUnit.Should().Throw<ArgumentException>();
        invalidValue.Should().Throw<ArgumentOutOfRangeException>();
        invalidSrid.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "P2-40: SQL Server exposes measurement tables and immutable backstops")]
    public async Task SchemaBackstops_ArePresent()
    {
        await using var context = _fixture.CreateDbContext();
        var tableCount = await context.Database.SqlQueryRaw<int>("""
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.tables
            WHERE [name] IN ('FieldInspectionAssignments', 'FieldInspectionSessions', 'GroundTruthMeasurements')
            """).SingleAsync();
        tableCount.Should().Be(3);

        var triggerCount = await context.Database.SqlQueryRaw<int>("""
            SELECT CAST(COUNT(*) AS int) AS [Value]
            FROM sys.triggers
            WHERE [name] IN ('TR_FieldInspectionSessions_Integrity', 'TR_FieldInspectionSessions_Immutable', 'TR_GroundTruthMeasurements_Integrity', 'TR_GroundTruthMeasurements_Immutable')
            """).SingleAsync();
        triggerCount.Should().Be(4);
    }

    private async Task<P240Scope> CreateScopeAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = $"P240-{Guid.NewGuid():N}",
            Name = "P2-40 measurement fixture",
            EngineeringUtmSrid = 32648,
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var pm = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p240_pm_{Guid.NewGuid():N}",
            DisplayName = "P2-40 PM",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.ProjectManager,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var crew = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"p240_crew_{Guid.NewGuid():N}",
            DisplayName = "P2-40 Crew",
            PasswordHash = "fixture-password-hash",
            RoleCode = UserRoleCode.RepairCrew,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"P240-ROAD-{Guid.NewGuid():N}");
        var roadVersion = RoadSectionVersion.Create(
            Guid.NewGuid(), road.Id, 1, true,
            EngineeringGeometryFactory.CreateLineString(new[]
            {
                new Coordinate(588500, 2325000),
                new Coordinate(588600, 2325100)
            }), DateTimeOffset.UtcNow, "P2-40 fixture version");
        var survey = Survey.Create(
            Guid.NewGuid(), null, project.Id, roadVersion.Id, SurveyType.Periodic,
            SurveyStatus.InProgress, false, null, null);
        var defectType = DefectType.Create($"P240-DT-{Guid.NewGuid():N}", "P2-40 fixture defect");
        var causeCategory = CauseCategory.Create($"P240-CC-{Guid.NewGuid():N}", "P2-40 fixture cause");
        var defect = Defect.Create(
            Guid.NewGuid(), project.Id, roadVersion.Id, null, defectType.Code, causeCategory.Code,
            DefectSeverity.Medium, DefectStatus.Open,
            EngineeringGeometryFactory.CreatePoint(new Coordinate(588550, 2325050)), DateTimeOffset.UtcNow);
        var task = FieldInspectionTask.Create(
            Guid.NewGuid(), $"P240-TASK-{Guid.NewGuid():N}", project.Id, defect.Id, survey.Id,
            roadVersion.Id, 1, "{\"points\":[1]}", null, null, DateTimeOffset.UtcNow.AddDays(1),
            FieldInspectionTaskStatus.Accepted, pm.Id, null, null, null, null);
        var assignment = FieldInspectionAssignment.Create(
            Guid.NewGuid(), task.Id, crew.Id, pm.Id, DateTimeOffset.UtcNow, null,
            FieldInspectionAssignmentStatus.Active, null);
        var file = StoredFile.Create(
            Guid.NewGuid(), $"file:///p240/{Guid.NewGuid():N}", "evidence.jpg", "image/jpeg", 128,
            new string('a', 64), crew.Id, DateTimeOffset.UtcNow, null);

        context.AddRange(project, pm, crew, road, roadVersion, survey, defectType, causeCategory, defect, task, assignment, file);
        await context.SaveChangesAsync();
        return new(project.Id, pm.Id, crew.Id, roadVersion.Id, survey.Id, defect.Id, task.Id, file.Id);
    }

    private sealed record P240Scope(
        Guid ProjectId,
        Guid ProjectManagerId,
        Guid CrewId,
        Guid RoadSectionVersionId,
        Guid SurveyId,
        Guid DefectId,
        Guid TaskId,
        Guid FileId);
}
