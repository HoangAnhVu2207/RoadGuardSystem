using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Repairs;

// Immutable producer records. Production admission must bind each FK and captured version inside its transaction.
// Historical actor/version facts never appoint a current actor or extend an execution window.
public sealed record RepairFieldTaskBinding(Guid Id, Guid ProjectId, Guid DefectId, Guid ObligationId,
    Guid ItemId, Guid TaskId, Guid AssignmentId, Guid CrewId, RepairMode Mode, Guid? AuthorizationId,
    Guid? PolicyRevisionId, string? PolicyContentHash, string PlanHash, string ChecklistVersion,
    Guid RouteVersionId, Guid? SegmentSetId, Guid? LayoutRevisionId, string? SlabId,
    Guid? MapPublicationId, Guid? CrsProfileRevisionId, string LocationVersion, Guid AssignedBy,
    DateTimeOffset AssignedAt, string Reason, string TaskVersion, string AssignmentVersion);

/// <summary>PRE_EXECUTION capture is not a formal FIELD intake or a finished repair claim.</summary>
public sealed record RepairMeasurementAssessment(Guid Id, Guid ProjectId, Guid ItemId, Guid BindingId,
    Guid TaskId, Guid AssignmentId, Guid OriginalActorId, Guid FirstStartId, Guid SessionId,
    Guid OperationOriginId, Guid OriginId, string ContentHash, string PayloadJson,
    DateTimeOffset ServerReceivedAt, string Stage, string Readiness, string MissingReasonsJson,
    string LocationFactsJson, string LocationState, Guid? FormalSourceSubmissionId)
{
    private List<RepairAssessmentMeasurementReference> _measurements = [];
    private List<RepairAssessmentEvidenceReference> _evidence = [];
    public IReadOnlyList<RepairAssessmentMeasurementReference> Measurements
    { get => _measurements.AsReadOnly(); init => _measurements = value.ToList(); }
    public IReadOnlyList<RepairAssessmentEvidenceReference> Evidence
    { get => _evidence.AsReadOnly(); init => _evidence = value.ToList(); }
}
public sealed record RepairAssessmentMeasurementReference(Guid MeasurementId);
public sealed record RepairAssessmentEvidenceReference(Guid Id, Guid CaptureOriginId, Guid? FileId,
    string Purpose, string ChecksumSha256, string MediaType, DateTimeOffset? CapturedAt,
    string StateAtIntake, string? FileVersion, Guid? ActualUploaderId, Guid OriginalActorId,
    Guid? ReuseDecisionId, string CaptureFactsJson);

public sealed record RepairExecutionStart(Guid Id, Guid ProjectId, Guid ItemId, Guid BindingId,
    Guid AssessmentId, Guid FirstStartId, Guid OriginalActorId, Guid OperationOriginId, Guid OriginId,
    string ContentHash, DateTimeOffset ClaimedAt, DateTimeOffset ServerReceivedAt,
    DateTimeOffset? VerifiedOriginalAt, RepairTimeProvenance TimeProvenance,
    string AssessmentContentHash, string ChecklistVersion, string AuthorityFactsJson, Guid? EligibilityAssessmentId);
public sealed record RepairExecutionFinish(Guid Id, Guid ProjectId, Guid ItemId, Guid BindingId,
    Guid ExecutionStartId, Guid OriginalActorId, Guid OperationOriginId, Guid OriginId,
    string ContentHash, DateTimeOffset ClaimedAt, DateTimeOffset ServerReceivedAt,
    DateTimeOffset? VerifiedOriginalAt, RepairTimeProvenance TimeProvenance, string TimeProofFactsJson);

/// <summary>Each link pins an actual immutable H3 revision. Supplements do not rewrite the physical attempt.</summary>
public sealed record RepairAttemptSubmissionLink(Guid Id, Guid ProjectId, Guid ItemId, Guid BindingId,
    Guid AttemptId, Guid SubmissionId, Guid FormalRootSubmissionId, Guid? PreviousLinkId,
    Guid? ExecutionFinishId, string SubmissionContentHash, DateTimeOffset FormalRootServerReceivedAt,
    Guid ReviewClockId, DateTimeOffset OriginalReviewDueAt);
public sealed record RepairAttemptReview(Guid Id, Guid ProjectId, Guid ItemId, Guid BindingId,
    Guid AttemptId, Guid IntakeLinkId, Guid SubmissionId, string SubmissionContentHash,
    string PlanHash, string ChecklistVersion, Guid ActorId, UserRoleCode Role,
    string Decision, string Reason, DateTimeOffset At, bool EvidenceSufficient,
    RepairFactState ExecutionAuthority, string AssessmentFactsJson);

public sealed record RepairItemLifecycleEvent(Guid Id, Guid ProjectId, Guid DefectId, Guid ObligationId,
    Guid ItemId, RepairMode Mode, string Kind, Guid ActorId, UserRoleCode Role, string Reason,
    DateTimeOffset At, Guid? BindingId, Guid? AttemptId, Guid? SubmissionId, Guid? ReviewId,
    Guid? DecisionId, string SourceVersion);

/// <summary>Explicit same-obligation normal continuation; this record supplies no new FT window or execution grant.</summary>
public sealed record RepairNormalSuccessor(Guid Id, Guid ProjectId, Guid ObligationId, Guid SourceItemId,
    Guid TargetItemId, Guid? SourceDecisionId, Guid ActorId, string Reason, DateTimeOffset At, Guid? SourceAssessmentId = null,
    Guid? SourceCancellationEventId = null, Guid? SourceHandoverEventId = null);

/// <summary>Raw existing coverage/handover snapshots and independently assessed predicates.
/// UNKNOWN owner mapping stays UNKNOWN; a pinned row or signed phone payload alone supplies no eligibility.</summary>
public sealed record RepairEligibilityAssessment(Guid Id, Guid ProjectId, Guid ItemId, Guid BindingId,
    Guid AssessmentId, Guid RoadSectionId, Guid PolicyRevisionId, string PolicyContentHash,
    RepairFactState RoadHandover, RepairFactState Coverage, string SourceMapping,
    string SourceFactsHash, DateTimeOffset EvaluatedAt, string MissingReasonsJson)
{
    private List<RepairEligibilityWarrantySource> _warranties = [];
    private List<RepairEligibilityHandoverSource> _handovers = [];
    public IReadOnlyList<RepairEligibilityWarrantySource> Warranties
    { get => _warranties.AsReadOnly(); init => _warranties = value.ToList(); }
    public IReadOnlyList<RepairEligibilityHandoverSource> Handovers
    { get => _handovers.AsReadOnly(); init => _handovers = value.ToList(); }
}
public sealed record RepairEligibilityWarrantySource(Guid WarrantyId, string RowVersion, string ContentHash,
    string FactsJson, Guid? SourceDocumentId);
public sealed record RepairEligibilityHandoverSource(Guid HandoverDocumentId, string RowVersion, string ContentHash,
    string FactsJson, Guid? FileId);
