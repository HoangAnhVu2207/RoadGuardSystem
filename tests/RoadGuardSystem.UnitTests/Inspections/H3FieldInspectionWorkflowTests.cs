using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Inspections;
using Xunit;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.UnitTests.Inspections;
public sealed class H3FieldInspectionWorkflowTests
{

    [Fact]
    public void OfflineFirstStart_OriginalClaimCannotBecomeTrustedTimeAtIntake()
    {
        var claimed = DateTimeOffset.Parse("2026-10-01T00:00:00Z");
        var received = claimed.AddDays(5);
        var origin = FieldTaskStartOrigin.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), new string('a', 64), Guid.NewGuid(), Guid.NewGuid(), claimed, 50, "boot", received,
            Guid.NewGuid(), null, null, false);
        origin.ClaimedAt.Should().Be(claimed);
        origin.ServerReceivedAt.Should().Be(received);
        origin.VerifiedOriginalAt.Should().BeNull();
        origin.TimeProvenance.Should().Be("CLAIMED_OFFLINE");
    }

    [Fact]
    public void PendingEvidenceDeclaration_DoesNotInventAFile()
    {
        var evidence = FieldInspectionEvidenceLink.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "AFTER", new string('b', 64), "image/jpeg", "{}");
        evidence.FileId.Should().BeNull();
        evidence.CaptureOriginId.Should().NotBeEmpty();
    }

    [Fact]
    public void SupplementReview_DoesNotClaimReceivedProtocolActivation()
    {
        var review = FieldInspectionReview.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "SUPPLEMENT", "AFTER missing", DateTimeOffset.UtcNow);
        review.ReceiptActivation.Should().Be("AWAITING_OWNER_RECEIPT_PROTOCOL");
    }

    [Fact]
    public void OperationalTask_ReporterPreservesNullSurveyAndMeasureOnlyPurpose()
    {
        var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), "FIELD", Guid.NewGuid(), Guid.NewGuid(),
            null, "REPORTER", Guid.NewGuid(), null, null, null, FieldInspectionPurpose.PreMeasurement,
            1, "{}", null, DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid());
        task.SurveyId.Should().BeNull();
        task.SourceKind.Should().Be("REPORTER");
        task.Purpose.Should().Be(FieldInspectionPurpose.PreMeasurement);
        task.LifecycleVersion.Should().Be(2);
    }

    [Fact]
    public void OperationalTask_SubmitBeforeStartIsRejected()
    {
        var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), "FIELD", Guid.NewGuid(), Guid.NewGuid(),
            null, "REPORTER", Guid.NewGuid(), null, null, null, FieldInspectionPurpose.Verification,
            1, "{}", null, DateTimeOffset.UtcNow.AddDays(1), Guid.NewGuid());
        var act = () => task.Transition(FieldInspectionTaskStatus.Submitted);
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("UNKNOWN", null, "Instrument unavailable")]
    [InlineData("KNOWN", 0, null)]
    public void CapturedMeasurement_PreservesUnknownOrGenuineZero(string state, int? value, string? reason)
    {
        var measurement = GroundTruthMeasurement.CreateCaptured(Guid.NewGuid(), Guid.NewGuid(), "M1", Guid.NewGuid(),
            null, Guid.NewGuid(), MeasurementType.DepressionDepth, value, state, reason, "LENGTH", "mm", null,
            "GPS unavailable", "manual", "visual", "Crew", DateTimeOffset.UtcNow, null, null);
        measurement.Value.Should().Be(value);
        measurement.ValueState.Should().Be(state);
        measurement.Location.Should().BeNull();
    }

    [Fact]
    public void CapturedMeasurement_AreaCannotUseLengthUnit()
    {
        var act = () => GroundTruthMeasurement.CreateCaptured(Guid.NewGuid(), Guid.NewGuid(), "M1", Guid.NewGuid(),
            null, Guid.NewGuid(), MeasurementType.Area, 2m, "KNOWN", null, "AREA", "m", null,
            "GPS unavailable", "manual", "visual", "Crew", DateTimeOffset.UtcNow, null, null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LegacyMeasurementFactoryCannotCreateAreaWithHistoricalLengthUnit()
    {
        var point=new NetTopologySuite.Geometries.GeometryFactory(new NetTopologySuite.Geometries.PrecisionModel(),4326).CreatePoint(new NetTopologySuite.Geometries.Coordinate(106,10));
        var act=()=>GroundTruthMeasurement.Create(Guid.NewGuid(),Guid.NewGuid(),"area",Guid.NewGuid(),null,null,MeasurementType.Area,0,"mm",point,"instrument",null,"method","Crew",DateTimeOffset.UtcNow,null,"reason");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CapturedAreaZeroRequiresSquareMetresAndLengthCannotUseThem()
    {
        var row=GroundTruthMeasurement.CreateCaptured(Guid.NewGuid(),Guid.NewGuid(),"area",Guid.NewGuid(),null,Guid.NewGuid(),MeasurementType.Area,0,"KNOWN",null,"AREA","m²",null,"GPS missing","instrument","method","Crew",DateTimeOffset.UtcNow,null,"reason");
        row.Value.Should().Be(0);row.Dimension.Should().Be("AREA");row.Unit.Should().Be("m²");
        var act=()=>GroundTruthMeasurement.CreateCaptured(Guid.NewGuid(),Guid.NewGuid(),"length",Guid.NewGuid(),null,Guid.NewGuid(),MeasurementType.DepressionDepth,0,"KNOWN",null,"LENGTH","m²",null,"GPS missing","instrument","method","Crew",DateTimeOffset.UtcNow,null,"reason");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CapturedGpsRejectsNonfiniteCoordinate(double coordinate)
    {
        var point=new NetTopologySuite.Geometries.GeometryFactory(new NetTopologySuite.Geometries.PrecisionModel(),4326).CreatePoint(new NetTopologySuite.Geometries.Coordinate(106,coordinate));
        var act=()=>GroundTruthMeasurement.CreateCaptured(Guid.NewGuid(),Guid.NewGuid(),"gps",Guid.NewGuid(),null,Guid.NewGuid(),MeasurementType.DepressionDepth,0,"KNOWN",null,"LENGTH","mm",point,null,"instrument","method","Crew",DateTimeOffset.UtcNow,null,"reason");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OperationalTask_HasDedicatedNoSurveyFactory()
    {
        typeof(FieldInspectionTask).GetMethod("CreateOperational").Should().NotBeNull(
            "Reporter-derived genuine Defects require FIELD tasks without a fabricated Survey");
        typeof(FieldInspectionTask).GetProperty("SurveyId")!.PropertyType.Should().Be(typeof(Guid?));
    }

    [Fact]
    public void CapturedMeasurement_HasExplicitUnknownValueAndLocation()
    {
        typeof(GroundTruthMeasurement).GetMethod("CreateCaptured").Should().NotBeNull(
            "structurally valid incomplete intake must preserve unknown measurements");
        typeof(GroundTruthMeasurement).GetProperty("Value")!.PropertyType.Should().Be(typeof(decimal?));
        typeof(GroundTruthMeasurement).GetProperty("ValueState").Should().NotBeNull();
        typeof(GroundTruthMeasurement).GetProperty("Dimension").Should().NotBeNull();
    }

    [Theory]
    [InlineData("FieldTaskStartOrigin")]
    [InlineData("FieldInspectionSubmission")]
    [InlineData("FieldInspectionEvidenceLink")]
    [InlineData("FieldInspectionReview")]
    public void ImmutableWorkflowFacts_AreConcreteDomainTypes(string name)
    {
        typeof(FieldInspectionTask).Assembly.GetType($"RoadGuardSystem.BusinessObjects.Inspections.{name}")
            .Should().NotBeNull("the workflow must persist real origins, intake, evidence and review facts");
    }
}
