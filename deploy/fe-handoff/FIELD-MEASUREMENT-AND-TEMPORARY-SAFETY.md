# FIELD measurement, TemporarySafety và AI MOCK

Base source: `9a022bd15be444e77a1ec3b500e9a7dce90ceb6e`. Quyết định owner 23/11/2026: làm đủ bốn mục; safety mới thuộc B ngay sau chuyển formal, giữ provenance A. Tài liệu này thay giới hạn AI ở yêu cầu cũ.

## 1. FIELD — trigger nhận mã đo 1–6

Nguyên nhân CURRENT_VERIFIED: validator/domain đã có mã 5/6 nhưng trigger `TR_FieldInspectionTasks_H3Scope` còn giới hạn 1–4. Migration mới `20261123093000_FieldTaskMeasurementDimensions` cài toàn bộ định nghĩa trigger đã kiểm soát, chỉ mở danh sách thành `(1,2,3,4,5,6)`. Giữ guard nguồn REPORTER/SURVEY, route/segment/layout/CRS đã pin, assignment, version và bất biến trường task. Không sửa migration cũ hay dữ liệu cũ.

| Mã | Ý nghĩa |
|---|---|
|1|DepressionDepth — độ sâu chỗ lõm|
|2|SlabFaultingHeight — chênh cao tấm|
|3|ShoulderErosionExtent|
|4|Area|
|5|DepressionLength — chiều dài chỗ lõm|
|6|DepressionWidth — chiều rộng chỗ lõm|

Mã 0/7 vẫn bị chặn. Việc bắt buộc đo theo policy/task hiện hành; đợt này không tự gán số đo AI hoặc thêm policy. Endpoint FIELD và payload cũ không đổi. Test tạo task thực qua HTTP cho cả nguồn REPORTER/SURVEY với từng mã; SQL xác nhận giá trị đã lưu. Đã kiểm upgrade từ migration trước và trigger tương đương được patch 1–6; chưa có bản patch DB local thực tế của Hoàng để khẳng định tương đương toàn bộ. Down từ migration FIELD bị từ chối 51990 khi còn task hiện đại dùng 5/6; không ép đổi mã để downgrade.

## 2. PM chọn/bổ sung nghĩa vụ TemporarySafety

Hai route mới:

- `GET /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/temporary-safety-obligations`: PM có membership hiện hành ở project đang chịu trách nhiệm formal; Supervisor đọc theo guard hiện hành. Crew không được gọi collection này.
- `POST` cùng path: chỉ PM có quyền hiện hành. Bearer hoặc Web session; Web gửi `credentials: "include"` và `X-CSRF-TOKEN` lấy từ `GET /api/v1/auth/web/csrf`. GET không cần CSRF.

Nguồn ID: mở item/formal qua các nguồn repair/lifecycle đã có. Nếu nhận formal từ A, lifecycle B trả `responsibilities[].packageId/currentRepairItemId` của đúng `obligationId`. Collection trả `formalObligationId`, scope và binding hiện hành để lấy geometry pins; không tự dùng geometry khác project. Chỉ liệt kê safety cùng package/Defect, chưa resolved, chưa dùng tạo measure, scope nằm trong formal, trách nhiệm hiện hành cùng project URL. `items: []` là kết quả hợp lệ.

POST cần `If-Match: "<packageVersion>"` từ ETag của GET và `Idempotency-Key` đã lưu cùng nguyên body/header trước gửi. Input ví dụ (ID thay bằng dữ liệu từ nguồn của item):

```json
{
  "scope": {
    "physicalRoadId": "<formalScope.physicalRoadId>",
    "routeVersionId": "<binding.routeVersionId>",
    "segmentSetId": "<binding.segmentSetId>",
    "layoutRevisionId": null,
    "slabId": null,
    "fromMeters": 1.0,
    "toMeters": 1.4,
    "offsetFromMeters": 0.0,
    "offsetToMeters": 1.0
  },
  "mandatory": true,
  "reason": "Bổ sung nghĩa vụ bảo đảm an toàn trong phạm vi formal"
}
```

