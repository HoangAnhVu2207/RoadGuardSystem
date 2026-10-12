# ROADGUARD — BE-DOC-02 FINAL
## Build a Complete, Source-Verified Backend Documentation Baseline

### ROLE

Bạn là Senior .NET Backend Architect, Software Documentation Engineer, Database Architect và Technical QA Reviewer của dự án RoadGuardSystem.

Nhiệm vụ duy nhất của bạn:

**Xây dựng một bộ tài liệu Backend RoadGuard đầy đủ, độc lập, nhất quán với nghiệp vụ đã chốt và toàn bộ mã nguồn Backend hiện tại.**

Không chỉ kiểm toán, không chỉ tạo mục lục, không chỉ viết bản tóm tắt.

Phải thực sự soạn toàn bộ tài liệu, kiểm tra chéo, xác minh, rồi commit local trên nhánh `huy-review`.

Không tự chia nhỏ thành những task giao lại người dùng.

---

# 1. FOUR FROZEN OWNER DECISIONS

Bốn quyết định đã được chủ dự án xác nhận:

**D1 — Source of Truth**

Sử dụng cả:

- Nghiệp vụ và quyết định sản phẩm đã được xác nhận.
- Source code Backend hiện tại để mô tả những gì thực sự được triển khai.

Khi hai nguồn mâu thuẫn, mô tả riêng `EXPECTED_BUSINESS_BEHAVIOR` và `CURRENT_IMPLEMENTATION`, lập GAP và cung cấp bằng chứng.

Không tự thay đổi nghiệp vụ để hợp với code.

**D2 — 100% Backend Scope**

Bao phủ toàn bộ:

- API/actions và DTO.
- Services và Repositories.
- Database entities, configurations, migrations.
- Business workflows.
- Authentication và Authorization.
- Offline Backend.
- AI integration boundary.
- Storage, background jobs, outbox, retention.
- Error handling, concurrency, idempotency.
- Tests và validation evidence.

Không chỉ viết các chức năng nổi bật.

**D3 — No Backend Modifications**

Chỉ viết tài liệu.

Nếu phát hiện code thiếu, sai hoặc chưa kiểm chứng:

- Mô tả hiện trạng.
- Ghi GAP.
- Đề xuất hướng giải quyết tối thiểu.
- Không tự sửa source code, DTO, tests, migrations hoặc configuration.

**D4 — Delivery**

- Tạo bộ docs mới tại `docs/RoadGuard-Backend-Official/`.
- Commit local trực tiếp vào `huy-review`.
- Không tạo branch khác.
- Không push.
- Không merge.
- Không deploy.

Bốn quyết định này là FROZEN. Không hỏi lại.

---

# 2. GIT BASELINE AND SAFETY

Project:

`D:\Project BE\RoadGuardSystem`

Required branch:

`huy-review`

Expected prior documentation-audit commit:

`cdeb1ca1037744d06995058af13d0582f6273c0a`

Trước khi viết:

1. Kiểm tra nhánh.
2. Kiểm tra HEAD.
3. Kiểm tra `git status --porcelain`.
4. Kiểm tra commit `cdeb1ca` có trong lịch sử HEAD.
5. Đọc diff và báo cáo DOC-AUDIT-01.
6. Lập manifest source files và docs.
7. Xác nhận không có thay đổi người khác chưa commit.

Nếu HEAD đã tiến lên bằng commit hợp lệ, sử dụng HEAD thực tế và báo cáo sự khác biệt.

Không tự reset, rebase, cherry-pick, merge hoặc pull.

Không ghi đè thay đổi chưa commit.

Chỉ stage đường dẫn:

`docs/RoadGuard-Backend-Official/**`

Các lệnh build/test phải tránh làm thay đổi file tracked khác trong repository.

---

# 3. DOCUMENTATION AUTHORITY

Dùng các nguồn theo đúng chức năng:

| Source | Authority |
|---|---|
| Owner-confirmed business decisions | Expected business behavior |
| Current Backend source | Actual implementation |
| EF entities/configuration/migrations | Persistence design |
| Executed tests | Verified behavior within tested scope |
| DOC-AUDIT-01 | Audit findings requiring cross-check |
| Legacy V2 documentation | Historical / proposed reference |
| FE / AI specifications | Integration expectations, not Backend runtime proof |

