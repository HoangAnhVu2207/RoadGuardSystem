# Source map — verified against the supplied RoadGuard archive

Recheck paths in the active checkout; the archive is evidence of structure, not of current runtime health.

| Root folder | Project file | Responsibility |
|---|---|---|
| RoadGuardSystem.BusinessObjects | RoadGuardSystem.aBusinessObjects.csproj | Domain entities and existing enum/extension files |
| RoadGuardSystem.DTOs | RoadGuardSystem.bDTOs.csproj | One request/response DTO per file |
| RoadGuardSystem.Repositories | RoadGuardSystem.cRepositories.csproj | Interfaces, facts, EF/SQL, transactions/storage |
| RoadGuardSystem.Services | RoadGuardSystem.dServices.csproj | Interfaces, use-case policy and orchestration |
| RoadGuardSystem.API | RoadGuardSystem.eAPI.csproj | Controllers, HTTP/error mapping, authentication composition and DI |

Direct references: BusinessObjects → none; DTOs → BusinessObjects; Repositories → BusinessObjects, DTOs; Services → Repositories; API → Services. Repositories' DTO project reference is an ADR exception, **not** permission to use the DTO namespace in repository source. Transitive references do not relax source boundaries.

The supplied `global.json` pins SDK **10.0.401**, latestPatch roll-forward. Production csproj files target **net8.0**; EF Core is **8.0.17**. SDK and target framework are distinct. Read actual files before commands; do not install/upgrade/change these values without scope.

Use `Interfaces/<Domain>/` and `Implementations/<Domain>/` for feature seams. Use existing sibling technical folders such as Options, Factories, Generators, Extensions, Configurations, Transactions, Concurrency, Storage, Seeding and Migrations. Reuse an existing contract file if the legacy interface is there; do not create a duplicate because a preferred filename is absent.

Examples to locate selectively:

- `RoadGuardSystem.API/Controllers/ProjectsController.cs`
- `RoadGuardSystem.Services/Implementations/Projects/ProjectCreationService.cs`
- `RoadGuardSystem.Repositories/Interfaces/Projects/IProjectCreationRepository.cs`
- `RoadGuardSystem.Repositories/Implementations/Projects/ProjectCreationPersistenceService.cs`
- `RoadGuardSystem.Repositories/RoadGuardDbContext.cs`
- `tests/RoadGuardSystem.UnitTests/Architecture/{DependencyGraphTests,RepositorySourceBoundaryTests,ServiceSourceBoundaryTests}.cs`

Source has retained namespaces such as `RoadGuardSystem.Services.Projects` inside `Implementations/Projects`. Do not rename namespaces to match paths; entity/context namespace changes can alter EF snapshots.

Current ProjectsController uses MVC/ProblemDetails, PUT updates, request `OperationId` and `ExpectedRowVersion`, with conflict results mapped to 409. V2 draft may use PATCH, headers and 412. These are real contract differences to resolve per task, not stylistic cleanup.

Read `docs/adr/001-backend-boundary.md` and `docs/adr/004-n-layer-backend-structure.md` for accepted architecture; read ADR 002 only for auth and ADR 005 for affected workflow synchronization. Historical ADR inventory counts are not current endpoint counts.

ADR 006 and current AGENTS assign each approved V2 task owner the complete required endpoint slice, including directly required persistence and migration work. Person 2 coordinates migration order, SQL integration, seed, Docker, CI and release evidence without exclusively owning endpoint persistence. Reserve shared files explicitly, serialize migrations, and preserve original completed worklogs.
