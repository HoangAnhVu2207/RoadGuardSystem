namespace RoadGuardSystem.BusinessObjects.Messaging;

// Private, immutable outbox fact. Never projected into the inbox/public response.
public sealed record FastTrackReviewSnapshot(DateTimeOffset CapturedAt, Guid[] ProjectManagerIds,
    Guid? SupervisorId, Guid? SupervisorClockId)
{
    public bool IsValid() => CapturedAt != default && CapturedAt.Offset == TimeSpan.Zero &&
        ProjectManagerIds is not null && ProjectManagerIds.Length <= 1000 &&
        ProjectManagerIds.All(id => id != Guid.Empty) && ProjectManagerIds.Distinct().Count() == ProjectManagerIds.Length &&
        SupervisorId != Guid.Empty && SupervisorClockId != Guid.Empty &&
        (SupervisorId is null) == (SupervisorClockId is null) &&
        (SupervisorId is null || !ProjectManagerIds.Contains(SupervisorId.Value));
}
