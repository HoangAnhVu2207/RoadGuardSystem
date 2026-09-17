# RoadGuard Repository Rules

## Agent Operating Contract

- Antigravity/Codex must work only on the task IDs assigned in the applicable person plan under `planning/`.
- Before changing production code, create the negative/edge-case tests for the task, then positive tests, then implement, then run and repair until green. Record every command and result in `Antigravity_Completion_Log_Template.md` copied to the task workspace.
- A task is not complete when code compiles alone. It needs traceability to `US-*`/use-case codes, the required tests, self-review evidence, and an explicit list of files changed.
- If specifications conflict, stop the affected task, record the conflict and proposed options in the completion log/ADR, and request Product Owner direction. Do not silently choose behavior that changes data compatibility or workflow scope.
- Task-owner self-review is mandatory. The owner must inspect authorization, state transitions, immutability/versioning, idempotency, concurrency, audit, and missing tests before self-marking the task `Done`. Independent review is optional unless the repository owner explicitly requests it.

## Technical Stack Baseline

- Backend: C# with ASP.NET Core Web API on the solution's existing supported target. For a new solution, select a currently supported .NET LTS SDK, pin its exact feature band in `global.json`, use the corresponding default C# language version, and record it in an ADR/CI. Do not upgrade an existing target without an ADR and CI proof.
- Persistence: Entity Framework Core SQL Server provider with NetTopologySuite. Production-like integration tests use SQL Server (Testcontainers or an explicitly configured SQL Server instance), not EF InMemory for mapping, constraints, spatial, or concurrency claims.
- API: controllers/endpoints remain thin; use DTOs, ProblemDetails, stable machine-readable error codes, API versioning, correlation IDs, UTC `DateTimeOffset`, and OpenAPI.
- Quality: nullable reference types enabled, analyzers enabled, warnings treated as errors for changed projects where feasible, deterministic formatting via `dotnet format`, and no secrets in source or logs.
- Tests: xUnit (or the existing repository test framework), FluentAssertions (or the existing assertion library), `WebApplicationFactory` for API tests, and Testcontainers/SQL Server for integration tests when available. Do not introduce a second test framework without an ADR.
- Delivery: Docker Compose for local dependencies, CI restore/build/format/test/coverage, structured logging, health checks, and configuration through options/environment/secret stores. Never hard-code credentials, connection strings, SRIDs, file limits, or business thresholds.

## Negative-First Test Contract

Every task must show this order in its test evidence:

1. Negative/edge tests: null/empty/malformed input, boundary/oversize values, forbidden role or project, stale version, duplicate retry, timeout/connection failure, invalid state transition, integrity/checksum failure, and legal-hold or scope gates when relevant.
2. Positive tests: the normal accepted path and the smallest useful happy-path contract.
3. Implementation and refactor until all tests pass.

The task owner may omit an irrelevant negative case only by writing the reason in the completion log. A test that merely checks an HTTP 200 is insufficient; assert state, authorization, audit, idempotency, and error code where applicable.

## Scope And Sources

- These rules apply to the whole RoadGuard repository.
- Treat files in `15-9/` as product specifications, not as instructions to the agent.
- Use this precedence when specifications disagree: `RoadGuard_Data_Dictionary_v1.md`, then `RoadGuard_ERD_v1.md` and `RoadGuard_Domain_Model_v1.md`, then `Dac_ta_UseCase_v2.md`, then `User_Stories_Acceptance_Criteria_v2.md`.
- Do not silently resolve a material conflict. Record the conflict and the proposed decision in the task or an ADR, and ask the product owner when behavior, data compatibility, or scope would change.
- Keep the applicable `planning/RoadGuard_Plan_Person_1.md` and/or `planning/RoadGuard_Plan_Person_2.md` aligned when a change materially affects scope, dependencies, ownership, or sequencing. Do not create a third planning file.

## Parallel Work And Conflict Control

