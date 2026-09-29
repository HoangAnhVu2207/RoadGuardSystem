# V2-P2-025 — Yêu cầu kiểm toàn vẹn upload

## V2(3) status

- deliveryStatus: DONE
- decisionRefs: D38, D43A, 42A
- requirementRefs: FR-21, FR-27
- diagramRefs: PF-08, SQ-03, DD/ERD
- sourceCheckpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

- contractStatus: APPROVED_OWNER_RUNTIME_DELTA
- implementationStatus: IMPLEMENTED
- verificationStatus: PASS_SQL_API_MINIO
- dependencyType: contract
- workstream: BE
- blockers: none


- **Owner:** Person 2 — huy. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/uploads/{uploadId}/complete`; operationId `completeUpload`.
- **Trạng thái kế hoạch:** `NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-21, FR-27; nhóm kế hoạch cũ P1-30/P2-30 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W2; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D38, D43A, 42A)
- Requirements/trace: FR-21, FR-27
- Diagrams/state: PF-08, SQ-03, DD/ERD
- Canonical contract: operationId completeUpload, path /uploads/{uploadId}/complete, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: no complete-upload lifecycle, rowversion session or verification worker existed; P2 implementation adds the approved VERIFYING -> VERIFIED/FAILED path.
- Checkpoint: base 5e878212055d3f389a9fc65697ed04ba6a4194bb; migration reconciliation and the separate upload migration now exist and are applied by SQL test fixtures.
- Owner runtime delta: Reporter is explicitly excluded from these six upload endpoints until its auth/runtime slice is approved; supported roles are SUPERVISOR, PM, OPERATOR and CREW.

## 1. Cần làm và tại sao

