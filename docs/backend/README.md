# Backend hiện hành — DOC-AUDIT-01

**ACTIVE · IMPLEMENTED_SOURCE_VERIFIED**, branch `huy-review`, source baseline `f0c8762509310b2779068891062267c5f520aff2`, 2026-10-12. Đây là tài liệu implementation local; chưa chứng nhận deploy hoặc sản phẩm E2E. Kết quả chạy mới, giới hạn và GAP nằm trong [báo cáo](../audit/DOC-AUDIT-01-REPORT.md) và [ma trận](../audit/DOC-AUDIT-01-MATRIX.md).

Kiến trúc giữ `Web/Android ↔ ASP.NET Core BE ↔ FastAPI AI`, SQL Server và object storage qua adapter MinIO/local theo cấu hình thực tế. BE quản lý quyền, nghiệp vụ, transaction, evidence, audit/outbox; nhóm ngoài triển khai Web/Android/FastAPI. AI phát hiện ứng viên từ **video drone**; Crew kiểm chứng và đo vật lý ngoài hiện trường. Không đưa orthomosaic, DSM, 3D hoặc AI đo depth vào phạm vi hiện hành.

| Nhóm | Luồng và source thực tế | Giới hạn |
|---|---|---|
| Identity | `AuthController`, `WebAuthController`, `ReporterRegistrationsController`, `UsersController` → identity/onboarding/session services → identity repositories. OTP, bearer access/refresh, server session/cookie, revoke và recovery có implementation. | Email thật, proxy/TLS, browser/mobile persistence chưa được audit E2E. |
| Projects/staff | `ProjectsController.List` → `ProjectListService` → `ProjectListReadModel`; `StaffDirectoryController` → `StaffReadService` → `StaffReadRepository`. GET projects/users/members/primary-PM đã có; account GET Supervisor có contacts. | Không thêm membership command hoặc đủ CRUD. Roster/primary không chứa contact. |
| Geometry/survey/upload | Geometry/Pavement/Survey controllers → service → repository, versioned CRS/route/segment/pavement, assignment/dataset, multipart/resume/verify. `StoredFile.SizeBytes` và `UploadSession.ExpectedSizeBytes` là `long`/SQL `bigint`. | 8 GiB video/32 GiB dataset là admission policy; chưa chạy truyền thật dung lượng lớn trong audit. Official CRS/control points và coverage thực cần bằng chứng riêng. |
| Reporter/case/defect | Reports/ReporterEvidence/Cases/Candidates/TrainingLabels controllers → services → repositories; report ownership, supplements, triage/link/split, ExistingEvidence conclusion, recipient publication, candidate decision/approved-label selection đã có. | FIELD/DRONE pre-Defect final conclusion còn gate; matching vị trí/accuracy chưa nối; race download và legacy callback có GAP cụ thể. |
| Processing/AI | `ProcessingV2Controller` job/attempt/callback và `AiMockRunsController` → ANH02 synthetic worker → immutable result/frame/model provenance. | Mock BE producer/consumer không chứng nhận FastAPI/model thật. Legacy callback chưa fence latest attempt. |
| Repair/Fast Track | RepairPolicies/Eligibility/Packages/Execution/Lifecycle/Safety controllers → corresponding services → `RepairPolicyRepository`, `RepairEligibilityRepository`, `RepairSafetyRepository` and `RepairWorkflowRepository` partials. PM policy, normal Supervisor approval/final, Crew physical execution, supplement/rework/cancel/normal-successor, clocks/correction hiện hữu. | FT-final Supervisor information thiếu producer/source adoption; additional-after-expiry vẫn deferred. Curing/material/traffic opening management bị R01 loại khỏi scope. |
| Offline | `OfflineWorkflowController` → workflow/engine services → offline repository/admission → shared FIELD/repair transactional core. Device, snapshot, signed batch, receipt/replay/conflict, encrypted transfer/grant/import/capture/recovery đã có. | Android queue/kill/reboot/camera/key/decrypt E2E chưa chạy. ND01 đã hỗ trợ admission trong original 24h khi đủ proof; không reset clock. |
| Operations | Notifications/H6 operations, business/clock duties, project lifecycle, coverage/statistics, reporting/exports/retention có implementation. | Notification recipient-scoped; registration không chứng minh event có producer. Benchmark/RPO/RTO, real coverage/public statistical formulas và production worker topology chưa xác minh. |

## Quyền và transport

Supervisor ACTIVE thực hiện **các thao tác dành cho Supervisor trên mọi project**, không cần membership. Không được thay PM/Crew/Operator. Luôn còn account/session hiện hành, đúng resource/project, state, evidence, concurrency và guarded idempotent replay. PM chịu effective membership; Operator/Crew còn assignment. Notification chỉ đọc recipient của chính actor và còn source/current-permission guards nơi quy định.

GET `/api/v1/projects`, `/api/v1/users`, `/api/v1/projects/{projectId}/members`, `/api/v1/projects/{projectId}/primary-project-manager` và `/api/v1/users/{userId}` có cookie opt-in ở **method**. Bearer tiếp tục hoạt động; header Authorization được ưu tiên. Cookie HTTPS thật lấy từ web login/session, GET không có body; unsafe cookie requests cần CSRF theo route. Không suy mọi route đều hỗ trợ cookie.

Nguồn wire quan sát: [241 operation từ local Swagger](../audit/DOC-AUDIT-01-API.md), Controller/DTO và [local Huy contract](../../contracts/http/huy-final.local-contract.md), [local Anh contract](../../contracts/http/anh01.local-contract.md). V2 YAML/FE lock vẫn là proposal; không dùng route draft để gọi API thật. [Postman staff guide](../postman/Staff-Membership-Read-Guide.md) và collections hiện có giữ nguyên identifiers.

## Data và authority

[Data baseline](data/README.md) dẫn tới dictionary/ERD mapping mới được lấy từ fresh owned SQL; migration hiện hành là baseline 20261008171739 và additive 20261009043010. Không áp replacement baseline lên shared database.

Expected behavior theo [confirmed 32–44](../product/confirmed-decisions.md), [HUY-FINAL-INTEGRATION](../../planning/development/HUY-FINAL-INTEGRATION.md) cùng latest ND01/activation và các specs GET Projects/Supervisor-global/Staff Read. Code chứng minh implementation, không tự phê duyệt nghiệp vụ. [Document index](../audit/DOC-AUDIT-01-DOCUMENT-INDEX.md) phân biệt ACTIVE/PROPOSED/HISTORICAL; RF-01/04/06/11 và các số 17 controller/57 action/37 migration là historical checkpoints.
