# Tiến độ sửa chữa của phản ánh

## Hoàng · Web PM

`POST /api/v1/cases/{caseId}/publications` (API-021) dùng cho PM còn quyền trong dự án của Case. Gọi `GET /api/v1/cases/{caseId}` để lấy ETag mới nhất và đọc chi tiết Report thuộc Case để chọn người nhận. Với Web session, dùng `credentials: "include"`. Sau đăng nhập, lấy `requestToken` từ `GET /api/v1/auth/web/csrf` cùng phiên và gửi vào `X-CSRF-TOKEN`. Caller Bearer tiếp tục dùng `Authorization: Bearer …`. Cả hai gửi `Content-Type: application/json`, `If-Match` nguyên ETag có dấu ngoặc kép và `Idempotency-Key` mới cho mỗi lần công bố có chủ đích.

```json
{
  "reportIds": ["11111111-1111-4111-8111-111111111111"],
  "defectIds": ["22222222-2222-4222-8222-222222222222"],
  "evidenceIds": [],
  "summary": "Đội sửa chữa đã bắt đầu thi công.",
  "repairProgressStatus": "IN_PROGRESS"
}
```

Dùng ID thật từ Case và dữ liệu liên kết đã xác minh. Bật nút khi có report đang thuộc Case, có đủ defect được xác minh của report và có dữ liệu repair cho các nghĩa vụ sửa chính thức bắt buộc. Nếu các report có tập defect khác nhau, gửi từng nhóm có cùng tập hoặc từng report. Giữ lựa chọn công bố summary/evidence riêng bằng cách bỏ `repairProgressStatus`. Mọi evidence gửi kèm vẫn phải được phép công bố cho từng report.

| Mốc yêu cầu | Điều kiện do BE kiểm |
| --- | --- |
| `NOT_STARTED` | Xác minh được phạm vi sửa và chưa có mốc bắt đầu thi công trên các item hiện hành. |
| `IN_PROGRESS` | Ít nhất một hạng mục trong phạm vi có sự kiện Crew bắt đầu thi công. Giao việc hoặc bắt đầu FIELD chưa đủ. |
| `ACCEPTED` | Tất cả nghĩa vụ sửa chính thức bắt buộc của các defect thuộc report đều có xác nhận cuối còn hiệu lực. NORMAL do Supervisor xác nhận cuối, Fast Track do PM xác nhận cuối. Nộp kết quả và PM review ACCEPT vẫn là bước trước xác nhận cuối. |

Thành công `201`, có ETag Case mới và `Location` về Case. Ví dụ body (ID và version minh họa):

```json
{
  "id": "33333333-3333-4333-8333-333333333333",
  "caseId": "44444444-4444-4444-8444-444444444444",
  "status": "PUBLISHED",
  "version": "AAAAAAAAAAE="
}
```

Sau thành công tải lại Case và dữ liệu repair đang hiển thị. Khi timeout, gửi lại đúng key, body và If-Match cũ. Replay trả biên nhận cũ nếu còn quyền, kể cả khi mốc đã mất hiệu lực. Dùng ETag và key mới khi thực sự công bố lại.

| HTTP | Cách xử lý |
| --- | --- |
| 400 `validation_error` | Kiểm tra JSON, mã mốc và ETag. |
| 401 / 403 | Đăng nhập lại nếu phiên hết hiệu lực. Với `csrf_failed`, lấy token mới cùng phiên. `access_forbidden` là thiếu quyền hiện hành. |
| 404 `not_found` | Tải lại tài nguyên. |
| 409 `repair_progress_scope_unverified`, `source_not_ready` | Kiểm tra liên kết Report–Case, tập defect, phạm vi nghĩa vụ và nguồn đã xác minh. |
| 409 `repair_progress_not_ready` | Tải lại Repair, mốc yêu cầu chưa khớp dữ liệu hiện hành. |
| 409 `idempotency_key_reused` | Khôi phục đúng request cũ để retry hoặc dùng key mới cho lần công bố khác. |
| 412 `concurrency_conflict` / 428 `precondition_required` | Đọc Case để lấy ETag mới hoặc gửi đủ header bắt buộc. |

## Tùng · App Reporter

Đọc `repairProgress` từ từng item trong `GET /api/v1/reports` (API-215) và từ `GET /api/v1/reports/{reportId}` (API-217). App dùng Bearer của Reporter. Hai GET không cần CSRF. Detail trả 404 nếu report thuộc người khác. Giữ phân trang `pageSize` / `cursor` / `nextCursor` hiện có.

```json
{
  "routingStatus": "ASSIGNED",
  "repairProgress": {
    "status": "IN_PROGRESS",
    "publishedAt": "2026-11-13T09:30:00Z"
  }
}
```

Đây là phần trích response. Hiển thị `NOT_STARTED` → Chưa sửa, `IN_PROGRESS` → Đang sửa, `ACCEPTED` → Đã nghiệm thu. Với `null`, ẩn nhãn hoặc hiển thị “Chưa có tiến độ công bố”. Trường hợp này gồm chưa công bố, case kết luận không có lỗi, phạm vi liên kết đã đổi, hoặc xác nhận cuối đã công bố bị correction/reopen thay thế. Khi có lại xác nhận cuối hợp lệ, PM cần công bố mới. `publicUpdates` tiếp tục giữ lịch sử, gồm bản cũ chỉ có summary/evidence. Dùng riêng `repairProgress` cho nhãn tiến độ. Tải lại list/detail khi mở màn hoặc refresh.

[OpenAPI bàn giao](openapi.json) · Swagger runtime tại `/swagger/v1/swagger.json`.

## Triển khai

Migration `20261113113000_ReporterRepairProgress` thêm hai cột nullable cho recipient của publication. Các bản công bố cũ giữ giá trị null.

```powershell
dotnet ef database update 20261113113000_ReporterRepairProgress --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
```
