# Tạo project bằng phiên Web

Supervisor tạo phiên upload hồ sơ bàn giao bằng `POST /api/v1/projects/handover-upload-intents`, upload PDF và chờ trạng thái `VERIFIED`. Gửi `fileId` trong `handoverFileIds` và ETag của phiên upload trong `If-Match` khi gọi `POST /api/v1/projects`. Dùng key riêng cho từng bước. Thao tác tạo project mới thiếu file hoặc ETag trả `428 handover_required`.

Xem [protocol, JSON mẫu, quyền, mã lỗi và migration](PM-REPAIR-READBACK-AND-WEB.md#bàn-giao-khi-tạo-project-mới). Project hiện hữu dùng [API bổ sung hồ sơ bàn giao](PROJECT-HANDOVER-ROAD-COVERAGE.md).
