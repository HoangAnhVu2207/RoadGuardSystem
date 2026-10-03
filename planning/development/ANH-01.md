# ANH-01 — Project/road/warranty → geometry/segment → upload → survey/dataset

Revision spec: 2, 2026-10-02. Writer triển khai: **Anh / Codex local**, nhánh **anh-review**. ChatGPT viết spec và review diff; không triển khai thay Codex local.

Execution evidence 2026-10-02: base/initial HEAD
`1ecae797caaed1ab912b02b2372a1940d1e05375`; branch `anh-review`; initial dirty
paths: none. Anh is the sole shared-file writer for migrations/snapshot,
DI, local adoption contract and integrated Postman. Independent geometry,
survey and demo implementations were delegated with disjoint file ownership;
their reviews are self-review, not ChatGPT external review. No identity,
processing or Huy branch writes. Final verification and remaining acceptance
gates are recorded in the single ANH-01 delivery summary.

**Trạng thái: ASSIGNED** cho luồng đã chốt trong §10. Nguồn giao việc: yêu cầu gốc của Anh và các trả lời D1–D5 ngày 2026-10-02, đặc biệt “Tôi PM thủ công trong đợt này”. Phần I và các thiết kế D1/D2/D3/D4 tương ứng được triển khai trong một gói; chữ D hiện là mã quyết định, không còn là cổng hỏi lại. D5 là hồ sơ demo có nghiên cứu, không kích hoạt nghiệp vụ unknown-date/multi-document mới. Migration chỉ generate/test isolated và bàn giao; không áp DB chung/deployed. Các contract phối hợp Huy, CRS ngoài khả năng đã xác minh và external/deployment evidence vẫn phải được giải quyết đúng phạm vi, không được tự bịa hoặc bỏ qua.

**Revision khảo sát:** GitHub `anh-review` có HEAD `1ecae797caaed1ab912b02b2372a1940d1e05375`, kiểm tra qua branch API ngày 2026-10-02; parent `21223aa1d18f510d012f9f25081b8c4bcf87b7a8`. Đây là mốc đọc source, không phải lệnh reset. Dirty paths của máy Anh: **UNKNOWN**, Codex phải ghi khi bắt đầu. Khảo sát này đọc source/tests, **không chạy build/SQL/MinIO và không xác nhận deployed compatibility**.

## 1. Mục tiêu, nguồn và ranh giới

Một gói theo luồng: Supervisor tạo/quản lý dự án, PM chuẩn bị tuyến, Supervisor xác nhận geometry, PM công bố segment và giao khảo sát, Operator nhận việc/upload nguồn/nộp dataset, PM đánh giá thủ công có bằng chứng và xác nhận baseline theo segment/band bằng phương pháp pilot `pm-evidence-review.v1` đã được Anh chọn. Giữ project/version/source provenance để Huy dùng trong case, inspection và offline.

Không mở lại RF audit/remediation. Không triển khai ANH-02, provider AI thật, processing retry/late-attempt, compatibility callback A08-01 hoặc A09 remediation. Retry/idempotency **upload và command nghiệp vụ trong ANH-01 vẫn nằm trong gói**. Không xây FE, Android, hệ thống map provider, retention delete, processing job/AI adapter, reporting/export. Không merge develop, thao tác main, sửa nhánh Huy, amend/force-push commit đang review.

Nguồn đã đọc ở revision trên:

| Nhãn | Nguồn | Cách dùng |
|---|---|---|
| TARGET_CONFIRMED | Yêu cầu Anh 2026-10-02; `AGENTS.md`, `.agents/manifest.json`, `.agents/rules/{evidence,delivery,safety,review-and-coordination}.md`, `.agents/modules/README.md`, `planning/development/{README,spec-template}.md` | Vai trò, gói công việc, một writer, một spec + PR summary. Manifest `skills: []`; không tái kích hoạt RoadGuard skills retired. |
| TARGET_CONFIRMED | `docs/product/confirmed-decisions.md`: 38, 39A, 41A, 43A, 44; 37 cho reconnect | Ảnh 20 MiB; video 8 GiB; SRT 10 MiB; dataset 32 GiB; multipart/resume; gợi ý segment 100 m, PM đổi và chọn giữ/gộp dư; giữ provenance/offline geometry; chưa biết hết bảo hành không suy thời hạn xóa. Không hỏi lại các quyết định này. |
| HISTORICAL / PROPOSED | `docs/product/{requirements,workflows,data-and-quality,historical-fr-br,historical-pf}.md`; `planning/refactor/02-decision-register.md` | FR-04..09, FR-26..28/30, BR-01/34..38/40..43 mô tả luồng. Q-RF02-03 (old/V2), Q-RF02-05 (coverage) còn mở tại revision source; nay được D1/D4 trong §10 giải quyết cho đợt này. Đọc để thiết kế, không coi chữ CHỐT trong nguồn cũ là quyền triển khai mọi chi tiết. |
| CURRENT_VERIFIED — static | Controllers, DTOs, Services, Repositories, entities/tests ở §2 | Chứng minh source hiện tại, không phải test đã chạy. |
| PROPOSED / inactive | `contracts/README.md`, `contracts/events/README.md`, `docs/backend/{README,persistence-and-operations}.md` | Không dùng draft V2 hay `work-package.proposed.yaml` làm contract canonical tự động; không relock FE. |

Các số byte chính xác: 20 MiB = **20,971,520**; 8 GiB = **8,589,934,592**; 10 MiB = **10,485,760**; 32 GiB = **34,359,738,368**. Giới hạn tính trên file/dataset, không cộng cả đời segment. Thời gian 1–2 ngày là mục tiêu điều phối; Done dựa trên acceptance, không dựa trên giờ đã làm.

## 2. Tận dụng source; delta thực sự

| Luồng | Source/symbol hiện có | Giữ/tận dụng | Delta của ANH-01 |
|---|---|---|---|
| Project/PM/warranty | `ProjectsController`, `ProjectCreationService`, `ProjectUpdateService`, `PrimaryProjectManagerService`, `WarrantyCreationService`; persistence tương ứng | Tạo/sửa project, chuyển PM, handover/warranty, SQL transaction/idempotency | Chứng minh production flow qua HTTP/SQL; không viết lại CRUD. Thiếu ngày bảo hành, tài liệu nhiều file và sửa nghĩa vụ: D5. |
| Geometry/version | `ProjectRoadSectionsController`, `RoadSectionVersionService.CreateRoadSectionAsync/CreateRoadSectionVersionAsync`; `RoadSectionVersionPersistenceService` | Tuyến metric EPSG 32648/32649, version immutable và expected-current guard | Source hiện chỉ Supervisor ghi; chưa có PM draft/GPX/source CRS/width/station/confirm API: D1+D2. |
| Segment | `BusinessObjects/Projects/RoadSegmentSet.cs`, `RoadSegment.cs` | ID, route version, sequence; set status DRAFT/PUBLISHED/SUPERSEDED | Chưa có chainage/geometry/publish orchestration. Không coi seed segment là GIS hoàn chỉnh: D1+D2. |
| Project read | `ProjectWorkPackagesController`, `ProjectWorkPackageService`, `ProjectWorkPackageReadModel` | WKT metric, SRID, project/road/warranty projection | Bổ sung snapshot geometry/segment/destination qua endpoint mới; giữ DTO cũ: D1+D2. |
| Upload | `UploadsController`, `UploadService`, `UploadPersistenceService`, `UploadVerificationWorker`, `MinioUploadObjectStorage` | Multipart, signed URL, async verify checksum/size/MIME, protected content stream | I: giới hạn PR-38, long/bigint preparation, replay/recovery. D3: current permission/target policy; Reporter staging chờ Huy. |
| Survey | `SurveyPlanningController`, `SurveyV2Controller`, `SurveyPlanningService`, `SurveyV2Service`, `SurveyV2PersistenceService` | Plan/task, accept/decline/cancel/reassign, scopes và rowversion | I: giữ và kiểm tra flow đã có. D1: old/V2 authority, GET cần thiết, linking plan. D3: membership/device/supplement assignment. |
| Dataset | `SurveyV2PersistenceService.Dataset.cs.SubmitDatasetAsync`, `SurveyDataVersion.CreateSubmitted` | File verified cùng project/target, manifest checksum, scope subset, immutable version | I: sum long <=32 GiB. D3: device/assignment; D4: pairing, evaluation/baseline. |
| Coverage | `SurveyV2Service.GetDatasetCoverageAsync` | `UNKNOWN/UNKNOWN/UNKNOWN`, `coverage-not-evaluated.v1` | Chưa có đánh giá thực. Không đổi thành PASS vì upload hoàn tất; D4. |

Điểm cần xử lý có căn cứ source:

- `UploadCreateRequestDto.SizeBytes` là long nhưng `[Range(1,int.MaxValue)]`; `UploadService.IsValidCreate` và `UploadPersistenceService.CreateAsync` chặn/cast int; `StoredFile.SizeBytes` và mapping `Files.SizeBytes` vẫn int. Chỉ đổi DTO là chưa đủ.
- `UploadPersistenceService.GetPartUrlsAsync` trả `Replayed`; `UploadService` giữ trạng thái này nhưng `UploadsController.MapPartUrls` chỉ map `Success` → 200: replay có thể rơi xuống 422. Viết test tái hiện rồi sửa mapping.
- Initiate multipart hiện diễn ra trước idempotency receipt và có external effect; SQL execution retry không bảo đảm exactly-once object storage. Phải kiểm tra race và cửa sổ crash, không tuyên bố exactly-once từ SQL receipt.
- `MinioUploadObjectStorage.CompleteAndVerifyAsync` có fallback `NoSuchUpload` đọc object đã complete; exception từ fallback/GetObject nằm ngoài catch bao đầu tiên. Recovery phải có test, storage unavailable không được biến thành checksum failure.
- `SubmitDatasetAsync` hiện query file ID/hash/scope/verified nhưng không tổng size; `deviceId` mới kiểm tra nonempty, chưa chứng minh device thuộc Operator/project.
- Supplement hiện lưu Operator mới trong `MissingScope`, gọi `MarkSupplementRequired`, nhưng không tạo/đổi assignment và không thay scope tác nghiệp. D3 tại §10 đã chốt thay đổi semantics sang child task.
- Old/V2 survey dùng chung `SurveyPlans/SurveyRequests`; `Rf1003SameRowSurveyTests` ghi rõ old scope không luôn chiếu được sang V2. Không ép deserialize, không xóa route cũ.
- `UploadService.CanAccessAsync` dùng owner OR project membership; survey Operator được kiểm tra assignment, không luôn kiểm tra membership. D3 đã chốt policy mới; áp ở module services, không âm thầm đổi shared guard toàn hệ thống.

## 3. Phần I — giao làm ngay, một luồng upload → verified source → dataset

### 3.1 Phạm vi độc lập và checkpoint schema

