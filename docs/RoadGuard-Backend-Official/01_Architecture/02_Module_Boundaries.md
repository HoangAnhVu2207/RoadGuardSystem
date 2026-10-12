# Ranh giới module

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Context và responsibility

| Module | Trách nhiệm BE | Authority / không suy diễn |
|---|---|---|
| Identity/staff | credentials/session/OTP/account/roster | Current account/role; token role chưa đủ |
| Project/GIS/pavement | membership/primary PM/network/geometry/slabs | Published/source pins; TEST_ONLY chưa official |
| Reporter/case/defect/label | ownership/triage/lifecycle/PM decisions | Không AI auto approve/merge |
| Survey/upload/processing | assignment/dataset/verified objects/AI attempts | Mock chưa live provider |
| FIELD/repair/safety | Crew measurement/policy/attempt/review/clock | PM không kế thừa Crew; timer không cấp grant |
| Offline | device/key/snapshot/signed adapter/handover | Client timestamp không trusted UTC |
| Messaging | source proof/occurrence/recipient/delivery/audit | Read không business ACK; không global broadcast |
| Reporting/export/retention | consistent facts/immutable issue/current read/hold | Unknown khác0; eligibility chưa xóa |

TARGET_CONFIRMED PR44: FE/Android/AI thuộc đối tác; BE chịu adapter/contract/fixtures/integration. Module map là reading route lịch sử, không current runtime inventory hoặc assignment mới.

## Producer → consumer

Reporting đọc ICaseDefectReadReader, ICurrentRepairFactsReader, IDefectStatisticsRepository; giữ UNKNOWN/partial/source revisions. Export đóng băng payload/manifest/hash: supersession đổi báo cáo mới, không đổi issued snapshot. Retention contributor giữ cả historical references; không chỉ live UI link.

H6 source adapters FIELD/repair/deadline/weekly/safety phải có real producer proof. Registered event không tự đủ delivery. Unknown audit giữ unsupported source minh bạch. Legacy NotificationOutboxConsumer khác H6 runtime dispatcher; DI registration chưa proof production caller.

## Ownership, errors và gaps

Huy điều phối shared DI/DbContext/migrations/canonical contract; task này chỉ docs mới, một writer/file. Cross-module consumer không được bypass producer guard cho test xanh. Transactions/receipts/source pins phải ở đúng boundary. Missing source/dependency trả reason thay complete facts giả.

UNKNOWN: live FE/Android/AI adoption. Localized case FIELD/DRONE pre-Defect, compatibility AI callback và official source gates phải giữ riêng; không nói blanket module absent. Shared history/contract không sửa để khớp mô tả.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [RoadGuardPersistenceExtensions.cs](../../../RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs) | `RoadGuardPersistenceExtensions` :32 | CURRENT_VERIFIED source |
| [Huy01ReporterPersistenceExtensions.cs](../../../RoadGuardSystem.Repositories/Extensions/Huy01ReporterPersistenceExtensions.cs) | `Huy01ReporterPersistenceExtensions` :8 | CURRENT_VERIFIED source |
| [ReportingService.cs](../../../RoadGuardSystem.Services/Implementations/Reporting/ReportingService.cs) | `ReportingService` :10 | CURRENT_VERIFIED source |
| [ExportService.cs](../../../RoadGuardSystem.Services/Implementations/Exports/ExportService.cs) | `ExportService` :16 | CURRENT_VERIFIED source |
| [RetentionInventoryRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionInventoryRepository.cs) | `RetentionInventoryRepository` :9 | CURRENT_VERIFIED source |
| [NotificationOutboxConsumer.cs](../../../RoadGuardSystem.Repositories/Implementations/Messaging/NotificationOutboxConsumer.cs) | `NotificationDeliveryResult` :5 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Symbol checkpoints

- [ReportingService.cs](../../../RoadGuardSystem.Services/Implementations/Reporting/ReportingService.cs) :15 — `CaptureAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