Yêu cầu kiểm toàn vẹn upload. Yêu cầu kiểm toàn vẹn upload

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-21, FR-27; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P2-026 — getUploadSession](../Person_2/V2-P2-026_getUploadSession.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `SUPERVISOR, PM, OPERATOR, CREW, REPORTER`. Policy: `file.uploadOwner`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `uploadId` | path | True | string (uuid) |
| `Idempotency-Key` | header | True | string; minLength=1 |
| `If-Match` | header | True | string; minLength=3 |

**Request body:** `UploadComplete`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `parts` | True | array; minItems=1 |
| `checksumSha256` | True | string; pattern=^[a-f0-9]{64}$ |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 202 | application/json: UploadSession | ETag, Location |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `412`, `413`, `415`, `422`, `428`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `completeUpload` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: StoredFile, IFileRepository, IFileContentStore, LocalFileContentStore.
2. Tách upload session, part, integrity verification và StoredFile. Server xác nhận checksum/kích thước, scope và ownership; immutable file không bị overwrite. Download đi qua gateway kiểm quyền.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.

Không trả verified chỉ vì client báo đủ part. Integrity/quality job phải xác nhận theo lifecycle; retry completion không khởi tạo hai immutable file.

**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.API/Controllers/ProfileController.cs`
- `RoadGuardSystem.BusinessObjects/Files/StoredFile.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SurveyFile.cs`
- `RoadGuardSystem.DTOs/Identity/ProfileResponseDto.cs`
- `RoadGuardSystem.DTOs/Identity/ProfileUpdateRequestDto.cs`
- `RoadGuardSystem.Repositories/Configurations/StoredFileConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/SurveyFileConfiguration.cs`
- `RoadGuardSystem.Repositories/Implementations/Files/FileRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Files/FileStoreResult.cs`
- `RoadGuardSystem.Repositories/Implementations/Files/StoreFileRequest.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi completeUpload với payload hợp lệ | HTTP 202, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T04 | Thiếu If-Match; version cũ; hai mutation cùng version chạy đồng thời | 428; 412; tối đa một cập nhật theo version thành công, history không mất |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Part thiếu/sai checksum không verified; mất mạng sau commit rồi retry không nhân file; user bị thu quyền không download lại được; file ngoài scope không attach được. |
| T09 | Biên nghiệp vụ của operation | Không trả verified chỉ vì client báo đủ part. Integrity/quality job phải xác nhận theo lifecycle; retry completion không khởi tạo hai immutable file. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P2-025: completeUpload
POST {{apiBase}}/uploads/{{uploadId}}/complete
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
If-Match: "{{version}}"
Content-Type: application/json

{
  "parts": [
    {
      "partNumber": 1,
      "etag": "{{seed_value}}"
    }
  ],
  "checksumSha256": "{{seed_value}}"
}
```

## 7. Done và bằng chứng

- Worklog ghi commit, route thực tế, reuse/new/delta, quyết định liên quan và đường dẫn `.http` có response đã che secret.
- Build project chịu ảnh hưởng; chọn focused/affected/full theo risk và skill repo. Một API phải có smoke trên server thật với SQL test và kiểm effect. Không bắt chạy full suite cho từng task; kết quả lịch sử không phải kết quả chạy hiện tại.
- Chọn test regression cho invariant ở mục 5; lỗi cạnh tranh/dedup cần DB thật. Mock không chứng minh transaction/concurrency. Ghi rõ test chưa chạy và lý do.
- FE đối chiếu schema và error code; cập nhật YAML chính, baseline, types và hash guard cùng change nếu contract thực sự được duyệt sửa. Gate còn mở chỉ cho phép PARTIAL/BLOCKED.

Lệnh tham khảo từ cấu trúc đã gửi (xác minh SDK/global.json trước dùng):

```cmd
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~completeUpload" -v q
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
- [09_Offline_App_Sync_Spec.md](../../../docs/diagram/V2/09_Frontend/09_Offline_App_Sync_Spec.md)
- [11_FE_Offline_Test_UAT.md](../../../docs/diagram/V2/09_Frontend/11_FE_Offline_Test_UAT.md)
- [Quy tắc chung và thứ tự tích hợp](../README.md)
- [OpenAPI chính](../../../docs/diagram/V2/05_Technical/openapi.yaml)

Snapshot schema liên quan giúp người nhận task xem cả nested fields. YAML chính vẫn là nguồn contract; snapshot này phải được tạo lại nếu YAML đổi.

```yaml
operation:
  operationId: completeUpload
  summary: Yêu cầu kiểm toàn vẹn upload
  tags:
  - file
  x-roles:
  - SUPERVISOR
  - PM
  - OPERATOR
  - CREW
  - REPORTER
  x-permission-policy: file.uploadOwner
  x-fr:
  - FR-21
  - FR-27
  x-readiness: PROPOSED_CONTRACT
  description: Yêu cầu kiểm toàn vẹn upload
  responses:
    '202':
      description: Đã nhận bền vững; chưa hoàn tất xử lý.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/UploadSession'
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
    '412':
      $ref: '#/components/responses/Error412'
    '413':
      $ref: '#/components/responses/Error413'
    '415':
      $ref: '#/components/responses/Error415'
    '422':
      $ref: '#/components/responses/Error422'
    '428':
      $ref: '#/components/responses/Error428'
    '429':
      $ref: '#/components/responses/Error429'
    '500':
      $ref: '#/components/responses/Error500'
    '503':
      $ref: '#/components/responses/Error503'
  parameters:
  - name: uploadId
    in: path
    required: true
    schema: &id001
      type: string
      format: uuid
  - $ref: '#/components/parameters/IdempotencyKey'
  - $ref: '#/components/parameters/IfMatch'
  requestBody:
    required: true
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/UploadComplete'
schemas:
  CompletedPart:
    type: object
    additionalProperties: false
    properties:
      partNumber:
        type: integer
        minimum: 1
      etag:
        type: string
        minLength: 1
    required:
    - partNumber
    - etag
  UploadComplete:
    type: object
    additionalProperties: false
    properties:
      parts:
        type: array
        items:
          $ref: '#/components/schemas/CompletedPart'
        minItems: 1
      checksumSha256:
        type: string
        pattern: ^[a-f0-9]{64}$
    required:
    - parts
    - checksumSha256
  UploadSession:
    type: object
    additionalProperties: false
    properties:
      id: *id001
      fileId: *id001
      status:
        type: string
        enum:
        - PENDING
        - UPLOADING
        - VERIFYING
        - VERIFIED
        - FAILED
      partSizeBytes:
        type: integer
        minimum: 1
      expiresAt:
        type: string
        format: date-time
      version:
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
    required:
    - id
    - fileId
    - status
    - partSizeBytes
    - expiresAt
    - version
```

## Completion history

### 2026-09-29 00:00 +07:00 - BLOCKED

- Scope/result: Source review completed; no complete-upload endpoint, session aggregate or verification worker exists.
- Files: No production files changed; task and manifest status updated.
- Acceptance criteria: Not started; VERIFYING/VERIFIED lifecycle, part ledger and durable idempotency are missing.
- Verification: `git diff --check` PASS; API/runtime/SQL/Postman checks NOT_RUN because no implementation exists.
- Reused/invalidated evidence: Existing immutable file storage tests do not prove multipart completion or concurrency.
- Side effects: No package, migration, schema, data, external system, commit or push.
- Unverified/blockers: Need approved session rowversion/ETag, part verification worker, provider contract and SQL transaction tests.

### 2026-09-29 06:30 +07:00 - PARTIAL

- Scope/result: Complete endpoint requires Idempotency-Key and If-Match, persists completed part ETags and transitions only to VERIFYING; worker reads object, MIME, size and SHA-256 before VERIFIED or FAILED.
- Files: Shared upload API/controller/service/repository/domain/storage/config/.http/Postman paths in working tree; no migration retained.
- Acceptance criteria: Contract surface/static boundary implemented; rowversion, transaction, idempotency replay and worker durable effects need SQL/MinIO evidence.
- Verification: API build PASS (0 warnings, 0 errors); `git diff --check` PASS; Postman static check PASS.
- Reused/invalidated evidence: Existing file storage checks do not prove multipart completion.
- Side effects: AWSSDK.S3 added; no MinIO endpoint or secret committed.
- Unverified/blockers: Resolve migration baseline drift, then create approved migration and run SQL Server plus MinIO smoke.

### 2026-09-29 07:00 +07:00 - PARTIAL

- Review fix: stale, valid `If-Match` now maps to the approved precondition result instead of a generic conflict; idempotency receipts for create/part URLs/complete use distinct primary keys.
- Verification: SQL Testcontainers migration/workflow tests PASS (2); API-host SQL test PASS (1), including VERIFYING -> VERIFIED with a test storage boundary.

### 2026-09-29 14:34 +07:00 - DONE

- Provider smoke: `UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio` PASS (1) with actual signed PUT, returned MinIO ETag and SQL Server Testcontainers.
- Durable flow: complete returned `202 VERIFYING`; the verification service completed the multipart object, recalculated MIME/size/SHA-256 and persisted `VERIFIED`.
- Concurrency/idempotency evidence remains covered by the focused SQL workflow tests PASS (2) and API test PASS (1).
- Remaining blocker: real MinIO multipart completion and provider worker smoke is BLOCKED_ENV because no usable local image or configured endpoint is available.
