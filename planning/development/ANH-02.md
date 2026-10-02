# ANH-02 — BE–AI contract/mock → reporting/export → retention basis/hold

Revision spec: **1 — ASSIGNED theo phản hồi owner tại §10**, ngày 2026-10-02. Writer triển khai: **Anh / Codex local**, nhánh **anh-review**. ChatGPT đọc source, viết spec và review diff; Codex local triển khai toàn luồng, tự review/fix, kiểm thử, commit/push.

**Nguồn yêu cầu:** Anh yêu cầu chuẩn bị ANH-02 theo khuôn ANH-01. Yêu cầu này giao soạn spec; chưa tự động phê duyệt các public contract, công thức KPI hoặc quyền hold mới. Những nội dung TARGET_CONFIRMED không hỏi lại. Các lựa chọn mới được gom thành **ANH02-D1…D4 tại §10**; sau khi Anh chốt, cập nhật nguyên văn quyết định vào chính file này rồi giao triển khai một gói. Không tạo plan/coordination/report RF riêng.

**Revision khảo sát:** GitHub `anh-review` được kiểm tra ngày 2026-10-02, identical với **`efc0ca10b53264bb24c807b7352ddba0cbe36b6d`**. ANH-01 Part A correction `7e73e872 → 5d6ecb5c`; Part B integration `5d6ecb5c → efc0ca10`, đã được ChatGPT review source không có finding chặn bàn giao. Đây là mốc đọc source, **không phải lệnh reset**. HEAD/base/dirty của máy Anh tại lúc bắt đầu: Codex phải quan sát và ghi; phiên soạn spec không truy cập checkout local của Anh.

**Evidence:** phiên này đọc GitHub source/tests/docs, không chạy build/SQL/MinIO/Postman. Kết quả 76 unit, 19 SQL, 18 API pass + 1 skipped của ANH-01 là Codex báo cáo ở handoff, không phải acceptance mới cho ANH-02. ANH-01 và HUY-01 vẫn Partial.

## 1. Mục tiêu, nguồn và ranh giới

Một gói theo luồng: nguồn dataset/geometry đã xác minh → PM yêu cầu phân tích mock có manifest bất biến → kết quả có provenance cho Huy review → đọc dashboard/timeline từ facts thực → xuất hồ sơ hoặc nhãn được duyệt thành snapshot → theo dõi căn cứ lưu trữ, hold và đánh giá điều kiện xóa. BE giữ quyền quyết định nghiệp vụ; AI chỉ đề xuất. Người dùng xem được nguồn và phần thiếu, không có số 0/PASS giả từ producer chưa triển khai.

Giữ đủ: BE–AI contract và samples hai bước video analysis/matching; mock thực thi có thể kiểm end-to-end BE; AI candidate provenance; dashboard, drilldown, timeline; PDF/ZIP dossier export; approved training-label export; retention basis, hold/history, dry-run evaluation; migration/DI/Postman và integration với Huy. Không tự cắt các mục khó bằng cách chỉ viết DTO hoặc seed trực tiếp rồi gọi Done.

**Ngoài phạm vi đã chốt:** provider AI thật; AI processing retry/late-attempt remediation; compatibility callback A08-01; A09 remediation. Những finding này giữ OPEN/KNOWN_LIMITATION, giữ evidence. Không xây FE/Android/FastAPI; không làm HUY-02 repair/Fast Track/notification/offline/handover; không tự thêm graph nhánh/tấm; không thay PM manual assessment `pm-evidence-review.v1` bằng AI đánh giá tự động. **Retry/idempotency của upload, export và command nghiệp vụ mới vẫn phải có.**

ANH-02 được giao retention/hold và **đánh giá** điều kiện xóa. Nó không tự cấp phép xóa object/file/hồ sơ thật, purge worker hoặc thay backup infrastructure. Tác vụ phê duyệt/thực thi xóa nếu Anh muốn mở thêm phải có scope riêng về đối tượng, backup/restore, authority và recovery; không suy từ việc đủ tuổi lưu trữ. Không sửa generic delete guard để làm evaluator chạy được.

Không merge `develop`, thao tác `main`, sửa nhánh Huy, amend/force-push commit đang review hoặc áp migration DB chung/deployed. Không coi mục tiêu 1–2 ngày là bằng chứng khả thi/Done.

| Nhãn | Nguồn ở revision khảo sát | Ý nghĩa |
|---|---|---|
| TARGET_CONFIRMED | Bàn giao/trao đổi của Anh; `AGENTS.md`, `.agents/manifest.json`, bốn rules và module map; `planning/development/README.md` | Vai trò, ownership, một spec + một summary; `skills: []`, không khôi phục RoadGuard skills retired. Assignment mới của owner ưu tiên bảng package PROPOSED lịch sử. |
| TARGET_CONFIRMED | `docs/product/confirmed-decisions.md`: 33A, 34A, 38, 40, 41A, 44 | Matching trong project, PM duyệt label, limits, retention và external ownership. PR-40 là target chưa benchmark/restore. |
| TARGET_CONFIRMED | `planning/development/ANH-01.md` §10; HUY-01 checkpoint ghi D1=A…D4=A | PM manual baseline; Reporter riêng tư; chỉ current approved label được xuất mới; publication riêng từng recipient. Không hỏi lại các quyết định đó. |
| CURRENT_VERIFIED — static | Source ở §2; ANH/Huy producer v1 và HUY-01 shared handoff | Chứng minh implementation đang có trong repo, không chứng minh tất cả consumer đã chạy. |
| HISTORICAL / PROPOSED | `docs/product/historical-fr-br.md` FR-34/35/36, BR-20/45; `planning/refactor/02-decision-register.md` Q06/Q08 | Ý định dashboard/export và các chi tiết còn thiếu. Không tự nâng chữ CHỐT ở tài liệu cũ thành authority. |
| PROPOSED_DELTA_NOT_ENABLED | `docs/diagram/V2/AI_Integration/{README.md,openapi.yaml}`, `contracts/events/README.md`; V2-P2-039/040/052 | Tham khảo hai bước AI, PDF/ZIP, hold. Không adopt toàn draft, không relock FE, không lấy role/schema cũ làm sự thật hiện hành. |

**Lựa chọn thiết kế:** (A) mở rộng module theo trách nhiệm, reuse domain/persistence primitives và thêm sidecar provenance/snapshot — **đề nghị**; (B) dùng nguyên callback cũ cho mọi mock — ít code nhưng dễ gọi limitation cũ là verified và thiếu nguồn đáng tin cho Huy; (C) dựng generic workflow/reporting framework — tăng công việc và shared conflicts, không phù hợp đợt này. Spec theo A, không viết lại processing engine.

## 2. Tận dụng source; delta thực sự

| Luồng | Source đã đọc | Giữ/tận dụng | Delta ANH-02 |
|---|---|---|---|
| Job/callback | `API/Controllers/ProcessingV2Controller.cs`, `Services/Implementations/Processing/ProcessingV2Service.cs`, `Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs` | Existing create/get/retry/callback routes; domain ProcessingJob/Attempt/Block, manifest, receipt/outbox primitives | Contract mới có schemaVersion/provenance; mock nhập qua boundary riêng, không dùng callback cũ làm bằng chứng khắc phục A08/A09. |
| Validation | `ValidationRunWorker`, CreateValidationAsync/GetValidationAsync | Worker và bias/MAE/RMSE hiện hữu; giữ A08-02 stored Conflict/InvalidInput semantics | Report có thể đọc kết quả thật, ghi used/excluded/unit; không viết lại evaluator hay đặt threshold AI. |
| Dataset/geometry | `AssessmentDtos.cs`, `AssessmentContracts.cs`, `DatasetAssessment.cs`, ANH-01 source producers | Immutable dataset files/hash, metric geometry, segment/band, manual assessment và baseline pointers | Project-scoped manifest/snapshot reader; không suy coverage từ GPS/AI score. |
| Huy integration | `IAnhHuyProducerService`, `AnhHuyProducerService`, `AnhHuyFactsRepository`, HUY-01 shared handoff | Private/publication evidence, geometry and REPORT facts v1 | Additive AI source producer khi có trusted provenance; giữ v1 REPORT behavior, không đổi Reporter evidence thành project-public. |
| Defect/label | `BusinessObjects/Defects/Defect.cs`; HUY-01 label reader contract/handoff | Defect IDs, Huy candidate/approval policies | Consume real Huy reader. Label persistence/approved-reader chưa có trong `anh-review` khảo sát; không tự tạo domain cạnh tranh. |
| Reporting/export | Cây source hiện tại không có module reporting/export production tương ứng | AuditLogs, project/geometry/dataset read models, file/storage primitives | Module mới nhỏ, reads có nguồn, snapshot jobs và protected output; không coi kế hoạch V2 là implementation. |
| Retention | `StoredFile.RetentionUntil`, immutable Files guard trong DbContext, `Warranty.cs`, `WarrantyPersistenceService` | Warranty dates/scope, evidence identities và deny generic delete | Basis revisions, reference inventory, hold/history, evaluator. `RetentionUntil` đơn lẻ không chứng minh mọi nghĩa vụ đã đủ. |
| Messaging | `IOutboxWorkRepository` có generic lease API, chưa có filter event-type trong signature đọc được | Idempotency/audit/outbox data model | Không cho worker ANH-02 lease rồi nuốt event của Huy. Dùng job table riêng; chỉ emit consumer-owned event đã thống nhất. |

Các điểm phải giữ rõ:

- Legacy `POST /processing-jobs` nhận mode MOCK/REAL; service cho PM/Supervisor theo source. Upload/dataset không tự tạo job. Không đổi quyền hoặc xóa enum REAL ở API cũ dưới tên cleanup.
- Legacy callback fingerprint hiện dựa trên detections/checksum; source không chứng minh toàn protocol receipt/active-attempt. Spec mới không gán trusted provenance cho mọi detection cũ chỉ vì job Completed.
- `ResolveCandidateSourceAsync` v1 hiện hỗ trợ REPORT; AI/FIELD trả SourceNotReady. FIELD tiếp tục pending producer của Huy; AI chỉ Ready khi có provenance ANH-02 đủ và được nhận theo contract §3/§8.
- File upload identity immutable, `SizeBytes` long; DbContext cấm Modified/Deleted StoredFile. Artifact do server sinh không được giả một multipart UploadSession VERIFIED để qua guard.
- Warranty có project/road scope và các ngày không null nhưng mapping nghĩa vụ theo từng evidence, tính đầy đủ inventory và hold authority chưa tồn tại. Không backfill bằng `UploadedAt + 5 years`.
- Source cũ có thể gọi tài liệu RF-04 “int size/giới hạn int”; ANH-01 mới đã đổi long. Lấy source tại SHA mới làm hiện trạng, không lặp lỗi lịch sử đã sửa.

