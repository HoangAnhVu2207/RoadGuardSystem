# Trình và xác nhận hồ sơ tuyến

PM dùng các API draft hiện có để tạo, sửa, xem preview và readiness. Khi draft đã sẵn sàng, PM trình một lần cho dự án. Supervisor tìm hồ sơ bằng project ID và dùng GET draft hiện có để xem hình học.

| Method và path dưới `/api/v1/projects/{projectId}` | Quyền | Request và kết quả |
| --- | --- | --- |
| `POST /road-geometry-drafts/{draftId}/submit` | PM có quyền hiện tại trong dự án | Body rỗng. Gửi `If-Match` từ GET/PUT draft và `Idempotency-Key`. Trả `201` cùng hồ sơ, `ETag` mới và `Location` trỏ tới lookup theo dự án. |
| `GET /road-geometry-submission` | Supervisor hoặc PM có quyền trong dự án | Trả `200` với `SUBMITTED` hoặc `CONFIRMED` và ETag hiện tại của draft. `404 road_geometry_submission_not_found` nếu chưa trình. |
| `GET /road-geometry-drafts/{draftId}` | Supervisor hoặc PM có quyền trong dự án | Dùng `draftId` lấy từ lookup. Response hiện có chứa `status`, `editedCoordinates` và metadata geometry. |
| `POST /road-geometry-drafts/{draftId}/confirm` | Supervisor | Gửi `If-Match` mới từ lookup hoặc GET draft và `Idempotency-Key`. Giữ JSON confirm hiện hành. `201` trả hình học và `routeVersionId` đã xác nhận. Alias confirm theo road section dùng cùng quy tắc. |

Web dùng cookie với `credentials: "include"`. Request ghi gửi `X-CSRF-TOKEN` lấy từ `GET /api/v1/auth/web/csrf` trong cùng phiên. GET không cần CSRF. Bearer vẫn được hỗ trợ. Swagger runtime ở `/swagger/v1/swagger.json`.

Ví dụ response sau submit, khi draft chưa gắn road section. ETag mẫu là `"AAAAAAAAAAI="`.

```json
{
  "projectId": "11111111-1111-4111-8111-111111111111",
  "draftId": "22222222-2222-4222-8222-222222222222",
  "roadSectionId": null,
  "status": "SUBMITTED",
  "version": "AAAAAAAAAAI=",
  "submittedContentVersion": "AAAAAAAAAAE=",
  "submittedBy": "33333333-3333-4333-8333-333333333333",
  "submittedAt": "2026-11-10T04:00:00Z",
  "confirmedRouteVersionId": null
}
```

`submittedContentVersion` ghi ETag của nội dung được trình, bỏ dấu ngoặc kép. Confirm dùng ETag **hiện tại** ở header của GET, hoặc thêm dấu ngoặc kép quanh `version`.

```json
{
  "expectedCurrentVersionId": null,
  "effectiveFrom": "2026-11-10T04:10:00Z",
  "reason": "Đã kiểm tra hồ sơ tuyến"
}
```

Nếu tuyến đã có phiên bản hiện hành, gửi ID của phiên bản đó vào `expectedCurrentVersionId`. Sau confirm, lookup trả `status: "CONFIRMED"` cùng `roadSectionId`, `confirmedRouteVersionId` và ETag mới. FE đọc lại lookup sau khi nhận confirm hoặc khi mở lại màn.


Ví dụ lookup sau confirm. Trường `confirmedRouteVersionId` khớp `routeVersionId` trong response confirm.

```json
{
  "projectId": "11111111-1111-4111-8111-111111111111",
  "draftId": "22222222-2222-4222-8222-222222222222",
  "roadSectionId": "44444444-4444-4444-8444-444444444444",
  "status": "CONFIRMED",
  "version": "AAAAAAAAAAM=",
  "submittedContentVersion": "AAAAAAAAAAE=",
  "submittedBy": "33333333-3333-4333-8333-333333333333",
  "submittedAt": "2026-11-10T04:00:00Z",
  "confirmedRouteVersionId": "55555555-5555-4555-8555-555555555555"
}
```

- `401 unauthorized` nếu phiên hết hiệu lực. `403 access_forbidden` nếu sai role hoặc dự án. `403 csrf_failed` nếu thiếu hoặc sai CSRF khi ghi bằng Web.
- `404 road_geometry_draft_not_found` nếu draft không thuộc dự án hoặc alias không khớp.
- `409 road_geometry_submission_exists` nếu dự án đã trình một hồ sơ. `409 road_geometry_submission_locked` nếu tạo hoặc sửa draft sau trình, hoặc ghi trực tiếp tuyến đã khóa. `409 road_geometry_submission_required` nếu Sup xác nhận draft chưa trình. `409 road_geometry_invalid_state` nếu trạng thái draft không cho phép thao tác. `409 project_closed` nếu dự án đã đóng.
- `409 duplicate_request` nếu dùng cùng key với request khác. `409 road_section_concurrency_conflict` nếu phiên bản tuyến hiện hành khác `expectedCurrentVersionId`.
- `412 concurrency_conflict` nếu ETag cũ hoặc sai định dạng. `428 validation_error` nếu thiếu precondition. `422` nếu readiness, CRS, geometry, topology hoặc dữ liệu confirm không hợp lệ. Mã thường gặp gồm `geometry_metadata_incomplete`, `unsupported_crs`, `geometry_validation_failed`, `route_topology_cycle` và `branch_junction_mismatch`.

Khi timeout lúc trình, gửi lại cùng draft ID, ETag trước trình và key. Server kiểm lại quyền hiện tại rồi trả receipt gốc nếu thao tác đã thành công. Receipt giữ trạng thái và ETag tại thời điểm trình. GET lookup cho biết trạng thái mới nhất.

Migration `20261110043224_RoadGeometrySubmission` thêm ba cột nullable, filtered unique index theo project và check constraint. Áp dụng bằng quy trình migration của BE trước khi đưa API mới vào hoạt động. Trong môi trường đã cấu hình `ROADGUARD_MIGRATION_CONNECTION_STRING` trỏ đúng DB và đã sao lưu, chạy từ root repository:

```powershell
dotnet ef database update 20261110043224_RoadGeometrySubmission --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
```

Draft cũ giữ `DRAFT`, vẫn có thể trình nếu dự án chưa có hồ sơ. Bản `CONFIRMED` cũ giữ nguyên geometry và lịch sử, không chiếm suất trình và không xuất hiện trong lookup mới. Migration từ chối downgrade khi đã có hồ sơ trình để tránh mất dấu vết.
