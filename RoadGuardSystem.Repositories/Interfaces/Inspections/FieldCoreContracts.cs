using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.Repositories.Inspections;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldStartData(Guid OriginId, DateTimeOffset ClaimedAt, Guid? DeviceId = null,
    long? MonotonicMilliseconds = null, string? BootId = null, string? OfflineProof = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldHandoverData(string PerformedPortionState, string Summary, Guid StartOriginId,
    Guid[] SubmissionIds, Guid? RecipientUserId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record FieldActionData(string Reason, Guid? AssignedToUserId = null, FieldHandoverData? Handover = null);
public sealed record FieldCoreOutcome(int Status, string? Code = null, JsonElement? Value = null,
    string? Version = null, bool Replayed = false);
