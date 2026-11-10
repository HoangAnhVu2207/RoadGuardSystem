# Xét duyệt hồ sơ tuyến

Mỗi dự án có một hồ sơ xét duyệt với cùng `draftId` qua các lượt trình. PM sửa nháp bằng API hiện có. Supervisor tìm hồ sơ bằng project ID và dùng GET draft để xem hình học.

| Method và path dưới `/api/v1/projects/{projectId}` | Quyền | Request và kết quả |
| --- | --- | --- |
| `POST /road-geometry-drafts/{draftId}/submit` | PM có quyền trong dự án | Body rỗng. Gửi `If-Match` từ GET/PUT draft và `Idempotency-Key`. Trả `201`, `ETag` mới và `Location` trỏ tới lookup theo dự án. |
| `GET /road-geometry-submission` | Supervisor hoặc PM có quyền trong dự án | Trả `200` với `SUBMITTED`, `REJECTED` hoặc `CONFIRMED` và ETag hiện tại của draft. `404 road_geometry_submission_not_found` nếu chưa trình. |
| `GET /road-geometry-drafts/{draftId}` | Supervisor hoặc PM có quyền trong dự án | Dùng `draftId` từ lookup. Response giữ `status`, `editedCoordinates` và metadata hình học. |
| `PUT /road-geometry-drafts/{draftId}` | PM có quyền trong dự án | Khi `REJECTED`, sửa và lưu cùng `draftId` bằng ETag hiện tại và key mới. Lưu trả `200` và vẫn giữ `REJECTED`. |
| `POST /road-geometry-drafts/{draftId}/reject` | Supervisor | Gửi ETag của lượt đang `SUBMITTED`, key mới và lý do. Trả `200` cùng hồ sơ `REJECTED` và ETag mới. |
| `POST /road-geometry-drafts/{draftId}/confirm` | Supervisor | Gửi ETag của lượt đang `SUBMITTED`, key mới và JSON confirm hiện có. `201` trả hình học và `routeVersionId`. Alias confirm theo road section dùng cùng quy tắc. |

Web dùng cookie với `credentials: "include"`. Request ghi gửi `X-CSRF-TOKEN` lấy từ `GET /api/v1/auth/web/csrf` trong cùng phiên. GET không cần CSRF. Bearer vẫn được hỗ trợ. Swagger runtime ở `/swagger/v1/swagger.json`.

Body reject mẫu:

```json
{ "reason": "Cập nhật tọa độ điểm nối với tuyến cha." }
```

Sau reject, dùng `currentReview.rejectionReason` khi trạng thái hồ sơ là `REJECTED`. PM lưu cùng mã tuyến và sửa nội dung tuyến trước khi trình lại. Chỉ đổi tên, lý do sửa, metadata kiểm toán, hoặc sửa rồi lưu về nội dung lượt trước sẽ bị chặn tại submit.

Ví dụ lookup sau khi PM trình lại. FE đọc `currentReview` để hiển thị lượt đang chờ và có thể hiển thị `previousRejection` ở phần quyết định trước.

```json
{
  "projectId": "11111111-1111-4111-8111-111111111111",
  "draftId": "22222222-2222-4222-8222-222222222222",
  "roadSectionId": null,
  "status": "SUBMITTED",
  "version": "AAAAAAAAAAU=",
  "submittedContentVersion": "AAAAAAAAAAQ=",
  "submittedBy": "33333333-3333-4333-8333-333333333333",
  "submittedAt": "2026-11-10T05:00:00Z",
  "confirmedRouteVersionId": null,
  "currentReview": {
    "number": 2,
    "contentVersion": "AAAAAAAAAAQ=",
    "outcome": "SUBMITTED",
    "submittedBy": "33333333-3333-4333-8333-333333333333",
    "submittedAt": "2026-11-10T05:00:00Z",
    "decidedBy": null,
    "decidedAt": null,
    "rejectionReason": null
  },
  "previousRejection": {
    "number": 1,
    "contentVersion": "AAAAAAAAAAE=",
    "outcome": "REJECTED",
    "submittedBy": "33333333-3333-4333-8333-333333333333",
    "submittedAt": "2026-11-10T04:00:00Z",
    "decidedBy": "44444444-4444-4444-8444-444444444444",
    "decidedAt": "2026-11-10T04:10:00Z",
    "rejectionReason": "Cập nhật tọa độ điểm nối với tuyến cha."
  }
}
```

Confirm dùng ETag **hiện tại** ở header, hoặc thêm ngoặc kép quanh `version`. Nếu tuyến đã có phiên bản hiện hành, gửi ID của phiên bản đó vào `expectedCurrentVersionId`.

```json
{
  "expectedCurrentVersionId": null,
  "effectiveFrom": "2026-11-10T05:10:00Z",
  "reason": "Đã kiểm tra hồ sơ tuyến."
}
```

Sau confirm, lookup trả `CONFIRMED` với `roadSectionId`, `confirmedRouteVersionId`, quyết định của lượt hiện tại và ETag mới. Mỗi lượt lưu snapshot và quyết định riêng trong DB. Lookup trả lượt hiện tại và lần từ chối gần nhất trước lượt đó.

- `401 unauthorized` nếu phiên hết hiệu lực. `403 access_forbidden` nếu sai role hoặc dự án. `403 csrf_failed` nếu ghi bằng Web thiếu CSRF hợp lệ.
- `404 road_geometry_draft_not_found` nếu draft không thuộc dự án hoặc alias không khớp.
- `409 road_geometry_submission_unchanged` nếu nội dung tuyến trùng lượt vừa bị từ chối. `409 road_geometry_submission_exists` nếu dự án đã có hồ sơ khác hoặc đã trình xong thao tác tương ứng. `409 road_geometry_submission_locked` khi ghi vào hồ sơ hoặc tuyến đã khóa.
- `409 road_geometry_submission_required`, `road_geometry_invalid_state` hoặc `project_closed` nếu trạng thái chưa cho phép thao tác. `409 road_section_concurrency_conflict` nếu phiên bản hiện hành khác `expectedCurrentVersionId`.
- `409 duplicate_request` nếu dùng cùng key cho request khác. `412 concurrency_conflict` nếu ETag cũ hoặc sai định dạng. `428 validation_error` nếu thiếu precondition.
- `422 road_geometry_rejection_reason_required` nếu lý do trống, chỉ có khoảng trắng hoặc dài quá 2000 ký tự. Các lỗi `422` geometry, CRS và topology vẫn theo contract cũ.

Khi timeout, gửi lại cùng route, body, ETag và key. Server kiểm lại quyền rồi trả receipt gốc dù hồ sơ đã sang lượt khác. Đọc GET lookup để đồng bộ trạng thái mới nhất.

## Migration

Migration `20261110122600_GeometrySubmissionReviewRounds` thêm bảng lịch sử và cho phép `REJECTED`. Bản `SUBMITTED` và `CONFIRMED` đã từng trình được tạo lượt 1 từ dữ liệu có sẵn. Người và thời điểm xác nhận lấy từ metadata đã lưu nếu có. Các trường chưa từng lưu để null. Draft chưa trình và bản xác nhận cũ chưa từng có hồ sơ trình giữ nguyên. Migration từ chối downgrade khi có lịch sử từ chối hoặc trình lại.

Sau khi sao lưu và cấu hình `ROADGUARD_MIGRATION_CONNECTION_STRING` đúng DB đích, áp migration trước khi cập nhật API:

```powershell
dotnet ef database update 20261110122600_GeometrySubmissionReviewRounds --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
```
