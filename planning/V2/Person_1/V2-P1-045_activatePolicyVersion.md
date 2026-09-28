# V2-P1-045 — Cho policy sẵn dùng

## V2(3) status

- deliveryStatus: TODO
- decisionRefs: D03, D04
- requirementRefs: FR-15
- diagramRefs: PF-03, PF-04, PF-05, SQ-01, DD/ERD
- sourceCheckpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

- contractStatus: REVIEWED
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- workstream: BE
- blockers: activation stays fail-closed when technical basis is missing


- **Owner:** Person 1 — anh. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/policy-versions/{policyVersionId}/activate`; operationId `activatePolicyVersion`.
- **Trạng thái kế hoạch:** `BLOCKED_SLICE / NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** CONDITIONAL; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-15; nhóm kế hoạch cũ P1-HF-01 / P2-HF-01 (V2 policy bổ sung) (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W3; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D03, D04)
- Requirements/trace: FR-15
- Diagrams/state: PF-03, PF-04, PF-05, SQ-01, DD/ERD
- Canonical contract: operationId activatePolicyVersion, path /policy-versions/{policyVersionId}/activate, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: to be read during NEEDS_REPO_CHECK; this alignment does not claim runtime verification.
- Checkpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

## 1. Cần làm và tại sao

Cho policy sẵn dùng. Supervisor ban hành khung công ty; PM activate trong giới hạn khung và project profile. Fail closed POLICY_NOT_CONFIGURED khi thiếu hồ sơ/ngưỡng kỹ thuật; không thêm duyệt từng sửa Fast Track. D03-D04 đã chốt boundary, phần căn cứ method vẫn là gate.

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-15; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Q02; Q03.
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P1-044 — getPolicyVersion](../Person_1/V2-P1-044_getPolicyVersion.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `PM`. Policy: `policy.activate`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `policyVersionId` | path | True | string (uuid) |
| `Idempotency-Key` | header | True | string; minLength=1 |
| `If-Match` | header | True | string; minLength=3 |

**Request body:** không khai báo trong OpenAPI; không tự thêm DTO body bắt buộc.

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 200 | application/json: PolicyVersion | ETag |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `412`, `413`, `415`, `422`, `428`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `activatePolicyVersion` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: PolicyVersion và snapshot: cần xác minh implementation.
2. Policy đã dùng phải bất biến; task tham chiếu version cụ thể, không tự chuyển sang policy mới nhất. Quyền activate và rule evaluation kiểm khung Supervisor, giới hạn project và hồ sơ method; thiếu dữ liệu trả POLICY_NOT_CONFIGURED.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.



**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs`
- `RoadGuardSystem.Repositories/Configurations/IdempotencyRecordConfiguration.cs`
- `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationResult.cs`
- `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs`
- `RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs`
- `RoadGuardSystem.Services/Interfaces/Authorization/ProjectScopeGuardContracts.cs`
- `tests/RoadGuardSystem.IntegrationTests/Persistence/P202TransactionAndIdempotencyTests.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi activatePolicyVersion với payload hợp lệ | HTTP 200, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T04 | Thiếu If-Match; version cũ; hai mutation cùng version chạy đồng thời | 428; 412; tối đa một cập nhật theo version thành công, history không mất |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Task cũ giữ policy cũ sau publish; policy chưa cấu hình không cho Fast Track; caller giả role hoặc version khác project bị chặn. |
| T10 | Chạy khi quyết định/gate chưa được duyệt | Không thực thi nhánh chưa chốt; test bị block phải ghi BLOCKED, không báo PASS hoặc giả success |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P1-045: activatePolicyVersion
POST {{apiBase}}/policy-versions/{{policyVersionId}}/activate
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
If-Match: "{{version}}"
```

## 7. Done và bằng chứng

- Worklog ghi commit, route thực tế, reuse/new/delta, quyết định liên quan và đường dẫn `.http` có response đã che secret.
- Build project chịu ảnh hưởng; chọn focused/affected/full theo risk và skill repo. Một API phải có smoke trên server thật với SQL test và kiểm effect. Không bắt chạy full suite cho từng task; kết quả lịch sử không phải kết quả chạy hiện tại.
- Chọn test regression cho invariant ở mục 5; lỗi cạnh tranh/dedup cần DB thật. Mock không chứng minh transaction/concurrency. Ghi rõ test chưa chạy và lý do.
- FE đối chiếu schema và error code; cập nhật YAML chính, baseline, types và hash guard cùng change nếu contract thực sự được duyệt sửa. Gate còn mở chỉ cho phép PARTIAL/BLOCKED.

Lệnh tham khảo từ cấu trúc đã gửi (xác minh SDK/global.json trước dùng):

```cmd
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~activatePolicyVersion" -v q
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
  operationId: activatePolicyVersion
  summary: Cho policy sẵn dùng
  tags:
  - policy
  x-roles:
  - PM
  x-permission-policy: policy.activate
  x-fr:
  - FR-15
  x-readiness: CONDITIONAL
  description: 'Vai trò PM activate là đề xuất; fail closed POLICY_NOT_CONFIGURED
    trước khi Q02/Q03 chốt; không tự thêm Supervisor gate. Điều kiện chưa chốt: Q02/Q03.'
  responses:
    '200':
      description: Thành công; trạng thái nghiệp vụ ở payload.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/PolicyVersion'
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
  x-open-decisions:
  - Q02
  - Q03
  parameters:
  - name: policyVersionId
    in: path
    required: true
    schema: &id002
      type: string
      format: uuid
  - $ref: '#/components/parameters/IdempotencyKey'
  - $ref: '#/components/parameters/IfMatch'
schemas:
  PolicyRule:
    type: object
    additionalProperties: false
    properties:
      defectTypeCode: &id001
        type: string
        minLength: 1
      measurementType: *id001
      unit: *id001
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
      repairMethodCode: *id001
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
        items: *id001
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
      id: *id002
      projectId: *id002
      name: *id001
      rules:
        type: array
        items:
          $ref: '#/components/schemas/PolicyRule'
      preparationList:
        type: array
        items: *id001
      status:
        type: string
        enum:
        - DRAFT
        - AVAILABLE
        - SUPERSEDED
      version:
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
    required:
    - id
    - projectId
    - name
    - rules
    - preparationList
    - status
    - version
```
