using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4PolicySafetyTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
    private static readonly Guid Project = Guid.NewGuid(), Defect = Guid.NewGuid(), Road = Guid.NewGuid(), Pm = Guid.NewGuid(), Crew = Guid.NewGuid();

    [Fact]
    public void DraftChangesRetainOriginalThresholdAndCopiedInputs()
    {
        var rules = new[] { new RepairMeasurementRule("width", "mm", 0, 2) };
        var draft = Draft(rules); var original = Assert.Single(draft.Changes); rules[0] = new("width", "mm", 0, 99);
        draft.Update(Guid.NewGuid(), original.Id, Pm, UserRoleCode.ProjectManager, At.AddHours(1), "new threshold", "CRACK", "checklist-v2", [new("width", "mm", 0, 4)], []);
        Assert.Equal(2, draft.Changes.Count); Assert.Equal(2m, original.Measurements[0].Maximum); Assert.Equal("checklist-v1", original.ChecklistVersion);
    }
    [Fact]
    public void DraftUpdateRequiresCurrentHeadAndPmRole()
    {
        var draft = Draft(); var head = draft.Changes[0].Id;
        Assert.Throws<InvalidOperationException>(() => draft.Update(Guid.NewGuid(), Guid.NewGuid(), Pm, UserRoleCode.ProjectManager, At, "stale", "CRACK", "v1", [], []));
        Assert.Throws<InvalidOperationException>(() => draft.Update(Guid.NewGuid(), head, Guid.NewGuid(), UserRoleCode.Supervisor, At, "override", "CRACK", "v1", [], []));
        Assert.Single(draft.Changes);
    }
    [Fact]
    public void PublishPinsLatestDraftWithoutSupervisorApprovalAndThenFreezesDraft()
    {
        var draft = Draft(); var policy = draft.Publish(Guid.NewGuid(), 1, Pm, UserRoleCode.ProjectManager, At.AddHours(1));
        Assert.Same(policy, draft.PublishedRevision); Assert.Equal(2m, policy.Measurements[0].Maximum);
        Assert.Throws<InvalidOperationException>(() => draft.Update(Guid.NewGuid(), draft.Changes[0].Id, Pm, UserRoleCode.ProjectManager,
            At.AddHours(2), "overwrite published draft", "CRACK", "v2", [], []));
    }
    [Fact]
    public void NotConfiguredDraftPublishesNoInventedThreshold()
    {
        var draft = Draft([]); var policy = draft.Publish(Guid.NewGuid(), 1, Pm, UserRoleCode.ProjectManager, At);
        Assert.Equal(RepairEligibilityReason.NotConfigured, policy.Evaluate([], []).Reason);
    }
    [Fact]
    public void PublishCannotReplaceRevisionOrBackdatePublication()
    {
        var draft = Draft(); Assert.Throws<ArgumentException>(() => draft.Publish(Guid.NewGuid(), 1, Pm, UserRoleCode.ProjectManager, At.AddTicks(-1)));
        draft.Publish(Guid.NewGuid(), 1, Pm, UserRoleCode.ProjectManager, At);
        Assert.Throws<InvalidOperationException>(() => draft.Publish(Guid.NewGuid(), 2, Pm, UserRoleCode.ProjectManager, At.AddHours(1)));
    }
    [Fact]
    public void SafetyMonitoringRequiresActualSameProjectFormalAndSafetyObligations()
    {
        var formal = Obligation(RepairObligationKind.FormalRepair); var safety = Obligation(RepairObligationKind.TemporarySafety);
        var measure = Measure(formal);
        Assert.Throws<InvalidOperationException>(() => RepairSafetyMonitoring.Create(measure, formal, safety));
        var wrong = RepairObligation.Create(Guid.NewGuid(), Guid.NewGuid(), Defect, RepairObligationKind.TemporarySafety, true, Scope());
        Assert.Throws<InvalidOperationException>(() => RepairSafetyMonitoring.Create(measure, wrong, formal));
    }
    [Fact]
    public void SafetyCheckDoesNotResolveOngoingObligationAndRetainsEvidence()
    {
        var monitor = Monitoring(); var evidence = new[] { Guid.NewGuid() }; var original = evidence[0];
        var check = monitor.RecordCheck(Guid.NewGuid(), Crew, At.AddHours(1), RepairSafetyCheckResult.Safe, "barrier observed intact", evidence);
        evidence[0] = Guid.NewGuid(); Assert.Equal(original, Assert.Single(check.EvidenceIds));
        Assert.False(monitor.SafetyObligation.IsResolved); Assert.True(monitor.Measure.RequiresMonitoring);
    }
    [Fact]
    public void CheckCannotPredateCompletedMeasureOrOverwriteCheckHistory()
    {
        var monitor = Monitoring(); var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => monitor.RecordCheck(id, Crew, At.AddTicks(-1), RepairSafetyCheckResult.Safe, "early", []));
        monitor.RecordCheck(id, Crew, At.AddHours(1), RepairSafetyCheckResult.Unknown, "visibility unavailable", []);
        Assert.Throws<InvalidOperationException>(() => monitor.RecordCheck(id, Crew, At.AddHours(2), RepairSafetyCheckResult.Safe, "overwrite", []));
        Assert.Single(monitor.Checks);
    }
    [Fact]
    public void DangerClockStartsAtActualServerWarningWithoutResolvingSafety()
    {
        var monitor = Monitoring(); var received = At.AddHours(8);
        var warning = monitor.ReceiveDangerWarning(Guid.NewGuid(), Guid.NewGuid(), received, "barrier displaced");
        Assert.Equal(received.AddHours(1), warning.OriginalAcknowledgementDueAt); Assert.Equal(Crew, warning.ResponsibleActorId);
        Assert.False(monitor.SafetyObligation.IsResolved); Assert.Equal(At.AddHours(24), monitor.Measure.FirstCheckDueAt);
    }
    [Fact]
    public void WarningReplayRetainsOriginalServerOriginAndRejectsChangedContent()
    {
        var monitor = Monitoring(); var id = Guid.NewGuid(); var source = Guid.NewGuid();
        var first = monitor.ReceiveDangerWarning(id, source, At, "danger");
        Assert.Same(first, monitor.ReceiveDangerWarning(id, source, At.AddHours(4), "danger"));
        Assert.Throws<InvalidOperationException>(() => monitor.ReceiveDangerWarning(id, Guid.NewGuid(), At.AddHours(4), "changed"));
        Assert.Single(monitor.Warnings); Assert.Equal(At.AddHours(1), first.OriginalAcknowledgementDueAt);
    }
    [Fact]
    public void LateDangerAcknowledgementIsRetainedWithOriginalDueAndNoObligationClosure()
    {
        var monitor = Monitoring(); var warning = monitor.ReceiveDangerWarning(Guid.NewGuid(), Guid.NewGuid(), At, "danger");
        var ack = monitor.AcknowledgeDanger(Guid.NewGuid(), warning.Id, Crew, At.AddHours(2), "received and inspected");
        Assert.True(ack.AfterOriginalDue); Assert.Equal(At.AddHours(1), warning.OriginalAcknowledgementDueAt);
        Assert.False(monitor.SafetyObligation.IsResolved); Assert.False(monitor.FormalObligation.IsResolved);
    }
    [Fact]
    public void AcknowledgementRejectsUnknownWarningOrTimestampBeforeReceipt()
    {
        var monitor = Monitoring(); var warning = monitor.ReceiveDangerWarning(Guid.NewGuid(), Guid.NewGuid(), At, "danger");
        Assert.Throws<InvalidOperationException>(() => monitor.AcknowledgeDanger(Guid.NewGuid(), Guid.NewGuid(), Crew, At, "missing"));
        Assert.Throws<ArgumentException>(() => monitor.AcknowledgeDanger(Guid.NewGuid(), warning.Id, Crew, At.AddTicks(-1), "early"));
        Assert.Empty(monitor.Acknowledgements);
    }
    private static RepairPolicyDraft Draft(IReadOnlyList<RepairMeasurementRule>? rules = null)
        => RepairPolicyDraft.Create(Guid.NewGuid(), Project, Guid.NewGuid(), Pm, UserRoleCode.ProjectManager, At, "configured draft", "CRACK", "checklist-v1", rules ?? [new("width", "mm", 0, 2)], []);
    private static RepairActualScope Scope() => RepairActualScope.Create(Guid.NewGuid(), Road, "location-v1", "main", 0, 10, 0, 3);
    private static RepairObligation Obligation(RepairObligationKind kind) => RepairObligation.Create(Guid.NewGuid(), Project, Defect, kind, true, Scope());
    private static TemporarySafetyMeasure Measure(RepairObligation formal)
        => TemporarySafetyMeasure.Create(Guid.NewGuid(), Project, Defect, formal.Id, Crew, "each shift", "displaced barrier", "after safe formal work");
    private static RepairSafetyMonitoring Monitoring()
    {
        var formal = Obligation(RepairObligationKind.FormalRepair); var measure = Measure(formal);
        measure.Install(Guid.NewGuid(), Crew, At, At.AddHours(24));
        return RepairSafetyMonitoring.Create(measure, Obligation(RepairObligationKind.TemporarySafety), formal);
    }
}
