# Web PM/Supervisor — assign, chuyển nghĩa vụ, retention và loại đo FIELD

Base `4934103d125be1caca4c6a588eed3768c9cafee4`, nhánh local `codex/assign-retention-field-contracts`. Owner đã cho phép push huy-review rồi fast-forward develop cùng SHA, không chờ CI; cập nhật nhánh không đồng nghĩa CI PASS. Source chưa được deploy lên backend FE. Không có migration mới; không sửa DB/seed dùng chung hoặc repo FE.

Các giá trị `<...>` dưới đây là placeholder, phải thay bằng ID/version đã đọc từ server. JSON mẫu không phải dữ liệu sandbox FE. Evidence kiểm thử local được ghi cuối tài liệu; không đồng nghĩa nghiệm thu app/browser.

## 1. Transport, ID và version

Prefix `/api/v1`. Web PM/Sup dùng `/auth/web/login`, lấy `requestToken` qua `GET /auth/web/csrf`; `fetch` gửi `credentials: "include"`. Lệnh ghi dùng cookie cần `X-CSRF-TOKEN`. GET không cần CSRF. Bearer vẫn là lựa chọn riêng; không gửi đồng thời cookie của actor khác. Crew tiếp tục dùng Bearer.

Assign, policy và lifecycle dùng `Idempotency-Key` cùng `If-Match` của đúng resource. Retry khi chưa rõ kết quả phải giữ nguyên key/body/If-Match; replay vẫn kiểm quyền hiện hành. Sau stale/conflict, tải lại rồi người dùng xác nhận trước khi tạo key mới. Không lấy task version thay item version hoặc ngược lại.

| ID/version | Nguồn đọc được cấp quyền |
|---|---|
| packageId/itemId trước chuyển | `GET /projects/{A}/repair-packages`, rồi `GET /projects/{A}/repair-packages/{packageId}/items` |
| packageId/itemId sau tiếp nhận | `GET /projects/{B}/lifecycle` → `responsibilities[]` khớp `obligationId` và `currentProjectId=B`; đọc `packageId` và `currentRepairItemId` **mới**, nullable nếu không còn đầu hiện hành. Sau đó đọc collection items dưới B hoặc GET item |
| item version | `GET /projects/{currentResponsibilityProject}/repair-packages/{packageId}/items/{itemId}` → `version`/ETag |
| defectId/version | GET defect/source-defect trong project nguồn mà actor có quyền. `task.defectVersion` là version của Defect, không phải item version |
| Survey ID | **Mới:** `GET /datasets/{datasetId}` → `surveyId` thật từ `SurveyDataVersion.SurveyId`; khác `id`/`dataVersionId` (dataset) và `surveyTaskId` (task). Trả nullable cho adapter không có giá trị; không đoán nếu null |
| datasetId | `GET /survey-tasks/{taskId}/datasets` → chọn `items[].id`; kết quả AI run/candidate cũng chứa `datasetVersionId` |
| route/set/slab/layout/map/CRS | Geometry package, layout/publication và source đã pin của task/obligation; giữ nguyên cả bộ pin. `GET /projects/{A}/geometry-package` cần routeVersionId/segmentSetId đã được cấp quyền |
| policyRevisionId | PM project nguồn: `/projects/{A}/repair-policies?applicableOnly=true&defectTypeCode=...`, rồi `/revisions/{id}`. Dùng published revision, không dùng draft ID; đối chiếu checklist của item |
| Crew ID | Thành viên Crew còn active và còn hạn trong **project task nguồn A**. Chuyển nghĩa vụ không tự tạo membership của Crew tại A |
| scopeHash/lifecycle version | GET lifecycle mới nhất; chọn đúng `transferableObligations[].id`, `scopeHash`, ETag của từng bên |

Package có nhiều nghĩa vụ, một phần đã chuyển: collection package tổng hợp có thể không trả package để tránh lộ trạng thái nghĩa vụ thuộc project khác. Đây không phải package bị xóa. Dùng responsibility mới để mở đúng items của B. GET item giữ `projectId` gốc A như provenance; URL vẫn dùng B để kiểm trách nhiệm PM hiện tại.

**Giới hạn còn cần quyết định nghiệp vụ:** chuyển nghĩa vụ không cấp cho PM B quyền đọc mọi defect/geometry/dataset/ảnh/policy của A. Sau chuyển, B có thể tiếp tục assignment với bộ reference nguồn đã được bàn giao và Crew có quyền A; source-defect và collection policy vẫn theo quyền source hiện hành. Chưa có quyết định cấp B quyền khám phá toàn project A hoặc cho Crew chỉ thuộc B làm task A. Không dùng SQL/ID đoán, policy B tùy ý hay membership giả để vượt guard. Nếu Web cần tự khám phá lại toàn bộ pin nguồn ở B mà chưa có bộ reference bàn giao, cần chốt read scope tối thiểu cho nguồn task/obligation trước; bản này không nới quyền ảnh/file hoặc quyền project nguồn.

