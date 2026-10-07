using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

[Trait("TaskId", "H6")]
public sealed class H6NotificationFoundationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EnvelopeBindsActualFieldSourceAndNormalizesUtc()
    {
        var origin = Guid.NewGuid(); var task = Guid.NewGuid(); var revision = Guid.NewGuid();
        var envelope = Envelope(NotificationEventKind.FieldSubmitted, task, origin, revision, Now.ToOffset(TimeSpan.FromHours(7)));
        envelope.MessageType.Should().Be("field.task.submitted.v1");
        envelope.SchemaVersion.Should().Be(1);
        envelope.SourceId.Should().Be(task); envelope.OriginEventId.Should().Be(origin);
        envelope.SourceRevisionId.Should().Be(revision); envelope.OccurredAtUtc.Offset.Should().Be(TimeSpan.Zero);
        envelope.RecipientStrategy.Should().Be(NotificationRecipientStrategy.ProjectManager);
    }

    [Fact]
    public void EnvelopeRejectsMismatchedSourceRatherThanInventingResolver()
    {
        Action act = () => NotificationEventEnvelope.Create(Guid.NewGuid(), NotificationEventKind.FieldAssigned,
            Guid.NewGuid(), NotificationSourceKind.RepairWork, Guid.NewGuid(), Guid.NewGuid(), Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnvelopeRejectsUnknownEvent()
    {
        Action act = () => Envelope((NotificationEventKind)255);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EnvelopeRequiresSubmissionRevision()
    {
        Action act = () => Envelope(NotificationEventKind.FieldSubmitted);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OccurrenceDuplicateTransportEventSharesSemanticIdentity()
    {
        var source = Guid.NewGuid(); var origin = Guid.NewGuid(); var project = Guid.NewGuid();
        var first = NotificationEventEnvelope.Create(Guid.NewGuid(), NotificationEventKind.FieldAssigned, project,
            NotificationSourceKind.FieldTask, source, origin, Now);
        var duplicate = NotificationEventEnvelope.Create(Guid.NewGuid(), NotificationEventKind.FieldAssigned, project,
            NotificationSourceKind.FieldTask, source, origin, Now);
        NotificationOccurrence.Create(Guid.NewGuid(), first).OccurrenceKey.Should()
            .Be(NotificationOccurrence.Create(Guid.NewGuid(), duplicate).OccurrenceKey);
    }

    [Fact]
    public void OccurrenceProjectPartitionIsPartOfIdentity()
    {
        var source = Guid.NewGuid(); var origin = Guid.NewGuid();
        var first = Envelope(source: source, origin: origin);
        var second = Envelope(source: source, origin: origin);
        NotificationOccurrence.Create(Guid.NewGuid(), first).OccurrenceKey.Should()
            .NotBe(NotificationOccurrence.Create(Guid.NewGuid(), second).OccurrenceKey);
    }

    [Fact]
    public void WeeklyOccurrenceDifferentCalendarPeriodsNeverCollapse()
    {
        var envelope = Envelope(NotificationEventKind.WeeklyReviewPending);
        var first = NotificationOccurrence.CreateWeekly(Guid.NewGuid(), envelope, Now);
        var next = NotificationOccurrence.CreateWeekly(Guid.NewGuid(), envelope, Now.AddDays(7));
        first.OccurrenceKey.Should().NotBe(next.OccurrenceKey);
        first.ScheduledAtUtc.Should().Be(Now);
    }

    [Fact]
    public void WeeklyOccurrenceSamePeriodDeduplicatesDifferentSchedulerOrigins()
    {
        var source = Guid.NewGuid(); var project = Guid.NewGuid();
        var first = NotificationEventEnvelope.Create(Guid.NewGuid(), NotificationEventKind.WeeklyReviewPending,
            project, NotificationSourceKind.ReviewObligation, source, Guid.NewGuid(), Now);
        var duplicate = NotificationEventEnvelope.Create(Guid.NewGuid(), NotificationEventKind.WeeklyReviewPending,
            project, NotificationSourceKind.ReviewObligation, source, Guid.NewGuid(), Now.AddSeconds(1));
        NotificationOccurrence.CreateWeekly(Guid.NewGuid(), first, Now).OccurrenceKey.Should()
            .Be(NotificationOccurrence.CreateWeekly(Guid.NewGuid(), duplicate, Now).OccurrenceKey);
    }

    [Fact]
    public void OccurrenceChangedRevisionSharesIdentityButHasConflictingContent()
    {
        var project = Guid.NewGuid(); var source = Guid.NewGuid(); var origin = Guid.NewGuid();
        var first = NotificationEventEnvelope.Create(Guid.NewGuid(), NotificationEventKind.FieldSubmitted,
            project, NotificationSourceKind.FieldTask, source, origin, Now, Guid.NewGuid());
        var changed = NotificationEventEnvelope.Create(Guid.NewGuid(), NotificationEventKind.FieldSubmitted,
            project, NotificationSourceKind.FieldTask, source, origin, Now, Guid.NewGuid());
        var firstOccurrence = NotificationOccurrence.Create(Guid.NewGuid(), first);
        var changedOccurrence = NotificationOccurrence.Create(Guid.NewGuid(), changed);
        firstOccurrence.OccurrenceKey.Should().Be(changedOccurrence.OccurrenceKey);
        firstOccurrence.ContentFingerprint.Should().NotBe(changedOccurrence.ContentFingerprint);
    }

    [Fact]
    public void WeeklyOccurrenceRejectsWrongDayOrLocalHour()
    {
        Action act = () => NotificationOccurrence.CreateWeekly(Guid.NewGuid(), Envelope(NotificationEventKind.WeeklyReviewPending), Now.AddHours(1));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalendarUsesMondayNineVietnamTimeAndConfirmedLatestRecovery()
    {
        NotificationCalendarPolicy.NextWeeklyReview(Now.AddTicks(-1)).Should().Be(Now);
        NotificationCalendarPolicy.NextWeeklyReview(Now).Should().Be(Now.AddDays(7));
        NotificationCalendarPolicy.EvaluateMissedPeriod(Now, Now.AddDays(8)).Should()
            .Be(NotificationCalendarDecision.RecoverLatest);
    }

    [Fact]
    public void LeaseCannotBeTakenBeforeExpiry()
    {
        var occurrence = Occurrence(); occurrence.AcquireLease(Guid.NewGuid(), Now, TimeSpan.FromMinutes(1));
        Action act = () => occurrence.AcquireLease(Guid.NewGuid(), Now.AddSeconds(59), TimeSpan.FromMinutes(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LeaseExpiredWorkerCannotCompleteReacquiredOccurrence()
    {
        var occurrence = Occurrence(); var oldFence = Guid.NewGuid(); var newFence = Guid.NewGuid();
        occurrence.AcquireLease(oldFence, Now, TimeSpan.FromMinutes(1));
        occurrence.AcquireLease(newFence, Now.AddMinutes(1), TimeSpan.FromMinutes(1));
        Action stale = () => occurrence.Complete(oldFence, Now.AddSeconds(61));
        stale.Should().Throw<InvalidOperationException>();
        occurrence.Complete(newFence, Now.AddSeconds(61));
        occurrence.CompletedAtUtc.Should().Be(Now.AddSeconds(61)); occurrence.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void LeaseExpiryBoundaryRejectsCompletion()
    {
        var occurrence = Occurrence(); var fence = Guid.NewGuid();
        occurrence.AcquireLease(fence, Now, TimeSpan.FromMinutes(1));
        Action act = () => occurrence.Complete(fence, Now.AddMinutes(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LeasePreviousFenceCannotBeRevivedAfterInterveningAcquisition()
    {
        var occurrence = Occurrence(); var firstFence = Guid.NewGuid();
        occurrence.AcquireLease(firstFence, Now, TimeSpan.FromMinutes(1));
        occurrence.AcquireLease(Guid.NewGuid(), Now.AddMinutes(1), TimeSpan.FromMinutes(1));
        Action act = () => occurrence.AcquireLease(firstFence, Now.AddMinutes(2), TimeSpan.FromMinutes(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LeaseCompletionCannotPrecedeAcquisition()
    {
        var occurrence = Occurrence(); var fence = Guid.NewGuid();
        occurrence.AcquireLease(fence, Now.AddMinutes(1), TimeSpan.FromMinutes(1));
        Action act = () => occurrence.Complete(fence, Now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeliveryHistoryRejectsBackdatingWithoutChangingState()
    {
        var delivery = NotificationRecipientDelivery.CreateUnresolved(Guid.NewGuid(), Guid.NewGuid(),
            NotificationRecipientStrategy.ProjectManager, Now);
        Action act = () => delivery.ResolveRecipient(Guid.NewGuid(), Now.AddTicks(-1));
        act.Should().Throw<ArgumentException>();
        delivery.Status.Should().Be(NotificationRecipientDeliveryStatus.Unresolved);
        delivery.RecipientUserId.Should().BeNull();
    }

    [Fact]
    public void LeaseAcknowledgmentLossReplayPreservesOriginalCompletion()
    {
        var occurrence = Occurrence(); var fence = Guid.NewGuid();
        occurrence.AcquireLease(fence, Now, TimeSpan.FromMinutes(1)); occurrence.Complete(fence, Now.AddSeconds(1));
        occurrence.Complete(fence, Now.AddDays(1));
        occurrence.CompletedAtUtc.Should().Be(Now.AddSeconds(1));
    }

    [Fact]
    public void LeaseCompletedOccurrenceDoesNotAcquireNewAttempt()
    {
        var occurrence = Occurrence(); var fence = Guid.NewGuid();
        occurrence.AcquireLease(fence, Now, TimeSpan.FromMinutes(1)); occurrence.Complete(fence, Now.AddSeconds(1));
        Action act = () => occurrence.AcquireLease(Guid.NewGuid(), Now.AddSeconds(2), TimeSpan.FromMinutes(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeliveryFanoutTracksDifferentRecipientsIndependently()
    {
        var occurrence = Guid.NewGuid(); var first = NotificationRecipientDelivery.Create(Guid.NewGuid(), occurrence, Guid.NewGuid());
        var second = NotificationRecipientDelivery.Create(Guid.NewGuid(), occurrence, Guid.NewGuid());
        first.RecordDelivered(Guid.NewGuid(), Now);
        first.Status.Should().Be(NotificationRecipientDeliveryStatus.Delivered);
        second.Status.Should().Be(NotificationRecipientDeliveryStatus.Pending);
        second.NotificationId.Should().BeNull();
    }

    [Fact]
    public void DeliveryCommittedEffectReplayIsWriteOnce()
    {
        var delivery = NotificationRecipientDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var notification = Guid.NewGuid(); delivery.RecordDelivered(notification, Now);
        delivery.RecordDelivered(notification, Now.AddSeconds(1));
        delivery.DeliveredAtUtc.Should().Be(Now);
        Action changed = () => delivery.RecordDelivered(Guid.NewGuid(), Now.AddSeconds(2));
        changed.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeliveryMissingActorRemainsUnresolvedThenBindsOnlyOnce()
    {
        var delivery = NotificationRecipientDelivery.CreateUnresolved(Guid.NewGuid(), Guid.NewGuid(), NotificationRecipientStrategy.ProjectManager, Now);
        delivery.Status.Should().Be(NotificationRecipientDeliveryStatus.Unresolved); delivery.RecipientUserId.Should().BeNull();
        var actor = Guid.NewGuid(); delivery.ResolveRecipient(actor, Now.AddMinutes(1));
        Action retarget = () => delivery.ResolveRecipient(Guid.NewGuid(), Now.AddMinutes(2));
        retarget.Should().Throw<InvalidOperationException>();
        delivery.RecipientUserId.Should().Be(actor); delivery.History.Should().HaveCount(2);
    }

    [Fact]
    public void DeliveryRevokedRecipientIsRetainedWithoutSuccessfulEffect()
    {
        var actor = Guid.NewGuid(); var delivery = NotificationRecipientDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), actor);
        delivery.RecordUnavailable(NotificationRecipientUnavailableReason.MembershipLost, Now);
        delivery.RecipientUserId.Should().Be(actor); delivery.Status.Should().Be(NotificationRecipientDeliveryStatus.Unresolved);
        delivery.NotificationId.Should().BeNull();
        Action act = () => delivery.RecordDelivered(Guid.NewGuid(), Now.AddSeconds(1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ScopeAuditUnknownIsProtectedAndRetainsResolutionReason()
    {
        var audit = NotificationScopeAudit.Record(Guid.NewGuid(), Guid.NewGuid(), null, "legacy-source.v1",
            NotificationScopeDecision.UnknownProtected, null, "Unsupported", Guid.NewGuid(), "source_unsupported", Now);
        audit.ProjectId.Should().BeNull(); audit.Decision.Should().Be(NotificationScopeDecision.UnknownProtected);
        audit.ReasonCode.Should().Be("source_unsupported");
    }

    [Fact]
    public void ScopeAuditProjectRequiresActualProjectAndSourceEvidence()
    {
        Action act = () => NotificationScopeAudit.Record(Guid.NewGuid(), Guid.NewGuid(), null, "field-task.v1",
            NotificationScopeDecision.Project, null, "FieldInspectionTask", Guid.NewGuid(), "source_resolved", Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ScopeAuditNonProjectRequiresProofRatherThanNullProjectConvention()
    {
        Action act = () => NotificationScopeAudit.Record(Guid.NewGuid(), Guid.NewGuid(), null, "identity.v1",
            NotificationScopeDecision.ProvenNonProject, null, "", Guid.Empty, "source_resolved", Now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ScopeAuditResolvedAuditLinksPriorEvidenceWithoutMutatingIt()
    {
        var notification = Guid.NewGuid(); var old = NotificationScopeAudit.Record(Guid.NewGuid(), notification, null, "legacy.v1",
            NotificationScopeDecision.UnknownProtected, null, "FieldInspectionTask", Guid.NewGuid(), "source_missing", Now);
        var project = Guid.NewGuid(); var next = NotificationScopeAudit.Record(Guid.NewGuid(), notification, old.Id, "field.v2",
            NotificationScopeDecision.Project, project, "FieldInspectionTask", old.EvidenceSourceId, "source_resolved", Now.AddDays(1));
        next.PreviousAuditId.Should().Be(old.Id); next.ProjectId.Should().Be(project);
        old.Decision.Should().Be(NotificationScopeDecision.UnknownProtected);
    }

    private static NotificationOccurrence Occurrence() => NotificationOccurrence.Create(Guid.NewGuid(), Envelope());
    private static NotificationEventEnvelope Envelope(NotificationEventKind kind = NotificationEventKind.FieldAssigned,
        Guid? source = null, Guid? origin = null, Guid? revision = null, DateTimeOffset? occurred = null)
        => NotificationEventEnvelope.Create(Guid.NewGuid(), kind, Guid.NewGuid(),
            kind == NotificationEventKind.WeeklyReviewPending ? NotificationSourceKind.ReviewObligation : NotificationSourceKind.FieldTask,
            source ?? Guid.NewGuid(), origin ?? Guid.NewGuid(), occurred ?? Now, revision);
}
