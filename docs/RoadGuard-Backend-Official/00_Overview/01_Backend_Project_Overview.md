# 01 Backend Project Overview

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

RoadGuard điều phối hồ sơ khiếm khuyết mặt đường từ nguồn Reporter hoặc drone tới quyết định PM, nhiệm vụ Crew, sửa chữa và quyết định kết thúc. Hệ thống giữ source/evidence/history; upload hay AI candidate không tự trở thành Defect hoặc repair accepted.

Kiến trúc hiện hành: **Web/Android ↔ ASP.NET Core Backend ↔ FastAPI AI**. ASP.NET Core chịu HTTP contracts, account/session, quyền, transaction, idempotency, clocks, audit/outbox và persistence. SQL Server giữ domain state và metadata; object storage giữ bytes qua adapter; background workers chỉ chạy khi được đăng ký/cấu hình. FastAPI là consumer/producer bên ngoài; worker ANH02 synthetic không chứng minh model thật.

AI đọc video drone để đề xuất ứng viên. Crew đo vật lý trực tiếp theo checklist, vị trí và evidence. PM xác nhận nghiệp vụ/nhãn; Supervisor phê duyệt đúng normal-track và giám sát theo scope đã chốt. Không đưa orthomosaic/DSM/3D/AI depth vào scope.

Entry points: API Controllers → Services → Repositories → RoadGuardDbContext/SQL/storage. Có các controller dùng service lookup hoặc concrete orchestrator; không ép kiến trúc hiện có thành mẫu lý tưởng. Complete inventory và operation map là bằng chứng navigation, không là chứng nhận deployed E2E.

Project baseline là commit docs-only sau feature GET Projects/Supervisor-global/Staff Read; những chức năng này được giữ nguyên. Bản docs mới mô tả toàn Backend, không chỉ các feature vừa thêm.
