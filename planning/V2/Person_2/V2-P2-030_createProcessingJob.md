# V2-P2-030 — Tạo job+manifest bất đồng bộ

## V2(3) status

- deliveryStatus: DONE
- decisionRefs: D12, D13, 33A, 34A
- requirementRefs: FR-29
- diagramRefs: PF-07, SQ-04, DD/ERD
- sourceCheckpoint: b4e195d / source comparison 2026-09-29

- contractStatus: PROPOSED_DELTA
- implementationStatus: COMPLETE
- verificationStatus: PASS
- dependencyType: contract
- workstream: BE
- blockers: None; owner approved target persistence and contract decisions on 2026-09-29. Runtime evidence remains required.


- **Owner:** Person 2 — huy. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/processing-jobs`; operationId `createProcessingJob`.
- **Trạng thái kế hoạch:** `NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-29; nhóm kế hoạch cũ P1-31/P2-31 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W5; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D12, D13, 33A, 34A)
- Requirements/trace: FR-29
- Diagrams/state: PF-07, SQ-04, DD/ERD
- Canonical contract: operationId createProcessingJob, path /processing-jobs, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: to be read during NEEDS_REPO_CHECK; this alignment does not claim runtime verification.
- Source comparison 2026-09-29: ProcessingJob/ProcessingAttempt/ProcessingBlock persistence exists only as a lower-level schema; no API/service/repository seam implements the proposed job creation contract.
- Checkpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

## 1. Cần làm và tại sao

