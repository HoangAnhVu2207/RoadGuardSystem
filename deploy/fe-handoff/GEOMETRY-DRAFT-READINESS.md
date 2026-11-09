# Đọc lại geometry draft và readiness

Hai GET dưới đây dùng Bearer hoặc Web session theo quyền PM/Supervisor của project. Nếu dùng `fetch` với Web session, gửi `credentials: "include"`. GET không cần CSRF. POST preview và PUT edit hoặc POST confirm bằng Web cookie dùng cặp CSRF hiện hành với header `X-CSRF-TOKEN`.

## Kiểm readiness

Gọi `GET /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/readiness`. Với draft `COORDINATES` hợp lệ, response `200` có dạng:

```json
{
  "ready": true,
  "missingFields": [],
  "errors": [],
  "crsStatus": "PROJECT_UTM",
  "sampleOnly": false,
  "warnings": ["WGS84_TRANSFORM_NOT_CONFIGURED"]
}
```

`COORDINATES` dùng cùng kiểm tra với preview và confirm. CRS phải khớp UTM đã cấu hình cho project. Tọa độ, lý trình, lý do sửa, `widthProfile` và `surveyWidthMeters` phải hợp lệ. Với draft đã lưu mà kiểm tra lại thất bại, BE trả `422` với `unsupported_crs` hoặc `geometry_validation_failed`. Nhánh `NATIVE_ALIGNMENT` vẫn kiểm profile được pin, calibration và tolerance theo luồng hiện tại, trả `missingFields` và `errors` khi draft native chưa sẵn sàng.

Preview là `POST /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/preview`. Supervisor xác nhận riêng qua `POST /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/confirm`, với `If-Match` là ETag draft hiện tại và `Idempotency-Key`. Body confirm gồm `expectedCurrentVersionId`, `effectiveFrom` và `reason`. `ready: true` xác nhận dữ liệu đã qua kiểm tra readiness. Confirm vẫn kiểm quyền, version và điều kiện tạo phiên bản tuyến.

## Khôi phục form sau GET draft

Gọi `GET /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}`. Route theo road section là `GET /api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}` và trả cùng schema. Lấy `draftId` từ `id` khi lưu draft.

Trích response mẫu cho BRANCH, với ID minh họa:

```json
{
  "id": "73912960-24e2-4a2a-a40e-43e23515abac",
  "sourceKind": "NATIVE_ALIGNMENT",
  "sourceCrs": 0,
  "routeSystemId": "36c3cc8a-aed8-4d25-93fc-68c2c704ff83",
  "routeKind": "BRANCH",
  "parentRouteVersionId": "cf40a166-bbe6-48ba-b6fc-aab608c6ed2c",
  "junctionOffsetMeters": 0,
  "declaredLengthMeters": 50,
  "chainageCalibration": {
    "sourceReference": "sample-only controls",
    "sourceChecksum": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
    "selectedReason": "PM fixture",
    "controls": [
      { "geometricOffsetMeters": 0, "stationMeters": 500 },
      { "geometricOffsetMeters": 50, "stationMeters": 600 }
    ]
  },
  "tessellationToleranceMeters": 0.1,
  "nativeAlignment": {
    "crsProfileRevisionId": "9c83a47e-6c7f-4d50-8b03-595dd069a1e6",
    "spatialSrid": 0,
    "primitives": [
      { "kind": "LINE", "start": { "x": 100, "y": 0 }, "end": { "x": 150, "y": 0 } }
    ]
  }
}
```

BE đọc các giá trị này từ draft đã lưu. FE lấy reference CRS profile từ `nativeAlignment.crsProfileRevisionId`. Các trường metadata mới được bỏ qua trong JSON khi chưa có giá trị, ví dụ tuyến MAIN không có parent hoặc junction. Giá trị `junctionOffsetMeters: 0` vẫn được trả nếu đã lưu. `nativeAlignment` của draft tọa độ giữ dạng `null` như trước.

Khi sửa, gửi lại metadata cần giữ trong body PUT cùng `changeReason` mới, `If-Match` lấy nguyên ETag có dấu ngoặc kép và `Idempotency-Key`. Nếu nhận `412 concurrency_conflict`, tải lại GET draft để lấy dữ liệu và ETag mới. Swagger hiện hành ở `/swagger/v1/swagger.json` của backend đang chạy.
