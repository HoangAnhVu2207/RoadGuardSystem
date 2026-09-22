namespace RoadGuardSystem.Repositories.Defects;

public sealed record DetectionReviewPersistenceResult(
    DetectionReviewPersistenceStatus Status,
    Guid DetectionId,
    Guid DefectId,
    Guid FieldInspectionTaskId,
    Guid VerificationLogId,
    Guid OutboxMessageId);
