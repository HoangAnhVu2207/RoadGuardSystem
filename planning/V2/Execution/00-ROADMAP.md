# 00 - Shared roadmap and AI execution rules

## Purpose and task goal

Deliver the next verified backend slices from the V2 documents without allowing an AI agent to invent scope, schema, policy, endpoint behavior or production readiness. Work is grouped by business flow so each task has enough context to understand why the code exists, while ownership remains split by ADR 006.

## Current baseline

- Record base revision and dirty state at task start with `git status --short --branch` and `git rev-parse HEAD`.
- Canonical design is under `docs/design`; `planning/V2/Person_*` cards are operation contracts and trace references.
- `task_manifest.json` is metadata, not runtime proof. Keep `CURRENT_VERIFIED`, `TARGET_DOCUMENTED`, `PROPOSED_DELTA` and `NOT_ENABLED` distinct.
- The current checkout includes uncommitted restructuring and migration-baseline work. No task may reset, overwrite or assume those changes are integrated into `anh` or `huy`.

## Wave order

1. **Wave 0: common gate.** Record base, resolve contract-lock/status discrepancies, read the relevant ERD/DD/BR/state/API sources, and reserve shared hotspots.
2. **Wave 1: identity.** Run ANH-01 and HUY-01 as a pair. Huy consumes repository facts only after Anh records them in `planning/CROSS_OWNER_HANDOFFS.md`.
3. **Wave 2: project and survey.** Run ANH-02 and HUY-02. Route/version, survey assignment and dataset readiness are separate states; upload does not imply quality or baseline confirmation.
4. **Wave 3: report, inspection and repair.** Run ANH-03 and HUY-03. Keep measurement, defect, repair, curing, traffic release and acceptance as separate transitions.
5. **Wave 4: processing and operations.** Run ANH-04 and HUY-04. AI callbacks, matching, retention and external providers remain gated unless their source contract and fixtures are verified.

Parallel work is allowed only between the two owners' layers inside one wave. Migrations, `RoadGuardDbContext`, mappings/snapshot, shared DI, errors, OpenAPI, seed, Docker and CI have one active writer.

## AI method for every task

1. **Read the task first.** Extract the goal, operation trace, exact in-scope files, out-of-scope list and stop conditions.
2. **Route sources by layer.** Read the named BR/FR/UC/US/AC, ERD/DD/domain/state/API/error sources and current symbols/tests. Do not substitute a similarly named historical file.
3. **Write evidence before editing.** Label every fact `CURRENT_VERIFIED`, `TARGET_DOCUMENTED`, `PROPOSED_DELTA`, `HISTORICAL`, or `NOT_ENABLED`, with exact path and heading.
4. **Write the 5-8 line contract.** Include actor/scope, input validation, success status/output, stable errors, state rule, persistence/idempotency/concurrency and audit/privacy.
5. **Compare current versus target.** If route, schema, authorization, state, migration, provider or compatibility differs, record the conflict and stop that slice until an approved decision exists.
6. **Implement the smallest bounded change.** Reuse existing interfaces and patterns. Do not add speculative endpoints, tables, packages, providers, migrations or broad refactors.
7. **Test the changed invariant.** Build fresh selected projects first. Use SQL Server for persistence/concurrency/migration claims and real HTTP smoke for API claims. Static Postman checks are separate evidence.
8. **Handoff explicitly.** Publish changed interfaces, fact semantics, fixtures, versions, errors and remaining gaps. Receiver must reply `VERIFIED`, `NO_CHANGE_NEEDED`, or `BLOCKED`.
9. **Report honestly.** Use `DONE` only when all applicable gates pass; otherwise use `PARTIAL` or `BLOCKED` with the exact operation and missing evidence.

## Universal scope guard

### In scope for every task

- The operation trace listed in that task.
- Current-source reconciliation against the named V2 documents.
- Focused tests for changed business, authorization, privacy, idempotency, concurrency or durable-effect invariants.
- Required cross-owner handoff and completion history.

### Out of scope for every task unless separately approved

- Applying migrations or changing a live database.
- Production thresholds, provider deployment, SMTP delivery, drone/device behavior, browser/mobile UI or AI accuracy claims.
- Rewriting historical `DONE`/`PASS` evidence.
- Editing the other owner's layer to make a test pass.
- Renaming or inventing operation IDs to hide a contract gap.

## Completion record

Each task appends changed files, source evidence, acceptance results, exact commands and counts, SQL/HTTP/durable effects, handoff receiver result, side effects, reused or invalidated evidence, and unverified risks. Documentation-only changes run the documentation validators; code tasks follow the verification ladder in `AGENTS.md`.
