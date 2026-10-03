# HUY-01 — Identity transport → Reporter/case → PM candidate/label

## 2026-10-03 mandatory Label / Session / CaseDefectRead checkpoint - PARTIAL

`CURRENT_VERIFIED` preflight: `huy-review` started at local/tracking/live
`67fb0699d269878be36a0433911f3a3fc017a7e0`; worktree was clean and no
continuation was displaced. The exact Anh source handoff was inspected by
fetching `origin/anh-review` at `f7a32dc...`; final handoff docs were inspected
at `47c9a539549e5fbf839ea4bec7da047baaeaecb2`. No Anh branch was merged and no
shared migration, DbContext, DI, Postman or ANH-02 consumer source was imported.
The only handoff boundary brought into this Huy checkpoint is the additive
`Anh02Contracts.cs` / reporting DTO shape required by `CaseDefectRead`; its
runtime binding remains disabled.

### Mandatory slice

- **Training Label:** `TrainingLabel` now exposes explicit current revision
  number/id and a row-version carrier; `TrainingLabelRevision` persists the
  immutable file-version alongside source/version, and `Materialize` sorts and
  validates a contiguous revision history before establishing the current head.
  Existing PM-only review, terminal decision and new-PENDING eligibility rules
  are preserved. This is source/domain verified only; no TrainingLabels schema
  or approved-label SQL reader exists.
- **Session:** `UserSession` now has typed `SessionTransport` values
  `LegacyBearer`, `Web`, `Android`, nullable `LastActivityAt`, Web idle expiry
  (30 minutes, `now >= deadline`) and a non-reviving `Touch` operation. Absolute
  `ExpiresAt` is never extended. Existing legacy sessions default to
  `LegacyBearer`. Because convention mapping sees both new members, this is
  `SOURCE READY / SCHEMA PENDING`; no startup or SQL acceptance was run.
- **CaseDefectRead:** `CaseDefectReadReader` uses the caller-scoped DbContext,
  rechecks current actor/role/project membership, reads active case/report links,
  current case status/conclusion/publication references and project Defects,
  and computes a stable snapshot hash. Missing source/version/geometry/evidence
  authority is returned explicitly in `MissingReasons`; it is not converted to
  an empty or complete dossier. The reader is not production-registered.

### Changed files and ownership

Huy-owned source/tests: `RoadGuardSystem.BusinessObjects/Labels/*`,
`RoadGuardSystem.BusinessObjects/Identity/SessionTransport.cs`,
`UserSession.cs`, `RoadGuardSystem.Services/Implementations/Integration/CaseDefectReadReader.cs`,
and focused unit tests. The exact additive boundary files are
`RoadGuardSystem.Services/Interfaces/Integration/Anh02Contracts.cs` and
`RoadGuardSystem.DTOs/Reporting/{ReportingDtos,CaseDefectReadDtos}.cs`.
No DbContext/configuration/migration/snapshot/shared DI/canonical HTTP consumer
was edited. `docs/superpowers/plans/2026-10-03-huy01-label-session-casedefect.md`
is the local implementation plan.

### EF/model impact and Anh integration delta

`TrainingLabel` remains unmapped in the current model, so its new members are
source-only. `UserSession.Transport` and `LastActivityAt` are public mapped
members discovered by EF convention: add `Sessions.Transport` with an explicit
legacy backfill, nullable `Sessions.LastActivityAt`, transport check constraint,
and compatible row-version/update semantics in an additive migration. Anh owns
that migration, model snapshot and shared mapping. Before binding the reader,
Anh must provide persisted Defect source identity/version, rowversion, geometry
route/segment/version facts, current source/disposition links, evidence checksum/
media/version and recipient-scoped publication facts, plus the scoped transaction
and DI registration order. No reader SQL/HTTP acceptance is valid until those
facts and schema are integrated.

### Verification - CURRENT_VERIFIED

| Command | Executed | Result | Level |
|---|---:|---|---|
| `dotnet build RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj --no-restore -v minimal -clp:ErrorsOnly` | yes | exit 0; 75 existing warnings, 0 errors | build |
| `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore -v minimal -clp:ErrorsOnly` | yes | exit 0; 0 errors | build |
| `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'TestCategory=HUY-01|Package=HUY-01|FullyQualifiedName~Huy01' -v minimal -clp:ErrorsOnly` | yes | 48 passed, 0 failed, 0 skipped, exit 0 | unit/module |
| focused Label/Session filter | yes | 19 passed, 0 failed, 0 skipped, exit 0 | domain/materialization |
| `git diff --check` | yes | exit 0 | patch |
| `.tools/dotnet-ef.exe migrations has-pending-model-changes ...` | no | NOT RUN: local `.tools/dotnet-ef.exe` is absent; Session model drift is independently known from convention-mapped public members | EF/model |

SQL persistence, concurrency, HTTP, exporter, approved-label reader,
CaseDefect consumer and deployment acceptance remain `BLOCKED/PENDING` on the
schema/producer/DI handoff above. Historical Anh/Huy counts in earlier sections
are not fresh evidence for this checkpoint. Self-review pass 1 checked current
authority/privacy, immutable label history, session expiry/touch and incomplete
snapshot semantics; pass 2 checked ownership/import closure, EF drift, contract
compatibility and final diff. New external review is `PENDING`; HUY-01 remains
`PARTIAL` and this checkpoint stops before the next capability.

Ngày: 2026-10-02. Writer: Huy / Codex local. Nhánh: `huy-review`.

## 2026-10-03 independent-scope checkpoint - PARTIAL

`CURRENT_VERIFIED` preflight: branch `huy-review`; initial local HEAD, tracking ref
and live `origin/huy-review` all
`afab3ecc8b33d8172e9b0c564808716ca067f39e`; no dirty or untracked paths.
The reviewed checkpoint is equal to HEAD, so no intervening Huy commit or
unrelated work was displaced. Historical A/B sources remain
`715ade2c20f652b77c8c7e995c76bb5d47ead966` and
`a46b97b271b4af1b9e89dc182b5e3fc7a10e8fff`; no newer Anh tip was
accepted as a handoff. This checkpoint changes planning/evidence only.

`TARGET_CONFIRMED` from the owner-supplied external ChatGPT review: the exact
`7e8261648e08adf5b5bdf0cea85fca5e55463b73` to `afab3ecc8b33d8172e9b0c564808716ca067f39e`
diff is `PASS`, with no actionable P0/P1/P2. Reporter P1 is `CLOSED` **only**
for intake and receipt guard. This review did not execute tests or close Anh's
private-file producer role gate, readers, consumers or deployment. Older C1-C3
and Reporter P1 `PENDING`/proposed-closure sentences below are historical at
their recorded SHAs, superseded by this scoped verdict. Any future code diff
needs its own external review. `deliveryStatus=PARTIAL`.

### Capability inventory at `afab3ecc`

| Capability | Current implementation | Contract/spec source SHA | Acceptance evidence | Independent Huy work remaining | Anh dependency | Next step |
|---|---|---|---|---|---|---|
| Reporter intake/receipt guard | Production intake-only binding; current Users/role/file locks, receipt replay and recovery | HUY-01 §6/§8 at `afab3ecc`; shared receipt `f626ea5`; A `715ade2` | Real disposable SQL/API 65/65 in the overlapping HUY/Reporter filter; external diff `PASS` for this scope | None identified; preserve reviewed code | Anh private-file producer role-row gate and canonical role sentence remain separate | Anh corrects producer/contract; rerun affected producer-consumer gates after exact handoff |
| Reporter lifecycle and Case workflows | Module service/repository/controller for own list/detail/supplement, scoped triage/link/split/conclusion/publication; production routes other than intake fail closed | HUY-01 §6/§9.4 at `afab3ecc`; A file/geometry facts `715ade2` | Domain 47/47 and overlapping API/SQL 65/65 include scoped workflows; not production activation | No independently implementable missing behavior identified | Production DI/canonical route adoption; FIELD/DRONE proof, verified Defect links and warranty facts | Receive reserved facts/binding SHA, then run real HTTP/SQL publication and scope fixtures |
| Candidate/matcher/Defect domain | REPORT REJECT/correction SQL/HTTP; pure matcher and Defect rules; KEEP_NEW/LINK_EXISTING fail closed | HUY-01 §7/§9.4 at `afab3ecc`; B matching/AI interface `a46b97b` | HUY unit 47/47 and API/SQL 65/65 cover existing REPORT scope, not target effects | None independent of target/current-head facts | Defect rowversion, typed source links/created-Defect relation, route/segment/metric geometry and AI/FIELD provenance | After exact schema/producer handoff, implement target effects and snapshot consumer; test drift/concurrency |
| Label lifecycle | Unbound `TrainingLabel` head/revision and PM policy; current-approved eligibility resets on new PENDING revision | HUY-01 §7.2 at `afab3ecc`; B approved/source access interfaces `a46b97b` | Domain-only tests in 47/47; no SQL reader/HTTP/export PASS | None without authoritative source/approval storage | Label head/revision/review schema, rowversion/current-head uniqueness, PM approval and file/source/checksum provenance | Persist and bind only after exact schema/producer handoff; test SQL approval/revision races |
| Approved-label reader | No local B interface import or Huy implementation; obsolete §7.2 cursor proposal is superseded | `Anh02Contracts.cs` at `a46b97b`: `CaptureApprovedAsync` | NOT RUN; domain eligibility is not reader acceptance | Acceptance matrix below is ready; no fake reader | Current approved SQL heads, immutable revision/proof, same scoped SERIALIZABLE export transaction or durable immutable snapshot | Import reserved B interface when consumer graph is handed off; implement SQL capture and exporter fixture |
| Training source-access reader | No implementation; private reference does not grant public access | B `ITrainingSourceAccessReader.CanReadAsync` at `a46b97b` | NOT RUN | None without current resource permission producer | Current file ACL/owner/project/publication source permission and caller transaction | Check live source access even for historical export; test revoked/private file |
| Matching snapshot reader | Pure matcher exists; no target snapshot reader | B `IMatchingCandidateSnapshotReader.CaptureAsync` at `a46b97b` | Domain-only matcher tests; real reader NOT RUN | None without persisted target/scope facts | Defect version/segment/route, authoritative source geometry/disposition, metric CRS and transaction locks | Capture current scoped snapshot; test project/route/geometry drift and no-GPS availability |
| AI candidate consumption | REPORT producer only; AI branch returns not-ready | B `IAiCandidateFactsReader.ResolveAsync`/`AiCandidateFactsV1` at `a46b97b` | AI external/provider NOT RUN; mock does not prove this chain | None without actual detection producer | Processing detection/job/attempt/result/model/dataset, video/frame versions and disposition facts | Bind real producer after exact handoff; verify BE chain, preserve generated frame provenance |
| Retention contributor/full inventory | Huy SQL contributor reads direct/report/case/history obligations with deterministic hashes, unbound and always incomplete | B retention boundary `a46b97b`; HUY C3 `afab3ecc` | Real disposable SQL included in 65/65; composite consumer NOT RUN | No missing pure version/reference logic established by current schema | Label/review refs, typed Defect source links, repair-reference facts if present, and Anh composite DI | Add only queryable obligations after schema handoff; rerun closed-link/version/privacy and composite acceptance |
| Case/Defect reporting/dossier read | Case source exists; no frozen typed reporting capture | HUY §9.4 proposal at `afab3ecc`; B `a46b97b` has no interface | NOT RUN | Contract/acceptance proposal below; no parallel shared API | Anh confirms consumer signature, scoped transaction and Defect/source/geometry facts | Implement Huy read after exact consumer contract handoff; test Report flow vs Case stock |
| Auth/session transport and event gates | Legacy auth retained; no new transport binding or report event | HUY §5/§8/§9.4 at `afab3ecc` | Legacy auth HTTP 37/37; Web/Android new transport and outbox NOT RUN | None without shared transport/event adoption | Anh Sessions.Transport/LastActivityAt/backfill, auth options/middleware, canonical event envelope/consumer | Integrate only reserved transport/event checkpoint; test clocks/CSRF/rotation/outbox separately |

The evidence columns describe overlapping historical runs, not one combined
distinct test total. Domain PASS does not make any unbound SQL reader ready.
Intake-only production DI is active; full lifecycle/Case/Candidate/Label
production DI is not. No HUY-02 or retention deletion is part of this inventory.

| Acceptance capability | Status at `afab3ecc` | Evidence type and limit |
|---|---|---|
| Reporter intake/receipt guard | PASS in scoped BE SQL/HTTP; external review PASS | Real disposable SQL/API and reviewed diff; not Anh's private-file producer or deployment |
| Reporter lifecycle/Case and REPORT REJECT | PASS in module/test-host BE SQL/HTTP; production activation BLOCKED | Domain plus real SQL/API; non-intake production services not composed |
| Matcher, Defect and Label policy | PASS for independent domain only | Unit tests; no matching reader, target effect or Label SQL implied |
| Approved-label/source-access/matching readers and AI consumer | BLOCKED; integration NOT RUN | B signatures inspected at `a46b97b`; source/schema/producer and consumer graph missing |
| Huy retention bounded contributor | PASS for existing SQL references; full inventory BLOCKED | Real SQL fixture; contributor deliberately incomplete/unregistered |
| Case/Defect reporting and dossier consumer | BLOCKED; integration NOT RUN | Huy §9.4 proposal only; no frozen B reader/consumer contract |
| Legacy auth regression | PASS for existing HTTP; new transport BLOCKED/NOT RUN | 37 historical HTTP tests; no Web/Android transport claim |
| Events, external AI/Web/Android, Postman network and deployment | NOT RUN | No agreed event/consumer or live external/deployment evidence |

### Reader, retention and reporting acceptance boundary

- **Approved labels, BLOCKED:** `CaptureApprovedAsync(actorId, role, projectId,
  filters, ct)` must select only current APPROVED heads in one scoped SQL
  snapshot. PENDING/REJECTED and an older approval behind a new PENDING head
  are ineligible; require unique current heads, non-null normalized annotation,
  actual PM/project approval proof, typed source/file version, checksum/size/
  media and AI provenance when applicable. Wrong project/current authority is
  denial, not an empty success. The returned snapshot ID/hash must remain
  consistent with the exporter's same SERIALIZABLE transaction or a durable
  immutable producer snapshot. Existing pure `CurrentApprovedRevision` tests
  prove only in-memory eligibility, not SQL integrity or exporter behavior.
- **Historical download, BLOCKED:** `CanReadAsync(actorId, role, projectId,
  fileIds, ct)` checks current resource permission for every requested file,
  including private Reporter files, even when an export references an old
  immutable approved revision. Historical export identity does not require
  the label to remain latest, but cannot waive current source access. Test
  revocation and cross-project/other-owner access with the real file producer.
- **Matching, BLOCKED:** `CaptureAsync` must return same-project target Defect
  IDs/versions, route and segment-set versions, source/disposition and geometry
  snapshot identity/hash under the caller's transaction. Stale route/set/source/
  target or wrong scope fails explicitly. No-GPS/incomplete metric geometry is
  represented as unavailable with no fabricated distance/CRS; matcher GET
  remains read-only and cannot auto-merge or create target effects.
- **AI candidate, BLOCKED:** `ResolveAsync` must establish detection/project,
  job/attempt/result, model/dataset, source video and generated frame IDs and
  versions, geometry/position and active disposition. Missing/stale/wrong-scope
  facts have distinct failures. An AI-generated frame is not a Reporter-private
  UploadSession. `MODE=MOCK` supports BE mock evidence only, not external AI.
- **Retention, BLOCKED:** add label/revision/review and typed Defect/source
  obligations to `ReadAsync` and project-file discovery only when persisted
  links exist; identify repair references via the responsible producer rather
  than treating no rows as no obligation. Keep historical closed links,
  deterministic inventory version drift and `Complete=false` until every
  contributor is real. Anh's composite consumer and registration remain
  unverified; no deletion or hold bypass follows from this inventory.
- **Case/Defect dossier, BLOCKED:** proposed scoped capture needs actor/current
  role/project, Case/Report IDs and revisions, status, recipient-authorized
  conclusion/publication/evidence, and explicit unavailable Defect/source/
  geometry facts. Report flow filters `ReceivedAt [from,to)`; current Case stock
  is not a received-in-window count. B has no frozen reader signature, so Huy
  has not created a parallel interface or claimed ANH-02 reporting acceptance.

### Evidence provenance and verification

The historical command/output evidence below was recovered from the actual
Codex session log at
`C:/Users/dell/.codex/sessions/2026/10/02/rollout-2026-10-02T10-56-31-01a0fac1-aa0d-7652-9838-3abbfe68743a.jsonl`.
That local log is not committed and contains broader session data; only the
commands and sanitized counts are recorded here. No HUY TRX artifact was found
in this checkout. Results are historical at source `afab3ecc`, not fresh tests
for this documentation-only checkpoint; overlapping reruns are not summed.

| Source | Exact recovered command | Executed / passed / failed / skipped | Exit/artifact | Evidence scope |
|---|---|---|---|---|
| `afab3ecc` | `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --disable-build-servers --nologo -v q -clp:ErrorsOnly` | build succeeded, 0 warnings/errors; test counts N/A | exit 0 in session; session log above | Affected API compile only |
| `afab3ecc` | `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'Package=HUY-01' --logger 'console;verbosity=minimal' -v q -clp:ErrorsOnly` | 47 / 47 / 0 / 0 | exit 0 in session; same log | Huy domain/policy units |
| `afab3ecc` | `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter 'Package=HUY-01\|FullyQualifiedName~Huy01Reporter' --logger 'console;verbosity=minimal' -v q -clp:ErrorsOnly` | 65 / 65 / 0 / 0 | exit 0 in session; same log | Overlapping Huy API/disposable SQL, not full consumer activation |
| `afab3ecc` | `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~Huy01IdentityBoundaryTests\|FullyQualifiedName~ReceiptAccessGuardSqlTests\|FullyQualifiedName~P202ServiceContractTests\|FullyQualifiedName~P202TransactionAndIdempotencyTests' --logger 'console;verbosity=minimal' -v q -clp:ErrorsOnly` | 57 / 57 / 0 / 0 | exit 0 in session; same log | Shared guard/P202 plus Huy identity SQL; not Reporter reader/exporter |
| `afab3ecc` | `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter 'FullyQualifiedName~AuthenticationFlowTests\|FullyQualifiedName~V2AuthenticationFlowTests\|FullyQualifiedName~V2IdentityOnboardingFlowTests' --logger 'console;verbosity=minimal' -v q -clp:ErrorsOnly` | 37 / 37 / 0 / 0 | exit 0 in session; same log | Legacy auth HTTP regression, not new transport |

