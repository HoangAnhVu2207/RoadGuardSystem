# ANH-02 — AI contract/mock, reporting/export, retention/hold

**Partial — external ChatGPT review PENDING.** Branch `anh-review`; base/preflight HEAD `efc0ca10b53264bb24c807b7352ddba0cbe36b6d`; initial dirty none. Verified implementation HEAD `1fd3821708ecdd5d98525410887e7d6c495c1396`; [implementation compare](https://github.com/HoangAnhVu2207/RoadGuardSystem/compare/efc0ca10b53264bb24c807b7352ddba0cbe36b6d...1fd3821708ecdd5d98525410887e7d6c495c1396). No unrelated dirty paths existed or were removed. One assigned spec and this summary; no RF report/ZIP/extra summary.

Owner response is preserved verbatim in ANH-02 §10 (TARGET_CONFIRMED): reuse existing processing identities; Anh reporting facts first and Huy facts when produced; one snapshot mechanism for dossier/training; Supervisor basis/independent holds with PM read/evaluate, no actual deletion; simple structures allowed. AGENTS/active manifest/rules/module routes read; `skills:[]` RoadGuard retired skills were not activated. Shared mapping/migration/DI/contracts/Postman writer was Anh/root; module agents wrote only assigned files and did not commit/push.

Delivered: 18 additive routes through Controller→Service→Repository. AI uses ProcessingJob/Attempt/AIDetection, immutable canonical manifest/result/proof, current authorization before receipt, same-run lease recovery and real checked-in MP4/decoded PNG allowlist. Source scope matches the actual existing grouped PascalCase/camelCase dataset producer, rejects ambiguous aliases, and preserves original-byte hashes. Generated frames have no fictitious UploadSession or CaseId. Matching recommendations never merge/approve. Historical result reads preserve references; candidate consumption requires current geometry/source/disposition. Mock requires explicit flag and Development/Testing/Test; worker polling defaults off. Result timestamp is a stable mock timestamp from first durable claim; run.CompletedAt records actual terminal SQL time, not benchmark latency.

Reporting captures actual project/task/dataset/distinct-file/assessment/baseline-per-band facts under SERIALIZABLE, redacted keyset timeline and version refs; missing Huy/repair facts remain null/UNAVAILABLE. Legacy scope under spatial filters is explicitly unavailable. Export materializes one admission snapshot, renders PDF/ZIP outside SQL, verifies originals/private access, persists real immutable artifacts, recovers lost storage acknowledgement, and checks current download authority and exact completedAt+30days UTC. A hold does not extend download expiry. Retention has revisioned basis, multiple holds/history, all-reference obligations, source/basis/hold drift and serializable evaluation snapshots; incomplete inventory stays WAITING and no deletion/purge is implemented.

Shared integration: additive migration `20261002151928_Anh02AiReportingExportRetention` (14 tables, immutable evidence triggers, identity/terminal fences, populated-Down fail-closed); existing applied migrations untouched. EF8 owned-FK snapshot omission repaired only in new designer/snapshot, preserving existing Restrict behavior. Idempotency adds opt-in Serializable execution while retaining the existing signature/default isolation. Legacy identity/processing production files are unchanged. Two existing regression fixtures were corrected: one published set plus a changed valid request field; historical processing lifecycle measured at its own migration revision, preserving the later irreversible guard.

Self-review1 (actual): fixed missing-video admission, actual TELEMETRY purpose/metadata ownership, grouped scope/PascalCase source mismatch, manifest source-version drift, expired-worker failure authority, canonical result nonhex/null/duplicate matching shape, candidate proof/raw-hash consistency, and matching recheck at completion. Reporting/export/retention reviews fixed nullable legacy metrics, versions/formulas, same-key races, private cursor/scope, PDF stream rewind/header and ZIP spooling, conditional durable object writes and separate-context lease heartbeat.

Self-review2 (actual): checked ownership/DI/flags, source/receipt/audit atomicity, FK cycles, immutable trigger enforcement, fresh/upgrade/empty Down–Up/populated guard, current authorization/expiry/hold, compatibility fixtures, UTF-8 samples/Postman and dependency/license/runtime distinction. An independent read-only AI review confirmed the lease/matching fixes. Final diff/whitespace checked. These are Codex self-reviews, not external ChatGPT acceptance.

CURRENT_VERIFIED checks (counts are test cases, not HTTP requests; all SQL fixtures own disposable databases):

| Command/check | Actual result |
|---|---|
| `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --no-incremental --disable-build-servers -v quiet -clp:ErrorsOnly` | 0 errors, 98 warnings |
| `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter FullyQualifiedName~Anh02 --disable-build-servers -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-final-unit.trx'` | 69 passed /0 failed /0 skipped: manifest14, result/flag/samples10, reporting14, export18, retention13 |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Anh02Retention -clp:ErrorsOnly` | 9 passed /0 failed /0 skipped; two focused reruns also passed (including immutable UPDATE rejection) |
| Same Integration csproj, `--filter FullyQualifiedName~Anh02Export --disable-build-servers -v minimal` | 4 passed /0 failed /0 skipped |
| Same Integration csproj, `--filter 'FullyQualifiedName~Anh02Migration\|FullyQualifiedName~P202TransactionAndIdempotencyTests\|FullyQualifiedName~A0802ValidationReplayTests\|FullyQualifiedName~P230ProcessingJobContractTests\|FullyQualifiedName~P231ProcessingPersistenceTests' --disable-build-servers -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-migration-regression-final.trx'` | 20 passed /0 failed /0 skipped: migration3 + legacy regression17; together with module SQL above, 33 distinct SQL/regression cases |
| `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter 'FullyQualifiedName~Anh02\|FullyQualifiedName~IdempotencyPerCommandCharacterizationTests\|FullyQualifiedName~Anh01SurveyCorrectionTests\|FullyQualifiedName~AnhHuyGeometrySourceTests\|FullyQualifiedName~Anh01LegacyGeometryCorrectionTests' --disable-build-servers -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-final-api-regression.trx'` | 19 passed /1 failed /0 skipped initially: stale two-published-set regression fixture failed before HTTP. Fixture corrected; targeted method rerun `--filter FullyQualifiedName~IdempotencyPerCommandCharacterizationTests.SurveyPlanCreate_SameKeyDifferentPayload_ReturnsConflict --logger 'trx;LogFileName=anh02-legacy-api-fixture-final.trx'` passed1/0/0. Thus 20 distinct affected cases verified, including 6 ANH-02 cases; no unresolved failure. |
| `./.tools/dotnet-ef.exe migrations has-pending-model-changes --context RoadGuardDbContext --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-build` | Exit0, no pending model changes; temporary unreachable design-only connection, no DB I/O |
| Python JSON/deep comparison + Node `new Function` syntax validation | 14 original folders/IDs/info/variables unchanged; 23 new requests; 25 new variables; 35 new scripts parsed; no unresolved variable; UTF-8 valid |
| Synthetic generator/decoder and C# canonical sample tests | Real MP4/PNG SHA256/bytes, 4frames/2fps/2000ms, frame1@500ms; four sample canonical byte/hash pairs checked. Empty scenario explicit. |
| PDF QA via pypdf/Poppler render | Three A4 pages, 45,848 bytes; Vietnamese text readable, embedded FontFile2, no clipping; installed Arial QA only, font not redistributed |
| `git diff --check` and selective staged review | Exit0; scope includes no historical RF/identity/processing production edits |

B01–B04/B06/B18–B20: actual HTTP→owned SQL upload/dataset/geometry→analysis→durable frame/result, replay/conflict, invalid model/scope/key, stale versions, two-context claim, expired owner, same-run recovery, legacy detection without proof/cross-project hiding and real source-hash rejection. Two-stage matching positive/stale completion uses an explicitly named reader fixture (**MOCK_VERIFIED**, not Huy integration). B08–B12/B14–B17: actual Anh reporting→dossier snapshot, consistent definitions/cursor/redaction/read invariance, private two-Reporter isolation, PDF/ZIP/recovery and current auth/hold/expiry. B21–B24: real incomplete inventories, independent/raced holds, basis/evaluation snapshots/drift; complete-Huy scenarios use explicitly named fixture contributors, not a live inventory claim. B05/B07/B13/B25 real Huy consumer/label/matching/inventory adoption remains pending.

Failure history retained: initial pure-helper TDD0pass/19fail→19pass; missing-video review22pass/1fail corrected; result validation red5pass/3fail corrected; cancelled retention SQL seed run had5 observed failures from missing fixture PasswordHash (then9pass). First integrated HTTP4pass/2fail exposed actual scope parsing and shared test-queue assumptions, both fixed. First migration/regression18pass/1fail hit existing ANH-01 irreversible Down; baseline-only SQL reproduces that guard, and the historical test now isolates its subject. Compile checks caught helper imports/options/role enum/hash-comparison/Geometry metadata issues before final passing build. No failure is hidden as a skip.

Dependencies/limitations: local `Anh02Contracts.cs` is additive Anh boundary; REPORT v1 unchanged. Read-only remote check still `origin/huy-review=b8ec845d69ba2c71249cec6fb15e13a5bad8126f`, with no required readers; exact accepted consumer SHA PENDING. Huy owns matching/current-approved labels/resource-access/complete inventory and subsequent Report/Case/Defect/repair facts. Mandatory training reader absent→503; matching absent→409; optional dossier Huy sections Partial; retention inventory incomplete→WAITING. Exact one PDF dependency PDFsharp6.2.3 MIT verified from official nuspec; deployed licensed Unicode font remains unverified. No unrelated package upgrade/framework expansion.

NOT RUN / external-deployment UNKNOWN: live MinIO/S3 conditional-write recovery and network streaming; Postman HTTP runner; actual Huy consumers/mapping acceptance; external AI/FE/Android/demo; deployed font; 8GiB transport (4GiB reporting bytes are metadata fixture only); real shared/deployed DB history/upgrade/backup restore; performance percentiles; real deletion. ANH-01 CRS and existing A08/A09 legacy callback/attempt remediation gates remain Partial/OPEN, unchanged. No shared DB migration, merge, main/Huy writes, amend or force-push.

Delivery checkpoint CURRENT_VERIFIED: implementation commit `1fd3821708ecdd5d98525410887e7d6c495c1396` contains the reviewed/tested source; worktree clean immediately after that commit. This handoff update changes only this existing summary, by a normal follow-up commit. Final delivery HEAD and exact full compare are supplied in the final handoff; [review branch compare](https://github.com/HoangAnhVu2207/RoadGuardSystem/compare/efc0ca10b53264bb24c807b7352ddba0cbe36b6d...anh-review). Normal push is authorized; external ChatGPT review PENDING.

## ANH-02 correction — receipt authorization and terminal analysis, 2026-10-03

**Partial; external ChatGPT re-review PENDING.** Owner's pasted correction request is the assignment. Initial branch/local HEAD/`origin/anh-review` all `anh-review` / `62388a8537fd368da43fce578afeb8cef0ab01df`; `git status --short` empty. Remote `anh-review` checked read-only and matched that base. No reset/stash/restore/discard/amend/force-push or Huy/main/develop writes. AGENTS, active manifest/rules/module map, ANH-02 spec/summary and actual callers read; retired RoadGuard skills not activated. Correction source/tests HEAD **`14c235a383053cf6be99fcbaf0770a3950c2cb39`**; [exact source/test compare](https://github.com/HoangAnhVu2207/RoadGuardSystem/compare/62388a8537fd368da43fce578afeb8cef0ab01df...14c235a383053cf6be99fcbaf0770a3950c2cb39). Final delivery HEAD is the subsequent summary-only commit, supplied in the handoff.

**P1 CURRENT_VERIFIED:** `Anh02AiRepository.AdmitAsync`, `ExportRepository.AdmitAsync` and `RetentionRepository.CommandAsync` now pass `receiptAccessGuard:` after the original command token. Retention covers basis confirmation, hold create/release and evaluation. The same scoped DbContext reads current SQL user/status/password requirement, active SQL role, effective membership and applicable resource scope. ANH-02 helper locks Users → actual Roles → Projects → ProjectMembers using UPDLOCK/HOLDLOCK; Retention first takes its existing control application lock, then authority locks, then ordered file/FileScope or hold/scope locks. Existing command-specific authorization is rechecked under the receipt service's short transaction. No guard starts/commits a transaction, saves rows, invokes capture/manifest construction, or substitutes cached preflight/JWT authority. Existing privacy/status codes remain AI/Retention `403 access_forbidden`, Export `403 forbidden`; current fingerprint conflicts remain unchanged when authorized. Cancellation and guard exception identity propagate; durable cancelled acknowledgement never returns a stored receipt. SqlClient's own transaction-start cancellation exception can contain a default token; it is propagated unchanged, not rewritten.

Shared `IdempotencyOperationService` is unchanged from the delivered seam `f626ea595420c3f28a35f2b3f4c6f196d313a7be` / `62388a8537fd368da43fce578afeb8cef0ab01df`; original seven-parameter callers remain compatible. Its executed 32-case primitive suite covers ordinary lookup, retry discovery, unique-key recovery, acknowledgement recovery, conflicts, locks and cancellation. Caller-specific SQL/HTTP tests independently exercise post-preflight revocation/deactivation, role/password changes, actual Export unique-key and post-commit recovery, unchanged receipt/audit/job/run/hold/evaluation counts, and no recapture. SQL AI receipt-only fixture is explicitly narrow; actual upload → dataset → admission → worker → result/provenance → replay/conflict/403 is verified by the existing expanded HTTP test.

**P2 CURRENT_VERIFIED:** terminal `AiRequestException` failure of VIDEO_ANALYSIS writes run FAILED, linked mock ProcessingJob DataFailure/error/completion, attempt EndedAt/Data and failure audit in one SERIALIZABLE transaction. Added `ProcessingJob.FailData` is restricted to pending MOCK jobs, with existing enum values; REAL/terminal jobs cannot use it. Successful analysis closes its one attempt with ErrorType.None and retains current result/provenance. DUPLICATE_MATCHING failure/completion never alters analysis job/attempt. Storage IOException still leaves the same run/attempt pending for lease recovery; no retry attempt/provider/remediation framework added. HTTP→SQL fault injection after run/job/audit writes but before attempt closure proves complete rollback. Invalid source produces no result/detection/proof; matching stale leaves completed analysis unchanged; successful storage recovery keeps stable identities/one durable frame.

Attempt closure required additive **`20261003090000_Anh02AnalysisAttemptClosure`** because existing EF metadata and SQL trigger prohibit all attempt updates. SQL-only migration permits exactly one pending→terminal closure for the matching ANH-02 VIDEO_ANALYSIS run/job/error/time; rejects legacy attempts, identity edits, later rewrites and deletes. EF generic mutation fence and model snapshot remain unchanged. Fresh/upgrade/empty Down→Up tests execute on disposable SQL; Down restores the prior append-only trigger without changing rows. Existing migrations are untouched. No historical backfill or shared/deployed database migration/history audit was performed; any already-persisted inconsistent historical states need an audited data correction decision before deployment.

Actual self-review pass 1: checked authorization before replay/conflict and all receipt recovery boundaries, lock order/same-context lifetime, no handler replay, exception/cancellation, atomic run/job/attempt/audit, stale lease and matching isolation. Added basis/release and real caller recovery/rollback evidence. Pass 2: compared current session password policy and added missing Export MustChangePassword recheck with two red tests; verified role deactivation, retained overloads/EF legacy fence, migration Up/Down, ownership and final staged diff. Production DI, routes/DTOs/headers/error mappings and Postman content are unchanged; JSON/content comparison passed after CRLF/LF normalization. Tests inject interceptors only into their owned test DbContexts. These two reviews are Codex self-review, not external ChatGPT acceptance.

Exact changed files (13 source/tests + this existing summary):

- `RoadGuardSystem.BusinessObjects/Processing/ProcessingJob.cs`
- `RoadGuardSystem.Repositories/Idempotency/Anh02ReceiptAuthority.cs`
- `RoadGuardSystem.Repositories/Implementations/Processing/Anh02AiRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs`
- `RoadGuardSystem.Repositories/Migrations/20261003090000_Anh02AnalysisAttemptClosure.cs`
- `tests/RoadGuardSystem.ApiTests/Authorization/Anh02ReceiptAuthorizationHttpTests.cs`
- `tests/RoadGuardSystem.ApiTests/Infrastructure/Anh02ReceiptRevocationInterceptor.cs`
- `tests/RoadGuardSystem.ApiTests/Processing/Anh02AiHttpTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Persistence/Anh02ReceiptAuthorizationTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Persistence/Anh02MigrationTests.cs`
- `tests/RoadGuardSystem.IntegrationTests/Retention/Anh02RetentionPersistenceTests.cs`
- `tests/RoadGuardSystem.UnitTests/Processing/Anh02ProcessingFailureTests.cs`
- `planning/development/ANH-02-summary.md`

Fresh final commands/results, CURRENT_VERIFIED; SQL owns disposable databases, no external storage:

| Exact executed command | Executed / passed / failed / skipped |
|---|---|
| `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --no-incremental --disable-build-servers -v quiet -clp:ErrorsOnly` | Exit0; 0 errors /98 warnings |
| `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Anh02' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-correction-unit.trx'` | 75 /75 /0 /0 (69 prior +6 terminal transition cases) |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Anh02\|FullyQualifiedName~ReceiptAccessGuardSqlTests\|FullyQualifiedName~P202ServiceContractTests\|FullyQualifiedName~P202TransactionAndIdempotencyTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-correction-sql-verified.trx'` | 93 /93 /0 /0: real caller19, retention13, export4, migration3, primitive32, P202 compatibility/transactions22 |
| `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Anh02\|FullyQualifiedName~IdempotencyPerCommandCharacterizationTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-correction-api-verified.trx'` | 22 /22 /0 /0: ANH-02 HTTP12 + per-command characterization10 |
| `./.tools/dotnet-ef.exe migrations has-pending-model-changes --context RoadGuardDbContext --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-build` | Exit0; no pending model change. Temporary unreachable design-only connection; no database I/O. |
| `git diff --check`; `git diff --cached --check`; allowlist/content/JSON checks | Exit0; exactly assigned paths; shared receipt service, old EF attempt fence, snapshot and Postman have no content changes |

**190 distinct final test cases**, all passed; overlapping focused/red runs are not added to that total. Failure/reproduction history: first compile had CA2016 (0 executed), corrected explicit token forwarding. First P1 red executed8/failed8 due OutOfMemory during EF model construction, not a bug reproduction; next base-caller comparison executed8/failed8 with actual stored replay/conflict after revocation (base caller bytes temporarily substituted and correction bytes restored in finally, no Git reset). P2 HTTP red executed1/failed1: expected DataFailure, actual Queued; focused green1/1. Initial caller green14/14. Additional password guard red2/2 reproduced missing recheck. Initial expanded HTTP executed22/passed15/failed7: interceptor registration was not applied to actual scoped DbContext and evaluation's empty-ID conflict payload was invalid; fixed test-only explicit DbContext injection/valid scope, final22/22. Initial expanded SQL executed91/passed90/failed1: unsupported assumption about SqlClient cancellation exception token; provider exception preserved and final93/93. No unresolved failure or failure converted to skip.

BE_VERIFIED: actual caller/HTTP authorization, SQL durability/recovery/rollback, migration/legacy fence and compatibility under owned fixtures. MOCK_VERIFIED: synthetic AI/storage and matching reader fixture only. NOT RUN / external-deployment UNKNOWN: live Huy reader/mapping/current-approved labels acceptance, live MinIO/S3, deployed fonts, demo/8GiB, shared/deployed migration/history/backup recovery, external deployment/provider/FE/Android, full solution suite, performance and Postman HTTP runner. No real deletion. Read-only remote Huy ref now `0e41913b225d4f72d4d1ead1ae322ad56bba5328`; this correction did not fetch/merge/integrate or inspect that newer consumer code and grants no acceptance at that SHA. ANH-02 remains Partial; ANH-01 CRS and excluded A08/A09 gates remain unchanged.


## ANH-02 continuation after Reporter checkpoint A — 2026-10-03

**Partial; external ChatGPT review/re-review PENDING.** Initial task base
`6365ae0ce0d6dc982a88b6d1ea3864ed37a7035e`; initial dirty none on anh-review.
Checkpoint A/base B `715ade2c20f652b77c8c7e995c76bb5d47ead966` is already pushed,
[exact A compare](https://github.com/HoangAnhVu2207/RoadGuardSystem/compare/6365ae0ce0d6dc982a88b6d1ea3864ed37a7035e...715ade2c20f652b77c8c7e995c76bb5d47ead966).
A integrates fixed Huy source `0e41913b225d4f72d4d1ead1ae322ad56bba5328`, not a tip
merge. Remote Huy later7e8261648e08adf5b5bdf0cea85fca5e55463b73 was inspected read-only
for dependencies; its lifecycle/case/candidate changes were not imported. No new
reader checkpoint/ownership adopted. A handoff is in existing ANH-01-summary.
Final B SHA/compare supplied in final handoff; normal selective push, no amend.

Dependency audit: prior AI mock/provenance/terminal job+attempt+audit correction,
export snapshot/lease/download/recovery, retention control and shared receipt
remain implemented; no reimplementation. Production DI activation invalidated
caller/binding evidence, rerun below. Available intake tables now support Anh
count/read/snapshot and reference inventory. Actual Huy approved labels/current
revision, matching, CaseDefectRead/internal dossier/timeline, full inventory,
external storage/deployment remain missing. No provider AI/A08/A09/HUY-02/CRS scope.

Delivered in B (Anh-owned files):
- ReportingRepository/IReportingRepository/ReportingDefinitions materialize
  distinct Report→active canonical CaseReportLink→current project and case stock
  under existing SERIALIZABLE capture. reportsReceived applies ReceivedAt [from,to);
  casesByStatus is current stock. Source IDs/rowversions are real; no description,
  contact, private file bytes or aggregate-array attribution. Intake counts/zero
  are PARTIAL/HUY_CASE_LIFECYCLE_NOT_INTEGRATED. Geometry filters return explicit
  null/UNAVAILABLE, never invented segment attribution; defect/repair stay unavailable.
- New ReporterIntakeRetentionInventoryContributor registered once, scoped with
  actual DbContext: original/supplement evidence/report versions and all case links,
  including closed links and case-head versions. Complete only for named bounded
  table slice; Name is REPORTER_INTAKE, not HUY. Composite still requires full Huy
  obligations and remains incomplete/WAITING. Private FileScope stays project-null
  and PM-hidden. Known project files include actual linked intake evidence.
- Export reuses the same reporting admission snapshot. Report/case partial counts
  reach dossier without granting photo access; immutable snapshot survives later
  fixture-only triage. Consumer now rejects duplicate LabelId heads/null annotation/
  malformed snapshot facts as producer_invalid and includes mandatory-password
  state in current service/worker/download authority. No approval policy implemented.
- Canonical local adoption appended; integrated Postman adds2 count drilldowns
  and fixes quoted signed-PUT ETag before JSON complete. No draft/FE lock changes.
  Existing Huy producer, shared receipt service, frozen Anh02Contracts interface,
  migrations/snapshot, processing terminal behavior and identity sources unchanged.

Actual producer→consumer evidence: real auth/production DI/SQL/BE private upload→
verification→received Report+UNASSIGNED Case→reporting exclusion→dossier admission/
render/download→intake inventory/FILE hold. Object storage explicitly mocked;
local installed Arial and actual PDFsharp renderer used, PDF parses with title
Hồ sơ RoadGuard/pages>0. No font redistributed or deployment claim. After explicitly
named SQL fixture triage (NOT Huy business-command acceptance): count/drilldown1,
future period report0 vs case stock1, historical dossier count0 preserved, FILE+
PROJECT holds2 then file release leaves project block1; evaluation stores actual
private and generated-artifact refs, PM hides private item/cursor. Supplement/
closed-link SQL fixtures preserve obligation and advance inventory hash. REPORT
candidate producer rejects missing case geometry as SourceNotReady/no fake facts.

Training consumer fixture: scoped IApprovedTrainingLabelReader capture asserts
same current SQL transaction SERIALIZABLE; actual exporter/snapshot/receipt/ZIP/
source/hash/download flow, typed immutable revision2 remains after fixture head3,
current source-access revoke denies bytes. Seven shapes: unavailable503, empty422,
foreign project422, duplicate heads422, null annotation422, denied source403,
valid typed fixture202. This is MOCK_VERIFIED approval/source authority, not SQL
current-approved Huy producer acceptance. No fixture reader registered in production.
IApprovedTrainingLabelReader and ITrainingSourceAccessReader exact fields remain
Anh02Contracts.cs: label/revision/approval IDs, project/type/normalized bbox, file
version/hash/int64 bytes/media, source identity/version, approver/time, job/model/
dataset/mode/segment; snapshot ID/hash/time/materialized labels. Huy must supply
adopted exact same-context current-approved reader + current resource access and
mapping/approval-revision tests. No CaseDefectRead/dossier projection is inferred
from Huy DTOs: typed case/defect/publication/repair facts, authorized evidence refs,
availability/current revision/permission semantics and exact checkpoint remain
owner Huy's missing producer/adoption; optional sections unavailable, not empty success.
IAiCandidateFactsReader remains registered/trusted new-provenance producer; real
Huy AI command and matching snapshot acceptance still PENDING at fixed source.

Actual self-review1: checked attribution/privacy/current links vs retention
historical obligations, shared scoped read/capture isolation, immutable admission,
private evidence permissions and typed reader failures; new TDD intake test found
missing REPORT refs. Added bounded reader/counts and domain/version/filter tests.
Contract tests exposed duplicate current heads accepted and null annotation500,
fixed Anh consumer (does not decide approval). Review clarified contributor
completeness only for named tables; aggregate Huy gate retained.
Actual self-review2: checked worker/service authority independent of JWT, callback
ownership, producer absence, final diff/compatibility/Postman and stable source/hash
ordering. Direct service password test reproduced success while MustChangePassword,
fixed current authority; supplemental hash evidence/ID ordering explicit. Corrected
quoted PUT ETag; old reporting fixture assertion now checks actual partial counts
and missing canonical links, not stale unavailable-reader assumption. Reviews are
Codex self-review, not external review. Huy role-row guard gap from A remains open.

Fresh commands/results (all SQL uses owned disposable DB + production migrations):
- `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --no-incremental --disable-build-servers -v quiet -clp:ErrorsOnly`:exit0,98 warnings/0 errors.
- `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Anh02|FullyQualifiedName~ReporterIntakeRequestNormalizerTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-continuation-unit-final.trx'`:93 executed/pass,0 failed/skip.
- `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --disable-build-servers --filter '(FullyQualifiedName~Huy01Reporter|FullyQualifiedName~Anh02|FullyQualifiedName~ReporterEvidenceApiTests|FullyQualifiedName~UploadApiTests|FullyQualifiedName~IdempotencyPerCommandCharacterizationTests)&FullyQualifiedName!~CompleteMultipartUploadAgainstConfiguredMinio' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-continuation-api-verified.trx'`:78 executed/pass,0 failed/skip.
- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Anh02|FullyQualifiedName~ReceiptAccessGuardSqlTests|FullyQualifiedName~P202ServiceContractTests|FullyQualifiedName~P202TransactionAndIdempotencyTests|FullyQualifiedName~Huy01SharedSchemaTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-continuation-sql-final.trx'`:97 executed/pass,0 failed/skip.
- After final fact/hash ordering edits: same ApiTests command, filter `FullyQualifiedName~Anh02_real_intake|FullyQualifiedName~Anh02_intake_inventory|FullyQualifiedName~Anh02_training_consumer|FullyQualifiedName~Anh02ReportingHttpTests`, TRX anh02-continuation-final-review.trx:10 executed/pass,0 failed/skip. Included in78, not added. **268 distinct passing cases**, not A258+B268.
- `./.tools/dotnet-ef.exe migrations has-pending-model-changes --context RoadGuardDbContext --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-build`:exit0,no pending model; process-only unreachable design connection restored. No migration generated/applied/shared DB.
- JSON parse/201 Postman script syntax checks and quoted-ETag simulation:pass. No Postman network-runner claim. Shared/frozen/Huy source byte comparisons, summary prefix preservation, selective staged checks/git diff --check:exit0.

Failure history: intake red1 failed at missing REPORT refs→green1; expanded snapshot/
holds green1. Training first compilation zero cases (missing Storage extension import),
fixed fixture; red7 executed4 pass3 fail: duplicate head202, null annotation500,
positive ZIP not-ready from earlier negative fixture's erroneously queued job.
Consumer fixes + bounded actual worker loop→7 pass. Existing reporting red1 failed
on stale UNAVAILABLE expectation after real reader activation, updated explicit
partial/canonical-link assertions. Password direct-service red1 failed→focused9
pass. First broad API58 pass; expanded PDF/retention run78=77pass1fail because actual
PDF adds a generated artifact and an assertion assumed one evaluation file; fixed
file-specific/privacy assertions→78pass. Final ordering affected10pass. No unresolved
failure or hidden skip; earlier overlapping runs not summed.

BE_VERIFIED: production Reporter DI/HTTP/SQL receipt paths, existing real AI terminal
invariants, project reporting/dossier capture, source references/holds/evaluator,
rollback/replay/recovery/auth and actual local PDF/ZIP backend. MOCK_VERIFIED: storage,
matching reader and typed approved-label/source-access fixtures. Triage/supplement/
closed-link setup is SQL fixture evidence, NOT real Huy lifecycle orchestration.
Live external verified: none newly. NOT RUN: real Huy current-approved labels/
CaseDefectRead/matching/dossier/timeline/full inventory consumers, Reporter role-row
revoke acceptance (Huy-owned source finding), external ChatGPT review/re-review,
MinIO/S3, deployed font/license packaging, demo/8GiB transport, shared/deployed DB
history/upgrade/backup recovery, deletion/backfill, external AI/FE/Android/provider,
full solution/performance/Postman network runner. Local disposable MinIO attempts:
`docker pull minio/minio:RELEASE.2025-04-22T22-12-26Z` denied; fallback
`docker pull quay.io/minio/minio:RELEASE.2025-04-22T22-12-26Z` returned401 Unauthorized.
No MinIO container/bucket created, no external/shared bucket or credentials used.
8GiB/demo live chain cannot run without that storage/configuration; existing asset/
limit/mock evidence is not promoted. ANH-01 CRS/A08/A09 and prior Partial gates remain.

Handoff gates: ANH-01 Partial (Reporter intake BE active, original deployment/CRS/
8GiB/demo gates remain); HUY-01 Partial (only handed-off intake activated, role-row
finding/external review/rest of lifecycle/readers remain); ANH-02 Partial (available
Anh intersections completed, required real Huy/live/deployment gates remain). Huy
can continue immediately from checkpoint A, supply exact permission-aware reader/
mapping checkpoint and role-row guard correction; Anh remains shared writer for
next adoption. No new business decision inferred, no request to reconfirm D1–D5.


## Independent follow-up after A/B — 2026-10-03 (Partial)

TARGET_CONFIRMED: owner continuation from attachment 2e815eed-b1fd-4f9d-b332-4401e30ccbce.
Initial/local/remote/base and HEAD before this new commit:
`a46b97b271b4af1b9e89dc182b5e3fc7a10e8fff`, branch `anh-review`, initial dirty paths:
none. A `715ade2c20f652b77c8c7e995c76bb5d47ead966` and B are retained, not amended or
redelivered as new work. Final commit SHA and exact compare are in the Git handoff;
[follow-up compare](https://github.com/HoangAnhVu2207/RoadGuardSystem/compare/a46b97b271b4af1b9e89dc182b5e3fc7a10e8fff...anh-review).
AGENTS, active post-rf11-development-1 manifest/rules/module map, ANH-01/02 specs,
summaries, canonical contract and writer reservations read against current source.
No retired RoadGuard skill, reset/restore/stash/discard, amend/force push, merge,
main/develop/Huy branch edit, new Huy checkpoint import, schema/migration/identity/
processing policy/dependency upgrade/shared DB/physical deletion. Anh shared writer
for this limited existing CI/contract/Postman change; Huy's Reporter/role-row/
readers/mapping are untouched. ANH-01-summary unchanged: no ANH-01 gate promoted.

Initial gate inventory: CURRENT_VERIFIED A/B BE admission/reporting/retention/
snapshot seams (prior evidence, selectively rerun here); immediately actionable:
PDF dimensions/details/font config, snapshot/reference change + storage ACK loss,
consumer filter validation and repeatable tooling. Huy dependencies: role-row
Reporter guard, current-approved/source-access/matching/case-defect/dossier/
timeline/full inventory. Environment dependencies: live MinIO, actual 8 GiB,
full CLI demo, Linux/container/deployed fonts and external review.

Findings reproduced/fixed inside Anh ownership:
- Warm PDFsharp cache hid absent font configuration: red1/1, then fail-closed
  font path/readability/size/bytes identity on every render. Explicit
  export_font_unavailable; process restart when replacing cached font. XFont
  configuration errors map to that code. No font binary or new package committed.
- PDF previously omitted metric dimensions/drilldown values, including validation
  values. Render stored scope/period, translated name + stable code, value/unit/
  availability/reasons, status/band/route/set, numerator/denominator/period flag,
  per-item source/version/segment/dataset/assessment and validation details;
  warnings and timeline availability are visible. Wrapped rows and page numbers,
  normal metric/item blocks kept together. No changed KPI/business formula.
- Typed training reader could return labels outside selected segment IDs: red8
  (7 pass/1 fail; wrong-segment returned202), now422 producer_invalid with zero
  export effects. Existing duplicate head/null annotation/private-source guards
  retained. Current APPROVED/head policy remains Huy's producer; not duplicated.

BE CURRENT_VERIFIED evidence: actual auth/production DI/SQL/private verified
upload -> Reporter intake; fixture-only case attribution explicitly labelled,
not claimed as unavailable Huy triage acceptance. Admission snapshot counts1;
closing canonical case link before render changes live count to0. Replay/render/
retry keep exact stored JSON/hash. Artifact write ACK lost after durable bytes:
recovery reuses same bytes, one write/GeneratedArtifact/job; protected HTTP200.
Existing scoped role/password/membership replay/conflict barriers and bounded
serializable basis/hold/reference/evaluation races rerun. Inventory keeps closed
intake links, original/supplement evidence and generated artifacts; named HUY
missing/incomplete always remains fail-closed. No ELIGIBLE promotion/deletion.

Actual self-review1: actor/role/password/membership, receipt/recovery ordering,
current file/source permissions, snapshot-only render, source reference drift,
transaction/durable-effect counts, hold/evaluation races and missing storage.
Fixed the reproduced segment-filter and warm-font failures. Existing authority/
retention/source formulas needed no new production changes. Source-based Huy
Roles.IsActive finding remains owner-Huy/PENDING; not patched concurrently.
Actual self-review2: final ownership/compatibility diff, font global-cache/runtime,
all PDF pages/text/bounds, ZIP manifest/entry/proof, CLI signed URL privacy and
bounded transfers, resume original complete If-Match, fixture/catalog/model scope,
Postman assertions and CI configuration. Fixed completion status COMPLETE for
retention (distinct from export SUCCEEDED), batch fresh signed URLs, explicitly
mark synthetic sample, keep ordinary PDF rows together. These two reviews are
Codex self-review; external ChatGPT A/B review stays PENDING.

Tooling/config changes reuse tools/demo/anh01/setup_demo.py: full `anh02` phase
uses a separate exact AI fixture dataset/current geometry-package ETag, existing
RELEASED model/ACTIVE device/current Operator, mock worker, private synthetic
Reporter PNG+intake replay, reporting/PDF/ZIP/hold/evaluation. No approval/case
conclusion/full reader output fabricated; unassigned Report is excluded from
project reporting. Existing prepare/survey phases preserved. `large` requires
actual valid MP4 of exactly8,589,934,592 bytes, hashes/PUTs/downloads with64KiB
buffers, URLs in64-part batches and original complete payload/version persisted
before submission. Loopback-only signed PUT; no credentials/URLs in state/logs,
no existing output overwrite; new download file/hash must match. Small input is
rejected, never accepted as A07. Fixed tiny ANH-01/02 fixture hashes validated.
Local transport helper tests use small synthetic bytes, NOT 8 GiB/MinIO acceptance.
Optional storage.compose.yml has separate explicit disposable Compose project,
loopback ports, required environment credentials and project-scoped volume;
config --quiet checked with non-used fixture placeholders, no service started.

Font tests now require ANH02_TEST_FONT_PATH; removed all C:/Windows/Fonts fallback
from source/tests. Existing CI unit/API jobs configure fonts-dejavu-core and
explicit DejaVuSans path, no trigger/package/framework changes. Local checks use
explicit system Arial environment; this does NOT verify font rights/deployment.
Primary font-source web lookup failed (tool service HTTP500); no new font
redistributed or license acceptance claimed. Deployment must mount an embedding-
licensed Unicode TTF and set Anh02__Export__UnicodeFontPath; see existing demo
README for mock/workers/fixture/storage/8 GiB prerequisite commands.

Fresh exact commands/results (owned disposable SQL only; counts not accumulated):
- `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --no-incremental --disable-build-servers -v quiet -clp:ErrorsOnly`:0 errors/98 warnings, exit0.
- With ANH02_TEST_FONT_PATH explicitly set and optional ANH02_SAMPLE_DIRECTORY=artifacts/anh02-runtime:
  `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --disable-build-servers --filter FullyQualifiedName~Anh02 -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-runtime-unit-final.trx'`:79 executed/79 pass/0 fail/0 skip.
- Same font environment:
  `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Anh02' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-runtime-api-final.trx'`:23 executed/23 pass/0 fail/0 skip.
- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Anh02ExportPersistenceTests|FullyQualifiedName~Anh02RetentionPersistenceTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-runtime-sql.trx'`:17 executed/17 pass/0 fail/0 skip.
- After final renderer pagination edit, ApiTests same command filter
  `FullyQualifiedName~Anh02ExportHttpTests|FullyQualifiedName~Anh02_real_intake|FullyQualifiedName~Anh02_reporting_snapshot`, TRX anh02-runtime-render-http-final.trx:4 executed/pass/0 fail/skip, overlapping23 (not added).
- `python -m unittest discover -s tools/demo/anh01 -p test_setup_demo.py -v`:4 executed/pass/0 fail/skip (bounded exact slice loopback PUT; external URL rejection; small-file reject; complete ACK replay retains original body/version).
- `python tools/demo/anh01/setup_demo.py --validate-only`:exit0, no API/DB accessed.
- `./.tools/dotnet-ef.exe migrations has-pending-model-changes --context RoadGuardDbContext --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-build`:exit0/no pending model, process-only ROADGUARD_MIGRATION_CONNECTION_STRING=unreachable127.0.0.1,1 restored afterward; no DB migration/application.
- `pwsh -File tests/CI/Verify-CiWorkflow.ps1`:exit0.
- `docker compose -f tools/demo/anh01/storage.compose.yml -p roadguard-anh02-disposable config --quiet`:exit0; temporary non-used placeholder environment removed, no credentials printed.
- Node VM parsed199 leaf Postman scripts and executed10 targeted manifest strong ETag/privacy/immutable hash/poll30-day-expiry/binary assertions; all pass. All156 previous leaf items retained; only4 existing export items changed. Postman HTTP runner NOT RUN.
- Local production renderer sample PDF5pages + empty PDF1page + dossier ZIP;
  PdfReader/ExportArchive proof assertions, Python pypdf/pdfplumber text/glyph/
  bounds and ZIP exact entries/manifest/recovery marker checks pass. Poppler
  rendered all5pages, visually reviewed contact sheet and first page: no clipped
  text/overlap/broken Vietnamese. Samples in ignored artifacts/anh02-runtime,
  synthetic fixture hash marker explicitly not SQL/storage production proof;
  no new report/PDF/ZIP handoff package committed.
- TRX counters parsed:119 distinct .NET cases +4 Python =123 passing,0 failed/
  skipped. Focused overlapping reruns excluded. No broad RF/full suite.

Failure history retained: warm-font regression red1; initial training red8=7pass/
1fail; CA1305 on new explicit numerator/denominator ToString stopped compile
(selected0), fixed invariant culture; duplicate using stopped unit compile
(selected0), removed then20 export/79 final unit pass. Initial EF attempt used
wrong process variable, design factory reported missing
ROADGUARD_MIGRATION_CONNECTION_STRING; diagnostic NOT PASS, corrected and
reran with no pending model. Canonical raw-byte prefix check initially failed
because working-tree CRLF versus Git LF; EOL-normalized source-prefix check and
final Git diff confirm old content unchanged. No unresolved test failure.

NOT RUN/PENDING: `docker pull minio/minio:RELEASE.2025-04-22T22-12-26Z` again
returned pull access denied; Docker inventory only SQL2019CU18/ryuk, no running
MinIO or assigned disposable bucket. No live upload/complete/verification/
download/export storage acceptance, actual8GiB input/transfer/memory benchmark,
full CLI seed/demo, hosted CI/Linux/container/deployed font/license packaging,
external AI/Android/Web, performance/backup/restore/shared/deployed DB, external
ChatGPT review, new Huy readers/role-row correction acceptance. Former quay401
and prior A/B failures/limitations remain in preceding sections.

Huy handoff required at exact assigned revision (no tip merge):
1. Reporter role-row guard locks/rechecks Roles.IsActive including HTTP post-
   preflight replay/conflict and SQL recovery; no concurrent Anh edit.
2. IApprovedTrainingLabelReader + ITrainingSourceAccessReader (Anh02Contracts.cs):
   same scoped transaction or adopted durable snapshot; current APPROVED head,
   malformed/duplicate/null/out-of-segment/foreign/file/provenance deny;
   approval/head change before/after commit; historical revision retained,
   current source/revocation rechecked at worker/download; actual producer SQL.
3. IMatchingCandidateSnapshotReader: same-project route/set/geometry version,
   requested snapshot identity/hash/source versions, stale/head correction races;
   real VIDEO_ANALYSIS -> DUPLICATE_MATCHING -> Huy consumption.
4. Permission-aware CaseDefectRead/dossier/timeline mapping with exact fields/
   fixture acceptance/adoption; absent interface is not invented here. Full named
   HUY IRetentionInventoryContributor using caller scoped DB/transaction,
   supplement/publication/candidate/defect/label/repair references, historical
   obligations and completeness/drift; partial stays incomplete.

No new business decision needed for delivered independent scope. Configuration
still needed to execute external gates: owner-available MinIO image/disposable
private bucket/loopback API with isolated migrated DB, authorised external Unicode
font, actual8GiB valid MP4/disk capacity, preregistered device/model/catalog and
current demo identities/membership; credentials remain environment-only. CRS/
WGS84/GPX decision/dependency gate unchanged. ANH-01/ANH-02/HUY-01 and Reporter P1
remain Partial/PENDING. Commit/push is normal/selective on anh-review; exact final
SHA/compare and changed files are delivered in the same final handoff.

Changed-file scope (16 files, selected commit only): existing ExportRenderer/
ExportService; API export/intake/training consumer tests; unit renderer/storage
tests; existing CI workflow; canonical anh01.local-contract; integrated Postman
collection/local environment; existing demo README/setup_demo.py; new optional
storage.compose.yml and test_setup_demo.py; this existing ANH-02-summary.
ANH-01-summary, all Huy production files/reader contracts, persistence/schema/
model/migrations, identity/processing and shared DI unchanged. Local/remote HEAD
will be checked again after normal push; exact final SHA is emitted in handoff.


## Runtime acceptance — live storage → PDF/ZIP → demo → actual 8 GiB (2026-10-03)

Initial base/local/remote HEAD: `d2414844af1a3fd9a39547fc131f85bda451fad2`,
branch anh-review, initial dirty paths none. Current HEAD was used; no reset,
Huy tip import, branch switch, amend/force-push or merge. Owner assignment is
recorded in ANH-02.md. Final exact HEAD/compare is supplied in handoff; compare
base is this initial SHA. This section supersedes preceding NOT RUN labels
only for the specific local live gates actually executed below.

**Delivery remains Partial: local runtime gates passed, but a preflight DB
isolation incident occurred and needs owner recovery decision.** A/B and all
follow-up external ChatGPT review remain PENDING; Huy/CRS gates are unchanged.

Preflight incident — CURRENT_VERIFIED, not hidden as a harmless failed test:
initial Windows publish copied the workspace's personal
appsettings.Development.local.json. Program loaded it after environment,
overriding the task-owned connection/seed settings. Two startup attempts reached
`.\HANHNAV / RoadGuardPostmanTest` and ran MigrateAsync/seed before stopping on
legacy PostmanScenarioSeedStep survey ownership collision. Read-only SQL later
observed44 applied migrations and38 tables created at2026-10-03 07:13:46–47
Asia/Bangkok, during the first startup. Users.modify_date was07:13:47. There was
no before-image of that DB, so the full migration/backfill/seed row impact and
prior migration count are UNKNOWN. This violated the task's no-shared-migration
boundary. The accidental DB is excluded from all acceptance claims. No rollback,
reset, data restore or additional mutation of it was attempted. Owner must decide
whether to retain its current state or recover from an appropriate backup after
reviewing impact; do not run Down migrations blindly against live data.

Observed new table groups in that incident: RoadGeometryDrafts/Metadata;
DatasetAssessments/Items, BaselineSelections/Items/CurrentPointers;
Anh02AiMockRuns/ManifestFiles/DetectionProvenance/ResultProvenance;
Anh02ExportSnapshots/SnapshotFiles/Jobs/GeneratedArtifacts;
RetentionBasisHeads/Revisions, RetentionEvaluations/Items/Holds/HoldHistories;
Reports/OriginalEvidence/Supplements/SupplementEvidence, IncidentCases,
CaseReportLinks/LinkHistory/LinkHistoryReports, SourceDecisions,
CandidateSourceHeads, CaseConclusions/ConclusionEvidence/ConclusionDefects,
CasePublications/PublicationEvidence/PublicationDefects/PublicationRecipients.
These38 names are observed schema evidence, not an assertion of zero data impact.

Root fix: Development reapplies environment and command-line precedence after
local JSON; publish excludes appsettings.Development.local.json. Fresh publish
proved it absent. A task-owned unreachable decoy local file with conflicting
DB/storage/InitializeOnStartup/seed settings was subsequently tested on Linux:
production Kestrel still served the correct task DB's protected stored PDF200
using environment settings. Original personal config was not edited. The decoy
was renamed inside the task runtime only. This is actual composition-root
regression evidence; not just a configuration-source inspection.

Storage blocker investigation — CURRENT_VERIFIED:
- Docker context desktop-linux, daemon29.6.1/linux/x86_64. Initially only SQL
  2019-CU18 and ryuk images, no running containers or local MinIO. No daemon
  proxy; Docker credential store configured, registry auth entry list empty.
- Official Docker Hub repository and requested tag REST endpoints both404;
  Quay manifest401 and anonymous bearer negotiation401. No token printed or
  persisted, no credential/daemon/TLS changes. Official GitHub/Go HTTPS downloads
  and official MCR image pull succeeded, so not general network unavailability.
- Official MinIO README at source commit
  `7aac2a2c5b7c882e68c1ce017d8256be2feea27f` records source-only distribution;
  GitHub API reports archived. Upstream Dockerfile depends on unavailable
  minio/minio:latest. Built unmodified official source with local official Go
  1.27.1 (Windows archive SHA256
  a3911b5e0e1b1053f25ed0675f4c1c6aad1e2bfcf253df2b9be4caabd2edd95d),
  GOOS=linux GOARCH=amd64 CGO_ENABLED=0, `go build -trimpath -o .../minio-linux .`.
  Binary SHA256554ff0775fc94780c4423a1bf6b3f7a3bcbf4c930481d393b25fd41a7754557f.
  Local-only scratch image copies binary and upstream AGPL LICENSE; no third-party
  storage provider/emulator. Image roadguard-local/minio:source-7aac2a2, inspected
  IDsha256:8dd86058d6f71764dd5df0492c804b3fdb38675c8f82eae8d0b68d02850e7ac1;
  platform manifestsha256:b1f22619840e0c16e16cdc64227cdd2ec47f0ef713b2adb7cbc78787af37b643.
  Server version DEVELOPMENT.GOGET, go1.27.1 linux/amd64. This is local
  compatibility acceptance, not vendor-supported/deployment adoption.
- storage.compose.yml now requires ANH02_MINIO_IMAGE explicitly instead of a
  removed-tag default. Existing Compose structure retained; no orchestration
  framework added. Official source/download URLs and reproduction notes are
  in the existing demo README.

Subsequent isolated acceptance environment — CURRENT_VERIFIED:
- Named containers labelled roadguard.task=anh02-runtime-d2414844:
  roadguard-anh02-live-sql/minio/api/linux-tools. Loopback SQL14633, MinIO19000/
  console19001, API15000 were checked free before startup. SQL image remains
  existing2019-CU18. No root/shared Compose resources reused or pruned.
- Task-created databases RoadGuard_Anh02_Runtime_d2414844 (Windows) and
  RoadGuard_Anh02_Linux_d2414844 (Linux):44 production migrations each, explicitly
  owned targets. Private bucket roadguard-anh02-live-d2414844; anonymous403.
  MinIO data binds only artifacts/anh02-live/objects. SQL/MinIO/JWT secrets,
  tokens and scratch probes are ignored, never committed/logged. Acceptance
  scripts assert exact task DB; state is isolated from previous demo states.
- Production Program/Kestrel on Debian12/.NET8.0.31, SDK8.0.425; official image
  mcr.microsoft.com/dotnet/sdk@sha256:78235e09001f52b6592c458ac010775ebac6725422e80cd0c1650590f67b2743.
  API shares the owned MinIO namespace so127.0.0.1:19000 signed URLs work from
  both API and host client. Environment workers/mock explicitly enabled only
  Development. Minimal existing role/device/user seed classes reused; task-only
  membership/model/CRACK fixture matches existing HTTP tests. No Huy approval,
  triage/conclusion, matching output or complete inventory fabricated.

Gate results:
- LIVE STORAGE: production HTTP create/part URL/actual PUT/complete/worker
  VERIFIED/protected exact download, document+MP4/SRT+private PNG. Additional
 23 HTTP assertions passed: other-owner404, scope extras400, changed create/
  intake409, pending/failed content/intake409, stale complete412, original
  complete version replay202, same report identity. Scoped SQL checks below
  establish one receipt/graph; global counts not used for acceptance.
  Reporter role-row IsActive/P1 remains OPEN at unchanged Huy source; positive
  storage checks do not close it.
- LIVE EXPORT: reporting→atomic snapshot→real MinIO PDF/ZIP→protected download.
  Actual HTTP task cancel after admission changes current summary ACCEPTED→
  CANCELLED. Both stored snapshot/hash and SQL payload fingerprints unchanged;
  rendered frozen PDF retains ACCEPTED, omits CANCELLED. ZIP entries exactly
  manifest.json/evidence-index.json/dossier.pdf and matching snapshot identity.
  PDF download35131bytes SHA2561c2c789b2a9827e333d283a6848e643ba5b0c4bb43768fbfcf6f47ec3a59a70c;
  ZIP34419bytes SHA25618b7a9e7072f6fb5befa4d812a7642478d01010ddb387130b9d3241201dd4de9.
  Existing HTTP test now has explicit live-MinIO variant: injected unavailable
  GET leaves QUEUED/export_storage_unavailable, zero artifact/write; injected
  ACK loss after actual PUT+durable readback leaves QUEUED, restarted scope
  verifies/reuses the real object. One write/GeneratedArtifact/snapshot/receipt/
  admission+completion audit. Protected bytes match SQL size/hash; other PM403,
  revoked membership replay403, expired410, changed key payload409. These are
  injected faults, not a naturally occurring network incident.
- LINUX FONT/PDF: official DejaVu2.37 archive SHA256
  7576310b219e04159d35ff61dd4a4ec4cdba4f35c00e002a136f00e96a908b0a;
  DejaVuSans.ttf SHA2567da195a74c55bef988d0d48f9508bd5d849425c1770dba5d7bfc6ce9ed848954.
  Release LICENSE read (Bitstream/Arev notices retained beside external font);
  OS/2.fsType=0. Actual Linux configured path, Unicode/glyph text, embedded fonts,
  word bounds, Poppler rendering and visual review of all5 synthetic pages and
 4 live demo pages: no clipping/overlap/broken Vietnamese. Empty sample1page.
  Warm-cache production renderer probe as UID65534 denies nonexistent and
  root-owned chmod000 font with export_font_unavailable. Actual Linux HTTP
  admission/worker with missing font ends FAILED/export_font_unavailable, no
  success artifact. Restore configured font afterward. No font/dependency binary
  committed. Hosted CI/actual deployment acceptance NOT RUN.
- FULL CLI: prepare→survey→anh02 ran on Windows, then independently Linux.
  Fixed duplicate active BASELINE plan by using existing PERIODIC for separate
  AI task. Real fixture video→dataset→VIDEO_ANALYSIS result MODE=MOCK; private
  PNG→intake/replay; reporting→PDF/ZIP; hold/evaluation COMPLETE with all items
  BLOCKED_HOLD and INVENTORY_INCOMPLETE/HUY_REFERENCE_INVENTORY_UNAVAILABLE.
  No ELIGIBLE promotion/delete. New command journal persists original body/
  role/path/method/version before transport. Repeated complete Linux phases
  preserve scoped counts2plans/3tasks/2datasets/4exports/54receipts. Journal
  test drops ACK on real loopback HTTP transport and proves original version
  after fresh client state reload; changed payload rejected. Old states without
  command journals cannot reconstruct a lost original version automatically.
- ACTUAL 8 GiB: generated synthetic H.264 CBR128M via FFmpeg5.1.9 Debian tool;
 536.84s/320x180/25fps/13421frames. Encoded MP48586432090bytes followed by a real
 3502502byte ISO-BMFF free box to reach exactly8589934592 (no sparse/truncate).
  ffprobe confirms MP4/H.264/size/duration; complete FFmpeg decode exits0.
  Source hash streaming,1024 actual8MiB parts, PUT batches64, real complete/
  verifier, protected new-file download all pass. Source/download SHA256
  08ba3133e6dc0659a55862e692167b84e593aaa46f3725633dd41502909ea7e5;
  both8589934592bytes. SQL bigint/owner/scope/VERIFIED/receipt assertions pass.
  Explicit application ACK-loss injection discards a real complete HTTP response;
  fresh CLI client resumes original payload/key/version, same VERIFIED file,
  no second write/receipt. Long verification polling allows1800s (tiny remains60s).
  Command elapsed292.46s. Windows GetProcessMemoryInfo sampled every~2s:
  client PeakWorkingSet31330304bytes (29.88MiB);133 API /proc/1/status samples,
  max VmHWM1056388KiB (1031.63MiB), verifier shares same process. Includes earlier
  worker activity; peaks are run observations, not a process-wide proof for all
  concurrency, CPU/throughput SLA or approved performance threshold. MinIO
  point observation293MiB, not a sampled peak.

Fresh verification commands/results (distinct automatic tests30, all pass):
- `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --no-incremental --disable-build-servers -v quiet '-clp:ErrorsOnly;Summary'`:0 errors/98warnings, exit0,16.26s.
- `dotnet publish RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --disable-build-servers -o artifacts/anh02-live/api-final -v quiet -clp:ErrorsOnly`:exit0; personal local config absent.
- With explicit task-owned MinIO environment and DejaVu font:
  `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Live_MinIO_export|FullyQualifiedName~UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh02-live-storage.trx'`:2 executed/pass,0fail/skip. Export case overlaps final3 below; smoke contributes1 distinct.
- Final same environment API filter `FullyQualifiedName~Anh02ExportHttpTests`, TRX anh02-live-export-final.trx:3executed/pass,0fail/skip. Includes current unavailable fault and scoped SQL audit/receipt/hash assertions.
- Linux SDK container, copied existing unit test output, explicit /runtime DejaVu path:
  `dotnet vstest /unit-tests/RoadGuardSystem.UnitTests.dll '/TestCaseFilter:FullyQualifiedName~Anh02ExportUnitTests|FullyQualifiedName~Anh02ExportArtifactStoreTests' '/Logger:trx;LogFileName=anh02-linux-font.trx' /ResultsDirectory:/runtime/linux-test-results`:21executed/pass,0fail/skip.
- `python -m unittest discover -s tools/demo/anh01 -p test_setup_demo.py -v`:5executed/pass,0fail/skip. Total21+3+1+5=30 distinct; overlapping reruns excluded.
- `python tools/demo/anh01/setup_demo.py --validate-only`, `pwsh -File tests/CI/Verify-CiWorkflow.ps1`:exit0.
- `.tools/dotnet-ef.exe migrations has-pending-model-changes --context RoadGuardDbContext --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-build`:exit0/no model changes, temporary process-only unreachable migration connection; no DB write.
- Runtime scratch wrappers execute the unchanged CLI arguments documented in README: `python artifacts/anh02-live/run_demo.py prepare|survey|anh02`, `run_large.py`, `verify_demo_resume.py`; all final exit0. Wrappers/secrets/state/media are ignored scratch, not a committed framework.
- `python artifacts/anh02-live/live_http_checks.py`:23 live HTTP assertions,0failure final; `live_sql_checks.py`:12 scoped SQL assertions,0failure final; `freeze_exports.py admit|verify`:2 real export formats plus source change/hash/receipt/audit proof, exit0 final.
- `docker exec roadguard-anh02-linux-tools ffprobe ...`, complete `ffmpeg -v error -i /runtime/synthetic-eight-gib.mp4 -f null /dev/null`:exit0. Synthetic source command uses lavfi testsrc2, -fs8582000000, libx264 ultrafast/yuv420p,128M CBR,nal-hrd=cbr/force-cfr,g25,600s upper limit; free box fills the exact remaining3502502 bytes via64KiB writes.
- pypdf/pdfplumber/stdlib TTF table checks, embedded font/text/word bounds, exact ZIP entries/CRC/snapshot IDs/hashes and Poppler visual review pass. The first QA assertions incorrectly expected '1/2' text and index.json; actual contract renders separate numerator/denominator and evidence-index.json; corrected QA, no backend change.
- Compose config --quiet, git diff --check, exact selected-file/scope and secret review are final delivery checks recorded below. Postman routes/DTOs unchanged; existing collection retained, Postman HTTP runner NOT RUN.

Failure history retained: initial publish/DB isolation breach above; legacy seed
collision (not patched outside ownership); duplicate BASELINE plan409; old
supplement resume409 and red journal test1failure/4pass → fixed →5pass. First
synthetic MP4 mux overshot size8591552207; regenerated and completed exact valid
container. Scratch seed path/CA2201 compile failures selected0; fixed before
isolated seed. Export test added local name collision CS0136 selected0; renamed,
final3pass. Read-only impact probe initially wrong cwd, corrected before any
connection. First freeze verification called during API restart and received
RemoteDisconnected; health established and rerun passes. Missing fontTools in
QA was replaced by stdlib reading actual OS/2 table; no new package installed.
Build-cache cleanup was rejected by automatic approval review ('blocked by
policy'); cache retained, no alternate deletion attempted. No unresolved
functional test failure; shared DB recovery decision remains unresolved.

Self-review1 actually checked auth/privacy/current authority before receipt,
real byte paths, SQL fixture predicates, one durable artifact/write, ACK loss/
unavailable states, snapshot/current source drift, streaming/memory and long
polling. Fixed original-version journal, active-plan collision and polling;
retained Huy role-row gate. Self-review2 checked config precedence/publish
secrets, actual connection targets, ownership/cleanup, full same-state resume,
Linux permission/font/embedding/glyph/layout, compatibility/model/CI and final
diff. Reproduced/fixed the config root cause; recorded the shared DB incident
rather than claiming an isolated-only run. These are self-reviews, not ChatGPT
or peer review.

NOT RUN/PENDING: actual deployment target/font packaging, hosted GitHub CI,
Postman HTTP runner, real AI/provider/Android/Web, CRS transform/GPX/WGS84,
Huy role-row correction/approved-label/access/matching/case-defect readers/full
retention inventory and exact handoff, A08/A09/HUY02 exclusions, performance
threshold/backup restore, accidental DB before-image/complete row-impact audit
and owner recovery decision. No new Huy import/edit or rollback. ANH-01 and
ANH-02 remain Partial despite successful local runtime gates.

Final delivery checks: Compose config --quiet and git diff --check passed;
the ten changed tracked files match the Anh runtime scope. Selected diff was
checked against generated runtime credentials, bearer tokens and signed URL
signatures; none present. No Postman wire change or Huy/model/migration edit.
Stopped only the four containers whose roadguard.task label matched
anh02-runtime-d2414844; their containers, owned databases, objects and evidence
are retained. Go cache cleanup remains blocked by automatic approval review.
Delivery base is d2414844af1a3fd9a39547fc131f85bda451fad2; the normal commit's
HEAD and compare URL are reported in the handoff, with no amend/force-push.


## Owner dependency/Postman continuation — 2026-10-03 (Partial checkpoint)

Authority: latest owner continuation assigns G1–G10 first and G11 next, D1–D4=A;
no retired RoadGuard skill, new RF report, HUY-02 workflow or business decision.
Initial local/remote anh-review base: **03f5c62d23788ac3c8f3c99e559c3b9bd9d05fea**.
Fixed Huy import source: **afab3ecc8b33d8172e9b0c564808716ca067f39e**, Huy base
7e8261648e08adf5b5bdf0cea85fca5e55463b73. Its external PASS is handoff provenance,
not external review of this new Anh integration. Normal checkpoint HEAD is in the
Git handoff; no amend/force/merge/develop/main/Huy-branch mutation.

Initial dirty paths retained and continued: PostmanScenarioSeedStep.cs,
SeederTests.cs, Seeder Program.cs, new PostmanDisposableBootstrap.cs and
new tools/postman/start_local.py. Anh is the sole shared-root/schema/docs writer.
40 selected Huy module/domain/DTO/repository/service/test files are imported
from the fixed SHA; no blanket cherry-pick/merge or latest-tip selection. Exact
import comparison has one compile-only adaptation: Huy01ReporterReportsApiTests
class is partial because three pre-existing Anh partial files extend it. All
other imports match normalized source bytes; no behavioral Huy rewrite. Shared
receipt service/test blobs remain exactly 8fa41dc4914ba7f2f475d4802727a2f8f1ef9f88 /
7d4fb79ba442f2359f3e26368196c24fdbf67aec. Import allowlist and private execution
artifacts are retained locally under ignored artifacts/anh02-postman.

| Gate | Current result / evidence / remaining owner boundary |
|---|---|
| G1 Reporter authority | Anh producer now reads actual Users + active Roles in scoped context; private create/part/complete receipt paths lock and recheck before replay/conflict/recovery. Huy terminal role guard imported unchanged. New negative HTTP/SQL tests compile; fresh corrected race rerun BLOCKED by Docker. Not a full runtime P1 closure. |
| G2 schema/model | Existing typed Report/Case/SourceDecision mappings reused. Additive Defects shadow RowVersion migration only; nullable legacy project/route remain unchanged, populated downgrade fails closed. Native disposable target has 45 migrations and 8-byte version; EF no pending model. Earlier Huy fresh/baseline constraints/hydration tests passed in this run, but two new Defect tests require corrected rerun. Label hydration/schema remains PENDING, no guessed JSON aggregate/backfill. |
| G3 approved label/access | Existing IApprovedTrainingLabelReader and ITrainingSourceAccessReader reused, no fake binding. Huy TrainingLabel lacks durable current-head/revision ordering/concurrency and persistent file-version facts; revision lacks file version/SHA/int64/media/provenance/terminal review proof identity. REPORT policy exists; AI/FIELD adapters absent. Huy supplies actual domain/SQL readers, then Anh maps typed persisted members. |
| G4 matching/Defect | Existing candidate heads/decision constraints reused; Defect concurrency seam added. IMatchingCandidateSnapshotReader absent. Created-Defect source/provenance relation and segment/geometry/current-version facts plus Huy writer are absent. REPORT REJECT/correction exact module imported, opt-in bound. KEEP_NEW/LINK_EXISTING remain unavailable with zero effects; matcher unit fixtures do not establish producer acceptance. |
| G5 AI projection | ReadCandidateAsync now joins actual ProcessingJob/Attempt/Block and checks project, MOCK, completed job/attempt, model/dataset and exact manifest. Existing generated-frame provenance retained without fabricated UploadSession. Persisted-job drift HTTP test added; fresh pipeline rerun BLOCKED. No second job system or production provider claim. |
| G6 retention | Exact real named HUY contributor retained Complete=false, with closed-link/publication/conclusion/decision/history refs. Registered only with explicit Huy opt-in, never renamed REPORTER_INTAKE. One actual Huy SQL contributor test passed before Docker outage. Composite/evaluator storage acceptance remains BLOCKED; label/Defect-source/repair inventories explicitly missing, no deletion or ELIGIBLE promotion. |
| G7 Case/Defect reporting/dossier | Minimal internal typed anh-huy.case-defect.v1 / ICaseDefectReadReader boundary and real ReportingService/ExportService consumers implemented. Nine unit tests verify half-open report period, status stock, incomplete-not-zero, scope/hash/nested shape and no private-byte permission. Huy reader unbound; typed metadata/source refs freeze in admission without ZIP private-file authorization. Actual Huy producer→consumer PENDING. |
| G8 roots/routes | Default intake-only preserved. Explicit Huy01:EnableLifecycleAndCase=true adds six scoped lifecycle/Case/Candidate service/repository bindings once and HUY contributor. Fresh production-root HTTP test compiles, isolated mutation acceptance BLOCKED. Actual local API opted-in own Report list=200, PM private list=403, Reporter internal Case list=403. Canonical/Postman describe this distinction. |
| G9 session transports | Fixed Huy source still has no UserSession.Transport or LastActivityAt or Web/Android handlers. No shadow member/platform guess, cookie activation or legacy expiry change. Huy must supply typed LEGACY_BEARER/WEB/ANDROID members and touch/absolute state; shared mapping/policy adoption waits at that boundary. Full clocks/CSRF/rotation acceptance NOT RUN. |
| G10 events | Existing OutboxWorkRepository/NotificationOutboxConsumer/ConsumerEffectService and draft contracts/events inspected. Imported workflows persist audits, not an approved new event registry/consumer agreement. Five proposed report/case/candidate/label event names remain PENDING envelope/routing/consumer/effect-key/retry agreement; no unsupported/no-op event emitted. |
| G11 DB/seed/Postman | Exact authorized DB recreated once, production migrations applied, seed twice exits0, five fixture authorities/passwords validated. Newman actual network below; full real storage/upload/intake/AI/PDF/ZIP/hold/evaluator rerun BLOCKED by Docker/MinIO. Missing Huy facts remain PENDING, not fake seed. |

TARGET_CONFIRMED: owner now accepts RoadGuardPostmanTest on .\HANHNAV as
fully disposable, no recovery of old data required. This supersedes only the
previous unresolved recovery decision; the prior config/startup incident above
remains HISTORICAL and is not rewritten as never happening. The disposable DB
was recreated once, now coherent; no further reset needed. Current reset code
uses plain DROP and fails on another active session rather than terminating it.
Guard validates configured exact instance/catalog and live SERVERPROPERTY / DB_NAME
before reset/migrate/seed/launcher start. Other DBs/storage are not covered.
Config precedence and local-config publish exclusion are retained. Go cache
remains untouched after the automatic approval-review rejection.

Seed is synthetic: existing users/roles/memberships/project/road/segments,
device/catalog reused; synthetic Reporter and released mock model added without
Huy approval or fake verified files. Legacy migrated SurveyRequest linkage is
accepted only for its exact expected request/project/route; foreign linkage still
fails. Repeated runs do not add fixture copies. Live read-only SQL confirmed
HANHNAV/RoadGuardPostmanTest, 45 migrations, 5 users, 1 project, 0 files,
Defects.RowVersion length8. No source reference points to absent VERIFIED bytes.
Private JWT/storage/connection settings and tokens remain ignored/local only.

Fresh commands/results (reruns are not added into unique-test totals):

| Command/check | Executed/pass/fail/skip, exit / evidence |
|---|---|
| dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --disable-build-servers -v quiet -clp:ErrorsOnly | exit0, 0 errors, 94 warnings at final shared-source build; earlier imported build required partial-class adaptation. |
| dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~Huy01\|FullyQualifiedName~Anh02\|FullyQualifiedName~CaseDefectCaptureConsumerTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=anh-huy-unit-checkpoint.trx' | 122/122/0/0, exit0; official DejaVu font supplied via process-only ANH02_TEST_FONT_PATH. Unit/mock evidence only. |
| dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --disable-build-servers --filter 'FullyQualifiedName~PostmanDisposableGuardTests' -v quiet -clp:ErrorsOnly --logger 'trx;LogFileName=postman-disposable-target-guards.trx' | 5/5/0/0, exit0; no SQL connection for wrong/missing target or invalid flags. |
| .tools/dotnet-ef.exe migrations has-pending-model-changes --context RoadGuardDbContext --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-build | exit0, no pending model; temporary unreachable DesignOnly process setting restored, no DB I/O. |
| dotnet tools/RoadGuardSystem.Seeder/bin/Debug/net8.0/RoadGuardSystem.Seeder.dll --postman-disposable (twice); --verify-only | each exit0, exact target checked, no duplicate fixture seed; native probe evidence above. |
| node tools/postman/run_smoke.cjs PRIVATE_ENV.json '00 - Preflight' 'HUY-01 current authority preflight - opted-in' | Newman6.2.2; 10 requests /14 assertions /0 failures, exit0; ignored newman-preflight-result.json. Actual loopback15112 current published API, launcher-verified SQL target. |
| same runner with folders '00 - Preflight', 'ANH-02 assigned - AI reporting export retention' and --request=Login seeded Project Manager / Reporting summary / Training missing Huy approved reader | 3 requests /5 assertions /0 failures, exit0. Login200, reporting200, missing-label-reader503 producer_unavailable; ignored newman-reporting-result.json. The PM login overlaps the first run; no sum presented as unique tests. |

Failure history for this continuation is preserved: seed adoption RED reproduced
old validation; wrong collision fixture initially violated active-request unique
then cancellation check (changed to Completed, rerun pending). Initial API run
102=59pass/43fail: private multipart manual transaction outside retry strategy,
preflight update locks blocking duplicate-handler barriers and a non-SHA fixture
fingerprint were found/fixed. Initial SQL mix56=53pass/3fail: two new Defect
fixture statuses were invalid and seed collision metadata was invalid, corrected.
Initial unit113=108pass/5fail lacked font environment, final122 pass. First imported
build CS0260 required partial adaptation; nested-consumer edit CS1525 selected0
was fixed. EF model check initially failed because generated snapshot removed
existing ownership Restrict overrides; restored those in current snapshot only,
no applied migration edited, final model check passes. Fresh API102 rerun then
failed all102 during Docker fixture initialization (HTTP500), before test bodies;
it is infrastructure failure, not a source acceptance pass. Last bounded Docker
version timed out12s and MinIO health timed out3s. Owner was asked to restore
Docker Desktop; no daemon/TLS/global configuration restart or bypass was done.

Self-review1: inspected current actor/role and private source guards on every
receipt path, terminal lock ordering, post-rollback/recovery, multipart admission,
source job/attempt/proof joins, publication recipient versus case metadata,
closed-link retention, half-open metrics and frozen exports. Fixed preflight lock
ordering/private transaction handling and nested snapshot shape checks; retained
historical publication references without requiring current membership.
Self-review2: checked exact imports/one adaptation/protected blobs, defaults and
opt-in descriptor closure, migration history/nullability/downgrade/no applied
edits, snapshot drift, config target/publish isolation, fixture idempotence,
Postman dynamic GUID scripts and secret-free environments. Fixed snapshot
Restrict loss, added target guard tests and reusable safe Newman launcher/runner.
These are Codex self-reviews; new external ChatGPT review is PENDING.

NOT RUN/PENDING: corrected SQL races/private receipt HTTP suite/new production
root mutations/Defect fresh+baseline tests and final full composite retention;
real bytes Postman upload→intake→mock AI→PDF/ZIP→hold/evaluation after this
integration, because Docker Engine/MinIO unavailable. Existing previous local
MinIO/Linux PDF/demo/8GiB evidence is HISTORICAL reuse for unaffected project
upload path, not a fresh private-path regression pass; 8GiB deliberately not
rerun. No new external AI, deployment, CI, Android/Web, CRS or Huy reader/label/
matching/schema approval. ANH-01/ANH-02/HUY-01 remain Partial. Source checkpoint
is reviewable, not blanket runtime acceptance or deployment approval.


### Post-checkpoint G11 independent verification

Shared source checkpoint **2d45315f09e137ad688d0377eb07c8af439c6704** was
normally pushed to origin/anh-review, then this task continued immediately.
Compare: https://github.com/HoangAnhVu2207/RoadGuardSystem/compare/03f5c62d23788ac3c8f3c99e559c3b9bd9d05fea...2d45315f09e137ad688d0377eb07c8af439c6704
39/40 imported committed Git blobs are exactly equal to fixed afab3ecc; the sole
other blob is the documented partial-class compile adaptation. No message was
sent to another thread without owner authorization. Huy can consume this
checkpoint's typed dependencies while runtime/external gates remain open.

API republished from that checkpoint (dotnet publish API project --no-restore
--disable-build-servers -o artifacts/anh02-postman/api -v quiet -clp:ErrorsOnly,
exit0), restarted only the saved-PID task-owned launcher/API after checking
command lines. Guard again verified live SQL target; localhost15112 remains the
local Postman endpoint, startup migrate/seed=false. No Docker/global restart.

- Final Newman6.2.2 selected canonical leaves: health, all five role logins,
  Supervisor POST projects201, actual PM work-package200, reporting summary200,
  training unavailable503, Reporter own-list200 and role denials403. Command:
  node tools/postman/run_smoke.cjs PRIVATE_ENV.json with folders 00 - Preflight,
  01 - Supervisor flow (create project -> invite -> administer),
  02 - Project Manager flow (work package -> survey planning),
  HUY-01 current authority preflight - opted-in,
  ANH-02 assigned - AI reporting export retention; --request selects the thirteen
  leaves recorded in ignored newman-final-result.json. **13 requests /18
  assertions /0 failures, exit0**. These replace overlapping earlier smoke counts.
- Storage-independent PROJECT hold folder uses existing real production
  contracts, not synthetic File/VERIFIED rows. Command:
  node tools/postman/run_smoke.cjs PRIVATE_ENV.json '00 - Preflight'
  'ANH-02 project hold - storage independent'
  --export-environment=artifacts/anh02-postman/private-hold-resume.json.
  **14 requests /21 assertions /0 failures, exit0**; same-state rerun using the
  exported environment also14/21/0. Do not sum overlapping retries. Create201
  and same-key replay, PM read200/Crew403, release200 and same-key original-ETag
  replay; evaluator admission202 and real worker COMPLETE/read200 with zero
  actual files. Not a complete Huy inventory or eligible-file claim.
- Native read-only before/after resume: 45 migrations, 5 users, 2 projects
  (one legitimate new Postman smoke project), 0 files; Defect version8bytes;
  0 export jobs/snapshots/admitted audits/export receipts; 1 RELEASED project
  hold, 2 hold histories, 1 COMPLETE evaluation, 0 evaluation items, 3 retention
  receipts. Replays/denial add no second scoped hold/history/evaluation effect.
  Login/session effects are intentional and not asserted absent.
- Additional failure history: first independent hold run29 HTTP requests,
  35 passing assertions, one script failure/exit1 because polling8seconds was
  shorter than real worker15seconds. Increased bounded poll to22seconds;
  recovered the three actual durable receipts privately before retry, with no
  new SQL write/hold reset or mocked terminal state. Scratch receipt-resume
  CS8600/CA1305 builds selected0 and were fixed before SQL execution. Final
  same-state network runs and durable counts pass. Newman Node deprecation
  warning is recorded; no package upgrade.

G11 now has verified auth/project/read/privacy/training-unavailable and
storage-independent hold/replay/evaluation paths. Remaining real-byte upload,
Reporter intake/case mutations, mock AI and PDF/ZIP current-source pipeline,
file basis/full retention inventory and SQL fixtures remain Docker/MinIO
BLOCKED. No 8GiB rerun or gate inflation. API source has no post-checkpoint
change; final follow-up commits contain Postman/tooling/docs only. Final HEAD,
remote equality and dirty status are reported in the handoff. ANH packages
remain Partial; external review/deployment/CRS/Huy gates remain PENDING.


Final Postman follow-up self-review checked actual receipt/original ETag reuse,
empty-inventory evidence versus reusable nonempty inventory contracts, worker
interval and bounded retry, and private environment export. Removed the test's
hard-coded empty-item invariant (the observed DB still has zero files); final
same-state run is **14 requests /22 assertions /0 failures, exit0**, with actual
COMPLETE status and array shape checked. Earlier21-assertion runs are history,
not additional unique successes. Export rejects credentials in URL, tracked
paths and any destination outside ignored artifacts JSON. Node syntax, JSON
parse and diff --check pass. This tooling-only follow-up does not invalidate
checkpoint BE tests or require rerunning8GiB. Final native scoped counts remain
one released hold/two history/one complete evaluation/three receipts and zero
export effects; newman-hold-repeat-result.json is the final local network record.
## Multipart correction continuation from 47c9a539 (2026-10-03)

CURRENT_VERIFIED preflight: anh-review, base 47c9a539549e5fbf839ea4bec7da047baaeaecb2, clean working tree. Anh reserves UploadPersistenceService, UploadSession, related Anh tests, and this summary; shared receipt primitive remains unchanged. Docker version/ps now succeed; fresh fixture execution is required before promoting any prior PASS to this correction.

Immediate Huy handoff (fixed source afab3ecc8b33d8172e9b0c564808716ca067f39e, no newer branch adoption):

- Labels: TrainingLabel has only in-memory Revisions and dictionary _fileVersions; CurrentRevision/CurrentApprovedRevision/CurrentFileVersion are computed, not SQL heads. Required durable head/revision ordering, current revision identity, concurrency token and persisted revision file version are absent. TrainingLabelRevision already has Id/LabelId/Revision/SourceId/SourceVersion/FileId, bbox/type, Status/ReviewedByUserId/ReviewedAt/ReviewReason. Missing immutable FileVersion/Sha256/SizeBytes (Int64)/MediaType, approval proof identity (ApprovalId), processing/model/dataset/Mode/SegmentId provenance. These can come from authoritative joins; do not fabricate columns or approval from status alone. REPORT current-approved policy exists; actual SQL writer/reader and AI/FIELD policy adapters remain Huy-owned.
- Session: UserSession currently has Id/UserId/IssuedAt/DeviceMetadataJson/ExpiresAt/RevokedAt/RowVersion only. Transport and LastActivityAt are missing, as are typed WEB/ANDROID creation/touch handlers. LEGACY_BEARER/WEB/ANDROID typed transport, authoritative activity/absolute expiry state and rotation/revocation integration must be supplied before Anh mapping; DeviceMetadataJson is not transport authority. No guessed cookie/session activation.
- Existing exact reader contracts: Services/Interfaces/Integration/Anh02Contracts.cs defines IApprovedTrainingLabelReader.CaptureApprovedAsync(actorId, role, projectId, TrainingLabelFilterV1, ct) -> ApprovedLabelSnapshotV1?; ITrainingSourceAccessReader.CanReadAsync(actorId, role, projectId, Guid[] fileIds, ct) -> bool; IMatchingCandidateSnapshotReader.CaptureAsync(actorId, role, projectId, routeVersionId, segmentSetId, geometryVersion, detectionIds, requestedSnapshotId, ct) -> MatchingCandidateSnapshotV1?; ICaseDefectReadReader.CaptureAsync(actorId, role, projectId, ReportingFiltersDto, ct) -> CaseDefectSnapshotV1?. Exact DTO members reside in that file and DTOs/Reporting/CaseDefectReadDtos.cs. Capture must enlist in caller scoped transaction or provide durable immutable snapshot; null is unavailable, not empty. Current resource access rechecks historical export permission. Case/report receivedAt interval is [from,to), Case/Defect status is stock; MissingReasons is explicit and evidence refs do not grant private byte access. These contracts are already supplied for implementation, but real Huy bindings/adoption remain PENDING.

Source finding confirmed by owned SQL reproduction: private GetPartUrlsCoreAsync invoked non-idempotent InitiateAsync inside a retryable receipt handler. The rollback fault produced two storage calls. A lost-storage-acknowledgement scenario produced three calls across requests. Correction commits a guarded initialization fence in existing UploadSession.FailureCode before storage, then calls storage outside SQL retry/transaction; the acknowledged ID is captured once and reused by receipt attempts. Private auth is rechecked on claim and handler/replay/recovery; session-owned app lock and one durable part/receipt remain. No shared primitive, migration, identity implementation, Huy source, processing implementation, branch merge or storage deletion changed.

Clear recovery boundary: SQL rollback retry within the same call uses the known ID; durable receipt after lost SQL commit acknowledgement replays. If SQL retries exhaust, storage acknowledgement is lost, or process dies before ID/receipt persistence, the durable unresolved claim blocks same/new keys with storage-unavailable; zero part receipts are fabricated. There is no reliable list/abort/reconciliation capability in IUploadObjectStorage, so no automatic claim reset or second initiation. Reconciliation is NOT IMPLEMENTED and requires separately authorized work. Even storage failure before creation remains ambiguous at this interface. This availability limitation is deliberate fail-closed behavior, not a claim of atomic SQL+storage or cross-process exactly-once recovery.

Self-review pass 1: reviewed physical I/O versus SQL retry/commit boundaries, SQL guard order/current authority, same-key competition and acknowledgement recovery. Fresh G1 execution found new private CreateAsync handler missing its own terminal guard (replay guard was present); added guard to new create and complete handlers without changing shared IdempotencyOperationService. Negative direct-caller test now checks revoked create/replay/conflict/parts/complete with unchanged durable effects.

Self-review pass 2: checked existing nullable FailureCode shape/length (no schema drift), compatibility, shared writer/file scope and producer/consumer boundaries. Test fault waits for storage initiation before launching the contender; storage asserts no active SQL transaction. Corrected test fixture role mismatch to an isolated constrained SQL state change (EF forbids direct production RoleCode mutation), and restoration uses a fresh context after receipt tracking clear. Missing Huy inventory/readers remain explicit; local Postman instructions/contract record unresolved-claim behavior. Shared receipt source blob remains 8fa41dc4914ba7f2f475d4802727a2f8f1ef9f88; shared guard test remains 7d4fb79ba442f2359f3e26368196c24fdbf67aec.

Failure/evidence history preserved: initial private-multipart-red.trx was 0 PASS/3 FAIL. Two failures prove duplicated external calls; the post-commit acknowledgement case also exposed an incorrect test expectation of Executed (shared semantics correctly return Replayed), subsequently corrected. Intermediate private multipart run was 3/3 PASS before final extra recovery tests. Intermediate corrected-g1-g2-sql was 58/58 PASS; superseded by final 60/60 below. Initial corrected-g1-g5-g6-g8-http-sql was 65 PASS/2 FAIL: one real terminal create-guard assertion and one invalid role-mutation fixture setup/cleanup, not infrastructure failures. Both corrected and rerun on current source; no previous-source PASS is reused. No Docker/SQL fixture initialization failure occurred in these runs; Docker was already healthy at continuation entry, no global daemon/settings/restart action was needed.

Fresh executed commands (logs/TRX are ignored under artifacts/anh02-postman/correction; synthetic storage is MOCK_VERIFIED, SQL and HTTP producer/consumer guards are BE_VERIFIED):

| Command/filter | Result |
|---|---|
| dotnet test tests/RoadGuardSystem.IntegrationTests --filter 'FullyQualifiedName~PrivateMultipartRetrySqlTests\|FullyQualifiedName~UploadPersistenceSqlTests\|FullyQualifiedName~AnhHuyDependencySchemaTests\|FullyQualifiedName~ReceiptAccessGuardSqlTests\|FullyQualifiedName~SeederTests' --logger 'trx;LogFileName=corrected-g1-g2-sql-final.trx' --results-directory artifacts/anh02-postman/correction -v quiet | exit0;60 PASS/0 FAIL/0 SKIP. Includes 5 new multipart fault/recovery cases,3 existing upload cases (8GiB metadata only),2 fresh/baseline nullable Defect version tests,32 unchanged shared guard tests and18 seed cases. |
| ANH02_TEST_FONT_PATH=local licensed DejaVuSans.ttf; dotnet test tests/RoadGuardSystem.ApiTests --filter 'FullyQualifiedName~ReporterEvidenceApiTests\|FullyQualifiedName~Huy01RoleAuthoritySqlTests\|FullyQualifiedName~Huy01ReporterReceiptAccessSqlTests\|FullyQualifiedName~Anh02AiHttpTests\|FullyQualifiedName~Huy01RetentionInventorySqlTests\|FullyQualifiedName~Anh02RetentionHttpTests\|FullyQualifiedName~AnhHuyProductionBindingTests\|FullyQualifiedName~Huy01CaseSqlTests' --logger 'trx;LogFileName=corrected-g1-g5-g6-g8-http-sql-final.trx' --results-directory artifacts/anh02-postman/correction -v quiet | exit0;67 PASS/0 FAIL/0 SKIP. G1 private12/Huy authority1/receipt27; G5 actual persisted-job drift and pipeline1; G6 real HUY contributor1 and HTTP hold/evaluation2; G8 actual production-root opt-in1; actual Case SQL22. G8 production-root test verifies bindings/own-list/role denial, not every lifecycle HTTP mutation. |
| dotnet test tests/RoadGuardSystem.IntegrationTests --no-build --filter 'FullyQualifiedName~Anh02RetentionPersistenceTests\|FullyQualifiedName~Anh02ReceiptAuthorizationTests' --logger 'trx;LogFileName=corrected-g1-g6-composite.trx' --results-directory artifacts/anh02-postman/correction -v quiet | exit0;32 PASS/0 FAIL/0 SKIP. Composite inventory/holds/basis drift and real caller authorization/recovery. Complete-Huy scenarios still use explicit fixture contributors; real HUY inventory remains incomplete. |
| dotnet test tests/RoadGuardSystem.IntegrationTests --no-build --filter FullyQualifiedName~Huy01SharedSchemaTests --logger 'trx;LogFileName=corrected-g2-hydration.trx' --results-directory artifacts/anh02-postman/correction -v quiet | exit0;4 PASS/0 FAIL/0 SKIP. Actual fresh/baseline aggregate hydration, relational constraints, empty Down/Up and domain mapping. |
| dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore -v minimal | exit0;0 errors/0 warnings in this incremental build. Test compilation logs retain existing analyzer warnings; no package upgrade. |
| git diff --check; shared source/test blob checks | exit0; unchanged shared receipt hashes above. Only seven intended Anh source/test/contract/docs paths selected; no initial dirty paths existed. |

NOT RUN on this correction: full suites, external AI/Web/Android/deployment/CRS, real approved-label/matching/CaseDefect reader adoption, live storage fault injection, full network Postman export/demo flow and actual8GiB. No project-upload behavior/config change requires an8GiB rerun; old live-storage/demo/8GiB evidence remains HISTORICAL at its recorded revision. Prior incident/recovery decision and Go cache refusal retained. ANH-01/ANH-02/HUY-01 remain Partial; external ChatGPT review PENDING.
