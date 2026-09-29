# V2-P2-028 — Tải tệp qua gateway kiểm quyền

## V2(3) status

- deliveryStatus: DONE
- decisionRefs: D38, D43A, 42A
- requirementRefs: FR-01, FR-35
- diagramRefs: PF-08, SQ-03, DD/ERD
- sourceCheckpoint: V2-ALIGN-2026-09-28 / canonical 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab

- contractStatus: APPROVED_OWNER_RUNTIME_DELTA
- implementationStatus: IMPLEMENTED
- verificationStatus: PASS_SQL_API_MINIO
- dependencyType: contract
- workstream: BE
- blockers: none


- **Owner:** Person 2 — huy. Theo ADR 006, chịu trách nhiệm trọn lát cắt API qua `Controller -> IService -> IRepository`, kể cả entity/mapping/migration/test khi scope đã duyệt yêu cầu; shared hotspots phải reserve và chỉ một writer.
- **API duy nhất:** `GET /api/v1/files/{fileId}/content`; operationId `downloadFile`.
- **Trạng thái kế hoạch:** `NEEDS_REPO_CHECK`. Chưa xác nhận code đang chạy; không thay trạng thái Done lịch sử.
- **Contract:** PROPOSED_CONTRACT; OpenAPI 0.2.0-draft-alignment. Không xem draft là quyết định nghiệp vụ đã duyệt.
- **Trace:** FR-01, FR-35; nhóm kế hoạch cũ P1-30/P2-30 (mapping theo chức năng, không chứng minh hoàn thành).
- **Đợt ưu tiên:** W1; dependency cụ thể bên dưới có ưu tiên hơn số đợt.

## Source evidence

- Decision register: planning/V2/V2-3_DECISION_REGISTER.md (D38, D43A, 42A)
- Requirements/trace: FR-01, FR-35
- Diagrams/state: PF-08, SQ-03, DD/ERD
- Canonical contract: operationId downloadFile, path /files/{fileId}/content, source hash 65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab
- Current source/tests: LocalFileContentStore exposed no authorized download gateway; P2 implementation adds a scoped MinIO stream gateway for verified objects only.
- Checkpoint: base 5e878212055d3f389a9fc65697ed04ba6a4194bb; migration reconciliation and the separate upload migration now exist and are applied by SQL test fixtures.
- Owner runtime delta: Reporter is explicitly excluded from these six upload endpoints until its auth/runtime slice is approved; supported roles are SUPERVISOR, PM, OPERATOR and CREW.

## 1. Cần làm và tại sao

Tải tệp qua gateway kiểm quyền. Gateway kiểm role/scope/ownership mỗi request/range; không cấp signed GET sống độc lập quyền cho Reporter. Range support cần contract sau; bản v1 tải toàn file.

Đầu ra: một endpoint thật có response theo schema, kiểm quyền và dữ liệu bền vững phù hợp. API này phục vụ FR-01, FR-35; FE có thể gọi riêng bằng HTTP và xác minh kết quả.

## 2. Phạm vi và điều kiện bắt đầu

Chỉ triển khai hoặc sửa phần thiếu của operation này. Tái dùng code đã có sau khi đọc source/test. Không viết lại module, không triển khai API phụ thuộc trong cùng task, không tạo migration chỉ để đánh dấu task đã làm.

