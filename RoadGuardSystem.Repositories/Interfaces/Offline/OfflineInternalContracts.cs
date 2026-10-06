using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Repairs;

namespace RoadGuardSystem.Repositories.Offline;

public sealed record OfflineDeviceRegisterData(Guid DeviceId, string EncryptionPublicKey, string SigningPublicKey);
public sealed record OfflineDeviceRevokeData(string Reason);
public sealed record OfflineSnapshotData(Guid DeviceRegistrationId, Guid TaskId);
public sealed record OfflineGrantItemData(Guid OriginId, string Kind, string CorePayloadHash, string EnvelopeHash,
    Guid TaskId, Guid AssignmentId, Guid SnapshotId, Guid? RepairResourceId = null);
public sealed record OfflineHandoverGrantData(Guid PackageId, Guid RecipientDeviceRegistrationId,
    OfflineGrantItemData[] Items, string Reason);
public sealed record OfflineGrantRevokeData(string Reason);
public sealed record OfflineOperationDescriptorData(Guid OriginId, Guid EffectId, string Kind, string EnvelopeHash,
    string CorePayloadHash, Guid TaskId, Guid AssignmentId, Guid SnapshotId, Guid? RepairResourceId);
public sealed record OfflineSourceManifestData(Guid SourceBatchId, Guid SourceDeviceRegistrationId,
    OfflineOperationDescriptorData[] Items, string Signature);
public sealed record OfflinePackageExportData(OfflineEncryptedPackage Package, string ExactCipherPackageJson,
    OfflineSourceManifestData Manifest, Guid? SourceFileId = null);
public sealed record OfflineRepairData(Guid ResourceId, string Action,
    RepairExecutionStartData? Start = null, RepairExecutionFinishData? Finish = null,
    RepairMeasurementAssessmentData? Assessment = null);
public sealed record OfflineOperationData(int SchemaVersion, Guid OriginId, Guid EffectId, string Kind,
    Guid OriginalActorId, Guid SourceDeviceId, Guid SnapshotId, Guid TaskId, Guid AssignmentId,
    string TaskVersion, string CorePayloadHash, Guid[] Dependencies, DateTimeOffset? ClaimedFinishedAt,
    FieldStartData? FieldStart = null, RepairFieldSubmissionData? FieldSubmission = null,
    OfflineRepairData? Repair = null, FieldActionData? FieldAction = null);
public sealed record OfflineSignedBatchData(Guid BatchId, Guid SourceDeviceRegistrationId,
    OfflineOperationData[] Operations, string Signature);
public sealed record OfflinePackageImportData(Guid BatchId, Guid PackageId, Guid GrantId,
    Guid RecipientDeviceRegistrationId, OfflineOperationData[] Operations, string SourceSignature,
    string RecipientSignature);
public sealed record OfflineCaptureArtifactData(OfflineCaptureManifest Manifest, string ManifestSignature,
    int ChunkIndex, OfflineEncryptedPackage Envelope);
public sealed record OfflineStartReconcileData(Guid BatchId, Guid OriginId);

public sealed record OfflineWorkflowFact(int Status, string? Code = null, object? Value = null,
    string? Version = null, bool Replayed = false);
public sealed record OfflineItemFact(Guid OriginId, string Kind, string State, string? Code,
    Guid? EffectId, Guid? CanonicalOriginId, bool DurableAcknowledgment, DateTimeOffset ServerReceivedAt,
    string TimeProvenance, string SyncLateness, DateTimeOffset? ClaimedFinishedAt,
    DateTimeOffset? VerifiedFinishedAt, DateTimeOffset? VerifiedSyncDueAt, object? Outcome);
public sealed record OfflineBatchFact(Guid BatchId, OfflineItemFact[] Items,
    string LocalQueueInstruction = "KEEP_UNACKNOWLEDGED_OR_CONFLICTED_ITEMS");
