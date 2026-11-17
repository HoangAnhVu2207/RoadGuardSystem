# Đọc lại phương án sửa chữa, policy và các route Web PM/Supervisor

Base `a54c806d3d3e9f58b678af4cae4aced58a506945`. Dùng Swagger runtime tại `/swagger/v1/swagger.json` trên bản build có diff này. Đối chiếu với [OpenAPI xuất từ runtime](openapi.json).

## Phương án và binding

PM/Supervisor theo quyền đọc repair item hiện hành gọi `GET /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}`. Thêm `/history` để đọc timeline. History trả cùng các trường dưới `item`. Lấy ID từ danh sách package và item của project.

Ví dụ phần dữ liệu bổ sung khi đã giao việc:

```json
{
  "repairPlan": "Đo độ sâu và sửa phần hư hỏng trong phạm vi được giao",
  "checklistVersion": "pothole-v1",
  "proposalPlanHash": "<persisted proposal hash>",
  "approvedPlanHash": null,
  "currentBinding": {
    "id": "<bindingId>",
    "taskId": "<taskId>",
    "assignmentId": "<assignmentId>",
    "policyRevisionId": "<revisionId>",
    "policyContentHash": "<persisted hash>",
    "planHash": "<persisted binding plan hash>",
    "checklistVersion": "pothole-v1"
  }
}
```

Hash approved xuất hiện khi đã lưu bước duyệt phương án. ID và hash policy trong binding phản ánh revision đã pin khi giao việc. Nếu dữ liệu cũ thiếu phần phương án, hash hoặc binding, FE xử lý trường không có giá trị như null. Khi reload, GET lại chi tiết từ BE. Gửi ETag chi tiết trong `If-Match` cho lệnh giao việc. ETag của history chỉ dùng cho bản biểu diễn timeline. Guard hiện hành vẫn có thể trả `403 access_forbidden`, `404 not_found` hoặc `409 concurrency_conflict` khi ghi bằng version cũ.

## Lý do policy

Các route dưới `/api/v1/projects/{projectId}/repair-policies` dành cho PM có membership hiện hành. Đọc `GET /drafts/{draftId}` sau tạo/sửa draft. Đọc `GET /revisions/{revisionId}` bằng ID từ publish hoặc ID revision đã pin.

```json
{
  "state": "REVOKED",
  "reason": "Thay bằng quy trình mới",
  "draftChangeReason": null,
  "publishReason": "Bắt đầu áp dụng checklist cho nhóm thử nghiệm",
  "revokeReason": "Thay bằng quy trình mới"
}
```

Draft trả `draftChangeReason` của lần tạo/sửa hiện hành. Revision đọc `publishReason` từ đúng audit event publish, và `revokeReason` từ revocation đã lưu. Revision cũ không có audit phù hợp cho giá trị null. `reason` giữ cách đọc cũ: lý do sửa draft trên draft, chuỗi rỗng ở revision đang hiệu lực, lý do thu hồi sau revoke. Receipt cũ có thể thiếu trường mới, vì vậy dùng GET sau reload.

Publish và revoke nhận body `{"reason":"Lý do thao tác"}`. Gửi `Idempotency-Key` và nguyên `If-Match` lấy từ draft hoặc revision tương ứng. Retry giữ nguyên key, body, và If-Match. Dùng ETag revision mới từ GET trước khi thu hồi. Bản ghi thu hồi làm ETag thay đổi. Thiếu điều kiện trả `428 precondition_required`; version lệch trả `409 concurrency_conflict`; dùng lại key với fingerprint khác trả `409 idempotency_key_reused`.

## Route Web

Gọi với `credentials: "include"`. GET không cần CSRF. Với POST/PUT, lấy `requestToken` qua `GET /api/v1/auth/web/csrf` trong cùng phiên rồi gửi `X-CSRF-TOKEN`. Bearer vẫn dùng được. Các request DTO và precondition tiếp tục theo các lệnh tương ứng trong [cookbook](FE-API-COOKBOOK.md).

