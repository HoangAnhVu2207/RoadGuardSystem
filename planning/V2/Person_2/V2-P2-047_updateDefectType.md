# V2-P2-047 — Cập nhật/ngừng mã; giữ lịch sử

## V2(3) status

- decisionRefs: none
- requirementRefs: register crosswalk; task-specific references remain authoritative
- contractStatus: PROPOSED_CONTRACT
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- workstream: BE
- blockers: Confirm current source and preserve compatibility before implementation.


- **Owner:** Person 2 — huy. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `PUT /api/v1/catalog/defect-types/{code}`; operationId `updateDefectType`.
- **Trạng thái kế hoạch:** `NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.1.1-draft-review1. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-36; nhóm kế hoạch cũ P1-64/P2-64 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W2; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## 1. Cần làm và tại sao

Cập nhật/ngừng mã; giữ lịch sử. Cập nhật/ngừng mã; giữ lịch sử

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-36; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** Không có dependency API cứng được chỉ định; seed trực tiếp fixture hợp lệ để test độc lập..
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `SUPERVISOR`. Policy: `admin.catalog`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `code` | path | True | string; minLength=1 |
| `Idempotency-Key` | header | True | string; minLength=1 |
| `If-Match` | header | True | string; minLength=3 |

**Request body:** `CatalogEntryUpdate`.

| Field cấp đầu | Bắt buộc | Kiểu / giới hạn |
|---|---|---|
| `label` | True | string; minLength=1 |
| `active` | True | boolean |
| `reason` | True | string; minLength=1 |

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 200 | application/json: CatalogEntry | ETag |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `409`, `412`, `413`, `415`, `422`, `428`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `updateDefectType` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: Identity, ProjectMember, catalog/configuration tùy resource.
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
| T01 | Seed đúng role/scope/trạng thái; gọi updateDefectType với payload hợp lệ | HTTP 200, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T03 | Bỏ từng field required; sai enum/type; giá trị ngoài min/max; reference không tồn tại hoặc khác project | 400/422 hoặc mã phù hợp operation; details chỉ rõ field; DB/outbox không có effect một phần |
| T04 | Thiếu If-Match; version cũ; hai mutation cùng version chạy đồng thời | 428; 412; tối đa một cập nhật theo version thành công, history không mất |
| T05 | Cùng key+payload gọi hai lần, đồng thời và sau mất response; cùng key đổi payload | Replay theo contract; đúng một effect nghiệp vụ; key khác payload trả conflict, không ghi thêm |
| T06 | Inject lỗi trước commit và sau commit trước trả response; retry theo cùng identity | Trước commit rollback; sau commit không nhân effect, audit hoặc notification; không ACK dữ liệu chưa bền vững |
| T08 | Rủi ro nghiệp vụ riêng | Người có role đúng nhưng ngoài scope vẫn bị chặn; mutation lặp không tạo audit nghiệp vụ trùng; không làm mất lịch sử đối tượng đã được tham chiếu. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P2-047: updateDefectType
PUT {{apiBase}}/catalog/defect-types/{{code}}
Authorization: Bearer {{accessToken}}
Idempotency-Key: {{IdempotencyKey}}
If-Match: "{{version}}"
Content-Type: application/json

{
  "label": "{{seed_value}}",
  "active": true,
  "reason": "{{seed_value}}"
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~updateDefectType" -v q
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
  operationId: updateDefectType
  summary: Cập nhật/ngừng mã; giữ lịch sử
  tags:
  - admin
  x-roles:
  - SUPERVISOR
  x-permission-policy: admin.catalog
  x-fr:
  - FR-36
  x-readiness: PROPOSED_CONTRACT
  description: Cập nhật/ngừng mã; giữ lịch sử
  responses:
    '200':
      description: Thành công; trạng thái nghiệp vụ ở payload.
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/CatalogEntry'
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
  parameters:
  - name: code
    in: path
    required: true
    schema: &id001
      type: string
      minLength: 1
  - $ref: '#/components/parameters/IdempotencyKey'
  - $ref: '#/components/parameters/IfMatch'
  requestBody:
    required: true
    content:
      application/json:
        schema:
          $ref: '#/components/schemas/CatalogEntryUpdate'
schemas:
  CatalogEntry:
    type: object
    additionalProperties: false
    properties:
      code: *id001
      label: *id001
      active: &id002
        type: boolean
      version:
        type: string
        minLength: 1
        description: Opaque concurrency version; không parse thành số ở client.
    required:
    - code
    - label
    - active
    - version
  CatalogEntryUpdate:
    type: object
    additionalProperties: false
    properties:
      label: *id001
      active: *id002
      reason: *id001
    required:
    - label
    - active
    - reason
```
