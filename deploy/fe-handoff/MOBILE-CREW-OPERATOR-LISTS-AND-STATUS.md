# Mobile Crew/Operator — danh sách, trạng thái và thông báo

Base: `c0c99ca6241421ea7ffd5f806935df05cdc3cd27`. Source được kiểm tại worktree riêng `D:\RG-web-pm-contracts`, branch `codex/mobile-crew-operator-lists`; checkout chính không sửa. Không có migration mới. Kết quả local không phải nghiệm thu app Tùng/runtime FE.

## 1. Transport và collection

Các path dưới đây có prefix `/api/v1`. Mobile dùng `Authorization: Bearer <accessToken>`; không mở thêm Web cookie cho collection Crew/Operator. GET không cần CSRF/If-Match/Idempotency-Key. Giữ role và membership/assignment hiện hành. Lệnh ghi vẫn theo contract riêng của từng task.

| Endpoint | Query | Quyền và thay đổi |
|---|---|---|
| `GET /me/inspection-tasks` | `limit`, `cursor`, `status`, `statusGroup`, `mode`, `search`, `acceptance` | RepairCrew; membership đúng project và assignment hiện hành, hoặc history từ chối của chính actor khi yêu cầu rõ |
| `GET /me/survey-tasks` | `limit`, `cursor`, `status`, `statusGroup`, `search` | DroneOperator; assignment chưa kết thúc, membership đúng project và task BAND_V1 |
| `GET /survey-tasks/{taskId}/datasets` | cursor/limit hiện hành | Giữ PM/Sup/Operator scope; mỗi item thêm `processingCounts` |

Mặc định limit 50, từ 1 đến 100. Response giữ `{items,nextCursor,asOf}` cho hai `/me` collection; dataset page giữ `{items,nextCursor}`. Query sai, status/mode/nhóm không nhận diện, search quá 100 ký tự hoặc chứa control character, cursor sai trả `400 validation_error`; sai role `403 access_forbidden`, không có phiên hợp lệ `401`. Không có mục được phép đọc trả `200` items rỗng.

`status`, `mode`, `statusGroup`, `acceptance` được trim và chuẩn hóa uppercase. Search trim + uppercase invariant, tìm substring theo SQL `Latin1_General_100_CI_AS` (không phân biệt hoa/thường, có phân biệt dấu). Không bỏ dấu, không tìm trong nội dung ảnh/hồ sơ hoặc message. Crew tìm `taskCode`; Operator tìm chuỗi UUID `id` và `projectName` được trả trên dòng (entity SurveyRequest hiện không có taskCode). Không tạo mã nhiệm vụ giả.

Quyền membership/assignment, filter và search được áp dụng trong SQL **trước Take/limit**. Scope guard vẫn kiểm hiện hành ở service; cache theo project, không query detail mỗi dòng. Scope rows và processing counts đọc theo lô.

### Phân trang

- Crew: `DueAt ASC, assignmentId ASC`. Khóa phụ là ID assignment để các lần từ chối khác nhau không bị gộp thành một task.
- Operator: `RequestedAt ASC, taskId ASC`. Không đổi task gốc thành task con hoặc trộn trạng thái hai task.
- Cursor mới opaque, gắn actor, loại collection, limit và các giá trị filter/search đã chuẩn hóa. Dùng cursor của actor/query khác bị 400. Cursor cũ trước bản này cũng cần bỏ và tải lại từ trang đầu.
- FE đổi search/filter/limit phải xóa cursor và danh sách cũ. Các điều kiện query có thể kết hợp, có quan hệ AND.
- Đây là keyset/live view, **không phải snapshot**. Accept/reject/reassign/đổi hạn/membership có thể làm một dòng biến mất hoặc đổi vị trí giữa hai request. Refresh từ đầu sau mutation; UI dedupe bằng assignmentId cho Crew, taskId cho Operator. Không dựa vào tổng số hoặc hứa giữ nguyên tất cả dòng xuyên thời gian.

## 2. Crew — mode, task và tiếp nhận

Ví dụ: `GET /me/inspection-tasks?limit=20&mode=CONDITIONAL_FT&statusGroup=INCOMPLETE&acceptance=PENDING&search=REPAIR`.

```json
{
  "items": [{
    "id":"<taskId>", "projectId":"<sourceProjectId>", "defectIds":["<defectId>"],
    "mode":"CONDITIONAL_FT", "crewId":"<currentCrewId>", "policyVersionId":"<policyRevisionId>",
    "status":"NEWASSIGNED", "version":"<taskRowversion>", "taskCode":"REPAIR-<serverCode>",
    "dueAt":"2026-12-01T08:00:00Z", "assignmentId":"<assignmentId>",
    "assignmentStatus":"ACTIVE", "acceptanceOutcome":"PENDING", "acceptedAt":null,
    "rejectedAt":null, "isHistory":false
  }], "nextCursor":null, "asOf":"2026-11-21T12:00:00Z"
}
```

Mode không còn gán cứng. `MEASURE_ONLY` cần task mode thật và không có repair item. NORMAL/CONDITIONAL_FT cần đúng binding hiện hành của item/obligation/assignment/Crew, mode khớp. Thiếu/mơ hồ binding trả mode null; không đoán từ tên task. Policy ID cũng chỉ cấp theo binding không mơ hồ. Summary không cấp execution grant và không khẳng định eligibility đủ điều kiện. API-080 vẫn là nguồn detail/guard cho command.

Giữ wire status cũ của API-037 (uppercase enum không thêm dấu gạch):

