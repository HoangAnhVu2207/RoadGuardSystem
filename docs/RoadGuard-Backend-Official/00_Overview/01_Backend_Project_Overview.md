# 01 Backend Project Overview

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

Current details: [integration, authority, compatibility and evidence](../CURRENT-INTEGRATION-20261027.md); [exact current endpoint catalog](../03_API_Contracts/02_Endpoint_Catalog.md); [384 current schema definitions](../../HoangHaiWarrantySystem-Official/04_API-Contracts/03_DTO-and-Schema-Reference.md). Historical deep traces below must be rechecked against current SHA before reuse.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

RoadGuard điều phối hồ sơ khiếm khuyết mặt đường từ nguồn Reporter hoặc drone tới quyết định PM, nhiệm vụ Crew, sửa chữa và quyết định kết thúc. Hệ thống giữ source/evidence/history; upload hay AI candidate không tự trở thành Defect hoặc repair accepted.

Kiến trúc hiện hành: **Web/Android ↔ ASP.NET Core Backend ↔ FastAPI AI**. ASP.NET Core chịu HTTP contracts, account/session, quyền, transaction, idempotency, clocks, audit/outbox và persistence. SQL Server giữ domain state và metadata; object storage giữ bytes qua adapter; background workers chỉ chạy khi được đăng ký/cấu hình. FastAPI là consumer/producer bên ngoài; worker ANH02 synthetic không chứng minh model thật.

AI đọc video drone để đề xuất ứng viên. Crew đo vật lý trực tiếp theo checklist, vị trí và evidence. PM xác nhận nghiệp vụ/nhãn; Supervisor phê duyệt đúng normal-track và giám sát theo scope đã chốt. Không đưa orthomosaic/DSM/3D/AI depth vào scope.

Entry points: API Controllers → Services → Repositories → RoadGuardDbContext/SQL/storage. Có các controller dùng service lookup hoặc concrete orchestrator; không ép kiến trúc hiện có thành mẫu lý tưởng. Complete inventory và operation map là bằng chứng navigation, không là chứng nhận deployed E2E.

Project baseline là commit docs-only sau feature GET Projects/Supervisor-global/Staff Read; những chức năng này được giữ nguyên. Bản docs mới mô tả toàn Backend, không chỉ các feature vừa thêm.
