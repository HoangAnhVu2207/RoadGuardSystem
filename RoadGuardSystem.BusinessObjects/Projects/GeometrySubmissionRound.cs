namespace RoadGuardSystem.BusinessObjects.Projects;

public sealed class GeometrySubmissionRound
{
    private GeometrySubmissionRound() { }

    public Guid DraftId { get; private set; }
    public int Number { get; private set; }
    public string ContentVersion { get; private set; } = "";
    public string? ContentFingerprint { get; private set; }
    public string InputJson { get; private set; } = "";
    public string OriginalCoordinatesJson { get; private set; } = "";
    public Guid SubmittedBy { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public string Outcome { get; private set; } = "SUBMITTED";
    public Guid? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public static GeometrySubmissionRound Start(RoadGeometryDraft draft, int number, string fingerprint) => new()
    {
        DraftId = draft.Id,
        Number = number,
        ContentVersion = draft.SubmittedContentVersion!,
        ContentFingerprint = fingerprint,
        InputJson = draft.InputJson,
        OriginalCoordinatesJson = draft.OriginalCoordinatesJson,
        SubmittedBy = draft.SubmittedBy!.Value,
        SubmittedAt = draft.SubmittedAt!.Value
    };

    public void Decide(Guid actor, DateTimeOffset now, string? rejectionReason)
    {
        if (Outcome != "SUBMITTED") throw new InvalidOperationException("This review round is already decided.");
        Outcome = rejectionReason is null ? "CONFIRMED" : "REJECTED";
        DecidedBy = actor;
        DecidedAt = now;
        RejectionReason = rejectionReason;
    }
}
