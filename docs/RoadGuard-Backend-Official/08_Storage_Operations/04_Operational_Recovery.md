# Operational recovery và retry

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Target/authority

TARGET_CONFIRMED PR40 RPO≤15min/RTO≤4h là target, chưa restore result. Recovery không reset business clock/grant origin/source pin/audit history. Offline handover cần Supervisor/receiving PM current scope/original actor; mất thiết bị/key trước sync có thể không cứu, không invent organization escrow.

## Runbook

1. Record SHA/deployment/correlation/request/job IDs, không raw credentials/PII.
2. Unknown HTTP result kiểm durable receipt; replay same canonical key/payload với current authority, không tạo key mới ép effect lần2.
3. Export QUEUED/RUNNING kiểm lease/token/LeaseUntil/NextAttemptAt; worker reclaim expired, render immutable snapshot, current rights/metadata, fenced completion. Không SQL-update SUCCEEDED thủ công.
4. Multipart dùng recovery service/exact object/upload IDs/durable state; phân biệt object completed với Verified. Không xóa prefix/bucket hàng loạt.
5. H6 kiểm source/recipient/responsibility/unresolved reason/occurrence history; retry same occurrence, không reset clock. Unknown audit chưa delivery. Missed weekly aggregate giữ missed history.
6. Retention evaluation chỉ review. Basis change fresh inventory+If-Match+reason, hold release đúng role/version. Incomplete/hold active không purge.
7. Offline chỉ xóa local sau durable ACK; expiry/revoke không xóa queue. Reconnect signed/device/current source và provenance. Client restart/device acceptance external gate.

## Atomicity/state/concurrency

SQL rollback giữ effect/receipt/audit/outbox cùng boundary. Object I/O không SQL rollback; deterministic artifact keys/hash/metadata/recovery state cần reconcile. Export heartbeat riêng scope, lease lost không complete job khác; content post-spool quyền/expiry check. Recovery không edit immutable history/applied migration.

Consistent restore cần SQL/object content/references/DataProtection+signing key custody/config revision cùng recovery point. SQL restore đơn độc chưa chứng minh file access/protected renewal. Hold/retention history/issued hashes phải giữ. Không chép secrets vào backup report.

API dùng same idempotency/If-Match khi command yêu cầu; DTO/state/action errors theo catalog, không generic retry mọi4xx. Wrong actor/project/revoked phải giải quyết current authorization, không retry bypass. Missing source giữ UNKNOWN/unresolved thay fabricate complete.

## Gaps/verification

UNKNOWN timed restore/crash replica/real network loss/device kill-reboot-import-storage-full/live mail/AI/SLO. PROPOSED isolated before-after-commit/lost ACK/concurrent replay/lease expiry-keyring restart/SQL-object consistency+timed targets. Tests NOT_RUN task này; historical suites không deployed recovery certificate. Không data-writing tests shared DB.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [MultipartRecoveryWorker.cs](../../../RoadGuardSystem.API/Workers/MultipartRecoveryWorker.cs) | `MultipartRecoveryWorker` :5 | CURRENT_VERIFIED source |
| [H6NotificationWorker.cs](../../../RoadGuardSystem.API/Workers/H6NotificationWorker.cs) | `H6NotificationWorker` :7 | CURRENT_VERIFIED source |
| [ExportWorker.cs](../../../RoadGuardSystem.API/Workers/ExportWorker.cs) | `ExportWorker` :6 | CURRENT_VERIFIED source |
| [ExportRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs) | `ExportRepository` :14 | CURRENT_VERIFIED source |
| [ExportService.cs](../../../RoadGuardSystem.Services/Implementations/Exports/ExportService.cs) | `ExportService` :16 | CURRENT_VERIFIED source |
| [RetentionRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs) | `RetentionRepository` :12 | CURRENT_VERIFIED source |
| [OutboxWorkRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/OutboxWorkRepository.cs) | `OutboxWorkRepository` :8 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [Anh01StorageRecoveryTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Files/Anh01StorageRecoveryTests.cs)
- [H6NotificationRetrySqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Notifications/H6NotificationRetrySqlTests.cs)

## Symbol checkpoints

- [ExportRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs) :92 — `ClaimAsync` (`CURRENT_VERIFIED` source).
- [ExportRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs) :109 — `CompleteAsync` (`CURRENT_VERIFIED` source).
- [MultipartRecoveryWorker.cs](../../../RoadGuardSystem.API/Workers/MultipartRecoveryWorker.cs) :17 — `RecoverMultipartsAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
