# Supervisor global project authority + existing GET Projects cookie

Status: ASSIGNED by owner chat 12/10/2026. TARGET_CONFIRMED: ACTIVE Supervisor needs no project membership for existing Supervisor actions on any project; cannot perform PM/Crew/Operator-only commands. Current account/session/role, exact resource/project, state, evidence, concurrency and protected replay still apply. Notifications remain recipient-scoped; no recipient expansion. Writer: Codex for this user-authorized local slice; no simultaneous writer observed. Branch huy-review; base/HEAD b70433c2e19431b4cfd27c3e237a2e8c31ba7497.

Preserve initial 11 dirty/untracked GET Projects files recorded in GET-PROJECTS.md and audit. Only additive method-level cookie metadata in ProjectsController may alter that work. No shared DI changes, migration/seed changes, deployment, commit/push; fixture-owned ephemeral SQL tests only. Existing public endpoint paths/DTOs unchanged; internal correction authority may represent Supervisor role without fabricating membership.

Reserved scope: membership predicates in RepairWorkflowRepository (read/correction/producer), RepairSafetyRepository, RepairEligibilityRepository, ProjectLifecycleRepository.ReadCore, RoadCoverageRepository, DefectStatisticsRepository, CurrentRepairFactsRepository, ReportingRepository.CaseDefect, ExportRepository.SourceAuthority, BusinessDutyRepository.CurrentAuthority; notification Visible project scope and weekly digest ScopeQuery read authority only (recipient filter/dispatch unchanged). BusinessObjects RepairCorrectionAuthority validation only; ProjectsController.List metadata only. Focused tests for these seams and prior tests whose Supervisor-membership denial is superseded; isolated additive Postman collection. No directory/member commands/package GET/new API, role expansion or unrelated refactor.

Plan: trace guards/retain role gates -> regression tests RED -> narrow Supervisor predicate changes -> focused SQL/HTTP including actual web csrf/login cookie without bearer -> both self-reviews/final endpoint-guard table. Cookie method metadata only, bearer/no-auth/CSRF unchanged. Update obsolete test denial triggers to current account/role revocation; keep PM/Crew/Operator membership/assignment denials. Existing notification recipient selection stays unchanged.

Verification: real repository + isolated SQL for approve/correct/final and related reads; real TestServer HTTP web cookie login, bearer, no auth, current session/account revocation, lifecycle/coverage/reporting + recipient boundaries. No full suite loop. Existing migrations used only by owned fixtures, no migration source change or shared DB application. Evidence and exact final counts to append after execution.

## Endpoint → guard → delivered change (CURRENT_VERIFIED source)

P = `/api/v1/projects/{projectId}`; I = `P/repair-packages/{packageId}/items/{itemId}`. No new routes.

