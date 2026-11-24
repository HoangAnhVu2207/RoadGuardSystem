# Đối chiếu BE sau T1–T3 — 24/11/2026

Base đã kiểm: `86e9f4e25f2e310f940e45c5cf60efd8d875a61a`. Đây là kết quả BE HTTP/test-host + SQL cô lập,
không phải nghiệm thu F5 trên UI hoặc AI thật. SHA/image runtime cuối và log chi tiết được bàn giao riêng sau tích hợp.

| Phát hiện cũ | Đã có trên base | Khoảng thiếu/hành động đợt này |
|---|---|---|
| FIELD 5/6 bị trigger chặn | Migration `20261123093000_FieldTaskMeasurementDimensions` từ `973e318` | Không thêm migration. Bổ sung HTTP GET readback cho từng mã và chạy lại upgrade/guard |
| AI candidate cookie 401, mất decision sau reload | Cookie action + `disposition.decisionId`, GET decision; đã có trên `973e318` | Kiểm lại cookie thật và phiên mới, giữ auth/wire hiện hành |
| MOCK thiếu chẩn đoán | `AiFailureDiagnostics`, log admission/worker, `originCorrelationId`, nullable `failure` | Chạy đường admission/worker/readback; không bỏ validation |
| Thiếu nguồn chọn TemporarySafety | GET/POST theo item từ `973e318` | Bàn giao đúng collection, không chọn FormalRepair từ road coverage |
| Hồ sơ tồn tại nhưng thiếu file không discover được | POST attach từ `d559e64`; sources chỉ là nguồn đủ điều kiện | **Thêm GET inventory Supervisor**, không sửa nguồn/guard attach |

## Authentication chung

Web dùng HTTPS và `credentials: "include"`. Lấy `requestToken` từ `GET /api/v1/auth/web/csrf`
trong cùng phiên và gửi `X-CSRF-TOKEN` cho POST. GET không cần CSRF. Bearer vẫn được hỗ trợ.
Không gửi đồng thời Authorization cũ và Web cookie: selector ưu tiên Authorization nếu header có mặt.
401 cần khôi phục phiên; 403 cần kiểm role/quyền hiện tại (hoặc CSRF đối với POST), không đổi ID để vượt scope.
Các lỗi dùng ProblemDetails với `code`, `correlationId`; giữ `X-Correlation-ID` để truy vết.

## 1. FIELD 1–6 và đường cập nhật DB

Mã 1 DepressionDepth, 2 SlabFaultingHeight, 3 ShoulderErosionExtent, 4 Area,
5 DepressionLength, 6 DepressionWidth. Hai mã mới là kích thước chỗ lõm, không suy từ AI.
Giữ policy/task bắt buộc, validation mm/cm/m cho LENGTH và m² cho AREA.

`POST /api/v1/projects/{projectId}/field-inspection-tasks` vẫn theo contract FIELD hiện hành;
`GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}` trả `requiredMeasurementType` đã lưu.
Type ngoài 1–6 bị 400 validation; source version/geometry sai bị conflict theo guard.
SQL trigger vẫn chặn bất biến/source sai bằng 51132/51133. SQL lỗi bất ngờ không được biến thành
validation chung: pipeline trả 500 `internal_error` đã lọc chi tiết nội bộ và correlation ID.
Không lấy lỗi cũ ở DB Hoàng làm bằng chứng schema local còn lỗi.

- DB chưa patch/chưa có history: backup, áp migration chain bằng EF theo repo, kiểm `OBJECT_DEFINITION`
  của trigger; không chỉ đếm history.
- DB đã patch thủ công nhưng chưa có history: lấy toàn bộ patch/trigger để diff; migration chính thức
  `CREATE OR ALTER` áp lại định nghĩa đầy đủ trong chain. Chỉ sau rehearsal/backup mới áp lên DB cần dùng.
  Không INSERT migration history để bỏ qua migration.
- DB có history nhưng định nghĩa trigger khác: EF không tự chạy lại migration. Lưu định nghĩa và chứng cứ
  drift, đối chiếu source và backup. Cần repair deployment hoặc migration forward riêng được review nếu
  thực sự xác nhận drift; không sửa migration đã phát hành hoặc xóa history. Đợt này không tạo repair trùng.

