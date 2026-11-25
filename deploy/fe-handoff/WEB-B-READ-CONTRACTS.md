# Web PM/Supervisor — matching, chuyển nghĩa vụ và lịch sử

Ngày 25/11/2026. Base `b891e88ea20073bab7b110f301b7b37ebfa1c40c`.
Quyết định mới: [owner 25/11](../../docs/product/owner-web-b-decisions-20261125.md).
Phạm vi triển khai: [spec](../../planning/development/WEB-B-READ-CONTRACTS.md).
`CURRENT_VERIFIED` trong bảng là source/kiểm BE, không phải nghiệm thu UI.
SHA bàn giao, runtime/image và số test cuối nằm trong biên bản bàn giao ngoài repo
và tin bàn giao kèm commit; không dùng source HEAD để suy runtime đã cập nhật.

## Bảng phạm vi

| Mã | Yêu cầu/căn cứ | Có sẵn | Bổ sung/sửa | Contract/quyền | Kiểm chứng và giới hạn | FE retest |
| --- | --- | --- | --- | --- | --- | --- |
| B1 | 33A + owner 25/11 | Project-only matching; AI segment; Case pin route/set; GPS từng ảnh | Xác nhận riêng có provenance; REPORT dùng segment đã xác nhận, topology neighbors; phân biệt GPS thiếu/chưa xác nhận/stale; cursor ràng buộc loại nguồn; khoảng cách WGS84 hợp lệ | PM hiện hành; không tự link/merge; GET/POST matching-location | HTTP/SQL: nhiều ảnh, có/không GPS, scope/neighbor/expand, explicit link/history, replay/CSRF/revocation, stale cursor; không có image matching. Khoảng cách hiện chỉ đo cặp WGS84 điểm bằng SQL geography; UTM/custom/native không có adapter đo authoritative thì null. Chưa có mô hình sai số đích nên không tự tạo overlap | Chọn evidence/segment có lý do, lấy version mới, tìm scope hoặc mở rộng rõ ràng |
| B3 | Owner: trách nhiệm hiện hành, không mở project A | Lifecycle → package/item; collection lọc trách nhiệm; guard item | Sửa currentBinding bị lọc theo project B; planning item và source-defect nhận B; package hỗn hợp chỉ metadata chung tối thiểu; sourceContext; đường đọc task/evidence giới hạn item | URL B, provenance A. Không đổi quyền mutation/assign/start hoặc cấp membership A | HTTP chuyển A→B, nguồn Reporter/Survey; ảnh Report và AI; sibling/task giả bị chặn; thu hồi membership chặn tải; quyền được kiểm lại sau mở stream. Storage ảnh trong ca này giả lập, SQL thật cô lập | Từ lifecycle lấy packageId/currentRepairItemId; đọc item/planning/source-defect, dùng contentPath được cấp |
| B6–B7 | Owner: discover đúng ID/version/lineage | FIELD submission immutable, review/reuse lưu riêng; repair attempts/final history | FIELD history thêm details cho PM/Sup; repair history thêm intakeSubmissions/attemptReviews/taskBindings | History ETag không thay item/task ETag; metadata không cấp quyền tải | HTTP submit → SUPPLEMENT → bổ sung → ACCEPT/final/correction; review cũ còn lịch sử nhưng current=false; reuse giữ owner/hash/fileVersion và replay một record | Reload history, mở submission qua ID; không gán review theo timestamp mới nhất |
| B5 | Owner: giữ quyền quan sát hiện hành | PM/Sup AI facts; matching/decision PM-only; designated FT notification source | Không mở quyền mới | Sup không được PM matching/decision history; không thêm Sup final FT. NORMAL final Sup, FT final PM | AI HTTP/SQL genuine MOCK fixture và notification tests; không AI thật, không broadcast Sup | Tách màn facts, PM quyết định và final; không coi Sup xem facts là quyền duyệt FT |
| B10 | Inventory/attach đã có ở 4284daf/d559e64 | GET/POST handover-documents | Không thêm route trùng | Chỉ Sup; file DOCUMENT VERIFIED đúng project; aggregate road-coverage ETag | LD07 HTTP/SQL inventory một/nhiều/rỗng, scope/version/replay/CSRF. HTTP storage mock | Inventory chọn documentId → upload VERIFIED → GET coverage mới → attach |
| B12 | Owner: lựa chọn khác lịch sử | Selection chỉ unresolved/chưa dùng; measure detail có checks/warnings/ACK | Collection safety-measures cho PM/Sup discover ID; detail thêm obligation scope/origin/current responsibility/version; Crew list giữ nguyên | Safety và formal có trách nhiệm riêng; đọc theo owner safety hiện hành, không tự transfer safety; writes giữ NORMAL guard | HTTP/SQL used/unused, formal transfer/transfer tiếp, Crew không lộ history mở rộng. Schema hiện không có workflow resolve TemporarySafety độc lập; không dựng dữ liệu resolve giả để tuyên bố đã chạy lifecycle đó. Reader nhận resolved cho PM/Sup nếu hồ sơ hợp lệ tồn tại; Crew vẫn không đọc resolved | Dropdown dùng collection chọn cũ; tab lịch sử dùng measureIds → detail |
| B15 | Owner: authoritative pins, không bypass | eligibility trả policy/hash/rules, sources/mapping/versions/missingReasons; start kiểm lại | Binding read sau transfer không còn mất policy pin | Eligibility preflight không là execution-ready; TEST_ONLY không được thi công | LD07/H4 policy/safety tests; thiếu nguồn, stale, source version, TEST_ONLY, quyền. Không tạo REAL_SOURCE hay ngưỡng mẫu | Hiển thị mã lý do thật, null giữ null; dùng nguồn/version đang pin |
| B13 | Source kết luận 200 | Runtime conclusion 200; publication 201 | Swagger kết luận đúng 200 InternalCaseDto | If-Match Case + key + quyền/CSRF giữ nguyên | HTTP Case regression và Swagger; không đổi status của mọi POST | Tách xử lý kết luận với công bố |
| B14 | Owner submit → confirm | `demo_ai.py` trước đây confirm trực tiếp sau create | PM submit; Sup GET slot/đối chiếu draft/content; confirm bằng ETag draft hiện hành; journal retry dùng request cũ | Không dùng ETag route/set/publication cho draft; trạng thái/round khác thì dừng | Python script contract + HTTP geometry workflow trên DB riêng. Không chạy bootstrap/scenario này lên DB dùng chung | Không chạy lại seed để sửa geometry của môi trường đã có |
| Exit 139 | Chưa có process/time/command/log/dump đúng sự cố | Stack lúc đầu task đang Exited(0), không phải bằng chứng exit139 | Không sửa API hoặc reset DB suy đoán | UNKNOWN | Cần container/process, UTC time, command, exit status, stderr/log và dump/tài nguyên đúng lúc sự cố | Cung cấp bằng chứng sự cố nếu tái diễn |

