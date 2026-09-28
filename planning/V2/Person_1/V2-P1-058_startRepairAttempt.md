# V2-P1-058 — Bắt đầu lần sửa đủ quyền

## V2(3) status

- deliveryStatus: TODO
- decisionRefs: D08, D09, D11, 35A
- requirementRefs: FR-18, FR-21
- diagramRefs: PF-05, PF-06, SQ-01, SQ-02, DD/ERD
- sourceCheckpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- workstream: BE
- blockers: BEFORE durability and curing/traffic states


- **Owner:** Person 1 — anh. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/repair-attempts`; operationId `startRepairAttempt`.
- **Trạng thái kế hoạch:** `BLOCKED_SLICE / NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-18, FR-21; nhóm kế hoạch cũ P1-53/P2-53 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W4; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D08, D09, D11, 35A)
- Requirements/trace: FR-18, FR-21
- Diagrams/state: PF-05, PF-06, SQ-01, SQ-02, DD/ERD
- Canonical contract: operationId startRepairAttempt, path /repair-attempts, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: to be read during NEEDS_REPO_CHECK; this alignment does not claim runtime verification.
- Checkpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

## 1. Cần làm và tại sao

Bắt đầu lần sửa đủ quyền. Online dùng quyền hiện hành; offline đã thực hiện ghi qua sync, giữ snapshot; không biến API này thành gate online bắt buộc.

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-18, FR-21; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Q02/Q03 (Fast Track); Q04 (đổi đội offline).
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P1-067 — getRepairItem](../Person_1/V2-P1-067_getRepairItem.md), [V2-P1-052 — evaluateFastTrack](../Person_1/V2-P1-052_evaluateFastTrack.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `CREW`. Policy: `repair.assigned`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `Idempotency-Key` | header | True | string; minLength=1 |

**Request body:** `StartAttempt`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `originTaskId` | False | string (uuid) |
| `repairItemId` | False | string (uuid) |
| `evaluationId` | False | string (uuid) |
| `beforeFileIds` | True | array; minItems=1 |
| `capturedTaskVersion` | False | string; minLength=1 |
| `policyVersionId` | False | string (uuid) |
| `startedAt` | True | string (date-time) |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 201 | application/json: RepairAttempt | ETag, Location |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `413`, `415`, `422`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `startRepairAttempt` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: RepairPackage/Item/Attempt: cần đối chiếu implementation thực tế.
2. Tách APPROVAL_TRACK và FAST_TRACK; lưu từng attempt, BEFORE/AFTER, quyết định và lịch sử assignment. Fast Track đủ policy thì Crew sửa, PM review/đóng và thông báo Supervisor; không thêm bước Supervisor duyệt trước sửa.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.

APPROVAL_TRACK dùng quyết định/assignment được duyệt; FAST_TRACK dùng evaluation+policy task snapshot. Dependency evaluateFastTrack chỉ áp dụng nhánh Fast Track, không chặn riêng nhánh Approval đã đủ contract.

**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs`
- `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs`
- `RoadGuardSystem.Repositories/Configurations/FieldInspectionTaskConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/IdempotencyRecordConfiguration.cs`
- `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationResult.cs`
- `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs`
- `RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs`
- `RoadGuardSystem.Services/Interfaces/Authorization/ProjectScopeGuardContracts.cs`
- `tests/RoadGuardSystem.IntegrationTests/Persistence/P202TransactionAndIdempotencyTests.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi startRepairAttempt với payload hợp lệ | HTTP 201, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Approval chưa được duyệt không bắt đầu; Fast Track thất bại policy không bắt đầu; AFTER của attempt khác không được dùng nhầm; rework tạo attempt mới giữ lịch sử. |
| T09 | Biên nghiệp vụ của operation | APPROVAL_TRACK dùng quyết định/assignment được duyệt; FAST_TRACK dùng evaluation+policy task snapshot. Dependency evaluateFastTrack chỉ áp dụng nhánh Fast Track, không chặn riêng nhánh Approval đã đủ contract. |
| T10 | Chạy khi quyết định/gate chưa được duyệt | Không thực thi nhánh chưa chốt; test bị block phải ghi BLOCKED, không báo PASS hoặc giả success |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P1-058: startRepairAttempt
POST {{apiBase}}/repair-attempts
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
Content-Type: application/json

"{{seed_value}}"
```

## 7. Done và bằng chứng

- Worklog ghi commit, route thực tế, reuse/new/delta, quyết định liên quan và đường dẫn `.http` có response đã che secret.
- Build project chịu ảnh hưởng; chọn focused/affected/full theo risk và skill repo. Một API phải có smoke trên server thật với SQL test và kiểm effect. Không bắt chạy full suite cho từng task; kết quả lịch sử không phải kết quả chạy hiện tại.
- Chọn test regression cho invariant ở mục 5; lỗi cạnh tranh/dedup cần DB thật. Mock không chứng minh transaction/concurrency. Ghi rõ test chưa chạy và lý do.
- FE đối chiếu schema và error code; cập nhật YAML chính, baseline, types và hash guard cùng change nếu contract thực sự được duyệt sửa. Gate còn mở chỉ cho phép PARTIAL/BLOCKED.

Lệnh tham khảo từ cấu trúc đã gửi (xác minh SDK/global.json trước dùng):

```cmd
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~startRepairAttempt" -v q
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
  operationId: startRepairAttempt
  summary: Bắt đầu lần sửa đủ quyền
  tags:
  - repair
  x-roles:
  - CREW
  x-permission-policy: repair.assigned
  x-fr:
  - FR-18
  - FR-21
  x-readiness: PROPOSED_CONTRACT
  description: Online dùng quyền hiện hành; offline đã thực hiện ghi qua sync, giữ
    snapshot; không biến API này thành gate online bắt buộc.
  responses:
    '201':
      description: Đã tạo bền vững.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/RepairAttempt'
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
          $ref: '#/components/schemas/StartAttempt'
schemas:
  RepairAttempt:
    type: object
    additionalProperties: false
    properties:
      id: &id001
        type: string
        format: uuid
      repairItemId: *id001
      status:
        type: string
        enum:
        - IN_PROGRESS
        - SUBMITTED
        - NEEDS_EVIDENCE
        - REWORK_REQUIRED
        - PM_CHECKED
        - ACCEPTED
      beforeFileIds:
        type: array
        items: *id001
      afterFileIds:
        type: array
        items: *id001
      version: &id002
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
    required:
    - id
    - repairItemId
    - status
    - beforeFileIds
    - afterFileIds
    - version
  StartAttempt:
    type: object
    additionalProperties: false
    properties:
      originTaskId: *id001
      repairItemId: *id001
      evaluationId: *id001
      beforeFileIds:
        type: array
        items: *id001
        minItems: 1
      capturedTaskVersion: *id002
      policyVersionId: *id001
      startedAt:
        type: string
        format: date-time
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
```
