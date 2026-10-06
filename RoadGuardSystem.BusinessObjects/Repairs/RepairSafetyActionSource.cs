namespace RoadGuardSystem.BusinessObjects.Repairs;

public sealed record RepairSafetyActionSource(Guid Id, Guid MeasureId, Guid ProjectId, Guid ItemId,
    Guid BindingId, Guid ActorId, string Kind, DateTimeOffset At, Guid OriginId, string Reason);