Chưa có nội dung patch máy Hoàng, nên **chưa xác minh tương đương patch đó**. Test patch tương đương trong
fixture chỉ chứng minh migration idempotent với định nghĩa chuẩn 1–6, không chứng minh mọi patch thủ công.

## 2. AI candidate → quyết định → Defect sau mất client state

1. Nếu mất cả run ID, dùng `GET /api/v1/projects/{projectId}/ai-mock-runs` (collection có phân trang
   theo contract discovery), chọn run của đúng dataset/task; không tự chọn run từ project khác.
2. `GET /api/v1/projects/{projectId}/ai-mock-runs/{runId}/result` trả `detections[].detectionId`.
3. `GET /api/v1/projects/{projectId}/ai-candidates/{detectionId}` trả facts authoritative,
   `disposition: {"decisionId":"...","version":"..."}` hoặc null và `dispositionVersion`.
4. PM gọi `GET /api/v1/projects/{projectId}/candidate-decisions/{decisionId}` để đọc quyết định
   `decision`, `defectId`, `version`, rồi GET defect theo quyền hiện tại.

Candidate/run/result cho PM có quyền và Supervisor theo guard nguồn. GET decision giữ contract **PM-only**;
không mở thêm quyền Supervisor vào decision/history. ETag candidate là `sourceVersion`, ETag quyết định là
version quyết định; không coi hai token là cùng resource. Nguồn/geometry/disposition đổi phải reload.
Đọc lại không tạo decision, receipt hoặc Defect mới. Không có disposition không có nghĩa đã KEEP_NEW.

Ví dụ request KEEP_NEW đã được kiểm qua HTTP (thay ID/version từ nguồn server):

```json
{"sourceKind":"AI_DETECTION","sourceId":"<detectionId>","sourceVersion":"<candidate.sourceVersion>","geometryVersion":"<candidate.geometryVersion>","decision":"KEEP_NEW","reason":"PM đã xem nguồn","classification":{"roadSectionVersionId":"<routeVersionId>","segmentId":null,"defectTypeCode":"CRACK","severity":"LOW"}}
```

POST `/api/v1/projects/{projectId}/candidate-decisions` cần Idempotency-Key và CSRF nếu cookie.
Response 201 có `id` (decision ID), `defectId`, `version`; retry giữ nguyên request/key và vẫn trả 201
theo receipt đã lưu, cùng ID, không tạo thêm quyết định. Không áp quy tắc replay 200 của TemporarySafety vào route này.
GET không tồn tại 404; nguồn chưa sẵn sàng 409 `source_not_ready`; stale geometry 409 `stale_geometry`.
Kiểm HTTP với phiên Web mới đã có trong `Anh02AiHttpTests`; UI browser F5 là NOT_RUN.

## 3. AI MOCK diagnostics

`POST /api/v1/projects/{projectId}/ai-mock-runs` cần PM, Idempotency-Key và CSRF với cookie.
`GET .../{runId}` có `originCorrelationId` và `failure` nullable. Lỗi asynchronous thuộc run được lưu;
correlation của lần GET có thể khác origin, không ghi đè origin bằng ID của readback.

| Code / reason | Step / field | FE/operator xử lý |
|---|---|---|
| `mock_fixture_source_mismatch` / `fixture_source_mismatch` hoặc `source_hash_mismatch` | theo diagnostic đã lưu / nguồn video | Dùng đúng bytes/checksum/kích thước/MIME fixture synthetic đã duyệt, không nhận video tùy ý |
| `ai_contract_invalid` / `fixture_version_unsupported` | `admission` / `fixtureVersion` | Chọn fixture version được hỗ trợ |
| `source_not_ready` / `source_lineage_invalid` | `admission` / `scope` | Reload đúng dataset/task/segment set/geometry |
| lỗi model / `model_not_released` | `admission` / `modelVersionId` | Chọn model Released đúng workflow |
| `ai_contract_invalid` / `fixture_missing` hoặc `fixture_hash_mismatch` | `mock_materialization` / trường do server trả | Khôi phục fixture version/hash trên BE; giữ run ID và originCorrelationId |
| `result_not_ready` / `result_pending` | `readback` | Đọc trạng thái run, chưa lấy result khi chưa SUCCEEDED |

