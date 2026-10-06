using RoadGuardSystem.BusinessObjects.Inspections;
using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Offline;

// Immutable claimed identity; effects remain in the existing canonical project/origin registry.
public sealed class OfflineOperationBinding
{
    private OfflineOperationBinding() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid OriginId { get; private set; }
    public Guid EffectId { get; private set; }
    public string Kind { get; private set; } = "";
    public string CorePayloadHash { get; private set; } = "";
    public string EnvelopeHash { get; private set; } = "";
    public Guid OriginalActorId { get; private set; }
    public Guid SourceDeviceRegistrationId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid? RepairResourceId { get; private set; }
    public string EnvelopeJson { get; private set; } = "{}";
    public DateTimeOffset FirstServerReceivedAt { get; private set; }
    public static OfflineOperationBinding BindHistoricalStart(Guid id, FieldInspectionOperationOrigin canonical,
        FieldTaskStartOrigin actualStart, Guid sourceRegistration, Guid snapshot, string envelopeHash,
        string envelopeJson, DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(canonical); ArgumentNullException.ThrowIfNull(actualStart);
        OfflineRuntimeGuards.Identity(id, sourceRegistration, snapshot); OfflineRuntimeGuards.Time(receivedAt);
        OfflineRuntimeGuards.Hash(envelopeHash); OfflineRuntimeGuards.Json(envelopeJson, 1048576);
        if (canonical.SchemaVersion != 1 || canonical.Kind != "FIELD_START" || actualStart.OperationKind != "FIELD_START" ||
            canonical.ProjectId != actualStart.ProjectId || canonical.Id != actualStart.OperationOriginId ||
            canonical.EffectId != actualStart.Id || canonical.TaskId != actualStart.TaskId ||
            canonical.OriginId != actualStart.OriginId || canonical.OriginalActorId != actualStart.OriginalActorId ||
            canonical.DeviceId != actualStart.DeviceId || canonical.ContentHash != actualStart.ContentHash ||
            !string.Equals(OfflineRuntimeGuards.Digest(envelopeJson), envelopeHash, StringComparison.Ordinal))
            throw new ArgumentException("Historical binding requires the exact retained canonical start.");
        using var document = JsonDocument.Parse(envelopeJson);
        var envelope = document.RootElement;
        bool SameGuid(string name, Guid expected) => envelope.TryGetProperty(name, out var value) && value.TryGetGuid(out var actual) && actual == expected;
        if (!envelope.TryGetProperty("schemaVersion", out var schema) || !schema.TryGetInt32(out var schemaValue) || schemaValue != 1 ||
            !envelope.TryGetProperty("kind", out var kind) || kind.GetString() != "FIELD_START" ||
            !SameGuid("originId", canonical.OriginId) || !SameGuid("effectId", actualStart.Id) ||
            !SameGuid("taskId", actualStart.TaskId) || !SameGuid("assignmentId", actualStart.AssignmentId) ||
            !SameGuid("originalActorId", actualStart.OriginalActorId) || !SameGuid("snapshotId", snapshot) ||
            !envelope.TryGetProperty("corePayloadHash", out var coreHash) || coreHash.GetString() != canonical.ContentHash ||
            !envelope.TryGetProperty("fieldStart", out var body) || body.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("The historical envelope differs from its retained start identity.");
        foreach (var competingBody in new[] { "fieldAction", "fieldSubmission", "repair" })
            if (envelope.TryGetProperty(competingBody, out var value) && value.ValueKind != JsonValueKind.Null)
                throw new ArgumentException("A historical start has one exact typed body.");
        // Reserializing JsonElement escapes '+' in DateTimeOffset strings differently from the
        // original typed FIELD serializer. Preserve the body bytes used by that canonical hash.
        var canonicalCoreJson = "{\"id\":" + JsonSerializer.Serialize(actualStart.TaskId) +
            ",\"input\":" + body.GetRawText() + ",\"originalActorId\":" +
            JsonSerializer.Serialize(actualStart.OriginalActorId) + "}";
        if (OfflineRuntimeGuards.Digest(canonicalCoreJson) != canonical.ContentHash)
            throw new ArgumentException("The original body hash cannot be rewritten for historical binding.");
        return new()
        {
            Id = id,
            ProjectId = canonical.ProjectId,
            OriginId = canonical.OriginId,
            EffectId = actualStart.Id,
            Kind = "FIELD_START",
            CorePayloadHash = canonical.ContentHash,
            EnvelopeHash = envelopeHash,
            OriginalActorId = actualStart.OriginalActorId,
            SourceDeviceRegistrationId = sourceRegistration,
            TaskId = actualStart.TaskId,
            AssignmentId = actualStart.AssignmentId,
            SnapshotId = snapshot,
            EnvelopeJson = envelopeJson,
            FirstServerReceivedAt = receivedAt.ToUniversalTime()
        };
    }
    public static OfflineOperationBinding Bind(Guid id, Guid project, Guid origin, Guid effect, string kind,
        string coreHash, string envelopeHash, Guid originalActor, Guid sourceRegistration, Guid task,
        Guid assignment, Guid snapshot, string envelopeJson, DateTimeOffset at, Guid? repairResourceId = null)
    {
        OfflineRuntimeGuards.Identity(id, project, origin, effect, originalActor, sourceRegistration, task, assignment, snapshot);
        OfflineRuntimeGuards.Time(at); OfflineRuntimeGuards.Hash(coreHash); OfflineRuntimeGuards.Hash(envelopeHash);
        OfflineRuntimeGuards.Json(envelopeJson, 1048576);
        if (effect != origin || kind is not ("FIELD_ACCEPT" or "FIELD_START" or "FIELD_SUBMISSION" or "REPAIR_ASSESSMENT" or "REPAIR_EXECUTION_START" or "REPAIR_EXECUTION_FINISH"))
            throw new ArgumentException("The versioned operation identity is invalid.");
        var repair = kind is "REPAIR_ASSESSMENT" or "REPAIR_EXECUTION_START" or "REPAIR_EXECUTION_FINISH";
        if (repairResourceId == Guid.Empty || repair != repairResourceId.HasValue)
            throw new ArgumentException("Repair kinds require an explicit actual item pin; FIELD kinds have none.");
        return new()
        {
            Id = id,
            ProjectId = project,
            OriginId = origin,
            EffectId = effect,
            Kind = kind,
            CorePayloadHash = coreHash.ToLowerInvariant(),
            EnvelopeHash = envelopeHash.ToLowerInvariant(),
            OriginalActorId = originalActor,
            SourceDeviceRegistrationId = sourceRegistration,
            TaskId = task,
            AssignmentId = assignment,
            SnapshotId = snapshot,
            RepairResourceId = repairResourceId,
            EnvelopeJson = envelopeJson,
            FirstServerReceivedAt = at.ToUniversalTime()
        };
    }
}