## 2. Assign Fast Track

`POST /projects/{currentProjectId}/repair-packages/{packageId}/items/{itemId}/assign`

Chỉ PM hiện hành có membership trong project chịu trách nhiệm. `If-Match: "<item.version>"`, `Idempotency-Key: <key>`; Web thêm CSRF. Body nguồn REPORTER (đầy đủ field task, pin không dùng có thể null):

```json
{
  "task": {
    "defectId": "<defectId>", "defectVersion": "<defect.version>",
    "surveyId": null, "sourceKind": "REPORTER",
    "routeVersionId": "<originRouteVersionId>", "segmentSetId": "<publishedSegmentSetId>",
    "layoutRevisionId": null, "slabId": null, "mapPublicationId": null,
    "crsProfileRevisionId": null, "purpose": "POST_REPAIR", "requiredMeasurementType": 1,
    "measurementScope": "{}", "instructions": "Đo lại theo checklist đã chốt",
    "assignedToUserId": "<crewIdWithOriginProjectRights>", "dueAt": "2026-12-01T08:00:00Z"
  },
  "policyRevisionId": "<publishedOriginPolicyRevisionId>", "reason": "Giao thi công Fast Track"
}
```

Nguồn SURVEY dùng nguyên cấu trúc trên nhưng `sourceKind: "SURVEY"` và `surveyId: "<GET-dataset.surveyId>"`. Không gửi ID giả cho REPORTER; không gửi task/dataset ID vào surveyId. Có layout/slab/map/profile thì gửi đúng các giá trị đã pin, không xóa pin để làm request qua guard.

Body SURVEY đầy đủ (cùng endpoint, các placeholder nguồn phải thuộc cùng lineage):

```json
{
  "task": {
    "defectId": "<aiDefectId>", "defectVersion": "<aiDefect.version>",
    "surveyId": "<GET-dataset.surveyId>", "sourceKind": "SURVEY",
    "routeVersionId": "<originRouteVersionId>", "segmentSetId": "<publishedSegmentSetId>",
    "layoutRevisionId": null, "slabId": null, "mapPublicationId": null,
    "crsProfileRevisionId": null, "purpose": "POST_REPAIR", "requiredMeasurementType": 1,
    "measurementScope": "{}", "instructions": "Đo lại theo checklist đã chốt",
    "assignedToUserId": "<crewIdWithOriginProjectRights>", "dueAt": "2026-12-01T08:00:00Z"
  },
  "policyRevisionId": "<publishedOriginPolicyRevisionId>", "reason": "Giao thi công Fast Track"
}
```

Response lần mới `201`, replay thành công `200`; ETag là **item version mới**:

```json
{
  "itemId": "<itemId>", "obligationId": "<obligationId>", "taskId": "<fieldTaskId>",
  "assignmentId": "<assignmentId>", "mode": "CONDITIONAL_FT",
  "authorizationId": "<executionAuthorizationId>", "policyRevisionId": "<revisionId>",
  "routeVersionId": "<originRouteVersionId>", "segmentSetId": "<segmentSetId>",
  "layoutRevisionId": null, "mapPublicationId": null, "crsProfileRevisionId": null,
  "slabId": null, "firstStartId": null
}
```

`RepairMode.FastTrack` trong item dùng wire `FAST_TRACK`; task/binding response dùng `CONDITIONAL_FT`. NORMAL có authorization/policy null và phải qua Supervisor approve đúng plan. Không đổi các mã này.

Guard thực tế, theo thứ tự chính (`RepairProducerService` → `RepairWorkflowRepository.AssignItemAsync` → FIELD validators):

1. PM, current membership/actor và quyền tài nguyên được kiểm trước receipt/replay. Item đúng package/obligation/current responsibility; original Defect vẫn ở A.
2. Item rowversion đúng, `ReadyToAssign`, chưa có `CurrentBindingId`, là `CurrentRepairItemId` của obligation. Defect ID/version và route phải khớp anchor; giữ ràng buộc trạng thái nguồn của `FreshAnchor`.
3. `POST_REPAIR`, numeric measurement 1–6. REPORTER: survey null, active DefectSourceLink và quyết định `KeepNew` nguồn Report. SURVEY: Survey thuộc project/route gốc; active source decision xác nhận detection; detection → processing job → processing block → dataset → đúng Survey.
4. Route thuộc A; segment set đúng route và `PUBLISHED`; layout/slab tồn tại trong snapshot cùng bộ pin; map publication và CRS profile khớp. Hash frame các pin phải khớp `obligation.Scope.LocationVersion`.
5. Crew còn quyền A. Fast Track cần policy của A đã publish qua draft/change hiện hành, chưa revoke, đúng defect type và item checklist; sau first start cũ, regrant có thể trả `execution_regrant_policy_pending`.
6. Task, assignment, authorization, binding, item head, audit và receipt được ghi cùng transaction. Không chuyển project ID lịch sử sang B.

