using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Defects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Defects;

[Trait("Package", "HUY-01")]
public sealed class Huy01DefectWorkflowTests
{
    private static Defect Create()
    {
        var facts = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, Guid.NewGuid(), "source"), Guid.NewGuid(), "geometry");
        return Defect.CreateFromReport(Guid.NewGuid(), facts, CandidateClassification.Create(Guid.NewGuid(), "CRACK", null, DefectSeverity.Low, null), null, DateTimeOffset.UtcNow);
    }
    [Fact]
    public void ReportFactory_NoGps_HasNoInventedGeometryAiDetectionOrTask()
    {
        var defect = Create(); Assert.Null(defect.Geometry); Assert.Null(defect.SourceAIDetectionId);
        Assert.NotNull(defect.ProjectId); Assert.Equal(DefectStatus.Open, defect.Status);
    }
    [Fact]
    public void Assessment_InvalidReasonDoesNotPartiallyChangeClassification()
    {
        var defect = Create();
        Assert.Throws<ArgumentException>(() => defect.Assess("POTHOLE", null, DefectSeverity.High, Guid.NewGuid(), " "));
        Assert.Equal("CRACK", defect.DefectTypeCode); Assert.Equal(DefectSeverity.Low, defect.Severity);
        var log = defect.Assess("POTHOLE", null, DefectSeverity.High, Guid.NewGuid(), "Reviewed source");
        Assert.Equal(DefectVerificationAction.Adjust, log.Action); Assert.Equal(defect.Id, log.DefectId);
        Assert.Equal(DefectStatus.Open, defect.Status);
    }
    [Theory]
    [InlineData(DefectVerificationAction.Confirm, DefectStatus.Verified)]
    [InlineData(DefectVerificationAction.Reject, DefectStatus.Rejected)]
    public void ExistingEvidenceDecision_RequiresRelatedFactsAndPreservesTerminalState(DefectVerificationAction action, DefectStatus terminal)
    {
        var defect = Create(); var evidence = Guid.NewGuid(); var actor = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => defect.DecideFromExistingEvidence(action, [evidence], [], actor, "Insufficient relation"));
        Assert.Equal(DefectStatus.Open, defect.Status);
        var log = defect.DecideFromExistingEvidence(action, [evidence], [evidence], actor, "Authoritative evidence");
        Assert.Equal(terminal, defect.Status); Assert.Equal(action, log.Action); Assert.Null(log.FieldInspectionTaskId);
        Assert.Throws<InvalidOperationException>(() => defect.DecideFromExistingEvidence(action, [evidence], [evidence], actor, "Cannot reopen"));
        Assert.Equal(terminal, defect.Status);
    }
}
