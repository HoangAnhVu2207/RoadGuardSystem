using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Candidates;

[Trait("Package", "HUY-01")]
public sealed class Huy01CandidateDecisionDomainTests
{
    [Fact]
    public void Create_KeepNew_RequiresClassificationAndDoesNotCreateTaskOrTarget()
    {
        var facts = CreateFacts();
        var create = () => CandidateDecision.Create(
            Guid.NewGuid(), facts, CandidateDecisionKind.KeepNew, null, Guid.Empty, null, null,
            Guid.NewGuid(), "new defect", DateTimeOffset.UtcNow);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_LinkExisting_RequiresTargetVersionAndForbidsClassification()
    {
        var facts = CreateFacts();
        var classification = CandidateClassification.Create(Guid.NewGuid(), "CRACK", null, DefectSeverity.High, null);
        var create = () => CandidateDecision.Create(
            Guid.NewGuid(), facts, CandidateDecisionKind.LinkExisting, classification, Guid.NewGuid(), null, null,
            Guid.NewGuid(), "same defect", DateTimeOffset.UtcNow);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Reject_ForbidsTargetAndClassificationWhilePreservingSourceFacts()
    {
        var facts = CreateFacts();
        var decision = CandidateDecision.Create(
            Guid.NewGuid(), facts, CandidateDecisionKind.Reject, null, Guid.Empty, null, null,
            Guid.NewGuid(), "not a defect", DateTimeOffset.UtcNow);

        decision.Source.Should().Be(facts.Source);
        decision.ProjectId.Should().Be(facts.ProjectId);
        decision.GeometryVersion.Should().Be("geometry-v3");
        decision.TargetDefectId.Should().BeNull();
        decision.Classification.Should().BeNull();
    }

    [Fact]
    public void Create_CorrectionRequiresMatchingActiveDispositionAndLeavesHistoryImmutable()
    {
        var originalFacts = CreateFacts();
        var original = CandidateDecision.Create(
            Guid.NewGuid(), originalFacts, CandidateDecisionKind.Reject, null, Guid.Empty, null, null,
            Guid.NewGuid(), "not enough evidence", DateTimeOffset.UtcNow);
        var activeFacts = CreateFacts(new ActiveCandidateDisposition(original.Id, "decision-v1"));
        var correction = CandidateCorrection.Create(original.Id, "decision-v1");

        var corrected = CandidateDecision.Create(
            Guid.NewGuid(), activeFacts, CandidateDecisionKind.KeepNew,
            CandidateClassification.Create(Guid.NewGuid(), "CRACK", null, DefectSeverity.Medium, null),
            Guid.Empty, null, correction, Guid.NewGuid(), "new evidence", DateTimeOffset.UtcNow);

        corrected.SupersedesDecisionId.Should().Be(original.Id);
        corrected.ExpectedPreviousDecisionVersion.Should().Be("decision-v1");
        original.Decision.Should().Be(CandidateDecisionKind.Reject);
        original.Reason.Should().Be("not enough evidence");
        var staleCorrection = () => CandidateDecision.Create(
            Guid.NewGuid(), activeFacts, CandidateDecisionKind.Reject, null, Guid.Empty, null,
            CandidateCorrection.Create(original.Id, "decision-v0"), Guid.NewGuid(), "still reject", DateTimeOffset.UtcNow);
        staleCorrection.Should().Throw<InvalidOperationException>();
    }

    private static CandidateSourceFacts CreateFacts(ActiveCandidateDisposition? activeDisposition = null)
    {
        return CandidateSourceFacts.Create(
            CandidateSourceIdentity.Create(CandidateSourceKind.Report, Guid.NewGuid(), "report-v2"),
            Guid.NewGuid(),
            "geometry-v3",
            activeDisposition);
    }
}
