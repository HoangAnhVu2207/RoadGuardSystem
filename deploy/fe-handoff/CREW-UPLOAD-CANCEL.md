# Hủy phiên upload ảnh Crew

Dùng route upload chung với Bearer. Lấy `uploadId` từ trường `id` trong response tạo phiên `POST /api/v1/uploads` hoặc `POST /api/v1/offline/uploads` của ảnh Crew. Hai trường `id` và `fileId` có mục đích khác nhau.

```http
POST /api/v1/uploads/{uploadId}/cancel
Authorization: Bearer <access-token>
```

Request không cần body, `If-Match` hay `Idempotency-Key`. Chỉ RepairCrew sở hữu phiên ảnh `BEFORE`, `AFTER` hoặc `MEASUREMENT` (JPEG/PNG) và còn quyền ghi trên task trong project mới hủy được. Với phiên offline, backend kiểm thêm admission đã ký theo guard multipart hiện có.

Dừng các request tải part đang chạy và dừng hàng đợi retry, cấp URL và complete trước khi gọi cancel. Có thể dùng `AbortController` cho các request đang chạy. Nếu cancel timeout, gọi lại cùng `uploadId` để xác định kết quả trước khi tạo phiên khác.

- `200` nếu phiên đang `PENDING` hoặc `UPLOADING`. Gọi lại khi vẫn có quyền trả cùng kết quả.
- `401` nếu thiếu Bearer hoặc token không hợp lệ.
- `403 access_forbidden` nếu sai role, sai owner, sai loại phiên hoặc đã mất quyền ghi theo project/task/admission.
- `404 upload_session_not_found` nếu Crew gửi ID phiên hợp lệ về cú pháp nhưng không tồn tại.
- `409 upload_not_cancellable` nếu phiên đã `VERIFYING`, `VERIFIED` hoặc `FAILED` vì lỗi khác. Gọi `GET /api/v1/uploads/{uploadId}` để cập nhật màn hình. Nếu complete đã thắng, tiếp tục theo dõi phiên đã nhận.

```json
{
  "id": "0f4b4aa9-1852-4e6f-8b9e-85e950681f04",
  "status": "FAILED",
  "failureCode": "upload_cancelled"
}
```

Sau `200`, dừng dùng phiên cũ. Khi còn quyền thao tác, request hợp lệ tới `part-urls` và `complete` của phiên đã hủy trả `409`, kể cả gửi lại key cũ. Worker dọn multipart bất đồng bộ và thử lại khi storage lỗi tạm thời. URL đã cấp có thể còn hiệu lực tới lúc multipart được dọn hoặc URL hết hạn.

Để tải lại online, tạo phiên mới với `Idempotency-Key` mới và dùng `id` mới cho các bước multipart. Với offline, capture origin đã ký chỉ gắn với một phiên. Muốn gửi lại qua offline, cần khai báo capture origin mới và có admission hợp lệ cho capture đó, rồi tạo phiên với key mới. Chỉ đổi key trên capture origin cũ sẽ không mở phiên mới.

Swagger hiện hành ở `/swagger/v1/swagger.json`. Luồng ảnh phản ánh tiếp tục dùng [cancel riêng của Reporter](REPORTER-UPLOAD-CANCEL.md).
