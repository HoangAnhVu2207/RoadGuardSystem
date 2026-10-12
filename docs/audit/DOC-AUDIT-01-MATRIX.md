# DOC-AUDIT-01 — Evidence matrix

**ACTIVE**, `f0c8762509310b2779068891062267c5f520aff2` / `huy-review`, 2026-10-12. Các paths controller/service/repository trong bảng là repository-relative dưới đúng project prefix: `RoadGuardSystem.API/Controllers`, `RoadGuardSystem.DTOs`, `RoadGuardSystem.Services/Implementations`, `RoadGuardSystem.Repositories/Implementations`, `RoadGuardSystem.BusinessObjects`, `RoadGuardSystem.Repositories/Configurations` và `Migrations`. Tên symbol/partial/line dùng source baseline này.

Requirement authority: confirmed32–44, latest assigned R01–R30/ND01 và three additive specs. FR/BR/UC cũ chỉ là traceable historical references; không invent ID hoặc nâng historical scope thành accepted. Test columns lấy fresh TRX tại source baseline; TEST_VERIFIED chỉ cho điều kiện exercised, không toàn bộ product. Một row partial có passing tests vẫn partial ở nhánh thiếu. NOT_RUN sources không là PASS.

Document paths dùng dưới `docs/` trừ explicit `planning/`/`contracts/`. Database evidence gắn full191-table dictionary/current2migrations, không deployed SQL. [Report](DOC-AUDIT-01-REPORT.md), [gaps](DOC-AUDIT-01-OPEN-GAPS.md), [API](DOC-AUDIT-01-API.md), [data](../backend/data/audited-data-model.md), [evidence](DOC-AUDIT-01-EVIDENCE.json) giữ commands/counts/full-path catalogs.

