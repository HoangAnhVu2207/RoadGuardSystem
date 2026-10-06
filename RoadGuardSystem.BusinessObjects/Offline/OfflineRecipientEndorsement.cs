using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed record OfflineRecipientImportClaim(Guid ProjectId, Guid PackageId, Guid GrantId,
    Guid ImportBatchId, Guid RecipientDeviceRegistrationId, string AttachedSourcePayloadHash);

public static class OfflineRecipientEndorsement
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static byte[] CanonicalClaim(OfflineRecipientImportClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        OfflineRuntimeGuards.Identity(claim.ProjectId, claim.PackageId, claim.GrantId, claim.ImportBatchId,
            claim.RecipientDeviceRegistrationId);
        OfflineRuntimeGuards.Hash(claim.AttachedSourcePayloadHash);
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            purpose = "roadguard.h5.recipient-import.v1",
            schemaVersion = 1,
            claim.ProjectId,
            claim.PackageId,
            claim.GrantId,
            claim.ImportBatchId,
            claim.RecipientDeviceRegistrationId,
            attachedSourcePayloadHash = claim.AttachedSourcePayloadHash.ToLowerInvariant()
        }, Json);
    }
}
