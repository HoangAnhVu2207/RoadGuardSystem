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

TARGET_CONFIRMED: FT final confirmation is PM; normal final confirmation is Supervisor after PM review. Mistaken/insufficient acceptance appends actor/reason/time/supersedes correction and continues the original obligation; it is not a new recurrence Defect. Original history/export snapshots remain immutable; live effective projection/KPI excludes superseded acceptance. Owner found no authoritative decision for correction authority or its re-review/re-confirm procedure; both remain localized PENDING. Same final-role correction, same individual, Supervisor override and audit authority are not inferred. FT PM/normal Supervisor correction is PROPOSED only. Production correction action without adopted authority denies/pends. Model/history/effective projection/authorization hook and clearly sourced test-policy allow/deny, current-project scope, version/supersedes, concurrency, rollback/replay tests proceed; test policy is not owner approval. Independent H4–H7 remains assigned.

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
| TARGET_CONFIRMED | Consolidated owner update §1 | FT correction: current project PM; normal correction: current project Supervisor; valid current authority, different individual permitted | H4 vertical and H7 corrected consumers assigned, IN_PROGRESS; no new PASS claim | None for correction actor or mandatory approval procedure; substitute appointment/grant remains independently pending |
| TARGET_CONFIRMED | Consolidated owner update §1 | Crew/PM current-authority review request; no effective-decision mutation | H4 request/history flow assigned | No inferred execution grant or automatic assignment |
| TARGET_CONFIRMED | Consolidated owner update §1 | Atomic correction effects on original obligations, Defect/package state, live UI/KPI/new export; immutable originals/snapshots | H4/H7 transaction and consumer verification assigned | Public time-bucket/period allocation/denominator interpretation remains pending |
| TARGET_CONFIRMED | Master and update §2 | All R01–R30, persistent login, immutable clock origins/durations, FT predicates/versioned PM policy, lifecycle separation | H0–H2 checkpoints and producing H3–H7 verification recorded separately | No reopened predicates or durations |
| UNKNOWN / PENDING | Update §3 | Real FT eligibility source interpretation | Reuse actual scoped Warranty dates/road scope/documents and project HandoverDocument facts; return source mapping UNKNOWN where absent | Project handover→specific road/route/segment mapping and maintenance coverage adoption; no inference from defaults |
| PENDING / NOT_VERIFIED | Update §3 | Official CRS publication and survey accuracy | Versioned H2 core and sampleOnly CANDIDATE fixtures verified separately | Authoritative profile/operation/tolerance/independent controls |
| PENDING | Update §3 | Actual-received protocol; per-clock extension/execute-right issuance authority; substitute appointment; missed weekly occurrence and business recurrence | Event provenance/history/idempotency/policy seams proceed; technical retry stays same occurrence | No inbox/read/delivery/receipt-as-received assumption; dedicated ACK and latest-period catch-up remain PROPOSED |
| PENDING | Update §3 | Separate Defect close, linked recurrence creation, construction completion, operational closure, obligation transfer grant/accept/eligible receiver | Lifecycle models/invariants/read projections proceed; production mutations denied without adopted authority | Actors/procedures only; does not block §1 correction effects |

H4 acceptance now requires the complete production correction vertical and actual adopted-role tests listed above. H7 consumers must read effective corrected decisions for live KPI and new snapshots while preserving existing published snapshots. Work review findings, technical design, owner decisions and executed checks retain separate evidence labels. Continue H3→H4→H5→H6→H7 without restarting checkpoints, cutting scope, rewriting applied migrations or deploying production.

## Owner clarification — closure, recurrence and lifecycle command authority

TARGET_CONFIRMED behavior remains R18/R19/R26: explicit Defect close only when mandatory obligations are resolved; Case conclusion/publication separate; true successful-repair recurrence creates a new linked Defect while mistaken acceptance continues the old obligation through correction. Construction completion, operational closure and warranty are separate; operational closure requires resolved obligations or receiver-confirmed transfer; new Reporter intake continues. Supervisor renewed handling scope remains CONFIRMED.

Owner found no adopted actor/procedure for explicit Defect-close, linked-recurrence-create, construction-completion, operational-closure or obligation-transfer grant/accept commands; each activation remains localized PENDING. D09 FT/historically Supervisor-file reopening, proposed closeProject/assessRecurrence and Supervisor→PM device data handover do not authorize these new commands. Do not infer Supervisor close, PM recurrence creation, arbitrary member transfer acceptance or dual approval. Current membership is necessary but not command authority. Continue model/state/history, obligation/linkage/provenance/read projections and authorization hooks with provenance-labeled controlled test policies. Production mutation denies/pends without adopted policy; pending/unaccepted transfer cannot satisfy closure and completion cannot auto-close obligations/warranty. No corresponding vertical command acceptance is claimed; independent H4–H7 continues.

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