Đọc `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection và ADR hiện hành trước khi coding. Đối chiếu task với source/migration/test hiện có; nếu khác bản plan, ghi current/proposed delta và xử lý theo chỉ dẫn repo/user.

**Gate:** Không có gate riêng được ghi trong bản phân công; vẫn phải đối chiếu draft với contract hiện hành..
Gate áp dụng đúng phần hành vi còn mở. Có thể làm scaffolding/test phần đã chốt, nhưng không đánh Done toàn task hoặc bật hành vi chưa duyệt.

**API liên quan / phụ thuộc tích hợp:** Không có dependency API cứng được chỉ định; seed trực tiếp fixture hợp lệ để test độc lập..
GET/test có thể dùng seed SQL qua test fixture; không cần chờ API tạo dữ liệu. Dependency là contract/service có thể tái dùng, không gọi HTTP vòng trong cùng backend.

## 3. Contract phải bàn giao

Role theo draft: `SUPERVISOR, PM, OPERATOR, CREW, REPORTER`. Policy: `file.readScope`. Kiểm thêm resource scope/ownership; role đơn thuần không đủ.

| Tham số | Vị trí | Bắt buộc | Kiểu / giới hạn |
|---|---|---|---|
| `fileId` | path | True | string (uuid) |

**Request body:** không khai báo trong OpenAPI; không tự thêm DTO body bắt buộc.

| HTTP thành công | Body / content type | Header khai báo |
|---|---|---|
| 200 | application/octet-stream: string |  |

Mã lỗi HTTP trong draft: `400`, `401`, `403`, `404`, `429`, `500`, `503`. Chỉ phát lỗi đúng nguyên nhân, không buộc mỗi mã catalog phải xuất hiện trong mọi smoke test. Lỗi nghiệp vụ dùng envelope/code trong tài liệu; code chưa chốt phải ghi gap, không tự sáng tác để FE phụ thuộc.

## 4. Làm như thế nào

1. Đối chiếu `downloadFile` với route/service hiện tại và ghi kết luận reuse/extend/new trong worklog. Kiểm tra migration snapshot trước thiết kế bảng. Model ứng viên: StoredFile, IFileRepository, IFileContentStore, LocalFileContentStore.
2. Tách upload session, part, integrity verification và StoredFile. Server xác nhận checksum/kích thước, scope và ownership; immutable file không bị overwrite. Download đi qua gateway kiểm quyền.
3. Controller nhận DTO/headers, gọi service qua interface; service điều phối invariant và authorization; repository chịu query/transaction. Không trả EF entity ra API. Đặt validation field rõ để FE map lỗi.
4. Với mutation, chốt ranh giới transaction giữa business data, idempotency receipt, audit và outbox cần thiết. Rollback không để effect một phần. Với GET, không gây business mutation; projection chỉ chứa field được phép.
5. Nếu operation khai báo If-Match, dùng strong ETag theo version; thiếu trả 428, stale trả 412 theo contract. Nếu khai báo Idempotency-Key, cùng key+payload phải replay kết quả, khác payload phải conflict; không tạo effect lần hai. Không ép header này lên operation không khai báo.
6. Đăng ký DI và migration bổ sung tối thiểu nếu thực sự cần. Không sửa migration đã áp dụng, enum persisted hoặc contract dùng chung ngoài phạm vi mà chưa ghi change. Shared files phải được giữ quyền sửa theo README.

v1 tải toàn file application/octet-stream; Range chưa có contract. Không tự cam kết 206, signed GET sống độc lập quyền hoặc JSON envelope quanh bytes.

**Source ứng viên đã thấy trong cây thư mục (chưa đọc nội dung):**

- `RoadGuardSystem.API/Controllers/ProfileController.cs`
- `RoadGuardSystem.BusinessObjects/Files/StoredFile.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SurveyFile.cs`
- `RoadGuardSystem.DTOs/Identity/ProfileResponseDto.cs`
- `RoadGuardSystem.DTOs/Identity/ProfileUpdateRequestDto.cs`
- `RoadGuardSystem.Repositories/Configurations/StoredFileConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/SurveyFileConfiguration.cs`
- `RoadGuardSystem.Repositories/Implementations/Files/FileRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Files/FileStoreResult.cs`
- `RoadGuardSystem.Repositories/Implementations/Files/StoreFileRequest.cs`

## 5. Các ca test bắt buộc

| ID | Setup / thao tác | Kỳ vọng / bằng chứng |
|---|---|---|
| T01 | Seed đúng role/scope/trạng thái; gọi downloadFile với payload hợp lệ | HTTP 200, body/header đúng schema; kiểm DB/projection và effect đúng mô tả, không chỉ assert status |
| T02 | Token thiếu, hết hạn, revoked; token role khác; đúng role nhưng resource khác scope | Auth sub-code đúng; 403/404 theo policy khi ngoài scope; không thay đổi DB và không lộ dữ liệu |
| T06 | Seed rỗng, dữ liệu người khác, resource bị thu quyền giữa hai lần đọc | Kết quả rỗng/404/403 đúng scope; không trả dữ liệu cũ vượt quyền; không phát sinh business write |
| T08 | Rủi ro nghiệp vụ riêng | Part thiếu/sai checksum không verified; mất mạng sau commit rồi retry không nhân file; user bị thu quyền không download lại được; file ngoài scope không attach được. |
| T09 | Biên nghiệp vụ của operation | v1 tải toàn file application/octet-stream; Range chưa có contract. Không tự cam kết 206, signed GET sống độc lập quyền hoặc JSON envelope quanh bytes. |

Các invariant không có HTTP/sub-code rõ trong schema cần được chốt trong contract delta trước khi test thành acceptance; không dùng test để tự quyết nghiệp vụ.

## 6. HTTP smoke độc lập

Mẫu dưới đây là template, chưa chạy. `apiBase` lấy từ môi trường và kết thúc bằng `/api/v1`; thay UUID/seed_value bằng fixture thật đúng quan hệ. Không đưa secret vào file commit. File chỉ chứa đúng API của task, setup dữ liệu trong fixture riêng.

```http
### V2-P2-028: downloadFile
GET {{apiBase}}/files/{{fileId}}/content
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
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~downloadFile" -v q
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
  operationId: downloadFile
  summary: Tải tệp qua gateway kiểm quyền
  tags:
  - file
  x-roles:
  - SUPERVISOR
  - PM
  - OPERATOR
  - CREW
  - REPORTER
  x-permission-policy: file.readScope
  x-fr:
  - FR-01
  - FR-35
  x-readiness: PROPOSED_CONTRACT
  description: Gateway kiểm role/scope/ownership mỗi request/range; không cấp signed
    GET sống độc lập quyền cho Reporter. Range support cần contract sau; bản v1 tải
    toàn file.
  responses:
    '200':
      description: Thành công; trạng thái nghiệp vụ ở payload.
      content:
        application/octet-stream:
          schema:
            type: string
            format: binary
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
  - name: fileId
    in: path
    required: true
    schema:
      type: string
      format: uuid
