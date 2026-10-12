# 03 System Actors and Responsibilities

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Supervisor ACTIVE thực hiện các thao tác Supervisor trên mọi project, không cần membership. Vẫn kiểm current account/session, đúng resource/project, state/evidence/version/idempotency. Không thay PM/Crew/Operator. Notification vẫn recipient-scoped; quyền global không broadcast hay đọc inbox người khác.

PM cấu hình/publish policy, xác nhận nguồn/Defect, gom đợt đo, giao công việc, kiểm tra FT/normal đúng nhánh và phạm vi hiện hành. PM vẫn chịu effective project membership; primary-PM là head cụ thể dùng cho duty/transfer, không đồng nhất mọi membership với primary.

Crew nhận assignment, xác nhận bắt đầu đi đo, lưu/nộp measurements và evidence, chỉ sửa khi đủ task/policy/coverage/location/window. MEASURE_ONLY không cấp quyền sửa. Nộp repaired là claim chờ review, chưa accepted.

Operator thực hiện assignment survey, video/SRT/dataset và source provenance; không làm FIELD/Crew command. Reporter đăng ký OTP, quản lý report/supplement/evidence của mình và đọc publication dành cho mình. Không đọc internal project content chỉ vì có report ID.

AI callback principal/worker được kiểm riêng; không là PM/Crew. Services/workers giữ source/recipient/occurrence identities và không tạo business authority từ technical delivery ACK. Intended recipient đọc/xác nhận đúng occurrence; substitute phải là actor hợp lệ, không account giả.

Owner quyết định business/public compatibility/data policy; Backend giữ adapter/proofs/contract; Web/Android/FastAPI do bên ngoài thực hiện theo PR44. Người nhận bên ngoài/ETA không được tự gán khi không có nguồn.