**Assign thành công không có nghĩa sẵn sàng thi công.** Crew đọc API-080, accept/start FIELD rồi assessment; đọc `/.../items/{id}/eligibility`, chỉ khi toàn bộ guard measurement, location, policy, coverage, thời gian và provenance đạt mới execution-start. FIELD start không phải execution-start. Mapping TEST_ONLY không cấp execution; không đổi nó thành REAL_SOURCE.

### Chuyển nghĩa vụ A → B

1. Supervisor GET lifecycle A; POST `/projects/{A}/lifecycle/obligation-transfers`, body `reason`, `evidenceFileIds:[]`, `obligationId`, `receivingProjectId:B`, `scopeHash`; ETag của A.
2. Supervisor GET lifecycle B; POST `/projects/{B}/lifecycle/obligation-transfer-acceptances`, body `reason`, `evidenceFileIds:[]`, `sourceActionId:<IssueTransfer action.id>`, `obligationId`, `scopeHash`; ETag B.
3. Đọc lifecycle A/B. Responsibility có origin A/current B; B dùng `packageId/currentRepairItemId` để mở item. PM A không còn ghi item đã chuyển. B assign dưới URL B, body vẫn pin defect/route/Survey/policy A.
4. Task/assignment/binding/auth vẫn project A theo contract bảo toàn Crew/clock/provenance; Crew đã được giao và còn quyền A mở `/projects/{A}/field-inspection-tasks/{taskId}`. Crew chỉ có B hiện bị `403 assignee_not_authorized` khi assign. Quyền mới cho Crew B chưa được owner chốt, không tự thay hàng loạt ProjectId.

### Kết quả assign trước/sau tiếp nhận — phân biệt với discovery

Ca HTTP `ReporterAndSurveyAssignmentsPreserveOriginAfterAcceptedTransfer` chạy riêng hai nguồn REPORTER và SURVEY trên SQL container cô lập. Nghĩa vụ thứ nhất kiểm assign trước chuyển; nghĩa vụ thứ hai của cùng package chưa có binding/first-start, được chuyển rồi mới assign. Không dùng task đã assign để giả lập một lần giao mới sau chuyển.

| Trường hợp | HTTP thực được kiểm | Kết luận / guard |
|---|---|---|
| PM A giao trước chuyển, Crew A, policy A | POST assign `201`; cùng key/body/version replay `200` | Assign thành công, không chỉ đọc được item |
| Supervisor phát hành A → B; B tiếp nhận | Hai POST lifecycle đều `201` | Chuyển đúng obligation qua workflow chính thức |
| PM B khám phá sau tiếp nhận | GET lifecycle B `200`; lấy `responsibilities[].packageId/currentRepairItemId`; GET item dưới B `200` | Đây chỉ là discovery. ETag của chính GET item được dùng cho assign tiếp theo |
| PM B giao mới sau tiếp nhận, Crew A, policy A | POST assign dưới B `201`; replay cùng request `200` | **Assign Fast Track thành công cho cả REPORTER và SURVEY**. Body trả binding `CONDITIONAL_FT`, task/assignment/authorization/policy; SQL đối chiếu task/assignment/authorization với binding đúng item |
| PM B dùng Crew chỉ thuộc B | POST assign dưới B `403 assignee_not_authorized` | `ValidateRepairTaskSourcesInTransactionAsync` → `GuardCrewAsync(package.ProjectId=A, crew)`: thiếu membership Crew active trong project nguồn A; giới hạn scope hiện hành, không phải lỗi discovery |
| PM A tiếp tục assign item đã chuyển | POST assign dưới A `404` | `GuardItemResource` kiểm effective responsibility B; không cấp lại quyền PM A vì provenance A |
| Crew A mở và nhận task vừa tạo | GET task dưới A `200`; POST accept `201` | Chứng minh task/binding sau assign dùng được theo quyền Crew gốc. Không chứng minh execution eligibility đạt |
| PM B hoặc Crew A mất membership tương ứng | GET item/task `403` | Vẫn kiểm quyền hiện hành; không dùng receipt/binding để vượt quyền |

Request sau tiếp nhận (placeholder, không phải ID thật của môi trường FE):

```http
POST /api/v1/projects/<B>/repair-packages/<lifecycle.packageId>/items/<lifecycle.currentRepairItemId>/assign
Cookie: <phiên Web PM B>
X-CSRF-TOKEN: <requestToken của phiên PM B>
If-Match: "<ETag-value từ GET item dưới B>"
Idempotency-Key: <key giữ nguyên khi retry>
Content-Type: application/json
```

