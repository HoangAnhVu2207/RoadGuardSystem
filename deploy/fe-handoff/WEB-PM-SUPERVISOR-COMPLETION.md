# Web PM/Supervisor — bộ contract bổ sung

Bản follow-up trên base `947166e395e93c707def4db01f2770bb3d20f980`, nhánh làm việc `codex/web-pm-api-completion`. Trạng thái push/CI xem commit và bàn giao của bản này; không đồng nghĩa runtime FE đã được cập nhật. Các ID, version trong ví dụ là ký hiệu để thay bằng dữ liệu API thật, không phải dữ liệu môi trường FE.

## Quy ước tích hợp

- Prefix `/api/v1`. PM là ProjectManager còn membership hiệu lực; Sup là Supervisor hiện hành. Mọi GET kiểm lại quyền hiện tại. Cookie Web và Bearer là **hai lựa chọn**; Operator tiếp tục dùng Bearer.
- Web: đăng nhập bằng `/auth/web/login`, lấy `requestToken` từ `GET /auth/web/csrf`; gửi cookie với `credentials: "include"`. POST/PUT cần `X-CSRF-TOKEN`. GET không cần CSRF. Không đọc cookie HttpOnly bằng JS.
- Mutation mới: giữ nguyên body/key trước gửi, `Idempotency-Key` và `If-Match: "<version>"` của đúng resource. Retry mất response dùng nguyên key/body/version; sau conflict tải lại, người dùng xác nhận rồi gửi key mới. Replay vẫn kiểm quyền hiện hành, trả version tại lần thành công gốc.
- `401`: thiếu/hết hiệu lực phiên; `403 access_forbidden`: sai role/project hoặc nguồn; lỗi CSRF trả 403 theo middleware; `404 not_found`: không có resource phù hợp scope. Mutation metadata: `428 precondition_required` thiếu header, `400 validation_error` input/version sai, `409 concurrency_conflict` version cũ, `409 duplicate_request` cùng key khác fingerprint. Mã lỗi nằm trong ProblemDetails `code`, cùng `correlationId`.
- Null là dữ liệu chưa lưu/không có nguồn phù hợp; không tự thay bằng 0, người gửi hoặc trạng thái nghiệp vụ suy diễn. Những collection mới dùng `{items,nextCursor}`; `nextCursor: null` là hết trang, không tự tạo cursor.
- [OpenAPI runtime đã xuất](openapi.json); [catalog route BE](../../tests/RoadGuardSystem.ApiTests/Sandbox/SandboxOfflineRouteCatalogTests.cs). Snapshot lấy từ Swagger provider của bản local. Bản bàn giao loại route Probe của fixture và giữ `/ready` của cấu hình handoff. Runtime FE chưa được xác minh.

## 1. AI run: dataset và task nguồn

Giữ route `GET /projects/{projectId}/ai-mock-runs` và `GET /projects/{projectId}/ai-mock-runs/{runId}`. PM/Sup theo quyền AI và điều kiện bật AI mock hiện hành. Thêm filter collection `datasetId`, `surveyTaskId`; có thể kết hợp với `limit`, `cursor` và filter cũ. Giữ nguyên filter khi đọc trang tiếp, cursor gắn actor/project/filter.

List/detail thêm `datasetId`, `datasetVersionNo`, `datasetVersion`, `surveyTaskId`, `createdAt`. Nguồn: run → processing job → input block → dataset → survey → task, kiểm project/identity. `datasetVersionNo` là số lần nộp, `datasetVersion` là rowversion hiện hành của dataset; không phải revision AI. Lịch sử thiếu hoặc sai quan hệ trả null cho reference, không dò theo thời gian/tên file. `createdAt` lấy từ run đã lưu. Response create/replay cũ có thể chưa có reference; FE gọi detail sau create.

```json
{"items":[{"id":"<runId>","processingJobId":"<jobId>","datasetId":"<datasetId>","datasetVersionNo":1,"datasetVersion":"<base64>","surveyTaskId":"<taskId>","createdAt":"2026-11-18T00:00:00Z"}],"nextCursor":null}
```

FE chọn task → lọc runs theo task → mở run/result hiện hành. Đây là phần trích trường bổ sung; các trường stage/mode/status/resultId/version cũ vẫn có. Route có thể không đăng ký khi AI mock bị tắt; không báo FE sử dụng được trên runtime chưa kiểm cấu hình.

## 2. Video private và HTTP Range

`GET /files/{fileId}/content`: Web PM/Sup đọc `SURVEY_VIDEO` VERIFIED đã liên kết trong source manifest của dataset thuộc đúng task/project; Bearer Operator theo membership/assignment hiện hành. Lấy datasetId từ `GET /survey-tasks/{taskId}/datasets`, rồi fileId từ `GET /datasets/{id}` → `sourceFiles[]` purpose `SURVEY_VIDEO`. File chưa VERIFIED và nguồn không được cấp quyền không được phát. Quyền kiểm trên từng request, không phát signed URL.

```http
GET /api/v1/files/<fileId>/content
Range: bytes=1048576-
If-Range: "<ETag nhận từ response trước>"
```

- Không Range: `200`, `Content-Type` mediaType đã lưu, `Content-Length` toàn file, `ETag`, `Accept-Ranges: bytes` cho video.
- Một khoảng `bytes=0-999`, `bytes=1000-`, `bytes=-1000`: `206`, `Content-Range: bytes start-end/total`, length đúng đoạn. Cận cuối vượt total được cắt xuống.
- Sai cú pháp, khoảng rỗng/ngoài file, nhiều khoảng: `416`, `Content-Range: bytes */total`.
- If-Range chỉ hỗ trợ **strong ETag trùng chính xác**. ETag cũ, weak hoặc HTTP date trả toàn bộ `200`. Không quảng cáo hỗ trợ multipart ranges.
- S3 ByteRange đọc đúng đoạn, copy stream có cancellation/dispose; không nạp toàn bộ video để cắt trong RAM. Các file purpose khác giữ luồng tải cũ.
- Web `<video src="...">` cần cookie/CORS phù hợp runtime; dùng fetch cần credentials. Codec/container phụ thuộc player. Test byte dùng object fixture khoảng 1 MiB, không phải benchmark video hàng GB. Chưa kiểm browser/player thật, codec hoặc tua trên app. Contract upload giữ SURVEY_VIDEO `video/mp4`, tối đa8 GiB/file; dataset tối đa32 GiB theo UploadAdmissionPolicy. Các giới hạn này không xác nhận codec bên trong MP4 phát được.