| Endpoint/surface | Guard | Change and retained boundary | Source |
|---|---|---|---|
| GET /api/v1/projects | ProjectsController.List / cookie route metadata | Chỉ method GET thêm WebCookieEligible; bearer/no-auth/current session pipeline giữ nguyên. | [ProjectsController.cs:39](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.API/Controllers/ProjectsController.cs:39>) |
| GET I; GET I/history; POST I/corrections | RepairWorkflowRepository.GuardAsync / GuardCommandAsync | Supervisor bỏ membership; current account/role + package/item/effective responsibility project còn nguyên. NORMAL correction ghi CURRENT_SUPERVISOR_ROLE, membershipId null; FT vẫn PM. | [RepairWorkflowRepository.cs:205](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.cs:205>) |
| POST I/approve; POST I/final-confirmation | CurrentProducerAuthority + GuardFinalResource | Bỏ membership khi actual required role là Supervisor. Role equality, mode, duty appointment, plan/evidence/state/item version và guarded receipt giữ nguyên. FT final vẫn PM. | [RepairWorkflowRepository.Producing.cs:686](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Producing.cs:686>) |
| GET I/eligibility | RepairEligibilityRepository.ReadAsync | Supervisor không cần member; scope/binding/source guards còn; Crew vẫn current binding/assignment + membership. | [RepairEligibilityRepository.cs:21](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Repairs/RepairEligibilityRepository.cs:21>) |
| GET I/safety-measures/{measureId} | RepairSafetyRepository.Guard | Supervisor read không cần member; measure/item/package/responsibility source joins còn. Create/installation/check/ack role rules giữ nguyên. | [RepairSafetyRepository.cs:235](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Repairs/RepairSafetyRepository.cs:235>) |
| GET P/lifecycle; POST Supervisor lifecycle actions | ProjectLifecycleRepository.ReadCore | Bỏ member nhánh Supervisor; current user/role/project và action-specific guards giữ. DeclareConstruction/LinkRecurrence chỉ PM. | [ProjectLifecycleRepository.cs:95](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Projects/ProjectLifecycleRepository.cs:95>) |
| GET/POST P/road-coverage | RoadCoverageRepository.Authority | Supervisor read/confirm toàn project; PM read vẫn member và không confirm. Thêm explicit project-exists ->404; scope/source/hash/evidence/replay giữ. | [RoadCoverageRepository.cs:21](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Repairs/RoadCoverageRepository.cs:21>) |
| GET P/defect-statistics | DefectStatisticsRepository.Authority | Supervisor read bỏ member; write vẫn chỉ PM + member. Explicit project-exists; source/filter/version guards giữ. | [DefectStatisticsRepository.cs:22](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Reporting/DefectStatisticsRepository.cs:22>) |
| GET P/reports/summary, items; source capture cho dossier | CurrentRepairFactsRepository + ReportingRepository.CaseDefect | Supervisor đọc repair/case source inventory không cần member; PM vẫn member, project/source identity còn. Không mở thêm full timeline/dossier data. | [CurrentRepairFactsRepository.cs:19](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Reporting/CurrentRepairFactsRepository.cs:19>) |
| POST P/exports; GET P/exports/{id}, manifest/content | ExportRepository.CanReadSnapshotSourcesAsync | Current private source read/replay không buộc Supervisor member; manifest/project/source identity/current account/role giữ. Legacy export scope không mở rộng. | [ExportRepository.SourceAuthority.cs:9](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.SourceAuthority.cs:9>) |
| GET P/receiving-requests[/id]; POST ack/appointment; POST P/clocks/{id}/extensions, appointments | BusinessDutyRepository.CurrentAuthority (dùng bởi ClockDutyRepository) | Supervisor project authority không member; project-exists/current account/role giữ. Ack vẫn đúng responsible actor/role; appointment/extend giữ required role và current source. | [BusinessDutyRepository.cs:24](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/BusinessDutyRepository.cs:24>) |
| GET P/clocks; current authority cho weekly digest | H6ProtectedNotificationRepository.ClocksPageAsync / BusinessDutyRepository | Supervisor project-clock read bỏ member, project-exists/current principal vẫn kiểm. Digest recipient guard giữ. Không biến clock privilege thành quyền nhận mọi notification. | [H6ProtectedNotificationRepository.Operations.cs:39](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Operations.cs:39>) |
| GET notifications[/id], scope; POST notification read | H6ProtectedNotificationRepository.Visible | Supervisor project-scope lấy project hiện có; owned = RecipientUserId == actor giữ nguyên trước union. Recipient selection/dispatch không sửa, không broadcast. | [H6ProtectedNotificationRepository.Scope.cs:12](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Scope.cs:12>) |
| GET own weekly-digest notification/detail | H6WeeklyReviewSourceAdapter.ScopeQuery | Supervisor recipient đọc digest đã được giao không cần member; current account/role/project và đúng recipient giữ. PM vẫn effective membership; explicit role branches tránh so sánh enum varchar/tinyint. Dispatch không đổi. | [H6WeeklyReviewSourceAdapter.cs:40](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Messaging/H6WeeklyReviewSourceAdapter.cs:40>) |

Case-defect capture companion: [ReportingRepository.CaseDefect.cs:24](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.Repositories/Implementations/Reporting/ReportingRepository.CaseDefect.cs:24>). Internal correction domain: [RepairWork.cs:366](<D:/Project BE/RoadGuardSystem/RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs:366>). Scoped reservation includes notification Operations.cs clock membership predicate, and regression tests at H6ProjectLifecycleSourceSqlTests/H4SafetyRuntimeSqlTests. No edits to dispatcher or recipient candidate selectors.

Already Supervisor-global on inspected seams, unchanged: ProjectScopeGuard-based project/GIS/FIELD reads and geometry confirm, project create/update/reassign, CaseWorkflow authority, retention, export outer authority. Existing PM/Crew/Operator required-role gates are not widened. PM-only repair create/propose/assign/review/cancel/normal successor/review request and policy publish remain their existing role rules; FT final/correction stay PM. Notification deliveries may still target explicitly selected current recipients; global permission does not enroll every Supervisor.

## Focused verification — executed 12/10/2026

No new production seed/migration, shared database writes, deployment, commit/push. Tests use existing fixture-owned ephemeral SQL databases and existing migration setup; no staging database touched. Actual HTTP uses production DI/authentication/authorization and real SQL with Testing WebApplicationFactory; no forged identity/bearer substituted for cookie login. Not a running staging binary/browser/Postman acceptance.