## 3. Phần I — BE–AI contract, manifest và mock có nguồn

### 3.1 Compatibility và phạm vi kích hoạt — ANH02-D1 + D4

Giữ nguyên endpoint/DTO/status hiện hữu của ProcessingV2Controller:

| Existing path, prefix `/api/v1` | Hành vi hiện hữu cần preserve |
|---|---|
| POST `/processing-jobs` | CreateProcessingJobRequestDto(datasetId, scope, modelVersionId, preprocessingVersion, configVersion, mode); Idempotency-Key; 202 + Location + ETag. |
| GET `/processing-jobs/{id}` | Job projection + ETag; PM/Supervisor/Operator theo policy hiện hữu. |
| POST `/processing-jobs/{id}/retry` | Existing Idempotency-Key/If-Match; **không mở remediation** hoặc dùng làm luồng nghiệm thu mới. |
| POST `/internal/processing-jobs/{id}/results` | AiCallback policy/service JWT audience `roadguard-be-ai`; existing schema và limitations giữ nguyên. Không dùng PM token gọi giả AI service. |
| POST `/projects/{id}/validation-runs`, GET `/validation-runs/{id}` | Giữ existing authority, receipt result semantics và output usedCount/excludedCount/bias/mae/rmse/unit. |

Contract mới `anh02.ai.v1` là **BE contract/mock verified** sau acceptance, chưa external provider accepted. Artifact contract tại `contracts/ai/anh02-ai-v1.md` và JSON samples dưới `contracts/ai/fixtures/anh02/`; không sửa draft V2 như thể toàn bộ đã active. Contract mô tả VIDEO_ANALYSIS → DUPLICATE_MATCHING; bước sau chỉ nhận output/snapshot đã kiểm tra, cả hai không tạo/merge defect hoặc approve label.

Đề nghị mock chạy bằng explicit PM command trong Development/Test, cấu hình `Anh02:MockEnabled=false` mặc định. Ngoài Development/Test luôn tắt kể cả flag=true. Không mở mock endpoint mặc định ở deployed environment. Nếu cần staging demo, Anh phải chỉ định môi trường đó; không đổi tên Production thành Development để bypass.

### 3.2 Manifest, request/result schema

`AiManifestV1` bất biến:

- `schemaVersion:"anh02.ai.v1"`, `runId`, `stage:"VIDEO_ANALYSIS"|"DUPLICATE_MATCHING"`, `mode:"MOCK"`, `projectId`, `jobId`, `attemptId` (duy nhất một processing attempt cho analysis mock v1; matching tham chiếu cùng attempt), `datasetVersionId`, `datasetManifestHash`, `routeVersionId`, `segmentSetId`, `geometryVersion`, `segmentIds[]`, `targetBand`.
- `modelVersionId`, `preprocessingVersion`, `configVersion`, `fixtureVersion`, `createdBy`, `createdAt` được BE resolve/lưu; model phải tồn tại/released theo existing model policy. Không seed model production hoặc activate model ngầm. `createdBy` là internal audit, không đưa user identity vào payload ra provider tương lai.
- `sourceFiles[]:{fileId,fileVersion,sha256,sizeBytes:int64,mediaType,purpose}`; `pairs[]:{videoFileId,telemetryFileId}` lấy pairing thật; thiếu telemetry được ghi `telemetryStatus`, không tạo GPS/CRS giả.
- Stage MATCHING thêm `analysisResultId`, `candidateSnapshotId`, `candidateSnapshotHash`, items `{defectId,version,segmentId?,routeVersionId}` server-scoped cùng project. Nếu chưa có Huy matching reader, stage này `source_not_ready`, không tự tìm toàn database.
- Hash SHA-256 trên UTF-8 canonical payload: schema field order cố định, dates UTC, GUID lowercase D, arrays tập hợp sort theo ID; arrays có thứ tự nghiệp vụ như tọa độ giữ nguyên. Loại `manifestHash` và URL/token khỏi input hash. Giữ canonical bytes/hash, không deserialize-reserialize để verify lịch sử bằng serializer khác.
- File transport là reader/scoped reference nội bộ; future signed read URL chỉ là transport hint, không nằm trong hash, không log secret và chưa là triển khai real-provider credential protocol.

Admission phải chứng minh dataset ServerConfirmed/integrity PASSED, các file thực VERIFIED và đúng dataset/project, scope là subset của immutable dataset scope, route/set/segment pairing đúng. Không coi integrity PASSED là manual coverage PASS và không buộc toàn dataset phải có baseline để xem proposal AI. Historical dataset giữ geometry refs gốc; manifest reader được đọc historical geometry đầy đủ (`requireCurrent=false`) nhưng phải match expectedGeometryVersion. Huy quyết định candidate trên current scope vẫn áp stale-geometry policy của HUY-01; không đổi route cũ sang current ngầm.

`AiResultV1`:

- `schemaVersion`, `runId`, `stage`, `mode`, `jobId`, `attemptId`, `manifestHash`, `modelVersionId`, `fixtureVersion`, `resultHash`, `completedAt`. Result hash dùng canonical bytes loại chính resultHash theo cùng quy tắc manifest.
- VIDEO_ANALYSIS: `detections[]:{detectionId,sourceVideoFileId,frameFileId,frameFileVersion,timestampMs,typeCode,confidence,bbox:[x,y,width,height],segmentId?,positionStatus,geometry?}`. confidence [0,1], bbox width/height >0, x/y≥0, x+w≤1, y+h≤1; timestamp không âm và phải trong nguồn có duration đáng tin. Không có duration thì không khẳng định đã verify upper bound; fixture phải có duration do generator/decoder xác minh.
- Frame phải là actual generated/verified image, có checksum/size và quan hệ source video + fixture derivation rõ. Không gắn file ngẫu nhiên cùng project làm frame. Geometry chỉ khi có source CRS/calibration/fixture metric provenance; thiếu thì null/UNKNOWN, không lấy tâm segment làm GPS “đo được”.
- MATCHING: `matches[]:{detectionId,candidateDefectId,candidateVersion,rank,score?,reasonCodes[]}`, mọi ref thuộc snapshot. Score là mô phỏng, không là recall/precision hay acceptance threshold; không tự merge và không tự tăng severity.
- Raw JSON result lưu thành immutable artifact/hash và FK provenance. Không lấy request metadata tự khai để tạo proof. Không gửi Reporter tên/email/contact hoặc evidence không có quyền sang mock payload/export training.

**Mock fixture v1:** repository-owned deterministic synthetic scenario, asset manifest ghi nguồn synthetic, duration/checksum/expected detections; chạy cùng run không đổi detection IDs/results. Có fixture không detection, thiếu telemetry, analysis có candidate/matching, rejected invalid refs. Không fake checksum/VERIFIED flag. Assets nhỏ đủ test; không yêu cầu video 8 GiB để demo AI. Bước VIDEO_ANALYSIS và MATCHING có contract tests riêng; trước khi Huy reader sẵn có thể mock transport/reader để kiểm adapter nhưng ghi đúng mock verified, integration gate còn mở.

Fixture có allowlist source asset hashes; dataset được upload qua luồng thật phải chứa sample tương ứng. Nguồn không match fixture trả422 `mock_fixture_source_mismatch`; không gắn hình synthetic vào video bất kỳ rồi gọi là frame trích xuất thật. Cùng fixture dùng project/geometry refs từ fixture setup đã xác minh, không chuyển tọa độ sample sang công trình thật bằng cách chỉ sửa SRID.

### 3.3 HTTP additive đề nghị

Prefix `/api/v1`; conventions §7. `source_not_ready` là thiếu fact/dependency, không tự đổi thành success rỗng.

| Endpoint | Role/request | Response và lỗi riêng |
|---|---|---|
| POST `/projects/{projectId}/ai-mock-runs` | Current PM project; Idempotency-Key; `{datasetId,scope:{routeVersionId,segmentSetId,segmentIds,targetBand},modelVersionId,preprocessingVersion,configVersion,fixtureVersion,stage,analysisRunId?,expectedGeometryVersion,candidateSnapshotId?}` | 202 `AiMockRunView` + Location + ETag; disabled404; wrong scope404; incomplete409 source_not_ready; stale409 candidate_stale; invalid422 ai_contract_invalid. MATCHING requires succeeded analysisRunId + snapshot; VIDEO_ANALYSIS forbids them. |
| GET `/projects/{projectId}/ai-mock-runs/{runId}` | Current PM/Supervisor project | 200 `{id,projectId,stage,mode:"MOCK",status,processingJobId,attemptId,manifestHash,resultId?,fixtureVersion,errorCode?,version}` + ETag; 403/404. |
| GET `/projects/{projectId}/ai-mock-runs/{runId}/result` | Current PM/Supervisor project | 200 typed result + source refs when succeeded;409 result_not_ready;404; không expose storage URI. |
| GET `/projects/{projectId}/ai-candidates/{detectionId}` | Current PM/Supervisor project; Huy gọi service interface nội bộ | 200 authoritative candidate provenance + geometry/source readiness; legacy thiếu proof409 source_not_ready; cross-project404. Không mutate decision. |

Mock run states `QUEUED → RUNNING → SUCCEEDED|FAILED`; worker recovery tiếp tục cùng run/attempt, **không tạo AI retry attempt**. Invalid fixture/facts → FAILED có code; transient local artifact write có thể tiếp tục cùng durable run sau restart. Không có public retry/cancel endpoint mới ở package này. Dùng existing ProcessingJob/Attempt/AIDetection làm source identities, thêm sidecar `AiMockRun`, `AiResultProvenance`, `AiDetectionProvenance`; không xây bảng AI detection cạnh tranh. MATCHING lưu recommendation riêng, không tạo processing defect hoặc fake FieldInspectionTask.