Các giá trị mét phải đúng phạm vi thực và chính xác tối đa ba chữ số thập phân. Không gửi `locationVersion` làm route ID. Nếu binding có layout/slab pin phải giữ đúng pin. Chưa có binding thì lấy pin từ nguồn item đã được phép đọc; không suy ID từ chuỗi hash.

201 tạo mới; replay đúng request trả 200, cùng ID, sau khi kiểm quyền hiện hành. Khác key không đương nhiên conflict: vẫn xét ETag và overlap nghiệp vụ. Cùng key khác fingerprint trả 409 `idempotency_key_reused`. Fingerprint có expected package version, nên retry giữ cả If-Match cũ. Sau conflict tải GET lại để PM xác nhận và gửi thao tác mới bằng key mới.

Response GET thực từ HTTP test trên SQL cô lập (ID/version chỉ thuộc fixture, không dùng trên FE):

```json
{
  "packageId": "c76fa379-97dc-4005-8b01-bbd209ee9b03",
  "itemId": "98201ff6-9c2e-4aee-8b7f-da81a8613d39",
  "defectId": "dc06650b-609b-497f-95c1-39a1654998fd",
  "formalObligationId": "f36bd641-d3a5-4f5c-ae27-00c39af877d5",
  "responsibilityProjectId": "8fb83e12-9a5d-4522-8e97-44a0a46653b0",
  "packageVersion": "AAAAAAAACCM=",
  "itemVersion": "AAAAAAAACBg=",
  "formalScope": {
    "physicalRoadId": "4fc37498-361f-45e5-8ae5-25b6fe221f16",
    "locationVersion": "h4-frame-v1:8403f693f34d6e64af3111b76c1277fcb15e217d0e78f5ad8ab815f26f050a38",
    "routeLabel": "actual road",
    "fromMeters": 1.0,
    "toMeters": 2.0,
    "offsetFromMeters": 0.0,
    "offsetToMeters": 1.0
  },
  "binding": {
    "itemId": "98201ff6-9c2e-4aee-8b7f-da81a8613d39",
    "obligationId": "f36bd641-d3a5-4f5c-ae27-00c39af877d5",
    "taskId": "f9a4b959-c815-4391-b11f-ec997529fb44",
    "assignmentId": "3894e0e3-3356-45bd-8c28-ff8e8abd3569",
    "mode": "NORMAL",
    "authorizationId": null,
    "policyRevisionId": null,
    "routeVersionId": "d95e6c4d-e695-4794-af13-053161867205",
    "segmentSetId": "14af8555-28f0-4d0f-a3c4-aa8e9bf0dbbf",
    "layoutRevisionId": null,
    "mapPublicationId": null,
    "crsProfileRevisionId": null,
    "slabId": null,
    "firstStartId": null
  },
  "items": [
    {
      "safetyObligationId": "98bf0014-0c25-461b-a8ac-f4e15ce16395",
      "formalObligationId": "f36bd641-d3a5-4f5c-ae27-00c39af877d5",
      "packageId": "c76fa379-97dc-4005-8b01-bbd209ee9b03",
      "itemId": "98201ff6-9c2e-4aee-8b7f-da81a8613d39",
      "defectId": "dc06650b-609b-497f-95c1-39a1654998fd",
      "kind": "TEMPORARY_SAFETY",
      "mandatory": true,
      "scope": {
        "physicalRoadId": "4fc37498-361f-45e5-8ae5-25b6fe221f16",
        "locationVersion": "h4-frame-v1:8403f693f34d6e64af3111b76c1277fcb15e217d0e78f5ad8ab815f26f050a38",
        "routeLabel": "actual road",
        "fromMeters": 1.0,
        "toMeters": 1.4,
        "offsetFromMeters": 0.0,
        "offsetToMeters": 1.0
      },
      "resolved": false,
      "originProjectId": "395cc6db-6c9d-4b1c-ad2d-5699942804d5",
      "responsibilityProjectId": "8fb83e12-9a5d-4522-8e97-44a0a46653b0",
      "version": "AAAAAAAACCI="
    }
  ]
}
```