Codex bắt đầu ngay bằng reuse các flow hiện hữu, sửa replay mapping, validation theo PR-38, durable resume/verification recovery, dataset size gate và focused tests. Có thể viết domain/DTO/persistence changes và **tạo migration mới để review, chạy migration trong SQL riêng**. Việc áp dụng schema trên DB dùng chung/deployed, bật nhận file >int trên môi trường có reader cũ là checkpoint rollout riêng, không được tự làm; D1 đã chốt thiết kế phát triển, không phải quyền ghi DB dùng chung. Thay đổi schema được bàn giao như proposed migration, không ghi rằng Anh đã phê duyệt triển khai DB.

Chuỗi nghiệm thu độc lập: fixture tạo project/road/segment references có sẵn → PM tạo task qua HTTP → Operator accept → create upload → lấy URL → retry/resume → complete 202 → verify → GET file/content → submit dataset 201 → GET coverage UNKNOWN. Fixture SQL được dùng cho segment chưa có production producer; ghi rõ giới hạn này. Sau D2, thay bước seed segment bằng production publish và chạy lại chuỗi thật.

### 3.2 Contract hiện hữu phải giữ

Các path dưới đây có prefix `/api/v1`. DTO JSON camelCase; GUID string; thời gian ISO-8601; date-only `yyyy-MM-dd`. Giữ shape và header cũ khi không có delta được duyệt. Response lỗi dùng `application/problem+json`, `status/title/detail/instance`, extension `code`, `correlationId`; middleware/model-binding có thể trả 400 trước service và cần test đúng wire.

| Endpoint | Request → response | Thành công / headers | Lỗi và retry |
|---|---|---|---|
| POST `/uploads` | `UploadCreateRequestDto`: purpose, projectId, targetId?, fileName, mediaType, sizeBytes:int64, checksumSha256 → `UploadSessionResponseDto`: id,fileId,status,partSizeBytes,expiresAt,version | 201, Location `/api/v1/uploads/{id}`, ETag quoted version | Idempotency-Key bắt buộc; thiếu 428 `validation_error`; sai semantic 422 `upload_validation_failed`; forbidden 403 `access_forbidden`; same key/payload replay 201 cùng IDs; khác payload 409 `duplicate_request`. |
| GET `/uploads/{id}` | Không body → session DTO | 200 + ETag | 404 `upload_session_not_found`, 403; GET không ghi SQL nghiệp vụ. |
| POST `/uploads/{id}/part-urls` | `{partNumbers:int[]}` → `{parts:[{partNumber,url,expiresAt}]}` | 200 cả lần đầu và replay | Idempotency-Key; 428/403/404/409/422 như source; 503 `upload_storage_unavailable`. Danh sách nonempty, unique, 1..ceil(size/partSize). Không trả URL cho session terminal/hết hạn. |
| POST `/uploads/{id}/complete` | `{parts:[{partNumber,eTag}],checksumSha256}` → session DTO | 202 + Location + ETag, status VERIFYING | Idempotency-Key + If-Match; thiếu 428; stale/malformed version theo source 412 `concurrency_conflict`; key conflict 409; parts/checksum invalid 422. Replay cùng request trả receipt cũ 202, không complete/ghi audit thêm. |
| GET `/files/{id}` | → `{id,status,checksumSha256,mediaType,sizeBytes:int64,version}` | 200 + ETag | Status VERIFIED/FAILED/PENDING; 403/404 `file_not_found`; không trả object key/signed upload URL. |
| GET `/files/{id}/content` | → stream | 200 với MIME đã verify | Chỉ VERIFIED; 403/404/503; không load video vào byte[] toàn bộ. |
| POST `/survey-tasks/{id}/datasets` | `SubmitDatasetRequestDto`: videoFileIds[],telemetryFileIds[],recordedAt,deviceId,scope[] → `DatasetResponseDto`: id,surveyTaskId,dataVersionId,integrityStatus,telemetryStatus,version | 201 + Location `/api/v1/datasets/{id}` + ETag của dataset | Idempotency-Key + If-Match **task**; 428; 403; 404 `survey_request_not_found`; 412; 409 `duplicate_request` hoặc `survey_invalid_state_transition`; 422 `survey_validation_failed`. |
| GET `/datasets/{id}/coverage` | → datasetId,items[{scope,positionCoverage,qualityCoverage,overallCoverage,reasons[]}],methodVersion | 200; hiện UNKNOWN; đọc không ghi | PM/Supervisor theo guard, Operator được assignment hiện tại; 403/404/422. D3/D4 mới thay đổi chính sách/kết quả. |

**Limits:** xác định file class từ purpose + MIME, kiểm tra cả admission và actual bytes; không chỉ tin filename. Survey video áp 8 GiB; telemetry SRT áp 10 MiB; photo purpose áp 20 MiB. Không áp 10 MiB cho mọi text/document hay tự cho DOCUMENT 8 GiB. Giữ giới hạn policy hiện có cho DOCUMENT/ROUTE_SOURCE; hồ sơ demo D5 dùng file nhỏ phù hợp, nêu rõ giới hạn của chúng chưa được PR-38 định nghĩa. DTO phải chứa được 8 GiB, validation semantic dùng giới hạn từng loại; body invalid có thể 400, valid JSON vượt limit →422 với `upload_validation_failed`, detail nêu loại và maxBytes; thêm extension `maxBytes`/`actualBytes` theo D1, không tự đổi code thành 413.

File bytes thật vượt declared size/limit hoặc sai checksum/MIME → FAILED với reason bền vững; không được reference vào dataset hay download. MIME detection hiện tại cần fixture cho loại được hỗ trợ; thêm loại mới, đặc biệt SRT/GPX, phải ghi allowlist trong D2/D4. Không dùng octet-stream để vượt loại file.

### 3.3 SQL, transaction, retry và recovery

- Widen `StoredFile.SizeBytes`, constructor, result/read models và toàn bộ callers bị ảnh hưởng sang `long`; `Files.SizeBytes` sang SQL `bigint`. `UploadSession.ExpectedSizeBytes`/metadata DTO đã long thì giữ. Tìm mọi `int` cast, sums, `StoredContent`, local store/result DTO trước khi kết luận end-to-end. `PartSizeBytes`, part number/count vẫn int nếu đã chứng minh upper bound; dùng integer ceiling an toàn, không overflow.
- `POST uploads`: một transaction chứa Files + FileScopes + UploadSessions + audit + idempotency receipt; chưa gọi external storage. Same key/same semantic request/cùng actor+project+operation trả đúng IDs; changed payload conflict, không tạo thêm rows.
- `part-urls`: serialization/claim theo uploadId cho initialize. Không giữ SQL transaction chứa toàn bộ network upload. Chỉ một `StorageUploadId` được nhận làm active; xử lý concurrency không trả 500. Nếu crash sau external initiate trước save, chưa thể biết orphan ID thì phải ghi limitation, không tự tuyên bố phục hồi exactly-once hoặc tự xây retention cleanup. Resume cho session có active ID không tạo multipart khác.
- Receipt URL issuance lưu part numbers và expiry, không log signed URL. Replay khi receipt còn hạn cho cùng part set; receipt đã hết hạn không đổi expiry dưới cùng key: trả conflict theo contract và client lấy URL bằng **key mới** trên cùng session. Session đã hết hạn không extend ngầm. Lifetime hiện tại 24h/session, 15 phút URL, part size 8 MiB là **CURRENT source config**, không nâng thành quyết định nghiệp vụ mới.
- Resume không được khẳng định dựa riêng vào GET session vì DTO hiện không chứa danh sách part đã lưu ở storage. Phần I có thể resume bằng session/part ETags client đã giữ hoặc re-upload deterministic part numbers. Theo D1, thêm GET `/api/v1/uploads/{id}/parts` → `{uploadId,status,parts:[{partNumber,sizeBytes:long|null,eTag}],source:"STORAGE|COMPLETION_RECEIPT",version}`: auth như session, UPLOADING lấy danh sách thực từ storage, VERIFYING/VERIFIED dùng receipt complete và ghi rõ source, FAILED không cấp URL. 200/403/404/503, read-only SQL; part URL đã cấp không chứng minh part đã upload. Test restart mất client part list phải dùng route này hoặc re-upload, không bịa server resume manifest.
- `complete`: so fingerprint/receipt trước business transition; gồm expected version và ordered part ETags trong fingerprint hiện hành. Auth lại trước replay; receipt không bypass quyền. Business mutation + parts completion + audit + receipt commit cùng transaction. Nếu mất response sau commit, replay không bị stale version chặn.
- Worker: VERIFYING → VERIFIED hoặc FAILED; transient storage outage giữ VERIFYING để retry. Restart sau complete object nhưng trước SQL commit phải đọc lại object, hash stream và xác nhận cùng size/MIME/hash; không tạo file/dataset khác. Concurrency worker: conditional state/rowversion claim để chỉ một durable terminal transition; loser reload/return, không overwrite kết quả terminal. Nếu cần lease columns thì đưa migration proposed dưới D1; không mở framework job chung.
- Chỉ gọi VERIFIED sau kiểm tra bytes thật; không coi client ETag là SHA-256. Signed PUT URL không là bằng chứng verified. Hash dùng bounded buffer; không giữ 8 GiB trong RAM.
- Dataset: trong transaction kiểm tra task/assignment/state/version, scope subset, **tất cả file IDs unique qua cả hai mảng**, VERIFIED, cùng project+target task, purpose đúng; sum `Files.SizeBytes` bằng long/checked <=32 GiB. Không cộng file lịch sử của task/segment ngoài snapshot đang submit. Đúng 32 GiB cho phép, +1 byte từ chối, không ghi Survey/DataVersion/SurveyFile/audit/receipt success.
- Dataset success: một Survey (nếu chưa có), một SurveyDataVersion ServerConfirmed/PASSED (integrity, không phải coverage), SurveyFiles/source manifest, task Submitted, audit + receipt trong một transaction. Không tạo ProcessingJob, baseline, label hay notification consumer ở ANH-01. Existing outbox intent nếu command đã có phải preserve; event mới chỉ khi schema/consumer đã thống nhất §8.
- Trường hợp hai key khác nhau, cùng task version submit đồng thời: tối đa một success; loser 412 hoặc existing documented 409 tùy race, không có orphan business rows. Thêm unique `(SurveyId,VersionNo)` nếu chưa có, chỉ sau audit migration. Không dùng `MAX+1` không guard làm cơ chế concurrency duy nhất.
- Manifest/dataset source immutable; correction/supplement tạo version mới. GET không đánh giá coverage hay cập nhật task. Failed command không phát outbox/audit success; receipt lỗi nếu shared engine hiện lưu thì giữ semantics, không sửa engine toàn cục chỉ để hợp một endpoint.

### 3.4 Migration/recovery đề xuất — không áp DB chung