VIDEO_ANALYSIS admission tạo mock-run + ProcessingBlock/Job/Attempt cần thiết + immutable manifest + audit + receipt cùng transaction; reuse domain methods, không gọi legacy CreateAsync rồi tạo sidecar ở transaction khác. MATCHING admission tạo stage-run/snapshot/manifest/receipt riêng, tham chiếu jobId/attemptId của analysisRun đã SUCCEEDED, không tạo ProcessingJob/Attempt thứ hai. Identity result gồm runId+stage+jobId+attemptId; nhiều stage không được bị nhầm thành retry attempt. Không emit `processing_job.dispatch` cho mock nội bộ vì chưa có accepted consumer; worker poll đúng mock-run table. VIDEO_ANALYSIS completion ghi provenance/detections/job completion/run terminal/audit trong một transaction sau khi artifact durable. MATCHING completion chỉ ghi matching result/provenance và stage-run terminal; job analysis đã Completed giữ nguyên. Không mark SUCCEEDED trước bytes/hash.

Worker claim bằng RowVersion/lease trên run; cùng run chỉ một terminal commit. Cùng request/postcommit response loss trả cùng run ID; same key changed any source/version/fixture/stage →409. Unique detection IDs/run/result identity chống duplicate durable effect. Storage I/O ngoài SQL retry delegate; deterministic object key/run/version; crash sau bytes trước DB commit nhận diện bằng hash rồi hoàn tất cùng run, không xóa source để recovery.

Provenance mới phải đủ dataset → file → frame → manifest → model → run/attempt → detection. Legacy callback thiếu provenance không được tự upgrade. Race/correction về attempt cũ của legacy vẫn OPEN, không thêm test rồi tuyên bố A08/A09 Fixed.

## 4. Reporting — dashboard, KPI, drilldown và timeline

### 4.1 Query contract — ANH02-D2 + D4

Chỉ PM có current membership của project và Supervisor theo existing project guard; check active SQL user/role, không tin claim cũ. Admin/Reporter/Operator/Crew không tự có quyền dashboard toàn project. Read project đóng vẫn được nếu actor còn quyền; không mở business mutation trên project đóng. Reporter tiếp tục own-source/publication của Huy, không dùng reporting route nội bộ.

| Endpoint | Query → response | Tính chất |
|---|---|---|
| GET `/projects/{projectId}/reports/summary` | `from?`, `to?`, `routeVersionId?`, `segmentSetId?`, repeated `segmentIds?` → `ProjectSummaryV1` | 200 + ETag; query invalid400/422, cross-project geometry404. Thời gian từ inclusive/to exclusive, ISO UTC; cả hai cùng có hoặc cùng bỏ. |
| GET `/projects/{projectId}/reports/items` | `metric` + cùng filter + `cursor?`, `pageSize=50` max200 → `{schemaVersion,metric,readAt,items,nextCursor,availability}` | Drilldown cùng định nghĩa metric, không query khác mẫu số. Cursor opaque gồm filter hash và sort key; đổi filter với cursor400. |
| GET `/projects/{projectId}/reports/timeline` | `aggregateType`, `aggregateId`, `from?`, `to?`, `cursor?`, `pageSize=50` max200 → `{schemaVersion,readAt,items:[{eventId,occurredAt,actorDisplay?,action,source:{type,id,version},summary}],nextCursor,availability}` | Event/action allowlist, không trả raw audit JSON/contact/token; stable order occurredAt+eventId. |

`ProjectSummaryV1:{schemaVersion:"anh02.reporting.v1",projectId,readAt,definitionVersion:"pilot-reporting.v1",filters,metrics:[{code,dimensions:{status?,band?,routeVersionId?,segmentSetId?},value,unit,numerator?,denominator?,periodApplicable,availability:"AVAILABLE"|"PARTIAL"|"UNAVAILABLE",reasonCodes[],sourceRefs}],warnings[]}`. Grouped metrics có một numeric entry mỗi dimensions, không nhét JSON array vào value. ValidationRuns chi tiết đi drilldown, summary chỉ dẫn availability/source refs. UNAVAILABLE → value=null; AVAILABLE không có rows hợp lệ →0; mẫu số0 → ratio=null, reason `EMPTY_DENOMINATOR`. PARTIAL không được gắn nghĩa toàn dự án; raw supported count ghi kèm missing section. Nếu chưa có producer chính thì dùng UNAVAILABLE thay một số thiếu.

Summary hiện trạng tính **tại transaction readAt**, không hỗ trợ time-travel bằng to. `from/to` chỉ lọc các metric dạng “created in period” và event timeline; current stock/coverage ghi rõ `periodApplicable:false`. Không hứa lịch sử tại ngày X nếu chưa có temporal facts. Dashboard GET không ghi rows nghiệp vụ/receipt/outbox, không tự sửa case/defect hoặc baseline.

### 4.2 Định nghĩa metric đề nghị để chốt một lần

| Code | Công thức / nguồn / đơn vị | Phần thiếu và tránh đếm sai |
|---|---|---|
| `reportsReceived` | Count distinct Report.Id có ReceivedAt trong [from,to), project resolve từ active case link tại readAt | Reports khác nhau cùng lỗi vẫn là reports khác nhau. Unassigned không gán project giả. Relink có thể đổi project attribution hiện tại; không gọi đây là lịch sử phân công. `Report.ReceivedAt` đã có ở SHA khảo sát. |
| `casesByStatus` | Count distinct IncidentCase.Id assigned project tại readAt, group theo actual status | Không cộng số report links thành số case; không suy case Closed từ publication tồn tại. |
| `defectsByStatus` | Count distinct accepted Defect.Id project tại readAt, group theo Huy current state | Detection/candidate/rejected không tính defect. Null project legacy giữ outside known scope, không tự gán. |
| `surveyTasksByStatus` | Count distinct SurveyRequest.Id thuộc supported BAND_V1 flow/project hiện tại; group actual status | Supplement child là task riêng, có parent link để drilldown; không cộng dataset versions thành task. Legacy count riêng `legacyUnclassified`, không ép new state. |
| `baselineCoverageByBand` | Cho từng route/set current published trong selected scope: distinct segment/band có current baseline hợp lệ cả position/quality/coverage PASS chia distinct segments của set; trả numerator/denominator và percent | Tách SURFACE/LEFT_EDGE/RIGHT_EDGE. Mỗi segment một lần/band. Denominator0→null. Đây là **tỷ lệ segment có baseline**, không phải % diện tích hoặc chất lượng AI. Dùng length-based chỉ nếu owner chọn thêm, không đổi formula âm thầm. |
| `verifiedSourceBytes` | Sum distinct verified source FileIds linked supported dataset/project bằng checked long, không cộng file lặp qua nhiều versions | Tách source bytes với generated/export artifact bytes. Không scan private Reporter staging qua project. |
| `repairItemsByStatus` | Count distinct repair item theo producer HUY-02, group actual state | Chưa có producer →null/UNAVAILABLE; không dùng inspection tasks thay repair item. |
| `repairAcceptanceRate` | Số repair items có first acceptance decision PASS trong [from,to) / số repair items có first acceptance decision trong kỳ ×100; distinct item, không distinct attempts | HUY-02 phải cung cấp immutable decision/time. Mẫu số0→null; thiếu first-decision semantics→UNAVAILABLE. Rework/recurrence báo riêng, không đổi công thức. |
| `validationMetrics` | Đọc từng ValidationRun thật: bias/MAE/RMSE/unit/used/excluded/model/split/version | Không bình quân các RMSE khác unit/split, không đặt ngưỡng đạt hoặc gọi mock score là accuracy. |

Không tạo recurrence/SLA/KPI kinh tế theo ngưỡng tự đặt. Nếu cần, giữ mục feature với checkpoint formula/producer và xin đúng quyết định, không coi là đã triển khai. Phạm vi tấm bị owner loại ở ANH-01 nên reporting không dựng lại bảng tấm để đáp ứng câu lịch sử FR-34.

Timeline dùng durable event/history ID, de-duplicate theo identity, không dùng text/time gần nhau để đoán duplicate. Audit + business event cùng hành động phải chọn một authoritative source theo mapping; không union thành hai entries. Huy cung cấp projection đã redacted. Nếu chỉ có audit thô chưa map đủ, section PARTIAL/UNAVAILABLE kèm source coverage; không gọi comprehensive timeline.

### 4.3 Consistency và query implementation

Repository tạo summary trong read transaction nhất quán (SQL SNAPSHOT nếu DB đã cấu hình hỗ trợ; nếu chưa, dùng bounded SERIALIZABLE read và ghi isolation thực, không tự bật setting shared DB). REPEATABLE READ riêng lẻ không đủ chống phantom cho grouped counts/selection. Không dùng lần đọc COUNT độc lập ở các thời điểm khác nhau rồi gắn chung watermark. RowVersion không phải commit-order watermark đủ cho phân trang lịch sử khi chưa có snapshot; không dùng `MAX(rowversion)` để hứa snapshot chống phantom.

Ví dụ acceptance formula: current published set có3 segments, SURFACE có2 eligible baselines →2/3 (66.67% hiển thị), RIGHT_EDGE có1 →1/3; backend giữ numerator/denominator, không lấy percent đã làm tròn để tính tiếp. Hai reports cùng một case và một accepted defect phải cho reports=2/cases=1/defects=1. Set lịch sử không cộng vào denominator current.

Summary/drilldown requests độc lập phản ánh readAt riêng và có thể thay đổi. Muốn kết quả bất biến dùng export snapshot §5. Timeline cursor keyset ổn định nhưng không hứa ảnh chụp toàn kỳ bất biến. Thêm indexes theo project/status/date/FK thực có kế hoạch query; không dựng materialized warehouse/event sourcing hoặc cache framework. PR-40 chỉ được ghi measured khi có benchmark workload/environment/percentile thực.

## 5. Export hồ sơ và approved training labels

### 5.1 Contract — ANH02-D2 + D4

| Endpoint | Request/quyền | Response/lỗi |
|---|---|---|
| POST `/projects/{projectId}/exports` | PM/Supervisor current project; Idempotency-Key; `{kind:"DOSSIER"|"TRAINING",format:"PDF"|"ZIP",segmentIds?,defectIds?,from?,to?,includeOriginalFiles:boolean}` | 202 `ExportJobView` + Location + ETag; filter404/422; dependency503 producer_unavailable; training empty422 no_eligible_labels; malformed combination422 export_invalid. |
| GET `/projects/{projectId}/exports/{exportId}` | Current PM/Supervisor project | 200 `ExportJobView` + ETag;403/404. |
| GET `/projects/{projectId}/exports/{exportId}/manifest` | Cùng quyền | 200 immutable `ExportManifestV1` + ETag hash; trước snapshot409 export_not_ready; quyền thiếu403/404. Không expose URI/PII. |
| GET `/projects/{projectId}/exports/{exportId}/content` | Cùng quyền kiểm lại tại request | 200 stream application/pdf hoặc application/zip;409 export_not_ready;410 export_expired;503 export_storage_unavailable;403/404. Content-Disposition filename server-generated, không chứa path. |