| Method và path dưới `/api/v1` | Caller và vận chuyển |
| --- | --- |
| GET `/datasets/{id}/assessments/{assessmentId}` | Web theo quyền đọc assessment hiện hành. Dùng Location của POST. |
| GET `/projects/{projectId}/baseline-selections/{id}` | Web theo quyền đọc baseline hiện hành. Dùng Location của POST để xem lịch sử. GET collection vẫn là nguồn `items[].selectionId` hiện hành để thay baseline. |
| POST `/processing-jobs`; GET `/processing-jobs/{jobId}`; POST `/processing-jobs/{jobId}/retry` | Web theo guard processing. Create/retry dành cho PM hoặc Supervisor. Read có hỗ trợ Operator theo assignment. Create/retry cần key; retry cần If-Match. |
| POST `/projects/{projectId}/validation-runs`; GET `/validation-runs/{runId}` | Web. PM trong project tạo với key, PM/Supervisor đọc theo scope hiện hành. |
| POST `/projects/{projectId}/ai-mock-runs`; GET `/projects/{projectId}/ai-mock-runs/{runId}`; GET `/projects/{projectId}/ai-mock-runs/{runId}/result` | Web khi tính năng mock được bật ở runtime hỗ trợ. Giữ guard mock và Idempotency-Key cho create. |
| GET `/projects/{projectId}/ai-candidates/{detectionId}` | Bearer. Chuỗi đọc được duyệt đi qua run/result và file ảnh, chưa có caller Web trực tiếp cho route này. Từ result đọc `detections[]` theo `detectionId` với `typeCode`, `confidence`, `bbox`, `timestampMs`, `segmentId`, `positionStatus`, `geometry`, `frameFileId` và `frameFileVersion`. Dùng `frameFileId` để mở ảnh. |
| POST `/projects/{projectId}/retention/evaluations`; GET `/projects/{projectId}/retention/evaluations/{id}` | Web theo guard retention. Gửi key khi tạo và dùng Location để đọc lại. |
| GET `/projects/{projectId}/retention/files/{fileId}`; PUT `/projects/{projectId}/retention/files/{fileId}/basis` | Web theo guard retention. Gửi key và If-Match cho PUT. |
| POST `/retention/holds`; GET `/retention/holds/{id}`; POST `/retention/holds/{id}/release` | Web theo guard retention. Gửi key khi tạo/thu hồi và If-Match khi thu hồi. |
| POST `/projects/{projectId}/warranties` | Web Supervisor theo quyền tạo warranty hiện hành. Giữ DTO gồm operationId cùng kiểm phạm vi tài liệu. |
| GET `/files/{fileId}`; GET `/files/{fileId}/content` | Web PM/Supervisor cho `AI_FRAME` của AI run thành công và còn quyền đọc. Lấy `frameFileId` từ result. Content kiểm lại quyền sau khi mở stream. PM/Supervisor cũng có thể đọc `DOCUMENT` đã VERIFIED và gắn với HandoverDocument đúng project, dùng `fileId` từ `sources[]` trong road coverage. Các purpose còn lại giữ đường Bearer ở route file chung. Màn chi tiết Case dùng route evidence có scope Case từ [handoff Case Report](CASE-REPORT-DETAILS.md). Export dùng route manifest/content của job theo [cookbook](FE-API-COOKBOOK.md). |
| POST `/internal/processing-jobs/{jobId}/results` | Callback nội bộ, JWT dịch vụ riêng. |
| POST/GET `/uploads` theo luồng multipart hiện có | Phạm vi Web giữ Supervisor, DOCUMENT, đúng project/target và chủ phiên. |

Khi thiếu hoặc mất phiên, FE xử lý 401. Với 403, đối chiếu quyền và `code` (ví dụ `csrf_failed` ở request ghi). Các mã nghiệp vụ và hành vi replay vẫn theo contract của từng route.

## Danh mục dùng chung cho Supervisor

| Method | Path | Headers |
| --- | --- | --- |
| GET | `/api/v1/catalog/defect-types/admin` | Bearer hoặc Web cookie |
| POST | `/api/v1/catalog/defect-types/admin` | `Idempotency-Key` và CSRF nếu dùng cookie |
| PUT | `/api/v1/catalog/defect-types/admin/{code}` | `Idempotency-Key`, `If-Match` và CSRF nếu dùng cookie |

