# BE–AI readiness audit: executed assessment

Date: 2026-11-23. Assigned scope: [audit spec](../../../planning/development/BE-AI-READINESS-AUDIT-20261123.md), [A01–A10 plan](2026-11-23-be-ai-readiness-audit.md). Codex is the authorized BE–AI writer; this execution changes audit/knowledge Markdown only. Existing application changes are preserved. No deployment, shared database write, migration application, cleanup, commit or push.

## Verdict

**Local inference and the demonstrated PM/Crew path work under the recorded test setup. Production integration is BLOCKED.** A01–A10 are assessment tasks: completing them records findings and unresolved gates; it does not mean every product requirement has passed.

CURRENT_VERIFIED, executed isolated SQL probe: `AiVideoWorkRepository.ClaimAsync` fails with the default SQL retry strategy because it starts a user transaction outside `CreateExecutionStrategy().ExecuteAsync`. This blocks automatic hosted processing even on an empty queue. Existing repository tests use non-retrying contexts and do not prove the real application configuration.

CURRENT_VERIFIED, source inspection: REAL artifact and external-SRT retention linkage is incomplete. The AI retention contributor covers mock provenance only. No deletion or resulting data loss was observed. The REPORT-only case/defect dossier adapter also lacks accepted SURVEY source support. These and the hosted/Ubuntu gates below prevent a ready verdict.

## Baseline and evidence inventory (A01)

BE branch `anh`, HEAD `4934103d125be1caca4c6a588eed3768c9cafee4`; AI HEAD `c689e544400fe326683c1c7f45115b3f7ec01daf`. Both have pre-existing uncommitted integration changes. Git HEAD alone does not identify assessed content: dirty-path snapshots and source hashes are required together.

Local evidence root: `D:/RoadGuard-AI-LocalTest/audit-20261123`. It contains `baseline-status.txt`, `ai-baseline-status.txt`, `source-hashes.csv`, `source-inventory.csv`, `audit-unit.trx`, `unit.log`, `python.log`, `python-ffmpeg.log`, `audit-sql.trx`, `sql.log`, `retry-probe.log`, and consumer/API reports. The standalone retry probe lives outside Git under `retry-probe/`; it uses `SqlServerTestFixture.CreateForSandboxSeed()`, verifies live Testcontainers ownership, and disposes its SQL container. No connection string is copied here.

| Authority/source | Baseline/status | Role in assessment |
| --- | --- | --- |
| `planning/development/BE-AI-INTEGRATION-20261119.md`, owner amendments | Same BE base; working-tree version | TARGET_CONFIRMED: automatic submission intent, operator estimate, PM authority, six separate labels; repair is existing patch |
| `docs/product/confirmed-decisions.md`; relevant `HUY-FINAL-INTEGRATION.md` | Same BE base; working-tree version | TARGET_CONFIRMED: company workflow/role and retention policy41A; historical prose does not approve new contracts |
| `contracts/ai/worker-v1.md`, `video-telemetry-v1.md` and schemas | Dirty/untracked integration slice; inventory snapshot | Local adopted contract evidence, not proof of deployed compatibility |
| `RoadGuardSystem.BusinessObjects/Processing/Worker/`, repository worker configurations/migrations | Dirty/untracked slice; hashes recorded | CURRENT_VERIFIED source: work/artifact/revision/outcome constraints and fields |
| Services/Repositories processing, candidate, field, notification, retention/reporting consumers | Mixed tracked and dirty; snapshot/hash evidence | CURRENT_VERIFIED source inspection; executed checks identified separately |
| `AI/roadguard-ai-engine/src/api/be_worker_*`, worker/Studio tests | AI base above plus dirty paths | Private transport, durable SQLite runner, FFmpeg and local Studio |
| `docs/superpowers/plans/2026-11-19-be-ai-integration-execution.md` | Existing evidence ledger | Prior executed local evidence; not a new hosted or Ubuntu check |
| Knowledge readiness/Studio notes | LOCAL_ONLY external Markdown | Reconciled below; old indexed assertions are HISTORICAL when they conflict |

## Requirement matrix (A02–A08)

Paths below are repository-relative. Field-level expansion follows. “Source” means inspected implementation, not an executed end-to-end claim. UNKNOWN always has a concrete next check.