DOSSIER PDF có summary, nguồn/version, chronology và missing sections; không nhét file video vào PDF. DOSSIER ZIP chứa PDF + manifest JSON + evidence index, và originals khi `includeOriginalFiles=true`. PDF + includeOriginalFiles=true trả422 để không silently bỏ originals. TRAINING chỉ ZIP; `includeOriginalFiles=true` nghĩa ảnh source cần thiết cho labels, không lấy mọi video của project. `defectIds` không áp TRAINING nếu producer không có mapping: reject422, không ignore filter.

`ExportJobView:{id,projectId,kind,format,status:"QUEUED"|"RUNNING"|"SUCCEEDED"|"FAILED",snapshotId,snapshotHash,artifactId?,createdAt,completedAt?,expiresAt?,errorCode?,completeness:"COMPLETE"|"PARTIAL",missingSections[],version}`. GET trạng thái không render/generate file. SUCCEEDED/PARTIAL là file tạo thành công có thiếu section được phép, không biến missing producer training thành thành công.

`ExportManifestV1:{schemaVersion:"anh02.export.v1",snapshotId,projectId,kind,format,requestedBy,createdAt,snapshotAt,definitionVersions,filters,sourceRevisions[],files:[{fileId,fileVersion,sha256,sizeBytes,mediaType,archivePath?,included,reasonCode?}],sections:[{name,availability,reasonCodes}],labels?[],snapshotHash}`. Opaque source versions vẫn cần store immutable IDs/revision proof; hash không thay FK/authorization. Không lưu signed URL, email/contact người báo hoặc secret. Model/mock provenance hiển thị `MOCK/SYNTHETIC` ở PDF/ZIP/manifest.

### 5.2 Snapshot, jobs, output và recovery

- Admission transaction kiểm current actor/filter/source access, materialize toàn bộ DTO/row facts và refs cần export thành immutable snapshot; ghi ExportJob+Snapshot+audit+receipt cùng transaction. Snapshot là **thời điểm request được nhận bền vững**, không thời điểm worker chạy. Snapshot lớn persist typed/chunk rows trong cùng transaction; không chỉ lưu query/filter rồi chạy query lại sau.
- Selection consistency dùng read isolation/locks thực §4.3. Snapshot giữ đúng label approvals/file refs tại commit. Render worker chỉ đọc snapshot, không nối current defect/label để thay nội dung. File identity immutable, bytes/hash vẫn verify khi đọc. Worker không giữ SQL transaction trong lúc stream/render ZIP.
- Claim job riêng bằng RowVersion/lease; worker restart tiếp tục **cùng snapshot**. Durable artifact storage key theo exportId/snapshotHash; bounded streaming + SHA256/size; bytes durable xong mới atomic attach artifact metadata + terminal job. Artifact table có unique export/snapshot, không tạo UploadSession VERIFIED giả. Reuse storage client qua interface phù hợp, không bypass private bucket.
- Artifact là server-generated file có verified checksum/bytes/provenance; có thể tạo StoredFile mới + GeneratedArtifact reference nhưng download đi route export với own authorization, không nới generic FileScope/private upload policy. Nếu dùng riêng ExportArtifact metadata thay StoredFile, retention resolver phải hỗ trợ rõ cả hai identities; **chọn một mô hình trong implementation, không giữ hai bản authoritative**. Đề nghị reuse StoredFile immutable + GeneratedArtifact source record, không cấp project-public scope để lách export expiry.
- Crash bytes đã write/DB chưa success: lần sau kiểm hash và hoàn tất đúng artifact; không generate khác snapshot. Permanent invalid/missing required bytes →FAILED code rõ. Transient storage fault giữ job recoverable; lease duration/scan/retry backoff là operational config có test deterministic, không dựng AI retry remediation.
- Fresh command retry dùng cùng idempotency key: same normalized payload replay cùng job/snapshot, kể cả requester mất response; khác payload409. Authority check trước replay. Không network/storage/render trong idempotency SQL delegate. Export copy không làm thay dữ liệu nguồn, không đóng defect, không đổi label/hold.
- Temporary export expiry **30 ngày ×24h từ completedAt UTC** theo đề nghị ANH02-D2; job/manifest/audit provenance vẫn theo nghĩa vụ hồ sơ, không xóa cùng binary tạm. Download hết hạn410 ngay cả object còn lưu do chưa purge; hold giữ bytes nhưng không tự gia hạn link/download. Không tạo TTL tự xóa bucket trong ANH-02.
- Nếu user mất project quyền sau enqueue: worker kiểm lại trước đọc private source/publish; không công bố artifact cho user đã mất quyền. Download luôn check current rights. Với private recipient-bound source cần resource-level permission hiện hành, project membership đơn thuần chưa đủ.

### 5.3 Privacy và phần thiếu

DOSSIER chỉ lấy source projection Huy cho phép PM/Supervisor, không scan Files/Reports rộng rồi lọc ở renderer. Triage không biến ảnh Reporter thành public; quyền internal dossier cũng cần producer explicit policy. Khi chưa có reader ảnh Reporter nội bộ được Huy xác nhận, xuất metadata nguồn được phép và ghi `REPORTER_EVIDENCE_ACCESS_NOT_AVAILABLE`; includeOriginalFiles không override privacy.

Missing repair/acceptance module có thể xuất dossier PARTIAL ghi mục thiếu. Ngược lại, file đã được chọn trong immutable snapshot để đóng gói mà mất bytes/hash sai →FAILED `export_source_unavailable`, không bỏ file rồi gọi COMPLETE. Schema/contract chưa có producer →503 khi export kind bắt buộc nguồn đó (TRAINING); optional dossier section →PARTIAL. Empty dossier hợp lệ vẫn ghi “chưa có dữ liệu”, không tạo evidence mẫu tự động.

PDF phải hiển thị tiếng Việt, font embedding hợp lệ, page numbers, timestamps/version/source và missing sections, không cắt bảng. ZIP chống path traversal/duplicate archive paths; checksum index; stream originals dài bằng buffer bounded. Không tự viết framework PDF. Chọn một thư viện PDF tối thiểu sau kiểm tra dependency hiện có, version/license/deployment requirements; ghi cụ thể trong summary trước thêm dependency, không nâng package unrelated. Nếu dependency chưa được chính sách repo cho phép, giữ đúng PDF gate và làm ZIP/snapshot phần độc lập, không tự coi ZIP thay PDF hoàn tất.

### 5.4 Training export — consume Huy, không duplicate approval policy

`IApprovedTrainingLabelReader` thuộc Huy, namespace/path freeze theo §8. Chỉ current APPROVED revision tại snapshot admission; query approval trong SQL. Output từng label gồm labelId/revision/revisionId, projectId, typeCode, annotation normalized bbox, fileId/version/hash/bytes/MIME, source kind/id/version và job/model/dataset refs khi có, approvalId/PM/time, reader schema version. Không dùng training label từ AI tự approve; không export PENDING/REJECTED hoặc approved revision cũ khi đã có head mới tại snapshot.

Reader phải tham gia cùng snapshot transaction của exporter hoặc cung cấp durable immutable snapshot token/materialized selection thực. Cursor/watermark tự khai không đủ chống sửa head giữa hai pages. Không giữ transaction mở xuyên nhiều HTTP request. Huy trả immutable projection nội bộ cho Anh; nếu signature ở nhánh Huy khác đề nghị, freeze một delta tương thích trước tích hợp, không tạo interface trùng tên hai chỗ.

Nếu label được tạo revision mới **sau** snapshot commit, export lịch sử vẫn chứa approved revision tại snapshot, đánh `asOf`; không regenerate/sửa file cũ, phù hợp HUY-01. Export mới phải loại revision cũ. Authorization/revocation vẫn kiểm hiện tại; đây là giữ snapshot lịch sử, không hứa thu hồi file người dùng đã tải. TEST có barrier trước/sau commit để chứng minh semantics.

TRAINING ZIP chứa `manifest.json`, `labels.jsonl` normalized bbox + approval/source refs, ảnh nguồn hợp lệ (khi include originals) và `README.txt` giải thích coordinate/mode/schema. Không âm thầm convert sang COCO/YOLO hoặc tự chia train/validation/test theo phần trăm. Split policy chưa được giao; optional split ID chỉ được ghi nếu producer authoritative có sẵn. MOCK/SYNTHETIC phải phân biệt trong manifest và không trộn vào dataset REAL không nhãn nguồn.

## 6. Retention basis, hold và evaluation

### 6.1 Policy đã xác nhận; authority mới đề nghị — ANH02-D3

PR-41A không hỏi lại: evidence/video nguồn/business audit giữ hết bảo hành +5 năm; technical logs90 ngày; temporary export30 ngày; backup35 ngày; hold chặn xóa; thiếu ngày hết hạn hoặc chưa đủ mọi nghĩa vụ →WAITING_RETENTION_BASIS. Không coi đây là luật/tiêu chuẩn đã nghiên cứu.

Đề nghị Supervisor duy nhất xác nhận/sửa bằng revision căn cứ và bật/gỡ hold; PM project được xem/evaluate và cung cấp danh sách warranty refs đề nghị qua nghiệp vụ hiện có, **không tự approve basis hoặc release hold**. Admin không được quyền chỉ vì role cao hơn. Authority thật resolve từ SQL + existing guard; nếu Supervisor global access hiện có, reuse đúng đó, không invent project membership riêng.

Hold hỗ trợ scope PROJECT và FILE, nhiều hold độc lập. Bất kỳ ACTIVE hold phù hợp chặn eligibility; release một hold không gỡ hold khác. Private/unassigned Reporter file cần Supervisor handling authority được chốt ở D3; không cho PM tra file private bằng ID. Hold không sửa file scope/read rights, không tự đóng case/dispute. Đừng thay nhiều hold bằng boolean dễ clear nhầm.

`basis` và `hold` là records mới; không mutate immutable `StoredFile.RetentionUntil` hay rewrite warranty/source facts. Giá trị cũ chỉ là evidence legacy, không quyết định deletion. Reason required cho confirm/supersede/release; actor/time/history immutable. Decision không đổi dữ liệu nguồn để làm ngày hết hạn sớm hơn.

