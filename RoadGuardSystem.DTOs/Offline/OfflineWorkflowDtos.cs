using System.Text.Json.Serialization;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Repairs;

namespace RoadGuardSystem.DTOs.Offline;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineDeviceRegisterInput(Guid DeviceId, string EncryptionPublicKey, string SigningPublicKey);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineDeviceRevokeInput(string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineSnapshotInput(Guid DeviceRegistrationId, Guid TaskId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineGrantItemScope(Guid OriginId, string Kind, string CorePayloadHash, string EnvelopeHash,
    Guid TaskId, Guid AssignmentId, Guid SnapshotId, Guid? RepairResourceId = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineHandoverGrantInput(Guid PackageId, Guid RecipientDeviceRegistrationId,
    OfflineGrantItemScope[] Items, string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineGrantRevokeInput(string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineOperationDescriptor(Guid OriginId, Guid EffectId, string Kind, string EnvelopeHash,
    string CorePayloadHash, Guid TaskId, Guid AssignmentId, Guid SnapshotId, Guid? RepairResourceId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineSourceManifestInput(Guid SourceBatchId, Guid SourceDeviceRegistrationId,
    OfflineOperationDescriptor[] Items, string Signature);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflinePackageExportInput(H5EncryptedPackageDto Package, OfflineSourceManifestInput Manifest,
    Guid? SourceFileId = null);

// Existing direct command DTOs are reused without adding fields or changing their canonical core hashes.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineRepairPayload(Guid ResourceId, string Action,
    RepairExecutionStartInput? Start = null, RepairExecutionFinishInput? Finish = null,
    RepairMeasurementAssessmentInput? Assessment = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineOperationInput(int SchemaVersion, Guid OriginId, Guid EffectId, string Kind,
    Guid OriginalActorId, Guid SourceDeviceId, Guid SnapshotId, Guid TaskId, Guid AssignmentId,
    string TaskVersion, string CorePayloadHash, Guid[] Dependencies, DateTimeOffset? ClaimedFinishedAt,
    FieldStartInput? FieldStart = null, FieldSubmissionInput? FieldSubmission = null,
    OfflineRepairPayload? Repair = null, FieldTaskActionInput? FieldAction = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineSignedBatchInput(Guid BatchId, Guid SourceDeviceRegistrationId,
    OfflineOperationInput[] Operations, string Signature);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflinePackageImportInput(Guid BatchId, Guid PackageId, Guid GrantId,
    Guid RecipientDeviceRegistrationId, OfflineOperationInput[] Operations, string SourceSignature,
    string RecipientSignature);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record OfflineStartReconcileInput(Guid BatchId, Guid OriginId);

public sealed record OfflineWorkflowResult(int Status, string? Code = null, object? Value = null,
    string? Version = null, bool Replayed = false);
public sealed record OfflineItemResult(Guid OriginId, string Kind, string State, string? Code,
    Guid? EffectId, Guid? CanonicalOriginId, bool DurableAcknowledgment, DateTimeOffset ServerReceivedAt,
    string TimeProvenance, string SyncLateness, DateTimeOffset? ClaimedFinishedAt,
    DateTimeOffset? VerifiedFinishedAt, DateTimeOffset? VerifiedSyncDueAt, object? Outcome);
public sealed record OfflineBatchResult(Guid BatchId, OfflineItemResult[] Items,
    string LocalQueueInstruction = "KEEP_UNACKNOWLEDGED_OR_CONFLICTED_ITEMS");