Tạo job+manifest bất đồng bộ. Tạo job+manifest bất đồng bộ

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-29; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P2-020 — getDatasetCoverage](../Person_2/V2-P2-020_getDatasetCoverage.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `SUPERVISOR, PM`. Policy: `ai.createJob`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `Idempotency-Key` | header | True | string; minLength=1 |

**Request body:** `ProcessingCreate`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `datasetId` | True | string (uuid) |
| `scope` | True | BandScope |
| `modelVersionId` | True | string (uuid) |
| `preprocessingVersion` | True | string; minLength=1 |
| `configVersion` | True | string; minLength=1 |
| `mode` | True | string; enum=MOCK,REAL |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 202 | application/json: Job | ETag, Location |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `413`, `415`, `422`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `createProcessingJob` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: ProcessingJob, ProcessingAttempt, OutboxMessage, AIModelVersion.
2. Lưu job+manifest+outbox nguyên tử, worker có lease và idempotency receipt. Callback xác thực service và attempt/model/manifest; dùng adapter deterministic cho nghiên cứu, không yêu cầu xây AI thật.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.



**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs`
- `RoadGuardSystem.BusinessObjects/Processing/AIDetection.cs`
- `RoadGuardSystem.BusinessObjects/Processing/AIModelVersion.cs`
- `RoadGuardSystem.BusinessObjects/Processing/ProcessingAttempt.cs`
- `RoadGuardSystem.BusinessObjects/Processing/ProcessingBlock.cs`
- `RoadGuardSystem.BusinessObjects/Processing/ProcessingJob.cs`
- `RoadGuardSystem.Repositories/Configurations/AIModelVersionConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/OutboxMessageConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/ProcessingAttemptConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/ProcessingBlockConfiguration.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi createProcessingJob với payload hợp lệ | HTTP 202, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Callback lặp không nhân detection; callback attempt cũ không hoàn tất attempt mới; enqueue thất bại không có job mất dấu; lỗi provider cho retry có kiểm soát. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P2-030: createProcessingJob
POST {{apiBase}}/processing-jobs
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
Content-Type: application/json

{
  "datasetId": "11111111-1111-4111-8111-111111111111",
  "scope": {
    "routeVersionId": "11111111-1111-4111-8111-111111111111",
    "segmentSetId": "11111111-1111-4111-8111-111111111111",
    "segmentIds": [
      "11111111-1111-4111-8111-111111111111"
    ],
    "targetBand": "SURFACE"
  },
  "modelVersionId": "11111111-1111-4111-8111-111111111111",
  "preprocessingVersion": "{{seed_value}}",
  "configVersion": "{{seed_value}}",
  "mode": "MOCK"
}
```

## Completion history

### 2026-09-29 15:59 +07:00 - BLOCKED

- Scope/result: Compared `createProcessingJob` with FR-29, D12/D13, AI integration README and ProcessingJob persistence. PM-triggering is approved, but the required durable manifest and readiness/version model are not.
- Files: Read `ProcessingJob.cs`, `ProcessingAttempt.cs`, `ProcessingBlock.cs`, `AIModelVersion.cs`, processing configurations/migration and P231 SQL tests; no production or Postman files changed.
- Acceptance criteria: Not executable: current schema cannot persist request dataset/scope/mode/config/preprocessing manifest or return the specified ETag/retry-safe job contract.
- Verification: Source comparison only; runtime, API, SQL and Postman checks NOT_RUN because no endpoint contract was implemented.
- Side effects: No package, migration, schema, data, storage, commit or push.
- Unverified/blockers: Approve immutable job-manifest schema and dataset eligibility predicate before implementation.

### 2026-09-29 19:07 +07:00 - Regression fixed and reverified

- Fixed model-variant creation: `ProcessingBlock` is now reused only for the same dataset/scope; preprocessing/config remain in the immutable job manifest. `ProcessingJob.ProcessingBlockId` is no longer unique, so a changed model or manifest can create a second job for the same block.
- Migration: `20260929120044_P2AllowProcessingJobModelVariants` drops `UX_ProcessingJobs_Block`, creates non-unique `IX_ProcessingJobs_Block`, and was applied to the Development test database.
- Evidence: SQL Server fixture test verifies the non-unique index; Development smoke created jobs `89c725d3-b4bf-4afd-9b29-f84b7e239d83` and `1c3fdf96-9d38-4eed-aabd-16490414b292`, both `202 QUEUED` and sharing one processing block. API build and focused unit/integration checks passed.

## 7. Done và bằng chứng

### 2026-09-29 - DONE

- Delivered immutable processing manifest, attempt, outbox and audit creation through `POST /api/v1/processing-jobs`.
- Evidence: Development SQL/API smoke returned 202 QUEUED with ETag; focused integration suite passed.

- Worklog ghi commit, route thực tế, reuse/new/delta, quyết định liên quan và đường dẫn `.http` có response đã che secret.
- Build project chịu ảnh hưởng; chọn focused/affected/full theo risk và skill repo. Một API phải có smoke trên server thật với SQL test và kiểm effect. Không bắt chạy full suite cho từng task; kết quả lịch sử không phải kết quả chạy hiện tại.
- Chọn test regression cho invariant ở mục 5; lỗi cạnh tranh/dedup cần DB thật. Mock không chứng minh transaction/concurrency. Ghi rõ test chưa chạy và lý do.
- FE đối chiếu schema và error code; cập nhật YAML chính, baseline, types và hash guard cùng change nếu contract thực sự được duyệt sửa. Gate còn mở chỉ cho phép PARTIAL/BLOCKED.

Lệnh tham khảo từ cấu trúc đã gửi (xác minh SDK/global.json trước dùng):

```cmd
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~createProcessingJob" -v q
```
Filter chỉ là tên gợi ý: đổi sang tên test thật, kiểm số test chạy > 0; zero tests không phải PASS.

## 8. Tài liệu và schema đầy đủ

- [01_FRD_SRS.md](../../../docs/diagram/V2/02_Requirements/01_FRD_SRS.md)
- [02_Business_Rules.md](../../../docs/diagram/V2/02_Requirements/02_Business_Rules.md)
- [02_Auth_Permission_Model.md](../../../docs/diagram/V2/05_Technical/02_Auth_Permission_Model.md)
- [03_API_Specification.md](../../../docs/diagram/V2/05_Technical/03_API_Specification.md)
- [04_Error_Handling_Convention.md](../../../docs/diagram/V2/05_Technical/04_Error_Handling_Convention.md)
- [01_Data_Dictionary.md](../../../docs/diagram/V2/03_Data/01_Data_Dictionary.md)
- [02_Test_Cases_UAT_Scenarios.md](../../../docs/diagram/V2/06_Testing/02_Test_Cases_UAT_Scenarios.md)
- [Quy tắc chung và thứ tự tích hợp](../README.md)
- [OpenAPI chính](../../../docs/diagram/V2/05_Technical/openapi.yaml)

Snapshot schema liên quan giúp người nhận task xem cả nested fields. YAML chính vẫn là nguồn contract; snapshot này phải được tạo lại nếu YAML đổi.

```yaml
operation:
  operationId: createProcessingJob
  summary: Tạo job+manifest bất đồng bộ
  tags:
  - ai
  x-roles:
  - SUPERVISOR
  - PM
  x-permission-policy: ai.createJob
  x-fr:
  - FR-29
  x-readiness: PROPOSED_CONTRACT
  description: Tạo job+manifest bất đồng bộ
  responses:
    '202':
      description: Đã nhận bền vững; chưa hoàn tất xử lý.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Job'
      headers:
        ETag:
          description: Strong ETag dạng quoted opaque version dùng If-Match; response.version
            là cùng opaque value chưa bọc quotes.
          schema:
            type: string
        Location:
          description: URI resource/job được tạo, khi có.
          schema:
            type: string
    '400':
      $ref: '#/components/responses/Error400'
    '401':
      $ref: '#/components/responses/Error401'
    '403':
      $ref: '#/components/responses/Error403'
    '404':
      $ref: '#/components/responses/Error404'
    '409':
      $ref: '#/components/responses/Error409'
    '413':
      $ref: '#/components/responses/Error413'
    '415':
      $ref: '#/components/responses/Error415'
    '422':
      $ref: '#/components/responses/Error422'
    '429':
      $ref: '#/components/responses/Error429'
    '500':
      $ref: '#/components/responses/Error500'
    '503':
      $ref: '#/components/responses/Error503'
  parameters:
  - $ref: '#/components/parameters/IdempotencyKey'
  requestBody:
    required: true
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/ProcessingCreate'
schemas:
  BandScope:
    type: object
    additionalProperties: false
    properties:
      routeVersionId: &id001
        type: string
        format: uuid
      segmentSetId: *id001
      segmentIds:
        type: array
        items: *id001
        minItems: 1
      targetBand:
        type: string
        enum:
        - SURFACE
        - LEFT_EDGE
        - RIGHT_EDGE
    required:
    - routeVersionId
    - segmentSetId
    - segmentIds
    - targetBand
    description: Band labels là API đề xuất, map enum DD trước codegen.
  Error:
    type: object
    additionalProperties: false
    properties:
      code: &id002
        type: string
        minLength: 1
      message: *id002
      details:
        type: array
        items:
          $ref: '#/components/schemas/ErrorDetail'
      traceId: *id002
      retryable:
        type: boolean
    required:
    - code
    - message
    - details
    - traceId
    - retryable
    description: Không chứa stacktrace, PII, secrets hoặc giá trị password/token.
  ErrorDetail:
    type: object
    additionalProperties: false
    properties:
      field:
        type: string
      code: *id002
      message: *id002
    required:
    - code
    - message
  Job:
    type: object
    additionalProperties: false
    properties:
      id: *id001
      status:
        type: string
        enum:
        - QUEUED
        - RUNNING
        - SUCCEEDED
        - FAILED
        - REQUIRES_REVIEW
      resultId:
        anyOf:
        - *id001
        - type: 'null'
      attemptNumber:
        type: integer
        minimum: 0
      version:
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
      jobType:
        type: string
        enum:
        - AI_ANALYSIS
        - UPLOAD_VERIFY
        - EXPORT
        - MISSION_EXPORT
        - VALIDATION
        - TRAINING_EXPORT
        - RETENTION
      error:
        anyOf:
        - $ref: '#/components/schemas/Error'
        - type: 'null'
      progressPercent:
        anyOf:
        - type: number
          minimum: 0
          maximum: 100
        - type: 'null'
    required:
    - id
    - status
    - resultId
    - attemptNumber
    - version
    - jobType
    - error
  ProcessingCreate:
    type: object
    additionalProperties: false
    properties:
      datasetId: *id001
      scope:
        $ref: '#/components/schemas/BandScope'
      modelVersionId: *id001
      preprocessingVersion: *id002
      configVersion: *id002
      mode:
        type: string
        enum:
        - MOCK
        - REAL
    required:
    - datasetId
    - scope
    - modelVersionId
    - preprocessingVersion
    - configVersion
    - mode
    description: Manifest persisted before 202; only server resolves file references/rights.
```
