using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Implementations.Cases;
using RoadGuardSystem.Services.Implementations.Integration;
using RoadGuardSystem.Services.Reporting;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

[Trait("Package", "HUY-01")]
public sealed class Huy01CaseDefectConsumerCompatibilityTests
{
    private static readonly Guid Project = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherProject = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid Case = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Report = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset CapturedAt = new(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(IncidentCaseStatus.Unassigned, "UNASSIGNED")]
    [InlineData(IncidentCaseStatus.Open, "OPEN")]
    [InlineData(IncidentCaseStatus.AwaitingEvidence, "AWAITING_EVIDENCE")]
    [InlineData(IncidentCaseStatus.Concluded, "CONCLUDED")]
    [InlineData(IncidentCaseStatus.Linked, "LINKED")]
    public void ReaderStatus_UsesCanonicalCaseWorkflowVocabulary(IncidentCaseStatus status, string expected)
    {
        CaseWorkflowRepository.WireStatus(status).Should().Be(expected);
        CaseDefectReadReader.WireStatus(status).Should().Be(expected);
    }

    [Fact]
    public void ReaderHash_MatchesActualConsumerAndIgnoresPreexistingHash()
    {
        var snapshot = Snapshot();
        var expected = CaseDefectCaptureConsumer.Hash(snapshot);

        expected.Should().HaveLength(64);
        CaseDefectReadReader.ComputeHash(snapshot).Should().Be(expected);
        CaseDefectReadReader.ComputeHash(snapshot with { Hash = new string('f', 64) }).Should().Be(expected);
        var changed = snapshot with { MissingReasons = [.. snapshot.MissingReasons, "new_source_gap"] };
        var changedExpected = CaseDefectCaptureConsumer.Hash(changed);
        changedExpected.Should().NotBe(expected);
        CaseDefectReadReader.ComputeHash(changed).Should().Be(changedExpected);
        CaseDefectCaptureConsumer.Apply(Capture(), snapshot with { Hash = CaseDefectReadReader.ComputeHash(snapshot) })
            .CaseDefectFacts.Should().NotBeNull();
    }

    [Fact]
    public void ActualConsumer_PreservesUnknownMetricsForIncompleteValidSnapshot()
    {
        var capture = Capture();
        var signed = Sign(Snapshot());

        var result = CaseDefectCaptureConsumer.Apply(capture, signed);

        result.CaseDefectFacts.Should().BeSameAs(signed);
        result.Summary.Metrics.Single().Value.Should().BeNull();
        result.Summary.Metrics.Single().Availability.Should().Be("UNAVAILABLE");
        result.Files.Should().BeEmpty();
    }

    [Fact]
    public void ActualConsumer_RejectsNestedWrongProjectEvenWithValidHash()
    {
        var snapshot = Snapshot();
        var wrongProject = Sign(snapshot with
        {
            Cases = [snapshot.Cases[0] with { ProjectId = OtherProject }]
        });

        var act = () => CaseDefectCaptureConsumer.Apply(Capture(), wrongProject);
        act.Should().Throw<InvalidOperationException>();
    }

    private static CaseDefectSnapshotV1 Snapshot() => new(
        "anh-huy.case-defect.v1",
        Guid.Parse("40000000-0000-0000-0000-000000000001"),
        Project,
        CapturedAt,
        "",
        [new CaseReadFactV1(Case, Project, "case-version-1", "AWAITING_EVIDENCE",
            [new CaseReportRefV1(Report, "report-version-1", CapturedAt.AddDays(-1))],
            [new ReportingSourceRefDto("CASE_CONCLUSION", Guid.Parse("50000000-0000-0000-0000-000000000001"), "case-version-1")],
            [new CasePublicationRefV1(Guid.Parse("60000000-0000-0000-0000-000000000001"), "case-version-1", [Report], [])])],
        [],
        [],
        ["defect_source_version_and_geometry_unavailable", "evidence_file_checksum_and_recipient_authority_not_captured"]);

    private static CaseDefectSnapshotV1 Sign(CaseDefectSnapshotV1 snapshot)
        => snapshot with { Hash = CaseDefectCaptureConsumer.Hash(snapshot) };

    private static ReportingCaptureDto Capture() => new(
        new ProjectSummaryV1("anh02.reporting.v1", Project, CapturedAt, "pilot-reporting.v1", new(),
            [new ReportingMetricDto("casesByStatus", new(), null, "count", null, null, false,
                "UNAVAILABLE", ["HUY01_REPORTING_READER_UNAVAILABLE"], [])], [], "SERIALIZABLE"),
        [], [], [], new("PARTIAL", ["HUY_TIMELINE_READER_UNAVAILABLE"]));
}
