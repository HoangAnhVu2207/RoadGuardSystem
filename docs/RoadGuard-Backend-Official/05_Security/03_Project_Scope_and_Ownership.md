# Project scope, assignment và ownership

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Authority

TARGET_CONFIRMED: Supervisor global chỉ hành động Supervisor được chốt. PM/Crew/Operator giữ project/assignment; Reporter giữ ownership. Operation-level predicate/current source quyết định, không role token đơn lẻ. Ma trận toàn operation do root giữ; trang này giải thích guard/exceptions.

ProjectScopeGuard.AuthorizeAsync kiểm IDs khác rỗng/role known, rồi explicit Supervisor bypass trả ProjectAccessScope không membership (dòng31–34). Với role khác, guard đọc membership actor/project, so role/Active/effective ValidFrom/ValidTo theo UTC day. Guard này không tự kiểm current account/role activation hoặc project existence; caller/service/repository vẫn phải làm các kiểm tra đó. Không suy Supervisor được execute hành động Crew/Operator chỉ vì guard trả scope. StaffReadRepository.CheckAccess kiểm current user/role/MustChangePassword rồi Supervisor global hoặc PM effective membership. ProjectListReadModel: Supervisor all, Operator/Crew membership+actual assignment/source binding.

## Preconditions, state và ownership

FIELD/repair cần active assignment/current native binding/policy/source pins/current project. Project-list visibility không execution grant. PM không kế thừa Crew, Operator dùng survey nguồn. Offline reconnect kiểm actor/device/key/snapshot/task/current rights trước typed command/replay/durable ACK; signature/timestamp không tự cấp business authority.

Reporter report/evidence cần owner hoặc explicit sourced business access; file/project ID không đủ. PII staff user/account detail chỉ Supervisor; members/primary chỉ IDs/name/role/status/dates/version. Export source access giữ riêng, không mọi project member được đọc mọi evidence.

Notifications Visible khởi owned RecipientUserId==actor rồi joins exact source/current projects/assignments. Global Supervisor chỉ nới protected project visibility của notification chính mình. Sidecar scope không independent grant; unknown legacy source không thành public. Delivery actual responsibility/recipient strategy khác global read privilege.

## Concurrent revoke / receipt

Server validator + repository current authority chống stale token. Check cả replay/transaction phù hợp; receipt chứng minh prior effect, không perpetual current read. Export kiểm trước capture/process/complete và sau awaited storage OpenRead; retention lock inventory/control scope. If-Match stale khác payload-conflicting Idempotency-Key. Wrong actor/project thường403 hoặc source-invisible404 theo operation; exact codes ở matrix/catalog.

## Gaps

UNKNOWN external client provenance/handover. Audit G03 ReporterEvidence delayed-open risk là NOT_VERIFIED risk, không exploit proven. PROPOSED isolated delayed I/O/revocation acceptance trước security fix; docs không đổi guard/broadcast policy. Unselected tests NOT_RUN; fresh selected outcomes được bổ sung bên dưới.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [ProjectScopeGuard.cs](../../../RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs) | `ProjectScopeGuard` :6 | CURRENT_VERIFIED source |
| [StaffReadRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Staff/StaffReadRepository.cs) | `StaffReadRepository` :6 | CURRENT_VERIFIED source |
| [ProjectListReadModel.cs](../../../RoadGuardSystem.Repositories/Implementations/Projects/ProjectListReadModel.cs) | `ProjectListReadModel` :7 | CURRENT_VERIFIED source |
| [H6ProtectedNotificationRepository.Scope.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Scope.cs) | `H6ProtectedNotificationRepository` :7 | CURRENT_VERIFIED source |
| [H6NotificationDispatchRepository.Delivery.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Delivery.cs) | `H6NotificationDispatchRepository` :9 | CURRENT_VERIFIED source |
| [ExportService.cs](../../../RoadGuardSystem.Services/Implementations/Exports/ExportService.cs) | `ExportService` :16 | CURRENT_VERIFIED source |
| [RetentionRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs) | `RetentionRepository` :12 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [StaffMembershipReadHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Projects/StaffMembershipReadHttpTests.cs)
- [H6NotificationOperationsHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Notifications/H6NotificationOperationsHttpTests.cs)
- [H6NotificationDispatchSqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Notifications/H6NotificationDispatchSqlTests.cs)
- [OwnerWeeklyRecoverySqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Notifications/OwnerWeeklyRecoverySqlTests.cs)

## Symbol checkpoints

- [ProjectScopeGuard.cs](../../../RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs) :30 — `authoritativeRole == UserRoleCode.Supervisor` (`CURRENT_VERIFIED` source).
- [H6ProtectedNotificationRepository.Scope.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Scope.cs) :23 — `var owned` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.

## Central fresh execution update

| Class | PASS | FAIL | Limit |
|---|---|---|---|
| StaffMembershipReadHttpTests | 17 | 0 | Selected actual cases only; no all-route/E2E certification |

Các test khác không trong executed-tests.json vẫn NOT_RUN.