## B1 — xác nhận dùng cho matching

Root `/api/v1/projects/{projectId}/reports/{reportId}/matching-location`.
GET/POST: PM có quyền hiện tại trên Report/Case/project. Bearer hoặc Web session.
GET không CSRF; POST cookie cần `X-CSRF-TOKEN`, lấy qua
`GET /api/v1/auth/web/csrf` sau đăng nhập; `fetch` dùng `credentials: "include"`.
POST cần `Idempotency-Key`. Version nguồn/hình học nằm trong body, không dùng If-Match.

GET trả `reportId, sourceVersion, geometryVersion, status, confirmation, evidence`.
`evidence[]`: `evidenceId, latitude, longitude, accuracyMeters, locationSource`.
Không chọn GPS đại diện tự động. `0` là tọa độ thật; thiếu là `null`.
Lấy `evidenceId` từ GET này hoặc Case Reports; segment IDs từ geometry-package
của đúng routeVersionId/segmentSetId đã pin trên Case.

```json
{
  "sourceVersion": "<sourceVersion từ GET mới>",
  "geometryVersion": "<geometryVersion từ GET mới>",
  "evidenceId": "<ID chứng cứ có tọa độ đã lưu, hoặc null>",
  "segmentIds": ["<segment trong bộ đã pin>"],
  "reason": "Đã đối chiếu ảnh và phạm vi tuyến"
}
```

Chọn evidence, segment, hoặc cả hai; không được bỏ cả hai. Evidence phải thực sự
thuộc Report và có tọa độ. Không tạo điểm thay thế. Nhiều segment là lựa chọn rõ
của PM; không tự snap điểm vào segment. POST trả **200 cùng cấu trúc GET**,
`confirmation` có `id, sequence, actorId, confirmedAt, reason, evidenceId,
segmentIds, latitude, longitude, accuracyMeters`.
`status`: `GPS_MISSING`, `GPS_UNCONFIRMED`, `GPS_CONFIRMED`, `SCOPE_CONFIRMED`,
`MATCHING_LOCATION_STALE`. Confirmation cũ trong cùng project được trả để đối soát;
stale thì không dùng nó cho thứ tự matching. Xác nhận cũ của project khác không lộ.