| ID / task / authority | Producer and API seam | Persistence and atomic guard | Authorized readback / evidence | Gap / disposition |
| --- | --- | --- | --- | --- |
| R01 / A02 / owner operator submission | Upload verification → `SurveyV2PersistenceService.Dataset` → dataset submission intent | Verified project/survey FileIds, source hash/version and scope; submission/outbox atomic; work rechecks source | Work repository SQL tests; prior actual multipart upload harness | CURRENT_VERIFIED in tested setup. Actual production upload verification composition remains F04 |
| R02 / A02,A06 / durable automatic intent | `ai.real.dataset_submitted.v1` → `NextSourceAsync` → prepare/claim | Dataset+video+configuration dispatch key unique; job/current attempt and lease/rowversion; rejected source quarantine separate | Five-video discovery, replay and worker ledger SQL tests | F01 blocks configured retry context; ordinary replay does not prove lost commit response |
| R03 / A03,A05 / six labels, AI proposes | Worker result → `RealAiCandidateFactsReader` → PM candidate decisions | Source class retained; six model mappings; `repair`→`REPAIR_PATCH`; proposals have NOT_MEASURED/null ground geometry | Reader unit tests; prior FT95 result; candidate API tests explicitly mock inference | PM may correct classification under current rules. No owner decision supports a blanket prohibition on PM correction. Repair-specific acceptance needs explicit regression, not an invented rule |
| R04 / A03,A06 / exact accepted attempt | `AiVideoCoordinator` validates raw/frame artifacts then complete | `AiWorkerArtifact` + StoredFile + AIDetection + terminal job/work facts committed together; hash/bytes/current-attempt fence | SQL ledger/outcome and unit tests; prior actual MinIO registration | F01; AI completion lost-ACK injection and restart count/hash proof UNKNOWN, see T02 |
| R05 / A04 / FFmpeg drone reference only | Python ffprobe/ffmpeg → cue association; detection-only fallback | Raw result/telemetry artifact and proposal timestamp retain parse status; no ground measurement | Python runner tests plus explicit real FFmpeg nonzero-clock test | Supported subtitle stream tested; arbitrary MP4 is not guaranteed to carry valid SRT |
| R06 / A04,A05 / assigned operator revision | `AiVideoTelemetryController` → service → telemetry repository; If-Match/Idempotency-Key | Append-only revisions, actor/time/video hash, immutable head and payload fingerprint; transaction execution strategy used here | SQL revision tests; HTTP fake-repository tests for auth/status/body/header contract | API+SQL combined recovery/race test UNKNOWN, F06. Studio annotation is not this API |
| R07 / A04 / owner precedence | External SRT → paired verified SRT/offset → embedded → estimate resolver | Revision/source IDs retained in source version; estimate keeps measured status missing; no new inference identity | Telemetry/reader tests; saved snapshot domain test | SQL race and historical authorized snapshot readback UNKNOWN, F06 |
| R08 / A05 / PM-only decision | `CandidateDecisionService` + repository KEEP_NEW/LINK_EXISTING/REJECT | Current rights/source locks; SourceDecision source version, geometry version, actor/time/reason/classification/link/head/supersession | Candidate API suites use real SQL/auth with mock AI; FIELD SQL current REAL lineage test | Stored historic snapshot exists; public decision DTO does not expose full historic telemetry/geometry facts. F06 |
| R09 / A05 / Crew ground truth, PM review | Explicit PM task assign → Crew accept/start → photo/submission → PM CONFIRM/SUPPLEMENT | Field task assignment, submission revisions, measurements/checklist/unknowns/evidence; durable event/audit/outbox | H3 SQL tests include reassignment, supplement, immutable facts and real post-commit lost ACK; prior Playwright KEEP_NEW/CONFIRM | No automatic dispatch from AI; test geometry/photo/measurements are synthetic, not surveyed accuracy |
| R10 / A07 / durable notification | FIELD Emit → H6 worker → current recipient resolution/delivery | Receipt/fence/dedup, occurrence+notification+delivery+audit+scope committed; retry rechecks rights | H6 dispatch/retry/field SQL suites; source separates outbox insertion and actual delivery | CURRENT_VERIFIED implementation is in-app notification, not proof of SMTP/push/production delivery |
| R11 / A07 / accepted sources only | Offline canonical FIELD; repair eligibility; statistics; dossier export | Durable offline ACK/effects, actual measurement and policy/window/coverage gates; statistics count durable Defect identities | H5, ND01, LD08 SQL suites; source inspection | F03: case/defect dossier reader only supports REPORT primary source. Other export paths remain UNKNOWN, not declared broken |
| R12 / A06 / durable recovery | Private SQLite admission/input/result; BE lease/retry/terminal outcomes | Identity/hash checks, bounded queue, atomic input publication, terminal outcome history and stale-attempt fence | Python recovery/runtime + BE unit/SQL tests | F01 and T02. FIELD lost-ACK test is not evidence of AI completion lost-ACK |
| R13 / A08 / private actual hosted worker | `Program.AddAiRealWorker` opt-in; background tick; private bearer HTTP | SQL retry settings are real-app dependency; disabled path needs no model/token; no mock fallback | Disabled startup HTTP test; source; isolated retry reproduction | F01/F04: no real-app verified upload→hosted REAL ingestion test without manual TickAsync; Ubuntu unverified |
| R14 / A07,A09 / policy41A | Retention inventory/contributors → evaluator | Warranty+5y, hold, log90/export30/backup35; missing basis WAITING_RETENTION_BASIS | Existing retention SQL suite plus current source inspection | F02: REAL artifacts/revision SRT absent from AI contributor linkage. No cleanup executed |
| R15 / A01,A10 / durable knowledge | Audit/Knowledge Markdown with baseline, facts and limits | Local note/README links; no copied secrets/media/GPS | Direct Markdown and link checks; two self-reviews below | CURRENT_VERIFIED Markdown update only. SQLite/FTS not rebuilt; registered roots point elsewhere |

