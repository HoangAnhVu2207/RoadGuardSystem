# Offline sync, crypto wire và canonical source

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**Phase 2C · 15/10/2026 · implementation source pinned `36c3a1d273f17dd29b816136ae0182a821fd79e4` · independent JEV pending.**

[API entrance](README.md) · [Review/evidence](PHASE2C_REVIEW_REPORT.md) · [10 workflows](../02_Business-Workflows/README.md) · [Backend/Data](../03_Backend-and-Data/PHASE2B_REVIEW_REPORT.md).

`CURRENT_VERIFIED` = source/static contract discovery, không HTTP/E2E/deployment PASS. `CURRENT_IMPLEMENTED` trong phần AI là phân nhóm contract BE đã có, không nhãn real provider acceptance. `TARGET_CONFIRMED` client/business theo [owner decisions](../../product/confirmed-decisions.md), [latest HUY](../../../planning/development/HUY-FINAL-INTEGRATION.md) và owner Phase2C authorization; precedence theo [governance](../00_Governance/01_Authority-and-Source-of-Truth.md). `PROPOSED`, `NOT_VERIFIED`, `HISTORICAL` giữ phạm vi. Product HoangHaiWarrantySystem; technical RoadGuardSystem/routes giữ nguyên.

## Current wire and custody

API-113..130 are primary offline contracts; request DTOs OfflineDeviceRegisterInput/OfflineSnapshotInput/OfflineSignedBatchInput/OfflinePackageImportInput/OfflinePackageExportInput and nested typed payloads are exact schema references in chapter03. Unknown fields Disallow attributes apply where source declares them. HTTP register DeviceId and operation SourceDeviceId are **Guid**; reference crypto OfflineDevicePublicKeys.DeviceId is a string identity in its own domain, not permission to send arbitrary string DeviceId to HTTP.

PM/Crew enrollment/writes, Supervisor grant issue/revoke/package prepare, artifact register Supervisor/PM/Crew, scoped reads Supervisor/PM/Crew. Source keys must be current independently registered public keys, not keys attached by untrusted sender. Separate encryption ECDH P-256 and signing ECDSA P-256 SPKI DER Base64, canonical complete import and curveOID1.2.840.10045.3.1.7. Keys max128decoded bytes at import; reject identical signing/encryption keys. Client retains private keys; no server private-key issuance/escrow/recovery inferred. Reporter/Operator not assigned signed sync.

## Canonical batch bytes — literal source algorithm, no fabricated fixture

OfflineWorkflowEngine uses JsonSerializerOptions(JsonSerializerDefaults.Web). Descriptor declaration property order: OriginId, EffectId, Kind, EnvelopeHash, CorePayloadHash, TaskId, AssignmentId, SnapshotId, RepairResourceId. Source manifest wrapper property order schemaVersion, projectId, sourceBatchId, sourceDeviceRegistrationId, items; sort descriptors by OriginId.ToString("N") ordinal. Serializer spelling/null/default/Guid/DateTimeOffset/number escaping must match exact source; arbitrary JSON.stringify output isn't canonical just because parsed object looks equal.

Below is actual source excerpt, not request sample or accepted test vector:

```csharp
    public static byte[] CanonicalManifest(Guid projectId, Guid batchId, Guid sourceRegistrationId,
        IReadOnlyList<OfflineOperationDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        if (projectId == Guid.Empty || batchId == Guid.Empty || sourceRegistrationId == Guid.Empty || descriptors.Count is < 1 or > 1000 ||
            descriptors.Any(x => x is null) || descriptors.Select(x => x.OriginId).Distinct().Count() != descriptors.Count)
            throw new ArgumentException("A bounded unique descriptor set is required.", nameof(descriptors));
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = 1,
            projectId,
            sourceBatchId = batchId,
            sourceDeviceRegistrationId = sourceRegistrationId,
            items = descriptors.OrderBy(x => x.OriginId.ToString("N"), StringComparer.Ordinal).ToArray()
        }, Json);
    }
```


