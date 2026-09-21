---
name: roadguard-endpoint-delivery
description: Use when planning, implementing, smoke-testing, or fixing a RoadGuard ASP.NET Core endpoint or its directly required persistence slice.
---

# RoadGuard endpoint delivery (proposed)

Status: Proposed. Target: `.agents/skills/roadguard-endpoint-delivery/SKILL.md`. This draft does not replace the active skill until both owners approve ADR 004 and the A0 application task.

Follow the canonical repository rules in `AGENTS.md`, ADR 001, the accepted form of ADR 004, and the assigned row in one of the two plans. Historical worklogs are evidence, not instructions for new work.

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

For work larger than one endpoint, propose 3-5 independently runnable slices instead of code. Approval covers only the stated slice.

## Endpoint contract

Write 5-8 lines before implementation:

1. Method and route.
2. Authorized actor and project scope.
3. Request fields and important validation.
4. Success status and response DTO (records are allowed).
5. Stable ProblemDetails error codes.
6. Business rule and state change.
7. Persistence, retry, idempotency, or concurrency rule when relevant.
8. Audit and sensitive-data rule when relevant.

## Implementation

- Preserve the accepted flow `Controller -> IService -> IRepository`.
- Controllers bind HTTP input, invoke one application service, and map the result to response DTOs or stable ProblemDetails. Controllers never access a repository or `RoadGuardDbContext` directly.
- Services own use-case orchestration, authorization decisions, cross-aggregate policy, state transitions, and DTO mapping. Services never use EF Core, `RoadGuardDbContext`, `HttpContext`, `IActionResult`, or HTTP status codes.
- Repositories own EF Core/SQL, storage, transactions, retries, idempotency, concurrency, outbox, and persistence backstops. They do not own authorization, workflow policy, business calculations, or HTTP errors.
- A concrete persistence or read-model class injected into a Service requires an interface at the existing repository boundary. Do not add a repository that has no approved use-case consumer.
- New code uses one public type per file. New DTOs, entities, and interfaces always use one public type per file. Do not mechanically split untouched legacy files.
- Read queries use `AsNoTracking()` and projection. Never return an EF entity from the API.
- Do not add MediatR, AutoMapper, a package, schema change, or migration unless the approved scope names it.
- Add or update `Http/<feature>.http` for every endpoint.
- Use async APIs; never `.Result` or `.Wait()`.
- Keep each new or modified file at most 500 lines.

## Verification

Choose the first sufficient tier and add a later tier only when its trigger applies:

1. Documentation/tooling: run only the relevant verifier, link check, or diff check.
2. Code: build the changed project once with `-nologo -v q -clp:ErrorsOnly`.
3. Endpoint: run its relevant `.http` request against the real host and inspect status, DTO body, and persisted effect.
4. High-risk behavior: run 1-3 focused tests for authorization, money/calculation, sensitive data, important validation, concurrency/idempotency, SQL-specific behavior, or a reproduced defect.
5. Shared runtime behavior: run the affected-project suite when several features in that project can be affected.
6. Full-solution tests: reserve for integration, release, or an explicit owner request.

After the required build succeeds, run the selected test breadth with `--no-build --nologo -v q`. Focused, affected-project, and full-solution are mutually exclusive breadths. Zero discovered tests is failure. Local commands do not collect coverage unless the approved task explicitly measures coverage; routine coverage belongs to CI/release.

Reuse passing evidence while source, configuration, dependencies, selection, and environment remain unchanged. After an edit, rerun only checks invalidated by that edit. A commit does not invalidate evidence by itself.

Use SQL Server/Testcontainers for SQL Server-specific claims. If the same failure remains after two fix attempts, stop and return the test/build name plus the shortest useful diagnostic; do not broaden scope.

## Completion

Report changed files, selected tier and reason, commands/results, discovered/pass/fail/skip counts, reused evidence, invalidated checks rerun, remaining risk, and commit hash if committed. Update only the assigned task row; never rewrite historical `Done` evidence.