## Durable field matrix

All rows use the owner authority in R01–R15. Configurations are under `RoadGuardSystem.Repositories/Configurations/Worker/`; entities are under `RoadGuardSystem.BusinessObjects/Processing/Worker/` unless stated. Missing independent readback is explicitly recorded rather than inferred from storage.

| Field group / producing action | Entity/column or artifact | Transaction/constraint | Readback / verification / gap | Task |
| --- | --- | --- | --- | --- |
| Operator + project/survey/dataset + source version on submit | Dataset manifest, `AiVideoSource.OwnerId`, work DatasetId/ProjectId/SourceVideoFileId; admission source hash/bytes | Submission intent then prepare rechecks current verified source/scope/owner | Discovery SQL tests; owner provenance follows manifest/source, not a new duplicate actor column | A02 |
| Logical processing identity | Work Id, ProcessingJobId, CurrentAttemptId, LogicalConfigurationIdentity, DispatchKey | Unique dispatch key/job, immutable source IDs/config/CreatedAt, restricted FKs | Work repository replay SQL; F01 prevents default retry runtime | A02,A06 |
| State, scheduler and fencing | State, LeaseOwner/LeaseUntil, NextPollAt, ErrorCode, CreatedAt/UpdatedAt, RowVersion | State/time/terminal lease checks; owner/current-attempt/time fence; rowversion | Ledger unit/SQL, coordinator unit; current lease recovery, not process scheduling proof | A06,A08 |
| Exact admitted input/config/model | Work AdmissionJson; job ManifestJson/ManifestHash/ModelVersionId; attempt number/ID | JSON checks, matching project/dataset/video/job/attempt/model/hash/bytes; released six-map validation | Repository/reader tests and private contract tests; catalog readiness on Ubuntu UNKNOWN | A02,A03,A09 |
| Per-frame facts and six source labels | `AIDetection.PayloadJson`: timestamp/bbox/frameFileId/sourceVideoFileId/sourceClass/workItemId/attemptId/proposalOnly/groundLocationStatus; confidence/type/model columns | Validate bounds, decimal normalization and frame ownership; no automatic Defect | Reader tests + prior raw/SQL sample; 63 proposals ≠ 63 physical defects | A03 |
| Raw/frame/telemetry content identity | AiWorkerArtifact WorkItemId/AttemptId/ArtifactId/Sha256/Bytes/Kind/Width/Height/FileId/RowVersion; private object; StoredFile | Completion transaction registers FileIds+proposals atomically, validates exact attempt/artifact inventory | Actual prior MinIO/SQL artifacts; fresh ledger tests. F02 retention linkage missing | A03,A06,A07 |
| Terminal history | AiVideoAttemptOutcome AttemptId/WorkId/ProcessingJobId/State/ErrorCode/AdmissionHash/OccurredAt; execution detail belongs to ProcessingAttempt | Terminal outcome append-only, AttemptId primary key; current mutable work does not erase old attempt | Outcome SQL tests; lost completion response proof still UNKNOWN | A06 |
| Measured telemetry/parse result | Embedded/paired artifact, intervals, cue times, explicit status/offset/source IDs | Parser coverage and relative timestamp bounds; no fabricated 0,0 or physical quantities | Python/telemetry tests, FFmpeg explicit-clock rerun | A04 |
| Operator revision identity/content | AiVideoTelemetryRevision Id/WorkId/Number/PreviousId/ActorId/CreatedAt/SourceVideoHash/Kind/IntervalsJson/OffsetMs/Note | Current rights/head, serializable append, immutable revision | SQL tests and HTTP fake contract; actual API-to-SQL revision lost-ACK UNKNOWN | A04 |
| Original external SRT and replay key | OriginalSrtObjectKey/Sha256/Bytes; IdempotencyKeyHash/PayloadFingerprint | Private storage, hash/size/parse checks; conflict vs identical replay; append-only source chain | Repository tests; object lacks independent StoredFile linkage for retention, F02 | A04,A07 |
| PM reviewed version and decision | SourceDecisions Source_Version (`r1` encoding includes evidence/telemetry refs), GeometryVersion/actor/time/reason/classification/target; current source head/link | Re-resolve facts under locks, append-only decisions/supersession | Stored snapshot source inspected; unit stale-head test; full historical DTO/read-after-restart UNKNOWN F06 | A05 |
| Crew authority and measured facts | Field task/assignment, GroundTruthMeasurement + submission revision/evidence/checklist/unit/unknown fields | Current assigned Crew, required evidence/readiness, PM explicit review, immutable prior submissions | H3 SQL + prior Playwright real API; FIELD post-commit ACK loss tested | A05 |
| Delivery and consumer facts | Field event/audit/outbox → H6 occurrence/notification/delivery/scope/receipt; offline effects/ACK | Serializable mutation, current recipient rights, dedup/fencing/retry | H6/H5 SQL; no claim of real mobile/SMTP consumer | A07 |
| Retention/consumer scope | FileScope/SurveyFiles/dataset manifests + contributor references | Missing basis fails closed; hold prevents deletion | Mock-only AI contributor omits REAL artifact scope, F02; SURVEY dossier gate F03 | A07,A09 |

