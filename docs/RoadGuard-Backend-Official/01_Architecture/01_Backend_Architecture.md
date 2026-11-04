# Kiến trúc Backend

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

Current details: [integration, authority, compatibility and evidence](../CURRENT-INTEGRATION-20261027.md); [exact current endpoint catalog](../03_API_Contracts/02_Endpoint_Catalog.md); [384 current schema definitions](../../HoangHaiWarrantySystem-Official/04_API-Contracts/03_DTO-and-Schema-Reference.md). Historical deep traces below must be rechecked against current SHA before reuse.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

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
