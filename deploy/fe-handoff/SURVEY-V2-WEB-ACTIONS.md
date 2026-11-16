# Survey V2 trên Web PM và Supervisor

Dùng HTTPS và `credentials: "include"` cho Web session. Lấy `requestToken` từ `GET /api/v1/auth/web/csrf` cùng phiên sau đăng nhập và gửi vào `X-CSRF-TOKEN` cho POST. Browser tự gửi session cookie và antiforgery cookie. GET không cần CSRF hay header ghi. Bearer tiếp tục dùng `Authorization: Bearer …` và không cần CSRF.

## Đọc dữ liệu

Các path dưới đây bắt đầu bằng `/api/v1`. PM đọc trong phạm vi project được cấp quyền. Supervisor dùng quyền đọc hiện hành. Operator giữ quyền theo nguồn assignment của từng action.

| Method/path | Response 200, trích trường | Mã lỗi |
| --- | --- | --- |
| GET `/datasets/{datasetId}/coverage` | `{"datasetId":"<datasetId>","methodVersion":"pm-evidence-review.v1"}`. Dùng `items` cho coverage theo đoạn và band. `assessmentId` và `version` có thể null. | 401 `auth_unauthorized`, 403 `access_forbidden`, 404 `not_found`, 422 `survey_validation_failed`. |
| GET `/datasets/{id}` | `{"id":"<datasetId>","surveyTaskId":"<taskId>","dataVersionId":"<datasetId>","sourceFiles":[],"pairs":[],"version":"<version>"}`. Response kèm ETag. | 401 `auth_unauthorized`, 403 `access_forbidden`, 404 `not_found`. |
| GET `/survey-tasks/{id}/work-package` | `{"schemaVersion":"anh01.survey-work.v1","task":{"id":"<taskId>","version":"<version>"},"geometryRefs":[],"scopeVersion":"<version>"}`. Có thể có `accessPoint`. Response kèm ETag. | 401 `auth_unauthorized`, 403 `access_forbidden`, 404 `not_found`. |

## Ghi thay đổi

Bốn POST dành cho **PM có quyền trong project**. Gửi `Content-Type: application/json`, `Idempotency-Key` và `If-Match` lấy từ ETag của task/plan tương ứng, giữ dấu ngoặc kép. Retry sau timeout bằng đúng key, body và If-Match đã gửi. Khi bắt đầu thao tác mới, dùng key mới và tải lại version hiện hành.

| Method/path | Body mẫu | Response, trích trường |
| --- | --- | --- |
| POST `/survey-tasks/{taskId}/cancel` | `{"reason":"Không thể triển khai tại hiện trường"}` | 200, `{"id":"<taskId>","status":"CANCELLED","version":"<version>"}` |
| POST `/survey-tasks/{taskId}/reassign` | `{"operatorId":"<operatorId>","reason":"Đổi người phụ trách"}`. `dueAt` tùy chọn. | 200, `{"id":"<taskId>","operatorId":"<operatorId>","status":"REASSIGNED","version":"<version>"}` |
| POST `/survey-tasks/{taskId}/supplements` | `{"operatorId":"<operatorId>","reason":"Thiếu dải mép phải","scope":[{"routeVersionId":"<routeVersionId>","segmentSetId":"<segmentSetId>","segmentIds":["<segmentId>"],"targetBand":"RIGHT_EDGE"}]}` | 201, `{"id":"<taskId>","status":"SUPPLEMENT_REQUIRED","supplementTaskId":"<childTaskId>","version":"<version>"}`. `Location` trỏ task con. |
| POST `/survey-plans/{planId}/postpone` | `{"reason":"Mưa lớn"}` | 200, `{"id":"<planId>","status":"POSTPONED","version":"<version>"}`. V2 dùng `reason`. |

Cả bốn trả ETag qua response header và version qua JSON body. Sau supplement, dùng `Location` hoặc `supplementTaskId` để mở chính lượt vừa tạo. Khi đọc lại task gốc bằng API-226, `supplementTaskId` là liên kết mở nhanh và có thể là null.

- 401 `auth_unauthorized` khi thiếu phiên hợp lệ. 403 `csrf_failed` khi cookie POST thiếu hoặc sai CSRF. 403 `access_forbidden` khi sai role hoặc scope.
- 428 `validation_error` khi thiếu header. 412 `auth_concurrency_conflict` khi version cũ. 409 `duplicate_request` khi dùng lại key với request khác.
- Cancel, reassign và supplements có thể trả 404 `survey_request_not_found`, 409 `survey_invalid_state_transition` hoặc `survey_scope_incompatible`, 422 `survey_validation_failed`. Reassign và supplements có thể trả 404 `identity_user_not_found` cho Operator không hợp lệ.
- Postpone có thể trả 404 `survey_plan_not_found`, 409 `survey_scope_incompatible` hoặc 422 `survey_validation_failed`. JSON không hợp lệ có thể bị trả 400 ở tầng API.

