# BE–AI develop reconciliation and workflow verification

Status: assigned by owner in this chat, 2026-11-23. Implementation owner: Codex for the whole BE–AI slice; Huy pauses these shared files as previously authorized.

## Baseline and preservation

- Branch `anh`, previous HEAD `4934103d125be1caca4c6a588eed3768c9cafee4`, fast-forwarded to `86e9f4e25f2e310f940e45c5cf60efd8d875a61a` (`origin/develop`). Seven Huy commits reviewed separately.
- Existing dirty BE/AI work preserved by backup `D:/RoadGuard-AI-LocalTest/develop-sync-20261123` (111 files with hashes and patches) and retained Git stash `be-ai-before-develop-sync-20261123`. Reapply completed without textual conflicts; semantic verification still required.
- Existing owner decisions remain binding: AI proposes, PM decides; operator supplies drone reference position, Crew supplies ground measurements; six labels remain separate, repair means existing patch.

## Authorized corrections

1. Make REAL work SQL transactions compatible with configured retry strategies. Preserve attempt/lease/source fences and atomic artifact ingestion. Inject failures before commit and after real commit; recover one result without duplicate work, artifacts, proposals or outcomes. Never save unrelated caller changes as a side effect.
2. Include REAL source/artifact and external telemetry retention references. Unresolvable ownership/file proofs fail closed. No retention deletion or new retention policy.
3. Reconcile accepted AI source capture using existing dossier contract only; do not invent public routes or silently reinterpret AI as Reporter.
4. Verify production upload-verification registration and correct it against existing explicit MinIO configuration. Worker remains private and opt-in.
5. Correct spool capacity measurement for the actual filesystem; local tests do not certify Ubuntu disk/GPU configuration.
6. Review Huy changes, auto-merged files, migration ordering/model alignment, and update local Knowledge with sourced implementation and test evidence.

Each writer owns separate files; shared context/migrations are not concurrently edited. Tests use fixture-owned disposable databases/object storage only. No deployment, shared data seed, schema application, push, model/package upgrade, or public API redesign is authorized by this package.

## E2E acceptance matrix

For every executed workflow record actor, input/precondition, HTTP or service boundary, durable effects, negative branches, and proof level. Existing adopted tests may provide evidence; route existence and unit tests alone do not prove E2E.

| Workflow | Required checks | Evidence boundary |
|---|---|---|
| Project and Reporter intake | actor/project scope, Reporter project association, routing, duplicates, active lifecycle | API/SQL suites, synthetic records |
| Survey and Operator upload | assignment/acceptance, types, source manifest, verification, pairing, submission notification | API/SQL and owned MinIO where available |
| REAL video processing | submission intent, prepare/claim, exact attempt, six labels, artifacts, zero/failure/retry, restart/lost ACK | SQL + private worker contract; actual model evidence separate |
| Telemetry supplement | assigned operator only, external/paired/embedded/estimate precedence, CAS/idempotency, immutable history | SQL/API; no GPS accuracy claim |
| PM decision | KEEP_NEW/LINK_EXISTING/REJECT, current source version and project authority, stored snapshot | API/SQL, inference may be mocked |
| FIELD inspection | PM assignment, Crew accept/start/submit, supplement, reassignment, confirm, measurement types 1–6, safety duties | API/SQL; synthetic field evidence |
| Repair NORMAL/FastTrack | eligibility, coverage, policy/deadlines, noncompliance outcomes, immutable completion | existing adopted SQL/API workflows |
| Offline | scope/version conflict, replay, durable ACK and exactly-once effects | SQL/API |
| Notification/jobs | current recipients, scope/retry/dedup, submission and FIELD delivery | SQL; SMTP/push separate |
| Retention/dossier/statistics | accepted sources, REAL evidence discoverability, failclosed unknown basis, no unapproved deletion | SQL and capture consumer |

Read-only inventory complements these journeys. A skipped test or mocked dependency must remain visible; missing end-to-end evidence stays UNKNOWN. Tests do not constitute business approval for an undocumented rule.

## Execution and completion

- [x] Preserve work and fast-forward develop.
- [ ] Review seven Huy commits and semantic merges.
- [ ] Reproduce/fix transaction retry and ambiguous commit defects with regression tests.
- [ ] Fix retention, reporting and startup/capacity seams within existing contracts.
- [ ] Run affected workflow suites and record distinct tests, failures and skips.
- [ ] Review final diff twice: acceptance/fences first, ownership/interfaces/docs second.
- [ ] Update Knowledge and publish concise report with remaining business/deployment gates.

## Continuation checkpoint — 2026-11-23

- [x] Re-ran browser shutdown/lease coverage: 3/3 Playwright integration tests passed with the task-local Node module path.
- [x] Re-ran AI worker coverage: 71 tests, 69 passed, 2 host-permission skips.
- [x] Corrected Windows resolved-directory containment in `AI/roadguard-ai-engine/src/inference/video_models.py`; full AI suite now 206 passed, 2 skips.
- [x] Re-ran sequential BE build and focused API/unit lanes: build 0 errors/0 warnings; API 33/33; processing unit 226/226.
- [x] Kept deployment boundary unchanged: no server deploy, migration application, shared DB seed/write, push/merge or public route change.

The broad API audit command was stopped after it hung without fresh output; it is INCONCLUSIVE/NOT RUN for completion evidence. The remaining deployment gates are unchanged and remain external/deployment NOT VERIFIED.

Historical audit: `docs/superpowers/plans/2026-11-23-be-ai-audit-matrix.md`. New test output is stored outside the repository under `D:/RoadGuard-AI-LocalTest/develop-sync-20261123`.

Integration follow-up ownership: review_transactions writer owns sandbox/seeds/v1/table-coverage.json and SandboxSyntheticSeedTests.cs for schema inventory reconciliation only. Actual schema facts must come from owned disposable SQL; new REAL AI tables are explicitly NOT_COVERED by the generic demo seed. Existing scenarios/accounts remain unchanged.
