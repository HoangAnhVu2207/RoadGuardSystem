# HUY final integration — assigned execution specification

Status: IN_PROGRESS. Authority: owner assignment 06/10/2026 (business 01:13 +07), reproduced below. Its decisions are TARGET_CONFIRMED; completion requires executed evidence.

Initial actual base/HEAD: c14709e058f96d6421ac10960e34d1f2af37c2bc on huy-review. Initial dirty paths: none. Fetched Anh source: 5089c3267dcdf60645ab34f61b58a79e3cbb0cf6. Integration strategy: inspected selective runtime deltas; preserve newer Huy code and migration identities; never import an equivalent migration under a second identity.

Writer: Huy/Codex for all assigned shared files. Current checkout reused (initially clean; one worktree). Process names alone do not establish another writer. Root owns guidance/contracts/Postman/spec, clocks and checkpoints. H0 retention writer completed its reserved runtime/tests/migration. H1 identity writer owns identity runtime/tests and combined additive migration/snapshot; root owns clock domain/config/tests and session persistence admission invariants. H2 audit is read-only until H1 checkpoint. Dirty paths at H1 are only these authorized reservations.

Execution order/dependencies: H0 composition/transport/integration → H1 renewable session and explicit clocks → H2 geometry → H3 FIELD lifecycle → H4 repair obligations/policies → H5 offline/handover → H6 notifications/project closure → H7 reporting/inventory/contracts/operations. Each package includes producing contracts, retention references, current-authority receipt guards, focused verification and two self-review passes before its checkpoint. No deployment or main/Anh branch writes.

H0 technical design: TryAddEnumerable scoped inspection contributor alongside HUY and any inspected Anh contributors; exact method/path matching for GET inspection list and GET notification list/detail, POST notification read (GUID route, normalized trailing slash); bearer header has precedence, middleware mixed actors and CSRF share that same eligibility predicate; explicit problem+json serialization overload. Migration integration is additive after model/SQL-equivalence discovery, with fresh and populated isolated SQL validation.

H1–H7 technical decisions will be recorded before their dependent edits; the reproduced assignment defines acceptance and authorized scope. Missing external compatibility/source facts remain localized, never represented as verified or used to halt independent implementation.

## Execution ledger

- Preflight CURRENT_VERIFIED: clean branch, fetched Anh/Huy tips, active rules/module routes and composition/transport inspected.
- H0 checkpoint 1a442f50dac0d985b944014027a6153f0d7c87e3 committed/pushed: DI/strict cookie routes/problem media fixed. Selective Anh retention runtime integrated with new 20261006015156_H0RetentionIntegration (six tables/three immutable triggers); original Huy migrations unchanged. Snapshot/designer preserve owned evidence Restrict FKs. Latest distinct SQL cases15/15 pass across full15-case run (14 pass/1 corrected fixture failure) and focused1-case rerun.
- H1 checkpoint ae2b28956052978d357fb7ee6c2ded3e629c6c1a committed/pushed; BE implementation and two self-reviews COMPLETE. Clock milestone producers remain assigned to H3–H6.
- H2 checkpoint b9541660575312620b73cd836090f02f5ea90e00 committed/pushed,64files; independent BE implementation VERIFIED, official CRS localized PENDING. H3 design/implementation IN_PROGRESS; H4 isolated foundation preparation (excluded from H2); H5 read-only design audit complete; H6 read-only design audit; H7 runtime NOT_STARTED. H2 stage contained no Repairs paths; initial H3 dirty paths are only authorized untracked Repairs foundation and root spec update.
- Prior 108 passing tests are HISTORICAL, not fresh results.
- H0 self-review pass 1 COMPLETE: inspected authority/receipt/fresh-handler transaction locks; fixed missing fresh retention authority locks and active-role read checks. Inspected migration identity equivalence and FK snapshot preservation. External review not performed.
- H0 self-review pass 2 COMPLETE: verified shared writer/file reservations, selective integration preserving Huy roots/readers/privacy, exact route predicate shared with mixed-actor middleware, and contract/Postman preservation. Found imported request folder lacked its opt-in parent event; restored skipRequest guard. Final diff and shared roots reviewed; build0errors, diffcheck passed; source snapshot changes exactly390 additions/0 deletions; applied migration files unchanged.
- Executed parent checks: composition red 15 cases (7 fail/8 pass); composition green 15/15; media red 17 (2 fail/15 pass); transport green 17/17; cookie+notification isolated SQL HTTP 13/13; integrated API h0-integrated-api.trx 41/41. One media test compile setup failure (missing System.Net.Http.Json using) corrected; not a product regression.
- Postman JSON parse and structural comparison: all original request/folder objects/info/variables retained, seven retention requests appended with opt-in folder guard. Live Postman NOT_VERIFIED.
- Final H0 BE evidence CURRENT_VERIFIED: API42 distinct latest cases, retention units13, SQL15 =70 distinct latest passing identities, zero latest failures/skips. TRX set contains167 executions (157 pass/10 fail including9 intentional transport reds and1 corrected lock-probe fixture failure), not70 separate executions. Prior additional retention attempts are recorded below, not silently included in that TRX aggregate.
- Final checks: API `h0-final-composition-retention-api.trx`20/20 (production scoped inspection evidence reaches actual composite with HUY incomplete preserved); `h0-integrated-api.trx`41/41; unit `h0-retention-unit.trx`13/13; SQL `h0-retention-sql.trx`14/15 then `h0-retention-authority-sql.trx`1/1. `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --verbosity quiet` final0warnings/0errors; `git diff --check` exit0. TRX files under respective test-project TestResults (ignored); logs under local Temp/roadguard-h0-*.
- Additional retention attempt history: missing imported type compile failure (no executed cases), initial model-parity failure, cancelled broad attempt (no fabricated completion count), concurrent test DLL build lock (no executed cases), finalized-model-context fixture error, label-table fixture typo (14-case run13pass/1fail), and lock-timeout probe connection fixture error (15-case run14pass/1fail, corrected focused1pass). Two owned FK Restrict annotations retained in new snapshot/designer resolved parity. Test setup issues were corrected; existing production migrations were not rewritten.
- Migration proof: discovered one shared160000 identity/all existing Huy identities/no duplicate Anh bulk AI/export identities; no pending model; fresh database and populated Huy baseline upgrade preserve project, revoked Web session times/transport, receipt outcome and frozen export hash; source-link/label/AI schema+identity preserved, not a populated full producer-graph proof. Three immutable triggers present; populated downgrade denied and history retained. H7 integrated upgrade remains required.
- Review classification: both Codex self-review passes complete (retention subagent review is internal Codex review, not ChatGPT/external acceptance). External review, live FE/Android/Postman, real provider/deployment/operational SLO/backup restore NOT_VERIFIED. No physical deletion/shared data mutation.

H3 normal checkpoint committed/pushed on `huy-review` at `999e2c3` after the117-check independent BE verification recorded below. Current work continues H4 vertical repair/correction, with disjoint H5 offline and H6 notification preparations; their foundation tests do not establish runtime acceptance. H3 staged64 files and excluded Repairs/Offline/Messaging/Reporting/runbook preparations. No applied migration, unrelated branch or production deployment was changed.

## H1 technical design (authorized target; implementation follows H0 checkpoint)

Newly issued sessions use explicit PersistentRenewable lifecycle; nullable server session/refresh-credential expiry means no routine idle/absolute policy. Legacy rows retain LegacyBounded policy and exact old expiry/revocation, with no mass promotion or resurrection. LastActivityAt is telemetry. Session persists issued role snapshot; renewal/rotation checks current user/role/session, and all password/reset/disable/role revocation queries include unrevoked persistent sessions/tokens. Membership authority remains at each project operation.

Access JWT and Web authentication ticket remain finite. A separately protected HttpOnly Web renewal credential can renew a ticket after its expiry through an explicit CSRF-protected endpoint; it revalidates current authority and never exposes credential material in Web JSON. Browser storage/retention is external verification, not a server lifetime promise. No far-future sentinel dates or sliding-cookie-only implementation.

New refresh rotations accept an operation key; atomic protected successor receipt binds original token hash, transport, actor/session and operation, with a bounded exact-retry window and successor-active check. Same-operation retries return the same successor under current-authority locks; different-operation reuse revokes family. Generic idempotency plaintext JSON must not contain refresh secrets. Keyless legacy behavior stays strict; client single-flight/persist-until-ACK is external integration. Production DI uses TimeProvider.System consistently; tests replace clock explicitly.

Clock foundation uses explicit assigned clock kinds, immutable origin event/time/original due and append-only extension/breach histories; completion and acknowledgment stay separate. Exact expiry uses now >= due. UTC instants, Vietnam calendar digest zone, captured/start/finish/server intake times and uncertain offline provenance remain separate. Extension preserves previous breach; deadline state never grants execution or approves workflow. Producing packages wire their exact milestones before any completion claim.

## H1 review and verification ledger

CURRENT_VERIFIED implementation: new PersistentRenewable sessions and non-expiring server renewal credentials; finite access JWT and twelve-hour Web ticket, separate protected renewal cookie and CSRF renewal endpoint; immutable issued-role/lifecycle and legacy expiry preserved. Protected two-minute same-operation refresh receipt checks original token, transport, current user/role/session and active successor. Changed password/reset/role/disable paths revoke all unrevoked credentials, preserving existing revocation timestamps.

Self-review pass 1: inspected fresh/replayed/recovered rotation locks, cipher/key rotation, cookie mixed identities, malformed/duplicate operation headers and authoritative role issuance. Fixed missing successor/current-authority locks, mixed-cookie precheck, fail-closed corrupt receipt/key handling, purpose-bound protected successor hash, encoded key IDs and invitation issued-role ordering. Default-analyzer API build found CA1854 header double lookup (110 warnings/one error); targeted fix and final build passed (109 analyzer warnings/zero errors, no unrelated warning cleanup). No external review claimed.

Self-review pass 2: inspected legacy migration preservation, write-once lifecycle, SQL check three-valued logic, populated downgrade guard, clock rowversion/rollback/history, production clock DI and Postman existing-object preservation. Fixed nullable acknowledgment check, retained owned evidence Restrict annotations in new designer/snapshot, and production named cookie options clock. No old migration rewrite or legacy promotion. Domain producer milestones for clocks remain H3–H6 work.

Executed evidence so far: authentication units57/57; clock/session invariant units21/21; identity SQL31/31; clock SQL three distinct latest passing cases (initial run2pass/1 fixture assertion failure, corrected focused1pass). Clock test first discovered zero cases against stale assembly, explicitly not a pass. Red history: identity1fail, clock16fail, session-invariant3fail before implementation. Intermediate failures: transient compile during reserved child edits; HTTP mixed-role fixture mutation prohibited; old target expiry assertions/invitation role; empty header omitted by HttpClient; SQL barrier ordering timeouts; model-parity owned-FK omission; clock parent DELETE hit legitimate FK before immutable trigger. Fixtures/targets fixed, latest resolving checks recorded at checkpoint; no skip-based resolution.

Migration 20261006021748_H1PersistentIdentityAndClocks applies additively after H0; SQL31 includes fresh database, populated legacy upgrade preserving expired/revoked rows, forbidden lifecycle promotion, guarded rollback and actual post-commit ACK-loss recovery. Three clock history/origin triggers plus identity lifecycle/credential guards. Model parity reports no pending changes after owned-FK preservation. Actual browser storage/restart, Data Protection key-ring durability, live Android single-flight and hosted/deployment acceptance remain NOT_VERIFIED.

Final command sources: `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --verbosity quiet` exit0 (log Temp/roadguard-h1-final-build-corrected.log); `git -c core.safecrlf=false diff --check` exit0. EF model check uses ROADGUARD_MIGRATION_CONNECTION_STRING pointing to local RoadGuard_H1DesignOnly, then `dotnet ef migrations has-pending-model-changes --project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj --startup-project RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj`: No changes, exit0. This check does not open/mutate the database. Postman JSON/structural check preserved all HEAD objects/info/variables and added nine guarded H1 requests; live execution NOT_VERIFIED.

Final H1 acceptance: 253 distinct latest passing test identities, API133 / unit78 / SQL42; zero latest failures/skips. Identity SQL union39 across h1-identity-sql-final31, h1-final-roles-migration10 (three overlap) and h1-legacy-target-final2 (one new); root clock SQL3 across full run/corrected focused case. Unit57 authentication +21 clock/invariants. API h1-auth-final-reviewed133 plus final-source focused13 (subset, not added acceptance). Actual finite expired signed JWT rejected; expired Web ticket renewed via protected credential under production TimeProvider options. Every result is BE/isolated SQL; no actual phone/browser/provider proof.
## H2 technical design and reservations

H2 core retains immutable legacy UTM versions without relabeling them. New profiles pin explicit source/projection/operation provenance and acceptance state; sampleOnly/CANDIDATE fixtures cannot unlock official publication. Native LINE/ARC models preserve analytic lengths and offsets independently of display tessellation. Partial drafts report readiness and invalidate confirmation after changed facts. Route systems pin MAIN/BRANCH topology and parent version/junction; lengths separate declared, analytic and explicit calibration.

Root reserves new pavement layout/slab/map/publication/impact domain, DTOs, services/repositories/controllers/tests plus spec/summary/contracts/Postman. Geometry writer reserves profiles/native alignment/routes/drafts/segment integration/spatial producer adapters and their tests; one H2 migration/snapshot writer only after all configurations are frozen. No simultaneous shared DI or DbContext edits; named edits delegated at the dependent seam. Identity writer now audits H3 read-only.

H2 current reservation update: identity writer completed read-only H3 audit, then took only NEW pavement repository/interface/service/controller and vertical HTTP/SQL tests. Root retains its models/configs/engines/DTOs, production DI, source-file retention contributor, docs and final reviews. Geometry writer remains sole migration/designer/snapshot writer after both configuration sets freeze; no H3 configurations introduced. H3 actual FIELD lifecycle/geometry package binding remains next checkpoint.

Pavement layout design: decimal equal-strip partition must divide the supplied road width exactly; no uneven final strip. Longitudinal planned slab boundaries remain independent of the 100m segment set. Width transitions generate explicit cells per interval; residual length is explicit. As-built revisions retain original planned identity, custom footprint source and nullable unknown dimensions, with gaps/overlaps visible. Immutable map publication pins route/segment/layout/profile revisions; layer pages use stable IDs, bounded size, native bbox and publication-bound cursors, rejecting mixed versions. Official WGS84/accuracy is unavailable until the localized source gate is satisfied.

H2 partial executed ledger: root layout11 intentional red then11 green; footprints6 intentional red then combined pavement17 green; map7 intentional red then7 green. Extreme decimal ratio guard, null-payload regressions and double/decimal JSON endpoint consistency added; latest coherent root engines28/28 pass (h2-root-engines-reviewed-final.trx). Two intermediate build attempts hit mid-edit geometry helper symbols and executed no tests, not product failures or passes. Default build found CA1725 configuration parameter names, fixed by respective owners. No real CRS/client claim.

H2 self-review pass1 checks analytic/source provenance, exact equal strips, nullable/custom evidence, bounded allocation, native/declared/calibrated station separation, project/profile/SRID/version authority and protected receipts. Fixed numeric conversion endpoint consistency without accepting a real coverage gap, cumulative vertex pre-allocation, null input handling, file-purpose/checksum/capture provenance, current role/membership locks and closed-project historical reads. Native25, root engine28, pavement service6 are separate test groups; final service rerun is pending.

H2 self-review pass2 checks full producer/consumer integration, migration history/SQL backstops/topology cycles, snapshot scope/inventory, public casing/ETag/Location, map pin/filter/cursor mixing and opt-in Postman preservation. Fixed caller-owned transaction reuse in geometry/pavement reads, native producer v2 explicit profile/system/length/calibration/sample readiness, profile-aware DbContext validation, road identity topology cycle checks, receipt lower camel case and query/header hash mismatch. Raw SQL guards and populated legacy preservation execute in isolated databases. Existing Reporter consumer test revealed a stale bounded-session fixture after H1; fixture now expects persistent idle acceptance/NULL expiry and revoked denial, while dedicated H1 tests retain finite ticket and legacy expiry coverage. Actual consumer rerun and final build remain pending. Both passes are Codex self-review, not external acceptance.

Latest focused H2 evidence so far: native/profile/spatial units25/25; root engines28/28; SQL native authority2/2, migration1/1, pavement11/11; pavement HTTP3/3. Initial pavement SQL9 cases had8pass/1 fixture failure when an existing immutable File trigger correctly rejected a corruption setup; corrected isolated corruption diagnostic now preserves/restores that trigger. Initial pavement HTTP3 had2pass/1 fixture failure from reading one response stream twice; corrected3pass. Native/legacy producer API run had2pass/1 existing cookie fixture failure, with resolving run pending. No skipped cases are counted as verified.

Source fixture: tests/fixtures/huy-final-native-source.txt defines a synthetic line200 + quarter-circle radius100, independently checkable analytic expression 200+50*pi. tests/fixtures/huy-final-geometry-sample.json records its SHA-256 source hash, CANDIDATE/sampleOnly, SRID0 explicit local profile and no operation/control/tolerance acceptance. Its 0.01m rendering tolerance is a display parameter only. This fixture is not a VN2000 project profile or survey evidence; server-created profile ID must replace its named placeholder.

## Owner clarification — H2 CRS source (06/10/2026)

TARGET_CONFIRMED owner reply: NO AUTHORITATIVE CRS SOURCE FOUND in checked repo/history/docs/project context. Existing COORDINATES/32648/32649 and WGS84_TRANSFORM_NOT_CONFIGURED are legacy implementation, not an accepted VN2000 project profile. Four proposed GIS reference documents are unresolved/unadopted.

Proceed with full H2 configurable/versioned CrsProfile, provenance, transform seam, analytic LINE/ARC, routes/segments/slabs/map contracts and explicitly labeled sampleOnly/CANDIDATE fixtures. Do not guess province/central meridian/EPSG/Helmert/tolerance/control points or relabel legacy CRS. Self-generated round-trip is not accuracy evidence. Official real-data publication and survey-grade accuracy remain localized PENDING/NOT_VERIFIED until an accepted profile/operation plus independent control points and sourced tolerance exist. This does not block H2 core or H3–H7 independent implementation, reopen R01–R30/deadlines, or promote GIS proposals/defaults to authority.
## Owner clarification — receipt and schedule policy (06/10/2026)

TARGET_CONFIRMED durations/origins remain: Crew supplement48h and Supervisor escalation24h from actual received; weekly Monday09:00 Asia/Ho_Chi_Minh while review remains; extension records actor/reason/new due/history without changing origin or prior overdue. No source found for receipt protocol, per-clock extension/substitute authority, catch-up/skip or reminder recurrence. These dependent activations are PENDING, not inferred from inbox persistence, notification ReadAt/delivery, actor ACK or command idempotency receipt. Supervisor handover authority does not authorize other extensions or grant substitute roles.

PROPOSED only: dedicated intended-actor business ACK with server timestamp/origin binding/dedup and visible awaiting receipt; one aggregated catch-up digest for the latest missed period if review remains. Neither proposal is activated without an owner decision. Implement provenance, immutable history, occurrence identity/idempotency and policy seams independently. Technical retry/backoff stays within the same occurrence and does not create business recurrence/catch-up. Request creation, intake/submission, explicit known review clocks and independent H4–H7 work continue. Do not re-ask R01–R30/durations or claim these pending semantics verified.

## Next-package technical seam and reservations

Final H2 CURRENT_VERIFIED acceptance: 96 distinct latest passing cases within H2 verification (unit59 / SQL14 / API23), zero latest failures/skips. Unit TRX: h2-native-core-unit25, h2-root-engines-reviewed-final28, h2-pavement-service-final6. SQL: h2-native-authority2, h2-native-migration1, h2-pavement-sql-final11. API: h2-integrated-producer-final20 and h2-pavement-http-final-reviewed3. The20 includes17 existing composition regressions, native end-to-end1, actual legacy geometry producer1, actual Reporter candidate/Defect consumer1; these are not new independent counts across H0/H1. Resolving Reporter flow now passes completely after the H1 lifecycle fixture correction. Transaction locks, rollback/lost ACK/current-authority replay, raw scope/cycle guards, immutable histories and fresh/populated legacy migration are BE/isolated SQL verified. Task mutation/narrow Crew package access follows H3, not a claim from impact recording alone.

Final H2 commands: default-analyzer API build exit0,126 warnings/zero errors (Temp/roadguard-h2-final-build.log); EF has-pending-model-changes --no-build reports no changes, exit0 using local design-only connection configuration without database writes. Postman JSON/script syntax and structural comparison preserve all HEAD objects/IDs/info/existing variables and append16 guarded H2 requests; git diff --check exit0. Old applied migrations remain unchanged. Migration20261006030147_H2NativeGeometryAndPavement has8 new typed tables,13 new immutable/scope guards and profile-field protection on existing immutable route history; populated downgrade refuses loss. H4 untracked reserved Repairs files are excluded from H2 checkpoint; their33 intentional domain reds and initial analyzer compile attempt belong to H4, not H2 evidence. External/live GIS/client/Postman/ChatGPT review/deployment/SLO/backup restore remain NOT_VERIFIED.

H3 full vertical writer follows H2 checkpoint: inspections domain/DTO/configuration/runtime/controller/tests, task-scoped file admission, actual FIELD producer/Defect consumer and immediate nullable-safe retention updates. Root alone integrates shared DbContext/DI/migration/contracts/Postman. No H3 schema is included in H2. H4 isolated preparation owns only new Repairs domain/DTO/unit files; these are excluded from H2 checkpoint and do not activate dependent repair flows. Geometry writer audits H5 read-only.

First-start seam pins stable origin ID/hash, project/task/assignment, original actor/source device, location/version, optional repair item/authorization/policy and separate claimed/server-received/verified time provenance. MEASURE_ONLY carries no repair grant; uncertain offline origins remain review evidence until H5 reconciliation proves authority. Reopen/remeasure/reassign/sync cannot change an original authorization window. Immutable submission root/revision/parent and evidence facts preserve pending readiness; valid server intake creates PM review24 immediately, and supplements preserve the original origin/due. H4 consumes these actual FIELD facts within the caller transaction and keeps repair acceptance separate from Defect verification. This is technical design under the assigned scope, not adoption of pending receipt/extension business proposals.

H5 admission seam separates authenticated current caller/role from immutable original actor/device, origin/schema/hash, DIRECT/SYNC/HANDOVER admission and optional scoped grant. Existing actor-partitioned command receipts alone cannot deduplicate an origin imported by another recipient. A project/origin unique persisted identity binds canonical content and effect, evaluated under current authority; typed router invokes the same finite FIELD/repair business core, with one atomic transaction per operation. Server renewal encryption keys never become device-data recovery keys. H6 source inspection confirms the current inbox is recipient-only and unique by source/event rather than occurrence, and the existing outbox consumer is registered without a real dispatcher. H6 must add proven project scope/backfill, occurrence fan-out receipts and registered leasing/dispatch, preserving unresolved/unsupported events instead of claiming no-op delivery. No H5/H6 runtime completion is claimed by this design.

## H3 technical design and writer reservation

H3 evolves FIELD with LifecycleVersion, nullable Survey and actual Survey/Reporter-Defect source discriminator, immutable route/set/layout/slab pins and new operational purposes separate from legacy1/2 verification/research. Existing constructors retain legacy invariants. Named FIELD writer owns the four inspection models/configs, inspection read/query DTO seams, new workflow models/config/repository/service/controller/tests, task-scoped upload/evidence/read guards, actual FIELD producer and Defect consumer, immediate inspection retention null-safety/new references. Narrow Common/Enums.cs delegation appends FIELD purpose3/4/5, Cancelled8 and Area4 without renumbering existing values. Root alone owns shared DbContext/DI/migration/clocks/spec/contracts/Postman. Repairs paths remain H4-only and excluded from H3 checkpoint.

Persist first-start root once per task with project/typed-origin uniqueness independent of importer; retain original actor/device/hash/pins and separate wall/monotonic/boot claims/server intake/server-owned proof. Direct controller constructs trusted online admission; public input cannot assert verified time. Uncertain/backdated/future/reboot claims retain history without execution authority; later source-bound proof preserves original identity/timestamp. Shared typed core accepts caller-owned transaction for H5 reuse.

Submission root and linked immutable revisions retain assignment/start/session, nullable value/location with explicit UNKNOWN reasons, correct unit/dimension and genuine zero. Real pending file uses an FK; capture before upload uses nullable FileId plus required capture origin/checksum/media/purpose declaration. Invalid auth/schema/foreign/private evidence denies; structurally valid incomplete intake immediately persists root/revision/PM24 origin/audit/outbox/receipt atomically. Supplements append and retain original clock/breach. Supplement request is visible AWAITING_RECEIPT with no inferred48h activation.

