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
    byte[] RowVersion);
