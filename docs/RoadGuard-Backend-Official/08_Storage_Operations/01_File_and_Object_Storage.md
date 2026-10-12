# File và object storage

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Expected / storage boundaries

TARGET_CONFIRMED PR38: ảnh20MiB/video8GiB/SRT10MiB,dataset32GiB,multipart/resume. Source video đi hồ sơ retention. Long/bigint metadata không chứng minh real8GiB transfer. FE arbitrary storage URI không bypass ownership; Crew đo FIELD, không lấy actual kích thước từ AI.

CURRENT_VERIFIED IFileContentStore→LocalFileContentStore local content; IUploadObjectStorage→MinioUploadObjectStorage S3-compatible multipart/source; IAnh02ArtifactStore→MinioAnh02ArtifactStore generated artifact. StoredFile original name/media/size/checksum/storage reference; Verified upload khác PUT hoàn tất. SQL/object không distributed atomic transaction.

## Actor, preconditions, main flow/state

Current project/resource actor→UploadSession admission/idempotency→multipart initiate/exact object part presign→client PUT→complete explicit parts→storage complete+stream verification→Verified file usable by producer. UploadSession options part/lifetime/URL/recovery. Signed URL không task authority. Retry exact object/upload IDs/fences/receipts; không delete arbitrary objects.

Local StoreAsync temporary→size/mime/checksum/path checks→finalize; cleanup failures. OpenReadAsync resolve controlled URI. IFileContentCleanup chỉ scoped caller cleanup, không retention purge. MinIO CompleteAndVerifyAsync check object/manifest/bytes; S3ResponseStream owns response/client disposal.

Artifact write deterministic job/snapshot key+size/hash/media; read spool temporary DeleteOnClose/verify. Export rechecks current source rights sau OpenRead và dispose khi revoked/expired; SHA ETag không proof permission. Issued snapshot immutable.

## API/DTO, alternative và concurrency

Files/Uploads controller dùng file/upload DTO và metadata/separate part requests; exhaustive schema/headers nằm catalog root. Errors FileStorageErrorCodes: file_path_invalid,file_too_large,file_mime_mismatch,file_checksum_mismatch,file_size_mismatch,file_storage_unavailable,idempotency_key_conflict. HTTP mapping service/controller, không suy cùng status cho mọi error.

Worker lease/state/recovery giữ retries; crash object-before-DB cần reconcile, commit-before-ACK replay same key. SQL rollback không rollback external bytes. Recovery limits là options/design, không tự policy mới. Signed upload current admission/source provenance tách actual uploader/capture actor.

## Gaps

UNKNOWN live MinIO/TLS/presign/multipart8GiB/interrupted resume/object lifecycle permissions. PROPOSED isolated real transfer+checksum+recovery, reconcile without shared-data mutation. Tests NOT_RUN; retention evaluation không object deletion/store cleanup không policy/hold.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [RoadGuardPersistenceExtensions.cs](../../../RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs) | `RoadGuardPersistenceExtensions` :32 | CURRENT_VERIFIED source |
| [LocalFileContentStore.cs](../../../RoadGuardSystem.Repositories/Implementations/Storage/LocalFileContentStore.cs) | `LocalFileContentStore` :8 | CURRENT_VERIFIED source |
| [MinioUploadObjectStorage.cs](../../../RoadGuardSystem.Repositories/Implementations/Storage/MinioUploadObjectStorage.cs) | `MinioUploadObjectStorage` :12 | CURRENT_VERIFIED source |
| [MinioAnh02ArtifactStore.cs](../../../RoadGuardSystem.Repositories/Implementations/Storage/MinioAnh02ArtifactStore.cs) | `MinioAnh02ArtifactStore` :10 | CURRENT_VERIFIED source |
| [S3ResponseStream.cs](../../../RoadGuardSystem.Repositories/Implementations/Storage/S3ResponseStream.cs) | `S3ResponseStream` :6 | CURRENT_VERIFIED source |
| [UploadSessionOptions.cs](../../../RoadGuardSystem.Repositories/Options/UploadSessionOptions.cs) | `UploadSessionOptions` :3 | CURRENT_VERIFIED source |
| [UploadPersistenceService.cs](../../../RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs) | `UploadPersistenceService` :13 | CURRENT_VERIFIED source |
| [ExportService.cs](../../../RoadGuardSystem.Services/Implementations/Exports/ExportService.cs) | `ExportService` :16 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [Anh01StorageRecoveryTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Files/Anh01StorageRecoveryTests.cs)

## Symbol checkpoints

- [LocalFileContentStore.cs](../../../RoadGuardSystem.Repositories/Implementations/Storage/LocalFileContentStore.cs) :63 — `StoreAsync` (`CURRENT_VERIFIED` source).
- [MinioUploadObjectStorage.cs](../../../RoadGuardSystem.Repositories/Implementations/Storage/MinioUploadObjectStorage.cs) :160 — `CompleteAndVerifyAsync` (`CURRENT_VERIFIED` source).
- [MinioAnh02ArtifactStore.cs](../../../RoadGuardSystem.Repositories/Implementations/Storage/MinioAnh02ArtifactStore.cs) :44 — `OpenReadAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