Task-scoped admission/attach/read/replay locks current actor/project/task/assignment/purpose/version. PM BEFORE reuse decision retains authorized original provenance and exposes only task-redacted Crew facts, never broadens private Reporter access. AFTER must be new actual attempt/checklist capture; timestamp/purpose relabel alone is insufficient. FIELD producer exposes actual readiness/sufficiency/measurement/location/evidence facts; Defect consumer records FIELD method and cannot confirm UNKNOWN/pending/insufficient submissions. Narrow Crew geometry uses actual task pins, not project-wide access/latest map. Cancel/reassign retains performed portion/handover and immutable submissions; retention inventories all new relations and safely handles nullable GPS immediately.

Focused acceptance includes real Reporter noSurvey/UNKNOWN/zero/dimension, purpose and actor/project/evidence denials, incomplete PM24/no reset, origin cross-caller conflicts/time uncertainty, immutable supplements, actual FIELD→Defect consumer, task map/upload/privacy, cancellation/reassignment, locked receipt replay/ACK loss/rollback/concurrency and fresh/populated SQL scope/history/retention. Technical design is authorized; actual verification follows implementation.

Root additionally reserves tests/IntegrationTests/Inspections/H3FieldMigrationTests.cs and the forward H3FieldLifecycleAndIntake migration. Populated upgrade fixture uses actual H2 legacy research session with genuine0mm and WGS84 longitude106/latitude10, protecting exact value/unit/GPS/purpose/legacy UTM profile/history. It is not yet executed. FIELD writer's first six seam reds executed0pass/6intentionalfail; subsequent behavioral/vertical verification pending. H4 isolated foundation latest46/46 domain tests pass after two self-review passes; full H4 persistence/services/authority/repair flows remain pending and all Repairs paths stay outside H3 checkpoint. H6 new domain/DTO/unit foundation preparation is similarly isolated from shared configuration/model/DI and excluded from H3/H4 checkpoint.

### H3 checkpoint evidence and historical attempts

Continuation review: internal read-only peer review found two additional current-authority windows in the FIELD file path: fresh multipart operations reused a historical/read guard after earlier admission, and evidence downloads released storage streams without rechecking after the awaited open. The FIELD writer is separating fresh upload admission from historical receipt access and adding post-open authority/evidence revalidation with disposal on denial. Controlled race verification is IN_PROGRESS; source edits or the peer finding alone are not a passing result. Expanded workflow SQL first executed11 cases (9pass/2fail): one raw-scope assertion expected a later guard instead of the earlier correct session guard, and BEFORE reuse exposed an EF owned-tracking query failure. The assertion and locked AsNoTracking query were corrected; a fresh full rerun remains required. Expanded HTTP first executed2 cases (1pass/1fail) with an incomplete multipart fixture; actual part issuance/current ETag fixture was corrected. Preserve these historical failures separately from the later verified run.

HEAD remains `b9541660575312620b73cd836090f02f5ea90e00` on `huy-review`; dirty H3 sources plus isolated H4/H6 foundation and H7 runbook preparation are preserved. No new checkpoint/production deployment is claimed. Root shared additions are eight DbSets, scoped FIELD repository/service DI, forward migration `20261006035334_H3FieldLifecycleAndIntake`, its source guard partial, model snapshot, migration fixtures, local contract and fourteen guarded Postman requests. H4/H6/runbook paths remain outside the H3 checkpoint.

CURRENT_VERIFIED: root SQL fresh schema and populated H2 legacy upgrade passed2/2, failed0/skipped0 (`TestResults/H3Migration/h3-field-migration-first-green.trx`), command `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore -p:RunAnalyzers=false --filter FullyQualifiedName~H3FieldMigrationTests`. It proves nullable task Survey/measurement value/GPS, canonical project/origin uniqueness, immutable trigger presence, exact genuine0mm/longitude106/latitude10/SRID4326/research purpose2/legacy profileNULL and model parity. Analyzer-disabled compilation is limited evidence; default build remains required. Empty downgrade/forward restoration and populated UNKNOWN downgrade refusal were added after this run and are not yet verified. Earlier migration-test initial compile executed0 tests due nullable legacy assertion; narrow fixture null assertion was corrected. Earlier pre-migration corrected run executed1 intentional missing-migration failure; these failures remain historical.

Forward SQL guards preserve historical purpose1/2 semantics, add lifecycle2 purposes3–5/nullable Survey, write-once task source/mode/geometry pins, immutable eight-table original history, assignment/project/file source scope, start/submission canonical origins and root/parent/revision lineage. SQL scope validation does not replace runtime current authorization. No legacy row is relabeled UNKNOWN or promoted to repair rights. Full actual FIELD producer/consumer, task upload/privacy/geometry, races/replay/rollback and retention checks remain producing acceptance work.

Postman JSON generation preserved every existing HEAD object/info/variable/identifier and appended14 disposable opt-in requests. All18 added scripts passed Node syntax checking. This is tooling verification only; live Postman/client execution remains NOT_VERIFIED. H3 contract section records frozen producing design with IN_PROGRESS status, not full acceptance.

Owner-unlocked H4 correction foundation has executed a new59-case red run:48passed/11expectedfailed/0skipped (`h4-correction-adopted-red.trx`, analyzers disabled). A preceding default compile failed on H3 checksum-culture analyzers and executed0 tests; FIELD source normalization was corrected by its writer. The red foundation does not establish production correction policy acceptance. H4 green source is undergoing focused verification; full correction transaction/adopted current-project authority/reporting/export acceptance follows H3.

Root independent H7 preparation reserves docs/backend/huy-final-release-runbook.md, excluded from H3/H4 checkpoints until final RC source bindings are verified. It records source/configuration/HTTPS/cookie/storage/worker/migration/recovery procedure and preserves50-user metadata p95≤2s/RPO≤15min/RTO≤4h as NOT_VERIFIED without benchmark/restore evidence. It is not a production deployment authorization or an active claim from file presence.

## Final H3 independent BE verification — CURRENT_VERIFIED

Candidate base/HEAD before the normal checkpoint is `huy-review@b9541660575312620b73cd836090f02f5ea90e00`; remote tip was rechecked and matches. H3 runtime/shared schema/contracts/Postman/spec/test paths are authorized changes. Untracked Repairs, Offline, Messaging notification foundation, Reporting inventory preparation and the H7 runbook are preserved outside this checkpoint. No applied migration or Anh/main/develop path is changed.

Latest117 distinct test identities within H3 and its affected regression set pass, failed0/skipped0: Unit39 (H3 domain18/service6/consumer2 plus affected upload13), SQL38 (H3 workflow19 plus migration2; affected legacy P2406 and pavement11), API40 (new FIELD3 plus affected37). TRX sources: `h3-domain-service-consumer-upload-final.trx`, `h3-field-workflow-v2-pair-final.trx`, `h3-root-affected-sql.trx`, `h3-field-http-final-reviewed.trx`, `h3-root-affected-api.trx`. Root executed affected API37 with default compilation and SQL17 with the coherent latest integration DLL; the full H3 SQL21/default analyzers includes the latest migration guards/model parity. Default API build succeeded with zero errors; its incremental zero-warning output does not imply unrelated existing analyzer warnings were removed. Counts are distinct inside this package, not additional identities across H0–H2.

Actual isolated SQL/HTTP evidence covers PM-confirmed Reporter/noSurvey and genuine Survey/AI KeepNew source chains; current task/assignment actor and protected replay/conflict/recovery; known0/UNKNOWN/m²/null GPS and malformed numeric/unit input; immutable first origin/root/supplements and original PM24 intake; actual FIELD producer→Defect consumer; caller-owned transaction and precommit rollback; ACK-loss recovery with revoked-right denial; cross-kind origin races; raw source/actor/purpose/root/session guards; actual pinned impact effects and R15 handover; private BEFORE reuse and current legacy relation-backed reads; null-safe retention and actual V2 validation pairing of known0 versus unknownNULL. Actual multipart completion/presign races deny fresh capability after task completion. Both dedicated FIELD and generic download paths recheck after storage open, dispose on denied authority, and map expected storage failure to503. Storage behavior is mocked; infrastructure/client accuracy is not asserted.

Root self-review pass1 inspected current role/project/task/file authority before admission/replay/recovery, source privacy/provenance, actual source consumers, first origins/intake clocks, handover/impact and atomic effects. Internal peer findings led to fresh multipart guard separation, post-open stream revalidation/disposal, null-safe evidence/reason SQL, legacy/new purpose partition, root/session uniqueness and actor linkage; actual controlled cases verify them. Pass2 inspected old consumer/receipt compatibility, shared migration/model/FK preservation and downgrade refusal, affected nullable consumers, contract/Postman scope and dirty-file reservations. It found the non-FIELD fingerprint regression; a seeded exact prior-shape receipt reproduced409 before the fix, and the restored old serialization shape now replays under current authority. That one compatibility case uses narrow mocked candidate facts with real SQL source locks/receipt/authority; actual FIELD source production/consumption is verified separately. The V2 known0/unknownNULL public consumer regression is real persisted SQL, with fixture-derived/model data and no prediction-accuracy claim.

Historical failed attempts above are retained. Additional diagnostic history includes SQL14(13pass/1invalid fingerprint fixture), SQL17(16pass/1duplicate detection fixture), zero-test compile blocked by unregistered H4 stub analyzer properties, and isolated prior-receipt source fixture failures before legitimate Case route/set pins were supplied. A corrected fingerprint-only red executed1 expected failure (`h3-legacy-receipt-fingerprint-only-red.trx`); final green includes it. The first standalone build used a wrong csproj filename and executed no build/tests, then the correct project build succeeded. No failing/skipped check is counted as verified.

Fresh final tooling check preserves every HEAD Postman object/info/variable/identifier and verifies all20 scripts for16 new guarded requests; the actual reviewed-source Defect consumer request has a separate manual opt-in. Diff/encoding checks follow before staging. Official CRS/GPS-source accuracy remains localized pending; actual receipt protocol remains pending. MEASURE_ONLY grants no repair execution. Repair claims without trusted actual attempt binding remain insufficient; H4 owns that dependent adapter and actual AFTER/review/final/correction acceptance. H5 imported actor/time/grant admission and H6 delivery activation remain assigned. This checkpoint establishes independent FIELD BE behavior, not repair/offline/client/deployment completion.

## Owner clarification — correction authority (HISTORICAL/SUPERSEDED)

HISTORICAL/SUPERSEDED (earlier owner reply): FT final confirmation is PM; normal final confirmation is Supervisor after PM review. Mistaken/insufficient acceptance appends actor/reason/time/supersedes correction and continues the original obligation; it is not a new recurrence Defect. Original history/export snapshots remain immutable; live effective projection/KPI excludes superseded acceptance. Owner found no authoritative decision for correction authority or its re-review/re-confirm procedure; both remain localized PENDING. Same final-role correction, same individual, Supervisor override and audit authority are not inferred. FT PM/normal Supervisor correction is PROPOSED only. Production correction action without adopted authority denies/pends. Model/history/effective projection/authorization hook and clearly sourced test-policy allow/deny, current-project scope, version/supersedes, concurrency, rollback/replay tests proceed; test policy is not owner approval. Independent H4–H7 remains assigned.

The paragraph above records the earlier decision only. The consolidated owner update below supersedes correction-authority/procedure pending; it does not supersede independent lifecycle-command pending.

## Consolidated owner update — effective correction authority

Source: owner Anh through Huy's consolidated update, 06/10/2026. TARGET_CONFIRMED authority is distinct from implementation evidence. Reviewed remote baselines `huy-review@b9541660575312620b73cd836090f02f5ea90e00` and `anh-review@5089c3267dcdf60645ab34f61b58a79e3cbb0cf6` are evidence, not reset targets. H3 dirty work and writer reservations remain preserved.

### 1. CORRECTION — TARGET_CONFIRMED, IMPLEMENT VERTICAL FLOW (owner text)

Authority:
- FT: PM có quyền hiện tại trong project được append correction.
- Normal: Supervisor có quyền hiện tại trong project được điều chỉnh final confirmation.
- Cùng loại final-role, không bắt buộc cùng cá nhân đã xác nhận.
- Người thay thế phải có quyền/phân công hợp lệ hiện tại; không auto-grant role/quyền.
- PM không correct final decision của Supervisor trong normal flow.
- Không suy Supervisor có FT override chỉ từ role Supervisor.

Report/review request:
- Crew và PM có thể báo sai sót/đề nghị xem xét theo current authority.
- Request không thay đổi effective decision hoặc tự tạo correction.
- Không tự thêm một mandatory approval chain chưa được giao để chặn correction đã được cấp quyền. Repair/rework thực tế vẫn theo authority flow tương ứng.

Correction record:
- Append-only bản ghi mới, liên kết original/superseded decision.
- Bắt buộc original decision, reason + evidence/basis, actor + timestamp, corrected result và obligations cần tiếp tục.
- Không sửa/xóa original decision, submission hoặc historical evidence.

Classification:
- Xác nhận nhầm/lần sửa chưa thực sự đạt → correction + tiếp nghĩa vụ Defect cũ.
- Repair thực sự đạt rồi tái phát → linked new Defect, giữ kết quả sửa đúng trước đó.
- Quy tắc recurrence đã confirmed; actor/procedure tạo linked Defect riêng vẫn theo pending ledger bên dưới.

Effects:
- Nếu correction làm mất căn cứ close Defect/package, cập nhật business state/projection/obligation trong cùng transaction.
- Không giữ confirmed/completed khi mandatory obligation còn.
- Effect này đã được owner cấp trong correction flow: không giữ nó pending vì actor của generic closure command chưa chốt.
- Không auto-assign Crew, duplicate repair attempt, reopen Case hoặc auto-extend warranty.
- Repair/rework đi qua flow có authority tương ứng; không phát sinh execution grant từ correction.

KPI/export:
- Current UI/KPI/new export dùng effective corrected decision.
- Original history và published snapshots/exports bất biến.
- Không invent time-bucket/period allocation hoặc đổi denominator.

Security/transaction:
- Current role/project/resource authority cả fresh admission, replay, conflict và recovery.
- Idempotency/concurrency ngăn contradictory correction và duplicate active obligation cùng scope.
- Correction/business state/obligation/projection/audit/receipt và outbox khi applicable phải atomic; reuse current transaction/receipt seams.

Verification bắt buộc:
correct/wrong role; wrong project; revoked rights; valid substitute khác cá nhân;
request không đổi effective decision; replay/current-authority replay;
concurrent correction; rollback; obligation/Defect/package/UI/KPI/new-export effects;
original decision/submission/evidence/published export không bị rewrite.
Test phải chứng minh production policy đã adopted, không chỉ injected allow-policy.

### Effective authority and localized activation ledger

| Evidence | Source | Command / role / scope | Implementation / evidence | Exact pending |
|---|---|---|---|---|
| TARGET_CONFIRMED | Owner Anh through consolidated update 06/10/2026 §1 | FT correction: current project PM; normal correction: current project Supervisor; valid current authority, different individual permitted | CURRENT_VERIFIED production repository SQL15/15 in h4-authority-history-policy-green-report-scope-red.trx; HTTP and remaining H4 producers still IN_PROGRESS | None for correction actor or mandatory approval procedure; substitute appointment/grant remains independently pending |
| TARGET_CONFIRMED | Consolidated owner update §1 | Crew/PM current-authority review request; no effective-decision mutation | H4 request/history flow assigned | No inferred execution grant or automatic assignment |
| TARGET_CONFIRMED | Owner Anh through consolidated update 06/10/2026 §1 | Atomic correction effects on original obligations, Defect/package state, live UI/KPI/new export; immutable originals/snapshots | CURRENT_VERIFIED scoped repository correction/rollback/Resolved effects and actual reporting→DOSSIER SQL admission; persisted old snapshot JSON/hash retained. Client UI/rendered exports NOT_VERIFIED | Public time-bucket/period allocation/denominator interpretation remains pending |
| TARGET_CONFIRMED | Master and update §2 | All R01–R30, persistent login, immutable clock origins/durations, FT predicates/versioned PM policy, lifecycle separation | H0–H2 checkpoints and producing H3–H7 verification recorded separately | No reopened predicates or durations |
| UNKNOWN / PENDING | Update §3 | Real FT eligibility source interpretation | Reuse actual scoped Warranty dates/road scope/documents and project HandoverDocument facts; return source mapping UNKNOWN where absent | Project handover→specific road/route/segment mapping and maintenance coverage adoption; no inference from defaults |
| PENDING / NOT_VERIFIED | Update §3 | Official CRS publication and survey accuracy | Versioned H2 core and sampleOnly CANDIDATE fixtures verified separately | Authoritative profile/operation/tolerance/independent controls |
| PENDING | Update §3 | Actual-received protocol; per-clock extension/execute-right issuance authority; substitute appointment; missed weekly occurrence and business recurrence | Event provenance/history/idempotency/policy seams proceed; technical retry stays same occurrence | No inbox/read/delivery/receipt-as-received assumption; dedicated ACK and latest-period catch-up remain PROPOSED |
| PENDING | Update §3 | Separate Defect close, linked recurrence creation, construction completion, operational closure, obligation transfer grant/accept/eligible receiver | Lifecycle models/invariants/read projections proceed; production mutations denied without adopted authority | Actors/procedures only; does not block §1 correction effects |

H4 acceptance now requires the complete production correction vertical and actual adopted-role tests listed above. H7 consumers must read effective corrected decisions for live KPI and new snapshots while preserving existing published snapshots. Work review findings, technical design, owner decisions and executed checks retain separate evidence labels. Continue H3→H4→H5→H6→H7 without restarting checkpoints, cutting scope, rewriting applied migrations or deploying production.

## Owner clarification — closure, recurrence and lifecycle command authority

TARGET_CONFIRMED behavior remains R18/R19/R26: explicit Defect close only when mandatory obligations are resolved; Case conclusion/publication separate; true successful-repair recurrence creates a new linked Defect while mistaken acceptance continues the old obligation through correction. Construction completion, operational closure and warranty are separate; operational closure requires resolved obligations or receiver-confirmed transfer; new Reporter intake continues. Supervisor renewed handling scope remains CONFIRMED.

Owner found no adopted actor/procedure for explicit Defect-close, linked-recurrence-create, construction-completion, operational-closure or obligation-transfer grant/accept commands; each activation remains localized PENDING. D09 FT/historically Supervisor-file reopening, proposed closeProject/assessRecurrence and Supervisor→PM device data handover do not authorize these new commands. Do not infer Supervisor close, PM recurrence creation, arbitrary member transfer acceptance or dual approval. Current membership is necessary but not command authority. Continue model/state/history, obligation/linkage/provenance/read projections and authorization hooks with provenance-labeled controlled test policies. Production mutation denies/pends without adopted policy; pending/unaccepted transfer cannot satisfy closure and completion cannot auto-close obligations/warranty. No corresponding vertical command acceptance is claimed; independent H4–H7 continues.

## H4–H6 technical continuation — producing contracts before shared edits

Base is H3 checkpoint `999e2c3` on `huy-review`. H4 writer owns new repair domain/DTO/config/repository/service/controller and focused SQL/HTTP tests; root owns shared DbContext/DI/forward migrations/snapshot/local contract/Postman/spec. H5 and H6 writers prepare disjoint new files, with one compile slot and producing dependencies joined before package acceptance. Existing schema-1 FIELD DTO and non-FIELD receipt fingerprints remain exact; separate new envelopes carry repair/offline context.

H4 technical design: typed package/obligation/actual road scope/item/attempt/decision/review-request, policy revision/draft/auth/task binding/execution/intake/safety histories, Restrict source relations and mutable-head rowversions. Lock current actor/membership then actual Defect+obligation-kind anchor before candidates; actual road/location pins come from server sources. Normal Supervisor approval→PM assignment→Crew work→PM review→Supervisor final; FT initial PM execute-right plus pinned configured sourced eligibility→Crew→PM final, without extra post-measure PM permission. MEASURE_ONLY stays non-executable. Trusted bound FIELD capture/formal intake adapters preserve the original formal intake PM clock even when physical claim revision is later. Final acceptance resolves repair obligation/package without activating generic Defect close. Adopted correction atomically appends decision/basis and changes effective head/resolution/package/invalidated Defect closure basis/audit/receipt/outbox; new export admission subsequently captures committed effective facts consistently. Existing exports/history remain immutable. Generic closure/recurrence and unsourced transfer authority stay denied. Foundation91 default-analyzer tests (including four formal-root/physical-revision cases after0/4 red) is preparation, not production-policy acceptance.

H5 frozen local design: new `/api/v2/projects/{projectId}/offline` finite FIELD and subsequently H4 typed operation envelopes retain exact core DTO/hash; outer envelope carries original actor/device/snapshot/dependencies/time evidence. Registered public signing/encryption keys, immutable task snapshots, project/origin kind/core+envelope binding, per-operation attempts/results/batches, exact Supervisor-issued24h scoped handover grants/revocation and signed ciphertext/package references. Each admitted item uses the existing typed core in one caller-owned serializable transaction with canonical origin/effect/receipt/audit/clock/outbox. Current importing authority and exact grant scope are rechecked before receipt/conflict/recovery; original actor identity is retained without impersonation, including revoked original actors under current authorized receiver admission. Both-direction registry/binding guards prevent a second origin namespace. Signed wall/mono/boot claims remain UNCERTAIN; independent existing verified original time may bind but signature does not prove clock accuracy. Server verifies independently registered sender signature and attached plaintext hash/header, without recipient private key/escrow or claiming AEAD decryption. Crypto30 fixtures remain separate from actual phone/runtime acceptance; H5 configs are held until H4 scaffold isolation.

H6 technical design: sidecar current project/source/occurrence/proof scope preserves legacy inbox wire; unknown protected history denies, actual source resolver backfill does not guess. Finite owned-event registry plus filtered fenced worker never leases AI/export work, and atomically records occurrence/recipient effects/receipts/outbox completion. Recognized non-R28 events require explicit audited behavior; unsupported owned events remain failed/pending rather than silently complete. Existing event type/category serialization must remain stable; occurrence dedup uses typed sidecar identities. Retry/backoff stays within the same occurrence. Actual-received, missed-week policy and extension/substitute authority remain localized pending. Root separately reserves lifecycle history/read projection/pending policy hooks; no command actor is inferred from role or project membership.
### Continuation foundation evidence (not vertical acceptance)

H4 latest combined100/100 default-analyzer checks comprises91 behavioral foundation plus9 persistence metadata checks. Nine mapping cases first executed0pass/9expectedfail. Metadata success does not prove SQL materialization, immutable triggers or production HTTP/current-project admission; persisted exact decision/resolution heads and actual roundtrip are being added after root review of ignored/collection-derived heads. Zero-test default compile attempts (root lifecycle constant-instance analyzer and H4 builder API/parameter analyzer issues) were fixed; not counted as tests.

Root lifecycle projection executed10 expected failures then10/10 default-analyzer green (`TestResults/H6Lifecycle/h6-lifecycle-projection-red.trx`, `h6-lifecycle-projection-green.trx`). Construction completion, recorded operational closure and warranty remain separate; only resolved mandatory obligations or controlled exact authorized receiver-confirmed transfers satisfy the invariant, reopening invalidates current closure basis, and new reports remain accepted. Controlled transfer facts do not establish production transfer command authority. Actual source-aware lifecycle persistence/read and confirmed Supervisor renewed-handling command remain required; generic close/completion/transfer activation stays pending.
Additional root technical review found scope decimal storage rounding and unordered/ignored EF head risks. H4 executed6 expected failures then106/106 default green: explicit item EffectiveDecisionId and obligation EffectiveResolutionHeadDecisionId Restrict pins; exact decimal(18,3) representability rejects before overlap comparison. This is technical storage precision, not survey accuracy. Full policy/head hydration, SQL races and production correction/reporting consumers remain required. Root has added only six already mapped repair DbSets; full H4 schema is not yet scaffolded, H5/H6 auto-discovered configs remain held.

Root lifecycle production policy-hook foundation executed13 expected failures then combined23/23 default green (`TestResults/H6Lifecycle/h6-lifecycle-authority-red.trx`, `h6-lifecycle-foundation-green.trx`): generic command authority remains pending even for current Supervisor, undefined commands deny, confirmed renewed-handling role is Supervisor with current actual project authority and verified operational closure source. Legacy ProjectStatus.Closed is not relabeled operational closure. Hook tests do not establish HTTP/current SQL history admission or completeness of an empty inventory. Root self-review1 checked lifecycle/transfer separation and duplicate/invalid identities; self-review2 checked fail-closed actor/action/source policy and immutable original projections. These foundation checks do not open pending command activation.

H5 source grant/signature foundation latest39/39 default green (crypto30 plus9 runtime domain after9 expected red failures): exact Supervisor issue+24h boundary, scope identity and registered signing-key/package/attached-hash checks. New signed batch descriptors bind sorted origin/kind/effect/core/envelope hashes; per-item admitted bodies are retained separately, unauthorized bodies are not persisted. EffectId==OriginId applies only to new offline envelopes so dependency rows can be referenced before connectivity; replay retains actual existing effect identity. Direct old DTO/hash/random IDs remain unchanged. Registered signature integrity is not server AEAD-decryption, execution/time proof or actual-device acceptance.

