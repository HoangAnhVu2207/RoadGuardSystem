# V2-P1-046 — Giao kiểm tra/đo-và-sửa

## V2(3) status

- decisionRefs: none
- requirementRefs: register crosswalk; task-specific references remain authoritative
- contractStatus: PROPOSED_CONTRACT
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- workstream: BE
- blockers: Confirm current source and preserve compatibility before implementation.


- **Owner:** Person 1 — anh. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/projects/{projectId}/inspection-tasks`; operationId `createInspectionTask`.
- **Trạng thái kế hoạch:** `NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.1.1-draft-review1. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-17, FR-18, FR-20; nhóm kế hoạch cũ P1-40/P2-40 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W4; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## 1. Cần làm và tại sao

Giao kiểm tra/đo-và-sửa. Giao kiểm tra/đo-và-sửa

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-17, FR-18, FR-20; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P1-044 — getPolicyVersion](../Person_1/V2-P1-044_getPolicyVersion.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `PM`. Policy: `inspection.assign`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `projectId` | path | True | string (uuid) |
| `Idempotency-Key` | header | True | string; minLength=1 |

**Request body:** `InspectionTaskCreate`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `defectIds` | True | array; minItems=1 |
| `mode` | True | string; enum=INSPECT_ONLY,MEASURE_ONLY,INSPECT_AND_REPAIR |
| `crewId` | True | string (uuid) |
| `policyVersionId` | False | string (uuid) |
| `dueAt` | False | string (date-time) |
| `destination` | False | Position |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 201 | application/json: InspectionTask | ETag, Location |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `413`, `415`, `422`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `createInspectionTask` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: FieldInspectionTask; inspection session/evaluation cần đối chiếu.
2. Kiểm assignment hiện hành và snapshot; measurement/evidence là dữ liệu bất biến có ID client. Tách accept/decline/submit/evaluate thành đúng operation; transaction ghi dữ liệu và outbox.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.



**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs`
- `RoadGuardSystem.Repositories/Configurations/FieldInspectionTaskConfiguration.cs`
- `RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs`
- `RoadGuardSystem.Services/Interfaces/Authorization/ProjectScopeGuardContracts.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi createInspectionTask với payload hợp lệ | HTTP 201, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Crew chưa nhận/không được giao không nộp; file chưa verified không là evidence hợp lệ; retry cùng session không nhân phép đo; đo offline không tự mất vì token hết hạn. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P1-046: createInspectionTask
POST {{apiBase}}/projects/{{projectId}}/inspection-tasks
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
Content-Type: application/json

{
  "defectIds": [
    "11111111-1111-4111-8111-111111111111"
  ],
  "mode": "INSPECT_ONLY",
  "crewId": "11111111-1111-4111-8111-111111111111"
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~createInspectionTask" -v q
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
  operationId: createInspectionTask
  summary: Giao kiểm tra/đo-và-sửa
  tags:
  - inspection
  x-roles:
  - PM
  x-permission-policy: inspection.assign
  x-fr:
  - FR-17
  - FR-18
  - FR-20
  x-readiness: PROPOSED_CONTRACT
  description: Giao kiểm tra/đo-và-sửa
  responses:
    '201':
      description: Đã tạo bền vững.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/InspectionTask'
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
  - name: projectId
    in: path
    required: true
    schema: &id001
      type: string
      format: uuid
  - $ref: '#/components/parameters/IdempotencyKey'
  requestBody:
    required: true
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/InspectionTaskCreate'
schemas:
  InspectionTask:
    type: object
    additionalProperties: false
    properties:
      id: *id001
      projectId: *id001
      defectIds:
        type: array
        items: *id001
      mode:
        type: string
        enum:
        - INSPECT_ONLY
        - MEASURE_ONLY
        - INSPECT_AND_REPAIR
      crewId: *id001
      policyVersionId:
        anyOf:
        - *id001
        - type: 'null'
      status:
        type: string
        minLength: 1
      version:
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
    required:
    - id
    - projectId
    - defectIds
    - mode
    - crewId
    - policyVersionId
    - status
    - version
  InspectionTaskCreate:
    type: object
    additionalProperties: false
    properties:
      defectIds:
        type: array
        items: *id001
        minItems: 1
        uniqueItems: true
      mode:
        type: string
        enum:
        - INSPECT_ONLY
        - MEASURE_ONLY
        - INSPECT_AND_REPAIR
      crewId: *id001
      policyVersionId: *id001
      dueAt: &id002
        type: string
        format: date-time
      destination:
        $ref: '#/components/schemas/Position'
    required:
    - defectIds
    - mode
    - crewId
    description: INSPECT_AND_REPAIR phải có policyVersionId, một lỗi nhỏ được giao;
      server không suy từ số lỗi. Batch nhiều lỗi qua endpoint batch luôn MEASURE_ONLY.
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
        - *id002
        - type: 'null'
    required:
    - point
    - source
```
