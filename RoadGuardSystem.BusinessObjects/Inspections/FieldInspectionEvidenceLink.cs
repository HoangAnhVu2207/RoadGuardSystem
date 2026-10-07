namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldInspectionEvidenceLink
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public Guid? FileId { get; private set; }
    public Guid CaptureOriginId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public string DeclaredChecksum { get; private set; } = string.Empty;
    public string MediaType { get; private set; } = string.Empty;
    public string CaptureFactsJson { get; private set; } = "{}";
    private FieldInspectionEvidenceLink() { }
    public static FieldInspectionEvidenceLink Create(Guid id, Guid project, Guid task, Guid assignment, Guid submission,
        Guid? file, Guid captureOrigin, string purpose, string checksum, string media, string facts)
    {
        if (new[] { id, project, task, assignment, submission, captureOrigin }.Any(x => x == Guid.Empty) || file == Guid.Empty ||
            purpose is not ("BEFORE" or "AFTER" or "MEASUREMENT") || string.IsNullOrEmpty(checksum) || checksum.Length != 64 || checksum.Any(x => !Uri.IsHexDigit(x)) ||
            string.IsNullOrWhiteSpace(media) || media.Length > 120) throw new ArgumentException("Typed declared evidence provenance is required.");
        using var document = System.Text.Json.JsonDocument.Parse(facts);
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) throw new ArgumentException("Capture facts must be an object.");
        return new FieldInspectionEvidenceLink
        {
            Id = id,
            ProjectId = project,
            TaskId = task,
            AssignmentId = assignment,
            SubmissionId = submission,
            FileId = file,
            CaptureOriginId = captureOrigin,
            Purpose = purpose,
            DeclaredChecksum = checksum.ToLowerInvariant(),
            MediaType = media,
            CaptureFactsJson = facts
        };
    }
}
