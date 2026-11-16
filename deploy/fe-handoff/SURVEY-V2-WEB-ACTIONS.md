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

## Nguồn datasetId

Biên nhận nộp dataset từ `POST /survey-tasks/{taskId}/datasets` trả `id` và `dataVersionId` của bản vừa nộp. Nếu FE đã có ID này, dùng nó cho cả hai GET dataset. API-226, work-package và GET plan không có danh sách dataset. Nếu màn hình cần đúng dataset đang được chọn làm baseline, `GET /api/v1/projects/{projectId}/baseline-selections?segmentSetId=…` trả `items[].datasetId` theo segment và band cho PM/Supervisor được phép qua Bearer của route đó. Đường đọc baseline chỉ bao phủ các bản đã được chọn làm baseline. Web chưa có đường đọc danh sách bản nộp theo task hoặc plan để chọn đúng phiên bản.

Đề xuất để nhóm duyệt: `GET /api/v1/survey-tasks/{taskId}/datasets?limit=20&cursor=…` theo quyền PM/Supervisor hiện hành. Trả `items[]` có `datasetId`, `versionNo`, `status`, `submittedAt`, `recordedAt` và `nextCursor`. Sắp theo version giảm dần và ID làm khóa phụ. Task có nhiều bản thì FE hiển thị từng bản cho người dùng chọn. Nếu màn plan chỉ có planId, cần thêm bộ lọc planId có kiểm scope cho danh sách task để lấy taskId trước. Đề xuất này chưa triển khai.

[OpenAPI bàn giao](openapi.json). Runtime Swagger tại `/swagger/v1/swagger.json` khi được bật.
