# Canonical business workflows — HoangHaiWarrantySystem

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**Phase 2A · source pinned `d91c2e82f1c6feb3a7dcffc4573902314927ae94` · 14/10/2026 · chờ independent JEV review.**

[Workflow index](README.md) · [Review report](PHASE2A_REVIEW_REPORT.md) · [Status/GAP registry](../00_Governance/03_Implementation-and-Verification-Status.md).

`TARGET_CONFIRMED`: [owner decisions 32–44](../../product/confirmed-decisions.md), [latest HUY integration R/H/ND01](../../../planning/development/HUY-FINAL-INTEGRATION.md) và authorization Phase 2A của owner. `CURRENT_VERIFIED` ở đây chỉ observed source tại pinned SHA, không runtime PASS. Source bytes khớp 1334 hashes của inventory tại `6fdd98d98b41311e3af07a7b8cb95c55d8d62bc5`; [Phase 0 API/JEV](../../audit/HHWS-DOCS-REBUILD-01/00_EXECUTIVE_SUMMARY.md) được giữ nguyên phạm vi. `NOT_VERIFIED` tương ứng unknown runtime/integration; `PROPOSED` chưa implementation acceptance; test/checkpoint cũ `HISTORICAL`. [Authority/legend](../00_Governance/01_Authority-and-Source-of-Truth.md) quy định precedence.

## Phạm vi và cách đọc

Phase 1 đã được owner PASS; Phase 2A được authorized để viết business workflows. Đây là canonical narrative liên kết quyết định → actor → source producer → durable history → consumer boundary; không chỉ đổi tên Backend Official. Backend implementation/data model/API contract rewrite Phase 2B, Web/Mobile UI và FastAPI provider implementation **NOT_STARTED**. Technical identifiers RoadGuardSystem và Android route giữ nguyên.

Supervisor/PM Web; Reporter/Operator/Crew Mobile React Native Expo. Reporter intake độc lập Drone Survey; Supervisor triage unassigned Case rồi PM xử lý. AI tạo candidates, PM quyết định Defect, Crew đo vật lý. NORMAL/FT có authority riêng. GIS segment/section khác image segmentation. Không SfM/orthomosaic/DSM/3D/AI physical measurement.

## Workflow coverage

| Workflow | Source operations / controllers | Business boundary |
|---|---|---|
| [Authentication, users, staff và project memberships](01_Authentication-Users-and-Memberships.md) | 29 / 9 | Auth và các producer/consumer cùng nhóm |
| [Project initialization, road GIS, geometry và slab configuration](02_Project-Road-Geometry-and-Pavement.md) | 44 / 7 | Projects và các producer/consumer cùng nhóm |
| [Reporter intake, private evidence, IncidentCase và Supervisor triage](03_Reporter-Evidence-and-IncidentCase.md) | 19 / 3 | Reports và các producer/consumer cùng nhóm |
| [AI/Reporter candidates, PM assessment và Defect authority](04_Candidate-Decisions-and-Defect.md) | 12 / 3 | Candidates và các producer/consumer cùng nhóm |
| [Drone survey, dataset verification và processing jobs](05_Drone-Survey-Dataset-and-Processing.md) | 40 / 6 | SurveyPlanning và các producer/consumer cùng nhóm |
| [FIELD inspection, Crew physical measurement và provenance](06_FIELD-Inspection-and-Measurements.md) | 20 / 2 | FieldInspectionWorkflow và các producer/consumer cùng nhóm |
| [NORMAL repair: approval, execution, review và final confirmation](07_NORMAL-Repair-and-Acceptance.md) | 22 / 5 | RepairPackages và các producer/consumer cùng nhóm |
| [Fast Track policy, eligibility, authorization và confirmation](08_FastTrack-Policy-and-Confirmation.md) | 9 / 3 | RepairPolicies và các producer/consumer cùng nhóm |
| [Offline snapshots, device enrollment, signed batches và handover](09_Offline-Snapshots-Sync-and-Handover.md) | 18 / 1 | OfflineWorkflow và các producer/consumer cùng nhóm |
| [Notifications, publications, reporting, retention và evidence export](10_Notifications-Publication-Reporting-and-Retention.md) | 28 / 8 | Notifications và các producer/consumer cùng nhóm |

Appendices phân vùng **47 controllers / 241 operations / 223 paths** đúng source, mỗi API ID chỉ có một bảng chủ quản; cross-links dẫn shared repair/upload/publication thay copy payload. GET `/health` là operational entry riêng, ngoài 241 controller operations; không workflow nghiệp vụ hoặc dữ liệu mới. Controller coverage không chứng minh mọi điều kiện semantic/path/consumer đã test. DTO/schema/cấu trúc database chi tiết giữ full source registry, không viết lại Phase 2B.

## Cross-workflow lifecycle và controls

| Nguồn | Producer | Consumer / gate tiếp theo |
|---|---|---|
| Private verified ảnh | Reporter upload verify | Report/supplement → unassigned Case → Supervisor triage |
| Report/AI observation | Reporter hoặc dataset/processing | PM KEEP_NEW/LINK_EXISTING/REJECT → Defect; không model auto-confirm |
| Defect + geometry | PM accepted source | FIELD/task/repair binding; Crew measurement riêng |
| Formal FIELD / repair attempt | Current assigned Crew, verified evidence | PM review; NORMAL Supervisor final, FT PM final |
| Case conclusion | Authorized PM source | Publication command riêng/explicit recipients; không auto public |
| Snapshot/signature/receipt | Enrolled PM/Crew/current key | Supported registry/per-item admission; không grant vĩnh viễn |
| Outbox/inbox/read | Business producer + scoped recipient adapter | Business received ACK khác read; clock theo producer source |
| Export/retention inventory | Current authorized immutable source | Temporal download guards/expiry; basis/hold/review không auto delete |

Main flow là intended success sequence có source guards, không cam kết mọi request thành công. State names domain khác serialized API status; appendix trỏ controller/DTO để tránh gửi enum PascalCase như wire text chưa kiểm. K/version chỉ áp theo từng command. Snapshot/client cache không thay current server rights. Evidence pin/version/history giữ provenance qua supplement/correction/reassign/handover.

## Open findings và review gate

P1-01 provider, P1-02 callback attempt fencing, P1-03 matching, P1-04 Reporter download temporal security, P1-05 external verification; P2-06 FT Supervisor informational notification, P2-07 localized pre-Defect producer giữ nguyên mở. Các GAP docs/contract/test khác vẫn ở status registry, không tự đóng sau viết narrative.

Source-only business confidence **PARTIAL**; runtime/native/external acceptance **NOT_VERIFIED**. Xem review report để biết actual docs checks, test NOT_RUN và hai self-review passes. **STOP sau Phase 2A push; independent JEV review pending, không tự chuyển Phase 2B.**
