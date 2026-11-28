# FIELD, approved REAL export, timeline và chống trùng formal repair

Ngày 2026-11-28. TARGET_CONFIRMED: yêu cầu owner trong task FIELD-REAL-EXPORT-TIMELINE-FORMAL-GUARDS. Base `d800e358afba00ed8db2be2975eda246af9c7b0f`. Đây là handoff BE; không xác nhận UI, Android, inference AI hoặc runtime đã cập nhật.

## 1. FIELD: dữ liệu bất biến và current state

GPS riêng ảnh, `locationProof`, `evidenceReferences` đã có từ **d800e35**, được tái sử dụng. Đợt này thêm `currentRepair` nullable và dùng chung projection:

- `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId}`.
- `GET /api/v1/projects/{responsibilityProjectId}/repair-packages/{packageId}/items/{itemId}/source-tasks/{taskId}/submissions/{submissionId}`.

Direct: quyền FIELD hiện hành, Crew đúng assignment/PM/Sup theo guard. Source-task: quyền item/nghĩa vụ hiện đang phụ trách, exact binding/task/submission; PM bên nhận không cần membership toàn project nguồn và không được đọc item anh em. GET dùng Bearer hoặc Web session ở route đủ eligibility, không CSRF. Quyền metadata và tải file vẫn kiểm riêng; nội dung ảnh lấy qua đường evidence được phép, không biến ID thành URL public.

`missingReasons`/`readiness` ở ngoài là **snapshot intake**. Chuỗi `AFTER_ATTEMPT_BINDING_UNAVAILABLE` có thể còn nguyên sau ACCEPT/Confirmed: guard review vẫn dùng snapshot gốc. Không xóa chuỗi khỏi DB/response, không sửa hash. FE dùng `currentRepair` để trình bày trạng thái hiện tại:

| Trường | Nguồn/ý nghĩa |
|---|---|
| itemId/itemVersion/itemStatus | RepairItem đúng task; itemVersion là repair rowversion |
| bindingId | CurrentBindingId có lineage item/obligation/task/assignment hợp lệ |
| attemptId/isEffectiveSubmission | CurrentIntakeLinkId + CurrentAttemptId + EffectiveIntakeSubmissionId và content hash phải khớp đúng submission đang đọc |
| effectiveSubmissionId | ID bản nộp hiện hành của item; không gán review của ID này cho submission cũ |
| reviewId/reviewDecision/reviewReason | CurrentReviewId của đúng link/attempt/effective submission; reason chỉ SUPPLEMENT |
| finalDecisionId/finalDecisionResult | EffectiveDecisionId đúng item/obligation/defect; không trả CONFIRMED khi current review/state không còn xác nhận |
| missingReasons | Blocker **current**, độc lập cảnh báo intake lịch sử |

Current reasons: `REPAIR_ITEM_NOT_CURRENT`, `CURRENT_BINDING_UNAVAILABLE`, `NOT_CURRENT_EFFECTIVE_SUBMISSION`, `CURRENT_REVIEW_MISSING`, `CURRENT_REVIEW_NOT_ACCEPTED`, `FINAL_CONFIRMATION_NOT_EFFECTIVE`. Chỉ có binding không có nghĩa đã ACCEPT/hoàn tất. Sau supplement, review cũ không đi theo submission mới; correction không giữ final CONFIRMED. FIELD độc lập/receipt POST có `currentRepair:null` (không pin trạng thái sống vào replay receipt).

`contentHash` luôn là hash submission bất biến. ETag GET có currentRepair là **opaque representation hash**, thay đổi khi current state thay đổi; ETag này không thay task version cho FIELD command hoặc itemVersion cho repair command. GET no-store. Dữ liệu cũ thiếu proof/GPS giữ null; không backfill. `locationProof.observedChainageMeters` tính bằng mét, không lấy scope obligation để thay. `evidence[].coordinates` là GPS từng capture (WGS84 longitude/latitude, accuracy mét nếu có), khác GPS số đo và proof. `evidenceReferences` pin đúng evidence/file/version/hash. 409 `field_submission_snapshot_inconsistent` khi snapshot/projection mâu thuẫn; 401 thiếu/thu hồi phiên; 403 thiếu quyền; 404 resource không thuộc scope.

