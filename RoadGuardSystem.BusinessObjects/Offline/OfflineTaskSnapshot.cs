namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed class OfflineTaskSnapshot
{
    private OfflineTaskSnapshot() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid OriginalActorId { get; private set; }
    public Guid DeviceRegistrationId { get; private set; }
    public string TaskVersion { get; private set; } = "";
    public string AssignmentHash { get; private set; } = "";
    public string SnapshotJson { get; private set; } = "{}";
    public string ContentHash { get; private set; } = "";
    public DateTimeOffset DownloadedAt { get; private set; }
    public static OfflineTaskSnapshot Capture(Guid id, Guid project, Guid task, Guid assignment, Guid originalActor,
        Guid deviceRegistration, string taskVersion, string assignmentHash, string snapshotJson, DateTimeOffset at)
    {
        OfflineRuntimeGuards.Identity(id, project, task, assignment, originalActor, deviceRegistration);
        OfflineRuntimeGuards.Time(at); OfflineRuntimeGuards.Hash(assignmentHash);
        OfflineRuntimeGuards.Json(snapshotJson, 1048576);
        try { if (Convert.FromBase64String(taskVersion).Length != 8) throw new ArgumentException("Task version is required."); }
        catch (FormatException exception) { throw new ArgumentException("Task version is invalid.", exception); }
        return new() { Id=id, ProjectId=project, TaskId=task, AssignmentId=assignment, OriginalActorId=originalActor,
            DeviceRegistrationId=deviceRegistration, TaskVersion=taskVersion, AssignmentHash=assignmentHash.ToLowerInvariant(),
            SnapshotJson=snapshotJson, ContentHash=OfflineRuntimeGuards.Digest(snapshotJson), DownloadedAt=at.ToUniversalTime() };
    }
}