This turn changes documentation only; no runtime test result is promoted or
replaced. Fresh checks: `git ls-remote origin refs/heads/huy-review` returned
`afab3ecc`; `git rev-list --no-walk` resolved all four recorded A/B/shared/Huy
SHAs (exit 0); the new inventory, acceptance, command and dependency Markdown
tables have consistent 7/3/5/5 columns respectively; no new links were added.
`git diff --check` exited 0 before commit. No build, test, SQL fixture, Postman
runner or external consumer was run in this docs-only turn. Self-review pass 1
checked auth/privacy, transaction and current-source claims against the scoped
BE evidence, preserving the externally reviewed code. Pass 2 checked exact
command provenance, historical versus current statuses, B interface signatures,
shared writer scope and the final one-file diff; the separate acceptance matrix
and explicit `NOT HANDED OFF` column were added during this pass.

### Exact dependency handoff matrix

No new coherent Anh checkpoint or import allowlist was handed off after A/B.
Every entry below has checkpoint `NOT HANDED OFF`; the named shared writer is
Anh. Existing source evidence is HUY §9.4 at `afab3ecc`, B contracts at
`a46b97b`, and the current source paths in the capability inventory.

| Capability | Missing facts/guarantee owned by Anh | Checkpoint | Huy action after exact handoff | Unblocking acceptance |
|---|---|---|---|---|
| Label/approved export/source access | TrainingLabel head/revision/review tables, unique current head + terminal review, rowversion/restrict FKs; authoritative file checksum/version/media, source job/model/dataset and current file ACL; B reader graph reserved for import | NOT HANDED OFF | Implement SQL label commands and B `IApprovedTrainingLabelReader`/`ITrainingSourceAccessReader` using one scoped transaction | Pending/rejected excluded, new revision invalidates current eligibility, old export immutable but access current, PM/project/race/rollback SQL and real exporter consumer |
| Candidate matching/KEEP/LINK/AI | Defect ID/rowversion/route/segment, typed Report/AI source-link and created-Defect relation, one active disposition, metric geometry/scope and current source versions; actual AI job/attempt/result/frame provenance | NOT HANDED OFF | Implement B matching snapshot and AI consumer, target effects without duplicate Defect/task | Two-project/no-GPS/stale target/source/route, correction downstream-use, concurrent decisions and durable atomic effects |
| Retention/composite | Queryable label and Defect/source links, responsible repair-reference producer (or explicit absence proof), composite DI/transaction contract | NOT HANDED OFF | Extend Huy contributor obligations; keep incomplete until all sources covered | Closed/historical links, version drift, privacy, complete composite evaluation and denial on missing contributor |
| Case/reporting/remaining Case producers | B reporting consumer signature not frozen; recipient-scoped Defect/source evidence, FIELD completion, DRONE dataset/file, warranty facts | NOT HANDED OFF | Add typed Case/Defect capture only against frozen interface; finish evidence branches | Report-flow vs Case-stock query, current scope and recipient privacy with real producer/consumer |
| Auth/session/events and full module binding | Sessions.Transport/LastActivityAt/backfill, cookie/CSRF/idle options/middleware, canonical event envelope/consumer and reserved production DI/HTTP/Postman adoption | NOT HANDED OFF | Integrate Huy transport/services after reserved shared SHA; emit only agreed event/outbox | Legacy + Web/Android clock/CSRF/rotation SQL/HTTP, full module routes, outbox retry and consumer fixtures |
| Anh private-file role/canonical correction | `AnhHuyFactsRepository.IsCurrentActorAsync` role-row `Roles.IsActive` recheck and stale A canonical role statement | NOT HANDED OFF | Re-run affected Reporter producer/consumer tests; retain reviewed Huy receipt guard | Inactive-role private-file preflight and current receipt denial; canonical contract agrees with behavior |

Integration order per capability: Anh supplies exact SHA, reserved paths and
parent/dependency closure; Huy verifies against current HEAD and imports only
the allowlist; Anh migrates only a fresh disposable SQL database and binds
shared composition/contract; Huy implements the now-supported module reader or
command; both run producer -> Huy reader/service -> actual consumer SQL/HTTP
fixtures. The earlier §9.4 `ReadCurrentApprovedAsync(project,watermark,ct)`
proposal is superseded by B's `CaptureApprovedAsync`, not an alternative API.
No new shared DI, EF configuration, mapping, migration, canonical contract,
Postman or fixture was edited in this documentation checkpoint.

## 2026-10-03 continuation C1-C3 - PARTIAL

`CURRENT_VERIFIED` preflight: `huy-review` local and `origin/huy-review` were both
`7e8261648e08adf5b5bdf0cea85fca5e55463b73`, clean, before this continuation.
`715ade2c20f652b77c8c7e995c76bb5d47ead966` (A) and Huy diverge at
`efc0ca10b53264bb24c807b7352ddba0cbe36b6d`; A's parent is
`6365ae0ce0d6dc982a88b6d1ea3864ed37a7035e`. B is
`a46b97b271b4af1b9e89dc182b5e3fc7a10e8fff`. Neither A nor B was
cherry-picked/merged as a commit, and no newer Anh tip was substituted.

### C1 production intake and exact-source imports

- A's shared composition-root delta is one Reporter registration call in each
  `ServiceCollectionExtensions.cs` and `RoadGuardPersistenceExtensions.cs`.
  Its exact `AddHuy01ReporterServices/Persistence()` calls were not independent
  on Huy HEAD: those module extensions now also register lifecycle, Case and
  Candidate, whose routes A explicitly did not canonical-adopt. Conflict
  resolution: Huy's extensions expose intake-only registration; the two
  production roots call only that subset. Test-host full-module extensions
  remain available. Production POST `/api/v1/reports` is bound exactly once
  with scoped real service/repository; GET/list/supplement/Case/Candidate stay
  unbound and return `dependency_unavailable`. This is an adapted A hunk, not a
  claim that either whole shared file matches A's blob. Anh remains named writer
  for any further shared DI activation/contract adoption.
- Imported A's additive 41-line `contracts/http/anh01.local-contract.md` and
  90-line Postman environment patch from A parent; appended only its 13-item
  `HUY-01 integrated Reporter intake - ordered` JSON folder (782 lines) to
  Huy's collection. Parsed folder content equals A, existing folders/IDs stay
  intact; ANH-02 folder and implementation were not imported. The A contract's
  sentence that role-row deactivation is unchecked is **HISTORICAL** after the
  C2 fix below. This Huy integration does not edit Anh's canonical text beyond
  the exact A patch; Anh must append the role-row correction before the next
  canonical adoption/review. Until then the contract contains a known stale
  statement and must be read with this superseding SHA-scoped checkpoint.
- Shared receipt service and SQL test blobs remain exactly `8fa41dc4914ba7f2f475d4802727a2f8f1ef9f88`
  and `7d4fb79ba442f2359f3e26368196c24fdbf67aec` from
  `f626ea595420c3f28a35f2b3f4c6f196d313a7be`. No shared service/test,
  fixture, DbContext, mapping, migration/snapshot or ANH-02 implementation
  changed. Production binding acceptance uses real Reporter SQL/producer and
  named mock object storage; no live storage claim.

### C2 fresh role authority

- `ReporterReportRepository.EnsureCurrentReceiptAccessAsync` now locks Users,
  reads the actual user role, then locks/reads that `Roles.Code` row and requires
  `IsActive=true` before ordered Files/FileScopes/UploadSessions locks. The same
  method is used by create, receipt guard and Reporter lifecycle. It runs in
  the shared receipt transaction for replay/conflict/retry/duplicate-key/
  postcommit recovery; it opens no transaction and writes no receipt/business
  row itself. `CaseWorkflowRepository.GuardAsync` follows Users -> actual Roles
  -> Projects/ProjectMembers -> Cases; Candidate REPORT guard reuses it.
- Disposable SQL regressions mutate role after a successful real preflight and
  before ordinary receipt lookup, retry discovery, observed duplicate-key
  recovery after loser disposal and postcommit durable lookup. They distinguish
  failed create attempts from durable winner effects, restore fixture role in
  `finally`, and assert no protected receipt. Production-root HTTP proves
  replay and changed-payload conflict return 403 without Location/ETag after
  role deactivation. New create while role inactive leaves no graph. Existing
  cancellation, guard exception, same-key, rollback and exact outcome probes
  remain in the full HUY filter. Reporter P1 closure is **proposed** on these
  fresh gates; external ChatGPT review of this diff is PENDING.
- `PROPOSED`, named writer Anh: `AnhHuyFactsRepository.IsCurrentActorAsync`
  still checks user `RoleCode` without `Roles.IsActive`. The Huy transactional
  guard prevents unauthorized intake receipt/write after this gap, but Anh
  should add the role-row read to that producer for consistent preflight and
  private-file behavior. Do not infer other Anh producer/consumer routes are
  protected by this Huy guard.

### C3 reader boundaries and remaining gates

- Imported B's exact `RetentionContracts.cs` and `RetentionDtos.cs` blobs
  `fae16ded105c8c3f3ebdac43090b6e1b9a6996a1` and
  `7d960aeb1195ae15bf4bb8aba8eedcaada47499d` as the minimal compile
  boundary for a Huy-owned `IRetentionInventoryContributor`. The unbound
  `HUY` contributor uses the caller's scoped DbContext/transaction and real
  original/supplement Report evidence, active/closed Case links, conclusion/
  publication evidence, REPORT decisions and link-history tables; versions are
  deterministic hashes of typed persisted facts. Its project-file query retains
  closed links. `Complete=false` with explicit missing label,
  Defect source-link and repair reference reasons even on an empty result;
  it cannot yet unblock Anh's composite retention evaluator. B's
  `REPORTER_INTAKE` contributor remains a distinct Anh producer and was not
  renamed, copied or registered here. Disposable SQL tests cover persisted
  plain active intake, conclusion evidence, deterministic reread, link-version
  change on close, closed-link project obligation and incomplete result. Anh
  must import/register the exact Huy contributor
  after its B composite/DI is integrated and after the remaining Huy obligations
  become queryable; no retention deletion is implied.
- B's `Anh02Contracts.cs` is the exact local additive consumer boundary, not
  yet imported because no approved-label/matching/AI Huy reader can satisfy
  its required current-source snapshot with present schema. This supersedes
  §9.4's older `ReadCurrentApprovedAsync(project,watermark,ct)` proposal:
  implement **`IApprovedTrainingLabelReader.CaptureApprovedAsync(actorId,role,projectId,TrainingLabelFilterV1,ct)`**
  and `ITrainingSourceAccessReader.CanReadAsync(...)` from B unchanged, in the
  exporter's same scoped SERIALIZABLE transaction (or a durable immutable
  producer snapshot). Do not create a parallel cursor/watermark API. Exact B
  `IMatchingCandidateSnapshotReader.CaptureAsync(...)` and
  `IAiCandidateFactsReader.ResolveAsync(...)` likewise remain the consumer
  targets; no fixture-only success adapter is registered in production.
- Named writer Anh must reserve/import B's consumer graph, then adopt Huy
  readers. Label SQL first needs TrainingLabel head current revision/rowversion,
  immutable revisions and terminal reviews, source/file version/checksum and
  REPORT/AI provenance with typed restrict FKs, unique(label,revision), one
  terminal review/revision, current-head concurrency and approved-only SQL
  capture. Adding `IEntityTypeConfiguration` prematurely would change the
  runtime model via assembly discovery; no mapping/EnsureCreated/test-only
  migration was added. AI frame provenance is not a private UploadSession.
  Matching still needs actual scoped Defect ID/rowversion/segment/route and
  current geometry/source-disposition snapshot. KEEP_NEW/LINK_EXISTING need
  Defect rowversion and typed source-link/created-Defect relation plus
  downstream-use correction semantics. Missing facts mean unavailable, never
  empty-success or fake candidate/label effects.
- `PROPOSED` CaseDefectRead handoff for Anh reporting/dossier: a typed capture
  under the caller's scoped transaction with actor/current role/project,
  Case/Report IDs and revisions, status, conclusion/publication/recipient refs,
  authorized evidence IDs, and explicit availability/missing reasons for
  Defect/source/geometry facts. `ReceivedAt [from,to)` applies to Report flow;
  Case stock is current state. No frozen caller interface exists at B, so no
  parallel shared API or integrated claim was created. Anh should confirm the
  signature/failure semantics and bind real Huy query before consuming it.
- Web/Android transport still needs Anh Sessions Transport/LastActivityAt,
  backfill and shared auth middleware/options; Huy has not changed legacy
  bearer lifetime. No report event/outbox was emitted: canonical envelope,
  consumer and retry ownership remain an Anh/owner gate. No HUY-02, AI provider,
  shared/deployed migration, Retention delete or new Defect target effect.

Fresh continuation verification before final commit: API build 0 warnings/0
errors; unit `Package=HUY-01` 47/47; API/SQL
`Package=HUY-01|FullyQualifiedName~Huy01Reporter` 65/65 on disposable SQL;
shared receipt/P202 plus Huy identity SQL 57/57; legacy auth HTTP 37/37.
Focused red observations:
role inactive originally returned Created/Replayed/IdempotencyConflict (3
failures), missing production DI failed one, retention stub failed one,
contributor name mismatch failed one, and the plain-intake retention regression
failed before direct refs were added; each was corrected and rerun.
These overlapping red/green runs are not added to the final distinct counts.
`git diff --check` and JSON parse/folder comparison passed before final staging.
Self-review 1 checked user/actual role/source locking, post-preflight receipt
paths, protected HTTP headers, scoped durable graph, private evidence and
retention history; corrected missing direct retention references. Self-review
2 checked fixed A/B source, contract/DI activation breadth, mapping discovery,
shared ownership and no unsupported reader success. An internal read-only code
review found the direct-reference omission (fixed) and stale A contract text;
the latter is an exact cross-owner correction for Anh, not silently edited by
Huy. Internal review is not external ChatGPT review, which remains PENDING.
External/deployment, real ANH-02 exporter/retention consumer, label/matching
reader and Postman network runner are NOT RUN. `deliveryStatus=PARTIAL`.

## Anh shared integration reservation — owner continuation 2026-10-02

TARGET_CONFIRMED: Anh is named writer for shared model/configuration,
DbContext, migrations/snapshot, DI, canonical adoption/Postman and Anh file/
geometry/source producers. This integration runs on `anh-review`; Huy keeps
Report/Case/Candidate module command service/repository/controller ownership.
Source handoff is fixed `8676226cf9d1b99adf83beb0391f147dfa4b112e`, with
domain-core external review at `ad52f4cae1d2f52f030ea23d59866004001c5aa6`.
Common base is `1ecae797caaed1ab912b02b2372a1940d1e05375`. Integration starts
after the independent ANH root-scope correction commit `5d6ecb5c6c4498c4803ddec735a00b153d0967b5`.
Selective `git restore --source=<handoff> --worktree` imports unchanged
Reports/Cases/Candidates domain directories, their two domain test files and
this spec only. No branch merge, auth transport/refresh or label import.
Domain PASS is historical evidence, not SQL/HTTP/adapter acceptance.

The concrete producer signatures are version 1 in
`Services/Interfaces/Integration/IAnhHuyProducerService.cs`; Anh produces,
Huy consumes. Implemented/adopted status and test evidence are recorded in
the existing ANH-01 summary. Private intake wire uses separate
`/api/v1/reporter-evidence/uploads` create/read/part-urls/complete and
`/reporter-evidence/files/{fileId}` metadata/content. Create accepts only
fileName/mediaType/sizeBytes/checksumSha256; principal owner and REPORT_PHOTO
purpose are server-resolved, project/target remain null, JPEG/PNG <=20 MiB.
Current active Reporter/owner is required before every new command/replay;
other owner and unauthorized publication projection return 404. File content
and evidence resolution return 409 source_not_ready until VERIFIED.
Complete uses If-Match (stale 412); command replay precedes fresh version
check after current authorization. Existing project upload contracts stay
project-scoped and do not acquire Reporter authority.

| Producer v1 | Authoritative source/scope/provenance | Exact failures and fixture |
|---|---|---|
| ResolvePrivateEvidenceAsync(actor,role,file,evidence,expectedFileVersion?,ct) | Files + immutable FileScopes + UploadSessions; active Reporter/owner, private project=null scope. Reference version is terminal upload rowversion; checksum/long bytes/MIME/uploadedAt are server facts. Capture metadata is supplied by the Report command, never inferred from upload GPS. | hidden ownership/missing 404; pending/failed 409 source_not_ready; expected file-version drift 412 concurrency_conflict. Owner/other/pending/failed/verified fixtures. |
| ResolvePublicationEvidenceAsync(actor,role,publication,report,evidence,ct) | Huy-owned immutable publication-recipient-evidence relation + recipient Report owner, actual original/supplement evidence, Files/UploadSessions. Anh checks exact projection/file readiness; Huy validates relation for every recipient when composing publication. | missing/other recipient/projection 404; pending/failed 409 source_not_ready. Two Reporters on one case with different selected recipient evidence; never a case-wide permission set. |
| ResolveGeometryAsync(actor,role,project,route,set,expectedVersion?,requireCurrent,ct) | Production geometry package, actual project/current membership, immutable route/set/segment refs and ordered adjacency. Package hash/version includes route current flag/set rowversion; no guessed CRS/offsets. | role/membership 403; scoped wrong project/ref 404; incomplete 409 source_not_ready; old route/set or expected-version drift 409 candidate_stale. Historical read permitted only with requireCurrent=false. |
| ResolveCandidateSourceAsync(actor,role,project,kind,id,expectedSource?,expectedGeometry?,expectedDisposition?,ct) | PM membership; REPORT via actual Report, active case link, assigned case project and server-persisted case geometry refs; report/case/head versions and verified files compose opaque source version. Active disposition comes from SQL head/decision, never client metadata. | untriaged/missing geometry/provenance and unavailable AI/FIELD producer 409 source_not_ready; wrong project 404; source/geometry drift 409 candidate_stale; expected active disposition drift 412 concurrency_conflict. Real SQL Report source + positive/negative/version fixtures. |

Huy must re-resolve facts and hold/check authoritative rows within its atomic
repository transaction. Preflight producer reads do not establish command
atomicity. Candidate AI/FIELD facts, ApprovedTrainingLabel reader/exporter,
event schema/consumer agreement and Huy business DI/HTTP remain PENDING until
their real implementations exist. No fake-success adapter, label mapping,
new event emission or Report/Case/Candidate command is activated by this section.

## Shared mapping and consumer handoff v1

Anh integration migration: `20261002120000_AnhHuySharedIntegration`, after ANH
file/geometry and forward scope corrections. Only fixture-owned SQL is applied.
No existing domain type is altered. EF8 generated owned relationship metadata
omits its Restrict DeleteBehavior; the shared designer/snapshot preserve it
explicitly. Preserve the two documented declarations when regenerating. Reports own original evidence; supplements
own separate evidence; neither rewrites source. Composite Report/owner FKs,
typed source/evidence FKs, correction identity FK, one active report-case link
(filtered index) and one source head (composite PK/FK) prevent ambiguous refs.
Append-only history/evidence/conclusions/publications/recipient rows reject
UPDATE/DELETE; Report source fields reject binary/datalength changes. Active
links can only be closed, not rewritten/reopened. Down rejects received data.
FileScopes keeps nullable project only for private REPORT_PHOTO/target-null,
with unchanged legacy project data; scope mutation/deletion is rejected.