```json
{
  "task": {
    "defectId": "<defectA.id>", "defectVersion": "<defectA.version>",
    "surveyId": null, "sourceKind": "REPORTER",
    "routeVersionId": "<routeVersionA.id>", "segmentSetId": "<publishedSegmentSetA.id>",
    "layoutRevisionId": null, "slabId": null, "mapPublicationId": null,
    "crsProfileRevisionId": null, "purpose": "POST_REPAIR", "requiredMeasurementType": 1,
    "measurementScope": "{}", "instructions": null,
    "assignedToUserId": "<Crew còn membership active tại A>",
    "dueAt": "2026-12-01T08:00:00Z"
  },
  "policyRevisionId": "<publishedPolicyRevisionA.id>", "reason": "Giao item đã tiếp nhận"
}
```

Với nguồn SURVEY, dùng body SURVEY đầy đủ ở mục 2, vẫn POST URL B, đổi sang defect AI tại A và `surveyId` thật từ dataset A; mọi pin/policy vẫn thuộc A. Response của cả hai nhánh sau chuyển:

```http
HTTP/1.1 201 Created
ETag: "<item version mới sau assign>"
```

```json
{
  "itemId": "<currentRepairItemId đã lấy từ lifecycle B>",
  "obligationId": "<obligation đã được B tiếp nhận>",
  "taskId": "<task mới thuộc A>", "assignmentId": "<assignment mới của Crew A>",
  "mode": "CONDITIONAL_FT", "authorizationId": "<authorization mới thuộc A>",
  "policyRevisionId": "<publishedPolicyRevisionA.id>",
  "routeVersionId": "<routeVersionA.id>", "segmentSetId": "<publishedSegmentSetA.id>",
  "layoutRevisionId": null, "mapPublicationId": null, "crsProfileRevisionId": null,
  "slabId": null, "firstStartId": null
}
```

Nguồn/version của ca kiểm: Defect và rowversion đang lưu ở A; REPORTER có active Report→KEEP_NEW lineage, không có Survey. SURVEY có detection→job→block→dataset→Survey tại A; GET dataset trả Survey ID, không dùng task/dataset ID. Route và segment set PUBLISHED tại A, đúng frame của obligation; fixture không pin layout/slab/map/CRS profile nên các field đó null. Policy revision được PM A publish, còn hiệu lực, khớp defect type/checklist. B lấy **item ETag sau tiếp nhận từ HTTP GET item**, không tái dùng task version hay lifecycle ETag. Test giữ bộ reference nguồn A hợp lệ; việc PM B tự khám phá lại toàn bộ nguồn A chưa được cấp thêm quyền trong đợt này.

Đường tiếp tục đã chứng minh: B assign bằng nguồn/policy A hợp lệ → Crew có quyền A mở và accept task A. Trường hợp obligation đã có first-start trước đó không nằm trong ca giao mới này: guard `execution_regrant_policy_pending` vẫn được giữ; không kết luận regrant đã hỗ trợ từ kết quả trên. Crew chỉ thuộc B, policy B hoặc cấp PM B quyền toàn project A không được mở để vượt guard.

| HTTP/code | FE xử lý |
|---|---|
| 400 `validation_error` | Kiểm shape, code đo 1–6, sourceKind, UUID và POST_REPAIR; không gửi string enum vào requiredMeasurementType |
| 401 | Đăng nhập lại đúng transport |
| 403 `access_forbidden` / `assignee_not_authorized` | Kiểm PM membership, Crew source membership, role/CSRF; không retry vô hạn |
| 404 `not_found` | Sai package/item/project trách nhiệm hoặc mất quyền; đọc lifecycle mới |
| 409 `concurrency_conflict` | Tải item/Defect mới, xác nhận lại trước key mới |
| 409 `repair_source_conflict` | Kiểm defect ID/version/route |
| 409 `source_not_ready` | Kiểm active KEEP_NEW lineage, Survey ID và chuỗi detection/dataset; không kết luận chung thiếu surveyId |
| 409 `geometry_version_mismatch`, `repair_scope_frame_mismatch` | Tải đúng pin geometry và scope frame, không trộn revision |
| 409 `published_policy_source_required` | Policy chưa publish/revoke/sai project/type/checklist; reload revision |
| 409 `invalid_state_transition` | Item không ReadyToAssign/binding đã tồn tại; đọc item, không assign trùng |
| 409 `execution_regrant_policy_pending` | Dừng bước phụ thuộc regrant; không tạo grant mới để né |
| 428 `precondition_required` | Bổ sung key và If-Match item |

## 3. Retention inventory và basis