## Findings and ordered remediation handoff

These are follow-up tasks, not application edits made by this audit. Codex remains proposed fixer for the assigned slice; schema/contract/rollout changes need their scoped design and verification. No new business authority is inferred.

### F01 — BLOCKER: SQL retry prevents automatic claim

CURRENT_VERIFIED reproduction: `retry-probe.log` contains `REPRO_CONFIRMED`; actual disposable SQL, `UseSqlServer(... EnableRetryOnFailure(3))`, call to `new AiVideoWorkRepository(db).ClaimAsync(...)`. Exception: retry execution strategy does not support user-initiated transactions. No business rows are needed or written. Fixture container disposed.

Sources: `RoadGuardSystem.Repositories/Options/RoadGuardDatabaseOptions.cs:26` (default3), `Extensions/RoadGuardPersistenceExtensions.cs:178–187` (retry enabled), `Implementations/Processing/AiVideoWorkRepository.cs:57,91,104,134` (explicit transactions), `AiVideoCoordinator.cs:38` (claim first), `RoadGuardSystem.API/Workers/AiRealVideoWorker.cs:30–31` (catch/retry). Telemetry repository already wraps transactions in execution strategy; work repository does not.

T01 expected fix/test: reproduce on the real retry configuration, then make each transaction boundary strategy-compatible with idempotent/ambiguous-commit handling. Assert empty claim succeeds, one eligible video ingests through hosted dispatch, retry creates exactly one accepted work/job/attempt and unchanged artifact count/hash. Do not merely disable retry to make the test pass.