Migration M1 `Anh01FileSize64`: widen Files.SizeBytes int→bigint, mapping/model snapshot đồng bộ, giữ immutable-file trigger/checks/FKs/indexes. Không sửa migration đã áp dụng. Không backfill size bằng giá trị giả: widening giữ nguyên số hiện có. Audit row count/min/max/null, negative/zero và size lệch UploadSession; zero đang SQL allow nhưng constructor không allow: ghi anomaly, không sửa/xóa dữ liệu tự động.

Reader cũ map int không đọc được >2 GiB: rollout schema trước, deploy toàn bộ affected readers/writers sau, rồi mới bật large upload. Trong giai đoạn reader cũ còn chạy, không nhận số lớn chỉ vì schema đã bigint. Isolated upgrade test dùng migration baseline thực, rows cũ và trigger; insert/read 2^31, 8 GiB; old rows còn nguyên. Down chỉ cho phép nếu mọi value <=int.MaxValue; nếu có large rows thì dừng downgrade, ưu tiên forward fix. Không truncate/delete để rollback. Large-byte MinIO test nếu không chạy phải báo NOT RUN, không lấy fake metadata làm chứng minh upload 8 GiB thật.

## 4. Project/road/warranty: giữ đường chạy hiện hữu

Các API hiện hữu sau dùng `/api/v1/projects`; không đổi sang route từ draft chỉ vì tên V2. Phần I bảo toàn behavior và chạy focused regression khi bị tác động; không tự thêm CRUD ngoài thiết kế D.

| Method/path tương đối | DTO / success | Quyền và lỗi hiện hữu cần giữ |
|---|---|---|
| POST `/` | `CreateProjectRequestDto` → `CreateProjectResponseDto`, 201 + Location | Supervisor. 400 validation; 404 primary PM/handover file; 409 code conflict/duplicate_request. |
| PUT `/{projectId}` | `{name,description?,engineeringUtmSrid?,startDate?,endDate?,expectedRowVersion,operationId}` → `UpdateProjectResponseDto`, 200 | Supervisor; 403,404 project_not_found,409 concurrency_conflict/duplicate_request,400. |
| PUT `/{projectId}/primary-project-manager` | `{primaryProjectManagerUserId,effectiveFrom,reason,expectedCurrentMembershipRowVersion,operationId}` → `{previousMembershipId,currentMembershipId,currentMembershipRowVersion}`, 200 | Supervisor; đúng một primary PM hiệu lực; 403,404 project_primary_pm_not_found,409 project_closed/concurrency_conflict/duplicate_request,400. |
| POST `/{projectId}/road-sections` | `{code,name?,srid,coordinates:[{x,y}],effectiveFrom,changeReason,operationId}` → `RoadSectionVersionResponseDto`, 201 | **Hiện Supervisor**, không phải PM. 403,404 project_not_found,409 project_closed/road_section_code_conflict/duplicate_request,400. |
| POST `/{projectId}/road-sections/{roadSectionId}/versions` | `{srid,coordinates,effectiveFrom,changeReason,expectedCurrentVersionId,operationId}` → cùng response, 201 | Supervisor; 404 road_section_not_found; 409 road_section_concurrency_conflict/project_closed/duplicate_request; 400/403. |
| POST `/{projectId}/warranties` | `CreateWarrantyRequestDto` → `CreateWarrantyResponseDto`, 201 | Supervisor; fields roadSectionId?,handoverDocumentId?,handoverDate,warrantyStartDate,warrantyEndDate,retainedValue?,scope,terms?,sourceDocumentId?,status,operationId. 404 `project_not_found\|warranty_road_section_not_found\|warranty_handover_document_not_found\|warranty_source_document_not_found`; 409 project_closed/duplicate_request;400/403. |
| GET `/{projectId}/work-package` | `ProjectWorkPackageResponseDto`, 200 | Theo project authorization hiện hành; lỗi guard có `project_access_forbidden` (khác `access_forbidden`); kiểm tra `accessRole` và nullable wire bằng HTTP. Không coi doc source required là HTTP proof. |

Project create hiện có hai shape tương thích trong một DTO: `code/name/primaryPmId/handoverDate/warrantyEndDate/handoverFileIds` và legacy `projectCode/.../primaryProjectManagerUserId/handover`. Controller ưu tiên shape có `code`, tối đa một handover file; `operationId` hoặc key header được derive. **Không xóa alias hay tự biến DateOnly không biết thành ngày hôm nay.** Canonical mới là D1/D5. Project creation và PM reassignment phải giữ transaction/history/receipt hiện có; road version cũ không được sửa geometry khi tạo version mới. Chuyển PM không tự rewrite actor của task/dataset cũ.

Warranty nhiều nghĩa vụ phải giữ từng obligation/source/scope; end date trên project không phải quyết định xóa. Phần ANH-01 cung cấp facts cho ANH-02 sau này, không viết retention engine hoặc đánh dấu hết hold.

## 5. Geometry/version/segment — giao triển khai theo D1+D2

**ASSIGNED theo D1/D2.** Dùng road section làm đơn vị tuyến/nhánh đang có, bổ sung draft + immutable geometry metadata và segment geometry; không tạo lại project hierarchy. Giữ API metric cũ như compatibility path. Nghiệp vụ đã chốt: PM nhập/chỉnh, Supervisor xác nhận, PM publish segmentation. Giữ API Supervisor cũ; PM dùng draft API riêng, không mở quyền PM lên API confirm cũ. Không xây graph mạng nhánh hoặc tài sản tấm trong đợt này theo D2.

### 5.1 Wire giao triển khai

Prefix `/api/v1/projects/{projectId}`. Command tạo cần `Idempotency-Key`; command sửa/xác nhận/publish còn cần `If-Match`. ETag là opaque base64 rowversion có quote. JSON dùng GUID, finite numbers, ISO dates; không trả EF entity. Mọi successful create và confirm sinh version trả 201+Location+ETag, edit/publish trả 200+ETag, GET 200+ETag; preview 200 không ghi SQL và không yêu cầu Idempotency-Key. Geometry-package ETag phải phản ánh các immutable refs/hash trả về, không dùng riêng Project.RowVersion vì publish segment có thể không đổi project row.

| Endpoint giao triển khai | Input chính xác | Output |
|---|---|---|
| POST `/road-geometry-drafts` | `GeometryDraftInput` + `roadCode,roadName?` cho tuyến mới chưa có roadSectionId | `GeometryDraftView`, roadSectionId nullable tới confirm |
| GET `/road-geometry-drafts/{draftId}` | — | Draft tuyến mới, scoped theo project |
| PUT `/road-geometry-drafts/{draftId}` | `GeometryDraftInput` + roadCode,roadName? | Draft tuyến mới; key+If-Match |
| POST `/road-geometry-drafts/{draftId}/preview` | — | `GeometryPreview`, read-only SQL |
| POST `/road-geometry-drafts/{draftId}/confirm` | `{expectedCurrentVersionId:null,effectiveFrom,reason}` | Supervisor; tạo RoadSection và version1 atomic,201+Location+ETag |
| POST `/road-sections/{roadSectionId}/geometry-drafts` | `GeometryDraftInput` bên dưới | `GeometryDraftView` |
| PUT `/road-sections/{roadSectionId}/geometry-drafts/{draftId}` | `GeometryDraftInput` | `GeometryDraftView` revision mới; chưa confirm |
| GET `/road-sections/{roadSectionId}/geometry-drafts/{draftId}` | — | `GeometryDraftView` |
| POST `/road-sections/{roadSectionId}/geometry-drafts/{draftId}/preview` | —; đọc đúng ETag nếu client gửi | `GeometryPreview` metric + WGS84; không tạo version |
| POST `/road-sections/{roadSectionId}/geometry-drafts/{draftId}/confirm` | `{expectedCurrentVersionId:guid, effectiveFrom:datetime, reason:string}` | `RoadGeometryVersionView` |
| GET `/road-sections/{roadSectionId}/versions/{versionId}/geometry` | — | `RoadGeometryVersionView`, kể cả bản lịch sử |
| POST `/road-sections/{roadSectionId}/versions/{versionId}/segment-sets/preview` | `SegmentDefinition` | `{routeVersionId,totalLengthMeters,boundariesMeters,segments:[SegmentGeometry],definitionHash}` |
| POST `/road-sections/{roadSectionId}/versions/{versionId}/segment-sets` | `SegmentDefinition` | `SegmentSetView` DRAFT, IDs bền vững |
| PUT `/road-sections/{roadSectionId}/versions/{versionId}/segment-sets/{setId}` | `SegmentDefinition` | `SegmentSetView` DRAFT mới version; không sửa PUBLISHED |
| POST `/road-sections/{roadSectionId}/versions/{versionId}/segment-sets/{setId}/publish` | `{expectedPublishedSetId:guid\|null,reason:string}` | `SegmentSetView` PUBLISHED |
| GET `/road-sections/{roadSectionId}/versions/{versionId}/segment-sets/{setId}` | — | `SegmentSetView`, kể cả SUPERSEDED |
| GET `/geometry-package` | query `routeVersionId`, `segmentSetId` bắt buộc theo cặp | `{schemaVersion:"anh01.geometry.v1",projectId,route:RoadGeometryVersionView,segmentSet:SegmentSetView}`; source cho Huy offline |

`GeometryDraftInput`:

- `sourceKind`: `COORDINATES` hoặc `GPX`; `sourceCrs`: EPSG int; `stationOriginMeters`: finite double bắt buộc; `changeReason`: nonblank.
- COORDINATES: `coordinates:[{x,y}]` >=2, `sourceFileId:null`, `trackIndex:null`, `trackSegmentIndex:null`.
- GPX: `sourceFileId` VERIFIED ROUTE_SOURCE đúng project; `trackIndex` và `trackSegmentIndex` bắt buộc zero-based; không tự chọn track đầu; `coordinates` là bản PM chỉnh có thể null nếu dùng đúng selected track. Lưu raw file hash và selected track, bản nhập và bản chỉnh riêng. GPX WGS84; route-point/waypoint-only không tự coi là track tim đường. Không fetch URL từ GPX, disable DTD/external entity.
- `widthProfile:[{fromOffsetMeters,toOffsetMeters,widthMeters}]`: chia phủ [0,L], không hở/chồng, mỗi width>0; offset đo từ đầu polyline, không phải station tuyệt đối. `surveyWidthMeters` >0 và >=width lớn nhất; đây là total width, không phải buffer mỗi bên.
- `branchCode`: dùng code road section hiện có; không tự suy graph node từ curve vertex. Chưa thiết kế node graph/slab inventory thì giữ D2 mở, không gọi các module đó Done.

`GeometryDraftView`: id,projectId,roadSectionId,status=`DRAFT|CONFIRMED`, sourceKind/sourceCrs/sourceFileId?/sourceChecksum?/trackIndex?/trackSegmentIndex?, originalCoordinates, editedCoordinates, stationOriginMeters,widthProfile,surveyWidthMeters,createdBy,updatedBy,version. Sau CONFIRMED không edit; sửa bằng draft mới.

