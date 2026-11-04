# 02 Backend Scope

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

EXPECTED_BUSINESS_BEHAVIOR: confirmed32–44 với supersession mới nhất trong HUY R01–R30, persistent login/ND01, GET Projects, Supervisor-global và Staff/Membership Read. CURRENT_IMPLEMENTATION: current source/EF/API Explorer tại baseline. Khi khác nhau, giữ hai nhãn riêng và GAP; không sửa nghiệp vụ theo code.

Scope gồm auth/OTP/account/session, directory/membership reads, projects/network roads/geometry/pavement/warranty/coverage, Reporter/Case/candidate/labels, survey/dataset/processing, uploads/storage, FIELD/measurement, policy/FT/normal repair/lifecycle/correction, offline device/snapshot/signed sync/transfer, notifications/clocks/duties, reporting/statistics/export/retention/hold, middleware/security/DI/tests/migrations.

R01 có hiệu lực rõ: bỏ quản lý vật liệu/bảo dưỡng và tính thời gian mở giao thông. Phương án sửa/checklist/evidence còn trong scope; warranty/maintenance coverage là eligibility fact, không tái tạo maintenance-management module. ND01 giữ original24h, chỉ additional-after-expiry deferred.

Out of scope: FE implementation, FastAPI internal model, orthomosaic/DSM/3D/depth. Deferred acceptance còn nguyên: live FastAPI, Android restart/reconnect, deployed physical SQL, real object storage, runtime deployment/UAT. Bộ tài liệu không chuyển các gate này thành requirement bị loại.

Task chỉ tạo docs mới; source/DTO/tests/config/migrations/V2/FE lock giữ byte-equivalent. Không seed/shared migrations, branch creation, remote publication hoặc deploy.