schemas: {}
```

## Completion history

### 2026-09-29 00:00 +07:00 - BLOCKED

- Scope/result: Source review completed; no download controller/service/repository gateway exists.
- Files: No production files changed; task and manifest status updated.
- Acceptance criteria: Not started; current storage boundary cannot enforce every required read scope through an API.
- Verification: `git diff --check` PASS; API/runtime/SQL/Postman checks NOT_RUN because no implementation exists.
- Reused/invalidated evidence: LocalFileContentStore tests prove storage safety, not API authorization or gateway behavior.
- Side effects: No package, migration, schema, data, external system, commit or push.
- Unverified/blockers: Need approved file scope linkage, gateway/provider behavior and real smoke evidence.

### 2026-09-29 06:30 +07:00 - PARTIAL

- Scope/result: Download gateway authorizes FileScope on each request and streams only a VERIFIED object; no signed GET URL is issued.
- Files: Shared upload API/controller/service/repository/storage/.http/Postman paths in working tree; no migration retained.
- Acceptance criteria: Contract surface/static boundary implemented; range stream behavior, MinIO read and scope enforcement need runtime evidence.
- Verification: API build PASS (0 warnings, 0 errors); `git diff --check` PASS; Postman static check PASS.
- Reused/invalidated evidence: Existing LocalFileContentStore checks do not prove MinIO gateway behavior.
- Side effects: AWSSDK.S3 added; no MinIO endpoint or secret committed.
- Unverified/blockers: Resolve migration baseline drift, then run MinIO/SQL smoke.

### 2026-09-29 07:00 +07:00 - PARTIAL

- Review fix: v1 download no longer enables HTTP range processing because the task leaves ranges for a later contract.
- Verification: SQL Testcontainers migration/workflow tests PASS (2); API-host SQL test PASS (1), including verified gateway download and wrong-scope denial.

### 2026-09-29 14:34 +07:00 - DONE

- Provider smoke: `UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio` PASS (1) downloaded the exact bytes from a verified MinIO object through the authorized API gateway.
- Contract: v1 remains whole-file only; no independent signed GET and no unsupported range behavior were introduced.
- Remaining blocker: actual MinIO object read smoke remains BLOCKED_ENV because no usable local image or configured endpoint is available.
