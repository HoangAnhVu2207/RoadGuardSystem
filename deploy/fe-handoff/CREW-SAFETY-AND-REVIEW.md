# Bàn giao cho Tùng · Crew safety, xem xét lại và vị trí FIELD

Dùng Bearer của Crew. Các GET dưới đây không cần CSRF. Mỗi thao tác ghi mới dùng `Idempotency-Key` mới và `If-Match` có dấu ngoặc kép. Khi timeout, gửi lại đúng URL, body, key và If-Match của lần gửi đầu.

## Biện pháp an toàn và link ảnh

1. Lấy `projectId`, `taskId`, `repairContext.packageId` và `repairContext.itemId` từ API-080. Nếu app đã lưu scope từ task trước, danh sách an toàn kiểm quyền phụ trách riêng trên scope đó.
2. Gọi `GET /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures`. Response `{"measureIds":["11111111-1111-4111-8111-111111111111"]}` hoặc `{"measureIds":[]}`. Chỉ trả biện pháp do Crew hiện tại phụ trách, thuộc item và nghĩa vụ an toàn chưa giải quyết. Sai role/membership trả 403, sai scope tài nguyên trả 404 sau kiểm quyền.
3. Dùng API-152 `GET …/safety-measures/{measureId}` lấy ETag của biện pháp. Crew đọc `eligibleEvidenceLinks`, mỗi mục gồm `evidenceLinkId` và `fileId`. Có thể trả mảng rỗng.
4. Upload ảnh `MEASUREMENT` theo project và task FIELD. Chờ upload `VERIFIED`, lấy `fileId`. API-085 đọc metadata bằng `fileId`. Sau khi accept/start FIELD, đưa ảnh vào `evidence[]` của API-095 khi thực sự nộp số đo hoặc bản bổ sung hợp lệ. API-095 dùng ETag **task**. Ví dụ một mục evidence:

```json
{
  "captureOriginId": "22222222-2222-4222-8222-222222222222",
  "fileId": "33333333-3333-4333-8333-333333333333",
  "purpose": "MEASUREMENT",
  "checksumSha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "mediaType": "image/jpeg"
}
```

5. API-096 đọc bản nộp theo submission ID trả từ API-095. Đọc lại API-152 và đối chiếu `fileId` để lấy `evidenceLinkId`. BE kiểm cùng project, task, assignment và chủ ảnh theo binding đã lưu của biện pháp. Khi FIELD đã qua trạng thái cho phép nộp, dùng link đủ điều kiện đã có. Nếu thiếu link, báo thiếu bằng chứng và chuyển xử lý theo luồng bổ sung FIELD được phép.
6. Sau khi biện pháp đã lắp đặt qua API-154, gọi API-153 `POST …/safety-measures/{measureId}/checks` với ETag **biện pháp** và body:

```json
{"result":"Safe","findings":"Rào chắn nguyên vẹn","evidenceIds":["44444444-4444-4444-8444-444444444444"]}
```

Chọn tối đa 20 link, mỗi file một lần. Thiếu header trả 428. Xử lý 403 khi mất quyền hoặc sai scope ảnh, 404 khi nguồn ảnh không tồn tại, 409 khi version cũ, ảnh chưa xác minh, thiếu liên kết hoặc trạng thái biện pháp không cho phép kiểm. Đọc lại API-152 sau khi ghi.

## Đọc quyết định và đối soát API-150

API-080 trả thêm hai trường trong `repairContext`:

```json
{
  "finalDecision": {"decisionId":"55555555-5555-4555-8555-555555555555","result":"CONFIRMED","at":"2026-11-14T08:00:00Z"},
  "reviewRequests":[{"requestId":"66666666-6666-4666-8666-666666666666","decisionId":"55555555-5555-4555-8555-555555555555","reason":"Đề nghị xem lại kết quả","at":"2026-11-14T08:05:00Z"}]
}
```

`finalDecision` là quyết định hiệu lực hoặc null. `attempt.review` vẫn là PM review của bản nộp. `reviewRequests` chỉ gồm yêu cầu của Crew đang đọc, có thể rỗng. POST API-150 tới `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/review-requests` với `{"reason":"Đề nghị xem lại kết quả"}` và If-Match từ **itemVersion**. Task ETag và ETag lịch sử dùng cho mục đích riêng. API-150 trả 201 lần đầu, 200 khi replay. Đối soát bằng ID response và `reviewRequests[].requestId`. Correction có thể thay `finalDecision`. Yêu cầu đã gửi giữ `decisionId` gốc. Khi 409 version cũ hoặc head đổi, tải lại API-080 để người dùng xem quyết định hiện hành trước lần gửi mới. Khi mất assignment hoặc quyền, bỏ dữ liệu đã cache của task. API-141/148 tiếp tục dành cho PM/Supervisor.

## POSITION_CHECKLIST trong locationProof của API-095

```json
{"kind":"POSITION_CHECKLIST","checklist":"SLAB_MARKINGS_CONFIRMED","observedSlabId":"SLAB-KEY"}
```

Dùng khi API-080 có cả `slabId` và `layoutRevisionId`. Gửi đúng `slabId` đã pin.

```json
{"kind":"POSITION_CHECKLIST","checklist":"SEGMENT_MARKINGS_CONFIRMED","observedSegmentId":"77777777-7777-4777-8777-777777777777","observedChainageMeters":5}
```

Với task theo đoạn, lấy `segments[].id` và khoảng `fromOffsetMeters` đến `toOffsetMeters` từ API-087 GET task geometry. Dùng đoạn trong đúng route version và segment set của task. Gửi lý trình quan sát nằm trong khoảng đoạn.

```json
{"kind":"POSITION_CHECKLIST","checklist":"ROUTE_CHAINAGE_MARKINGS_CONFIRMED","observedRouteVersionId":"88888888-8888-4888-8888-888888888888","observedChainageMeters":5}
```

Với task theo tuyến, lấy `routeVersionId` từ API-080 và nhập lý trình quan sát trong phạm vi chiều dài tuyến. Nếu thiếu pin hoặc chưa xác định vị trí, dùng `{"kind":"UNKNOWN"}` và hiển thị lý do chưa đủ dữ liệu. API-087 có thể trả 409 `source_not_ready` nếu thiếu segment set phù hợp. Sai checklist hoặc sai phạm vi cho `INCOMPLETE` với `POSITION_CHECKLIST_INSUFFICIENT`. Gửi GPS cùng checklist cho `LOCATION_PROOF_CONTRADICTORY`. Payload sai cú pháp/loại hoặc tọa độ ngoài miền trả 400. PM xác nhận bản nộp chưa đủ dữ liệu có thể nhận 409 `field_result_insufficient`.

`GPS_CAPTURE` có tọa độ hợp lệ vẫn có thể trả `INCOMPLETE` với `GPS_SOURCE_BINDING_UNAVAILABLE`. Để xác minh GPS tự động cần geometry được pin theo task, CRS và phép chuyển đổi được duyệt, dung sai không gian, cùng quy tắc dùng độ chính xác GPS và xử lý dữ liệu thiếu.

[OpenAPI bàn giao](openapi.json)
