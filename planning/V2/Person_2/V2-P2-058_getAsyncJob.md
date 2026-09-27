# V2-P2-058 — Poll job mọi loại theo scope

- **Owner:** Person 2 — huy. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `GET /api/v1/jobs/{jobId}`; operationId `getAsyncJob`.
- **Trạng thái kế hoạch:** `NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.1.1-draft-review1. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-29, FR-31, FR-33, FR-35, FR-36; nhóm kế hoạch cũ P1-31/P2-31 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W5; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## 1. Cần làm và tại sao

Poll job mọi loại theo scope. Kiểm owner/scope theo jobType, không cấp Operator quyền export/model/retention. resultId trỏ resource theo jobType; EXPORT/MISSION_EXPORT/TRAINING_EXPORT trỏ file; VALIDATION trỏ validation run; AI_ANALYSIS trỏ result nguồn.

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-29, FR-31, FR-33, FR-35, FR-36; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** Không có dependency API cứng được chỉ định; seed trực tiếp fixture hợp lệ để test độc lập..
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `SUPERVISOR, PM, OPERATOR`. Policy: `ai.readJob`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `jobId` | path | True | string (uuid) |

**Request body:** không khai báo trong OpenAPI; không tự thêm DTO body bắt buộc.

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 200 | application/json: Job | ETag |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `getAsyncJob` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: ProcessingJob, ProcessingAttempt, OutboxMessage, AIModelVersion.
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
| T01 | Seed đúng role/scope/trạng thái; gọi getAsyncJob với payload hợp lệ | HTTP 200, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T06 | Seed rỗng, dữ liệu người khác, resource bị thu quyền giữa hai lần đọc | Kết quả rỗng/404/403 đúng scope; không trả dữ liệu cũ vượt quyền; không phát sinh business write |
| T08 | Rủi ro nghiệp vụ riêng | Callback lặp không nhân detection; callback attempt cũ không hoàn tất attempt mới; enqueue thất bại không có job mất dấu; lỗi provider cho retry có kiểm soát. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P2-058: getAsyncJob
GET {{apiBase}}/jobs/{{jobId}}
Authorization: Bearer {{accessToken}}
```

## 7. Done và bằng chứng

- Worklog ghi commit, route thực tế, reuse/new/delta, quyết định liên quan và đường dẫn `.http` có response đã che secret.
- Build project chịu ảnh hưởng; chọn focused/affected/full theo risk và skill repo. Một API phải có smoke trên server thật với SQL test và kiểm effect. Không bắt chạy full suite cho từng task; kết quả lịch sử không phải kết quả chạy hiện tại.
- Chọn test regression cho invariant ở mục 5; lỗi cạnh tranh/dedup cần DB thật. Mock không chứng minh transaction/concurrency. Ghi rõ test chưa chạy và lý do.
- FE đối chiếu schema và error code; cập nhật YAML chính, baseline, types và hash guard cùng change nếu contract thực sự được duyệt sửa. Gate còn mở chỉ cho phép PARTIAL/BLOCKED.

Lệnh tham khảo từ cấu trúc đã gửi (xác minh SDK/global.json trước dùng):

```cmd
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~getAsyncJob" -v q
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
  operationId: getAsyncJob
  summary: Poll job mọi loại theo scope
  tags:
  - ai
  x-roles:
  - SUPERVISOR
  - PM
  - OPERATOR
  x-permission-policy: ai.readJob
  x-fr:
  - FR-29
  - FR-31
  - FR-33
  - FR-35
  - FR-36
  x-readiness: PROPOSED_CONTRACT
  description: Kiểm owner/scope theo jobType, không cấp Operator quyền export/model/retention.
    resultId trỏ resource theo jobType; EXPORT/MISSION_EXPORT/TRAINING_EXPORT trỏ
    file; VALIDATION trỏ validation run; AI_ANALYSIS trỏ result nguồn.
  responses:
    '200':
      description: Thành công; trạng thái nghiệp vụ ở payload.
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
    '400':
      $ref: '#/components/responses/Error400'
    '401':
      $ref: '#/components/responses/Error401'
    '403':
      $ref: '#/components/responses/Error403'
    '404':
      $ref: '#/components/responses/Error404'
    '429':
      $ref: '#/components/responses/Error429'
    '500':
      $ref: '#/components/responses/Error500'
    '503':
      $ref: '#/components/responses/Error503'
  parameters:
  - name: jobId
    in: path
    required: true
    schema: &id002
      type: string
      format: uuid
schemas:
  Error:
    type: object
    additionalProperties: false
    properties:
      code: &id001
        type: string
        minLength: 1
      message: *id001
      details:
        type: array
        items:
          $ref: '#/components/schemas/ErrorDetail'
      traceId: *id001
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
      code: *id001
      message: *id001
    required:
    - code
    - message
  Job:
    type: object
    additionalProperties: false
    properties:
      id: *id002
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
        - *id002
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
```
