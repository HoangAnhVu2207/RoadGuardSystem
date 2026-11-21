using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Inspections;

public sealed record InspectionTaskPersistenceView(
    Guid Id,
    Guid ProjectId,
    Guid DefectId,
    Guid AssignedToUserId,
    FieldInspectionTaskStatus Status,
    byte RequiredMeasurementType,
    string TaskCode,
    DateTimeOffset DueAt,
    byte[] RowVersion, string? Mode = null, Guid? PolicyRevisionId = null, Guid? AssignmentId = null, string? AssignmentStatus = null, string? AcceptanceOutcome = null, DateTimeOffset? AcceptedAt = null, DateTimeOffset? RejectedAt = null, bool IsHistory = false);