## 3. Overview danh sách project và địa bàn

`GET /projects/overviews?ids=<projectId>&ids=<projectId2>`: PM/Sup, tối đa100 ID, một batch, không N+1 theo từng project. PM phải được quyền tất cả project yêu cầu. Response là mảng:

```json
[{"projectId":"<projectId>","name":"Dự án","locality":null,"engineeringUtmSrid":32648,"routes":[{"roadSectionId":"<roadId>","routeVersionId":"<versionId>","versionNo":1,"sourceSrid":32648,"crsProfileRevisionId":null,"lengthMeters":100.0}],"totalLengthMeters":100.0,"lengthAvailability":"AVAILABLE","warranties":[],"openDefects":{"definitionVersion":"defect.open.v1","includedStatuses":["OPEN","VERIFIED"],"value":0,"availability":"AVAILABLE","unknownCount":0},"version":"<projectVersion>"}]
```

`engineeringUtmSrid` là CRS kỹ thuật project (cũng có ở work-package), không thay thế SRID/profile của route. Tổng chỉ cộng **version IsCurrent** của mỗi road, không cộng tất cả revision. Ưu tiên native canonical length đã lưu; nếu không có chỉ dùng geometry.Length của SRID32648/32649. Không biết đơn vị/không có tuyến → length null; `MISSING`/`INCOMPLETE`/`AVAILABLE` tách rõ.

`warranties[]`: từng hồ sơ có `id,roadSectionId,handoverDate,startDate,endDate,status`; FE hiển thị nhiều hồ sơ, không lấy một ngày tùy ý thành ngày toàn project. Danh sách ngày bàn giao/bảo hành lấy từ Warranty, không giả là cùng ngày HandoverDocument được upload.

Sup `PUT /projects/{projectId}/locality` với If-Match từ overview, body `{"locality":"Địa bàn đã nhập","reason":"Bổ sung địa bàn"}`; `locality` nullable, tối đa500. Trả `200 {"version":"..."}` + ETag. Lưu DB, reload gọi overview. Luồng tạo project staging/bàn giao cũ giữ nguyên.

## 4. Notification metadata

`GET /notifications`, `GET /notifications/{id}` và `GET /notifications/{id}/scope` trả `priority`, `priorityPolicy`, `priorityReasonCode`, `priorityEvaluatedAt`. Scope còn trả `title,senderKind,deadlines[]`. Cùng `projectId,occurrenceId,classification,sourceKind,sourceId,eventType,resolverVersion` cũ để điều hướng theo loại nguồn; không phân tích message.

```json
{"notificationId":"<id>","projectId":"<projectId>","occurrenceId":null,"classification":"UNRESOLVED","sourceKind":"RepairItem","sourceId":"<itemId>","eventType":"<event>","resolverVersion":"legacy-visible.v1","title":"Thông báo","priority":"UNKNOWN","priorityPolicy":"MANDATORY_DEADLINE_V1","priorityReasonCode":"DEADLINE_SOURCE_INCOMPLETE","priorityEvaluatedAt":"2026-11-18T00:00:00Z","senderKind":"UNKNOWN","deadlines":[]}
```

`SYSTEM` chỉ khi có occurrence hệ thống đã lưu; không suy ra người gửi từ recipient/assignee/audit gần nhất. `MANDATORY_DEADLINE_V1` tính lại theo UTC server khi đọc. `HIGH` (`MANDATORY_DEADLINE_OVERDUE`) khi clock bắt buộc còn hiệu lực, chưa hoàn tất và đã tới hạn. `NORMAL` (`NO_CURRENT_OVERDUE_DEADLINE`) khi nguồn xác định đủ và không còn clock quá hạn. `UNKNOWN` (`DEADLINE_SOURCE_INCOMPLETE`) khi thiếu lineage hoặc không xác minh được clock hiện hành. FE có thể để UNKNOWN ở nhóm chưa xác định và giữ tương thích với response cũ `NOT_CONFIGURED`. Hạn đã đổi dùng `CurrentDueAt`. Hạn đã hoàn tất hoặc xác định không còn hiệu lực không tạo HIGH. Ưu tiên chỉ dùng để sắp xếp hiển thị, không phải severity hay SLA mới. POST đánh dấu đã đọc giữ receipt cũ; FE đọc lại GET để lấy priority mới. Clock liên quan theo source target/origin event đã lưu, cùng project; trả `clockId,dueAt,completedAt`, dùng API clock để đọc đầy đủ SLA. Ví dụ clock có thể xuất hiện ở thông báo PM review, Sup duyệt ban đầu/xác nhận cuối, kiểm an toàn lần đầu và xác nhận cảnh báo nguy hiểm. FE chỉ hiện deadline từ `deadlines[]` thực sự được trả. Không có clock thì nhận `[]`. Vị trí lấy tiếp từ API scope/geometry được cấp quyền. Thông báo legacy vẫn đọc nếu còn Visible(), reference không tự cho quyền mở nguồn. Mất quyền nguồn thì resolver từ chối/không trả scope theo contract cũ.

## 5. Liên hệ và đơn vị công tác

`GET /projects/{projectId}/member-contacts?limit=25&cursor=...`: PM/Sup, limit1–100, thứ tự membershipId SQL tăng dần, cursor opaque gắn actor/role/project. Chỉ membership active còn ngày hiệu lực và role khớp tài khoản hiện hành; không trả Reporter.

