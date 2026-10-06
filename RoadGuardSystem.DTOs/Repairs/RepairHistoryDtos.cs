namespace RoadGuardSystem.DTOs.Repairs;

public sealed record RepairHistoryView(RepairItemView Item, RepairDecisionHistoryView[] Decisions,
    RepairAttemptHistoryView[] Attempts, RepairReviewRequestHistoryView[] ReviewRequests);

public sealed record RepairDecisionHistoryView(Guid Id, Guid ItemId, Guid ObligationId, Guid DefectId,
    string Mode, Guid ActorId, string Role, string Reason, DateTimeOffset At, Guid? SupersedesDecisionId,
    string Result, RepairCorrectionBasisHistoryView? Basis);

public sealed record RepairCorrectionBasisHistoryView(string Text, RepairEvidenceHistoryView[] Evidence);

public sealed record RepairEvidenceHistoryView(Guid FileId, string? FileVersion, string? Sha256, string Purpose,
    bool Verified, string SourceKind, Guid SourceId, DateTimeOffset? CapturedAt, Guid? ReuseDecisionId, bool ReusedSource);

public sealed record RepairAttemptHistoryView(Guid Id, Guid OriginId, string ContentHash, Guid ItemId,
    Guid ObligationId, Guid ProjectId, Guid DefectId, Guid CrewId, Guid TaskId, Guid AssignmentId,
    Guid? AuthorizationId, string LocationVersion, Guid? PolicyRevisionId, bool Performed, string? UnperformedReason,
    DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, DateTimeOffset ServerReceivedAt,
    string TimeProvenance, RepairEvidenceHistoryView[] Evidence);

public sealed record RepairReviewRequestHistoryView(Guid Id, Guid ItemId, Guid DecisionId, Guid ActorId,
    string Role, string Reason, DateTimeOffset At);
