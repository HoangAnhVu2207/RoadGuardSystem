# Kiến trúc Backend

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## EXPECTED_BUSINESS_BEHAVIOR

TARGET_CONFIRMED: Web/Android ↔ ASP.NET Core BE ↔ FastAPI AI. BE giữ business authority, actor provenance, quyền hiện tại và durability; AI trả ứng viên từ video drone. Crew đo thực địa; PM quyết định nghiệp vụ. AI không đo kích thước thực tế. Live AI/Android/object storage cần acceptance riêng.

## CURRENT_IMPLEMENTATION

API là composition root. Controller bind HTTP/version/headers, đọc actor principal, gọi service và map DTO/ProblemDetails. Services kiểm actor, orchestration và typed reader. Repositories giữ EF/SQL, current scope, transaction, receipt, audit/outbox. BusinessObjects chứa entities/state/policy; DTOs giữ wire records. Migration source không chứng minh SQL đã deploy.

Read ví dụ: StaffDirectoryController.Members → StaffReadService.MembersAsync → CheckAccessAsync/MembersAsync → DTO không contact. Write ví dụ: export admission → current authority → serializable receipt/snapshot/job → worker claim/render → fenced completion → content kiểm quyền lại trước byte.

Pipeline: correlation → exception/status → API version validation → Development Swagger → HTTPS redirect → authentication → authorization → cookie mixed-actor/CSRF middleware → endpoints. WebCookieActivityFilter touch sau result phù hợp. /health là liveness, không SQL/object/AI readiness.

## State, transaction và concurrency

ETag bảo vệ resource version; receipt bảo vệ command lặp; immutable snapshot giữ facts đã phát hành. Chúng không thay thế nhau. Scheduler/retry không tự cấp quyền sửa, reset deadline hay origin. Export heartbeat dùng scope riêng vì DbContext không parallel-safe. SQL rollback không tự rollback external objects.

Supervisor global chỉ hành động Supervisor đã xác nhận; PM/Crew/Operator giữ project/assignment. Inbox luôn own recipient. Reporter ownership/file-specific quyền không mất khi biết ID. Cookie transport không là business authority.

## Gaps

UNKNOWN: replica/proxy/TLS/keyring durability, live external dependencies và deployed Swagger. PROPOSED: isolated SQL/object/keyring consistency + restart/restore acceptance. Không thêm framework, deploy hay sửa runtime trong docs task.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [Program.cs](../../../RoadGuardSystem.API/Program.cs) | `Program` :100 | CURRENT_VERIFIED source |
| [ServiceCollectionExtensions.cs](../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs) | `ServiceCollectionExtensions` :39 | CURRENT_VERIFIED source |
| [RoadGuardPersistenceExtensions.cs](../../../RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs) | `RoadGuardPersistenceExtensions` :32 | CURRENT_VERIFIED source |
| [StaffReadService.cs](../../../RoadGuardSystem.Services/Implementations/Staff/StaffReadService.cs) | `StaffReadService` :9 | CURRENT_VERIFIED source |
| [StaffReadRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Staff/StaffReadRepository.cs) | `StaffReadRepository` :6 | CURRENT_VERIFIED source |
| [ExportService.cs](../../../RoadGuardSystem.Services/Implementations/Exports/ExportService.cs) | `ExportService` :16 | CURRENT_VERIFIED source |
| [ExportRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs) | `ExportRepository` :14 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Symbol checkpoints

- [Program.cs](../../../RoadGuardSystem.API/Program.cs) :86 — `UseAuthentication` (`CURRENT_VERIFIED` source).
- [ExportService.cs](../../../RoadGuardSystem.Services/Implementations/Exports/ExportService.cs) :173 — `ProcessNextAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
