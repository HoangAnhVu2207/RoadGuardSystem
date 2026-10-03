using FluentAssertions;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Services.Reporting;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reporting;

public sealed class CaseDefectCaptureConsumerTests
{
    [Fact]
    public void CompleteTypedFacts_UseHalfOpenReceivedPeriodAndCurrentCaseStock_WithoutFilePermission()
    {
        var project = Guid.NewGuid(); var start = DateTimeOffset.UnixEpoch;
        var capture = Capture(project, new(start, start.AddDays(1)));
        var report = Guid.NewGuid(); var incident = Guid.NewGuid();
        var snapshot = Sign(new("anh-huy.case-defect.v1", Guid.NewGuid(), project, start, "",
            [new(incident, project, "case-v1", "CONCLUDED",
                [new(report, "report-v1", start), new(Guid.NewGuid(), "report-v2", start.AddDays(1))], [], [])], [],
            [new(incident, report, Guid.NewGuid(), Guid.NewGuid(), "file-v1", new string('a',64), 4294967296L, "image/png")], []));
        var result = CaseDefectCaptureConsumer.Apply(capture, snapshot);
        result.Summary.Metrics.Single(m => m.Code == "reportsReceived").Value.Should().Be(1);
        result.Summary.Metrics.Single(m => m.Code == "casesByStatus").Value.Should().Be(1);
        result.Summary.Metrics.Single(m => m.Code == "defectsByStatus").Value.Should().Be(0);
        result.Files.Should().BeEmpty(); // Case metadata never grants private byte access.
        result.CaseDefectFacts.Should().BeSameAs(snapshot);
    }

    [Fact]
    public void IncompleteFacts_DoNotPromoteUnavailableCountsToEmptySuccess()
    {
        var project = Guid.NewGuid(); var capture = Capture(project, new());
        var snapshot = Sign(new("anh-huy.case-defect.v1", Guid.NewGuid(), project, DateTimeOffset.UnixEpoch, "", [], [], [], ["LABEL_REFERENCES_UNAVAILABLE"]));
        var result = CaseDefectCaptureConsumer.Apply(capture, snapshot);
        result.Summary.Should().BeSameAs(capture.Summary);
        result.Summary.Metrics.Single().Value.Should().BeNull();
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("hash")]
    [InlineData("scope")]
    public void InvalidOrOutOfScopeProducerFacts_FailClosed(string mutation)
    {
        var project = Guid.NewGuid(); var route = Guid.NewGuid();
        var capture = Capture(project, new(RouteVersionId: route));
        var defect = new DefectReadFactV1(Guid.NewGuid(), project, "v1", "OPEN", "REPORT", Guid.NewGuid(), "s1", route, null, null, null, "UNKNOWN");
        var snapshot = Sign(new("anh-huy.case-defect.v1", Guid.NewGuid(), project, DateTimeOffset.UnixEpoch, "", [], [defect], [], []));
        snapshot = mutation switch {
            "foreign" => Sign(snapshot with { ProjectId = Guid.NewGuid() }),
            "scope" => Sign(snapshot with { Defects = [defect with { RouteVersionId = Guid.NewGuid() }] }),
            _ => snapshot with { Hash = new string('a',64) } };
        var action = () => CaseDefectCaptureConsumer.Apply(capture, snapshot);
        action.Should().Throw<InvalidOperationException>();
    }
    [Theory]
    [InlineData("empty-report")]
    [InlineData("duplicate-report")]
    [InlineData("bad-sha")]
    [InlineData("duplicate-evidence")]
    public void SignedMalformedNestedFacts_AreRejected(string mutation)
    {
        var project = Guid.NewGuid(); var incident = Guid.NewGuid(); var report = Guid.NewGuid();
        var reference = new CaseReportRefV1(report, "v1", DateTimeOffset.UnixEpoch);
        var evidence = new CaseEvidenceRefV1(incident, report, Guid.NewGuid(), Guid.NewGuid(), "v1", new string('a',64), 4, "image/png");
        var snapshot = new CaseDefectSnapshotV1("anh-huy.case-defect.v1", Guid.NewGuid(), project, DateTimeOffset.UnixEpoch, "",
            [new(incident, project, "v1", "OPEN", [reference], [], [])], [], [evidence], []);
        snapshot = mutation switch {
            "empty-report" => snapshot with { Cases = [snapshot.Cases[0] with { CurrentReports = [reference with { ReportId = Guid.Empty }] }] },
            "duplicate-report" => snapshot with { Cases = [snapshot.Cases[0] with { CurrentReports = [reference, reference] }] },
            "bad-sha" => snapshot with { AuthorizedEvidence = [evidence with { Sha256 = new string('z',64) }] },
            _ => snapshot with { AuthorizedEvidence = [evidence, evidence] } };
        var action = () => CaseDefectCaptureConsumer.Apply(Capture(project, new()), Sign(snapshot));
        action.Should().Throw<InvalidOperationException>();
    }
    private static CaseDefectSnapshotV1 Sign(CaseDefectSnapshotV1 snapshot) => snapshot with { Hash = CaseDefectCaptureConsumer.Hash(snapshot) };
    private static ReportingCaptureDto Capture(Guid project, ReportingFiltersDto filters) => new(
        new("anh02.reporting.v1", project, DateTimeOffset.UnixEpoch, "pilot-reporting.v1", filters,
            [new("defectsByStatus", new(), null, "count", null, null, false, "UNAVAILABLE", ["HUY01_REPORTING_READER_UNAVAILABLE"], [])], [], "SERIALIZABLE"),
        [], [], [], new("PARTIAL", ["HUY_TIMELINE_READER_UNAVAILABLE"]));
}