### 6.2 Basis và reference inventory

Đối tượng v1: source/generated files và logical dossier refs liên quan; API evaluation trả file eligibility với references. Business audit/dossier metadata luôn giữ theo scope policy, không gán class TECHNICAL_LOG chỉ vì là log. Không invent purge mechanics cho backup90/35; ngày của backup tính từ backup artifact creation, technical log từ log event time khi nguồn đó có inventory; chưa có inventory →UNAVAILABLE, không đọc config rồi báo đã retention verified.

Resolver liệt kê authoritative references: FileScopes, SurveyFiles/source manifest/pairing, assessment/baseline evidence, project/handover/warranty documents, mock result/frame/source refs, Report/supplement/publication, candidate/defect/repair evidence, exports/snapshot đang sử dụng. Producer Huy chưa tích hợp hoặc có ambiguous legacy refs →`referenceInventoryComplete=false`; không trả ELIGIBLE. Private file chưa có obligation rõ →WAITING, không tính từ upload hoặc từ case triage guessed date.

Basis proposal chọn warranty IDs thực và đúng project/road obligations từ references; một file thuộc nhiều nghĩa vụ/projects phải xét **tất cả**. Không lấy max trên subset client gửi. `referenceInventoryVersion` là hash canonical inventory + source versions + relevant warranty facts; `expectedReferenceInventoryVersion` dùng detect drift. Supervisor xác nhận inventory đang biết và resolver chỉ đánh complete khi adapters cần thiết đều sẵn, refs được phân loại và không unresolved obligations. Lời xác nhận client không override missing adapter/unknown ref.

`RetentionBasisRevision:{id,fileId,revision,policyVersion:"pr41a.v1",classification,referenceInventoryVersion,warrantyRefs:[{id,projectId,scope,warrantyEndDate,sourceVersion}],inventoryComplete,confirmedBy,confirmedAt,reason,supersedesId?,version}`. Dates lấy từ warranty records; DTO không cho nhập deleteAt/retentionUntil tùy ý. Nếu ngày cần sửa, phải đi approved warranty nghiệp vụ riêng; không sửa ngay bảng Warranty trong evaluator. Supersede lưu revision mới và cập nhật head bằng rowversion; reference change làm basis stale/WAITING tới re-evaluation/confirmation phù hợp.

Evidence retention ngày: lấy latest applicable `WarrantyEndDate.AddYears(5)` theo calendar (29/2→28/2 năm không nhuận), giữ hết ngày đó theo **Asia/Ho_Chi_Minh**; `eligibleAfter` là đầu ngày kế tiếp chuyển UTC. Đề nghị này cần D3 vì PR41A chưa định nghĩa timezone/boundary chi tiết. Test chính xác trước/tại boundary. Nhiều nghĩa vụ có bất kỳ ngày/null/invalid/unknown scope →WAITING; không bỏ obligation thiếu để max phần còn lại. Warranty inactive/expired không tự mất nghĩa vụ hồ sơ lịch sử.

Ví dụ: hết bảo hành2027-10-02 →giữ hết2032-10-02 giờ Việt Nam →eligibleAfter2032-10-02T17:00:00Z, nếu mọi obligation/basis đều đầy đủ và không hold. Hết2028-02-29 →giữ hết2033-02-28. Đây là ví dụ kiểm chính sách đề nghị, không dữ liệu công trình thật.

Temporary export bytes dùng completedAt+30d UTC; nếu file đồng thời được dùng như evidence thì áp nghĩa vụ dài hơn cùng mọi hold, không chọn class ngắn để thoát giữ. Backup/technical log không dùng mẫu số warranty khi class thực được chứng minh; source inventory chưa được xây thì để non-file class UNAVAILABLE.

### 6.3 HTTP đề nghị

| Endpoint | Request/quyền | Response và lỗi riêng |
|---|---|---|
| GET `/projects/{projectId}/retention/files/{fileId}` | PM/Supervisor project, file phải nằm trong authorized inventory | 200 `{fileId,basisHeadId?,basisVersion,inventoryVersion,inventoryComplete,classification,applicableObligations,holdSummary,evaluation}` + ETag basisVersion;404 private/cross-scope; PM không thấy refs/IDs project khác, chỉ blocker `OTHER_SCOPE_OBLIGATIONS`. |
| PUT `/projects/{projectId}/retention/files/{fileId}/basis` | Supervisor; Idempotency-Key+If-Match; `{warrantyIds,expectedReferenceInventoryVersion,reason}` | 200 basis revision/head+ETag;422 basis_invalid;409 retention_inventory_stale/incomplete;412 stale head;404 wrong scope. Empty head version từ GET là `none`, If-Match `"none"`; unique head race tạo đúng một. |
| POST `/projects/{projectId}/retention/evaluations` | PM/Supervisor; Idempotency-Key; `{fileIds?:Guid[]}` (bỏ: toàn known inventory project) | 202 persisted evaluation job+Location+ETag; input limit500 explicit IDs, unique; không có `execute` flag. Snapshot/control records do server capture, worker đánh giá, không delete. |
| GET `/projects/{projectId}/retention/evaluations/{id}` | PM/Supervisor project | 200 `{id,status,evaluatedAt?,policyVersion,items:[{fileId,eligibility,reasonCodes,eligibleAfter?,basisVersion,inventoryVersion,holdVersion}],nextCursor?,version}`; paginate result nếu project lớn, page size50 max200. |
| POST `/retention/holds` | Supervisor; Idempotency-Key; `{scopeType:"PROJECT"|"FILE",scopeId,reason}` | 201 `{id,scopeType,scopeId,state:"ACTIVE",createdBy,createdAt,reason,version}` + Location/ETag;404 scope missing;403;422 invalid. FILE private/unassigned chỉ Supervisor có authority D3. |
| GET `/retention/holds/{holdId}` | Supervisor hoặc PM được đọc scope project tương ứng; private/unassigned PM404 | 200 hold view+ETag; không expose Reporter PII. |
| POST `/retention/holds/{holdId}/release` | Supervisor; Idempotency-Key+If-Match; `{reason}` | 200 state RELEASED+ETag;412 stale;409 invalid_transition nếu release mới trên terminal; replay hợp lệ trả receipt cũ. |

Scope PROJECT hold áp cả evidence có reference thuộc project, kể cả file private không đổi FileScope, và generated artifacts cần giữ; FILE hold áp exact file cùng mọi project refs. Endpoint project-file basis không cho xác nhận thay nghĩa vụ project khác; resolver/global Supervisor orchestration phải đủ rights cho mọi refs, nếu thiếu fail closed. Hold file ngoài project vẫn có history, không gán project giả.

### 6.4 Evaluation states, SQL và race

Evaluation trả `eligibility:"BLOCKED_HOLD"|"WAITING_RETENTION_BASIS"|"RETAIN_UNTIL"|"ELIGIBLE_FOR_REVIEW"`, kèm **toàn bộ** reasonCodes. Precedence chính: hold → unresolved/stale basis/inventory → future date → eligible. Ví dụ vừa hold vừa thiếu basis vẫn có hai reasons, release hold không biến thành eligible. `ELIGIBLE_FOR_REVIEW` tuyệt đối không là approved/deleted.

Evaluation snapshot ghi policy/basis/warranty/reference/hold versions và time. Khi đọc kết quả cũ, trả cùng snapshot và `isCurrent` computed từ authoritative versions (GET không ghi); nếu source đổi phải chạy evaluation mới. Không dùng result cached để thực thi deletion tương lai. Nếu sau này có deletion command, phải check lại cùng locks/versions tại điểm thực thi, kể cả hold mới sau approval; đây là acceptance cho future scope, không triển khai purge ở gói này.

Basis/hold commands: current authority → receipt fingerprint toàn resource+payload+expected version → version guard → history+head+audit+receipt cùng SQL transaction. Idempotency same request không nhân history. Nếu cần outbox event, chỉ thêm sau khi Huy chốt consumer; audit không phụ thuộc notification thành công.

Evaluation admission ghi job/filter/selection intent+audit+receipt; worker materialize/control evaluation trong transaction nhất quán tại **evaluatedAt** (khác export snapshot-at-admission), lưu immutable result rows cùng terminal state. Nếu retry/restart sau terminal giữ evaluatedAt/results cũ. Worker claim theo evaluation ID, không lease event của module khác. Concurrent basis/hold/reference mutation được serialize/phiên bản hóa; test không trả eligibility mới với hold version cũ trong cùng snapshot. GET/evaluation tuyệt đối không DELETE/UPDATE Files, buckets, audit, backup hoặc source documents.

## 7. Quy ước contract, SQL và migration dùng chung

### 7.1 HTTP/errors/auth

JSON camelCase, GUID string, UTC timestamps ISO-8601, date-only yyyy-MM-dd, int64 cho bytes/count sum; unknown null có reason. DTO allowlist, reject unknown mutation fields400; enum chưa hỗ trợ422. Strong ETag quoted opaque; DTO version cùng value không quotes. Commands có Idempotency-Key theo bảng, thiếu428 `validation_error`; If-Match thiếu428, sai/stale412 `concurrency_conflict`. Normalized payload fingerprint includes resource IDs, expected versions, filter/stage/fixture; không include correlationId.

Lỗi `application/problem+json`: status/title/detail/instance + code/correlationId; không secrets/stacktrace/raw DB messages. Common401 `unauthorized`,403 `access_forbidden`,404 `not_found`,409 `duplicate_request`,422 `validation_error`; specific codes theo bảng. Legacy route giữ actual existing codes/status, không đổi hàng loạt để “đồng nhất”. Nhận cookie/bearer qua Huy canonical principal; không tự thêm auth transport hoặc bypass CSRF. Test actual registered middleware khi integration có cookie.

Không cho service-to-service/worker lấy PM authority từ body. Current authorization trước receipt replay. Query project from authoritative resource, không tin projectId client để truy cập foreign ID; với private existence hiding dùng404, generic forbidden project dùng403. Không bật download unscoped signed URL cho artifacts/reporter evidence.

### 7.2 Persistence tối thiểu đề nghị

