---
name: roadguard-endpoint-delivery
description: Use when planning, implementing, smoke-testing, or fixing a RoadGuard ASP.NET Core endpoint or its directly required persistence slice.
---

# RoadGuard endpoint delivery

Follow [repository rules](../../../AGENTS.md) and the assigned row in one of the two plans. Historical worklogs are evidence, not instructions for new work.

## Scope gate

Before any edit, return this card and wait for explicit approval in the same session:

```text
Task / owner / branch:
Goal:
In scope:
Out of scope:
Files and shared hotspots:
Dependencies present in this checkout:
Verification tier and commands:
Packages, migration, data or external side effects:
Reply: Dong y <TASK-ID>
```

For a task larger than one endpoint, propose 3-5 independently runnable slices instead of code. Approval covers only the stated slice.

## Endpoint contract

Write 5-8 lines before implementation:

1. Method and route.
2. Authorized actor and scope.
3. Request fields and important validation.
4. Success status and response record.
5. Stable ProblemDetails error codes.
6. Business rule and state change.
7. Persistence, retry or concurrency rule when relevant.
8. Audit/sensitive-data rule when relevant.

## Implementation

- Prefer one vertical-slice file under `Features/<Name>/<Action>.cs`; use a second file only for a meaningful split.
- Direct `RoadGuardDbContext` access is the target after the architecture-migration task is complete. Until then, preserve the current Controller/Service/Repository boundary.
- Do not add MediatR, AutoMapper, a new repository, package, or migration unless the approved scope names it.
- Read queries use `AsNoTracking()` and projection. Never return an EF entity.
- Return TypedResults/ProblemDetails with stable codes. Use async APIs; never `.Result` or `.Wait()`.
- Add or update `Http/<feature>.http` for every endpoint.
- Keep each new or modified file at most 500 lines.

## Verification

Choose the first sufficient tier and add a later tier only when its trigger applies:

1. Documentation/tooling: run only the relevant verifier, link or diff check.
2. Code: build only the changed project with `-nologo -v q -clp:ErrorsOnly`.
3. Endpoint: execute the relevant `.http` request; inspect status, body and persisted effect.
4. High-risk behavior: run 1-3 focused tests for money/calculation, authorization, sensitive data, important validation, concurrency/idempotency, SQL behavior, or a reproduced bug.
5. Shared runtime behavior: run the affected-project suite when several features in that project can be impacted.
6. Full-solution tests are reserved for integration, release, or an explicit owner request.

Focused, affected-project and full-solution are mutually exclusive breadths. Choose once from the known impact before testing; do not run a narrower breadth first when a broader one is already required.

Reuse a passing result while its source/config/dependencies, test selection and environment are unchanged. After an edit, rerun only the invalidated checks. Do not rerun the same command at handoff or commit merely to make it fresh; a commit is not a test-tier trigger.

Use SQL Server/Testcontainers for SQL Server-specific claims. If the same failure remains after two fix attempts, stop and return the test/build name plus the shortest useful diagnostic; do not broaden scope.

## Completion

Report changed files, selected tier and reason, commands/results, reused evidence, invalidated checks rerun, remaining risk, and commit hash if committed. Update only the assigned task row; never rewrite historical `Done` evidence.
