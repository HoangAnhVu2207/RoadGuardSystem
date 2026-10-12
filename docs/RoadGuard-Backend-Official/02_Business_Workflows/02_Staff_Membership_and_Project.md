# Staff, membership và project

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## EXPECTED_BUSINESS_BEHAVIOR

TARGET_CONFIRMED: Supervisor global trên hành động được chốt; PM/Crew/Operator vẫn project/assignment. Primary PM là effective responsibility, giữ history. Contacts chỉ Supervisor directory/account; roster/primary không email/phone. Cookie không thay business scope.

## Current GET/DTO/actor

| v1 route | Current actor / scope | Response |
|---|---|---|
| GET /users | Supervisor Active/role Active/!MustChangePassword | StaffPageResponseDto<StaffUserResponseDto>, contacts |
| GET /users/{id} | Supervisor qua AccountAsync | StaffAccountResponseDto, email/phone/version |
| GET /projects/{id}/members | Supervisor global hoặc effective PM; history chỉ Supervisor | ProjectMemberResponseDto, không contacts |
| GET /projects/{id}/primary-project-manager | Supervisor global hoặc effective PM | PrimaryProjectManagerResponseDto, nullable member |
| GET /projects | Supervisor all; PM membership; Operator/Crew membership+assignment | ProjectListResponseDto |

Users mặc định Supervisor/PM/Operator/Crew; explicit role=REPORTER source cho phép Reporter. Đây CURRENT_VERIFIED behavior, không quyết định mới Reporter là staff. Users/members search/role/status/limit/cursor; default20,limit1–100. Members status filter chỉ cùng includeHistory. Cursor staff bind actor/scope/filters; projects cursor CreatedAt+Id, không chung format.

## Main flow / alternative / state

Controller parse sub/role→service role gate→repository actor account/role check→project exists→Supervisor hoặc effective PM membership theo UTC day→query scope trước pagination. Active member read yêu cầu Active/ValidFrom≤day≤ValidTo, user Active/current role khớp. Primary chọn latest ValidFrom rồi membership ID trong qualifying PM; thiếu trả wrapper null, không auto appoint.

Operator project list thêm survey assignment chưa ended/rejected/task state whitelist. Crew thêm FIELD assignment Active/chưa ended/task whitelist; native repair current item/binding/crew đúng, không cancelled/superseded. List project không cấp quyền mọi resource.

CreateProjectRequestDto có canonical/legacy mapping, primary PM/handover/warranty/CRS/file/operation;201+ETag. Update dùng expected rowversion/OperationId; primary reassignment effective date/current membership version/OperationId giữ previous/current history. Planning/Active/Closed; service chặn reassignment project closed. Không suy state machine toàn bộ từ tên API.

## Persistence / errors

Reads AsNoTracking. Write repositories giữ transaction/receipt/code uniqueness/membership history. 400 invalid cursor/query,401 principal,403 actor/history/scope,404 project/account,409 write stale/closed/duplicate. Account missing code identity_user_not_found khác StaffDirectory project_not_found. Version response phục vụ concurrency, không substitute current authority.

## Gaps

GET mới/account contacts đã implemented, không đề xuất làm lại. UNKNOWN deployed PII audit/browser E2E. PROPOSED owner xem explicit Reporter filter nếu cần thay behavior; docs không đổi public policy. Unselected tests NOT_RUN; fresh selected outcomes được bổ sung bên dưới.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [StaffDirectoryController.cs](../../../RoadGuardSystem.API/Controllers/StaffDirectoryController.cs) | `StaffDirectoryController` :19 | CURRENT_VERIFIED source |
| [UsersController.cs](../../../RoadGuardSystem.API/Controllers/UsersController.cs) | `UsersController` :18 | CURRENT_VERIFIED source |
| [ProjectsController.cs](../../../RoadGuardSystem.API/Controllers/ProjectsController.cs) | `ProjectsController` :19 | CURRENT_VERIFIED source |
| [StaffReadService.cs](../../../RoadGuardSystem.Services/Implementations/Staff/StaffReadService.cs) | `StaffReadService` :9 | CURRENT_VERIFIED source |
| [StaffReadRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Staff/StaffReadRepository.cs) | `StaffReadRepository` :6 | CURRENT_VERIFIED source |
| [ProjectListService.cs](../../../RoadGuardSystem.Services/Implementations/Projects/ProjectListService.cs) | `ProjectListService` :9 | CURRENT_VERIFIED source |
| [ProjectListReadModel.cs](../../../RoadGuardSystem.Repositories/Implementations/Projects/ProjectListReadModel.cs) | `ProjectListReadModel` :7 | CURRENT_VERIFIED source |
| [PrimaryProjectManagerService.cs](../../../RoadGuardSystem.Services/Implementations/Projects/PrimaryProjectManagerService.cs) | `PrimaryProjectManagerService` :6 | CURRENT_VERIFIED source |
| [PrimaryProjectManagerPersistenceService.cs](../../../RoadGuardSystem.Repositories/Implementations/Projects/PrimaryProjectManagerPersistenceService.cs) | `PrimaryProjectManagerPersistenceService` :13 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [StaffMembershipReadHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Projects/StaffMembershipReadHttpTests.cs)

## Symbol checkpoints

- [StaffReadService.cs](../../../RoadGuardSystem.Services/Implementations/Staff/StaffReadService.cs) :11 — `UsersAsync` (`CURRENT_VERIFIED` source).
- [StaffReadRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Staff/StaffReadRepository.cs) :8 — `CheckAccessAsync` (`CURRENT_VERIFIED` source).
- [ProjectListReadModel.cs](../../../RoadGuardSystem.Repositories/Implementations/Projects/ProjectListReadModel.cs) :9 — `ListAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.

## Central fresh execution update

| Class | PASS | FAIL | Limit |
|---|---|---|---|
| StaffMembershipReadHttpTests | 17 | 0 | Selected actual cases only; no all-route/E2E certification |

Các test khác không trong executed-tests.json vẫn NOT_RUN.