`GeometryPreview`: sourceCrs,engineeringSrid,lengthMeters,metricCenterline (GeoJSON LineString có SRID field bên ngoài),wgs84Centerline (GeoJSON longitude,latitude),roadSurface (metric polygon/multipolygon),surveyArea (metric polygon/multipolygon),warnings[]. Width buffer theo profile, survey buffer =total/2. CRS thiếu/ngoài allowlist không trả giả geometry. D2 chọn nguồn CRS thật và chính sách transform; không chỉ sửa SRID.

`RoadGeometryVersionView`: projectId,roadSectionId,routeVersionId,versionNo,isCurrent,effectiveFrom,sourceDraftId?,sourceCrs?,engineeringSrid,stationOriginMeters?,widthProfile?,surveyWidthMeters?,metricCenterline,wgs84Centerline?,lengthMeters,metadataStatus=`COMPLETE|LEGACY_INCOMPLETE`,approvedBy?,approvedAt?,geometryHash. Legacy rows không tự điền 0/width/phê duyệt; geometry-package nêu incomplete, endpoint cần width thì 422.

`SegmentDefinition`: `targetLengthMeters` >0, default 100 nếu bỏ qua; `remainderMode:KEEP|MERGE_PREVIOUS`; `boundariesMeters:double[]|null`. Nếu explicit boundaries có giá trị, chúng là authority và phải bắt đầu 0/kết thúc L, tăng nghiêm ngặt; target length là ý định không ép mỗi đoạn bằng nhau. Nếu dùng target, chia dọc polyline, KEEP giữ dư, MERGE_PREVIOUS nhập phần dư cuối vào đoạn trước; L<target tạo một đoạn. Không tự dùng ngưỡng “dư quá nhỏ”. PM nhìn preview và chọn. Không mix station absolute vào offsets.

`SegmentGeometry`: id (null trong preview),sequence,fromOffsetMeters,toOffsetMeters,startStationMeters,endStationMeters,lengthMeters,metricGeometry,wgs84Geometry. `SegmentSetView`: id,routeVersionId,status,definition,geometryHash,segments[],publishedBy?,publishedAt?,version. Preview deterministic, IDs khi persistence mới là identity; không reuse IDs của bộ cũ theo sequence.

Errors mới giao triển khai theo D1: 400 `validation_error` cho malformed; 401 `unauthorized`; 403 `access_forbidden`; 404 `road_section_not_found|road_geometry_draft_not_found|road_section_version_not_found|segment_set_not_found`; 409 `duplicate_request|road_geometry_invalid_state|road_section_concurrency_conflict|segment_publication_conflict`; 412 `concurrency_conflict` cho stale ETag; 428 `validation_error` thiếu header; 422 `geometry_validation_failed|geometry_metadata_incomplete|segment_validation_failed|unsupported_crs`. Resource của project khác không được gắn vào request project; trả 404 scoped lookup, không lộ raw geometry.

### 5.2 Quyền, transitions và persistence

PM membership hiện lực đúng project tạo/edit/preview draft và segment; Supervisor confirm geometry; PM publish. Supervisor đọc mọi project theo guard hiện có, không mặc định được sửa mọi command PM. Operator/Crew chỉ đọc package đúng project/task theo D3/interface Huy; Reporter không đọc geometry nội bộ từ đây.

Confirm tuyến mới: reserve unique project+roadCode trong transaction, tạo RoadSection+version1 cùng immutable metadata/audit/receipt; không cần Supervisor tạo tuyến giả trước khi PM nhập. Hai draft cùng code cạnh tranh chỉ một được tạo, loser409 road_section_code_conflict. Draft view có roadCode/roadName và roadSectionId null trước confirm; RoadGeometryDraft FK roadSectionId nullable cho draft tuyến mới, projectId bắt buộc. Confirm tuyến đã có: DRAFT + valid input + current-version guard → tạo RoadSectionVersion mới, clear IsCurrent cũ, attach immutable metadata, mark draft CONFIRMED + approvedBy/time, audit+receipt trong một transaction. Hai confirm cạnh tranh cùng current version chỉ một thắng; receipt replay trước stale guard. Không copy survey/defect/job sang version mới.

Publish: DRAFT + complete geometry + route version đúng → previous current set SUPERSEDED và new PUBLISHED cùng transaction; rowversion compare trên parent/current pointer để hai publisher không cùng thắng. Nếu code route version đã đổi so expected thì 409; không publish nhầm bản. Một current set trên mỗi route version; old set vẫn đọc được, reference cũ vẫn hợp lệ cho task đang tồn tại. New task chỉ chọn current published set; việc tiếp tục task cũ dùng superseded set được giữ, không bị ResolveScopeAsync mới chặn hồi tố.

M2 migration candidate giao generate/test isolated: thêm `RoadGeometryDraft` (source/edited snapshots, actor/time/status,rowversion); immutable `RoadGeometryMetadata` keyed FK version; extend `RoadSegmentSet` definition/published metadata/rowversion; extend `RoadSegment` offsets/stations/geometry nullable cho legacy. Unique set+sequence và composite relationship set/version; publication pointer/unique guard. Legacy segment chỉ ID/sequence không đủ dựng ranh → giữ nullable/incomplete, **không tự chia đều**. Snapshot FK/id cũ giữ nguyên. Audit riêng các set PUBLISHED nhiều bản, segment thiếu geometry trước khi đặt constraint. M2 tách biệt M1, không chỉnh applied migration. Deployed application/backfill chờ rollout authorization riêng và dữ liệu thật; không hỏi lại D1/D2.

Không có thư viện CRS transform được chứng minh từ các csproj đã đọc; NetTopologySuite không tự đổi CRS chỉ nhờ gán SRID. Codex phải kiểm tra toàn solution trước lựa chọn adapter. Nếu cần dependency mới, đưa đúng package/version/lý do để Anh duyệt; không upgrade framework hay tự viết công thức projection chưa được kiểm chứng. Isolated transform fixtures phải có expected coordinates nguồn đáng tin, không dùng inverse của cùng hàm làm chứng cứ duy nhất.

## 6. Survey/task/dataset đầy đủ (D1+D3; assessment thêm D4)

### 6.1 API và state hiện có

Prefix `/api/v1`. `BandScopeDto={routeVersionId,segmentSetId,segmentIds:guid[],targetBand}`; targetBand `SURFACE|LEFT_EDGE|RIGHT_EDGE`. Survey type wire `BASELINE|PERIODIC|AD_HOC`, ánh xạ enum hiện hành; không dùng tên C# V2 để suy URL `/v2`.

| Endpoint | Request → success | Authority hiện hữu |
|---|---|---|
| POST `/projects/{projectId}/survey-plans` | `{scope[],plannedAt,surveyType}` → plan{id,projectId,scope,plannedAt,status,version}; 201+Location+ETag | PM in project, Idempotency-Key |
| POST `/survey-plans/{planId}/postpone` | `{reason}` → plan DTO; 200+ETag | PM project; key + If-Match |
| POST `/projects/{projectId}/survey-tasks` | `{scope[],surveyType,operatorId,dueAt?,accessPoint?}` → task{id,projectId,scope,operatorId,status,version};201+Location+ETag | PM project; key; operator active role DRONE_OPERATOR, hiện chưa kiểm project membership |
| GET `/survey-tasks/{taskId}` | task DTO + ETag | Supervisor/PM scoped; Operator assignment |
| GET `/me/survey-tasks?cursor=&limit=` | `{items[],nextCursor?,asOf}`;200 | Chỉ DroneOperator, limit 1..100 (default50), invalid cursor/limit400 |
| POST `/survey-tasks/{taskId}/accept` | không body → task,200 | Assigned Operator; key+If-Match |
| POST `/survey-tasks/{taskId}/decline` | `{reason}` → task,200 | Assigned Operator; key+If-Match |
| POST `/survey-tasks/{taskId}/cancel` | `{reason}` → task,200 | PM project; key+If-Match |
| POST `/survey-tasks/{taskId}/reassign` | `{operatorId,reason,dueAt?}` → task,200 | PM project; key+If-Match |
| POST `/survey-tasks/{taskId}/supplements` | `{scope[],reason,operatorId}` → task,201 | PM project; key+If-Match; current semantics chưa đủ cho operator khác |

Lưu ý source hiện SubmitDataset sai role có thể rơi về422 InvalidInput; không mô tả đó là policy403 đã có. D3 chọn403 cho actor không được phép, public delta theo D1. Lỗi survey giữ như source:401/403;404 project/task/plan/operator tương ứng;428 thiếu header;412 stale version;409 `duplicate_request` khi key khác payload và `survey_invalid_state_transition` khi sai state;422 `survey_validation_failed`;400 malformed input. Plan create hiện map conflict→duplicate_request, postpone có khác biệt; chỉ đổi các public codes được chỉ rõ trong spec, không normalize mọi route ngoài phạm vi.

Transitions hiện tại cần bảo toàn ở phần I: NEW_ASSIGNED/REASSIGNED→ACCEPTED hoặc REJECTED; reassign không được từ SUBMITTED/COMPLETED/CANCELLED; cancel không được từ SUBMITTED/COMPLETED/CANCELLED và không khi có server-confirmed dataset; submit từ ACCEPTED/IN_PROGRESS/SUPPLEMENT_REQUIRED→SUBMITTED. Task mutation, assignment history, audit và receipt atomic. Test rollback sau domain guard fail vì code có thể stage assignment trước khi task method throw.

Old routes vẫn giữ dưới `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans`, `/survey-requests` và project-scoped `/survey-plans/{surveyPlanId}/postpone`, theo `SurveyPlanningController`. Chúng dùng body operationId, DTO enum/time window khác. Không rename, convert JSON hàng loạt hay redirect sang V2 tự động.

### 6.2 Delta giao triển khai theo D1/D3 đã chốt