Không coi README hoặc kế hoạch cũ là bằng chứng runtime.

Không coi method tồn tại là end-to-end PASS.

Không coi build PASS là toàn bộ nghiệp vụ đã hoàn thành.

Không coi mock AI là FastAPI tích hợp thật.

Không coi EF migration source là deployed SQL schema.

---

# 4. ROADGUARD FUNCTIONAL BASELINE

Giữ nguyên kiến trúc:

**Web / Android FE ↔ ASP.NET Core Backend ↔ FastAPI AI**

Backend là hệ thống điều phối nghiệp vụ và lưu trữ dữ liệu.

AI xử lý video drone để phát hiện khiếm khuyết mặt đường và trả kết quả ứng viên.

Crew đo đạc trực tiếp tại hiện trường, không sử dụng AI để đo kích thước hư hỏng thực tế.

PM đánh giá và quyết định nghiệp vụ.

Supervisor thực hiện các quyền phê duyệt/giám sát theo đúng role và scope.

Các quyết định đã chốt về Reporter, IncidentCase, network roads, Fast Track, offline, handover, retention và authorization phải được bảo toàn.

**Out of scope:**

- Orthomosaic.
- DSM.
- 3D photogrammetry.
- AI geometric depth measurement.
- FE implementation details.
- FastAPI internal model architecture.

**Deferred, not removed:**

- Live BE ↔ FastAPI integration.
- Android offline/reconnect E2E.
- Physical SQL Server schema verification.
- Real object-storage integration tests.
- Deployed runtime Swagger/UAT.

---

# 5. REQUIRED DOCUMENTATION LOCATION

Tạo thư mục:

`docs/RoadGuard-Backend-Official/`

Không sửa trực tiếp bộ docs V2 cũ trong task này.

Tạo tài liệu theo cấu trúc:

```text
docs/RoadGuard-Backend-Official/
│
├── README.md
├── DOCUMENTATION_MANIFEST.md
│
├── 00_Overview/
│   ├── 01_Backend_Project_Overview.md
│   ├── 02_Backend_Scope.md
│   ├── 03_System_Actors_and_Responsibilities.md
│   └── 04_Implementation_Status.md
│
├── 01_Architecture/
│   ├── 01_Backend_Architecture.md
│   ├── 02_Module_Boundaries.md
│   ├── 03_Controller_Service_Repository_Flow.md
│   ├── 04_Background_Workers_and_Outbox.md
│   └── 05_Configuration_and_Dependency_Injection.md
│
├── 02_Business_Workflows/
│   ├── 01_Authentication_and_Accounts.md
│   ├── 02_Staff_Membership_and_Project.md
│   ├── 03_Roads_Geometry_and_Pavement.md
│   ├── 04_Reporter_and_IncidentCase.md
│   ├── 05_Drone_Survey_and_AI_Processing.md
│   ├── 06_Field_Inspection_and_Measurements.md
│   ├── 07_Repair_Policy_and_Fast_Track.md
│   ├── 08_Repair_Execution_and_Acceptance.md
│   ├── 09_Notifications_and_Reporting.md
│   └── 10_Retention_and_Export.md
│
├── 03_API_Contracts/
│   ├── 01_API_Conventions.md
│   ├── 02_Endpoint_Catalog.md
│   ├── 03_Request_Response_DTOs.md
│   ├── 04_Error_Catalog.md
│   ├── 05_Concurrency_and_Idempotency.md
│   ├── 06_OpenAPI_Runtime_Alignment.md
│   └── openapi.backend.yaml
│
├── 04_Database/
│   ├── 01_Database_Overview.md
│   ├── 02_Entity_Data_Dictionary.md
│   ├── 03_Entity_Relationships.md
│   ├── 04_Persistence_Constraints.md
│   ├── 05_Migrations_and_Schema_Baseline.md
│   └── 06_Database_ERD.md
│
├── 05_Security/
│   ├── 01_Authentication.md
│   ├── 02_Authorization_Matrix.md
│   ├── 03_Project_Scope_and_Ownership.md
│   └── 04_Cookie_CSRF_and_Token_Security.md
│
├── 06_Integrations/
│   ├── 01_BE_FE_Integration_Boundary.md
│   ├── 02_BE_FastAPI_Integration_Boundary.md
│   └── 03_File_and_Artifact_Exchange.md
│
├── 07_Offline_Sync/
│   ├── 01_Offline_Backend_Architecture.md
│   ├── 02_Snapshot_and_Device_Management.md
│   ├── 03_Signed_Sync_and_Idempotency.md
│   └── 04_Conflict_Handover_and_Recovery.md
│
├── 08_Storage_Operations/
│   ├── 01_File_and_Object_Storage.md
│   ├── 02_Retention_and_Legal_Hold.md
│   ├── 03_Deployment_Configuration.md
│   └── 04_Operational_Recovery.md
│
├── 09_Testing/
│   ├── 01_Backend_Test_Strategy.md
│   ├── 02_API_Test_Matrix.md
│   ├── 03_Database_Integration_Tests.md
│   └── 04_Known_Test_Limitations.md
│
├── 10_Traceability/
│   ├── 01_Requirement_to_Implementation_Matrix.md
│   ├── 02_API_to_Source_Matrix.md
│   └── 03_Database_to_Source_Matrix.md
│
└── 11_Audit_and_Gaps/
    ├── 01_Documentation_Alignment_Report.md
    ├── 02_Known_Implementation_Gaps.md
    └── 03_Pending_Environment_Verification.md
```

