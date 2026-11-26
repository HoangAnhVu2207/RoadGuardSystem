using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Inspections;

// Assignments/events retain their existing wire. Details is present for PM/Supervisor only.
public sealed record FieldHistoryView(JsonElement[] Assignments, JsonElement[] Events,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] FieldHistoryDetailsView? Details = null);
public sealed record FieldHistoryDetailsView(FieldSubmissionHistoryView[] Submissions,
    FieldReviewHistoryView[] Reviews, FieldReuseHistoryView[] EvidenceReuse);
public sealed record FieldSubmissionHistoryView(Guid Id, Guid RootId, Guid? ParentSubmissionId,
    int Revision, string ContentHash, string Readiness, DateTimeOffset ServerReceivedAt);
public sealed record FieldReviewHistoryView(Guid Id, Guid SubmissionId, Guid ActorId,
    string Decision, string Reason, DateTimeOffset OccurredAt, string ReceiptActivation);
public sealed record FieldReuseHistoryView(Guid Id, Guid FileId, Guid SourceEvidenceId, Guid ActorId,
    string SourceKind, string FileChecksum, string Reason, DateTimeOffset OccurredAt, JsonElement Provenance);