```json
{"items":[{"membershipId":"<id>","userId":"<id>","displayName":"Nhân sự","role":"REPAIR_CREW","email":"crew@example.test","phoneNumber":null,"phoneNumberConfirmed":false,"organization":null}],"nextCursor":null}
```

Thông tin tài khoản **hiện tại**, không snapshot lúc tham gia project. Null phone/org = chưa nhập; bị giới hạn quyền trả403, không giả null. Membership hết hiệu lực không nằm trong danh sách. `/profile` và global `/users` không đổi quyền.

Sup `GET /users/{userId}/organization` → `{userId,organization,version}` + ETag; `PUT` cùng path + If-Match/key/reason, body `{"organization":"Đơn vị công tác","reason":"Cập nhật hồ sơ"}`. Nullable, tối đa255. Đây là chuỗi đơn vị công tác đã lưu; không phải role, nhà thầu hay membership project mới.

## 6. Sup đọc Defect gốc theo repair item

`GET /projects/{projectId}/repair-packages/{packageId}/items/{itemId}/source-defect`: PM/Sup qua guard ReadItem hiện hành, kiểm item/package/obligation/project và trách nhiệm chuyển giao. Không mở global Defect write hoặc /users.

Trả `defectId,defectTypeCode,severity,status,geometryWkt,srid,dataKind,version,sources[],missingSections[],evidence[]`. `dataKind: CURRENT_SOURCE` là Defect hiện hành; khác snapshot/phương án item đã pin. Evidence chỉ có `evidenceId,contentPath,sourceKind,capturedAt`, không Reporter PII. Report resolver kiểm Case/current link; AI resolver kiểm detection/run/frame và version; download vẫn kiểm quyền nguồn tại thời điểm gọi.

```json
{"defectId":"<id>","defectTypeCode":"<code>","severity":"MEDIUM","status":"VERIFIED","geometryWkt":"POINT (500010 1100000)","srid":32648,"dataKind":"CURRENT_SOURCE","version":"<base64>","sources":[],"missingSections":["LEGACY_SOURCE_NOT_STORED"],"evidence":[]}
```

Không có source/evidence được phép → báo thiếu, không mở public ảnh hoặc đoán từ defect gần nhau. Source được đọc tối đa100 reference; nếu vượt có `SOURCE_READ_LIMIT`.

## 7. Chỉ số defect mở

Trong `GET /api/v1/projects/overviews?ids=<projectId>`: `definitionVersion: "defect.open.v2"`, đếm distinct Defect theo project của chính Defect, không đếm item và không lọc thời gian/tuyến/đoạn. `OPEN`/`VERIFIED` tính mở; `RESOLVED` không tính. Giữ cách loại `CLOSED`/`REJECTED` đã có. Quyết định bổ sung ngày 18/11/2026 chỉ phân loại khi đọc, không đổi trạng thái/backfill. `RESOLVED` không chứng minh “Đã nghiệm thu”, không mở quyền đánh giá hoặc bỏ điều kiện repair.

Project rỗng trả `value:0,availability:"AVAILABLE"`. Project có OPEN/VERIFIED/RESOLVED và không có trạng thái chưa phân loại trả số xác định. UNKNOWN, mã không nhận diện hoặc null → `value:null,availability:"INCOMPLETE",unknownCount>0,reasonCodes:["LEGACY_DEFECT_STATUS_UNCLASSIFIED"]`. DB hiện chặn null/mã ngoài enum; projection vẫn đọc nullable để không suy chúng thành 0 nếu gặp schema legacy. Thiếu bảng/cột nguồn defect trả `value:null,availability:"INCOMPLETE",reasonCodes:["DEFECT_SOURCE_UNAVAILABLE"]`; lỗi SQL khác tiếp tục fail, không trả số 0 giả.

Đọc trạng thái Defect hiện hành: item đã hoàn tất nhưng Defect còn OPEN/VERIFIED vẫn tính; reopen đưa Defect về OPEN thì lần GET sau tính lại. Nhiều obligation/item cùng Defect không nhân count. Metric reporting có bộ lọc riêng; không trình bày overview toàn project như metric đã lọc.

Ví dụ xác định:
```json
{"definitionVersion":"defect.open.v2","includedStatuses":["OPEN","VERIFIED"],"value":2,"availability":"AVAILABLE","unknownCount":0,"reasonCodes":[]}
```

## 8. Metadata kế hoạch repair

PM ghi; PM/Sup đọc đúng project/trách nhiệm hiện hành. Các GET có ETag; PUT If-Match của chính GET đó + key + reason.

- `GET/PUT /projects/{p}/repair-packages/{pkg}/planning`: `id,name,description,plannedDueAt,createdBy,createdAt,version`. Name255, description4000; ngày kế hoạch nullable. Creator/time chỉ từ audit `repair_package_created`, dữ liệu cũ thiếu audit trả null.
- `GET/PUT /projects/{p}/repair-packages/{pkg}/items/{item}/planning`: `id,plannedQuantity,plannedUnit,plannedMaterials,version`. Quantity decimal(18,6)>0 khi có, tối đa6 số lẻ; unit tối đa32 và phải có cùng quantity; materials tối đa4000. Gửi cả quantity/unit null để xóa.

```json
{"name":"Gói sửa dự kiến","description":"Phạm vi điều phối","plannedDueAt":"2026-12-01T00:00:00Z","reason":"Lập kế hoạch"}
```
```json
{"plannedQuantity":12.5,"plannedUnit":"m2","plannedMaterials":"Vật tư dự kiến","reason":"Lập dự toán sơ bộ"}
```

Đây là kế hoạch điều phối, không sửa RepairPlan/checklist đã duyệt, số đo hiện trường, tiêu hao vật tư thực tế hay deadline clock có thẩm quyền. Obligation vẫn giữ scope/trách nhiệm/mandatory hiện hành, không nhân bản các trường này lên obligation.

## 9. Tên assignment sau reload

