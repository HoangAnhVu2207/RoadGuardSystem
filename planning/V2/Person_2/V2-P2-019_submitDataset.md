# V2-P2-019 — Nộp video/telemetry

## V2(3) status

- deliveryStatus: TODO
- decisionRefs: D14, D15, D16, D17, D20, D21, D22
- requirementRefs: FR-27
- diagramRefs: PF-07, SQ-06, DD/ERD
- sourceCheckpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- workstream: BE
- blockers: explicit video to telemetry mapping and segment history


- **Owner:** Person 2 — huy. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/survey-tasks/{taskId}/datasets`; operationId `submitDataset`.
- **Trạng thái kế hoạch:** `NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-27; nhóm kế hoạch cũ P1-23/P2-23 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W3; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D14, D15, D16, D17, D20, D21, D22)
- Requirements/trace: FR-27
- Diagrams/state: PF-07, SQ-06, DD/ERD
- Canonical contract: operationId submitDataset, path /survey-tasks/{taskId}/datasets, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: to be read during NEEDS_REPO_CHECK; this alignment does not claim runtime verification.
- Checkpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

## 1. Cần làm và tại sao

Nộp video/telemetry. Nộp video/telemetry

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-27; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P2-012 — getSurveyTask](../Person_2/V2-P2-012_getSurveyTask.md), [V2-P2-027 — getFileMetadata](../Person_2/V2-P2-027_getFileMetadata.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `OPERATOR`. Policy: `survey.assigned`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `taskId` | path | True | string (uuid) |
| `Idempotency-Key` | header | True | string; minLength=1 |
| `If-Match` | header | True | string; minLength=3 |

**Request body:** `DatasetSubmit`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `videoFileIds` | True | array; minItems=1 |
| `telemetryFileIds` | True | array |
| `recordedAt` | True | string (date-time) |
| `deviceId` | False | string (uuid) |
| `scope` | True | array; minItems=1 |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 201 | application/json: Dataset | ETag, Location |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `412`, `413`, `415`, `422`, `428`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `submitDataset` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: SurveyPlan, SurveyAssignment, SurveyTask mapping, SurveyDataVersion, QualityCheck.
2. Giữ scope/version khảo sát và lịch sử giao Operator. Upload hoàn tất không đồng nghĩa dataset verified; chỉ quality/coverage được xác nhận mới dùng baseline/processing.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.



**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.API/Controllers/SurveyPlanningController.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/Flight.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/QualityCheck.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SupplementarySurveyRequest.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/Survey.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SurveyAssignment.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SurveyDataVersion.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SurveyFile.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SurveyPlan.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SurveyPlanPostponement.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi submitDataset với payload hợp lệ | HTTP 201, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T04 | Thiếu If-Match; version cũ; hai mutation cùng version chạy đồng thời | 428; 412; tối đa một cập nhật theo version thành công, history không mất |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Operator khác không nhận/nộp; dataset chưa verified không mở downstream; hoãn plan không ngầm hủy task; reassign giữ lịch sử và không mất dataset cũ. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P2-019: submitDataset
POST {{apiBase}}/survey-tasks/{{taskId}}/datasets
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
If-Match: "{{version}}"
Content-Type: application/json

{
  "videoFileIds": [
    "11111111-1111-4111-8111-111111111111"
  ],
  "telemetryFileIds": [
    "11111111-1111-4111-8111-111111111111"
  ],
  "recordedAt": "2026-09-27T08:00:00Z",
  "scope": [
    {
      "routeVersionId": "11111111-1111-4111-8111-111111111111",
      "segmentSetId": "11111111-1111-4111-8111-111111111111",
      "segmentIds": [
        "11111111-1111-4111-8111-111111111111"
      ],
      "targetBand": "SURFACE"
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~submitDataset" -v q
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
  operationId: submitDataset
  summary: Nộp video/telemetry
  tags:
  - survey
  x-roles:
  - OPERATOR
  x-permission-policy: survey.assigned
  x-fr:
  - FR-27
  x-readiness: PROPOSED_CONTRACT
  description: Nộp video/telemetry
  responses:
    '201':
      description: Đã tạo bền vững.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Dataset'
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
  - name: taskId
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
          $ref: '#/components/schemas/DatasetSubmit'
schemas:
  BandScope:
    type: object
    additionalProperties: false
    properties:
      routeVersionId: *id001
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
  Dataset:
    type: object
    additionalProperties: false
    properties:
      id: *id001
      surveyTaskId: *id001
      dataVersionId: *id001
      integrityStatus:
        type: string
        enum:
        - VERIFIED
        - PENDING
        - INVALID
      telemetryStatus:
        type: string
        enum:
        - PRESENT
        - MISSING
        - UNUSABLE
      version:
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
    required:
    - id
    - surveyTaskId
    - dataVersionId
    - integrityStatus
    - telemetryStatus
    - version
  DatasetSubmit:
    type: object
    additionalProperties: false
    properties:
      videoFileIds:
        type: array
        items: *id001
        minItems: 1
      telemetryFileIds:
        type: array
        items: *id001
      recordedAt:
        type: string
        format: date-time
      deviceId: *id001
      scope:
        type: array
        items:
          $ref: '#/components/schemas/BandScope'
        minItems: 1
    required:
    - videoFileIds
    - telemetryFileIds
    - recordedAt
    - scope
```