JSON HTTP thực từ fixture SQL cô lập được trích ở phần mẫu cuối; ID chỉ là ID test, FE phải discover ID thật. Retest: nộp → hai GET → intake → SUPPLEMENT → supplement → ACCEPT → final → correction; warning/hash cũ giữ nguyên, current thay đổi. Đọc lại bằng bên nhận và thử sai item/mất quyền.

## 2. Export nhãn REAL đã APPROVED

Giữ pipeline hiện hành:

```http
POST /api/v1/projects/{projectId}/exports
Idempotency-Key: <key của thao tác>
Content-Type: application/json
```
```json
{"kind":"TRAINING","format":"ZIP","includeOriginalFiles":true}
```

PM hiện hành trong project. Dùng Bearer hoặc Web cookie + `X-CSRF-TOKEN` cho POST; lấy token bằng `GET /api/v1/auth/web/csrf`, browser `credentials:"include"`. 202 chỉ là admission. GET cùng path `/{exportId}`, `/{exportId}/manifest`, `/{exportId}/content`; GET không CSRF. Collection vẫn creator-only theo contract riêng. Supervisor có thể đọc metadata job theo quyền export cũ, nhưng quyền project/export không cấp quyền đọc nhãn/ảnh nguồn PM-only: TRAINING manifest/download bị từ chối nếu không có source authority. Với job chưa hoàn tất, content vẫn 409 `export_not_ready`.

`RealAiCandidateFactsReader` + composite DI là resolver REAL; chỉ fallback MOCK khi REAL NotFound, không fallback khi Forbidden/stale/SourceNotReady. Reader kiểm job/attempt/work/result/manifest hash, mapping artifact/file, video version và geometry đã pin. Candidate source được khóa/đọc lại trong snapshot transaction; chỉ **current label revision có review APPROVED** được chọn. Bbox/class lấy từ nhãn duyệt đã chỉnh, không lấy detection gốc.

Manifest labels thêm `aiSource` có kiểu: workItemId (REAL), mockRunId (MOCK; không giả làm work item), attempt/result, manifest/result hash, contract version, video ID/version, timestampMs, kích thước frame nullable và `NORMALIZED_XYWH`. Top-level vẫn có label/revision/approval, detection, job/model/dataset, file/version/hash và `mode:REAL`. Legacy thiếu aiSource được bỏ qua khi serialize lại để không đổi hash snapshot cũ.

ZIP: `manifest.json`, `labels.jsonl`, README và frame files khi includeOriginalFiles=true theo archivePath. `x,y,width,height` normalized [0,1], theo đúng frame và kích thước đã pin. false chỉ bỏ attachment; **không bỏ kiểm quyền nguồn**. Không tự xuất video gốc. Nhãn sửa/reject sau snapshot không sửa ZIP cũ. Admission, worker, manifest/download kiểm lại quyền/lineage; source/storage không còn hợp lệ có thể chặn/fail job, không hứa snapshot tải mãi mãi.

Lỗi: 428 precondition_required, 409 idempotency_conflict/export_not_ready, 403 forbidden, 410 export_expired, 422 no_eligible_labels/export_source_unavailable/producer_invalid; thêm `export_ai_source_not_ready`, `export_ai_source_mode_unsupported`, `export_ai_source_version_mismatch`, `export_ai_segment_scope_unresolved`; 503 producer_unavailable/export_storage_unavailable. REAL hiện chưa có per-segment attribution: yêu cầu filter segment mà thiếu binding authoritative trả lỗi rõ, không âm thầm bỏ REAL. Retry cùng key/body giữ cùng job; thao tác mới dùng key mới.

Ví dụ label thực từ HTTP manifest (fixture REAL schema/lineage, ảnh JPEG thật trên MinIO cô lập, **không inference**, source video là metadata fixture, không phải MP4 đã kiểm):

