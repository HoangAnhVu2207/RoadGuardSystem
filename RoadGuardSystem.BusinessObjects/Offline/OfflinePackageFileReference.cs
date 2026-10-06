namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed class OfflinePackageFileReference
{
    private OfflinePackageFileReference() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid PackageId { get; private set; }
    public Guid FileId { get; private set; }
    public string ContentChecksum { get; private set; } = "";
    public string CaptureFactsJson { get; private set; } = "{}";
    public static OfflinePackageFileReference Capture(Guid id, Guid project, Guid package, Guid file,
        string checksum, string factsJson)
    {
        OfflineRuntimeGuards.Identity(id,project,package,file);OfflineRuntimeGuards.Hash(checksum);OfflineRuntimeGuards.Json(factsJson,1048576);
        return new(){Id=id,ProjectId=project,PackageId=package,FileId=file,ContentChecksum=checksum.ToLowerInvariant(),CaptureFactsJson=factsJson};
    }
}