- New writes dùng band-scoped flow; old routes còn hoạt động cho đúng legacy shape. Thêm discriminator `ScopeFormatVersion` nullable/additive cho plan/request; rows có normalized scope relational là BAND_V1, legacy chỉ đánh dấu LEGACY khi nhận diện được, ambiguous giữ UNKNOWN; không suy từ ID hay tên route. V2 mutation trên LEGACY không thể project trả 409 `survey_scope_incompatible` trước write (đây là public delta, D1); old-compatible action không rewrite normalized scopes. Existing old/V2 tests phải cập nhật theo decision, không sửa test để che cross-write.
- Thêm optional `planId` vào new-task DTO; nếu gửi, plan phải cùng project/type/scope chứa task scope và state cho phép, snapshot riêng scope task, transition plan/task atomic. Không tự tạo task từ plannedAt, postpone hoặc reminder.
- Thêm GET `/survey-plans/{id}` → plan DTO+ETag; GET `/datasets/{id}` → `DatasetDetailView`+ETag; GET `/survey-tasks/{id}/work-package` → `SurveyTaskWorkPackage`. These reads close Location/consumer gaps; scope như task/dataset, no writes.
- `SurveyTaskWorkPackage={schemaVersion:"anh01.survey-work.v1",task:SurveyTaskV2ResponseDto,dueAt,accessPoint:PositionDto|null,geometryRefs:[{routeVersionId,segmentSetId,segmentIds}],scopeVersion}`. Position point WGS84, source/accuracy/capturedAt giữ như DTO; không tự lấy midpoint segment/GPS drone làm destination. geometry package resolve đúng version đã giao, không current mới nhất. Huy tổ chức offline cache/sync receipt, Anh cung cấp BE read.
- D3 chốt access cần current active project membership **và** assignment cho Operator; PM/Supervisor như hiện hữu; revoked/expired membership mất quyền đọc/mutate/replay ngay request sau. File owner không bypass membership cho private project assets. `TargetId` với SURVEY_VIDEO/TELEMETRY phải là task đang assigned; PM/Supervisor có read oversight, không được upload thay actor thành Operator. Replay sau reassignment/revocation phải auth lại trước receipt.
- D3 chốt `deviceId` là `DroneDevice.Id` trong registry đang có, active, không phải phone session/device string. `DroneDevice` hiện chỉ có SerialNo/Model/Status/ChecklistVersion, **không có owner/project FK**; pilot dùng registry chung, task assignment xác định Operator chịu trách nhiệm. Nếu Anh yêu cầu device thuộc owner/project thì cần thiết kế allocation riêng trước migration, không tự thêm ownership. API lựa chọn/provision device cần contract riêng nếu client chưa có nguồn registry; phần này không được gọi hoàn chỉnh chỉ nhờ test seed. Thiếu fixture/device thật là blocker dependent.
- Supplement tạo **child task** cùng project, `ParentTaskId` và `SupplementRequestId`, scope subset được PM chọn từ task/dataset gốc; assignment mới cho chosen Operator; parent và source dataset không rewrite. Giữ response Task DTO ở endpoint supplement hiện tại, additive field `supplementTaskId` và Location trỏ child; parent ID/status không giả thành child. Parent chuyển SUPPLEMENT_REQUIRED như compatibility hiện có; new upload/submit của round bổ sung chỉ trên child, không cho original Operator tiếp tục submit parent để bỏ qua child assignment. Trả child task qua GET của ID mới. API cũ không bị xóa. Một key chỉ tạo một child. PM có thể yêu cầu nhiều round hợp lệ; không đặt cap. New Operator accept child trước submit, file targetId là child ID. Historical supplement rows không tự backfill child/assignment. Child task đã chốt; không quay lại hỏi lựa chọn reuse parent.
- M3 additive: scope discriminator, task parent/plan link, supplement child link, dataset submitter snapshot và file-to-dataset/pair relation cần thiết; retain assignment history. Không thêm owner/project FK device; validate registry active đúng D3.

### 6.3 Dataset source reference và pairing (D4)

`DatasetDetailView={id,surveyTaskId,projectId,dataVersionId,submittedBy,submittedAt,recordedAt,deviceId,scope[],integrityStatus,telemetryStatus,sourceFiles:[{fileId,checksumSha256,sizeBytes,mediaType,purpose}],pairs:[{videoFileId,telemetryFileId|null,timeOffsetMilliseconds}],version}`. Không trả bucket URI hay secret. Existing SourceManifest JSON reader đang dùng fileId/hash phải giữ đọc được; enrich bằng additive property chỉ sau so caller, hoặc lưu normalized rows mới để tránh đổi parser ANH-02 ngoài phạm vi.

D4 triển khai submit bổ sung optional `pairs`; nếu gửi phải cover mỗi video đúng một lần, telemetry đúng file trong snapshot, không pair chéo dataset, nhiều video không suy pairing theo index/filename. Nếu bỏ pairs, không tự đoán; giữ nguồn và telemetry status MISSING/PRESENT theo meaning hiện tại, assessment báo pairing unavailable. Đây là dữ liệu provenance, không tự khẳng định telemetry hợp lệ hay PASS coverage.

## 7. Assessment/coverage/baseline — PM thủ công, D4 đã chốt

**TARGET_CONFIRMED, ASSIGNED:** Anh chọn PM đánh giá thủ công có bằng chứng cho đợt này sau phần giải thích ngày 2026-10-02. Triển khai đầy đủ assessment và baseline dưới đây; không để chúng BLOCKED vì thiếu thuật toán tự động. Method `pm-evidence-review.v1` là checklist người duyệt, không phải thuật toán đo độ phủ, tiêu chuẩn nghiệm thu công trình hay kết luận không có lỗi.

Checklist pilot được áp dụng cho từng tuple routeVersion/set/segment/band:

- **Position PASS:** PM xác định được bằng chứng thuộc đúng segment/band từ telemetry hoặc mốc/vị trí đối chiếu có ghi lý do; evidence trỏ vào file/đoạn clip cụ thể. Có bằng chứng sai vị trí → FAIL; chưa xác định được → UNKNOWN.
- **Quality PASS:** PM thấy hình ảnh đủ rõ/sáng/không bị che để thực hiện mục đích quan sát band; video nhòe/tối/che khuất làm không quan sát được → FAIL; chưa đủ căn cứ → UNKNOWN. Không dùng ngưỡng pixel, blur score hay phần trăm chưa được duyệt.
- **Coverage PASS:** PM đã kiểm tra phạm vi hình ảnh bao quát band của segment và ghi căn cứ; thấy thiếu phạm vi → FAIL; chưa đủ căn cứ → UNKNOWN. GPS nằm trong vùng không thay chứng cứ hình ảnh thấy đủ band.
- Mỗi item phải có reason nonblank; PASS phải có ít nhất một evidence thuộc dataset, video interval 0<=from<to (nếu khai báo), không vượt duration đã xác minh. Nếu BE chưa có duration đáng tin, chỉ validate interval hình thức và ghi giới hạn; không nhận định đã kiểm clip content tự động. UNKNOWN có thể không có evidence nhưng phải giải thích phần còn thiếu. Không cho PM bỏ trống chiều rồi mặc định PASS.
- Ba chiều PASS mới đủ chọn baseline. Giữ phạm vi đạt, giao bổ sung phạm vi còn thiếu. Baseline là dữ liệu tham chiếu khảo sát, không tự xác nhận NO_DEFECT, hết bảo hành hoặc nghiệm thu sửa chữa.

Các endpoint sau được giao triển khai theo D1+D4; không cần hỏi lại quyết định manual:

| Endpoint giao triển khai | Request | Result / guard |
|---|---|---|
| POST `/api/v1/datasets/{id}/assessments` | `{methodVersion,items:[{routeVersionId,segmentSetId,segmentId,targetBand,positionStatus,qualityStatus,coverageStatus,reason,evidence:[{fileId,fromMilliseconds?,toMilliseconds?}]}]}` | PM project; key + If-Match dataset;201 assessment view+Location+ETag. Status mỗi chiều `UNKNOWN\|PASS\|FAIL`; chỉ xét scope trong dataset, file evidence thuộc snapshot/verified. |
| GET `/api/v1/datasets/{id}/assessments/{assessmentId}` | — | 200 immutable assessment{id,datasetId,methodVersion,reviewedBy,reviewedAt,items[],version} |
| GET `/api/v1/datasets/{id}/coverage` | — | Giữ property hiện hữu, result map từ assessment mới nhất; thêm assessmentId/version qua D1. Khi chưa có assessment, giữ UNKNOWN. `positionCoverage` không đồng nghĩa optical coverage. |
| POST `/api/v1/projects/{projectId}/baseline-selections` | `{items:[{routeVersionId,segmentSetId,segmentId,targetBand,datasetId,assessmentId,expectedBaselineSelectionId:guid\|null}],reason}` | PM project; key; mỗi item all required dimensions PASS theo method đã duyệt;201 `{id,items:[{selectionId,...refs}],selectedBy,selectedAt,version}`. Compare expected selection từng tuple thay vì dùng ETag dataset làm concurrency baseline. |
| GET `/api/v1/projects/{projectId}/baseline-selections?segmentSetId=...` | — | 200 `{items:[current selection views]}`; scoped, no writes; historical selection còn tra theo ID qua GET `/baseline-selections/{id}` cùng project prefix. |

Method manual đã chọn `pm-evidence-review.v1`; không gọi đó là thuật toán tự đo coverage. PM phải xem bằng chứng gắn đúng clip/time interval/scope rồi đánh dấu độc lập: vị trí có căn cứ định vị; chất lượng đủ quan sát; hình ảnh nhìn đủ band cần xác nhận. Không có căn cứ cho chiều nào → UNKNOWN, không có override ngầm. Thiếu telemetry không đương nhiên FAIL và cũng không PASS; cần evidence thay thế được policy duyệt. Checklist ở đầu §7 là phương pháp pilot được cụ thể hóa từ phương án manual Anh đã chọn; không chờ thêm calibration tự động để triển khai. Fixture demo phải thể hiện đủ PASS/FAIL/UNKNOWN, không seed tất cả PASS.

Aggregate overall cho response: bất kỳ FAIL→FAIL; không FAIL nhưng có UNKNOWN→UNKNOWN; mọi chiều PASS→PASS. Không cộng percent giả. Baseline nhận subset các band đủ điều kiện, không bắt cả Survey đạt; toàn bộ command baseline atomic, item không hợp lệ trả422 `baseline_not_eligible`, không ghi phần còn lại. 409 `baseline_selection_conflict` nếu expected selection đã đổi; key replay giữ IDs. Assessment mới không tự thay baseline đã chốt; PM phải chọn lại, lịch sử cũ giữ nguyên. 404 scoped refs,403 role,428 missing required headers,412 stale dataset,409 duplicate key,422 `assessment_validation_failed|assessment_method_not_approved`.

M4 migration candidate giao generate/test isolated: immutable `DatasetAssessment`, `DatasetAssessmentItem` unique assessment+version/set/segment/band; append-only `BaselineSelection` và current pointer unique project+routeVersion+set+segment+band. Audit/receipt cùng transaction; select khóa/compare pointer để cùng tuple chỉ một current. Không update source dataset, không dùng `Survey.IsBaseline` thay chi tiết. Không backfill UNKNOWN thành PASS; legacy dataset không có assessment giữ UNKNOWN. Tự động phân tích SRT/chấm vị trí/chất lượng/độ phủ không thuộc triển khai D4 đợt này theo lựa chọn manual; vẫn lưu SRT và pairing làm bằng chứng. Không ghi manual review là đã hoàn thành thuật toán tự động.

## 8. Ownership, interface với Huy và thứ tự tích hợp

**Anh là writer ANH-01.** Reservation dưới đây là kế hoạch cho phiên triển khai; Codex phải kiểm tra PR/thông tin Anh cung cấp để không chiếm file đang có writer khác. Nếu đã có reservation Huy, dừng đúng file đó, tiếp tục phần khác và ghi một yêu cầu tích hợp trong spec/PR; không tự sửa nhánh Huy.