| statusGroup | Các task status cụ thể |
|---|---|
| `INCOMPLETE` — Chưa hoàn thành | `NEWASSIGNED`, `ACCEPTED`, `INPROGRESS`, `SUPPLEMENTREQUIRED`, `SUBMITTED` |
| `COMPLETED` — Hoàn thành | Chỉ `COMPLETED` |
| `OTHER` | `REJECTED`, `CANCELLED`, `UNKNOWN` và không nhận diện; không coi hủy/từ chối là hoàn thành |

Task đã kết thúc assignment thông thường không xuất hiện trong danh sách công việc hiện hành. Filter status không cấp lại assignment đã mất. API-080 giữ wire riêng như `NewAssigned`, `InProgress`; FE không tự áp một kiểu chữ cho mọi API.

| acceptance | Tập dữ liệu / nguồn |
|---|---|
| Bỏ query | Assignment ACTIVE hiện hành; vẫn có thể chưa nhận |
| `PENDING` | Assignment ACTIVE, task NEWASSIGNED, chưa có ACCEPTED event của chính assignment; “Mới giao” và “Chờ tiếp nhận” là hai nhãn cùng tập |
| `ACCEPTED` | Assignment ACTIVE và có FIELD ACCEPTED event cùng assignmentId; `acceptedAt` từ event đầu tiên của assignment đó |
| `REJECTED` | Assignment REJECTED thuộc chính actor, membership project vẫn hợp lệ; thời gian từ chối là EndedAt của assignment |

Khi mở tab từ chối, FE bỏ mode/task-status filter cũ: history không có mode/task status hiện hành để khớp các filter đó. Search taskCode vẫn dùng được.

History trả `isHistory:true`, `acceptanceOutcome:"REJECTED"`, assignmentId/status/rejectedAt, task ID/code/project để nhận diện; `status/mode/policyVersionId/dueAt:null`, `defectIds:[]`, `version:""`. Không gán status của người mới cho người cũ. Không dùng history để mở detail, ảnh hoặc gửi command: API đích vẫn chặn sau giao lại. Không có nguồn accepted event ở dữ liệu legacy thì outcome có thể UNKNOWN; ACTIVE không bị suy thành ACCEPTED.

## 3. Operator — bộ lọc và task bổ sung

Ví dụ: `GET /me/survey-tasks?limit=20&statusGroup=SUBMITTED&search=<projectName-or-taskId>`.

```json
{
  "items":[{
    "id":"<surveyTaskId>", "projectId":"<projectId>",
    "scope":[{"routeVersionId":"<routeVersionId>","segmentSetId":"<setId>","segmentIds":["<segmentId>"],"targetBand":"SURFACE"}],
    "operatorId":"<assignedOperatorId>", "status":"SUBMITTED", "version":"<taskRowversion>",
    "supplementTaskId":null, "dueAt":"2026-12-01T08:00:00Z", "projectName":"<currentProjectName>", "parentTaskId":null
  }],"nextCursor":null,"asOf":"2026-11-21T12:00:00Z"
}
```

`status` nhận mã wire của API Survey V2, không phải tên CLR:

| statusGroup / nhãn | Wire status |
|---|---|
| `WAITING` — Chờ nhận | `NEW_ASSIGNED`, `REASSIGNED` |
| `IN_PROGRESS` — Đang thực hiện | `ACCEPTED`, `IN_PROGRESS` |
| `SUBMITTED` — Đã nộp | `SUBMITTED` |
| `SUPPLEMENT_REQUIRED` — Cần bổ sung | `SUPPLEMENT_REQUIRED` |
| `COMPLETED` — Hoàn tất | Chỉ `COMPLETED` |
| `OTHER` — Trạng thái khác | `REJECTED`, `CANCELLED`, `POSTPONED`, `UNKNOWN`, không nhận diện |

Bổ sung sau base 368e556: PM xác nhận bằng `POST /survey-tasks/{taskId}/complete` với datasetId/assessmentId và task ETag. SUBMITTED không tự đổi thành COMPLETED; xem protocol ở mục 9.

Mã không nhận diện chỉ có thể đi vào OTHER khi đọc; không chấp nhận một filter status không được công bố. Decline thường kết thúc assignment nên task không còn trong collection hiện hành; OTHER không khôi phục quyền lịch sử. ACCEPTED chưa phải SUBMITTED. SUBMITTED chưa phải COMPLETED.

Task con có `parentTaskId` thật. Mở task gốc/task con bằng ID riêng, mỗi task giữ status/assignment riêng. `supplementTaskId` trên GET API-226 giữ quy tắc một con trực tiếp đọc được; null có thể là không có, nhiều con hoặc actor không có quyền, không đồng nghĩa chưa từng bổ sung. Collection không gọi GET từng dòng để đoán liên kết; mở API-226 khi cần giải quyết liên kết con và quyền hiện hành.

## 4. Operator — từng tầng hồ sơ/video

| Tầng | Nguồn chuẩn / ID | Ý nghĩa |
|---|---|---|
| Upload | `GET /uploads/{uploadId}`; uploadId là `id` khi tạo phiên | PENDING/UPLOADING/VERIFYING/VERIFIED/FAILED. Giữ uploadId/request trong draft app; VERIFIED chỉ là file hợp lệ, chưa chứng minh nộp dataset |
| Dataset | `/survey-tasks/{taskId}/datasets` → chọn `items[].id`, rồi `/datasets/{id}` | Bản nộp đã lưu, versionNo/submittedAt/integrityStatus/telemetryStatus/sourceFiles/pairs. Không dùng file status thay dataset |
| File video | dataset `sourceFiles[]` purpose SURVEY_VIDEO → `fileId`; `/files/{fileId}/content` | Kiểm VERIFIED và quyền nguồn/assignment mỗi request theo contract hiện hành; bản này không thay streaming/Range |
| Processing | Mỗi dataset list item `processingCounts` | Tổng các job đã lưu qua ProcessingBlock→SurveyDataVersion, theo status hiện hành; không dùng run mới nhất tùy ý |
| Run cụ thể | `/processing-jobs/{jobId}` khi caller có jobId hợp lệ | Giữ guard Operator assignment hiện hành; không cấp quyền nhờ biết ID. Counts không phải danh sách job ID |
| Assessment | `/datasets/{id}/coverage` → assessmentId, hoặc dataset list item assessmentId → `/datasets/{id}/assessments/{assessmentId}` | Readback từng position/quality/coverage của assessment, không suy đã duyệt từ processing |
| Cần bổ sung | Task `SUPPLEMENT_REQUIRED`; API-226/232 và task con | Nhu cầu khảo sát bổ sung, không nhập vào status dataset/video |
| Hoàn tất | Task `COMPLETED` đã lưu | Không suy từ upload VERIFIED, integrity PASSED, processing SUCCEEDED hay task SUBMITTED |

