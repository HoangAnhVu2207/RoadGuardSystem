# Offline admission → upload → exact replay — Tùng

Cập nhật 2026-11-28: GPS riêng từng ảnh và proof theo revision được mô tả tại [FIELD-EVIDENCE-GPS-READBACK.md](FIELD-EVIDENCE-GPS-READBACK.md). GPS optional nằm trong evidence declaration đã ký; absent/null không đổi canonical payload legacy. Không thay readiness hoặc quyền tải file.

Base `fbde7c4`, đối chiếu phản ánh từ `b891e88`. Source đã có admission/binding/capture persistence và guard upload, nhưng thiếu mapping admission trong readback và có thể lưu FIELD submission incomplete trước ảnh VERIFIED. Bản này bổ sung discovery **per operation**, staging chờ ảnh và tiếp tục nguyên hồ sơ đã ký. Không cần SQL/admin hoặc chèn server ID vào signed core.

## Thứ tự gọi

Bearer Crew; prefix `P = /api/v1/projects/{projectId}/offline`. Account/device/membership/assignment/snapshot và importer/grant vẫn được kiểm ở thời điểm gọi. Admission ID không phải capability.

1. `POST P/devices`: đăng ký cặp public key của thiết bị. Lưu registration `id`, phân biệt với deviceId do app tạo. `POST P/snapshots` với `deviceRegistrationId`, `taskId`, lấy snapshot và task/assignment/version đã pin.
2. Lưu capture/operation, core hash, chữ ký và toàn bộ batch bền vững **trước lần gửi đầu**. Dùng cơ chế canonical manifest/signing hiện có; dependencies chỉ tới origin trong cùng batch. Không ký JSON tùy ý bằng serializer khác.
3. `POST P/sync` nguyên `OfflineSignedBatchInput`. Admission/binding được stage bền vững. Nếu thiếu ảnh bắt buộc: HTTP `200`, item `state: PENDING_DEPENDENCY`, code `evidence_upload_required` hoặc `evidence_verification_pending`, `durableAcknowledgment: false`. Đây là bước staging được hỗ trợ, không phải failed-sync hack.
4. Lấy `items[].admission.id` của đúng `originId`; chọn đúng `admission.captures[].captureOriginId`. Nếu mất response, gửi lại nguyên batch để lấy server batch ID; sau đó `GET P/batches/{serverBatchId}` hoặc `GET P/origins/{operationOriginId}` để đối soát.
5. `POST /api/v1/offline/uploads` với AdmissionId/CaptureOriginId và upload metadata khớp declaration. Gửi `Idempotency-Key`, lưu nguyên request/key trước lần đầu. Response `201` có upload `id` và `fileId`. Mất response: retry cùng key/body hoặc GET batch/origin để lấy mapping, không tạo capture/file khác.
6. `POST /api/v1/uploads/{uploadId}/part-urls`, body `{"partNumbers":[1]}`, Idempotency-Key. PUT bytes vào URL part (không ghi URL ký vào log). `GET /api/v1/uploads/{uploadId}` lấy version mới, `POST …/complete` gửi Idempotency-Key và `If-Match: "<version>"`, body parts/ETag/checksum. `202` chưa có nghĩa VERIFIED. Poll session hoặc `GET /api/v1/files/{fileId}` đến VERIFIED.
7. Gửi lại **đúng batch, payload và chữ ký ban đầu** tại sync (handover dùng đúng import/package/endorsement). Server resolve capture → file qua mapping transport đã kiểm. Khi thành công item có DurableAck và effect/canonical origin; không tạo effect/evidence trùng.
8. Phiên Crew mới đọc batch/origin/receipt để đối soát; chỉ bỏ local queue khi durable acknowledgment. Gửi lại khi mất response business commit không tạo lần nộp mới.

Các POST offline yêu cầu Idempotency-Key theo action hiện hành; sync/import chống trùng chính bằng signed identity và binding. Nếu dùng Web session ở action được hỗ trợ, POST vẫn cần CSRF. Mobile Crew dùng Bearer.