H6 catalog latest21/21 default green after21 expected red failures validates existing actual H3 shape and finite actions. Root review found numeric enum strings and duplicate semantic JSON properties requiring explicit decoder rejection; FT final Supervisor-information source under R03 is being added. Worker/inbox/current-SQL/HTTP and H4/H5 producer adapters are not yet verified.
H4 latest112/112 default-green foundation includes6 policy/safety mapping cases after6 expected red failures; exact policy draft/publication heads and private List-backed read-only navigations are prepared, but actual reload/transaction remains required. H6 catalog review first executed21pass/3expectedfail then catalog24+foundation27=51/51 default green; exact named source kinds, duplicate semantic JSON rejection and FT final Supervisor-information registration fixed. No worker/SQL/HTTP completion inferred.

H4 core migration foundation CURRENT_VERIFIED (isolated disposable SQL only): actual missing-guard RED1 (expected19, actual0), then GREEN1 installing19 enabled triggers, fresh model parity, populated H3 Project upgrade, empty H4 downgrade and re-up preserving that Project. Core19 tables and seven mutable/twelve append-only guards are a new candidate migration, never applied to shared/production databases. Package shadow MutationRevision was aligned in the new draft migration/designer/snapshot before SQL testing. Populated correction heads/history/reload/rollback and production-policy HTTP acceptance remain under verification; guard existence alone does not establish them. H4 domain foundation latest118/118 includes6 plan/draft-staging/threshold-precision checks after meaningful reds. H5 identity/envelope11/11 default green is separate from39 crypto/grant cases; explicit FIELD_ACCEPT/replayed-dependency RED2 has executed and green source awaits validation. Preserve the initial zero-test compile failure (ambiguous assertion) separately, never count it as a red behavior test.


H5 has a named minimal delegation for existing Files UploadService/UploadPersistenceService FieldAdmission+Recovery/IUploadRepository to add a new signed-capture/import-admission/grant-bound evidence validator. Actual recipient uploader/owner must remain distinct from original actor; direct Crew-only admission/hash stays exact. New blob admission after handover expiry denies; scoped bounded technical upload-session continuation must be designed separately from grant/session/execution permission. Current source/recipient authority and exact capture/hash/task/purpose/FileId are rechecked around storage waits and protected replay/conflict/recovery. New Offline capture-reference table and retention refs are part of H5, not an unavailable-path scope cut.

Read-only GitHub preflight confirms CLI authentication and active workflow `RoadGuard Continuous Integration` (360781604), whose local configuration has workflow_dispatch but push branches exclude `huy-review`. Exact final-SHA hosted run is still required in H7; no CI run/success has yet been claimed.
## Authoritative owner assignment (verbatim)
ONE SELF-CONTAINED FINAL CODEX EXECUTION PROMPT — ROADGUARD
Latest assignment: 06/10/2026, Asia/Ho_Chi_Minh
Implementer: Codex LOCAL của Huy
Repository: HoangAnhVu2207/RoadGuardSystem
Receiving branch: huy-review

Đây là một execution assignment duy nhất cho toàn bộ H0–H7.
Thực hiện preflight → H0 → H1 → H2 → H3 → H4 → H5 → H6 → H7.
Không chia thành prompt API nhỏ, không dừng ở preflight/spec, không chờ prompt mới sau mỗi checkpoint.
Prompt này tự chứa business authority và execution instructions. Không yêu cầu handoff hoặc review attachment khác.

======================================================================
A. AUTHORITY, OWNERSHIP VÀ ACCEPTED BASELINE
======================================================================

1. Latest leader assignment

Leader Anh đã chốt assignment ngày 06/10/2026; business rules/deadlines được chốt lúc 01:13 +07.

Những decisions và assignment được inline trong prompt này là TARGET_CONFIRMED, supersede HUY-01/HUY-02/ANH plans cũ khi có mâu thuẫn.

Không hỏi lại R01–R30, deadlines, persistent-login decision hoặc ownership đã chuyển.
Current source/tests chứng minh implementation, không tự thay business authority.
Technical DTO/API/state/storage/index/backoff/implementation choices trong phạm vi được giao do Huy/Codex thiết kế và ghi vào spec, không hỏi permission từng bước.

2. Accepted work phải preserve

- HUY-01 CLOSED.
- HUY-02 tại c14709e058f96d6421ac10960e34d1f2af37c2bc:
  I1 = APPROVE; N1 = APPROVE; R1 = APPROVE; DOC = APPROVE.
- Implementation base HUY-02:
  5a6d4c3b1957c0142c4d4d83679c11066ca1cdb7.
- Code checkpoint:
  f1b8908c0a5f34eec1503bd27245b44b490d8c40.
- Final reviewed checkpoint:
  c14709e058f96d6421ac10960e34d1f2af37c2bc.
- HUY-02 spec ghi 108 distinct latest passing cases; đây là historical executed evidence, không phải tests vừa chạy trong review này.
- HUY-02-summary.md không tồn tại tại reviewed Huy HEAD; summary/evidence nằm trong HUY-02.md. Không tạo finding vì khác tên file.

Giữ Reporter privacy/intake/evidence/replay guards, KEEP-LINK-REJECT, Case/Defect/publication separation, label approval/readers, identity transport foundation và survey assessment-baseline.

Không reopen accepted corrections nếu không có direct regression mới.
New requirement được leader thay đổi phải ghi là approved target delta, không biến old PASS thành một finding cũ “chưa sửa”.
Tests affected bởi merge/new requirement phải được kiểm lại đúng phạm vi; không blanket-rerun historical PASS hoặc duplicate tests chỉ để tạo việc.

3. Old blockers chỉ còn HISTORICAL

Các trạng thái:
- B1–B6 BLOCKED;
- S1/S2 PENDING_ANH;
- independent Huy-owned correction remaining = 0;
- additional independent Huy run needed = 0;
- NO FURTHER HUY RUN / NO FURTHER HUY CODE SHOULD RUN BEFORE LEADER DECISIONS;
là kết luận đúng của assignment pre-decision cũ, không chặn assignment mới này.

Mapping continuation:
- B1 FIELD → H3.
- B2 normal repair và B3 Fast Track → H4.
- B5 sync và B6 encrypted device handover → H5.
- B4 notification producers/reminders → H6.
- S1/S2 → H0 do Huy thực hiện.
- Interface/facts/inventory integration → các producing packages và H7.

4. Shared writer/coordinator hiện là Huy

Huy nhận:
- DbContext;
- entity mapping/configuration;
- migrations/model snapshot;
- shared DI/composition roots;
- receipt callers/guards và affected shared integration;
- canonical contracts;
- integrated Postman;
- integration phần Anh cần thiết;
- affected CI/fixtures/root guidance trong assignment này.

Một writer/file vẫn bắt buộc.
Ownership transfer không chứng minh writer/process khác đã dừng.
Nếu có conflict writer thực tế, checkpoint/điều phối bảo toàn đúng file, giữ delta và tiếp tục phần độc lập.

======================================================================
B. REVIEWED REPOSITORY FACTS VÀ INTEGRATION RISKS
======================================================================

Các facts sau là source review tại baseline ngày 06/10/2026, không phải local/runtime verification:

- Reviewed live huy-review:
  c14709e058f96d6421ac10960e34d1f2af37c2bc.
- Reviewed live anh-review:
  5089c3267dcdf60645ab34f61b58a79e3cbb0cf6.
- Branches diverged.
- Merge base:
  efc0ca10b53264bb24c807b7352ddba0cbe36b6d.
- Ancestry API: Anh 15 commits riêng, Huy 31 commits riêng.
- Direct tip comparison: 191 differing file paths:
  51 chỉ ở Anh, 55 chỉ ở Huy, 85 ở cả hai nhưng khác Git blob.
- Không suy counts thành missing features/cherry-pick list.
- Local HEAD/dirty/worktrees/process/writers chưa được Work xác minh.

Các risks phải xử lý:

a. Semantic integration
Anh tip không chứa toàn bộ newer Huy Web/session/readers/label/notification corrections.
Huy tip thiếu một số Anh retention/recovery/integration deltas.
Không overwrite accepted Huy code bằng Anh tip hoặc import cả directory.
So path/blob/patch equivalence, ancestry và behavior để chọn phần thực sự cần.

b. Migration identity
20261003160000_AnhHuyDependencyDefectConcurrency.cs khác namespace/attribute/designer arrangement giữa hai nhánh.
Observed Up/Down vẫn tương đương AddColumn Defects.RowVersion và guarded DropColumn.
Đây chưa phải finding conflicting SQL.
Kiểm migration discovery, duplicate IDs, historical applied lineage và snapshot.
Không sửa migration đã applied; dùng additive forward reconciliation nếu cần.

c. Persistent identity
UserSession.IsActiveAt/IsExpiredAt, AuthoritativeSessionValidator, TouchWebSession, cookie và refresh paths đang có idle/expiry checks.
Refresh token expiry bị clamp bởi session.ExpiresAt.
UserSessionConfiguration yêu cầu ExpiresAt.
Chỉ tăng Cookie.ExpireTimeSpan không đủ thực hiện decision mới.

d. FIELD và retention
FieldInspectionTask.Create yêu cầu SurveyId.
DefectVerification session yêu cầu task/survey/inspector.
GroundTruthMeasurement value/location bắt buộc; units hiện mm/cm/m.
Huy02InspectionRetentionContributor chỉ hai purposes/two evidence FKs và truy cập measurement.Location.X/Y.
No-survey/nullable/multi-evidence delta phải cập nhật readers/contributor cùng package, tránh null crash hoặc invisible references.

e. GIS
GeometryEngine hiện chỉ COORDINATES với 32648/32649, WGS84 output null.
GeometryWorkflowService đã yêu cầu Supervisor cho confirm; phải preserve, không invent finding thiếu Supervisor confirm.
GeometryWorkflowPersistenceService hiện gọi receipt overload không truyền receipt access guard; service precheck không chứng minh protected replay authority tại transaction/recovery.
Đây là source risk cần coverage theo R29, không claim reproduced exploit.

f. GIS reference documents
Các tên API-Contract.md, FE-MapLibre-Guide.md, openapi-road-manual.yaml và RoadGuard-VN2000-Manual-Design.md chưa resolve được trong hai reviewed trees hoặc tìm theo tên.
Chúng là proposed references, không phải authority bổ sung mà prompt này phụ thuộc.
Nếu local có, inspect để đối chiếu compatibility; nếu không, implement core theo requirements đã inline và ghi localized reference/compatibility gate.
Không claim đã đọc unseen source; không blanket-block H0–H7; không bỏ H2 khỏi scope.
Official publish dữ liệu thật vẫn cần verified CrsProfile/source/control information.

g. Notification
Notification hiện chưa có ProjectId/occurrence riêng.
Existing consumer receipt (messageId, notification-inbox) chưa là per-recipient fan-out protocol.
Recipient-only historical inbox khác current-project-permission target R29.

h. Correction/KPI
Old Anh reporting requirements giữ immutable first decision/time.
ReportingDefinitions tại reviewed Anh source vẫn trả repair metrics UNAVAILABLE.
Latest R19 yêu cầu live projection/KPI phản ánh effective corrected decision.
Giữ original history/export snapshots nhưng không tiếp tục đếm sai từ old PASS đã superseded.

i. Stale active guidance
Một số .agents rules/module map vẫn ghi Anh shared coordinator.
Áp explicit assignment mới; cập nhật scoped reservations cần thiết, giữ nguyên one-writer/data-safety rules.
Không dùng stale ownership text để dừng assignment.

======================================================================
C. FULL BUSINESS AUTHORITY — R01–R30 = TARGET_CONFIRMED
======================================================================

R01:
Quản lý lỗi/đo/giao sửa/chứng cứ/xác nhận/lịch sử.
Bỏ quản lý vật liệu, bảo dưỡng và tính thời gian mở giao thông.
Không đưa excluded fields/workflows trở lại như điều kiện Fast Track hoặc Done.
Phương án sửa/checklist/chứng cứ vẫn trong scope.

R02:
Sửa thường:
PM đề xuất → Supervisor duyệt cho sửa → PM giao → Crew làm/nộp →
PM kiểm tra → Supervisor xác nhận cuối.

R03:
Fast Track cho đường đã bàn giao, thuộc bảo hành/bảo trì.
PM giao task cho phép đo và tự sửa theo policy.
Crew đủ điều kiện sửa.
PM kiểm tra/xác nhận.
Supervisor nhận thông tin.
Coverage bảo hành/bảo trì là eligibility fact cần chứng minh; không tái tạo maintenance-management module.

R04:
Task chỉ đo tuyệt đối không cấp quyền sửa, kể cả đạt ngưỡng.
Đợt đo nhiều lỗi chờ PM giao sửa sau.

R05:
PM tự cấu hình/công bố policy project:
loại lỗi, số đo bắt buộc, ngưỡng, điều kiện dừng.
Không yêu cầu Supervisor duyệt policy.

R06:
Ngưỡng có đơn vị.
Không tự áp 3mm/1m.
Chưa cấu hình là NOT_CONFIGURED; chỉ đo/chờ sửa.
Không biết phải UNKNOWN, không dùng 0 làm unknown sentinel.

R07:
Mỗi publish policy là version mới, có actor/time.
Task pin policy version lúc giao.
Thay đổi/thu hồi có xử lý task bị ảnh hưởng.
Không overwrite policy version đã pin.

R08:
Quyền tự sửa offline 24 giờ liên tục từ lần đầu Crew xác nhận Bắt đầu đi đo.
Tải trước task/vị trí/quyền-policy.
Reopen/remeasure/sync không reset hạn.

R09:
Hết hạn không bắt đầu sửa mới.
Vẫn giữ/gửi chứng cứ và giữ an toàn.
Conflict giữ PM review, không áp kết quả bằng quyền cũ.

R10:
FIELD target là Defect do PM xác nhận lỗi mới.
Pre-Defect Report/Case inspection cần task type riêng.
Không fake Defect/Survey.
Support legitimate Reporter-source Defect không Survey bằng model thật.
Không claim pre-Defect task type đã supported nếu chưa có assigned contract/implementation.

R11:
Lưu/nộp thiếu GPS/số đo được nhưng chưa đủ kết luận.
Vị trí dùng GPS hoặc mã tấm/lý trình/mốc/ảnh theo checklist.
Chưa đúng nơi không tự sửa.
Arbitrary note không tự thành verified location.

R12:
PM xác nhận số đo/ảnh cũ còn phù hợp trước reuse.
Ảnh Reporter/Drone có thể BEFORE khi đủ quyền/nguồn.
AFTER phải mới theo checklist.
Reuse decision và immutable source provenance phải lưu.

R13:
Crew chọn Đã thực hiện sửa hoặc Chưa thực hiện sửa.
Đã sửa cần ảnh sau/chứng cứ.
Chưa sửa cần lý do.
Nộp → chờ kiểm tra.
Claim repaired không phải accepted repair.

R14:
PM xác nhận FT.
Supervisor xác nhận sửa thường sau PM review.
UI:
Chưa sửa / Đã báo sửa, chờ kiểm tra / Đã xác nhận sửa.
Cho bổ sung/làm lại.

R15:
Chưa làm: hủy/giao lại có lý do.
Đang làm: ghi phần thực hiện và bàn giao.
Đã nộp: bất biến; bổ sung bằng lần nộp/task tiếp.
Không rewrite submitted history.

R16:
Một item active cho cùng Defect + nghĩa vụ + phạm vi.
Report/Case/polygon không nhân việc sửa.
An toàn tạm và sửa chính thức có thể là hai nghĩa vụ.
Không bypass uniqueness bằng đổi scope ID nếu actual scopes overlap.

R17:
Mixed package giữ loại sửa/người duyệt từng item.
Hoàn tất khi mọi nghĩa vụ bắt buộc giải quyết hợp lệ.
Hủy item không xóa nghĩa vụ.

R18:
Đóng Defect bằng quyết định rõ khi không còn nghĩa vụ bắt buộc.
Case kết luận/publication riêng.
Không auto warranty extension hoặc cascade-close.

R19:
Tái phát sau sửa thực sự đạt → Defect mới linked.
Xác nhận nhầm/chưa đạt → correction và tiếp nghĩa vụ cũ.
Lịch sử giữ.
KPI phản ánh correction.

R20:
Thu hồi/khác hiện trường:
- chưa sửa: chặn/re-evaluate;
- đang sửa: dừng phần ngoài phạm vi/giữ an toàn;
- đã sửa: giữ execution event/review.
Offline biết revoke khi nhận update.
Không tuyên bố thu hồi tức thì trên thiết bị mất mạng.

R21:
Tách bàn giao thiết bị khỏi nhận dữ liệu.
Grant giới hạn project/device-source/action.
Recipient có current rights.
Giữ actor gốc.
Import dedup.
Admission không phải acceptance.
Không chuyển account/session/execute authority qua device handover.

R22:
Task giữ location version đã giao.
Sửa sai ảnh hưởng thao tác phải đánh dấu affected task.
PM xác minh nơi rồi continue/stop/reassign.
Không chuyển task cũ ngầm.

R23:
Hồ sơ và model length riêng.
Default segment theo geometry.
Official calibrated chainage chỉ khi có nguồn/PM chọn.
Không stretch tọa độ/tấm để ép length.

R24:
Project nhiều road/hệ tuyến.
MAIN/BRANCH theo hệ tuyến.
Mỗi tuyến có length/chainage/version riêng.
Project total không cộng trùng phần đường dùng chung.

R25:
Một Defect nhiều location refs.
Không nhân nghĩa vụ.
Khối lượng theo phạm vi sửa.
Project defect count distinct.
Segment allocation phải explicit.

R26:
Tách kết thúc thi công, đóng vận hành, bảo hành.
Đóng vận hành chỉ khi nghĩa vụ giải quyết hoặc bên nhận chuyển giao xác nhận.
Report mới vẫn intake.
Supervisor quyết định phạm vi xử lý lại.
Không cascade-close records.

R27:
An toàn tạm có responsible actor, lịch kiểm tra,
điều kiện thay thế/tháo bỏ và link sửa chính thức.
Lắp xong chưa hết theo dõi.
Đổi phụ trách phải bàn giao.

R28:
Giao task → Crew.
Nộp → PM.
Bổ sung/rework → Crew.
Cần duyệt/quá hạn/safety → Supervisor/người phụ trách.
Notification không chuyển business state.

R29:
Current permission cho read/write.
Mất membership chặn nội dung project.
Lịch sử còn cho người có quyền kiểm tra.
Receipt/notification cũ không cấp quyền mới.
Áp dụng cả protected successful replay và conflict/recovery paths.

R30:
Gia hạn có actor/reason/new due/history.
Quá hạn trước giữ.
Có người thay thế khi nghỉ/mất quyền.
Gia hạn quyền sửa là cấp phép riêng, không suy từ review/sync deadline extension.

======================================================================
D. PERSISTENT-LOGIN DECISION = TARGET_CONFIRMED
======================================================================

- Không ordinary idle/absolute session timeout ép user login lại.
- Cookie/refresh renewable theo thiết kế.
- Access token kỹ thuật hữu hạn, không perpetual JWT.
- Revoke/logout/password change/reset/account disable vẫn chặn.
- Giữ current role/membership authorization và refresh reuse protection.
- OTP giữ policy hiện hữu.
- Không chỉ tăng cookie expiry hoặc đặt ngày hết hạn cực lớn.
- Sửa đồng bộ issuer/renew/refresh/validator/touch/schema/client contract.
- Không hồi sinh historical expired/revoked credentials/session.
- Explicit migration/transition strategy cho legacy sessions.
- Login persistence, offline execute right, sync deadline và handover grant là bốn cơ chế khác nhau.
- Token expiry hoặc renewal/network failure không xóa local offline queue.

======================================================================
E. FULL DEADLINE/CLOCK AUTHORITY = TARGET_CONFIRMED
======================================================================

Clock                               | Duration/schedule                 | Origin
FT offline execution authorization  | 24h liên tục                      | First Crew Bắt đầu đi đo
Data handover grant                 | 24h liên tục                      | Supervisor cấp grant
Result sync                         | 24h liên tục                      | Crew kết thúc công việc
PM FIELD/FT review                  | 24h liên tục                      | Server nhận bản nộp, kể cả incomplete
Supervisor normal approval          | 48h liên tục                      | PM gửi hồ sơ bước approval
Supervisor normal final confirmation| 48h liên tục                      | PM gửi hồ sơ bước final confirmation
Crew supplement                     | 48h liên tục                      | Crew nhận yêu cầu
Supervisor escalation handling      | 24h liên tục                      | Nhận sự kiện chuyển cấp
Danger acknowledgement              | 1h liên tục                       | Server nhận cảnh báo
First temporary-safety inspection    | ≤24h, có thể sớm hơn              | Hoàn thành biện pháp
Weekly digest                       | Monday 09:00 Asia/Ho_Chi_Minh      | Khi còn việc chờ review

Clock semantics:
1. Lưu UTC; weekly calendar lưu timezone.
2. Các hạn giờ là elapsed time, không business days.
3. Mỗi clock có origin event ID/time, original due,
   extensions, completed/acknowledged time và overdue history riêng.
4. Origin write-once; duplicate/concurrency/retry/replay/worker restart không tạo origin mới.
5. Supplement clock riêng; không xóa review breach trước.
6. Gia hạn deadline giữ lịch sử, không reset origin.
7. Deadline không auto-approval/confirmation/Defect close.
8. Ghi captured/started/finished/serverReceived times riêng.
9. Late sync không xóa sự kiện sửa đã bắt đầu hợp lệ.
10. Client backdate không cấp execution authority.
11. First-start offline được phép; không ép Start phải online.
12. Reopen/remeasure/sync/reinstall hoặc reassignment không tự cấp lại 24h.
13. New execute authorization có ID/actor/reason/scope/validity riêng, giữ grant cũ.
14. Nếu không chứng minh được clock/time provenance:
    giữ evidence cho review, không tự kết luận grant hợp lệ hoặc tạo lại window.
15. “Received” của supplement/escalation khác notification creation/read.
    Exact received milestone là dependent interpretation được quản lý ở mục K.
16. Freeze và test boundary convention nhất quán cho new authorization/admission,
    gồm trước, đúng và sau expiry; không thay duration đã chốt.

======================================================================
F. EXECUTION, GIT VÀ DOCUMENTATION RULES
======================================================================

Work only on receiving branch huy-review.
Inspect actual HEAD/remote/dirty/worktrees/visible writers/process trước mutation.
Reviewed SHAs chỉ là baseline, không reset target.

Không:
- stash/reset/discard pre-existing changes;
- amend/force-push;
- sửa/push anh-review/main/develop;
- overwrite whole branch/directory;
- ours/theirs wholesale;
- sửa migration đã applied;
- mutate shared/deployed DB vì một task map;
- commit secrets/credentials/personal connection configuration.

Có thể:
- fetch/read Anh source;
- reviewed merge hoặc integrate selected necessary commits/patches vào huy-review;
- tạo separate worktree bảo toàn dirty/writer conflicts;
- normal commit/push huy-review sau focused validation/checkpoint.

Một writer/file.
Nếu phát hiện active writer conflict, giữ delta và coordinate đúng checkpoint/file;
không dùng quyền ownership mới để overwrite writer đang chạy.
Không tự gửi message bên ngoài nếu chưa có authorization.

Đọc repository AGENTS.md, manifest, evidence/delivery/safety/review-and-coordination rules,
relevant module routes và development spec template để inspect source/conventions.
Explicit latest assignment trong prompt này supersede stale ownership/plan khi conflict.

Maintain:
- planning/development/HUY-FINAL-INTEGRATION.md
- planning/development/HUY-FINAL-INTEGRATION-summary.md

HUY-02.md chỉ thêm supersession/banner/link/source attribution;
giữ historical evidence/failures, không rewrite closure history.

New spec phải persist:
- authority snapshot R01–R30/clocks/login/ownership;
- actual initial base và integrated source SHA;
- allowed files/symbols, exact contract/action/state/SQL effects;
- H0–H7 dependency/acceptance/status;
- proposed/unknown dependent interpretations;
- checkpoints, tests, failures/not-run, resume instructions.

Summary ngắn nhưng đủ resume:
current H/substep, completed checkpoints/SHAs, current HEAD/dirty,
frozen contracts/migrations, failed/incomplete checks, next exact action,
pending capability/external gate và affected evidence.

Mỗi H thực hiện:
inspect → freeze exact contract → implement complete vertical flow →
self-review 1 → fix → self-review 2 → fix →
focused validation → git diff check → normal commit/push checkpoint → continue.

Không coi “đã viết spec” là hoàn tất execution.
Không RF report, ZIP, coordination document riêng hoặc H8 tự mở.
Không bắt buộc subagents; nếu có executing-plans skill thật thì dùng phù hợp.