- ETag ngoài cùng và `packageVersion`: version package để append safety.
- `itemVersion`: version item cho POST `.../items/{itemId}/safety-measures` hiện có. Không dùng packageVersion cho lệnh này.
- `items[].version`: version nghĩa vụ safety; `safetyObligationId` khác `measureId`, `evidenceLinkId` và `fileId`.
- `binding` nullable khi chưa có binding. Append không tự approve, đổi item state hoặc gán Crew. Tạo measure vẫn đòi NORMAL, approval/binding còn hiệu lực và Crew đúng binding.

Chuỗi FE: GET collection → nếu rỗng có thể POST nghĩa vụ → GET lại → lấy `safetyObligationId` và `itemVersion` → POST safety-measures. Body measure gồm `formalRepairObligationId`, `safetyObligationId`, `responsibleActorId` đúng Crew binding, `checkSchedule`, `replacementCondition`, `removalCondition`, `reason`; gửi item If-Match và key riêng. Crew đọc `GET .../items/{itemId}/safety-measures` lấy measureIds, rồi `GET .../safety-measures/{measureId}` theo quyền Crew hiện hành. Collection loại nghĩa vụ đã được dùng; không tạo measure thứ hai.

Sau chuyển A→B: PM B dùng URL B, nghĩa vụ mới có originProjectId=A/responsibilityProjectId=B, source package/Defect/geometry A. Authority riêng kind `InitializeTemporarySafetyResponsibility` (8) cùng transaction với obligation/audit/receipt. Không tạo AcceptTransfer giả, không copy acceptance formal. Cột persistence lịch sử `AcceptanceActionId` giữ authority head; wire lifecycle phân biệt `acceptanceActionId: null`, `initialResponsibilityActionId: <creation action>`. Chuyển sau đó vẫn dùng workflow transfer thực. Safety cũ không tự chuyển theo formal. Crew vẫn phải có quyền/binding nguồn A; Crew chỉ thuộc B không tự đủ điều kiện. Append thành công không chứng minh execution-ready.

Lỗi: 400 `validation_error`; 401 thiếu/không còn phiên; 403 `access_forbidden`/CSRF hoặc `safety_scope_not_authorized` ở measure; 404 `not_found` khi item không thuộc trách nhiệm project URL; 428 `precondition_required` thiếu key/version; 409 `concurrency_conflict`, `active_scope_conflict`, `safety_obligation_source_conflict`, `temporary_safety_not_allowed`. Package hoàn tất, item cancelled/confirmed/superseded hoặc formal resolved không được append; dùng workflow sửa/correction được phép hiện hành. Không âm thầm mở lại package. Retention/inventory tiếp tục fail-closed, không coi action khởi tạo safety là bằng chứng đã chuyển nghĩa vụ.

## 3. AI candidate Web readback

Action chính xác: `GET /api/v1/projects/{projectId}/ai-candidates/{detectionId}`. Bổ sung eligibility riêng action, không mở toàn bộ controller/file. PM có membership project hoặc Supervisor theo source guard; Operator/Reporter/Crew không được cấp quyền mới. Bearer giữ nguyên; Web dùng credentials include, GET không cần CSRF. ETag là sourceVersion của candidate, không phải Defect version.

Sau `KEEP_NEW` thành công: mở phiên Web mới → GET candidate → `disposition.decisionId` → `GET /api/v1/projects/{projectId}/candidate-decisions/{decisionId}` → `defectId` → `GET /api/v1/projects/{projectId}/defects/{defectId}`. Các request đọc vẫn kiểm quyền hiện hành. Refresh không tạo Defect và không dùng decision của candidate khác. POST quyết định hiện hành giữ CSRF/ETag/idempotency của contract riêng; đợt này không đổi lệnh đó. 401 thiếu phiên; 403 sai role/project/membership; 404 không có nguồn đúng project; 409 `source_not_ready`/`stale_geometry` nếu provenance/pin không còn hợp lệ.

