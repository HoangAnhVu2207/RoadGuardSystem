# RoadGuard Working Rules

## Start, Sources, And Scope

- Start with `git status --short --branch`, the assigned `planning/V2/Person_*/V2-*.md` task (or the assigned legacy plan row when no V2 task exists), and real dependencies in this checkout.
- Apply instructions in this order: current owner request and approval; this `AGENTS.md`; accepted ADRs with later amendments winning; current source/migrations/tests/runtime evidence; assigned V2 task and V2 OpenAPI draft; historical plans/worklogs as evidence only.
- `PROPOSED_CONTRACT`, `NEEDS_REPO_CHECK`, and `BLOCKED_SLICE` are not proof of current behavior or approval. Record current versus proposed behavior and stop for a decision when compatibility, schema, authorization, workflow, or an open gate would change.
- Historical `Done` rows and worklogs are evidence. Never rewrite them to match the current workflow.
- Before edits, show: task/owner/branch, goal, In scope, Out of scope, exact files and shared hotspots, dependencies present, verification breadth and commands, and package/migration/data/external side effects. Wait for explicit owner approval in this session unless that exact scope was already approved.
- For work larger than one endpoint, propose 3-5 independently runnable slices and wait for approval before code.
- Preserve unrelated and uncommitted work. Do not overwrite another active task's shared file reservation.

## Skill Routing

- Use `.agents/skills/roadguard-endpoint-delivery/SKILL.md` to plan, implement, fix, review, or smoke-test one endpoint.
- Load `.agents/skills/roadguard-persistence/SKILL.md` whenever the approved endpoint changes an EF query, mapping, atomic write, entity shape, migration, SQL concurrency, idempotency, outbox, storage, or offline durability.
- Use `.agents/skills/roadguard-test-selection/SKILL.md` before selecting or running verification.
- Use `.agents/skills/roadguard-postman/SKILL.md` whenever an API is added, changed, removed, or prepared for manual/collection-run testing; update the existing collection in the same task.
- Repository rules and accepted ADRs win over skill defaults. Skills do not expand task scope or approve proposed product behavior.

## Source Evidence And Task Lifecycle

- Read `planning/V2/TASK_LIFECYCLE.md` for every V2 API or governance task. The task file is the durable checkpoint for resume/compaction; do not rely on chat memory alone.
- Before edits, record `Source evidence` in the task: exact path plus heading/ID for decisions, BR/FR/UC/US/AC, PF/SQ/state, OpenAPI operation/schema, DD/ERD and current source/tests actually used. Record the base revision and canonical contract hash when relevant; do not hash unrelated files.
- Use `deliveryStatus` only for task progress (`TODO`, `IN_PROGRESS`, `PARTIAL`, `BLOCKED`, `DONE`, `REOPENED`). Keep it separate from contract, implementation and verification status.
- Move to `IN_PROGRESS` when approved work starts. After each slice, immediately append completion history with changed files, acceptance evidence, exact checks/results, reused/invalidated evidence, side effects and unverified risks; mirror status in the operation manifest or governance index.
- Mark `DONE` only when every required gate in the approved scope passes. Use `PARTIAL` or `BLOCKED` with exact affected tasks/gates when required evidence is missing. Reopening appends history and preserves prior DONE evidence.
- Historical task rows, completion records and PASS evidence are append-only in meaning. Never rewrite them to make current validation appear older or broader than it is.

## Endpoint Contract And Delivery

- Before implementation, write a 5-8 line contract: route/method, actor/scope, input/validation, success output/status, stable errors, business/state rule, persistence/idempotency/concurrency, and audit/sensitive-data handling when relevant.
- Compare the V2 proposal with current source for route/method, ProblemDetails/envelope, error codes, pagination, idempotency key, concurrency token placement, authorization scope, and state transitions. Preserve current behavior until an incompatible delta is approved.
- Deliver one endpoint through the accepted `Controller -> IService -> IRepository` flow. Do not introduce Minimal APIs, MediatR, AutoMapper, direct Controller-to-Repository access, or direct `RoadGuardDbContext` access.
- Controllers bind HTTP input, extract the actor, invoke Services through interfaces, and map service results to DTOs or stable ProblemDetails. They do not own workflow or persistence decisions.
- Services own use-case orchestration, authorization/scope decisions, business/state rules, stable business result codes, and DTO mapping. They never use EF Core, `RoadGuardDbContext`, `HttpContext`, MVC/controller result types, or HTTP status decisions.
- Repositories own EF/SQL queries and writes, mappings, transactions, retries, idempotency, rowversion/concurrency, outbox, storage, and persistence backstops. They return facts and never decide authorization, workflow policy, business calculations, or HTTP/stable error codes.
- Every concrete persistence/read-model class injected into a Service requires an interface at the existing repository boundary. Do not add a repository without an approved use-case consumer.
- Read queries use `AsNoTracking()`, projection, bounded ordering/pagination, and no N+1 behavior. Never return an EF entity from an endpoint.
- Use UTC `DateTimeOffset`, cancellation-aware async APIs, and never `.Result` or `.Wait()`.
- Every new or changed endpoint adds a runnable request to `RoadGuardSystem.API/RoadGuardSystem.API.http`, using environment values and fixture IDs rather than committed secrets.
- API delivery follows `IMPLEMENT -> UPDATE_POSTMAN -> REVIEW_FIX -> VERIFY -> REPORT`; Postman is a completion criterion, not a separate approval round. Preserve request names/IDs and report static collection checks separately from runtime/API evidence.

## Layer And File Placement