`GET /projects/{projectId}/task-assignees?taskIds=<id>&taskIds=<id2>`: PM/Sup, tối đa100 task, batch Survey và FIELD thuộc project. Mảng gồm `taskId,taskKind,assignmentId,userId,displayName,nameSource,assignedAt,endedAt,current`. Trả cả lịch sử reassign dù actor đã rời project. Tên `CURRENT_ACCOUNT`, thiếu tài khoản/tên trả null, không gọi đây là snapshot lịch sử. Chỉ đọc ID task thuộc project, không mở danh bạ toàn cục.

Nguồn cũ `GET /projects/{projectId}/members` vẫn dùng cho membership hiện hành; không dùng danh sách active đó để kết luận assignment lịch sử không có người phụ trách.

## 10. Khám phá policy

`GET /projects/{projectId}/repair-policies?kind=REVISION&applicableOnly=true&defectTypeCode=<code>&limit=25&cursor=...`: **PM hiện hành**, Sup vẫn403 theo policy read authority cũ. `kind=DRAFT|REVISION`, mặc định REVISION. Stable SQL GUID order, limit1–100, cursor ràng actor/project/filter; không là snapshot bất biến khi dữ liệu đang đổi.

```json
{"items":[{"id":"<revisionId>","kind":"REVISION","state":"PUBLISHED","defectTypeCode":"<code>","checklistVersion":"v1","revision":1,"at":"2026-11-18T00:00:00Z","applicable":true,"version":"<h4-policy-version>"}],"nextCursor":null}
```

Collection dùng chọn ID; GET draft/revision hiện có để lấy định nghĩa đầy đủ và lý do. Version khớp detail. `applicableOnly=true` chỉ revision chưa revoked; draft trả rỗng. Không dùng list như giấy phép thực thi: command giao việc kiểm lại policy/defect/checklist. Revoke sau khi load có thể khiến command bị từ chối. `applicableOnly=false` giữ lịch sử revoked, revision đã pin vẫn được đọc qua guard lịch sử cũ.

## 11. Bulk đọc notification

`POST /notifications/read-batch`, role theo notification authority hiện hành. Web cần CSRF + key. Không cần If-Match từng notification.

```json
{"through":"2026-11-18T00:00:00Z","limit":100,"snapshotVersion":null}
```
```json
{"updated":100,"hasMore":true,"through":"2026-11-18T00:00:00Z","snapshotVersion":"<base64-rowversion>"}
```

Lần đầu chụp DB rowversion watermark; chỉ unread thuộc actor, hiện đọc được, OccurredAtUtc<=through và RowVersion<=watermark. Thông báo chèn mới kể cả backdate không lọt tập. Mỗi batch transaction nguyên khối, không partial success. Loop khi hasMore với cùng through/snapshotVersion, **key mới mỗi batch**; retry chính batch sau timeout dùng nguyên request/key. Concurrent cùng key replay một kết quả, không đếm đôi. `updated:0` là hợp lệ. Replay vẫn kiểm current visibility của các ID đã xử lý; mất quyền có thể403. API mark từng notification không đổi.

## 12. DOSSIER theo item/package

Giữ pipeline `POST /projects/{p}/exports` → `GET /projects/{p}/exports/{exportId}` → `GET .../{exportId}/manifest` → `GET .../{exportId}/content`. Dùng Web PM/Sup hoặc Bearer theo export authority **cũ** (bao gồm yêu cầu project membership của export); Sup không tự có toàn quyền export nếu guard cũ không cho. POST key/CSRF.

```json
{"kind":"DOSSIER","format":"ZIP","repairItemIds":["<itemId>"],"repairPackageIds":null,"includeOriginalFiles":true}
```

Chỉ DOSSIER; PDF hoặc ZIP. Tối đa100 item,20 package, hợp union tối đa100 item có quyền; package tự resolve item. Không trộn với segmentIds/defectIds/from/to. PDF không includeOriginalFiles. Không truyền scope repair giữ export cũ. Admission202 trả export id/status; poll đến SUCCEEDED trước tải. Reuse cùng key/body lấy cùng admission; không tạo lại snapshot theo dữ liệu mới khi replay.

Snapshot `repair.dossier.v1` gồm item/plan/checklist, scope obligation, attempts/evidence refs, FIELD measurements liên kết intake, PM reviews và quyết định cuối/correction đã lưu. PDF in nội dung; ZIP có `dossier.pdf`, `repair-dossier.json`, manifest/evidence index và file ảnh JPEG/PNG gốc khi request/authority/version/hash cho phép. Chỉ lấy item đã chọn, không lẫn item cùng package.

Thiếu final-confirmation/measurement/plan hoặc evidence không truy cập được được ghi trong sections/missing sections; không gọi hồ sơ đầy đủ khi thiếu. Byte limits, hash và policy artifact hiện hành vẫn áp dụng; vượt giới hạn trả lỗi job theo contract export cũ. Quyền recheck lúc admission, worker và download; snapshot là lịch sử, correction sau capture không sửa snapshot đã lưu. Nếu cần bản hiện tại phải create key mới. Xem `sections` theo item. `finalConfirmation` cho biết có bản ghi quyết định; `currentAcceptance` phản ánh kết quả có còn được chấp nhận tại lúc chụp. Sau reopen, snapshot mới trả `NO_EFFECTIVE_ACCEPTANCE` và vẫn giữ lịch sử các quyết định. File mất quyền/version sai khiến worker/download từ chối, không bỏ qua quyền vì đã render.

## 13. Summary survey, tránh DB N+1

Các collection hiện có `GET /projects/{p}/survey-plans` và `/survey-tasks` giữ cursor/filter/order. PM/Sup đọc summary từ batch queries cho cả trang, không gọi detail từng item tại DB. Operator giữ đường guard/read cũ.