| Nhóm | Rows/indexes/concurrency |
|---|---|
| AI mock/provenance | AiMockRun FK project/job/attempt/source, unique run↔job as appropriate; immutable manifest/result refs, rowversion state; AiDetectionProvenance FK detection+source/frame; matching snapshot/result immutable. FK route-set-segment đúng pairing, source files verified trên command, không FK đơn lẻ giả chứng minh scope. |
| Exports | ExportJob rowversion + project/createdBy/status/lease; ExportSnapshot immutable facts/version hash; ExportSnapshotFile refs; GeneratedArtifact immutable hash/size/media/FK StoredFile and export; unique published artifact per export. |
| Retention | RetentionBasisHead unique file + rowversion; immutable BasisRevision + WarrantyRef rows; RetentionHold + immutable history; RetentionEvaluation/job/results snapshot; indexes target scope/state/project/file. |
| Shared primitives | Reuse IdempotencyRecords/AuditLogs; new operation namespaces `Anh02.*`; no new global job framework, no role/enum renumbering, no new notification consumer. |

Files class/type refs là server-owned classification. Constraints: positive bytes, valid enums/dates/bbox, unique immutable IDs and snapshot hash as needed; restrict cascades; filtered uniqueness/current-head constraints; correction revisions append, không UPDATE old result/snapshot. Đừng tạo SQL trigger cấm các state transitions được spec cho phép. Entity/model/shadow fields phải có documented materialization/write responsibility như HUY handoff.

### 7.3 Migration và recovery

Generate additive migration mới sau current HEAD migration; recheck pending Huy delta để chọn order. Không sửa `20261002120000_AnhHuySharedIntegration` hay các applied correction migrations. Fresh DB + upgrade từ `efc0ca10` bằng SQL fixture; audit counts/refs trước/sau; old routes/file ownership/source JSON giữ nguyên.

No broad backfill: old jobs không có trusted provenance vẫn unsupported; old files chưa basis giữ WAITING; old StoredFile.RetentionUntil không trở thành confirmed head. Existing data dùng nullable new FK/default disabled; không dùng Guid.Empty làm manufactured relation. Generated artifacts không sửa upload source schema để giả status.

Down khi tables rỗng: thực sự revert tới baseline và Up lại. Có exported/provenance/basis/hold/history data: guarded Down báo rõ cần owner-reviewed preservation plan; không drop mất evidence. Test FK/index ordering SQL Server, EF model snapshot alignment, no pending model changes. Không chạy migration/seed trên DB chung/deployed, không thay recovery chain lịch sử.

## 8. Ownership, interface với Huy và thứ tự tích hợp

### 8.1 File ownership

Anh writer module mới `BusinessObjects/{Reporting,Exports,Retention}/`, AI sidecar trong Processing; `DTOs/{Reporting,Exports,Retention,Processing}/`; `Services/Interfaces` + `Implementations` tương ứng; `Repositories/Interfaces` + `Implementations` tương ứng; controllers `AiMockRunsController`, `ReportingController`, `ExportsController`, `RetentionController`; workers mock/export/evaluation; tests `Anh02*`.

Anh điều phối Configurations, DbContext, migrations/snapshot, Program/shared DI/options, canonical local adoption records và integrated Postman. Sửa existing processing service/repository chỉ để extract/reuse hoặc thêm boundary được D1 duyệt, giữ legacy tests/semantics và A08/A09 limitation. Không sửa identity transport, Huy Report/Case/Candidate/Label/Repair policy hoặc notification dispatcher.

Huy writer label approved reader, candidate snapshot/internal dossier/timeline/repair facts và own domain/repositories. Anh cung cấp exact interface delta + fixture, tích hợp mapping/DI khi Huy có implementation; không đăng ký adapter fake trả empty. Nếu một writer khác đang giữ shared slot, ghi reservation/integration order ngay spec/summary và làm file độc lập. Không tự viết command consumer thay Huy.

### 8.2 Interface proposal v1, phải freeze tại exact integration commit

Đây là **PROPOSED signatures**, chưa tuyên bố đã có trong repo. Tên/path có thể align với interface Huy đã commit mới, semantics/fields phải giữ; freeze một chỗ và ghi SHA, không đồng thời tạo hai interface khác namespace cùng trách nhiệm.

| Interface — producer → consumer | Input và output bắt buộc | Failure/transaction/evidence |
|---|---|---|
| `IAiCandidateFactsReader.ResolveAsync` — Anh → Huy | actor/current PM, project, detectionId, expectedSourceVersion?, expectedGeometryVersion?, expectedDispositionVersion? → `AiCandidateFactsV1`: immutable detection+dataset/model/run/attempt/manifest/result/file/frame refs, mode, scope/geometry version, optional position, active disposition | Ready chỉ trusted new provenance; Forbidden/NotFound/SourceNotReady/StaleSource/StaleGeometry/StaleDisposition theo v1. Snapshot read không là lock; Huy recheck trong command. |
| `IMatchingCandidateSnapshotReader.CaptureAsync` — Huy → Anh | actor/project, geometry scope + detection IDs → `{snapshotId,hash,items:[defectId,version,segmentId?,routeVersionId],scope,createdAt}` | Same-project only, adjacency preference; no-GPS scope+ảnh; không auto merge. Required stage MATCHING; absent producer fail ready gate. |
| `IApprovedTrainingLabelReader.CaptureApprovedAsync` — Huy → Anh | current actor/project, normalized filters, shared SQL snapshot context → materialized `ApprovedLabelSnapshotV1` immutable revision/approval/file/source refs | SQL approved current-only; empty differs from unavailable. Capture atomic with export receipt or durable producer snapshot. No paginated watermark alone. |
| `IProjectDossierFactsReader.ReadAsync` — Huy → Anh | actor/project + filters/current resource permissions → typed case/defect/publication/repair facts + allowed evidence refs + availability sections | Internal projection permission explicit; never reuse recipient permission as project-wide grant; no raw Reporter PII. Missing section PARTIAL, mandatory training fail. |
| `IProjectTimelineReader.ReadAsync` — Huy → Anh | actor/project/aggregate/cursor → immutable event identity/time/action/version + redacted summary | Stable IDs, no duplicate audit+business event; no new event emission just for dashboard read. |
| `IFileReferenceInventoryReader.ResolveAsync` — Anh composite, Huy supplies domain refs | fileId + authorized service context → all known obligations/source refs, inventory hash, completeness, active usages | Missing consumer adapter/ambiguous legacy => incomplete; exact same facts for basis/evaluator; PM projection hides other-project identities. |

Để giữ `ResolvedCandidateSourceFacts` REPORT v1 (có CaseId/report evidence shape) không ép AI vào đó bằng Guid.Empty case hoặc private Reporter photo giả. Thêm typed AI method/interface; Huy dispatch theo source kind. Nếu chọn một discriminated union thay interface riêng, đó là version2 additive và cần consumer fixture, không breaking replace v1. Report producer giữ nguyên.

Đề nghị path shared contract mới `Services/Interfaces/Integration/Anh02Contracts.cs`, Huy interface implementation ở module của Huy; nếu `IApprovedTrainingLabelReader` đã có, Anh dùng đúng file ấy. Các records phải có concrete primitive fields đã nêu, không `object`/free-form JSON thay contract chính. Repository internal transaction binding dùng DbContext/UoW hiện hữu, không truyền DbContext ra HTTP/DTO hoặc tạo generic UoW framework chỉ để đủ interface.

### 8.3 Thứ tự thực hiện một gói

1. Preflight HEAD/dirty, freeze D1–D4 đã trả lời và interface delta với Huy. Anh không chờ toàn HUY-01 mới làm phần độc lập.
2. Manifest validators/read facts + deterministic mock adapter; schema sidecar; fixture independent (ghi mock-only nếu consumer chưa thật).
3. Retention pure evaluator/hold/basis và project/dataset reporting sources của Anh; missing Huy refs fail closed. Không chặn core vì label reader chưa xong.
4. Export snapshot/job/renderer/storage và authorization; dossier sections chưa có hiển thị thiếu; approved-label filter không viết thay Huy.
5. Tích hợp exact Huy producer commit đã thỏa thuận: label, candidate snapshot, dossier/timeline/inventory. Anh làm shared migration/DI/Postman một lần theo slot.
6. Chạy real BE chain source → mock → Huy candidate command; approved reader → exporter; source references → retention. Nếu producer chưa sẵn, hoàn tất độc lập và ghi đúng integration PENDING, không gọi whole package Done.

Công việc còn lại ANH-01 (live MinIO/8 GiB, CRS/WGS84/GPX dependency, demo dossier, deployed migration) không mất khi mở ANH-02. Reuse phần đã verified, không bắt chạy lại mọi gate cho mỗi commit. Chỉ rerun phần bị change invalidated. ANH-02 không nhận tự động trách nhiệm hoàn tất chúng trong cùng diff.

## 9. Acceptance và verification theo rủi ro