## Chọn bộ dữ liệu trên /pm/surveys

Gọi `GET /api/v1/survey-tasks/{taskId}/datasets?limit=25` bằng Web cookie hoặc Bearer. Lấy `taskId` từ `id` của task trên API-226 hoặc danh sách task. Backend xác định project từ task đã lưu. PM cần quyền trên project đó, Supervisor theo quyền đọc dự án hiện hành. GET không cần CSRF, If-Match hay Idempotency-Key.

```json
{
  "items": [
    {
      "id": "bb92e0c9-f112-490d-97ef-83a0e3d581b2",
      "surveyTaskId": "06c58571-b48d-4eb7-9bcf-a08a3082ff2c",
      "versionNo": 1,
      "submittedAt": "2026-11-16T08:30:00+00:00",
      "integrityStatus": "PASSED",
      "version": "AAAAAAAAAAE=",
      "assessmentId": null
    }
  ],
  "nextCursor": null
}
```

- Mặc định chọn item đầu. Lấy `items[].id` gọi `GET /api/v1/datasets/{id}` và `GET /api/v1/datasets/{id}/coverage`. Hai API này vẫn kiểm quyền khi mở bản đã chọn và trả 404 nếu task không còn assignment hiện hành, kể cả khi danh sách còn bản đã lưu. `version` là Base64 rowversion của dataset, `versionNo` là số thứ tự bản trong survey.
- Các bản có `submittedAt` mới hơn đứng trước. Khi trùng thời gian, BE dùng ID giảm dần theo thứ tự SQL Server. Bản cũ thiếu thời gian có `submittedAt: null` và đứng cuối. `limit` mặc định 25, từ 1 đến 100. Gửi lại `nextCursor` nguyên vẹn qua query `cursor` (URL-encode), bằng cùng tài khoản và task. Khi `nextCursor: null`, đã hết trang. Task hợp lệ chưa có dữ liệu trả `200 {"items":[],"nextCursor":null}`.
- `assessmentId` lấy theo cùng thứ tự với coverage, `ReviewedAt` giảm dần rồi ID giảm dần. Trả `null` khi chưa có assessment. Coverage trả các kết quả đánh giá theo đoạn và band tại thời điểm đọc.
- 400 `validation_error` khi limit/cursor sai. 401 `auth_unauthorized` khi thiếu phiên hợp lệ. 403 `access_forbidden` khi sai role hoặc project. 404 `survey_request_not_found` khi task ID hợp lệ về định dạng UUID nhưng không tồn tại.
- Với luồng nộp V2, lần ghi mới yêu cầu task ở trạng thái `Accepted` hoặc `InProgress` và chưa có task bổ sung. Nộp thành công đưa task sang `Submitted`. Retry cùng request trả lại bản trước. Bản nộp bổ sung đi theo task con riêng. Danh sách đọc được nhiều bản đã lưu cùng task nếu dữ liệu lịch sử có các bản đó. Chọn bản mặc định trên UI không đổi baseline.

## Đánh giá dataset và khóa baseline trên /pm/surveys

Cả hai POST dành cho **PM có quyền trên project**. Khi dùng phiên Web, gửi `credentials: "include"`, `Content-Type: application/json`, `X-CSRF-TOKEN` lấy từ `requestToken` của `GET /api/v1/auth/web/csrf` và `Idempotency-Key` cho thao tác. Với Bearer, gửi `Authorization: Bearer …` cùng các header nghiệp vụ của từng POST. Khi retry, dùng cùng key và request ban đầu, gồm `If-Match` của assessment.

### Tạo assessment

`POST /api/v1/datasets/{id}/assessments` dùng `id` của dataset đã chọn. Đọc `GET /api/v1/datasets/{id}` và gửi nguyên ETag của dataset vào `If-Match`, gồm dấu ngoặc kép. Lấy tuyến, bộ đoạn, đoạn và dải từ `scope`, lấy `fileId` từ `sourceFiles` của chính dataset. Điểm `PASS` ở bất kỳ tiêu chí nào cần dẫn chứng file đã xác minh.

```json
{
  "methodVersion": "pm-evidence-review.v1",
  "items": [{
    "routeVersionId": "<routeVersionId>",
    "segmentSetId": "<segmentSetId>",
    "segmentId": "<segmentId>",
    "targetBand": "SURFACE",
    "positionStatus": "PASS",
    "qualityStatus": "PASS",
    "coverageStatus": "PASS",
    "reason": "Đã kiểm tra video tại đoạn khảo sát",
    "evidence": [{ "fileId": "<fileId>", "fromMilliseconds": 0, "toMilliseconds": 1000 }]
  }]
}
```