Mỗi file phải có nội dung thực chất, không tạo placeholder để đạt đủ danh sách.

Nếu thông tin chưa thể xác minh, ghi rõ giới hạn bằng chứng thay vì bịa dữ liệu.

---

# 6. SOURCE INVENTORY — NO SKIPPING

Trước khi soạn docs, inventory toàn bộ Backend.

Phải có danh sách chính xác:

- Controllers và API actions.
- Services interfaces/implementations.
- Repositories interfaces/implementations.
- Entity classes.
- DTO classes.
- DbSets và EF configurations.
- Migrations.
- Middleware/filters.
- Authorization policies.
- Background workers.
- Outbox/notification consumers.
- Unit/API/integration tests.

Không chỉ dựa vào thống kê của Antigravity.

DOC-AUDIT-01 từng ghi nhận khoảng 241 API và 191 bảng; phải tự xác minh trên commit hiện tại.

Nếu không kiểm chứng được một con số, không dùng con số đó như fact.

Mọi inventory được lưu trong bộ docs mới, có đường dẫn nguồn và trạng thái kiểm tra.

---

# 7. API DOCUMENTATION — 100% COVERAGE

Lập catalog cho toàn bộ API trong source hiện tại.

Mỗi operation phải có:

- HTTP method.
- Route đầy đủ.
- API version.
- Controller.Action.
- Authentication.
- Authorization.
- Request parameters.
- Request DTO.
- Response DTO.
- Required headers.
- HTTP statuses.
- Error codes.
- Service implementation.
- Test references.
- Implementation status.

Không bỏ qua API chỉ vì không có trong V2.

Không bổ sung ghost route vào runtime contract.

Không tự xóa ghost routes khỏi V2 proposed specification.

## Runtime OpenAPI

Tạo:

`03_API_Contracts/openapi.backend.yaml`

Đây là **snapshot đặc tả Backend implemented**, không phải canonical V2 proposal.

Ưu tiên tạo từ ASP.NET API Explorer/Swagger runtime của Backend ở môi trường local an toàn.

Nếu không khởi chạy được runtime:

- Dùng source-level route discovery.
- Xác minh từng method/path/schema.
- Gắn `SOURCE_DERIVED_NOT_RUNTIME_VERIFIED`.
- Không gọi đó là fully runtime-verified OpenAPI.

Phải kiểm tra:

- OpenAPI validity.
- Duplicate operationId.
- Method/path collisions.
- Schema references.
- Security schemes.
- Nullable/required fields.
- ETag/If-Match.
- Idempotency-Key.
- Error responses.

Không sửa FE generated contracts.

---

# 8. DATABASE — COMPLETE SOURCE-BASED MODEL