## 4. AI MOCK — chẩn đoán và correlation

`POST /api/v1/projects/{projectId}/ai-mock-runs`, GET run và GET run/result giữ status/code/correlationId cũ. Lỗi AI thêm `reasonCode`, `step`, `field` nullable và `recovery` (chuỗi do server kiểm soát). Ví dụ 422 có chủ đích:

```json
{
  "status": 422,
  "title": "AI mock request could not be completed",
  "code": "ai_contract_invalid",
  "correlationId": "<X-Correlation-ID>",
  "reasonCode": "fixture_version_unsupported",
  "step": "admission",
  "field": "fixtureVersion",
  "recovery": "Select a supported, versioned MOCK fixture."
}
```

Phân biệt fixture thiếu/sai version/hash, source/hash/lineage, manifest hash/schema, result schema/hash và artifact integrity. Không trả stack trace, đường dẫn fixture, payload hoặc URL storage cho client. Chỉ dùng fixture approved `synthetic-road-v1` và đúng hash; không gắn nhãn AI thật. Source không hợp lệ vẫn bị chặn, không tự sửa hash/provenance.

Run readback thêm `originCorrelationId` nullable và `failure` nullable. Run cũ không có origin giữ null. Lỗi bất đồng bộ có run FAILED/errorCode cũ và `failure` chứa reasonCode/step/field/recovery/originCorrelationId lưu cùng audit trong transaction. Ví dụ thiếu approved frame: `ai_contract_invalid`, `fixture_missing`, `mock_materialization`, field `fixtureVersion`. Thiếu artifact tạm thời dạng I/O vẫn theo lease/retry, không biến thành kết quả AI thành công. Log server nối project/run/job và origin qua enqueue→worker→kết quả; tra bằng originCorrelationId của run và correlationId của request, không chỉ correlation của GET sau này. Không đưa token/credential vào log.

## Migration và kiểm lại FE

Thứ tự triển khai: backup theo quy trình vận hành → áp hai migration trên → backend mới → FE mở phiên Web mới để kiểm. Lệnh mẫu từ checkout đúng source, cấu hình connection theo môi trường được phép:

```powershell
dotnet ef database update --project RoadGuardSystem.Repositories --startup-project RoadGuardSystem.API
```

Migration safety `20261123093100_InitialTemporarySafetyResponsibility` mở check kind 1–8 và trigger exact provenance; không backfill. Down từ migration này từ chối 51991 khi đã có kind-8 history. Không xóa lịch sử để rollback. Cần phương án restore được review nếu buộc quay về schema cũ. Sandbox catalog giữ 197 bảng, thêm hai migration và chữ ký schema SQL thực tế. Không thêm/đổi identity SQL integration test nên manifest lane giữ nguyên.

Checklist Hoàng: cập nhật DB/backend local đúng thứ tự; đọc trigger OBJECT_DEFINITION để đối chiếu 1–6 và guard trước khi tạo task; kiểm mã 5/6 REPORTER/SURVEY; mở item NORMAL đang phụ trách, đọc collection, append, tạo measure và Crew đọc lại; kiểm B/A quyền hiện hành; đọc AI candidate sau F5 và tra lỗi bằng correlation. Patch DB ngoài repo chưa được áp/đối chiếu trên DB Hoàng. Không coi các fixture này là nghiệm thu Web T1–T3 hay AI thật.

## Bằng chứng kiểm thử và giới hạn

Các lượt tập trung (không cộng thành số test độc lập):

| Lượt cuối còn áp dụng | PASS | FAIL | SKIP |
|---|---:|---:|---:|
| FIELD upgrade + AI HTTP + toàn bộ ManualSwaggerContractTests |39|0|0|
| Safety/candidate/retention/route inventory + runtime Swagger |14|0|0|
| Swagger nullability/security/description sau chỉnh contract |3|0|0|
| SQL migration/FIELD/safety/responsibility/seeder/sandbox |73|0|0|
| Unit AI/canonical/diagnostic/repair admission/lifecycle |58|0|0|

