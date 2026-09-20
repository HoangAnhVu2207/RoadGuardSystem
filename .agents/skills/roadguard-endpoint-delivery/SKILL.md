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

1. Build the changed project with `-nologo -v q -clp:ErrorsOnly`.
2. Run the API with `dotnet watch` and execute the `.http` request; inspect status, body and persisted effect.
3. After smoke succeeds, add 1-3 focused integration tests only for high-risk behavior: money/calculation, authorization, sensitive data, important validation, concurrency/idempotency, SQL behavior, or a reproduced bug.
4. Run the focused filter with quiet output. Run the full relevant suite only before commit/merge or for shared behavior.

Use SQL Server/Testcontainers for SQL Server-specific claims. If the same failure remains after two fix attempts, stop and return the test/build name plus the shortest useful diagnostic; do not broaden scope.

## Completion

Report changed files, build result, real smoke result, focused tests or N/A reason, full-test status, remaining risk, and commit hash if committed. Update only the assigned task row; never rewrite historical `Done` evidence.
