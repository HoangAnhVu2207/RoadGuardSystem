# RoadGuard V2 Skill And Governance Design

**Date:** 2026-09-27
**Status:** Implemented and verified in the working tree
**Owner:** Repository owner on branch `anh`

## Goal

Make this prompt a reliable entry point for implementing one RoadGuard V2 API task:

```text
Dung $roadguard-endpoint-delivery thuc hien task <duong dan task>.
Doc AGENTS.md va doi chieu code hien co truoc khi sua.
Chi thay doi trong pham vi task, kiem thu du rui ro
va bao ro phan chua xac minh.
```

The invoked agent must derive the implementation from the assigned task, accepted repository decisions, and current source instead of treating the V2 draft as proof that behavior already exists or is approved.

## Decision

RoadGuard adopts the `planning/V2` vertical ownership model. The owner named by a V2 task owns the complete endpoint slice from Controller through Service and Repository, including directly required entity shape, mapping, migration, SQL tests, and endpoint verification when the approved task needs them.

This changes ownership, not architecture. Production continues to use `Controller -> IService -> IRepository` under ADR 001 and ADR 004. Layer boundaries, one-public-type rules, source dependency restrictions, and the 500-line limit remain in force.

Shared persistence files remain serialized hotspots. Only one task may modify `RoadGuardDbContext`, a mapping, migration snapshot, shared DI, shared error contracts, or OpenAPI at a time. Person 2 coordinates migration ordering, SQL integration, and release evidence but does not exclusively own persistence changes. A task owner must declare the shared files and side effects in the approved scope card before editing them.

## Sources Of Truth

Apply sources in this order when they disagree:

1. The user's current request and explicit approval.
2. Root and applicable nested `AGENTS.md` rules.
3. Accepted ADRs, with later accepted amendments superseding conflicting earlier sections.
4. Current source, project references, migrations, tests, and runtime contract evidence.
5. The assigned V2 task and central V2 OpenAPI draft.
6. Historical plans and worklogs as evidence only.

A `PROPOSED_CONTRACT`, `NEEDS_REPO_CHECK`, or `BLOCKED_SLICE` task never silently overrides accepted runtime behavior. The endpoint agent records current versus proposed behavior and stops for a decision when the delta affects compatibility, schema, authorization, workflow, or an open product gate.

## Skill Set

### `roadguard-endpoint-delivery`

This is the user-facing orchestrator for one API task. It must:

1. Resolve the repository root and exact task path, then inspect Git state without changing it.
2. Read `AGENTS.md`, the assigned task, relevant accepted ADR sections, and only the referenced V2 contract/product sections.
3. Locate the closest current Controller, Service interface/implementation, DTO, Repository interface/implementation, entity/mapping, HTTP example, and focused tests.
4. Produce the repository scope card and a 5-8 line endpoint contract. Wait for explicit approval before edits unless the same exact scope was already approved in the session.
5. Compare current behavior with the V2 proposal, including route/method, error envelope and codes, pagination, idempotency, concurrency token placement, authorization scope, and state transitions.
6. Preserve `Controller -> IService -> IRepository` and current folder/namespace conventions.
7. Load `roadguard-persistence` whenever the task changes EF queries, mappings, atomic writes, migrations, concurrency, idempotency, outbox, storage, or SQL behavior.
8. Load `roadguard-test-selection` to choose and execute sufficient fresh verification.
9. Update `RoadGuardSystem.API/RoadGuardSystem.API.http` for the endpoint and report actual evidence plus unverified risks.

It must not implement multiple dependent endpoints, invent unresolved product decisions, add packages or architectural frameworks, return EF entities, expose secrets, or claim success from templates, stale binaries, zero tests, or documentation validators.

### `roadguard-persistence`

This skill owns persistence reasoning inside the endpoint owner's approved slice. It preserves repository interfaces and layer boundaries while covering SQL projection, atomicity, constraints, rowversion, idempotency, retries, outbox, storage, and migrations. It requires SQL Server/Testcontainers evidence for provider-specific claims and never treats permission to draft a migration as permission to apply it to a live database.