Lưu request trước khi gửi; timeout đọc GET rồi retry **cùng key/body** để lấy receipt.
Replay kiểm quyền hiện hành; cùng key khác body trả `409 idempotency_key_reused`.
Body version cũ trả `409 candidate_stale`; sai segment `matching_scope_conflict`;
evidence sai/thiếu GPS `matching_evidence_location_unavailable`. Sai role/scope
`403/404`; thiếu key `428 precondition_required`; sai DTO `400 validation_error`.

GET `/api/v1/projects/{projectId}/candidates?sourceKind=REPORT&sourceId=...&expand=false&pageSize=50`:
confirmed segment → topology neighbors → (chỉ khi `expand=true`) phần còn lại cùng project.
Giữ thứ tự matcher hiện có trong mỗi nhóm; không ngưỡng loại ứng viên mới.
REPORT thêm `matchingLocationStatus, sourceEvidenceRefs`; `algorithmVersion=report-confirmed-1`.
Thiếu khoảng cách: `distanceMeters:null`; sai số nguồn chưa lưu: `accuracyMeters:null`.
`DISTANCE_SOURCE_UNAVAILABLE` khác `GPS_MISSING`/`GPS_UNCONFIRMED`.
GPS có nhưng chưa xác nhận không tính khoảng cách. Scope-only không bắt nhập GPS.
`sourceEvidenceRefs` để PM mở ảnh qua Case Reports đúng quyền; `items[].evidenceRefs`
chỉ chứa ảnh Report hiện tại khi có liên kết lịch sử thực với Defect đó.
Không gán ảnh của Report này cho Defect khác. AI matching giữ đường hiện hành.
Cursor cũ trả `409 candidate_stale` khi source/confirmation/geometry/target đổi;
tiếp trang giữ nguyên sourceKind/sourceId/project/expand.

## B3/B6–B7 — đọc đúng nguồn sau chuyển giao

Lifecycle của project B trả `packageId/currentRepairItemId` của obligation đã nhận.
Root item `I=/api/v1/projects/{B}/repair-packages/{packageId}/items/{itemId}`.

* `GET I`: `currentBinding` giữ task/assignment/policy revision/hash nguồn A;
  `sourceContext` có originProjectId=A, responsibilityProjectId=B, defectId/type/status/version,
  routeVersionId, physical scope và task pins. Không đổi provenance.
* `GET I/planning`, `GET I/source-defect`: chỉ đúng item. Source-defect trả
  `sources[]` và `evidence[].contentPath` đã kiểm quyền. Mất một phần nguồn ghi
  `missingSections` thay vì giả danh sách đầy đủ. ID nguồn không mở quyền GET toàn A.
* `GET /projects/{B}/repair-packages/{packageId}/planning`: package hỗn hợp chỉ ID/version;
  name/description/dueAt/creator để null. Collection items hiện hành lọc theo trách nhiệm.
* `GET I/source-evidence/{evidenceId}/content`: ảnh Report/AI theo active source,
  Report–Case hiện hành, VERIFIED/version/hash, genuine MOCK proof khi là AI.
  Không cấp URL public/signed, không thay quyền route file chung.
* `GET I/history`: `attempts/decisions/reviewRequests` cũ giữ nguyên, thêm
  `intakeSubmissions/attemptReviews/taskBindings` để discover đúng ID.
* Với task từ binding/history: `T=I/source-tasks/{taskId}`;
  GET `T/geometry`, `T/history`, `T/submissions/{submissionId}`,
  `T/evidence/{fileId}`, `T/evidence/{fileId}/content`. Chỉ task bind đúng item,
  kể cả binding lịch sử còn thuộc item; không chấp nhận task anh em.

Các GET trên dùng Bearer/Web, không cần CSRF; `401` thiếu/hết phiên,
`403` mất actor/membership/role, `404` sai resource/trách nhiệm/item/task,
`409` nguồn/phiên bản file không còn hợp lệ, content có thể `503 storage_unavailable`.
Mỗi lần tải kiểm quyền lại, thêm một lần sau khi mở storage; stream đã phát không
thu hồi byte đã gửi. Tải file không đồng nghĩa quyền sửa nguồn hoặc cấp quyền lâu dài.
PM B assign vẫn theo contract đã có: URL B nhưng Crew/policy/nguồn A; Crew chỉ ở B
không được tự cấp quyền A. Assign thành công không đồng nghĩa đủ điều kiện start.

