using System.Text.Json;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Implementations.Repairs;

namespace RoadGuardSystem.Services.Offline;

public static class OfflineContractMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static OfflineWorkflowAlgorithms RepositoryAlgorithms => new(
        operation => OfflineWorkflowEngine.EnvelopeHash(ToWire(operation)),
        operation => ToData(OfflineWorkflowEngine.Describe(ToWire(operation))),
        (project, batch, device, items) => OfflineWorkflowEngine.CanonicalManifest(project, batch, device,
            items.Select(ToWire).ToArray()),
        (batch, device, items, signature) => OfflineWorkflowEngine.CanonicalAttachedPayload(batch, device,
            items.Select(ToWire).ToArray(), signature),
        (items, states) => OfflineWorkflowEngine.ReadyOrigins(items.Select(ToWire).ToArray(), states),
        OfflineWorkflowEngine.SyncLateness);

    public static object? ToData(string action, object? input) => action switch
    {
        "device-register" when input is OfflineDeviceRegisterInput value =>
            new OfflineDeviceRegisterData(value.DeviceId, value.EncryptionPublicKey, value.SigningPublicKey),
        "device-revoke" when input is OfflineDeviceRevokeInput value => new OfflineDeviceRevokeData(value.Reason),
        "snapshot-create" when input is OfflineSnapshotInput value =>
            new OfflineSnapshotData(value.DeviceRegistrationId, value.TaskId),
        "grant-issue" when input is OfflineHandoverGrantInput value =>
            new OfflineHandoverGrantData(value.PackageId, value.RecipientDeviceRegistrationId,
                value.Items.Select(item => new OfflineGrantItemData(item.OriginId, item.Kind,
                    item.CorePayloadHash, item.EnvelopeHash, item.TaskId, item.AssignmentId,
                    item.SnapshotId, item.RepairResourceId)).ToArray(), value.Reason),
        "grant-revoke" when input is OfflineGrantRevokeInput value => new OfflineGrantRevokeData(value.Reason),
        "package-export" or "package-prepare" when input is OfflinePackageExportInput value =>
            new OfflinePackageExportData(value.Package.ToDomain(), JsonSerializer.Serialize(value.Package, Json),
                new(value.Manifest.SourceBatchId, value.Manifest.SourceDeviceRegistrationId,
                    value.Manifest.Items.Select(ToData).ToArray(), value.Manifest.Signature), value.SourceFileId),
        "artifact-register" when input is OfflineCaptureArtifactInput value =>
            new OfflineCaptureArtifactData(value.Manifest, value.ManifestSignature, value.ChunkIndex,
                value.Envelope.ToDomain()),
        "sync" when input is OfflineSignedBatchInput value =>
            new OfflineSignedBatchData(value.BatchId, value.SourceDeviceRegistrationId,
                value.Operations.Select(ToData).ToArray(), value.Signature),
        "import" when input is OfflinePackageImportInput value =>
            new OfflinePackageImportData(value.BatchId, value.PackageId, value.GrantId,
                value.RecipientDeviceRegistrationId, value.Operations.Select(ToData).ToArray(),
                value.SourceSignature, value.RecipientSignature),
        "start-reconcile" when input is OfflineStartReconcileInput value =>
            new OfflineStartReconcileData(value.BatchId, value.OriginId),
        _ => input
    };

    public static OfflineOperationData ToData(OfflineOperationInput value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var mapped = new OfflineOperationData(value.SchemaVersion, value.OriginId, value.EffectId,
            value.Kind, value.OriginalActorId, value.SourceDeviceId, value.SnapshotId, value.TaskId,
            value.AssignmentId, value.TaskVersion, value.CorePayloadHash, value.Dependencies,
            value.ClaimedFinishedAt,
            value.FieldStart is null ? null : new FieldStartData(value.FieldStart.OriginId,
                value.FieldStart.ClaimedAt, value.FieldStart.DeviceId,
                value.FieldStart.MonotonicMilliseconds, value.FieldStart.BootId, value.FieldStart.OfflineProof),
            value.FieldSubmission is null ? null : RepairContractMapping.ToData(value.FieldSubmission),
            value.Repair is null ? null : new OfflineRepairData(value.Repair.ResourceId, value.Repair.Action,
                value.Repair.Start is null ? null : RepairContractMapping.ToData(value.Repair.Start),
                value.Repair.Finish is null ? null : RepairContractMapping.ToData(value.Repair.Finish),
                value.Repair.Assessment is null ? null : RepairContractMapping.ToData(value.Repair.Assessment)),
            value.FieldAction is null ? null : new FieldActionData(value.FieldAction.Reason,
                value.FieldAction.AssignedToUserId, value.FieldAction.Handover is null ? null :
                    new FieldHandoverData(value.FieldAction.Handover.PerformedPortionState,
                        value.FieldAction.Handover.Summary, value.FieldAction.Handover.StartOriginId,
                        value.FieldAction.Handover.SubmissionIds,
                        value.FieldAction.Handover.RecipientUserId)));
        if (!JsonSerializer.SerializeToUtf8Bytes(value, Json).AsSpan()
            .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(mapped, Json)))
            throw new ArgumentException("Internal offline mapping changed signed envelope bytes.", nameof(value));
        return mapped;
    }

    public static OfflineOperationDescriptorData ToData(OfflineOperationDescriptor value)
        => new(value.OriginId, value.EffectId, value.Kind, value.EnvelopeHash,
            value.CorePayloadHash, value.TaskId, value.AssignmentId, value.SnapshotId,
            value.RepairResourceId);

    public static RepairFieldEvidenceData ToData(FieldEvidenceDeclaration value)
        => RepairContractMapping.ToData(value);

    public static OfflineOperationInput ToWire(OfflineOperationData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        var wire = JsonSerializer.Deserialize<OfflineOperationInput>(bytes, Json)
            ?? throw new ArgumentException("Internal offline operation is invalid.", nameof(value));
        if (!bytes.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(wire, Json)))
            throw new ArgumentException("Internal offline operation changed canonical wire bytes.", nameof(value));
        return wire;
    }

    public static OfflineOperationDescriptor ToWire(OfflineOperationDescriptorData value)
        => new(value.OriginId, value.EffectId, value.Kind, value.EnvelopeHash,
            value.CorePayloadHash, value.TaskId, value.AssignmentId, value.SnapshotId,
            value.RepairResourceId);

    public static OfflineWorkflowResult ToWire(OfflineWorkflowFact value)
        => new(value.Status, value.Code, value.Value is OfflineBatchFact batch
            ? new OfflineBatchResult(batch.BatchId, batch.Items.Select(item => new OfflineItemResult(
                item.OriginId, item.Kind, item.State, item.Code, item.EffectId, item.CanonicalOriginId,
                item.DurableAcknowledgment, item.ServerReceivedAt, item.TimeProvenance, item.SyncLateness,
                item.ClaimedFinishedAt, item.VerifiedFinishedAt, item.VerifiedSyncDueAt, item.Outcome)).ToArray(),
                batch.LocalQueueInstruction)
            : value.Value is OfflineItemFact item
                ? new OfflineItemResult(item.OriginId, item.Kind, item.State, item.Code, item.EffectId,
                    item.CanonicalOriginId, item.DurableAcknowledgment, item.ServerReceivedAt,
                    item.TimeProvenance, item.SyncLateness, item.ClaimedFinishedAt, item.VerifiedFinishedAt,
                    item.VerifiedSyncDueAt, item.Outcome)
                : value.Value, value.Version, value.Replayed);
}
