using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Messaging;

// Finite internal bookkeeping event: no notification recipient or business command authority.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record H6RepairCorrectionAuditDto(int SchemaVersion, Guid EventId, Guid OriginEventId,
    Guid ProjectId, string Kind, string SourceKind, Guid SourceId, Guid SourceRevisionId,
    DateTimeOffset OccurredAtUtc, Guid ObligationId, Guid SupersedesDecisionId, string Result);
public sealed record H6RepairCorrectionAuditFacts(Guid ObligationId, Guid SupersedesDecisionId, string Result);
