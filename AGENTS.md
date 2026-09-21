# RoadGuard Working Rules

## Start And Scope
- Start with `git status --short --branch`, the assigned row in `planning/`, and real dependencies in this checkout.
- Historical `Done` rows and worklogs are evidence. Never rewrite them to match the current workflow.
- Before edits, show: task/owner, goal, In scope, Out of scope, exact files, dependencies, verification tier, and side effects. Wait for explicit owner approval in this session.
- For work larger than one endpoint, propose 3-5 small slices and wait for approval before code.
- Preserve unrelated and uncommitted work. Shared files have one declared owner at a time.

## Endpoint And Layer Workflow
- Write a 5-8 line contract: route, actor, input/validation, success output/status, stable errors, business rule, persistence/idempotency, and audit/sensitive-data rule when relevant.
- Follow the accepted N-layer flow `Controller -> IService -> IRepository` and ADR 001 plus ADR 004. Do not introduce a second production architecture silently.
- Controllers remain thin and never access Repositories or `RoadGuardDbContext`. Services own use-case policy and never use EF or HTTP types. Repositories own persistence mechanisms and never own business or HTTP decisions.
- Every concrete persistence/read-model class injected into a Service requires an interface. Do not add MediatR or AutoMapper.
- New code uses one public type per file; new DTOs, entities, and interfaces always do. Leave untouched legacy multi-type files unchanged.
- `RoadGuardSystem.BusinessObjects` contains only domain entities, `Common/Enums.cs`, and enum extension files under `Common/Extensions/`. Each entity has its own file and belongs to its related domain folder.
- Entity-local invariants, normalization, factories, and state transitions are allowed. Do not add `*Options`, validators, sanitizers, persistence/EF/HTTP/configuration helpers, constants classes, repository/service interfaces, direct clock/random access, or infrastructure code to BusinessObjects; place them in the owning Service or Repository functional folder.
- `RoadGuardSystem.DTOs` contains API request/response contracts only. Every DTO has one public type per file from its first addition; records/properties and validation attributes are allowed.
- DTOs never contain business logic, computed behavior, data access, clocks/random generation, or references to Repositories/Services/EF/HTTP. Services map between entities, repository facts, and DTOs.
- `RoadGuardSystem.Repositories` owns `RoadGuardDbContext`, EF queries/entity persistence, mappings, transactions, idempotency, rowversion/concurrency, outbox, storage, and persistence backstops. Every concrete persistence/read-model class injected by a Service has a repository interface.
- Repositories never decide authorization, project scope, role/status workflow, business calculations, or HTTP/stable error codes. ADR 001 permits the Repositories-to-DTOs project reference, but no Repository source `.cs` file may reference the `RoadGuardSystem.DTOs` namespace; the architecture test enforces this.
- `RoadGuardSystem.Services` owns use-case orchestration, business and state rules, authorization/scope decisions, stable business result codes, and DTO mapping. Controllers call Services through `IService` interfaces; Services call persistence/read models through repository interfaces.
- Services never reference `HttpContext`, MVC/controller result types, EF Core, or `RoadGuardDbContext`. `ServiceSourceBoundaryTests` enforces the HTTP/MVC/DbContext boundary.
- Direct project-reference matrix: BusinessObjects has no project references (only the verified `Microsoft.Extensions.Identity.Stores` model package and `NetTopologySuite`); DTOs references BusinessObjects; Repositories references BusinessObjects and DTOs under ADR 001; Services references Repositories; API references Services. DTO/BusinessObject types are available transitively to higher layers, but source boundaries still apply.
- Every new endpoint adds a runnable request in `RoadGuardSystem.API/RoadGuardSystem.API.http`; return response DTOs (records allowed), never EF entities.
- Use ProblemDetails/stable error codes, `AsNoTracking()` for reads, projection to avoid N+1, UTC `DateTimeOffset`, and `await` only.
- New or modified files must stay at or below 500 lines. Do not expand an existing oversized file; splitting it needs its own approved scope.
- Repository rules and accepted ADRs win over third-party skill defaults.

