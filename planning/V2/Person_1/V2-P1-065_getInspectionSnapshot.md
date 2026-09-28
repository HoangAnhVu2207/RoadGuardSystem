# V2-P1-065 — Snapshot task/policy/bằng chứng

## V2(3) status

- deliveryStatus: TODO
- decisionRefs: D02, D07, 32A
- requirementRefs: FR-15, FR-18, FR-22
- diagramRefs: PF-03, PF-04, PF-05, SQ-01, DD/ERD
- sourceCheckpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

- contractStatus: PROPOSED_CONTRACT
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- workstream: BE
- blockers: Confirm current source and preserve compatibility before implementation.


- **Owner:** Person 1 — anh. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `GET /api/v1/inspection-tasks/{taskId}/snapshot`; operationId `getInspectionSnapshot`.
- **Trạng thái kế hoạch:** `BLOCKED_SLICE / NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-15, FR-18, FR-22; nhóm kế hoạch cũ P1-40/P2-40 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W4; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D02, D07, 32A)
- Requirements/trace: FR-15, FR-18, FR-22
- Diagrams/state: PF-03, PF-04, PF-05, SQ-01, DD/ERD
- Canonical contract: operationId getInspectionSnapshot, path /inspection-tasks/{taskId}/snapshot, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: to be read during NEEDS_REPO_CHECK; this alignment does not claim runtime verification.
- Checkpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

## 1. Cần làm và tại sao

Snapshot task/policy/bằng chứng. Snapshot task/policy/bằng chứng

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-15, FR-18, FR-22; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** FE-GAP-07.
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P1-048 — getInspectionTask](../Person_1/V2-P1-048_getInspectionTask.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `CREW`. Policy: `inspection.assigned`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `taskId` | path | True | string (uuid) |

**Request body:** không khai báo trong OpenAPI; không tự thêm DTO body bắt buộc.

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 200 | application/json: TaskSnapshot |  |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `getInspectionSnapshot` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: FieldInspectionTask; inspection session/evaluation cần đối chiếu.
2. Kiểm assignment hiện hành và snapshot; measurement/evidence là dữ liệu bất biến có ID client. Tách accept/decline/submit/evaluate thành đúng operation; transaction ghi dữ liệu và outbox.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.

FE-GAP-07: snapshot/policy/evidence pack phải đủ dùng offline và có version/assignment identity. Không phát blob tùy ý hoặc claim Android offline-ready khi thiếu pack contract.

**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs`
- `RoadGuardSystem.Repositories/Configurations/FieldInspectionTaskConfiguration.cs`
- `RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs`
- `RoadGuardSystem.Services/Interfaces/Authorization/ProjectScopeGuardContracts.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi getInspectionSnapshot với payload hợp lệ | HTTP 200, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T06 | Seed rỗng, dữ liệu người khác, resource bị thu quyền giữa hai lần đọc | Kết quả rỗng/404/403 đúng scope; không trả dữ liệu cũ vượt quyền; không phát sinh business write |
| T08 | Rủi ro nghiệp vụ riêng | Crew chưa nhận/không được giao không nộp; file chưa verified không là evidence hợp lệ; retry cùng session không nhân phép đo; đo offline không tự mất vì token hết hạn. |
| T09 | Biên nghiệp vụ của operation | FE-GAP-07: snapshot/policy/evidence pack phải đủ dùng offline và có version/assignment identity. Không phát blob tùy ý hoặc claim Android offline-ready khi thiếu pack contract. |
| T10 | Chạy khi quyết định/gate chưa được duyệt | Không thực thi nhánh chưa chốt; test bị block phải ghi BLOCKED, không báo PASS hoặc giả success |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P1-065: getInspectionSnapshot
GET {{apiBase}}/inspection-tasks/{{taskId}}/snapshot
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~getInspectionSnapshot" -v q
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
  operationId: getInspectionSnapshot
  summary: Snapshot task/policy/bằng chứng
  tags:
  - inspection
  x-roles:
  - CREW
  x-permission-policy: inspection.assigned
  x-fr:
  - FR-15
  - FR-18
  - FR-22
  x-readiness: PROPOSED_CONTRACT
  description: Snapshot task/policy/bằng chứng
  responses:
    '200':
      description: Thành công; trạng thái nghiệp vụ ở payload.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/TaskSnapshot'
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
  - name: taskId
    in: path
    required: true
    schema: &id001
      type: string
      format: uuid
schemas:
  EvidenceReference:
    type: object
    additionalProperties: false
    properties:
      fileId: *id001
      purpose: &id002
        type: string
        minLength: 1
      source: *id002
      capturedAt:
        anyOf:
        - &id004
          type: string
          format: date-time
        - type: 'null'
      checksumSha256: *id002
    required:
    - fileId
    - purpose
    - source
    - capturedAt
    - checksumSha256
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
      status: *id002
      version: &id003
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
  PolicyRule:
    type: object
    additionalProperties: false
    properties:
      defectTypeCode: *id002
      measurementType: *id002
      unit: *id002
      operator:
        type: string
        enum:
        - LT
        - LTE
        - GT
        - GTE
        - EQ
      threshold:
        type: number
      repairMethodCode: *id002
      requiredEvidenceTypes:
        type: array
        items:
          type: string
          enum:
          - BEFORE
          - AFTER
          - MEASUREMENT
        minItems: 1
      exclusions:
        type: array
        items: *id002
    required:
    - defectTypeCode
    - measurementType
    - unit
    - operator
    - threshold
    - repairMethodCode
    - requiredEvidenceTypes
    - exclusions
  PolicyVersion:
    type: object
    additionalProperties: false
    properties:
      id: *id001
      projectId: *id001
      name: *id002
      rules:
        type: array
        items:
          $ref: '#/components/schemas/PolicyRule'
      preparationList:
        type: array
        items: *id002
      status:
        type: string
        enum:
        - DRAFT
        - AVAILABLE
        - SUPERSEDED
      version: *id003
    required:
    - id
    - projectId
    - name
    - rules
    - preparationList
    - status
    - version
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
        - *id004
        - type: 'null'
    required:
    - point
    - source
  TaskSnapshot:
    type: object
    additionalProperties: false
    properties:
      snapshotId: *id001
      capturedAt: *id004
      task:
        $ref: '#/components/schemas/InspectionTask'
      policy:
        anyOf:
        - $ref: '#/components/schemas/PolicyVersion'
        - type: 'null'
      assignmentVersion: *id003
      pmFastTrackBlocked:
        type: boolean
      evidence:
        type: array
        items:
          $ref: '#/components/schemas/EvidenceReference'
      destination:
        anyOf:
        - $ref: '#/components/schemas/Position'
        - type: 'null'
    required:
    - snapshotId
    - capturedAt
    - task
    - policy
    - assignmentVersion
    - pmFastTrackBlocked
    - evidence
    - destination
    description: Không có offline_expires_at; auth token expiry là riêng. Snapshot
      metadata không chứng minh file bytes đã tải bền vững. Kiểm download/checksum
      local trước ready.
```
