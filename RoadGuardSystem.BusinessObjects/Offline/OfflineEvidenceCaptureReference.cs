namespace RoadGuardSystem.BusinessObjects.Offline;

// Actual uploader/owner remains the receiver. Original captured actor is separate immutable provenance.
public sealed class OfflineEvidenceCaptureReference
{
    private OfflineEvidenceCaptureReference() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AdmissionId { get; private set; }
    public Guid BindingId { get; private set; }
    public Guid CaptureOriginId { get; private set; }
    public Guid OriginalActorId { get; private set; }
    public Guid ActualUploaderId { get; private set; }
    public Guid? GrantId { get; private set; }
    public Guid FileId { get; private set; }
    public Guid UploadSessionId { get; private set; }
    public string Purpose { get; private set; } = "";
    public string Checksum { get; private set; } = "";
    public string MediaType { get; private set; } = "";
    public DateTimeOffset? DeclaredCapturedAt { get; private set; }
    public string CaptureFactsJson { get; private set; } = "{}";
    public DateTimeOffset AdmittedAt { get; private set; }
    public static OfflineEvidenceCaptureReference Bind(Guid id, Guid project, Guid task, Guid admission,
        Guid binding, Guid capture, Guid originalActor, Guid uploader, Guid? grant, Guid file, Guid uploadSession,
        string purpose, string checksum, string mediaType, DateTimeOffset? declaredCapturedAt,
        string captureFactsJson, DateTimeOffset at)
    {
        OfflineRuntimeGuards.Identity(id,project,task,admission,binding,capture,originalActor,uploader,file,uploadSession);
        OfflineRuntimeGuards.Time(at);OfflineRuntimeGuards.Hash(checksum);OfflineRuntimeGuards.Json(captureFactsJson,1048576);
        if(purpose is not("BEFORE" or "AFTER" or "MEASUREMENT") || mediaType is not("image/jpeg" or "image/png") ||
            grant==Guid.Empty || originalActor!=uploader && grant is null)
            throw new ArgumentException("Exact scoped capture and actual uploader provenance are required.");
        return new(){Id=id,ProjectId=project,TaskId=task,AdmissionId=admission,BindingId=binding,CaptureOriginId=capture,
            OriginalActorId=originalActor,ActualUploaderId=uploader,GrantId=grant,FileId=file,UploadSessionId=uploadSession,
            Purpose=purpose,Checksum=checksum.ToLowerInvariant(),MediaType=mediaType,DeclaredCapturedAt=declaredCapturedAt,
            CaptureFactsJson=captureFactsJson,AdmittedAt=at.ToUniversalTime()};
    }
}
