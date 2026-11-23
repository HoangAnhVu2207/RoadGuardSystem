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


## API-080 — lý do yêu cầu bổ sung hiện hành

`GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}` bằng Bearer Crew đang có membership/assignment và binding hợp lệ. Không mở quyền API-141/148 hoặc lịch sử PM/Supervisor. Response mở rộng tương thích:

```json
{
  "repairContext": {
    "attempt": {
      "attemptId": "11111111-1111-4111-8111-111111111111",
      "effectiveSubmissionId": "22222222-2222-4222-8222-222222222222",
      "review": {
        "reviewId": "33333333-3333-4333-8333-333333333333",
        "decision": "SUPPLEMENT",
        "reason": "Bổ sung ảnh xác nhận phạm vi sửa"
      }
    }
  }
}
```

ID trong ví dụ minh họa, lấy ID thật từ response. `reason` lấy nguyên văn review đã lưu khớp currentReviewId, attempt, binding, intake link và effectiveSubmissionId/content hash; không lấy notification hay final decision. Chỉ hiển thị lý do khi review hiện hành có `decision: "SUPPLEMENT"`. `reason: null` (legacy thiếu/rỗng hoặc quyết định khác) thì dùng nhãn chung “Cần bổ sung hồ sơ”, không suy nội dung khác. Không có attempt/review thì giữ null theo contract cũ.

Sau API-159 thành công, GET lại API-080: cùng attempt nhưng effectiveSubmissionId mới; nếu review đã clear, `review: null` và không giữ lý do cũ trên UI. Khi PM review bản mới, hiển thị reviewId/reason mới. Sau reload/đăng nhập lại cũng GET API-080. Task ETag vẫn là task version; repair commands dùng repairContext.itemVersion. Không đổi request hoặc lifecycle, không có migration.

Kiểm chứng đợt bổ sung (base `973e318`): 38 API/Swagger test PASS, 0 FAIL, 0 SKIP; 6 SQL test PASS, 0 FAIL, 0 SKIP. Ba ca Swagger kiểm lại riêng đã PASS (có trùng test với nhóm 38, không cộng thành số độc lập). Lệnh tập trung:

```powershell
dotnet build RoadGuardSystem.slnx --no-restore
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --filter "FullyQualifiedName~CloseoutNormalJourneyHttpTests|FullyQualifiedName~ManualSwaggerContractTests"
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~CrewBootstrap|FullyQualifiedName~IncompleteFormalFieldIntakePinsOneAttemptAndOriginalPmReviewClock"
dotnet format --verify-no-changes --no-restore
git diff --check
```

HTTP dùng Bearer/auth thật trên TestServer và SQL cô lập; kiểm SUPPLEMENT → phiên Crew mới → API-159 clear review → review lý do mới → ACCEPT/null, sai Crew/project và assignment ended. SQL kiểm source/binding, assignment mất hiệu lực và cùng attempt/submission sau bổ sung. Legacy thiếu reason kiểm default DTO/fact hai chiều; không tạo dữ liệu null trái constraint hiện hành để giả lập legacy. Ảnh trong fixture là storage lineage giả lập, chưa kiểm trên app Crew hay DB FE. Full API đã được chạy sau khi gộp sửa CI; xem kết quả ở cuối tài liệu.

Hai lượt self-review: (1) xác nhận reason nằm trong chính truy vấn review có currentReviewId/attempt/intake/submission/hash và giữ guard; (2) kiểm mapper hai chiều, nullable/exact Swagger schema, không đổi endpoint/request/schema DB, fixture và quyền đọc. Đã sửa fixture ACCEPT phải có đủ bằng chứng và cập nhật exact-schema assertion thêm reason; không nới guard/assertion. External review chưa thực hiện. Không đổi identity SQL test nên không đổi lane manifest.


### Sửa CI base cùng đợt API-080 reason

Đã đọc api.trx artifact `api-results` (14792702600) của run [45116026588](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45116026588), đúng SHA `973e318`: 1689 PASS / 5 FAIL / 1 SKIP. Bước Verify-TestResults trong job bị skip do bước dotnet test fail; không gọi gate đó là PASS.