### F02 — Integration/persistence gate: REAL retention references incomplete

CURRENT_VERIFIED source: `Implementations/Retention/AiRetentionInventoryContributor.cs:12–34` queries AiManifestFileReference/AiDetectionProvenance joined only to AiMockRun and returns Complete=true. `AiVideoWorkRepository.cs:164–168` registers StoredFile/AiWorkerArtifact without FileScope; external revision SRT retains private object identity without a StoredFile reference. `RetentionInventoryRepository` cannot discover omitted REAL provenance through that contributor. Source video itself is covered by survey/dataset references.

T03: add REAL job/work/artifact and revision-SRT ownership/reference inventory under policy41A. In owned SQL, complete a REAL attempt and append external SRT; assert each raw/frame/telemetry object appears in project inventory with exact source version, missing basis stays WAITING, warranty+5y and hold remain honored. Source-proven omission; no observed deletion, no claim that every evidence file is unprotected.

### F03 — Integration gate: accepted SURVEY dossier adapter

CURRENT_VERIFIED source: `Implementations/Reporting/ReportingRepository.CaseDefect.cs` only resolves REPORT primary source; others produce `defect_source_version_and_geometry_unavailable`. This is the case/defect dossier adapter, not proof every reporting/export route fails.

T04: design compatibility for accepted SURVEY lineage/source geometry and evidence, then test authorized capture/export with KEEP_NEW and LINK_EXISTING; rejected/raw proposals must not appear as accepted measured damage. Do not relabel survey facts as REPORT.

### F04 — Hosted application and production composition gate

UNKNOWN: actual enabled application processes verified public dataset submission without manual coordinator calls. `AiRealVerticalTests` and local `PmCrewHost` manually tick. `Program.cs:41–45` registers UploadVerificationWorker only in Development with MinIO; the harness adds it explicitly. Worker startup does not establish remote model/catalog/FFmpeg readiness. Current checkout has no historical `deploy/server` real-worker package.

T02, after T01: use actual Program/WebApplicationFactory + owned SQL/MinIO, supply settings before `AddAiRealWorker` binds them, pin model/token/private URL/spool/FFmpeg and released six-map catalog. Seed only prerequisite actors/project/survey/geometry/catalog. Upload via HTTP, reach VERIFIED, submit `POST /api/v1/survey-tasks/{taskId}/datasets`; poll without TickAsync. Restart BE/AI against owned durable stores, re-read identity/count/hash; inject failure before commit and loss of successful completion response. Expected one intent/work/job per video/config, one accepted terminal outcome per attempt, no duplicate registered artifacts/decisions/tasks/notifications. Disabled worker starts without token/model; invalid enabled prerequisites fail explicitly. A test design alone is not a passing hosted test.

### F05 — Ubuntu filesystem capacity gate

CURRENT_VERIFIED source risk: coordinator uses `new DriveInfo(Path.GetPathRoot(TempRoot)!)`. On Linux a spool on a separate `/mnt/...` filesystem may be checked against `/` capacity. No actual wrong-volume exhaustion observed.

T05: isolated Linux test with distinct spool mount/free-space limits, then choose actual backing-filesystem capacity measurement. Assert reserve and budget use that volume, reject symlink paths and bounded oversize inputs. CPU is explicitly selected by FT95 runner; installed GPU does not prove CUDA inference.

### F06 — Historical readback/race coverage gate

CURRENT_VERIFIED storage includes reviewed telemetry/source/geometry identities. UNKNOWN: real API+SQL telemetry append racing PM decision, and authorized historical facts read after process restart. Public decision projection exposes less than stored source version; the stale-head unit test is not the full persistence proof.

T06: race on one candidate, assert one accepted coherent reviewed snapshot or stale conflict; append a later revision, restart, and compare saved decision source/geometry/telemetry facts unchanged. Verify former operator/Crew cannot mutate and unauthorized users cannot read private artifacts. Determine required historical read DTO from adopted contract before adding a public response field. PM classification correction remains allowed by existing policy unless owner specifies otherwise.

## Failure timeline (A06)