`201 Created` hoặc replay hợp lệ trả assessment đã ghi. Ví dụ rút gọn:

```json
{
  "id": "<assessmentId>",
  "datasetId": "<datasetId>",
  "methodVersion": "pm-evidence-review.v1",
  "version": "AAAAAAAAAAI="
}
```

Response còn có `reviewedBy`, `reviewedAt`, `items` và header ETag của assessment. Lấy `id` để gửi vào `assessmentId` khi khóa baseline. Web có thể tải lại `/api/v1/datasets/{id}/coverage` để xem kết quả theo đoạn/dải. URL đọc assessment trong `Location` hiện dùng Bearer.

### Khóa hoặc thay baseline

`POST /api/v1/projects/{projectId}/baseline-selections` dùng `Idempotency-Key` và không cần `If-Match`. Mỗi item trỏ đúng dataset và assessment của dataset đó. Vị trí, chất lượng và độ phủ phải cùng `PASS` trên chính đoạn/dải được chọn.

```json
{
  "items": [{
    "routeVersionId": "<routeVersionId>",
    "segmentSetId": "<segmentSetId>",
    "segmentId": "<segmentId>",
    "targetBand": "SURFACE",
    "datasetId": "<datasetId>",
    "assessmentId": "<assessmentId>",
    "expectedBaselineSelectionId": null
  }],
  "reason": "Chốt bộ dữ liệu cho dải mặt đường"
}
```

`201 Created` hoặc replay hợp lệ trả đợt selection và từng item. Ví dụ rút gọn:

```json
{
  "id": "<baselineBatchId>",
  "items": [{
    "selectionId": "<selectionId>",
    "routeVersionId": "<routeVersionId>",
    "segmentSetId": "<segmentSetId>",
    "segmentId": "<segmentId>",
    "targetBand": "SURFACE",
    "datasetId": "<datasetId>",
    "assessmentId": "<assessmentId>"
  }],
  "version": "AAAAAAAAAAM="
}
```

- Lần khóa đầu gửi `expectedBaselineSelectionId: null` cho item chưa có baseline. Khi thay, gửi `selectionId` của item hiện hành khớp cả tuyến, bộ đoạn, đoạn và dải. `id` ngoài cùng là ID đợt selection.
- Khi mở lại màn, gọi `GET /api/v1/projects/{projectId}/baseline-selections?segmentSetId=<segmentSetId>`. PM có quyền trong project và Supervisor được đọc qua Web cookie (`credentials: "include"`) hoặc Bearer. GET không cần CSRF. Response là `{"items":[…]}` với cùng cấu trúc item như ví dụ POST. Bộ lọc `segmentSetId` chỉ lấy item trong bộ đoạn đã chọn.
- Trong `items`, tìm mục khớp cả `routeVersionId`, `segmentSetId`, `segmentId` và `targetBand`. Dùng `selectionId` của đúng mục đó làm `expectedBaselineSelectionId`. Nếu không có mục khớp, gửi `null` cho lần khóa đầu.
- Khi gặp `409 baseline_selection_conflict`, gọi lại GET và hiển thị baseline vừa tải để PM xác nhận. Sau khi PM xác nhận, gửi lệnh mới với ID hiện hành và `Idempotency-Key` mới. URL lịch sử trong `Location` vẫn dùng Bearer.

| HTTP | Mã lỗi và cách xử lý |
| --- | --- |
| 400 | `validation_error` khi JSON hoặc header sai quy cách. |
| 401 | `auth_unauthorized` khi thiếu phiên hợp lệ. |
| 403 | `csrf_failed` khi token cookie POST thiếu/sai. `access_forbidden` khi sai role hoặc PM ngoài project. |
| 404 | `not_found` khi dataset không đọc được theo điều kiện hiện hành, hoặc cặp dataset/assessment không thuộc project trong lệnh khóa. |
| 409 | `duplicate_request` khi dùng lại key với request khác. `baseline_selection_conflict` khi selection kỳ vọng đã đổi. |
| 412 | Assessment trả `concurrency_conflict` khi ETag dataset cũ hoặc sai. Tải lại dataset và dùng key mới cho thao tác mới. |
| 422 | Assessment có thể trả `assessment_method_not_approved` hoặc `assessment_validation_failed`. Khóa baseline trả `baseline_not_eligible` khi item không hợp lệ hoặc thiếu ba tiêu chí PASS. |
| 428 | `validation_error` khi thiếu `Idempotency-Key`, hoặc thiếu `If-Match` ở POST assessment. |

[OpenAPI bàn giao](openapi.json). Runtime Swagger tại `/swagger/v1/swagger.json` khi được bật.