```json
{
  "labelId": "8f908e6d-48de-44d1-87cc-52d2ed13351c",
  "revision": 1,
  "revisionId": "e4b079e1-eddc-4390-8e8c-a0250cd5f922",
  "projectId": "c487a4c3-be26-460d-aef7-bbaac40fea02",
  "typeCode": "H4fa29bb8ea89c4abb9e6223f2fccfd541",
  "x": 0.2,
  "y": 0.3,
  "width": 0.4,
  "height": 0.5,
  "fileId": "65f4956a-170f-4e70-9586-7ca3472a7758",
  "fileVersion": "309a4eaae942962263b643e5779804fd6f4145840f5bba5a5a014d911928d904",
  "sha256": "309a4eaae942962263b643e5779804fd6f4145840f5bba5a5a014d911928d904",
  "sizeBytes": 631,
  "mediaType": "image/jpeg",
  "sourceKind": "AI_DETECTION",
  "sourceId": "a4606d8e-e6eb-4ef5-bb09-5fb928227e5a",
  "sourceVersion": "r1:78f08108f3efe4df56f1d34cb81624c6b5cf0eb81f4d8342347c91e7fa4498d5:00000000000000000000000000000000:00000000000000000000000000000000:00000000000000000000000000000000:00000000000000000000000000000000",
  "approvalId": "4c643312-7a64-4c9e-9be4-2d7c4269b8ec",
  "approvedBy": "291da90c-47cf-4357-95fa-85c15b4c8847",
  "approvedAt": "2026-11-28T15:34:38.2811497+00:00",
  "processingJobId": "4f491273-b311-413e-8cc9-876ef938fca1",
  "modelVersionId": "2aa6bb7b-02ee-4be7-9ee6-81781276169f",
  "datasetVersionId": "de860a05-5412-4ba5-b35e-dd96893d4c8d",
  "mode": "REAL",
  "segmentId": null,
  "aiSource": {
    "workItemId": "4fb9966e-5879-4bbd-9d90-d7ad1549e2be",
    "attemptId": "27addb25-0ed5-4223-a553-1f02d64bce67",
    "resultId": "044d7efb-2287-4394-907f-9c2c42d34bbd",
    "manifestHash": "c6e3fd689a69a28f8025d541d2f7ea72f74a6d6f1e7e1e67792f16c3d64199f4",
    "resultHash": "1daeb9baf45c05791462a72dd40db4633fc047409ce9cc08fe88b337228b6d1e",
    "contractVersion": "rg-ai-worker-v1",
    "sourceVideoFileId": "eed80660-301c-461e-b299-da8865859f71",
    "sourceVideoFileVersion": "AAAAAAAAB+s=",
    "timestampMs": 1500,
    "frameWidth": 1,
    "frameHeight": 1,
    "coordinateConvention": "NORMALIZED_XYWH",
    "mockRunId": null
  }
}
```

## 3. Timeline PM/Supervisor

`GET /api/v1/projects/{projectId}/reports/timeline?aggregateType=Project&aggregateId={projectId}&pageSize=50&cursor=...`. Quyền project hiện hành; GET Web cookie/Bearer, không CSRF. Không nới quyền candidate matching/decision/history cho Supervisor.

| Nguồn | Identity/time/actor authoritative | Phạm vi/nội dung |
|---|---|---|
| Survey audits cũ | AuditLog.Id/OccurredAtUtc/ActorUserId | Survey aggregate/project như trước |
| FIELD events | FieldInspectionTaskEvent.Id/OccurredAt/ActorId | Task độc lập trong project hoặc task binding đúng item/nghĩa vụ hiện phụ trách |
| Repair lifecycle | RepairItemLifecycleEvent.Id/At/ActorId | PROPOSED/APPROVED/ASSIGNED/CANCELLED; metadata item đúng trách nhiệm hiện hành |
| Repair operational audits | AuditLog.Id/OccurredAtUtc/ActorUserId | assessment/execution start/finish/intake/supplement/review/final/correction/review-request; không duplicate audit của lifecycle phía trên |
| Processing audits | AuditLog.Id/OccurredAtUtc/ActorUserId | processing_job_created/retried |
| MOCK audits | AuditLog.Id/OccurredAtUtc/ActorUserId | anh02_ai_mock_admitted/completed/failed; actor là người được audit ghi nhận, không khẳng định họ trực tiếp chạy worker |
| REAL terminal outcome | AiVideoAttemptOutcome.AttemptId/OccurredAt | SYSTEM từ kết quả worker đã lưu, không dựng actor người |