Huy implementation order and exact persistence responsibilities:

1. Consume `Services/Interfaces/Integration/IAnhHuyProducerService.cs` v1
   (actual namespace `RoadGuardSystem.Services.Integration`). Evidence resolution
   gives VerifiedEvidenceReference + server checksum/bytes/version. Geometry
   gives real package and segment adjacency; CandidateFacts gives source,
   project, geometry and current disposition versions. Resolve every current
   authorization/fact before receipt replay, then re-resolve/lock/check them
   within the command transaction. Public 409/412 mappings are frozen above.
2. Build real Report/Case/Candidate module service/repository/controller seams
   on Huy-owned paths; add business DI only after real implementations exist.
   Shared read producers are already registered. No no-op command repository.
3. Use actual imported domain via DbContext Reports/ReportSupplements/
   IncidentCases/SourceDecisions, with support rows in
   `Repositories/Models/Huy01/HuyIntegrationRows.cs`. Populate CandidateDecision
   shadows SourceKind/SourceId/ReportSourceId or AIDetectionSourceId consistently
   with Source_Kind/Source_Id. Atomically insert decision/correction and update
   HuyCandidateSourceHead.DecisionId, checking head RowVersion/current disposition.
   FIELD schema/facts remain not-ready; AI facts remain not-ready without the
   required provenance producer. Classification must verify segment belongs
   to selected geometry route/project (independent catalog/route/segment FKs
   alone do not prove that relation).
4. Increment Report shadow `Revision` on each supplement/evidence append, and
   Case `Revision` on each child/link/state/geometry mutation (both cases for
   link/split). EF rowversion alone does not change when only children change.
   Save the parent bump with all child/history writes in one transaction.
   Populate Case geometry shadows GeometryRouteVersionId/GeometrySegmentSetId
   from the resolved geometry. Pair/composite FK enforces route-set pairing.
5. Atomically synchronize `_activeReportIds` snapshot with CaseReportLinks
   close/create. Persist CaseReportLinkHistory once by Id and its
   HuyLinkHistoryReport FK refs; query history FromCaseId OR ToCaseId. The shared
   object occurs in both domain collections, so EF LinkHistory is ignored.
   If a reloaded aggregate needs historical navigation for a command, Huy must
   add an explicit materialization hook/signature; this package does not mutate
   the reviewed domain to add one silently.
6. Persist conclusion CaseId and HuyConclusionDefect/HuyConclusionEvidence
   refs, publications plus HuyPublicationRecipient/HuyPublicationDefect and
   HuyPublicationEvidence per recipient. Domain JSON snapshots and relational
   refs require atomic sync; SQL FKs alone do not prove snapshot equality.
   `CasePublicationRecipientFacts.Create(reportId,visibleDefectIds,
   visibleEvidenceIds)` must be composed individually from that Reporter's real
   visibility. Never replace it with a common case evidence set. Recipient
   content producer checks exact projection + immutable evidence FileVersion.
7. Prove Report/supplement, triage/link/split, conclude/publication and candidate
   decision/correction atomic commit/rollback including audit/receipt/outbox
   using real Huy repositories. Then adopt the implemented Huy HTTP routes and
   agreed events and run producer-to-real-consumer HTTP/SQL. Until then business
   DI/HTTP/event/label reader-exporter remain PENDING; no new Huy event emitted.

Schema tests are fixture persistence/constraints/recovery evidence, not Huy
business orchestration PASS. Runtime counts and known limitations are in the
existing ANH-01-summary; external review of this integration is PENDING.

The checkpoint entries below are HISTORICAL at the fixed Huy handoff. Their
shared-pending statements are superseded only by the implemented Anh shared
reservation above; Huy business/consumer gates remain pending.

### 2026-10-02 - PARTIAL consumer implementation checkpoint

- `CURRENT_VERIFIED` local graph: Huy started from `8676226cf9d1b99adf83beb0391f147dfa4b112e` and normally merged Anh's fixed integration handoff `efc0ca10b53264bb24c807b7352ddba0cbe36b6d` as merge commit `2bf02bae24a38dc5c3c47e17d6190757e471e610`. The add/add conflict in this spec was resolved with Anh's copy because it retains every Huy checkpoint and adds this reservation/handoff; no source/auth/label file was overwritten.
- Scope/result: Huy-owned Reporter intake implementation now has DTO, service, EF repository, controller, and module-only DI extensions. `POST /api/v1/reports` accepts only a current Reporter and server-resolved private VERIFIED evidence, re-resolves evidence again inside the idempotent transaction, creates one immutable Report, one UNASSIGNED IncidentCase and one active CaseReportLink, and stores the sanitized response receipt. Same key/fingerprint replays the same report response without a second intake. Other-owner/missing evidence is hidden as 404; pending/failed evidence is 409 `source_not_ready`; stale evidence is 412 `concurrency_conflict`; wrong role/current authority is 403. The response has the durable report rowversion ETag.
- Boundaries: `AddHuy01ReporterPersistence()` and `AddHuy01ReporterServices()` are intentionally module extensions only. Anh remains the named writer to call them from shared composition root after reviewing bindings. Report list/detail/supplement, case commands/publication, candidate/defect commands, labels/export and events remain PENDING; no controller/service is represented as full HUY-01 completion.
- Evidence: the HTTP regression was added before the implementation, but the disposable SQL Server fixture could not initialize because Testcontainers could not connect to Docker's `npipe://./pipe/docker_engine`; endpoint assertion and SQL effects are therefore `NOT_RUN`, not a red/green HTTP claim. Fresh API build passed with 0 warnings/0 errors; existing Unit `Package=HUY-01` trait executed 20, passed 20, failed 0, skipped 0. No HTTP/SQL PASS is claimed from the unavailable fixture.

### 2026-10-02 - PARTIAL Reporter intake review remediation

- Reporter intake now normalizes a typed request through canonical JSON before SHA-256 fingerprinting; the key must be printable ASCII, 1..200, and `locationSource` must be one of `UNKNOWN|CAPTURE|EXIF|MANUAL`. Unknown DTO fields, null evidence elements, malformed capture coordinates and hints not represented by the current persisted Report schema are rejected at the API boundary as 400 `validation_error`; they are not dropped. Capture metadata is stored on immutable ReportEvidence, but upload GPS is never used as capture metadata.
- For a new command, the service preflights current authority before any receipt lookup. Inside the idempotent SQL transaction, the Huy repository acquires `UPDLOCK,HOLDLOCK` rows for actor, file, file scope and upload session in stable FileId order, rereads/compares actor authority, owner/private scope, VERIFIED state and FileVersion, then saves Report + UNASSIGNED Case + active link + sanitized `report_received` audit + receipt atomically. Replays repeat current authority/source preflight before reading the stored outcome; stale file is 412 `concurrency_conflict`, pending/failed is 409 `source_not_ready`, missing/other owner stays 404.
- Event/outbox remains pending: Anh has not adopted a `report.received.v1` canonical envelope/consumer. No event is emitted. Anh’s required shared follow-up is composition-root registration of the two Huy module extensions and, before production activation, an agreed SQL fixture test for lock/revoke races and receipt recovery.

### 2026-10-02 - Reporter intake receipt-authorization and validation delta

- `CURRENT_VERIFIED`: request normalization rejects control/non-ASCII bytes first, trims only leading/trailing ASCII space (`0x20`) from `Idempotency-Key`, then accepts normalized printable ASCII `0x20..0x7E` (including internal spaces) at length 1..200 and passes that exact key to receipt lookup. All-space, control and non-ASCII keys return a 400 `validation_error` with an `errors["Idempotency-Key"]` entry; an absent header remains the 428 precondition. Location coordinates are nullable at the .NET 8 DTO boundary so omitted JSON `latitude`/`longitude` cannot deserialize as `0`; both must be present for `CAPTURE|EXIF|MANUAL`, while explicit `(0,0)` remains valid. Normalizer errors are preserved by the service and emitted in `application/problem+json` under `errors`.
- `CURRENT_VERIFIED` focused evidence from this revision: `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ReporterIntakeRequestNormalizerTests" --nologo -v q -clp:ErrorsOnly` executed 17, passed 17, failed 0, skipped 0; its red regression executed 2, passed 0, failed 2 before printable internal space was accepted. `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter "FullyQualifiedName~Huy01ReporterReportsApiTests" --nologo --logger "console;verbosity=minimal"` executed 7, passed 7, failed 0, skipped 0 on the disposable SQL Server fixture. Those seven API cases cover the three missing-coordinate shapes, explicit `(0,0)` plus normalized-key replay and exact HTTP outcome, invalid whitespace/non-ASCII key validation, and one verified-evidence create/replay/conflict flow. That flow asserts one Report, intake Case, active link, audit and receipt scoped to its actor/report/case/operation IDs, plus the same five counts across that test actor's intake/operation scope; replay/conflict do not add any of those rows. It does not use database-wide row counts.
- `CURRENT_VERIFIED` limit: create attempts still preflight source facts and the Huy repository locks/rechecks actor/file/scope/upload rows through commit before Report + Case + link + audit + receipt are saved. That protects new writes and does not create a second Report during normal retry. It does **not** prove a current authorization check at each generic receipt return: `IdempotencyOperationService` maps a found record before Huy's handler runs on the ordinary replay, execution-strategy retry, duplicate-key recovery and durable post-commit acknowledgement recovery paths.
- `PROPOSED shared delta, writer Anh`: extend `IdempotencyOperationService.ExecuteAsync` with an optional module-supplied `receiptAccessGuard(CancellationToken)`. When a record is found, call the guard after every `FindExistingAsync` and immediately before `MapExisting`: initial lookup, each execution-strategy attempt, duplicate-key exception recovery and post-commit durable-record recovery. Execute the found-record guard in a short transaction with the actor row locked (`UPDLOCK,HOLDLOCK`); Huy's guard must re-resolve Reporter active/role/must-change state and private evidence owner/scope/VERIFIED/version using the same ordered locks as intake. A guard failure must bubble as the module's privacy-safe 403/404/409/412 result, never return the stored receipt. The create handler stays as-is inside its transaction and must not run for a found receipt.
- Integration order/fixtures: Anh first adopts the generic callback without changing existing module behavior; Huy then supplies the Reporter guard adapter and updates this service call. Run disposable SQL fixtures that revoke/deactivate the Reporter after service preflight but before each of the four receipt paths, plus concurrent same-key, rollback and acknowledgement recovery. Assert no receipt response after revocation and exactly one Report/Case/link/audit/receipt for durable success. The seven focused API/SQL cases above are `PASS`; receipt-path authorization, revoke race, concurrent same-key, rollback and acknowledgement-recovery behavior remain `PENDING` because that shared seam and its fixtures do not yet exist. `deliveryStatus` remains `PARTIAL`.
- Fixture note: direct normalizer coverage proves empty/control/non-ASCII keys are validation errors. `HttpClient`/TestServer drops an empty request-header value before the action sees it, so that transport shape is indistinguishable from an absent header and correctly receives the existing 428 gate; the HTTP fixture covers whitespace/control and non-ASCII invalid values plus normalized-key replay. No 400 claim is made for an empty value that this fixture cannot transmit.

### 2026-10-03 - Reporter receipt-access integration checkpoint

- `CURRENT_VERIFIED` graph and preservation: initial and remote `huy-review` were both clean at `b8ec845d69ba2c71249cec6fb15e13a5bad8126f`. After `git fetch origin --prune`, `origin/anh-review` was `62388a8537fd368da43fce578afeb8cef0ab01df`; the requested implementation object `f626ea595420c3f28a35f2b3f4c6f196d313a7be` was fetched directly and is the parent of that documentation-only delivery commit. It is not an ancestor of `huy-review`; their merge base is `efc0ca10b53264bb24c807b7352ddba0cbe36b6d`. No tip merge, reset, overwrite of ANH-02, or local modification of shared files was performed.
- `TARGET_CONFIRMED` handoff fact: `f626ea5` is Anh's reviewed shared seam, with no blocking finding reported for its source/focused shared tests. It adds the optional positional overload `ExecuteAsync(actorUserId, projectId, operation, idempotencyKey, requestFingerprint, operationHandler, cancellationToken, receiptAccessGuard)` in `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs`; the callback is invoked before replay **and** conflict mapping on ordinary lookup, execution-strategy retry, duplicate-key recovery and post-commit acknowledgement recovery. It owns the short transaction and passes the original command cancellation token to the guard.
- `HISTORICAL` dependency blocker: the preceding pre-integration observation is superseded. Non-ancestry alone is not a blocker when a fixed shared source import is explicitly assigned.
- `CURRENT_VERIFIED` fixed-source integration: from clean `huy-review` base `1e24a48aad1c4ce5d51750bd316dc8aff6a57e7d`, Huy imported exactly `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs` and `tests/RoadGuardSystem.IntegrationTests/Persistence/ReceiptAccessGuardSqlTests.cs` from source SHA `f626ea595420c3f28a35f2b3f4c6f196d313a7be`, by path rather than branch merge/cherry-pick. Before import the local service blob `b73e76d432b0d97c470778e2b619ffd6b06261d8` equalled `efc0ca1`; fixture, both commit-failure interceptors and integration test project blobs equalled the source checkpoint. After import, service blob `8fa41dc4914ba7f2f475d4802727a2f8f1ef9f88` and guard test blob `7d4fb79ba442f2359f3e26368196c24fdbf67aec` exactly equal `f626ea5`. No ANH-02/tip files were imported and the shared implementation bytes were not locally edited.
- `CURRENT_VERIFIED` Huy adapter: `ReporterReportService` now passes `operationHandler, cancellationToken, receiptAccessGuard: Guard`; the guard delegates to module-owned `IReporterReportRepository.EnsureCurrentReceiptAccessAsync` on the same scoped DbContext. The create handler and receipt guard reuse ordered `UPDLOCK,HOLDLOCK` fresh reads for active Reporter role/must-change state and every private `REPORT_PHOTO` file owner, null project/target, VERIFIED upload state and expected FileVersion. The guard opens/commits no transaction, writes no rows and does not invoke the create handler. `ReporterIntakeFactsException` continues to map privacy-safe 403/404/409/412 after the shared seam rethrows its original cause.
- `CURRENT_VERIFIED` focused execution: the new disposable-SQL regression first failed on the pre-bind source with `Expected: Forbidden; Actual: Replayed` after a real producer returned preflight facts and the Reporter was revoked immediately after it. With the guard bound, `Huy01ReporterReceiptAccessSqlTests|Huy01ReporterReportsApiTests` executed 8, passed 8, failed 0, skipped 0; it proves ordinary receipt replay denial and no new actor/report/case/link/audit/receipt effects. Shared `ReceiptAccessGuardSqlTests|P202ServiceContractTests|P202TransactionAndIdempotencyTests` executed 54, passed 54, failed 0, skipped 0; it is shared seam evidence, not Reporter acceptance. The fresh API build passed with 0 warnings/0 errors and normalizer unit tests executed 17, passed 17, failed 0, skipped 0.
- `PENDING` Reporter acceptance: Huy has not yet added true Reporter fault probes for execution-strategy retry receipt discovery, duplicate-key recovery, post-commit acknowledgement recovery, wrong owner/scope, pending/failed/stale evidence after preflight, guard cancellation, concurrent same-key and precommit rollback. The existing HTTP cases retain exact durable success body/Location/ETag coverage, but do not prove those guarded recovery paths. Reporter P1 is not proposed `CLOSED` until those module-specific SQL/HTTP cases run.
- `deliveryStatus` remains `PARTIAL`. No `report.received.v1`, DI/composition-root binding, schema/migration, canonical contract, Postman, HUY-02, or shared/deployed database change is authorized by this checkpoint.

### 2026-10-03 - Reporter receipt authorization acceptance checkpoint

- `CURRENT_VERIFIED` graph/preservation: this acceptance run began from clean local and remote `huy-review` at `d5bf770b1a2007ab7c0bd19bd1d9162407a21df0`; no `anh-review` tip/ANH-02 source was merged or imported. The two shared paths retained their already-verified bytes from original source `f626ea595420c3f28a35f2b3f4c6f196d313a7be`. This checkpoint changes only Huy's Reporter SQL acceptance test and this spec.
- `CURRENT_VERIFIED` Reporter acceptance uses the actual `ReporterReportService`, `ReporterReportRepository`, `AnhHuyProducerService` and `AnhHuyFactsRepository` on the disposable SQL Server fixture. Its module-owned decorators only create deterministic barriers, mutate authoritative facts after a successful real preflight, or inject an observed database failure; they delegate the producer/repository behavior under test.
- `CURRENT_VERIFIED` receipt paths: ordinary replay returns the stored report while changed payload returns the idempotency conflict without a second graph. An intercepted first `IdempotencyRecords` lookup proves execution-strategy retry rediscovered the receipt (one injected lookup failure, at least two lookup attempts, zero create-handler calls); the revoked-after-preflight variant is denied by the guarded retry path. Two concurrent services are held through real preflight and handler entry, then produce two handler attempts, one `Created`, one `Replayed`, and exactly one scoped Report, `UNASSIGNED` Case, active link, `report_received` audit and receipt. A post-commit acknowledgement interceptor proves one durable handler attempt returns replay from durable recovery; a variant revokes the Reporter after durable commit and before recovery, and returns no protected outcome. An always-failing pre-commit interceptor exhausts the test retry strategy after three handler attempts and leaves zero scoped Report/Case/link/audit/receipt rows.
- `CURRENT_VERIFIED` fresh authority/source gates: after a real preflight, suspended state, atomic Reporter-to-PM role change, and `MustChangePassword` each deny the stored receipt. Pending, failed and changed upload-version facts after a real preflight map to `source_not_ready`, `source_not_ready`, and `concurrency_conflict` respectively, without added rows. Cancellation injected inside the receipt guard after its real locked validation propagates rather than returning the receipt or invoking the create handler. A separate guard decorator that first delegates the real locked validation then fails proves no create-handler call and no protected receipt. File owner and private/project scope are immutable integration facts, protected by the fixture trigger, so they cannot be rewritten to fabricate an after-preflight race; separate real-producer negative inputs prove both wrong owner and non-private project scope return privacy-safe `NotFound` before receipt lookup.
- `CURRENT_VERIFIED` commands/results from this checkpoint: `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --disable-build-servers --nologo -v q -clp:ErrorsOnly` passed with 0 warnings/0 errors. `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter "FullyQualifiedName~Huy01ReporterReceiptAccessSqlTests" --nologo -v q -clp:ErrorsOnly --logger "console;verbosity=minimal"` executed 17, passed 17, failed 0, skipped 0 on disposable SQL Server. `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --no-restore --filter "FullyQualifiedName~Huy01ReporterReportsApiTests" --nologo -v n -clp:ErrorsOnly --logger "console;verbosity=normal"` executed 7, passed 7, failed 0, skipped 0, including exact HTTP body/Location/ETag replay evidence. `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --disable-build-servers --filter "Package=HUY-01" --nologo -v q -clp:ErrorsOnly --logger "console;verbosity=minimal"` executed 33, passed 33, failed 0, skipped 0. Shared regression command `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --no-restore --filter "FullyQualifiedName~ReceiptAccessGuardSqlTests|FullyQualifiedName~P202ServiceContractTests|FullyQualifiedName~P202TransactionAndIdempotencyTests" --nologo -v q -clp:ErrorsOnly --logger "console;verbosity=minimal"` executed 54, passed 54, failed 0, skipped 0; it rechecks the shared seam and is not substituted for Reporter acceptance.
- `PROPOSED close condition`: Reporter P1 acceptance gates listed in the receipt-access checkpoint are now BE verified on disposable SQL Server. P1 is proposed for closure only after external ChatGPT review of this new Huy diff; external review is `PENDING`. HUY-01 remains `PARTIAL`: DI/composition root activation, canonical contract/Postman, event/outbox, migration/shared schema ownership and all unrelated HUY-01 gates remain outside this acceptance result.