## Các ID không được tráo

| Field | Nguồn / ý nghĩa |
|---|---|
| `batchId` request / `admission.sourceBatchId` | Client source batch đã ký |
| `batchId` response / `admission.batchId` | Server batch/import được lưu; dùng GET batch |
| `sourceDeviceRegistrationId` | ID đăng ký server, khác deviceId |
| `originId` | Identity operation đã ký, khác capture |
| `effectId`, `canonicalOriginId` | Business effect/receipt thực tế; không suy bằng admissionId |
| `admission.id` | Admission của operation/binding/importer hiện hành; dùng body `admissionId` của offline upload |
| `captures[].captureOriginId` | Capture khai báo trong signed operation |
| `captures[].fileId`, `uploadId` | Transport mapping nullable, chỉ có khi tạo upload hợp lệ |

Ví dụ **hình dạng wire**, UUID dùng ký hiệu nguồn vì mỗi lần chạy fixture sinh ID riêng:

```json
{
  "batchId": "<serverBatchId>",
  "items": [{
    "originId": "<operationOriginId>",
    "kind": "FIELD_SUBMISSION",
    "state": "PENDING_DEPENDENCY",
    "code": "evidence_upload_required",
    "durableAcknowledgment": false,
    "admission": {
      "id": "<admissionId>",
      "projectId": "<projectId>",
      "batchId": "<serverBatchId>",
      "sourceBatchId": "<clientBatchId>",
      "sourceDeviceRegistrationId": "<registrationId>",
      "originId": "<operationOriginId>",
      "taskId": "<taskId>",
      "assignmentId": "<assignmentId>",
      "snapshotId": "<snapshotId>",
      "originalActorId": "<crewId>",
      "currentImporterId": "<crewId>",
      "grantId": null,
      "status": "WAITING_UPLOAD",
      "captures": [{
        "captureOriginId": "<captureOriginId>",
        "fileId": null,
        "uploadId": null,
        "uploadStatus": "NOT_CREATED",
        "purpose": "MEASUREMENT",
        "mediaType": "image/png",
        "checksumSha256": "<sha256 của bytes gốc>",
        "declaredSizeBytes": 68,
        "sizeBytes": null
      }]
    }
  }],
  "localQueueInstruction": "KEEP_UNACKNOWLEDGED_OR_CONFLICTED_ITEMS"
}
```

```json
{
  "admissionId": "<items[].admission.id>",
  "captureOriginId": "<captureOriginId đã ký>",
  "upload": {
    "purpose": "MEASUREMENT",
    "projectId": "<projectId>",
    "targetId": "<taskId>",
    "fileName": "offline.png",
    "mediaType": "image/png",
    "sizeBytes": 68,
    "checksumSha256": "<sha256 khớp declaration>"
  }
}
```

Declaration thêm `sizeBytes` nullable. App mới ghi size trước khi ký; BE so khớp size khi có, cùng hash/MIME/purpose/owner/scope và byte verification. Legacy không có size giữ nguyên canonical serialization (field null bị omitted), không thay core hash cũ. `fileId: null` trong signed core vẫn giữ null sau upload; chỉ readback transport có fileId. `capturedAt` vẫn là claimed, không thành thời gian xác minh.

Sau VERIFIED mapping có fileId/uploadId, `uploadStatus: VERIFIED`; sau sync thành công `admission.status: COMMITTED`, `durableAcknowledgment: true`, kết quả/receipt tương ứng. JSON thực từ ca HTTP được lưu ở evidence ngoài repo, có thể cung cấp riêng; không đưa token/URL ký vào tài liệu.

## Pending và lỗi

`WAITING_UPLOAD`, `WAITING_VERIFICATION`, `WAITING_DEPENDENCY` chưa ack; hoàn thành transport/dependency rồi replay nguyên batch. `READY` là trạng thái admission dữ liệu, không thay business commit. `UPLOAD_FAILED` chưa ack: đọc session/failureCode và xử lý lỗi, không gắn file VERIFIED bất kỳ hoặc đổi signed capture để giả replay. `CONFLICT`, `REJECTED`, `STALE_SNAPSHOT` cần xử lý đúng code; không retry mù bằng ID/key mới. Admission có thể omitted khi item không còn quyền hoặc proof hợp lệ; không dùng admission đã cache để vượt quyền.