Chỉ trả summary cố định an toàn, không raw audit JSON, reason riêng, email, ảnh hay snapshot evidence. Mỗi item thêm actorId nullable, actorKind USER/SYSTEM/UNKNOWN, actorUnavailableReason và actorDisplaySemantics. Với USER, tên **tài khoản hiện tại** (`CURRENT_ACCOUNT`), không phải tên lịch sử. Account không còn đọc được: ID/tên null, UNKNOWN/ACCOUNT_UNAVAILABLE. Legacy không có actor: UNKNOWN/ACTOR_NOT_RECORDED; không tự gọi SYSTEM.

Project timeline vẫn **PARTIAL** với `CASE_CANDIDATE_HISTORY_NOT_INCLUDED`, `REAL_AI_NONTERMINAL_HISTORY_NOT_RECORDED`. Các aggregate survey riêng và snapshot reporting/dossier vẫn giữ coverage cũ; không quảng cáo là toàn bộ audit. Decision/evidence đầy đủ vẫn qua history có guard riêng.

Union tất cả nguồn trước khi sort/page; key `(occurredAt ASC, SQL Guid eventId ASC, source.type ordinal ASC)`, dedup `(source.type,eventId)`. Giữ pageSize 1–200 và nextCursor opaque. Token timeline đổi namespace v2: cursor cũ trả **400 cursor_invalid**; FE bắt đầu trang đầu, không diễn giải lại token cũ. Không ghép các trang riêng từng nguồn. 401/403/404 giữ contract; query/filter không hỗ trợ trả validation_error.

## 4. Normal/FastTrack: formal scope admission

Mới: `GET /api/v1/projects/{projectId}/repair-packages/eligibility?defectId=...&scope.physicalRoadId=...&scope.routeVersionId=...&scope.segmentSetId=...&scope.fromMeters=1&scope.toMeters=2&scope.offsetFromMeters=0&scope.offsetToMeters=1` (thêm layoutRevisionId/slabId khi scope dùng chúng). PM đúng project, GET Web/Bearer không CSRF. Các ID/pin lấy từ Defect/geometry được phép, không tự chế.

- Không truyền scope: 200 eligible:null/code scope_required; không hiểu thành đủ điều kiện cho mọi scope.
- Scope đã chứng minh chưa bị reserve: 200 eligible:true/reason FORMAL_SCOPE_AVAILABLE_NOT_EXECUTION_AUTHORITY. Đây chỉ là **scope preflight**, không thay eligibility execution/coverage/policy/approval.
- Bị chặn: 200 eligible:false/code active_scope_conflict hoặc completed_scope_conflict; reason ACTIVE_FORMAL_SCOPE, COMPLETED_FORMAL_SCOPE hoặc SCOPE_COMPARISON_UNVERIFIED.
- obligationId/packageId/itemId/mode/status chỉ có khi thuộc trách nhiệm đang được đọc; chưa có item thì nullable. Metadata ngoài quyền bị ẩn nhưng conflict vẫn chặn.

Mutation `POST /projects/{projectId}/repair-packages` và `POST .../{packageId}/items` kiểm lại cùng evaluator trong transaction/SQL lock Defect anchor (kể cả chưa có obligation). Mandatory formal scope được reserve từ package, trước khi chọn NORMAL/FAST_TRACK. Chặn equal/contains/overlap/unknown, cả đã resolve; geometry/project/road ID khác không tự chứng minh Disjoint. Chỉ rectangle bounds trong cùng frame authoritative chứng minh disjoint, không buffer/khoảng cách suy đoán. Batch tự so sánh các scope trong request và rollback toàn batch khi conflict. Item đầu của chính obligation không tự chặn nó. TemporarySafety và FIELD độc lập giữ guard cũ.