### 2026-10-03 - Remaining HUY-01 execution checklist

This is the active implementation checklist for the `0e41913` continuation,
not a reopening of D1=A/D2=A/D3=A/D4=A or of historical `PROPOSED` text.

| Flow | Current implementation evidence | Remaining action / real dependency |
|---|---|---|
| Reporter receipt acceptance | Real Reporter/producer SQL probes now cover ExecuteAttempt retry after a rolled-back create, duplicate-key recovery after losing-transaction disposal, guarded revocation, and scoped orphan-case rollback. | Propose P1 closure only after external ChatGPT review of this diff; do not treat shared Project harness as Reporter evidence. |
| Reporter read, supplements, safe download | Module service/repository/DTO/controller now provide own list/detail, recipient-scoped public updates, append-only supplement/reopen, guarded replay and authorized evidence download. Real HTTP/SQL fixture covers replay, privacy and immutable publication snapshot. | Anh owns production composition, canonical contract/Postman and intake hint persistence; fake object storage in HTTP fixture proves adapter behavior only. |
| Case command workflows/publication | Module repository/service/controller now persist triage, link/split, conclusion and publication with ordered SQL locks, current scope, history/relational refs, revisions and receipt/audit transaction. Real SQL fixture covers multi-case rollback/recovery and recipient privacy. | FIELD/DRONE completion provenance, verified Defect source links, warranty facts, event agreement and shared production binding remain dependencies. |
| Candidate/defect decisions | REPORT REJECT/correction persists immutable decision/head/audit/receipt using real geometry/source producer. Pure matcher and Defect factory/assessment/verification domain rules have focused unit evidence; HTTP tests cover REPORT reject/replay/correction. | KEEP_NEW/LINK_EXISTING and Defect HTTP/SQL require target Defect rowversion and source-link/created-defect schema. Production matching needs assigned-segment/target scope facts. AI/FIELD provenance is not ready. |
| Identity transport | Existing refresh clamp remains; disposable SQL tests cover OTP resend/expiry and concurrent verify boundaries, with legacy auth HTTP regression. | Session Transport/LastActivityAt columns, Web cookie/CSRF/idle middleware, Android transport issuance and shared options/bindings remain Anh-owned integration gates. |
| Training labels | Unbound current-head aggregate/policy now enforce current PM scope from authoritative facts, verified source file version, normalized BBOX, revision/review history and current-approved eligibility. Unit tests cover transitions/invalid inputs. | Label mapping/tables, authoritative AI/file provenance, approved-only SQL reader/watermark, exporter contract and shared binding are still absent; no endpoint or discovered mapping is active. |

Every completed row receives fresh build/test evidence below. `deliveryStatus` stays
`PARTIAL` until the shared schema, composition, canonical contracts and remaining
acceptance gates are actually integrated.

#### 2026-10-03 Huy continuation verification and handoff

- `CURRENT_VERIFIED` initial state: local `huy-review` and `origin/huy-review` were clean/equal at `0e41913b225d4f72d4d1ead1ae322ad56bba5328`; parent is `d5bf770b1a2007ab7c0bd19bd1d9162407a21df0`. Original shared implementation source is `f626ea595420c3f28a35f2b3f4c6f196d313a7be`, imported in the preceding checkpoint. Its `IdempotencyOperationService.cs` blob is `8fa41dc4914ba7f2f475d4802727a2f8f1ef9f88` and shared guard test blob is `7d4fb79ba442f2359f3e26368196c24fdbf67aec`; this continuation does not edit them or merge Anh's tip.
- Reporter P1 follow-up: module SQL probes now intercept the *fourth* actual `FROM [IdempotencyRecords]` lookup to prove `ExecuteAttemptAsync` receipt discovery after a rolled-back Reporter create attempt; duplicate-key probe observes SQL 2601/2627 and checks recovery after losing transaction disposal, including revoke before guarded return. Both variants keep one scoped durable graph. Rollback captures attempted Report and Case IDs and detects orphan cases directly. These supplement the ordinary replay/conflict, source mutation, cancellation, concurrent same-key, and post-commit recovery probes. P1 is **proposed CLOSED subject to external ChatGPT review**, which remains `PENDING`; shared guard harness alone is not used as Reporter acceptance.
- Implemented Huy consumers: own Report list/detail/cursor, supplement/reopen/download; Supervisor triage and PM case read/link/split/conclusion/publication; REPORT candidate REJECT/correction; candidate matcher, Report Defect factory/assessment/ExistingEvidence verification and unbound label current-head policy. Real HTTP/SQL tests cover exact replay body/Location/ETag, actor and recipient privacy, current project membership, ordered multi-case rollback, append-only history, parent revisions, candidate head/decision and supplement/candidate precommit/postcommit receipt behavior. New controllers return `503 dependency_unavailable` when their module services are not composed; this is a fail-closed integration checkpoint, not production activation. The intake response contract remains compatible and does not claim unsupported hint persistence.
- Final focused commands/results: `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --nologo -v q -clp:ErrorsOnly` succeeded, 0 warnings/0 errors. `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter "Package=HUY-01" --no-build --no-restore --logger "console;verbosity=minimal"` executed 47/passed 47/failed 0/skipped 0. `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter "Package=HUY-01" --no-restore --logger "console;verbosity=minimal" -v q -clp:ErrorsOnly` executed 54/passed 54/failed 0/skipped 0 on disposable SQL Server (21 Reporter receipt, 22 Case SQL, 11 Reporter/Case/Candidate HTTP). Fake object storage verifies the adapter contract; SQL business/receipt rows and real file/geometry producers were used.
- Affected regression: unit `Package=HUY-01|AuthServiceTests|P232DomainInvariantTests` executed 70/passed 70; API `AuthenticationFlowTests|V2AuthenticationFlowTests|V2IdentityOnboardingFlowTests` executed 37/passed 37; integration `Huy01IdentityBoundaryTests|ReceiptAccessGuardSqlTests|P202ServiceContractTests|P202TransactionAndIdempotencyTests` executed 57/passed 57 (3 identity boundary + 54 shared receipt/P202). The broader integration run also selected `P232DetectionDefectSchemaTests`: executed 65/passed 64/failed 1/skipped 0. The failure is its legacy migration downgrade/reapply case: unchanged shared `20261002100000_Anh01RequestScopeRootCorrection.Down` deliberately throws SQL 51027 requiring an owner-reviewed preservation plan. No shared migration or test was modified to make it pass; this migration downgrade is not HUY acceptance evidence.
- Self-review pass 1 (auth/privacy/transaction): fixed receipt retry probe to target the actual retry lookup, prevented stale-handler fallback from retrying guard failures, checked current evidence before Case receipt replay, added exact case-orphan/parent-rowversion rollback assertions, and tested recipient publication isolation. Pass 2 (ownership/compatibility): fixed unconditional controller DI dependency before shared composition, removed a brittle test asserting absent future schema, checked module-only changed paths and shared writer boundaries. Final `git diff --check`, staged scope, exact commit/remote SHA are recorded at handoff after the last checks.
- `deliveryStatus=PARTIAL`. Anh's exact shared delta is in §9.4 above: producer facts, Reporter hints, Defect rowversion/source-link effects, matching source/target scope, FIELD/DRONE/warranty provenance, Session transport/last activity, Label tables/approved reader, production bindings/canonical contracts/Postman and event agreement. Huy has no approved event envelope/consumer and emits none. Web/Android, KEEP_NEW/LINK_EXISTING, production matching, Defect HTTP/SQL and Label SQL/HTTP/export stay pending their respective shared facts/schema/bindings. No HUY-02, ANH-02, deployed migration or external provider/deployment verification was performed.

## Implementation checkpoint - 2026-10-02 11:09 +07:00

- `deliveryStatus`: `PARTIAL`; branch `huy-review`; base `1ecae797caaed1ab912b02b2372a1940d1e05375`; initial worktree status clean. The caller's original checkout on `huy` had the untracked source path `planning/development/HUY-01-spec-and-codex-prompt.md`; it was preserved and copied here rather than modified.
- Huy confirmed `D1=A`, `D2=A`, `D3=A`, `D4=A` on 2026-10-02. That approves this package's documented behavior, but does not reserve or integrate shared files.
- `CURRENT_VERIFIED`: `RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs` creates replacement refresh credentials; `RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.RefreshTokens.cs` enforces active parent session during rotation; `UserSessionConfiguration.cs` and migrations remain current mapped schema evidence.
- `TARGET_DOCUMENTED`: Sections 5.2 and 8.2 require refresh expiry never to exceed the parent session's absolute expiry, without sliding that absolute expiry. `PROPOSED_DELTA`: the new transport, Reporter/case, candidate and label schemas/routes still require the shared integration named in section 9.2.
- Slice contract: existing `POST /api/v1/auth/refresh`; authenticated refresh-token holder and current active user/session; existing `RefreshCommand`; existing success token response and existing auth error statuses; no new HTTP contract; replacement refresh expiry is `min(now + configured lifetime, session.ExpiresAt)`; rotation remains repository-atomic and does not add sensitive data/audit behavior.
- Source evidence: `AGENTS.md` "Endpoint Contract And Delivery" and "V2 Ownership And Shared Hotspots"; `planning/V2/README.md` "Ownership va tranh xung dot"; this document sections 5.2, 8.2, 9.2 and 10; `tests/RoadGuardSystem.UnitTests/Authentication/AuthServiceTests.cs` for current auth unit seam. The V2 canonical OpenAPI is `NOT_ENABLED` for HUY-01 routes.
- Shared reservations/dependencies remain `PENDING`: Anh is named writer for `RoadGuardDbContext`, migrations/model snapshot, shared DI/configuration, canonical HTTP/events contracts, and integrated Postman. ANH-01 must provide verified Reporter-private evidence upload/download; geometry and processing provenance producers need their agreed interfaces. No `DbContext`, migration, snapshot, shared DI, canonical OpenAPI or integrated Postman file has been changed.

### 2026-10-02 - IN_PROGRESS

- Scope/result: Began the independent D1 refresh-boundary slice. A focused unit regression now requires a rotated refresh token not to outlive its parent session; `AuthService` clamps the replacement expiry. Reporter/case, candidate/defect and training-label work remains pending the named shared contracts and persistence integration.
- Files: `RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs`; `tests/RoadGuardSystem.UnitTests/Authentication/AuthServiceTests.cs`.
- Acceptance criteria: Unit behavior passed after a verified red run. This is not HTTP, SQL, migration, mapping, producer/consumer, or deployment evidence.
- Verification: red command `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter FullyQualifiedName~AuthServiceTests.Refresh_SessionExpiresBeforeConfiguredLifetime_ClampsReplacementExpiry --nologo -v q` executed 1, passed 0, failed 1. Green: `dotnet build RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj --nologo -v q -clp:ErrorsOnly` passed; `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --nologo -v q -clp:ErrorsOnly` passed with 133 pre-existing analyzer warnings; `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --filter FullyQualifiedName~AuthServiceTests --nologo -v q` executed 18, passed 18, failed 0, skipped 0.
- Reused/invalidated evidence: no prior execution evidence reused. Any later service edit invalidates the focused authentication result. SQL Server, API smoke, migration, Postman and shared integration are `NOT_RUN`.

### 2026-10-02 - PARTIAL

- Scope/result: Added the independent `TrainingLabelRevision` domain invariant for normalized BBOX annotations and a single terminal PM review outcome. A revision begins `PENDING`; an `APPROVED` or `REJECTED` review retains actor, timestamp and reason; a second decision is rejected. This is not an HTTP endpoint, persistence model, approved-reader implementation, or export integration.
- Files: `RoadGuardSystem.BusinessObjects/Labels/TrainingLabelReviewStatus.cs`; `RoadGuardSystem.BusinessObjects/Labels/TrainingLabelRevision.cs`; `tests/RoadGuardSystem.UnitTests/Labels/Huy01TrainingLabelRevisionTests.cs`.
- Acceptance criteria: unit evidence covers the terminal review proof and invalid BBOX boundary. The remaining D3 acceptance criteria (PM project authorization, current revision reader, revision creation, SQL filtering, migration and exporter consumer) are blocked by shared persistence/DI/canonical contract coordination.
- Verification: red command `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter FullyQualifiedName~Huy01TrainingLabelRevisionTests.Create_BboxExceedsImageBounds_IsRejected --nologo -v q` executed 1, passed 0, failed 1 after BBOX validation was intentionally removed. Green: `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --nologo -v q -clp:ErrorsOnly` passed with 5 analyzer warnings; `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --filter FullyQualifiedName~Huy01TrainingLabelRevisionTests --nologo -v q` executed 2, passed 2, failed 0, skipped 0.
- Reused/invalidated evidence: the prior authentication focused result remains valid because its source path was unchanged. SQL Server, API smoke, migration, Postman, real producer/consumer and deployment evidence remain `NOT_RUN`.

### 2026-10-02 - PARTIAL

- Scope/result: Added the repository-level D1 backstop so refresh rotation clamps any supplied replacement expiry to the authoritative parent session expiry inside the SQL transaction.
- Files: `RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.RefreshTokens.cs`; `tests/RoadGuardSystem.IntegrationTests/Identity/P110AuthenticationPersistenceTests.cs`.
- Acceptance criteria: SQL Server test proves the durable replacement row and returned fact do not outlive the parent session. Web/Android transport routes, cookie/CSRF, session schema changes, and full HTTP smoke remain outside this slice.
- Verification: red `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter FullyQualifiedName~P110AuthenticationPersistenceTests.RefreshRotation_ReplacementOutlivesSession_ClampsPersistedExpiry --nologo -v q` executed 1, passed 0, failed 1. Green build `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --nologo -v q -clp:ErrorsOnly` passed with 47 analyzer warnings; green SQL test executed 1, passed 1, failed 0, skipped 0 using the disposable `IdentitySqlServerFixture`.
- Reused/invalidated evidence: prior authentication and label tests remain valid; HTTP/Postman/migration/producer-consumer/deployment gates remain `NOT_RUN`.

### 2026-10-02 - PARTIAL

- Scope/result: External review P2 fixed in `TrainingLabelRevision.Review`: every fallible review input is normalized before terminal state/proof mutation, so a rejected reason leaves the revision pending for a later valid review.
- Files: `RoadGuardSystem.BusinessObjects/Labels/TrainingLabelRevision.cs`; `tests/RoadGuardSystem.UnitTests/Labels/Huy01TrainingLabelRevisionTests.cs`.
- Verification: red focused test executed 2, passed 0, failed 2; fresh unit build passed with 5 analyzer warnings; green focused label tests executed 4, passed 4, failed 0, skipped 0. HTTP/SQL/schema gates are unchanged and `NOT_RUN` for this entity-local fix.

### 2026-10-02 - PARTIAL

- Scope/result: Implemented the approved independent Report/IncidentCase/Candidate domain core. Reports preserve immutable owner/original evidence and append-only supplements; cases implement `UNASSIGNED`, `OPEN`, `AWAITING_EVIDENCE`, `CONCLUDED`, `LINKED`, triage, supplement reopen, project-safe link/split, conclusion and immutable publication selection; candidates implement immutable `KEEP_NEW`, `LINK_EXISTING`, `REJECT` decisions plus matching active-disposition correction prerequisites. There is no inspection task, duplicate detector, auto-routing, auto-merge, fake producer, endpoint, or persistence activation.
- Files: `RoadGuardSystem.BusinessObjects/{Reports,Cases,Candidates}/`; `tests/RoadGuardSystem.UnitTests/{Reports,Candidates}/Huy01*DomainTests.cs`; this spec's §§5.3 and 9.4.
- Source evidence: `planning/development/HUY-01.md` §§6–8 and §9, `AGENTS.md`, current `BusinessObjects/Defects/Defect.cs` and `BusinessObjects/Surveys/SurveyRequest.cs`. The initial red command stopped at missing Report/Case/Candidate namespaces as expected. The later regression red run executed 9 reporter/case tests: passed 7, failed 2 for duplicated evidence and no-defect prerequisites; both were fixed before mutation occurs.
- Verification: `dotnet build RoadGuardSystem.BusinessObjects/RoadGuardSystem.aBusinessObjects.csproj --nologo -v q -clp:ErrorsOnly` passed with 5 pre-existing analyzer warnings and 0 errors; `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --nologo -v q -clp:ErrorsOnly` passed with 0 warnings/errors; focused Reporter/Case/Candidate test filter executed 13, passed 13, failed 0, skipped 0. Unit tests prove only entity-local invariants, not current authorization, true file verification, SQL uniqueness/rowversion, atomic durability, HTTP, Postman, or external consumers.
- Self-review: pass 1 found/fixed direct mutable collection exposure and discarded triage provenance; pass 2 confirmed no `DbContext`, mapping, migration, shared DI/config, canonical contract, integrated Postman, API or HUY-02 file changed. `deliveryStatus` remains `PARTIAL`; shared integration requirements are recorded in §9.4.

### 2026-10-02 - PARTIAL