- Plan thêm `plannedEndAt,scopeSummary[],scopeAvailability`.
- Task thêm `planId,operatorDisplayName,operatorNameSource,dueAt,datasetCount,scopeSummary[],scopeAvailability`, giữ operatorId/status/version/supplementTaskId.
- Scope row gồm `routeVersionId,segmentSetId,targetBand,segmentCount`. Đây là scope **đã lưu**, `STORED` không xác nhận geometry hiện tại còn đủ điều kiện. Legacy không có scope: `MISSING`, mảng rỗng. Mở detail để lấy scope đầy đủ/kiểm điều kiện.
- Name từ account hiện hành, datasetCount từ số dataset đã persist cho task; không suy ra assessment/baseline. Singular supplementTaskId giữ null nếu không duy nhất/không có link phù hợp; null không chứng minh chưa từng bổ sung.

## Migration, triển khai và bằng chứng

Migration mới `20261117183505_WebPmCoordinationMetadata`: 8 cột nullable trên Projects/Users/RepairPackages/RepairItems, check quantity+unit. Không có bảng mới, không seed dữ liệu mẫu thành production metadata. Snapshot/model parity, catalog chữ ký schema/migration đã đồng bộ theo SQL test nâng cấp từ `20261117101500_ProjectHandoverStaging`. Dữ liệu cũ giữ null.

Lệnh áp migration **sau khi đã review, build và chuẩn bị đúng cấu hình DB đích**:

```powershell
dotnet ef database update --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
```

Không tự chạy lên DB FE. Nâng DB trước runtime đọc các cột mới, cập nhật backend rồi FE kiểm các màn bằng runtime Swagger thực. Code local và OpenAPI không đồng nghĩa runtime đã cập nhật.

Kiểm chứng: TestServer đi qua login/session Web thật và Bearer, SQL Server cô lập; Range và artifact PDF/ZIP dùng MinIO local thật. Fixture Repair history được tạo ở SQL, không thay cho chứng minh toàn bộ Crew workflow qua HTTP. Nguồn Case/AI tái dùng resolver có kiểm quyền; Chrome local được kiểm trong follow-up bên dưới; môi trường FE, CORS triển khai và tải video hàng GB chưa kiểm. Xem bảng kết quả ở bàn giao cuối; các lượt test có thể trùng ca, không cộng thành số test độc lập.

Hai self-review local: (1) authority/scope, replay/concurrency, stream disposal, dữ liệu cũ; (2) wire DTO, schema upgrade/catalog, inventory/OpenAPI, test/handoff. Đây không phải review bên ngoài hoặc CI PASS.


## Ví dụ projection bổ sung

Assignment batch (mảng, không phân trang; giới hạn100 task mỗi request):

```json
[{"taskId":"<taskId>","taskKind":"SURVEY","assignmentId":"<assignmentId>","userId":"<operatorId>","displayName":"Tên hiện hành","nameSource":"CURRENT_ACCOUNT","assignedAt":"2026-11-18T00:00:00Z","endedAt":null,"current":true}]
```

Survey task summary (trích trường; các trường cũ vẫn có):

```json
{"items":[{"id":"<taskId>","projectId":"<projectId>","operatorId":"<operatorId>","status":"NEW_ASSIGNED","version":"<taskVersion>","supplementTaskId":null,"planId":"<planId>","operatorDisplayName":"Tên hiện hành","operatorNameSource":"CURRENT_ACCOUNT","dueAt":"2026-11-20T00:00:00Z","datasetCount":0,"scopeSummary":[{"routeVersionId":"<routeId>","segmentSetId":"<setId>","targetBand":"SURFACE","segmentCount":1}],"scopeAvailability":"STORED"}],"nextCursor":null}
```

Planning GET item (version là rowversion item, không phải task version):

```json
{"id":"<itemId>","plannedQuantity":12.5,"plannedUnit":"m2","plannedMaterials":"Vật tư dự kiến","version":"<itemVersion>"}
```

### Các mã lỗi cần tách theo API

- AI: giữ `ai_contract_invalid`, `source_not_ready` và error của pipeline hiện hành; filter GUID sai trả400.
- Video:401/403/404 theo quyền/file;503 `upload_storage_unavailable`;416 là response range không có ProblemDetails nghiệp vụ.
- Coordination/contact/policy:400 `validation_error`,401,403 `access_forbidden`,404 `not_found`; resource mutations thêm428 và409 như phần đầu.
- Export:422 `export_invalid`/`export_source_unavailable`,409 `idempotency_conflict`/`export_not_ready`,410 `export_expired`,503 `producer_unavailable`/`export_storage_unavailable`; lỗi nguồn sau admission nằm trong job.errorCode. Không retry tự động bằng key mới để bỏ qua lỗi quyền.

## Kết quả local và giới hạn nghiệm thu

Không cộng các lượt dưới đây thành số ca độc lập vì có ca trùng:

| Lượt | PASS | FAIL | SKIP | Bằng chứng |
|---|---:|---:|---:|---|
| API/contract cuối |48|0|0|web13-final.trx: Web session/Bearer, CSRF, metadata, bulk, survey summary, policy, AI, Range parser, Swagger/source/inventory|
| Metric và quyền metadata kiểm bổ sung |2|0|0|web13-metric-authority.trx; ca metadata có trùng lượt trên|
| MinIO thật cuối |2|0|0|web13-storage.trx: byte ranges; PDF/ZIP, ảnh gốc, item/package scope, thu hồi membership, recovery proof|
| SQL notification/export |9|0|0|web13-export-notification-sql.trx|
| Migration + Seeder ban đầu |2|2|0|2 lỗi fixture cũ dùng model mới ở schema cũ; không phải lỗi SQL engine|
| Hai migration sau sửa fixture |2|0|0|web13-migrations-recheck.trx; giữ model parity/history assertions|
| Unit export ban đầu |24|5|0|5 ca thiếu ANH02_TEST_FONT_PATH|
| Chạy lại đúng 5 unit với font |5|0|0|web13-export-unit-recheck.trx|

Build solution có analyzer:0 error, còn warnings được compiler báo (không tắt analyzer). Format toàn repo và diff-check phải đạt trước bàn giao. SQL migration/catalog và Development Seeder dùng DB Testcontainers riêng; không ghi DB sandbox dùng chung/DB FE. Không thêm/đổi identity SQL test nên không đổi sql-lanes.json hoặc Verify-SqlLanes.ps1. Full Unit/API/4 SQL lane và CI chưa chạy trong lượt này.