## File Placement Gate
- Before creating any production or test source file, search the target layer with `rg --files`, find the closest existing type, and state the exact new path in the approved scope. Do not create a file merely because a name or folder would be convenient.
- Repository and Service functional folders are root-level siblings of domain folders: `Extensions`, `Options`, `Factories`, `Generators`, `Configurations`, `Concurrency`, `Transactions`, `Storage`, `Seeding`, and `Migrations`. Place a technical type in its functional folder; place feature policy, contract, fact, or implementation in its domain ownership area.
- `Interfaces/<Domain>/` and `Implementations/<Domain>/` are the established Repository/Service seam layout. Put Service-injected repository contracts and service contracts in `Interfaces/<Domain>/`; put their concrete persistence/read-model or service implementation and domain-local command/result types in `Implementations/<Domain>/`. Do not create another generic interface/implementation tree.
- A type's responsibility chooses its folder. Do not create a catch-all `Helpers`, `Utils`, `Common`, `Shared`, `Models`, `Managers`, a root-level source file, or a parallel domain folder unless an approved task explicitly names the folder, its responsibility, and the files it owns.
- Never create an empty directory. After a relocation, remove an empty legacy domain directory in the same approved scope; do not leave placeholders for a possible future type.
- Reuse or extend an existing focused file only when it remains within the 500-line limit and the responsibility is identical. Otherwise create one named type per file at the approved location; never create speculative, empty, duplicate, or compatibility files.

## Verification Ladder
- Select the cheapest sufficient tier in the approved scope. Documentation/tooling changes run only their relevant verifier, link or diff check; they do not trigger runtime tests.
- For code, build only the changed project first. After a successful build, run the one selected test breadth with `--no-build --nologo -v q`.
- Add 1-3 focused tests after the smoke call only for money/calculation, authorization, sensitive data, important validation, concurrency/idempotency, SQL-specific behavior, or a reproduced bug.
- Choose one test breadth before running tests: focused, affected-project, or full-solution. They are alternatives, not a sequence; when a broader breadth is known to be required, skip the narrower run.
- Test only affected features/projects. Expand to an affected-project suite only when shared runtime behavior can affect multiple features. Full-solution tests are reserved for integration, release, or an explicit owner request.
- Coverage is CI/release-only unless an approved scope explicitly requests a local coverage measurement. Use `roadguard-test-selection` to select the sufficient breadth. Zero discovered tests or skipped required tests is not a pass.
- Reuse a passing result while its source/config/dependencies, test selection and environment are unchanged. After a change, rerun only checks it invalidated. A commit alone does not require more tests, and handoff must not repeat an already-valid command.
- SQL Server spatial, constraints, migrations and rowversion require SQL Server/Testcontainers; SQLite is not proof for them.
- If the same failure survives two fix attempts, stop, report the short error and ask the owner whether to restore/revert or open a fresh session.

## Ownership And Safety
- Person 1 (`anh`) owns endpoint/API feature files, HTTP examples and API-focused tests after required Person 2 schema tasks are `Done`.
- Person 2 (`huy`) owns entity shape, `DbContext`, mappings, migrations, SQL tests, seed, Docker and CI. Under `Repositories/Implementations`, Person 2 owns schema/mapping/migration and persistence infrastructure; Person 1 may add an approved endpoint-specific read-only repository method only when its scope card names the exact files.
- Do not add packages, create/run/edit migrations, delete data, or change schema without explicit approval.
- Commit only on the assigned `anh` or `huy` branch, stage explicit paths, and inspect status/diffs first. Merge, rebase, pull, push, tags, branch/worktree changes, stash and destructive restore require owner approval.
- Before commit, verify that the selected evidence is still valid; never treat zero discovered or skipped required tests as a pass.