- Scope/result: Addressed external ChatGPT review P2-1/P2-2 from domain-core commit `8ad937aea0c893fdc5e9dc7ae60dcfa742a944d3`. `SplitReports` now rejects its source ID before link/history mutation. Publication prerequisites now use server-resolved `CasePublicationRecipientFacts` per report; every selected defect/evidence ID must be present in every selected recipient's authoritative facts before a new immutable snapshot is appended.
- Files: `RoadGuardSystem.BusinessObjects/Cases/{IncidentCase,CasePublicationPrerequisites,CasePublicationRecipientFacts}.cs`; `tests/RoadGuardSystem.UnitTests/Reports/Huy01ReporterCaseDomainTests.cs`; this spec §§5.3 and 9.4.
- Verification: P2-1 red focused test executed 1, passed 0, failed 1; P2-2 red compile stopped at the intentionally absent recipient-scoped type. Fresh focused Reporter/Case tests executed 12, passed 12, failed 0, skipped 0. Fresh BusinessObjects and UnitTests builds passed with 0 warnings/errors; full `Package=HUY-01` trait executed 20, passed 20, failed 0, skipped 0. The final documentation checkpoint `git diff --check` passed with no whitespace errors.
- Self-review: privacy pass confirmed a case-wide verified/permitted set cannot establish the report-recipient relation and the replacement fails missing recipient/relation before `_publications.Add`. Partial-mutation pass confirmed self-ID split rejects before source mutation/history creation and failed publication leaves `Publications` unchanged. External ChatGPT review of this domain-core slice is `PASS`: P2-1 and P2-2 are `CLOSED`, with no new P0/P1/P2. `deliveryStatus` remains `PARTIAL` and SQL/HTTP/shared-integration gates remain pending.

### 2026-10-02 - PARTIAL integration-preparation checkpoint

- Scope/result: Documentation-only readiness checkpoint from base `ad52f4cae1d2f52f030ea23d59866004001c5aa6`. Revalidated §9.4 handoff: Anh is the named writer for `RoadGuardDbContext`, migrations/snapshot, shared DI/configuration, canonical HTTP/event contracts and integrated Postman; Huy's domain inputs remain inactive until producer facts, schema and composition are integrated under that reservation. No domain, label, API, persistence, schema, DI, contract, Postman, or HUY-02 file changed.
- External review: ChatGPT `PASS` for the domain-core slice at `ad52f4c`; self-ID split and recipient-scoped publication authorization are closed. HUY-01 is not `DONE`: the next blocker is Anh's shared integration, including SQL/HTTP producer-consumer fixtures, not missing domain implementation.
- Evidence reuse: no code changed, so builds and full `Package=HUY-01` test evidence (20 executed, 20 passed, 0 failed, 0 skipped) is reused from `ad52f4c`; no new compilation/test claim is made for this documentation-only commit. Final `git diff --check` passed with no whitespace errors.
- Source prompt: `planning/development/HUY-01-spec-and-codex-prompt.md` remains absent from this checkout. It was not recreated from this spec or memory; recovery requires the original external source.

## 1. Assignment, baseline và giới hạn

- **ASSIGNED có checkpoint** theo yêu cầu Huy trong cuộc trao đổi này: triển khai trọn HUY-01, tận dụng source; phần đủ căn cứ ở §3 làm ngay. Các lựa chọn nghiệp vụ/public compatibility ở §11 vẫn **PROPOSED**, không được coi im lặng là chấp thuận.
- GitHub đã kiểm tra `compare(base=1ecae797caaed1ab912b02b2372a1940d1e05375, head=huy-review)`: `identical`, ahead=0, behind=0. Remote `huy-review` đã có đúng guidance. Không cần cherry-pick hay cập nhật từ nhánh Anh.
- Revision dùng để đọc source: `1ecae797caaed1ab912b02b2372a1940d1e05375`. Baseline RF-11 tham chiếu: `21223aa1d18f510d012f9f25081b8c4bcf87b7a8`. Local HEAD/dirty paths của Huy: **UNKNOWN**, Codex ghi lúc bắt đầu.
- ChatGPT viết spec và review diff; Codex local triển khai, hai lượt self-review/fix, test, commit/push. Không amend/force-push commit đang review. Không merge develop, chạm main hoặc sửa nhánh Anh.
- Một spec này và một PR summary là đủ. Không RF audit/remediation mới, ZIP, audit package hay coordination document riêng. Manifest `skills: []`; không dùng RoadGuard skills retired, kể cả khi tài liệu V2 cũ yêu cầu.
- Không triển khai HUY-02. Không provider AI thật, AI processing retry/late-attempt remediation, compatibility callback A08-01 hoặc remediation A09. Idempotency/retry của command nghiệp vụ vẫn bắt buộc. HUY-01 bàn giao interface cho sync/inspection/notification, chưa xây các flow đó.
- Mục tiêu 1–2 ngày là timebox điều phối, không là bằng chứng hoàn thành. Điểm kiểm soát theo luồng chạy được và SQL evidence; không bỏ chức năng vì hết giờ.

## 2. Nguồn và kết luận reuse

Đã đọc `AGENTS.md`, `.agents/manifest.json`, `.agents/modules/README.md`, bốn rules evidence/delivery/safety/review-and-coordination, `planning/development/README.md`, `spec-template.md`; product confirmed-decisions/workflows/data-and-quality, các đoạn FR-11..14/36, BR-29/30/47/48 trong historical-fr-br; backend README/persistence-and-operations; contracts README/events; decision register và D-AUTH-proposed. Tài liệu historical được dùng tìm yêu cầu, không tự nâng thành quyết định mới.

| Bằng chứng tại revision trên | Kết luận và cách dùng |
|---|---|
| **TARGET_CONFIRMED** PR-33A | Candidate cùng project; ưu tiên segment giao/lân cận; dùng vị trí/sai số/lịch sử; thiếu GPS dùng scope+ảnh; PM được mở rộng trong project; không auto-merge. Không có ngưỡng khoảng cách/recall đã duyệt. |
| **TARGET_CONFIRMED** PR-34A | PM đúng project duyệt/từ chối nhãn; chỉ approved xuất training; AI không tự duyệt. Không hỏi lại quyền duyệt nhãn. |
| **TARGET_CONFIRMED** PR-36A/37 | Web cookie + server session, idle 30 phút/max 12 giờ; Android access 15 phút, refresh rotation max 30 ngày từ login; OTP 10 phút/5 lần, resend ≥60 giây, ≤3 lần/15 phút. Compatibility với bearer cũ còn mở. |
| **CURRENT_VERIFIED — source inspection** `API/Controllers/AuthController.cs`, `ReporterRegistrationsController.cs`, `MeController.cs`, `ProfileController.cs`; `Services/Implementations/Authentication/{AuthService,IdentityOnboardingService,AuthoritativeSessionValidator}.cs` | Có login/refresh/logout/password flows, Reporter OTP, session và authoritative validation. Giữ và mở rộng; không xây lại identity. Đây không phải kết quả test mới. |
| `API/Extensions/ServiceCollectionExtensions.cs`, `Authentication/JwtBearerConfiguration.cs` | Default bearer; có AI scheme riêng, chưa có cookie flow. Không đổi default toàn API khiến route cũ đột nhiên nhận cookie. Bearer có ngoại lệ replay hẹp cho logout/change-password; không mở rộng ngoại lệ này sang command nghiệp vụ. |
| `AuthService.RefreshAsync`, `Repositories/Implementations/Identity/IdentityRepository.RefreshTokens.cs` | Replacement expiry đang tính `now + RefreshTokenLifetimeDays`; parent session vẫn có expiry riêng. Chưa thể coi đã đạt Android max 30 ngày từ login. `UserSession` chưa có transport/last-activity. Phải xử lý cả issuance từ onboarding/invitation, không chỉ Login. |
| `IdentityOnboardingOptions.cs` | Default OTP khớp PR-37; cần kiểm effective config và HTTP/SQL, không đổi số đã chốt. |
| `BusinessObjects/Defects/{Defect,DefectVerificationLog}.cs`, `Processing/AIDetection.cs`, mappings và tests P232 | Có defect/detection/log, immutable AI detection, unique `Defect.SourceAIDetectionId`. Dùng lại catalog, audit, reference và invariants. Chưa có production Report/IncidentCase/TrainingLabel aggregate/controller trong tree đã đọc. |
| `DetectionReviewPersistenceService.PersistAsync/PersistAggregateAsync` | Primitive hiện tạo **mới** detection + defect + field inspection task + log/audit/outbox cùng transaction. Không gọi nguyên trạng để review detection đã có, link defect cũ hoặc quyết định không cần task. Không biến việc giữ candidate thành tự giao inspection. Giữ contract cũ cho caller/test cũ; thêm seam mới đúng HUY-01. |
| `ProcessingV2PersistenceService` result ingestion | Detection được lưu với job/model, nhưng geometry/road-version có thể null. PM consumer phải nhận trạng thái thiếu dữ liệu; không bịa geometry từ RawPayload, không sửa callback bị loại khỏi đợt. |
| `Services/Implementations/Files/UploadService.cs` | Có purpose REPORT_PHOTO nhưng `IsSupportedRole` loại Reporter, Create yêu cầu project. Đây là dependency ANH-01 thật cho report chưa rõ project. Không lách bằng cấp Reporter role staff hoặc nhận tùy ý fileId. |
| `ProjectScopeGuard.AuthorizeAsync` | Có live membership/effective date; Supervisor bypass project membership. Vì PR-34A là PM, label command phải kiểm role PM **trước** guard; không suy Supervisor/Admin được duyệt nhãn. |
| `IdempotencyOperationService` + `IdempotencyRecordConfiguration` | Reuse SQL receipt, scope unique `(actor, project, operation, key)`, replay recovery sau commit acknowledgement failure. Không dùng in-memory receipt. Handler retry không được chứa gửi email/storage/provider. |
| API auth/identity tests, `Rf1006ReporterDefectCharacterizationTests`, P232 SQL tests | Có nền regression, nhưng test RF1006 hiện kiểm processing-job read/scope, không chứng minh Reporter/case đã có. Test file tồn tại không là PASS. |

External Web/Android client implementation và consumer sign-off: **UNKNOWN**. FE authentication guide trong `docs/diagram/V2/09_Frontend/` là tài liệu lịch sử, không chứng minh client triển khai. Mọi test/build trong phiên viết spec này: **NOT RUN**.

## 3. Phần giao làm ngay và checkpoint

Giao **một gói HUY-01**, không chia ticket theo từng API:

1. Reuse/hoàn thiện identity core: policy thời gian theo PR-37, authoritative user/session checks, rotation/revocation concurrency, OTP boundaries; thêm unit và regression trên luồng hiện có. Giữ wire/behavior compatibility hiện tại cho đến D1; policy mới có thể chạy qua service/test mà chưa đổi live route/config. Không sửa shared DI/migration.
2. Candidate matching core theo PR-33A: internal service/query trên nguồn project/version hợp lệ, trả lý do xếp hạng, thiếu GPS không loại mất nguồn, không auto-merge. Làm các test scope, deterministic order, stale version, thiếu provenance. Geometry/file producer chưa có thì báo dependency cụ thể; mock chỉ chứng minh consumer contract.
3. Label authorization/domain core theo PR-34A: PM project-scoped, decision có actor/revision/source, approved-only projection, không AI auto-approve. Chưa bật HTTP hoặc mapping chưa được Anh tích hợp; lifecycle mới ở D3 phải được chốt trước phần phụ thuộc.
4. Chuẩn bị source/entity/config/tests của Huy theo §9 sau checkpoint tương ứng; các thay đổi schema là additive đề xuất cho Anh, không tự generate migration/snapshot. Không dùng `EnsureCreated`/test-only mapping để tuyên bố production persistence xong.
5. Gửi ngay trong spec/PR delta file chung và dependency upload Reporter tới Anh. Khi D1–D4 được trả lời và interface/writer được thống nhất, tiếp tục hoàn tất nguyên luồng §4–§8, không yêu cầu approval lại từng endpoint.

Nếu chưa có câu trả lời, vẫn commit phần độc lập có giá trị; PR ghi `PARTIAL — decision/integration pending`, không nhận HUY-01 DONE. Không cài branch chưa được duyệt rồi che bằng response thành công giả. Không hỏi lại quyết định 32–44.

## 4. HTTP conventions cho phần mới — chờ D4 adoption

Các bảng dưới là thiết kế **PROPOSED** cụ thể của spec này, không phải mô tả API đã tồn tại, cũng không tự kích hoạt toàn bộ OpenAPI V2 lịch sử. D4 chốt adoption cho đúng HUY-01; Anh là writer canonical contract.

- Giữ `/api/v1` và camelCase như current controllers. UUID không rỗng; UTC ISO-8601; enum wire là chuỗi explicit, không serialize số enum persistence. Không đổi serializer toàn ứng dụng. Không trả EF entity, storage key/URI, raw AI payload hoặc secret.
- New list: `pageSize` mặc định 20, khoảng 1..100; opaque `cursor`; response `{items,nextCursor}`. Order ổn định `(createdAt,id)`; matching dùng thứ tự riêng §7. Cursor gắn filter/project/actor và version nguồn khi cần; cursor sai trả 400. Đây là limit phân trang kỹ thuật, không giới hạn số report/bằng chứng hợp lệ.
- New resource GET/mutation trả strong `ETag: "<base64-rowversion>"`; body `version` là cùng opaque value không quotes. Mutation tài nguyên đã có cần `If-Match`; không chấp nhận wildcard/weak tag. Missing 428, malformed 400, stale 412. Create không cần If-Match.
- Command mới cần `Idempotency-Key` 1..200 ký tự printable ASCII, trim; new UUID command identity không thay business dedup. Same key/request trả cùng status/body/Location/ETag đã commit; key khác payload 409. Chi tiết ordering ở §8.
- HTTP errors `application/problem+json`: `{type,title,status,detail,instance,code,correlationId}`; validation có `errors` theo field. Giữ lỗi lowercase current API, không đổi sang catalog uppercase draft.

| Status | Code/điều kiện cho API mới |
|---|---|
| 400 | `validation_error`: JSON/UUID/enum/header không hợp lệ, mutually exclusive fields, unknown fields trên DTO mới. |
| 401 | `auth_unauthorized`, `auth_session_revoked`, `refresh_token_invalid`, `refresh_token_expired` tùy auth; cookie không redirect HTML. |
| 403 | `access_forbidden`: sai role hoặc live project membership; `csrf_failed` cho cookie write sai antiforgery; `auth_password_change_required` cho tài khoản buộc đổi mật khẩu. |
| 404 | `not_found`: không có resource hoặc Reporter truy cập report/file không thuộc mình. PM sai project route trả 403 trước đọc resource; ID nằm ngoài project đã được authorize trả 404. Không lộ object metadata. |
| 409 | `idempotency_key_reused`; `invalid_state_transition`; `source_not_ready`; `reference_scope_conflict`; `candidate_stale` theo nguyên nhân. |
| 412 / 428 | `concurrency_conflict` / `precondition_required`. |
| 413 / 415 | Payload vượt request-body limit hiện hành / `unsupported_media_type`; không biến multipart bytes thành report JSON. |
| 503 | `dependency_unavailable` khi dependency cần cho request không khả dụng; không tạo ACK/receipt success. |

Các code mới để trong `Huy01ErrorCodes.cs` do Huy sở hữu; thay central constants phải qua Anh. Error payload không chứa token, email người khác, raw request, SQL exception. Header/shape v1 cũ giữ nguyên; bảng này không normalize API cũ.

## 5. Identity transport — D1

### 5.1 Compatibility cụ thể được đề nghị

Giữ nguyên bearer `/auth/login`, `/auth/refresh`, `/auth/logout`, password routes, `/profile`, `/me`, invitation và Reporter registration hiện có. Không đặt ngày xóa route/client khi chưa có owner. Không auto-relock FE snapshot. Bổ sung transport rõ ràng, không suy từ User-Agent:

| Endpoint | Request | Success / effects |
|---|---|---|
| `GET /api/v1/auth/web/csrf` | Anonymous hoặc web session | 200 `{requestToken,headerName:"X-CSRF-TOKEN"}` + antiforgery cookie; `Cache-Control: no-store`; không tạo authenticated session. |
| `POST /api/v1/auth/web/login` | `{email,password}` + antiforgery pair | 200 `{user:{id,displayName,role,version},mustChangePassword,issuedAt,absoluteExpiresAt,idleExpiresAt}`; Set-Cookie session mới; không access/refresh token trong body. Wrong credential 401 generic. |
| `GET /api/v1/auth/web/session` | Cookie | 200 cùng projection; cập nhật last activity theo quy tắc dưới. |
| `POST /api/v1/auth/web/logout` | Cookie + CSRF + Idempotency-Key, không body | 204; atomic revoke SQL, xóa cookie đúng attributes. |
| `POST /api/v1/auth/android/login` | `{email,password}` | 200 reuse `AuthTokenResponseDto` chính xác: accessToken, refreshToken, tokenType, expiresIn, mustChangePassword, user. |
| `POST /api/v1/auth/android/refresh` | `{refreshToken}` | 200 cùng token DTO; rotation atomic, hết absolute session 401. Không thêm idempotency receipt chứa plaintext credential. |

Android sử dụng logout/change-password bearer hiện có. Web được dùng các identity endpoints hiện có cần thiết (`/me`, `/profile`, change-password) sau khi thêm cookie acceptance + CSRF **có chủ đích** và regression bearer; các staff/onboarding endpoint chưa tích hợp cookie được ghi rõ chưa tương thích, không đổi default ngầm.

Reporter verify/invitation accept đang trả token: giữ response legacy. New Web client có thể hoàn tất onboarding hiện tại rồi đăng nhập Web; không đưa token đó vào cookie và không coi đó là web session. Nếu muốn one-step cookie onboarding, đó là delta compatibility riêng ở D1, không tự thêm. Android mới đăng nhập Android sau onboarding để nhận đúng transport policy.

### 5.2 Session/security semantics

