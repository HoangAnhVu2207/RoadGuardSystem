using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4SafetyVerifiedEvidenceTests
{
    [Fact]
    public void VerifiedCheckPinsActualUploaderAndOnlyDangerCreatesWarningHistory()
    {
        var at = new DateTimeOffset(2026, 10, 6, 3, 0, 0, TimeSpan.Zero);
        var project = Guid.NewGuid(); var defect = Guid.NewGuid(); var crew = Guid.NewGuid();
        var road = Guid.NewGuid();
        RepairObligation Obligation(RepairObligationKind kind) => RepairObligation.Create(Guid.NewGuid(), project,
            defect, kind, true, RepairActualScope.Create(Guid.NewGuid(), road, "actual-frame", "road", 1, 2, 0, 1));
        var formal = Obligation(RepairObligationKind.FormalRepair);
        var safety = Obligation(RepairObligationKind.TemporarySafety);
        var measure = TemporarySafetyMeasure.Create(Guid.NewGuid(), project, defect, formal.Id, crew,
            "each shift", "barrier displaced", "formal repair finished");
        measure.Install(Guid.NewGuid(), crew, at, at.AddHours(12));
        var monitoring = RepairSafetyMonitoring.Create(measure, safety, formal);
        var link = Guid.NewGuid(); var file = Guid.NewGuid(); var uploader = Guid.NewGuid();
        var verified = new RepairSafetyEvidenceReference(file, "file-v1", new string('a', 64),
            RepairEvidencePurpose.Safety, true, true, "FIELD_MEASUREMENT", link, at, null, false, uploader);
        Assert.Throws<ArgumentException>(() => monitoring.RecordVerifiedCheck(Guid.NewGuid(), crew, at,
            RepairSafetyCheckResult.Safe, "barrier intact", [link], [verified with { ActualUploaderId = null }]));
        var check = monitoring.RecordVerifiedCheck(Guid.NewGuid(), crew, at.AddMinutes(1),
            RepairSafetyCheckResult.Danger, "barrier displaced", [link], [verified]);
        Assert.Equal(check.Id, monitoring.CurrentCheckId);
        Assert.Equal(uploader, Assert.Single(check.Evidence).ActualUploaderId);
        var warning = monitoring.ReceiveDangerWarning(Guid.NewGuid(), check.Id, at.AddMinutes(1), "barrier displaced");
        Assert.Equal(monitoring.Id, warning.MonitoringId);
        Assert.Equal(warning.ServerReceivedAt.AddHours(1), warning.OriginalAcknowledgementDueAt);
        var acknowledged = monitoring.AcknowledgeDanger(Guid.NewGuid(), warning.Id, crew, at.AddHours(2),
            "received and checked");
        Assert.Equal(monitoring.Id, acknowledged.MonitoringId);
        Assert.True(acknowledged.AfterOriginalDue);
        Assert.False(safety.IsResolved);
    }
}