| Allowlist | Writer / giới hạn |
|---|---|
| `RoadGuardSystem.API/Controllers/{Projects,ProjectRoadSections,ProjectWarranties,ProjectWorkPackages,SurveyPlanning,SurveyV2,Uploads}Controller.cs`; các controller geometry/segment/assessment mới mô tả trong spec | Anh; API mới theo các quyết định đã ghi tại §10. |
| `RoadGuardSystem.DTOs/{Projects,Warranties,Surveys,Files}/`; `RoadGuardSystem.Services/{Implementations,Interfaces}/{Projects,Warranties,Surveys,Files}/` | Anh cho flow trong spec; không sửa DTO identity/defect/inspection/processing. |
| `RoadGuardSystem.BusinessObjects/{Projects,Warranties,Surveys,Files}/`; `RoadGuardSystem.Repositories/{Implementations,Interfaces}/{Projects,Warranties,Surveys,Files}/` và configurations đúng entity | Anh; thay đổi type có caller ngoài subtree phải ghi rõ compile dependency, không đổi semantics ngoài gói. |
| `Repositories/Implementations/Storage/{MinioUploadObjectStorage,LocalFileContentStore,StoredContent}.cs`, `Repositories/Interfaces/Storage/`, `Repositories/Spatial/`, `Repositories/Options/UploadSessionOptions.cs`, `API/Workers/UploadVerificationWorker.cs` | Anh chỉ size/stream/recovery/geometry cần thiết; không thay storage provider hoặc tạo cleanup retention. |
| `Repositories/RoadGuardDbContext.cs`, `Repositories/Migrations/*` mới, `RoadGuardDbContextModelSnapshot.cs`, shared configuration registration | **Anh tích hợp độc quyền**, migration M1→M2→M3→M4 nếu đã chốt. Huy gửi entity/config requirement và patch proposal; không cùng generate snapshot. Applied migrations immutable. |
| `API/Extensions/ServiceCollectionExtensions.cs`, `API/Program.cs`, shared DI và `API/Constants/ApiErrorCodes.cs` | Anh tích hợp sau khi nhận signatures của Huy; sửa tối thiểu phần binding/options/error cần thiết. Huy giữ auth implementation, không sửa trực tiếp trong reservation này. |
| `contracts/http/anh-01.proposed.yaml` (mới nếu cần máy đọc), canonical `contracts/http/openapi.yaml` nếu được D1 adopt; `docs/postman/`, `API/RoadGuardSystem.API.http` | Anh writer; chỉ update ANH-01 và integration đã nhận; không relock FE hoặc thay operationId/request ID cũ. Không cần thêm file proposed nếu spec + examples đủ. |
| `tests/RoadGuardSystem.{ApiTests,IntegrationTests,UnitTests}/{Projects,Warranties,Surveys,Files,Spatial}/`, tests mới `Anh01*` | Anh; reuse fixture. `tests/*/Infrastructure/`, architecture checks, shared idempotency/authorization tests là shared: Anh tích hợp có reservation, không viết lại fixture cả solution. |
| `planning/development/ANH-01.md`; PR summary | Anh/Codex lưu spec này và cập nhật decisions/evidence trong cùng file. Không tạo bộ RF report/ZIP. |

`IdempotencyOperationService`, `IProjectScopeGuard`/implementation, identity auth/session, notification outbox consumer là **shared contracts**: tiêu thụ semantics hiện hữu; chỉ đổi nếu có tái hiện in-scope và Anh xác nhận writer/order, không refactor chung. Nếu cần thêm helper policy riêng cho upload/survey thì ở Services module Anh, không fork identity role authority.

Interface **PROPOSED** cho handoff, không tự ghi là đã thỏa thuận với Huy:

| Interface/version | Anh cung cấp | Huy phải gửi trước tích hợp |
|---|---|---|
| `anh01.geometry.v1` | Immutable project/routeVersion/set/segment IDs, offsets, geometry metric+WGS84, CRS, incomplete marker. Existing refs vẫn đọc sau publish mới. | Case/inspection cần fields nào, ownership read rule cho Crew/Reporter, fixture wrong-project và historical-version; tuyệt đối không lưu chỉ segment sequence. |
| `anh01.verified-file.v1` (BE service view, không phải public link) | `{fileId,projectId,targetId,purpose,ownerUserId,checksumSha256,sizeBytes:int64,mediaType,status,version}`; resolve-and-authorize trước attach; content qua route protected | Enum target kind/purpose, ai tạo/đọc/attach, lifecycle entity target, receipt/replay policy của case/inspection và test IDOR. Reporter không có project staging: cần policy riêng, không bật Reporter vào upload project chỉ để pass integration. |
| `anh01.dataset.v1` | Dataset immutable source refs + geometry scope + submitter/device; integrity tách coverage/baseline | Huy chỉ cần facts nào, không tự gọi create processing; consumers phải giữ datasetId và routeVersionId. |
| `anh01.survey-work.v1` | Assigned task snapshot, dueAt/accessPoint có provenance, geometry refs/version; không tự đổi task state khi đọc | HUY-02 gửi sync command envelope, actor gốc, claim/session semantics, stale-version/conflict mapping, schema offline/handover. Anh không triển khai sync endpoint. |
| Auth principal existing/approved by HUY-01 | ANH-01 cần actor GUID + authoritative role và project guard; dùng cùng 401/403 strategy | Huy gửi claim mapping cho cookie/bearer, CSRF header cho web mutations, forced-password/session restriction, fixture revoked/session-expired. Anh không tạo login/cookie thứ hai. |
| Event integration (nếu cần notification) | Tên aggregate/id/version, actor/project/correlation và payload thay đổi ở Anh | Huy gửi event type/version, recipient policy và consumer fixture. Chưa agreed thì không tự publish event mới có consumer không biết. Existing audit/outbox hành vi giữ. |

Mọi request tích hợp Huy gửi gồm: base/head SHA; exact files/symbols; interface version + JSON success/error fixture; SQL/DI/migration yêu cầu; tests; thứ tự cần merge/cherry-pick **đề nghị**. Anh quyết định tích hợp vào anh-review; Codex không tự merge develop hay kéo cả huy-review. Một request trong PR/spec đủ, không cần coordination document riêng.

Thứ tự: (1) khóa shared types/role/file/geometry refs và owner; (2) Anh M1 + storage/dataset independent; (3) geometry/segment producer D2 và task consumer; (4) survey policy/supplement D3; (5) assessment/baseline D4; (6) nhận patch/shared registration từ Huy; (7) chạy real BE producer→consumer integration. Huy có thể làm identity/case/inspection chống versioned fixtures song song; mock fixture chỉ ghi mock verified tới khi chạy được producer thật.

## 9. Acceptance và verification theo rủi ro

Mỗi test phải quan sát HTTP status/body/headers và SQL effects khi áp dụng. Không yêu cầu viết test chỉ mirror implementation. Test names bên dưới là **kế hoạch**, không phải đã tồn tại/đã chạy. Existing suite names là nguồn reuse, kết quả cần chạy mới.

| ID | Case và expected result | Phần/gate |
|---|---|---|
| A01 | Supervisor tạo project/PM/road/warranty; replay giữ IDs/count; switch PM giữ history; PM project B/Reporter bị chặn; rollback không để project nửa chừng | I regression; existing `P120ProjectCreationTests`, `P120ProjectUpdateTests`, `P120WarrantyCreationTests`, `P121RoadSectionVersionTests`, `Rf1002ProjectGisCharacterizationTests` |
| A02 | Widen isolated migration từ baseline có data/trigger; giữ rows/hash/scope/receipt; read int.MaxValue và +1,8 GiB bằng long; down >int bị chặn | I prepare, D1 deployed gate; `FileSchemaContractTests` + new migration test |
| A03 | Mỗi loại file test limit−1,limit,limit+1;0/negative/overflow/malformed/MIME-purpose mismatch; invalid admission không ghi file/scope/session/audit success | I; `UploadApiTests`, new `Anh01UploadLimitTests` |
| A04 | Same create/part-url/complete key+payload replay đúng status/IDs; key khác payload409; same key khác operation độc lập theo namespace; cùng operation nhưng khác resource/payload phải409; mất response sau commit vẫn một outcome | I; `UploadApiTests`, `IdempotencyPerCommandCharacterizationTests` phần affected |
| A05 | Resume sau restart dùng session/active multipart cũ; PUT lại cùng part; fresh URL key sau URL expiry; session expiry không reset; hai initialize cạnh tranh không có hai active IDs/500 | I; SQL + fake adapter deterministic; MinIO riêng cho wire |
| A06 | Crash trước commit, sau object complete/trước terminal save, transient GetObject lỗi, hai verification worker; không duplicate durable success; checksum/size/MIME mismatch FAILED; pending/failed không download/attach | I; `UploadPersistenceSqlTests` + recovery tests |
| A07 | Streaming memory bounded; file >2 GiB và đúng8 GiB metadata thực không truncate; fake storage phân biệt với MinIO upload real | I/D1 activation; real8GiB nếu chưa chạy báo NOT RUN |
| A08 | Dataset sum32GiB cho phép,+1 từ chối; hai arrays trùng file; cross-project/target/purpose/unverified IDs; immutable manifest hash; không có rows/outbox success trên reject | I; API + SQL dataset tests |
| A09 | Hai submit keys cùng task ETag: tối đa một dataset version/task transition; failure injection rollback; replay sau success trả cùng dataset và ETag; GET coverage không đổi SQL | I; không sửa processing |
| A10 | Revoked membership/assignment đọc/mutate/replay bị chặn; file owner cũ không bypass; device unknown/inactive bị chặn theo D3; same-project wrong Operator bị chặn | D3; HTTP thật qua auth, không chỉ service fake |
| A11 | Geometry 30/40→50m và polyline cong; missing/mixed SRID reject; transform expected point fixture; WGS84 roundtrip trong tolerance ghi rõ; GPX nhiều track bắt chọn, waypoint-only reject | D2; unit+SQL spatial |
| A12 | Width8/10,total survey12: road widths khác, survey biên6m trên thẳng; gap/overlap/NaN/reversed offsets reject; legacy incomplete không tự fill | D2 |
| A13 | Draft edit PM, confirm Supervisor; stale ETag/currentVersion conflict; retry không tạo version; old survey/segment/job refs unchanged | D1+D2 |
| A14 | Length4500,target1000 KEEP→1000×4+500; MERGE→1000×3+1500; target default100; length<target; no gaps/overlaps; manual boundary valid/invalid | D2; geometry assertions không chỉ count |
| A15 | Hai publish cùng expected current set: một thắng; old refs đọc được; new task dùng current publish; task cũ/supplement hợp lệ giữ bản cũ; foreign project/segment reject | D2+D3 |
| A16 | Old/V2 same-row tests theo decision, discriminator ambiguous fail closed trước write; không route retirement/JSON lossy migration | D1; `Rf1003SameRowSurveyTests` |
| A17 | PM plan→task→Operator accept/decline/reassign; reasons required; state guards/replay/ETag; work-package giữ accessPoint và version; read không viết | I existing + D1/D3 delta |
| A18 | Supplement chosen Operator khác: child/assignment thực, accept→upload→submit được; old Operator không mutate child; parent/source/band đạt giữ nguyên; replay không tạo child2 | D3, không chỉ assert JSON operatorId |
| A19 | Pairing explicit one video→source SRT, duplicate/cross-dataset reject; missing telemetry giữ UNKNOWN; SRT chỉ trong vùng không tự PASS quality/coverage | D4 |
| A20 | PM assessment đúng project+method+scope/evidence; UNKNOWN/FAIL không baseline; surface đạt/right-edge thiếu chỉ baseline surface; repeated selection không duplicate; stale expected selection409 | D4; SQL concurrency |
| A21 | Baseline history vẫn resolve source sau assessment/route/model mới; audit actor thật; upload/dataset không tự tạo ProcessingJob/Defect/repair | D4 và I invariant |
| A22 | Demo dùng ngày đầy đủ, source docs scope đúng; không thêm unknown-date workflow; multiple obligations hiện có giữ riêng; không retentionUntil suy từ uploadedAt; không delete/hold mutation | D5: demo + existing warranty invariant; ANH-02 vẫn ngoài phạm vi |
| A23 | Huy real BE consumer nhận đúng geometry/file refs từ production producer; wrong-project refs denied và consumer SQL không ghi; cookie/bearer principal compatibility | Sau tích hợp; chưa có consumer thì PENDING, không fake PASS |

