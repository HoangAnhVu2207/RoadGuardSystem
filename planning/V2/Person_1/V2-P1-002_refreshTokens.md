# V2-P1-002 — Rotate refresh token

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
- **API duy nhất:** `POST /api/v1/auth/refresh`; operationId `refreshTokens`.
- **Trạng thái kế hoạch:** `NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.1.1-draft-review1. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-01; nhóm kế hoạch cũ P1-10/P2-10 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W1; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## 1. Cần làm và tại sao

Rotate refresh token. Refresh token một lần, revoke family khi reuse; policy đề xuất. Không coi refresh là anonymous trust.

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-01; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** [V2-P1-001 — login](../Person_1/V2-P1-001_login.md).
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `PUBLIC`. Policy: `auth.refresh`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| Không có | — | — | Không tự thêm header bắt buộc ngoài security |

**Request body:** `RefreshRequest`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `refreshToken` | True | string; minLength=1 |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 200 | application/json: TokenPair |  |

Mã lỗi HTTP trong draft: `400`, `409`, `413`, `415`, `422`, `429`, `500`, `503`, `401`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `refreshTokens` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: Identity, ApplicationUser, UserSession, RefreshToken; AuthService/IdentityService.
2. Xác thực và quản lý phiên là ranh giới quyền; dùng Identity và session validator hiện có. Không log mật khẩu/token, không lưu refresh token rõ. Tách giao dịch rotate/revoke khỏi việc trả response.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.

Rotate refresh token một lần; phân biệt REFRESH_TOKEN_INVALID, REFRESH_TOKEN_EXPIRED, SESSION_REVOKED. Kiểm hai refresh đồng thời và response mất sau rotation theo ADR hiện tại; không tự chốt grace window.

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
| T01 | Seed đúng role/scope/trạng thái; gọi refreshTokens với payload hợp lệ | HTTP 200, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Gửi dữ liệu xác thực/OTP/invitation không hợp lệ; lặp request hoặc account không tồn tại | Public flow không yêu cầu bearer ngầm; không lộ danh tính qua response khác biệt; error đúng catalog |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Phiên revoked vẫn còn JWT chưa hết hạn phải bị từ chối; không lộ tài khoản qua lỗi public; hai request refresh đồng thời không sinh hai phiên kế tiếp hợp lệ. |
| T09 | Biên nghiệp vụ của operation | Rotate refresh token một lần; phân biệt REFRESH_TOKEN_INVALID, REFRESH_TOKEN_EXPIRED, SESSION_REVOKED. Kiểm hai refresh đồng thời và response mất sau rotation theo ADR hiện tại; không tự chốt grace window. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P1-002: refreshTokens
POST {{apiBase}}/auth/refresh
Content-Type: application/json

{
  "refreshToken": "{{seed_value}}"
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~refreshTokens" -v q
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
  operationId: refreshTokens
  summary: Rotate refresh token
  tags:
  - auth
  x-roles:
  - PUBLIC
  x-permission-policy: auth.refresh
  x-fr:
  - FR-01
  x-readiness: PROPOSED_CONTRACT
  description: Refresh token một lần, revoke family khi reuse; policy đề xuất. Không
    coi refresh là anonymous trust.
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
    '401':
      $ref: '#/components/responses/RefreshError401'
  security: []
  requestBody:
    required: true
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/RefreshRequest'
schemas:
  Actor:
    type: object
    additionalProperties: false
    properties:
      id:
        type: string
        format: uuid
      displayName: &id001
        type: string
        minLength: 1
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
  RefreshRequest:
    type: object
    additionalProperties: false
    properties:
      refreshToken: *id001
    required:
    - refreshToken
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
