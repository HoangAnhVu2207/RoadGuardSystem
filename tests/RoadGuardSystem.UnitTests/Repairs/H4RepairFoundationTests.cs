using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairFoundationTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
    private static readonly Guid Project = Guid.NewGuid(), Defect = Guid.NewGuid(), Road = Guid.NewGuid();
    private static readonly Guid Pm = Guid.NewGuid(), Supervisor = Guid.NewGuid(), Crew = Guid.NewGuid();

    [Fact]
    public void PhysicalOverlapCannotBeBypassedByDifferentDisplayIds()
    {
        var a = Scope(0, 10); var b = Scope(5, 15);
        Assert.Equal(RepairScopeComparison.Overlapping, a.Compare(b));
        Assert.Equal(RepairScopeComparison.Disjoint, a.Compare(Scope(10, 20)));
        Assert.Equal(RepairScopeComparison.Unknown, a.Compare(Scope(5, 15, "location-v2")));
    }

    [Fact]
    public void SamePhysicalRoadAcrossDifferentRouteLabelsStillOverlaps()
    {
        var a = Scope(0, 10, label: "main"); var b = Scope(1, 9, label: "branch");
        Assert.Equal(RepairScopeComparison.Overlapping, a.Compare(b));
        Assert.Throws<InvalidOperationException>(() => RepairScopeReservation.EnsureAvailable(a, [b]));
    }

    [Fact]
    public void UnknownScopeComparisonCannotGrantReservation()
        => Assert.Throws<InvalidOperationException>(() => RepairScopeReservation.EnsureAvailable(Scope(0, 10), [Scope(0, 10, "v2")]));

    [Fact]
    public void PolicyPublicationHasNoDefaultThresholdAndUnknownIsNotZero()
    {
        var policy = Policy();
        Assert.Equal(RepairEligibilityReason.UnknownMeasurement, policy.Evaluate([new("width", null, "mm")], []).Reason);
        Assert.True(policy.Evaluate([new("width", 0, "mm")], []).Eligible);
        Assert.Equal(RepairEligibilityReason.NotConfigured, RepairPolicyRevision.Publish(Guid.NewGuid(), Project, 1, Pm,
            UserRoleCode.ProjectManager, At, "CRACK", "checklist-v1", [], []).Evaluate([], []).Reason);
    }

    [Fact]
    public void PolicyRequiresExactConfiguredUnitsAndStopConditions()
    {
        var policy = Policy();
        Assert.Equal(RepairEligibilityReason.WrongUnit, policy.Evaluate([new("width", 1, "m")], []).Reason);
        Assert.Equal(RepairEligibilityReason.ThresholdNotMet, policy.Evaluate([new("width", 4, "mm")], []).Reason);
        Assert.Equal(RepairEligibilityReason.StopCondition, policy.Evaluate([new("width", 1, "mm")], ["unsafe"]).Reason);
        Assert.True(policy.Evaluate([new("width", 3, "mm")], []).Eligible);
    }

    [Fact]
    public void PolicyPublicationRequiresPmAndCopiesCallerCollections()
    {
        var rules = new[] { new RepairMeasurementRule("width", "mm", 0, 3) };
        var policy = RepairPolicyRevision.Publish(Guid.NewGuid(), Project, 1, Pm, UserRoleCode.ProjectManager, At,
            "CRACK", "checklist-v1", rules, ["unsafe"]);
        rules[0] = new("width", "mm", 0, 100);
        Assert.False(policy.Evaluate([new("width", 4, "mm")], []).Eligible);
        Assert.Throws<InvalidOperationException>(() => RepairPolicyRevision.Publish(Guid.NewGuid(), Project, 2, Supervisor,
            UserRoleCode.Supervisor, At, "CRACK", "checklist-v1", rules, []));
    }

    [Fact]
    public void MeasureOnlyNeverGrantsRepairEvenWithPassingFacts()
    {
        var grant = Grant(RepairTaskMode.MeasureOnly); Start(grant);
        Assert.Equal(RepairEligibilityReason.MeasureOnly, grant.Evaluate(At.AddHours(1), Facts()).Reason);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void FastTrackExactExpiryDeniesNewExecution(int ticks, bool eligible)
    {
        var grant = Grant(); Start(grant);
        Assert.Equal(eligible, grant.Evaluate(At.AddHours(24).AddTicks(ticks), Facts()).Eligible);
    }

    [Fact]
    public void DuplicateFirstStartDoesNotResetWindowAndDifferentOriginCannotReplaceIt()
    {
        var grant = Grant(); var origin = Guid.NewGuid();
        grant.RecordFirstStart(origin, "hash", At, At, RepairTimeProvenance.VerifiedOnline);
        grant.RecordFirstStart(origin, "hash", At, At.AddHours(2), RepairTimeProvenance.VerifiedOnline);
        Assert.Equal(At.AddHours(24), grant.ExpiresAt);
        Assert.Equal(At, grant.FirstServerReceivedAt);
        Assert.Throws<InvalidOperationException>(() => grant.RecordFirstStart(Guid.NewGuid(), "hash2", At.AddHours(2), At.AddHours(2), RepairTimeProvenance.VerifiedOnline));
    }

    [Fact]
    public void UncertainOfflineStartRetainsHistoryWithoutGrantingAuthority()
    {
        var grant = Grant(); var origin = Guid.NewGuid();
        grant.RecordFirstStart(origin, "hash", At, At.AddHours(30), RepairTimeProvenance.Uncertain);
        Assert.Equal(origin, grant.FirstStartOriginId);
        Assert.Null(grant.ExpiresAt);
        Assert.Equal(RepairEligibilityReason.UnprovenTime, grant.Evaluate(At.AddHours(1), Facts()).Reason);
        grant.VerifyOriginalStart(origin, "hash", At, At, RepairTimeProvenance.VerifiedOffline);
        Assert.Equal(At.AddHours(24), grant.ExpiresAt);
        Assert.False(grant.Evaluate(At.AddHours(30), Facts()).Eligible);
    }

    [Fact]
    public void CoverageUnknownAndLocationAffectedPreventExecution()
    {
        var grant = Grant(); Start(grant);
        Assert.Equal(RepairEligibilityReason.CoverageUnknown, grant.Evaluate(At.AddHours(1), Facts() with { Coverage = RepairFactState.Unknown }).Reason);
        Assert.Equal(RepairEligibilityReason.LocationUnverified, grant.Evaluate(At.AddHours(1), Facts() with { LocationVerified = false }).Reason);
        Assert.Equal(RepairEligibilityReason.PolicyUnavailable, grant.Evaluate(At.AddHours(1), Facts() with { PolicyCurrentAuthority = false }).Reason);
        Assert.Equal(RepairEligibilityReason.Revoked, grant.Evaluate(At.AddHours(1), Facts() with { KnownRevoked = true }).Reason);
    }

    [Fact]
    public void AssignmentAndPinnedVersionsMustMatchBeforeRepair()
    {
        var grant = Grant(); Start(grant);
        Assert.Equal(RepairEligibilityReason.ScopeMismatch, grant.Evaluate(At.AddHours(1), Facts() with { AssignmentId = Guid.NewGuid() }).Reason);
        Assert.Equal(RepairEligibilityReason.ScopeMismatch, grant.Evaluate(At.AddHours(1), Facts() with { LocationVersion = "v2" }).Reason);
        Assert.Equal(RepairEligibilityReason.ScopeMismatch, grant.Evaluate(At.AddHours(1), Facts() with { PolicyRevisionId = Guid.NewGuid() }).Reason);
    }

    [Fact]
    public void NormalRequiresSupervisorApprovalThenPmAssignmentThenSupervisorFinal()
    {
        var item = Item(RepairMode.Normal);
        Assert.Throws<InvalidOperationException>(() => item.Assign(Crew, Pm, UserRoleCode.ProjectManager, At));
        Assert.Throws<InvalidOperationException>(() => item.Approve(Supervisor, UserRoleCode.ProjectManager, At));
        item.Approve(Supervisor, UserRoleCode.Supervisor, At);
        item.Assign(Crew, Pm, UserRoleCode.ProjectManager, At);
        item.Start(Crew, At.AddMinutes(1));
        var attempt = Attempt(item, true, [Evidence(RepairEvidencePurpose.After)]);
        item.Submit(attempt);
        item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2));
        Assert.False(item.IsEffectivelyConfirmed);
        Assert.Throws<InvalidOperationException>(() => item.Confirm(Guid.NewGuid(), Pm, UserRoleCode.ProjectManager, "accepted", At.AddHours(3)));
        item.Confirm(Guid.NewGuid(), Supervisor, UserRoleCode.Supervisor, "accepted", At.AddHours(3));
        Assert.True(item.IsEffectivelyConfirmed);
    }

    [Fact]
    public void FastTrackPmFinalDoesNotRequireSupervisorApproval()
    {
        var item = Item(RepairMode.FastTrack);
        item.Assign(Crew, Pm, UserRoleCode.ProjectManager, At); item.Start(Crew, At.AddMinutes(1));
        item.Submit(Attempt(item, true, [Evidence(RepairEvidencePurpose.After)]));
        item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2));
        item.Confirm(Guid.NewGuid(), Pm, UserRoleCode.ProjectManager, "accepted", At.AddHours(3));
        Assert.True(item.IsEffectivelyConfirmed);
    }

    [Fact]
    public void PerformedClaimWithPendingAfterEvidenceIsNotAccepted()
    {
        var item = Assigned(RepairMode.FastTrack); item.Start(Crew, At.AddMinutes(1));
        item.Submit(Attempt(item, true, [Evidence(RepairEvidencePurpose.After, false)]));
        Assert.Equal(RepairPresentationState.ReportedAwaitingReview, item.Presentation);
        Assert.Throws<InvalidOperationException>(() => item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2)));
        Assert.False(item.IsEffectivelyConfirmed);
    }

    [Fact]
    public void UnperformedSubmissionNeedsReasonAndCannotBeAccepted()
    {
        var item = Assigned(RepairMode.FastTrack); item.Start(Crew, At.AddMinutes(1));
        Assert.Throws<ArgumentException>(() => Attempt(item, false, [], null));
        item.Submit(Attempt(item, false, [], "conditions differ"));
        Assert.Equal(RepairPresentationState.Unrepaired, item.Presentation);
        Assert.Throws<InvalidOperationException>(() => item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2)));
    }

    [Fact]
    public void CancellationNeverResolvesMandatoryObligation()
    {
        var obligation = Obligation(); var item = RepairItem.Propose(Guid.NewGuid(), obligation, RepairMode.FastTrack, Pm, UserRoleCode.ProjectManager, At);
        item.Cancel(Pm, UserRoleCode.ProjectManager, "not started", At.AddHours(1), null);
        Assert.False(obligation.IsResolved);
        Assert.False(RepairPackageCompletion.AllMandatoryResolved([obligation]));
    }

    [Fact]
    public void InProgressCancellationRequiresPerformedScopeAndHandover()
    {
        var item = Assigned(RepairMode.FastTrack); item.Start(Crew, At.AddMinutes(1));
        Assert.Throws<InvalidOperationException>(() => item.Cancel(Pm, UserRoleCode.ProjectManager, "reassign", At.AddHours(1), null));
        var handover = new RepairWorkHandover(Guid.NewGuid(), Crew, Guid.NewGuid(), "none yet", "safe stop", At.AddHours(1));
        item.Cancel(Pm, UserRoleCode.ProjectManager, "reassign", At.AddHours(1), handover);
        Assert.Equal(handover, item.Handover);
    }

    [Fact]
    public void SubmittedHistoryCannotBeCancelledOrReplaced()
    {
        var item = Assigned(RepairMode.FastTrack); item.Start(Crew, At.AddMinutes(1)); var attempt = Attempt(item, true, []);
        item.Submit(attempt);
        Assert.Throws<InvalidOperationException>(() => item.Cancel(Pm, UserRoleCode.ProjectManager, "erase", At.AddHours(2), null));
        Assert.Throws<InvalidOperationException>(() => item.Submit(Attempt(item, true, [])));
        Assert.Same(attempt, Assert.Single(item.Attempts));
    }

    [Fact]
    public void CorrectionPreservesOriginalAcceptanceAndReopensSameObligation()
    {
        var obligation = Obligation(); var item = Accepted(obligation); var decision = item.EffectiveDecision!;
        obligation.Resolve(decision);
        var correction = item.Correct(Guid.NewGuid(), decision.Id, Pm, UserRoleCode.ProjectManager, "mistaken acceptance", At.AddHours(4), CorrectionPermission(item));
        obligation.Reopen(correction);
        Assert.False(item.IsEffectivelyConfirmed); Assert.False(obligation.IsResolved);
        Assert.Same(decision, item.Decisions[0]);
        Assert.Equal(decision.Id, correction.SupersedesDecisionId);
        Assert.Equal(RepairRecurrenceKind.CorrectionRequired, RepairRecurrence.Classify(item));
    }

    [Fact]
    public void GenuineAcceptedRepairAllowsNewLinkedDefectButCorrectionDoesNot()
    {
        var item = Accepted(Obligation());
        var recurrence = RepairRecurrence.LinkNewDefect(Guid.NewGuid(), item, Pm, "reappeared", At.AddHours(5));
        Assert.Equal(item.DefectId, recurrence.PreviousDefectId);
        Assert.Equal(item.EffectiveDecision!.Id, recurrence.PreviousAcceptanceId);
        Assert.Throws<InvalidOperationException>(() => RepairRecurrence.LinkNewDefect(item.DefectId, item, Pm, "same id", At.AddHours(5)));
    }

    [Fact]
    public void CorrectionMustSupersedeEffectiveDecisionAndHaveExplicitAuthority()
    {
        var item = Accepted(Obligation());
        Assert.Throws<InvalidOperationException>(() => item.Correct(Guid.NewGuid(), Guid.NewGuid(), Pm, UserRoleCode.ProjectManager, "wrong head", At.AddHours(4), CorrectionPermission(item)));
        Assert.Throws<InvalidOperationException>(() => item.Correct(Guid.NewGuid(), item.EffectiveDecision!.Id, Supervisor, UserRoleCode.Supervisor, "unadopted authority", At.AddHours(4), null));
        Assert.Single(item.Decisions);
    }

    [Fact]
    public void MixedPackageNeedsEveryMandatoryObligationAndDoesNotCountAttempts()
    {
        var a = Obligation(); var b = Obligation(); var item = Accepted(a); a.Resolve(item.EffectiveDecision!);
        Assert.False(RepairPackageCompletion.AllMandatoryResolved([a, b]));
        Assert.True(RepairPackageCompletion.AllMandatoryResolved([a]));
        Assert.Throws<InvalidOperationException>(() => RepairPackageCompletion.EnsureDefectClosable(Defect, [a, b]));
    }

    [Fact]
    public void SafetyInstallationKeepsMonitoringAndFirstCheckWithin24Hours()
    {
        var safety = Safety();
        safety.Install(Guid.NewGuid(), Crew, At, At.AddHours(12));
        Assert.True(safety.RequiresMonitoring); Assert.Equal(At.AddHours(12), safety.FirstCheckDueAt);
        Assert.Throws<ArgumentException>(() => Safety().Install(Guid.NewGuid(), Crew, At, At.AddHours(24).AddTicks(1)));
    }

    [Fact]
    public void SafetyResponsibilityTransferRetainsPriorActorAndRequiresHandover()
    {
        var safety = Safety(); var next = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => safety.Transfer(Guid.NewGuid(), next, Pm, " ", "reason", At));
        safety.Transfer(Guid.NewGuid(), next, Pm, "installed barriers, check due", "crew changed", At);
        Assert.Equal(next, safety.ResponsibleActorId);
        Assert.Equal(Crew, Assert.Single(safety.Transfers).PreviousActorId);
        Assert.Equal("installed barriers, check due", safety.Transfers[0].Handover);
    }

    [Fact]
    public void EvidenceCallerMutationCannotRewriteSubmittedAttempt()
    {
        var item = Assigned(RepairMode.FastTrack); var files = new[] { Evidence(RepairEvidencePurpose.After) };
        var attempt = Attempt(item, true, files); var original = files[0]; files[0] = Evidence(RepairEvidencePurpose.Before);
        Assert.Equal(original, Assert.Single(attempt.Evidence));
    }

    [Fact]
    public void ReusedBeforeRequiresPmDecisionAndAfterCannotReuseOldSource()
    {
        var item = Assigned(RepairMode.FastTrack);
        Assert.Throws<ArgumentException>(() => Attempt(item, true, [Evidence(RepairEvidencePurpose.Before) with { SourceKind = "REPORTER", ReusedSource = true }]));
        Assert.Throws<ArgumentException>(() => Attempt(item, true, [Evidence(RepairEvidencePurpose.After) with { SourceKind = "REPORTER", ReuseDecisionId = Guid.NewGuid(), ReusedSource = true }]));
        var source = Evidence(RepairEvidencePurpose.Before) with { SourceKind = "REPORTER", ReuseDecisionId = Guid.NewGuid(), ReusedSource = true };
        var attempt = Attempt(item, true, [source, Evidence(RepairEvidencePurpose.After)]);
        Assert.Equal(source.ReuseDecisionId, attempt.Evidence[0].ReuseDecisionId);
    }

    [Fact]
    public void UncertainPerformedClaimCannotBeReviewedAsValidExecution()
    {
        var item = Assigned(RepairMode.FastTrack); item.Start(Crew, At.AddMinutes(1));
        var attempt = RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), "hash", item.Id, item.ObligationId, Project, Defect,
            Crew, TaskId, AssignmentId, Guid.NewGuid(), "location-v1", PolicyId, true, null, At.AddMinutes(1), At.AddHours(1),
            At.AddHours(30), RepairTimeProvenance.Uncertain, [Evidence(RepairEvidencePurpose.After)]);
        item.Submit(attempt);
        Assert.Throws<InvalidOperationException>(() => item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(31)));
        Assert.Same(attempt, Assert.Single(item.Attempts));
    }

    [Fact]
    public void CrossObligationDecisionCannotResolveAnotherObligation()
    {
        var first = Obligation(); var second = Obligation(); var item = Accepted(first);
        Assert.Throws<InvalidOperationException>(() => second.Resolve(item.EffectiveDecision!));
        Assert.False(second.IsResolved);
    }

    [Fact]
    public void InvalidScopeBoundsAndDuplicatedMeasurementCodesFailValidation()
    {
        Assert.Throws<ArgumentException>(() => Scope(10, 0));
        Assert.Throws<ArgumentException>(() => RepairPolicyRevision.Publish(Guid.NewGuid(), Project, 1, Pm, UserRoleCode.ProjectManager,
            At, "CRACK", "checklist", [new("width", "mm", 0, 3), new("width", "mm", 0, 4)], []));
        Assert.Throws<ArgumentException>(() => Policy().Evaluate([new("width", 1, "mm"), new("width", 2, "mm")], []));
    }

    [Fact]
    public void MismatchingOriginalStartProofCannotChangeStoredTime()
    {
        var grant = Grant(); var origin = Guid.NewGuid();
        grant.RecordFirstStart(origin, "hash", At, At.AddHours(30), RepairTimeProvenance.Uncertain);
        Assert.Throws<InvalidOperationException>(() => grant.VerifyOriginalStart(origin, "hash", At.AddHours(10), At.AddHours(10), RepairTimeProvenance.VerifiedOffline));
        Assert.Equal(At, grant.FirstStartedAt); Assert.Null(grant.ExpiresAt);
    }

    [Fact]
    public void OnlineStartUsesServerOriginWhileRetainingClientClaim()
    {
        var grant = Grant();
        grant.RecordFirstStart(Guid.NewGuid(), "hash", At.AddHours(48), At.AddHours(12), RepairTimeProvenance.VerifiedOnline);
        Assert.Equal(At.AddHours(48), grant.FirstStartedAt);
        Assert.Equal(At.AddHours(12), grant.VerifiedStartedAt); Assert.Equal(At.AddHours(36), grant.ExpiresAt);
    }

    [Fact]
    public void NormalAttemptCanOmitFastTrackAuthorizationAndPolicy()
    {
        var item = Assigned(RepairMode.Normal); item.Start(Crew, At.AddMinutes(1));
        var attempt = RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), "hash", item.Id, item.ObligationId, Project, Defect,
            Crew, TaskId, AssignmentId, null, "location-v1", null, true, null, At.AddMinutes(1), At.AddHours(1),
            At.AddHours(1), RepairTimeProvenance.VerifiedOnline, [Evidence(RepairEvidencePurpose.After)]);
        item.Submit(attempt); item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2));
        Assert.Null(attempt.AuthorizationId); Assert.Null(attempt.PolicyRevisionId);
    }

    [Fact]
    public void UnknownExecutionTimesRemainIntakeFactsWithoutFakeZeroTimestamp()
    {
        var item = Assigned(RepairMode.FastTrack); item.Start(Crew, At.AddMinutes(1));
        var attempt = RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), "hash", item.Id, item.ObligationId, Project, Defect,
            Crew, TaskId, AssignmentId, Guid.NewGuid(), "location-v1", PolicyId, true, null, null, null,
            At.AddHours(1), RepairTimeProvenance.Uncertain, []);
        item.Submit(attempt);
        Assert.Null(attempt.StartedAt); Assert.Null(attempt.FinishedAt);
        Assert.Throws<InvalidOperationException>(() => item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2)));
    }

    [Fact]
    public void PolicyRevocationAppendsFactWithoutRewritingPublishedCriteria()
    {
        var policy = Policy(); var id = Guid.NewGuid();
        policy.Revoke(id, Pm, UserRoleCode.ProjectManager, "field conditions changed", At.AddHours(1));
        Assert.True(policy.IsRevoked); Assert.Equal(At, policy.PublishedAt);
        Assert.Equal(RepairEligibilityReason.PolicyUnavailable, policy.Evaluate([new("width", 1, "mm")], []).Reason);
        Assert.Equal(3, policy.Measurements[0].Maximum);
        Assert.Equal(id, Assert.Single(policy.Revocations).Id);
        Assert.Throws<InvalidOperationException>(() => policy.Revoke(Guid.NewGuid(), Pm, UserRoleCode.ProjectManager, "new reason", At.AddHours(2)));
    }

    [Fact]
    public void CorrectionActorAndItemMustMatchExplicitPolicyFacts()
    {
        var item = Accepted(Obligation()); var permission = CorrectionPermission(item);
        Assert.Throws<InvalidOperationException>(() => item.Correct(Guid.NewGuid(), item.EffectiveDecision!.Id,
            Pm, UserRoleCode.ProjectManager, "mismatched item", At.AddHours(4), permission with { ItemId = Guid.NewGuid() }));
        Assert.Throws<InvalidOperationException>(() => item.Correct(Guid.NewGuid(), item.EffectiveDecision!.Id,
            Pm, UserRoleCode.ProjectManager, "mismatched actor", At.AddHours(4), permission with { ActorId = Supervisor }));
        Assert.True(item.IsEffectivelyConfirmed); Assert.Single(item.Decisions);
    }

    [Fact]
    public void ObligationHistoryRetainsOriginalResolutionAfterCorrection()
    {
        var obligation = Obligation(); var item = Accepted(obligation); var decision = item.EffectiveDecision!; obligation.Resolve(decision);
        var correction = item.Correct(Guid.NewGuid(), decision.Id, Pm, UserRoleCode.ProjectManager, "wrong acceptance", At.AddHours(4), CorrectionPermission(item));
        obligation.Reopen(correction);
        Assert.Equal(2, obligation.ResolutionHistory.Count);
        Assert.Equal(decision.Id, obligation.ResolutionHistory[0].DecisionId);
        Assert.Equal(decision.Id, obligation.ResolutionHistory[1].SupersedesDecisionId);
    }

    [Fact]
    public void PendingEvidenceMetadataNeedsNoFakeVersionHashOrCaptureTime()
    {
        var item = Assigned(RepairMode.FastTrack); item.Start(Crew, At.AddMinutes(1));
        var pending = new RepairEvidenceReference(Guid.NewGuid(), null, null, RepairEvidencePurpose.After,
            false, true, "CREW_CAPTURE", Guid.NewGuid(), null, null, false);
        item.Submit(Attempt(item, true, [pending]));
        Assert.Null(Assert.Single(item.Attempts[0].Evidence).CapturedAt);
        Assert.Throws<InvalidOperationException>(() => item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2)));
    }

    [Theory]
    [InlineData(-12)]
    [InlineData(48)]
    public void BadClockClaimsAreRetainedAsUncertainWithoutGrantingWindow(int hours)
    {
        var grant = Grant(); var claim = At.AddHours(hours);
        grant.RecordFirstStart(Guid.NewGuid(), "hash", claim, At.AddHours(1), RepairTimeProvenance.Uncertain);
        Assert.Equal(claim, grant.FirstStartedAt); Assert.Null(grant.VerifiedStartedAt); Assert.Null(grant.ExpiresAt);
        Assert.False(grant.Evaluate(At.AddHours(2), Facts()).Eligible);
    }

    [Fact]
    public void OfflineProofStoresVerifiedUtcWithoutRewritingOriginalClaim()
    {
        var grant = Grant(); var origin = Guid.NewGuid(); var claim = At.AddHours(-12);
        grant.RecordFirstStart(origin, "hash", claim, At.AddHours(30), RepairTimeProvenance.Uncertain);
        grant.VerifyOriginalStart(origin, "hash", claim, At, RepairTimeProvenance.VerifiedOffline);
        Assert.Equal(claim, grant.FirstStartedAt); Assert.Equal(At, grant.VerifiedStartedAt); Assert.Equal(At.AddHours(24), grant.ExpiresAt);
        Assert.Throws<ArgumentException>(() => Grant().RecordFirstStart(Guid.NewGuid(), "hash", claim, At, RepairTimeProvenance.VerifiedOffline));
    }

    [Fact]
    public void AfterCaptureCannotPredateCompletedRepairOrReuseBeforeFile()
    {
        var item = Assigned(RepairMode.FastTrack);
        Assert.Throws<ArgumentException>(() => Attempt(item, true, [Evidence(RepairEvidencePurpose.After) with { CapturedAt = At.AddMinutes(30) }]));
        var before = Evidence(RepairEvidencePurpose.Before);
        var after = Evidence(RepairEvidencePurpose.After) with { FileId = before.FileId };
        Assert.Throws<ArgumentException>(() => Attempt(item, true, [before, after]));
    }

    [Fact]
    public void FreshAfterUsesChecklistSourceFactsWithoutInventingCrewOnlyRule()
    {
        var item = Assigned(RepairMode.FastTrack);
        var fresh = Evidence(RepairEvidencePurpose.After) with { SourceKind = "DRONE_CAPTURE", ReusedSource = false };
        Assert.Equal("DRONE_CAPTURE", Assert.Single(Attempt(item, true, [fresh]).Evidence).SourceKind);
        Assert.Throws<ArgumentException>(() => Attempt(item, true, [Evidence(RepairEvidencePurpose.Before) with { ReusedSource = true }]));
    }

    [Fact]
    public void ProvenOriginCannotBeReplacedByLaterProofOrNewHash()
    {
        var grant = Grant(); var origin = Guid.NewGuid();
        grant.RecordFirstStart(origin, "hash", At, At.AddHours(30), RepairTimeProvenance.Uncertain);
        grant.VerifyOriginalStart(origin, "hash", At, At, RepairTimeProvenance.VerifiedOffline);
        Assert.Throws<InvalidOperationException>(() => grant.VerifyOriginalStart(origin, "hash", At, At.AddHours(12), RepairTimeProvenance.VerifiedOffline));
        Assert.Throws<InvalidOperationException>(() => grant.VerifyOriginalStart(origin, "hash2", At, At, RepairTimeProvenance.VerifiedOffline));
        Assert.Throws<InvalidOperationException>(() => grant.VerifyOriginalStart(Guid.NewGuid(), "hash", At, At, RepairTimeProvenance.VerifiedOffline));
        Assert.Equal(At.AddHours(24), grant.ExpiresAt);
    }

    [Theory]
    [InlineData(RepairMode.FastTrack, UserRoleCode.Supervisor)]
    [InlineData(RepairMode.Normal, UserRoleCode.ProjectManager)]
    public void AdoptedCorrectionRoleCannotBeOverriddenByMatchingSuppliedFacts(RepairMode mode, UserRoleCode wrongRole)
    {
        var obligation = Obligation(); var item = Accepted(obligation, mode); var actor = Guid.NewGuid();
        var matching = new RepairCorrectionAuthority(Guid.NewGuid(), item.Id, actor, wrongRole, "MATCHING_FACTS_DO_NOT_OVERRIDE_MODE");
        Assert.Throws<InvalidOperationException>(() => item.Correct(Guid.NewGuid(), item.EffectiveDecision!.Id, actor, wrongRole,
            "wrong mode", At.AddHours(4), matching));
        Assert.True(item.IsEffectivelyConfirmed); Assert.Single(item.Decisions);
    }

    [Theory]
    [InlineData(RepairMode.FastTrack, UserRoleCode.ProjectManager)]
    [InlineData(RepairMode.Normal, UserRoleCode.Supervisor)]
    public void DifferentCurrentIndividualWithCorrectRoleCanCorrect(RepairMode mode, UserRoleCode role)
    {
        var obligation = Obligation(); var item = Accepted(obligation, mode); var original = item.EffectiveDecision!; var actor = Guid.NewGuid();
        var authority = new RepairCorrectionAuthority(Guid.NewGuid(), item.Id, actor, role, "TARGET_CONFIRMED_ROLE_TEST_SCOPE_FACTS");
        var correction = item.Correct(Guid.NewGuid(), original.Id, actor, role, "wrong acceptance", At.AddHours(4), authority);
        Assert.NotEqual(original.ActorId, correction.ActorId); Assert.False(item.IsEffectivelyConfirmed);
    }

    [Theory]
    [InlineData(UserRoleCode.ProjectManager)]
    [InlineData(UserRoleCode.RepairCrew)]
    public void ReviewRequestPreservesEffectiveAcceptanceAndObligation(UserRoleCode role)
    {
        var obligation = Obligation(); var item = Accepted(obligation); var original = item.EffectiveDecision!; obligation.Resolve(original);
        var request = item.RequestReview(Guid.NewGuid(), role == UserRoleCode.RepairCrew ? Crew : Guid.NewGuid(), role,
            "check AFTER provenance", At.AddHours(4));
        Assert.Same(original, item.EffectiveDecision); Assert.True(obligation.IsResolved); Assert.True(item.IsEffectivelyConfirmed);
        Assert.Single(item.Decisions); Assert.Single(item.ReviewRequests); Assert.Equal(original.Id, request.DecisionId);
    }

    [Fact]
    public void SupervisorRequestCannotProvideAnInferredFastTrackOverride()
    {
        var item = Accepted(Obligation());
        Assert.Throws<InvalidOperationException>(() => item.RequestReview(Guid.NewGuid(), Supervisor, UserRoleCode.Supervisor, "override", At.AddHours(4)));
        Assert.Empty(item.ReviewRequests); Assert.True(item.IsEffectivelyConfirmed);
    }

    [Fact]
    public void EffectiveCorrectionResultControlsUiWithoutRewritingPerformedClaim()
    {
        var item = Accepted(Obligation()); var original = item.Attempts[0];
        var basis = RepairCorrectionBasis.Create("capture belongs to another work item", [original.Evidence[0]]);
        item.CorrectResult(Guid.NewGuid(), item.EffectiveDecision!.Id, Pm, UserRoleCode.ProjectManager, "not performed",
            At.AddHours(4), CorrectionPermission(item), RepairPresentationState.Unrepaired, basis);
        Assert.Equal(RepairPresentationState.Unrepaired, item.Presentation); Assert.True(original.Performed);
        Assert.Same(original, item.Attempts[0]); Assert.Single(item.Attempts);
    }

    [Fact]
    public void CorrectionChainMustSupersedeLatestHeadAndKeepEveryPriorDecision()
    {
        var item = Accepted(Obligation()); var first = item.EffectiveDecision!;
        var second = item.Correct(Guid.NewGuid(), first.Id, Pm, UserRoleCode.ProjectManager, "insufficient", At.AddHours(4), CorrectionPermission(item));
        var third = item.CorrectResult(Guid.NewGuid(), second.Id, Pm, UserRoleCode.ProjectManager, "actually unperformed",
            At.AddHours(5), CorrectionPermission(item), RepairPresentationState.Unrepaired, RepairCorrectionBasis.Create("different work location", []));
        Assert.Equal(3, item.Decisions.Count); Assert.Same(first, item.Decisions[0]); Assert.Equal(second.Id, third.SupersedesDecisionId);
        Assert.Throws<InvalidOperationException>(() => item.Correct(Guid.NewGuid(), first.Id, Pm, UserRoleCode.ProjectManager,
            "stale head", At.AddHours(6), CorrectionPermission(item)));
    }

    [Fact]
    public void CorrectionEffectsContinueSameObligationWithoutNewAttemptOrGrant()
    {
        var obligation = Obligation(); var item = Accepted(obligation); var initial = item.EffectiveDecision!; obligation.Resolve(initial);
        var attempt = item.Attempts[0]; var basis = RepairCorrectionBasis.Create("AFTER evidence does not prove completed scope", []);
        var correction = RepairCorrectionEffects.Apply(item, obligation, Guid.NewGuid(), initial.Id, Pm, UserRoleCode.ProjectManager,
            "wrong acceptance", At.AddHours(4), CorrectionPermission(item), RepairPresentationState.ReportedAwaitingReview, basis);
        Assert.False(obligation.IsResolved); Assert.False(item.IsEffectivelyConfirmed); Assert.Equal(2, obligation.ResolutionHistory.Count);
        Assert.Equal(correction.Id, obligation.ResolutionHistory[1].DecisionId); Assert.Same(attempt, Assert.Single(item.Attempts));
        Assert.False(RepairPackageCompletion.AllMandatoryResolved([obligation]));
    }

    [Fact]
    public void EffectsRejectMismatchedObligationBeforeMutatingItem()
    {
        var obligation = Obligation(); var item = Accepted(obligation); obligation.Resolve(item.EffectiveDecision!);
        Assert.Throws<InvalidOperationException>(() => RepairCorrectionEffects.Apply(item, Obligation(), Guid.NewGuid(), item.EffectiveDecision!.Id,
            Pm, UserRoleCode.ProjectManager, "wrong target", At.AddHours(4), CorrectionPermission(item), RepairPresentationState.Unrepaired,
            RepairCorrectionBasis.Create("wrong location", [])));
        Assert.True(item.IsEffectivelyConfirmed); Assert.Single(item.Decisions); Assert.True(obligation.IsResolved);
    }

    [Fact]
    public void BasisEvidenceIsImmutableAndNeedsActualVerifiedRelatedFacts()
    {
        var source = Evidence(RepairEvidencePurpose.After); var evidence = new[] { source };
        var basis = RepairCorrectionBasis.Create("wrong AFTER", evidence); evidence[0] = Evidence(RepairEvidencePurpose.Before);
        Assert.Equal(source, Assert.Single(basis.Evidence));
        Assert.Throws<ArgumentException>(() => RepairCorrectionBasis.Create(" ", []));
        Assert.Throws<ArgumentException>(() => RepairCorrectionBasis.Create("pending", [source with { Verified = false }]));
    }

    [Fact]
    public void CorrectedConfirmedResultDoesNotRequireAnInventedAdditionalApprovalChain()
    {
        var obligation = Obligation(); var item = Accepted(obligation); obligation.Resolve(item.EffectiveDecision!);
        var basis = RepairCorrectionBasis.Create("same accepted scope, corrected source reference", [item.Attempts[0].Evidence[0]]);
        var correction = RepairCorrectionEffects.Apply(item, obligation, Guid.NewGuid(), item.EffectiveDecision!.Id,
            Pm, UserRoleCode.ProjectManager, "correct source", At.AddHours(4), CorrectionPermission(item), RepairPresentationState.Confirmed, basis);
        Assert.True(item.IsEffectivelyConfirmed); Assert.True(obligation.IsResolved);
        Assert.Equal(correction.Id, obligation.EffectiveResolutionDecisionId); Assert.Equal(2, item.Decisions.Count);
        Assert.Single(item.Attempts);
    }

    private static RepairActualScope Scope(decimal from, decimal to, string version = "location-v1", string label = "main")
        => RepairActualScope.Create(Guid.NewGuid(), Road, version, label, from, to, 0, 3);
    private static RepairObligation Obligation() => RepairObligation.Create(Guid.NewGuid(), Project, Defect, RepairObligationKind.FormalRepair, true, Scope(0, 10));
    private static RepairPolicyRevision Policy() => RepairPolicyRevision.Publish(PolicyId, Project, 1, Pm, UserRoleCode.ProjectManager, At, "CRACK", "checklist-v1", [new("width", "mm", 0, 3)], ["unsafe"]);
    private static readonly Guid TaskId = Guid.NewGuid(), AssignmentId = Guid.NewGuid(), PolicyId = Guid.NewGuid();
    private static RepairExecutionAuthorization Grant(RepairTaskMode permission = RepairTaskMode.ConditionalFastTrack)
        => RepairExecutionAuthorization.Issue(Guid.NewGuid(), Project, Defect, TaskId, AssignmentId, Crew, Pm, At, "assigned FT", permission, "location-v1", PolicyId);
    private static void Start(RepairExecutionAuthorization grant) => grant.RecordFirstStart(Guid.NewGuid(), "hash", At, At, RepairTimeProvenance.VerifiedOnline);
    private static RepairExecutionFacts Facts() => new(Project, Defect, TaskId, AssignmentId, Crew, "location-v1", PolicyId, true, true, RepairFactState.Confirmed, true, true, true, false, false);
    private static RepairItem Item(RepairMode mode) => RepairItem.Propose(Guid.NewGuid(), Obligation(), mode, Pm, UserRoleCode.ProjectManager, At);
    private static RepairItem Assigned(RepairMode mode) { var item = Item(mode); if (mode == RepairMode.Normal) item.Approve(Supervisor, UserRoleCode.Supervisor, At); item.Assign(Crew, Pm, UserRoleCode.ProjectManager, At); return item; }
    private static RepairEvidenceReference Evidence(RepairEvidencePurpose purpose, bool verified = true) => new(Guid.NewGuid(), "file-v1", "sha256", purpose, verified, true, "CREW_CAPTURE", Guid.NewGuid(), purpose == RepairEvidencePurpose.After ? At.AddHours(1).AddMinutes(1) : At.AddMinutes(2), null, false);
    private static RepairAttempt Attempt(RepairItem item, bool performed, IReadOnlyList<RepairEvidenceReference> evidence, string? reason = null)
        => RepairAttempt.Submit(Guid.NewGuid(), Guid.NewGuid(), "payload-hash", item.Id, item.ObligationId, Project, Defect, Crew, TaskId, AssignmentId,
            Guid.NewGuid(), "location-v1", PolicyId, performed, reason, At.AddMinutes(1), At.AddHours(1), At.AddHours(1), RepairTimeProvenance.VerifiedOnline, evidence);
    private static RepairItem Accepted(RepairObligation obligation, RepairMode mode = RepairMode.FastTrack)
    { var item = RepairItem.Propose(Guid.NewGuid(), obligation, mode, Pm, UserRoleCode.ProjectManager, At); if(mode == RepairMode.Normal) item.Approve(Supervisor, UserRoleCode.Supervisor, At); item.Assign(Crew, Pm, UserRoleCode.ProjectManager, At); item.Start(Crew, At.AddMinutes(1)); item.Submit(Attempt(item, true, [Evidence(RepairEvidencePurpose.After)])); item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2)); item.Confirm(Guid.NewGuid(), mode == RepairMode.Normal ? Supervisor : Pm, mode == RepairMode.Normal ? UserRoleCode.Supervisor : UserRoleCode.ProjectManager, "accepted", At.AddHours(3)); return item; }
    private static TemporarySafetyMeasure Safety() => TemporarySafetyMeasure.Create(Guid.NewGuid(), Project, Defect, Guid.NewGuid(), Crew, "check every shift", "replace damaged barrier", "remove after formal repair");
    private static RepairCorrectionAuthority CorrectionPermission(RepairItem item)
        => new(Guid.NewGuid(), item.Id, Pm, UserRoleCode.ProjectManager, "TEST_FIXTURE_AUTHORITY_ONLY");
}