### `roadguard-test-selection`

This skill selects one verification breadth from known risk: focused, affected-project, or full-solution. It must build the changed production project required by the repository gate and also refresh the selected test project's binaries before using `--no-build`. Endpoint changes include a real smoke call; SQL-specific changes include focused SQL Server evidence. Documentation-only changes run only their validators.

## Repository Rules And Discovery

`AGENTS.md` is the canonical repository rule. `.agents/rules/roadguard.md` is a short discovery/router file and must not duplicate the complete policy. It routes endpoint work to all three skills and states the V2 ownership model plus shared-hotspot serialization.

The skill UI metadata in `agents/openai.yaml` must describe the real trigger and default prompt. All three skills remain available for implicit invocation, while the explicit `$roadguard-endpoint-delivery` prompt remains the recommended entry point.

## ADR Strategy

Create a new accepted ADR for V2 endpoint ownership and persistence coordination. Preserve the historical rationale in ADR 001-005 and amend only conflicting or stale sections:

- ADR 001: reference the V2 ownership ADR and clarify that layer ownership is architectural responsibility, not exclusive person ownership.
- ADR 002: replace fixed legacy person/task assignments with V2 task-owner delivery while preserving security decisions; distinguish current legacy error codes from proposed V2 uppercase codes until an endpoint delta is approved.
- ADR 003: retire the statement that exactly two execution plans control scheduling and point to V2 task cards; preserve the external AI boundary and unresolved gates.
- ADR 004: supersede its person-based ownership section while retaining N-layer architecture and file conventions.
- ADR 005: keep `Proposed`; add the V2 execution/ownership relationship without marking open product decisions accepted.

## Planning V2 Alignment

Update `planning/V2/README.md` and the common boilerplate in all 133 task files listed by `task_manifest.json`:

- The task owner owns the complete endpoint slice.
- Migration/schema work is allowed only when required, declared, and approved in that task scope.
- Shared hotspots require reservation and one writer.
- Read all three project skills, loading persistence details only when touched.
- Repository rules and accepted ADRs win over proposed task text.
- `NEEDS_REPO_CHECK` requires a source comparison before implementation.

Do not change operation contracts, gates, dependencies, task IDs, owners, or historical Done evidence as part of the mechanical alignment.

## Portable Documentation Verification

Python documentation tools must specify UTF-8 when reading repository text. `validate_package.py` and `test_contract_guard.py` currently depend on the process locale and fail under Windows `cp1252`; update their reads so the documented commands work without setting `PYTHONUTF8=1`.

## Implementation Slices

1. Add the governance ADR and amend ADR 001-005.
2. Replace/merge the installed endpoint and test skills from the supplied packages and install the persistence skill with references, assets, and agent metadata.
3. Align `AGENTS.md`, the discovery rule, `planning/V2/README.md`, and the 133 task boilerplates.
4. Fix UTF-8 portability, update validation evidence, and run all documentation/skill checks.

No slice changes backend C# source, packages, database state, or generated migrations.

## Verification

Run:

```text
python C:/Users/HoangAnhVu/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/roadguard-endpoint-delivery
python C:/Users/HoangAnhVu/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/roadguard-persistence
python C:/Users/HoangAnhVu/.codex/skills/.system/skill-creator/scripts/quick_validate.py .agents/skills/roadguard-test-selection
python docs/diagram/V2/09_Frontend/contracts/check_contracts.py
python docs/diagram/V2/09_Frontend/contracts/validate_package.py
python docs/diagram/V2/ci/test_contract_guard.py
git diff --check
```

Also verify that the manifest still maps exactly 133 unique OpenAPI operations to 133 existing task files, dependencies reference known operations, the owner totals remain P1 71/P2 62, and every task contains the new ownership/skill boilerplate exactly once.

Backend build, SQL, HTTP, and runtime tests are out of scope because this change modifies only governance, skills, plans, ADRs, and documentation tooling.
