# 02 Backend Scope

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

EXPECTED_BUSINESS_BEHAVIOR: confirmed32–44 với supersession mới nhất trong HUY R01–R30, persistent login/ND01, GET Projects, Supervisor-global và Staff/Membership Read. CURRENT_IMPLEMENTATION: current source/EF/API Explorer tại baseline. Khi khác nhau, giữ hai nhãn riêng và GAP; không sửa nghiệp vụ theo code.

Scope gồm auth/OTP/account/session, directory/membership reads, projects/network roads/geometry/pavement/warranty/coverage, Reporter/Case/candidate/labels, survey/dataset/processing, uploads/storage, FIELD/measurement, policy/FT/normal repair/lifecycle/correction, offline device/snapshot/signed sync/transfer, notifications/clocks/duties, reporting/statistics/export/retention/hold, middleware/security/DI/tests/migrations.

R01 có hiệu lực rõ: bỏ quản lý vật liệu/bảo dưỡng và tính thời gian mở giao thông. Phương án sửa/checklist/evidence còn trong scope; warranty/maintenance coverage là eligibility fact, không tái tạo maintenance-management module. ND01 giữ original24h, chỉ additional-after-expiry deferred.

Out of scope: FE implementation, FastAPI internal model, orthomosaic/DSM/3D/depth. Deferred acceptance còn nguyên: live FastAPI, Android restart/reconnect, deployed physical SQL, real object storage, runtime deployment/UAT. Bộ tài liệu không chuyển các gate này thành requirement bị loại.

Task chỉ tạo docs mới; source/DTO/tests/config/migrations/V2/FE lock giữ byte-equivalent. Không seed/shared migrations, branch creation, remote publication hoặc deploy.