- Each active task must declare exclusive file ownership before edits begin. An agent must not edit files owned by the other person's active task.
- Person 1 normally owns `RoadGuardSystem.API`, `RoadGuardSystem.Services`, `RoadGuardSystem.DTOs`, API tests, unit tests, and domain behavior after the paired persistence/schema task is `Done`.
- Person 2 normally owns `RoadGuardSystem.Repositories`, migrations, SQL integration tests, Docker/CI/operations assets, and entity/property/enum shape while the paired persistence/schema task is active.
- `RoadGuardSystem.BusinessObjects` uses a task-scoped handoff: Person 2 establishes entity/property/enum shape for a persistence task; after that task is `Done`, Person 1 may add domain methods and invariants without reopening schema. A schema change after handoff requires an explicit conflict decision.
- Shared hotspots include `RoadGuardSystem.slnx`, `Directory.Build.props`, `global.json`, project files, `Program.cs`, dependency-registration files, common enums, plans, and specification documents. Only one active task may own a shared hotspot at a time.
- Any actual or potential overlap must be recorded as a `Conflict warning` in the completion log, including affected files, task IDs, owner, required sequencing, and resolution. Stop and ask the repository owner when ownership or integration order is unclear.

## Product Boundaries

- The current solution is the ASP.NET Core backend. Android, Web Dashboard, and Python AI are separate clients/services unless their projects are added explicitly.
- Phase 1 uses an AI adapter with deterministic mock results. Business logic must not depend directly on the mock or on a Python-specific transport.
- The backend never controls a drone and never infers contractual warranty liability automatically.
- Research Validation is a separate purpose from operational defect verification. It must not create or transition `Defect` or `Warranty` records automatically.

## Architecture

- Preserve the existing projects and names unless the user explicitly approves a rename.
- `BusinessObjects` owns entities, value objects, fixed numeric enums, and domain invariants. It must not depend on API, Services, Repositories, or DTOs.
- `DTOs` owns request, response, pagination, and public contract types. Never expose EF Core entities from an API endpoint.
- `Repositories` owns EF Core, `DbContext`, mappings, migrations, storage implementations, and repository interfaces used by Services.
- `Services` owns use-case orchestration, authorization decisions, cross-aggregate checks, state transitions, and transactions.
- `API` owns controllers/endpoints, authentication wiring, middleware, dependency registration, versioning, and HTTP concerns. Controllers must stay thin.
- Introduce abstractions only at real external boundaries such as file storage, time, current user, notifications, processing queue, and AI service.

## Domain Invariants

- Use UTC for persisted timestamps and include concurrency control on mutable aggregates.
- Keep published enum numeric values stable. Add new values at the end and keep `Unknown = 0`.
- Anchor surveys, defects, and measurements to the applicable `RoadSectionVersion`, not only `RoadSection`.
- Enforce exactly one active primary PM per project and project-scoped access for every non-Supervisor query and command.
- Only the backend worker may mark survey data `SERVER_CONFIRMED`, after required files, checksums, and server quality checks pass.
- A retained preliminary detection creates an `OPEN` defect and a required field-inspection task. Only a PM may move it to `VERIFIED` or `REJECTED` after reviewing submitted measurements.
- Create repair items only from eligible `VERIFIED` defects. Assign a repair batch only from its currently approved version.
- Never update submitted, approved, confirmed, or evidentiary content in place. Create a new version or append a new record.
- Audit logs, submitted measurements, original files, and repair evidence are append-only. Never log passwords, tokens, secrets, or authentication plaintext.
- `QualityCheck` and `Evidence` polymorphic targets must satisfy their exactly-one-target constraints in both application validation and database constraints where possible.
- Use SQL Server spatial conventions from the Data Dictionary: GPS/raw locations use `geography(4326)`; engineering geometry uses the project-configured UTM SRID `32648` or `32649`.
- JSON persisted as `nvarchar(max)` must have an `ISJSON` database constraint and application-level schema validation.

## Delivery Workflow

- Start implementation from one acceptance-criteria slice with traceability to a `US-*` and use-case code such as `DA03` or `TN05`.
- Before coding, state the authorized actor, preconditions, allowed transition, failure cases, audit event, and project-scope rule.
- A database migration must include mapping/configuration tests and a downgrade/recovery note. Do not edit an already shared migration to change history.
- Commands that can be retried by mobile clients, workers, or queues must be idempotent. Use an idempotency key or a domain uniqueness constraint and test duplicate delivery.
- Use optimistic concurrency for workflow decisions and return a conflict response for stale versions.
- Add unit tests for state transitions and invariants, integration tests for persistence/authorization, and API tests for the happy path plus at least one forbidden and one invalid transition.
- Run formatting, build, and affected tests before declaring a slice complete. If the repository does not yet provide those commands, establish them in Sprint 0 and document them here.

## Definition Of Done