`GET /projects/{projectId}/retention/files/{fileId}`: PM/Sup theo scope file; file private có thể chỉ Supervisor thấy reference chéo scope. `inventoryComplete` là kết quả contributor/lineage do server tính, không có nút/API ép true. `evaluation.status: COMPLETE` chỉ nói job đã xử lý hết, không nói tất cả file có inventory đầy đủ hoặc được xóa.

```json
{
  "fileId":"<fileId>", "basisHeadId":null, "basisVersion":"none",
  "inventoryVersion":"<inventoryHash>", "inventoryComplete":false, "classification":"EVIDENCE",
  "references":[{"kind":"HANDOVER_DOCUMENT","id":"<handoverId>","projectId":"<projectId>","sourceVersion":"<rowversion>"}],
  "applicableObligations":[], "inventoryReasonCodes":["WARRANTY_OBLIGATION_MISSING"],
  "activeHoldCount":0,
  "evaluation":{"fileId":"<fileId>","eligibility":"WAITING_RETENTION_BASIS","reasonCodes":["WARRANTY_OBLIGATION_MISSING"],"eligibleAfter":null,"basisVersion":"none","inventoryVersion":"<inventoryHash>","holdVersion":"<holdHash>","isCurrent":true}
}
```

Mẫu trên minh họa shape, không khẳng định reasonCodes evaluator chỉ chứa một mã. Sau bổ sung dữ liệu, GET lại để dùng giá trị thực tế. ETag của GET là basis version (`"none"` khi chưa có head), không phải inventory hash.

Supervisor PUT `/projects/{projectId}/retention/files/{fileId}/basis`, Web cookie + CSRF; `Idempotency-Key`, `If-Match: "<GET.basisVersion>"`:

```json
{"warrantyIds":["<all-applicableObligations[].id>"],"expectedReferenceInventoryVersion":"<GET.inventoryVersion>","reason":"Xác minh toàn bộ nguồn và nghĩa vụ hiện hành"}
```

Trả `200 RetentionBasisView`: `id,fileId,revision,policyVersion,classification,referenceInventoryVersion,inventoryComplete,warrantyRefs,confirmedBy,confirmedAt,reason,supersedesId,version`; ETag là head version mới. Mở phiên Web mới GET lại kiểm `basisHeadId`, `basisVersion`, `inventoryVersion` và evaluation. Basis không sửa file/lineage, không làm inventory từ thiếu thành đủ. Đầy đủ vẫn cần basis khớp inventory, thời hạn và không có hold mới đủ xét xóa; đợt này không thực hiện xóa.

| Reason | Điều kiện/nguồn | Ai và hướng xử lý |
|---|---|---|
| `HUY_REFERENCE_INVENTORY_UNAVAILABLE`, `HUY_REPAIR_REFERENCE_UNAVAILABLE`, `AI_REFERENCE_INVENTORY_UNAVAILABLE`, `EXPORT_REFERENCE_INVENTORY_UNAVAILABLE` | Thiếu contributor theo tên HUY/HUY02_INSPECTION/AI/EXPORT | BE kiểm DI/reader; FE không có API đặt complete. Source bản này đã đăng ký đủ các reader |
| `<contributor>_INVENTORY_INCOMPLETE` | Reader tự báo incomplete | BE đối chiếu các reason chi tiết bên dưới; không bỏ reader |
| `LEGACY_DATASET_MANIFEST_UNRESOLVED` | Dataset manifest không phải array canonical, JSON lỗi, fileId sai/trùng/thiếu | references có `UNRESOLVED_DATASET` và dataset ID/version. BE/data owner xác minh nguồn lịch sử. Không sửa DB sandbox hoặc đoán file. Một dataset project khác vẫn chặn vì manifest mơ hồ không chứng minh file không được tham chiếu; không bỏ qua chỉ vì project khác |
| `OBLIGATION_SCOPE_UNRESOLVED` | Không có reference hoặc mọi reference project null | Cần hoàn thiện liên kết nguồn bằng workflow thực (Report triage/link, handover attach…); Supervisor/PM đúng quyền. Không gán project tùy tiện cho ảnh private |
| `WARRANTY_OBLIGATION_MISSING` | Project được tham chiếu thiếu warranty cần tính retention | Supervisor tạo `/projects/{id}/warranties` với hồ sơ/ngày/scope hợp lệ; không dùng basis thay warranty |
| `WARRANTY_OBLIGATION_UNRESOLVED` | Scope Unknown/Other/ContractItem, ngày thiếu hoặc end trước start | Owner dữ liệu xác minh hồ sơ; workflow warranty đúng scope nếu được phép. Không tự coi legacy hợp lệ |
| `HUY02_INSPECTION_PROVENANCE_UNRESOLVED`, `H3_FIELD_EVIDENCE_PROVENANCE_UNRESOLVED`, `H3_FIELD_REUSE_PROVENANCE_UNRESOLVED`, `H4_H5_EVIDENCE_PROVENANCE_UNRESOLVED` | Sai/thiếu session/task/submission/reuse/repair/offline provenance | BE kiểm reader với reference cụ thể; nghiệp vụ dùng đúng FIELD/repair/upload workflow, không giả capture/assignment |
| `LIFECYCLE_EVIDENCE_PROVENANCE_UNRESOLVED` | Checksum lifecycle evidence không còn khớp file | BE/data owner điều tra; bảo toàn lịch sử, không đổi checksum cũ |
| `PAVEMENT_SOURCE_CONTENT_VERSION_UNRESOLVED` | Nội dung/version nguồn pavement không khớp | BE/data owner đối chiếu layout/publication bất biến |
| `EXPORT_COMPLETION_UNRESOLVED` | Artifact export chưa có completion được xác nhận | Kiểm trạng thái/worker export; không ép completion |
| `OTHER_SCOPE_OBLIGATIONS` | PM đọc file có reference project khác | Supervisor đối chiếu toàn bộ nghĩa vụ; không làm mất reference chéo project |