GET trả mảng gồm cả mục active và inactive, sắp theo code. DB hiện có thể chứa mục fixture. Tạo qua POST:

```json
{"code":"DEPRESSION","name":"Chỗ lõm","description":"Lún cục bộ trên mặt đường","isActive":true,"reason":"Bổ sung danh mục khảo sát"}
```

PUT nhận `name`, `description`, `isActive` và `reason`. Trường `isActive` bắt buộc. BE chuẩn hóa mã mới bằng trim và viết hoa, cho phép ASCII A–Z, 0–9, gạch dưới và gạch ngang, tối đa 80 ký tự. Tên tối đa 150, mô tả tối đa 2.000, lý do tối đa 1.000. Mô tả rỗng được chuẩn hóa thành null. Mã đã lưu là định danh bất biến.

POST thành công trả 201. PUT và replay cùng lệnh trả 200. Body gồm `code`, `name`, `description`, `isActive` và `version`. Khi mở form sau reload, GET danh sách và gửi version của mục trong dấu ngoặc kép qua `If-Match`. Response ghi cũng có ETag. Version đổi sau mỗi lần ghi. Audit lưu actor, lý do, giá trị trước/sau và mã của mục. Ngừng áp dụng giữ hàng dữ liệu để hồ sơ cũ tiếp tục tham chiếu. PM vẫn gọi GET `/api/v1/catalog/defect-types` để lấy `{code,name}` của mục active. Khi ghi `KEEP_NEW`, BE tiếp tục kiểm mục còn active.

Mã lỗi gồm 400 `validation_error`, 401 thiếu phiên, 403 `access_forbidden` hoặc `csrf_failed`, 404 `not_found`, 409 `defect_type_code_exists`, `idempotency_key_reused` hoặc `concurrency_conflict`, và 428 `precondition_required`. Sau xung đột version, tải lại mục và xác nhận lại trước khi ghi với key mới.

## Bàn giao khi tạo project mới

Supervisor khởi tạo staging bằng `POST /api/v1/projects/handover-upload-intents`. Gửi `Idempotency-Key` và CSRF nếu dùng cookie. Body:

```json
{"purpose":"DOCUMENT","fileName":"handover.pdf","mediaType":"application/pdf","sizeBytes":1024,"checksumSha256":"<sha256>"}
```

BE trả `201` với `id` là upload ID, `fileId`, `status`, `expiresAt`, version và ETag. Staging thuộc riêng Supervisor đã tạo. PDF tối đa 20 MiB. Phiên hết hạn sau 24 giờ. Gọi `POST /api/v1/uploads/{id}/part-urls` với key riêng và `{"partNumbers":[1]}`. PUT từng part qua URL trả về, lấy ETag của storage. Gửi `POST /api/v1/uploads/{id}/complete` với key riêng, If-Match từ GET upload hiện hành, SHA-256 và mảng parts. `202` chuyển sang xác minh. Chờ GET upload trả `VERIFIED`.

`POST /api/v1/projects` hoàn tất project với `Idempotency-Key` mới, nguyên ETag upload VERIFIED trong `If-Match` và một `fileId`:

```json
{"code":"RG-A","name":"Dự án A","primaryPmId":"<pmId>","handoverDate":"2026-11-17","warrantyEndDate":"2027-11-17","handoverFileIds":["<fileId>"]}
```

BE tạo project, hồ sơ bàn giao, warranty, scope chính thức, audit và receipt trong cùng transaction. `201` trả `projectId` và `handoverDocumentId`. Retry sau timeout giữ nguyên key, body và If-Match. Hai finalize cạnh tranh chỉ một lệnh dùng được file. Sau reload, đọc `GET /api/v1/projects/{projectId}/road-coverage`, chọn nguồn `HANDOVER` và dùng `fileId` để đọc `/api/v1/files/{fileId}` hoặc `/content`. Với project cũ còn thiếu tài liệu, dùng [luồng attach hiện hành](PROJECT-HANDOVER-ROAD-COVERAGE.md).

