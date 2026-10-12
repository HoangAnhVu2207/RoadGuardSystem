# Retention và export workflow

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## EXPECTED_BUSINESS_BEHAVIOR

TARGET_CONFIRMED PR41A: hồ sơ/evidence/video hết mọi bảo hành+5năm, technical log90d/export tạm30d/backup35d, hold chặn xóa. Missing basis/date/multiple unresolved nghĩa vụ→WAITING_RETENTION_BASIS. Training chỉ approved labels. Server DOSSIER/TRAINING export khác encrypted device handover PR42A.

## Retention actor/API/DTO

RetentionController principal/current role rồi repository current account/project/resource. Supervisor confirm basis/create/release hold; các read/evaluation theo explicit authorization source, không mọi bearer. GET project retention/files/{fileId}; PUT basis ConfirmRetentionBasisRequest+Idempotency-Key+If-Match; POST evaluations/CreateRetentionEvaluationRequest→202 Location; GET evaluation cursor/pageSize; POST retention/holds/CreateRetentionHoldRequest→201; GET hold; POST release/ReleaseRetentionHoldRequest+If-Match.

Main: reference inventory đủ current/history/warranties/additional obligations→pin ExpectedReferenceInventoryVersion/exact warranty set/head version/reason→append basis revision/head/audit. Hold PROJECT/FILE ACTIVE→released giữ history. Evaluation QUEUED→COMPLETE atomically với item control snapshots/reasons/basis/inventory/hold versions. ELIGIBLE_FOR_REVIEW không deletion. Max mọi proven boundaries; thiếu basis tiếp tục waiting.

## Export actor/API/DTO/main flow

PM/Supervisor Active/active role/current project guard + source rights trước replay/capture/render/download. POST project exports/CreateExportRequestDto+Idempotency-Key→202+ETag+Location; GET ExportJobViewDto; GET manifest/ExportManifestV1Dto; GET content file+SHA ETag/private,no-store. DOSSIER PDF/ZIP, TRAINING ZIP, PDF không originals. Nonempty DefectIds bị Normalize từ chối, không bỏ filter âm thầm; SegmentIds distinct/sorted và bounded time/filter checks.

Serializable receipt admission→immutable payload/manifest/file/source revisions/hash+QUEUED→lease RUNNING→render/store/check metadata→fenced SUCCEEDED. TRAINING cần approved source/provenance, empty/missing/invalid trả reason. Dossier reporterEvidence unavailable khi không có access producer; không dựng evidence giả. Snapshot đã phát hành không sửa theo nguồn superseded.

## Concurrency, persistence, errors

Retention If-Match thiếu428/malformed412,stale/incomplete inventory409,basis422,key conflict409; serializable application/control/source locks bảo vệ current state. Export thiếu key428,conflict/not-ready409,expired410,dependency/storage503,invalid422. Lease token/heartbeat scope riêng; same-key retry giữ snapshot. Content recheck current rights/pins/expiry sau OpenRead, dispose khi denied. Job expiry không tự xóa artifact.

## Gaps

Evaluation không purge. PROPOSED destructive workflow riêng cần owner scope/current all-obligations/hold recheck/audit/recovery. UNKNOWN live object/PDF deployment/legal nghĩa vụ ngoài policy/actual backups. Tests NOT_RUN; source migration không LIVE_SQL_VERIFIED.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [RetentionController.cs](../../../RoadGuardSystem.API/Controllers/RetentionController.cs) | `RetentionController` :17 | CURRENT_VERIFIED source |
| [ExportsController.cs](../../../RoadGuardSystem.API/Controllers/ExportsController.cs) | `ExportsController` :16 | CURRENT_VERIFIED source |
| [RetentionService.cs](../../../RoadGuardSystem.Services/Implementations/Retention/RetentionService.cs) | `RetentionService` :6 | CURRENT_VERIFIED source |
| [RetentionRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs) | `RetentionRepository` :12 | CURRENT_VERIFIED source |
| [RetentionEligibilityEvaluator.cs](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionEligibilityEvaluator.cs) | `RetentionEligibilityInput` :4 | CURRENT_VERIFIED source |
| [ExportService.cs](../../../RoadGuardSystem.Services/Implementations/Exports/ExportService.cs) | `ExportService` :16 | CURRENT_VERIFIED source |
| [ExportRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs) | `ExportRepository` :14 | CURRENT_VERIFIED source |
| [RetentionInventoryRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionInventoryRepository.cs) | `RetentionInventoryRepository` :9 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [Anh02RetentionHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Retention/Anh02RetentionHttpTests.cs)
- [Anh02RetentionPersistenceTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Retention/Anh02RetentionPersistenceTests.cs)

## Symbol checkpoints

- [RetentionRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs) :24 — `AuthorizeAsync` (`CURRENT_VERIFIED` source).
- [ExportService.cs](../../../RoadGuardSystem.Services/Implementations/Exports/ExportService.cs) :132 — `ContentAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
