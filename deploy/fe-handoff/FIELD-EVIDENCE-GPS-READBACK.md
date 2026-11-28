# FIELD — GPS từng ảnh và readback proof (2026-11-28)

Nguồn yêu cầu: owner/Tùng, task FIELD-EVIDENCE-GPS-READBACK; baseline `dd64412bc9e1ce629d86c316e3300aa72e12e981` (đã gồm thay đổi warranty schedule của leader). Đây là mở rộng tương thích response/declaration, không thay readiness, review, assignment, xác minh GPS hoặc quyền tải file. Không có migration mới.

## Thứ tự gọi và ID

1. Crew đọc API-080 `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}` theo assignment hiện hành; lấy task `version`/ETag và start origin bằng workflow hiện có.
2. Tạo upload đúng project/task/purpose, tải multipart, complete và chờ file `VERIFIED` như trước. GPS không gửi vào upload/file metadata chung.
3. API-095 `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions`: Bearer Crew, `Content-Type: application/json`, `Idempotency-Key`, nguyên ETag task trong `If-Match`. Lưu nguyên request/key/If-Match để retry khi chưa rõ kết quả. Thành công mới `201`; replay receipt `200`. Đổi GPS là đổi nội dung; cùng key → `409 idempotency_key_reused`, cùng origin/khác nội dung với key mới → `409 origin_content_conflict`.
4. Lưu `id` của submission. API-096 `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId}` trả `200` cho chính revision đó. Không cần If-Match hoặc Idempotency-Key trên GET. Mở lại app: task trả latestSubmissionId cho bản mới nhất; giữ ID của các revision đã nhận để mở lại bản cũ. PM/Sup có details.submissions trong history; history Crew hiện không có collection này, không coi đó là nguồn discovery đầy đủ của mọi revision. Không lấy proof của bản mới nhất thay bản đang xem.
5. Crew hiện hành đọc theo assignment; PM theo project và Supervisor theo guard hiện có. Web PM/Sup dùng cookie với `credentials: "include"`; GET không cần CSRF. Cookie mutation vẫn cần `X-CSRF-TOKEN` theo cơ chế có sẵn. Không mở quyền submit cho PM/Sup hay quyền đọc cho Reporter.

## JSON wire

Đoạn input API-095 minh họa dưới đây dùng ID hợp lệ lấy từ workflow của chính task, không dùng UUID mẫu để gọi thật. `measurements` vẫn theo yêu cầu task; GPS ảnh không thay số đo.

```json
{
  "originId": "<new-origin-id>",
  "startOriginId": "<first-start-id>",
  "parentSubmissionId": null,
  "measurements": [],
  "evidence": [
    {"captureOriginId":"<capture-A>","fileId":"<verified-file-A>","purpose":"MEASUREMENT","checksumSha256":"<sha256-A>","mediaType":"image/png","capturedAt":"2026-11-28T01:02:03Z","coordinates":{"longitude":106.1,"latitude":10.1,"accuracyMeters":0,"source":"CAPTURE","acquiredAt":"2026-11-28T01:02:03Z"}},
    {"captureOriginId":"<capture-B>","fileId":"<verified-file-B>","purpose":"MEASUREMENT","checksumSha256":"<sha256-B>","mediaType":"image/png","coordinates":{"longitude":107.2,"latitude":11.2,"accuracyMeters":null,"source":"EXIF","acquiredAt":null}},
    {"captureOriginId":"<capture-C>","fileId":"<verified-file-C>","purpose":"MEASUREMENT","checksumSha256":"<sha256-C>","mediaType":"image/png"}
  ],
  "locationProof":{"kind":"POSITION_CHECKLIST","checklist":"ROUTE_CHAINAGE_MARKINGS_CONFIRMED","observedRouteVersionId":"<pinned-route-version>","observedChainageMeters":5.25},
  "captureType":"MEASUREMENT",
  "deviceId":"<source-device-if-present>"
}
```

API-096 trả các phần `evidence` và `locationProof` đã khai của đúng submission, cùng các trường response hiện hữu. Phần bổ sung `evidenceReferences` được join bằng `captureOriginId`, không bằng vị trí phần tử:

```json
{
  "locationProof":{"kind":"POSITION_CHECKLIST","checklist":"ROUTE_CHAINAGE_MARKINGS_CONFIRMED","longitude":null,"latitude":null,"observedSlabId":null,"observedSegmentId":null,"observedRouteVersionId":"<pinned-route-version>","observedChainageMeters":5.25},
  "evidenceReferences":[{"evidenceLinkId":"<immutable-link-id>","captureOriginId":"<capture-A>","fileId":"<actual-file-id>","fileVersion":"<pinned-file-version>","checksumSha256":"<sha256-A>","originalActorId":"<original-crew>","sourceDeviceId":"<declared-source-device>","actualUploaderId":"<actual-uploader>","offlineAdmissionId":null,"grantId":null}]
}
```

Ví dụ trên là hình dạng contract với placeholder; bằng chứng HTTP/SQL và response có UUID thật nằm trong kết quả test cô lập, có thể cung cấp riêng. Không phải dữ liệu nghiệm thu FE.

## Ý nghĩa và null

- Mobile map GPS của **chính capture** sang `evidence[].coordinates`. Không có source Mobile được kiểm trong task này nên không khẳng định tên field nội bộ ngoài mapping wire cần nối.
- `longitude`, `latitude` là số WGS84 và phải đi cùng nhau, hữu hạn, lần lượt trong [-180,180], [-90,90]. Object rỗng/một vế bị `400 validation_error`. JSON NaN/Infinity hoặc kiểu số không hợp lệ bị từ chối. `accuracyMeters` nullable, hữu hạn và >=0; thiếu khác 0. `source` nullable (tối đa 120 ký tự, không áp thứ tự tin cậy), `acquiredAt` nullable. Đây là khai báo thiết bị, không phải xác minh độc lập.
- Ảnh C không có GPS thì field `coordinates` bị bỏ khỏi JSON; FE coi absent/null là không có GPS. Không lấy GPS ảnh A/B, proof submission, GPS số đo, defectTarget, tâm đoạn hay EXIF đọc lại để điền.
- `observedChainageMeters` là lý trình **mét**, không phải lat/lon hoặc chiều dài hư hỏng. Proof dùng đúng route/segment/slab pin và guard hiện có.
- Không khai locationProof → `locationProof: null`. Khai `{ "kind":"UNKNOWN" }` → object UNKNOWN (các thành phần thiếu giữ null). Không tái dựng vị trí legacy từ task hiện tại.
- PayloadJson bất biến là declaration authoritative dùng cho hash/readiness/review. Bảng proof và capture snapshot là projection được đối chiếu, không trộn hay chọn nguồn khác khi mâu thuẫn. `409 field_submission_snapshot_inconsistent` yêu cầu báo BE để đối soát; không submit lại hay tự sửa dữ liệu cũ.
- `evidenceReferences` chứa ID link thật, fileId thực (offline declaration có thể vẫn fileId:null), version/hash và provenance bất biến khi có. `actualUploaderId` legacy không lưu riêng giữ null, không suy từ owner. `sourceDeviceId` chỉ là declaration của nguồn khi có, không tự chứng nhận thiết bị. Không dùng các ID này để mở rộng quyền tải ảnh.
- GPS_CAPTURE hợp lệ về định dạng vẫn có `GPS_SOURCE_BINDING_UNAVAILABLE` khi chưa có cấu hình xác minh. Không đổi POSITION_CHECKLIST/readiness/review.

## Offline, supplement và reuse

- `coordinates` optional và bị omit khi null trên public DTO, persistence fact và adapter repair/offline. Golden FIELD_START và FIELD_SUBMISSION legacy khóa UTF8/core hash/envelope hash. Không chèn null hoặc ID server vào payload cũ.
- Offline ký cả declaration có GPS trước sync/admission; importer/handover tải file thật theo admission. Link/readback trả actor gốc, device gốc khi đã khai, actual uploader/importer, admission/grant riêng; không viết đè declaration đã ký bằng fileId được cấp sau.
- Bản bổ sung tạo revision mới; đọc ID cũ vẫn thấy GPS/proof cũ. Cùng request replay không tạo link/submission trùng. Receipt đã tồn tại từ trước bản sửa có thể giữ response cũ; GET API-096 là readback mới.
- BEFORE reuse hiện có vẫn giữ source/version/hash và capture metadata nguồn trong reuse provenance. Không nhận GPS mới của người reuse để thay nguồn (`400 validation_error` nếu khai coordinates trên evidence thuộc nhánh reuse). Không tự copy GPS nguồn sang declaration mới; nguồn giữ nguyên trong contract evidence/reuse hiện hữu, không mất ownership hay lineage.