Evidence labels:
TARGET_CONFIRMED, CURRENT_VERIFIED, PROPOSED, UNKNOWN, HISTORICAL.
Runtime status ghi thêm PASS/FAIL/SKIPPED/NOT_RUN/NOT_VERIFIED đúng evidence.
Self-review không gọi peer/external review.
Source presence không phải executed PASS.

======================================================================
G. COMMON CONTRACT, SQL, TRANSACTION VÀ SECURITY REQUIREMENTS
======================================================================

Architecture:
Controller → Service → Repository.
Theo ASP.NET Core C#/EF Core/SQL Server/MinIO-S3 versions đang có.
Không package/framework upgrade, unrelated refactor hoặc duplicate domain/framework.

Trước code từng package freeze:
- method/path/version/DTO;
- headers, status/error codes, ETag;
- actor/current role/project/assignment permissions;
- action/state transitions;
- SQL/audit/outbox/receipt effects;
- idempotency scope/fingerprint/replay behavior;
- rowversion/concurrency;
- migration/backfill/compatibility;
- producer/consumer interface version;
- cookie/bearer/CSRF;
- tests/acceptance và owner paths.

Required capabilities có API/contract:
policy draft/publish/revoke;
task create/assign/start/download/cancel/reassign;
submission incomplete/supplement/review;
repair propose/approve/assign/start/submit/PM-review/final-confirm/correct;
Defect close/recurrence-link;
safety record/ack/check/transfer/remove;
sync intake/result/status;
handover grant/revoke/export/import/status;
deadline acknowledge/extend;
project close/accepted transfer trong authority đã freeze.

Reuse routes nếu đủ semantics; không cần một endpoint mỗi dòng nếu composite atomic command phù hợp.
Tên route/module đề xuất phải freeze trước implementation, không gọi existing/adopted khi chưa có.

Read contracts phải thể hiện:
task mode, allowed actions/reasons;
policy/location/assignment versions;
firstStart/authorization expiry;
claimed execution vs effective confirmed state;
evidence readiness;
review deadline/overdue/extensions;
outstanding obligations/correction links.

SQL:
- FKs và same-project/source relations;
- rowversion/conditional update;
- active Defect+obligation+actual-scope uniqueness, gồm overlap races;
- append-only policy/location/submission/decision/correction history;
- immutable snapshot/source refs;
- verified file facts do server xác nhận: state/hash/version/MIME/int64 bytes;
- origin/result intake/audit/receipt/outbox atomic khi applicable;
- scoped DbContext/caller transaction cho producer/consumer/inventory.

Reuse shared receipt guard trên ordinary/retry/duplicate/recovery paths.
Current protected authority kiểm trước trả success/conflict/receipt payload.
Replay không rerun business handler.
Không mở transaction ngoài EF execution strategy.
Recovery dispose old transaction theo existing verified seam.
Denial không làm lộ protected receipt/payload hoặc cấp authority mới.

Migrations:
Audit legacy rows and known migration histories.
Preserve applied migrations.
Additive forward changes, explicit backfill/null/discriminator strategy,
fresh DB và populated-baseline upgrade tests.
Unknown source/scope không đoán thành confirmed relation.
Không fake Survey/Defect/GPS/value.

SQL verification dùng disposable isolation/Testcontainers và production migrations/mapping.
Không InMemory/SQLite/EnsureCreated thay production SQL proof.
Không reset/migrate shared/deployed DB.
Authorization cũ cho RoadGuardPostmanTest chỉ đúng target được cấp;
không áp sang DB Huy hoặc môi trường khác.

Cookie/bearer:
Authorization-header precedence;
invalid bearer không fallback cookie;
mixed actors bị reject;
unsafe cookie commands cần CSRF;
current revoke/user/role/membership checks.
Bind exact new routes khi route package được implemented, không broad substring allowlist.

======================================================================
H. DEPENDENCY MAP VÀ CONTINUATION
======================================================================

Default order: H0 → H1 → H2 → H3 → H4 → H5 → H6 → H7.

- H0 integrated roots/model/security baseline precedes all.
- H1 persistent identity/clock foundation precedes final new-command time/security.
- H2 GIS core cần H0; final persistent-session HTTP integration dùng H1.
  GIS math/fixtures không phải chờ renewal implementation độc lập.
- H3 cần H1 và versioned location/source seam.
  Core no-survey/UNKNOWN/incomplete có thể dùng valid existing pinned geometry
  khi H2 reference gate pending; final GIS-backed path dùng H2.
- H4 cần H3 task/evidence và H2 location/coverage facts, H1 clocks.
- H5 cần H1–H4 services/versions.
  Freeze wire/first-start/grant contract sớm cùng H3/H4 để không tạo circular dependency.
- H6 dùng H1 clocks và actual H3/H4/H5 origin events/obligations.
  Producing packages emit source/outbox origins ngay; không đợi dispatcher mới intake.
- H7 inventory/canonical/Postman/tests cập nhật ngay trong mỗi H.
  Final integrated acceptance sau ready H0–H6.

Localized missing interpretation/source/external evidence:
hold đúng dependent capability, continue independent work.
Pending promised scope vẫn ở ledger, không tự xóa/cắt scope hoặc đánh DONE.
Không quay lại D1–D4 blanket BLOCKED.

======================================================================
I. FULL H0–H7 ASSIGNMENT
======================================================================

H0 — BASELINE INTEGRATION, OWNERSHIP, S1/S2
---------------------------------------

Dependency:
Actual local preflight và latest assignment; không cần leader chốt lại business rules.

Reusable modules:
RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs
RoadGuardSystem.Repositories/Extensions/Huy01ReporterPersistenceExtensions.cs
RoadGuardSystem.Repositories/Implementations/Retention/Huy02InspectionRetentionContributor.cs
RoadGuardSystem.API/Authentication/WebCookieConfiguration.cs
RoadGuardSystem.API/Authentication/JwtBearerConfiguration.cs
Existing DbContext/configurations/migrations/snapshot/receipt engine,
Anh multipart recovery/reporting/export/retention roots và current Huy guards/readers.

New delta:
Semantic integration của phần Anh cần thiết; shared ownership update;
actual production contributor composition và cookie/problem-media binding.

Authorization/state:
Preserve Huy auth transports/Reporter privacy/replay/label/notification corrections.
S2 bind:
GET /api/v1/me/inspection-tasks
GET /api/v1/notifications
GET /api/v1/notifications/{guid}
POST /api/v1/notifications/{guid}/read
Kiểm method/path/GUID/trailing slash; giữ bearer/CSRF/mixed-actor rules.
JwtBearerConfiguration.WriteChallengeAsync trả application/problem+json qua actual HTTP,
giữ error codes/correlation conventions.

SQL/migration/transaction/idempotency:
Inspect necessary deltas bằng paths/blobs/patch equivalence/semantic behavior.
Không auto-import theo 15/31 commit count.
Kiểm same migration ID discovery/history/designer/snapshot;
không retain duplicate declarations hoặc sửa applied migration.
Actual production AddRoadGuardPersistence composition phải register scoped
HUY02_INSPECTION contributor exactly once,
giữ HUY và contributors khác, giữ incomplete repair signal tới khi inventory thật đủ.
Không nhầm manual composite harness với production DI.

Acceptance/tests:
Integrated build/model parity/no pending model changes.
Fresh và populated-baseline migration checks theo integration delta.
Actual production root resolves expected contributors exactly once.
Focused cookie/bearer/CSRF/current-session/problem+json HTTP.
Affected old caller/replay/privacy compatibility checks; không blanket-rerun.

Checkpoint/STOP:
Persist integrated SHA/source selection/discovery/model/DI/HTTP evidence.
Hai self-reviews, fixes, focused reruns, git diff check, normal commit/push H0.
Nếu dirty/writer conflict, hold đúng file và bảo toàn; không reset.
Khi H0 ready, tiếp tục H1.

H1 — PERSISTENT IDENTITY VÀ CLOCK FOUNDATION
------------------------------------------

Dependency:
H0 integrated baseline.

Reusable modules:
RoadGuardSystem.BusinessObjects/Identity/UserSession.cs
RoadGuardSystem.Services/Implementations/Authentication/AuthoritativeSessionValidator.cs
RoadGuardSystem.API/Authentication/WebCookieConfiguration.cs
WebCookieActivityFilter, WebCookieRequestMiddleware, WebAuthController,
AuthService/AccessTokenFactory,
IdentityRepository.SessionIssuance/WebSessions/RefreshTokens/Reads,
UserSessionConfiguration và current session/refresh contracts/tests.

New delta:
Coherent persistent session/cookie/refresh lifecycle;
domain clock/extension/origin foundation cho H3–H6.
Không generic workflow engine.

Authorization/state:
No ordinary idle/absolute forced login.
Finite access token, renewable cookie/refresh.
Revoke/logout/password/reset/disable/current role checks và refresh-reuse protection retained.
Không resurrect historical expired/revoked sessions.
Session expiry semantics, renewal credential và legacy transition được thiết kế đồng bộ.
LastActivity telemetry không tự thành removed idle authorization timeout.
Offline execute/grant deadlines không mở rộng bởi login renewal.

SQL/migration/transaction/idempotency:
Explicit schema/legacy-session policy migration/audit strategy.
Issue/renew/touch/refresh/validate nhất quán, không hidden clamp bởi old absolute session end.
Concurrent renewal/refresh/reuse/revoke atomic theo existing execution strategy.
No retry resurrection hoặc duplicate credential effects.
Clock origins immutable, extensions append/history riêng;
production audit time không fake bởi test clock.

Acceptance/tests:
New sessions qua old 30m/12h/30-day boundaries không ordinary forced login.
Actual HTTP/SQL revoke/logout/password/reset/disable/role-change.
Renewal race, refresh reuse, duplicate ACK/recovery.
Finite access-token expiry và renewal contract.
OTP policy unchanged; offline queue not deleted.
Controllable clock tests; actual client persistent storage riêng external gate.

Checkpoint/STOP:
Record designed session/transition/clock contracts và executed cases.
Commit/push H1 sau hai reviews/focused validation.
Missing actual client storage evidence không chặn BE completion hoặc H2;
không claim mobile persistence verified.

H2 — VN2000/GIS/ROUTES/SEGMENTS/SLABS/MAP
---------------------------------------

Dependency:
H0 roots/model/receipt.
Final session HTTP uses H1.
Real CRS/source và unseen reference compatibility là localized gates.

Reusable modules:
RoadGuardSystem.Services/Implementations/Projects/GeometryEngine.cs
RoadGuardSystem.Services/Implementations/Projects/GeometryWorkflowService.cs
RoadGuardSystem.Repositories/Implementations/Projects/GeometryWorkflowPersistenceService.cs
GeometryWorkflowController/GeometryWorkflowDtos,
Projects/RoadSection/RoadSectionVersion/RoadSegment/RoadSegmentSet models/configuration,
IAnhHuyProducerService.ResolveGeometryAsync và current geometry tests.

New delta:
Typed analytic VN2000 source alignment/CRS/profile/layout;
multi-road/route-system/branch/version;
stepwise draft/readiness;
slabs/map layers/manifests/pagination/impact handling.
Reuse road domain, không tạo parallel road framework.

Authorization/state:
Supervisor confirm → PM publish.
Supervisor confirm đã tồn tại: preserve, không rewrite để tạo việc.
Current project permissions cho read/write/replay.
Crew geometry access qua assigned-task adapter, không blanket Crew project geometry.
Draft partial chưa đủ config được lưu với readiness/error/UNKNOWN đúng.
Confirm pins revision; later affected edits invalidate đúng scope.
Publish immutable snapshot; partial publish không claim missing layers complete.
Wrong-coordinate update marks affected tasks; PM verify/continue/stop/reassign.
Legacy tasks/datasets/Defects giữ location/version đã pin.

Geometry/contract requirements:
- VN2000 E/N analytic LINE/ARC, canonical length analytic.
- Derived spatial query geometry cùng source/engineering CRS.
- WGS84 GeoJSON cho MapLibre.
- Pin CrsProfile revision, transform operation, source và accuracy.
- Không relabel existing UTM/4326 thành VN2000.
- Không đo canonical length bằng chord rendering.
- Declared/model length riêng; official chainage calibration sourced/PM selected,
  không stretch tọa độ/slabs.
- Multiple route systems, MAIN/BRANCH trong system.
- Route-specific length/chainage/version.
- Branch đúng junction/parent version/same project/no cycle; không auto-nearest.
- Default segments 100m geometric; PM boundaries/remainder.
- STRICT_EQUAL_STRIPS: 12/4=3; 12/6=2; 12/5 invalid.
- Explicit strip nguồn rõ, longitudinal residual policy documented,
  custom transition footprints; không auto rotate/ceil/cut ở segment boundary.
- Planned khác as-built.
- Validate offsets/corners/curve radii/gaps/overlaps.
- Manifest/count/feature kind/stable IDs/nullable custom dimensions.
- Bbox/cursor pin project/route/version/set scope.
- AMBIGUOUS/OUTSIDE không tự attach.
- Map client contract reject stale mixed-version batch; không setData stale batches.
- Missing metadata/cursor fail rõ.
- Sample data labeled sampleOnly/CANDIDATE.

SQL/migration/transaction/idempotency:
Additive model/source/profile/history constraints; legacy data retained.
Confirm/publication/version changes atomic cùng audit/receipt/outbox khi applicable.
Existing geometry receipt call phải current authority guard
trên ordinary/retry/duplicate/recovery; pre-service check không đủ.
Snapshot/branch references pin versions, stale rowversion rejected.
No official real-data publication bằng guessed CRS/province/control profile.

Acceptance/tests:
Analytic LINE/ARC known calculations; independent known-control transform fixtures.
Không chỉ self-generated round-trip để claim accuracy.
Legacy reading/no relabel, partial draft/readiness.
Wrong actor/project, stale preview/confirm/publish.
12/5 invalid, curved transition/layout validation.
Branch update không move old task; version/impact actions.
Manifest/pagination/partial layers/stale batch contracts.
Actual HTTP/SQL; browser evidence chỉ khi actual client available.

Checkpoint/STOP:
Freeze/adopt exact BE contract và Postman, preserve existing identifiers.
Nếu local có proposed GIS reference documents, inspect compatibility;
nếu không, record exact unavailable reference, không yêu cầu attachment để hiểu scope.
Continue implemented GIS core/fixtures; keep reference-dependent compatibility/real CRS publication gate.
Commit/push H2; tiếp tục H3, không claim official/browser acceptance khi chưa có.

H3 — FIELD/NO-SURVEY/UNKNOWN/INCOMPLETE/TYPED EVIDENCE
---------------------------------------------------

Dependency:
H0/H1, versioned location/source seam H2 cho final GIS-backed flow.
Core có thể reuse valid legacy pinned geometry.
Freeze first-start/task/authorization wire với H4/H5 trước incompatible design.

Reusable modules:
RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs
RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionSession.cs
RoadGuardSystem.BusinessObjects/Inspections/GroundTruthMeasurement.cs
Existing inspection assignment/read service/repository/controller,
corresponding configuration/SQL triggers,
DefectWorkflowService.VerifyAsync,
UploadService/IUploadRepository,
IAnhHuyProducerService,
Huy02InspectionRetentionContributor và current inspection/retention tests.

New delta:
Complete task lifecycle/mutations,
no-survey source discriminator,
purpose/state rules pre/post/verification,
UNKNOWN value/location và unit dimensions,
immutable incomplete submission/supplement/review,
task-specific evidence producer/consumer,
real FIELD result.

Authorization/state:
PM create/assign; Crew start/capture/submit; PM review/supplement.
MEASURE_ONLY khác conditional repair authority.
FIELD actual PM-confirmed Defect; Reporter/no-survey supported bằng relation thật.
Không fake Defect/Survey; separate pre-Defect inspection type không tự claim implemented.
Cancel/reassign/in-progress/immutable submission theo R15.
Unknown data được capture/submit nhưng không đủ conclusion/execute.
GPS hoặc checklist position proof, arbitrary note không location verification.
PM recorded reuse decision cho BEFORE, authorized immutable Reporter/Drone provenance.
Crew chỉ thấy task-authorized content, không private Reporter identity/evidence ngoài scope.
AFTER fresh đúng attempt/checklist.

Submission:
Auth/schema-invalid request không là valid submission.
Structurally valid incomplete submission được server intake ngay.
serverReceivedAt và original PM due24h write-once.
Không đợi uploads hoàn tất mới start review clock.
Pending file không VERIFIED hoặc acceptance evidence.
Claim repaired thiếu AFTER vẫn insufficient/waiting supplement, không confirmed.
Unrepaired reason theo R13.
Supplement immutable linked revision, không rewrite old payload hoặc second execution/origin.
File arrival later chỉ evidence-readiness/supplement history.

SQL/migration/transaction/idempotency:
Nullable Survey/value/location + explicit source/purpose/status/reason/dimension constraints.
m/mm/cm/m² đúng measurement type; genuine zero giữ nguyên.
Audit legacy rows; không convert legitimate zero thành UNKNOWN.
Update relevant triggers theo adopted purpose, không blanket mọi measurement Defect.Open;
giữ legacy/research invariants đúng scope.
Same-project/task/assignment/evidence target/version relations.
Intake/audit/clock origin/receipt/outbox atomic.
Current actor/task/assignment/purpose file admission/attach/read/replay guards trong same context.
Generic project upload không chứng minh task authority.
Narrow Crew geometry seam.
FIELD producer/result typed gắn đúng task/Defect/version/sufficiency,
real consumer trong DefectWorkflowService, không REPORT-v1 masquerade.

Update retention/readers ngay cùng model delta:
new purposes/multi-evidence/nullable Location safe,
session/measurement/submission history refs đầy đủ,
same scoped caller tx, deterministic versions,
keep incomplete reason cho chưa đủ inventory.
Không để null crash chờ H7.

Acceptance/tests:
HTTP/SQL no-survey, UNKNOWN value/GPS, m²/dimension.
Wrong actor/project/task/assignment/purpose/source/file.
Incomplete intake starts due24h dù pending evidence.
Immutable supplement/replay không reset origin.
Cancel/reassign/in-progress/offline history.
Concurrency/rollback/recovery/current-authority receipts.
Real FIELD producer-consumer and retained privacy/read behavior.

Checkpoint/STOP:
Commit/push H3 sau full vertical acceptance, hai reviews/fixes.
Partial external/file readiness được ghi đúng gate, không fake result.
Tiếp tục H4; không stop vì old B1/D1 ledger.

H4 — REPAIR/FAST TRACK/OBLIGATIONS/CORRECTION/SAFETY
--------------------------------------------------

Dependency:
H1 clocks/security, H3 task/evidence/result, H2 location/version/coverage facts.
Design first-start/offline authorization cùng H5 sớm;
actual mobile acceptance không điều kiện giả để dừng online BE.

Reusable modules:
Current Defect/Case workflows/domain/repositories,
inspection/task/evidence services,
warranty/project/road facts,
shared receipts/outbox,
Anh02Contracts/reporting reader seam và retention composition.

New delta:
Repair package/item/obligation/attempt module;
policy draft/publish/version/revoke;
normal/FT execution/review/final/correction;
explicit Defect closure/recurrence;
temporary safety and responsibility history.

Authorization/state:
Normal R02 đầy đủ; PM review không normal final confirmation.
FT R03–R09 đầy đủ; PM policy publish, không Supervisor policy approval.
Không thêm extra PM permission after measurement cho already-authorized eligible FT.
MEASURE_ONLY dù PASS không sửa.
Eligibility cần đồng thời:
correct project/task/assignee/Defect/location/version;
task execute permission;
handed-over road + confirmed warranty/maintenance coverage;
published pinned configured policy;
required measurements/units/checklist;
verified location;
no stop condition;
valid bounded authorization/no known revoke.
Coverage UNKNOWN không tự thành eligible.
No configured threshold → NOT_CONFIGURED; unknown → UNKNOWN;
eligible/ineligible reasons explicit.

Crew claim và effective confirmed state tách biệt.
FT PM confirms; normal Supervisor final.
Supplement/rework/cancel preserve attempts/submissions/history.
Mixed packages giữ per-item type/approver.
Mandatory obligations resolved hợp lệ mới complete;
cancel item không resolve obligation.
Explicit Defect closure riêng, không cascade Case/warranty.
True recurrence → new linked Defect.
Mistaken acceptance → correction supersedes decision, continue original obligation.

Safety:
Responsible actor/check schedule/replacement-removal/formal-repair link.
Installation chưa resolve ongoing safety obligation.
First check≤24h từ completed measure, danger ack1h từ server warning.
Responsibility transfer history giữ phần thực hiện.

SQL/migration/transaction/idempotency:
New typed domain/config/forward migrations trong current DbContext.
Active uniqueness Defect+obligation+actual scope, including overlap,
deterministic locking/SQL backstops/concurrency.
Không duplicate by Report/Case/location/scope string.
Append-only attempts/submissions/execution/decisions/corrections.
Correction actor/reason/time/supersedes ID.
Atomic decision/obligation/projection/audit/clock/outbox/receipt.
Preserve original first events nhưng producer exposes effective corrected decision.
Snapshot exports immutable; no silent KPI denominator/time-bucket change.
New evidence refs cập nhật inventory cùng package.
Source/outbox events emitted trước H6 dispatcher; không worker tự mutate lifecycle.

Acceptance/tests:
Measure-only PASS denies execute.
FT PM scope/no Supervisor-policy gate/no extra PM after-measure gate.
Policy configured/UNKNOWN/pinned/change/revoke/coverage/location/time eligibility.
Normal Supervisor final and FT PM final.
Active scope overlap race, start/submit/review/confirm/correct races.
Mixed package/cancel obligations/recurrence vs correction.
Safety first-check/danger/transfer.
Privacy/current receipt guards/rollback/recovery.

Checkpoint/STOP:
Commit/push H4 after reviews/focused checks.
Record offline end-to-end dependent on H5; không fake phone-ready.
Retain any genuine KPI interpretation gate, continue H5.

H5 — OFFLINE FIRST-START/SYNC/ENCRYPTED HANDOVER
----------------------------------------------

Dependency:
H1 persistent auth/time, H2 downloadable pinned location,
H3 submissions/evidence, H4 policy/repair/obligation services.

Reusable modules:
IdempotencyOperationService,
current direct command services,
upload/multipart recovery/verified-file facts,
task/assignment/policy/location snapshots and current authority guards.

New delta:
Versioned offline download/grant/start reconciliation/sync/result/status,
per-operation dependency processing,
encrypted device data handover grant/export/import/status.
Module placement/routes freeze before code; no general-purpose sync framework.

Authorization/state:
Offline first-start allowed; no forced online start.
Downloaded task/location/policy/auth + durable local origin event + reconciliation.
Persist first-start write-once linked task/assignment/authorization.
Duplicate/concurrent/reopen/remeasure/sync/device reinstall không reset24h.
Changed assignment không tự grant new window.
New authorization explicit ID/actor/reason/scope/validity, giữ old grant.
Clock rollback/forward/reboot/multiple-device uncertainty giữ evidence/review,
không tự cấp authority hoặc fake backdate.
Offline revoke learned when update received; reconnect current authority.
Direct/sync commands cùng service/policy, không hai bản business logic.

Wire:
operation origin ID/type/schema/payloadHash;
original actor/source device;
task/assignment/resource/policy/location versions;
dependency IDs;
capture/start/finish and time provenance;
current importing/syncing caller.
Origin actor không là caller impersonation.

Per-operation partial processing:
Each admitted command atomic riêng.
Dependencies pending/blocked có result rõ; independent operations continue.
Stale/conflict evidence giữ review khi caller có admission rights,
không LWW hoặc đổi payload giữ old key.
Revoked/unauthorized caller không được protected admission/replay;
rejection không xóa local data, không silently tạo business effect.
Sync due24h từ finished; record late state/serverReceived times.
Late valid-start execution không bị xóa; chưa proof không tự gọi valid.
Incomplete metadata intake trước file readiness; upload/resume→VERIFIED→supplement.
Lost ACK same origin one effect; local queue không delete trước durable ACK.

Handover:
Supervisor issues grant24h riêng.
Bind project/source device/data/actions/recipient scope/manifest hash/version.
Recipient current authority; original actor/provenance retained.
Import sau original actor revoked có thể admitted qua valid scoped grant/current recipient;
không impersonate hoặc inherit execution/session rights.
Original execution authorization và acceptance được evaluate riêng.
Expired grant denies new admission.
Current-authorized replay của committed admission khác new import.
Dedup origin across recipients/import/direct-sync, không chỉ importing-user key.
Encrypted manifest integrity/tamper/key-handoff protocol documented.
No organization recovery key; inaccessible lost device/key may be unrecoverable.