Giới hạn: chưa kiểm ma trận đầy đủ mọi source Case/AI bị đổi liên kết trong endpoint source-defect mới; các resolver quyền gốc được tái dùng. Export fixture kiểm immutable attempt và ảnh gốc, test MinIO này dùng fixture SQL. Chuỗi NORMAL qua HTTP riêng đã đi từ Crew nộp đến PM review, Sup xác nhận, tạo snapshot dossier, reopen và đọc hai manifest; snapshot lịch sử/guard correction có bộ kiểm riêng của repo. Đã kiểm Chrome local trong follow-up bên dưới; chưa kiểm app/dữ liệu FE hoặc storage triển khai thực. Priority đã dùng policy deadline. `RESOLVED` đã được owner chốt loại khỏi tập mở của metric v2; không thay đổi lifecycle.


### Kiểm bổ sung trước review

- CI base `de4e97b`, run `44142682988` trên `huy-review`. TRX `api-results` ghi `Assert.False()` ở `Huy01RetentionInventorySqlTests.cs:49`, expected False, actual True. HUY đã đọc đủ lớp nguồn của mình. Test hiện kiểm cả reference conclusion đúng project và aggregate vẫn `Complete=false` khi thiếu HUY02/AI/EXPORT reader. Không thay đổi logic retention production. Ca `UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio` bị skip trên CI do thiếu cấu hình MinIO smoke. [Job API gốc](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/44142682988/job/134249423273)
- Retention, policy reload/revoke, metric hỗn hợp và bulk 103 thông báo: 16 đạt, 0 lỗi, 0 skip. Bulk trả 100/3, replay cả hai batch, giữ cùng watermark và loại thông báo backdate chèn sau khi mở snapshot.
- Journey NORMAL, metric, scope notification, Swagger và route inventory: 5 đạt, 0 lỗi, 0 skip sau khi sửa payload correction của test sang `basis.evidenceReferenceIds`. Journey dùng ảnh có trạng thái VERIFIED từ fixture SQL. Đã kiểm snapshot dossier cũ giữ nguyên và manifest package mới giữ hai quyết định, trả `NO_EFFECTIVE_ACCEPTANCE`. Render/tải bytes PDF/ZIP được kiểm riêng bằng MinIO.
- MinIO follow-up: 3 đạt, 0 lỗi, 0 skip, bao gồm smoke test bị skip trên CI. Có kiểm đọc dải 10 byte trực tiếp từ provider, token đã hủy làm copy dừng, dispose và mở dải lại thành công. Giới hạn tại lượt lịch sử đó; follow-up bên dưới đã mở transport loopback cho Chrome và đo working set/managed allocation.
- FE kiểm lại sau migration và deploy backend: đăng nhập Web mới, reload danh sách và policy, đánh dấu batch tới `hasMore=false`, mở lại hồ sơ trước/sau reopen, và thử phát/tua video MP4 có quyền. Giữ nguyên giới hạn của fixture và kiểm lại dữ liệu thực ở môi trường đích.


## Checklist 13 mục khi FE kiểm lại

| Mục | Kịch bản kiểm bằng phiên Web mới | Bằng chứng BE và giới hạn |
|---|---|---|
| AI run | Chọn task rồi lọc run theo task/dataset, mở detail | API kiểm đúng dataset/version và phạm vi hiện hành |
| Video | Mở MP4 bằng Web PM/Sup, tua và kiểm request Range. Kiểm lại sau thu hồi quyền | SQL/MinIO đã kiểm byte `200/206/416`, Operator Bearer và hủy copy ở tầng storage. Chrome PM/Sup phát/tua đã đạt; đo tiến trình chung TestServer được ghi ở follow-up, không phải tải production |
| Overview | Mở batch project và đọc lại địa bàn sau khi Sup cập nhật | HTTP kiểm quyền, ETag và replay |
| Notification | So cùng một thông báo trên list, detail và scope. Đọc lại sau hạn hoặc khi deadline hoàn tất | HTTP kiểm thiếu nguồn và wire. SQL kiểm clock trước/sau hạn, đổi hạn và hoàn tất |
| Liên hệ | Reload danh bạ và kiểm thành viên hết hiệu lực biến mất | HTTP kiểm phạm vi project và Supervisor sửa đơn vị |
| Defect nguồn | Mở từ item/package, mở ảnh nếu còn quyền rồi thử lại sau đổi liên kết | Case đọc ảnh rồi merge và tải dossier được kiểm; dossier không tự đóng gói ảnh private Reporter hoặc frame AI. Xem phạm vi chặt hơn bên dưới |
| Defect mở | Kiểm số đang mở của project rỗng, project hỗn hợp và sau reopen | HTTP kiểm `0`, `INCOMPLETE` và đếm theo trạng thái hiện hành |
| Kế hoạch repair | Đọc ETag đúng resource, lưu rồi reload. Thử stale version | HTTP kiểm ETag, audit, replay và quyền |
| Người được giao | Tải lại tên sau reassign và xem lịch sử | Batch trả tên tài khoản hiện hành |
| Policy | PM đọc lại revision sau revoke và kiểm bộ lọc còn áp dụng | Phiên mới được kiểm; Supervisor không được mở collection |
| Bulk notification | Đánh dấu hơn 100 mục, tiếp tục cùng snapshot đến hết | HTTP kiểm 103 mục, replay và notification chèn muộn |
| Dossier | Xuất item/package, reopen rồi xuất lại bằng key mới | Chuỗi NORMAL qua HTTP và MinIO kiểm hai manifest, tải PDF trước/sau reopen, PDF cũ giữ nguyên và quyền tải mất khi membership bị thu hồi. ZIP và ảnh repair đã pin kiểm riêng |
| Survey summary | Đọc list rồi đối chiếu scope đầy đủ ở detail | HTTP kiểm dữ liệu summary và quyền theo project |

