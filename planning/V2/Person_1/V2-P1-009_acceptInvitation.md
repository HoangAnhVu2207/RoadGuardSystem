# V2-P1-009 — Nhận lời mời

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
- blockers: Focused fake-sender/API and SQL fixture checks pass; real-email acceptance and external smoke remain NOT_RUN.


- **Owner:** Person 1 — anh. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `POST /api/v1/invitations/accept`; operationId `acceptInvitation`.
- **Trạng thái kế hoạch:** `PARTIAL / IMPLEMENTED`. Code complete; awaiting review/verification.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-02; nhóm kế hoạch cũ P1-10/P2-10 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W2; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D25, 36A, 37)
- Requirements/trace: FR-02, FR-01
- Diagrams/state: SQ-06, DD/ERD
- Canonical contract: operationId acceptInvitation, path /invitations/accept, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: `InvitationsController.Accept`, `IdentityOnboardingService.AcceptInvitationAsync`, `IdentityOnboardingRepository.AcceptInvitationAsync`; reviewed at base `561dd0a`, runtime verification NOT_RUN.
- Checkpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

## 1. Cần làm và tại sao

Nhận lời mời. Nhận lời mời

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-02; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P1-010 — createInvitation](../Person_1/V2-P1-010_createInvitation.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `PUBLIC`. Policy: `auth.invitation`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `Idempotency-Key` | header | True | string; minLength=1 |

**Request body:** `AcceptInvitation`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `invitationToken` | True | string; minLength=1 |
| `displayName` | True | string; minLength=1 |
| `password` | True | string; minLength=1 |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 200 | application/json: TokenPair |  |

Mã lỗi HTTP trong draft: `400`, `409`, `413`, `415`, `422`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `acceptInvitation` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: Identity, ApplicationUser, UserSession, RefreshToken; AuthService/IdentityService.
2. Xác thực và quản lý phiên là ranh giới quyền; dùng Identity và session validator hiện có. Không log mật khẩu/token, không lưu refresh token rõ. Tách giao dịch rotate/revoke khỏi việc trả response.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.



**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.API/Authentication/JwtBearerConfiguration.cs`
- `RoadGuardSystem.API/Authorization/ProjectAccessAuthorization.cs`
- `RoadGuardSystem.API/Authorization/ProjectAuthorizationMiddlewareResultHandler.cs`
- `RoadGuardSystem.API/Controllers/AuthController.cs`
- `RoadGuardSystem.API/Extensions/AuthorizeOperationFilter.cs`
- `RoadGuardSystem.BusinessObjects/Identity/AccountStatusChangeLog.cs`
- `RoadGuardSystem.BusinessObjects/Identity/ApplicationRole.cs`
- `RoadGuardSystem.BusinessObjects/Identity/ApplicationUser.cs`
- `RoadGuardSystem.BusinessObjects/Identity/PasswordResetLog.cs`
- `RoadGuardSystem.BusinessObjects/Identity/RefreshToken.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi acceptInvitation với payload hợp lệ | HTTP 200, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Gửi dữ liệu xác thực/OTP/invitation không hợp lệ; lặp request hoặc account không tồn tại | Public flow không yêu cầu bearer ngầm; không lộ danh tính qua response khác biệt; error đúng catalog |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Phiên revoked vẫn còn JWT chưa hết hạn phải bị từ chối; không lộ tài khoản qua lỗi public; hai request refresh đồng thời không sinh hai phiên kế tiếp hợp lệ. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P1-009: acceptInvitation
POST {{apiBase}}/invitations/accept
Idempotency-Key: {{IdempotencyKey}}
Content-Type: application/json

{
  "invitationToken": "{{seed_value}}",
  "displayName": "{{seed_value}}",
  "password": "{{seed_value}}"
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~acceptInvitation" -v q
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
- [02_Authentication_Flow.md](../../../docs/diagram/V2/09_Frontend/02_Authentication_Flow.md)
- [Quy tắc chung và thứ tự tích hợp](../README.md)
- [OpenAPI chính](../../../docs/diagram/V2/05_Technical/openapi.yaml)

Snapshot schema liên quan giúp người nhận task xem cả nested fields. YAML chính vẫn là nguồn contract; snapshot này phải được tạo lại nếu YAML đổi.

```yaml
operation:
  operationId: acceptInvitation
  summary: Nhận lời mời
  tags:
  - auth
  x-roles:
  - PUBLIC
  x-permission-policy: auth.invitation
  x-fr:
  - FR-02
  x-readiness: PROPOSED_CONTRACT
  description: Nhận lời mời
  responses:
    '200':
      description: Thành công; trạng thái nghiệp vụ ở payload.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/TokenPair'
    '400':
      $ref: '#/components/responses/Error400'
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
  security: []
  parameters:
  - $ref: '#/components/parameters/IdempotencyKey'
  requestBody:
    required: true
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/AcceptInvitation'
schemas:
  AcceptInvitation:
    type: object
    additionalProperties: false
    properties:
      invitationToken: &id001
        type: string
        minLength: 1
      displayName: *id001
      password: *id001
    required:
    - invitationToken
    - displayName
    - password
  Actor:
    type: object
    additionalProperties: false
    properties:
      id:
        type: string
        format: uuid
      displayName: *id001
      role:
        type: string
        enum:
        - SUPERVISOR
        - PM
        - OPERATOR
        - CREW
        - REPORTER
      version:
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
    required:
    - id
    - displayName
    - role
    - version
  TokenPair:
    type: object
    additionalProperties: false
    properties:
      accessToken: *id001
      refreshToken: *id001
      tokenType:
        type: string
        enum:
        - Bearer
      expiresIn:
        type: integer
        minimum: 1
      mustChangePassword:
        type: boolean
      user:
        $ref: '#/components/schemas/Actor'
    required:
    - accessToken
    - refreshToken
    - tokenType
    - expiresIn
    - mustChangePassword
    - user
```

## Completion history

### 2026-09-29 09:42 +07:00 - PARTIAL

- Scope/result: source comparison completed after dependency 010; reused the acceptance transaction and added repository concurrency translation so same-key races replay and competing acceptance cannot create a second account/session.
- Files/symbols: existing controller/service/repository/DTO/entities/mappings/DI; evidence in `V2-P1-006-015-CODE-CHECKPOINT.md`.
- Acceptance criteria: lifecycle/idempotency/session code complete; runtime behavior not yet verified.
- Verification: shared API build PASS (0 errors, 40 warnings); tests/Newman/smoke/review-autofix NOT_RUN by owner instruction.
- Side effects: no package, migration, schema, data, external call, commit or push; `UNCOMMITTED`.
- Unverified/blockers: Code complete — awaiting review/verification.

### 2026-09-29 11:22 +07:00 - PARTIAL

- Scope/result: prepared guarded invitation acceptance using a mailbox token entered only in the private Postman environment.
- Files/symbols: Postman invitation environment/collection/README and existing fake sender flow.
- Acceptance criteria: focused API test proves invitation acceptance creates the account once and same-key replay succeeds; no token is read from persistence.
- Verification: API build PASS (0 errors); `V2IdentityOnboardingFlowTests` PASS 3/3; Postman JSON/YAML and secret-placeholder scan PASS. Real mailbox/manual accept NOT_RUN.
- Reused/invalidated evidence: prior build-only evidence replaced by current focused tests; external smoke remains missing.
- Side effects: no package/migration/schema/Development DB/SMTP/commit/push; `UNCOMMITTED`.
- Unverified/blockers: real invitation receipt/link parsing/manual accept and external smoke remain required before DONE.