## Lỗi và tích hợp

`400 validation_error`: sai cặp/range/accuracy/shape; `401`: phiên không hợp lệ hoặc bị thu hồi; `403 access_forbidden`/`evidence_access_forbidden`: sai role/project/task/assignment/file scope; `404 not_found`: ID không thuộc task hoặc không tồn tại; `409`: origin/key/version/lineage/projection theo ProblemDetails; `428 precondition_required`: thiếu header của command. Không retry bằng key mới khi chưa rõ kết quả.

Tùng cần nối GPS từng ảnh vào declaration, giữ signed payload/request cũ cho retry, đọc proof và evidence theo submissionId, join reference bằng captureOriginId và hiển thị thiếu GPS rõ ràng. Tải ảnh vẫn qua đường FIELD/file được cấp quyền, kiểm quyền riêng. Cần retest app thật; HTTP/SQL/MinIO fixture không chứng minh camera/OS/cache/offline app hoặc UI đã tích hợp.

## Knowledge và triển khai

Đã đối chiếu handoff leader `FE-WARRANTY-SCHEDULE-VALIDATION.md` và các hướng dẫn Knowledge sync trong repo. Chưa xác định được quyền/path của đúng Knowledge Space local từ nguồn truy cập được; tài liệu này và spec là bản repo, **chưa cập nhật/reindex Knowledge Space**.

Đợt này không thay schema, không seed/reset, không cập nhật runtime local/public server, không SSH hay đổi AI gitlink/model. Source push không đồng nghĩa runtime FE đã cập nhật. Cần runtime có source mới rồi mới kiểm app. CI tự chạy, chưa theo dõi/chưa xác nhận theo yêu cầu owner.

## Readback HTTP thực từ fixture SQL/MinIO cô lập

Đây là trích response của ca `IndependentCaptureGpsSurvivesRealUploadReadbackReplayAndValidation`, không phải runtime FE. Ba ảnh đã VERIFIED; GPS ảnh khác proof, readiness vẫn INCOMPLETE. UUID chỉ có ý nghĩa trong DB test đã được fixture dọn.