RED: new no-membership SQL reads/approve/correction failed with 403 (3 regression failures); actual csrf/web-login cookie GET Projects failed 401, own recipient notification failed 404, lifecycle read failed 403 (3 HTTP regression failures). Initial test compile typos fixed before behavioral RED. Old Supervisor-membership denials are superseded; updated to active account/role revocation, or PM membership tests where membership remains required. Initial full analyzer-enabled compilation emitted existing analyzer warnings; final focused commands use `--no-restore -p:RunAnalyzers=false`, without suppressing compiler errors.

GREEN distinct focused tests: **91 API + 74 SQL integration + 75 unit = 240 PASS, 0 FAIL, 0 SKIP**. Counts do not add overlapping reruns. Two strengthened SQL fixtures and one HTTP fixture were re-executed separately after adding explicit no-membership assertions; results: 2/2 SQL and 1/1 HTTP PASS (overlapping tests, not added to distinct total). No full-suite loop.

Executed commands (all with `--logger 'console;verbosity=minimal' -p:RunAnalyzers=false --no-restore`):
- `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter 'FullyQualifiedName~SupervisorGlobalAuthorityHttpTests|FullyQualifiedName~ProjectListTests|FullyQualifiedName~LD07RoadCoverageHttpTests|FullyQualifiedName~H6ProjectLifecycleHttpTests|FullyQualifiedName~H4RepairProducerHttpAdmissionTests|FullyQualifiedName~H4RepairExecutionHttpAdmissionTests|FullyQualifiedName~H4RepairSafetyHttpAdmissionTests|FullyQualifiedName~H6NotificationOperationsHttpTests|FullyQualifiedName~HuyFinalCookieHttpTests'` — 91/91.
- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter 'FullyQualifiedName~H4RepairProducingAdmissionSqlTests|FullyQualifiedName~H4CorrectionAuthorityTests|FullyQualifiedName~GlobalSupervisorWithoutMembershipConfirmsNormalRepair|FullyQualifiedName~GlobalSupervisorReportIncludesCurrentRepairSource|FullyQualifiedName~LD07RoadCoverageSqlTests|FullyQualifiedName~H6ProjectLifecycleSourceSqlTests|FullyQualifiedName~LD06LifecycleSqlTests|FullyQualifiedName~H4SafetyRuntimeSqlTests|FullyQualifiedName~H4RepairLifecycleSqlTests'` — 69/69.
- `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter 'FullyQualifiedName~H4RepairFoundationTests|FullyQualifiedName~H4ObligationContinuationTests|FullyQualifiedName~AuthoritativeSessionValidatorTests'` — 75/75.

Evidence: ACTIVE Supervisor with no membership reads foreign project/list/item/history/source inventory and approves NORMAL; complete genuine Reporter→repair→FIELD→PM review→global Sup final/correction runs with durable heads/receipts. Resource/project mismatches, stale item/project hashes, wrong state, PM-only calls and FT-vs-NORMAL authority still deny. PM expired/future/ended membership and Crew membership/current assignment remain enforced; Operator/Crew project-list assignments and current binding tested by preserved ProjectListTests. Suspended account/current role and revoked sessions deny; protected replay/conflicting key and acknowledgement-loss recovery deny after current authority loss. Cookie GET has no Authorization; real csrf/login and automatic cookie jar; bearer 200, no-auth 401, revoked cookie/bearer401; method-level metadata does not opt project POST into cookies. Two Supervisors each see only own notification; cross-recipient GET/read and replay target reject. Export private source checks current account/project/existing source, including false source IDs and PM lost membership.

## Self-review and preservation

Self-review 1: final diff against owner decision; retained role equality before membership bypass, resource/effective responsibility project, state/evidence/version, transaction locks and receiptAccessGuard. Found and fixed missing explicit project existence after membership bypass in coverage/statistics and related read authority. Correction membership is nullable only for explicit current Supervisor role fact; audit stores null member + CURRENT_SUPERVISOR_ROLE, no fabricated member or ID.

Self-review 2: diff/file scope, consumer/compatibility, transport and privacy. Original 11 GET Projects files byte-preserved except adding one method-level cookie attribute (verified by stripping that line and comparing original SHA-256); both prior DI registrations and all original untracked files unchanged. Cookie auth selector/global allowlist unchanged. Notification `owned`/recipient predicates and dispatch/candidate routing untouched. Existing full-dossier limitations unchanged; Postman has 3 GET requests, no body/credentials, blank variables, explicit bearer vs cookie vs no-auth cookie-jar behavior. `git diff --check` clean. Schema/seed/deploy/commit/push untouched. Browser/external providers/running staging deployment NOT VERIFIED.

External read-only code review: completed separately from the two self-review passes. Initial findings: weekly digest read authority still required membership, and redundant project-existence predicates. Fixed both; reviewer rechecked and reported no actionable concerns, including the explicit role-storage branches. Source-only external review; runtime evidence is the focused tests below. TestHost/SQL is BE verified; no external AI/storage/mobile acceptance claimed. Postman collection import/manual execution NOT RUN.


Final reviewer regression: own delivered Supervisor weekly digest failed both theory cases without membership before ScopeQuery fix. SQL execution then exposed a cross-column enum storage mismatch (User.RoleCode varchar vs digest.RecipientRole tinyint); explicit constant role branches fixed it without schema changes. One existing missing-reviewer test used a fixed Monday before its real-clock intake origin; fixture recovery now derives the next weekly boundary from that origin. Production scheduler/dispatch remains unchanged.

Final scoped reruns:
- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter 'FullyQualifiedName~OwnerWeeklyRecoverySqlTests|FullyQualifiedName~GlobalSupervisorRelatedReadsWorkWithoutMembershipWhilePmStillNeedsMembership' --no-restore --logger 'console;verbosity=minimal' -p:RunAnalyzers=false` — 6/6 PASS: 5 weekly recovery cases + 1 overlapping related-read case. The 5 weekly cases add to the original 69 SQL cases.
- `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter 'FullyQualifiedName~SupervisorGlobalAuthorityHttpTests|FullyQualifiedName~LD07RoadCoverageHttpTests.ProductionDiRequiresSupervisorExactSourceScopeVersionAndCurrentAuthorityBeforeReplay' --no-restore --logger 'console;verbosity=minimal' -p:RunAnalyzers=false` — 4/4 PASS after initial digest scope change, overlapping prior API tests.