Ví dụ item dataset:

```json
{"id":"<datasetId>","surveyTaskId":"<taskId>","versionNo":2,"submittedAt":"2026-11-21T12:00:00Z","integrityStatus":"PASSED","version":"<datasetVersion>","assessmentId":"<assessmentId>","processingCounts":{"QUEUED":1,"SUCCEEDED":2,"FAILED":1}}
```

Counts có các mã `QUEUED`, `RUNNING`, `SUCCEEDED`, `FAILED` (RetryableFailure/DataFailure theo wire processing hiện hành), `CANCELLED`, `UNKNOWN`; mã chưa nhận diện không bị coi là thành công. `{}` nghĩa chưa có job đã lưu; null là adapter không cung cấp, không tự thay bằng 0. Có thể có nhiều trạng thái đồng thời; FE hiện từng số hoặc danh sách nhãn, không ép thành một trạng thái tổng hợp. Đếm job, không đếm lịch sử từng attempt của cùng job. Không expose AI payload/ảnh qua summary.

Dataset page giữ `submittedAt DESC, id DESC` và quy tắc cursor riêng đã bàn giao. FE có thể mặc định hiển thị item đầu nhưng đây chỉ là lựa chọn UI. AssessmentId lấy cùng quy tắc `/coverage`: assessment ReviewedAt giảm dần và ID làm khóa phụ theo `LatestFirst`; baseline đang khóa có thể trỏ bản khác. Khi cần một assessment cụ thể, mở đúng ID, không tự thay baseline/bản được đánh giá bằng dataset mới nhất.

Chưa có collection upload draft theo task trong đợt này; app phải giữ uploadId đã được cấp. Không có endpoint mới liệt kê mọi processing job: counts phục vụ tình trạng hồ sơ; GET job detail cần jobId từ luồng đã biết. Không khẳng định đã có job discovery nếu API hiện hành chưa trả ID đó.

## 5. Crew — hồ sơ và tiến độ sửa

Nguồn detail: API-080 `GET /projects/{projectId}/field-inspection-tasks/{taskId}`. Không gọi API quản trị PM/Sup 141/148 bằng Crew. Task `version`/ETag dùng FIELD; `repairContext.itemVersion` dùng repair commands. Assignment và current binding vẫn được kiểm.

| Nhãn UI | Nguồn kiểm |
|---|---|
| Đang thực hiện | Task Accepted/InProgress và, với repair, trạng thái item Assigned/InProgress; không tự coi task FIELD start là execution-start |
| Đã nộp hồ sơ | Task Submitted hoặc item Submitted có attempt/effectiveSubmissionId tương ứng; chưa phải COMPLETED |
| Cần bổ sung | attempt.review.decision SUPPLEMENT hoặc task SupplementRequired; chỉ gắn quyết định review với bản nộp có hiệu lực (summary review không trả reason) |
| Đã được duyệt | attempt.review.decision ACCEPT của effectiveSubmissionId: PM review, chưa phải nghiệm thu cuối |
| Hoàn thành sửa | finalDecision hiện hành result CONFIRMED, đúng vòng và quyền hiện tại; không dùng PM review thay quyết định cuối |
| Mở lại/correction | finalDecision mới UNREPAIRED/REPORTED_AWAITING_REVIEW theo contract; bỏ nhãn hoàn thành, giữ lịch sử theo decisionId |

NORMAL xác nhận cuối theo Supervisor; Fast Track theo PM. Sau API-159, refresh API-080: cùng attempt, effectiveSubmissionId mới, review cũ đã clear không gắn vào submission mới. Sau PM review/final/correction/reopen cũng GET lại. `repairContext` nullable khi không có liên kết hiện hành đủ điều kiện; không chế context từ task mode. MEASURE_ONLY dùng lifecycle FIELD Completed và review nghiệp vụ FIELD, không bắt buộc repairContext.

## 6. Ma trận notification

Nguồn chính: `FieldInspectionWorkflowRepository.Business.cs`/`.Intake.cs`; `RepairWorkflowRepository.Producing.cs`/`.Lifecycle.cs`; `SurveyV2PersistenceService.cs`/`.Dataset.cs`/`.MobileNotifications.cs`; các H6 source adapter, catalog, dispatch và protected inbox.