```json
{
  "id": "23776b7f-739e-4ea2-8a4f-35fad6b8fc24",
  "taskId": "5e0ccaf7-a531-45f7-ad3a-9fdfb4fa1dbb",
  "revision": 1,
  "readiness": "INCOMPLETE",
  "missingReasons": [
    "GPS_SOURCE_BINDING_UNAVAILABLE",
    "MEASUREMENT_MISSING"
  ],
  "locationProof": {
    "kind": "GPS_CAPTURE",
    "checklist": null,
    "longitude": 108.3,
    "latitude": 12.3,
    "observedSlabId": null,
    "observedSegmentId": null,
    "observedRouteVersionId": null,
    "observedChainageMeters": null
  },
  "evidence": [
    {
      "captureOriginId": "c11de7a1-3b84-4046-bf24-293368139664",
      "fileId": "1d39d221-5888-436d-98a5-e12b9727d9a3",
      "purpose": "MEASUREMENT",
      "checksumSha256": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
      "mediaType": "image/png",
      "capturedAt": "2026-11-28T13:54:13.421892+00:00",
      "attemptChecklist": null,
      "sizeBytes": 68,
      "coordinates": {
        "longitude": 106.1,
        "latitude": 10.1,
        "accuracyMeters": 0,
        "source": "CAPTURE",
        "acquiredAt": "2026-11-28T13:54:13.421892+00:00"
      }
    },
    {
      "captureOriginId": "ae884f27-c160-4efd-a264-63b4336c9469",
      "fileId": "bbbf4da2-1590-40af-a6d8-510eeea2a4f3",
      "purpose": "MEASUREMENT",
      "checksumSha256": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
      "mediaType": "image/png",
      "capturedAt": "2026-11-28T13:54:13.421892+00:00",
      "attemptChecklist": null,
      "sizeBytes": 68,
      "coordinates": {
        "longitude": 107.2,
        "latitude": 11.2,
        "accuracyMeters": null,
        "source": "EXIF",
        "acquiredAt": "2026-11-28T13:54:13.421892+00:00"
      }
    },
    {
      "captureOriginId": "6adccc64-029b-424f-a79e-93961dea62ff",
      "fileId": "7a789b79-26cb-47d4-a54f-98fa7803c5ab",
      "purpose": "MEASUREMENT",
      "checksumSha256": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
      "mediaType": "image/png",
      "capturedAt": "2026-11-28T13:54:13.421892+00:00",
      "attemptChecklist": null,
      "sizeBytes": 68
    }
  ],
  "evidenceReferences": [
    {
      "evidenceLinkId": "bd1943d6-7ed7-435a-8bc1-862a3a65d406",
      "captureOriginId": "c11de7a1-3b84-4046-bf24-293368139664",
      "fileId": "1d39d221-5888-436d-98a5-e12b9727d9a3",
      "fileVersion": "AAAAAAAAB/M=",
      "checksumSha256": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
      "originalActorId": "8f7778d1-2494-416e-84d4-81beb828c7f2",
      "sourceDeviceId": "34de9eef-6a36-4503-aae4-255e6058a4ba",
      "actualUploaderId": "8f7778d1-2494-416e-84d4-81beb828c7f2",
      "offlineAdmissionId": null,
      "grantId": null
    },
    {
      "evidenceLinkId": "3ee15227-ff65-4292-ac61-34880091a645",
      "captureOriginId": "ae884f27-c160-4efd-a264-63b4336c9469",
      "fileId": "bbbf4da2-1590-40af-a6d8-510eeea2a4f3",
      "fileVersion": "AAAAAAAAB/k=",
      "checksumSha256": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
      "originalActorId": "8f7778d1-2494-416e-84d4-81beb828c7f2",
      "sourceDeviceId": "34de9eef-6a36-4503-aae4-255e6058a4ba",
      "actualUploaderId": "8f7778d1-2494-416e-84d4-81beb828c7f2",
      "offlineAdmissionId": null,
      "grantId": null
    },
    {
      "evidenceLinkId": "7c99123b-bf5f-4108-837f-724ed578697c",
      "captureOriginId": "6adccc64-029b-424f-a79e-93961dea62ff",
      "fileId": "7a789b79-26cb-47d4-a54f-98fa7803c5ab",
      "fileVersion": "AAAAAAAAB/8=",
      "checksumSha256": "081b627cac8d3d4e337ee059fcf2696e8e14c3247021f9fab4ed44578bbbc9ec",
      "originalActorId": "8f7778d1-2494-416e-84d4-81beb828c7f2",
      "sourceDeviceId": "34de9eef-6a36-4503-aae4-255e6058a4ba",
      "actualUploaderId": "8f7778d1-2494-416e-84d4-81beb828c7f2",
      "offlineAdmissionId": null,
      "grantId": null
    }
  ]
}
```

## Kết quả và giới hạn kiểm chứng

- Preflight HTTP/Swagger: 40/40 PASS, 0 skip. Ca ảnh bổ sung PM/Sup/overflow chạy lại 1/1 PASS, trùng một ca trong nhóm 40.
- Hồi quy FIELD hiện có: 8/8; offline direct/handover với MinIO thật cô lập: 2/2. Dùng lại các kết quả còn hợp lệ, không cộng lượt trùng.
- Canonical/repair unit: 11/11; SQL registry/reuse: 28/28, không skip. Build Release/analyzer, format scope, TRX gate và diff check đạt. Không phải full suite hoặc nghiệm thu app.
- Hai lượt self-review đã thực hiện; chưa có external review. Không có migration mới và chưa cập nhật runtime/server/DB FE. CI tự chạy, chưa theo dõi/chưa xác nhận.