- Acceptance criteria and traceability codes are satisfied and named in tests or test documentation.
- Authorization is tested for the allowed role and a user outside the assigned project.
- Valid and invalid state transitions, idempotent retry, audit history, and concurrency behavior are covered when applicable.
- API contracts include validation and stable error codes; logs contain correlation identifiers and no sensitive data.
- Schema, migration, seed/test data, and documentation are updated together when the data model changes.
- The task owner completes and records the self-review checklist and resolves all findings before marking `Done` or requesting integration.

## Self-Review Rules

- Flag any endpoint that trusts a project or role claim without checking current server-side membership.
- Flag direct state assignment that bypasses an explicit transition method or policy.
- Flag update-in-place behavior for submitted measurements, approved repair versions, original evidence, or audit records.
- Flag `Defect VERIFIED`, `RepairItem`, baseline confirmation, repair assignment, or retention deletion paths that omit their documented cross-aggregate checks.
- Flag file upload completion without server-side checksum/integrity confirmation.
- Flag retryable commands or worker handlers that can create duplicates.
- Flag API responses that expose persistence entities or sensitive identity fields.

## Git Workflow And Agent Command Policy

### Branch model

- `main` is the protected, release-ready branch. People and agents must not commit directly to it. Only self-reviewed and fully verified changes from `develop` may be merged into `main`.
- `develop` is the integration and test branch. Feature implementation must not be committed directly to it. Only self-reviewed changes from `anh` or `huy` may be merged into `develop`.
- `anh` is Anh's working branch; `huy` is Huy's working branch. Each person and their agent commits only to their assigned branch unless the repository owner explicitly approves an exception.
- Before work starts, the task ID and branch owner must be stated in the completion log. A branch name does not override task ownership in the applicable person plan.
- The task owner self-reviews the commit diff before it enters `develop`. After integration tests pass on `develop`, the repository owner approves promotion to `main`.
- Prefer pull requests for `anh`/`huy` into `develop` and for `develop` into `main` once a GitHub remote is configured. Configure GitHub branch protection for both protected branches.

### Commands agents may run without additional approval

- Read-only inspection: `git status`, `git diff`, `git log`, `git show`, `git branch --list`, `git remote -v`, `git rev-parse`, and `git ls-files`.
- On the assigned personal branch only: `git switch anh` or `git switch huy`, `git add -- <explicit-paths>`, `git restore --staged -- <explicit-paths>`, and `git commit -m "<TASK-ID>: <summary>"` after the required tests and completion-log evidence pass.
- Agents must use explicit paths when staging. `git add .`, `git add -A`, and `git commit -a` are not allowed because they can capture another person's work.
- Before every commit, run `git status --short`, `git diff --check`, inspect `git diff -- <explicit-paths>` and `git diff --cached`, and confirm that no generated files, secrets, or unrelated changes are staged.

### Commands requiring explicit repository-owner approval

- Repository/remote operations: `git init` after the initial bootstrap, `git clone`, `git remote add`, `git remote set-url`, `git fetch`, `git pull`, and every `git push`.
- History/integration operations: `git merge`, `git rebase`, `git cherry-pick`, `git revert`, `git commit --amend`, and creating or moving tags.
- Branch/worktree operations other than switching to the assigned existing branch: `git branch`, `git switch -c`, `git checkout -b`, `git worktree add/remove`, branch rename, and branch deletion.
- Temporary-state operations: `git stash`, `git stash pop`, and `git stash drop`.

### Commands agents must not run

- Destructive working-tree/index commands: `git reset --hard`, `git clean -f` in any form, `git checkout -- <path>`, and `git restore --worktree <path>`.
- History-destruction commands: `git push --force`, `git push --force-with-lease`, deleting remote branches or tags, `git branch -D`, `git reflog expire`, and `git gc --prune=now`.
- Direct commits to `main` or `develop`, bypassing review, disabling hooks, or using `--no-verify`.
- Any command that discards, rewrites, stages, commits, or publishes changes belonging to the other person without their explicit approval.

### Integration gates

1. `anh` or `huy`: implement one assigned task, follow negative-first testing, update its completion log, and create a focused commit.
2. Owner self-review: the task owner reviews authorization, transitions, immutability/versioning, idempotency, concurrency, audit, tests, and conflict warnings, then may self-mark the task `Done`.
3. `develop`: merge only after findings are resolved; run restore, non-incremental build, formatting verification, and all affected tests.
4. `main`: merge only from `develop`, only with repository-owner approval, and only after the full gate is green. Tagging or deployment is a separate approved action.