| ID | Case phải chứng minh | Evidence cần |
|---|---|---|
| B01 | Existing processing/create/get/validation wire giữ; A08-02 stored Conflict/InvalidInput replay không đổi thành success; no legacy callback/retry remediation | Focused regression source + tests thật khi paths bị đổi; không tuyên bố old limitations Fixed. |
| B02 | PM trigger mock explicit; upload/dataset alone không tạo job; inactive/wrong project/role deny; production mock disabled | Authenticated HTTP → isolated SQL, before/after counts. |
| B03 | Canonical hash stable cho set order, geometry sequence preserved; hash mismatch/foreign files/unverified frames reject | Unit + source-reader SQL + schema/JSON fixture tests. |
| B04 | VIDEO_ANALYSIS synthetic real bytes → persisted provenance; missing telemetry remains UNKNOWN; no auto defect/label/baseline | Actual BE producer, bounded storage mock phân biệt live. |
| B05 | MATCHING chỉ input valid analysis+candidate snapshot same project/version; stale candidate fail; no automatic merge | Huy real reader integration hoặc pending; mocked reader không đủ đóng gate. |
| B06 | Same key same payload/concurrent/response-loss → một run; changed job/source/fixture/version409; worker double-claim một terminal | Two SQL contexts + barriers/failure injection; không timing sleep tùy may. |
| B07 | AI facts typed source; legacy missing proof fail; PM/Huy current authorization/source version/disposition guards | Producer→consumer test, không CaseId Guid.Empty workaround. |
| B08 | Counts Report/Case/Defect/task khác nhau; multiple links/versions không nhân; nullable legacy/unsupported not zero | SQL fixtures có 2 reports/1 case/1 defect, supplement children, repeated files. |
| B09 | Baseline per band: numerator distinct current eligible, denominator current published segments; stale geometry/out-of-scope; denominator0 null | Actual ANH-01 HTTP-generated geometry/assessment/baseline ít nhất một chain. |
| B10 | Period inclusive/exclusive; stocks current không giả historical; repair acceptance first item decision không count attempts; unavailable explicit | Formula tests + Huy projection fixtures; missing producer gate. |
| B11 | Timeline retry/audit+event không duplicate, order/cursor deterministic, payload redacted, GET DB business snapshot unchanged | HTTP + SQL read invariance; wrong actor/project/aggregate. |
| B12 | Export admission snapshot; nguồn đổi sau commit không đổi PDF/ZIP; output manifests match selected versions | HTTP + SQL transaction + artifact content assertions. |
| B13 | TRAINING current APPROVED only, pending/rejected excluded; new revision before snapshot excludes old; after snapshot preserves historical copy | Real Huy SQL reader, concurrency barriers; no fake labels-only seed called integrated. |
| B14 | Reporter other-owner/private bytes không lọt dossier/training; include originals không bypass; download after revoke denies | Two reporters + PM wrong project + current revoked SQL user. |
| B15 | Export same-key replay/concurrent, precommit rollback, lost response, worker restart after artifact before commit, checksum failure | Durable job/snapshot/artifact/audit/receipt counts; no source deletion. |
| B16 | PDF Unicode/font/tables readable, ZIP filenames safe/hash true; large originals streaming, no all-file RAM | Render sample PDF + inspect pages; parse ZIP manifest; bounded-stream test; live MinIO separately. |
| B17 | Exact export expiry30d, hold preserves bytes but download410; worker required source missing fails, optional dossier section marked PARTIAL | TimeProvider boundary + HTTP/source/storage fixture. |
| B18 | Retention missing basis/unknown adapter/unassigned private/multiple obligations partial →WAITING; no guessed uploadedAt retention | Pure evaluator + real inventory SQL. |
| B19 | Latest valid warranty +5 calendar years end-of-day Vietnam; leap day and before/at UTC boundary | Table-driven date tests with documented expected instants. |
| B20 | Multiple holds, release one still blocked, wrong role/replay/stale If-Match, new hold vs evaluation race | HTTP + two-context SQL; history/audit/receipt atomic. |
| B21 | Basis change/source relink/new obligation invalidates inventory version; max subset cannot permit deletion; cross-project refs hidden | Real producers/SQL; no client-provided deleteAt. |
| B22 | Evaluation read-only source bytes/files; captured policy/version/time stable, GET isCurrent drift detection; no delete/purge registration | SQL before/after source snapshots + storage spy, DI/runtime configuration check. |
| B23 | Fresh + baseline upgrade preserve old data; constraints/hydration; empty Down→Up; populated Down guard | SQL Server disposable fixture, EF no pending model changes, migration diff review. |
| B24 | New Postman folders/variables/requests usable, previous IDs/folders unchanged; new contract adoption only local | JSON/schema diff + actual runner smoke nếu chạy; parse-only không gọi runner PASS. |
| B25 | End-to-end demo PM manual baseline → mock source → Huy review → dashboard → export → basis/hold/evaluate | Real BE HTTP+SQL when Huy ready; synthetic media tagged, external AI/Android/deploy NOT VERIFIED. |

Source test reuse: `P230ProcessingJobContractTests`, `P231ProcessingPersistenceTests`, `P234ValidationRunContractTests`, `A0802ValidationReplayTests`, `Anh01*`, `ReporterEvidenceApiTests`, `AnhHuyGeometrySourceTests`, `Huy01SharedSchemaTests`, persistence idempotency/outbox primitives. Read actual fixtures before execute; IntegrationTests name không tự chứng minh SQL runtime.

Test mới nhóm `Anh02*` theo module; có thể tag `Package=ANH-02`. Lệnh gợi ý, Codex kiểm tra paths/SDK/filter thật ở HEAD:

```sh
git status --short
git branch --show-current
git rev-parse HEAD
git diff --check
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter 'FullyQualifiedName~Anh02'
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter 'FullyQualifiedName~Anh02'
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter 'FullyQualifiedName~Anh02'
```

Thêm affected regression theo actual diff, không broad RF suite theo tên. EF command dùng tool/version hiện repo và design-only isolated connection đã kiểm; không cung cấp `database update` mặc định có thể trúng shared DB. Đọc seed/script/test fixture, verify connection ownership; no SQL =>NOT RUN và gate mở. 0 tests không PASS, skip không passed. Mock storage không phải live MinIO; mock AI không phải provider integration. Không hứa PR40 p95/RPO/RTO nếu không benchmark/restore.

## 10. Quyết định cần Anh chốt một lần

### Phản hồi owner đang active — 2026-10-02

Owner phản hồi câu hỏi D1–D4 bằng bảng dưới đây, không bằng chuỗi literal `D1=A…D4=A`. Giữ nguyên lời owner; triển khai phạm vi/invariants/output của spec theo phản hồi này, cấu trúc bảng/interface có thể đơn giản hóa. Không tạo package mới hoặc hỏi lại từng bước đã giao. Các gate producer Huy, PDF dependency facts, external/deployment vẫn riêng biệt.

| Phần | Đề xuất dành cho RoadGuard | Căn cứ |
|---|---|---|
| **AI contract/mock** | Tận dụng `ProcessingJob`, manifest, dataset và detection hiện có. Thêm adapter mock tối thiểu và provenance cần cho Huy; không dựng hệ thống job song song. Hai bước analysis/matching giữ contract, nối matching khi Huy có producer. | Repo đã có nền processing; thiếu nguồn đáng tin để Huy tiêu thụ. Callback/retry remediation cũ đã được loại. |
| **Reporting** | Làm trước số liệu project, survey/task, dataset và baseline từng band. Bổ sung Report/Case/Defect khi HUY-01 tích hợp; repair/acceptance khi HUY-02 cung cấp facts. | Nhóm đầu đã có producer của Anh; các nhóm sau thuộc phần Huy đang triển khai. Không cần chờ toàn bộ mới bắt đầu. |
| **Export** | Dùng **một cơ chế snapshot dùng chung** cho hồ sơ PDF/ZIP và training export. Huy chịu trách nhiệm trả nhãn current-approved, Anh đóng gói và kiểm quyền tải. | Tránh lặp business policy duyệt nhãn; giữ đúng ownership đã thống nhất. |
| **Retention** | Triển khai căn cứ bảo hành, nhiều hold độc lập và evaluator. Supervisor xác nhận/gỡ hold; PM xem và đánh giá trong project. Chưa chạy xóa thật. | PR-41A đã chốt thời hạn; dữ liệu hiện chưa có đủ inventory/authority/recovery để tự động xóa an toàn. |
| **Cách triển khai** | Một spec, một summary; Codex tự chọn cấu trúc đơn giản nhất đáp ứng contract và tests. Các bảng/interface trong thiết kế là đề nghị, chỉ invariant và đầu ra đã chốt mới bắt buộc. | Phù hợp hai người làm song song, tận dụng code và giảm thủ tục. |

Phần lựa chọn A/B bên dưới được giữ làm provenance thiết kế; phần ASSIGNED được xác định bởi phản hồi owner trên. Không coi producer Huy chưa có là đã được freeze hay nghiệm thu.

Đây là lựa chọn mới cho ANH-02; không hỏi lại ANH-01 D1–D5, HUY-01 D1–D4, PR34A/38/41A/44. Đề nghị phản hồi **`ANH02-D1=A, D2=A, D3=A, D4=A`** hoặc chỉnh đúng dòng cần đổi. Sau phản hồi, update chính file này thành ASSIGNED theo từng phần được chốt; giữ nguyên evidence/limitations, không dùng chữ PROPOSED cũ để xin xác nhận từng bước.

| ID | Phương án A đề nghị cụ thể | Nếu chưa chốt / lựa chọn khác |
|---|---|---|
| ANH02-D1 — mock/AI contract | Giữ legacy APIs/limitations; thêm `anh02.ai.v1` hai stage và explicit PM mock-run Development/Test, một attempt/run, synthetic provenance, không auto defect/merge/label/baseline. Không provider thật, không legacy callback/retry remediation. | Nếu muốn chỉ contract files/test harness, nói rõ; sẽ không có demo mock API. Nếu muốn deployed staging mock, chỉ định env. Chưa chốt: source/validator fixtures làm được, route/migration activation mới chờ. |
| ANH02-D2 — reporting/export | Duyệt metric definitions §4.2, snapshot tại admission, PDF/ZIP dossier + approved training export, current PM/Supervisor project read/export, missing module=null/PARTIAL, training mandatory producer fail closed; artifact download expires30d từ completedAt, hold không gia hạn download. | Có KPI khác, đổi formula/role/expiry anchor tại đây. Không suy SLA/recurrence hoặc AI quality threshold. Chưa chốt: read adapter/snapshot infrastructure thiết kế được, public KPI/exports chờ. |
| ANH02-D3 — retention authority/boundary | Supervisor confirm basis, create/release project/file holds (kể cả private/unassigned file trong queue có thẩm quyền); PM chỉ đọc/evaluate own project. Multiple holds; inventory đầy đủ mọi nghĩa vụ; evidence +5 calendar years giữ hết ngày Vietnam; chỉ evaluator ELIGIBLE_FOR_REVIEW, không purge/delete worker. | Nếu quyền hold/confirm khác hoặc muốn actual deletion, cần nêu actor và scope dữ liệu/backup/recovery; không tự mở deletion. Chưa chốt: pure evaluator theo PR41A + negative tests được chuẩn bị, public mutations chờ. |
| ANH02-D4 — adopt delta/ownership | Duyệt additive HTTP/DTO/errors/schema trong spec cho development; Anh writer migration/DI/contract/Postman, Huy writer own producers; generate/test isolated, commit/push anh-review, no shared DB/deploy. Cho chọn tối đa một PDF dependency cần thiết sau ghi exact version/license/reason theo repo policy; không unrelated upgrades. | Nếu consumer FE/Android có route/DTO constraint, cung cấp trước activation. Library/license/deployment chưa kiểm không tự gọi verified. Không chuyển quyền ghi file Huy hoặc merge develop/main. |

**Checkpoint tích hợp không phải câu hỏi nghiệp vụ:** Huy cần nhận/align interface §8 bằng exact commit; PDF dependency fact check; live MinIO; external provider/FE/Android; shared/deployed migration. Tiếp tục mọi phần độc lập trong gói, gom blocker một lần, không bắt Anh trả lời lại các lựa chọn đã chốt.