POST giữ Idempotency-Key/If-Match đúng resource (Defect cho package, package cho proposal), CSRF nếu cookie. Conflict mutation 409 active_scope_conflict/completed_scope_conflict; cùng key/body replay theo receipt, khác body vẫn idempotency_key_reused. Các gate geometry_version_mismatch/physical_road_source_mismatch/repair_anchor_not_ready/... không đổi. Crew từ chối/proposal rejected/thiếu item/cancelled item không tự giải phóng obligation; successor/replacement/correction giữ lineage hiện có. Không tạo lifecycle hủy/tái phát/chuyển mode mới, không tự xử lý hồ sơ trùng cũ.

## Nghiệm thu FE và giới hạn

- Hoàng: phân biệt warning lịch sử/currentRepair, không dùng GET submission ETag cho FIELD mutation; xem tên actor theo semantics; xử lý cursor_invalid bằng reload; disable chọn scope bị chặn và vẫn xử lý 409 mutation.
- Export: tạo nhãn → APPROVE → TRAINING ZIP → poll → manifest/content; test chỉnh nhãn sau snapshot và mất quyền. Không dùng job 202 để báo export hoàn tất.
- Tùng: GPS ảnh/proof dùng theo FIELD-EVIDENCE-GPS-READBACK.md đã bàn giao ở d800e35; không suy GPS cho ảnh thiếu, không dùng điểm bản đồ làm GPS verified.
- Không migration mới, không thay DB model/schema. Không build image/đổi runtime trong đợt này. Push source không cập nhật môi trường FE.
- SQL/MinIO dùng container/DB test sở hữu riêng; FIELD closeout ảnh AFTER là metadata VERIFIED fixture (không upload ảnh thật). REAL export ảnh/ZIP dùng MinIO thật, worker completion/provenance schema thật nhưng không inference/video validation. Không kiểm browser/app/UI F5/full suite/local SQL lanes.
- Repo docs là fallback; chưa xác minh quyền/đích Knowledge Space, chưa tuyên bố đồng bộ/reindex Space.

Tham chiếu source: FieldInspectionWorkflowRepository.CurrentRepair, RepairWorkflowRepository.SourceReads, TrainingLabelExportReader, RealAiCandidateFactsReader, ExportService, ReportingRepository.Timeline, FormalRepairScopeAdmission, RepairWorkflowRepository.FormalScope. OpenAPI runtime: `/swagger/v1/swagger.json`; snapshot `openapi.json` cùng thư mục được sinh từ bản build kiểm cuối. Spec: planning/development/FIELD-REAL-EXPORT-TIMELINE-FORMAL-GUARDS.md.

## Mẫu readback từ lượt HTTP/SQL kiểm tập trung

ID chỉ thuộc DB test cô lập. Đây là trích field liên quan, không phải payload đầy đủ.

```json
{
  "id": "333ad29e-f570-40d6-b0ab-9598f9ce7a9f",
  "taskId": "ecf87cf8-4f26-46bc-ae28-a322a29b07dd",
  "revision": 3,
  "readiness": "INCOMPLETE",
  "missingReasons": [
    "AFTER_ATTEMPT_BINDING_UNAVAILABLE"
  ],
  "contentHash": "c9bd8189db154a1fc1b1261ba1824ff1abba72645e10ce5e9fe8d3ba21ac0f4b",
  "locationProof": {
    "kind": "POSITION_CHECKLIST",
    "checklist": "ROUTE_CHAINAGE_MARKINGS_CONFIRMED",
    "longitude": null,
    "latitude": null,
    "observedSlabId": null,
    "observedSegmentId": null,
    "observedRouteVersionId": "0b0588ee-dcaf-48e3-b2bd-832f2c48df53",
    "observedChainageMeters": 1
  },
  "currentRepair": {
    "itemId": "c94a5e50-c1e6-430d-a2a2-0977e22ad2b1",
    "itemVersion": "AAAAAAAACEc=",
    "bindingId": "5899e2c1-6eb4-440e-bd59-17c6ac6967e1",
    "attemptId": "5475058d-aa86-43a6-9998-1f661020e4ef",
    "effectiveSubmissionId": "333ad29e-f570-40d6-b0ab-9598f9ce7a9f",
    "isEffectiveSubmission": true,
    "reviewId": "60586e3c-d5c1-4b56-9378-b51e627d4f84",
    "reviewDecision": "ACCEPT",
    "reviewReason": null,
    "itemStatus": "CorrectionRequired",
    "finalDecisionId": "e0d07a58-a293-488c-ba53-f99b4ec416c0",
    "finalDecisionResult": "UNREPAIRED",
    "missingReasons": [
      "FINAL_CONFIRMATION_NOT_EFFECTIVE"
    ]
  }
}
```

