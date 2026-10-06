using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.DTOs.Repairs;

/// <summary>Local H3/H4/H5 producer seam, not an adopted external client contract.
/// Original actor and importing caller are distinct. Measure-only starts have no repair grant.
/// ClaimedStartedAt is retained even when proof is uncertain; server intake never backdates permission.</summary>
public sealed record RepairFirstStartV1(string SchemaVersion, Guid OriginEventId, string PayloadHash,
    Guid ProjectId, Guid DefectId, Guid TaskId, Guid AssignmentId, Guid? RepairItemId, Guid? AuthorizationId,
    Guid OriginalActorId, string SourceDeviceId, string TaskVersion, string AssignmentVersion,
    string LocationVersion, Guid? PolicyRevisionId, DateTimeOffset ClaimedStartedAt,
    DateTimeOffset ServerReceivedAt, RepairTimeProvenance TimeProvenance);

/// <summary>Immutable submission-intake facts. Pending files are admitted facts, not acceptance evidence.
/// Supplement revises readiness/evidence; it cannot replace the original execution or review-clock origin.</summary>
public sealed record RepairSubmissionFactsV1(string SchemaVersion, Guid OriginEventId, string PayloadHash,
    Guid SubmissionId, Guid RevisionId, Guid? ParentRevisionId, Guid OriginalIntakeId,
    Guid ProjectId, Guid DefectId, Guid TaskId, Guid AssignmentId, Guid? RepairItemId, Guid? AuthorizationId,
    Guid OriginalActorId, string SourceDeviceId, string TaskVersion, string AssignmentVersion,
    string LocationVersion, Guid? PolicyRevisionId, string ChecklistVersion, bool Performed,
    string? UnperformedReason, DateTimeOffset? ClaimedStartedAt, DateTimeOffset? ClaimedFinishedAt,
    DateTimeOffset CapturedAt, DateTimeOffset ServerReceivedAt, DateTimeOffset OriginalServerReceivedAt,
    RepairTimeProvenance TimeProvenance, IReadOnlyList<RepairMeasurementFactV1> Measurements,
    IReadOnlyList<RepairEvidenceFactsV1> Evidence, IReadOnlyList<string> MissingReasons);

public sealed record RepairMeasurementFactV1(Guid MeasurementId, string Code, decimal? Value, string Unit,
    string ValueStatus, decimal? Longitude, decimal? Latitude, string LocationStatus, string? VerifiedLocationReference);

public sealed record RepairEvidenceFactsV1(Guid ReferenceId, Guid FileId, string? FileVersion, string? Sha256,
    string Purpose, string Readiness, string SourceKind, Guid SourceId, string SourceVersion,
    Guid OriginalActorId, DateTimeOffset? CapturedAt, Guid? ReuseDecisionId);

/// <summary>Effective correction-aware reporting facts; original performed and decision events remain separate.
/// Neither this seam nor its counts define public rate denominator or segment allocation.</summary>
public sealed record EffectiveRepairFactsV1(Guid ProjectId, Guid DefectId, Guid ObligationId, Guid ItemId,
    RepairMode Mode, bool Mandatory, bool ObligationResolved, Guid? OriginalPerformedAttemptId,
    Guid? OriginalDecisionId, Guid? EffectiveDecisionId, Guid? SupersededDecisionId,
    RepairPresentationState EffectiveResult, bool EffectivelyAccepted, string HeadVersion, IReadOnlyList<Guid> DecisionHistoryIds,
    IReadOnlyList<Guid> LocationReferenceIds, IReadOnlyList<string> MissingReasons);