| Luồng / sự kiện | Event code, producer / dữ liệu nguồn | Người nhận / điều kiện |
|---|---|---|
| FIELD giao/giao lại | `field.task.assigned.v1`, Emit ASSIGNED/REASSIGNED + FieldInspectionTaskEvent/assignment | Crew được giao còn quyền |
| FIELD nộp | `field.task.submitted.v1`, Emit SUBMITTED + submission revision | PM chịu xử lý theo nguồn/duty |
| FIELD bổ sung | `field.task.supplement_requested.v1`, review/supplement đã lưu | Crew đang được giao |
| FIELD accept/reject/start/review/cancel | `field.task.lifecycle.v1` | **VERIFIED_AUDIT_ONLY**, không mô tả thành inbox gửi Crew/PM |
| Repair giao | `repair.work.assigned.v1`, binding/item lifecycle | Crew đúng binding hiện hành |
| Repair nộp | `repair.work.submitted.v1`, attempt/intake/lifecycle + snapshot | Người review theo quy tắc PM/Sup snapshot/duty hiện hành; không gửi mọi role |
| Repair làm lại/bổ sung | `repair.work.rework_requested.v1`, PM review đã lưu | Crew đúng binding |
| Repair chờ Sup | `review.supervisor_required.v1`, proposal/PM review ACCEPT theo NORMAL | Supervisor được phân trách nhiệm, không hiểu là thông báo nghiệm thu gửi Crew |
| Fast Track xác nhận cuối | `repair.work.fast_track_confirmed.v1`, quyết định PM và provenance | Thông tin tới Supervisor được chỉ định theo contract cũ, không tự thêm người nhận Crew |
| Correction repair | `repair.decision.corrected.v1` | Audit-only hiện hành; API-080 là nguồn Crew đọc quyết định mới |
| Survey giao/giao lại **mới** | `survey.task.assigned.v1`, task/assignment đã lưu trong cùng receipt transaction | Operator của chính assignment còn hiệu lực; giao lại không gửi cho người cũ |
| Survey task bổ sung **mới** | `survey.task.supplement_requested.v1`, task con+ParentTaskId+SupplementRequestId+assignment | Operator được giao **task con**; deep link sourceId là task con, không phải SupplementarySurveyRequest ID |
| Survey nộp **mới** | `survey.task.submitted.v1`, dataset đã lưu + Survey/task lineage | PM chính hiện hành của project để xử lý; không biến thành phê duyệt |
| Survey assessment **mới** | `survey.dataset.assessed.v1`, assessment và outbox trong cùng receipt transaction | Operator liên quan còn assignment/membership hiện hành; scope cung cấp taskId/datasetId/assessmentId. Đây là kết quả đánh giá, không phải thông báo hoàn thành |
| Survey COMPLETED | PM gọi lệnh complete sau kiểm full task scope; audit + receipt | Readback task COMPLETED qua API-226/list; không phát notification hoàn thành riêng trong đợt này |

Inbox dùng lại `GET /notifications`, `GET /notifications/{id}`, `GET /notifications/{id}/scope`, `POST /notifications/{id}/read`. SourceKind FieldTask→API-080, RepairWork→đọc item/task theo quyền nguồn, SurveyTask→API-226 với sourceId. Scope bổ sung `eventCode` nullable từ occurrence+sidecar được xác minh (ví dụ survey.task.assigned.v1); legacy thiếu căn cứ trả null. Giữ `eventType` cũ có thể là khóa chống trùng dạng hash, không parse nó như event code. Scope trả sourceKind/sourceId/projectId và bổ sung `taskId/taskProjectId` nullable từ liên kết hiện hành đã được kiểm quyền; đặc biệt RepairWork.sourceId là item ID, không dùng nó như task ID. Crew mở API-080 bằng taskProjectId/taskId từ scope; nếu null thì refresh danh sách, không đoán task. không parse message để lấy ID hoặc quyết định hành động. Read command dùng Idempotency-Key và If-Match từ notification detail. GET và đánh dấu đã đọc không cấp quyền tới task/file.

Các producer Survey mới dùng outbox trong cùng transaction với assignment/dataset và receipt; replay không thêm outbox. H6 finite registry/source proof/fenced delivery/occurrence dedupe/retry được giữ. Adapter đối chiếu transport với assignment/dataset thật, task/project và thời điểm. Delivery/retry và inbox kiểm lại assignment/membership; mất quyền hoặc giao lại thì notification cũ không cấp đường đọc. Những notification audit-only/nguồn chưa có producer đã ghi rõ, không được gắn nhãn “đã gửi”.

## 7. FE nối lại

1. Thay lọc/search cục bộ bằng query BE từ trang đầu; debounce search, bỏ cursor khi query đổi.
2. Crew hiển thị mode/status/acceptance riêng; không gọi API-080 từng dòng. Chỉ mở detail khi chọn task hiện hành; history từ chối là dòng đối soát.
3. Operator giữ task gốc/con riêng; vào task rồi gọi dataset collection, chọn bản theo ID và đọc detail/coverage/assessment. Giữ draft upload/request bền vững như contract retry trước đây.
4. Sau accept/reject/reassign/submit/review/correction, bỏ cursor và reload nguồn đọc tương ứng. Deep link bị 401 thì đăng nhập lại; 403/404 thì refresh list, không dùng history để vượt guard.
5. Source được push không tự cập nhật runtime. Đợt này không có migration mới; cập nhật backend theo quy trình deploy hiện hành rồi kiểm lại bằng phiên app mới. Không deploy trong nhiệm vụ này.

## 8. Kiểm chứng đợt 368e556 (kết quả lịch sử, xem mục 9 cho đợt bổ sung)

HTTP sử dụng production DI/Bearer và SQL Server container cô lập; không phải app/browser, không chạy AI thật hoặc cập nhật DB FE. Fixture trạng thái được tạo có kiểm soát để kiểm read projection, không được xem là workflow nghiệp vụ tự có quyền ghi trạng thái đó. Không chạy full API/4 SQL lane trong nhiệm vụ này. Upload/storage trong các journey dùng fixture/mock hiện có; không chứng minh MinIO hay phát video trên app thật.

| Lượt có bằng chứng còn phù hợp | PASS / FAIL / SKIP | Phạm vi |
|---|---|---|
| Core API (rg-mobile-fourth) | 23 / 0 / 0 | List Crew cũ, P2 Survey, list mới, NORMAL/Fast Track và transfer |
| Expanded API (rg-mobile-final) | 42 / 1 / 0 | Một lỗi source evidence sau thay đổi code; assertion giữ nguyên, đã cập nhật excerpt đúng source và chạy lại ở lượt source bên dưới |
| Protected scope (rg-mobile-scope) | 5 / 0 / 0 | List mới, eventCode và H6 read scope |
| Survey workflow (rg-mobile-workflow) | 4 / 0 / 0 | Hai nhánh survey thực qua HTTP: nộp video-only/assessment/baseline/supplement/revoke, dispatch đúng Operator task con và PM; runtime Swagger, H6 read |
| Navigation/readback (rg-mobile-navigation) | 7 / 0 / 0 | Mode theo API-080, review/final/correction NORMAL, notification task navigation và runtime Swagger |
| Source evidence (rg-mobile-source) | 1 / 0 / 0 | ProductionInventoryAndEvidenceExcerptsMatchCurrentSource |
| Pagination edges (rg-mobile-edge) | 4 / 0 / 0 | Chọn đích chắc chắn ngoài trang đầu, cả hai list; accepted/rejected theo assignment, sai query/actor, processing counts và inbox |
| Regression cuối (rg-mobile-last-regression) | 4 / 0 / 0 | Hai nguồn assign sau transfer, P2 Survey và trạng thái/quyền liên quan |
| Authority cuối (rg-mobile-authority) | 5 / 0 / 0 | Giữ SurveyTask inbox riêng cho Operator, đổi role sang Crew bị chặn, mất assignment, read/replay |
| Unit (rg-mobile-unit) | 69 / 0 / 0 | InspectionTaskQueryService, P2V2SurveyService, H6 notification |
| SQL regression (rg-mobile-sql) | 9 / 0 / 0 | H4 supplement/effective review/intake, H6 dispatch và P202 model parity |
| Migration fixture (rg-mobile-migration-fixed) | 1 / 0 / 0 | V2P1063MigrationUpgradeTests: legacy data không tự cấp quyền; thêm membership đúng scope mới đọc được |

Các lượt có trùng test, không cộng thành một suite. Kết quả mới nhất của **63 identity API** đã chạy đều PASS. Các lỗi trung gian gồm LINQ projection chưa dịch được SQL, assertion cũ về trang rỗng/dueAt, nhầm eventType là event code và fixture migration chưa cấp membership; đã sửa đúng nguyên nhân và chạy lại. Không bỏ test hoặc nới production guard. Các TRX cuối được kiểm bằng `tests/CI/Verify-TestResults.ps1`; không có ca skip trong các nhóm đã chọn.

Lệnh chính (settings serial ở TEMP chỉ đặt MaxParallelThreads/MaxCpuCount=1 và không parallelize test collections):

```powershell
dotnet build RoadGuardSystem.slnx --no-restore
dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~MobileTaskListsHttpTests|FullyQualifiedName~H6NotificationOperationsHttpTests' --logger 'trx;LogFileName=authority.trx' --results-directory "$env:TEMP/rg-mobile-authority"
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~Anh01SurveyFlowTests|FullyQualifiedName~WebPmCoordinationHttpTests.RuntimeOpenApiDescribesWebRangesBatchAndScopedExport|FullyQualifiedName~H6NotificationOperationsHttpTests' --logger 'trx;LogFileName=workflow.trx' --results-directory "$env:TEMP/rg-mobile-workflow"
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~IncompleteFormalFieldIntakePinsOneAttemptAndOriginalPmReviewClock|FullyQualifiedName~P202MigrationLifecycleTests|FullyQualifiedName~H6NotificationDispatch' --logger 'trx;LogFileName=sql.trx' --results-directory "$env:TEMP/rg-mobile-sql"
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~V2P1063MigrationUpgradeTests'
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~InspectionTaskQueryServiceTests|FullyQualifiedName~P2V2SurveyServiceTests|FullyQualifiedName~H6Notification'
pwsh -File tests/CI/Verify-TestResults.ps1 -ResultsPath "$env:TEMP/rg-mobile-authority"
pwsh -File tests/Documentation/Verify-P102Docs.ps1
pwsh -File tests/Tooling/Verify-AgentSetup.ps1
git diff --check
```

Build/analyzer đạt (0 error; có warning, không tắt analyzer). Format toàn repo, source/docs/agent checks và diff check được kiểm trước commit. Không có migration/model change; sandbox catalog và manifest SQL lane giữ nguyên. Test SQL chỉ sửa fixture trong identity hiện có, không thêm/đổi tên hoặc xóa test. [OpenAPI](openapi.json) sinh từ runtime Swagger provider của test host production DI, bỏ Probe test-only và giữ readiness contract; path set không đổi, hai GET `/me` thêm query, bốn schema read được mở rộng.

Tự review lượt 1: quyền trước pagination, mode/binding, history không cấp quyền, status wire và các tầng hồ sơ độc lập; sửa LINQ và fixture/assertion cũ theo contract mới. Lượt 2: outbox/source proof/recipient/replay, đọc scope sau mất assignment/đổi role, eventType tương thích, navigation đúng task/project, legacy/null, source evidence/OpenAPI, file scope và bảo toàn main checkout. Không có peer/external review được thực hiện trong lượt này.


## 9. Bổ sung sau 368e556 — PM hoàn tất Survey và inbox assessment

Base `368e556e4c882570cbbeeed7002a6b9e1825c33d`, nhánh local `codex/survey-completion-assessment`. Không có migration mới. Không deploy/apply DB FE trong đợt này. Những ghi chú mục 3/6 về command complete và inbox assessment đã được cập nhật; FIELD lifecycle/correction audit-only vẫn giữ nguyên.

### 9.1. Xác nhận hoàn thành từng task

Mới: **`POST /api/v1/survey-tasks/{taskId}/complete`**. PM có membership hiện hành trong project của task. Supervisor/Operator không được xác nhận. Hai lựa chọn auth riêng: Bearer hoặc Web cookie; Web phải lấy CSRF bằng `GET /api/v1/auth/web/csrf` và gửi `X-CSRF-TOKEN`, `credentials: "include"`.

```http
POST /api/v1/survey-tasks/<taskId>/complete
Content-Type: application/json
Idempotency-Key: <new-command-key>
If-Match: "<task-version-from-GET-API-226>"
X-CSRF-TOKEN: <requestToken-if-cookie>
```

```json
{"datasetId":"<explicit-dataset-of-this-task>","assessmentId":"<current-assessment-of-that-dataset>","reason":"PM xác nhận hồ sơ đáp ứng đủ phạm vi nhiệm vụ"}
```

Nguồn ID/version:

1. `GET /survey-tasks/{taskId}` → status/version và ETag **task**, không dùng ETag dataset/assessment.
2. `GET /survey-tasks/{taskId}/datasets` → chọn `items[].id` làm datasetId; mặc định UI mới nhất chỉ là lựa chọn hiển thị.
3. `GET /datasets/{datasetId}/coverage` → `assessmentId` hiện hành; mở `GET /datasets/{datasetId}/assessments/{assessmentId}` để PM kiểm từng item. Dataset list assessmentId dùng cùng quy tắc này.
4. PM xác nhận rồi gửi lệnh; sau 409/412 phải GET lại, kiểm scope/assessment và để PM xác nhận trước key mới.

Guard completion:

- Task V2/BAND_V1 hiện tại phải **SUBMITTED** và có assignment hiện hành. Chưa nộp, đã hủy, cần bổ sung hoặc đã hoàn thành với lệnh mới đều không tự đi tiếp. Không dùng upload VERIFIED, integrity PASSED hay processing SUCCEEDED để hoàn thành.
- Dataset chỉ định phải ServerConfirmed và thuộc **đúng task/project** qua Survey. Không lấy dataset của cha hoàn thành con hoặc ngược lại; không tự chọn dataset mới nhất. Không thêm điều kiện integrity PASSED hoặc Supervisor duyệt ngoài yêu cầu đã chốt.
- Assessment phải là revision hiện hành của dataset (ReviewedAt DESC, Id DESC, cùng `/coverage`), method `pm-evidence-review.v1`. Không cho dùng PASS cũ bỏ qua FAIL/UNKNOWN mới hơn.
- Mọi segment/band bắt buộc của **toàn task** phải nằm trong dataset scope và có PositionStatus/QualityStatus/CoverageStatus đều **PASS** ở assessment chỉ định. Đủ một phần chưa đủ. Task scope lấy từ scope rows đã pin; task cũ thiếu rows dùng scope trong OutputRequirements đã lưu, thiếu/malformed thì fail-closed.
- Kiểm PM hiện hành, scope và role trong transaction/receipt guard. Chuyển trạng thái task + audit chứa datasetId/assessmentId/reason + receipt cùng transaction serializable. Giữ rowversion; hai lệnh cùng key replay một kết quả, khác key dùng cùng version chỉ một chuyển trạng thái thành công. Không hoàn tất cha/con, không thay baseline hay trạng thái dataset/processing.

Thành công mới và replay: **200**, response `SurveyTaskV2ResponseDto` hiện có, ETag task mới:

```json
{
  "id":"<completed-taskId>","projectId":"<projectId>",
  "scope":[{"routeVersionId":"<routeId>","segmentSetId":"<setId>","segmentIds":["<segmentId>"],"targetBand":"RIGHT_EDGE"}],
  "operatorId":"<currentOperatorId>","status":"COMPLETED","version":"<new-task-rowversion>",
  "supplementTaskId":null,"dueAt":"2026-12-01T08:00:00Z","projectName":null,"parentTaskId":"<parentTaskId>"
}
```

GET detail/list của Operator vẫn theo assignment + membership hiện hành. Gọi lại API-226 và `GET /me/survey-tasks?statusGroup=COMPLETED` để mở lại sau reload. ParentTaskId giữ đúng task gốc. Task con COMPLETED **không** làm task gốc SUPPLEMENT_REQUIRED thành COMPLETED. Đợt này không thêm thao tác tổng kết cả chuỗi bổ sung hoặc dùng hồ sơ con đóng task gốc; nếu cần đóng nhiệm vụ gốc sau khi hợp nhất các lượt thì cần quy tắc/workflow riêng, không giả trạng thái.

Replay sau timeout: cùng taskId/body/key/If-Match ban đầu; trả kết quả đã ghi dù version ban đầu nay cũ, nhưng vẫn kiểm quyền PM hiện hành trước receipt. Mất membership/role bị 403, không lấy receipt làm quyền. Key khác không phải replay; key cũ/body khác conflict.

