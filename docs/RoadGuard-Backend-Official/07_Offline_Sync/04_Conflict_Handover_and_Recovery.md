# Conflict, bàn giao và phục hồi dữ liệu offline

Baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`; `huy-review`; đối chiếu source 13/10/2026. `CURRENT_VERIFIED` = đọc implementation; tests `NOT_RUN` trong BE-DOC-02. Kết quả cũ là `HISTORICAL`; không có live SQL/Android/storage/deployment acceptance trong task này.

Authority: [planning/development/HUY-FINAL-INTEGRATION.md](../../../planning/development/HUY-FINAL-INTEGRATION.md) (ND01 L3–15; R08 L679, R09 L684, R20–R22 L750–772, R29/R30 L812–825), [docs/product/confirmed-decisions.md](../../../docs/product/confirmed-decisions.md) (37/42A/43A), [docs/audit/DOC-AUDIT-01-OPEN-GAPS.md](../../../docs/audit/DOC-AUDIT-01-OPEN-GAPS.md).

## EXPECTED_BUSINESS_BEHAVIOR — TARGET_CONFIRMED

R09/R15/R20–R22/R29: giữ chứng cứ và queue khi conflict hoặc mất ACK. Khi đang làm cần ghi phần thực hiện/bàn giao, submission đã nộp immutable. Device handover tách data admission, không chuyển login/account/quyền execute. Grant giới hạn project/source-device/action; người nhận còn quyền và original actor được giữ. Thu hồi không tức thì trên thiết bị offline; reconnect/recovery vẫn kiểm quyền hiện tại. Mất key không được hứa server decrypt/escrow khi chưa có chính sách và implementation.

## CURRENT_IMPLEMENTATION: actor và tiền điều kiện

`OfflineWorkflowRepository.Transfer` xử lý export/prepare/get package, issue/get/revoke grant, import, artifact register/get, batch/origin get, starts/reconcile. Service chỉ Supervisor prepare/grant issue/revoke; PM/Crew export/import/reconcile; artifact register cho PM/Supervisor/Crew nhưng resource/signature/current grant guard hẹp hơn. All protected reads kiểm current scope; package/artifact access cho current Supervisor, source owner hoặc recipient grant hiện hành theo ReadTransferAsync. Imported batch/status chỉ actual importer đã lưu và current transport proof hợp lệ.

Package có encrypted payload/header/recipient wraps/source signature và manifest descriptor scope. Sender source registration phải thật/not revoked, encrypted package/header/hash/signature khớp manifest. Grant pin package/source/recipient device/actor/items/reason/issue time/expires=issue+24h. Grant upload window24h là clock riêng, không cấp thêm FT execution24h.

## Luồng bàn giao và artifact

1. Source tạo signed encrypted package và source manifest; POST packages/export đăng ký package/bindings. POST packages/prepare là Supervisor ghi package đã được source ký, không backend tự decrypt hoặc tự ký thành actor nguồn.
2. Supervisor POST grants chỉ cho current recipient registration/actor và đúng descriptor origins/kinds/core/envelope/task/assignment/snapshot/repair-resource. Persist grant + scope + device-handover clock; revoke append reason/history.
3. Recipient decrypt trên thiết bị bằng key wrap phù hợp, POST import với package/grant/recipient registration, nguyên operations, sourceSignature và recipientSignature. Repository verify source payload/manifest, recipient endorsement và active grant. Chỉ import action scope đã pin; signature transport không thay nguyên Crew actor.
4. Shared sync core nhập đúng origin/effect với AdmissionMode HANDOVER, CurrentImporterId/ImporterRole và GrantId; signed declaration giữ FileId gốc (có thể null). Offline upload resolver liên kết verified file của actual uploader với original CaptureOriginId/declaration, không rewrite signed content.
5. POST artifacts nhận capture manifest+manifest signature+ChunkIndex+encrypted envelope; GET artifacts trả encrypted metadata/chunks theo authority. Artifact không tự trở thành file VERIFIED. Capture upload phải qua admitted capture/FileSession/verified checksum và evidence relation, giữ actual uploader khác original actor.

State machine: package registered immutable; grant active → expired hoặc revoked; imported operation durable hoặc conflict theo sync state. Grant expiry không reset khi replay/read/prepare; direct device handover không chuyển assignment. AFTER source vẫn phải mới đúng checklist/attempt; quyền recipient upload không thay quyền Crew execution.

## Recovery, conflict và queue

GET batches/{batchId} lấy attached signed payload/hash/source signature và actual importer; `GuardBatchStatusAsync` kiểm direct source hoặc full import transport hiện tại trước trả item results. GET origins/{originId} tra operation binding/admissions/latest result/canonical effect có quyền; không public global origin lookup. POST starts/reconcile chỉ nhận BatchId+OriginId, load lại persisted signed batch với đúng một FIELD_START và đúng origin rồi chạy ProcessBatchAsync; không cho client chọn arbitrary timestamp hoặc tự xác minh thời gian.

Conflict/stale snapshot/mất membership/grant hết hạn không được tự ACK hoặc bỏ local queue. Giữ immutable payload/evidence để PM review, update current task/assignment/policy/impact bằng luồng được phép. Existing effect retry trả canonical receipt chỉ sau current proof checks; ACK loss không đòi tạo origin mới. Source content khác cùng origin⇒409 origin_content_conflict. Start recovery không renew execution expiry. Evidence download tiếp tục cần verified relation/current permission, không cấp từ notification cũ.

## HTTP, DTO, concurrency và persistence

Exact routes/actions và body ở [kiến trúc](01_Offline_Backend_Architecture.md); `OfflinePackageExportInput`, `OfflineHandoverGrantInput`, `OfflinePackageImportInput`, `OfflineStartReconcileInput` ở [DTO snapshot/sync](02_Snapshot_and_Device_Management.md). Capture/H5 encrypted DTO bên dưới. Artifact-register/signature/key/manifest sai⇒400/403 theo validation; grant revoked⇒403 handover_grant_revoked; expired⇒403 handover_grant_expired; device key revoke⇒403 offline_key_revoked; missing404; conflict409; key thiếu428. ProblemDetails code/correlationId theo controller.

Transfer writes dùng serializable IdempotencyOperationService/receipt guard; reads own serializable transaction khi cần. Package/origin binding/grant scope và recipient endorsement hash kiểm exact content. Captures bounded chunk index<32, chunk length≤16777216, offsets/manifest size/type/hash constraints trong EF; unique ParentPackageId+CaptureOriginId+ManifestHash+ChunkIndex. OfflineAdmittedFileReference/PackageFileReference/EvidenceCaptureReference và original declaration snapshot giữ file provenance qua recipient upload. EF source không xác nhận physical SQL/object storage deploy.

Crypto ECDH/P-256, AES-GCM wrap/content, ECDSA source signature và recipient endorsement được source kiểm; integrity verified không bằng time established/evidence verified/business authorized. DecryptedPayload dispose zeroes plaintext memory; mất device private key/không có recipient wrap dẫn OfflineKeyUnavailableException, không có organization escrow endpoint.

## Bằng chứng, tests và gaps

`OfflineWorkflowRepository.Transfer.cs`: ImportAsync L71, GuardImportTransportAsync L90, RegisterPackageAsync L166, IssueGrantAsync L227, RevokeGrantAsync L272, RegisterArtifactAsync L290, ReadTransferAsync L332, GuardBatchStatusAsync L475, ReconcileStartAsync L502; UploadPersistenceService.Offline/Recovery và OfflineAdmissionValidator là source dependency, không bỏ qua vì không trong Offline folder.

`NOT_RUN`: H5OfflineTransferServiceAdmission/RecipientEndorsement/RecipientProofPersistence/CaptureArtifact/CaptureArtifactPersistence/AdmittedFileReference tests; H5OfflineCanonicalRegistrySqlTests signed package/grant/replay/revocation; H5OfflineRepairSqlTests recipient upload without declaration rewrite; H5OfflinePositiveHttpTests/H5OfflineHttpAdmissionTests; MultipartRecoveryStateSqlTests/MultipartRecoveryHttpTests cho storage recovery boundary. Đây là danh mục test tồn tại, không claim chạy fresh.

GAP-RECOVERY-01 actual device lost-key/decrypt/import/chunk retry/kill/restart/storage-full chưa verified. GAP-RECOVERY-02 real object storage multipart recovery chưa chạy task này; source route/helper đã có, không ghi missing recovery API. GAP-RECOVERY-03 trusted UTC time proof adapter phải có nguồn độc lập; reconcile retained signed start không tự chứng minh clock. GAP-RECOVERY-04 FT-final Supervisor information producer vẫn thiếu ở repair final flow, không giải bằng handover grant. [Signed sync](03_Signed_Sync_and_Idempotency.md).

## DTO exact theo khai báo source

JSON web/camelCase; `?`/default theo C#; không suy required JSON từ nullable type. Input tương ứng dùng Disallow unknown members. Anonymous metadata views được mô tả riêng trong flow.

| Record | Tham số | Nguồn |
|---|---|---|
| `OfflineCaptureArtifactInput` | `OfflineCaptureManifest Manifest, string ManifestSignature, int ChunkIndex, H5EncryptedPackageDto Envelope` | [RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs) L8 |
| `OfflineCaptureUploadInput` | `Guid OfflineAdmissionId, Guid CaptureOriginId, UploadCreateRequestDto Upload` | [RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs) L11 |
| `OfflineResolvedCaptureView` | `Guid CaptureOriginId, Guid FileId, string UploadState, string Checksum, string Purpose, string MediaType, Guid ActualUploaderId` | [RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineCaptureTransferDtos.cs) L15 |
| `H5PackageHeaderDto` | `int Version, string PayloadSchema, Guid PackageId, Guid ProjectId, Guid OriginalActorId, string SourceDeviceId, Guid[] OriginIds, string PayloadSha256` | [RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs) L8 |
| `H5RecipientKeyWrapDto` | `Guid ActorId, string DeviceId, string RecipientKeyFingerprint, string EphemeralPublicKey, string Nonce, string WrappedKey, string Tag` | [RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs) L12 |
| `H5EncryptedPackageDto` | `H5PackageHeaderDto Header, string PayloadNonce, string Ciphertext, string PayloadTag, H5RecipientKeyWrapDto[] Recipients, string SenderSigningPublicKey, string Signature` | [RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs](../../../RoadGuardSystem.DTOs/Offline/H5EncryptedPackageDto.cs) L16 |

## API dùng trong luồng này

SOURCE_DERIVED_NOT_RUNTIME_VERIFIED; authentication/authorization và header/error được mô tả ở phần flow.

| Method / path | Action / dòng controller | Body |
|---|---|---|
| POST `/api/v1/projects/{projectId:guid}/offline/packages/export` | `ExportPackage` L43 | `OfflinePackageExportInput` |
| POST `/api/v1/projects/{projectId:guid}/offline/packages/prepare` | `PreparePackage` L47 | `OfflinePackageExportInput` |
| GET `/api/v1/projects/{projectId:guid}/offline/packages/{packageId:guid}` | `GetPackage` L51 | Route resource ID |
| POST `/api/v1/projects/{projectId:guid}/offline/grants` | `IssueGrant` L55 | `OfflineHandoverGrantInput` |
| GET `/api/v1/projects/{projectId:guid}/offline/grants/{grantId:guid}` | `GetGrant` L59 | Route resource ID |
| POST `/api/v1/projects/{projectId:guid}/offline/grants/{grantId:guid}/revoke` | `RevokeGrant` L63 | `OfflineGrantRevokeInput` |
| POST `/api/v1/projects/{projectId:guid}/offline/import` | `ImportPackage` L67 | `OfflinePackageImportInput` |
| GET `/api/v1/projects/{projectId:guid}/offline/batches/{batchId:guid}` | `GetBatch` L71 | Route resource ID |
| GET `/api/v1/projects/{projectId:guid}/offline/origins/{originId:guid}` | `GetOrigin` L75 | Route resource ID |
| POST `/api/v1/projects/{projectId:guid}/offline/starts/reconcile` | `ReconcileStart` L79 | `OfflineStartReconcileInput` |
| POST `/api/v1/projects/{projectId:guid}/offline/artifacts` | `RegisterArtifact` L83 | `OfflineCaptureArtifactInput` |
| GET `/api/v1/projects/{projectId:guid}/offline/artifacts/{artifactId:guid}` | `GetArtifact` L87 | Route resource ID |

## DTO exact theo khai báo source

JSON web/camelCase; `?`/default theo C#; không suy required JSON từ nullable type. Input tương ứng dùng Disallow unknown members. Anonymous metadata views được mô tả riêng trong flow.

| Record | Tham số | Nguồn |
|---|---|---|
| `OfflineDeviceRegisterInput` | `Guid DeviceId, string EncryptionPublicKey, string SigningPublicKey` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L8 |
| `OfflineDeviceRevokeInput` | `string Reason` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L10 |
| `OfflineSnapshotInput` | `Guid DeviceRegistrationId, Guid TaskId` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L12 |
| `OfflineGrantItemScope` | `Guid OriginId, string Kind, string CorePayloadHash, string EnvelopeHash, Guid TaskId, Guid AssignmentId, Guid SnapshotId, Guid? RepairResourceId = null` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L14 |
| `OfflineHandoverGrantInput` | `Guid PackageId, Guid RecipientDeviceRegistrationId, OfflineGrantItemScope[] Items, string Reason` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L17 |
| `OfflineGrantRevokeInput` | `string Reason` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L20 |
| `OfflineOperationDescriptor` | `Guid OriginId, Guid EffectId, string Kind, string EnvelopeHash, string CorePayloadHash, Guid TaskId, Guid AssignmentId, Guid SnapshotId, Guid? RepairResourceId` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L22 |
| `OfflineSourceManifestInput` | `Guid SourceBatchId, Guid SourceDeviceRegistrationId, OfflineOperationDescriptor[] Items, string Signature` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L25 |
| `OfflinePackageExportInput` | `H5EncryptedPackageDto Package, OfflineSourceManifestInput Manifest, Guid? SourceFileId = null` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L28 |
| `OfflineRepairPayload` | `Guid ResourceId, string Action, RepairExecutionStartInput? Start = null, RepairExecutionFinishInput? Finish = null, RepairMeasurementAssessmentInput? Assessment = null` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L33 |
| `OfflineOperationInput` | `int SchemaVersion, Guid OriginId, Guid EffectId, string Kind, Guid OriginalActorId, Guid SourceDeviceId, Guid SnapshotId, Guid TaskId, Guid AssignmentId, string TaskVersion, string CorePayloadHash, Guid[] Dependencies, DateTimeOffset? ClaimedFinishedAt, FieldStartInput? FieldStart = null, FieldSubmissionInput? FieldSubmission = null, OfflineRepairPayload? Repair = null, FieldTaskActionInput? FieldAction = null` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L37 |
| `OfflineSignedBatchInput` | `Guid BatchId, Guid SourceDeviceRegistrationId, OfflineOperationInput[] Operations, string Signature` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L43 |
| `OfflinePackageImportInput` | `Guid BatchId, Guid PackageId, Guid GrantId, Guid RecipientDeviceRegistrationId, OfflineOperationInput[] Operations, string SourceSignature, string RecipientSignature` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L46 |
| `OfflineStartReconcileInput` | `Guid BatchId, Guid OriginId` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L50 |
| `OfflineWorkflowResult` | `int Status, string? Code = null, object? Value = null, string? Version = null, bool Replayed = false` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L52 |
| `OfflineItemResult` | `Guid OriginId, string Kind, string State, string? Code, Guid? EffectId, Guid? CanonicalOriginId, bool DurableAcknowledgment, DateTimeOffset ServerReceivedAt, string TimeProvenance, string SyncLateness, DateTimeOffset? ClaimedFinishedAt, DateTimeOffset? VerifiedFinishedAt, DateTimeOffset? VerifiedSyncDueAt, object? Outcome` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L54 |
| `OfflineBatchResult` | `Guid BatchId, OfflineItemResult[] Items, string LocalQueueInstruction = "KEEP_UNACKNOWLEDGED_OR_CONFLICTED_ITEMS"` | [RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs](../../../RoadGuardSystem.DTOs/Offline/OfflineWorkflowDtos.cs) L58 |


## Danh mục nguồn và symbol

Các dòng sau là vị trí khai báo tại baseline; không phải kết quả test.

| Đường dẫn | Symbol / dòng |
|---|---|
| [RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs](../../../RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs) | `OfflineWorkflowController` L16; `RegisterDevice` L19; `GetDevice` L23; `RevokeDevice` L27; `CaptureSnapshot` L32; `GetSnapshot` L36; `Synchronize` L40; `ExportPackage` L44; `PreparePackage` L48; `GetPackage` L52; `IssueGrant` L56; `GetGrant` L60; `RevokeGrant` L64; `ImportPackage` L68; `GetBatch` L72; `GetOrigin` L76; `ReconcileStart` L80; `RegisterArtifact` L84; `GetArtifact` L88 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineAdmittedFileReference.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineAdmittedFileReference.cs) | `OfflineAdmittedFileReference` L4 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineCaptureArtifacts.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineCaptureArtifacts.cs) | `OfflineCaptureChunk` L6; `OfflineCaptureManifest` L8; `OfflineCaptureArtifacts` L13 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineCryptoFormat.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineCryptoFormat.cs) | `OfflineCryptoFormat` L7; `OfflinePackageHeader` L146; `OfflineDevicePublicKeys` L160; `OfflineRecipientKeyWrap` L161; `OfflineEncryptedPackage` L163 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceKeys.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceKeys.cs) | `OfflineDeviceKeys` L6; `EnsureAvailable` L31; `Dispose` L38 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceRegistration.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceRegistration.cs) | `OfflineDeviceRegistration` L5; `OfflineDeviceRevocation` L40 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineEncryptedCaptureArtifact.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineEncryptedCaptureArtifact.cs) | `OfflineEncryptedCaptureArtifact` L8 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineEncryptedPackageRecord.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineEncryptedPackageRecord.cs) | `OfflineEncryptedPackageRecord` L5 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineEvidenceCaptureReference.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineEvidenceCaptureReference.cs) | `OfflineEvidenceCaptureReference` L4 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverCrypto.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverCrypto.cs) | `OfflineKeyUnavailableException` L5; `OfflineDecryptedPayload` L11; `Dispose` L21; `OfflineHandoverCrypto` L24 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverGrant.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverGrant.cs) | `OfflineHandoverGrant` L6; `AllowsNewAdmissionAt` L57 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverGrantRevocation.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineHandoverGrantRevocation.cs) | `OfflineHandoverGrantRevocation` L3 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineOperationAdmission.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineOperationAdmission.cs) | `OfflineOperationAdmission` L6 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineOperationBinding.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineOperationBinding.cs) | `OfflineOperationBinding` L7 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineOperationResult.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineOperationResult.cs) | `OfflineOperationResult` L4 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineOriginTimeVerification.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineOriginTimeVerification.cs) | `OfflineOriginTimeVerification` L4 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflinePackageAuthentication.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflinePackageAuthentication.cs) | `OfflinePackageAuthenticationResult` L6; `OfflinePackageAuthentication` L9 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflinePackageFileReference.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflinePackageFileReference.cs) | `OfflinePackageFileReference` L3 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineRecipientEndorsement.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineRecipientEndorsement.cs) | `OfflineRecipientImportClaim` L5; `OfflineRecipientEndorsement` L8 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineRuntimeGuards.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineRuntimeGuards.cs) | `OfflineRuntimeGuards` L7 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineSyncBatch.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineSyncBatch.cs) | `OfflineSyncBatch` L3 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineTaskSnapshot.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineTaskSnapshot.cs) | `OfflineTaskSnapshot` L3 |
| [RoadGuardSystem.DTOs/Files/OfflineUploadCreateRequestDto.cs](../../../RoadGuardSystem.DTOs/Files/OfflineUploadCreateRequestDto.cs) | `OfflineUploadCreateRequestDto` L5 |
| [RoadGuardSystem.Repositories/Configurations/OfflinePersistenceConfigurations.cs](../../../RoadGuardSystem.Repositories/Configurations/OfflinePersistenceConfigurations.cs) | `OfflinePersistenceConfigurations` L13; `where` L51 |
| [RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.Offline.cs](../../../RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.Offline.cs) | `UploadPersistenceService` L12; `IsCurrentOfflineFileActorAsync` L14 |
| [RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.Recovery.cs](../../../RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.Recovery.cs) | `UploadPersistenceService` L11; `RecoverMultipartsAsync` L135 |
| [RoadGuardSystem.Repositories/Implementations/Offline/OfflineAdmissionValidator.cs](../../../RoadGuardSystem.Repositories/Implementations/Offline/OfflineAdmissionValidator.cs) | `OfflineAdmissionValidator` L19; `ValidateAsync` L31; `ValidateRepairAsync` L55; `GuardOriginBindingAsync` L283; `ResolveDeclaredEvidenceAsync` L304; `ValidateFieldEvidenceAsync` L426; `GuardExistingFileAsync` L440; `GuardNewUploadAsync` L476; `BindCreatedFileAsync` L510; `GuardCommittedUploadAsync` L546 |
| [RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.cs) | `OfflineWorkflowRepository` L17; `CommittedReceipt` L23; `ExecuteAsync` L24 |
| [RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.Sync.cs](../../../RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.Sync.cs) | `OfflineWorkflowRepository` L13; `StagedBatch` L15 |
| [RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.Transfer.cs](../../../RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.Transfer.cs) | `OfflineWorkflowRepository` L15 |
| [RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineEvidenceAdmissionValidator.cs](../../../RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineEvidenceAdmissionValidator.cs) | `OfflineEvidenceAdmissionFacts` L5; `IOfflineEvidenceAdmissionValidator` L10 |
| [RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineFieldAdmissionValidator.cs](../../../RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineFieldAdmissionValidator.cs) | `never` L6; `OfflineFieldAdmissionFacts` L7; `IOfflineFieldAdmissionValidator` L12 |
| [RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineRepairAdmissionValidator.cs](../../../RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineRepairAdmissionValidator.cs) | `IOfflineRepairAdmissionValidator` L5 |
| [RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineRepairCommandAdapter.cs](../../../RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineRepairCommandAdapter.cs) | `OfflineRepairSnapshotQuery` L5; `OfflineRepairSnapshotFacts` L6; `OfflineRepairCoreCommand` L8; `OfflineRepairEffect` L11; `IOfflineRepairCommandAdapter` L15 |
| [RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineUploadAdmissionValidator.cs](../../../RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineUploadAdmissionValidator.cs) | `OfflineUploadCaptureRequest` L6; `OfflineUploadCaptureFacts` L8; `IOfflineUploadAdmissionValidator` L13 |
| [RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineWorkflowRepository.cs](../../../RoadGuardSystem.Repositories/Interfaces/Offline/IOfflineWorkflowRepository.cs) | `OfflineWorkflowCommand` L5; `OfflineWorkflowAlgorithms` L7; `IOfflineWorkflowRepository` L13 |
| [RoadGuardSystem.Repositories/Interfaces/Offline/OfflineAdmissionRejectedException.cs](../../../RoadGuardSystem.Repositories/Interfaces/Offline/OfflineAdmissionRejectedException.cs) | `OfflineAdmissionRejectedException` L4 |
| [RoadGuardSystem.Repositories/Interfaces/Offline/OfflineInternalContracts.cs](../../../RoadGuardSystem.Repositories/Interfaces/Offline/OfflineInternalContracts.cs) | `OfflineDeviceRegisterData` L8; `OfflineDeviceRevokeData` L9; `OfflineSnapshotData` L10; `OfflineGrantItemData` L11; `OfflineHandoverGrantData` L13; `OfflineGrantRevokeData` L15; `OfflineOperationDescriptorData` L16; `OfflineSourceManifestData` L18; `OfflinePackageExportData` L20; `OfflineRepairData` L22; `OfflineOperationData` L25; `OfflineSignedBatchData` L30; `OfflinePackageImportData` L32; `OfflineCaptureArtifactData` L35; `OfflineStartReconcileData` L37; `OfflineWorkflowFact` L39; `OfflineItemFact` L41; `OfflineBatchFact` L45 |
| [RoadGuardSystem.Services/Implementations/Offline/OfflineContractMapping.cs](../../../RoadGuardSystem.Services/Implementations/Offline/OfflineContractMapping.cs) | `OfflineContractMapping` L11 |
| [RoadGuardSystem.Services/Implementations/Offline/OfflineWorkflowEngine.cs](../../../RoadGuardSystem.Services/Implementations/Offline/OfflineWorkflowEngine.cs) | `OfflineWorkflowEngine` L8 |
| [RoadGuardSystem.Services/Implementations/Offline/OfflineWorkflowService.cs](../../../RoadGuardSystem.Services/Implementations/Offline/OfflineWorkflowService.cs) | `OfflineWorkflowService` L8; `ExecuteAsync` L10 |
| [RoadGuardSystem.Services/Interfaces/Offline/IOfflineWorkflowService.cs](../../../RoadGuardSystem.Services/Interfaces/Offline/IOfflineWorkflowService.cs) | `IOfflineWorkflowService` L6 |
| [tests/RoadGuardSystem.ApiTests/Files/MultipartRecoveryHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Files/MultipartRecoveryHttpTests.cs) | `MultipartRecoveryHttpTests` L23; `LostAcknowledgement_NewHostRecoversSameKey_ConflictsAndAuthority_CompleteDownload` L26; `LegacyTerminalViaRealWorker_ReturnsConflictAndPermitsNewCreate` L71; `RemotePhoto` L99; `PhotoAdapter` L105; `InitiateAsync` L107; `HasPartsAsync` L110; `AbortMultipartAsync` L111; `CompleteAndVerifyAsync` L113; `OpenReadAsync` L114 |
| [tests/RoadGuardSystem.IntegrationTests/Files/MultipartRecoveryStateSqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Files/MultipartRecoveryStateSqlTests.cs) | `MultipartRecoveryStateSqlTests` L19; `RestartFromDurableCalling_TwoReconcilersAndTwoKeys_AuthorizeOneUpload` L24; `AmbiguousOrTerminal_RequiresNewSession_CleansOnlyExactOwnedKey_AndLateOrphans` L51; `LateWorkerAfterFenceTransfer_CannotIssueStaleUrl_RecoveryContinues` L82; `InterruptedRecovery_RemainsDurableAndRetryableWithoutProtectedEffects` L105; `CancellationAfterClaimCommit_BeforeRemoteCall_HasBoundedTerminalRestart` L146; `ProjectRecovery_UsesCurrentMembershipBeforeAdoptionReplayAndConflict` L163; `ExpiredAcceptedUpload_IsFencedAndAborted_WithoutResurrectingReceipts` L196; `CallerCancellationAfterRemoteCreate_FreshContextRecoversIndependently` L211; `RevokeDuringPresign_WithholdsCommittedProtectedReceipt` L225; `AcknowledgedIdWithTransportDuplicate_SweepAbortsExtrasWithoutDriftingVerifiedVersion` L237; `VerifiedAndVerifyingSessions_AreNotResurrectedOrVersionDriftedByWorker` L259; `RecoveryStorage` L299; `InitiateAsync` L312; `HasPartsAsync` L316; `AbortMultipartAsync` L317; `CompleteAndVerifyAsync` L321; `OpenReadAsync` L322; `CancelClaimCommit` L325 |
| [tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineCanonicalRegistrySqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineCanonicalRegistrySqlTests.cs) | `H5OfflineCanonicalRegistrySqlTests` L46; `SignedApprovedBeforeReuseRetainsReporterOwnerAndRechecksActualSource` L55; `ActualSignedSyncCommitsOneTypedStartAndGuardsItsDurableReplay` L96; `CurrentDeviceOwnerCanRevokeOnceAndOldSignedSourcesLoseAdmission` L163; `ActualSnapshotPinsCurrentAssignmentAndDeniesAnEndedAssignment` L221; `SignedDeclarationResolvesActualVerifiedFileWithoutChangingOriginalPayload` L283; `RawAcknowledgmentCannotClaimAnAdmittedBindingWithoutItsCanonicalTypedEffect` L317; `RawAdmittedFileCannotFalsifyActualOwnerOrUploaderForGenuineSignedDeclaration` L332; `RawGuardSource` L437; `SignedUnboundCaptureCreatesOneActualUploaderFileAndGuardsUploadReceipt` L441; `ControlledUploadStorage` L513; `InitiateAsync` L516; `CompleteAndVerifyAsync` L525; `OpenReadAsync` L528; `SignedPackageGrantImportCommitsOneOriginalCrewEffectAndReplaysUnderCurrentRecipient` L533; `SignedGrantHandoverBreachUsesActualRecipientAndRevocation` L539; `ActualDeviceRegistrationHasOneDurableEffectAndRechecksCallerBeforeReceipt` L780; `PersistedSignedAdmissionBindsActualOriginalAssignmentAndDoesNotPromoteClockClaims` L814; `ImporterCurrentAuthorityIsCheckedBeforeReadingProtectedAdmission` L888; `GenuineExistingDirectStartKeepsItsOriginalNullDeviceAndActualEffectOnHistoricalDescription` L907; `GenuineReporterFieldStartCannotBeReclassifiedOrRehashedByOfflineAdmission` L939; `Scope` L1041; `FixedClock` L1042 |
| [tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineRepairSqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineRepairSqlTests.cs) | `H5OfflineRepairSqlTests` L35; `SignedNormalAssessmentStartFinishCommitOriginalCrewAndKeepClaimedTimeUncertain` L40; `SignedHandoverAssessmentResolvesRecipientUploadWithoutRewritingDeclaration` L171; `VerifiedStorage` L316; `InitiateAsync` L318; `CompleteAndVerifyAsync` L324; `OpenReadAsync` L327; `RepairPrepared` L331 |

## Mức persistence và giới hạn baseline

Các mô tả persistence ở đây là `EF_LOGICAL_MODEL`/`MIGRATION_DERIVED_PHYSICAL_MODEL`, không phải `LIVE_SQL_VERIFIED`. Baseline migration hiện tại: [BaselineCurrentSchema](../../../RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs), [model snapshot](../../../RoadGuardSystem.Repositories/Migrations/RoadGuardDbContextModelSnapshot.cs), [post-repair task correction](../../../RoadGuardSystem.Repositories/Migrations/20261009043010_AllowVerifiedDefectPostRepairTasks.cs). Danh mục nguồn hỗ trợ truy vết; constraint/trigger/relationship tổng thể cần đối chiếu bộ Database. Không chạy migration, không gọi database shared.

## Nguồn clock, business request và notification liên mô-đun

Đây là dependencies/proof-source của workflow; sự hiện diện adapter/catalog không chứng minh producer phát event hoặc worker đã triển khai.

- [RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs](../../../RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs): `DeadlineClockKind` L5; `DeadlineClock` L12; `Appoint` L37; `IsOverdueAt` L78; `ObserveBreach` L89; `Complete` L121; `Acknowledge` L132; `DeadlineExtension` L158; `DeadlineBreach` L170; `DeadlineDutyAppointment` L178
- [RoadGuardSystem.Repositories/Implementations/Messaging/BusinessRequestProducer.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/BusinessRequestProducer.cs): `BusinessRequestProducer` L8; `Supplement` L10; `CompleteSupplements` L15; `ReassignSupplements` L24
- [RoadGuardSystem.Repositories/Implementations/Messaging/H6RepairNotificationSourceAdapter.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6RepairNotificationSourceAdapter.cs): `H6RepairNotificationSourceAdapter` L12; `Supports` L14; `ResolveAsync` L35
- [RoadGuardSystem.Repositories/Implementations/Messaging/H6DeadlineNotificationSourceAdapter.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6DeadlineNotificationSourceAdapter.cs): `H6DeadlineNotificationSourceAdapter` L13; `Supports` L16; `ResolveAsync` L51; `ResolveDutyAsync` L71; `ResponsibilityProjectAsync` L92