Các fixture hiện hữu đã đọc: `UploadApiTests` có fake storage và `MinioSmokeFact`; `P219DatasetContractTests` đang là entity assertion dù nằm IntegrationTests; không dùng nó một mình làm SQL proof. `P2SurveyV2ApiTests`, `Rf1003SameRowSurveyTests`, `Rf1002ProjectGisCharacterizationTests` là nguồn test flow và legacy behavior, không phải kết quả mới của phiên này.

Lệnh local gợi ý (điều chỉnh filter theo class thực, không đổi scope):

```bash
git status --short
git branch --show-current
git rev-parse HEAD
git log -1 --format=fuller
git diff --check
# Build đúng project entrypoint đã có, restore chỉ khi cần dependency cache:
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter 'FullyQualifiedName~Projects|FullyQualifiedName~Surveys|FullyQualifiedName~Files|FullyQualifiedName~Anh01'
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter 'FullyQualifiedName~Projects|FullyQualifiedName~Warranties|FullyQualifiedName~Surveys|FullyQualifiedName~Files|FullyQualifiedName~Anh01'
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter 'FullyQualifiedName~Files|FullyQualifiedName~Surveys|FullyQualifiedName~Spatial|FullyQualifiedName~Anh01'
```

Trước SQL commands đọc fixture/setup scripts và xác minh DB riêng, không shared database. Không copy secrets vào PR. Chỉ broad regression nếu shared type/schema/DI thay đổi tạo rủi ro cụ thể. Nếu csproj/filter ở HEAD mới khác, dùng path thật và ghi lại; zero selected tests không là PASS. Migration command dùng existing EF factory/options và isolated connection đã kiểm tra, không cung cấp lệnh `database update` mặc định có thể trúng shared DB.

PR summary duy nhất cần: problem/result; base/head + dirty paths preserved; files/scope; decision IDs active/blocked; self-review pass1(contract/auth/state/SQL/retry) và pass2(ownership/compatibility/consumer/rollback), findings đã fix; exact commands + passed/failed/skipped/not-run counts; **BE verified / mock verified / external-deployment not verified**; ChatGPT review PENDING. Không tạo seven-part RF report hoặc ZIP.

## 10. Quyết định đã chốt và yêu cầu demo

Nguồn: các trả lời của Anh trong cùng phiên ngày 2026-10-02; D4 được chốt bằng câu “Tôi PM thủ công trong đợt này”. Không hỏi lại D1–D5.

| ID | Quyết định / áp dụng |
|---|---|
| D1 — CONFIRMED | Giữ API cũ, thêm contract mới; band-scoped survey là new-write flow, không tự chuyển dữ liệu legacy. Các additive endpoints/deltas trong spec là phạm vi triển khai; migration generate/test isolated, không ghi DB chung/deployed. |
| D2 — CONFIRMED | PM nhập/chỉnh tuyến → Supervisor xác nhận → PM publish segment. Không thêm graph mạng nhánh hoặc quản lý tấm. Geometry vẫn phải khai báo CRS, tính mét đúng; không lấy câu “Không cần” làm quyền gán SRID giả. Bắt đầu bằng coordinate CRS đã hỗ trợ và fixture có provenance; GPX/CRS khác chỉ mở khi source/transform được kiểm chứng, không làm blocker của toàn gói. |
| D3 — CONFIRMED | Membership hiện lực + assignment cho Operator; file owner không bypass private-project access; device registry chung active theo model đã đọc; supplement tạo child task cho Operator được chọn, giữ parent/source/band đạt. Không tự thêm device owner/project FK. |
| D4 — CONFIRMED | PM review thủ công có bằng chứng, tách position/quality/coverage; PASS/FAIL/UNKNOWN; baseline chỉ phần đủ điều kiện. Method pilot `pm-evidence-review.v1`; không triển khai auto-assessment trong đợt này. |
| D5 — CONFIRMED DEMO SCOPE | Làm hồ sơ/tài liệu hư cấu để gắn vào demo, có nghiên cứu nhằm nội dung hợp lý và dữ liệu nhất quán. Không coi đây là phê duyệt mở rộng unknown-date/multi-document APIs. Demo dùng ngày bàn giao/bảo hành đầy đủ và đường attach hiện có. |

### 10.1 Demo được giao cùng ANH-01

Chuẩn bị một dự án đường demo có tên/mã rõ ràng, ngày bàn giao và kỳ bảo hành hợp lý, ít nhất hai segment để thể hiện baseline từng phần. Dùng bộ IDs/seed deterministic hiện hữu hoặc fixture riêng không trùng production. Geometry, chiều dài, width, segment, band, lịch khảo sát và hồ sơ phải cùng một tập facts; mọi dữ liệu đều hư cấu có nhãn DEMO.

Bộ tài liệu nên gói vào một PDF dossier để phù hợp current create tối đa một handover attachment: trang thông tin dự án; biên bản bàn giao mô phỏng; bảng nghĩa vụ bảo hành mô phỏng; sơ đồ/danh mục tuyến-segment và kế hoạch khảo sát. Nếu production upload cho DOCUMENT hiện chưa hỗ trợ MIME PDF, nêu đúng gap rồi tích hợp detector/allowlist cần thiết cho demo; không đổi hạn mức chung hoặc fake status VERIFIED. Tài liệu bổ sung đi qua target/project upload đúng scope, không mở quyền đọc công khai.

Codex nghiên cứu nguồn chính thức/primary về cấu trúc các loại tài liệu trước khi soạn, ghi URL + ngày truy cập + nội dung tham khảo trong phần provenance của dossier. Chỉ tham khảo cấu trúc/thuật ngữ; không tự khẳng định thời hạn bảo hành hoặc thông số hư cấu là quy định pháp luật/tiêu chuẩn bắt buộc. Nếu chưa truy cập được nguồn, ghi nghiên cứu NOT RUN và handoff đúng limitation; không gọi dữ liệu “đã nghiên cứu”. Không chép dấu/chữ ký thật; ghi “DỮ LIỆU DEMO — KHÔNG CÓ GIÁ TRỊ PHÁP LÝ”. Không tạo hồ sơ công trình/người thật.

Dataset demo phải có các trạng thái: một band PASS cả ba chiều; một band UNKNOWN do thiếu căn cứ; một band FAIL do bằng chứng không đủ chất lượng/phạm vi. Dùng media sample hợp lệ có quyền sử dụng hoặc fixture tạo được và ghi rõ synthetic; PDF dossier không thay bằng chứng video. PM tạo assessment và baseline qua API thật, supplement child cho phần thiếu; không seed baseline trực tiếp rồi tuyên bố producer đã verified. Không cần file video 8 GiB cho demo UX; large-byte acceptance là test riêng.

Chỉ thêm assets demo và setup fixture nằm trong phạm vi ANH-01; đọc script trước chạy, DB isolated/local. Không seed dữ liệu vào shared DB hoặc chạy script xóa/reset sẵn có. Một spec + PR summary vẫn đủ; dossier là dữ liệu demo phục vụ chức năng, không phải audit package.

### 10.2 Checkpoint còn lại — không hỏi lại quyết định nghiệp vụ

External AI/Android/client compatibility chưa verified; phối hợp Huy cần interface/fixture thật theo §8. Package/version transform mới cần đúng dependency decision nếu chưa có trong repo. DB chung/deployed vẫn chưa được phép migrate. Ngoài các checkpoint cụ thể này, Codex triển khai trọn gói đã chốt, không dừng ở phần I vì text lịch sử “D chưa chốt”.

## 11. Prompt thực thi cho Codex local

Copy nguyên khối này; đặt file spec tại `planning/development/ANH-01.md`. Không cần file prompt riêng.