| Test/line base | Nguyên nhân từ TRX | Sửa |
|---|---|---|
|WebPmCoordination…UpgradeFromPreviousMigration…:276|Expected suffix WebPmCoordinationMetadata, actual InitialTemporarySafetyResponsibility|Giữ upgrade từ staging cũ, dữ liệu project, schema evidence và model parity; kiểm hai migration mới và toàn bộ chain applied khớp discovery|
|AiMockReadBoundary…:225|Exact run wire thiếu originCorrelationId/failure|Thêm đúng hai field, kiểm origin khớp header/manifest và Web read, failure null khi success|
|AiMockReadLifecycle…:259|Exact failed run wire thiếu hai field|Kiểm exact top-level/nested failure, source_hash_mismatch/source_stream/datasetId, recovery và origin; giữ snapshot GET không ghi dữ liệu|
|AiReadRouteValidation…:90, hai case run/result|Exact ProblemDetails cũ thiếu field/reasonCode/recovery/step|Giữ exact fields riêng candidate/run, kiểm mã lý do, readback, field null, recovery; giữ GUID/feature gate/quyền nguồn/correlation và snapshot|

Ca skip duy nhất: `UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`, chưa bật ROADGUARD_MINIO_SMOKE=1/MinioStorage; đúng whitelist hiện hành. Không thêm skip, sửa gate hoặc production để chiều assertion cũ. Các test AI vẫn MOCK; sửa CI không đổi contract production của bản `973e318`. Phần bổ sung contract của đợt này chỉ là reason API-080.


Kiểm tập trung CI + reason: cả 15 test của toàn bộ class WebPmCoordinationHttpTests, AiMockReadBoundaryAcceptanceTests, AiMockReadLifecycleAcceptanceTests, AiReadRouteValidationAcceptanceTests và CloseoutNormalJourneyHttpTests đều PASS, 0 FAIL, 0 SKIP. Đây là lượt riêng, có ca trùng với 38 ca API/Swagger ở trên. Source-contract gates (docs/agent/readiness và ba Python self-test/Docker/workflow/test-results self-test/dependency), format toàn repo, build API `--no-restore --no-incremental` với analyzer đều đạt. Giữ kết quả SQL 6 ca của phần reason vì production/fixture SQL không thay đổi sau lượt đó.

Lượt đầu toàn class gặp 500 ở ca export do local chưa cấp RG_TEST_UNICODE_FONT; fixture yêu cầu biến này như CI. Một lượt diagnostic riêng thiếu RG_MINIO_IMAGE và bị từ chối ngay tại OwnedWebMinio.StartAsync. Chạy lại với `RG_MINIO_IMAGE=roadguard-ci/minio:source-pinned` và font Unicode có trên Windows thì cả 15 ca đạt. Chỉ cải thiện thông báo assertion 403 để có server log khi fail; không đổi quyền production.

Full API được chạy bằng lệnh workflow `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v q --logger "trx;LogFileName=api.trx"`, dùng SQL/MinIO Testcontainers cô lập, SDK 10.0.401. Windows local dùng `DOTNET_PROCESSOR_COUNT=2` để giới hạn concurrency phù hợp Docker 7.6 GiB; `RG_TEST_UNICODE_FONT=C:\Windows\Fonts\arial.ttf` thay DejaVuSans trên Ubuntu runner. Đây là khác biệt môi trường được báo rõ, không sửa workflow, không bỏ test hoặc đổi assertion. Kết quả full API: **1695 PASS / 0 FAIL / 1 SKIP**, tổng 1696 test, thời gian 16 phút 44 giây. `Verify-TestResults.ps1 -AllowMinioSmokeSkip` PASS, xác nhận 1696 identity duy nhất; skip đúng test MinIO smoke cấu hình ngoài đã nêu. Không có ca fail còn lại của run base trong bản cuối. Kết quả này là local Windows với khác biệt môi trường ở trên, không gọi là CI PASS hoặc nghiệm thu FE. Không chạy lại full Unit/bốn SQL lane vì không đổi logic các nhóm đó; giữ bằng chứng CI base PASS và 6 SQL regression reason đã chạy.

Self-review bổ sung CI: đối chiếu toàn bộ ErrorInfo/stack của 5 ca với source, giữ exact-wire và thêm giá trị cụ thể; giữ schema/model/data preservation. Lượt hai rà staged diff, không có migration, gate/whitelist/workflow, endpoint hoặc quyền mới ngoài reason. Không thay production AI để chiều test cũ. Full API đã kiểm toàn bộ bản code/test cuối; OpenAPI chỉ thêm nullable reason. External review chưa thực hiện.
