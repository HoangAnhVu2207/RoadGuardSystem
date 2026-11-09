# Hủy upload video hoặc telemetry của Drone Operator

Dùng `POST /api/v1/uploads/{uploadId}/cancel` với Bearer. Lấy `uploadId` từ trường `id` trong response tạo phiên `POST /api/v1/uploads`. Áp dụng cho `SURVEY_VIDEO` và `TELEMETRY` thuộc task của Operator. Request không cần body, `If-Match` hay `Idempotency-Key`. Route `/offline/uploads` dùng cho FIELD.

1. Dừng tải part đang chạy và hàng đợi retry, cấp URL hoặc complete. Có thể dùng `AbortController` cho các request đang chạy.
2. Gọi cancel. Nếu timeout hoặc mất kết nối, gọi lại cùng `uploadId` để xác định kết quả trước khi tạo phiên khác.
3. Nếu nhận `200`, bỏ phiên cũ. Khi cần tải lại, tạo phiên bằng `Idempotency-Key` mới rồi dùng `id` và URL part của phiên mới.

```http
POST /api/v1/uploads/{uploadId}/cancel
Authorization: Bearer <access-token>
```

```json
{
  "id": "0f4b4aa9-1852-4e6f-8b9e-85e950681f04",
  "status": "FAILED",
  "failureCode": "upload_cancelled"
}
```

- `200` khi hủy phiên `PENDING` hoặc `UPLOADING`. Gọi lại cùng phiên trả cùng dữ liệu nếu Operator vẫn là chủ phiên, còn membership và assignment, và task còn cho ghi upload.
- `409 upload_not_cancellable` nếu phiên đã `VERIFYING`, `VERIFIED` hoặc fail vì lý do khác. Gọi `GET /api/v1/uploads/{uploadId}` để đồng bộ UI. Nếu complete đã được nhận, tiếp tục theo dõi phiên đó.
- `401` nếu Bearer thiếu hoặc hết hiệu lực. `403 access_forbidden` nếu sai owner, purpose hoặc quyền hiện tại. `404 upload_session_not_found` nếu ID phiên không tồn tại.

Nếu cancel thắng, `part-urls` và `complete` của phiên cũ trả `409` khi caller còn quyền, kể cả với key cũ. Worker dọn multipart bất đồng bộ và retry khi storage lỗi tạm thời. URL đã cấp có thể còn hiệu lực đến khi multipart được dọn hoặc URL hết hạn.

Swagger ở `/swagger/v1/swagger.json` trên backend đang chạy. Xem thêm [retry nộp dataset](OPERATOR-DATASET-RETRY.md).
