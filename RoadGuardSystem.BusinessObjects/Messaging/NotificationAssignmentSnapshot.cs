using System.Text.Json.Serialization;

namespace RoadGuardSystem.BusinessObjects.Messaging;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NotificationAssignmentSnapshot(int SchemaVersion, DateTimeOffset CapturedAt,
    Guid[] ProjectManagerIds, Guid? SupervisorId, Guid? AssignmentId, string? AssignmentVersion)
{
    public bool IsValid() => SchemaVersion == 1 && CapturedAt != default && CapturedAt.Offset == TimeSpan.Zero &&
        ProjectManagerIds is { Length: <= 1000 } && ProjectManagerIds.All(id => id != Guid.Empty) &&
        ProjectManagerIds.Distinct().Count() == ProjectManagerIds.Length &&
        (SupervisorId is null && AssignmentId is null && AssignmentVersion is null ||
         SupervisorId is Guid supervisor && supervisor != Guid.Empty && !ProjectManagerIds.Contains(supervisor) &&
         AssignmentId is Guid assignment && assignment != Guid.Empty && IsVersion(AssignmentVersion));

    public static bool IsVersion(string? value)
    {
        if (value is null) return false;
        try { var bytes = Convert.FromBase64String(value); return bytes.Length == 8 && Convert.ToBase64String(bytes) == value; }
        catch (FormatException) { return false; }
    }
}

// The head survives withdrawal. Generations and command audit are retained independently.
public sealed class NotificationSupervisorHead
{
    public Guid ItemId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid PackageId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid SupervisorId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class NotificationSupervisorGeneration
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid SupervisorId { get; set; }
    public Guid ActorId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public string Version { get; set; } = "";
}

public sealed class NotificationSupervisorAudit
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid? PreviousAssignmentId { get; set; }
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public DateTimeOffset At { get; set; }
}

public sealed record FastTrackFinalSource(Guid PackageId, Guid ReviewId, Guid IntakeLinkId, Guid AttemptId);
