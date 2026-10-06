using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairWorkflowTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
    private static readonly Guid Project = Guid.NewGuid(), Defect = Guid.NewGuid(), Road = Guid.NewGuid();
    private static readonly Guid Pm = Guid.NewGuid(), Crew = Guid.NewGuid(), TaskId = Guid.NewGuid(), Assignment = Guid.NewGuid(), StartId = Guid.NewGuid();
    private static readonly string Hash = new('a', 64);

    [Fact]
    public void PackageCannotHideWrongProjectOrDefectObligation()
    {
        var wrong = RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Defect, RepairObligationKind.FormalRepair, true, Scope(0, 10));
        Assert.Throws<InvalidOperationException>(() => RepairPackage.Create(Guid.NewGuid(), Project, Defect, [wrong]));
    }
    [Fact]
    public void PackageRequiresDistinctRealObligationsAndCopiesCollection()
    {
        var obligation = Obligation(); var source = new[] { obligation };
        Assert.Throws<ArgumentException>(() => RepairPackage.Create(Guid.NewGuid(), Project, Defect, [obligation, obligation]));
        var package = RepairPackage.Create(Guid.NewGuid(), Project, Defect, source); source[0] = Obligation();
        Assert.Same(obligation, Assert.Single(package.Obligations)); Assert.False(package.IsComplete);
    }
    [Fact]
    public void DifferentObligationOrScopeDisplayIdentityCannotDuplicateActivePhysicalWork()
    {
        var first = Obligation(); var second = Obligation(scope: Scope(5, 15));
        var package = RepairPackage.Create(Guid.NewGuid(), Project, Defect, [first, second]);
        package.AddItem(Item(first));
        Assert.Throws<InvalidOperationException>(() => package.AddItem(Item(second)));
        Assert.Single(package.Items);
    }
    [Fact]
    public void SafetyAndFormalWorkMayCoexistWithoutCompletingMandatorySafety()
    {
        var formal = Obligation(); var safety = Obligation(RepairObligationKind.TemporarySafety);
        var package = RepairPackage.Create(Guid.NewGuid(), Project, Defect, [formal, safety]);
        package.AddItem(Item(formal)); package.AddItem(Item(safety));
        Assert.Equal(2, package.Items.Count); Assert.False(package.IsComplete);
    }
    [Fact]
    public void CorrectionImmediatelyInvalidatesDerivedPackageCompletion()
    {
        var obligation = Obligation(); var item = Accepted(obligation); var package = RepairPackage.Create(Guid.NewGuid(), Project, Defect, [obligation]);
        package.AddItem(item); obligation.Resolve(item.EffectiveDecision!); Assert.True(package.IsComplete);
        RepairCorrectionEffects.Apply(item, obligation, Guid.NewGuid(), item.EffectiveDecision!.Id, Pm, UserRoleCode.ProjectManager,
            "wrong accepted scope", At.AddHours(4), new(Guid.NewGuid(), item.Id, Pm, UserRoleCode.ProjectManager, "TEST_CURRENT_MEMBERSHIP_FACTS"),
            RepairPresentationState.Unrepaired, RepairCorrectionBasis.Create("wrong location", []));
        Assert.False(package.IsComplete); Assert.False(obligation.IsResolved); Assert.Single(item.Attempts);
    }
    [Fact]
    public void CancelledItemAllowsExplicitReplacementWithoutRemovingObligation()
    {
        var obligation = Obligation(); var item = Item(obligation); var package = RepairPackage.Create(Guid.NewGuid(), Project, Defect, [obligation]);
        package.AddItem(item); item.Cancel(Pm, UserRoleCode.ProjectManager, "not started", At.AddHours(1), null);
        package.AddItem(Item(obligation)); Assert.Equal(2, package.Items.Count); Assert.False(package.IsComplete);
    }
    [Fact]
    public void IntakeConsumesActualH3RootAndNeverSubstitutesReceiptForOriginalAttempt()
    {
        var item = Item(Obligation()); var source = Submission(); var attempt = Attempt(item, source.OriginId);
        var intake = RepairAttemptIntake.Create(attempt, source);
        Assert.Same(source, intake.OriginalSubmission); Assert.Same(attempt, intake.OriginalAttempt);
        Assert.Equal(source.ServerReceivedAt, intake.OriginalReviewOrigin); Assert.False(intake.IsReady);
    }
    [Fact]
    public void IntakeRejectsWrongTaskHashOrOriginalCrewBeforeCreatingHistory()
    {
        var item = Item(Obligation()); var source = Submission(); var attempt = Attempt(item, source.OriginId);
        Assert.Throws<InvalidOperationException>(() => RepairAttemptIntake.Create(attempt, Submission(task: Guid.NewGuid())));
        Assert.Throws<InvalidOperationException>(() => RepairAttemptIntake.Create(attempt, Submission(origin: source.OriginId, hash: new string('b', 64))));
        Assert.Throws<InvalidOperationException>(() => RepairAttemptIntake.Create(attempt, Submission(origin: source.OriginId, actor: Guid.NewGuid())));
    }
    [Fact]
    public void SupplementUpdatesReadinessWithoutResettingReviewOriginOrOriginalPerformedClaim()
    {
        var item = Item(Obligation()); var original = Submission(); var attempt = Attempt(item, original.OriginId);
        var intake = RepairAttemptIntake.Create(attempt, original);
        var next = Submission(root: original.Id, parent: original.Id, revision: 2, received: At.AddHours(3), readiness: "READY");
        intake.Supplement(next); Assert.True(intake.IsReady); Assert.Equal(original.ServerReceivedAt, intake.OriginalReviewOrigin);
        Assert.Same(attempt, intake.OriginalAttempt); Assert.True(attempt.Performed); Assert.Equal(2, intake.Submissions.Count);
    }
    [Fact]
    public void SupplementCannotForkRootSkipHeadOrReplaceFirstStart()
    {
        var source = Submission(); var intake = RepairAttemptIntake.Create(Attempt(Item(Obligation()), source.OriginId), source);
        Assert.Throws<InvalidOperationException>(() => intake.Supplement(Submission()));
        Assert.Throws<InvalidOperationException>(() => intake.Supplement(Submission(root: source.Id, parent: source.Id, revision: 3)));
        Assert.Throws<InvalidOperationException>(() => intake.Supplement(Submission(root: source.Id, parent: source.Id, revision: 2, start: Guid.NewGuid())));
        Assert.Single(intake.Submissions);
    }
    [Fact]
    public void SupplementCannotRewritePhysicalPerformedClaimIntoAnotherAttempt()
    {
        var source = Submission(); var intake = RepairAttemptIntake.Create(Attempt(Item(Obligation()), source.OriginId), source);
        Assert.Throws<InvalidOperationException>(() => intake.Supplement(Submission(root: source.Id, parent: source.Id, revision: 2,
            payload: "{\"captureType\":\"REPAIR_CLAIM\",\"repaired\":false,\"unrepairedReason\":\"not done\"}")));
        Assert.Single(intake.Submissions); Assert.True(intake.OriginalAttempt.Performed);
    }
    [Fact]
    public void SupplementCannotBackdateServerIntake()
    {
        var source = Submission(); var intake = RepairAttemptIntake.Create(Attempt(Item(Obligation()), source.OriginId), source);
        Assert.Throws<InvalidOperationException>(() => intake.Supplement(Submission(root: source.Id, parent: source.Id, revision: 2, received: At)));
    }
    private static RepairActualScope Scope(decimal from, decimal to) => RepairActualScope.Create(Guid.NewGuid(), Road, "location-v1", "main", from, to, 0, 3);
    [Fact]
    public void PhysicalRepairClaimAfterFormalMeasurementIntakePreservesFirstReviewOrigin()
    {
        var root = Submission(received: At, payload: "{\"captureType\":\"MEASUREMENT\"}");
        var physical = Submission(root: root.Id, parent: root.Id, revision: 2);
        var intake = RepairAttemptIntake.Create(Attempt(Item(Obligation()), physical.OriginId), new[] { root, physical });
        Assert.Same(root, intake.FormalIntakeRoot); Assert.Same(physical, intake.OriginalSubmission);
        Assert.Equal(At, intake.OriginalReviewOrigin); Assert.Equal(At.AddHours(1), intake.OriginalAttempt.ServerReceivedAt);
        Assert.Equal(2, intake.Submissions.Count);
    }
    [Fact]
    public void PhysicalClaimCannotInventRootOrSkipActualLineage()
    {
        var root = Submission(received: At, payload: "{\"captureType\":\"MEASUREMENT\"}");
        var physical = Submission(root: root.Id, parent: root.Id, revision: 3);
        var attempt = Attempt(Item(Obligation()), physical.OriginId);
        Assert.Throws<InvalidOperationException>(() => RepairAttemptIntake.Create(attempt, new[] { root, physical }));
    }
    [Fact]
    public void AssessmentLineageCannotReplaceTaskFirstStartOrHashOfPhysicalClaim()
    {
        var root = Submission(received: At, payload: "{\"captureType\":\"MEASUREMENT\"}");
        var physical = Submission(root: root.Id, parent: root.Id, revision: 2, start: Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => RepairAttemptIntake.Create(Attempt(Item(Obligation()), physical.OriginId), new[] { root, physical }));
    }
    [Fact]
    public void PhysicalClaimSupplementsDoNotResetEarlierMeasurementIntakeDue()
    {
        var root = Submission(received: At, payload: "{\"captureType\":\"MEASUREMENT\"}");
        var physical = Submission(root: root.Id, parent: root.Id, revision: 2);
        var intake = RepairAttemptIntake.Create(Attempt(Item(Obligation()), physical.OriginId), new[] { root, physical });
        intake.Supplement(Submission(root: root.Id, parent: physical.Id, revision: 3, received: At.AddHours(2), readiness: "READY"));
        Assert.Equal(At, intake.OriginalReviewOrigin); Assert.Same(physical, intake.OriginalSubmission);
        Assert.True(intake.IsReady); Assert.Equal(3, intake.Submissions.Count);
    }
    private static RepairObligation Obligation(RepairObligationKind kind = RepairObligationKind.FormalRepair, RepairActualScope? scope = null)
        => RepairObligation.Create(Guid.NewGuid(), Project, Defect, kind, true, scope ?? Scope(0, 10));
    private static RepairItem Item(RepairObligation obligation) => RepairItem.Propose(Guid.NewGuid(), obligation, RepairMode.FastTrack, Pm, UserRoleCode.ProjectManager, At);
    private static RepairAttempt Attempt(RepairItem item, Guid origin)
        => RepairAttempt.Submit(Guid.NewGuid(), origin, Hash, item.Id, item.ObligationId, Project, Defect, Crew, TaskId, Assignment,
            Guid.NewGuid(), "location-v1", Guid.NewGuid(), true, null, At.AddMinutes(1), At.AddHours(1), At.AddHours(1),
            RepairTimeProvenance.VerifiedOnline, [new(Guid.NewGuid(), "file-v1", Hash, RepairEvidencePurpose.After, true, true, "CREW_CAPTURE", Guid.NewGuid(), At.AddHours(1), null, false)]);
    private static RepairItem Accepted(RepairObligation obligation)
    {
        var item = Item(obligation); item.Assign(Crew, Pm, UserRoleCode.ProjectManager, At); item.Start(Crew, At.AddMinutes(1));
        item.Submit(Attempt(item, Guid.NewGuid())); item.Review(Pm, UserRoleCode.ProjectManager, At.AddHours(2));
        item.Confirm(Guid.NewGuid(), Pm, UserRoleCode.ProjectManager, "confirmed", At.AddHours(3)); return item;
    }
    private static FieldInspectionSubmission Submission(Guid? root = null, Guid? parent = null, int revision = 1,
        Guid? task = null, Guid? origin = null, Guid? actor = null, string? hash = null, Guid? start = null,
        DateTimeOffset? received = null, string readiness = "INCOMPLETE", string payload = "{\"captureType\":\"REPAIR_CLAIM\",\"repaired\":true}")
    {
        var id = Guid.NewGuid(); return FieldInspectionSubmission.Create(id, Project, task ?? TaskId, root ?? id, parent, revision,
            Assignment, start ?? StartId, Guid.NewGuid(), origin ?? Guid.NewGuid(), hash ?? Hash, actor ?? Crew, received ?? At.AddHours(1), payload, readiness, "[]");
    }
}