Postman static verification: 3 GET requests, no request bodies, blank baseUrl/supervisorToken, cookie request uses noauth, download copy identical to repository collection. Manual import/run not performed. Final preservation SHA-256 check passed for all original 11 files (ProjectsController excluding only the added method-level attribute). HEAD unchanged.


Initial GET Projects files preserved:
- `RoadGuardSystem.API/Controllers/ProjectsController.cs`
- `RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs`
- `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs`
- `RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs`
- `RoadGuardSystem.Repositories/Implementations/Projects/ProjectListReadModel.cs`
- `RoadGuardSystem.Repositories/Interfaces/Projects/IProjectListRepository.cs`
- `RoadGuardSystem.Services/Implementations/Projects/ProjectListService.cs`
- `RoadGuardSystem.Services/Interfaces/Projects/IProjectListService.cs`
- `planning/development/GET-PROJECTS.md`
- `tests/RoadGuardSystem.ApiTests/Projects/ProjectListTests.cs`
- `tools/add-project-list-postman.py`

Task-local changed/new paths (alongside the additive ProjectsController GET attribute):
- `RoadGuardSystem.BusinessObjects/Repairs/RepairWork.cs`
- `RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.SourceAuthority.cs`
- `RoadGuardSystem.Repositories/Implementations/Messaging/BusinessDutyRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Operations.cs`
- `RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Scope.cs`
- `RoadGuardSystem.Repositories/Implementations/Messaging/H6WeeklyReviewSourceAdapter.cs`
- `RoadGuardSystem.Repositories/Implementations/Projects/ProjectLifecycleRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RepairEligibilityRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RepairSafetyRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Producing.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RoadCoverageRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Reporting/CurrentRepairFactsRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Reporting/DefectStatisticsRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Reporting/ReportingRepository.CaseDefect.cs`
- `tests/RoadGuardSystem.ApiTests/Projects/H6ProjectLifecycleHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Repairs/LD07RoadCoverageHttpTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Notifications/OwnerWeeklyRecoverySqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Projects/H6ProjectLifecycleSourceSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4CorrectionAuthorityTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairCoreMigrationTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairExecutionSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairProducingAdmissionSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4SafetyRuntimeSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/LD07RoadCoverageSqlTests.cs`
- `docs/postman/RoadGuard-Supervisor-Projects.postman_collection.json`
- `planning/development/SUPERVISOR-GLOBAL-PROJECT-AUTHORITY.md`
- `tests/RoadGuardSystem.ApiTests/Projects/SupervisorGlobalAuthorityHttpTests.cs`
