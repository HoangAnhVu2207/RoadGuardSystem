# Hủy upload ảnh phản ánh

Theo handoff W03, màn ảnh phản ánh tích hợp nhóm route của Reporter. Lấy `id` từ response tạo phiên `POST /api/v1/reporter-evidence/uploads` và giữ làm `uploadId`.

```http
POST /api/v1/reporter-evidence/uploads/{uploadId}/cancel
Authorization: Bearer <access-token>
```

Dừng các request tải part đang chạy, ví dụ bằng `AbortController`, và dừng retry phía client trước khi gọi cancel. Request cancel dùng phiên Bearer như các route ảnh phản ánh, không có body và không yêu cầu `If-Match` hay `Idempotency-Key`.

Phiên `PENDING` hoặc `UPLOADING` trả `200`. Gọi lại cùng phiên đã hủy trả cùng dữ liệu.

```json
{
  "id": "0f4b4aa9-1852-4e6f-8b9e-85e950681f04",
  "status": "FAILED",
  "failureCode": "upload_cancelled"
}
```

- `401` nếu thiếu hoặc hết hiệu lực Bearer.
- `404 upload_session_not_found` nếu phiên không tồn tại hoặc người gọi không có quyền hiện hành trên phiên.
- `409 upload_not_cancellable` nếu phiên đang `VERIFYING`, đã `VERIFIED` hoặc đã fail vì nguyên nhân khác. Đọc lại GET session để đồng bộ trạng thái UI.

Sau cancel, request hợp lệ tới `part-urls` và `complete` trả `409` kể cả khi gửi lại key cũ. Nếu muốn tải lại ảnh, tạo phiên mới bằng key mới. Cancel đóng phiên ở BE. Worker dọn multipart trên storage bất đồng bộ và thử lại khi storage lỗi tạm thời. URL đã cấp có thể còn hiệu lực cho đến khi multipart bị abort hoặc URL hết hạn.

Swagger hiện hành ở `/swagger/v1/swagger.json` của backend đang chạy. Bảng [API W03](FE-SCREEN-API-MAPPING.md) ghi các route tạo, đọc, cấp URL và complete của cùng luồng.