Lệnh đã chạy từ worktree:

```powershell
dotnet build RoadGuardSystem.slnx --no-restore
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --filter "FullyQualifiedName~FieldMeasurementTriggerHttpTests|FullyQualifiedName~Anh02AiHttpTests|FullyQualifiedName~ManualSwaggerContractTests"
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --filter "FullyQualifiedName~TemporarySafetyObligationHttpTests|FullyQualifiedName~Anh02AiHttpTests|FullyQualifiedName~SandboxOfflineRouteCatalogTests|FullyQualifiedName~RuntimeOpenApiDescribesWebRangesBatchAndScopedExport|FullyQualifiedName~H4RepairSafetyHttpAdmissionTests|FullyQualifiedName~Huy01RetentionInventorySqlTests"
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~H0RetentionMigrationTests|FullyQualifiedName~H2NativeMigrationTests|FullyQualifiedName~H3FieldInspectionWorkflowSqlTests|FullyQualifiedName~H4SafetyRuntimeSqlTests|FullyQualifiedName~LD06ResponsibilitySqlTests|FullyQualifiedName~LD06LifecycleMigrationTests|FullyQualifiedName~DevelopmentSeed_PreservesSupportedProjectUpdate_AndAssessesEveryTable|FullyQualifiedName~SandboxSyntheticSeedTests"
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --filter "FullyQualifiedName~Anh02Ai|FullyQualifiedName~AiFailureDiagnosticTests|FullyQualifiedName~H4RepairProducerAdmissionTests|FullyQualifiedName~ProjectLifecycle"
dotnet format --verify-no-changes --no-restore
git diff --check
```

Analyzer giữ bật. Test AI bật `RG_MINIO_IMAGE=roadguard-ci/minio:source-pinned`, tạo MinIO riêng qua fixture. TRX từng lượt được kiểm riêng bằng `tests/CI/Verify-TestResults.ps1`, không ghép các lượt trùng identity. Các validator tài liệu, agent, repository readiness, Docker, workflow, test-results self-tests và dependency scan đều đạt; không sửa workflow/gate/skip whitelist. Không chạy lại full API hoặc đủ bốn SQL lane; các con số trên là nhóm tập trung local.

Self-review 1: đối chiếu scope/nguồn, transaction, initial responsibility và race với transfer; đọc lại replay và Down. Sửa drift do EF generator làm mất 14 owned-FK Restrict annotations trong snapshot/designer, giữ SQL schema không đổi ngoài kind constraint. Snapshot cuối chỉ đổi kind 1–7 thành 1–8; model parity và schema catalog được test SQL xác nhận.

Self-review 2: đối chiếu producer/consumer/Swagger, nguồn ID và version, riêng biệt acceptance với initial responsibility; sửa mô tả Swagger vượt 320 ký tự, khai báo nullable reference, cập nhật source excerpts. Helper test pin sai được sửa để thật sự gửi route/set giả, rồi cả hai ca FIELD chạy lại đạt. Một build bị khóa DLL bởi testhost đang chạy đã được chạy lại tuần tự và đạt. Không giảm assertion, không nới guard. Đây là self-review, chưa phải external review.

CI base `9a022bd`: run [45083560622](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45083560622) đã SUCCESS khi kiểm. CI của commit mới cần đọc trạng thái riêng; kết quả local không phải CI PASS. OpenAPI snapshot được sinh từ runtime provider bản cuối; bỏ test-only Probe và giữ `/ready` operational hiện có. BE HTTP dùng TestServer với auth Web/Bearer thật, SQL Server cô lập. AI artifact có MinIO test thật; source video/upload storage trong ca AI là fixture mock. Không chạy trên app/browser FE, DB Hoàng/DB FE hoặc AI provider thật. Không deploy hay áp migration môi trường FE trong đợt này.
