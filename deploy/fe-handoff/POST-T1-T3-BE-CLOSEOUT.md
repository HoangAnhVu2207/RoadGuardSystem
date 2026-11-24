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
