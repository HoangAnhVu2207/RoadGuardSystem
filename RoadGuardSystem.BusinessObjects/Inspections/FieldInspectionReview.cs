namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldInspectionReview
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Decision { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public string ReceiptActivation { get; private set; } = "NOT_APPLICABLE";
    private FieldInspectionReview() { }
    public static FieldInspectionReview Create(Guid id, Guid project, Guid task, Guid submission, Guid actor,
        string decision, string reason, DateTimeOffset at)
    {
        if (new[] {id,project,task,submission,actor}.Any(x => x == Guid.Empty) ||
            decision is not ("CONFIRM" or "NO_DEFECT" or "SUPPLEMENT") || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000)
            throw new ArgumentException("A bounded actor review decision is required.");
        return new FieldInspectionReview { Id=id,ProjectId=project,TaskId=task,SubmissionId=submission,ActorId=actor,
            Decision=decision,Reason=reason.Trim(),OccurredAt=at.ToUniversalTime(),
            ReceiptActivation=decision == "SUPPLEMENT" ? "BUSINESS_ACK_REQUIRED" : "NOT_APPLICABLE" };
    }
}
