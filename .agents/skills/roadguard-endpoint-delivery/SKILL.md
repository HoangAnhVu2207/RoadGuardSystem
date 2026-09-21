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

- Preserve the accepted flow `Controller -> IService -> IRepository`.
- Controllers bind HTTP input, invoke one application service, and map the result to response DTOs or stable ProblemDetails. Controllers never access a repository or `RoadGuardDbContext` directly.
- Services own use-case orchestration, authorization decisions, cross-aggregate policy, state transitions, and DTO mapping. Services never use EF Core, `RoadGuardDbContext`, `HttpContext`, `IActionResult`, or HTTP status codes.
- Repositories own EF Core/SQL, storage, transactions, retries, idempotency, concurrency, outbox, and persistence backstops. They do not own authorization, workflow policy, business calculations, or HTTP errors.
- A concrete persistence or read-model class injected into a Service requires an interface at the existing repository boundary. Do not add a repository that has no approved use-case consumer.
- New code uses one public type per file. New DTOs, entities, and interfaces always use one public type per file. Do not mechanically split untouched legacy files.
- `RoadGuardSystem.BusinessObjects` is limited to one entity per domain file, `Common/Enums.cs`, and enum extensions in `Common/Extensions/`. Entity-local invariants, factories, normalization, and state transitions are allowed; `*Options`, validators, sanitizers, constants helpers, infrastructure interfaces, EF/HTTP/configuration, and direct clock/random access belong in Services or Repositories.
- `RoadGuardSystem.DTOs` contains request/response contracts only. Use one public DTO per file from the first addition; data properties/records and validation attributes are allowed. Business/computed logic, data access, clocks/random generation, and Repository/Service/EF/HTTP references are forbidden. Services own DTO mapping.
- `RoadGuardSystem.Repositories` owns `RoadGuardDbContext`, EF query/write, mappings, transactions, idempotency, rowversion/concurrency, outbox, storage, and persistence backstops. A Service injects persistence/read models only by their interfaces. Repositories return facts, never authorization/scope/workflow/calculation/HTTP decisions, and never reference `RoadGuardSystem.DTOs` in source even though ADR 001 permits the project reference.
- `RoadGuardSystem.Services` owns use-case orchestration, business/state policy, authorization/scope decisions, stable business result codes, and DTO mapping. Controllers call `IService`; Services use repository interfaces. `HttpContext`, MVC/controller result types, EF Core, and `RoadGuardDbContext` are forbidden in Service source and locked by an architecture test.
- Respect the verified direct project-reference matrix: `BusinessObjects -> (none)`, `DTOs -> BusinessObjects`, `Repositories -> BusinessObjects, DTOs` under ADR 001 (but no DTO source reference), `Services -> Repositories`, and `API -> Services`. Transitive availability does not authorize a forbidden source dependency.
- File placement gate: before creating source, run `rg --files` in the target layer, use the closest existing ownership folder, and include the exact path in scope. Repository/Service technical concerns use sibling functional folders (`Extensions`, `Options`, `Factories`, `Generators`, `Configurations`, `Concurrency`, `Transactions`, `Storage`, `Seeding`, `Migrations`); feature seams use the established `Interfaces/<Domain>/` and `Implementations/<Domain>/` layout. Do not invent catch-all folders, another interface/implementation tree, root files, speculative/duplicate files, or empty directories; remove empty legacy folders in the approved relocation scope.
- Do not add MediatR, AutoMapper, a package, schema change, or migration unless the approved scope names it.
- Read queries use `AsNoTracking()` and projection. Never return an EF entity.
- Return TypedResults/ProblemDetails with stable codes. Use async APIs; never `.Result` or `.Wait()`.
- Add or update `RoadGuardSystem.API/RoadGuardSystem.API.http` for every endpoint.
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

Use SQL Server/Testcontainers for SQL Server-specific claims. Routine local commands do not collect coverage unless the approved task explicitly measures coverage; routine coverage belongs to CI/release. If the same failure remains after two fix attempts, stop and return the test/build name plus the shortest useful diagnostic; do not broaden scope.

## Completion

Report changed files, selected tier and reason, commands/results, reused evidence, invalidated checks rerun, remaining risk, and commit hash if committed. Update only the assigned task row; never rewrite historical `Done` evidence.
