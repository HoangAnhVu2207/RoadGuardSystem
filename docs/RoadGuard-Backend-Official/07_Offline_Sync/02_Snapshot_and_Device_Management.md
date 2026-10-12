# Snapshot, thiết bị và pin quyền đã tải

Baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`; `huy-review`; đối chiếu source 13/10/2026. `CURRENT_VERIFIED` = đọc implementation; tests `NOT_RUN` trong BE-DOC-02. Kết quả cũ là `HISTORICAL`; không có live SQL/Android/storage/deployment acceptance trong task này.

Authority: [planning/development/HUY-FINAL-INTEGRATION.md](../../../planning/development/HUY-FINAL-INTEGRATION.md) (ND01 L3–15; R08 L679, R09 L684, R20–R22 L750–772, R29/R30 L812–825), [docs/product/confirmed-decisions.md](../../../docs/product/confirmed-decisions.md) (37/42A/43A), [docs/audit/DOC-AUDIT-01-OPEN-GAPS.md](../../../docs/audit/DOC-AUDIT-01-OPEN-GAPS.md).

## EXPECTED_BUSINESS_BEHAVIOR — TARGET_CONFIRMED

R08/R20–R22/R29 và PR42A/43A: tải task/location/assignment/policy trước tác nghiệp; bảo toàn actor gốc, version vị trí và quyền đã cấp. Snapshot giúp tác nghiệp mất mạng, không miễn current permission lúc reconnect. Token expiry không xóa local data; revoke chỉ biết trên thiết bị khi nhận update. Offline geometry/route/segment/destination có thể dùng khi chưa chọn nền map thương mại. Không gọi sample CRS là official accuracy.

## CURRENT_IMPLEMENTATION và actor

POST devices dùng DeviceId, EncryptionPublicKey, SigningPublicKey; public key parsing/fingerprint giữ encryption và signing identity. Registration pin project/actor/role snapshot/device/revision/time. PM/Crew register own device; revoke chỉ actual actor/role của registration, có reason và current scope. GET device cho current Supervisor/PM hoặc chính Crew owner theo repository guard.

POST snapshots dùng registration/task ID, repository lock actual device/revocation/task/active assignment. Crew phải là người được giao hiện hành. `LifecycleVersion==2`, task không Completed/Cancelled/Rejected, SegmentSetId và FIELD geometry core phải có. Repair task đòi repair adapter, actual item/task/assignment/actor match. GET snapshot chỉ original Crew, đúng registration/role không revoke và assignment còn active; current membership/project kiểm trước trả dữ liệu.

## Luồng chính, output và persistence

1. Client tạo key pair và đăng ký public keys; private keys không gửi vào DTO. Device đăng ký không tự tạo task hoặc repair permission.
2. Crew capture snapshot: FIELD `geometry` đọc route/segment/layout/map/CRS version đã pin. Repository serialize immutable assignment facts, task facts/location và optional repair SafePayload.
3. Snapshot lưu TaskVersion base64 rowversion, AssignmentHash SHA256, SnapshotJson/ContentHash và DownloadedAt server. GET trả Id,TaskId,AssignmentId,OriginalActorId,DeviceRegistrationId,TaskVersion,AssignmentHash,ContentHash,DownloadedAt,snapshot.
4. Optional repair payload từ `RepairWorkflowRepository.Offline.ReadSnapshotInTransactionAsync` gồm current item/version/obligation/binding/assignment, plan/checklist/location, policy revision/hash/body nếu áp dụng, authorization/first-start facts. NORMAL không có applicable FT policy là hợp lệ, không tự cấp execute grant. FT pin policy body không đồng nghĩa coverage đủ quyền.
5. Revoke append OfflineDeviceRevocation và audit/outbox `offline.device.revoked.v1`; không rewrite snapshot/registration. Next protected read/sync/import kiểm revocation/current rights lại.

Snapshot task payload chứa id,projectId,defectId,surveyId,sourceKind,roadSectionVersionId,segmentSetId,layoutRevisionId,slabId,mapPublicationId,crsProfileRevisionId,requiredMeasurementType,measurementScope,instructions,taskMode,repairItemId,purpose,status,dueAt,assignmentId,originalActorId,location,repair. Geometry/download chính xác phụ thuộc pin source; official CRS chưa được chứng nhận bằng JSON/hash.

## Nhánh lỗi, state machine và concurrency

Device revisions immutable; registration → revoke fact. Snapshot không refresh in place; task reassign/location impact tạo state/source mới. Unknown registration/current actor mismatch/revoke hoặc assignment absent ⇒403; task absent⇒404; terminal/legacy⇒409 source_not_ready; thiếu segment/geometry⇒409 offline_location_source_unavailable; repair source mismatch⇒409 repair_snapshot_source_unavailable. Project inactive chặn write. GET snapshot sau mất assignment bị deny dù snapshot từng tải được.

Idempotency operation names `h5.offline.device-register.v1`, `.device-revoke.v1`, `.snapshot-create.v1`. Receipt fingerprint gồm project/action/input và ResourceId với revoke; serializable transaction/locks và receiptAccessGuard chống replay bằng quyền cũ. Write cần Idempotency-Key; duplicate key khác body⇒409 idempotency_key_reused; replay cùng input trả200, fresh create201. Registration/snapshot metadata không nhất thiết có Version/ETag riêng; không suy ETag chỉ vì controller có nhánh xuất header.

## Bằng chứng, tests và gaps

`OfflineWorkflowRepository.cs`: ExecuteAsync L24, RevokeDeviceResultAsync L141, CaptureSnapshotAsync L186, SnapshotView L261, GuardSnapshotScopeAsync L276; `RepairWorkflowRepository.Offline.cs` ReadSnapshotInTransactionAsync L38. `OfflineTaskSnapshot.Capture` tạo immutable hashed content; OfflinePersistenceConfigurations unique revision và indexes/payload constraints.

`NOT_RUN`: H5OfflineCanonicalRegistrySqlTests snapshot current assignment/ended assignment; H5FtPolicySnapshotSqlTests actual policy download TEST_ONLY; H4RepairLifecycleSqlTests normal binding plan/item version; H5OfflineIdentityTests/RepairResourcePinTests/PersistenceModelTests; H5OfflinePositiveHttpTests/ND01 SQL/HTTP. Pin serialization source được kiểm đọc, không claim executed test ở task này.

GAP-SNAPSHOT-01 Android encrypted local store/backup/private key custody chưa được kiểm. GAP-SNAPSHOT-02 official CRS/independent controls/authoritative source mapping còn gate thực tế; không suy phép execution từ TEST_ONLY payload. GAP-SNAPSHOT-03 mất key không có server escrow/recovery service trong scope đã chốt. [Kiến trúc và toàn bộ route](01_Offline_Backend_Architecture.md).

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

## API dùng trong luồng này

SOURCE_DERIVED_NOT_RUNTIME_VERIFIED; authentication/authorization và header/error được mô tả ở phần flow.

| Method / path | Action / dòng controller | Body |
|---|---|---|
| POST `/api/v1/projects/{projectId:guid}/offline/devices` | `RegisterDevice` L18 | `OfflineDeviceRegisterInput` |
| GET `/api/v1/projects/{projectId:guid}/offline/devices/{registrationId:guid}` | `GetDevice` L22 | Route resource ID |
| POST `/api/v1/projects/{projectId:guid}/offline/devices/{registrationId:guid}/revoke` | `RevokeDevice` L26 | `OfflineDeviceRevokeInput` |
| POST `/api/v1/projects/{projectId:guid}/offline/snapshots` | `CaptureSnapshot` L31 | `OfflineSnapshotInput` |
| GET `/api/v1/projects/{projectId:guid}/offline/snapshots/{snapshotId:guid}` | `GetSnapshot` L35 | Route resource ID |

## Danh mục nguồn và symbol

Các dòng sau là vị trí khai báo tại baseline; không phải kết quả test.

| Đường dẫn | Symbol / dòng |
|---|---|
| [RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceKeys.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceKeys.cs) | `OfflineDeviceKeys` L6; `EnsureAvailable` L31; `Dispose` L38 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceRegistration.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineDeviceRegistration.cs) | `OfflineDeviceRegistration` L5; `OfflineDeviceRevocation` L40 |
| [RoadGuardSystem.BusinessObjects/Offline/OfflineTaskSnapshot.cs](../../../RoadGuardSystem.BusinessObjects/Offline/OfflineTaskSnapshot.cs) | `OfflineTaskSnapshot` L3 |
| [RoadGuardSystem.Repositories/Configurations/OfflinePersistenceConfigurations.cs](../../../RoadGuardSystem.Repositories/Configurations/OfflinePersistenceConfigurations.cs) | `OfflinePersistenceConfigurations` L13; `where` L51 |
| [RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.cs) | `OfflineWorkflowRepository` L17; `CommittedReceipt` L23; `ExecuteAsync` L24 |
| [RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Offline.cs](../../../RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Offline.cs) | `RepairWorkflowRepository` L12; `OfflineRepairContext` L14; `ComputeCoreHash` L17; `ReadSnapshotInTransactionAsync` L38; `ApplyInTransactionAsync` L110 |
| [tests/RoadGuardSystem.IntegrationTests/Offline/H5FtPolicySnapshotSqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Offline/H5FtPolicySnapshotSqlTests.cs) | `H5FtPolicySnapshotSqlTests` L28; `PublishedTestOnlyFtPolicyIsPinnedAndDownloadedForCurrentCrewWithoutExecution` L33 |
| [tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineCanonicalRegistrySqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineCanonicalRegistrySqlTests.cs) | `H5OfflineCanonicalRegistrySqlTests` L46; `SignedApprovedBeforeReuseRetainsReporterOwnerAndRechecksActualSource` L55; `ActualSignedSyncCommitsOneTypedStartAndGuardsItsDurableReplay` L96; `CurrentDeviceOwnerCanRevokeOnceAndOldSignedSourcesLoseAdmission` L163; `ActualSnapshotPinsCurrentAssignmentAndDeniesAnEndedAssignment` L221; `SignedDeclarationResolvesActualVerifiedFileWithoutChangingOriginalPayload` L283; `RawAcknowledgmentCannotClaimAnAdmittedBindingWithoutItsCanonicalTypedEffect` L317; `RawAdmittedFileCannotFalsifyActualOwnerOrUploaderForGenuineSignedDeclaration` L332; `RawGuardSource` L437; `SignedUnboundCaptureCreatesOneActualUploaderFileAndGuardsUploadReceipt` L441; `ControlledUploadStorage` L513; `InitiateAsync` L516; `CompleteAndVerifyAsync` L525; `OpenReadAsync` L528; `SignedPackageGrantImportCommitsOneOriginalCrewEffectAndReplaysUnderCurrentRecipient` L533; `SignedGrantHandoverBreachUsesActualRecipientAndRevocation` L539; `ActualDeviceRegistrationHasOneDurableEffectAndRechecksCallerBeforeReceipt` L780; `PersistedSignedAdmissionBindsActualOriginalAssignmentAndDoesNotPromoteClockClaims` L814; `ImporterCurrentAuthorityIsCheckedBeforeReadingProtectedAdmission` L888; `GenuineExistingDirectStartKeepsItsOriginalNullDeviceAndActualEffectOnHistoricalDescription` L907; `GenuineReporterFieldStartCannotBeReclassifiedOrRehashedByOfflineAdmission` L939; `Scope` L1041; `FixedClock` L1042 |

## Mức persistence và giới hạn baseline

Các mô tả persistence ở đây là `EF_LOGICAL_MODEL`/`MIGRATION_DERIVED_PHYSICAL_MODEL`, không phải `LIVE_SQL_VERIFIED`. Baseline migration hiện tại: [BaselineCurrentSchema](../../../RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs), [model snapshot](../../../RoadGuardSystem.Repositories/Migrations/RoadGuardDbContextModelSnapshot.cs), [post-repair task correction](../../../RoadGuardSystem.Repositories/Migrations/20261009043010_AllowVerifiedDefectPostRepairTasks.cs). Danh mục nguồn hỗ trợ truy vết; constraint/trigger/relationship tổng thể cần đối chiếu bộ Database. Không chạy migration, không gọi database shared.

## Nguồn clock, business request và notification liên mô-đun

Đây là dependencies/proof-source của workflow; sự hiện diện adapter/catalog không chứng minh producer phát event hoặc worker đã triển khai.

- [RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs](../../../RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs): `DeadlineClockKind` L5; `DeadlineClock` L12; `Appoint` L37; `IsOverdueAt` L78; `ObserveBreach` L89; `Complete` L121; `Acknowledge` L132; `DeadlineExtension` L158; `DeadlineBreach` L170; `DeadlineDutyAppointment` L178
- [RoadGuardSystem.Repositories/Implementations/Messaging/BusinessRequestProducer.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/BusinessRequestProducer.cs): `BusinessRequestProducer` L8; `Supplement` L10; `CompleteSupplements` L15; `ReassignSupplements` L24
- [RoadGuardSystem.Repositories/Implementations/Messaging/H6RepairNotificationSourceAdapter.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6RepairNotificationSourceAdapter.cs): `H6RepairNotificationSourceAdapter` L12; `Supports` L14; `ResolveAsync` L35
- [RoadGuardSystem.Repositories/Implementations/Messaging/H6DeadlineNotificationSourceAdapter.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6DeadlineNotificationSourceAdapter.cs): `H6DeadlineNotificationSourceAdapter` L13; `Supports` L16; `ResolveAsync` L51; `ResolveDutyAsync` L71; `ResponsibilityProjectAsync` L92