PUT basis: thiếu key/If-Match `428 validation_error`; key quá 128 ký tự `422 validation_error`; If-Match sai định dạng `412 concurrency_conflict`; PM ghi `403 access_forbidden`; thiếu scope `404`; ETag cũ `412 concurrency_conflict`; inventory đổi `409 retention_inventory_stale`; incomplete `409 retention_inventory_incomplete`; bỏ thiếu/thừa obligation `422 basis_invalid`; cùng key khác input `409 duplicate_request`. Retry cùng request không cập nhật inventory/basis mới; sau conflict GET lại và xác nhận trước key mới.

## 4. requiredMeasurementType: enum kỹ thuật cố định

| Mã số | Enum / nhãn | Dimension | Unit |
|---:|---|---|---|
| 1 | DepressionDepth — độ sâu chỗ lõm | LENGTH | mm, cm, m |
| 2 | SlabFaultingHeight — chênh cao tấm | LENGTH | mm, cm, m |
| 3 | ShoulderErosionExtent — kích thước xói lề | LENGTH | mm, cm, m |
| 4 | Area — diện tích | AREA | m² |
| 5 | DepressionLength — chiều dài chỗ lõm | LENGTH | mm, cm, m |
| 6 | DepressionWidth — chiều rộng chỗ lõm | LENGTH | mm, cm, m |

`FieldTaskCreateInput.requiredMeasurementType` nhận **số** 1–6. 0 (Unknown), ngoài enum hoặc string tên enum bị 400 cho task operational mới; repair assign đã có guard 1–6. Bản này bổ sung Range và kiểm domain/repository để tạo FIELD chung không rơi xuống lỗi DB với mã ngoài khoảng. Không backfill hoặc đổi mã 1–6 đã lưu. DB task/measurement và migration `20261113051802_CrewFieldMeasurementDimensions` hiện hành vẫn giữ; không có migration mới.

Không có API discovery/CRUD loại đo; FE khai báo mapping cố định trên theo contract. Khác với defect types có catalog/admin API. Loại mới cần sửa code, validator, constraint/migration khi cần, các producer/adapters và contract, không tạo mã tùy ý trên UI.

Tạo task đo FIELD: `POST /projects/{projectId}/field-inspection-tasks`, body `FieldTaskCreateInput` như `task` ở mẫu assign nhưng `purpose:"PRE_MEASUREMENT"` (hoặc mục đích hợp lệ) và `requiredMeasurementType:5`; GET API-080 trả `requiredMeasurementType:5`. Scope quyền PM, Crew assignment và header task tuân FIELD hiện hành. Assign repair dùng `POST_REPAIR`, không thay bằng endpoint task đo chung để có execution grant.

Một required type không giới hạn submission ở một measurement. API-095 `POST /projects/{projectId}/field-inspection-tasks/{taskId}/submissions`: Bearer Crew, key, `If-Match` **task version**, giữ toàn bộ start/checklist/location/evidence theo contract; đoạn `measurements` mẫu:

```json
[
 {"sampleId":"length-01","type":"DepressionLength","value":120,"state":"KNOWN","unknownReason":null,"dimension":"LENGTH","unit":"cm","longitude":null,"latitude":null,"locationReason":"Chưa có GPS","instrument":"thước","method":"đo trực tiếp","notes":null},
 {"sampleId":"width-01","type":"DepressionWidth","value":0.8,"state":"KNOWN","unknownReason":null,"dimension":"LENGTH","unit":"m","longitude":null,"latitude":null,"locationReason":"Chưa có GPS","instrument":"thước","method":"đo trực tiếp","notes":null},
 {"sampleId":"depth-01","type":"DepressionDepth","value":0,"state":"KNOWN","unknownReason":null,"dimension":"LENGTH","unit":"mm","longitude":null,"latitude":null,"locationReason":"Chưa có GPS","instrument":"thước","method":"đo trực tiếp","notes":null}
]
```

