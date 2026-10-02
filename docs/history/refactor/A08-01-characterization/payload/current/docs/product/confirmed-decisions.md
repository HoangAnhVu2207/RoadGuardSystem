# Confirmed owner decisions 32-44 (RF-04 draft transfer)

The owner reconfirmed these complete interpretations on 2026-09-30. Original 2026-09-28 question/option wording remains unavailable. Accepted here means requirement authority only: implementation, HTTP contract and tests remain separately unverified. Source: `planning/refactor/02-decision-register.md`, first confirmed table.

| ID | Confirmed interpretation | Consequence/qualification |
|---|---|---|
| 32A | PM tự gom đợt đo; hệ thống nhắc rà soát hằng tuần; không tự tạo/giao task hoặc cấp quyền sửa. | Cập nhật BR-10 và reminder/batch. |
| 33A | Matching trong cùng dự án, ưu tiên segment giao và lân cận; dùng vị trí/sai số/lịch sử; thiếu GPS dùng scope+ảnh; PM tìm mở rộng trong dự án. | Candidate selection có version, quyền, không auto merge. |
| 34A | PM duyệt/từ chối nhãn trong dự án; chỉ nhãn đã duyệt được xuất cho training; AI không tự duyệt. | Bổ sung FR-36, US-10; P2-036/056 không còn chờ chọn role. |
| 35A | PM giao xử lý an toàn tạm trong phạm vi/phương án cho phép; thông báo Supervisor; không đóng lỗi/thay nghiệm thu. | P1-062 và flow, checklist, audit. |
| 36A | Web cookie bảo mật + session server; Android access/refresh token. | APPROVED_DESIGN; ghi compatibility delta và CSRF, không âm thầm phá client hiện có. |
| 37 | Web idle 30 phút/max 12 giờ; Android access 15 phút/refresh luân chuyển max 30 ngày từ login; OTP 10 phút/5 lần thử, resend >= 60 giây và <= 3 lần/15 phút. | APPROVED_PILOT_CONFIG; token hết hạn không xóa offline; kiểm hiện quyền khi reconnect. |
| 38 | Ảnh 20 MiB/video 8 GiB/SRT 10 MiB; dataset 32 GiB; upload chia phần/resume. | APPROVED_PILOT_CONFIG; không giới hạn tổng số video cả đời segment; báo rõ nếu file vượt. |
| 39A | Segment gợi ý 100 m, PM đổi được và giữ/gộp đoạn dư. | APPROVED_PILOT_CONFIG; không là tiêu chuẩn công trình/độ phủ video. |
| 40 | 50 user đồng thời; metadata server p95 <= 2 giây; RPO <= 15 phút; RTO <= 4 giờ. | APPROVED_TARGET; chưa benchmark/restore thì chưa VERIFIED. |
| 41A | Bằng chứng trong phạm vi lưu hết bảo hành + 5 năm; video nguồn đi cùng hồ sơ; log kỹ thuật 90 ngày, export tạm 30 ngày, backup 35 ngày; hold chặn xóa. | APPROVED_PROJECT_POLICY; không là tuyên bố luật chung; audit nghiệp vụ theo hồ sơ, không dọn như log thường. Khi thiếu căn cứ ngày hết bảo hành/nhiều nghĩa vụ: WAITING_RETENTION_BASIS, không suy ngày xóa từ ngày upload; chỉ xóa khi mọi nghĩa vụ hợp lệ đều cho phép và không hold. |
| 42A | Export bàn giao mã hóa khi còn truy cập thiết bị; mất thiết bị/khóa trước sync có thể không cứu được. | APPROVED_PILOT_SCOPE; chưa xây khóa khôi phục tổ chức. Supervisor cho phép, PM đúng dự án nhận, giữ actor gốc. |
| 43A | Offline có route/segment/destination/task đã tải; nền map offline chưa bắt buộc, thêm sau khi chọn provider/license. | Thiếu nền không làm mất tác nghiệp theo geometry. |
| 44 | Web, Android và AI giao bên khác. | APPROVED_OWNERSHIP; không gán xây FE/AI cho P1/P2. BE owner chịu adapter, contract, fixtures và tích hợp; đầu mối/ETA bên nhận chưa cung cấp. |

No recall@5, AI timeout, orphan window, or response-cache duration is accepted by this table.