SQL/migration/transaction/idempotency:
Unique origin/start/import identities, immutable hashes/provenance/grants.
Atomic per-op command/effect/receipt/audit/outbox/clock.
Retry/crash/partial recovery preserves admission and status.
Same domain service direct vs sync.
Current authority guards trước protected receipt/conflict.
Handover/evidence/storage references added to inventory ngay.
Do not let grant/review extension reset execute window.

Acceptance/tests:
Before/exact/after24h boundaries.
Concurrent first-start/duplicate origin/multiple device/no reset.
Clock tamper/backdate/reboot proof uncertainty.
Revoke discovered reconnect, current caller denied.
Late valid execution/late sync retained.
Partial dependencies/lost ACK/stale payload/rollback/receipt recovery.
Grant expiry/new admission vs authorized receipt.
Original actor revoked/current recipient rights.
Tampered manifest, origin dedup across recipients, crash/partial import.
Actual-client matrix:
queue survives kill/reboot; storage full/camera failure;
network regain/background limits; no delete before durable ACK.
BE fixtures chỉ prove BE/fixture behavior, không phone clock/storage/crypto acceptance.

Checkpoint/STOP:
Freeze/version client wire and fixtures; complete BE implementation/evidence.
If actual client absent, retain exact external matrix/gate,
không drop H5 hoặc claim phone verified.
Commit/push H5, continue H6.

H6 — NOTIFICATIONS/DEADLINES/SUBSTITUTE/PROJECT CLOSURE
-----------------------------------------------------

Dependency:
H1 clock foundations và actual H3/H4/H5 source events/obligations.
Dependent received/extension/substitute/repetition meanings quản lý mục K.

Reusable modules:
Notification/NotificationConfiguration,
NotificationPersistenceService/NotificationOutboxConsumer,
ConsumerEffectService/OutboxWorkRepository,
existing inbox/controller/service,
project/membership/lifecycle/safety services.

New delta:
Project/occurrence-aware protected inbox,
real registry/dispatcher/fan-out receipts,
business clocks/ack/overdue/extensions,
substitute handling and independent project lifecycle.

Authorization/state:
Current user/role/project membership for list/detail/mark-read/replay.
Lost membership không expose old project content.
Authorized reviewers retain history; no purge on revoke.
Receipt/notification not new permission.
Triggers exactly R28.
Notifications never change business state.
Danger business ack khác delivery/read.
Supplement/escalation actual received origin không silently inbox-created.
No autoapproval/Defect close from overdue.
Unresolved recipient/substitute visible, không auto-grant roles.

Project:
Construction completion/operational closure/warranty separate.
Operational close only obligations resolved or accepted transfer.
New Reporter intake continues.
Supervisor decides renewed handling scope.
No cascade-close historical records.

SQL/migration/transaction/idempotency:
Add ProjectId/source scope/occurrence identity with audited legacy backfill.
Unknown protected source scope fail-closed, không guess globally readable.
Genuine non-project notification only when source proves non-project scope.
Per-occurrence/per-recipient unique effects/delivery receipts.
Transaction source event/outbox/receipt; registered event lease/retry;
không unsupported-event no-op consume.
Dispatcher crash/restart/retry no duplicate effects/business handler.
Origin/ack/due/extensions/overdue history append-only.
Weekly Monday09 Asia/Ho_Chi_Minh persistent occurrence when pending review;
technical retry not new calendar occurrence.
No invented daily escalation/catchup business policy.
Extension/substitute authority not broadened past agreed flow.
Missing recipient retained unresolved, not dropped.

Acceptance/tests:
Real worker/SQL/HTTP current-membership list/detail/read/replay.
Historical protected scope/backfill fail-closed.
Fan-out recipients/duplicate occurrence/restart/crash/retry/rollback.
Every clock boundary via controllable clock.
Incomplete submission starts PM due.
Supplement/replay no reset prior origin/overdue.
Danger ack≠delivery; weekly timezone/occurrence.
Unauthorized extension/substitute prevention.
Operational closure outstanding obligations/accepted transfer/new Reporter intake.

Checkpoint/STOP:
Commit/push completed ready H6 flows after reviews/tests.
Only ambiguous received/extension/substitution/repetition capability activation pending.
Keep full pending scope visible; continue H7.
Không quay lại blanket B4/D4 stop.

H7 — REPORTING/KPI/RETENTION/CONTRACTS/POSTMAN/CI/RC
--------------------------------------------------

Dependency:
Integrated ready H0–H6 source behaviors.
Relevant public KPI/allocation interpretation and external acceptance gates explicit.

Reusable modules:
RoadGuardSystem.Services/Interfaces/Integration/Anh02Contracts.cs
CaseDefectReadReader/reporting readers/repositories,
ReportingDefinitions/ReportingService,
export/dossier immutable snapshots,
RetentionContracts/contributors/composite/evaluator,
canonical contracts/http, docs/postman, .github workflows.

New delta:
Actual repair/attempt/decision/correction/obligation/safety/location facts,
correction-aware live KPI/projections,
complete reference inventory,
integrated canonical/client fixtures/Postman,
exact-SHA CI and reviewable release candidate.

Authorization/state:
Reporting/dossier/source access checks current user/role/project/resource.
Snapshot membership không grant current file permission.
Live confirmed repair derives effective corrected decision.
Original execution/submission/decision history preserved.
Distinct repair items/Defects, không count mỗi attempt/location as completed repair.
Same/cross-period correction fixtures.
Old exported snapshots immutable.
No unapproved denominator/time-bucket/allocation change.
Retention basis/hold/evaluator authority unchanged; no deletion.

SQL/migration/transaction/idempotency:
Actual producer→consumer scoped transaction/snapshot;
no fake reader/empty-success when facts unavailable.
Inventory all file relations:
purposes/submission revisions/attempts/corrections,
BEFORE reuse/AFTER,
geometry sources/handover/history/multiple obligations.
Same file many references giữ đầy đủ.
Null locations safe, source scope/version deterministic.
Only retire HUY incomplete/unavailable signals when actual full referenced scope proven;
inspection completeness không tự là repair completeness.
Fresh DB and populated integrated-baseline upgrade;
migration identity/trigger/backfill/model snapshot parity.
No shared/deployed migration or fake production DB substitute.

Contracts/Postman:
Maintain exact designed/adopted APIs/DTO/errors/roles/versions/fixtures.
Preserve identifiers/environment IDs/FE lock.
GIS drafts not automatically canonical.
New route cookie/bearer/CSRF integrated.
No secrets/personal config/sample policy labeled production policy.
Inventory/canonical/Postman updated each producing H, not deferred wholly to H7.

Acceptance/tests:
E2E:
road/branch→Supervisor confirm→PM publish/segments→survey OR Reporter→Defect→
FIELD→normal+FT→offline incomplete submission→review/supplement/correction→
report/export→retention inventory.
Wrong actor/project/version, late/incomplete evidence,
failure-before-commit/retry/replay/duplicate ACK recovery.
Actual production DI/model/HTTP/SQL/worker paths.
Corrected live reporting with immutable old snapshot.
Build/focused tests and hosted CI on exact final SHA.
Production-like storage/font/proxy/config/worker smoke according to changed risk.
Large8GiB test only if byte/stream/proxy/storage behavior relevant changed;
don't rerun because docs or unrelated domain changes.
Historical large-file evidence reused only with applicability recorded.

Release candidate:
Runbook target/config/secrets/HTTPS/cookie/CORS,
proxy upload limits,
workers/queues/recovery,
migration order/backup/rollback-forward,
health/logging.
Keep performance/recovery targets:
50 concurrent users, metadata server p95≤2s, RPO≤15min, RTO≤4h.
Unexecuted benchmark/restore NOT_VERIFIED, not PASS.
No auto production deploy or main/develop merge.

Checkpoint/STOP:
Complete independent BE package and verification; normal commit/push final checkpoint.
If exact-SHA hosted CI unavailable, retain exact NOT_VERIFIED gate.
Missing client/deployment/source interpretation blocks only corresponding acceptance.
RC reviewable does not mean deployed/full external acceptance.
Do not claim all H done while required scoped capability remains pending.

======================================================================
J. FIVE CROSS-FLOW RISKS THAT REQUIRE MEANINGFUL TESTS
======================================================================

1. Offline FT expired/revoked/clock uncertainty + late execution evidence:
   no renewed authority, no destroyed execution history.
2. Measure-only task used as repair grant or Reporter-no-Survey/UNKNOWN
   forced into fake IDs/0/(0,0): reject authority misuse, preserve model truth.
3. Incomplete/duplicate submission + receipt recovery:
   one submission origin, PM review clock starts at valid server intake,
   no auto-confirm from claim/upload.
4. Policy/location/assignment change during work:
   pin history, mark impacted task, current-authority re-evaluation.
5. Acceptance correction or multi/shared segment refs:
   obligations/KPI/inventory consistent, no duplicate repair or missing references.

======================================================================
K. LOCALIZED PENDING INTERPRETATIONS — NOT R01–R30 REOPEN
======================================================================

Only these unresolved interpretations/source-adoption details may need a grouped update:

1. Segment allocation/shared-road total:
   Proposed counts related to segments not summed into project total;
   actual quantity assigned to explicit scopes, unresolved part unallocated;
   shared-road identity explicit, not inferred solely polygon overlap.
   This is PROPOSED allocation method, not owner-confirmed formula.
   Store actual facts/refs/distinct totals while withholding ambiguous public formula activation.

2. “Crew received supplement” / “Supervisor received escalation”:
   Need actual received milestone separate from notification creation/read.
   Proposed explicit agreed delivery/ack protocol.
   No retries resetting receipt origin.
   Do not substitute inbox creation for actual received as an unapproved business choice.

3. Deadline extensions/substitutes/repeated reminders:
   Keep role authority from accepted flow.
   No assumed PM right to extend Supervisor deadline or execute grant.
   Missing substitute authority/catchup/repetition policy:
   visible unresolved assignment/escalation, no dropped warnings or automatic role grants.

4. CRS/tolerance/map batching and actual source:
   Technical bounds can be designed/configured/documented.
   No guessed real survey/CrsProfile/accuracy.
   Official real-data publish waits verified profile/source.
   Missing proposed GIS reference docs affects only details depending on unseen compatibility.

5. Correction reporting public semantics:
   R19 effective-correction behavior is confirmed.
   Preserve original events and snapshots.
   If exact time-bucket/denominator change goes beyond fixing wrong counts,
   ask only that dependent public-contract interpretation;
   no silent denominator change/backfill.

For any such gate:
- document exact missing choice, proposed implementation/consequences;
- do not promote PROPOSED to TARGET_CONFIRMED;
- consolidate genuinely missing questions into one update;
- continue independent H0/H1/GIS core/FIELD core/repair/BE integration;
- retain dependent pending capability and acceptance in spec;
- do not ask all D1–D4 again, recreate pre-decision stop or cut scope.

======================================================================
L. EXCLUSIONS AND EXTERNAL-EVIDENCE BOUNDARIES
======================================================================

Không:
- real AI provider implementation;
- A08/A09 AI retry/late-attempt remediation đã loại;
- physical retention deletion;
- materials-management module;
- maintenance-management module;
- open-traffic-time calculation;
- package/framework upgrades/unrelated refactor;
- tự implement external FE/Android/AI repositories thiếu ownership/source;
- tự production deploy;
- merge/push main/develop hoặc sửa/push anh-review;
- claim BE/mock fixture = phone/browser/provider/deployment acceptance.

Upload/offline/command retry/recovery vẫn bắt buộc;
không nhầm exclusion AI retry với việc bỏ command durability.

Huy chịu BE adapters/contracts/fixtures và integration coordination.
Actual-client matrix/deployment target rights absent:
complete independent BE, retain exact external gate/owner/evidence needed.
No credentials in chat/git.
No hypothetical external success claim.

======================================================================
M. VALIDATION, CHECKPOINT, RESUME VÀ FINAL HANDOFF
======================================================================

Test anchors available in reviewed repository:

dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --nologo

dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter 'FullyQualifiedName~Inspections|FullyQualifiedName~Notifications|FullyQualifiedName~AuthoritativeSessionValidator' --nologo

dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter 'FullyQualifiedName~Inspections|FullyQualifiedName~Notifications' --nologo

dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter 'FullyQualifiedName~Huy02NotificationAuthorityTests|FullyQualifiedName~P207NotificationPersistenceTests|FullyQualifiedName~P240FieldInspectionMeasurementSchemaTests|FullyQualifiedName~ReceiptAccessGuardSqlTests' --nologo

git diff --check

Đây chỉ là initial affected-regression anchors, không full H1–H7 acceptance.
Inspect actual tests, select/add exact new-risk test identities/filters.
Use actual integrated-base EF model-check harness/tooling;
don't guess context/startup command.
Keep dotnet exit code; wrapper/rg exit không PASS.
Zero discovered không PASS.
Counts distinguish executed/pass/fail/skip/not-run and distinct identities,
không cộng repeated runs thành additional acceptance.
Retain intentional red/intermediate failure history and resolving evidence.

Mỗi checkpoint:
- complete vertical behavior and source/interface/SQL/production DI;
- self-review1: authority/state/privacy/time/atomicity/retry/concurrency;
- fix;
- self-review2: compatibility/integration/migration/history/contract/inventory;
- fix;
- rerun only invalidated/affected checks;
- final diff/check;
- normal commit/push huy-review;
- persist spec/summary and continue next ready H.

Nếu session/context bị ngắt:
Persist current H/substep/frozen contracts/dirty paths/SHAs/test results/next action.
New chat recheck actual HEAD/dirty/delta, resume from recorded step.
Reuse unchanged inspected source/evidence;
review only new affected delta, không research/rerun whole assignment.
Không label interrupted session là assignment completed.

STOP:
- Không stop ở preflight/spec nếu authorized independent work còn.
- Không mở unrelated scope/H8.
- Hold only real missing interpretation/writer/environment capability;
  continue other authorized work.
- Finish when independent authorized H0–H7 BE scope/checks complete,
  RC reviewable và every remaining promised capability/external gate explicitly recorded.
- Do not claim complete release/deployment/client acceptance beyond evidence.
- Nếu còn dependent implementation pending, record it as pending,
  không silently remove from scope or mark DONE.

Final response phải có:
1. Initial actual Huy base, current inspected Anh source SHA,
   integration strategy/semantic conflicts resolved.
2. H0–H7 checkpoint SHAs, final SHA/compare, changed files.
3. R01–R30/clocks/persistent-login acceptance matrix.
4. Exact commands/results/counts, failure history, skipped/not-run.
5. Both actual self-review passes and fixes.
6. Production DI/model/migration/HTTP/SQL/replay/race/rollback/recovery evidence.
7. Actual reporting/correction/inventory/canonical/Postman/CI status.
8. Retained HUY-01/I1/N1/R1/DOC evidence and any direct new regressions.
9. Dependent interpretations/CRS/reference compatibility/external client/deployment gates.
10. Release-candidate runbook, benchmark/restore limitations.
11. Clear distinction BE verified / mock-fixture verified / live storage /
    actual phone-browser-provider / hosted CI / deployment.

Bắt đầu preflight và H0 ngay.
Tiếp tục toàn assignment theo dependency, không chỉ báo cáo kế hoạch.

H4 core populated foundation latest SQL3/3 default-analyzer pass (h4-core-populated-green.trx): null-project legacy source fails closed; raw decision/correction staging, exact terminal item/obligation heads, immutable original decision, hydrated package/history, rollback and populated downgrade refusal. This fixture uses controlled raw decision facts, not current production command authorization. Earlier populated run executed1pass/2fail: null-project scope guard bypass was actual product red; missing fixture PasswordHash was setup failure. Initial missing catalog import/ambiguous Coordinate compile executed0 tests and is not a behavior red. Peer review also found nullable road source bypass, corrected via LEFT JOIN/source existence; exact policy measurement publication source remains under verification. Production correction EF staging/current authorization/replay/concurrency/API and actual reporting consumer are still IN_PROGRESS.

Runtime TDD admission checkpoint (not package acceptance): h4-h5-h6-runtime-admission-red.trx executed88 cases:56pass/32 intended behavior reds. Breakdown H4 admission10red, current-stock capture consumer5red, H6 dispatch4red, H5 evidence factories6red/service7red; H5 prior50+explicitaccept2+canonicalfixture1=53pass, and3 existing tests matched the broad Offline method-name filter also pass. Current concrete SQL repair reader is still NotImplemented pending its real SQL red. Subsequent SQL compile attempts stopped before tests on root unread primary parameters and H4 fixture catalog/Coordinate issues; both fixed and logs retained separately. No zero-test compile attempt is a behavior red or SQL acceptance.

H6 inbox actual red checkpoint:8 executed,6 intended behavior failures/2 inert denial passes/0skip after correcting required fixture normalized username; the prior8 fixture failures and compile-only attempts remain separate. Root core SQL reader red (h4-core-reader-red.trx) executed2pass/1 intended NotImplemented failure at actual SQL CaptureAsync; prior core3 green remains historical guard/transaction foundation evidence. Concrete current-source reader implementation now has an observed SQL red, not a test-only allow policy.

H4 current repair reader actual SQL3 green verifies exact committed correction heads, historical capture retention and revoked project membership denial. Stock consumer5/unit service admission14 are separate from SQL proof. First real correction repository authority SQL5 now passes (different current PM FT, mode-role denials, Supervisor without project membership, revoked normal Supervisor replay/conflict); actual staged EF append/history/heads/package/receipt/audit persist. This is newly unlocked correction evidence, not full H4 acceptance. Actual reporting pipeline1 executed red before its internal producer injection; wire DTO shape and old snapshot bytes remain unchanged. Stored-resource receipt guards, concurrency/fault/Resolved effects, HTTP/export and remaining normal/FT flows are still under implementation.

CURRENT_VERIFIED coordinated boundary 06/10: `TestResults/H4H5H6/h5-artifact-green-h6-audit-red.trx` executes83: H5 Offline77 pass; H6 correction catalog3 intended positive reds/3 rejection passes. `h4-policy-green-history-red-authority-h6-receipt.trx` executes12:10 pass/2 intended product reds. Production H4 authority6 now passes including changed-resource canonical receipt scope; exact draft measurement publication and corrected genuine recovery fixture pass. Actual reporting pipeline passes, superseding its prior red. Published decision late evidence insertion (no exception) and revoked-original H6 receipt changed-target admission (IdempotentConflict instead of protected NotFound) are the two observed reds. Source fixes are under verification; actual DOSSIER immutable snapshot before/after correction assertions and late published policy-rule insertion regression have been added. No complete H4/H5/H6/H7 acceptance, rendering, client or external deployment result is inferred.

Root technical transport design for the new assigned H2–H7 route tranche: exact matched MVC endpoint metadata opts only implemented owned controllers/actions into the existing user-cookie selector and mixed-actor predicate. Existing static legacy eligibility remains intact; no broad project-prefix matching or guessed new routes. Matching method/GUID/version constraints stays the router's responsibility. Unsafe cookie calls use existing CSRF middleware; explicit Authorization header retains bearer precedence. Meaningful actual HTTP reds are prepared for geometry/FIELD reads and geometry/FIELD/pavement writes, current session revocation and mixed actors. Production implementation follows the observed boundary; later repair/offline endpoints receive the marker only after their producer/controller is implemented. This changes transport wiring, not project/resource command authority.

CURRENT_VERIFIED subsequent focused boundaries: H4 production correction authority SQL15/15, core SQL5/5, review-request SQL4/4, correction/request HTTP5/5, exact owned-route cookie5 and legacy cookie2 pass. Actual FIELD notification source SQL3/3 passes after its observed EF query translation failure was fixed. Unit13/13 covers correction source proof5 plus recipient/file provenance8. `h5-canonical-h6-claim-red-correction-green.trx` executes8 failures: claim3/canonical-registry2 intended NotImplemented reds; correction-source3 are fixture-only expected200/actual201 before source assertions, not an adapter failure or pass. `h4-finite-history-http-red.trx` executes1 actual wire red: enum serialized Number rather than finite String. Full normal/FT producers, history representation version and current source authority for historical private exports continue. Correction decisions remain TARGET_CONFIRMED; these partial executed checks do not establish complete H4–H7 acceptance or external compatibility.

CURRENT_VERIFIED next frozen boundary06/10: H4 additive metadata18 and production repair unit159 pass; H5 recipient endorsement6 pass. `h4-additive-metadata-green-h5-endorsement-green-artifact-red.trx` executes167:165 pass/2 intended artifact-registration reds, subsequently artifact2 green. Default API build after nullable decision/assessment successor mapping passes142 warnings/zero errors; post-removal scaffold build128 warnings/zero errors. Wrong API filename attempt executed no build. Only NEW UNAPPLIED producer candidate regenerated as20261006084919_H4RepairProducers after preserving v1 source/Designer/Snapshot in task TEMP. Design-time connection deliberately points to unreachable127.0.0.1:1; EF remove reported unable to check applied status and removed only the new local candidate, without shared database access. Applied migrations remain unchanged.

Actual SQL `h4-producer-guards-h6-dispatch-unknown-red.trx` executes8 genuine reds: producer guards3 (pending snapshot model, missing current-item scope and unsafe populated downgrade), dispatch4 and unknown-audit1 explicit stubs. Model diagnosis initially encountered two diagnostic-fixture initialization failures, recorded separately; v3 exposes EF-owned FK metadata Restrict omission. New candidate Designer/Snapshot preserve14 owned Restrict edges;40 redundant existing-FK operations removed from new candidate Up/Down, including original Reporter evidence edges. Existing migration identities/files untouched. `h4-producer-guard3-green-candidate.trx` executes3/3 green, verifies exact model parity,18 actual enabled history/monitoring triggers, foreign-obligation current-item denial, fresh/populated core preservation and populated producer-pin downgrade refusal. Additional native task/session/source-head guards and complete normal/FT producers remain IN_PROGRESS; this is not full H4 schema/command acceptance.

H5 actual importer current-authoritySQL2 passes in `h4-parity-diagnostic-h5-current-authority.trx`; its separate parity diagnostic1 fails before model comparison. Unit `h5-artifact-green-selector-h6-unknown-red.trx` executes11:2 artifact pass/4 meaningful body-action and strictJSON selector reds/5 unknown-policy explicit-stub reds. Subsequent `h5-selector-h6-unknown-green-h4-producer-red.trx` executes21:12 pass (artifact2,selector4,unknown policy5,unchanged generic measure-only1)/9 intended H4 factory5, typed hash golden3 and correction previous-head1 reds. A preceding unit compile attempt executed0 tests because a fixture called nonexistent StartWork; narrowly corrected to existing Start. No client/native offline hash equivalence or actual encrypted-phone acquisition is inferred.

H7 exact Reporting/Exports cookie routes: `h7-report-export-cookie-red.trx` executes8:5 pass/3 actual missing-route-admission reds. Marker adoption then `h7-report-export-cookie-green.trx` executes8/8 pass, including authorized reporting, export scoped-not-found, current session, bearer precedence and unsafe-write CSRF. This adopts transport only, not new project/repair command authority. Full H4→H7 independent producer/consumer work continues; no complete package, external/deployment, official CRS or accuracy PASS.

CURRENT_VERIFIED native/reporting boundary: `h4-native-factory-hash-previous-green.trx` unit10/10 verifies factory6, frozen typed core hash3 and correction prior-head1. Existing generic MEASURE_ONLY remains unchanged; C# golden literals do not claim a browser/native client implementation. First SQL9 in `h4-guard-green-current-stock-native-red.trx` has3 pass/6 failures:3 native task mode actual source reds, legacy inventory actual red1 and2 reader fixture failures before capture (missing package linkage). Follow-up reader-red2 first still stops at incomplete hydrated package fixture, not a projection assertion. Corrected controlled retired-state fixtures then `h7-current-item-reader-red-v2.trx` executes2 actual projection reds: missing ambiguity reason and stock count3 instead of2. Controlled legacy state is not a production cancel command or current command authority.

`h7-stock-native-task-green-session-red.trx` executes9:8 pass/1 actual session-purpose red after reaching the completed PRE_EXEC session insertion. It verifies prior guard3, current-item/legacy-inventory reader3 and genuine Reporter-backed NORMAL/CONDITIONAL_FT native task2. Reader chooses explicit CurrentRepairItemId or exact legacy obligation resolution-head item; multiple unpinned siblings produce UNKNOWN rather than first/latest/count-all. A Defect lacking any observed obligation inventory cannot establish complete repair stock. No period/denominator/shared-road allocation is invented. Native task guard is forward-corrected only in new unapplied producer candidate, preserving original H3 lineage/geometry predicates and enforcing immutable actual repair-item/plan/mode scope. Existing applied H3 source unchanged. Full producer admission/task binding/execution/safety and H7 reporting remain in progress.

Next focused native session/empty downgrade2 initially stops at DDL installation (SQL Server stored CREATE OR ALTER spelling causes duplicate-trigger error); no session success claimed. Prefix handling corrected in new candidate guard helper; fresh verification pending. Historical failed diagnostic, fixture and compile runs remain evidence, not behavior PASS.