EnvelopeHash = lowercase hex SHA256(JsonSerializer.SerializeToUtf8Bytes(operation, Web options)); core payload hash is command-specific FIELD/repair canonical source, **not** interchangeable with envelope hash. OfflineContractMapping serializes wire/internal data both ways and verifies byte equality; no extra/null-normalized fields injected during mapping. Describe validates schema/kind/typed payload/origin/device/dependency shape. Preserve array/dependency/operation order unless exact source method sorts it.

VerifyClaim digest = SHA256(UTF8 literal `roadguard.h5.direct-sync.v1` + NUL byte + canonicalClaim bytes), then ECDSA verify with registered P256 key; signature Base64 canonical bytes, decoded length64, IEEE-P1363 fixed-field r||s (not DER). `ClaimPurpose` source includes terminator. Base64 must roundtrip exactly; hashes64lowercasehex. CanonicalAttachedPayload serializes OfflineSignedBatchInput(sourceBatchId,sourceRegistrationId,operations,sourceSignature) under Web options; don't confuse signature over descriptors with attached payload content identity.

## Encrypted package is a distinct canonical format

OfflineCryptoFormat writes UTF8 using Utf8JsonWriter with UnsafeRelaxedJsonEscaping. Header order version,payloadSchema,packageId,projectId,originalActorId,sourceDeviceId,originIds,payloadSha256; GUIDs explicitly D, origins canonical sorted by N. Unsigned package order header,payloadNonce,ciphertext,payloadTag,recipients,senderSigningPublicKey; recipient order/identity/keywrap fields follow writer/validated SnapshotPackage source. Package signature excludes signature field, SHA256(CanonicalUnsignedPackage), not direct-sync ClaimDigest. Structural PackageContentFingerprint also excludes varying signature and is not authentication.

Version1/payloadSchema huy-final.offline.bundle.v1; origins≤1000, recipients≤32, sealed payload≤16MiB are protocol bounds, not an overall photo/evidence collection limit. AES/keywrap/nonce/tag and chunk/artifact canonical source must be read from OfflineHandoverCrypto/OfflineCaptureTransfer and DTO converter; no alternative format invented. Encryption authenticates bounded package under registered keys, doesn't prove local SQLite/photo encryption or real person/time.

## Receipts, dependencies, reconciliation and handover

Initial source/project/snapshot/signature/manifest guards can reject full batch before per-item processing. Supported registry then commits per-item receipt/effect. Actual result strings include COMMITTED, REPLAYED, PENDING_DEPENDENCY, STALE_SNAPSHOT, CONFLICT; don't treat200 as all items ACK. ReadyOrigins only dependencies with COMMITTED/REPLAYED. Source receipt/current rights/key/grant/assignment checks still apply on replay/import; no last-write-wins or ID reset.

SyncLateness: independently verified finish exists → server received strictly before finish+24h ON_TIME, otherwise LATE; no verified finish UNKNOWN. Device claimed timestamp/signature isn't historic UTC. Original/first-start/origin/task/snapshot/source hash/actor preserved through reconcile and import. Handover source signature/package/grant/recipient signature/admission are separate; importer isn't original measuring Crew. Recipient grant doesn't grant PM/Crew authority generally. ND01 FT original24h conservative current admission remains; post-expiry extra data deferred.

Lost private key/new device/uninstall may strand unacked evidence; public keys cannot recover private keys. Native Expo interoperability, secure keys/queue/local encryption/kill-restart/key-loss policy NOT_VERIFIED; .NET reference codec isn't FE implementation. No invented accepted crypto vectors. Test source references in catalog are NOT_RUN; actual interop/E2E NOT_RUN.

Source: [engine](../../../RoadGuardSystem.Services/Implementations/Offline/OfflineWorkflowEngine.cs#L111), [mapping](../../../RoadGuardSystem.Services/Implementations/Offline/OfflineContractMapping.cs), [wire DTO](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs), [crypto writer](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineCryptoFormat.cs), [authentication/digest](../../../RoadGuardSystem.BusinessObjects/Offline/OfflinePackageAuthentication.cs), [key custody](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceKeys.cs), [sync](../../../RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.Sync.cs), [transfer](../../../RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.Transfer.cs). Exact pinned source is final authority for byte interoperability.
