# Backend workflow E2E plan and execution record

Assigned request: plan and execute each principal workflow and inspect remaining business defects. Date: 2026-11-23. Base: `86e9f4e25f2e310f940e45c5cf60efd8d875a61a` plus preserved working changes. Authority/spec: `planning/development/BE-AI-DEVELOP-RECONCILIATION-20261123.md`.

## Test boundaries and fixtures

- HTTP journeys use the actual ASP.NET test host, authentication and disposable SQL where the suite provides them. Explicit fake storage/AI consumers remain fake evidence.
- SQL workflows use fixture-owned Testcontainers after removing process-local inherited shared SQL configuration. No shared/server database is seeded or changed.
- Input data is synthesized by each suite and torn down with its owned database. This does not add demo records to company data or claim surveyed accuracy.
- Current run does not use FE. Backend contract/journey E2E is distinct from an Internet/Ubuntu deployment test, ten-user load test, GPU test or real-model accuracy evaluation.
- Never print credentials, private object URLs, raw videos or production GPS. Failure logs/TRX remain local.

## Principal workflows and assertions

| ID | Actors and journey | Negative/business branches | Evidence suites |
|---|---|---|---|
| E01 | Supervisor/PM project and route geometry setup/read | wrong role/project, stale geometry/version, pavement and scope | Projects HTTP; H2 SQL |
| E02 | Reporter intake → project/case association → PM consumption | idempotency, out-of-scope project, access to own receipt/evidence, invalid state | Reports/Cases/Files HTTP |
| E03 | PM plan/request → assigned Operator → dataset/upload → assessment → explicit completion | reassignment, confirmation vs processing state, type/scope conflicts, stale assessment, supplement | Surveys HTTP; survey scope SQL |
| E04 | verified video → intent → work prepare/lease → private AI results → atomic proposals/artifacts | checksum/catalog/attempt mismatch, source mutation, duplicate dispatch, concurrency, retry before commit and lost commit response | Processing HTTP + AiVideo/ledger SQL + worker unit |
| E05 | Operator SRT/estimated drone reference → current telemetry view | unauthorized operator, head CAS, idempotency conflict, precedence, immutable revisions and source versions | telemetry HTTP/SQL/unit |
| E06 | PM KEEP_NEW/LINK_EXISTING/REJECT → durable defect/source decision | cross-project, stale source, correction and replay, six separate labels; no automatic acceptance of an existing patch | candidate HTTP/SQL/unit |
| E07 | PM FIELD assignment → Crew accept/start/photo/measurement → PM CONFIRM/SUPPLEMENT | codes 1–6, missing/unknown measurement, reassignment/revocation, stale submission, safety duties | Inspections HTTP + H3 SQL |
| E08 | NORMAL repair eligibility/assignment → execution → review/closeout | coverage, formal source, responsibility/transfer, lifecycle and TemporarySafety obligations | Repairs HTTP + H4 SQL |
| E09 | FastTrack execution/review | original window, policy snapshot, overdue/noncompliance paths, review notifications | Repairs HTTP + H5/ND01/FastTrack SQL |
| E10 | offline submit/replay → canonical effect/ACK | stale window/version, denied actor, duplicate replay, concurrency and immutable evidence | Offline HTTP + H5/ND01 SQL |
| E11 | outbox → recipient resolution → notification claim/delivery/retry | duplicate claim/delivery, changed authority, protected source, FIELD/repair lifecycle/deadline | Notifications HTTP + H6 SQL |
| E12 | accepted Reporter/AI source → dossier/export/statistics and retention inventory | incomplete source pins, unknown ground geometry, private evidence, unresolved SRT references, historic attempts | Reporting HTTP + retention/statistics SQL |
| E13 | Production settings → background service composition and integrated model | configured/unconfigured storage, disabled REAL worker, pending-model drift and additive migration sequence | Startup HTTP/model tests |

## Commands

Reproducible local runner: `tests/local-ai/run-workflow-audit.ps1 -Lane Api`, then `-Lane Sql`, then `-Lane Unit`. Build/test lanes are serialized to avoid concurrent writes to shared output folders. Output is under `D:/RoadGuard-AI-LocalTest/develop-sync-20261123/results`.

## Execution log

