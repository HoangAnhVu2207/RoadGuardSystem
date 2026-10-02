# <TASK-ID>-<A|B>-CN-001 — <Vấn đề>

- Status: Open | Discussing | Assigned | Implemented | Verified | Closed | Deferred
- Người phát hiện / ngày / branch / base và HEAD:
- Task của mình / task người kia / owner hiện tại:
- File/symbol/contract/version và dữ liệu liên quan:
- Bằng chứng: expected từ nguồn nào, actual, cách tái hiện; chưa tái hiện thì ghi giả thuyết.
- Tác động: quyền, dữ liệu, wire, retry/concurrency, migration hoặc file conflict.
- Checkpoint bị chặn; phần vẫn làm độc lập được:

## Phương án để hai người thảo luận
| Phương án | Ai sửa chính | Phía còn lại sửa gì | Tương thích/rủi ro | Test và thứ tự tích hợp |
|---|---|---|---|---|
| Sửa producer/invariant gốc | Đề xuất, chưa phân công | ... | ... | ... |
| Sửa consumer/adapter nếu lỗi cục bộ | Đề xuất, chưa phân công | ... | ... | ... |

- Khuyến nghị và căn cứ; bỏ phương án không áp dụng, không ép có hai cách sửa.
- Trong lúc chờ: không sửa file của người kia; không nhân bản logic để né dependency.

## Quyết định của hai người
- Người sửa chính được thống nhất / reviewer / ngày / nguồn trao đổi:
- Scope/allowlist được điều chỉnh; reservation/baseline chuyển giao:
- Owner từng follow-up; thứ tự tích hợp; thời điểm xử lý do nhóm thống nhất:
- Nếu chưa thống nhất: để trống, không tự ghi Approved/Assigned.

## Hoàn tất
- Commit/patch/PR thực tế (nếu có), report và test của cả producer/consumer:
- Integration evidence và phần chưa xác minh:
- Người xác nhận đóng / ngày / lý do Deferred nếu có:
