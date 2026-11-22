# Survey: loại khảo sát, conflict và đóng kế hoạch

Base: `2ab6539f21a69b935e491008d64c21fcc050152a`. Phạm vi được chủ nhiệm vụ chốt ngày 22/11/2026. Bản này không có migration, không backfill hay đổi unique index. Source đã cập nhật không đồng nghĩa backend FE đang dùng đã cập nhật.

## Loại khảo sát và quan hệ task

| API | Field |
|---|---|
| GET `/api/v1/projects/{projectId}/survey-plans` | `items[].surveyType` |
| GET `/api/v1/projects/{projectId}/survey-tasks` | `items[].surveyType`, `items[].parentTaskId` |
| GET `/api/v1/survey-plans/{id}` | `surveyType`, `completion` |
| GET `/api/v1/survey-tasks/{taskId}` | `surveyType`, `parentTaskId`, `supplementTaskId` |
| GET `/api/v1/me/survey-tasks` | `items[].surveyType`, `items[].parentTaskId` |
| Create/mutation sử dụng cùng DTO plan/task; work-package chứa `task` | Cùng field, từ dữ liệu của chính tài nguyên |

`BASELINE` = Cơ sở; `PERIODIC` = Định kỳ; `AD_HOC` = Đột xuất; `UNKNOWN` = Chưa xác định. Task bổ sung hiện được tạo với loại riêng `AD_HOC`, không lấy loại cha. Dữ liệu không nhận diện hoặc receipt cũ thiếu field trả `UNKNOWN`, không giả định BASELINE. Sau replay response lịch sử, FE GET lại tài nguyên hiện hành; snapshot receipt không được viết lại.

`parentTaskId` là cha trực tiếp đã lưu; task gốc trả null. `supplementTaskId` là task con trực tiếp khi xác định được đúng một con và caller được mở con. Null có thể là chưa có, nhiều con hoặc không có quyền mở; không suy ra chưa từng bổ sung. ID quan hệ không cấp thêm quyền đọc. Collection PM/Supervisor vẫn đọc theo lô, cursor/limit và scope hiện hành.

## Phân biệt conflict

Ví dụ HTTP 409, `application/problem+json` (ID dưới đây chỉ minh họa):

```json
{
  "status": 409,
  "code": "survey_active_plan_conflict",
  "existingPlanId": "00000000-0000-0000-0000-000000000001",
  "existingPlanStatus": "IN_PROGRESS",
  "correlationId": "<server correlation ID>"
}
```

`existingPlanStatus`: `PLANNED`, `POSTPONED`, `IN_PROGRESS`, thống nhất với status DTO. FE dùng ID để GET plan hiện hành. Chỉ PM còn quyền trong project mới nhận metadata. Khóa unique vẫn là project + roadSection + surveyType ở trạng thái active 1/2/3; không phải segment set hay ngày dự kiến. Hai lần tạo đồng thời: một thành công; bên thua đọc lại đúng plan sau rollback.

| HTTP/code | FE xử lý |
|---|---|
| 409 `survey_active_plan_conflict` | Mở plan hiện có; nếu đủ hồ sơ, dùng quy trình đóng dưới đây trước kỳ mới |
| 409 `duplicate_request` | Cùng actor/project/operation/key nhưng fingerprint khác; kiểm request đã lưu. Không dùng key khác để vượt nghiệp vụ |
| 409 `survey_task_plan_mismatch` | Loại hoặc scope task không thuộc plan (kể cả định dạng lineage không tương thích) |
| 409 `survey_plan_state_conflict` | Trạng thái plan không cho tạo task theo workflow hiện hành |
| 409 `survey_source_scope_invalid` | Nguồn route/set/segment/band không hợp lệ, không phải trùng key |
| 404 theo contract hiện hành | Plan khác project/không tồn tại; không trả ID ngoài phạm vi |

Replay cùng key/body trong đúng phạm vi trả kết quả đã ghi. Khác key/cùng nội dung vẫn chịu ràng buộc active plan. Không coi mọi lỗi SQL unique là active-plan conflict; BE chỉ nhận index `UX_SurveyPlans_ActiveScope`.

## PM xác nhận hoàn tất kế hoạch

`POST /api/v1/survey-plans/{planId}/complete`

Chỉ PM có quyền hiện hành trong project; Supervisor và Operator không được gọi. Bearer tiếp tục được hỗ trợ. Với Web:

1. Đăng nhập `/api/v1/auth/web/login`; gọi GET `/api/v1/auth/web/csrf` cùng phiên, lấy `requestToken`.
2. GET `/api/v1/survey-plans/{id}`, giữ nguyên ETag có dấu ngoặc kép.
3. Gửi cookie bằng `credentials: "include"`, header `X-CSRF-TOKEN: <requestToken>`, `Content-Type: application/json`, `If-Match: <plan ETag>`, `Idempotency-Key: <key mới>`.

```json
{
  "sources": [
    { "taskId": "<task gốc>", "datasetId": "<dataset của task gốc>", "assessmentId": "<assessment hiện hành của dataset đó>" },
    { "taskId": "<task bổ sung>", "datasetId": "<dataset bổ sung>", "assessmentId": "<assessment hiện hành của dataset bổ sung>" }
  ],
  "reason": "PM xác nhận đủ phạm vi kế hoạch qua hồ sơ đã đánh giá"
}
```

`sources`: 1–100 nguồn, dataset ID không lặp; `reason`: không trắng, tối đa 1000 ký tự. ID lấy từ task thuộc plan, API task datasets và coverage/assessment đã đọc. Không dùng một dataset của task khác, route/set/band cùng tên nhưng khác ID, hoặc assessment cũ thay assessment hiện hành. Bản mới nhất của dataset không tự là căn cứ được chọn; PM chỉ rõ nguồn.

Điều kiện: plan `IN_PROGRESS`, scope `BAND_V1` còn xác định; nguồn ServerConfirmed, lineage plan → task gốc → task bổ sung có yêu cầu bổ sung đã lưu; assessment hiện hành `pm-evidence-review.v1`; tổng các ô Position/Quality/Coverage đều PASS bao phủ đúng toàn bộ scope đã pin. Có thể ghép phần SURFACE của cha với RIGHT_EDGE của con. FAIL/UNKNOWN không đóng góp PASS. Không lấy hồ sơ cùng project nhưng không thuộc lineage. Nhánh chưa giải quyết chặn đóng; phải xử lý bằng workflow có audit. Operator từ chối (REJECTED) chưa phải PM giải quyết nhánh: PM phải xử lý/giao lại hoặc hủy qua workflow trước khi đóng. Task cha `SUPPLEMENT_REQUIRED` và task con giữ trạng thái riêng; đóng plan không đổi baseline hay assessment.

HTTP 200 trả DTO plan và ETag mới, ví dụ cấu trúc:

```json
{
  "id": "<planId>",
  "projectId": "<projectId>",
  "scope": [{ "routeVersionId": "<routeId>", "segmentSetId": "<setId>", "segmentIds": ["<segmentId>"], "targetBand": "SURFACE" }],
  "plannedAt": "2026-10-02T08:00:00+00:00",
  "status": "COMPLETED",
  "version": "<base64 plan rowversion>",
  "surveyType": "BASELINE",
  "completion": {
    "id": "<confirmation/auditId>", "at": "<UTC server time>", "actorId": "<PM ID>",
    "reason": "PM xác nhận đủ phạm vi kế hoạch qua hồ sơ đã đánh giá",
    "sources": [{ "taskId": "<taskId>", "datasetId": "<datasetId>", "assessmentId": "<assessmentId>" }]
  }
}
```

Đây là JSON minh họa contract; ID, version, timestamp phải lấy từ response thực tế. `completion` null khi chưa có căn cứ đã lưu; không suy diễn lịch sử. GET detail sau F5 đọc cùng căn cứ audit bất biến; list đọc `COMPLETED` và version mới. Sau đó PM tạo plan kỳ tiếp theo cùng khóa nghiệp vụ bằng key mới, pin route/set hiện hành. Plan cũ vẫn đọc được. Với plan cũ IN_PROGRESS, PM dùng cùng quy trình xác nhận; không tự backfill. Legacy thiếu/mơ hồ lineage bị chặn.

| HTTP/code | Ý nghĩa |
|---|---|
| 401 `unauthorized` | Thiếu/hết hiệu lực phiên hoặc Bearer |
| 403 `access_forbidden` / `csrf_failed` | Không còn quyền PM/project hoặc cookie thiếu/sai CSRF; replay cũng kiểm quyền hiện hành |
| 404 `survey_plan_not_found` | Không có plan đọc được qua contract này |
| 428 `validation_error` | Thiếu If-Match hoặc Idempotency-Key |
| 422 `survey_validation_failed` | Body/reason/danh sách nguồn không hợp lệ |
| 412 `auth_concurrency_conflict` | ETag cũ; tải lại trước một quyết định mới |
| 409 `duplicate_request` | Key đã dùng với fingerprint khác |
| 409 `survey_plan_completion_blocked` + `reasonCode` | Điều kiện nghiệp vụ chưa đạt |

Reason codes: `plan_not_in_progress`, `plan_lineage_unavailable`, `plan_scope_unavailable`, `plan_source_task_invalid`, `plan_dataset_lineage_invalid`, `plan_assessment_not_current`, `plan_source_scope_mismatch`, `plan_scope_not_satisfied`, `plan_unhandled_task_branch`.

Khi timeout, retry đúng key/body/If-Match cũ. Không tự đổi nguồn hoặc version trong retry. Khi 409/412 tải lại plan, task, assessment để PM quyết định, rồi tạo thao tác mới bằng key mới. Cùng key đồng thời không tạo trùng audit; khác key cạnh tranh vẫn chịu rowversion/trạng thái plan. Quyền bị thu hồi chặn cả replay.

## Kiểm chứng và triển khai

- CURRENT_VERIFIED: base `2ab6539` đã có CI PASS trước khi sửa. Kết quả local cuối ghi ở phần bàn giao; các lượt có thể trùng test, không cộng thành số ca độc lập.
- SQL thật cô lập, production authentication/DI; storage video giả lập trong fixture. Không kiểm Web FE, app Operator, SMTP/MinIO hay runtime FE trong đợt này. Không chạy full suite local thay cho CI.
- Không có migration, thay baseline, backfill hoặc thay SQL lane identity. Căn cứ đóng dùng AuditLogs hiện có; giữ nguyên dữ liệu cũ và receipt.
- Correlation ID `58900b1d-bae5-40a5-b008-5668a893b862`: đã tìm không thấy ID này trong log của 5 container API local còn lưu và các file log/jsonl/txt tại các checkout RoadGuard được truy cập. Chưa có log/audit đúng runtime của request gốc để đối chiếu. Không xác định endpoint/thời điểm/nguyên nhân request đó; các test tái hiện không phải bằng chứng log gốc.
- OpenAPI: [snapshot runtime](openapi.json), runtime `/swagger/v1/swagger.json`; route inventory kiểm trong `SandboxRouteAdmissionTests` và `SandboxOfflineRouteCatalogTests`.
- Sau đưa source lên nhánh: cập nhật backend đúng SHA, rồi FE mở phiên Web mới để kiểm. Không deploy hoặc áp migration lên DB FE từ task này.

### Kết quả local cuối (các lượt có trùng ca)

| Lệnh/nhóm | PASS / FAIL / SKIP |
|---|---|
| `dotnet test ...ApiTests.csproj --no-build --filter` Survey/P2 + ManualSwagger + route inventory + runtime Swagger | 51 / 0 / 0 |
| Nhóm cuối sau rà bổ sung guard và metadata legacy: Anh01SurveyFlowTests + Swagger mới + xuất OpenAPI runtime | 4 / 0 / 0; gồm 2 ca lifecycle đã có trong nhóm 51 |
| Nhóm trước có DiscoveryCollectionsHttpTests | 72 / 4 / 0 lúc còn 4 assertion/snapshot lỗi; tất cả 4 đã sửa và đạt trong nhóm 51. Phần collection không đổi, 25 ca được dùng lại |
| `dotnet test ...UnitTests.csproj --filter 'FullyQualifiedName~Surveys|FullyQualifiedName~SourceBoundary'` | 42 / 0 / 0 |
| `Verify-TestResults.ps1` trên từng TRX đạt | Đạt, không nới whitelist/gate |
| Build toàn solution và build API/tests sau các sửa cuối, analyzer bật | Đạt, 0 error; còn warning trong repo |
| Source contracts theo CI: docs, agent setup, readiness/guidance/repository, Compose, workflow, TRX gate, dependency security | 10 bước đạt |
| `dotnet format --verify-no-changes --no-restore`, `git diff --check` | Đạt |

Nhóm HTTP cuối có fixture âm thay **metadata nguồn** trên SQL cô lập để kiểm route/set/band khác pin và lineage legacy thiếu; khôi phục bằng finally. Không đổi trạng thái DB để cho luồng thành công. Luồng thành công đi qua HTTP. Kiểm cạnh tranh tạo plan và đóng plan, replay, audit một lần, không đổi baseline, task cha vẫn SUPPLEMENT_REQUIRED. Snapshot OpenAPI lấy từ test host bản cuối: loại route/schema Probe chỉ có trong test, giữ nguyên `/ready` của production không đổi; chỉ thêm route complete trong phạm vi.

Full API suite và 4 SQL lane chưa chạy local trong đợt này. Không gọi các nhóm tập trung là toàn bộ CI PASS. CI SHA mới được báo riêng sau push; không chờ CI hoàn tất theo quyền đã cấp.

Rà cuối: thêm ca Operator từ chối nhánh bổ sung rồi PM đóng vẫn bị chặn; PM hủy nhánh đó qua HTTP mới tiếp tục đóng được. Chạy lại riêng hai biến thể lifecycle sau sửa guard này.
Kết quả chạy lại sau guard REJECTED cuối: 2 PASS, 0 FAIL, 0 SKIP; build API/tests analyzer bật 0 error và format các file sửa cuối đạt. Đây là hai ca đã có trong các nhóm trên, không cộng thành ca độc lập.