- Reuse UserSession và credential verifier. Đề nghị thêm `Transport` (`LEGACY_BEARER`, `WEB`, `ANDROID`) và nullable `LastActivityAt`; session absolute boundary dùng `IssuedAt`/`ExpiresAt` sẵn có. Existing rows backfill `LEGACY_BEARER`, không đoán platform, không kéo dài expiry. Legacy issuance giữ semantics cũ trong cửa sổ compatibility; target Android issuance explicit dùng 30 ngày từ login.
- Web: `ExpiresAt=IssuedAt+12h`; hết hạn khi `now >= ExpiresAt` hoặc `now >= LastActivityAt+30m`. Touch last activity chỉ sau xác thực, CSRF và authorization thành công, kể cả GET bảo vệ; không touch cho anonymous/failed request, OPTIONS hoặc rejected replay. Update nguyên tử với điều kiện vẫn active và chưa timeout; request cũ không hồi sinh session đã logout/timeout. Cookie expiry/renewal không vượt absolute SQL expiry.
- Cookie `__Host-RoadGuardSession`, HttpOnly, Secure, Path=/, không Domain; đề nghị SameSite=Lax cho topology same-site. Cookie chứa protected ticket/reference `sid`, server kiểm user/session hiện tại mỗi request; không dùng ticket claims làm authority. Antiforgery token rotation sau login/logout, token gắn session/identity. Không log token.
- D1 phải chọn topology: same-site dùng cấu hình trên; nếu Web/API cross-site thì SameSite=None; Secure, CORS allowlist chính xác + credentials và antiforgery bắt buộc. Không wildcard origin; không tự điền production domain. Thiếu config giữ web transport chưa activate, không hạ Secure cho production.
- New HUY business routes nhận explicit cookie hoặc bearer; nếu có Authorization header thì chỉ dùng bearer, invalid bearer không fallback cookie. Khi credentials cookie+bearer chỉ tới hai actor khác nhau, reject 400 `validation_error`; không ghép claims. AI scheme không được vào các routes này.
- CSRF trên mọi unsafe request dùng cookie, kể cả login/logout và identity write đã opt-in. SameSite không thay CSRF; không bật cookie toàn v1 mà chỉ bảo vệ endpoints mới. Bearer routes không buộc CSRF khi không dùng cookie.
- Android access 15 phút; refresh expiry không vượt `session.IssuedAt+30d` và `session.ExpiresAt`. Clamp ở cả service và repository để caller khác không kéo dài. Absolute expiry không trượt khi rotation. Tại đúng boundary không phát token mới. Session/token không được hồi sinh khi logout/role change/password reset đua với refresh.
- Reuse replay-revokes-family semantics hiện có; hai refresh đồng thời có tối đa một rotation winner nhưng replay có thể revoke cả family (test hiện có chứng minh ý định). Không hứa retry refresh trả lại plaintext token cũ. Client single-flight; reconnect không xóa offline queue.
- User inactive, revoked session, changed role, mustChangePassword kiểm bằng current SQL, không cached claim. Must-change-password chỉ vào đúng recovery/profile-minimal/logout/password endpoints được cho phép; không tạo report/duyệt label.
- OTP effective config đúng PR-37; test đủ cả attempt 5/6, resend 59/60 giây, lần 3/4 trong 15 phút, OTP cũ sau resend, concurrent verify. Reuse sender test double, không gửi email thật; ghi email delivery `mock verified`.
- Cookie/transport state, revoke audit và receipt logout thuộc transaction SQL. Session auth errors không chứa credential. Bảo toàn replay logout hẹp sau revoke khi chứng minh đúng actor/sid/key đã commit; không mở quyền business chỉ để trả replay.

### 5.3 Internal domain contract — approved independent core (2026-10-02)

This contract is `TARGET_CONFIRMED` for the pure domain package below. It is deliberately not an HTTP, schema, DI, or cross-owner producer contract.

- **Source identity and scope facts:** `CandidateSourceIdentity={kind,id,sourceVersion}` is immutable and `kind` is `REPORT|AI_DETECTION|FIELD_OBSERVATION`. A command additionally receives server-resolved `projectId`, `geometryVersion`, and, for a correction, the active disposition `{decisionId,version}`. The service/repository must re-resolve these facts under its concurrency boundary; client hints, a client-supplied project, provenance, owner, or `verified` flag are never authoritative evidence.
- **Evidence facts:** a `VerifiedEvidenceReference` is a Huy-owned domain input with immutable `fileId`, `fileVersion`, `ownerUserId`, and verification state; the created `ReportEvidence` records its immutable report/supplement relationship. The domain accepts only `VERIFIED`; the future file adapter, not the client, resolves ownership, verification, source relation, and provenance. Capture metadata is descriptive and does not make upload GPS capture GPS.
- **Project and version facts:** `IncidentCase` stores only its already-authorized assigned project. Triage must supply a non-empty project; link/split require equal non-empty project facts. Expected report/case/source/target/disposition versions are repository preconditions, not an in-memory substitute for rowversion or atomic multi-aggregate writes.
- **Report/case commands:** Report owner and original evidence are immutable. Supplements append immutable descriptions/evidence and do not create a case. A case starts `UNASSIGNED`; triage makes it `OPEN`; awaiting-evidence and supplement/reopen transitions preserve sources and historic publications. Split must reject a new case ID equal to its source ID before it changes links/history. Conclusion needs server-resolved verified-defect/evidence facts where the outcome requires them. Publication creates an immutable selected-recipient snapshot; every selected defect and evidence item must be authorized and related for every selected recipient report, not merely present in a case-wide set. It never changes a prior publication or a defect state.
- **Candidate decisions:** `KEEP_NEW` requires classification and forbids a target; `LINK_EXISTING` requires target id/version and forbids classification; `REJECT` forbids both. Every decision has a reason. History is append-only: correction is a new decision and requires both `supersedesDecisionId` and the matching active disposition version. The domain does not create tasks, detect duplicates, route a project, merge cases, or alter the underlying source.
- **Layer boundary:** Services/repositories resolve actor authorization, project membership, source/file/geometry/provenance, catalog, verified-defect and verified-evidence facts, recipient-scoped publication relations, idempotency and expected versions. Domain entities validate only the invariant facts passed in. The repository must later enforce one active report-case link, one active source disposition, current-head concurrency, recipient-to-publication relations, and atomic multi-case mutations in SQL.
- **Cross-owner proposals, not frozen/implemented interfaces:** Anh must confirm producers for `VerifiedEvidenceReference`, `ProjectGeometryContext`, and `CandidateSourceFacts`, including `source_not_ready` versus stale/failure semantics. No fake adapter or endpoint consumes them in this package.

## 6. Reporter → case → kết luận/công bố — D2, D4 và ANH-01 file interface

### 6.1 DTO và endpoint đề nghị

`EvidenceInput={fileId, capturedAt?, location?:{latitude,longitude,accuracyMeters?}, locationSource}`; `locationSource=CAPTURE|EXIF|MANUAL|UNKNOWN`; lat [-90,90], lon [-180,180], accuracy ≥0; UNKNOWN có thể location=null. Không lấy GPS lúc upload làm capture GPS. `description/reason` trim, 1..1000 ký tự; danh sách evidence không rỗng khi tạo report. Media ownership/verification kiểm server, không tin client flags.

`OwnReport={id,description,createdAt,version,evidence:[own safe evidence],routingStatus,publicUpdates}`. Không trả internal case/defect list, PM notes, AI raw data hay report của người khác. `publicUpdates` chỉ snapshot đã publish cho report đó.

`InternalCase={id,projectId?,status,version,reportIds,defectIds,verificationMethod?,warrantyRouting,conclusion?,createdAt}`. Chỉ staff có scope thấy internal links. `warrantyRouting=UNKNOWN|IN_SCOPE|OUT_OF_SCOPE`, là kết luận có provenance, không tự tính từ upload/report date.

| Endpoint | Actor/input | Success và ý nghĩa |
|---|---|---|
| `POST /api/v1/reports` | Reporter; `{description,projectHintId?,roadSectionVersionHintId?,segmentHintId?,evidence:[EvidenceInput]}` | 201 OwnReport + Location + ETag; lưu Report và case tiếp nhận, original actor và source. Hint không cấp membership hay quyết định project tự động. |
| `GET /api/v1/reports` | Reporter hiện tại; pagination | 200 chỉ report của mình. Không nhận ownerId filter tùy ý. |
| `GET /api/v1/reports/{reportId}` | Owner Reporter | 200 OwnReport + ETag. |
| `POST /api/v1/reports/{reportId}/supplements` | Owner; `{description,evidence}` + If-Match | 200 OwnReport; append supplement immutable, tăng version; không tạo case mới. |
| `GET /api/v1/cases?projectId=...&status=...` | PM project; Supervisor có unassigned queue | 200 page InternalCase; PM bắt buộc project scope, không xem queue project=null. |
| `GET /api/v1/cases/{caseId}` | Scoped PM / Supervisor | 200 InternalCase + ETag. |
| `POST /api/v1/cases/{caseId}/triage` | Supervisor gán project lần đầu; PM project chọn phương thức; `{projectId,verificationMethod,reason}` | 200 InternalCase; `verificationMethod=FIELD|DRONE|EXISTING_EVIDENCE`; không tự tạo/giao inspection task. |
| `POST /api/v1/cases/{caseId}/report-links` | PM đúng project; `{reportIds,sourceCaseVersions,reason}` + target If-Match | 200 InternalCase; move active case links nguyên tử, giữ original reports và link history. Tất cả source case cùng project, đang mở. |
| `POST /api/v1/cases/{caseId}/report-splits` | PM; `{reportIds,reason}` + If-Match | 201 new InternalCase + Location/ETag; subset không rỗng và không toàn bộ; link history giữ cả cũ/mới, không nhân report/file. |
| `POST /api/v1/cases/{caseId}/conclusions` | PM; `{outcome,defectIds,evidenceIds,reason}` + If-Match | 200 InternalCase; outcome `CONFIRMED|NO_DEFECT|NEEDS_EVIDENCE`; CONFIRMED cần defect đã Verified trong project và evidence thật; NO_DEFECT phải có căn cứ; NEEDS_EVIDENCE không là kết luận hoàn tất. |
| `POST /api/v1/cases/{caseId}/publications` | PM; `{reportIds,defectIds,summary,evidenceIds}` + If-Match | 201 `{id,caseId,status:"PUBLISHED",version}`; tạo immutable public snapshot cho đúng reports, outbox intent một lần. |
| `GET /api/v1/reports/{reportId}/evidence/{evidenceId}/download` | Owner hoặc evidence trong publication gửi đúng report đó | 200 stream từ authorized file adapter; 404 nếu không thuộc projection; không lộ raw storage URI. Upload/download storage do Anh tích hợp. |

Các command mutation có idempotency theo §4/§8. File chỉ nhận VERIFIED và đúng uploader/authorized publication scope; PENDING/FAILED trả 409 source_not_ready, không nhận tạm rồi tạo report thành công. Upload ảnh trước tạo report phải có private owner scope riêng, không yêu cầu fake project/case ID.

### 6.2 Transitions được đề nghị, không dùng trạng thái draft như authority

- Report là nguồn immutable + append supplements; case trạng thái riêng. New report tạo case `UNASSIGNED`; Supervisor triage → `OPEN` với project, không mất source. Nếu muốn auto-route known project, cần D2 khác đề nghị này.
- `OPEN → AWAITING_EVIDENCE` khi cần bổ sung; supplement → `OPEN` để PM xem lại; `OPEN/AWAITING_EVIDENCE → CONCLUDED` chỉ theo kết luận có đủ căn cứ. Supplement sau CONCLUDED giữ bằng chứng và đưa lại `OPEN`; publication cũ giữ nguyên là lịch sử, không tự sửa nội dung đã công bố.
- Linking/splitting chỉ case chưa CONCLUDED; source case rỗng sau move → `LINKED` kèm target pointer. Split tạo OPEN cùng project. Phải lock/update các case theo thứ tự ID ổn định; kiểm version tất cả case nguồn/đích.
- Trong HUY-01 không reassignment case đã có project sang project khác; cần routing correction rõ từ owner trước nếu phát sinh. Cross-project link/split cấm. Không sửa project của defect/AI/file để làm cho link hợp lệ.
- Ngoài/không rõ bảo hành vẫn nhận, không xóa/không từ chối dữ liệu; Supervisor điều phối, PM xử lý theo project được giao; không tự phán trách nhiệm/bảo hành. Chưa rõ project/PM giữ UNASSIGNED, không gán ngẫu nhiên.
- `CONCLUDED` chỉ là kết luận phản ánh, **không** là sửa chữa hoàn tất hay defect Resolved. Không có closeMixedCase/repair completion API trong HUY-01; đó là handoff HUY-02.
- Publication độc lập trạng thái case: đề nghị cho publish từng defect đã Verified liên quan report, dù case còn OPEN; text thể hiện từng phần, không báo toàn case hoàn tất. NO_DEFECT được publish khi case đã kết luận NO_DEFECT. Không publish toàn bộ ảnh internal bằng wildcard; PM chọn evidence được phép. Publication không đổi Defect.Status, không tạo nhãn Approved.
- Reporter chỉ thấy own original/supplement và snapshot đã công bố cho report mình; không thấy contact/identity/reportIds của Reporter khác. Linking nhiều nguồn không mở quyền đọc lẫn nhau. Supervisor/PM internal PII projection tối thiểu: report ID + nội dung/evidence, không mặc định trả email/số điện thoại nếu không cần điều phối.

## 7. PM candidate → defect decision → training label — PR-33A/34A, D3/D4

### 7.1 Matching và review

Nguồn `sourceKind=REPORT|AI_DETECTION|FIELD_OBSERVATION`, sourceId immutable; FIELD_OBSERVATION chỉ interface/future producer HUY-02, chưa xây capture flow. Route project luôn kiểm relation từ nguồn qua case/job/observation adapter. Reporter chưa triage không có candidate project.

| Endpoint | Request | Response/effects |
|---|---|---|
| `GET /api/v1/projects/{projectId}/candidates?sourceKind=...&sourceId=...&expand=false` | PM; optional cursor/pageSize | 200 `{source:{kind,id,version},geometryVersion,algorithmVersion:"huy01-1",items,nextCursor}`. Item `{defectId,version,segmentId?,priorityGroup,distanceMeters?,accuracyMeters?,reasonCodes,evidenceRefs,historySummary}`; version là version hiện tại của target defect. Không mutation. |
| `POST /api/v1/projects/{projectId}/candidate-decisions` | PM; `{sourceKind,sourceId,sourceVersion,geometryVersion,decision,targetDefectId?,targetVersion?,classification?,reason}` + Idempotency-Key | 201 `{id,sourceKind,sourceId,decision,defectId?,version}` + Location tới decision GET; tạo decision và effect duy nhất. |
| `GET /api/v1/projects/{projectId}/candidate-decisions/{decisionId}` | PM scope | 200 decision + ETag. |
| `GET /api/v1/projects/{projectId}/defects` và `.../defects/{defectId}` | PM scope; list filter status/type/segment | 200 projection `{id,projectId,roadSectionVersionId?,segmentId?,defectTypeCode,causeCategoryCode?,severity,status,geometry?,version}`; không expose internal raw payload. |
| `POST .../defects/{defectId}/assessments` | PM; `{defectTypeCode,causeCategoryCode?,severity,reason,evidenceIds}` + If-Match | 200 Defect + ETag; append assessment history, giữ PM decision khi suggestion thay đổi; không tự xác minh/đóng. |
| `POST .../defects/{defectId}/verification-decisions` | PM; `{decision:"CONFIRM"|"REJECT",verificationMethod,evidenceIds,reason}` + If-Match | 200 Defect + ETag; Open → Verified hoặc Rejected; không yêu cầu fake inspection task khi evidence đủ; branch cần measure/field evidence mà chưa có phải chặn. |

`classification={defectTypeCode,causeCategoryCode?,severity,roadSectionVersionId,segmentId?,geometry?}` cho KEEP_NEW. Type/cause đối chiếu catalog; severity wire mới là `LOW|MEDIUM|HIGH|CRITICAL`, map explicit tới enum hiện hữu Low=1/Medium=2/High=3/Critical=4; không dùng AI confidence suy ra severity. Defect status wire mới `OPEN|VERIFIED|REJECTED|RESOLVED`, không đổi persisted values. Geometry phải qua adapter CRS/version Anh; thiếu GPS có thể geometry=null nhưng giữ project+scope+ảnh. Không tạo Point(0,0), không coi null là đã đo/xác minh. Delta factory/mapping null geometry phải được Anh tích hợp và SQL test.

Candidate decision DTO có thêm optional `supersedesDecisionId, previousDecisionVersion` khi correction; thiếu một trong hai hoặc correction không khớp active decision trả 400/412. KEEP_NEW bắt buộc classification và cấm targetDefectId; LINK_EXISTING bắt buộc targetDefectId/targetVersion và cấm classification; REJECT cấm cả hai. Mọi decision cần reason. Field `sourceVersion` opaque đại diện source + active disposition; source AI immutable vẫn phải kèm review-state version, không chỉ job ID.

Verification proposal D3: EXISTING_EVIDENCE cần PM chọn evidence VERIFIED thuộc defect/source và ghi lý do đủ căn cứ; DRONE cần thêm dataset/source-file provenance từ Anh, không coi AI confidence là xác minh; FIELD cần completed verification result đúng task/defect từ HUY-02. Branch FIELD chưa có producer trong đợt này trả source_not_ready, không tạo kết quả giả. Các trường hợp cần số đo/method riêng mà không có hồ sơ đã duyệt không được CONFIRM; không tự đặt ngưỡng. Assessment chỉ trên OPEN/VERIFIED, verification chỉ từ OPEN; thay đổi VERIFIED/REJECTED/RESOLVED sang vòng đời khác cần correction/reopen contract ở gói sau, không âm thầm thực hiện. HUY-01 phải ghi rõ dependency FIELD khi bàn giao, không tính flow đó đã end-to-end verified.

- Matching `expand=false`: ưu tiên segment giao và immediate neighboring segments theo ordered segment set/version của Anh; so overlap với vùng sai số GPS nếu có. `expand=true`: tìm toàn project, vẫn xếp assigned/neighbor trước; không mở cross-project.
- Thứ tự deterministic theo priority group, overlap accuracy, distance khi có giá trị metric hợp lệ, lịch sử/source links liên quan, rồi ID làm tie-break. Không tính mét bằng geometry degrees; metric CRS chưa có → distance=null. Không bịa score confidence/threshold merge. Thiếu GPS: dùng scope + evidence thumbnail/references và history để PM so; không tuyên bố đã có image similarity model.
- Response nêu reason codes (`ASSIGNED_SEGMENT`, `NEIGHBOR_SEGMENT`, `ACCURACY_OVERLAP`, `HISTORICAL_LINK`, `PROJECT_EXPANSION`, `GPS_MISSING`) và model/geometry thiếu dữ liệu khi có. Không mất source thiếu GPS, không bỏ nguồn chỉ vì matching rỗng.
- Decision đề nghị: `KEEP_NEW`, `LINK_EXISTING`, `REJECT`; ADJUST thể hiện classification của KEEP_NEW hoặc assessment sau đó, không mutate AI source. KEEP_NEW tạo Open defect + source link, không inspection task; LINK_EXISTING giữ riêng nguồn, không tạo defect mới; REJECT giữ source/reason, không xóa evidence.
- Cần sourceVersion và geometryVersion; LINK_EXISTING thêm targetVersion. Re-resolve version và scope lúc commit; stale source/geometry → 409 candidate_stale; stale target → 412. Không yêu cầu candidateId ephemeral đã xem mới được link, nhưng server phải kiểm tất cả facts lại.
- Một source có tối đa một active disposition; sửa quyết định theo D3 bằng decision mới có `supersedesDecisionId` + expected previous version, giữ history và không tự xóa defect/task. Nếu defect cũ đã được downstream sử dụng, trả 409 invalid_state_transition và bàn giao correction workflow; không âm thầm chuyển link. Đây là checkpoint nghiệp vụ, không tự cascade.
- PR-33A cho phép nhiều report cùng một defect. Unique SourceAIDetectionId hiện có chỉ biểu diễn một nguồn tạo defect; thêm source-link relation cho nhiều source, không bỏ unique hoặc sửa AIDetection immutable. Do đó không dùng primitive cũ tạo lại detection đã tồn tại.