Khác biệt quan trọng: submission `measurements[].type` là tên enum theo mẫu, còn `requiredMeasurementType` khi tạo task là mã số. SampleId không trùng (so sánh ordinal); max 200 số đo; số không âm, tối đa 6 chữ số thập phân, không vượt giới hạn decimal contract. KNOWN có value (0 hợp lệ); UNKNOWN phải value null và unknownReason, không thay null thành 0. Unit/dimension phải khớp; tọa độ đi cặp, thiếu vị trí cần lý do. Invalid trả 400 `validation_error`.

Thiếu loại task yêu cầu không bị tự thêm số đo: server lưu intake với `REQUIRED_MEASUREMENT_MISSING`; không được hiểu receipt 201 là hồ sơ READY. Có UNKNOWN thêm `MEASUREMENT_VALUE_UNKNOWN`. API-096 `GET /projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId}` đọc đúng measurements của bản nộp đó, gồm type/value/unit; supplement là revision mới, không viết lại bản cũ. API-159 là bổ sung repair attempt riêng. Offline signed payload giữ nguyên, không đổi enum/wire hoặc reserialize payload đã ký.

Chưa có rule được duyệt bắt mọi loại defect phải có đủ cả ba chiều. Loại bắt buộc theo task và policy revision đã publish, không suy từ AI hoặc tên defect. Các ô dài/rộng/sâu là tùy chọn trừ khi task/policy thực sự yêu cầu; chênh cao tấm không dùng DepressionDepth.

## 5. Phạm vi kiểm và điểm còn mở

- Các test mới chạy HTTP qua production DI/auth Web cookie và Crew Bearer với SQL Server container cô lập. Nguồn REPORTER đi qua fixture Report→KEEP_NEW có persisted provenance. Nguồn SURVEY dùng SQL fixture detection/job/block/dataset/Survey và quyết định KEEP_NEW; không gọi AI thật.
- Đã kiểm trước chuyển và Supervisor issue/accept → lifecycle A/B → discovery item B → PM B assign nguồn A → Crew A đọc/accept. Crew B-only, PM A ghi sau chuyển, wrong role, mất membership, stale defect/route/Survey/policy và replay được đối chiếu bằng test. Assign không chứng minh execution-ready.
- Retention fixture SQL có file/VERIFIED/document, không dùng storage thật vì các lệnh chỉ đọc metadata; warranty/basis qua HTTP cookie và phiên mới. Không gọi xóa. Manifest legacy project khác được giữ fail-closed và có ID để chẩn đoán, không nới inventory.
- Không có request lỗi FE cụ thể/correlation ID trong yêu cầu này; chưa chẩn đoán một 409 FE cụ thể. Runtime FE local trước đây dùng stack cũ `rg-step23-local` (không phải source 4934103), không deploy/reseed stack đó trong đợt này. Các kết quả dưới đây là BE test cô lập, không phải nghiệm thu FE.
- Quyền Crew B-only, policy B thay policy A và quyền PM B khám phá nguồn project A còn cần quyết định nếu muốn mở rộng. Giữ contract hiện hành và con đường dùng nguồn/Crew A hợp lệ; không cấp quyền chéo project chỉ vì chuyển nghĩa vụ.

## 6. Kiểm chứng local và tự review

Tại worktree `D:\RG-web-pm-contracts`, SDK `10.0.401`, base/HEAD vẫn `4934103d125be1caca4c6a588eed3768c9cafee4`. Không có commit mới. SQL là Testcontainers cô lập; không sửa dữ liệu sandbox FE.

| Lượt | Kết quả | Phạm vi |
|---|---|---|
| API final-a | 7 PASS / 0 FAIL / 0 SKIP | Assign REPORTER/SURVEY, retention, hai dataset read boundary và hai nhánh FIELD measurements/readback |
| API final-b | 38 PASS / 1 FAIL / 0 SKIP | Assign đã bổ sung mất quyền Crew, Swagger/schema/source evidence; lỗi duy nhất là fixture retention dùng manifest `{}` bị constructor từ chối, chưa tới inventory |
| API final-c sau sửa fixture | 2 PASS / 0 FAIL / 0 SKIP | Retention dùng legacy array `[{}]` hợp lệ theo schema nhưng không đủ lineage; runtime OpenAPI. Không bỏ constraint/guard |
| SQL regression | 11 PASS / 0 FAIL / 0 SKIP | LD06ResponsibilitySqlTests, H4H5RetentionInventorySqlTests, P240FieldInspectionMeasurementSchemaTests, P202MigrationLifecycleTests |
| Unit | 2 PASS / 0 FAIL / 0 SKIP | P240FieldInspectionMeasurementSchemaTests |
| Assign sau tiếp nhận — bổ sung theo yêu cầu chốt | 2 PASS / 0 FAIL / 0 SKIP | REPORTER và SURVEY: URL lấy trực tiếp từ lifecycle B; item ETag từ GET dưới B; assign 201, replay 200, response khớp binding SQL; Crew B-only trả đúng code assignee_not_authorized |

