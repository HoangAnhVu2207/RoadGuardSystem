using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6FieldSourceProofTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 2, 0, 0, TimeSpan.Zero);
    [Fact]
    public void GenuineAssignmentUsesPersistedEventAndOriginalResponsibleActor()
    {
        var (claim, task, source, assignment) = Assignment();
        Assert.True(NotificationFieldSourceProof.Verify(claim, task, source, assignment));
    }
    [Fact]
    public void DistinctTransportIdentityKeepsTheSameActualImmutableSemanticOrigin()
    {
        var (claim, task, source, assignment) = Assignment();
        // The EF adapter separately requires the matching persisted transport envelope.
        Assert.True(NotificationFieldSourceProof.Verify(claim with { EventId = Guid.NewGuid() }, task, source, assignment));
    }
    [Fact]
    public void LaterAssignmentEndDoesNotEraseTheOriginalImmutableSourceEvent()
    {
        var (claim, task, source, assignment) = Assignment(); assignment.End(At.AddHours(1), "handover to different crew");
        Assert.True(NotificationFieldSourceProof.Verify(claim, task, source, assignment));
    }
    [Fact]
    public void PayloadCannotChooseAProjectOrResponsibleActorDifferentFromSourceRows()
    {
        var (claim, task, source, assignment) = Assignment();
        Assert.False(NotificationFieldSourceProof.Verify(claim with { ProjectId = Guid.NewGuid() }, task, source, assignment));
        Assert.False(NotificationFieldSourceProof.Verify(claim with { ResponsibleUserId = Guid.NewGuid() }, task, source, assignment));
    }
    [Fact]
    public void MatchingTaskDoesNotProveADifferentEventOrAssignment()
    {
        var (claim, task, source, assignment) = Assignment();
        Assert.False(NotificationFieldSourceProof.Verify(claim with { OriginEventId = Guid.NewGuid() }, task, source, assignment));
        Assert.False(NotificationFieldSourceProof.Verify(claim, task, source, null));
    }
    [Fact]
    public void SubmittedRevisionMustBeActualIntakeOfTheSameTaskAssignmentAndServerTime()
    {
        var (_, task, _, assignment) = Assignment(); var id = Guid.NewGuid();
        var submission = FieldInspectionSubmission.Create(id, task.ProjectId, task.Id, id, null, 1, assignment.Id,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), assignment.AssignedToUserId, At, "{}", "INCOMPLETE", "[]");
        var source = FieldInspectionTaskEvent.Create(Guid.NewGuid(), task.ProjectId, task.Id, assignment.Id, assignment.AssignedToUserId, "SUBMITTED", "formal intake", At);
        var claim = new NotificationFieldSourceClaim(source.Id, source.Id, task.ProjectId, task.Id, source.Kind, At, id, null);
        Assert.True(NotificationFieldSourceProof.Verify(claim, task, source, assignment, submission));
        Assert.False(NotificationFieldSourceProof.Verify(claim with { RevisionId = Guid.NewGuid() }, task, source, assignment, submission));
    }
    [Fact]
    public void SupplementRevisionIsActualSubmissionNotTheReviewIdOrReceiptAcknowledgment()
    {
        var (_, task, _, assignment) = Assignment(); var id = Guid.NewGuid();
        var submission = FieldInspectionSubmission.Create(id, task.ProjectId, task.Id, id, null, 1, assignment.Id,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), assignment.AssignedToUserId, At.AddMinutes(-1), "{}", "INCOMPLETE", "[]");
        var review = FieldInspectionReview.Create(Guid.NewGuid(), task.ProjectId, task.Id, id, Guid.NewGuid(), "SUPPLEMENT", "missing measurement", At);
        var source = FieldInspectionTaskEvent.Create(Guid.NewGuid(), task.ProjectId, task.Id, assignment.Id, review.ActorId, "SUPPLEMENT", "missing measurement", At);
        var claim = new NotificationFieldSourceClaim(source.Id, source.Id, task.ProjectId, task.Id, source.Kind, At, id, assignment.AssignedToUserId);
        Assert.True(NotificationFieldSourceProof.Verify(claim, task, source, assignment, submission, review));
        Assert.False(NotificationFieldSourceProof.Verify(claim with { RevisionId = review.Id }, task, source, assignment, submission, review));
        Assert.False(NotificationFieldSourceProof.Verify(claim, task, source, assignment, submission));
    }
    private static (NotificationFieldSourceClaim, FieldInspectionTask, FieldInspectionTaskEvent, FieldInspectionAssignment) Assignment()
    {
        var task = FieldInspectionTask.CreateOperational(Guid.NewGuid(), "proof-sample", Guid.NewGuid(), Guid.NewGuid(), null,
            "REPORTER", Guid.NewGuid(), null, null, null, FieldInspectionPurpose.PreMeasurement, 1, "{}", null, At.AddDays(1), Guid.NewGuid());
        var assignment = FieldInspectionAssignment.Create(Guid.NewGuid(), task.Id, Guid.NewGuid(), task.AssignedByUserId, At, null, FieldInspectionAssignmentStatus.Active, null);
        var source = FieldInspectionTaskEvent.Create(Guid.NewGuid(), task.ProjectId, task.Id, assignment.Id, task.AssignedByUserId, "ASSIGNED", "source fixture", At);
        return (new(source.Id, source.Id, task.ProjectId, task.Id, source.Kind, At, null, assignment.AssignedToUserId), task, source, assignment);
    }
}