Coherent follow-up boundary: native session forwarding and exact empty downgrade SQL2 now pass; latest SQL13 has6 pass/7 intended implementation reds (six H4 producer transactions and one H6 pending recovery cap). Unit29 executed all29 intended reds before implementations (H4 producer service10, H5 mapping15, H6 deadline proof4). Subsequent unit43 executed32 pass/11 fail: all15 adopted H5 model checks, H4 producer service10, deadline proof4 and retained historical3 pass; new historical binding2/transfer service5 remain implementation reds, and export storage revocation4 shows actual bytes reaching renderer after authority changed during OpenRead. Narrow analyzer-only attempts executed0 tests and are not behavioral evidence. Post-open authorization/disposal fixes and offline runtime fixes await focused green checks.

Separate NEW UNAPPLIED H5 scaffold20261006094905 is additive15 tables, preserves14 existing owned Restrict metadata edges and contains no old FK rewrites. Fresh API build succeeds2 warnings/0 errors. Declared SQL triggers/populated downgrade/actual signed admission are not verified by mapping or scaffolding; migration guard checks are being executed separately. No shared/applied database or migration was changed. H4–H7 full acceptance remains IN_PROGRESS.

Next actual boundary: SQL24 executes20 PASS/4 intended runtime reds. H4 producer transactions6 now PASS after native assignment state was explicitly saved before native task INSERT inside the same guarded receipt transaction; H5 SQL schema2 verifies30 enabled append-only/source guards, parity and empty downgrade, while populated registration mutation/delete/downgrade refuse; FIELD deadline producer2 and claim recovery5 PASS. Signed admission2/device registration2 remain actual NotImplemented reds. Historical replay latest5/5 PASS preserves raw JSON bytes to avoid JsonElement changing DateTimeOffset escaping; transfer service5 and post-storage export revocation4 PASS separately. These controlled storage tests do not establish external storage delivery.

HTTP producer5 executed actual404 reds before controller/DI adoption; root wired the producer interfaces to the same scoped repair repository. Separate NEW UNAPPLIED H6 candidate20261006100156 adds7 scalar notification tables, preserves14 existing owned Restrict metadata edges, no old FK rewrites. Fresh API default build205 warnings/0 errors. Actual mapped dispatcher6 + sanitized unknown audit1 PASS7/7; SQL immutable/scope guards, retry/calendar/worker/full source registry and final integration remain unverified. Technical cancellation-source and exact attached offline payload refinements are being separately scaffolded; no pending actor decision is guessed. Full H4–H7 and final exact-SHA CI remain IN_PROGRESS.

### Execution boundary 06/10 — source guards, execution routes and lifecycle continuation (IN_PROGRESS)

Actual HEAD remains huy-review@999e2c3d2cd1912eb3f775164f099c4ffde694bf (H3 checkpoint); H4/H5/H6/H7 source and additive candidates are uncommitted. No shared/applied database or applied migration was modified. Root owns mapping/DI/migrations/contracts/docs; H4/H5/H6 assigned writers use source-only release and explicit freeze before the single build/test slot.

Retained diagnostic evidence: SQL17 executed2 PASS/15 FAIL (H5 signed admission2 PASS; H6 retry2/lifecycle2 implementation stubs, normal proposal clock1 missing source, H4 execution5 stale fixture rowversion, guard1 missing exception and4 schema loss after shared destructive downgrade). SQL10 subsequently executed10 FAIL:5 missing H6 guard behavior,4 H4 runtime stubs and1 first-origin fixture shadow-version failure. Unit30 executed20 PASS additive source/model tests and10 intentional execution-service stubs. These failures remain historical evidence, not owner decision changes. Default API build had a CA1862 admission query analyzer failure (0 tests); canonical checksum normalized before EF predicate, then build207 warnings/0 errors. Next SQL30 first attempt executed30 setup failures because root's generated trigger text included an explanatory comment as SQL; exact offending line was removed, with failed TRX preserved.

Corrected SQL30 (`h6-guards-retry-green-h4-execution-h5-file-lifecycle-renew-red-after-ddl.trx`) executed20 PASS/10 FAIL/0 SKIP. PASS: notification guard5, retry2, mapped dispatch6, normal proposal Supervisor clock1, current lifecycle read2, device registration2 and execution source denials2. FAIL: H4 assessment-dependent3 rejected by old canonical kind CHECK; missing start/finish2 used receipt authority outside the service-owned transaction; lifecycle renewed handling2 explicit NotImplemented; H5 raw ACK1 and false owner/uploader2 incorrectly admitted. No positive execution or full lifecycle completion is claimed. Unit13 executed10 PASS execution service admission/3 FAIL explicit notification proposal proof stubs. HTTP execution3 executed3 actual404 FAIL before route adoption.

Technical fixes awaiting focused validation: NEW UNAPPLIED H4 continuation candidate now forwards canonical FIELD_ACCEPT/REPAIR_ASSESSMENT/REPAIR_EXECUTION_START/REPAIR_EXECUTION_FINISH kinds and retains all H3 task/schema/identity/hash source checks; populated kind downgrade refuses. Corresponding current model and later un-applied model checkpoints match. H5 result guard requires actual canonical typed durable effect; admitted file guard retains actual FileScope owner/StoredFile uploader, exact task-purpose scope or authorized BEFORE reuse source and verified upload/checksum. This historical relational backstop does not replace cryptographic/current-admission authorization. Actual H4 service-owned transaction and typed three execution routes/DI are being validated after observed reds.

Root lifecycle source read is verified separately from mutating generic lifecycle commands. Newly implemented renewed handling uses current Supervisor authority and an actual retained, verified operational closure source, canonical typed fingerprint and serializable guarded receipts; append history/audit/receipt atomically, with concurrency version and current-authority replay/conflict checks. A prior closure source remains history when new intake/correction invalidates current closure basis; renewed scope never re-closes obligations or grants execution. Controlled prior closure fixtures are TEST_ONLY and do not approve the pending generic operational-close actor/procedure. Candidate sources and legacy Project.Status never establish closure or inventory completeness. Full renewed acceptance and HTTP activation remain under focused verification.

H4 normal/FT/safety/policy/retention vertical producers/consumers, H5 full sync/handover/upload/reconciliation, H6 full registry/calendar/worker/operations and H7 full integration/RC/exact-SHA CI remain IN_PROGRESS. All localized owner-pending activations retain the consolidated authority ledger. Correction authority is TARGET_CONFIRMED owner Anh update06/10; old correction/re-review pending is HISTORICAL/SUPERSEDED, not a current blocker.
Latest focused boundaries: SQL24 (`h4-source-h5-guards-lifecycle-renew-v3.trx`) executed15 PASS/9 FAIL/0 SKIP. Newly fixed H5 raw ACK/file provenance3 PASS; lifecycle7 PASS includes candidate denial, current scoped read/revocation, confirmed renewed source append/replay/conflict/revoked replay, same-version concurrent decisions. H4 assessment and negative admission5 PASS. True H4 reds are valid start/finish2 explicit stubs, first-start obligation1 missing immediate native pin, generic bound FIELD cancel/reassign2 returning201 instead of409. H5 snapshot2 and actual verified-file resolver2 are explicit runtime reds. Unit notification6 executed proposal3 PASS/assignment3 stub FAIL. HTTP5 executed H4 execution admission3 PASS/H5 device routes2 actual404 FAIL. SQL notification operations/legacy unknown4 executed4 FAIL: three explicit operation stubs and missing unknown-source audit. These are actual scoped implementation gaps; no owner-confirmed requirement is reopened. Writers released source-only for the observed reds and full remaining scope.

Root has added actual lifecycle audit-failure rollback and immutable-candidate-history tests, plus proposed GET lifecycle/POST renewed-scope HTTP tests before route implementation. These additions are not executed evidence yet. Production confirmed renewed command never activates generic close/transfer/recurrence procedures. Local contract records verified package/native initial tranche and explicitly partial execution route evidence; public/external compatibility remains not adopted.
### Coherent follow-up boundary — 27/31 SQL and remaining actual stubs

The first v4 compile attempt executed0 tests: EF1002 rejected interpolated test-only trigger DDL. Root replaced it with fixed SQL literals, without weakening analyzers. Corrected `h4-execution-h5-resolver-h6-ops-lifecycle-v4.trx` executed27 PASS/4 FAIL/0 SKIP: H4 execution10/10 PASS; lifecycle source/read/renewal9/9 PASS including SQL audit failure rollback and candidate history UPDATE/DELETE refusal; H5 snapshot2/file resolver2 PASS; H6 operations3/unknown-source audit1 PASS. Remaining failures are exactly signed sync2 and explicit clock paging2 NotImplemented; no owner decision was invented to fix them. Native first-start now pins the obligation before assessment, generic bound cancel/reassign deny, actual normal start/finish replay preserves claim vs verified UTC and creates one finish-origin Sync24 without formal review intake. This is execution tranche acceptance, not full H4.

Notification assignment source proof latest3/3 PASS. HTTP device2 PASS; new lifecycle2 actual404 FAIL before controller/service adoption. An earlier HTTP compile attempt executed0 cases due nullable test account email, fixed with explicit assertion. Root has now designed/adopted GET `/api/v1/projects/{projectId}/lifecycle` and Supervisor POST `/renewed-handling-scope` through Controller→Service→Repository, exact quoted SHA256 projection If-Match and visible-ASCII key, typed bounded scope/reason/basis and unknown-property rejection. HTTP activation is not yet verified. History exposes a finite handlingScope field only, not raw facts JSON. Generic close/transfer/recurrence/construction commands remain unexposed/pending.

Postman adds7 H4 producer requests separately disabled (`runH4ProducerDisposable=false`), typed body JSON supplied manually, distinct per-command keys and Defect/package/item ETags. Static11 request/21 script syntax checks PASS; previous parsed collection items/info/variables and environment ID/values preserved against HEAD. Live Postman/client execution NOT_VERIFIED. Full H4 normal review/supplement/final/FT policy/real-source eligibility/safety/retention and H5/H6/H7 required scope remain IN_PROGRESS. Writers released source-only for the remaining genuine stubs and complete assigned vertical scope; root retains the sole build slot and shared schema/composition/docs.
Latest scoped source check `h4-imported-core-red-h6-paging-sources.trx`:6 executed,3 PASS/3 FAIL/0 SKIP. Explicit clock paging2 PASS and actual normal proposal→Supervisor source1 PASS. Assigned source→dispatch1 REJECTED (not a fake adapter PASS); root-cause review distinguishes optional omitted header responsibility from actual immutable binding Crew. Bare same-actor SYNC1 incorrectly returned201 without retained signed admission; caller-owned core1 leaked its private Denied exception instead of the finite typed rejection. H4/H5 writers coordinate the actual same-context signed bridge before runtime sync activation. Earlier accepted direct online paths are preserved; imported original actor is never impersonated by receiver.

Root lifecycle HTTP2 now PASS after observed404 reds and a narrow CA1859 ObjectResult return-type compile fix (0 tests in the failing attempt). The added cookie/CSRF regression and positive finite handlingScope read check are not yet executed. Read and confirmed renewed-scope contract are locally recorded; no generic pending lifecycle command was activated. Current root renewed source selectors now also reject whitespace-only authority provenance. Full H4–H7 remains IN_PROGRESS with the exact pending ledger, two full phase self-reviews and normal phase checkpoints still required.

### Historical progress-summary snapshot (superseded by current concise summary)

This retained snapshot is chronological historical execution evidence, not the current remaining-gap ledger.

# HUY final integration — delivery summary

H0–H3 independent BE verified; H4–H7 continue under the same [assigned spec](HUY-FINAL-INTEGRATION.md). Branch huy-review, initial base c14709e058f96d6421ac10960e34d1f2af37c2bc; selectively integrated Anh source5089c3267dcdf60645ab34f61b58a79e3cbb0cf6. Initial dirty paths none. Huy owns assigned shared integration.

H0 registers inspection/AI/export/intake contributors once while preserving HUY incomplete repair inventory, admits exact inspection/inbox cookie routes with CSRF/bearer precedence, fixes JWT problem media, and integrates retention evaluation/holds with additive six-table migration. Existing migration identities and Huy privacy/readers/labels/source links are preserved. Seven opt-in Postman requests appended without replacing existing objects/IDs; local contract updated.

Two Codex self-review passes fixed fresh/replayed retention authority locks, role reads, concurrent receipt scope guard, scaffolded FK restrictions and Postman opt-in guard. Latest70 distinct cases pass (API42/unit13/SQL15); full SQL14pass/1fixturefail followed by corrected focused1pass. Failure/attempt ledger in spec. Final API build and diffcheck pass. Fresh/populated migration/model parity verified; upgrade population covers session/receipt/export, not all producer graphs.

No ChatGPT/external review, live clients/providers/Postman/deployment, SLO or backup/restore success claimed. H1 implements persistent server lifecycle, finite JWT/Web tickets with protected renewal, guarded encrypted refresh retry and immutable deadline clock groundwork. Latest253 distinct tests pass (API133/unit78/SQL42), including real SQL ACK-loss recovery, revoked-authority recovery denial and populated legacy migration. Two self-review passes fixed current-authority locks, mixed-cookie/header checks, key encoding/hash binding, invitation issued role, clock SQL null checks, named clock DI and model metadata. Final default-analyzer API build passes with109 warnings/zero errors; model parity/diffcheck pass. H3–H6 clock producers remain pending. Full H0–H7 task remains IN_PROGRESS.

Owner CRS clarification: no authoritative project source found. Full H2 core proceeds; fixtures remain sampleOnly/CANDIDATE. Official real-data publication/survey-grade accuracy stays localized PENDING/NOT_VERIFIED pending accepted source/operation and independent controls with sourced tolerance.

H1 checkpoint ae2b28956052978d357fb7ee6c2ded3e629c6c1a pushed. H2 implements sourced candidate profiles, analytic LINE/ARC, partial readiness, pinned MAIN/BRANCH topology/calibration/segments, exact strips, planned/custom as-built slabs, immutable map layers/paging and location-impact inventory. Two self-reviews fixed caller-owned transactions, real native producer facts, authority locks, topology cycles, bounded allocation, source privacy/provenance and wire hashes/casing. Latest96 distinct cases pass (unit59/SQL14/API23), including actual Reporter producer/consumer and fresh/populated migration. Default-analyzer build126 warnings/zero errors; model parity/diffcheck/Postman preservation pass. Synthetic fixtures remain sampleOnly; official CRS/accuracy pending. H3 wires task-scoped Crew geometry and impact execution; recording an impact decision grants nothing. H4 foundation untracked files excluded from H2 checkpoint. Full H3–H7 remains in scope; task IN_PROGRESS.

Grouped policy clarification: actual-received receipt protocol, per-clock extension/substitute authority, weekly catch-up/skip and reminder recurrence are PENDING. Dedicated business ACK and latest-period aggregated catch-up are PROPOSED, not activated. Existing durations and known origins stay confirmed; provenance/history/occurrence/idempotency/policy seams proceed without assuming inbox/read/command receipt as actor receipt or granting authority. Independent H3–H7 remains in scope.

Correction authority is TARGET_CONFIRMED by owner Anh through the consolidated 06/10/2026 update: current project PM for FT; current project Supervisor for normal, without requiring the original individual. Crew/PM review requests have no effective-decision effect. Append-only correction must atomically update continuing obligations, Defect/package state and effective projection when closure loses its basis; generic closure authority gaps do not block these authorized effects. No additional mandatory approval chain is inferred. UI/KPI/new exports use the effective corrected decision; old history and published snapshots remain immutable. Prior correction-authority/procedure PENDING and same-role PROPOSED are HISTORICAL/SUPERSEDED. Production vertical implementation and required security/concurrency/rollback/reporting verification remain IN_PROGRESS, not PASS from this decision alone.

Historical progress before the final H3 checkpoint (superseded by the final verification below): latest peer-fixed fresh/legacy-populated migration checks passed2/2 with default analyzers, including empty downgrade/forward restoration, UNKNOWN downgrade refusal, null-safe evidence/reason rejection and model parity. R15 handover/impact provenance is incorporated into the same unapplied migration; full FIELD producer/consumer/HTTP/SQL/retention acceptance remains ongoing. Internal peer review found and corrected legacy/new session partition, SQL-null evidence bypass, multiple-root/session linkage and downgrade data-loss risks; actual workflow SQL cases remain in progress. H4 adopted correction foundation passes59/59 with default analyzers after11 meaningful red failures; it does not establish production correction routes/current-project authorization/transaction/reporting/export acceptance. H5 isolated device crypto preparation uses no shared schema or server escrow and remains separate from actual client/runtime verification. All preparation is preserved and excluded from the H3 checkpoint.

H2 checkpoint b9541660575312620b73cd836090f02f5ea90e00 pushed. H3 implementation now starts from that HEAD; authorized untracked H4 domain/DTO/unit foundation is preserved separately. H5/H6 audits prepare independent seams; no runtime completion inferred from their plans.

Defect-close/linked-recurrence-create/construction-completion/operational-closure/obligation-transfer grant+accept authority and procedures are localized PENDING. Existing proposal/reopen/data-handover evidence does not authorize them. Their invariants/history/provenance/read projections/policy hooks proceed; production mutation denies without adopted authority and unaccepted transfer cannot permit closure. Supervisor renewed handling scope remains confirmed; new Reporter intake remains enabled. No pending command is labeled completed.

Final H3 independent BE checks pass117 distinct identities: unit39/SQL38/API40, zero latest fail/skip. Includes actual Reporter/noSurvey and Survey KeepNew chains, FIELD producer→Defect consumer, UNKNOWN/zero/m² and nullable-safe V2 pairs, immutable intake/root clocks, current-authority replay/recovery, rollback/origin races, actual handover/impact, privacy/retention and legacy readers. Both file paths deny/dispose after post-storage revoke; fresh multipart capability after task completion is denied. Two root self-review passes fixed scope/SQL/history/privacy and exact prior non-FIELD fingerprint compatibility; historical failures remain recorded. The fingerprint case has mocked candidate facts with actual SQL guards/receipt; actual FIELD production is separate. Default build succeeds; final Postman preservation plus20 script syntax checks pass for16 opt-in requests. Client/storage/official accuracy/deployment remain NOT_VERIFIED. Base before normal checkpoint b9541660575312620b73cd836090f02f5ea90e00; H4/H5/H6/H7 preparation stays excluded. Actual trusted repair attempt/AFTER binding proceeds in H4; no repair grant or offline authority is claimed from H3.

H3 committed/pushed normally at `999e2c3` on `huy-review`; current execution continues H4 vertical repair/correction, H5 offline/handover and H6 notification runtime preparation. Their foundation verification is recorded separately from production-policy/transaction/HTTP/SQL acceptance. No external client, hosted CI or deployment PASS is inferred.

H4 core migration foundation: actual isolated SQL RED1→GREEN1 verifies19 triggers, model parity and empty H4 downgrade/re-up preserving populated H3 Project. No shared/production database changed. Populated correction transaction/reload/current production authority/UI/KPI/export acceptance remains IN_PROGRESS; foundation118/118 is recorded separately. H5 identity/envelope11 green and explicit acceptance2 red are technical preparation, not offline runtime acceptance.

H4 core populated foundation latest SQL3/3 default-analyzer pass (h4-core-populated-green.trx): null-project legacy source fails closed; raw decision/correction staging, exact terminal item/obligation heads, immutable original decision, hydrated package/history, rollback and populated downgrade refusal. This fixture uses controlled raw decision facts, not current production command authorization. Earlier populated run executed1pass/2fail: null-project scope guard bypass was actual product red; missing fixture PasswordHash was setup failure. Initial missing catalog import/ambiguous Coordinate compile executed0 tests and is not a behavior red. Peer review also found nullable road source bypass, corrected via LEFT JOIN/source existence; exact policy measurement publication source remains under verification. Production correction EF staging/current authorization/replay/concurrency/API and actual reporting consumer are still IN_PROGRESS.

H4 current repair reader actual SQL3 green verifies exact committed correction heads, historical capture retention and revoked project membership denial. Stock consumer5/unit service admission14 are separate from SQL proof. First real correction repository authority SQL5 now passes (different current PM FT, mode-role denials, Supervisor without project membership, revoked normal Supervisor replay/conflict); actual staged EF append/history/heads/package/receipt/audit persist. This is newly unlocked correction evidence, not full H4 acceptance. Actual reporting pipeline1 executed red before its internal producer injection; wire DTO shape and old snapshot bytes remain unchanged. Stored-resource receipt guards, concurrency/fault/Resolved effects, HTTP/export and remaining normal/FT flows are still under implementation.

Latest coordinated boundary: Offline H5 77/77 passes; correction audit catalog executes3 positive reds/3 rejection passes before parser implementation (83 total,80 pass/3 fail), `TestResults/H4H5H6/h5-artifact-green-h6-audit-red.trx`. SQL12 executes10 pass/2 intended reds in `h4-policy-green-history-red-authority-h6-receipt.trx`: H4 production correction authority6 passes including canonical stored-resource authority; exact draft measurement publication now passes; genuine recovery fixture passes. Published decision late evidence insertion and H6 changed-target receipt authority remain actual product reds, now being fixed. Actual reporting pipeline is GREEN, superseding its earlier red; actual DOSSIER admission/snapshot assertions are newly added and not yet verified. These partial checks do not close H4–H7 acceptance or establish rendering/client/deployment evidence.

Subsequent boundary verifies H6 unit61/61 and actual protected inbox SQL9/9 (revoked canonical original receipt target included). `h4-history-export-green-policy-race-red-h6-inbox-green.trx` executes28:24 pass/4 fail. Actual DOSSIER admission uses corrected stock/source revisions while old persisted snapshot JSON/hash remain immutable; late published decision evidence is denied. H4 authority15 has12 pass,2 intended product reds (stale obligation head, same-key concurrent replay) and1 fixture-only owned projection error. Root core4 has3 pass/1 intended late published policy-rule insertion red. Fixes written, not yet verified; rendering/storage/client/external acceptance stays NOT_VERIFIED. New private repair data must not narrow existing legacy global Supervisor reporting access; only the dependent repair dimension remains unavailable without current project source authority, with a regression under execution.

Later focused checks supersede those open correction failures: production correction authority SQL15/15, core SQL5/5, review-request SQL4/4 and correction/request HTTP5/5 pass; exact owned-route cookie5 plus legacy cookie2 pass. Real FIELD notification source SQL3/3 passes after an observed EF query translation failure was fixed. Unit13/13 verifies recipient/file provenance8 and correction source proof5. The next SQL batch executes8 failures: claim3 and canonical-registry2 are intended implementation reds; correction-source3 stop at a fixture-only 200/201 mismatch, before source-proof assertions. Finite history HTTP1 executes an actual numeric-enum wire red. Formatter, history representation ETag, current permission for private historical export snapshots and complete normal/FT producers remain under implementation. No full H4–H7 acceptance is claimed.

Next SQL9 executes8 pass/1 actual private-export authorization red: canonical2, claim3 and correction-source3 now pass; the correction fixtures use correct fresh201. A preceding CA1859 compile attempt executed0 tests, fixed narrowly. Finite history formatter HTTP1 passes while new representation ETag1 executes actual unchanged-token red. After source-permission implementation, `h7-private-export-green-h5-history-h6-envelope-red.trx` executes11:7 pass/4 intended reds. The populated actual Reporting→DOSSIER fixture now verifies private old snapshot Get/Manifest/Content denial without membership, current-authorized old original decision access after correction, revoked membership denial and canonical create replay/conflict denial; snapshot bytes/hash remain unchanged. Other reds are historical offline replay1, FIELD transport/source binding2 and SQL padded notification type1. Unit10 executes6 pass/4 corresponding historical replay3 and distinct transport proof1 reds. Worker rendering/storage delivery and full H4–H7 acceptance remain unverified.

Latest boundary: H4 additive candidate20261006084919 replaces only a preserved NEW UNAPPLIED scaffold after nullable decision/assessment successor refinement. API builds zero errors (142/128 warnings); actual SQL3 now green for model parity,18 enabled producer history/monitoring guards, current-item scope and populated downgrade protection, after actual reds and EF-owned Restrict metadata repair. Applied migrations/data unchanged. Remaining native source guards and full normal/FT producers are not accepted by these checks.

H5 actual importer current-authoritySQL2, artifact2 and finite selector4 pass; H6 unknown-policy5 passes. Latest unit21 has12 pass/9 intended H4 factory/hash/previous-head reds being implemented. H6 dispatchSQL4 and unknown-auditSQL1 remain observed implementation reds awaiting mapped runtime adoption. Report/export cookie8 passes after3 actual admission reds. Historical diagnostic/compile failures remain in spec. Full H4–H7 remains IN_PROGRESS; no external/client/official CRS/deployment acceptance claimed.