| Boundary | Durable recovery owner / executed coverage | Remaining exact check |
| --- | --- | --- |
| Admission accepted; input interrupted | AI SQLite identity/state and bounded temporary upload; Python recovery/runtime | Actual simultaneous BE restart + transfer loss UNKNOWN |
| Queue full; running job lost | AI one-running/three-waiting contract, queue/recovery tests; BE leases/current attempt | Hosted real-app retry configuration blocked F01 |
| Completed artifacts; BE registration fails | Immutable result/artifact IDs/hashes, BE completion transaction/attempt fence; unit/repository tests | Inject partial object registration then restart: stable accepted FileIds/counts/hashes, T02 |
| BE commit succeeds; success response lost | FIELD genuine post-commit interceptor test exists | AI completion/telemetry lost-ACK injection remains UNKNOWN; ordinary replay does not substitute |
| New telemetry after PM review | Revision chain + source version snapshot | SQL/API race and restart historic readback T06 |
| Zero detections / invalid media / cancelled / expired attempt | Separate outcome/work discovery, source quarantine and recovery tests | Production scheduling still F01/F04; no conversion of failure into silent empty success |

## Ubuntu rollout prerequisites (A09; not executed)

All actual Ubuntu observations here are UNKNOWN; owner-reported GPU and unused500GB are TARGET_CONFIRMED inventory reports, not observed mounted capacity. No SSH or production mutation was run by this audit.

| Prerequisite | Read-only evidence required before a separate rollout |
| --- | --- |
| Pinned artifacts | Exact BE/AI commits/images, Python lock/runtime, model SHA256, six released catalog mappings, FFmpeg/ffprobe versions and executable paths |
| OS/GPU | `uname -a`, OS release, `nvidia-smi`; actual model device configuration and bounded representative inference; CPU runner is current source behavior |
| Disk/mount | `lsblk -o NAME,SIZE,FSTYPE,MOUNTPOINTS`, `findmnt -T <planned-spool>`, `df -hT`, `df -i`, path owner/mode; no formatting/mounting implied |
| Private connectivity | Internal HTTP bearer secret file metadata only; worker not on Funnel, no SQL credentials in AI; verify port bindings and actual BE→AI access |
| Persistence/catalog | Read-only migration state, reviewed additive SQL/backfill and live catalog mappings; authorized application is separate from review |
| Backup/restore | Verified backup manifest and restore drill into disposable owned destination; evidence of restore readability before any rollout |
| Application composition | T01–T06 resolved/accepted as appropriate; actual upload verifier and hosted worker, catalog/geometry/model readiness; disabled/invalid modes tested |
| Smoke and rollback | Private staged submission→REAL proposal→PM→Crew; disable dispatch/worker if failed, preserve accepted SQL/objects/revisions, no blind migration-down; never expose Studio/private worker |

## Knowledge and review (A10)

Knowledge note: `C:/Users/HoangAnhVu/Desktop/Personal-Knowledge-Base/spaces/RoadGuardSystem-Development/notes/2026-11-23-be-ai-readiness.md`. README navigation already links it and Studio notes. Update records F01–F06, current evidence limits and ordered tasks. Existing planning-only conclusion remains historical context, superseded by this executed assessment. SQLite/FTS was not rebuilt; search roots do not prove this worktree or note is indexed. Read linked Markdown directly. No credentials, coordinates, media or runtime payloads are copied.

Self-review1: checked each R01–R15 against producer, persistence, current rights and readback; separated operator drone reference from Crew measurements and PM decision authority. Do not ban legitimate PM label correction. Source-proven missing retention linkage prevents a ready claim without alleging deletion.

Self-review2: checked baseline/dirty scope and source paths, test ownership and fresh/prior/fake distinctions, hosted/manual separation, consumer/reporting scope, index privacy and rollout boundary. Corrected field inventory to actual artifact WorkItemId/FileId and terminal outcome AdmissionHash/OccurredAt, without inventing metrics/JSON columns. Corrected initial SQL run interpretation: 44 failures were unavailable explicitly configured test SQL, not 44 product defects; rerun results are recorded separately.

Read-only parallel audit sections were supplied by audit_pm_crew, audit_consumers and audit_startup and checked against source. These are audit assistance, not a GitHub/ChatGPT diff review. External GitHub review: NOT_REQUESTED. No merge/commit/deployment completion claim.

## Executed verification

CURRENT_VERIFIED fresh local runs, against this dirty baseline:

| Check | Result and limits | Evidence |
| --- | --- | --- |
| BE focused unit | 182 passed,0 failed,0 skipped; rebuilt current sources | `audit-unit.trx`, `unit.log` |
| SQL work/ledger/telemetry/outcomes + FIELD | First run53 passed/44 failed because explicit inherited test SQL was unreachable; all44 were H3 fixture setup errors. Fail-closed behavior prevented fallback/shared writes. Removed variable only in test process, reran H3 with owned Testcontainers | `audit-sql.trx`, `sql.log` |
| FIELD + notification/offline/repair/statistics/retention | 125 passed,0 failed,0 skipped, including all44 H3 reruns. Combined with first53: **178 distinct SQL tests passed** | `audit-consumers.trx`, `consumers.log` |
| Telemetry/candidate HTTP | First18 passed/1 failed for missing configured Unicode font. Reran that one test with existing licensed DejaVuSans:1 passed. **19 distinct API tests passed**; telemetry persistence is fake, candidate inference is mock, auth/SQL boundaries as described in each fixture | `audit-api.trx`, `audit-api-font-rerun.trx`, corresponding logs |
| Python contract/HTTP/runner/recovery/runtime/Studio | 84 distinct tests: initial82 passed/2 skipped; explicit FFmpeg/ffprobe rerun passed one skipped clock case. **83 distinct passed;1 remaining Windows symlink-privilege skip** | `python.log`, `python-ffmpeg.log` |
| Default SQL retry reproduction | Confirmed expected InvalidOperationException in ClaimAsync on actual owned SQL; probe exit0 means defect reproduced, not fixed | `retry-probe.log`, external probe source |
| Prior actual inference/browser | One-minute63 proposals/60 cues; five uploads repeat same clip; Playwright15 checkpoints using already ingested proposals. Not re-executed by this audit | Prior integration ledger and `D:/RoadGuard-AI-LocalTest/pm-crew/playwright/20261123T091124732529Z/report.json` |

Commands: `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~AiVideo|FullyQualifiedName~AiWorker|FullyQualifiedName~RealAiCandidateFactsReader|FullyQualifiedName~AiRealJobState|FullyQualifiedName~H3Field|FullyQualifiedName~CandidateDecision'`; integration filter `AiVideoWorkRepositoryTests|WorkerLedgerPersistenceTests|AiVideoTelemetryRepositoryTests|AiVideoAttemptOutcomeRepositoryTests|H3FieldInspectionWorkflowSqlTests` with `FullyQualifiedName~` on each term; consumer filter `H3FieldInspectionWorkflowSqlTests|H6NotificationDispatchSqlTests|H6NotificationRetrySqlTests|H6FieldSourceSqlTests|H5OfflineCanonicalRegistrySqlTests|ND01OriginalWindowSqlTests|LD08DefectStatisticsSqlTests|Anh02RetentionPersistenceTests`; API filter `AiVideoTelemetryHttpTests|AiCandidateLifecycleAcceptanceTests|AiCandidateReadBoundaryAcceptanceTests`. TRX/logger/results path is the evidence root; reruns used `--no-build --no-restore` after successful build. No persistent user environment changed.

Python: `D:/RoadGuard-AI-LocalTest/venv/Scripts/python.exe -m unittest tests.test_be_worker_contract tests.test_be_worker_http tests.test_be_worker_runner tests.test_be_worker_recovery tests.test_be_worker_runtime tests.test_test_studio -v`, from AI submodule. Explicit FFmpeg test: `tests.test_be_worker_runner.MediaProcessBoundsTests.test_real_nonzero_video_and_embedded_cues_share_relative_clock`, using existing tools under `C:/Users/HoangAnhVu/AppData/Local/Temp/roadguard-media-tools/ffmpeg-9.0.2-essentials_build/bin/` via process-local ROADGUARD_TEST_FFMPEG/FFPROBE.

Source-preservation check: all19 baseline-hashed worker source files unchanged after execution. BE/AI `git diff --check` passed (existing line-ending warning only). Final local D: free70,178,566,144bytes, above30GiB floor; no repeated full-video pilot. Documentation links/requirement coverage and final Git status comparison are saved in `document-checks.json`/`final-status.txt`. Only assessment/knowledge Markdown was written in the source workspace by this audit.