Các lượt có trùng test, không cộng thành một suite. Kết quả mới nhất của 44 identity API khác nhau đều PASS; không chạy full API/SQL suite. Lỗi fixture ở final-b đã được chạy lại thành công ở final-c. Format cuối chỉ đổi whitespace của hai test mới, không thay assertion. `Verify-TestResults.ps1` đạt cho từng TRX final-c, SQL regression và unit.

Lệnh (các filter dưới đây dùng OR `|` và kết quả TRX riêng):

```powershell
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~AssignSourceResponsibilityHttpTests|FullyQualifiedName~RetentionBasisReadbackHttpTests|FullyQualifiedName~ManualSwaggerContractTests' --logger 'trx;LogFileName=contracts.trx' --results-directory "$env:TEMP/rg-contracts-final-b"
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~RetentionBasisReadbackHttpTests|FullyQualifiedName~WebPmCoordinationHttpTests.RuntimeOpenApiDescribesWebRangesBatchAndScopedExport' --logger 'trx;LogFileName=retention.trx' --results-directory "$env:TEMP/rg-contracts-final-c"
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~LD06ResponsibilitySqlTests|FullyQualifiedName~H4H5RetentionInventorySqlTests|FullyQualifiedName~P240FieldInspectionMeasurementSchemaTests|FullyQualifiedName~P202MigrationLifecycleTests' --logger 'trx;LogFileName=regression.trx' --results-directory "$env:TEMP/rg-contracts-sql"
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~P240FieldInspectionMeasurementSchemaTests' --logger 'trx;LogFileName=measurements.trx' --results-directory "$env:TEMP/rg-contracts-unit"
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~AssignSourceResponsibilityHttpTests.ReporterAndSurveyAssignmentsPreserveOriginAfterAcceptedTransfer' --logger 'trx;LogFileName=after-transfer.trx' --results-directory "$env:TEMP/rg-assign-after-transfer"
dotnet build RoadGuardSystem.slnx --no-restore
dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore
git diff --check
```

Build/analyzer: PASS, 0 error (381 warning, không tắt analyzer). Format toàn repo: PASS sau chỉnh whitespace của test mới. Không thêm/đổi tên SQL integration test nên manifest lane giữ nguyên. Không có migration/schema change; model parity được kiểm bằng P202MigrationLifecycleTests. Runtime OpenAPI được sinh bằng production Swagger provider trong test host, loại các Probe test-only và giữ readiness path; path set không đổi, chỉ ba schema DatasetDetailView/FieldTaskCreateInput/ObligationResponsibilityDto thay đổi. [OpenAPI snapshot](openapi.json).

Tự review lượt 1: role/project, transfer, source lineage, version, replay, policy revoke/checklist và fixed measurement codes. Lượt 2: nullable/default compatibility, mapping DTO/fact, Swagger/source expectations, legacy fail-closed, phạm vi file và giới hạn bằng chứng. Chưa có external review. Không kiểm storage/browser/app thật hoặc runtime FE trong đợt này.

Lượt chốt assign sau tiếp nhận chỉ tăng assertion của test và chi tiết handoff; không sửa production. Build analyzer của API test project trong lượt đó đạt; format verify file test, `Verify-TestResults.ps1` cho TRX mới và `git diff --check` đều đạt. Dùng lại các kết quả khác vì source tương ứng giữ nguyên. Hai ca mới chạy lại là hai identity đã có, không cộng thêm vào tổng 44 identity API.

Nguồn đối chiếu chính: `RepairWorkflowRepository.Producing.cs:549` (assign), `:741` (effective responsibility); `FieldInspectionWorkflowRepository.Business.cs:122` (nguồn) và `:149` (pins); `ProjectLifecycleRepository.cs:123,194` (discovery); `SurveyV2PersistenceService.Dataset.cs:216` (Survey ID); `RetentionInventoryRepository.cs:25–75` (manifest/contributors/obligations); `FieldInspectionWorkflowRepository.Intake.cs:103,135` (validation/missing required type). Các file thuộc `RoadGuardSystem.Repositories/Implementations/` theo module tương ứng.
