# Nhân sự và phân công — hướng dẫn Postman

> **DOC-AUDIT-01 · 2026-10-12 · ACTIVE** — API/source kiểm lại tại `f0c8762`; fresh auth/directory/HTTP counts xem [audit report](../audit/DOC-AUDIT-01-REPORT.md). Account detail contact chỉ Supervisor; roster/primary không contact. Source đã commit, gói review trước là historical evidence; chưa chạy Postman thủ công hoặc xác nhận backend đang phục vụ port local.

Import lại `RoadGuard-ANH-Manual.postman_collection.json` trong Downloads/testing để thấy folder **Nhân sự và phân công**. Chín folder cũ được giữ nguyên; environment hiện có không bị sửa. Nếu chỉ cần nhóm đọc mới, import `RoadGuard-Staff-Membership-Read.postman_collection.json` (repo canonical cùng tên). Postman không tự reload file sau cập nhật; Bruno có thể import collection JSON này.

1. Chọn environment local/staging hiện có, kiểm `baseUrl`, chạy **H00 Login bearer Supervisor** (và PM nếu muốn test scope PM). Không điền body cho các GET mới.
2. Trong folder mới, request đầu tiên là **01 — Chọn dự án (Supervisor)**. Có thể điền `projectSearch` để thu hẹp bằng mã/tên. Nếu chỉ có một kết quả, script lưu `projectId`; nếu có nhiều và chưa chọn project, chọn kết quả đầu. Để chọn dự án khác, thu hẹp search và chạy lại.
3. Chạy **02 — Nhân sự nội bộ**, rồi **03 — Chọn PM ACTIVE theo tên/email**. Điền `staffSearch` để chỉ còn đúng PM muốn chọn. Script lưu `selectedStaffUserId`/`selectedPmUserId` riêng, không đổi actor hoặc target account của các folder cũ. Tìm không ra hoặc response lỗi sẽ xóa lựa chọn cũ.
4. Chạy **05 — Thành viên hiện hành** và **08 — PM chính + version cho PUT**. `currentPrimaryMembershipVersion` lấy từ đúng membership của PM chính; không phải account/project ETag. Request 08 loại PM chính khỏi tập ứng viên đã đọc nếu cần; nếu không còn ứng viên thì chạy lại 03 với PM khác. Primary null nghĩa không có assignment hiện hành hợp lệ, không tạo assignment bằng GET.
5. Request **09** xem contact của tài khoản đã chọn bằng Supervisor. Members/primary-PM không có contact. PM chỉ đọc roster/primary của project mình có membership hiện hành; request **06/14** dùng `pmToken`.

Các request Cookie nằm cuối folder: dùng web CSRF/login hiện có trên cùng **HTTPS origin**, bật cookie jar; các GET này không gửi Authorization. Collection không chứa login credentials và không tạo cookie giả. Bearer requests tắt cookie jar để không che lỗi token; no-auth cũng tắt jar.

Directory query: `search`, `role` (`SUPERVISOR`, `PM`, `OPERATOR`, `CREW`, `REPORTER`), `status` (`ACTIVE`, `SUSPENDED`, `PENDING`), `limit` (default20, 1..100), `cursor` (chỉ gửi `nextCursor` khi có). Default directory không liệt kê Reporter, không tự ẩn staff suspended/pending. Members search chỉ tên; cùng role codes, `includeHistory=false` mặc định; `status=ACTIVE|ENDED` chỉ dùng với Supervisor và `includeHistory=true`. Cursor gắn actor/scope/project/filter; đổi filter phải bỏ cursor. Không dùng cursor/asOf làm command version.

## Chỉ khi chủ động muốn đổi PM bằng PUT hiện có

Folder đọc và manual collection không thêm PUT, nên chạy cả folder không đổi PM. Import collection riêng **RoadGuard-Primary-PM-Optional-Write.postman_collection.json** khi muốn test ghi. Dùng cùng environment sau các GET trên, kiểm PM đích khác PM chính rồi chọn request PUT. Body hiện rõ các field, dùng biến version từ GET và operationId mới; không có credentials hardcode. EffectiveFrom tự lấy ngày UTC hiện tại; nghiệp vụ hiện có còn yêu cầu ngày này lớn hơn ValidFrom của PM chính. Không đổi tiếp trong cùng ngày vừa gán PM; không nới rule để test.

Sau PUT, chạy GET đọc lại trong collection tùy chọn hoặc request 10 của folder đọc. Sau 409, đọc lại primary-PM, kiểm lại đích và nghiệp vụ trước khi thử lại; không tự retry command. Revoked/suspended session cần đăng nhập lại theo quy trình hiện có.

## Ví dụ fixture đã loại contact

```json
{
  "primaryProjectManager": {
    "membershipId": "11111111-1111-1111-1111-111111111111",
    "userId": "22222222-2222-2222-2222-222222222222",
    "displayName": "Fixture PM",
    "validFrom": "2026-10-10",
    "validTo": null,
    "membershipVersion": "AAAAAAAAAAE="
  }
}
```

Đây là fixture minh họa, không phải ID/version của database đang chạy. Runtime đã kiểm bằng real-host TestServer + SQL disposable; chưa chạy Postman thủ công, chưa restart/deploy backend đang chạy. Test/build/commit/source evidence nằm trong gói review.