- New code uses one public type per file; new DTOs, entities, and interfaces always do. Leave untouched legacy multi-type files unchanged.
- `RoadGuardSystem.BusinessObjects` contains only domain entities, `Common/Enums.cs`, and enum extensions under `Common/Extensions/`. Entity-local invariants, normalization, factories, and state transitions may use only entity state and caller-supplied values.
- Do not put `*Options`, validators, sanitizers, infrastructure constants/interfaces, persistence/EF/HTTP/configuration helpers, or direct clock/random access in BusinessObjects.
- `RoadGuardSystem.DTOs` contains API request/response contracts only. Records/properties and validation attributes are allowed; business/computed behavior, data access, clocks/random, and Repository/Service/EF/HTTP references are forbidden. Services map entities/facts to DTOs.
- `RoadGuardSystem.Repositories` owns `RoadGuardDbContext`, EF persistence, mappings, transactions, concurrency, idempotency, outbox, storage, and persistence backstops. Repository source `.cs` files must not reference the `RoadGuardSystem.DTOs` namespace even though ADR 001 permits the project reference.
- `RoadGuardSystem.Services` owns application policy and orchestration through repository interfaces. Keep `ServiceSourceBoundaryTests` and repository source boundary tests green.
- Direct project-reference matrix: BusinessObjects has no project references beyond its verified model packages; DTOs references BusinessObjects; Repositories references BusinessObjects and DTOs under ADR 001; Services references Repositories; API references Services. Transitive availability does not authorize a forbidden source dependency.
- Before creating production or test source, run `rg --files` in the target layer, find the closest existing responsibility, and list the exact new path in approved scope.
- Repository/Service technical concerns use root-level sibling folders: `Extensions`, `Options`, `Factories`, `Generators`, `Configurations`, `Concurrency`, `Transactions`, `Storage`, `Seeding`, and `Migrations`.
- Feature seams use the established `Interfaces/<Domain>/` and `Implementations/<Domain>/` layout. Do not create another interface/implementation tree, root-level source files, parallel domains, or catch-all `Helpers`, `Utils`, `Common`, `Shared`, `Models`, or `Managers` folders.
- Never create an empty, speculative, duplicate, or compatibility file. Remove an empty legacy folder in the same approved relocation scope.
- Reuse a focused file only when responsibility is identical and it remains at most 500 lines. Do not expand an oversized file; splitting it requires approved scope.

## V2 Ownership And Shared Hotspots

- Under ADR 006, the owner named by an approved V2 task owns the complete required endpoint slice, including directly required DTO, Service, Repository, entity, mapping, migration, SQL test, API test, and HTTP example changes.
- Vertical ownership changes delivery responsibility, not N-layer boundaries. Change only layers required by the source comparison.
- Shared hotspots include `RoadGuardDbContext`, mappings, migrations and snapshot, project references, shared DI, shared errors, OpenAPI, seed, Docker, and CI. Name them in the scope card and allow one active writer per hotspot.
- Concurrent schema tasks reserve and sequence migrations first. Person 2 coordinates migration order, SQL integration, seed, Docker, CI, and release evidence without exclusively owning endpoint persistence.
- Do not add packages, create/edit/run migrations, change schema, apply database changes, or delete data unless the approved scope explicitly names the effect. Permission to draft a migration is not permission to apply it to a live database. Never edit an applied migration.

## Verification Ladder

- Select the cheapest sufficient breadth before running tests: focused, affected-project, or full-solution. These are alternatives; a focused breadth may include multiple projects needed to cover all changed risks.
- Documentation/tooling changes run only their relevant validator, link, or diff checks; they do not trigger runtime tests.
- For code, build the changed production project first with `-nologo -v q -clp:ErrorsOnly` when the repository gate requires it.
- Before `dotnet test --no-build`, build each selected test project and its referenced dependencies from current source with matching configuration/framework. A production-only build does not prove test binaries are fresh.
- Endpoint work requires a real smoke call that inspects status, body, relevant headers, and durable effects. Authorization changes also check wrong-actor/wrong-scope behavior.
- Add approximately 1-3 focused high-value tests for money/calculation, authorization, sensitive data, important validation, concurrency/idempotency, SQL behavior, or a reproduced bug; cover every changed invariant even when that requires more.
- Shared auth, serializer, middleware, or cross-feature runtime changes use affected-project suites. Full-solution tests are reserved for broad integration, release, project-reference changes, or explicit owner request.
- Coverage is CI/release-only unless approved scope explicitly requests local coverage. Zero discovered tests, invalid filters, absent required projects, or skipped required tests is a failed gate.
- SQL Server spatial behavior, constraints, migrations, rollback, uniqueness, and rowversion require SQL Server/Testcontainers evidence; mocks or SQLite are not proof.
- Reuse passing evidence only while source/config/dependencies, selected breadth, binaries, and environment remain unchanged. After an edit, rerun only invalidated checks. A commit alone does not invalidate evidence.
- If the same failure survives two fix attempts, stop and report the command plus shortest useful diagnostic. Do not weaken assertions, skip required tests, or broaden implementation merely to obtain green output.

## Completion And Git Safety

- Report changed files, selected verification breadth and reason, exact commands/configuration/filter, executed/pass/fail/required-skip counts, environment, smoke/durable-effect result, reused evidence, invalidated checks rerun, unverified risks, and commit hash only if committed.
- Update only the assigned task status/evidence after its required gates pass. Never mark Done from a template, static skill validation, zero-test run, or unresolved blocker.
- Do not wait for the owner to remind you to update status. Follow `planning/V2/TASK_LIFECYCLE.md` and update the assigned task/index as soon as its gate result is known.
- Commit only on the assigned `anh` or `huy` branch. Stage explicit paths and inspect status/diffs first.
- Merge, rebase, pull, push, tags, branch/worktree changes, stash, destructive restore, migration application, and data deletion require explicit owner approval.