| HTTP/code thực tế | Xử lý |
|---|---|
| 200 | Task xác nhận hoặc replay; refresh task/list, giữ phân biệt assessment |
| 400 validation_error/model validation | Sai JSON/type; field ngoài DTO bị chặn |
| 401 auth_unauthorized | Phiên không hợp lệ |
| 403 access_forbidden | Sai role hoặc PM không còn quyền project |
| 403 csrf_failed | Cookie thiếu/sai CSRF; không có tác động nghiệp vụ |
| 404 survey_request_not_found | Task không có hoặc không đọc được nguồn task |
| 409 survey_invalid_state_transition | Task/source/assessment không đủ điều kiện, current assessment không khớp, còn thiếu PASS hoặc task không ở SUBMITTED |
| 409 duplicate_request | Cùng key nhưng command khác |
| 409 survey_scope_incompatible | Không phải BAND_V1 |
| 412 auth_concurrency_conflict | Task version cũ; đây là mã route Survey hiện hành, không đổi tên trong đợt này |
| 422 survey_validation_failed | ID rỗng, reason trắng hoặc >1000, key quá 128; thiếu field bắt buộc/type sai có thể bị model validation 400 trước service |
| 428 validation_error | Thiếu Idempotency-Key hoặc If-Match |

### 9.2. Thông báo kết quả assessment cho Operator

Mới: **`survey.dataset.assessed.v1`**, kind **ASSESSED**, sourceKind **SurveyTask**. Producer `SurveyAssessmentRepository.CreateAsync`: ghi assessment/items/audit/outbox cùng receipt transaction. Receipt replay không tạo thêm event. Không gửi đại trà cho Crew/PM/Sup.

Recipient là Operator của assignment hiện hành liên quan task tại lúc assessment được ghi. Dispatch chứng minh transport, assessment ID/revision, dataset→Survey→task/project, ReviewedAt và assignment/current actor. Membership và assignment được kiểm lại khi delivery/retry/inbox/đọc nguồn. Nếu đã giao người khác trước dispatch, không chuyển thông báo lịch sử sang người mới; source không còn hợp lệ thì bị từ chối theo H6 hiện hành. Nếu đã gửi rồi mà người nhận mất quyền, inbox/scope/detail đích vẫn chặn.

Handoff đọc inbox giữ `eventType` cũ (có thể là dedup hash); dùng **eventCode** từ scope. `GET /notifications/{id}/scope` bổ sung **datasetId/assessmentId nullable**, chỉ điền từ occurrence assessment có lineage đã kiểm chứng. Không parse message, không tự dùng assessment mới nhất thay ID của thông báo.

```json
{
  "notificationId":"<notificationId>","projectId":"<projectId>","occurrenceId":"<occurrenceId>",
  "classification":"PROJECT","sourceKind":"SurveyTask","sourceId":"<taskId>",
  "eventType":"<existing-dedup-key>","eventCode":"survey.dataset.assessed.v1",
  "taskId":"<taskId>","taskProjectId":"<projectId>",
  "datasetId":"<datasetId>","assessmentId":"<exact-assessmentId>",
  "resolverVersion":"huy-final.notification-registry.v1","title":"Dataset assessment recorded",
  "priorityPolicy":"MANDATORY_DEADLINE_V1","senderKind":"SYSTEM","deadlines":[],
  "priority":"NORMAL","priorityReasonCode":"NO_CURRENT_OVERDUE_DEADLINE",
  "priorityEvaluatedAt":"2026-11-21T17:00:00Z"
}
```

Đây là mẫu shape với ID placeholder; priority/reason lấy server theo deadline hiện hành, không hardcode các giá trị ví dụ. Với thông báo khác/legacy thiếu căn cứ, datasetId/assessmentId null; không suy từ sourceId hoặc title. FE mở API-226 bằng taskId và `/datasets/{datasetId}/assessments/{assessmentId}` bằng ID scope. Assessment trả item status PASS/FAIL/UNKNOWN; notification không tự phát “đã hoàn thành”. Operator vẫn thấy SUBMITTED trước khi PM gọi complete, kể cả mọi assessment item PASS. Completion hiện chỉ có readback task/audit, không tạo thông báo hoàn thành riêng.

Sau đọc, dùng POST `/notifications/{id}/read` theo Idempotency-Key và ETag notification hiện hành. Giao lại/mất scope trả 403/404 ở API đích: refresh danh sách, không thử ID đoán. Sự kiện audit-only FIELD lifecycle và repair correction ngoài yêu cầu inbox lần này giữ nguyên như ma trận mục 6.

### 9.3. Kiểm chứng đợt bổ sung

Kết quả và tình trạng CI được ghi tại bàn giao cuối. Không có migration: mã SurveyRequestStatus.Completed đã có trong DB constraint; các cột occurrence/audit có sẵn, không sửa bản migration cũ. Test SQL mới/đổi identity: không có, manifest lane giữ nguyên. HTTP test dùng production auth/DI và SQL container thật, upload verification/storage fixture/mock; chưa kiểm app Tùng, SMTP/AI/MinIO thật hay runtime FE trong đợt bổ sung.


### 9.4. CI base và kiểm chứng cuối

Run base [44927099895](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/44927099895), head huy-review **368e556e4c882570cbbeeed7002a6b9e1825c33d**, đã FAILURE. Artifact `api-results` ID **14716473906**, `api.trx` xác nhận **1682 PASS / 1 FAIL / 1 SKIP**. Job [API 136915475018](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/44927099895/job/136915475018) fail đúng `NotificationWireAcceptanceHttpTests.InboxListDetailScopeAndReadHaveExactWireAndDurableLegacyAudit`, dòng 47 (helper Fields dòng 146): tập exact field còn cũ, thiếu eventCode/taskId/taskProjectId. Bản bổ sung hiện tại có thêm datasetId/assessmentId nên assertion cập nhật đủ năm field, kiểm từng field **null** cho nguồn Project legacy. Các HTTP nguồn thật kiểm eventCode/taskId/taskProjectId và datasetId/assessmentId đúng assessment; thông báo giao task không giả dataset/assessment ID.