Later unit10/10 verifies native factory/hash/correction previous edges. Actual SQL9 has8 pass/1 remaining PRE_EXEC session-purpose red: current repair stock now selects explicit obligation pins, unpinned siblings/unknown legacy inventory report incomplete, genuine Reporter-backed native NORMAL/CONDITIONAL_FT tasks persist. New candidate forward guard preserves H3 source/geometry checks. Session-purpose refinement initially hit normalized trigger DDL spelling; corrected helper is not yet verified. Full producer admission/normal/FT/safety and H5–H7 remain in scope and IN_PROGRESS.

Coherent follow-up boundary: native session forwarding and exact empty downgrade SQL2 now pass; latest SQL13 has6 pass/7 intended implementation reds (six H4 producer transactions and one H6 pending recovery cap). Unit29 executed all29 intended reds before implementations (H4 producer service10, H5 mapping15, H6 deadline proof4). Subsequent unit43 executed32 pass/11 fail: all15 adopted H5 model checks, H4 producer service10, deadline proof4 and retained historical3 pass; new historical binding2/transfer service5 remain implementation reds, and export storage revocation4 shows actual bytes reaching renderer after authority changed during OpenRead. Narrow analyzer-only attempts executed0 tests and are not behavioral evidence. Post-open authorization/disposal fixes and offline runtime fixes await focused green checks.

Separate NEW UNAPPLIED H5 scaffold20261006094905 is additive15 tables, preserves14 existing owned Restrict metadata edges and contains no old FK rewrites. Fresh API build succeeds2 warnings/0 errors. Declared SQL triggers/populated downgrade/actual signed admission are not verified by mapping or scaffolding; migration guard checks are being executed separately. No shared/applied database or migration was changed. H4–H7 full acceptance remains IN_PROGRESS.

Next actual boundary: SQL24 executes20 PASS/4 intended runtime reds. H4 producer transactions6 now PASS after native assignment state was explicitly saved before native task INSERT inside the same guarded receipt transaction; H5 SQL schema2 verifies30 enabled append-only/source guards, parity and empty downgrade, while populated registration mutation/delete/downgrade refuse; FIELD deadline producer2 and claim recovery5 PASS. Signed admission2/device registration2 remain actual NotImplemented reds. Historical replay latest5/5 PASS preserves raw JSON bytes to avoid JsonElement changing DateTimeOffset escaping; transfer service5 and post-storage export revocation4 PASS separately. These controlled storage tests do not establish external storage delivery.

HTTP producer5 executed actual404 reds before controller/DI adoption; root wired the producer interfaces to the same scoped repair repository. Separate NEW UNAPPLIED H6 candidate20261006100156 adds7 scalar notification tables, preserves14 existing owned Restrict metadata edges, no old FK rewrites. Fresh API default build205 warnings/0 errors. Actual mapped dispatcher6 + sanitized unknown audit1 PASS7/7; SQL immutable/scope guards, retry/calendar/worker/full source registry and final integration remain unverified. Technical cancellation-source and exact attached offline payload refinements are being separately scaffolded; no pending actor decision is guessed. Full H4–H7 and final exact-SHA CI remain IN_PROGRESS.


Latest corrected SQL30:20 PASS/10 FAIL,0 SKIP. H6 guards5/retry2/dispatch6/Sup proposal clock1, lifecycle read2, device2 and execution denial2 PASS. H4 canonical CHECK3/transaction2, lifecycle renewal2 and H5 raw ACK/file provenance3 remain observed reds; narrow production fixes await recheck. Unit execution10 PASS/proposal proof3 stub FAIL; HTTP execution3 actual404 FAIL before adoption. Earlier SQL30 setup DDL failure retained explicitly in spec. Full H4–H7 IN_PROGRESS; correction authority confirmed, dependent genuine activations pending, external/deployment NOT_VERIFIED.


Latest SQL31:27 PASS/4 explicit stubs FAIL; H4 execution10 and lifecycle9 PASS, H5 snapshot/resolver4 and H6 ops/unknown4 PASS. Signed sync2 + finite clock paging2 remain implementation reds. Notification assignment proof3 PASS. Device HTTP2 PASS; lifecycle HTTP2 actual404 reds observed before new route/service adoption (recheck pending). H4 Postman producer7 added disabled; static11-request/21-script preservation/syntax PASS, live NOT_VERIFIED. Full H4–H7 not complete.

Subsequent 06/10 runtime checkpoint, separate from H4–H7 package acceptance: `h4-imported-green-h5-sync-h6-binding.trx` SQL20 executed17 PASS/3 FAIL. H4 execution12, H5 signed per-item sync2 with actual typed effect/ACK, lifecycle positive renewal1 and H6 normal proposal/assigned dispatch2 PASS. H6 current binding revocation was a genuine dispatch error; H5 Reporter BEFORE reuse theory2 initially stopped in fixture EF-owned projection. After an AsNoTracking fixture correction, one BEFORE case reached the genuine resolver `evidence_access_forbidden` red while ended source correctly denied. Exact approved-decision/current Reporter evidence/version/current Defect source validation now passes, preserving actual Reporter file owner/uploader in signed H5 provenance. H6 assigned recipient authority checks current RepairItem binding and obligation at delivery, so current-binding removal cannot deliver using a still-active native assignment. Focused SQL4/4 verifies both branches and the valid assignment case; former SQL20 failures are historical, not currently open. H6 calendar pure admission began with seven NotImplemented behavior reds and now passes unit7/7: only exact continuous same-run scheduled callback before the next weekly period is ready; missed/uncertain periods remain pending owner catch-up policy. It is not durable scheduler-run provenance. Lifecycle bearer/cookie HTTP3/3 PASS. H5 `POST /offline/sync` typed route and shared scoped FIELD/offline validator are now wired; focused H5/lifecycle HTTP6/6 PASS covers route/key/CSRF admission. Positive signed-sync HTTP and full H5 handover/upload remain NOT_VERIFIED. Default-analyzer final build, self-reviews, full H4–H7 vertical acceptance, and exact-SHA CI still required.

H6 delivery-role refinement: actual deadline/FIELD/Repair source SQL10/10 passes with source-proved recipient role retained through delivery. A new current-role SQL fixture first stopped at the Identity EF direct-role guard (fixture setup only), then used controlled SQL current-state change and passed1/1: stale PM membership plus current Supervisor role/membership does not deliver the PM deadline notification. Identity role-change command authority is not inferred from this controlled fixture. H5 Reporter source recheck includes H3's approved supplement evidence path; original/denial and ordinary verified-file focused SQL checks passed before the dedicated supplement fixture below. Full H4–H7 work remains in progress.

H5 source coverage follow-up: the same SQL fixture now creates a real Report supplement evidence reference and exact PM reuse decision. Four actual SQL cases PASS for original/supplement by active/ended Defect source; original owner/uploader are retained and ended source is denied. This supersedes the prior supplement-specific test gap only. H5 full handover/upload/reconciliation and positive signed-sync HTTP remain IN_PROGRESS.

H4 normal formal intake follow-up: actual H3 root `REPAIR_CLAIM` and original PM-review clock are consumed in the same guarded producer transaction. One immutable RepairAttempt/RepairAttemptSubmissionLink is appended; incomplete intake remains a claim awaiting review, not effective confirmation. Focused SQL1/1 PASS tests exact original Crew/project/task/binding/start/finish/hash, fresh/replay/key conflict, revoked-membership replay, one attempt/clock and H7 current-stock projection. The H7 reader requires the exact current item/attempt/link/root submission/review-clock/finish chain before showing `REPORTED_AWAITING_REVIEW`; absent or conflicting pins fail closed. Execution HTTP admission4/4 PASS. A subsequent coordinated H4/H7 SQL regression32/32 PASS, 0 SKIP. Positive HTTP attempt submission, supplements, PM review, final confirmation, FT eligibility and full H4/H7 package acceptance remain IN_PROGRESS; no external client acceptance is inferred.

Bound repair supplement follow-up: generic H3 PM review returned actual409 `repair_binding_required` for a native repair task, so the H4 PM command now owns the SUPPLEMENT request and H3 native status transition atomically. It appends H3/H4 review provenance without declaring actor receipt; Crew48 origin remains PENDING. Crew supplement consumes the next immutable H3 revision, proves exact parent/root/actor/first-start/claim/current binding and prior review, appends a link to the original attempt and keeps the original PM-review clock/due. The effective H7 reader verifies root and predecessor before showing current stock. Focused SQL1/1 and HTTP6/6 precondition routes PASS after fixture correction; wrong-role/project, replay, different task path, duplicate command and revoked actor branches are included. PM sufficient-evidence acceptance, normal Supervisor/FT PM final, positive HTTP and full H4/H7 acceptance remain IN_PROGRESS. The prior H3 review409 is retained as a source-boundary finding, not a pending business choice.

Normal PM acceptance/final extension: H3 repaired submissions retain exactly `AFTER_ATTEMPT_BINDING_UNAVAILABLE` even with required measurement, checked location and verified AFTER because H3 cannot authenticate H4 physical finish. The controlled fixture initially retained `REQUIRED_MEASUREMENT_MISSING` from an Area/type4 declaration against required DepressionDepth/type1; fixed the fixture rather than relaxing the product rule. H4 PM `ACCEPT` now requires exact current attempt/link/finish, that sole H3 missing reason, declared/link identity, current verified file/version, actual AFTER capture and file upload after physical finish; the evidence snapshot belongs to the append-only review, not a rewrite of the attempt. Normal acceptance completes original PM-review24 and starts Supervisor-final48 at the PM review event. Current project Supervisor final appends decision and resolves the mandatory obligation/package in one receipt/audit transaction, without cascade Defect/Case close. Focused positive SQL1/1 PASS covers SUPPLEMENT twice, actual H3 revision3/verified file, PM acceptance, normal Supervisor final, role/project denials, replay/conflict/revoked-role admission, clock origins/due, H7 current stock and immutable root/attempt. HTTP final/precondition routes7/7 PASS; positive HTTP and broad race/rollback, FT sourced eligibility and full H4/H7 acceptance remain IN_PROGRESS. The initial accepted-review 400 was the expected implementation red; compile-only test enum/namespace failures were fixture errors, not behavior failures.

Actual normal producer→correction→H7 consumer extension: the same SQL scenario creates a real PM-reviewed/Supervisor-confirmed normal decision, captures ReportingService and DOSSIER before correction, then executes the adopted current-Supervisor correction against that exact decision. Focused SQL1/1 PASS verifies obligation/package reopen, H7 current stock and reporting metric `UNREPAIRED`, new DOSSIER manifest referencing the correction head, and unchanged original decision/FIELD submission/attempt/pre-correction export JSON+hash. This supplements the older controlled historical decision fixture; it does not establish rendered object storage or external delivery. The post-source-change H4/H7 SQL regression32/32 PASS, 0 SKIP; API precondition7/7 PASS. Default-analyzer API build initially found CA1862 in new checksum comparison, fixed using ordinal case-insensitive comparison; repeat build PASS with 0 errors/209 warnings. `git diff --check` remains to be rerun after these edits.

Partial normal-tranche self-review pass 1 (authority/state/privacy/time/transaction): checked current PM/Supervisor/Crew, stored resource replay, original PM clock, physical finish and verified AFTER. Fixed PM receipt task-path mismatch and cross-revision evidence version/reuse provenance. Pass 2 (contract/integration/migration/history/inventory): checked immutable root/link/decision/export and H7 effective head; found open H6 notification source/outbox for review/final, offline-attached AFTER positive acceptance, positive HTTP, FT source mapping and race/rollback coverage. These are actual partial-tranche reviews, not the two final H4 package reviews. H4 remains IN_PROGRESS; no package commit/push or external review is claimed.

H4→H6 root submission notification follow-up: the actual guarded intake now writes one immutable `SUBMITTED` lifecycle event and `repair.work.submitted.v1` outbox in the same transaction as attempt/link/audit/receipt. H6 adapter checks transport identity, item/Defect/obligation/mode, binding, original Crew/attempt, H3 root/hash/time and the original PM-review clock before fanout; current primary PM membership/role is still checked at delivery. Focused actual SQL1/1 PASS delivers exactly one PM notification; existing proposal/assignment/correction/deadline repair-source SQL7/7 PASS; default analyzer API build PASS 0 errors/214 warnings. This is not proof of PM supplement/final notification, weekly scheduler/run provenance, external push or full H6 acceptance. The earlier missing-outbox `SingleAsync` red and compile-only local-variable collision are retained as implementation evidence, not owner choices.

H4→H6 normal final-review source follow-up: actual PM `ACCEPT` appends one immutable `PM_REVIEWED` lifecycle event and `review.supervisor_required.v1` outbox in the same H3/H4 review transaction. H6 verifies transport, current item/binding/link, H4 accepted review, matching H3 field review and still-open Supervisor-final48 clock before delivery to current Supervisor. The actual normal H4 SQL scenario PASS1/1 includes committed Supervisor delivery and a late-source `REJECTED` after final confirmation completes that clock. Existing H6 repair source regression SQL7/7 and default-analyzer API build PASS (0 errors/214 warnings). First build command used a nonexistent API csproj path and executed no compilation; corrected `RoadGuardSystem.eAPI.csproj` build is the evidence. This establishes neither Crew supplement actual-received protocol, external push, durable calendar worker nor full H4/H6 package acceptance.

H4→H6 supplement request follow-up: first focused actual H4 SQL run failed at missing `REWORK` source `SingleAsync`; the implemented PM `SUPPLEMENT` transaction now appends immutable `REWORK` event and `repair.work.rework_requested.v1` outbox alongside H3/H4 review and audit. H6 checks matching H3/H4 supplement review, current item/link/task, unresolved obligation and active assigned Crew binding before fanout. Focused normal producer SQL1/1 PASS proves one Crew delivery and zero CrewSupplement clock rows. It also proves a later queued `REWORK` source is `REJECTED` after Crew's next linked revision, without duplicate Crew notification. H6 repair-source regression SQL7/7, default-analyzer API build PASS (0 errors/214 warnings). Notification/inbox persistence is not actual receipt; Crew48 origin remains PENDING pending owner receipt protocol. External push and full H4/H6 package acceptance remain NOT_VERIFIED.

H4 unit suite follow-up: first unit command compiled zero tests because the older `Recording` fake lacked four new `IRepairExecutionRepository` methods. Fake updated and four normal continuation service-admission cases added; one DTO constructor-order compile error also executed zero tests and was corrected. Focused Repairs unit195/195 PASS. Full unit suite executed873:871 PASS/2 FAIL, both architecture source-boundary tests on pre-existing HEAD violations (Service `MatchingCandidateSnapshotReader` references DbContext; Repository `CaseWorkflowRepository` imports DTO). HEAD already has37 Repository source files referencing DTO; working tree has68 under the same pattern, so this rule remains materially unresolved, not a passing gate. No architecture test was suppressed or changed. These reds are independent of business authority, and no full H4–H7 acceptance is claimed.

Follow-up H4 producer/execution/source SQL22/22 PASS after the supplement outbox addition; `git diff --check` exit0. This is not full H4 package completion, and the 2 full-unit architecture reds remain open.

H5 positive bearer device HTTP follow-up: actual API/DI/SQL case PASS1/1 covers Crew registration201, exact same-key replay200, revoke201/replay200, read200, exactly one persisted registration/revocation/audit, then current membership termination with read and revoke replay403 under the same still-valid bearer token. This is device command evidence, not positive signed-sync HTTP or complete H5 handover/upload/reconciliation acceptance.

Combined H4/H5 HTTP admission regression17/17 PASS after the positive device case; `git diff --check` exit0 and branch remains `huy-review`. Current source/tests are uncommitted and no H4–H7 package checkpoint, PR, external review or deployment is claimed.

Continuation reservation: root owns H4 policy/normal/safety/retention verticals, shared DI/config/DbContext/new migration/contracts/Postman/spec and single build slot. H5 writer owns only Offline source/tests; H6 writer owns only Messaging source/tests/new worker; architecture writer owns new H7 SQL-reader separation and narrow lifecycle/export seam mapping. No writer commits/pushes; root checkpoints after integration. Existing work is preserved. Architecture corrected exact baseline:36 Repository C# files (37 included permitted csproj),3 Services readers/6 forbidden-token pairs; working initially adds32 Repository files and1 Services reader/2 pairs. New H7 Service reader is being split into internal Repository facts plus Service wire mapping; broad baseline refactors and test weakening excluded. PM policy runtime uses internal Repository contracts and the local wire freeze above; owner PM policy authority remains TARGET_CONFIRMED, implementation tests still under execution.

H5 private snapshot follow-up: `GET /offline/snapshots/{id}` returns the immutable create-view only to the original Crew while project/actor, source device and exact assignment remain current. Focused SQL2/2 verifies active read with unchanged content hash, denial to a different PM actor and denial after the assignment ends; the separate ended-assignment create branch is still denied. This is not full geometry/evidence download or encrypted handover acceptance.

H5 same-owner device revocation follow-up: typed POST `/offline/devices/{id}/revoke` retains the old register/snapshot fingerprints, binds device ID into its own receipt fingerprint, and rechecks current actor/role/project/exact registration at fresh/replay/conflict. Revocation/audit/outbox/receipt share one transaction. Focused isolated SQL1/1 passes wrong PM actor403, owner fresh201, identical authorized replay200, new-key duplicate409, one durable revocation/audit, subsequent snapshot and signed sync403, and revoked Crew membership replay403. This is self-revocation by the currently registered actor, not guessed PM authority over another actor or complete H5 encrypted handover.

The revocation fixture was extended to an independently registered second device and original snapshot: same key/reason with changed registration yields409 without revoking the second device; the first device's immutable snapshot read is denied after revocation. Focused SQL1/1 remains green. A fresh default-analyzer `RoadGuardSystem.eAPI.csproj` build succeeded0 errors/214 warnings; the first command used a nonexistent guessed csproj path and executed no build, then was corrected. Warnings were not treated as failures or full regression acceptance.

Continuation review06/10: architecture accounting previously saying full-unit failures were solely pre-existing is HISTORICAL and superseded. Correct baseline is36 Repository C# files and3 Services readers/6 forbidden-token pairs; initial worktree added32 Repository files and1 Services reader/2 pairs. Rules/tests remain unchanged. H7 SQL reader, lifecycle/export authority and H4/FIELD new seams have explicit internal facts and Service mapping; H5 mappings remain in progress. Exact final counts and fresh suite results must be recorded before acceptance.

Root integrated protected H6 inbox/operations/source/dispatch/worker DI. Policy SQL meaningful RED: UPDATE of immutable draft-change after staging insert with missing shadow DraftId; fixed by binding DraftId before first insert, not relaxing append-only trigger. Fresh rerun pending current writer freeze. Calendar planning additive migration20261006155824 adds only PlannedAtUtc, preserves existing planned rows as PENDING_POLICY, strengthens source/run/planning/final-observation immutability and refuses populated downgrade; no existing migration edited.

Reservations extended: architecture writer owns NEW H4 cancellation/normal continuation lifecycle files and tests; safety writer owns NEW RepairSafety repository/service/controller/tests plus narrow monitoring verified-evidence/current-head methods. Root owns policy/Files upload/retention, shared schema/DI/contract/Postman/spec and sole build slot. Safety setup derives from already approved/assigned normal work, never arbitrary PM Crew appointment. Extra grant/substitute authority remains exactly gated. H5 upload uses separate signed-admission identity operation and same-context file/session/capture receipt; actual source/uploader facts stay separate. Retention adds persisted repair attempts, superseded correction evidence, assessments/safety and Offline capture/admitted/package file inventories. No retention deletion or new duration/period/allocation policy is introduced. All changes still require focused verification and two reviews.

