using FluentAssertions;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;
using Xunit;

namespace RoadGuardSystem.UnitTests.Defects;

public sealed class P232DomainInvariantTests
{
    [Fact]
    public void AIDetection_Create_ValidatesConfidenceAndPreservesRawPayload()
    {
        var geometry = new Point(new Coordinate(106.7, 10.8)) { SRID = 4326 };

        var detection = AIDetection.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            geometry,
            "CRACK",
            0.875m,
            1.2m,
            3.4m,
            "{\"source\":\"mock\"}");

        detection.Confidence.Should().Be(0.875m);
        detection.RawPayload.Should().Be("{\"source\":\"mock\"}");
        detection.Geometry.Should().BeSameAs(geometry);

        var invalidConfidence = () => AIDetection.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            1.001m,
            null,
            null,
            "{}");

        invalidConfidence.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AIDetection_Create_RejectsMalformedRawPayload()
    {
        var create = () => AIDetection.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null,
            0.5m,
            null,
            null,
            "not-json");

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Defect_Create_UsesTypedWorkflowShapeAndRetainsCatalogFactory()
    {
        var geometry = new Point(new Coordinate(106.7, 10.8)) { SRID = 4326 };
        var defect = Defect.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CRACK",
            "WEATHER",
            DefectSeverity.High,
            DefectStatus.Open,
            geometry,
            new DateTimeOffset(2026, 9, 22, 8, 0, 0, TimeSpan.FromHours(7)));

        defect.Status.Should().Be(DefectStatus.Open);
        defect.Severity.Should().Be(DefectSeverity.High);
        defect.ReportedAt!.Value.Offset.Should().Be(TimeSpan.Zero);

        var legacy = Defect.Create(Guid.NewGuid(), "CRACK", "WEATHER");
        legacy.DefectTypeCode.Should().Be("CRACK");
        legacy.CauseCategoryCode.Should().Be("WEATHER");
    }

    [Fact]
    public void DefectVerificationLog_Create_RequiresExactlyOneTarget()
    {
        var detectionId = Guid.NewGuid();
        var log = DefectVerificationLog.Create(
            Guid.NewGuid(),
            null,
            detectionId,
            DefectVerificationAction.PreliminaryKeep,
            "{\"before\":1}",
            "{\"after\":2}",
            null,
            null,
            Guid.NewGuid(),
            "kept for PM review");

        log.AIDetectionId.Should().Be(detectionId);

        var twoTargets = () => DefectVerificationLog.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            detectionId,
            DefectVerificationAction.Adjust,
            null,
            null,
            null,
            null,
            Guid.NewGuid(),
            "invalid");

        twoTargets.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FieldInspectionTask_Create_RequiresValidScopeAndStatus()
    {
        var task = FieldInspectionTask.Create(
            Guid.NewGuid(),
            "TASK-001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "{\"points\":[1,2]}",
            "Use calibrated gauge.",
            null,
            new DateTimeOffset(2026, 9, 30, 8, 0, 0, TimeSpan.FromHours(7)),
            FieldInspectionTaskStatus.NewAssigned,
            Guid.NewGuid(),
            null,
            null,
            null,
            null);

        task.Status.Should().Be(FieldInspectionTaskStatus.NewAssigned);
        task.DueAt.Offset.Should().Be(TimeSpan.Zero);

        var invalidStatus = () => FieldInspectionTask.Create(
            Guid.NewGuid(),
            "TASK-002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "{}",
            null,
            null,
            DateTimeOffset.UtcNow,
            FieldInspectionTaskStatus.Unknown,
            Guid.NewGuid(),
            null,
            null,
            null,
            null);

        invalidStatus.Should().Throw<ArgumentOutOfRangeException>();
    }
}
