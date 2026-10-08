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
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Inspections;

public sealed class P240FieldInspectionMeasurementSchemaTests
{
    private static readonly GeometryFactory EngineeringGeometryFactory = new(new PrecisionModel(), 32648);
    private static readonly GeometryFactory GpsGeometryFactory = new(new PrecisionModel(), 4326);
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


    [Fact(DisplayName = "P2-40: purpose gates reject task-bearing research sessions")]
    public void ResearchValidationSession_WithTask_IsRejectedByDomainInvariant()
    {
        var create = () => FieldInspectionSession.Create(
            Guid.NewGuid(), FieldInspectionPurpose.ResearchValidation, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, "invalid-research", null, "Research Engineer", DateTimeOffset.UtcNow,
            null, "research method", FieldInspectionSessionStatus.Draft, null);

        create.Should().Throw<ArgumentException>();
    }

}
