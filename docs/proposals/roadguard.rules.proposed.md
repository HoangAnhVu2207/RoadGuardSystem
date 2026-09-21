# RoadGuard Working Rules (proposed)

Status: Proposed. Target: `AGENTS.md` plus the discovery pointer in `.agents/rules/roadguard.md`. This draft does not apply either change.

Repository fact: the Minimal API/vertical-slice wording is currently in `AGENTS.md`, lines 12-14, not in `.agents/rules/roadguard.md`. The active `.agents/rules/roadguard.md` is a five-line discovery pointer. An A0 application task must update the canonical source and preserve a non-duplicating pointer.

## Start And Scope

- Start with `git status --short --branch`, the assigned row in `planning/`, and real dependencies in this checkout.
- Historical `Done` rows and worklogs are evidence. Never rewrite them to match the current workflow.
- Before edits, show: task/owner, goal, In scope, Out of scope, exact files, dependencies, verification tier, and side effects. Wait for explicit owner approval in this session.
- For work larger than one endpoint, propose 3-5 small slices and wait for approval before code.
- Preserve unrelated and uncommitted work. Shared files have one declared owner at a time.

## Endpoint And Layer Workflow

- Write a 5-8 line endpoint contract: route, actor, input/validation, success output/status, stable errors, business rule, persistence/idempotency, and audit/sensitive-data rule when relevant.
- Follow the accepted N-layer flow `Controller -> IService -> IRepository` and the layer boundary in ADR 001 plus the accepted form of ADR 004. Do not introduce a second production architecture silently.
- Controllers remain thin and never access Repositories or `RoadGuardDbContext`. Services own use-case policy and never use EF/HTTP types. Repositories own persistence mechanisms and never own business or HTTP decisions.
- Every concrete persistence/read-model class injected into a Service requires an interface. Do not add MediatR or AutoMapper.
- New code uses one public type per file; new DTOs, entities, and interfaces always do. Leave untouched legacy multi-type files unchanged.
- Every new endpoint adds a runnable request in `Http/*.http`; return response DTOs (records allowed), never EF entities.
- Use ProblemDetails/stable error codes, `AsNoTracking()` for reads, projection to avoid N+1, UTC `DateTimeOffset`, and async APIs only.
- New or modified files must stay at or below 500 lines. Splitting an existing oversized file needs its own approved scope.
- Repository rules and accepted ADRs win over third-party skill defaults.

## Verification Ladder

- Documentation/tooling changes run only their relevant verifier, link check, or diff check.
- For code, build the changed project once with `-nologo -v q -clp:ErrorsOnly`.
- After a successful build, run the one selected test breadth with `--no-build --nologo -v q`.
- Coverage is CI/release-only unless an approved scope explicitly requests local coverage measurement.
- Use the proposed `roadguard-test-selection` skill to map changed paths to one breadth: focused, affected-project, or full-solution. These breadths are alternatives, not a sequence.
- Zero discovered tests or skipped required tests is not a pass.
- Endpoint work includes the relevant real `.http` smoke request before risk-based tests.
- SQL Server spatial, constraints, migrations, and rowversion require SQL Server/Testcontainers; SQLite is not proof.
- Reuse unchanged evidence and rerun only checks invalidated by later edits.
- If the same failure survives two fix attempts, stop and report the shortest useful diagnostic.

## Ownership And Safety

- Person 1 (`anh`) owns endpoint/API, Services, DTOs, HTTP examples, and API-focused tests after required Person 2 dependencies are `Done`.
- Person 2 (`huy`) owns entity shape, `RoadGuardDbContext`, mappings, migrations, SQL tests, seed, Docker, CI, and persistence infrastructure.
- Under `Repositories/Implementations`, Person 2 owns schema/mapping/migration and infrastructure persistence. Person 1 may add or change only an endpoint-specific read-only repository method when the approved scope card names the exact files and no schema/mapping change is needed.
- Cross-owner repository work must declare the shared hotspot and sequence before edits.
- Do not add packages, create/run/edit migrations, delete data, or change schema without explicit approval.
- Commit only on the assigned `anh` or `huy` branch and stage explicit paths. Merge, rebase, pull, push, tags, branch/worktree changes, stash, and destructive restore require owner approval.

## Proposed discovery pointer

The applied `.agents/rules/roadguard.md` should remain short and non-duplicating:

```markdown
# RoadGuard discovery

Use the canonical repository rules at `../../AGENTS.md`.
Use accepted ADR 004 with ADR 001 for backend layer decisions.
For endpoint work, load `../skills/roadguard-endpoint-delivery/SKILL.md`.
For selecting test breadth, load `../skills/roadguard-test-selection/SKILL.md`.
Repository rules and accepted ADRs win over third-party skill defaults.
```
