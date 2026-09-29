# V2-P1-010 — Mời nhân sự

## V2(3) status

- deliveryStatus: PARTIAL
- decisionRefs: D25, 36A, 37
- requirementRefs: FR-02, FR-01
- diagramRefs: SQ-06, DD/ERD
- sourceCheckpoint: V2-P1-006-015-CODE-CHECKPOINT / base 561dd0a / canonical dd991f20c9a27a564bf37c06bba63776424b770f3abfa54ec2bef9d1a1e678bd

- contractStatus: PROPOSED_CONTRACT
- implementationStatus: IMPLEMENTED
- verificationStatus: PARTIAL_FOCUSED_AUTH
- dependencyType: contract
- workstream: BE
- blockers: Focused fake-sender/API and SQL fixture checks pass; real-email delivery and external smoke remain NOT_RUN.


- **Owner:** Person 1 — anh. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/invitations`; operationId `createInvitation`.
- **Trạng thái kế hoạch:** `PARTIAL / IMPLEMENTED`. Code complete; awaiting review/verification.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-02; nhóm kế hoạch cũ P1-64/P2-64 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W2; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D25, 36A, 37)
- Requirements/trace: FR-02, FR-01
- Diagrams/state: SQ-06, DD/ERD
- Canonical contract: operationId createInvitation, path /invitations, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: `InvitationsController.Create`, `IdentityOnboardingService.CreateInvitationAsync`, `IdentityOnboardingRepository.CreateInvitationAsync`; reviewed at base `561dd0a`, runtime verification NOT_RUN.
- Checkpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

## 1. Cần làm và tại sao

Mời nhân sự. Mời nhân sự

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-02; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** Không có dependency API cứng được chỉ định; seed trực tiếp fixture hợp lệ để test độc lập..
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `SUPERVISOR`. Policy: `admin.invite`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `Idempotency-Key` | header | True | string; minLength=1 |

**Request body:** `InvitationRequest`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `email` | True | string (email) |
| `role` | True | string; enum=PM,OPERATOR,CREW,SUPERVISOR |
| `projectIds` | True | array |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 201 | application/json: Invitation | ETag, Location |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `413`, `415`, `422`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `createInvitation` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: Identity, ProjectMember, catalog/configuration tùy resource.
2. Áp dụng policy quản trị của operation, kiểm tra ảnh hưởng tới membership/phiên trước mutation; ghi actor, lý do và giá trị trước/sau. Không cho client tự tăng quyền.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.



**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.BusinessObjects/Catalogs/CauseCategory.cs`
- `RoadGuardSystem.BusinessObjects/Catalogs/DefectType.cs`
- `RoadGuardSystem.BusinessObjects/Catalogs/SeverityRuleVersion.cs`
- `RoadGuardSystem.BusinessObjects/Identity/AccountStatusChangeLog.cs`
- `RoadGuardSystem.BusinessObjects/Identity/ApplicationRole.cs`
- `RoadGuardSystem.BusinessObjects/Identity/ApplicationUser.cs`
- `RoadGuardSystem.BusinessObjects/Identity/PasswordResetLog.cs`
- `RoadGuardSystem.BusinessObjects/Identity/RefreshToken.cs`
- `RoadGuardSystem.BusinessObjects/Identity/UserSession.cs`
- `RoadGuardSystem.BusinessObjects/Messaging/Notification.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi createInvitation với payload hợp lệ | HTTP 201, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Người có role đúng nhưng ngoài scope vẫn bị chặn; mutation lặp không tạo audit nghiệp vụ trùng; không làm mất lịch sử đối tượng đã được tham chiếu. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P1-010: createInvitation
POST {{apiBase}}/invitations
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
Content-Type: application/json

{
  "email": "tester@example.com",
  "role": "PM",
  "projectIds": [
    "11111111-1111-4111-8111-111111111111"
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~createInvitation" -v q
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
  operationId: createInvitation
  summary: Mời nhân sự
  tags:
  - admin
  x-roles:
  - SUPERVISOR
  x-permission-policy: admin.invite
  x-fr:
  - FR-02
  x-readiness: PROPOSED_CONTRACT
  description: Mời nhân sự
  responses:
    '201':
      description: Đã tạo bền vững.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Invitation'
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
          $ref: '#/components/schemas/InvitationRequest'
schemas:
  Invitation:
    type: object
    additionalProperties: false
    properties:
      id: &id001
        type: string
        format: uuid
      status:
        type: string
        enum:
        - PENDING
        - ACCEPTED
        - EXPIRED
        - REVOKED
      expiresAt:
        type: string
        format: date-time
      version:
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
    required:
    - id
    - status
    - expiresAt
    - version
  InvitationRequest:
    type: object
    additionalProperties: false
    properties:
      email:
        type: string
        format: email
      role:
        type: string
        enum:
        - PM
        - OPERATOR
        - CREW
        - SUPERVISOR
      projectIds:
        type: array
        items: *id001
        uniqueItems: true
    required:
    - email
    - role
    - projectIds
```

## Completion history

### 2026-09-29 09:42 +07:00 - PARTIAL

- Scope/result: source comparison completed; removed undocumented create-time `displayName` so request matches `InvitationRequest` (email/role/projectIds); reused repository persistence for Supervisor authorization, scope, token hash, expiry, delivery and idempotency.
- Files/symbols: `CreateInvitationRequestDto`, `InvitationsController.Create`, `IIdentityOnboardingService.CreateInvitationAsync`, `IdentityOnboardingService.CreateInvitationAsync`, API HTTP/Postman and focused test source; repository/entities/mappings/DI reused; evidence in `V2-P1-006-015-CODE-CHECKPOINT.md`.
- Acceptance criteria: 201/Location/ETag, idempotent concurrent create handling and no secret token response are code complete; runtime behavior not yet verified.
- Verification: shared API build PASS (0 errors, 40 warnings); tests/Newman/smoke/review-autofix NOT_RUN by owner instruction.
- Side effects: no package, migration, schema, data, external call, commit or push; `UNCOMMITTED`.
- Unverified/blockers: Code complete — awaiting review/verification.

### 2026-09-29 11:22 +07:00 - PARTIAL

- Scope/result: added guarded manual invitation delivery variables and fixture project IDs without seeding invitations or recipient accounts.
- Files/symbols: `PostmanScenarioSeedStep`, seeding DI, Postman invitation request/environment/README and local SMTP template.
- Acceptance criteria: focused API test proves Supervisor creation and token non-disclosure through the fake sender; seed remains independent of invitation state.
- Verification: API build PASS (0 errors); focused SQL seed tests PASS 2/2; `V2IdentityOnboardingFlowTests` PASS 3/3; Postman JSON/YAML and safety guards PASS. Real SMTP/mailbox delivery NOT_RUN.
- Reused/invalidated evidence: prior build-only evidence replaced by current focused tests; external smoke remains missing.
- Side effects: no package/migration/schema/Development DB/SMTP/commit/push; `UNCOMMITTED`.
- Unverified/blockers: real invitation delivery and external endpoint smoke remain required before DONE.