Ví dụ phần history (ID minh họa, dùng ID thật từ response):

```json
{
  "intakeSubmissions": [{"id":"<link>","attemptId":"<attempt>","submissionId":"<bản bổ sung>","rootSubmissionId":"<root>","previousLinkId":"<link cũ>","taskId":"<task>","contentHash":"<hash>","effective":true}],
  "attemptReviews": [{"id":"<review cũ>","attemptId":"<attempt>","intakeLinkId":"<link cũ>","submissionId":"<bản cũ>","contentHash":"<hash cũ>","actorId":"<PM>","role":"ProjectManager","decision":"SUPPLEMENT","reason":"Cần ảnh bổ sung","at":"2026-11-25T00:00:00Z","current":false}]
}
```

FIELD `GET /projects/{projectId}/field-inspection-tasks/{taskId}/history` giữ
`assignments/events`; PM/Sup có thêm `details.submissions/reviews/evidenceReuse`.
Mỗi submission có root/parent/revision/hash, mỗi review có submissionId riêng;
reuse có sourceEvidenceId/fileId/hash/actor/time/reason và `provenance.fileVersion`.
Crew không nhận `details`. Dùng API-096 hoặc route source-task để mở bản cụ thể.
Không dùng readiness thay review. API-080 `attempt.review` vẫn là PM review hiện hành;
nộp bổ sung clear review cũ; finalDecision là nguồn riêng.

## B5/B12/B15 — quyền và version

AI candidate facts GET `/projects/{projectId}/ai-candidates/{detectionId}`: PM/Sup theo
scope hiện hành; matching và candidate-decisions vẫn PM-only. Fast Track final PM,
NORMAL final Sup. Notification chỉ người được source/assignment chỉ định, kiểm quyền
hiện hành khi resolve; cấu hình notification-supervisor do PM, không phải broadcast Sup.

Safety selection GET/POST `I/temporary-safety-obligations` không đổi:
formal NORMAL, unresolved, chưa có measure, đúng scope/trách nhiệm.
Lịch sử dùng GET `I/safety-measures` → `measureIds` → GET `I/safety-measures/{id}`.
PM/Sup chỉ thấy measure có safety obligation mình đang chịu trách nhiệm trong URL;
Crew vẫn chỉ assigned unresolved measures. Detail có checks/warnings/acknowledgements
và `obligation:{resolved,originProjectId,responsibilityProjectId,version,scope}`.
Sau formal chuyển tiếp, safety cũ có thể ở B: đọc safety bằng URL B, không chuyển theo formal.
Không có ca thực thi resolve-safety độc lập trong source hiện tại; không được tạo
trạng thái resolved bằng SQL để nghiệm thu lifecycle không có.

ETag/packageVersion cho append obligation; itemVersion cho tạo measure;
measure.Version cho installation/check/ACK; obligation.Version là version nghĩa vụ,
không dùng thay version item/measure. History hash là version representation,
không dùng cho mutation. Web mutation cần CSRF + key + If-Match đúng resource.

GET `I/eligibility`: dùng `policyRevisionId/policyContentHash/policyState/rules`,
`coverageMapping`, `warrantySources/handoverSources`, versions và missingReasons.
Không ép null thành UNKNOWN. `POLICY_UNAVAILABLE`, `PINNED_POLICY_SOURCE_MISMATCH`,
`ASSESSMENT_REQUIRED`, `CURRENT_EXECUTION_AUTHORITY_DENIED`,
`TEST_ONLY_SOURCE_NOT_EXECUTABLE`, mapping/source stale là mã thực, không phải lý do bypass.
Policy revision read giữ route/quyền cũ; PM discovery qua `/projects/{id}/repair-policies`.
Publish/eligibility không cho phép thi công; start kiểm lại policy/coverage/quyền,
original first-start và FT expiry. Không có nguồn đủ điều kiện là blocker dữ liệu.

## B10/B13/B14 — thao tác FE

GET `/projects/{id}/handover-documents` là inventory Sup (all ID ordered, empty hợp lệ),
**khác** `road-coverage.sources`. Có một/nhiều hồ sơ thiếu file vẫn discover được.
Hồ sơ đã có: POST cùng path `{fileId,handoverDocumentId}`; nhiều hồ sơ cần chọn ID rõ.
Chưa có hồ sơ: `{fileId,documentNo,handoverDate}`. Upload purpose DOCUMENT đúng project,
chờ VERIFIED → GET road-coverage lấy **aggregate ETag mới** → attach bằng key/If-Match.
Document ETag không thay aggregate. Không overwrite hồ sơ đã có file/đã mapping;
VERIFIED không là REAL_SOURCE. Xem [handover/coverage](PROJECT-HANDOVER-ROAD-COVERAGE.md).

