using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Repairs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairCorrectionBasisInput(string Text, Guid[] EvidenceReferenceIds);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairCorrectionInput(Guid SupersedesDecisionId, string Result, string Reason, RepairCorrectionBasisInput Basis);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RepairReviewRequestInput(string Reason);

public sealed record RepairReviewRequestView(Guid Id, Guid ItemId, Guid DecisionId, Guid ActorId,
    string Role, string Reason, DateTimeOffset At, string Version);

public sealed record RepairCorrectionView(Guid Id, Guid ItemId, Guid ObligationId, Guid SupersedesDecisionId,
    string Result, string Reason, string Basis, Guid[] EvidenceReferenceIds, Guid ActorId, DateTimeOffset At,
    bool ObligationResolved, string ItemState, string Version);
