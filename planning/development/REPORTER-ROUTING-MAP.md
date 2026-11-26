# Reporter routing map and intake decisions

Status: ASSIGNED, owner instruction dated 2026-11-26 and attached routing-map proposal, qualified by the additional owner decisions below. Writer: Huy/Codex, `codex/reporter-routing-map`; delivery through `huy-review`. Base: `7019602f2d348f5e5f51a1863e9a6c81cccaea93` (`origin/develop`). Initial worktree clean. Main checkout `D:\DO_AN\RoadGuardSystem` and its pre-existing WIP are reserved and untouched.

Integration update: fetched and fast-forwarded this worktree to `9000858adddf911ec9a0d353f00ef05f21e9a898`, preserving the three upstream CI balancing commits. Current workflow has nine API and nine SQL lanes; new routing tests join the current Cases lane (API 6). No gate/skip policy is changed. The task diff was backed up outside the repository before reconciling its lane manifest.

## Authority and scope

TARGET_CONFIRMED (new owner decisions, 2026-11-26): A organization road map for active Supervisors; B exact selected Report evidence routing candidates; C additive source pins on existing Case triage; D internal, append-only NEEDS_LOCATION_EVIDENCE / OUTSIDE_COMPANY_SCOPE decisions on unassigned Cases, with reconsideration and stale readback. These are new decisions, not historical authority. No frontend edits.

- Preserve original evidence GPS, Capture/Exif/Manual provenance, accuracy and claimed capture time. Never select a different image, average, snap, or replace missing positions with zero.
- Display geometry is distinct from canonical measurement. No invented business radius, corridor, confidence, uncertainty or responsibility. Missing source/time/CRS is an explicit incomplete result, not proof of outside scope. Server received/evaluated time is labeled, never historical proof.
- Current confirmed/effective route versions are default. Include closed projects and identify historical obligation pins without silently substituting geometry versions. Single-organization authority must be established from current code; no synthetic tenancy.
- Supervisor routes an unassigned Case; PM selects the method in its current project. No assigned-Case project transfer, extra Supervisor matching/split authority, automatic technical conclusion, task, defect, publication or notification.
- FIELD triage may proceed on grounded scope/reason without GPS. Optional evaluation pins are checked when used. New sources invalidate old routing dispositions. OUTSIDE requires a complete Case attestation with explicit source references and resolution of conflicts; incomplete machine results never suffice.
- Append immutable decisions and supersession events. Triage replaces effective dispositions atomically. Internal reasons/attestations do not become Reporter/PM permissions.

CURRENT_VERIFIED source anchors at base: `CasesController` -> `CaseWorkflowService` -> `CaseWorkflowRepository`; `CaseReportDetailsRepository` checks active Case links and private verified file versions; `Wgs84DisplayProjection` / `Wgs84MetricProjection` support explicit WGS84 UTM zones 48N/49N; `RoadCoverageResolver` retains source versions and TEST_ONLY provenance. Current implementation is evidence, not independent business approval.

## Reserved files and delivery order

Sole writer for the routing slice, Case DTO/facts/repository/service/controller additions, routing persistence/configuration/migration/snapshot, DI registrations, route inventory and generated OpenAPI, relevant tests and schema/seed catalogs. No package upgrades, unrelated refactors, changes to CI gates or existing applied migrations. Order: contract and persistence -> authenticated HTTP vertical slice -> migration/isolated SQL verification -> handoff/OpenAPI -> two self-reviews -> normal push and safe fast-forward -> verified local runtime update.

## Verification and recovery

Isolated SQL/storage only for writing tests. Test A pagination/filter/snapshot/authorization/conditional GET/bbox intersection and unsupported CRS; B exact evidence/supplements/current links/source versions/provenance and missing data; C existing authority, FIELD without GPS, lineage, stale pins, ETag/replay/concurrency; D reconsideration/source changes/full-Case attestation/history/no side effects and unauthorized readers. Run analyzer/build, format, source gates, unit, full API lanes and affected SQL lanes using the current workflow. Preserve lane identities and permitted skips.

Any additive migration must retain old rows; verify upgrade/model/schema/seed catalogs on isolated SQL. Before updating the local runtime DB: backup, checksum, VERIFYONLY, restore rehearsal and migration trial; retain old image/volumes, no seed/reset or LAN exposure. Runtime/bootstrap/health are separate from workflow acceptance.

## Evidence and reviews

Execution results, two actual self-review passes, final SHA, runtime image, migration/recovery evidence and limitations will be recorded in the routing handoff. External MapLibre/mobile/UI acceptance remains NOT VERIFIED unless actually exercised; no claim of live AI or externally verified source provenance.

Self-review pass 1 (2026-11-26, source/contract): reviewed A/B/C/D authority, exact active evidence lineage, canonical distance, receipt guards, serializable Case writes, append-only history and source invalidation. Fixed registry fingerprint omission of Defect lineage/rowversion; rejected null attestation entries; aligned conditional-GET Swagger headers. The two new map tests now traverse every page instead of assuming fixture routes occur on the first page. Assertions about actual geometry and historical obligation pins remain intact. Affected routing verification is repeated on the corrected content.

Self-review pass 2 (2026-11-26, integration/recovery): inspected the complete owned path list, additive migration/snapshot, old direct service constructors, Seeder no-fabrication behavior, schema/coverage catalogs, OpenAPI/catalog and source excerpts. Three upstream CI commits remain in ancestry; no workflow or skip gate was changed. Existing clients omit the nullable routing command property from fingerprints. New map metadata is distinct from responsibility/source-document data. Separate local recovery scripts preserve the current DB, volumes and old image, and require backup/restore/schema checks before runtime migration. Runtime execution and final gates remain separately recorded evidence, not inferred from this review.

Regression follow-up: full API lane 8 exposed hard-coded previous migration head and catalog count (260 versus the four new operations). Updated those exact expectations, the related safety migration head/table count and sandbox schema table count (203). Existing upgrade/downgrade, immutable-history, row preservation, exact route-set and fail-closed assertions are retained; no test identities or gates are removed.

Final seam/performance review: controller depends on IRoutingMapService, with the existing service/repository seams retained. Registry snapshots use immutable route identities/current flags and existing metadata hashes; the baseline SQL trigger forbids route geometry/CRS/version rewrite or deletion. Geometry bytes are read only for the bounded page, rather than globally serializing every route geometry. The affected routing HTTP/Swagger tests are repeated after these changes.

Final upstream integration: origin/develop advanced to `6257e61e629b269bd91d18d5fe5a8c96da65c6cc` while validation was running. Backed up all 47 task paths outside the repository, fast-forwarded without touching the main checkout and reapplied only the task's API identity additions. Upstream changes are CI balancing and role-flow harness changes, not backend production/model changes. Current workflow has nine API and ten SQL lanes. Re-run affected API lanes 3/4/6 and SQL lanes 4/10; unchanged lane identities/results are retained, followed by both current identity gates. Gate scripts and skip lists remain unchanged by this task.

Final local verification: Unit 1312/1312; API 1771 passed plus one whitelisted external MinIO smoke skip (1772 identities); SQL 897/897 across current ten lanes. Current API/SQL identity gates, 14 source checks, full format and diff check passed. Focused 13-case runs overlap the full API suite. EF model parity and two Development seed runs passed without count changes. Self-review reread the final service interface, immutable-route snapshot optimization, contract limits, migration regression expectations and generated OpenAPI. External UI/MapLibre and real-world document provenance remain unverified. Remote/runtime delivery evidence is recorded separately after publication and rollout.