- Transaction RED: 5 failed, 0 passed, 0 skipped; configured SQL strategy rejected user transactions in the pre-fix repository, including all four lost-ACK cases. `work-retry-red.trx`.
- First transaction GREEN: 5 passed, 0 failed, 0 skipped; actual configured retry plus prepare/claim/change/complete after real commit ACK loss. `work-retry-green.trx`. Expanded rollback-before-commit and unrelated-tracker regressions are included in the later SQL lane.
- Retention plus telemetry regression follow-up: 12 passed, 0 failed, 0 skipped on owned SQL (retention 8 + revision replay 4).
- Python worker/telemetry/Studio: 109 discovered, 108 passed, 1 skipped (Windows symlink privilege unavailable). Explicit local FFmpeg/FFprobe enabled; synthetic MP4 embedded-SRT nonzero-clock extraction test passed. No real model inference claimed by this batch.
- Initial broad API lane: 1006 discovered, 1003 passed, 2 failed, 1 skipped; `workflow-api.trx`. Failures: integrated sandbox schema signature (catalog predates four REAL tables) and Survey supplement notification assertion (target delivery synchronization under investigation). Catalog reconciled with actual owned-SQL export; focused rerun pending. These are initial failing results, not final green evidence.
- FIELD schema export: 2 passed, 0 failed, 0 skipped; `integrated-schema-export.trx`; actual schema inventory exported from owned migrated SQL.
- Initial SQL workflow lane: 610 discovered, 596 passed, 13 failed, 1 skipped; `workflow-sql.trx`. All 35 `AiVideoWorkRepositoryTests` passed, including commit/rollback/query-fault/SQL precision regressions. Twelve export process-crash cases were not executable because the local runner omitted the dedicated child-worker prerequisite; one migration test still expected the pre-AI migration list. Runner and explicit migration expectations corrected; focused reruns are pending. Do not cite initial failures as final green evidence or historical audit counts as results of this baseline.

## Focused follow-up results

- `workflow-sql-followup.trx`: 42 passed, 1 failed, 0 skipped. Dedicated export child-worker prerequisite was built and set; all export recovery and sandbox-seed cases in this selection passed. The remaining H0 failure was an explicit environment refusal: inherited `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` could not connect; the fixture refused fallback before creating a test DB. No connection value was logged.
- `workflow-h0-followup.trx`: after removing that inherited variable and using fixture-owned SQL, 1 passed, 0 failed, 0 skipped. Explicit 16-migration expectation and preserved-history/model assertions passed. Future runs use the runner's process-local environment isolation.
- `workflow-api-followup.trx`: 3 passed, 0 failed, 0 skipped after a fresh build. Both survey variants and Reporter migration/catalog assertions passed. Survey synchronization waits for the target occurrence's actual persisted notification instead of treating an unrelated dispatcher IDLE result as completion; recipient/cardinality and lifecycle assertions remain.
- `workflow-unit.trx`: 226 passed, 0 failed, 0 skipped in the processing unit lane. These unit checks do not establish live GPU/model accuracy.
- Follow-ups replace the affected failing observations; they are not a second full broad-suite run. Initial failures remain recorded above for reproducibility.
- Latest result per TRX `testId`, merging the broad lanes with their focused follow-ups: API 1006 distinct (1005 passed, 1 skipped); SQL 634 distinct (633 passed, 1 skipped, including 24 added follow-up identities); processing unit 226 (226 passed). Total 1866 distinct within these three lanes: 1864 passed, 0 remaining failed, 2 skipped. Supplemental FIELD/Python/earlier regression runs are excluded from this count to avoid duplicate counting. This merged observation is not a claim that all lanes were rerun after every test-only correction.

## Corrections under verification

- REAL work transactions: execution-strategy retries with isolated trackers and durable commit receipts. Receipts read database scalar values inside the transaction, including SQL decimal rounding; high-precision detection confidence is covered in the completion ACK regression.
- External SRT: append stores/reuses one exact private StoredFile proof atomically with the immutable revision. Retention discovers REAL sources, historic-attempt artifacts and external SRT; missing historical mappings remain incomplete rather than eligible for deletion.
- Dossier: accepted AI sources are included with matching decision/Defect lineage; unknown ground geometry stays UNKNOWN.
- Startup: explicitly configured MinIO enables upload verification in Production. Capacity checks select the containing spool filesystem mount.

## Current business/deployment gates

- AI GPS is drone reference; Crew remains responsible for physical defect location and measurements. A frame box is not a unique physical defect or billable quantity.
- Huy's safe MOCK diagnostics are separate from REAL inference integration.
- Actual Program + hosted upload → REAL worker → SQL/object-store ingestion without manually ticking the coordinator is not yet end-to-end proven by composition tests.
- Mixed migration history on an already-running Huy Nov23 database, populated AI evidence rollback, Ubuntu mount/GPU and live external infrastructure remain deployment gates. No shared downgrade is allowed.
- Full historical PM snapshot readback and telemetry/PM race need dedicated evidence; stored immutable source versions alone do not prove a public history UI/API.

## Reviews

Follow-up self-review pass 1: inspected the Survey test diff against the original failing delivery assertion; preserved recipient, cardinality, origin and lifecycle assertions while replacing shared-queue draining with target-delivery polling. Compared the explicit migration list and catalog to the actual integrated owned SQL export; kept new REAL tables NOT_COVERED instead of inventing seed coverage. Focused API/SQL checks above passed.

Follow-up self-review pass 2: inspected file scope, preserved shared dirty work and production migration history, and checked runner isolation/child-worker setup against CI. Corrected Knowledge evidence labels and recorded server/local release differences. `git diff --check` passed. These passes cover this verification follow-up; they do not certify full hosted REAL integration or approve production data/deployment. External acceptance/deployment review is NOT VERIFIED.