### 7.2 Training labels

Nhãn là record riêng có revision và provenance, không đồng nhất trạng thái candidate/defect. PM có thể duyệt nhãn mà không tự resolve defect; raw AI detection không là approved label.

| Endpoint | Request/actor | Success |
|---|---|---|
| `POST /api/v1/projects/{projectId}/labels` | PM; `{sourceKind,sourceId,sourceVersion,fileId,annotation,defectTypeCode,reason}` | 201 Label + Location + ETag, state PENDING. `annotation={kind:"BBOX",coordinateSpace:"NORMALIZED",x,y,width,height}`; 0..1 và nằm trong ảnh; định dạng bbox này chờ D3, không suy từ raw AI JSON. |
| `GET /api/v1/projects/{projectId}/labels` và `.../labels/{labelId}` | PM scope | 200 Label/page. |
| `POST .../labels/{labelId}/revisions` | PM; `{fileId,annotation,defectTypeCode,reason}` + If-Match | 201 Label revision mới PENDING; revision cũ immutable. |
| `POST /api/v1/labels/{labelId}/review` | PM của project được resolve server; `{decision:"APPROVE"|"REJECT",reason}` + If-Match | 200 `{id,status:"APPROVED"|"REJECTED",revision,version}` + ETag. Status này cố ý khác Ack ACCEPTED trong draft cũ, phải qua D4. |

Label projection: `{id,projectId,revision,source:{kind,id,version,jobId?,modelVersionId?,datasetVersionId?},fileId,annotation,defectTypeCode,status,reviewedBy?,reviewedAt?,reason?,version}`. Provenance do BE lấy, không cho client tự khai job/model/dataset để bypass scope. BBOX width/height phải >0, x+width và y+height ≤1; file phải là ảnh VERIFIED thực sự thuộc source. Annotation các định dạng khác không được tự convert thành bbox. Create/revision Location trỏ resource GET thuộc project; ETag là current label head sau mutation.

- D3 đề nghị PENDING → APPROVED/REJECTED; review approved/rejected lại phải tạo revision mới PENDING. Tạo revision mới không kế thừa approval. Chỉ current revision APPROVED export; revision approved cũ vẫn giữ audit và được trích dẫn trong export lịch sử, không tự sửa file export đã phát hành.
- AI adapter có thể cung cấp pending label draft qua contract Anh; không review. PM manual label cho phép flow test thật trước AI; nếu không chấp thuận manual bbox, producer nhãn phụ thuộc format Anh thống nhất và ghi BLOCKED thay vì seed giả production path.
- `IApprovedTrainingLabelReader` là interface bàn giao ANH-02: query project + cursor; trả current approved revision với approval ID/time/PM, provenance và verified file reference. Không viết export job/API của Anh. Test producer → reader bằng SQL thật; ANH-02 phải test reader → exporter thật trước nhận export DONE.
- Export query phải lọc approval trong SQL, không trả toàn bộ rồi mong exporter tự lọc. Không cho client supplied status/approvedBy; sửa annotation invalidates eligibility ngay. Race export/revision có snapshot/as-of semantics rõ trong manifest exporter do Anh quyết định; reader trả revision immutable và watermark, không hứa thu hồi export cũ.

## 8. SQL, transaction, idempotency và migration

### 8.1 Schema delta đề nghị cho Anh

Huy viết entity/config theo ownership; Anh integrate model/migrations/snapshot sau khi D tương ứng được chốt. Tên sau là đề nghị cụ thể, dùng naming hiện hành khi implement, không đổi các enum persisted cũ:

| Aggregate/tables | Dữ liệu/constraints chính |
|---|---|
| UserSession extension | Transport, LastActivityAt; rowversion đã có; check transport hợp lệ, WEB có activity, expiry cố định; legacy default/backfill không revoke hàng loạt. |
| Reports, ReportEvidence, ReportSupplements | ReporterUserId immutable, description, receivedAt; capture metadata riêng uploadedAt; file FK Restrict; unique report/evidence reference theo source, supplement append-only; rowversion Report. |
| IncidentCases, CaseReportLinks, CaseDefectLinks | nullable project chỉ UNASSIGNED; one active case link/report qua filtered unique index; link history endedAt/reason/actor; project FK Restrict; rowversion Case. Report original owner không đổi khi move. |
| CaseConclusions, CasePublications, CasePublicationRecipients/Evidence | Append-only kết luận/snapshot, revision/source versions; publication audience bằng report ID; unique publication/report recipient; không cascade xóa nguồn. |
| SourceDecisions, DefectSourceLinks | sourceKind/id + resolved project/sourceVersion; one active disposition/source, unique active source link; actor/reason/time; rowversion decision; typed FK cho Report/AIDetection, FIELD future chưa có FK/producer thì không nhận production input. |
| TrainingLabels, TrainingLabelRevisions, TrainingLabelReviews | Head/current revision rowversion; immutable source/annotation revision; unique (label,revision), một terminal decision/revision; approval actor/time; query index (project,status,current revision); FK file/catalog/actor, no cascade. |
| Defect existing | Reuse ID/status/catalog/source; thêm/expose concurrency version qua convention đúng source; optional scope/geometry cho thiếu GPS nếu D3 duyệt. Source links bổ sung, không backfill guessed project cho legacy null rows. |

Project relation phải nhất quán ở mọi link. Nếu composite FK thích hợp thì dùng alternate key `(Id,ProjectId)` sau phối hợp; nếu reference nullable/đa nguồn không thể FK trực tiếp, transaction kiểm authoritative relation và SQL tests chứng minh không có đường bypass repository. Không coi một check DTO là đủ integrity.

`RoadGuardDbContext` tự `ApplyConfigurationsFromAssembly`; chỉ thêm configuration cũng đã đổi EF runtime model, dù không thêm DbSet. Vì vậy **không** đưa mapping mới vào deployed/verified build trước migration được Anh tích hợp. Huy có thể commit source/config pending trong feature branch, nhưng phải ghi rõ model/schema drift pending, không mở production host với DB cũ và gọi đó là ready.

### 8.2 Atomic boundary

Một command commit trong cùng SQL transaction: aggregate + links/history + audit + durable idempotency outcome + outbox intent nếu cần. Create report gồm case và report links; link/split gồm toàn bộ nguồn/đích; candidate decision gồm source decision/defect/source links; review label gồm review/current state; publish gồm projection/recipients/outbox.

- Tất cả auth/project/source/version checks quan trọng lặp lại trong transaction. Dùng rowversion cho aggregates; read/lock membership/user/reference tham gia quyết định để tránh check-then-act khi revoked/reassigned đồng thời; giữ locks đến commit hoặc cơ chế equivalent được test. Retry delegate phải load entities mới, không tái add graph đã tracked sau `ChangeTracker.Clear`.
- SQL uniqueness giải quyết concurrent same-key/source, không dùng `if !exists` ngoài transaction rồi insert. Hai update cùng version tối đa một winner. Nếu client mất response sau commit, receipt trả lại outcome; trước commit fail phải rollback hết.
- Idempotency scope cho new commands: `(actorUserId, projectId?, operation:v1, key)` và fingerprint SHA-256 của normalized canonical request **bao gồm resource IDs, expected versions, source/geometry versions**. Authorization/ownership kiểm trước đọc/trả receipt; không replay dữ liệu sau khi actor mất quyền.
- Unassigned/report lifecycle có project thay đổi: các Reporter command dùng immutable receipt scope project=null + actor + operation, report ID nằm fingerprint; case-specific commands cũng dùng scope project=null + actor + operation và case ID trong fingerprint để receipt không mất khi triage. Project candidate/label commands dùng project thật. Không đổi namespace sau routing.
- Ordering: authenticate → validate request/header shape → authorize current resource/project → lookup receipt → nếu same fingerprint replay committed result → nếu key khác fingerprint 409 → nếu chưa receipt thì transaction reread auth/source/versions/transition → mutate+receipt. Nhờ đó retry với If-Match cũ của command đã commit vẫn replay; thay key nhưng stale If-Match trả 412.
- Chỉ persist success receipt cho command mới trong proposal này; validation/unauthorized/stale/error không chiếm key. Outcome lưu sanitized response/status/Location/ETag, không access token, CSRF token, password, OTP hay signed storage URL. Không tự đặt thời hạn xóa receipt/audit; retention do Anh.
- Outbox events đề nghị `report.received.v1`, `report.supplemented.v1`, `case.published.v1`, `candidate.decided.v1`, `training-label.reviewed.v1`; envelope `{eventId,eventVersion,aggregateId,aggregateVersion,projectId?,actorId,occurredAt,correlationId,payload}`. Payload tối thiểu IDs, không ảnh/raw PII. Event names/schema phối hợp Anh; chưa có consumer không được làm dispatcher hiện tại retry/dead-letter vô hạn. Integration phải route supported event types rõ; HUY-02 notification consumption chưa là DONE.
- Không SMTP/object storage/AI call trong transaction hoặc idempotency handler retry. Read file metadata/verification qua adapter trong cùng DB transaction nếu khả dụng; external byte checks hoàn tất ở upload trước đó. Không notification delivery success giả.

### 8.3 Migration/recovery

Anh audit schema/row populations trong isolated copy/fixture: sessions transport unknown; defect project/geometry null; existing uniqueness/immutable detection triggers; scopes file hiện hành. Chỉ báo aggregate counts, không xuất secrets/PII. Schema additive, nullable/backfill tương thích; không sửa migration đã áp dụng. Test migrate baseline → head và database mới → head, old row reads, indexes/checks/FKs/triggers, model snapshot matching. Recovery mặc định rollback application khi schema additive tương thích hoặc forward-fix; không destructive downgrade production làm mất reports/labels. Down/reapply chỉ trên disposable DB và chỉ khi thực sự có safe Down path. Không apply shared/production DB trong gói local này.

## 9. File ownership và interface với Anh

### 9.1 Huy được sửa

- Spec: `planning/development/HUY-01.md` (copy nội dung file này; prompt appendix có thể giữ nguyên).
- Existing module paths: `RoadGuardSystem.Services/Implementations/Authentication/`, `Interfaces/Authentication/`, `Options/IdentityOnboardingOptions.cs`, `Options/JwtOptions.cs`; `Repositories/Implementations/Identity/`, `Interfaces/Identity/`; `BusinessObjects/Identity/UserSession.cs`, `Configurations/UserSessionConfiguration.cs`; relevant auth/identity DTO/controller/validator trong allowlist chỉ để transport hiện rõ, không user-admin feature mới.
- `RoadGuardSystem.API/Authentication/` cho cookie/session handler mới và bounded bearer integration sau D1; Controllers `AuthController.cs`, `ReporterRegistrationsController.cs`, `MeController.cs`, `ProfileController.cs` chỉ thay đổi đã mô tả, thêm `WebAuthController.cs`, `AndroidAuthController.cs`.
- New module folders `BusinessObjects/{Reports,Cases,Labels}/`, `DTOs/{Reports,Cases,Defects,Labels}/`, `Services/{Interfaces,Implementations}/{Reports,Cases,Defects,Labels}/`, `Repositories/{Interfaces,Implementations}/{Reports,Cases,Defects,Labels}/`.
- Controllers mới `ReportsController.cs`, `CasesController.cs`, `CandidatesController.cs`, `DefectsController.cs`, `TrainingLabelsController.cs`; `API/Constants/Huy01ErrorCodes.cs`.
- Existing `BusinessObjects/Defects/`, `Repositories/Configurations/DefectConfiguration.cs` và configuration mới đúng entities trên; không sửa AIDetection/Processing producer của Anh. Enum mới đặt file trong module, không sửa `Common/Enums.cs` tùy tiện.
- Tests mới/ảnh hưởng ở `tests/RoadGuardSystem.{UnitTests,ApiTests,IntegrationTests}/{Authentication,Identity,Reports,Cases,Defects,Labels}/`; fixture module mới không override shared isolation. Giữ tests cũ; chỉ cập nhật expected behavior đúng decision đã chốt, không xóa test để xanh.

### 9.2 Shared files — Anh writer, Huy nêu delta trước

| File/nhóm | Delta cần Anh tích hợp |
|---|---|
| `Repositories/RoadGuardDbContext.cs`, rowversion conventions | DbSets/navigation/constraints nếu cần; kiểm discovery config và query joins. Không lách shared coordination bằng dùng Set<T> rồi tự activate model. |
| `Repositories/Migrations/*`, `RoadGuardDbContextModelSnapshot.cs` bên trong migrations | Additive session/report/case/source-link/label schema, indexes/backfill; một migration writer theo thứ tự ANH-01 trước các FK HUY-01. |
| `API/Extensions/ServiceCollectionExtensions.cs` | Explicit cookie/dual policies, module controller services, CORS/CSRF wiring; AI scheme giữ riêng. |
| `Services/Extensions/AuthenticationServiceCollectionExtensions.cs`, `Repositories/Extensions/RoadGuardPersistenceExtensions.cs` | New service/repository bindings và options validators; Huy có thể viết extension module riêng, Anh gọi từ shared composition root. |
| `API/Program.cs`, appsettings / environment options | Antiforgery/middleware order, cookie settings/allowed origins; không secret, không đổi startup seeding/isolation. |
| Central `ApiErrorCodes.cs`, `BusinessObjects/Common/Enums.cs` nếu cần | Additions rõ, không đổi numeric persisted codes; ưu tiên module-owned definitions. |
| Canonical HTTP/events contracts và integrated Postman/API.http | Adopt đúng HUY routes/DTO/errors/fixtures; giữ legacy identifiers; không promote cả draft hay tự relock FE. |
| Shared test fixtures/project files/CI | Mọi change reserve với Anh; new module fixture dùng isolation hiện có. Không update package/framework. |

Reservation chưa được Anh xác nhận: **PENDING**, không tuyên bố đã phối hợp. Không tự gửi tin nhắn cho Anh từ phiên viết spec. Huy/Codex ghi delta trong spec/PR và Huy chuyển cho Anh; không cần thêm coordination artifact.

### 9.3 Interface freeze đề nghị

| Interface và owner | Input/output + failures | Fixture tích hợp bắt buộc |
|---|---|---|
| `ProjectGeometryContext v1` — Anh producer, Huy consumer | projectId, roadSectionVersionId, segmentSetId/version, segment IDs/adjacency, metric CRS, scope version; missing/stale/wrong-project phân biệt. Huy không tự chỉnh geometry. | Hai project; adjacent/nonadjacent segments; republish version; source không GPS. Read thật → candidate query thật. |
| `VerifiedEvidenceReference v1` — Anh producer, Huy consumer | fileId, ownerUserId, nullable projectId/private intake scope, purpose, verification state, checksum, long sizeBytes, safe media metadata/version. Reporter intake trước project; scoped authorization cho download qua projection. | Reporter upload private → VERIFIED → tạo report thật → owner download; Reporter khác bị chặn; pending/failed/cross-project bị chặn. Test storage fake chỉ ghi mock storage, SQL vẫn thật. |
| `CandidateSource v1` — Anh AI producer/Huy report producer | source identity+version, project, job/model/dataset provenance khi có, geometry/scope/evidence nullable có lý do; không nhận current approved label từ AI. | BE processing/source rows → Huy candidate consumer; missing provenance trả not-ready, không fabricate. Không sửa A08/A09 hoặc retry/late-attempt. |
| `ApprovedTrainingLabel v1` — Huy producer, Anh consumer | Immutable label revision, approval proof/PM, project, source/file/checksum/provenance, watermark/cursor; approved-only. | Pending/rejected bị loại, new revision vô hiệu eligibility cũ, wrong project bị chặn; SQL producer → reader thật. Exporter integration do Anh. |
| `CaseDefectRead v1` — Huy producer, HUY-02 consumer sau | Stable case/defect IDs, source/decision versions, states, verification evidence; không tự quyền repair/close. | Chỉ contract fixture trong HUY-01; ghi HUY-02 consumer NOT IMPLEMENTED. |

### 9.4 2026-10-02 domain-package delta for Anh

- **Producer facts and failure semantics to confirm:** `VerifiedEvidenceReference(fileId,fileVersion,ownerUserId,VERIFIED,captureMetadata?)` must be resolved from storage, with `source_not_ready` for pending/failed verification and a distinct forbidden/not-found result for ownership or authorized-publication mismatch. Publication composition additionally needs `CasePublicationRecipientFacts(reportId, verifiedDefectIds, permittedEvidenceIds)`, resolved server-side for each recipient report; a missing relation/permission must fail the command rather than broaden visibility across linked reporters. `CandidateSourceFacts(CandidateSourceIdentity(kind,id,sourceVersion),projectId,geometryVersion,activeDisposition?)` must be resolved from Report/AI/Field records; absent Field producer or required geometry/provenance is `source_not_ready`, while source/geometry/disposition version drift is stale (409/412 at the wire contract). Huy has implemented only these domain inputs, not their cross-owner adapters.
- **Shared schema writer: Anh.** Integrate `Reports`, original/supplement evidence, `IncidentCases`, active report links plus append-only link/conclusion/publication history, and `SourceDecisions` with source identity/version, active disposition and correction links. Required constraints include a filtered unique active report-case link, one active source disposition, `(case/report/publication recipient)` uniqueness as applicable, restrict FKs, rowversion/current-head checks, and SQL-atomic multi-case link/split, candidate decision, publication and idempotency/outbox writes. No `DbContext`, configuration, migration, snapshot, index, or model discovery change was made here.
- **Composition writer: Anh.** After schema is accepted, wire Huy's Report/Case/Candidate repository interfaces and services through the existing shared composition roots, then adopt canonical HTTP/events and integrated Postman. This package intentionally adds no endpoint, DTO, controller, DI binding, API.http, or fake success adapter.
- **Label boundary:** Training-label persistence/endpoint remains pending until Anh freezes source/file/provenance producer facts; this checkpoint does not activate mapping or create a parallel label integration path.
- **Integration order and fixtures:** (1) Anh freezes producer facts and creates private Reporter upload/verified-file, project geometry and candidate-source fixture rows; (2) Anh integrates additive schema/migration and Huy supplies/aligns mappings only under that reservation; (3) compose services/contracts; (4) run isolated SQL tests for filtered uniqueness, rowversion and multi-aggregate rollback; (5) run API/Postman smoke for owner/scope/replay. Writer for every shared step remains Anh until an explicit reservation transfers it. Unit-domain evidence below does not prove authorization, actual verified files, SQL concurrency, durable atomicity, or external consumers.

#### 2026-10-03 consumer delta to the shared writer

