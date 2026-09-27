# V2-P2-029 — Đồng bộ từng operation có snapshot

- **Owner:** Person 2 — huy. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/sync/batches`; operationId `syncOperations`.
- **Trạng thái kế hoạch:** `BLOCKED_SLICE / NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.1.1-draft-review1. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-22; nhóm kế hoạch cũ P1-40/P2-40 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W4; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## 1. Cần làm và tại sao

Đồng bộ từng operation có snapshot. Envelope hợp lệ trả 200 với từng APPLIED/DUPLICATE/CONFLICT/REJECTED; invalid envelope 422; không ACK toàn batch chỉ vì nhận HTTP. Current revocation không cho ghi quyền cũ; giữ local và conflict; Q04/17 xử lý có kiểm soát.

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-22; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Q04; Q17; FE-GAP-05/07/08/10.
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P1-051 — submitInspection](../Person_1/V2-P1-051_submitInspection.md), [V2-P1-058 — startRepairAttempt](../Person_1/V2-P1-058_startRepairAttempt.md), [V2-P1-059 — submitRepairAttempt](../Person_1/V2-P1-059_submitRepairAttempt.md), [V2-P1-065 — getInspectionSnapshot](../Person_1/V2-P1-065_getInspectionSnapshot.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `CREW, OPERATOR`. Policy: `sync.actorScope`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `Idempotency-Key` | header | True | string; minLength=1 |

**Request body:** `SyncBatch`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `deviceId` | True | string (uuid) |
| `operations` | True | array; minItems=1; maxItems=100 |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 200 | application/json: SyncResult |  |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `413`, `415`, `422`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `syncOperations` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: IdempotencyRecord, session/assignment snapshot, offline operation receipt.
2. Xử lý kết quả theo từng operation; clientOperationId dùng chống lặp bền vững độc lập batch. ACK chỉ sau commit. Tái kiểm tra quyền hiện tại và snapshot, không last-write-wins tự động.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.

Hiện chỉ 3 kind: INSPECTION_SUBMIT, REPAIR_START, REPAIR_SUBMIT. FAST_TRACK_EVALUATE ở amendment proposal là NOT_ENABLED; không thêm enum vào runtime trước chốt schema/ordering/dependency. Q04/Q17 chặn E2E conflict/rescue, không chặn unit test durability/dedup. Test restart app, mất ACK, duplicate operation qua hai batch, partial success, attachment pending, lệch giờ thiết bị; không dùng thời gian client làm authority.

**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs`
- `RoadGuardSystem.BusinessObjects/Identity/UserSession.cs`
- `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs`
- `RoadGuardSystem.Repositories/Configurations/FieldInspectionTaskConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/IdempotencyRecordConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/UserSessionConfiguration.cs`
- `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationResult.cs`
- `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs`
- `RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.SessionIssuance.cs`
- `RoadGuardSystem.Repositories/Options/SessionDeviceMetadataOptions.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi syncOperations với payload hợp lệ | HTTP 200, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | HTTP 200 có thể chứa CONFLICT/REJECTED; đổi batchId không được replay effect; crash sau commit trước ACK phải trả duplicate; operation lỗi không làm FE xóa queue. |
| T09 | Biên nghiệp vụ của operation | Hiện chỉ 3 kind: INSPECTION_SUBMIT, REPAIR_START, REPAIR_SUBMIT. FAST_TRACK_EVALUATE ở amendment proposal là NOT_ENABLED; không thêm enum vào runtime trước chốt schema/ordering/dependency. Q04/Q17 chặn E2E conflict/rescue, không chặn unit test durability/dedup. Test restart app, mất ACK, duplicate operation qua hai batch, partial success, attachment pending, lệch giờ thiết bị; không dùng thời gian client làm authority. |
| T10 | Chạy khi quyết định/gate chưa được duyệt | Không thực thi nhánh chưa chốt; test bị block phải ghi BLOCKED, không báo PASS hoặc giả success |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P2-029: syncOperations
POST {{apiBase}}/sync/batches
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
Content-Type: application/json

{
  "deviceId": "11111111-1111-4111-8111-111111111111",
  "operations": [
    {
      "operationId": "11111111-1111-4111-8111-111111111111",
      "kind": "INSPECTION_SUBMIT",
      "taskId": "11111111-1111-4111-8111-111111111111",
      "expectedVersion": "{{seed_value}}",
      "capturedAt": "2026-09-27T08:00:00Z",
      "payload": {
        "taskVersion": "{{seed_value}}",
        "measurements": [
          null
        ],
        "observedAt": "2026-09-27T08:00:00Z"
      }
    }
  ]
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~syncOperations" -v q
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
- [12_Decisions_Contract_Gaps.md](../../../docs/diagram/V2/09_Frontend/12_Decisions_Contract_Gaps.md)
- [03_Offline_Policy_Decision_Workshop.md](../../../docs/diagram/V2/07_Change_Management/03_Offline_Policy_Decision_Workshop.md)
- [Quy tắc chung và thứ tự tích hợp](../README.md)
- [OpenAPI chính](../../../docs/diagram/V2/05_Technical/openapi.yaml)

Snapshot schema liên quan giúp người nhận task xem cả nested fields. YAML chính vẫn là nguồn contract; snapshot này phải được tạo lại nếu YAML đổi.

```yaml
operation:
  operationId: syncOperations
  summary: Đồng bộ từng operation có snapshot
  tags:
  - sync
  x-roles:
  - CREW
  - OPERATOR
  x-permission-policy: sync.actorScope
  x-fr:
  - FR-22
  x-readiness: PROPOSED_CONTRACT
  description: Envelope hợp lệ trả 200 với từng APPLIED/DUPLICATE/CONFLICT/REJECTED;
    invalid envelope 422; không ACK toàn batch chỉ vì nhận HTTP. Current revocation
    không cho ghi quyền cũ; giữ local và conflict; Q04/17 xử lý có kiểm soát.
  responses:
    '200':
      description: Thành công; trạng thái nghiệp vụ ở payload.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/SyncResult'
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
          $ref: '#/components/schemas/SyncBatch'
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
  InspectionSubmit:
    type: object
    additionalProperties: false
    properties:
      taskVersion: &id004
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
      policyVersionId: &id002
        type: string
        format: uuid
      measurements:
        type: array
        items:
          $ref: '#/components/schemas/Measurement'
        minItems: 1
      observedAt: &id003
        type: string
        format: date-time
    required:
    - taskVersion
    - measurements
    - observedAt
  Measurement:
    type: object
    additionalProperties: false
    properties:
      defectId: *id002
      measurementType: *id001
      value:
        type: number
      unit: *id001
      instrument: *id001
      measuredAt: *id003
      position:
        $ref: '#/components/schemas/Position'
      evidenceFileIds:
        type: array
        items: *id002
        minItems: 1
    required:
    - defectId
    - measurementType
    - value
    - unit
    - measuredAt
    - evidenceFileIds
  Point:
    type: object
    additionalProperties: false
    properties:
      longitude:
        type: number
        minimum: -180
        maximum: 180
      latitude:
        type: number
        minimum: -90
        maximum: 90
    required:
    - longitude
    - latitude
    description: WGS84; tên thuộc tính tránh đảo lat/lon; GeoJSON position array bên
      dưới là lon,lat.
  Position:
    type: object
    additionalProperties: false
    properties:
      point:
        $ref: '#/components/schemas/Point'
      source:
        type: string
        enum:
        - EXIF
        - MANUAL
        - SURVEY
        - DERIVED
      accuracyMeters:
        anyOf:
        - type: number
          minimum: 0
        - type: 'null'
      capturedAt:
        anyOf:
        - *id003
        - type: 'null'
    required:
    - point
    - source
  StartAttempt:
    type: object
    additionalProperties: false
    properties:
      originTaskId: *id002
      repairItemId: *id002
      evaluationId: *id002
      beforeFileIds:
        type: array
        items: *id002
        minItems: 1
      capturedTaskVersion: *id004
      policyVersionId: *id002
      startedAt: *id003
    required:
    - beforeFileIds
    - startedAt
    description: APPROVAL_TRACK có repairItemId đã duyệt/giao; FAST_TRACK có originTaskId,
      evaluationId, capturedTaskVersion, policyVersionId. Không chờ PM duyệt số đo
      của task FT đủ điều kiện.
    oneOf:
    - required:
      - repairItemId
      not:
        required:
        - originTaskId
    - required:
      - originTaskId
      - evaluationId
      - capturedTaskVersion
      - policyVersionId
      not:
        required:
        - repairItemId
  SubmitAttempt:
    type: object
    additionalProperties: false
    properties:
      afterFileIds:
        type: array
        items: *id002
        minItems: 1
      completedAt: *id003
      resultNotes: *id001
      measurementIds:
        type: array
        items: *id002
    required:
    - afterFileIds
    - completedAt
    - resultNotes
  SyncAttemptSubmit:
    type: object
    additionalProperties: false
    properties:
      operationId: *id002
      kind:
        type: string
        enum:
        - REPAIR_SUBMIT
      attemptId: *id002
      expectedVersion: *id004
      capturedAt: *id003
      payload:
        $ref: '#/components/schemas/SubmitAttempt'
    required:
    - operationId
    - kind
    - attemptId
    - expectedVersion
    - capturedAt
    - payload
  SyncBatch:
    type: object
    additionalProperties: false
    properties:
      deviceId: *id002
      operations:
        type: array
        items:
          $ref: '#/components/schemas/SyncOperation'
        minItems: 1
        maxItems: 100
    required:
    - deviceId
    - operations
    description: 100 là giới hạn đề xuất contract; xử lý theo thứ tự, transaction
      từng operation. Đồng bộ file đã VERIFIED trước. Thực hiện phase submit sau khi
      đã map local→server ID từ start response.
  SyncMeasurement:
    type: object
    additionalProperties: false
    properties:
      operationId: *id002
      kind:
        type: string
        enum:
        - INSPECTION_SUBMIT
      taskId: *id002
      expectedVersion: *id004
      capturedAt: *id003
      payload:
        $ref: '#/components/schemas/InspectionSubmit'
    required:
    - operationId
    - kind
    - taskId
    - expectedVersion
    - capturedAt
    - payload
  SyncOperation:
    oneOf:
    - $ref: '#/components/schemas/SyncMeasurement'
    - $ref: '#/components/schemas/SyncStart'
    - $ref: '#/components/schemas/SyncAttemptSubmit'
    discriminator:
      propertyName: kind
      mapping:
        INSPECTION_SUBMIT: '#/components/schemas/SyncMeasurement'
        REPAIR_START: '#/components/schemas/SyncStart'
        REPAIR_SUBMIT: '#/components/schemas/SyncAttemptSubmit'
  SyncOutcome:
    type: object
    additionalProperties: false
    properties:
      operationId: *id002
      status:
        type: string
        enum:
        - APPLIED
        - DUPLICATE
        - CONFLICT
        - REJECTED
      resourceId:
        anyOf:
        - *id002
        - type: 'null'
      version:
        anyOf:
        - *id004
        - type: 'null'
      error:
        anyOf:
        - $ref: '#/components/schemas/Error'
        - type: 'null'
    required:
    - operationId
    - status
    - resourceId
    - version
    - error
  SyncResult:
    type: object
    additionalProperties: false
    properties:
      results:
        type: array
        items:
          $ref: '#/components/schemas/SyncOutcome'
    required:
    - results
  SyncStart:
    type: object
    additionalProperties: false
    properties:
      operationId: *id002
      kind:
        type: string
        enum:
        - REPAIR_START
      taskId: *id002
      expectedVersion: *id004
      capturedAt: *id003
      payload:
        $ref: '#/components/schemas/StartAttempt'
    required:
    - operationId
    - kind
    - taskId
    - expectedVersion
    - capturedAt
    - payload
```
