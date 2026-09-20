# RoadGuard Working Rules

## Start And Scope
- Start with `git status --short --branch`, the assigned row in `planning/`, and real dependencies in this checkout.
- Historical `Done` rows and worklogs are evidence. Never rewrite them to match the current workflow.
- Before edits, show: task/owner, goal, In scope, Out of scope, exact files, dependencies, verification tier, and side effects. Wait for explicit owner approval in this session.
- For work larger than one endpoint, propose 3-5 small slices and wait for approval before code.
- Preserve unrelated and uncommitted work. Shared files have one declared owner at a time.

## Endpoint Workflow
- Write a 5-8 line contract: route, actor, input/validation, success output/status, stable errors, business rule, persistence/idempotency, and audit/sensitive-data rule when relevant.
- Target architecture after the separately approved migration is Minimal API vertical slices in `Features/<Name>/<Action>.cs`, normally 1-2 files per endpoint, with direct `RoadGuardDbContext` use.
- Until that migration is completed, follow the current Controller/Service/Repository structure; do not mix architectures silently.
- Do not add Repository, MediatR or AutoMapper abstractions. Extract a service only when logic is genuinely complex or shared.
- Every new endpoint adds a runnable request in `Http/*.http`; return response records, never EF entities.
- Use ProblemDetails/stable error codes, `AsNoTracking()` for reads, projection to avoid N+1, UTC `DateTimeOffset`, and `await` only.
- New or modified files must stay at or below 500 lines. Do not expand an existing oversized file; splitting it needs its own approved scope.

## Verification Ladder
- Select the cheapest sufficient tier in the approved scope. Documentation/tooling changes run only their relevant verifier, link or diff check; they do not trigger runtime tests.
- For code, build only the changed project first. For an endpoint, then run its relevant `.http` request against the real response.
- Add 1-3 focused tests after the smoke call only for money/calculation, authorization, sensitive data, important validation, concurrency/idempotency, SQL-specific behavior, or a reproduced bug.
- Choose one test breadth before running tests: focused, affected-project, or full-solution. They are alternatives, not a sequence; when a broader breadth is known to be required, skip the narrower run.
- Test only affected features/projects. Expand to an affected-project suite only when shared runtime behavior can affect multiple features. Full-solution tests are reserved for integration, release, or an explicit owner request.
- Reuse a passing result while its source/config/dependencies, test selection and environment are unchanged. After a change, rerun only checks it invalidated. A commit alone does not require more tests, and handoff must not repeat an already-valid command.
- SQL Server spatial, constraints, migrations and rowversion require SQL Server/Testcontainers; SQLite is not proof for them.
- If the same failure survives two fix attempts, stop, report the short error and ask the owner whether to restore/revert or open a fresh session.

## Ownership And Safety
- Person 1 (`anh`) owns endpoint/API feature files, HTTP examples and API-focused tests after required Person 2 schema tasks are `Done`.
- Person 2 (`huy`) owns entity shape, `DbContext`, mappings, migrations, SQL tests, seed, Docker and CI. Schema work and endpoint work use separate task scopes.
- Do not add packages, create/run/edit migrations, delete data, or change schema without explicit approval.
- Commit only on the assigned `anh` or `huy` branch, stage explicit paths, and inspect status/diffs first. Merge, rebase, pull, push, tags, branch/worktree changes, stash and destructive restore require owner approval.
- Before commit, verify that the selected evidence is still valid; never treat zero discovered or skipped required tests as a pass.