```text
Bạn triển khai ANH-01 cho Anh trên RoadGuardSystem, nhánh anh-review.
Spec duy nhất: planning/development/ANH-01.md (revision2 hoặc revision mới hơn
Anh đã gửi). ChatGPT đã đọc GitHub HEAD 1ecae797caaed1ab912b02b2372a1940d1e05375;
đó là mốc khảo sát, không được reset nếu nhánh đã tiến thêm.

Vai trò: bạn triển khai toàn bộ luồng đã được giao, tự review/fix hai lượt,
chạy focused tests và commit/push anh-review. Không giao lại cho người dùng
các bước đã nằm trong spec. Không amend/force-push commit đang review,
không merge develop, thao tác main, sửa nhánh Huy hoặc áp DB chung.

1. Đọc AGENTS.md, .agents/manifest.json, rules/module map liên quan,
planning/development/README.md, spec-template.md và spec ANH-01.
Manifest skills=[]; không gọi RoadGuard skills retired; không mở RF audit.
Ghi branch, HEAD thực, dirty paths và base review trước thay đổi. Giữ dirty
không liên quan; dùng checkout/worktree riêng nếu thực sự đang cạnh tranh.
Nếu HEAD khác mốc spec, đọc delta có liên quan và cập nhật evidence;
không lặp khảo sát toàn repository hoặc reset về mốc cũ.

2. D1..D5 đã được chốt theo §10; D4 là PM thủ công. Triển khai toàn gói:
- reuse production project/road/warranty, giữ old API;
- PR-38 limits, long end-to-end, migration candidate isolated, replay/resume/recovery;
- PM geometry draft, Supervisor confirm, PM segment publish;
- band-scoped plan/task, current authorization, supplement child task;
- immutable dataset/pairing, PM manual assessment, partial baseline;
- hồ sơ demo có nghiên cứu và fixture/demo flow theo §10.1.
Làm I trước để mở các producer, rồi triển khai các slice đã chốt theo dependency.
Không dừng ở UNKNOWN coverage read hoặc chỉ entity test; phải có PM assessment
và baseline HTTP→SQL thật. Không xây automatic coverage/quality/SRT evaluation.
Không hỏi lại D1–D5; không tự thêm unknown-date/multi-document nghiệp vụ mới.

3. Ownership theo §8. Anh writer DbContext/migrations/snapshot, shared DI,
canonical contract, integrated Postman. Kiểm tra reservation hiện có trước sửa
shared file. Nếu Huy đang giữ, ghi exact yêu cầu signature/patch/fixture trong
spec/PR, tiếp tục files độc lập. Không edit identity/processing/notification
policy hoặc tạo framework chung. Public/error/schema delta theo spec revision2 đã chốt; rollout DB chung vẫn ngoài quyền.
Không relock FE, retire legacy route, rewrite old JSON/backfill guessed values.

4. Dùng acceptance §9; tests phải quan sát lỗi/scope/replay/version và durable
effects, có rollback/race cases nơi được yêu cầu. SQL chỉ DB isolated, đọc
script/fixture trước chạy. Mock storage/AI không chứng minh live integration.
Không có SQL/MinIO thì NOT RUN rõ gate; không coi zero tests/skip là PASS.
Không tạo provider AI/processing retry/callback remediation, ANH-02,
retention delete hay sync/handover của Huy.

5. Tự review pass1: contract, actor/project scope, state, transaction,
idempotency/retry/concurrency, source immutability, bytes giới hạn.
Pass2: final diff, allowlist/shared writers, compatibility, producer/consumer,
migration recovery, docs/Postman. Fix in-scope, rerun test bị ảnh hưởng và
đọc final diff. Self-review không ghi thành external/peer review.

6. Bàn giao một PR summary: base SHA/head SHA; thay đổi theo luồng;
files/dirty preserved; decisions đã áp dụng và checkpoint tích hợp còn lại; hai review passes và fixes;
commands + counts passed/failed/skipped/not-run; BE verified/mock verified/
external-deployment not verified; limitation/điểm cần ChatGPT review.
Commit/push thường trên anh-review, không amend/force-push. Không cần ZIP,
audit package, seven-part RF report hoặc coordination file riêng.

Hoàn thành toàn bộ gói đã chốt; nếu bị chặn kỹ thuật/tích hợp, làm phần còn lại
và ghi đúng checkpoint. Không gọi ANH-01 Done khi geometry/manual baseline
hoặc acceptance còn thiếu. Khi có quyết định mới ngoài D1..D5, ghi source conflict, phương án cụ thể,
file/symbol bị chặn, tiếp tục phần độc lập; không hỏi lại quyết định đã chốt.
```

## 12. Gate bàn giao và review tiếp

ANH-01 chỉ Done khi các chức năng được xác nhận included đều có acceptance, không còn checkpoint nghiệp vụ cần thiết bị bỏ qua; migrations/compatibility/handoff có bằng chứng tương ứng. Nếu chỉ phần I đạt, ghi **ANH-01 Partial — independent flow complete**, liệt kê chức năng chưa triển khai/checkpoint kỹ thuật; không ghi D1–D5 còn chờ owner vì đã được trả lời. Không đồng nhất BE verified với deploy hay nghiệm thu AI/Android.

Anh gửi base SHA/head SHA (hoặc PR kèm hai SHA). ChatGPT review **đúng diff**, findings có severity, file/symbol, impact và fix; không review ngầm toàn RF. Codex tiếp tục sửa trên anh-review bằng commit mới, rerun affected checks. Không cần tạo PR summary thứ hai cho cùng lần handoff; cập nhật summary hiện có.

Nguồn GitHub cố định để đối chiếu: [HEAD khảo sát](https://github.com/HoangAnhVu2207/RoadGuardSystem/commit/1ecae797caaed1ab912b02b2372a1940d1e05375), [guidance](https://github.com/HoangAnhVu2207/RoadGuardSystem/blob/1ecae797caaed1ab912b02b2372a1940d1e05375/AGENTS.md), [confirmed decisions](https://github.com/HoangAnhVu2207/RoadGuardSystem/blob/1ecae797caaed1ab912b02b2372a1940d1e05375/docs/product/confirmed-decisions.md).

## Multipart recovery continuation — owner assignment 2026-10-03

TARGET_CONFIRMED: owner explicitly assigns real recovery including legacy claims, crash/lost ack, background orphan cleanup, SQL/HTTP/live/process restart, normal commit/push. Initial base/local/remote f7a32dcfaa842e1ab6557ec19b0acb0c0c053a74; initial dirty none. Anh/root reserves UploadSession/mapping/additive migration/snapshot, upload repository/storage/worker/options, focused tests, canonical contract/Postman and existing summaries. Shared receipt primitive and Huy files remain unchanged.

Design (before implementation): bounded single-initiation per logical session, at its existing globally unique uploads/{fileId:N} key. The stable durable attempt identity is UploadSession.Id + immutable ObjectKey; nullable generation fence, phase, deadline and next-reconcile time on UploadSession; no FailureCode state payload expansion. Phases CLAIMED (no remote call yet), CALLING (remote outcome unknown), RECONCILING (new generation fences old completion), DURABLE, TERMINAL. Claim and CALLING transitions commit inside SQL execution strategy, network outside SQL transaction/locks. A per-generation GUID fences every result; ownership transfer never implies the old worker stopped. Caller cancellation propagates; later requests/worker use durable rows independently.

Provider: existing AWSSDK.S3 4.0.103.4 has ListMultipartUploads, ListParts, AbortMultipartUpload. Recovery capability is additive separate interface; no interface break for existing test/consumer adapters. Concrete initiation disables SDK retries (external create is not idempotent). Exact-key list exhausts key/upload-ID marker pagination and filters exact equality, never prefix-neighbor ownership. One candidate with zero parts may be adopted while session is Pending, current authority remains valid, and fencing generation matches. Zero candidates before deadline =>503 retry; zero after deadline or multiple/nonempty candidates => FAILED/multipart_restart_required, never blind re-initiate. Existing create route with a new key creates a new logical session/file; old create receipt still replays the original terminal session identity. No stored part failure is rewritten, and no URL/receipt is emitted before authoritative ID durability. Normal receipts/business/audit retain their SQL atomicity.

Terminal is a tombstone, not a lease expiry proof. Worker continues exact-key sweeps for terminal/expired attempts to abort late-created uploads; never deletes objects or complete evidence. Active authoritative ID is excluded from cleanup; extra IDs at the owned key are aborted. NoSuchUpload is success; lost abort ack is retried by listing. No finite empty sweep proves an in-flight old request stopped, so tombstones remain sweepable. StoredFile URI/content identity stays immutable. Legacy multipart_initiating claims always use terminal/restart at the proven session/file key; old SDK calls lack sufficient attempt metadata, so no ID is guessed or adopted. Legacy accepted uploads without attempt metadata remain compatible.

Configuration: recovery grace/deadline 120 seconds default (bounded 1..3600), retry interval 30 seconds (1..300), batch 20 (1..100). Request retry runs recovery for its session; dedicated small hosted worker opt-in UploadSession:RecoveryEnabled (automatic in configured Development storage) processes due claims/terminal tombstones without a client. Transient provider outage remains503 and schedules retry, expiry forbids resurrection. SQL ownership state changes are short transactions with row locks; no SQL app lock held over network. Recovery result persist reacquires terminal/current authority/fence checks in a fresh execution-strategy transaction after prior transaction disposal.

Implementation/verification plan (inline, no extra plan/report):
- [x] Add nullable phase/fence/deadline scheduling metadata and isolated additive migration; preserve old rows and no pending EF model changes.
- [x] Add paginated exact-key recovery adapter/list-parts/abort and disabled physical create retries; provider fixture tests for pagination/ambiguity/NoSuchUpload/lost abort ack.
- [x] Replace private claim path with fenced coordinator; current auth before receipt/new write/replay, no remote I/O in SQL retry; background recovery caller and controlled terminal/restart.
- [x] Fault SQL tests map a..r to durable state/effects/counts; actual HTTP/SQL status, recovery/complete/download/authority/replay; live MinIO lost ack + owned process termination/restart + hash + orphan cleanup.
- [x] Self-review twice, affected build/tests/model/diff checks, canonical/Postman/docs/summary evidence, selective normal commit/push. Preserve initial failures/NOT RUN and global Partial gates.

CURRENT_VERIFIED implementation refinements: project and private callers share the fenced coordinator so removing the old network-held SQL app lock does not introduce a project initiation race. Project guards repeat the existing active actor/role/effective membership and current survey assignment policy in scoped SQL; private Reporter guards are unchanged. No shared receipt primitive edit. Direct acknowledgement binds its trusted returned ID once; a unique unknown candidate is adoptable only with zero parts. Provider/transport retries are not assumed exactly-once, so extra IDs around a known authoritative ID have cleanup as well.

UploadMultipartSweeps is a minimal scheduling-only table for durable bindings. It avoids poll-induced RowVersion/ETag drift on evidence. Expired UPLOADING transitions terminal and aborts outstanding attempts; admitted VERIFYING/VERIFIED is preserved, and their authoritative ID is never aborted. Recovery transaction rechecks fence/status/current authority before binding, terminal caller mutation and protected receipt delivery after presigning. Worker has proven ownership rather than actor authority for cleanup; it does not grant a protected response. Terminal tombstones remain sweepable indefinitely because an empty list never proves a late worker cannot appear. SDK list uses exact key equality after pagination; no object deletion.

One additive new migration 20261003170000_Anh01MultipartRecovery follows actual last baseline 20261003160000_AnhHuyDependencyDefectConcurrency. Earlier scaffold wall-clock timestamp preceded a manually dated migration and was corrected before package delivery. Existing applied migration files unchanged; test-owned fresh/upgrade fixtures only. Scaffolding omitted baseline owned-navigation Restrict metadata; preserved that annotation in snapshot/designer without changing Huy mapping/FKs. Populated downgrade is blocked rather than losing live fences. EF reports no pending model changes.

Acceptance a..r and exact commands/counts are in existing ANH-02-summary. Final new source has85 distinct focused cases (69 SQL/adapter/migration +16 HTTP), plus3 separate live MinIO flows at run9093accef53e433da8a99bc7c565d19d. All pass; previous-source results remain historical. Synthetic valid PNG9,004,013bytes has2 real multipart PUTs, complete receipt replay, download/hash. Real owned API process termination/restart and independently scheduled orphan cleanup executed. No8GiB rerun (no streaming/buffering/limits/part sizing/assembly change), no shared/native DB migration, no Huy/identity/processing implementation edits. Overall packages remain Partial with Huy/CRS/external review/deployment gates.