Đọc toàn bộ:

- DbContext.
- Entities.
- Fluent API configurations.
- Value converters.
- Migration source.
- Snapshot.
- SQL scripts.
- Triggers.

Tạo:

1. Data Dictionary của toàn bộ schema.
2. Mapping table/entity.
3. Relationship catalog.
4. Constraint catalog.
5. Migration baseline notes.
6. ERD theo từng bounded context.

Phân biệt:

- `CONCEPTUAL_MODEL`
- `EF_LOGICAL_MODEL`
- `MIGRATION_DERIVED_PHYSICAL_MODEL`
- `LIVE_SQL_VERIFIED`

Không ghi `LIVE_SQL_VERIFIED` khi chưa kiểm tra SQL thật.

Không trộn lẫn UTM WGS84 EPSG:32648/32649 với VN2000 như thể là một hệ tọa độ.

Mô tả đúng cách code lưu CRS, geometry, transform, length/stationing và source-of-truth tọa độ nếu đã triển khai.

Không chạy migration trên database shared/team.

---

# 9. COMPLETE BUSINESS WORKFLOWS

Phải tài liệu hóa đầy đủ các phân hệ:

1. Auth, OTP, accounts, web cookie, token.
2. Supervisor, Staff Directory, Membership, Primary PM.
3. Projects, road network, centerline, geometry, slabs.
4. Reporter, ReportSupplement, IncidentCase.
5. Survey, Operator, video/SRT, uploads.
6. Processing jobs, BE-side AI orchestration, callbacks.
7. Field Inspection, Crew physical measurements.
8. Fast Track/Approval Track, repair policies.
9. Repair execution, evidence, attempts, review, acceptance.
10. Offline device, snapshot, sync, handover, recovery.
11. Notifications, reminders, outbox, workers.
12. Reporting, statistics, exports, retention, legal hold.

Với mỗi workflow:

- Expected business behavior.
- Current implementation.
- Actor/authorization.
- Preconditions.
- Main flow.
- Alternative/error flows.
- State machine.
- API sequence.
- DTOs.
- Persistence effects.
- Concurrency/idempotency.
- Evidence.
- Tests.
- Gaps.

Không tự loại bỏ curing/traffic release nếu chưa có quyết định nghiệp vụ có hiệu lực rõ ràng.

Không tự bổ sung nghiệp vụ kiểm soát vật liệu chi tiết đã bị loại khỏi scope.

---

# 10. SECURITY AND ACCESS CONTROL

Tạo ma trận authorization chính xác ở mức operation.

Kiểm tra:

- `[Authorize]`.
- Policies.
- Web cookie eligibility.
- JWT bearer.
- Supervisor authority.
- PM project scope.
- Crew assignment.
- Operator assignment.
- Reporter ownership.
- Membership effective dates.
- CSRF.
- Session revocation.
- Account status.
- Offline reconnect authorization.
- Actor provenance.
- PII visibility.

Đặc biệt xác minh:

- `GET /users`.
- `GET /projects/{id}/members`.
- `GET /projects/{id}/primary-project-manager`.
- `GET /projects`.
- Supervisor account detail email/phone.

Không suy quyền chỉ từ role.

---

# 11. TESTING AND TRACEABILITY

Đối với mỗi module:

- Liệt kê tests hiện có.
- Map tests với API và business requirements.
- Ghi PASS/FAIL/NOT_RUN theo kết quả execution có căn cứ.
- Không sử dụng tên test làm bằng chứng rằng behavior đã được bảo vệ.
- Kiểm tra negative cases, permissions, conflicts, versioning, replay, persistence.

Đọc lại DOC-AUDIT-01:

- Build PASS, 0 lỗi, 222 warning được báo cáo.
- 380/381 test PASS.
- 1 test FAIL liên quan Swagger.
- 2 FE validators FAIL.
- 281 legacy references bị thiếu.

Những dữ liệu này là historical audit evidence cho đến khi xác nhận baseline và nguyên nhân.

Không sửa tests hoặc FE contract locks trong task này.

---

# 12. QUALITY GATES

Phải hoàn thành các gate:

**QG-01 — Git baseline integrity**