Response còn có `recovery` theo vocabulary an toàn; không render raw SQL/stack trace. Warning
`AiMockRequestRejected` nối admission với correlation/step/reason; log worker nối run và origin.
Chỉ kiểm MOCK, không có bằng chứng độ chính xác hoặc kết nối AI thật. Các đường dẫn máy Hoàng và correlation
ID cũ không phải fixture local mặc định.

## 4. TemporarySafety đúng item, không lấy FormalRepair

GET/POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/temporary-safety-obligations`.
GET: PM có trách nhiệm hiện hành hoặc Supervisor theo guard; POST: PM hiện hành. Cookie/Bearer;
POST cookie cần CSRF. Contract hiện tại chỉ chấp nhận item formal **NORMAL** đủ điều kiện;
không mở rộng FastTrack hay bỏ guard để tạo safety.

GET trả `packageId`, `itemId`, `defectId`, `formalObligationId`, `responsibilityProjectId`,
`packageVersion`, `itemVersion`, `formalScope`, nullable `binding`, `items[]`.
Mỗi item có `safetyObligationId`, `kind: TEMPORARY_SAFETY`, `mandatory`, `scope`, `resolved`,
`originProjectId`, `responsibilityProjectId`, `version`. Chỉ trả nghĩa vụ chưa resolved/chưa dùng làm measure
và đúng package/Defect/phạm vi/trách nhiệm. `items: []` là inventory đủ quyền nhưng chưa có lựa chọn hợp lệ;
không thay cho lỗi 403/404/409.

POST dùng `scope` (physicalRoadId, routeVersionId, segmentSetId, layoutRevisionId, slabId,
fromMeters, toMeters, offsetFromMeters, offsetToMeters), `mandatory`, `reason`. Mét, tối đa 3 số lẻ;
scope phải nằm trong formal và giữ pins. GET ETag / `packageVersion` dùng cho If-Match POST thêm nghĩa vụ.
201 mới; replay 200 cùng ID, recheck quyền. Stale 409 `concurrency_conflict`, khác fingerprint
409 `idempotency_key_reused`; trùng vùng `active_scope_conflict`; sai scope `safety_obligation_source_conflict`;
mode/trạng thái không phù hợp `temporary_safety_not_allowed`; thiếu header 428 `precondition_required`.

Sau đó PM POST `.../items/{itemId}/safety-measures` với `formalRepairObligationId`, `safetyObligationId`
lấy từ chính collection, `responsibleActorId`, `checkSchedule`, `replacementCondition`, `removalCondition`,
`reason`. If-Match lúc này là **itemVersion** (không phải packageVersion); key mới cho thao tác mới.
Không dùng version nghĩa vụ hay task version thay itemVersion. Sau chuyển trách nhiệm, dùng project nhận
trong URL nhưng giữ package/Defect/geometry gốc; không suy quyền Crew từ URL project nhận.

Xem JSON đầy đủ đã kiểm trước đây và header trong [FIELD-MEASUREMENT-AND-TEMPORARY-SAFETY.md](FIELD-MEASUREMENT-AND-TEMPORARY-SAFETY.md).
Đợt này chạy lại fixture A→B, concurrency/replay, chọn đúng nghĩa vụ rồi tạo measure; không nhận đó là
full Crew installation/check E2E hay nghiệm thu FastTrack.

## 5. Inventory hồ sơ thiếu file và attach

GET mới `/api/v1/projects/{projectId}/handover-documents` chỉ Supervisor, Bearer hoặc cookie, không CSRF.
Trả toàn bộ `items` (ID theo thứ tự SQL uniqueidentifier), không phân trang; `fileId: null` được giữ.
Các trường `id`, `documentNo`, `handoverDate`, `fileId`, document `version`, `isMapped`, `canAttachFile`.
Không trả thông tin người dùng, URL file hoặc note ngoài nhu cầu chọn hồ sơ. `Cache-Control: no-store`.
Project không tồn tại với Supervisor hợp lệ 404; PM/role khác 403; thiếu phiên 401; chưa có hồ sơ 200 rỗng.

- Có hồ sơ: POST cùng path `{"fileId":"<verifiedFileId>","handoverDocumentId":"<items[].id>"}`.
- Chưa có hồ sơ: `{"fileId":"<verifiedFileId>","documentNo":"BG-2026-01","handoverDate":"2026-11-16"}`.
- Nhiều hồ sơ: phải cho chọn đúng ID; thiếu chọn trả 409 `handover_document_selection_required`.

Sau upload DOCUMENT đúng project/target và VERIFIED, GET road-coverage lấy **aggregate ETag** cho If-Match;
không dùng document version trong inventory. POST cần Idempotency-Key và CSRF khi cookie; 200 trả road coverage.
Retry cùng key/body/If-Match. Sai purpose/project/chưa verified: 409 `handover_file_not_ready`.
Đã file/mapping không được ghi đè: `handover_document_already_linked`. Stale: `concurrency_conflict`.
Attach VERIFIED không tự chứng minh REAL_SOURCE, không thay TEST_ONLY và không tự cho execution.
Chi tiết upload/CSRF/errors: [PROJECT-HANDOVER-ROAD-COVERAGE.md](PROJECT-HANDOVER-ROAD-COVERAGE.md).

## Kiểm chứng và triển khai

- Không có migration mới, không đổi SQL lane identity. Migration FIELD hiện hành vẫn cần có đủ chain.
- Build API tests với SDK 10.0.401, analyzer bật: 0 error (có warning hiện hữu).
- Lượt baseline: 11 pass / 1 fail / 0 skip; fail ở dossier do thiếu `RG_TEST_UNICODE_FONT` trong môi trường local.
- Sau cấu hình font Unicode và bổ sung discovery/readback: 11 pass / 0 fail / 0 skip.
  Sau tăng kiểm cookie KEEP_NEW/replay/ETag/SQL không trùng: chạy lại riêng `Anh02AiHttpTests`, 1 pass / 0 fail / 0 skip.
  Assertion mới về replay được đối chiếu receipt và test cũ: route candidate giữ 201, không phải 200.
  `Verify-TestResults.ps1` đạt cho từng TRX cuối; 10 script source contracts và format toàn solution đạt.
  Các lượt có test trùng, không cộng thành tổng test duy nhất. Full suite local/CI chưa được suy là PASS.
- HTTP là TestServer với pipeline cookie/CSRF/Bearer thật và SQL Server container cô lập. Storage/AI của
  các test này là fixture/mock; không nhận là MinIO upload/player hay UI E2E thật.
- Test identity: `FieldMeasurementTriggerHttpTests`, `LD07RoadCoverageHttpTests`,
  `TemporarySafetyObligationHttpTests`, `Anh02AiHttpTests`, `AiCandidateReadBoundaryAcceptanceTests`,
  `SandboxOfflineRouteCatalogTests`. Lượt đầu còn chạy các lớp AI read lifecycle/route.
- Hai self-review: contract/authority/transaction; scope/compatibility/docs. Không thay guard hoặc assertion cũ để PASS.
- Runtime triển khai sau tích hợp giữ DB, object/state/keys/TLS volume, tắt bootstrap/seed. `/health` là liveness;
  `/ready` vẫn 404 có chủ đích. Kiểm SQL/private bucket riêng. Bằng chứng runtime cuối được ghi ngoài repo.
- Hoàng retest: FIELD 5/6 và âm tính; PM cookie đọc candidate→decision sau mất state; MOCK reject/readback;
  chọn TemporarySafety đúng collection/version; discover hồ sơ thiếu file/nhiều hồ sơ→attach đúng ID.

## Follow-up CI Survey trên `4284daf` — 24/11/2026

Run develop [45363439324](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45363439324)
có API 1695 pass / 1 fail / 1 skip, tổng 1697. Cùng SHA, run huy-review
[45363393207](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45363393207)
có API 1696 pass / 0 fail / 1 skip. Unit, source contracts và toàn bộ SQL của hai run đều đạt.

TRX thật xác nhận lỗi `Anh01SurveyFlowTests.ManualAssessmentPartialBaselineSupplementAndRevocationPersistThroughHttp(withAccessPoint: True)`:
`InvalidOperationException: Sequence contains no elements`, tại dòng 764 của source `4284daf`,
truy vấn `SingleAsync` tìm H6 occurrence của assessment. Đây không phải lỗi HTTP/validation accessPoint.
Không có expected/actual của assertion: truy vấn đòi một occurrence nhưng sequence rỗng.

Nguyên nhân fixture: test dùng DB/hàng đợi toàn cục của collection Authentication, tự gọi dispatcher
tối đa 40/50 lần rồi đọc kết quả ngay, đồng thời factory vẫn bật `H6NotificationWorker`.
Message của class khác có thể chiếm lượt; consumer nền có thể giữ lease khi manual consumer báo IDLE
nhưng transaction occurrence chưa commit. Không được dùng một lượt chạy riêng xanh để phủ nhận race này.

Đã kiểm hai biến thể trên base: 2 pass. Reproducer riêng với 64 message đăng ký cũ hơn trước lượt
dispatch cuối tái hiện đúng exception; khi tắt consumer nền để kiểm soát lịch chạy, cả hai biến thể
đều fail ở cùng truy vấn occurrence. Reproducer/patch âm tính chỉ lưu ngoài repo và đã gỡ khỏi bản sửa.
Số backlog/lease chính xác của máy CI lúc fail không được TRX ghi lại; không suy ra con số đó từ reproducer.

Bản sửa chỉ trong test: collection SQL riêng cho flow Survey, vẫn `DisableParallelization = true`;
gỡ đúng registration `H6NotificationWorker` khỏi factory của scenario đang chủ động chạy dispatcher.
Assert worker được đăng ký trước khi gỡ; giữ dispatcher/repository thật, mọi assertion, hai biến thể,
các giới hạn dispatch và kiểm quyền/ETag/replay. Không đổi worker production hoặc factory chung.
Nhóm Survey + notification wire/scope/quyền sau sửa: **47 pass / 0 fail / 0 skip**.

Ca skip của run cũ là `UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`,
lý do chưa bật `ROADGUARD_MINIO_SMOKE=1` và cấu hình provider ngoài. Đúng whitelist hiện hành của
`Verify-TestResults.ps1 -AllowMinioSmokeSkip`; không coi skip là PASS và không đổi whitelist/gate.
11 ca T1–T3 đã báo là 11 identity duy nhất; AI rerun 1/1 trùng một identity, hợp vẫn là 11.

Full API local trên Docker 8 GiB với parallelism mặc định của máy Windows đã gặp thiếu tài nguyên
và được ngắt; log giữ riêng, không gọi là PASS. Các container test đúng session đã được dọn,
không xóa volume. SQL local bị OOM-kill 137 trong lượt đó và đã được phục hồi bằng container cũ,
không seed/reset. Lượt full tiếp theo dùng `DOTNET_PROCESSOR_COUNT=2` để giới hạn số fixture đồng thời,
không filter/skip/thay assertion. Kết quả full: **1696 pass / 0 fail / 1 skip / tổng 1697**,
17 phút 34 giây; gate TRX đạt với đúng một MinIO smoke tùy chọn nêu trên. 47 ca tập trung nằm trong
full suite, không cộng thành 1744 ca duy nhất. Đây là cấu hình tài nguyên local, không sửa workflow CI Ubuntu.

Không có thay đổi production, migration hoặc wire/OpenAPI trong follow-up này. Không rebuild/restart API
để đưa test/doc lên runtime; API local vẫn là `4284daf` với đủ 13 migration. Các kết quả HTTP/SQL T1–T3
vẫn giữ phạm vi test-host/SQL cô lập; smoke Docker chỉ chứng minh các lần đọc đã ghi nhận, không phải
UI F5, AI thật hay toàn bộ happy path nghiệp vụ. Kết quả full API và CI SHA sửa cuối được bàn giao riêng.