Không xóa kiểm exact wire hoặc nới quyền. Source contracts, unit, cả bốn SQL lane và SQL tổng hợp của run base đều PASS. Bước `Verify-TestResults.ps1` API của run cũ bị SKIPPED do bước dotnet test đã fail, không được báo gate PASS. Không rerun run cũ để đổi dấu đỏ.

Skip duy nhất trong TRX là `RoadGuardSystem.ApiTests.Files.UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`, thông báo yêu cầu ROADGUARD_MINIO_SMOKE=1 và MinioStorage__*. Identity này đúng whitelist `tests/CI/Verify-TestResults.ps1` khi CI gọi `-AllowMinioSmokeSkip` và smoke không bật; giữ nguyên script, workflow và whitelist. Không có skip trong các nhóm local được chọn cho đợt này.

| Lượt local đợt bổ sung (có trùng ca) | PASS / FAIL / SKIP | Phạm vi |
|---|---|---|
| `rg-survey-complete-second` | 2 / 0 / 0 | HTTP submit/assessment/supplement/complete/Operator readback sau sửa fixture CSRF |
| `rg-survey-complete-third` | 4 / 0 / 0 | Workflow bổ sung, route catalog và W05 auth admission |
| `rg-survey-complete-fourth` | 9 / 0 / 0 | Cạnh tranh same/different key complete, legacy scope fallback, wrong role/project/version/assessment, receipt quyền hiện hành; Mobile/P2/Swagger |
| `rg-survey-complete-final` | 41 / 0 / 0 | Toàn ManualSwaggerContractTests, source excerpts, route catalog/admission, workflow và runtime Swagger |
| `rg-survey-complete-scopes` | 2 / 0 / 0 | Scope rows và fallback legacy khi thiếu rows, đầy đủ PASS, cả Bearer và Web PM |
| `rg-survey-complete-unit` | 66 / 0 / 0 | P2V2SurveyServiceTests và H6 notification unit |
| `rg-survey-complete-sql` | 8 / 0 / 0 | H6 dispatch SQL regression và P202 model parity |
| `rg-survey-complete-ci-fix` | 14 / 0 / 0 | Notification exact wire, H6 scope, ProtectedSourceInbox rights/replay, Mobile, Survey completion/assessment và runtime Swagger |
| `rg-survey-ci-catalog` | 31 / 0 / 0 | H6 catalog, gồm ca mới bắt revision của assessment và từ chối kind COMPLETED |
| `rg-survey-ci-delivery` | 7 / 0 / 0 | Chạy cuối sau review: notification exact wire, hai chuỗi Survey completion/assessment, production source evidence và Swagger |

Không cộng các lượt thành full suite. Lượt đầu lỗi fixture test lấy Cookie từ default header trong khi cookie nằm ở handler; đã sửa bằng cùng phiên Web hợp lệ, bỏ CSRF rồi khôi phục, chạy lại thành công. Đã gặp copy-lock khi khởi động build lúc testhost cũ đang đóng; lượt sau hoàn tất trên bản cuối. Không sửa production auth để vượt fixture. Artifact/TRX/log lưu ngoài repo ở TEMP, không đưa vào commit.

Đối chiếu identity giữa các TRX cuối: **51 identity API** và **67 identity unit** có kết quả mới nhất PASS; SQL hồi quy **8 PASS**. Đây không phải kết quả full suite. `Verify-TestResults.ps1` đạt với từng nhóm TRX local cuối (0 skip). Build solution với analyzer và format verify toàn repo đạt, `git diff --check` đạt. Docs/agent validators đạt. Runtime Swagger khớp snapshot: chỉ thêm path `/survey-tasks/{taskId}/complete`, schema CompleteSurveyTaskRequestDto và hai field nullable của H6NotificationScopeDto; route catalog/admission đã kiểm. Không có migration hoặc SQL lane identity thay đổi.

Lệnh kiểm chính:

```powershell
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~NotificationWireAcceptanceHttpTests|FullyQualifiedName~H6NotificationOperationsHttpTests|FullyQualifiedName~ProtectedSourceInboxHttpTests|FullyQualifiedName~MobileTaskListsHttpTests|FullyQualifiedName~Anh01SurveyFlowTests|FullyQualifiedName~WebPmCoordinationHttpTests.RuntimeOpenApiDescribesWebRangesBatchAndScopedExport' --logger 'trx;LogFileName=wire.trx' --results-directory "$env:TEMP/rg-survey-complete-ci-fix"
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~H6NotificationCatalogTests' --logger 'trx;LogFileName=catalog.trx' --results-directory "$env:TEMP/rg-survey-ci-catalog"
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --settings "$env:TEMP/rg-ft04-serial.runsettings" --filter 'FullyQualifiedName~P202MigrationLifecycleTests|FullyQualifiedName~H6NotificationDispatch' --logger 'trx;LogFileName=sql.trx' --results-directory "$env:TEMP/rg-survey-complete-sql"
dotnet build RoadGuardSystem.slnx --no-restore
dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore
pwsh -File tests/CI/Verify-TestResults.ps1 -ResultsPath "$env:TEMP/rg-survey-complete-ci-fix"
git diff --check
```

Tự review lượt 1: xác nhận PM scope/CSRF, task ETag, exact dataset/assessment và full task scope; serializable/replay với quyền hiện hành; không tự complete cha/con hoặc nhầm assessment thành nghiệm thu. Lượt 2: H6 transport/lineage/recipient, scope ID nullable, exact wire legacy và nguồn thật, current assignment/reassign, OpenAPI/inventory/source snippets, no migration/SQL identity change, file scope/secrets. External review chưa thực hiện. Không chạy full suite local hay kiểm app/runtime FE. CI của SHA mới phải xem riêng, không suy PASS từ các lượt local hoặc run base.