POST `/cases/{caseId}/conclusions`: **200** InternalCaseDto + Case ETag; publication
vẫn **201**, không có tự công bố tiến độ chỉ vì conclusion. Headers/key/CSRF giữ nguyên.

Script tìm được: [demo_ai.py](demo_ai.py), được gọi bởi [scenarios.py](scenarios.py).
Đã sửa submit PM → Sup đọc slot/so draftId+submittedContentVersion → confirm bằng
draft ETag. Journal lưu body/key/If-Match trước gửi; retry uncertainty đối soát GET
rồi dùng request đã lưu. Round/state khác dừng, không tạo lệnh mới tùy ý.
Không chạy script/bootstrap này lên DB nghiệm thu đã có dữ liệu để “sửa” trạng thái.
Script/log exit139 bên ngoài chưa được cung cấp nên nguyên nhân crash chưa xác minh.

## Migration, triển khai và giới hạn

Migration mới `20261125053521_ReportMatchingLocationConfirmation` chỉ thêm bảng xác nhận,
FK/index và trigger bất biến. Không backfill hoặc sửa GPS/Report/case/task cũ.
Down từ chối khi bảng có lịch sử. Snapshot, sandbox catalog và Development Seeder
được đồng bộ; Seeder không tạo xác nhận vị trí giả. Không bật bootstrap để nâng cấp
DB đã seed. Backup/VERIFYONLY/recovery trước migration; giữ old image/volumes.

Thứ tự: backup và kiểm phục hồi → migration → backend có Git revision → health/SQL/storage
→ FE mở phiên Web mới và retest từng mục. Local HTTP/SQL không chứng minh UI F5,
AI thật, browser/MinIO E2E hoặc đầy đủ mọi happy path. Không có nghiệm thu FE tự động.
OpenAPI runtime `/swagger/v1/swagger.json`; snapshot [openapi.json](openapi.json) được
đồng bộ các operation/schema thực sự đổi, catalog 260 route lịch sử vẫn giữ lịch sử.

Bằng chứng SQL/HTTP giả lập storage phải đọc riêng với runtime thật. Kết quả rerun
có thể trùng test giữa các nhóm; không cộng số lần thực thi thành số test duy nhất.

## Kết quả local trên bản chốt

- API lượt rộng: **65 PASS / 2 FAIL / 0 SKIP** (67 ca). Hai lỗi là assertion
  algorithm REPORT cũ và thiếu mô tả header CSRF trong Swagger; đã sửa và kiểm lại.
- API kiểm lại: **8/8 PASS**, rồi **5/5 PASS** cho Swagger và quyền nguồn sau
  chỉnh cuối. Đối soát TRX theo identity và lấy kết quả thực thi mới nhất:
  **70 identity API đều PASS**; đây không phải full API suite.
- SQL tập trung migration/retention/geometry/Seeder/sandbox/safety/coverage/policy:
  **37 PASS / 0 FAIL / 0 SKIP**. Unit candidate/repair/retention:
  **289 PASS / 0 FAIL / 0 SKIP**. Script geometry: **4/4 PASS**.
- Build qua các lệnh test có analyzer bật; format toàn solution, 10 source-contract
  scripts, gate TRX của các lượt kiểm lại/SQL/unit và `git diff --check` đạt.
  Không sửa workflow, whitelist skip hoặc SQL lane manifest; identity các test SQL
  đã chỉnh được giữ nguyên. Full API và bốn SQL lane đầy đủ chưa chạy trong đợt này.
- HTTP test chạy pipeline BE với SQL cô lập; storage ảnh giả lập và AI MOCK có
  provenance fixture đã kiểm. Chưa kiểm UI, AI thật hoặc MinIO E2E cho đường đọc mới.
  Migration nâng cấp từ mốc liền trước, giữ Report cũ, parity/catalog và trigger bất
  biến đã kiểm riêng. Không chạy seed trên DB dùng chung.

CI của base `b891e88` đã PASS; kết quả đó không đại diện CI của commit mới.
SHA remote và trạng thái CI mới được ghi riêng tại lần bàn giao, không polling kéo dài.