H4 eligibility source-read technical freeze: current PM/Supervisor or exact currently assigned Crew GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/eligibility`. Returns pinned binding/policy/checklist/assessment measurement IDs/original authorization time and expiry plus actual existing Warranty project/road scope, dates, HandoverDocumentId/SourceDocumentId and project HandoverDocument metadata. It explicitly returns eligible=false, OWNER_SOURCE_ACTIVATION_PENDING, UNKNOWN_OWNER_MAPPING and individual missing facts; project handover does not become road handover. ETag hashes captured source facts and current pending reasons; it is not a command rowversion or grant. No survey accuracy, maintenance scope or aggregate measurement decision is inferred. `RepairSafetyCheckInput.evidenceIds` means actual current FIELD MEASUREMENT evidence-link IDs, resolved under same task/assignment/project and verified source file; it does not accept arbitrary naked file GUIDs. `safety.measure_assigned.v1` records actual allowed temporary-safety setup from35A and notifies current Supervisor without mislabelling it danger or inspection-due. SafetyActionSource is additive immutable event/provenance, not new command authority.

Continuation verification06/10 (IN_PROGRESS, not handoff): current HEAD remains999e2c3d; all authorized dirty work preserved. Fresh default-analyzer API build0 errors/210 warnings after lifecycle/policy/eligibility/safety/Offline DI integration. Compile-only reds (nullable/definite assignment, LoggerMessage/options analyzers, fixture imports) executed no tests and were fixed without suppression. H4SafetySourceAdmission is a NEW UNAPPLIED additive candidate: nullable actual uploader (historical UNKNOWN), unique safety-obligation monitor, immutable scoped action sources, Kind10 first-check due may equal installation origin, populated downgrade refusal. Generator's14 owned-FK metadata omissions were repaired in latest snapshot/designer;56 spurious FK drop/add operations were removed, retaining actual existing Restrict behavior. A processing encoding mistake changed snapshot m² constraint; exact original Unicode restored, no unit policy change. H4 producer/model migration test subsequently passed in the21-case tranche.

Fresh SQL46:36 PASS/10 FAIL/0 SKIP. Expanded PM policy rollback and concurrent publish, H5 signed sync/export/grant/import/capture/create/multipart resume/complete/replay/current-right guards and H6 weekly callback/restart cases pass. Nine lifecycle failures were a test fixture stale task ETag after accept; one model failure was the Unicode snapshot issue above. Fresh safety/execution/modelSQL21:19 PASS/2 FAIL/0 SKIP; meaningful runtime safety-source trigger rejected insertion before parent installation UPDATE. Fixed SaveChanges sequencing inside the same transaction, preserving factual trigger. Retention assertion exposed original incomplete attempt has no direct evidence; adopted derived inventory from actual immutable attempt-submission links to FIELD evidence, without rewriting original attempt. Next SQL25:22 PASS/3 FAIL/0 SKIP; safety install/due and actual normal correction→KPI/new-export/old-snapshot retention pass, remaining3 were detached test fixture shadow rowversions, fixed by reading actual database values. These failures remain historical; latest integrated reruns are required.

Fresh focused HTTP11:10 PASS/1 FAIL/0 SKIP. Policy positive publish/revoke/current-membership, lifecycle/safety admission and protected scoped notification clock HTTP pass. H5 positive signed-sync/device/package export reaches grant creation but receives403; diagnosis remains active, no handover HTTP PASS claimed. Static Postman preservation againstHEAD preserves every existing folder/object/ID/environment value;35 new independently opted-in requests use exact external body/source/token/ETag variables without guessed defaults. All91 additive scripts parse. Documentation/agent guidance validators PASS; CI workflow and12 negative fixtures PASS. Live Postman/client execution remains NOT_VERIFIED.

Self-review findings addressed: new Repository DTO dependencies removed to exact36-file baseline; H7 SQL reader moved to Repository/internal facts with explicit Service mapping; immutable Warranty snapshot SourceDocumentId was missing from retention and added; actual Crew rejection previously prevented later PM cancellation/normal succession, now source-matched rejected-before-start recovery preserves original rejection/end. R15's submitted alternative is new submission OR task; current append-only same-task supplementation is adopted, no unsolicited submitted-next-task schema. H7 effective correction/obligation-current-item chain and immutable export-source authority received a read-only integrated review; this is Codex self-review, not external GitHub review.

Continuation verification07/10, still IN_PROGRESS: full unit `huy-final-full-unit.trx` executes909 cases:907 PASS,2 FAIL,0 SKIP. The two unchanged architecture assertions report `Services/Implementations/Defects/MatchingCandidateSnapshotReader.cs` RoadGuardDbContext dependency and `Repositories/Implementations/Cases/CaseWorkflowRepository.cs` DTO dependency. The complete Repository DTO-source set is36 files at HEAD and36 working, identical; initial newly introduced violations have been removed within owned seams. Tests/rules are unchanged and the full-unit gate remains FAILED_BASELINE, not PASS. Initial subsequent unit/integration attempts stopped during compilation (options-cache CA1869 and fixture CS1012); they executed zero tests, and fixes retain default analyzers.

Fresh EF CLI `migrations has-pending-model-changes --no-build` using Repositories as project/startup and ROADGUARD_MIGRATION_CONNECTION_STRING targeting a local design-only database reports no model changes, exit0, with existing EF tool8.0.0/runtime8.0.17 and enum-sentinel warnings; no database writes or upgrade. An earlier wrong Seeder path MSB1009 executed no check. Fresh CI-workflow, documentation and agent-guidance validators exit0; historical requirement count remains148. These checks do not imply hosted CI/deployment acceptance.

Integrated review07/10 found two genuine H4/H5 source-boundary defects: predecessor ResourceVersion could cross FIELD-task/repair-item resources, and signed REPAIR_ASSESSMENT capture could not resolve recipient-uploaded evidence. H5 now selects nearest unambiguous same-resource ancestry; H4 accepts exact trusted admission facts for assessment evidence while retaining signed payload/hash and recording actual uploader separately. Actual signed recipient-upload SQL/HTTP and broad affected regression remain required before acceptance; no passing claim from this source fix.

Cost-stop closeout07/10: owner asked to stop all new implementation, optional test expansion and further review. The currently running affected SQL tranche completed177 cases:173 PASS,4 FAIL,0 SKIP. Two failures are unchanged HEAD baseline tests: `H0RetentionMigrationTests.Fresh_and_populated_Huy_upgrade_preserve_history_and_model_parity` seeds a Session through the current EF model after migrating to an old schema without HEAD's `IssuedRole`/`Lifecycle` columns; `P202MigrationLifecycleTests.Migration_AppliesDowngradesAndReapplies` asks to downgrade through HEAD's `20261002100000_Anh01RequestScopeRootCorrection.Down`, which always throws51027 by design. Neither test or its implicated HEAD migration/model was changed in this H4–H7 worktree. The two worktree regression failures were H4 correction migration fixture reading current columns after a partial populated downgrade and safety ACK inserting a second breach when its clock was loaded without prior breaches. The H4 fixture now restores current schema after proving populated core Down refused; focused H4+Safety rerun executed2 with H4 PASS, Safety FAIL on duplicate breach. Safety ACK now loads existing breach history; final focused rerun is pending. These exact failures are retained rather than calling the177-case suite PASS.

Closeout regression result: the second focused safety SQL run passes1/1/0 after loading the existing breach collection in the safety ACK command. The preceding two-case rerun passes H4 populated-downgrade/current-reader fixture1 and exposes the then-unfixed safety duplicate1; it is not a two-case PASS. Current source has both in-scope regression fixes, each with its own fresh focused SQL PASS. The already completed177-case broad run remains recorded as173 PASS/4 FAIL, with the two HEAD fixture failures unresolved baseline. No new optional suite was started after the cost-stop instruction.

Normal cost-stop H4–H7 source checkpoint: `huy-review@d6ec8041c346d3836b41c80d30290c70be263e3e`, from H3 `999e2c3d2cd1912eb3f775164f099c4ffde694bf`; 343 source/contract/Postman/CI/test paths committed, including only new unapplied migration candidates and model snapshot. Staged `git diff --check` exits0 after four owned EOF whitespace fixes; no applied migration or other branch changed. This assigned spec and summary are delivered in a follow-up normal documentation commit. The combined source checkpoint is partial H4–H7 acceptance; pending owner/source activations and external NOT_VERIFIED evidence remain gated as recorded above. Hosted exact-final-SHA CI outcome is not asserted by local tests.

H5 positive bearer HTTP now executes1/1 PASS through signed FIELD sync, encrypted package export, Supervisor grant, recipient endorsement/import and durable original Crew effect. Its earlier403 was a test fixture that left the HTTP bearer as Crew when registering the supposed PM recipient device; explicit PM login repaired the fixture and the production grant guard is unchanged. H5 normal signed assessment/start/finish runs inside the affected SQL suite; claimed offline time remains UNCERTAIN. The signed recipient-uploaded assessment source fix has no finished dedicated SQL/HTTP acceptance under the cost stop; classify it NOT_VERIFIED, not complete. H6 source/worker and H7 correction-aware reporting/export/retention successes are subsets of the affected suite; external client/storage/worker deployment remain NOT_VERIFIED.

Independent completeness audit remains active: H5 actual downloadable geometry/pinned policy source payload and same-core signed normal repair bridge/time uncertainty; complete confirmed-clock H6 producer/source adapters and genuine FIELD safety evidence/warning/ack SQL. No obsolete correction authority or generic lifecycle pending gate blocks those. Bounded committed-upload continuation is a technical design inside existing file/session expiry/current-authority, not a new grant or reset; expired grants deny new admission. Pending owner/source ledger remains grouped, no repeated owner question.
CI-closeout transfer checkpoint 07/10/2026: owner redirected this chat to a clean local/remote checkpoint for the next Codex chat, not full H0–H7 handoff. Previous pushed SHA `67147bd7d90185e41fc10241f093ee9f98e4538d` had hosted CI failure: source-contract format, unit907 PASS/2 baseline architecture FAIL, API361 PASS/19 FAIL/1 SKIP, SQL605 PASS/43 FAIL. Current uncommitted closeout fixes restore the original HUY repair inventory `Complete=false`/`HUY_REPAIR_REFERENCE_UNAVAILABLE`, verify recipient-uploaded H5 evidence before assessment import in the SQL fixture, and give old HUY02/P2/RF1009/cookie inbox fixtures actual project membership/source. Production H6 denies unknown protected history as intended. Fresh focused SQL2/2 PASS; affected API47:44 PASS/3 FAIL/0 SKIP. The three API failures predate H4: H1 session-expiry fixture conflicts with `CK_Sessions_ExpiresAt`; H2 SurveyPlan fixture duplicates `UX_RoadSegmentSets_CurrentPublished`; P112 WorkPackage asserts legacy `application/json` against earlier problem+json response. Full unit909:907 PASS/2 unchanged baseline architecture FAIL. Default-analyzer solution build and EF pending-model check exit0. `dotnet format --verify-no-changes` scoped to all assignment-changed C# paths exit0; full-solution format exit2 exclusively on130 unchanged files, none in H4–H7 diff. Git diff check, normal commit/push, remote SHA confirmation and hosted new-SHA CI are pending this transfer checkpoint. These residual baseline CI gates are not worktree-introduced PASS claims; H4–H7 full acceptance remains open, and all owner/source gates above remain PENDING/NOT_VERIFIED.

## Technical closeout resume at a56f767 (07/10/2026)

CURRENT_VERIFIED preflight: receiving branch `huy-review`, local HEAD/upstream/remote `a56f7672d0f77fa62eb5b48e2ff8851c5ccc67f0`, remote `anh-review` `5089c3267dcdf60645ab34f61b58a79e3cbb0cf6`; clean tracked/untracked status, one listed worktree, no other visible `dotnet` writer. Huy remains the assigned shared writer; process inspection cannot prove an unseen writer is absent. H0 `1a442f5`, H1 `ae2b289`, H2 `b954166`, H3/pre-H4 `999e2c3`, H4–H7 `d6ec804`, docs `67147bd`, closeout fixes `a56f767` are comparison checkpoints, not reset targets. This section supersedes the preceding transfer paragraph's uncommitted/pending-push/hosted-CI status; its earlier commands and failures remain historical evidence.

CURRENT_VERIFIED hosted [a56 run 37509532426](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/37509532426), attempt 1, exact head `a56f767`: SQL job `112426763812` completed 607 PASS/41 FAIL/0 SKIP (648); API `112426764170` 377 PASS/3 FAIL/1 SKIP (381); unit `112426764211` 907 PASS/2 FAIL/0 SKIP (909); source-contract job `112426764343` passed its validators, then failed whole-solution format on unchanged files. The same SQL run explicitly PASSed `H5OfflineRepairSqlTests.SignedHandoverAssessmentResolvesRecipientUploadWithoutRewritingDeclaration`; that resolves the stale dedicated-SQL NOT_VERIFIED statement above for this exact mock-storage fixture only. It does not prove live storage, client or deployment. Earlier `67147bd` results (SQL605/43, API361/19, unit907/2) remain historical and are not added to the a56 counts.

Failure ledger below uses pre-H4 `999e2c3` as the comparison. Every row is a56 run/attempt 1 unless marked otherwise. `BASELINE` means not introduced by H4–H7, including stale tests that were already incompatible with H0–H3. Source comparisons are `git diff 999e2c3 a56f767`; passing source validators or a failing test alone does not establish business acceptance. No applied migration or authority guard is weakened to satisfy historical fixtures.

| Exact failing identity/parameters | Root cause and pre-H4/source evidence | Category; narrow action |
|---|---|---|
| 13 migration tests: `P2-30 supplementary request`, `P2-20 Project schema`, `FileRepositorySqlTests.MigrationLifecycle_FromP210`, `P2-30 dataset and quality schema`, `P2-22 survey planning`, `P2-10 Negative orphan AuditLog`, `P2-10 Positive identity migration`, `P1-22 planning contract`, `P2-21 road and warranty`, `P2-30 flight and survey file`, `P2-23 survey assignment`, `P2-02 Positive`, `P2-31 processing` (each named `downgrades and reapplies` or equivalent in the CI log) | All reach unchanged `20261002100000_Anh01RequestScopeRootCorrection.Down` and its unconditional 51027 preservation refusal, present at 999e2c3. | BASELINE; retain guard and record historical test assumptions. |
| 12 SQL cases with `Invalid column name 'CrsProfileRevisionId'`: `P2-30` confirmed-manifest immutability, dataset/quality round-trip, invalid confirmation; `P2-32` schema backstops, detection/defect/task/log round-trip, duplicate detection rollback, missing verifier rollback, changed-key conflict, retained-key/raw-payload immutability; `P1-22` plan/request/postpone commit; `P2-23` cancellation rollback, invalid status/duplicate assignment | Old fixture schema/seed uses the pre-H2 shape with the H2 model's CRS column. These test/source paths have no H4–H7 delta; H2 is in 999e2c3. | BASELINE; no invented CRS profile or schema rollback. |
| `H0RetentionMigrationTests.Fresh_and_populated_Huy_upgrade_preserve_history_and_model_parity`; `P222SurveyPlanModelTests.Model_MapsSurveyPlanAndAppendOnlyPostponement` | Old-schema Session seed uses current H1 `IssuedRole`/`Lifecycle` model; SurveyRequest expectation has 5 FKs while the pre-H4 model has 7. Both test files are unchanged. | BASELINE; historical fixture compatibility, outside closeout fix scope. |
| `Rf06aSchemaInventoryTests.MigratedSchema_MatchesModelAndSnapshot_AndWritesInventory`; `Rf09TransitionRehearsalTests.OwnedMigratedSql_BackfillResumeBoundaryAndBackupRestore`; `Rf09TransitionRehearsalTests.FilesCandidateWidening_RequiresNewReaderBeforeLargeWrite` | Unchanged scanner fails to locate `TR_DatasetAssessments_Immutable` in unchanged Anh migration; the two unchanged rehearsal assertions expect `int` and an `InvalidCastException` after pre-H4 `bigint` widening. | BASELINE; preserve historical tests and migration source. |
| `P2-32 migration downgrades and reapplies the detection schema`; `V2P1063MigrationUpgradeTests.Upgrade_FromRoadSegmentBaseline_PreservesTaskOnboardingAndRoadSegments` | Pre-H4 defect-concurrency Down refuses a populated graph; pre-H4 H3 FieldInspectionTasks trigger makes the old test's `OUTPUT` without `INTO` invalid. | BASELINE; retain immutable/guarded schema. |
| `UploadPersistenceSqlTests.UploadFlow_ReplaysCreateRejectsStaleVersionAndVerifiesCompletedObject`; `UploadPersistenceSqlTests.Anh01_LargeMetadata_ResumeAndExpiredReceipt_DoNotCreateAnotherMultipart` | Unchanged legacy fixture creates project and DroneOperator without an active membership/assigned survey task. First returns `NotFound` at `GetPartUrlsAsync`; second gets zero successful initializers. Direct-upload `GuardMultipartAsync` membership/survey checks exist at 999e2c3; H5's new branch is conditional on an actual offline capture. | BASELINE; preserve current-authority guard. |
| `IdentityPersistencePositiveTests.RefreshToken_RotationAndFamilyRevocation`; `MultipartRecoveryMigrationTests.FreshAndBaselineUpgrade_PreserveLegacyClaimAndFileIdentity_NoPendingModelChanges` | The unchanged H1 fixture reloads a second tracked UserSession. Multipart test uses dynamic `migrations[^2]`, which at pre-H4 H3 already points after `20261003170000_Anh01MultipartRecovery`, so its expected missing `MultipartFence` is false. | BASELINE; no change to H1 identity runtime or applied migration. |
| `P2V2SurveyScopeConcurrencyTests` three `P2 V2` cases: `dataset cannot claim a segment outside its assigned scope`, `dataset cannot use a verified document as a survey video`, `assigned scope and verified survey video persist once on replay` | CI gets `ScopeIncompatible` before old Conflict/Success assertions. Dataset admission and these tests are unchanged since pre-H4; actual scoped source is required. | BASELINE; do not relax authority for a historical expectation. |
| `H2NativeMigrationTests.Fresh_and_populated_upgrade_preserve_legacy_geometry_receipts_and_model` | Unchanged test compares full migration history to H2 latest; 999e2c3 already includes H3 after H2, before H4–H7 were added. | BASELINE; retain migration history. |
| `IdentityPersistenceNegativeTests.RefreshToken_ControlledCompetingUpdate_LoserReceivesStaleConcurrency` (display `P2-10 Negative: Controlled competing refresh token rotation causes loser to receive StaleConcurrency`) | Hosted execution timed out. Paired isolated `dotnet test --filter FullyQualifiedName~RefreshToken_ControlledCompetingUpdate_LoserReceivesStaleConcurrency` on pre-H4 `999e2c3` and current `a56f767`, same Testcontainers SQL Server 2019-CU18 image, each discovered 1 and failed in 20 seconds with the same `System.TimeoutException` under EF `DbUpdateException`. TRX: `tests/RoadGuardSystem.IntegrationTests/TestResults/closeout-refresh-preh4.trx` and `closeout-refresh-current.trx` (ignored). | BASELINE; do not weaken current H1 concurrency guard in this H4–H7 closeout. |
| Three API failures: `Huy02NotificationApiTests.SessionRevocationOrExpiryRejectsCommittedReplayAtRequestBoundary(expire: True)`, `IdempotencyPerCommandCharacterizationTests.SurveyPlanCreate_SameKeyDifferentPayload_ReturnsConflict`, `P112ProjectAuthorizationTests.WorkPackage_Unauthenticated_ReturnsStableUnauthorizedProblem`; two unit architecture failures (`MatchingCandidateSnapshotReader` DbContext, `CaseWorkflowRepository` DTO); 130 full-format paths | Unchanged H1 expiry, H2 published-set and pre-H4 problem+json assumptions; unchanged source-boundary violations; full-format diagnostics on unchanged C# files, while assignment-owned format passed at a56. Previous and a56 CI show the same API/unit residual identities. | BASELINE; visible CI failures, no broad formatting or unrelated refactor. |

The 41 SQL failures classify as 41 BASELINE after the paired timeout reproduction; 0 newly reproduced H4–H7 regressions in this hosted run. The 607 passing SQL cases include H4 correction/safety and the H5 recipient-upload assessment, but do not prove every independent H4–H7 acceptance path. Earlier introduced H4 populated-downgrade fixture and safety duplicate-breach defects were fixed at a56 with focused resolving tests as recorded above. Two closeout self-review passes at this checkpoint: (1) current authority, privacy, signed upload provenance, obligation/correction effects, immutable clocks, transaction/replay/concurrency and recovery were checked against failure paths; direct-upload failures still hit pre-H4 guards, and the timeout was paired at both SHAs. (2) old wire/hash, migration/history/model, source adapters, effective reporting/export snapshots, retention and contract evidence were compared; no new runtime fix was justified, and whole-solution formatting remains an unchanged-file gate. This is Codex self-review, not external GitHub/client review.

The timeout pair used fresh builds and separate fixture-owned disposable SQL databases; both commands exited 1, with no skipped or zero-discovery case. The first long-path detached checkout attempt failed before testing and was removed by Git; a short `D:\rg-h3` detached checkout succeeded and was removed after its TRX was preserved. The receiving branch/source was never switched or reset.

Next action is to complete the assigned independent H4–H7 acceptance/completeness audit using existing required paths, including the open H5 payload/signed normal repair and H6 clock/safety producer evidence named above. Record any actual implementation gap outside this regression-fix scope without silently opening a new feature. H4–H7 full-package independent acceptance remains NOT_VERIFIED; localized owner/source gates above remain PENDING without re-asking. Verdict at a56: TECHNICAL CLOSEOUT INCOMPLETE. Do not claim a fully green CI or the owner-activation verdict from this evidence.

## Finite independent acceptance audit at 24cd32b (07/10/2026)

CURRENT_VERIFIED preflight: clean `huy-review` at local/upstream/remote `24cd32bd9159262ac58776586e134c9033de7d80`; parent `a56f767`, documentation-only delta, one worktree, no visible active `dotnet`/Git writer. Huy remains the named shared writer. Docker/SQL test image and `dotnet` are available, but no existing passing test was rerun. Hosted [run 37513320669](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/37513320669), attempt 1, SQL job `112439798332`, executed 648: 607 PASS/41 unchanged pre-H4 baseline FAIL/0 SKIP. API377/3/1 skip, unit907/2/0 skip and whole-solution format failure also remain unchanged. The SQL failure identity set equals a56's set; no H4–H7 regression is reproduced. Because 24cd32b changed docs only, its exact-SHA SQL PASS identities are valid evidence for the unchanged runtime/tests. No new local test result or external review is claimed.

Bounded acceptance matrix (all hosted PASS citations below are `24cd32b`, run 37513320669 attempt 1, SQL job 112439798332; inspected assertions are in the named test source at that SHA). `PASS` means only the stated assertions, not a whole package or deployment:

| Requirement | Producer/source path | Existing test and relevant assertion | Status |
|---|---|---|---|
| H5 downloadable geometry and immutable protected read | `OfflineWorkflowRepository.CaptureSnapshotAsync` calls FIELD `geometry`, stores hashed snapshot; `snapshot-get` rechecks original Crew/device/active assignment | `H5OfflineCanonicalRegistrySqlTests.ActualSnapshotPinsCurrentAssignmentAndDeniesAnEndedAssignment(false/true)` both PASS: route/set/SRID/segments and content hash present; PM and ended-assignment reads denied. | PASS, BE/mock fixture; official CRS/external download separate. |
| H5 repair binding/resource/version, normal plan | `RepairWorkflowRepository.Offline.ReadSnapshotInTransactionAsync` validates current binding/item/assignment and supplies `SafePayload` | `H4RepairLifecycleSqlTests.OfflineRepairSnapshotUsesActualCurrentBindingPlanAndItemVersionWithoutExecuteGrant` PASS: item/version/plan/assignment and PM denial. `H5OfflineRepairSqlTests.SignedNormalAssessmentStartFinishCommitOriginalCrewAndKeepClaimedTimeUncertain` PASS asserts snapshot repair item ID. | PASS for normal repair facts. |
| H5 applicable pinned policy identity, hash and populated policy body in downloaded snapshot | `RepairWorkflowRepository.Producing.AssignItemAsync` only accepts a policy for FT; `ReadSnapshotInTransactionAsync` selects a policy and serializes rules/stops. Normal assignment rejects policy revision, so its null policy is inapplicable, not a failure. | Existing snapshot tests above use NORMAL and assert neither a populated policy revision/hash nor body. Search of existing SQL/API tests found no equivalent downloaded FT policy-payload assertion. Source presence and an eligibility read do not prove it. | NOT_VERIFIED independent acceptance for an applicable policy; real road coverage/FT activation remains OWNER_GATED separately. |
| H5 signed assessment -> start -> finish, original Crew and durable effect; uncertain claimed time | `OfflineWorkflowRepository.Sync` -> `RepairWorkflowRepository.Offline.ApplyInTransactionAsync` -> shared `AssessAsync`/`StartExecutionAsync`/`FinishExecutionAsync` | `H5OfflineRepairSqlTests.SignedNormalAssessmentStartFinishCommitOriginalCrewAndKeepClaimedTimeUncertain` PASS: three durable ACK rows, original Crew, two UNCERTAIN records, null verified-original times and no fabricated FinishedDataSync clock. | PASS, BE fixture. |
| H5 recipient-uploaded evidence provenance | Signed declaration plus `UploadPersistenceService`/offline admission and repair assessment | `H5OfflineRepairSqlTests.SignedHandoverAssessmentResolvesRecipientUploadWithoutRewritingDeclaration` PASS: original signed payload `fileId` stays null, resolved verified file and PM actual uploader/Crew original actor retained; grant revoke denies later protected access. | PASS, SQL/mock storage; real storage/client EXTERNAL. |
| H6 PM review and Supervisor initial/final clock origins | FIELD intake, H4 proposal and accepted PM review create immutable `DeadlineClock`; `H6DeadlineNotificationSourceAdapter` checks root/current head | `H6DeadlineProducerSqlTests.ActualIncompleteFieldIntakeHasOneStableBreachAndCurrentPrimaryManagerResponsibility`; `H6RepairDeadlineSourceSqlTests` both; `H4RepairExecutionSqlTests.IncompleteFormalFieldIntakePinsOneAttemptAndOriginalPmReviewClock`; its normal final scenario all PASS. They assert incomplete intake origin/one original due, current PM/Supervisor source, initial approval and final origin/due. | PASS for origins and tested PM/initial duty. Supervisor-final deadline breach adapter/recipient itself remains NOT_VERIFIED: existing final scenario dispatches `review.supervisor_required.v1`, not `deadline.breached.v1`. |
| H6 FT execute 24h | `RepairWorkflowRepository` issues FT authorization/first-start clock; adapter has `ResolveFastTrackClock` | Existing `H4RepairExecutionSqlTests` asserts no FT clock on NORMAL; no positive real-road eligible FT first-start/adapter assertion exists. | OWNER_GATED for real coverage mapping; independent positive FT clock/source acceptance NOT_VERIFIED until an applicable authorized source exists. No invented coverage fixture or role grant. |
| H6 device handover and finished-data sync 24h | H5 grant and H4 verified finish create clocks; adapter has `ResolveDeviceHandoverClock`/`ResolveFinishedSyncClock` | `H5OfflineCanonicalRegistrySqlTests.SignedPackageGrantImportCommitsOneOriginalCrewEffectAndReplaysUnderCurrentRecipient` PASS checks grant issue/expiry origin; `H4RepairExecutionSqlTests.ActualFinishCreatesExactlyOneSyncClockFromServerFinishAndNoFormalReviewClock` PASS checks server finish/24h due; signed uncertain finish test PASS checks no false sync clock. No existing test asserts breach dispatch/source adapter and current recipient for either kind. | PASS for clock creation; NOT_VERIFIED for both breach source/recipient paths. |
| H6 safety first check, danger warning and 1h ACK | `RepairSafetyRepository` verified FIELD evidence link, action/warning/clock; `H6NotificationDispatchRepository.Clocks` observes breach and source adapter validates source | All four `H4SafetyRuntimeSqlTests` PASS. `ActualFieldMeasurementAdmitsDangerWarningBreachAndLateAcknowledgement` checks verified MEASUREMENT file/link, original uploader, warning/source relation, one-hour due, breach delivery and late business ACK; installation and first-check tests verify original due and current Crew/Supervisor recipients. Inbox delivery does not ACK. | PASS, BE fixture. |
| H6 Monday 09:00 Asia/Ho_Chi_Minh digest and retry | `H6NotificationDispatchRepository` calendar plan/occurrence and source adapter | All three `H6WeeklyCalendarSqlTests` PASS: Monday 02:00 UTC, same-run callback idempotency/current recipients, and missed period left `PENDING_POLICY` without catch-up. | PASS for confirmed schedule/same occurrence; missed catch-up/repetition OWNER_GATED. |
| H6 immutable breach history/current recipient | `H6NotificationDispatchRepository.ObserveClocksAsync` and deadline adapter | All three `H6DeadlineProducerSqlTests` PASS: one breach, prior overdue retained after test-only extension/completion, current PM role and old primary PM role revoked from delivery. Test-only extension is not extension-command authority. | PASS for these paths. |

No test was selected for rerun: all named existing acceptance cases were visible as PASS on the exact source SHA. The missing policy-payload and three deadline breach-adapter assertions have no equivalent existing acceptance identity; adding tests or implementing a new FT activation would exceed this finite no-new-coverage/no-feature instruction. The gap is verification, not a newly reproduced production regression, so no runtime/tests/migrations were edited. Earlier H5 signed-normal and H6 safety/clock open wording is superseded only for the assertions listed PASS here. The independent policy-payload and deadline-source assertions remain open. Prior owner/source gates (official CRS, actual-received protocol, extension/extra execution, substitutes, weekly missed policy, generic lifecycle/transfer, real road coverage, public allocation/period) remain localized PENDING; browser/phone/real storage/provider/worker deployment/Postman/SLO/backup restore remain EXTERNAL/NOT_VERIFIED. Verdict: TECHNICAL CLOSEOUT INCOMPLETE.
