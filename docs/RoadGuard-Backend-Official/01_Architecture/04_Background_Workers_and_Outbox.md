# Background workers và outbox

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## EXPECTED_BUSINESS_BEHAVIOR

TARGET_CONFIRMED: reminder tuần để PM tự gom đo, không tạo/giao task/quyền. Retry giữ occurrence/clock history, read/delivery khác business ACK. Retention chỉ evaluation; AI mock chỉ BE/mock.

## Hosted registration

| Worker | Điều kiện registration | Công việc / interval source |
|---|---|---|
| ValidationRunWorker | Luôn tại AddApiPlatformServices | CompleteNextValidationRunAsync, idle1s |
| H6NotificationWorker | Luôn tại AddApiPlatformServices | ObserveCalendar/Clocks, RetryUnresolved, AuditUnregistered, tối đa100 ProcessOne/iteration,5s |
| UploadVerificationWorker | Program: Development + MinioStorage:Endpoint khác rỗng | ProcessOneVerificationInRoundAsync,30s |
| MultipartRecoveryWorker | UploadSession:RecoveryEnabled | RecoverMultipartsAsync,5s |
| ExportWorker | Anh02:WorkersEnabled | ProcessNextAsync; ExportOptions poll tối thiểu1s |
| RetentionEvaluationWorker | Anh02:WorkersEnabled | ProcessOneAsync,15s |
| AiMockRunWorker | WorkersEnabled + MockEnabled + Development/Testing/Test | BE mock, không FastAPI thật |

Worker tạo scope cho scoped DbContext; cancellation thoát, logged error retry không chứng minh effect hoàn thành. Host register không là deployed scheduler liveness.

## Durable consumers và state

Business producer ghi nguồn/audit/outbox cùng command transaction. H6 claim registered message → parse catalog → source proof/current recipient → occurrence/attempt/notification/scope/audit với fenced lease/atomic receipt. Protocol lỗi RejectAsync; missing proof/actor giữ unresolved. Retry không gửi mọi Supervisor toàn hệ thống.

Legacy NotificationOutboxConsumer.ConsumeAsync dùng ConsumerEffectService với consumer name notification-inbox, receipt bảo vệ một effect. Đăng ký scoped không tự chạy background, không thay H6 source pipeline. OutboxWorkRepository là lease seam; enqueue không chứng minh external push/email.

Export QUEUED/RUNNING lease expiry được reclaim; token fence Complete/Fail. Heartbeat20s scope riêng renew. Retention dùng serializable + SQL application lock, item snapshots/COMPLETE cùng commit; không deletion. Missed weekly occurrence giữ history/bounded aggregate, restart không reset clock origin.

## Consumers không hosted

| Consumer | Caller và effect | Phân biệt registration |
|---|---|---|
| ConsumerEffectService | NotificationOutboxConsumer gọi ProcessAsync(messageId,consumerName,effect) | Scoped transaction/ConsumerEffectReceipt, không hosted loop |
| H6NotificationDispatchService | H6 worker gọi ProcessOneAsync → dispatch repo | Hosted worker là caller; source proof nằm repo/adapters |
| CaseDefectCaptureConsumer | ReportingService.CaptureAsync gọi Apply | Static synchronous projection, không queue consumer |
| CurrentRepairCaptureConsumer | ReportingService.CaptureAsync gọi Apply | Static current facts→metrics/source refs |
| DefectStatisticsCaptureConsumer | ReportingService.CaptureAsync gọi Apply | Static statistics→report facts; không tự tạo measurements |

Các static consumers không cần DI đăng ký và không chạy độc lập. ExportService gọi ReportingService capture để freeze payload; không rewrite issued snapshot. NotificationOutboxConsumer legacy registration khác actual H6 delivery repository ghi Notification trực tiếp.

## Recovery/gaps

Crash trước commit retry; sau commit trước ACK replay canonical key/payload với current rights. UNKNOWN: replica topology/backlog/SLO/live delivery. PROPOSED: monitor queued age/expired lease/retry reason/unresolved source và isolated concurrent worker/process-restart tests. NOT_RUN task này.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [Program.cs](../../../RoadGuardSystem.API/Program.cs) | `Program` :100 | CURRENT_VERIFIED source |
| [ServiceCollectionExtensions.cs](../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs) | `ServiceCollectionExtensions` :39 | CURRENT_VERIFIED source |
| [ValidationRunWorker.cs](../../../RoadGuardSystem.API/BackgroundServices/ValidationRunWorker.cs) | `ValidationRunWorker` :5 | CURRENT_VERIFIED source |
| [H6NotificationWorker.cs](../../../RoadGuardSystem.API/Workers/H6NotificationWorker.cs) | `H6NotificationWorker` :7 | CURRENT_VERIFIED source |
| [UploadVerificationWorker.cs](../../../RoadGuardSystem.API/Workers/UploadVerificationWorker.cs) | `UploadVerificationWorker` :5 | CURRENT_VERIFIED source |
| [MultipartRecoveryWorker.cs](../../../RoadGuardSystem.API/Workers/MultipartRecoveryWorker.cs) | `MultipartRecoveryWorker` :5 | CURRENT_VERIFIED source |
| [ExportWorker.cs](../../../RoadGuardSystem.API/Workers/ExportWorker.cs) | `ExportWorker` :6 | CURRENT_VERIFIED source |
| [RetentionEvaluationWorker.cs](../../../RoadGuardSystem.API/Workers/RetentionEvaluationWorker.cs) | `RetentionEvaluationWorker` :4 | CURRENT_VERIFIED source |
| [AiMockRunWorker.cs](../../../RoadGuardSystem.API/Workers/AiMockRunWorker.cs) | `AiMockRunWorker` :4 | CURRENT_VERIFIED source |
| [NotificationOutboxConsumer.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/NotificationOutboxConsumer.cs) | `NotificationDeliveryResult` :5 | CURRENT_VERIFIED source |
| [OutboxWorkRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/OutboxWorkRepository.cs) | `OutboxWorkRepository` :8 | CURRENT_VERIFIED source |
| [H6NotificationDispatchService.cs](../../../RoadGuardSystem.Services/Implementations/Messaging/H6NotificationDispatchService.cs) | `H6NotificationDispatchService` :6 | CURRENT_VERIFIED source |
| [H6NotificationDispatchRepository.Delivery.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Delivery.cs) | `H6NotificationDispatchRepository` :9 | CURRENT_VERIFIED source |
| [ExportRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs) | `ExportRepository` :14 | CURRENT_VERIFIED source |
| [RetentionRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs) | `RetentionRepository` :12 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [H6NotificationOperationsHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Notifications/H6NotificationOperationsHttpTests.cs)
- [H6NotificationDispatchSqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Notifications/H6NotificationDispatchSqlTests.cs)
- [OwnerWeeklyRecoverySqlTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Notifications/OwnerWeeklyRecoverySqlTests.cs)

## Symbol checkpoints

- [ServiceCollectionExtensions.cs](../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs) :131 — `AddHostedService<ValidationRunWorker>` (`CURRENT_VERIFIED` source).
- [Program.cs](../../../RoadGuardSystem.API/Program.cs) :39 — `AddHostedService<ExportWorker>` (`CURRENT_VERIFIED` source).
- [NotificationOutboxConsumer.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/NotificationOutboxConsumer.cs) :26 — `ConsumeAsync` (`CURRENT_VERIFIED` source).
- [ConsumerEffectService.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/ConsumerEffectService.cs) :29 — `ProcessAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