The earlier sentence that this package has no HTTP/controller/SQL consumer is historical. The current Huy module extensions bind `IReporterLifecycleRepository/Service`, `ICaseWorkflowRepository/Service` and `ICandidateDecisionRepository/Service` for disposable test-host composition. Anh remains the named writer for production `RoadGuardDbContext`, mappings/migration/snapshot, shared composition root/options, canonical HTTP/event contracts, integrated Postman and shared fixtures. The imported generic receipt service and its shared guard tests must retain the exact `f626ea595420c3f28a35f2b3f4c6f196d313a7be` blobs; this continuation changes neither file.

- **Producer facts/signatures:** `IAnhHuyProducerService.ResolvePrivateEvidenceAsync(actor,role,file,evidence,expectedFileVersion?,ct)` already returns server-owned owner/private `REPORT_PHOTO` scope, VERIFIED upload version and media/checksum facts; missing/other owner is 404, pending/failed 409, stale version 412. Huy's Case publication repository constructs `CasePublicationRecipientFacts(reportId, verifiedDefectIds, permittedEvidenceIds)` separately per recipient from locked current source/file/Defect relations. Anh must preserve that recipient-scoped relation in any producer or canonical contract; case-wide evidence/defect sets cannot authorize publication. `ResolveCandidateSourceAsync(actor,role,project,kind,sourceId,expectedSourceVersion?,expectedGeometryVersion?,expectedDispositionVersion?,ct)` is the current REPORT producer; AI/FIELD need a separately reviewed provenance producer and `source_not_ready` until then. Wrong project/owner stays privacy-safe; source/geometry/head drift maps to 409/412, without accepting client-supplied authority flags.
- **Schema and SQL:** Existing shared HUY integration rows support Reporter, Case and REPORT REJECT. Anh must add an immutable Report project/road/segment *hint* representation to carry the approved intake fields without pretending a client hint is an assigned scope, and adopt the revised safe create projection without silently dropping fields. For KEEP_NEW/LINK_EXISTING and Defect HTTP writes, add Defect rowversion/current-head concurrency, typed Report/AI source-link and created-Defect relation, unique active disposition/source link, project/route/segment FKs, nullable metric geometry for no-GPS, and correction/downstream-use constraints. Keep the existing filtered one-active-case-per-report link, recipient/evidence relation uniqueness and immutable history/ref FKs. SQL must enforce current-head uniqueness, expected versions, ordered multi-case mutations and atomic business/history/audit/receipt writes. Huy has not edited these shared objects or applied migrations.
- **Remaining producer/consumer seams:** Production matching needs an authoritative Report assigned-segment/scope version and project-filtered Defect target facts (ID, rowversion, segment, geometry metric CRS, history/source links); propose `ResolveMatchSourceAsync(actor,project,report,expectedSourceVersion,ct)` and `ListMatchTargetsAsync(actor,project,scopeVersion,ct)` returning explicit missing/stale/not-ready reasons. Field/Drone conclusion and Defect verification require completion/dataset/source-file proof; confirmed Defect publication requires verified source-link relation per recipient. Warranty stays `UNKNOWN` until the authorized warranty producer exists. Label SQL needs `ResolveLabelSourceAsync(actor,project,source,file,ct)` with immutable source/file/checksum/provenance versions, `TrainingLabels` head/revision/review tables with current-head rowversion and restrict FKs, and `IApprovedTrainingLabelReader.ReadCurrentApprovedAsync(project,watermark,ct)` filtering current APPROVED heads in SQL with an immutable watermark. These signatures are **proposals for Anh confirmation**, not frozen/implemented shared interfaces.
- **Identity/event/binding:** Add/backfill `Sessions.Transport` and `Sessions.LastActivityAt` without changing legacy bearer expiry, then integrate Web cookie/CSRF/idle and Android absolute expiry/rotation through shared auth middleware/options and both issuance paths. Anh must adopt the Huy module extensions in production DI only after schema/contract review. `report.received.v1`, supplement/publication/candidate/label event envelopes, consumer names and outbox behavior need owner/Anh agreement; Huy emits none of them. Anh also owns canonical route/status adoption and integrated Postman.
- **Integration order and fixtures:** (1) Anh confirms producer contracts and missing columns/tables/indexes, supplies an exact shared SHA; (2) import only reserved paths and check blobs, then build/migrate a fresh disposable SQL database; (3) wire module extensions and canonical HTTP/event contracts; (4) rerun real Reporter receipt paths, Reporter A/B private file and publication, PM scope/revoke, cross-project link/split rollback, same-key and post-commit recovery, no-GPS/metric geometry matching, KEEP/LINK target effects and approved-only label SQL reader/exporter fixtures; (5) run legacy auth/defect regression and Postman. No current test asserts production DI, deployed DB, AI/Android external behavior or an Anh exporter.

Thứ tự: chốt semantic D1–D4 → Anh/Huy freeze interface + named writer → Huy commit entity/config/core → Anh integrate ANH-01 refs và HUY-01 migration/DI/contract → Huy nhận integration commit theo phối hợp (không tự merge nhánh Anh), chạy HTTP+SQL end-to-end → cả hai ghi base/head thực tế. Contract/mock có thể chạy sớm nhưng không thay gate SQL/real BE producer.

## 10. Acceptance và tests theo luồng

| Nhóm | Cases bắt buộc / durable proof |
|---|---|
| A — identity transport | Legacy bearer login/refresh/logout và profile/me không regression; Web cookie Secure/HttpOnly/Path/SameSite đúng, không token response/HTML redirect; login/logout/write thiếu CSRF 403; invalid bearer không fallback cookie; origins không được phép bị chặn. |
| A — clocks/concurrency | Web 29:59/30:00 idle và 11:59:59/12:00 absolute; touch không kéo absolute hoặc hồi sinh revoked; Android access 15 phút và refresh ngay trước/tại ngày 30; rotation không extend; duplicate concurrent refresh và logout/reset race; assert SQL session/token/audit đúng. OTP boundaries/concurrent verify đúng PR-37. |
| B — Reporter intake | Owner upload verified + report → đúng một Report/Case/evidence/source/audit/receipt/outbox; chưa project vẫn nhận; same key retry/concurrent/post-commit failure không nhân; same key changed payload 409. File của người khác/pending/bad checksum không commit; GPS upload khác capture không bị dùng sai. |
| B — scope/privacy | Reporter A/B báo cùng lỗi: chỉ own source/publication; đoán report/case/file IDs không lộ ảnh/contact. PM sai project, expired membership, inactive user không đọc/ghi/replay. Supervisor triage unassigned đúng authority, PM không tự nhận unassigned case. |
| B — case workflow | Supplements không tạo case mới; link nhiều report giữ nguồn và một active link; split phục hồi history; cross-project/stale multi-case mutation rollback toàn bộ; conclusion thiếu evidence bị chặn; publication partial không resolve defect, replays không nhân intent. |
| C — matching | Assigned/neighbor ưu tiên; expand vẫn cùng project; no GPS có scope+ảnh; geometry CRS/version không hợp lệ không fake distance; changed candidate/version bị chặn; matching GET không mutate/auto-merge; repeated report không tự tăng severity. |
| C — decisions/labels | Keep-new đúng một defect, link-existing không tạo defect/detection/task; reject giữ source. Không task giả. PM đúng project mới review; Supervisor/Admin/AI không được quyền chỉ vì role cao hơn. Label revision PENDING không export, REJECTED không export, APPROVED mới reader trả; edit tạo revision mất eligibility, history nguyên vẹn. |
| D — SQL risks | Fresh/baseline migration, old rows compatible, rowversion/unique/check/FK/immutable triggers; same-key two connections và same-version two commands; inject precommit failure rollback, postcommit ack loss replay; test actual HTTP status/body/headers và SQL effects. Không in-memory/SQLite thay SQL Server. |
| E — integration | Real BE geometry/file/source producers và Huy consumers; real Huy approved reader và Anh exporter khi Anh tích hợp. Mock AI/Android không được gắn external verified; HUY-02 downstream chưa triển khai. |

Test reuse: `AuthenticationFlowTests`, `AuthenticationSessionFlowTests`, `V2AuthenticationFlowTests`, `V2IdentityOnboardingFlowTests`, `Identity` API/SQL suites, `P232DetectionDefectSchemaTests`, `P232DomainInvariantTests`, `P202TransactionAndIdempotencyTests`. Chọn affected classes, không chạy RF audit suite chỉ vì tên trùng.

Thêm nhóm `Huy01*Tests` và trait `Package=HUY-01` trên test mới. Lệnh gợi ý (Codex xác minh SDK/global.json và test discovery trước chạy):

```sh
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --nologo
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter 'Package=HUY-01|FullyQualifiedName~Authentication|FullyQualifiedName~Defects' --logger trx
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter 'Package=HUY-01|FullyQualifiedName~Authentication|FullyQualifiedName~Identity' --logger trx
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter 'Package=HUY-01|FullyQualifiedName~Identity|FullyQualifiedName~P232DetectionDefectSchemaTests|FullyQualifiedName~P202TransactionAndIdempotencyTests' --logger trx
git diff --check
```

API fixture hiện có dùng Testcontainers SQL Server, unique owned database và `AuthenticationWebApplicationFactory` kiểm owned connection, tắt startup seed. Reuse cơ chế đó; không dùng shared DB. Docker/SQL không sẵn → `NOT RUN`, chưa đạt persistence gate. Test filter chọn 0 tests không là PASS. Với changed clock code, unit dùng TimeProvider, SQL test có controlled fixture/boundary injection, không sleep chờ 30 ngày.

Hai self-review ngắn: (1) auth/scope/transition/transaction/replay/privacy; (2) ownership/interfaces/migration/compatibility/final diff. Fix trong scope, rerun phần bị invalidated. Ghi rõ external ChatGPT review PENDING, không gọi self-review là peer review.

Done HUY-01 chỉ khi phần được owner chốt đã chạy đầy đủ, mapping/migration/shared wiring tích hợp, HTTP+SQL cases pass có counts, open decisions không còn chặn flow. Nếu còn blocker, ghi đúng chức năng/dependency chưa xong; không tự đánh Done nhờ build hoặc mock.

## 11. Một lần gom quyết định còn thiếu

Đề nghị Huy trả lời `D1=A, D2=A, D3=A, D4=A` hoặc chỉnh đúng ý khác. Không phải xác nhận lại PR-33A/34A/36A/37; các câu này chốt phần còn mở trong docs. Không cần hỏi lại approval cho từng bước khi đã chọn.

| ID | Phương án A được đề nghị cụ thể | Lựa chọn khác / phần bị chặn |
|---|---|---|
| D1 — auth compatibility/topology | Giữ v1 bearer/profile/me/reset/onboarding hiện có; thêm explicit Web cookie + Android transport §5; legacy session không bị kéo dài/đổi platform. Web pilot same-site, SameSite=Lax, origins thực cấu hình lúc tích hợp. Onboarding Web hai bước verify hiện có → Web login. Không retire route trong đợt. | Nếu FE/API bắt buộc cross-site, cho biết topology/origins để dùng None+Secure+CSRF+allowlist; nếu muốn cutover breaking/one-step onboarding phải sửa compatibility delta. Chặn activation new transport/legacy expiry changes, không chặn identity core và regression. |
| D2 — tiếp nhận, ownership, case | Cho Reporter gửi chưa rõ project/ngoài bảo hành vào Supervisor queue; hints không auto-route. Một report có một active case; case nhiều reports/defects; PM link/split trong project giữ history. Reporter chỉ own sources + publication riêng. Cho partial publication của defect Verified; supplement sau conclusion mở lại review, không sửa publication cũ. | Nếu không cho partial publication hoặc muốn khác cardinality/routing/supplement policy, chỉ rõ. Chặn case schema/commands/public projection phụ thuộc; Reporter upload adapter là dependency bắt buộc Anh, không âm thầm bỏ. |
| D3 — quyết định PM/label lifecycle | Candidate giữ mới/link/reject, corrections có revision/history; keep không tự tạo inspection. PM verify theo bằng chứng, không giả measure. Label tách defect; manual PM bbox normalized v1 tạo PENDING; PM approve/reject; sửa tạo revision mới PENDING, chỉ current approved export. Không tự reopen/correct defect đã downstream sử dụng. | Nếu cần polygon/mask thay bbox, người khác tạo nhãn, hoặc approved revision cũ vẫn được xuất sau revision mới, chốt thay đổi trước schema/wire. Chặn branches này; matching core và PM scope theo quyết định đã chốt vẫn làm. |
| D4 — adopt wire/schema delta và phối hợp | Duyệt proposal endpoint/DTO/status/errors/transitions/schema trong §4–§8 **chỉ HUY-01** để Codex triển khai trên huy-review; Anh writer migration/DI/canonical contract/Postman theo §9. Không tự đổi consumer cũ, không apply production DB. | Có route/DTO/frontend constraint khác thì Huy đưa một lần; Anh phải xác nhận interface/version/writer slot trước shared integration. Chưa chốt không tự promote draft V2 hoặc gọi dependency fake là integrated. |

Các vấn đề ngoài HUY-01 (Fast Track threshold, repair/acceptance, notification dispatcher, encrypted handover, retention deletion, AI retry) giữ ở owner/gói tương ứng, không đưa vào vòng hỏi này.

## 12. Prompt thực thi hoàn chỉnh cho Codex local

Copy phần dưới cùng spec này vào Codex local. Nếu Huy đã trả lời D1–D4, append nguyên văn câu trả lời ở đầu prompt; nếu chưa thì chạy phần độc lập và dừng đúng checkpoint, không hỏi lặp.

```text
Bạn là Codex local của Huy, triển khai trọn HUY-01 theo spec đính kèm.
Repo HoangAnhVu2207/RoadGuardSystem; chỉ nhánh huy-review.
ChatGPT giữ vai trò định hướng/spec và external review; bạn implement,
hai lượt self-review/fix, chạy tests, commit/push. Không HUY-02.

1) Preflight:
   - Ghi git status --short, git branch --show-current, git rev-parse HEAD.
   - Preserve dirty files; nếu có thay đổi không thuộc task, dùng worktree sạch,
     không reset/clean/stash tự ý. Không sửa checkout của Anh.
   - git fetch origin huy-review
   - Xác minh guidance là ancestor HEAD bằng:
     git merge-base --is-ancestor 1ecae797caaed1ab912b02b2372a1940d1e05375 HEAD
   - Remote đã được ChatGPT quan sát identical guidance, nhưng phải recheck drift.
     Nếu local sạch đang ở huy-review và chỉ behind origin/huy-review:
     git merge --ff-only origin/huy-review
   - Nếu guidance chưa có, chỉ dùng:
     git merge --ff-only 1ecae797caaed1ab912b02b2372a1940d1e05375
     khi commit hiện hữu và ancestor cho phép. Nếu diverged, báo graph/base/head,
     không reset, force-push, amend hoặc cherry-pick làm giả ancestry.
   - Không fetch/merge tip anh-review thay cho exact integration commit đã phối hợp.

2) Đọc AGENTS.md, .agents/manifest.json, relevant rules/module map,
   planning/development/README.md, spec-template.md và spec HUY-01 này.
   Manifest skills=[]; không retired skills, không RF remediation/audit.
   Lưu spec tại planning/development/HUY-01.md và ghi base SHA/dirty paths thực.

3) Đối chiếu source mới với revision spec; reuse auth/onboarding/session,
   project guard, immutable detection, defect/catalog, audit/outbox/idempotency.
   Không copy nguyên primitive DetectionReviewPersistenceService để tạo task giả
   hay duplicate detection. Không coi Upload REPORT_PHOTO đã hỗ trợ Reporter.

4) Thực hiện một package theo flow:
   identity transport/core -> Reporter private evidence/intake/case -> PM matching,
   decision/defect -> training-label approval/approved reader.
   D1-D4 chưa chốt: làm độc lập đúng §3; không activate branch còn PROPOSED.
   Có câu trả lời rồi: thực hiện đúng semantics ấy, không hỏi lại từng API.
   Không tự cắt tính năng, invent business thresholds, role, publication policy.

5) Tuân thủ allowlist §9. Trước shared edit, ghi delta cụ thể/named writer/integration
   order ngay trong spec hoặc PR summary để Huy chuyển Anh. Anh owns DbContext,
   migrations/snapshot, shared DI/config, canonical contract và integrated Postman.
   Chưa có Anh integration thì persistence/integration PENDING, không dùng mock
   thay SQL evidence. Không tự sửa nhánh Anh, không áp migration shared/production DB.

6) HTTP controllers -> service policy -> repository SQL. Ownership/current project
   checks phải có cả new command và replay. Business+history+audit+receipt+outbox
   atomic; idempotency fingerprint gồm resource/version, replay trước fresh ETag
   check nhưng sau authorization. Same request/postcommit lost response không
   nhân effects. Cross-project, revoked actor, stale version đều có negative tests.
   Không SMTP/storage/AI call trong SQL retry delegate.

7) Chạy focused build/unit/API/SQL tests của §10 sau shared integration.
   SQL Server disposable owned fixture; không in-memory/SQLite thay SQL Server.
   Ghi executed/passed/failed/skipped/not-run counts; filter zero tests không PASS.
   Không broad suite nếu không có risk/gate cụ thể. External AI/Android dùng fixture
   thì chỉ ghi mock verified; external/deployment chưa kiểm chứng.

8) Self-review pass 1: auth/scope/privacy/transitions/transaction/retry/concurrency.
   Pass 2: file ownership/producer-consumer/compatibility/migrations/final diff.
   Fix trong scope, rerun invalidated tests, git diff --check. Nếu blocker còn,
   tiếp tục phần độc lập; ghi rõ phần còn thiếu, không tuyên bố DONE.

9) Commit mới trên huy-review và push bình thường origin huy-review; kiểm diff/staged
   để không gom unrelated files hoặc secrets. Không amend/force-push đang review,
   không merge develop/main. Không tự sửa conflict do concurrent push bằng rewrite.
   Trả một PR summary: behavior, base/head, changed scope, two self-review results,
   commands/counts, SQL migration integration SHA, open D/dependencies, BE verified /
   mock verified / external-deployment not verified. External ChatGPT review PENDING.
   Huy sẽ gửi base SHA/head SHA cho ChatGPT review đúng diff.
```

PR summary mẫu (điền kết quả thật, không thêm báo cáo riêng):

```text
HUY-01 — identity transport / Reporter-case / PM candidate-label
Base: <sha>; Head: <sha>; branch: huy-review; dirty preserved: <paths/none>
Behavior delivered: <luồng thực tế>; Pending: <decision/interface/SQL còn thiếu>
Shared integration: <Anh commit SHA + migration/DI/contract>; external review: PENDING
Self-review 1: <findings/fixes>; Self-review 2: <findings/fixes>
Checks: <commands; executed/pass/fail/skip/not-run counts; SQL fixture>
Evidence: BE verified <...>; mock verified <...>; external/deployment not verified <...>
```
