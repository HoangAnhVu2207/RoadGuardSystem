using RoadGuardSystem.BusinessObjects.Labels;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Labels;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.Services.Integration;
using Xunit;

namespace RoadGuardSystem.UnitTests.Labels;

[Trait("Package", "HUY-01")]
public sealed class Huy01TrainingLabelLifecycleTests
{
    [Fact]
    public void Policy_CreateAndRevise_RequireFileRelationAndPreserveSourceIdentity()
    {
        var actorId = Guid.NewGuid(); var project = Guid.NewGuid(); var sourceId = Guid.NewGuid(); var file = Guid.NewGuid(); var evidenceId = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var actor = new UserSecurityState(actorId, "pm", "pm", UserRoleCode.ProjectManager, UserStatus.Active, false, [1]);
        var membership = new EffectiveProjectMembership(Guid.NewGuid(), project, actorId, UserRoleCode.ProjectManager, ProjectMemberStatus.Active, DateOnly.FromDateTime(now.UtcDateTime), null);
        var domain = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, sourceId, "source-v1"), project, "geometry");
        var evidence = new ResolvedEvidenceFacts(VerifiedEvidenceReference.Create(evidenceId, file, "file-v1", Guid.NewGuid()), null, "REPORT_PHOTO", new string('a', 64), 4, "image/jpeg", now);
        var facts = new ResolvedCandidateSourceFacts(domain, Guid.NewGuid(), [evidenceId], [evidence], null!);
        Assert.Throws<InvalidOperationException>(() => TrainingLabelPolicy.Create(Guid.NewGuid(), actor, membership, facts, Guid.NewGuid(), 0, 0, 1, 1, "CRACK", "Unrelated file", now));
        var head = TrainingLabelPolicy.Create(Guid.NewGuid(), actor, membership, facts, file, 0, 0, 1, 1, "CRACK", "Manual", now);
        Assert.Equal("file-v1", head.CurrentFileVersion);
        var unrelated = facts with { DomainFacts = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.Report, Guid.NewGuid(), "source-v2"), project, "geometry") };
        Assert.Throws<InvalidOperationException>(() => TrainingLabelPolicy.Revise(head, actor, membership, unrelated, 1, file, 0, 0, 1, 1, "CRACK", "Changed source", now));
        Assert.Single(head.Revisions);
        TrainingLabelPolicy.Revise(head, actor, membership, facts, 1, file, 0, 0, 1, 1, "CRACK", "New revision", now);
        Assert.Equal(sourceId, head.SourceId); Assert.Equal(2, head.CurrentRevision.Revision);
    }
    [Theory]
    [InlineData(UserRoleCode.Supervisor)]
    [InlineData(UserRoleCode.RepairCrew)]
    [InlineData(UserRoleCode.Reporter)]
    public void Policy_HigherOrOtherRoleCannotApprovePmOnlyLabel(UserRoleCode role)
    {
        var actorId = Guid.NewGuid(); var project = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var actor = new UserSecurityState(actorId, "actor", "actor", role, UserStatus.Active, false, [1]);
        var member = new EffectiveProjectMembership(Guid.NewGuid(), project, actorId, role, ProjectMemberStatus.Active, DateOnly.FromDateTime(now.UtcDateTime), null);
        Assert.Throws<UnauthorizedAccessException>(() => TrainingLabelPolicy.Authorize(actor, member, project, now));
    }

    [Fact]
    public void Policy_ExpiredMembershipAndPasswordChangeFailBeforeReviewMutation()
    {
        var actorId = Guid.NewGuid(); var project = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var actor = new UserSecurityState(actorId, "pm", "pm", UserRoleCode.ProjectManager, UserStatus.Active, false, [1]);
        var date = DateOnly.FromDateTime(now.UtcDateTime);
        var member = new EffectiveProjectMembership(Guid.NewGuid(), project, actorId, UserRoleCode.ProjectManager, ProjectMemberStatus.Active, date.AddDays(-10), date.AddDays(-1));
        var head = TrainingLabel.Create(Guid.NewGuid(), project, Guid.NewGuid(), "REPORT", "source", Guid.NewGuid(), "file", 0, 0, 1, 1, "CRACK", "Manual");
        Assert.Throws<UnauthorizedAccessException>(() => TrainingLabelPolicy.Review(head, actor, member, 1, TrainingLabelReviewStatus.Approved, "Review", now));
        var valid = member with { ValidTo = null };
        Assert.Throws<UnauthorizedAccessException>(() => TrainingLabelPolicy.Review(head, actor with { MustChangePassword = true }, valid, 1, TrainingLabelReviewStatus.Approved, "Review", now));
        Assert.Equal(TrainingLabelReviewStatus.Pending, head.CurrentRevision.Status);
        TrainingLabelPolicy.Review(head, actor, valid, 1, TrainingLabelReviewStatus.Approved, "Review", now);
        Assert.NotNull(head.CurrentApprovedRevision);
    }
    [Fact]
    public void NewRevision_ResetsEligibilityAndKeepsOldApprovalImmutable()
    {
        var head = TrainingLabel.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "REPORT", "source-v1", Guid.NewGuid(), "file-v1",
            0.1m, 0.1m, 0.2m, 0.2m, "CRACK", "Manual annotation");
        head.Review(1, Guid.NewGuid(), TrainingLabelReviewStatus.Approved, "Reviewed", DateTimeOffset.UtcNow);
        Assert.NotNull(head.CurrentApprovedRevision);
        var approved = head.CurrentApprovedRevision;
        head.AppendRevision(1, "source-v2", Guid.NewGuid(), "file-v2", 0.2m, 0.2m, 0.2m, 0.2m, "CRACK", "New annotation");
        Assert.Null(head.CurrentApprovedRevision);
        Assert.Equal(TrainingLabelReviewStatus.Approved, approved!.Status);
        Assert.Equal(2, head.Revisions.Count);
        Assert.Equal(TrainingLabelReviewStatus.Pending, head.CurrentRevision.Status);
    }

    [Fact]
    public void StaleOrInvalidRevision_DoesNotMoveHeadOrChangeHistory()
    {
        var head = TrainingLabel.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "REPORT", "source-v1", Guid.NewGuid(), "file-v1",
            0.1m, 0.1m, 0.2m, 0.2m, "CRACK", "Manual annotation");
        Assert.Throws<InvalidOperationException>(() => head.AppendRevision(0, "v2", Guid.NewGuid(), "f2", 0, 0, 1, 1, "CRACK", "Stale"));
        Assert.Throws<ArgumentOutOfRangeException>(() => head.AppendRevision(1, "v2", Guid.NewGuid(), "f2", 0.9m, 0, 1, 1, "CRACK", "Invalid"));
        Assert.Single(head.Revisions); Assert.Equal(1, head.CurrentRevision.Revision);
        Assert.Throws<InvalidOperationException>(() => head.Review(2, Guid.NewGuid(), TrainingLabelReviewStatus.Approved, "Stale", DateTimeOffset.UtcNow));
        Assert.Equal(TrainingLabelReviewStatus.Pending, head.CurrentRevision.Status);
    }
}