HTTP `401` phiên không hợp lệ; `403` quyền actor/device/importer không hợp lệ; `409` stale snapshot/origin-content/capture/evidence mismatch tùy endpoint. Offline upload dùng mapping lỗi upload hiện hành: admission/capture không tồn tại hoặc guard admission/tương ứng hash/size/MIME/purpose từ chối đều được route tạo offline upload che bằng `404 upload_session_not_found`; không dùng mã này để suy tài nguyên khác có tồn tại; FE xử lý cả ProblemDetails và item code trong response sync 200. Body/signature không hợp lệ có thể trả `400`. Batch có thể có item committed và item pending; không bỏ cả queue hoặc coi HTTP 200 là tất cả hoàn tất.

Handover không giả actor gốc: admission giữ originalActor/source device, currentImporter và grant. Grant hiện có hạn **24 giờ**; việc tiếp tục capture đã được admit trước hạn còn phụ thuộc upload session và guard continuation hiện có. Không có TTL mới cho admission/receipt. Rà source hiện không tìm thấy sweeper xóa batch/binding/admission/result; retention inventory ghi nhận capture/admitted file references. Đây là hiện trạng source, **không cam kết lưu vĩnh viễn hoặc cấu hình SQL jobs ngoài repo**.

## Kiểm và triển khai

Xem [spec kết quả cuối](../../planning/development/RESOURCE-EXPORT-OFFLINE-ADMISSION.md) và [OpenAPI](openapi.json). Không có migration mới. BE HTTP/SQL/MinIO không chứng minh UI/native Android. App phải giữ file ảnh gốc và request ký bền vững, tránh gửi song song không kiểm soát, đối soát sau timeout.

## Wire thực từ HTTP fixture SQL/MinIO cô lập

Các UUID dưới đây là dữ liệu test, không dùng như ID môi trường FE. Không có token/URL ký.

Stage:

```json
{
  "originId": "f7f399c9-0164-4157-92c1-5cc4e0ed631b",
  "state": "PENDING_DEPENDENCY",
  "code": "evidence_upload_required",
  "durableAcknowledgment": false,
  "admission": {
    "id": "c7271efc-bcc0-4970-8e34-e5d0f6d51d97",
    "projectId": "590fccaa-5c12-414f-bb1a-d2cd0c833e52",
    "batchId": "d77a46fb-c990-4d9b-93ba-02421c585ff7",
    "sourceBatchId": "d739e6c7-757c-472e-84cd-4813c27312ce",
    "sourceDeviceRegistrationId": "dd1cde4c-6c31-4d1b-8bb4-1ebd56dfdd6a",
    "originId": "f7f399c9-0164-4157-92c1-5cc4e0ed631b",
    "taskId": "cef85d21-6d27-437e-9eef-8784def60a5b",
    "assignmentId": "88e44bd5-dd35-4222-a800-6db3a732e50b",
    "snapshotId": "834cf8da-48cd-45c0-92e2-2d3367c6361d",
    "originalActorId": "fb62fd02-749b-41ca-9915-a2fa077069d3",
    "currentImporterId": "fb62fd02-749b-41ca-9915-a2fa077069d3",
    "grantId": null,
    "status": "WAITING_UPLOAD",
    "captures": [
      {
        "captureOriginId": "0fe317fa-6ed2-4159-8730-f1ae0db9b142",
        "fileId": null,
        "uploadId": null,
        "uploadStatus": "NOT_CREATED",
        "purpose": "MEASUREMENT",
        "mediaType": "image/png",
        "checksumSha256": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
        "declaredSizeBytes": 68,
        "sizeBytes": null
      }
    ]
  }
}
```

Receipt sau phiên Crew mới:

```json
{
  "originId": "f7f399c9-0164-4157-92c1-5cc4e0ed631b",
  "effectId": "f7f399c9-0164-4157-92c1-5cc4e0ed631b",
  "canonicalOriginId": "f7f399c9-0164-4157-92c1-5cc4e0ed631b",
  "state": "COMMITTED",
  "durableAcknowledgment": true,
  "admission": {
    "id": "c7271efc-bcc0-4970-8e34-e5d0f6d51d97",
    "projectId": "590fccaa-5c12-414f-bb1a-d2cd0c833e52",
    "batchId": "d77a46fb-c990-4d9b-93ba-02421c585ff7",
    "sourceBatchId": "d739e6c7-757c-472e-84cd-4813c27312ce",
    "sourceDeviceRegistrationId": "dd1cde4c-6c31-4d1b-8bb4-1ebd56dfdd6a",
    "originId": "f7f399c9-0164-4157-92c1-5cc4e0ed631b",
    "taskId": "cef85d21-6d27-437e-9eef-8784def60a5b",
    "assignmentId": "88e44bd5-dd35-4222-a800-6db3a732e50b",
    "snapshotId": "834cf8da-48cd-45c0-92e2-2d3367c6361d",
    "originalActorId": "fb62fd02-749b-41ca-9915-a2fa077069d3",
    "currentImporterId": "fb62fd02-749b-41ca-9915-a2fa077069d3",
    "grantId": null,
    "status": "COMMITTED",
    "captures": [
      {
        "captureOriginId": "0fe317fa-6ed2-4159-8730-f1ae0db9b142",
        "fileId": "d0eead5a-4bf6-4de1-92a4-3b8fb84dd4cb",
        "uploadId": "db602527-22fd-41ab-93de-a5efd969b522",
        "uploadStatus": "VERIFIED",
        "purpose": "MEASUREMENT",
        "mediaType": "image/png",
        "checksumSha256": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
        "declaredSizeBytes": 68,
        "sizeBytes": 68
      }
    ]
  }
}
```

DurableAck chỉ chứng minh effect đã lưu/chống trùng. Nó không thay readiness, PM review hoặc nghiệm thu nghiệp vụ; submission có thể còn INCOMPLETE theo dữ liệu đo/vị trí hiện hành.

Handover cũng đã chạy HTTP/SQL/MinIO thật; receipt giữ actor nguồn và importer khác nhau:

```json
{
  "id": "9bf6573e-e8f5-44f2-bf4e-7272ab18f2c8",
  "originId": "ac58aad4-679e-4930-a8ea-f70b81f6112f",
  "originalActorId": "4dfd6b04-6bb6-42b3-8f8b-bd7a96bab48a",
  "currentImporterId": "be2cf1a8-79fa-4640-8cd1-f8f25b6e3c4b",
  "grantId": "58b74267-201b-4c87-9e95-5f927841547e",
  "status": "COMMITTED"
}
```

Kết quả cuối: nhóm API/Swagger 52/52 (có các ca trùng full run), SQL lanes 808/808, unit 1156/1156. Full API trước sửa mô tả Swagger có 1717 PASS/1 FAIL/1 smoke SKIP; đã sửa và chạy lại toàn bộ class Swagger, không gọi đó là full API PASS mới. Chi tiết/log trong spec.

## Tin nháp

BE bổ sung admission per operation trong sync/batch/origin để app khép luồng ảnh offline. Sync ban đầu có thể trả pending chờ upload, DurableAck false; lấy admission.id + captureOriginId đúng operation để tạo offline upload, hoàn tất multipart và chờ VERIFIED rồi gửi lại nguyên batch/chữ ký cũ. Không đưa admissionId/fileId server vào signed core. Sau timeout/restart, đọc mapping hoặc retry nguyên request/key để tiếp tục, chỉ bỏ queue khi nhận DurableAck. Quyền Crew/device/assignment và importer/grant vẫn kiểm lại. Nhờ nối theo handoff và test trên app; kết quả backend không thay nghiệm thu Android.