## 11. Prompt thực thi cho Codex local

Lưu spec tại `planning/development/ANH-02.md`. Copy khối dưới cùng nguyên văn câu trả lời D1–D4 của Anh. Không cần file prompt riêng.

```text
Bạn là Codex local của Anh, triển khai ANH-02 theo planning/development/ANH-02.md.
Repo HoangAnhVu2207/RoadGuardSystem; writer branch anh-review.
ChatGPT viết spec/review GitHub; bạn triển khai trọn gói, tự review/fix hai lượt,
chạy focused tests, cập nhật Postman và commit/push. Một spec + một summary.

1. Preflight git branch/status/HEAD; đọc AGENTS.md, manifest (skills=[]), relevant
rules/module map, development README và spec. Base khảo sát là efc0ca10b53264bb24c807b7352ddba0cbe36b6d;
recheck HEAD thực và related delta; không reset/amend/force-push về baseline.
Preserve dirty files. Không sửa checkout Huy; worktree riêng nếu có writer khác.
Shared files chỉ một writer; đọc current reservation trước sửa.

2. Ghi nguyên văn quyết định ANH02-D1…D4 owner đã gửi vào §10; mark ASSIGNED
đúng phần được chốt. Chưa có decision thì chuẩn bị phần độc lập của §10 và gom
câu hỏi, không tự activate contract/schema/business policy còn PROPOSED.
Đã chốt rồi thì triển khai đầy đủ, không hỏi lại từng API/migration isolated/test.

3. Làm gói theo dependency §8.3:
manifest/source → deterministic AI mock/provenance → Huy candidate facts;
reporting dashboard/drilldown/timeline → snapshot PDF/ZIP/training export;
retention basis/hold/inventory → evaluation, không actual delete.
Reuse ProcessingJob/Attempt/AIDetection, verified-file/geometry/dataset,
manual baseline, idempotency/audit và current project guard.
Không generic workflow framework hoặc no-op producer trả success rỗng.

4. Legacy processing APIs giữ compatibility. Không mở real AI provider,
AI retry/late-attempt remediation, compatibility callback A08-01/A09 remediation.
Findings excluded giữ OPEN/KNOWN_LIMITATION. New mock v1 là một attempt/run,
restart tiếp tục cùng run; không đổi mock thành REAL hoặc auto accept defect.
Retry/idempotency command/export vẫn phải làm. PM assessment vẫn thủ công.

5. Freeze interfaces §8 với exact Huy SHA. Không edit Huy domain/business policy.
Anh tích hợp mapping/DbContext/migrations/snapshot/DI/canonical adoption/Postman
theo slot. Huy chưa có reader thì explicit unavailable/pending, không giả consumer
hay gọi seed fixture là end-to-end. Không tự merge develop/main/nhánh Huy.

6. Controller → Service → Repository. Current authorization trước replay;
business/head/history/audit/receipt atomically commit. External storage/PDF I/O
ngoài SQL retry transaction. Hash/snapshot/provenance immutable. Strong version,
unique constraints/claims chống duplicate, test race bằng SQL hai contexts.
Export snapshot tại admission; label edit sau snapshot không rewrite export cũ;
download check current authority và expiry. Private Reporter evidence không
bị project export mở quyền. Missing mandatory bytes/producer fail closed.

7. Retention giữ đúng PR41A. File metadata/source immutable, no uploadedAt+5yr
guess; mọi nghĩa vụ+hold+inventory completeness; current reference drift invalidates
basis. ELIGIBLE_FOR_REVIEW không cấp quyền xóa. Không gọi Remove/DeleteObject,
không bật lifecycle purge, không áp migration/seed lên DB chung/deployed.

8. Acceptance §9. SQL Server isolated owned fixture, inspect scripts/connections.
Run affected build/unit/API/SQL/schema/storage cases, zero-test không PASS.
Kiểm PDF render tiếng Việt và ZIP contents/hash, synthetic assets/provenance.
Self-review1: auth/state/contract/transaction/replay/privacy/snapshot/retention.
Self-review2: ownership/dependency/migration recovery/compatibility/consumer/docs.
Fix in-scope, rerun invalidated checks, đọc final diff, git diff --check.

9. Bàn giao planning/development/ANH-02-summary.md ngắn: problem/result,
base/head/dirty preserved, decisions, interface integration SHA, changed scope,
hai review passes/fixes, exact commands và pass/fail/skip/not-run counts,
BE verified / mock verified / external-deployment not verified, limitations.
Commit/push thường origin anh-review; external ChatGPT review PENDING.
Không ZIP/audit RF/seven-part report. Không gọi ANH-02 Done khi required features
hoặc real Huy consumer/mapping/acceptance còn thiếu; tiếp tục phần độc lập.
```

## 12. Gate bàn giao và review tiếp

Done ANH-02 khi scope owner đã duyệt có actual implementation + acceptance: mock two-stage/provenance có nhãn mock; reports có formulas/source/drilldown và null handling; dossier PDF/ZIP + current-approved training export có real Huy reader; hold/basis/evaluator có complete inventory/race proof; shared mappings/migrations/DI/Postman tích hợp, không còn thiếu feature required. Không yêu cầu external provider thật để Done phần mock được giao, nhưng không gọi external/deployment verified.

Nếu còn Huy producer/MinIO/PDF dependency/runtime gate, ghi **ANH-02 Partial** với feature cụ thể, owner và điều kiện tiếp tục; không gom mọi thứ thành “chờ review” khi source chưa chạy. Giữ separate ANH-01 limitations, không dùng ANH-02 để đóng chúng. Không mở correction package RF chỉ để làm đẹp trạng thái.

Summary ngắn dùng:

```text
ANH-02 — AI contract/mock, reporting/export, retention/hold
Branch anh-review; Base <actual>; Head <actual>; dirty preserved <actual>.
Decisions active <ANH02-D…>; pending <specific checkpoint or none>.
Delivered <flows>; missing <feature/dependency>; shared/Huy integration <exact SHA>.
Self-review1 <findings/fixes>; self-review2 <findings/fixes>.
Checks <commands and actual passed/failed/skipped/not-run counts>.
Evidence: BE verified <...>; mock verified <...>; external/deployment <...>.
Known limitations <unchanged exclusions and new material limits>; ChatGPT review PENDING.
```

Anh gửi PR/compare URL + base/head thực. ChatGPT review đúng diff, finding có severity/file/symbol/impact; Codex sửa bằng commit mới cùng nhánh, không amend commit đang review.

## 13. Local execution — writer reservation và kiểm chứng

Preflight CURRENT_VERIFIED: `anh-review`, HEAD/base `efc0ca10b53264bb24c807b7352ddba0cbe36b6d`; dirty ban đầu không có; không reset. Manifest `skills:[]`; RoadGuard skills retired không kích hoạt.

Shared writer: Anh/root duy nhất ghi DbContext/migration/snapshot, DI/options wiring, contract adoption, integrated Postman và spec/summary. Ba agent giữ file module riêng reporting, export, retention; không commit/push riêng. Export writer giữ đúng một Services csproj PDFsharp reference; root tích hợp sau khi slot đó được trả. Dotnet verification chạy một slot tại một thời điểm.

- [x] Manifest/source admission: canonical bytes/hash, actual immutable dataset/file/scope/geometry, released model; TDD helper checks trước admission; actual HTTP/SQL negative scope/version/fixture checks.
- [x] AI sidecars reuse ProcessingJob/Attempt/AIDetection; worker table chỉ stage/provenance/claim. Một analysis attempt, matching cùng attempt. Checked-in synthetic MP4/frame có generator/decoder duration proof; durable frame bytes trước terminal; no old callback adoption.
- [x] Reporting source capture SERIALIZABLE: task/dataset/file/baseline per band, drilldown/cursor/redacted timeline, unsupported Huy metrics null/unavailable, GET invariant.
- [x] One export admission snapshot: DTO/row facts + receipt/audit atomic; worker storage/PDF outside SQL; immutable StoredFile + GeneratedArtifact, expiry/download/auth; training pending actual Huy reader. PDFsharp 6.2.3 MIT theo official nuspec, configured licensed Unicode font; no unrelated upgrade.
- [x] Retention warranty/basis/holds/history/evaluation: complete composite inventory hoặc WAITING; current snapshot/drift detection; no actual delete. Missing Huy inventory remains explicit incomplete.
- [x] Shared additive migration after 20261002120000; SQL fresh/upgrade/empty Down-Up/populated guard, model alignment; DI/workers/options safe defaults; module Postman compatibility.
- [x] Two concise self-reviews, affected focused tests, exact counts/NOT RUN trong ANH-02-summary.md; selective normal commit/push and base/head/compare.

Interface adoption: `Services/Interfaces/Integration/Anh02Contracts.cs` là local additive Anh contract. Existing REPORT v1 unchanged; AI typed interface không dùng CaseId giả. Huy matching/approved-label/resource-access implementations và exact consumer SHA **PENDING**, không đăng ký fake success. External AI/Android/deployment/MinIO/8GiB và ANH-01 CRS gate không đóng bằng package này.

**Nguồn GitHub cố định để đối chiếu:** [HEAD khảo sát](https://github.com/HoangAnhVu2207/RoadGuardSystem/commit/efc0ca10b53264bb24c807b7352ddba0cbe36b6d), [khuôn ANH-01](https://github.com/HoangAnhVu2207/RoadGuardSystem/blob/efc0ca10b53264bb24c807b7352ddba0cbe36b6d/planning/development/ANH-01.md), [HUY-01 handoff](https://github.com/HoangAnhVu2207/RoadGuardSystem/blob/efc0ca10b53264bb24c807b7352ddba0cbe36b6d/planning/development/HUY-01.md), [confirmed decisions](https://github.com/HoangAnhVu2207/RoadGuardSystem/blob/efc0ca10b53264bb24c807b7352ddba0cbe36b6d/docs/product/confirmed-decisions.md).

Acceptance hiện tại vẫn **Partial**: các checkbox implementation trên không đóng required live Huy gates. Real matching/approved-label/resource-access/inventory và exact consumer SHA chưa có ở `origin/huy-review` đã đọc `b8ec845d69ba2c71249cec6fb15e13a5bad8126f`; không merge hay sửa nhánh Huy. Matching adapter fixture chỉ MOCK_VERIFIED. Commands/counts, failures và NOT RUN nằm trong chính ANH-02-summary.md.