HEAD và branch đúng, working tree được bảo vệ.

**QG-02 — API coverage**

Mọi Backend API operation đều được catalog hoặc có ngoại lệ minh bạch.

**QG-03 — DTO coverage**

Tất cả request/response schema có traceability.

**QG-04 — Database coverage**

Entity/table/configuration/constraint có mapping.

**QG-05 — Workflow coverage**

Không thiếu workflow Backend.

**QG-06 — Authorization coverage**

Không có operation bị gán role/phạm vi quyền bằng suy đoán.

**QG-07 — Integration boundaries**

FE ↔ BE ↔ FastAPI được giữ nhất quán.

**QG-08 — Document consistency**

Không có đường dẫn hỏng trong bộ mới, tài liệu không tự mâu thuẫn.

**QG-09 — OpenAPI validity**

YAML hợp lệ, schema references đúng, không có route/method duplicates.

**QG-10 — Source-only diff**

Chỉ có docs mới trong phạm vi thay đổi.

**QG-11 — Evidence accuracy**

Không có trạng thái PASS không có bằng chứng.

Nếu gate FAIL thì sửa nội dung docs thuộc quyền cho phép rồi chạy lại.

Nếu fail do source/runtime/environment không thuộc quyền sửa, ghi rõ BLOCKED hoặc PENDING, không đánh dấu PASS.

---

# 13. EXECUTION ORDER

Thực hiện liên tục:

**Phase A:** Git verification, read DOC-AUDIT-01 và confirmed decisions.

**Phase B:** Full source/API/DTO/DB/security inventory.

**Phase C:** Trace workflows và map requirements.

**Phase D:** Viết bộ tài liệu theo cấu trúc đã chỉ định.

**Phase E:** Tạo source-derived/backend OpenAPI.

**Phase F:** Cross-check docs → source và source → docs.

**Phase G:** Validation, link checks, schema checks, syntax checks.

**Phase H:** Final diff inspection, local commit, final report.

Không dừng để hỏi người dùng về những quyết định đã chốt.

Nếu gặp giới hạn context/tool, phải ghi trạng thái và đảm bảo không tuyên bố hoàn thành giả tạo.

---

# 14. DEFINITION OF DONE

Chỉ được kết luận `BE-DOC-02 COMPLETE` khi:

- Bộ tài liệu đầy đủ nằm tại `docs/RoadGuard-Backend-Official/`.
- Bao phủ 100% API Backend trong inventory đã xác minh.
- Bao phủ 100% entity/persistence schema theo source.
- Các workflow Backend có bằng chứng.
- Security matrix có thể truy vết.
- Backend OpenAPI độc lập, hợp lệ.
- FE/AI chỉ được mô tả ở integration boundary.
- Nghiệp vụ đã chốt được bảo toàn.
- Known gaps và deferred tests được ghi rõ.
- Document internal links hợp lệ.
- Không sửa Backend source.
- Không sửa tests.
- Không sửa migration.
- Không sửa V2 proposal cũ.
- Commit local trên `huy-review`.
- Working tree sạch sau commit.

Không đánh đồng `DOCUMENTATION_COMPLETE` với `RUNTIME_E2E_CERTIFIED`.

---

# 15. REQUIRED FINAL RESPONSE

Sau khi hoàn thành, trả về đúng cấu trúc:

## A. Git
Branch, baseline SHA, final SHA, commit, working tree.

## B. Documentation
Tổng số file docs, danh sách thư mục, phạm vi.

## C. API
Số Controller/action, catalog count, OpenAPI count, mismatches.

## D. Database
DbSets, entities, tables, constraints, ERD coverage.

## E. Workflows
Danh sách workflows và tình trạng đối chiếu.

## F. Validation
Build/tests/document checks/OpenAPI checks với PASS/FAIL/NOT_RUN.

## G. Open Gaps
Số lượng, mức độ, location, owner và nguyên nhân.

## H. Deliverables
Đường dẫn README, manifest, OpenAPI và commit.

## I. Final Status
`COMPLETE`, `PARTIAL` hoặc `BLOCKED`.

**Không push, không merge, không deploy.**

**BEGIN BE-DOC-02 FINAL NOW.**
