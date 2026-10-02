# RF-10-A08-01 characterization report

## 1. Ket qua va pham vi

Assigned owner prompt 2026-10-02: sole writer Anh/Codex, reviewer pending. Bounded characterization under RF-10-05 of A08-01 discovered in RF-10-08 assessment; test/documentation class, no production implementation. Characterization Done locally, delivery Ready for reviewer (not Integrated/Released). Final focused TRX13/13 confirms the identity risk within authenticated HTTP and isolated SQL/seeded setup. A08-01 fix remains OPEN; no exploit/deployed incident conclusion. RF-10/overall/RF-11 Partial; R01/F01 not created or marked Done.

## 2. Baseline va quyen sua

Dirty `anh`, HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`, unchanged branch/commit expected and checked at packaging. Actual baseline, dirty list, source/config hashes and exact allowlist in `../../../docs/history/refactor/A08-01-characterization/payload/checks/baseline.json`. Real before copies precede docs edits. No fixture edits; existing source/schema/contracts/migrations/CI read-only; isolated owned AuthenticationSqlServerFixture/Testcontainers only. START trace complete; INTEGRATE focused verification/package checks; RELEASE reviewer pending, no deployment assigned.

## 3. Thay doi va doi chieu yeu cau

| AC | Source / change | Evidence boundary |
|---|---|---|
| A08-01 identity | New A0801CallbackIdentityCharacterizationTests, six independently changed fields and fresh-key controls | CURRENT_VERIFIED: each same key/hash returns original job200, body+ETag identical; existing same-project second JobId also returns first job. New key rejects409, adds only Conflict receipt; full scoped effects unchanged |
| First/replay/fingerprint/project | Positive control, changed structured detections/checksum, cross-project independent empty-array callback | CURRENT_VERIFIED: first200 completes job, changes its version, inserts exact detection and success receipt; same replay adds none. Changed hash409 duplicate_request leaves SQL unchanged. Different project same key/hash completes own job, creates independent receipt (empty detections) |
| Upstream guards | Invalid mode/empty attempt and missing AI credential | CURRENT_VERIFIED: mode400, empty attempt422, credential401 with scoped SQL unchanged. No claim every identity change is reachable |
| Checkpoint reference | RF-10-07-C01 and continuing ledger | Inspection correction01 belongs to checkpoint12; processing11 separate; no historical rerun |

No production behavior, fingerprint, wire schema, operation name or migration changed. Compatibility questions documented; owner rollout decision deferred.

## 4. Self-review va autofix

Two self-review passes recorded in `review.md`: correctness of boundary/identity/hash/state/effects and coordination/allowlist/history/parent status. Pre-build review corrected test setup view to query actual manifest from SQL and corrected scalar audit query; no production autofix. First runtime exposed two incorrect assertions against ProblemDetails.detail; controller assigns specific message to title and code duplicate_request. Corrected only test assertion, then captured final pre-build02 sources and rebuilt. Final source/script/docs/dirty scope reviewed; generated drift/hash/package gates decide final evidence validity. Peer review PENDING; integration with external provider NOT RUN.

## 5. Kiem chung

| Command/check | Result | Evidence / limit |
|---|---|---|
| dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-incremental --nologo (build01 and final build02) | Both exit0, 186 warnings/0 errors | runtime/build-*/stdout.txt and command.json; warnings retained, no warning cleanup scope |
| dotnet test same project --no-build --no-restore --filter FullyQualifiedName~A0801CallbackIdentityCharacterizationTests --logger trx;LogFileName=A08-01.trx --results-directory unique-run-directory --nologo | test01 FAIL11/13 (2 assertions); final test02 PASS13/13, 0 failed/skipped, exit0 | Both real TRXs/logs/command UTC start/end preserved. Final TRX2026-10-01 19:11:30.4592348-19:11:49.6290498 UTC, observed clock; not a recreated historical run |
| Final source/assembly drift | Generated checks/verification-summary.json | pre-build02 vs post-build02 vs post-test02; assembly after build/test; no reproducible-build claim |
| Final dirty/allowlist/extract | Generated checks/final-state.json, external archive-verification.json | Preexisting sources preserved, only new test added; missing/mismatch/unlisted/duplicates must be zero |

Thirteen cases: positive1 + six identity controls + two fingerprint cases + cross-project1 + upstream2 + auth1. Each claimed operation has fresh SQL baseline/post snapshot (including full scenario detection sets for new IDs and receipt Id/OperationId/fingerprint/outcome). First callback leaves attempts, files/linkage/uploads and scoped audit/outbox unchanged; replay/rejection/conflict assertions enumerate all observed effects. New-key rejection adds only durable failure receipt; cross-project empty-array success changes only its job and independent receipt within observed sets. No whole-DB immutability claim. HTTP does not expose repository enum Replayed; source maps stored-success replay while runtime proves identical outcome and scoped effect immutability. Handler skipping is SOURCE_INSPECTED, not call-count instrumentation.

Setup: entity/factory seeded SQL with domain upload transitions, production repository job/attempt creation. Not complete HTTP upload/PM workflow, real checksum/object/provider verification or synthetic retry/late-A setup. No full suite/inspection rerun, external consumer or deployed claim. Evidence-only report/ledger/package finalized after test; executable test/fixture/production/config source frozen at final pre-build02.

## 6. Phoi hop va quyet dinh

No shared production writer or new coordination note needed for this test/docs-only task. Primary follow-up RF-10-05 with AI/provider owner UNKNOWN. Compatibility considerations require owner choices for identity fields, persisted legacy receipt retries, operation/version/key namespace and canonical serialization. RF-10-08/09 assessments RETAIN accepted read-only by this prompt, not R01 implementation acceptance. No A08-02/A09-01/A09-02/F/G or CG11/RF10-R07 closure.

## 7. Checkpoint va ban giao

Deliver `RF-10-A08-01-characterization-handoff.zip` with complete byte-generated manifest, extraction verification and external SHA256; stop for reviewer. A08-01 confirmed only in bounded HTTP/isolated SQL scenario, fix OPEN. Historical old-token, correction04 linkage, survey08 missing before snapshots, inspection12 correction01 metadata/correction02 production-hash gap, notification delivery, provider and F/G limits stay open. A08-02/A09-01/A09-02 remain source-only, not runtime-confirmed this turn. Test/docs recovery uses real before snapshots; no production rollback. Planner: sole-writer characterization13/13, two self-review passes, failed run retained, peer review pending; define separate RF-10-05 compatibility-approved fix after deciding legacy receipt/key/version policy. No parent status upgrade.