| Module | Requirement FR/BR/UC hoặc accepted decision | Document | Code evidence Controller/DTO/service/repository | Database evidence entity/config/migration | Test evidence + executed result | Status | Gap | Action | Severity |
|---|---|---|---|---|---|---|---|---|---|
| Identity | PR36A/37; H1 persistent login | product/confirmed-decisions; ADR002; V2 auth | `AuthController`, `WebAuthController` → Authentication DTOs/`AuthService`/`AuthoritativeSessionValidator` → identity/session repository; `WebCookieConfiguration.cs:18–29,94–134` selector/current server validation | `ApplicationUser`, `UserSession`, `RefreshToken`, identity/session configurations; baseline + current snapshot | `H1PersistentIdentitySqlTests`: 3 Passed; `V2AccountFlowTests`: 5 Passed | TEST_VERIFIED | Actual browser/proxy/replica E03 remains pending | Document route opt-in/bearer precedence/session checks; no cookie-global change | P2 |
| Identity/Reporter OTP | PR37; registration UC reference historical | product/confirmed-decisions; product/workflows | `ReporterRegistrationsController` → Register/Verify/Resend DTOs → `IdentityOnboardingService.cs:50,123,163` → `IdentityOnboardingRepository.cs:36,104,182`; options10min/5attempt/60s/3per15min | `ReporterRegistrationIntent`, config/indexes, baseline:822 | `V2IdentityOnboardingFlowTests`: 4 Passed | TEST_VERIFIED | Real mail E03, not verified by fake sender | Keep OTP scope and external delivery limits | P2 |
| Projects discovery | GET-PROJECTS owner spec | planning/development/GET-PROJECTS.md; API inventory | `ProjectsController.cs:38–64` → `ProjectListResponseDto` → `ProjectListService.cs:11–43` → `ProjectListReadModel.cs:11–103`; Supervisor global, PM membership, Crew/Operator current assignments | Project/ProjectMember/SurveyAssignment/FieldInspectionAssignment/binding configurations; no new migration for list | `ProjectListTests`: 55 Passed | TEST_VERIFIED | Already solved in cb612bd; not project detail/version endpoint | Document default20, positional CreatedAt+Id cursor and fresh scope reapplication | P2 |
| Staff directory | STAFF-MEMBERSHIP-READ §GET users | planning/development/STAFF-MEMBERSHIP-READ.md; staff Postman guide | `StaffDirectoryController.cs:22–30` → `StaffReadResponses.cs` → `StaffReadService.cs:10–27` → `StaffReadRepository.cs:25–36`; Supervisor-only/all internalstatuses/Reporter explicit | ApplicationUser/UserRole/rowversion; existing SQL, no schema delta | `StaffMembershipReadHttpTests`: 17 Passed | TEST_VERIFIED | Already solved in f0c8762; live Postman E03 pending | Document nullable contact/userId/role/filter bound cursor; no credential body | P2 |
| Membership roster | STAFF-MEMBERSHIP-READ §GET members | Staff-Membership-Read-Guide; V2 auth reconciliation | `StaffDirectoryController.cs:33–43` → member DTO → `StaffReadService.MembersAsync:40–62` → `StaffReadRepository.MembersAsync:42–63`; Supglobal/PM nonprimary current effective membership; history Sup | ProjectMember/User configs; membership identity+RowVersion; UTC effective dates inclusive | `StaffMembershipReadHttpTests`: 17 Passed | TEST_VERIFIED | Already solved; roster no contact, snapshots vs current role differ | Keep exact membership rows/version, no fake membership or new membership commands | P2 |
| Primary PM | STAFF-MEMBERSHIP-READ / GET primary | Staff guide; API inventory | `StaffDirectoryController.cs:46–52` → nullable primary DTO → `StaffReadService.PrimaryAsync:65–76` → `StaffReadRepository.PrimaryAsync:65–75` one joined current head; existing `ProjectsController.cs:67–117` PUT consumes membership version | ProjectMember config filtered unique IsPrimary/status index:38–40; rowversion existing | `StaffMembershipReadHttpTests`: 17 Passed; `ProjectListTests`: 55 Passed | TEST_VERIFIED | Already solved; valid project/no head is200/null; same-day PUT rule unchanged | Do not use account/project ETag for ExpectedCurrentMembershipRowVersion | P2 |
| Account contacts/privacy | STAFF read; latest Supervisor decision | Staff guide; V2 data/auth | `UsersController.cs:34–63` → dedicated StaffAccountResponseDto/ETag → `StaffReadService.AccountAsync:29–38` → StaffReadRepository.AccountAsync; `/me`/PATCH shared response unchanged | ApplicationUser Email/Phone/RowVersion; no new columns | `StaffMembershipReadHttpTests`: 17 Passed; `V2AccountFlowTests`: 5 Passed | TEST_VERIFIED | No field gap remaining; roster/primary still omit contacts | Describe separate read projection, no blanket PII exposure | P2 |
| Supervisor authority | SUPERVISOR-GLOBAL-PROJECT-AUTHORITY; R29 | Supervisor spec; backend README; V2 auth | Supervisor branches in RepairWorkflowRepository/Eligibility/Safety, ProjectLifecycleRepository, RoadCoverageRepository, reporting/export/clock/business duties. Exact required role/current account before membership exemption; policy/assign/execute PM/Crew remain role-specific | Existing account/role/resource/project/scope/binding/version columns; no memberships fabricated | `SupervisorGlobalAuthorityHttpTests`: 3 Passed; `H4RepairWorkflowHttpAdmissionTests`: 6 Passed | TEST_VERIFIED | Already solved; test evidence limited to selected cases, not every endpoint E2E | No Supervisor impersonation, keep resource/state/evidence/replay guards | P2 |
| Cookie compatibility | PR36A; GET projects/staff specs | V2 auth; Staff guide | Method WebCookieEligible on Projects.List/Staff GETs/Users.Get; WebCookieConfiguration selector auth precedence; AuthoritativeSessionValidator and WebCookieRequestMiddleware retain current identity/CSRF | UserSession transport/revoked/expiry/account status; existing schema | `SupervisorGlobalAuthorityHttpTests`: 3 Passed; `StaffMembershipReadHttpTests`: 17 Passed | TEST_VERIFIED | Cookie real web login in TestHost/HTTPS, not actual browser/proxy E03 | Keep bearer/noauth/invalid/revoked results and method-only opt-in | P2 |
| Geometry/survey | PR39A/43A; H2/LD07; FR/UC historical references only | V2 overview/data; ANH local contract | GeometryWorkflow/PavementWorkflow/SurveyV2/SurveyAssessments controllers → domain DTO/services → geometry/pavement/survey repositories; immutable published sets/CRS/pavement and assigned dataset bindings | RoadSectionVersion/RoadSegmentSet/RoadSegment/CRS/pavement/survey mappings; baseline + additive | NOT_RUN targeted geometry/survey suites in this audit; source + owned schema inspected | IMPLEMENTED_SOURCE_VERIFIED | Official CRS/control points/real coverage E04 | Document implemented routes, keep sample/test-only/accuracy distinction | P1 |
| Upload/storage | PR38 | product/data-and-quality; backend/data mapping | UploadsController → CreateUpload/Complete DTOs → UploadService:80 → upload persistence/recovery/MinioUploadObjectStorage; UploadAdmissionPolicy8GiB/32GiB; long sizes | StoredFileConfiguration:27 bigint; UploadSession ExpectedSizeBytes; upload/file scopes/parts and retention constraints | `ReporterEvidenceApiTests`: 7 Passed; `Anh02AiHttpTests`: 1 Passed | TEST_VERIFIED | Selected private/synthetic flows only; no8GiB live transfer E03 | Remove int.MaxValue video-absence claim, retain physical streaming acceptance gate | P2 |
| Report intake/supplement | R26/R29; Reporter UC historical | product/workflows; backend README | ReportsController Create/List/Read/Supplement → ReporterReportDtos → ReporterReportService/ReporterLifecycleService → ReporterReportRepository:15–38/ReporterLifecycleRepository:46–116; immutable evidence + one unassigned case | Report/ReportEvidence/ReportSupplement/HuyCaseReportLink (SQL CaseReportLinks); Huy01IntegrationConfiguration:39–93; baseline:852/1724 | `Huy01ReporterReportsApiTests`: 17 Passed; `Huy01ReporterReceiptAccessSqlTests`: 27 Passed | TEST_VERIFIED | Storage/provider E03; source changes during download G03 separate | Describe ownership, reload/read/version and guarded receipt | P2 |
| Private evidence download | R29 privacy/current access | product/data-and-quality; API inventory | ReporterEvidenceController → ReporterEvidenceService.DownloadPublicationAsync:160/DownloadAsync:174 → IUploadRepository → UploadPersistenceService.GetFileMetadataAsync:404/OpenFileAsync:443; ReporterLifecycleService.DownloadAsync serves report-scoped download; reporter persistence checks active Reporter, not storage opening | Private REPORT_PHOTO owner/no project target; StoredFile/UploadSession metadata/checksum | `ReporterEvidenceApiTests`: 7 Passed | NOT_VERIFIED | G03 post-open current-rights risk not reproduced | Separate passing static owner tests from proposed delayed-open race verification | P1 |
| Case triage/link/split | R18/R29; FR/UC refs historical | product/workflows; V2 process | CasesController → case DTOs → CaseWorkflowService.CommandAsync → CaseWorkflowRepository:17–44,85–147 locks/closes/appends links; strong versions/guarded idempotency | IncidentCase/history/active HuyCaseReportLink (SQL CaseReportLinks) immutable source; Huy01IntegrationConfiguration:93/139/192 | `Huy01CaseSqlTests`: 22 Passed; `Huy01ReporterReportsApiTests`: 17 Passed | TEST_VERIFIED | No blanket missing case APIs; deployed clients E03 | Keep transactional split/link and private report/publication distinction | P2 |
| Case conclusion/publication | R10/R18/R26 | product/workflows; V2 process | CasesController.Conclude/Publish → workflow service → CaseWorkflowRepository:150–191 ExistingEvidence final, NeedsEvidence; FIELD/DRONE source_not_ready; recipient projection in ReporterLifecycleRepository:97–116 | Case conclusion/publication recipient/evidence tables and pinned defect head; Huy01IntegrationConfiguration:205–286 | `Huy01CaseSqlTests`: 22 Passed; `Huy01ReporterReportsApiTests`: 17 Passed | PARTIALLY_IMPLEMENTED | G05 explicit pre-Defect producer gate; privacy ExistingEvidence path verified | No fake source; document exact solved branch and remaining gate | P2 |
| Candidate matching | PR33A | product/requirements; AI README | CandidatesController → CandidateMatchingService:22–105 → source/target repository facts; CandidateMatcher scope/neighbor/history; call88–90 null distance/false GPS | Candidate source/geometry snapshot/current target/version; Huy01IntegrationConfiguration:152/229/241 | Candidate matcher test sources exist; targeted matcher suite NOT_RUN; integrated candidate HTTP within Huy01ReporterReportsApiTests ran | PARTIALLY_IMPLEMENTED | G01 GPS/accuracy/image ranking not connected | Minimal future method/facts integration, no invented numerical threshold | P1 |
| Candidate decisions/labels | PR33A/PR34A | product/workflows; contracts local | Candidates/TrainingLabelsController → decision/label DTO/services → CandidateDecisionRepository source locks + save decision/reject; TrainingLabelRepository.ReadApprovedFactsAsync joins current revision/review | CandidateDecision/Label revision/review/source, immutable/current head constraints | `Huy01ReporterReportsApiTests`: 17 Passed | TEST_VERIFIED | Actual AI training consumer E01 pending | Explicit human decision, approved-only projection; do not equate candidate with Defect acceptance | P2 |
| Legacy processing/callback | PR44; async boundary | contracts/events; V2 AI integration | ProcessingV2Controller:28–54 → Create/Retry/ReceiveAiResult DTOs → ProcessingV2Service → ProcessingV2PersistenceService:35–148; dedicated AiCallback scheme client_type=AI_SERVICE | ProcessingJob/Attempt/AIModelVersion/AIDetection/verified raw file configurations; baseline5354/5390 | Legacy callback identity/characterization tests source inspected, NOT_RUN in fresh audit | PARTIALLY_IMPLEMENTED | G02 latest attempt fence/idempotency binding; E01 live provider | Document legacy checks separately from synthetic worker, keep real integration scope | P1 |
| ANH02 AI provenance | PR44/34A | AI README; contracts/ai fixtures | AiMockRunsController → Anh02AiDtos → Anh02AiService:29,89,104–122,147–196,219–260 → Anh02AiRepository:98,172,186,230,238; immutable manifest/result/frame + synthetic source stream | Anh02AiProvenance/config rowversion/unique result/immutable triggers; Processing model/attempt; baseline3585/3653 | `Anh02AiHttpTests`: 1 Passed | TEST_VERIFIED | BE MOCK only; live model/FastAPI/storage E01/E03 | Label SYNTHETIC/MOCK prominently, no physical AI measurements | P2 |
| Policy version/revoke | R05–R07 | V2 rules/current workflows | RepairPoliciesController:17–33 → RepairPolicyDtos/RepairPolicyService → RepairPolicyRepository.ExecuteAsync:20–137; PM-only, exact draft/version/publish/revoke; eligibility source pins | RepairPolicyDraft/Revision/Revoke/RepairPolicySafetyConfiguration; baseline current | `H4RepairPolicySqlTests`: 1 Passed; `H4RepairPolicyHttpTests`: 1 Passed; `H4PolicySafetyTests`: 12 Passed | TEST_VERIFIED | Real policy/source acceptance E04, TEST_ONLY not executable permission | No Supervisor policy approval/default3mm1m, preserve immutable pin/revocation | P2 |
| Repair eligibility | R03/R04/R06/R11 | V2 rules; product/workflows | RepairEligibilityController → eligibility DTO/service → RepairEligibilityRepository.ReadAsync + RepairFtEligibility.Missing; pinned policy/handover/coverage/location/checklist/measurement conditions | Repair producer source/assessment/authorization configs; exact resource/binding relations | H4RepairProducerSourceSqlTests NOT_RUN; workflow admission/unit proof inspected; schema fresh | IMPLEMENTED_SOURCE_VERIFIED | Executable real handover/coverage/CRS facts E04 | Do not grant execute from threshold or measure-only mode | P1 |
| Normal approve/assign/final | R02/R14/R29 + Sup global | product/workflows; Supervisor spec | RepairPackagesController:19–36 → producer service/admission → Producing.cs:488,517,537,90,33; required role guards686–694, mode-specific final734–741; PM review separate Supervisor normal final | RepairPackage/Item/obligation/proposal/decision/binding/evidence/version; RepairWorkflowConfiguration | `H4RepairWorkflowHttpAdmissionTests`: 6 Passed; `H4RepairExecutionSqlTests`: 21 Passed | TEST_VERIFIED | Selected actual normal chain only; not all resource state combinations runtime | Keep no membership exemption outside Supervisor branch, use correct item/plan versions | P2 |
| Crew measure/BEFORE | R11/R12 | V2 data; product/workflows | RepairExecutionController.Assess → assessment/execution service → Execution.cs:18–63 + FieldInspectionWorkflowRepository.RepairCapture.cs:30–114; genuine current session/measurements/uploader/reuse source | GroundTruthMeasurement/RepairMeasurementAssessment/AssessmentEvidence/FieldEvidenceReuse config | `H4RepairExecutionSqlTests`: 21 Passed; `H4PolicySafetyTests`: 12 Passed | TEST_VERIFIED | Real field/camera/storage E02/E03 pending | Known0 vs UNKNOWN; require current exact scope and immutable evidence | P2 |
| Physical start/finish/formal attempt | R03/R08/R13/R15 | product/workflows; V2 sequences | RepairExecutionController:19–42 → execution DTO/service → Execution.cs:65,172; Producing.cs:337–426 actual H3 root/start/finish/attempt; AFTER reviewed on90–256 | RepairExecutionStart/Finish/Attempt/SubmissionLink/AttemptEvidence/Review; producer configuration | `H4RepairExecutionSqlTests`: 21 Passed; `H4RepairWorkflowTests`: 16 Passed | TEST_VERIFIED | After evidence requires fresh capture/upload and accepted state; device claimed time uncertain | Do not call physical finish acceptance/formal intake/Defect close | P2 |
| FT original clock/ND01 | R08/R09; latest ND01 supersedes earlier defer | HUY-FINAL latest ND01; offline guide | RepairFirstStart.cs:12–66 immutable first start/clock; Execution.cs:103–145 independently proven original window/current admission/final prewrite expiry; offline signed proof | RepairExecutionAuthorization/DeadlineClock exact original window, append-only history | `ND01OriginalWindowSqlTests`: 22 Passed; `ND01OriginalWindowHttpTests`: 1 Passed | TEST_VERIFIED | Additional-after-expiry deferred; uncertain claimed device time not verified actual execution | Never reset first origin on reopen/remeasure/retry/sync | P2 |
| Rework/supplement | R14/R15/R28 | product/workflows; contracts/events | Producing.cs:211–224 real supplement request+event;258–335 append submission link without replacing root/review origin; H6 adapter143–197 immutable proof | RepairAttemptSubmissionLink/Review/RepairItemLifecycleEvent + business duty/source outbox | `H4RepairExecutionSqlTests`: 21 Passed; `H4RepairWorkflowTests`: 16 Passed; `H6RepairLifecycleSourceSqlTests`: 3 Passed | TEST_VERIFIED | Notification is not business ACK | Keep history, original obligation/root/clock; append correction/supplement | P2 |
| Cancel/normal successor | R15/R17/R20 | product/workflows; local contract | RepairLifecycleController:19–24 → lifecycle DTO/service → Lifecycle.cs:17–188 PM-only retire guards/real safety handover/new normal successor/fresh approval | RepairNormalSuccessor/obligation/lifecycle event/task assignment; RepairWorkflowConfiguration | `H4RepairLifecycleSqlTests`: 12 Passed; `H4RepairLifecycleDomainTests`: 4 Passed | TEST_VERIFIED | No retire submitted/finished history; cancel not resolved defect | Use existing business POST commands, no universal DELETE CRUD | P2 |
| Mixed obligations/correction | R16/R17/R19 | product/workflows; V2 states | Producing.cs:428–486 physical scope reservation; RepairPackage.IsComplete; RepairWorkflowRepository:28–131 correction exact authority/current head/audit/outbox and effective obligations | RepairScopeReservation/ObligationResolution/Decision/correction/history/current head + Restrict mappings | `H4RepairWorkflowTests`: 16 Passed | IMPLEMENTED_SOURCE_VERIFIED | CorrectionAuthority SQL NOT_RUN; effective statistics/export consumer sources inspected only | FT PM vs normal Supglobal, keep immutable original export vs current facts | P2 |
| Safety/notifications | PR35A/R28 | contracts/events; current workflows | RepairSafetyController/service/repo exact role/method/checklist/no closure; producing source + H6 safety/deadline/lifecycle adapters recipient scope | Safety authorization/evidence/deadline/source/audit/outbox/recipient proof tables | `H4SafetyRuntimeSqlTests`: 5 Passed; `H6RepairLifecycleSourceSqlTests`: 3 Passed | TEST_VERIFIED | Real physical safety/storage E02/E03 | Safety does not close defect or replace normal acceptance | P2 |
| FT-final notification | R03 | contracts/events; current backend README | Producing.cs ConfirmFinalAsync:33–88 → RepairItem.Confirm:346–356; registered type/adapter but no source production; H6 source adapter259–260 PENDING_PRODUCER | Catalog/NotificationRegisteredTypes existing; absent trusted immutable FT-final information source | No actual final→dispatch acceptance found/run; catalog tests do not establish producer | PARTIALLY_IMPLEMENTED | G04 accepted informational behavior missing producer/adoption | Bounded future producer/proof/recipient transaction tests, no business authority change | P2 |
| Curing/traffic opening | R01 supersedes historical BR/UC | V2 process/rules/domain reconciliation | No curing/release management controller/source required by current R01; historical target not current implementation requirement | No schema required by audit | NOT_RUN; scope authority comparison only | HISTORICAL | Not a missing endpoint/current feature | Preserve historical design, explicitly apply latest R01 | P2 |
| Offline device/snapshot | R20/R22/R29; PR37/43A | offline guide; contracts/events | OfflineWorkflowController:18–39 → workflow DTO/service → OfflineWorkflowRepository.RegisterDevice:299/Revoke:141/Snapshot:186/GuardSnapshot:275 → actual FIELD/repair snapshot adapter | OfflineDevice/Revocation/Snapshot + OfflinePersistenceConfigurations:38–56; Repair policy snapshot relations | `H5OfflineHttpAdmissionTests`: 4 Passed; `H5OfflineRepairSqlTests`: 2 Passed | TEST_VERIFIED | Device key custody/task download actual phone E02 | Current assignment/device owner/role/revocation; downloaded policy not general execute permission | P2 |
| Signed sync/shared core | R29/R30 | offline guide; local contract | OfflineWorkflowController:41–43 → OfflineWorkflowEngine finite typed Describe → Sync.cs:25 ProcessBatch/175 ApplyItem → FIELD ApplyInternalInTransaction or repair ApplyInTransaction | Registered source/signature/snapshot/binding/origin/effect/canonical hash/admission ACK configs | `H5OfflineRepairSqlTests`: 2 Passed; `H5OfflinePositiveHttpTests`: 1 Passed; `H5OfflineEnvelopeTests`: 7 Passed; `H5OfflineAcceptTests`: 2 Passed | TEST_VERIFIED | Actual Android signing interoperability E02 | Finite command kinds, original actor separate current caller; no fake success ACK | P2 |
| Replay/conflict/recovery | R29/R30 | offline guide; product/workflows | Sync.cs:175 proof before durable result/268 predecessor/307 failure/352 caller transaction; Transfer.cs:475 status/502 reconcile; controller75–85 batch/origin reads | Origin/effect and batch/admission unique indexes112–113/159/176, receipt+effect same transaction | `ND01OriginalWindowSqlTests`: 22 Passed; `H5OfflineEnvelopeTests`: 7 Passed; GET batches/origins and POST starts/reconcile focused execution NOT_RUN | IMPLEMENTED_SOURCE_VERIFIED; exercised retry/rollback TEST_VERIFIED | Actual Android queue persistence E02 | Per-item commits, dependencies pending, keep unacknowledged/conflicts after reload/timeout | P2 |
| Package/grant/import | PR42A; latest R21 | offline guide; current local contract | OfflineWorkflowController45–73 → transfer service → Transfer.cs:166 register/227 grant/272 revoke/90 GuardImport → original actor typed core; source export vs revocation distinguished | OfflinePackage/Grant/Revocation/RecipientKeyWrap/Endorsement/ImportAdmission, immutable scope/hash relations | `H5OfflinePositiveHttpTests`: 1 Passed; `H5OfflineCryptoTests`: 30 Passed; `H5OfflineRepairSqlTests`: 2 Passed | TEST_VERIFIED | Server signature/integrity fixture not actual recipient decryption E02 | Keep latest scoped recipient authority; no organization escrow or impersonation | P2 |
| Capture/upload provenance | R12/R20/R21/R29 | offline guide; V2 data | Controller87–93 → Transfer.RegisterArtifact:290/ReadTransfer:332 → OfflineAdmissionValidator.ResolveEvidence:304/GuardUpload:476,546/Grant:592,604 + UploadPersistenceService.Offline | OfflineCaptureArtifact/declared evidence/file uploader/purpose/checksum/task-capture immutable constraints204–247 | `H5OfflineRepairSqlTests`: 2 Passed; `H5OfflinePositiveHttpTests`: 1 Passed for upload/original declaration provenance; artifact register/get focused execution NOT_RUN | IMPLEMENTED_SOURCE_VERIFIED; upload provenance TEST_VERIFIED | Real camera/media/interrupt E02/E03 | Preserve actual uploader and original declaration; grant not general file read | P2 |
| Notification own recipient | R28/R29; latest notification rule | contracts/events; V2 auth | NotificationsController/H6 operations → service → NotificationPersistenceService/H6 dispatch/read proof; current recipient and source checks remain, global Sup does not read others | Notification RecipientUserId + sidecar current source/occurrence/receipt; recipient indexes/append-only proof | `SupervisorGlobalAuthorityHttpTests`: 3 Passed; `H6ProtectedInboxSqlTests`: 10 Passed; `H6NotificationOperationsHttpTests`: 1 Passed | TEST_VERIFIED | Production delivery/client read E03; FT-final G04 | Keep recipient-scoped list/get/read, source proof and read/business ACK separation | P2 |
| Lifecycle/coverage/statistics/export | R18/R19/R26/R27; LD06–08 | backend runbook; current workflows | ProjectLifecycle/RoadCoverage/DefectStatistics/Reporting/Exports controllers → services → lifecycle/coverage/statistics/report/export repositories; global Supervisor branch retains resource/obligation/source | Project lifecycle/history/transfer, coverage publication/source pins, statistics/extraction and export snapshots; fresh dictionary | Targeted lifecycle/coverage/stats/export suites NOT_RUN in audit; selected report tests cover source intake through operational closure | IMPLEMENTED_SOURCE_VERIFIED | Real sources/period formulas E04; deployment E05 | Do not repeat missing routes; do not call unverified KPI formulas accepted | P1 |
| Retention/hold | PR41A | backend persistence; product quality | RetentionController → retention service/repository → inventory adapters/obligations/basis/holds/evaluate; unknown basis fail-closed | Retention/hold/basis/export/FileScopes + warranties/repair source relationships; baseline | Targeted retention runtime suites NOT_RUN; code/model/catalog inspected | IMPLEMENTED_SOURCE_VERIFIED | No physical deletion/production retention proof E05 | Keep obligations/hold/no date-from-upload; no deletion authorization | P2 |
| Physical database | RG-CI-01 baseline; actual schema | backend/data/audited-data-model.md | RoadGuardDbContext + Configurations; migrations baseline20261008171739/additive20261009043010 + snapshot | 191 tables/1940columns/662FK/693indexes/229checks/189triggers; fresh extractor and input hashes | `Rf06aSchemaInventoryTests`: 5 Passed | TEST_VERIFIED | Shared/deployed schema/row policy NOT_VERIFIED | New dictionary/SQL-enforced FK overview; preserve RF06 historical snapshots | P2 |
| Runtime API/errors/headers | GET Projects/staff specs; actual controllers | DOC-AUDIT-01-API; V2 API/error | Local isolated Swagger + API Explorer47controllers/241operations/223paths; DTO/schema filter/controller route attributes; no health in controller count | No persistence needed for disconnected Swagger capture | `AuthenticationOpenApiTests`: 1 Passed; `ManualSwaggerContractTests`: 10 Passed/1 Failed | PARTIALLY_IMPLEMENTED | G07 one stale excerpt test failure; source/route contract assertions otherwise pass | Document actual per-route errors/headers/body/no-body/paging; never infer133draft routes | P2 |
| FE/static proposal | Unadopted V2 wire | V2 API/FE types/contracts README | check_alignment.py133 manifest PASS; check_contracts.py/validate_package.py FAIL lock; no wire/generated contract changes | N/A; proposed wire not DB acceptance | Fresh static commands: alignment PASS;2 lock-dependent validators FAIL | CONTRACT_PROPOSED | G06 lock mismatch; no consumer adoption/runtime equivalence | Keep hash values/failed checks, review before regenerate | P1 |
| Document/source authority | DOC-AUDIT-01 assignment | Document index/CSV; backend/product entrypoints | Full tracked scope text/hash/link inventory; source/static/matrix checks; current wrappers vs preserved original bodies | Historical inventory not fresh SQL; new source dictionary separate | Fresh docs link/JSON/fence checks; exact counts in report/evidence | DOCUMENTATION_UPDATED | G08 legacy references; G09 stale .agents routing outside edits | Classify every file; preserve snapshots, point current docs to evidence | P2 |

