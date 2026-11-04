# 03 System Actors and Responsibilities

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

Supervisor ACTIVE thực hiện các thao tác Supervisor trên mọi project, không cần membership. Vẫn kiểm current account/session, đúng resource/project, state/evidence/version/idempotency. Không thay PM/Crew/Operator. Notification vẫn recipient-scoped; quyền global không broadcast hay đọc inbox người khác.

PM cấu hình/publish policy, xác nhận nguồn/Defect, gom đợt đo, giao công việc, kiểm tra FT/normal đúng nhánh và phạm vi hiện hành. PM vẫn chịu effective project membership; primary-PM là head cụ thể dùng cho duty/transfer, không đồng nhất mọi membership với primary.

Crew nhận assignment, xác nhận bắt đầu đi đo, lưu/nộp measurements và evidence, chỉ sửa khi đủ task/policy/coverage/location/window. MEASURE_ONLY không cấp quyền sửa. Nộp repaired là claim chờ review, chưa accepted.

Operator thực hiện assignment survey, video/SRT/dataset và source provenance; không làm FIELD/Crew command. Reporter đăng ký OTP, quản lý report/supplement/evidence của mình và đọc publication dành cho mình. Không đọc internal project content chỉ vì có report ID.

AI callback principal/worker được kiểm riêng; không là PM/Crew. Services/workers giữ source/recipient/occurrence identities và không tạo business authority từ technical delivery ACK. Intended recipient đọc/xác nhận đúng occurrence; substitute phải là actor hợp lệ, không account giả.

Owner quyết định business/public compatibility/data policy; Backend giữ adapter/proofs/contract; Web/Android/FastAPI do bên ngoài thực hiện theo PR44. Người nhận bên ngoài/ETA không được tự gán khi không có nguồn.