Item timeline terminal worker thực đã lưu:

```json
{
  "eventId": "27addb25-0ed5-4223-a553-1f02d64bce67",
  "occurredAt": "2026-11-28T15:34:34.8850426+00:00",
  "actorDisplay": null,
  "action": "AI_ATTEMPT.Ingested",
  "source": {
    "type": "AiVideoAttemptOutcome",
    "id": "4fb9966e-5879-4bbd-9d90-d7ad1549e2be",
    "version": "aadbcc7641db4ce7a2a1f270ba117f63f8c90e7ee51b803bb45770808a2449ec"
  },
  "summary": "Persisted worker terminal outcome; no human executor is recorded.",
  "actorId": null,
  "actorKind": "SYSTEM",
  "actorUnavailableReason": null,
  "actorDisplaySemantics": null
}
```

Mixed REAL/MOCK được kiểm bằng typed consumer fixture riêng; fixture đó không chứng minh joint source resolver/inference. REAL SQL resolver/MinIO và MOCK producer/read lifecycle có lượt kiểm riêng.

Snapshot OpenAPI chỉ cập nhật năm path và các schema thuộc bốn nhóm từ Swagger build đã kiểm; giữ phần snapshot ngoài phạm vi. Không đưa route/schema Probe của test assembly vào snapshot. `/ready` có điều kiện HandoffBootstrap vẫn giữ mục tài liệu cũ; không suy cấu hình runtime từ factory kiểm Swagger.

Preflight HTTP thực, trước khi có item (nullable là có chủ đích):

```json
{
  "eligible": false,
  "code": "active_scope_conflict",
  "reason": "ACTIVE_FORMAL_SCOPE",
  "obligationId": "06acfaff-5014-4882-b91d-4dfce73ec21b",
  "packageId": "23f0a25e-a2fc-4a25-84fc-c2883890953c",
  "itemId": null,
  "mode": null,
  "status": null
}
```

Cùng scope sau khi item NORMAL đã được đề xuất:

```json
{
  "eligible": false,
  "code": "active_scope_conflict",
  "reason": "ACTIVE_FORMAL_SCOPE",
  "obligationId": "06acfaff-5014-4882-b91d-4dfce73ec21b",
  "packageId": "23f0a25e-a2fc-4a25-84fc-c2883890953c",
  "itemId": "0e17a7ff-593a-4eb2-8445-4ea850219920",
  "mode": "NORMAL",
  "status": "AwaitingApproval"
}
```

## Kết quả local và kiểm lại

- HTTP focused 12/12; SQL hai class 19/19; unit focused cuối 161/161, đều 0 skip.
- Contract/regression rộng hơn: 49 pass, 1 fail ở metadata Swagger (khai báo persistence fact không public). Đã sửa contract và chạy lại hai ca schema 2/2. Hai ca theory route giữ nguyên assertion đã đổi alias tham số để identity ổn định: 2/2. Rerun formal/timeline 5/5 và FIELD bổ sung completed mutation/transfer privacy 2/2. Các lượt có trùng nhau, không cộng tổng.
- Mixed REAL/MOCK chỉ là consumer fixture có kiểm ZIP giữ đủ hai nhãn; REAL resolver/MinIO và MOCK producer/read có bằng chứng riêng. Không suy thành inference hay nghiệm thu dữ liệu thật.
- Build Release/analyzer 0 error; format changed files, diff check và các preflight source/docs/agent/workflow liên quan đạt. API lane identities cập nhật từ TRX; SQL manifest không đổi vì không thêm/đổi SQL test.
- Không chạy full API/SQL lanes, không truy vấn/chờ CI. Không migration mới; không build image/deploy hoặc cập nhật môi trường FE.

Hồi quy collection/append TemporarySafety HTTP: 4 pass / 0 fail / 0 skip. Giữ filter unresolved/chưa dùng và guard nguồn/trách nhiệm.