Không phát hiện chắc chắn P0 từ inspected source/selected checks. Ma trận không thay full-suite UAT, live FastAPI, actual Android hay production certification. Các source limitations/risk và external gates được đếm một lần theo GAP ID, không đếm lại severity ở từng matrix row.


## Full source-path lookup

Tên symbol/partial dùng trong bảng có các source paths sau (tại baseline; không là code thay đổi):

- `RoadGuardSystem.API/Authentication/WebCookieConfiguration.cs`
- `RoadGuardSystem.API/Controllers/AiMockRunsController.cs`
- `RoadGuardSystem.API/Controllers/AuthController.cs`
- `RoadGuardSystem.API/Controllers/CandidatesController.cs`
- `RoadGuardSystem.API/Controllers/CasesController.cs`
- `RoadGuardSystem.API/Controllers/NotificationsController.cs`
- `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs`
- `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs`
- `RoadGuardSystem.API/Controllers/ProjectsController.cs`
- `RoadGuardSystem.API/Controllers/RepairEligibilityController.cs`
- `RoadGuardSystem.API/Controllers/RepairExecutionController.cs`
- `RoadGuardSystem.API/Controllers/RepairLifecycleController.cs`
- `RoadGuardSystem.API/Controllers/RepairPackagesController.cs`
- `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs`
- `RoadGuardSystem.API/Controllers/RepairSafetyController.cs`
- `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs`
- `RoadGuardSystem.API/Controllers/ReporterRegistrationsController.cs`
- `RoadGuardSystem.API/Controllers/ReportsController.cs`
- `RoadGuardSystem.API/Controllers/RetentionController.cs`
- `RoadGuardSystem.API/Controllers/StaffDirectoryController.cs`
- `RoadGuardSystem.API/Controllers/TrainingLabelsController.cs`
- `RoadGuardSystem.API/Controllers/UploadsController.cs`
- `RoadGuardSystem.API/Controllers/UsersController.cs`
- `RoadGuardSystem.API/Controllers/WebAuthController.cs`
- `RoadGuardSystem.API/Middlewares/WebCookieRequestMiddleware.cs`
- `RoadGuardSystem.BusinessObjects/Candidates/CandidateDecision.cs`
- `RoadGuardSystem.BusinessObjects/Cases/IncidentCase.cs`
- `RoadGuardSystem.BusinessObjects/Clocks/DeadlineClock.cs`
- `RoadGuardSystem.BusinessObjects/Files/StoredFile.cs`
- `RoadGuardSystem.BusinessObjects/Files/UploadAdmissionPolicy.cs`
- `RoadGuardSystem.BusinessObjects/Files/UploadSession.cs`
- `RoadGuardSystem.BusinessObjects/Identity/ApplicationUser.cs`
- `RoadGuardSystem.BusinessObjects/Identity/RefreshToken.cs`
- `RoadGuardSystem.BusinessObjects/Identity/ReporterRegistrationIntent.cs`
- `RoadGuardSystem.BusinessObjects/Identity/UserSession.cs`
- `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionAssignment.cs`
- `RoadGuardSystem.BusinessObjects/Inspections/GroundTruthMeasurement.cs`
- `RoadGuardSystem.BusinessObjects/Messaging/Notification.cs`
- `RoadGuardSystem.BusinessObjects/Messaging/NotificationRegisteredTypes.cs`
- `RoadGuardSystem.BusinessObjects/Processing/AIDetection.cs`
- `RoadGuardSystem.BusinessObjects/Processing/AIModelVersion.cs`
- `RoadGuardSystem.BusinessObjects/Processing/Anh02AiProvenance.cs`
- `RoadGuardSystem.BusinessObjects/Processing/ProcessingJob.cs`
- `RoadGuardSystem.BusinessObjects/Projects/Project.cs`
- `RoadGuardSystem.BusinessObjects/Projects/ProjectMember.cs`
- `RoadGuardSystem.BusinessObjects/Projects/RoadSectionVersion.cs`
- `RoadGuardSystem.BusinessObjects/Projects/RoadSegment.cs`
- `RoadGuardSystem.BusinessObjects/Projects/RoadSegmentSet.cs`
- `RoadGuardSystem.BusinessObjects/Repairs/RepairPackage.cs`
- `RoadGuardSystem.BusinessObjects/Repairs/RepairPolicyDraft.cs`
- `RoadGuardSystem.BusinessObjects/Reports/ReportEvidence.cs`
- `RoadGuardSystem.BusinessObjects/Reports/ReportSupplement.cs`
- `RoadGuardSystem.BusinessObjects/Surveys/SurveyAssignment.cs`
- `RoadGuardSystem.DTOs/Processing/Anh02AiDtos.cs`
- `RoadGuardSystem.DTOs/Projects/ProjectListResponseDto.cs`
- `RoadGuardSystem.DTOs/Repairs/RepairPolicyDtos.cs`
- `RoadGuardSystem.DTOs/Reports/ReporterReportDtos.cs`
- `RoadGuardSystem.DTOs/Staff/StaffReadResponses.cs`
- `RoadGuardSystem.Repositories/Configurations/Huy01IntegrationConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/OfflinePersistenceConfigurations.cs`
- `RoadGuardSystem.Repositories/Configurations/RepairPolicySafetyConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/RepairWorkflowConfiguration.cs`
- `RoadGuardSystem.Repositories/Configurations/StoredFileConfiguration.cs`
- `RoadGuardSystem.Repositories/Implementations/Cases/CaseWorkflowRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Defects/CandidateDecisionRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.Offline.cs`
- `RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs`
- `RoadGuardSystem.Repositories/Implementations/Identity/IdentityOnboardingRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.RepairCapture.cs`
- `RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Labels/TrainingLabelRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Messaging/NotificationPersistenceService.cs`
- `RoadGuardSystem.Repositories/Implementations/Offline/OfflineAdmissionValidator.cs`
- `RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Processing/Anh02AiRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs`
- `RoadGuardSystem.Repositories/Implementations/Projects/ProjectLifecycleRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Projects/ProjectListReadModel.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RepairEligibilityRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RepairFtEligibility.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RepairPolicyRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Repairs/RoadCoverageRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Reports/ReporterLifecycleRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Reports/ReporterReportRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Staff/StaffReadRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Storage/MinioUploadObjectStorage.cs`
- `RoadGuardSystem.Repositories/Interfaces/Files/IUploadRepository.cs`
- `RoadGuardSystem.Repositories/RoadGuardDbContext.cs`
- `RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs`
- `RoadGuardSystem.Services/Implementations/Authentication/AuthoritativeSessionValidator.cs`
- `RoadGuardSystem.Services/Implementations/Authentication/IdentityOnboardingService.cs`
- `RoadGuardSystem.Services/Implementations/Cases/CaseWorkflowService.cs`
- `RoadGuardSystem.Services/Implementations/Defects/CandidateMatchingService.cs`
- `RoadGuardSystem.Services/Implementations/Files/ReporterEvidenceService.cs`
- `RoadGuardSystem.Services/Implementations/Files/UploadService.cs`
- `RoadGuardSystem.Services/Implementations/Offline/OfflineWorkflowEngine.cs`
- `RoadGuardSystem.Services/Implementations/Processing/Anh02/Anh02AiService.cs`
- `RoadGuardSystem.Services/Implementations/Processing/ProcessingV2Service.cs`
- `RoadGuardSystem.Services/Implementations/Projects/ProjectListService.cs`
- `RoadGuardSystem.Services/Implementations/Repairs/RepairPolicyService.cs`
- `RoadGuardSystem.Services/Implementations/Reports/ReporterLifecycleService.cs`
- `RoadGuardSystem.Services/Implementations/Reports/ReporterReportService.cs`
- `RoadGuardSystem.Services/Implementations/Staff/StaffReadService.cs`
- `RoadGuardSystem.Services/Interfaces/Candidates/CandidateMatcher.cs`
- `tests/RoadGuardSystem.ApiTests/Authentication/AuthenticationOpenApiTests.cs`
- `tests/RoadGuardSystem.ApiTests/Authentication/ManualSwaggerContractTests.cs`
- `tests/RoadGuardSystem.ApiTests/Authentication/V2IdentityOnboardingFlowTests.cs`
- `tests/RoadGuardSystem.ApiTests/Cases/Huy01CaseSqlTests.cs`
- `tests/RoadGuardSystem.ApiTests/Files/ReporterEvidenceApiTests.cs`
- `tests/RoadGuardSystem.ApiTests/Identity/V2AccountFlowTests.cs`
- `tests/RoadGuardSystem.ApiTests/Notifications/H6NotificationOperationsHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Offline/H5OfflineHttpAdmissionTests.cs`
- `tests/RoadGuardSystem.ApiTests/Offline/H5OfflinePositiveHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Offline/ND01OriginalWindowHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Processing/Anh02AiHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Projects/ProjectListTests.cs`
- `tests/RoadGuardSystem.ApiTests/Projects/StaffMembershipReadHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Projects/SupervisorGlobalAuthorityHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairPolicyHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairWorkflowHttpAdmissionTests.cs`
- `tests/RoadGuardSystem.ApiTests/Reports/Huy01ReporterReceiptAccessSqlTests.cs`
- `tests/RoadGuardSystem.ApiTests/Reports/Huy01ReporterReportsApiTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Identity/H1PersistentIdentitySqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Migrations/Validation/Rf06aSchemaInventoryTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Notifications/H6ProtectedInboxSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Notifications/H6RepairLifecycleSourceSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineRepairSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Offline/ND01OriginalWindowSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairExecutionSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairLifecycleSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairPolicySqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairProducerSourceSqlTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Repairs/H4SafetyRuntimeSqlTests.cs`
- `tests/RoadGuardSystem.UnitTests/Offline/H5OfflineAcceptTests.cs`
- `tests/RoadGuardSystem.UnitTests/Offline/H5OfflineCryptoTests.cs`
- `tests/RoadGuardSystem.UnitTests/Offline/H5OfflineEnvelopeTests.cs`
- `tests/RoadGuardSystem.UnitTests/Repairs/H4PolicySafetyTests.cs`
- `tests/RoadGuardSystem.UnitTests/Repairs/H4RepairLifecycleDomainTests.cs`
- `tests/RoadGuardSystem.UnitTests/Repairs/H4RepairWorkflowTests.cs`