CI base `d39c8f8`, run `44237220044` trên `huy-review`, có 11 lỗi API và 2 lỗi SQL lane 1. TRX cho thấy nhóm API gồm exact wire Survey/AI/Notification cũ, Swagger bỏ mất header chung hoặc mô tả quá dài, và fixture migration dùng EF model mới trên schema cũ. Hai lỗi SQL đều ở `Anh02MigrationTests`, cột `Locality` chưa tồn tại trên schema downgrade. Bản sửa dùng đúng schema ở mốc lịch sử và vẫn kiểm dữ liệu sau upgrade. Không thêm migration, giữ `20261117183505_WebPmCoordinationMetadata` và catalog hiện hành. [CI base](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/44237220044)


Lượt chốt follow-up dùng SQL cô lập. Nhóm migration `Anh02MigrationTests` đạt 3/3. Nhóm deadline đạt 3/3 qua SQL, sau đó ca mở rộng kiểm task hủy đạt 1/1. Nhóm inbox, quyền và bulk đạt 50/50 sau khi sửa rollback/retry sidecar. Nhóm Range và PDF/ZIP trên MinIO đạt 2/2. Chuỗi NORMAL qua HTTP với PDF trên MinIO đạt 1/1. Các lượt có ca trùng nhau. Suite API tập trung đầu ghi 49 đạt và 1 lỗi fixture đọc bằng model mới ở schema cũ; ca migration này đạt ở lượt sau. Full suite chưa chạy local.

Hồ sơ repair chỉ đóng gói ảnh gốc đủ quyền từ evidence đã pin với attempt hoặc basis quyết định, đồng thời vẫn kiểm quyền đọc file nguồn. Dossier tổng quát không tự đưa ảnh Report/Case hoặc frame AI vào PDF/ZIP. Không có contract cho phép đóng gói mọi ảnh xem được ở `source-defect`; đợt này giữ phạm vi chặt hơn đó và kiểm việc loại ảnh khỏi archive.


## Follow-up chốt metric, video và CI — 18/11/2026

Base `947166e395e93c707def4db01f2770bb3d20f980`. Hai lượt self-review kiểm contract/quyền trước, rồi scope/fixture/tài liệu. Không có migration mới, không sửa workflow, gate TRX hay danh sách skip. Không thay đổi priority `HIGH/NORMAL/UNKNOWN`: vẫn dựa deadline bắt buộc còn hiệu lực và UTC server, không suy từ message hoặc severity.

**CI base:** [run 44240686879](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/44240686879), event push `huy-review`, đúng SHA base. Unit, source contracts, bốn SQL lane và job SQL tổng hợp pass. [Job API](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/44240686879/job/134587560639) đỏ tại `Verify-TestResults.ps1`, dù `api.trx` ghi 1.673 pass, 0 fail và 3 NotExecuted. Hai ca mới `WebPmVideoRangeHttpTests.RealMinioRangesPreserveBytesAndCurrentWebAndOperatorAuthority` và `WebPmCoordinationHttpTests.ScopedRepairExportPersistsOnlySelectedItemAndRendersPdfAndZipToRealStorage` bị skip ngoài whitelist. Ca smoke upload cũ là skip duy nhất được gate cho phép khi chưa cấu hình. Đã tải TRX và chạy lại đúng gate trên artifact gốc để tái hiện lỗi; API tải raw job log trả 403 khi không có credential GitHub, nên không gọi phần raw log đó là đã đọc. Job/step conclusion được đối chiếu qua API GitHub.

Hai ca mới dùng `[Fact]` và MinIO container/bucket/credential tạm do test sở hữu, theo image `RG_MINIO_IMAGE` mà CI đã build. Không còn phụ thuộc `ROADGUARD_MINIO_SMOKE` hoặc storage ngoài. Thiếu image hoặc lỗi provider làm test fail, không skip. Gate/whitelist giữ nguyên. Worker fixture chờ đúng job mình đã tạo và hoàn tất export package trước khi hủy storage, tránh giả định job vừa tạo luôn được claim đầu tiên.

| Lượt local | Pass | Fail | Skip | Phạm vi |
|---|---:|---:|---:|---|
| Nhóm cuối SQL/HTTP/Swagger |15|0|0|Metric, thiếu reader (fault injection SQL 208), metadata/policy/bulk, Case merge/split, AI MOCK, dossier và migration hiện có. Gate TRX pass |
| Hai ca MinIO sau bổ sung kiểm MP4 qua CLI |2|0|0|SQL + MinIO container thật; video và repair PDF/ZIP. Gate TRX với `-AllowMinioSmokeSkip` pass, thực tế không skip |
| Chrome local đã ghi nhận trước khi dừng Computer Use |1|0|0|PM/Sup login cookie thật, phát MP4 rồi tua tới phần chưa tải, tiếp tục phát |

Các lượt có ca trùng nhau, không cộng thành tổng test độc lập. Lượt nhóm trước từng có 14 pass/1 fail do fixture gọi worker một lần trong DB dùng chung; đã sửa chờ đúng job và chạy lại nhóm 15 ca đạt. Lượt browser đầu có lỗi test harness đổi tài khoản thiếu logout key; lượt thành công dùng logout/CSRF đúng contract. Lượt browser cuối bị dừng, không dùng làm PASS; phần byte MP4 được kiểm lại qua CLI. Full API/SQL suite của SHA mới chưa được chạy local; cần kết quả CI trên đúng SHA đã push.

Lệnh trọng tâm (PowerShell, SDK từ `global.json`):
```powershell
# RG_MINIO_IMAGE trỏ image MinIO test đã build; RG_TEST_UNICODE_FONT trỏ TTF có quyền nhúng.
# Để kiểm MP4 qua CLI, RG_BROWSER_VIDEO trỏ file MP4; không đặt RG_BROWSER_READY.
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter "FullyQualifiedName~WebPmVideoRangeHttpTests|FullyQualifiedName~ScopedRepairExportPersistsOnlySelectedItemAndRendersPdfAndZipToRealStorage" --logger "trx;LogFileName=minio-final.trx" --results-directory <isolated-results>
pwsh -File tests/CI/Verify-TestResults.ps1 -ResultsPath <isolated-results> -AllowMinioSmokeSkip
dotnet build RoadGuardSystem.slnx --no-restore
dotnet format --verify-no-changes --no-restore
git diff --check
```

