namespace RoadGuardSystem.BusinessObjects.Offline;

// Server-only version/provenance retention. This is not a grant to read a private Reporter source.
public sealed class OfflineAdmittedFileReference
{
    private OfflineAdmittedFileReference() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AdmissionId { get; private set; }
    public Guid BindingId { get; private set; }
    public Guid CaptureOriginId { get; private set; }
    public Guid FileId { get; private set; }
    public Guid OriginalActorId { get; private set; }
    public Guid CurrentImporterId { get; private set; }
    public Guid ActualFileOwnerId { get; private set; }
    public Guid? ActualUploadedById { get; private set; }
    public string Purpose { get; private set; } = "";
    public string ContentChecksum { get; private set; } = "";
    public string CaptureFactsJson { get; private set; } = "{}";
    public DateTimeOffset ReferencedAt { get; private set; }
    public static OfflineAdmittedFileReference Capture(Guid id, Guid project, Guid task, Guid admission,
        Guid binding, Guid capture, Guid file, Guid originalActor, Guid currentImporter, Guid actualOwner,
        Guid? actualUploadedBy, string purpose, string declaredChecksum, string actualChecksum,
        string factsJson, DateTimeOffset at)
    {
        OfflineRuntimeGuards.Identity(id, project, task, admission, binding, capture, file, originalActor,
            currentImporter, actualOwner);
        OfflineRuntimeGuards.Time(at); OfflineRuntimeGuards.Hash(declaredChecksum);
        OfflineRuntimeGuards.Hash(actualChecksum); OfflineRuntimeGuards.Json(factsJson, 1048576);
        if (actualUploadedBy == Guid.Empty || purpose is not ("BEFORE" or "AFTER" or "MEASUREMENT") ||
            !string.Equals(declaredChecksum, actualChecksum, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Actual scoped file version and uploader provenance must be retained.");
        return new()
        {
            Id = id, ProjectId = project, TaskId = task, AdmissionId = admission, BindingId = binding,
            CaptureOriginId = capture, FileId = file, OriginalActorId = originalActor,
            CurrentImporterId = currentImporter, ActualFileOwnerId = actualOwner, ActualUploadedById = actualUploadedBy,
            Purpose = purpose, ContentChecksum = actualChecksum.ToLowerInvariant(), CaptureFactsJson = factsJson,
            ReferencedAt = at.ToUniversalTime()
        };
    }
}
