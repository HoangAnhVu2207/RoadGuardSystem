# Notifications, clocks, reporting và statistics

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## EXPECTED_BUSINESS_BEHAVIOR

TARGET_CONFIRMED: weekly Monday09 Asia/Ho_Chi_Minh, PM tự gom đo, không automatic assignment. Read/delivery khác business ACK; không reset deadline/grant origin. Current responsibility/substitution giữ provenance. Report facts có source, unknown/partial khác0; không invent public-period/cohort/denominator/allocation. Supervisor vẫn own inbox recipient.

## Actor, API/DTO và preconditions

Authenticated current account, inbox own recipient + current source scope. GET /api/v1/notifications(cursor/limit), GET /{id}(ETag), POST /{id}/read(If-Match+Idempotency-Key), NotificationDto/NotificationPageDto. H6 GET notification scope/project clocks/weekly-digests qua H6NotificationReadService tiếp tục source/recipient checks. Cookie exact inbox methods hoặc H6 metadata.

ReportingController summary/items/timeline theo project/ReportingFiltersDto; authorized consistent capture dùng typed case/defect/repair/statistics readers. ProjectSummaryV1/ReportingCaptureDto giữ definitionVersion/readAt/availability/reason/sourceRefs. DefectStatisticsController GET/POST project defect-statistics là current read/actual source confirmation, không tự public allocation formula.

## Main flow/state/persistence

Producer source+audit+outbox cùng business transaction→worker observe calendar/clocks→claim→parse→source proof→current recipient→occurrence/delivery/notification/audit/scope. Unknown events audit; missing actor/source unresolved, retry. Strategies dùng actual responsibility/primary PM/project Supervisor membership, không broadcast tất cả Supervisor toàn hệ thống.

Inbox Visible lọc RecipientUserId==actor rồi exact source/current scope trước cursor/order/take; legacy unknown không thành public. Supervisor project set global vẫn owned filter. Mark-read version/receipt không ACK business request. Weekly missed occurrence history/bounded aggregate giữ pending stock, process restart không origin clock.

Reporting consistent capture joins current repair pins, LD08 distinct Defect/shared physical part và actual FIELD measurements. Unallocated/unknown không0. Supersession đổi current facts, immutable issued export giữ payload/hash cũ. Statistics-source failure không silently làm complete aggregate.

## Concurrency/errors

Inbox missing headers428, stale412, conflicting key409,invisible404. Clocks bad cursor/limit400, scope403,stale cursor409. Fenced lease/source proof/current recipient/atomic receipt tránh nhân delivery; rollback/retry giữ occurrence. Reporting invalid producer/current authority trả reason.

## Gaps

CURRENT_VERIFIED source: ConfirmFinalAsync (`RepairWorkflowRepository.Producing.cs:33–88`) ghi final decision/obligation/audit/save nhưng không EmitProducer; H6 repair adapter (`H6RepairNotificationSourceAdapter.cs:259–260`) trả PENDING_PRODUCER cho nhánh final. Đây localized thiếu source-proven final FT notification, không thiếu final business decision và không authorization thêm Supervisor FT approval. Historical audit G04 được cross-check lại bằng source; chưa runtime reproduction/test ở task này. UNKNOWN live push/email/backlog SLO/public statistics formulas. PROPOSED bounded producer follow-up bởi Huy, không thêm Supervisor FT approval. Tests NOT_RUN.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [NotificationsController.cs](../../../RoadGuardSystem.API/Controllers/NotificationsController.cs) | `NotificationsController` :17 | CURRENT_VERIFIED source |
| [H6NotificationOperationsController.cs](../../../RoadGuardSystem.API/Controllers/H6NotificationOperationsController.cs) | `H6NotificationOperationsController` :18 | CURRENT_VERIFIED source |
| [ReportingController.cs](../../../RoadGuardSystem.API/Controllers/ReportingController.cs) | `ReportingController` :17 | CURRENT_VERIFIED source |
| [DefectStatisticsController.cs](../../../RoadGuardSystem.API/Controllers/DefectStatisticsController.cs) | `DefectStatisticsController` :15 | CURRENT_VERIFIED source |
| [H6ProtectedNotificationRepository.Scope.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Scope.cs) | `H6ProtectedNotificationRepository` :7 | CURRENT_VERIFIED source |
| [H6NotificationDispatchRepository.Delivery.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Delivery.cs) | `H6NotificationDispatchRepository` :9 | CURRENT_VERIFIED source |
| [H6NotificationWorker.cs](../../../RoadGuardSystem.API/Workers/H6NotificationWorker.cs) | `H6NotificationWorker` :7 | CURRENT_VERIFIED source |
| [ReportingService.cs](../../../RoadGuardSystem.Services/Implementations/Reporting/ReportingService.cs) | `ReportingService` :10 | CURRENT_VERIFIED source |
| [DefectStatisticsCaptureConsumer.cs](../../../RoadGuardSystem.Services/Implementations/Reporting/DefectStatisticsCaptureConsumer.cs) | `DefectStatisticsCaptureConsumer` :5 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [H6NotificationOperationsHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Notifications/H6NotificationOperationsHttpTests.cs)
- [H6NotificationDispatchSqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Notifications/H6NotificationDispatchSqlTests.cs)
- [OwnerWeeklyRecoverySqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Notifications/OwnerWeeklyRecoverySqlTests.cs)

## Symbol checkpoints

- [H6ProtectedNotificationRepository.Scope.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Scope.cs) :12 — `Visible` (`CURRENT_VERIFIED` source).
- [RepairWorkflowRepository.Producing.cs](../../../RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Producing.cs) :33 — `ConfirmFinalAsync` (`CURRENT_VERIFIED` source).
- [H6RepairNotificationSourceAdapter.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6RepairNotificationSourceAdapter.cs) :252 — `PENDING_PRODUCER` (`CURRENT_VERIFIED` source).
- [ReportingService.cs](../../../RoadGuardSystem.Services/Implementations/Reporting/ReportingService.cs) :20 — `Authorized` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