Build/analyzer đạt (còn warning hiện có). Các script source contracts trong workflow đã chạy đạt; format toàn repo đã đạt. Runtime Swagger được sinh lại và đối chiếu: 260 paths, không có thay đổi route/schema/security so với snapshot `openapi.json`; quy tắc metric v2 và reason code nằm trong phần 7 ở trên. Không thêm/đổi identity SQL integration nên không đổi lane manifest.

### Bằng chứng video và giới hạn đo

- MP4 H.264 local dài 120 giây, kích thước `60.041.915` byte. Chrome qua transport loopback `http://localhost:18199` chuyển tiếp nguyên cookie tới pipeline auth thật TestServer, SQL và MinIO thật. Transport test giới hạn tốc độ để buộc seek ra ngoài buffer, không thay authentication hoặc cấp URL public.
- PM chỉ buffer `[0,1.875]` giây trước khi tua; sau tua/phát ở `90.8092` giây. Supervisor buffer `[0,3.248]`; sau tua/phát ở `90.8001`. Trace có `206`, `video/mp4`, `Content-Range`/`Content-Length`, gồm dải `41648128-60041914/60041915` và `46301184-60041914/60041915`.
- CLI cuối lấy 65.536 byte cuối bằng Web PM cookie: `206`, `Content-Range: bytes 59976379-60041914/60041915`, `Content-Length: 65536`, đối chiếu byte-for-byte với file MP4 gốc đạt. Các ca `200`, Range đầu/cuối/suffix/If-Range và `416` vẫn đạt cho PM/Sup cookie, Operator Bearer.
- Fixture lớn riêng `268.435.456` byte (256 MiB, dữ liệu byte để đo Range, không gọi là video có thể phát). Năm HTTP Range, mỗi dải 65.536 byte ở cuối file, đối chiếu nội dung đạt. Lượt CLI cuối đo `GC.GetTotalAllocatedBytes(true)` tăng `5.994.344` byte và `Process.WorkingSet64` tăng `376.832` byte. Client TestServer và BE chung tiến trình; đây là allocation toàn tiến trình và working set Windows tại hai thời điểm, không phải RSS riêng backend/container, peak hay load benchmark. Không suy khả năng phục vụ mọi kích thước/tải từ phép đo này.
- Provider `OpenRangeAsync` gửi `ByteRange(first,last)` tới MinIO; không đọc toàn object để cắt. Test cấp phát chặn hồi quy buffer toàn 256 MiB. Hủy copy với cancellation token, dispose rồi mở lại dải thành công. Chrome seek hủy request trước, bridge ghi upstream response đã dispose; controller dùng `await using` cho stream và truyền request cancellation vào copy. Chưa đo tài nguyên dưới tải hủy đồng thời dài hạn.
- Sau mất assignment Operator hoặc membership PM, request tải kế tiếp trả 403. Không cam kết thu hồi những byte đã gửi trước đó. Chưa kiểm app/player Tùng, CORS/reverse proxy hoặc runtime FE.

### Ảnh nguồn và dossier: giữ phạm vi quyền chặt hơn

- Report/Case: `SourceBackedExportHttpTests` dùng PNG hợp lệ, metadata/VERIFIED là fixture SQL cô lập, byte lưu trên MinIO thật. PM tải qua Case content path được 200 và đúng byte; route file chung vẫn 403 với ảnh private Reporter. Chuyển Report bằng API `report-links` có If-Match/key: URL Case cũ 404, URL Case mới 200 đúng ảnh. Test merge/split/khác project hiện có tiếp tục đạt.
- Dossier tổng quát chỉ đóng gói survey originals theo contract hiện hành. Metadata nguồn Report được giữ trong snapshot nhưng ảnh private không nằm trong `manifest.files` hoặc archive; section vẫn ghi `REPORTER_EVIDENCE_ACCESS_NOT_AVAILABLE`. Export mới sau merge không tự mở thêm ảnh. Archive cũ giữ nguyên byte, không ghi lại snapshot. Membership bị thu hồi chặn status/manifest/content bằng 403 trước khi mở storage.
- AI: chạy producer HTTP/worker `MOCK/SYNTHETIC`, frame/artifact dùng storage giả lập trong test. Frame có quyền tải được; dossier tổng quát không tự đóng gói frame này. SQL chặn sửa provenance bằng trigger bất biến; không giả lập một workflow chuyển frame/run mà BE không hỗ trợ. Dossier mới/cũ giữ giới hạn nguồn đó; archive cũ byte-for-byte không đổi, mất membership chặn cả frame và dossier bằng 403. Chưa chứng minh một archive được phép chứa mọi ảnh Report/Case lẫn AI, vì contract hiện hành không cấp quyền đó.
- Hồ sơ repair chọn item/package có kiểm riêng trên MinIO thật: chỉ ảnh đã pin và còn quyền từ attempt/basis mới được đóng gói; item ngoài lựa chọn không lẫn vào archive. Quyền dossier không cấp thêm quyền tải file nguồn.

FE: dùng metric v2 theo `value/availability/reasonCodes`, hiển thị chưa đầy đủ khi null; không đổi `RESOLVED` thành nhãn nghiệm thu. Giữ priority hiện hành. Phát/tua bằng cookie phiên Web hoặc Bearer đúng caller, xử lý 401/403 và không giữ URL ảnh Case cũ sau đổi liên kết. Sau backend được cập nhật, thử lại bằng phiên Web mới trên đúng môi trường FE. Đợt follow-up không có migration mới; nếu môi trường chưa có migration của bản trước, triển khai migration hiện hành trước backend rồi FE kiểm lại. Đã push source không đồng nghĩa đã deploy, seed hoặc áp migration lên DB FE.