Mã lỗi chính: `428 handover_required` (thiếu file/ETag), `404 project_handover_file_not_found` (sai owner, purpose, scope hoặc đã dùng), `409 handover_file_not_ready` (chưa VERIFIED, thất bại hoặc hết hạn), `409 concurrency_conflict` (ETag cũ), `409 duplicate_request` (đổi request cùng key), `403 csrf_failed` khi thiếu/sai CSRF. File staging hết hạn không thể finalize.

Worker recovery multipart hiện có quét staging hết hạn, kể cả phiên PENDING, UPLOADING, VERIFYING, VERIFIED hoặc FAILED chưa được project sử dụng. Worker dùng cùng thứ tự khóa actor → file → scope → session với finalize; ghi FAILED/`staging_expired` trước khi abort multipart và xóa object. Lỗi storage được retry theo lịch recovery. Trong một giờ sau expiry, worker tiếp tục quét để thu gom thao tác storage đã chạy trước khi phiên hết hạn; sau lần dọn thành công ngoài khoảng này, phiên mang `staging_expired_cleaned`. Multipart ID, fence, deadline và lịch retry được clear. File/scope/session/part/receipt vẫn giữ làm tombstone phục vụ audit, khóa ngoại và replay; không xóa cứng lịch sử bất biến. File đã gắn project/hồ sơ không thuộc tập dọn này. Chỉ bucket không versioning của sandbox local đã được kiểm; chính sách xóa phiên bản object trên bucket versioning chưa được xác minh.

Môi trường test dùng HTTPS cho API và MinIO local qua HTTP. Đã kiểm bằng HTTP client có phiên Web thật. Khi tích hợp từ browser, xác minh URL storage, CORS và quyền đọc ETag trên bản triển khai FE dùng.

Chạy migration `20261117101500_ProjectHandoverStaging` trước khi đưa BE mới lên môi trường đích. Migration mở thêm một trạng thái private staging trong check constraint và cho phép trigger thực hiện đúng một lần chuyển tệp vào scope project nếu đã VERIFIED và đã có hồ sơ tương ứng. Dữ liệu project và hồ sơ cũ được giữ nguyên. Downgrade từ chối nếu còn scope staging.

## Kiểm tra lại trên Web

- Processing và validation dùng dữ liệu survey/measurement hợp lệ trong fixture SQL. Mutation cookie gửi CSRF cùng Idempotency-Key. Processing retry dùng ETag hiện hành của job.
- Với hồ sơ bàn giao, PM trong project và Supervisor có thể đọc file đã gắn sau reload. PM ngoài project nhận 403.
- Retention basis tiếp tục yêu cầu inventory đầy đủ. Reader report cũ từng luôn gắn `HUY_REPAIR_REFERENCE_UNAVAILABLE` dù reader inspection/repair đã được đăng ký. Bản này chỉ gắn mã đó khi thiếu reader `HUY02_INSPECTION`; mọi lý do thiếu provenance/warranty/inventory thực tế vẫn chặn `409 retention_inventory_incomplete`.
- Supervisor GET `/api/v1/projects/{projectId}/retention/files/{fileId}`, lấy ETag, `inventoryVersion` và toàn bộ ID trong `applicableObligations`. PUT cùng path thêm `/basis`, gửi CSRF, `Idempotency-Key`, `If-Match` và body dưới đây. Thành công 200 có ETag/version mới. GET sau reload phải dùng basis hiện hành. Retry mất response giữ nguyên key/body/If-Match; `412 concurrency_conflict` phải tải lại và xác nhận lại trước khi tạo lệnh mới.

```json
{"warrantyIds":["<applicableObligations[].id>"],"expectedReferenceInventoryVersion":"<inventoryVersion>","reason":"Xác nhận căn cứ lưu trữ hồ sơ bàn giao"}
```

Fixture API cũ tạo project được chuyển sang helper chuẩn bị staging VERIFIED bằng SQL rồi gọi finalize thật qua HTTP. Đây là metadata test, không chứng minh multipart/MinIO. Development Seeder giữ đường dẫn `TEST_ONLY/…` và không nâng provenance thành nguồn thật. Script demo dùng staging multipart với PDF trước khi tạo project. Postman cần thiết lập `projectHandoverFileId` và `projectHandoverUploadETag` từ phiên VERIFIED; không gửi mảng file rỗng để tạo mới.
