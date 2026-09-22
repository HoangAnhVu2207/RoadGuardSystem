using RoadGuardSystem.BusinessObjects.Defects;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.Processing;

namespace RoadGuardSystem.Repositories.Defects;

public sealed record DetectionReviewPersistenceRequest(
    AIDetection Detection,
    Defect Defect,
    FieldInspectionTask FieldInspectionTask,
    DefectVerificationLog VerificationLog,
    Guid ActorUserId,
    Guid ProjectId,
    string IdempotencyKey,
    string RequestFingerprint,
    Guid CorrelationId,
    string EventPayloadJson,
    string AuditReason,
    Guid? VerifiedByUserId = null);
