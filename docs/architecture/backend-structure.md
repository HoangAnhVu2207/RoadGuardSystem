# RoadGuard Backend Structure

## Accepted Flow

RoadGuard uses the accepted N-layer flow:

`Controller -> IService -> IRepository`

ADR 001 defines the project-reference graph. ADR 004 amends its implementation conventions.

## Boundaries

| Layer | Responsibility |
|---|---|
| API | HTTP binding, authorization composition, stable ProblemDetails mapping, and DTO responses. |
| Services | Use-case policy, cross-aggregate decisions, orchestration, and mapping. |
| Repositories | EF Core, storage, transaction, idempotency, concurrency, outbox, and persistence backstops. |
| BusinessObjects | Entity-local invariants, factory methods, normalization, and pure calculations from local state. |
| DTOs | Transport request/response shapes and validation attributes. |

Repositories never choose authorization, workflow outcomes, business calculations, or HTTP errors. Services never use EF Core, `RoadGuardDbContext`, `HttpContext`, or MVC result types. Controllers never access repositories or `RoadGuardDbContext`.

## Conventions

- A Service injects repository/read-model interfaces, not concrete persistence classes.
- New public DTOs, entities, interfaces, and implementations use one public type per file.
- DTOs are request/response contracts only: data properties/records and validation attributes are allowed; business/computed logic, data access, clocks/random generation, and Repository/Service/EF/HTTP references are forbidden. Services own mapping.
- Repositories own `RoadGuardDbContext`, EF query/write, mappings, transactions, idempotency, rowversion/concurrency, outbox, storage, and persistence backstops. They return facts rather than deciding authorization, scope, workflow, calculations, or HTTP errors. ADR 001 allows the Repositories-to-DTOs project reference, but Repository source must not reference `RoadGuardSystem.DTOs`; `RepositorySourceBoundaryTests` enforces this.
- Services own use-case orchestration, business/state rules, authorization/scope decisions, stable business result codes, and DTO mapping. Controllers use `IService`; Services use repository interfaces. Service source may not reference `HttpContext`, MVC/controller result types, EF Core, or `RoadGuardDbContext`; `ServiceSourceBoundaryTests` enforces this.
- Existing multi-type files are only split when their owning feature is materially changed.
- Entity, enum, and `RoadGuardDbContext` namespaces are frozen for structure-only work.

## Verified Dependency Matrix

This is the verified direct `ProjectReference` matrix, not a claim that every permitted type is directly referenced by source.

| Layer | Direct project references | Source boundary |
|---|---|---|
| BusinessObjects | None. `Microsoft.Extensions.Identity.Stores` is the verified Identity-model package; `NetTopologySuite` provides geometry primitives. | No production-project dependency. |
| DTOs | BusinessObjects | May use entity/enum types only when a transport contract requires them; no Repositories or Services. |
| Repositories | BusinessObjects, DTOs | ADR 001 permits the DTO project reference, but Repository `.cs` source must not reference DTOs; no Services or MVC. |
| Services | Repositories | Uses repository interfaces and may consume BusinessObjects/DTOs transitively; no MVC, `HttpContext`, EF Core, or `RoadGuardDbContext`. |
| API | Services | Uses `IService` and DTOs available transitively; Controllers never use Repositories directly. |

## Directory Layout

### Observed layout

`BusinessObjects` and `DTOs` are organized domain-first today. `BusinessObjects` has entity folders including `Projects`, `Identity`, `Surveys`, `Warranties`, `Auditing`, and `Messaging`, plus `Common/Enums.cs` and `Common/Extensions/` for enum extensions. `DTOs` has `Authentication`, `Identity`, `Projects`, `Warranties`, and `Commons`.

`Repositories` and `Services` are currently mostly flat within domain folders. For example, `RoadGuardSystem.Repositories/Projects/ProjectCreationPersistenceService.cs` and `RoadGuardSystem.Repositories/Projects/ProjectWorkPackageReadModel.cs` live directly in `Projects/`. Both projects contain empty root-level `Interfaces/` and `Implementations/` directories at the time of writing; neither has populated `Interfaces/<Domain>/` or `Implementations/<Domain>/` folders. `Services` has no `Mappers/` directory at the time of writing.

`API` remains organized by host concern: `Authentication`, `Authorization`, `Constants`, `Controllers`, `Extensions`, `Middlewares`, and `Properties`. It does not use `Interfaces/` or `Implementations/` directories.

The repository has one HTTP request file: `RoadGuardSystem.API/RoadGuardSystem.API.http`.

### Functional grouping convention

Domain code remains under `<Domain>/` in every layer. Entity, DTO, controller, feature service, repository/read model, and feature contract files are not moved merely to satisfy a generic folder rule.

`BusinessObjects` is deliberately entity-only: each entity is in one file under its related domain folder. The only non-entity files are `Common/Enums.cs` and enum extensions in `Common/Extensions/`. Entity-local invariants, normalization, factories, and state transitions stay with the entity; options, validators, sanitizers, infrastructure constants/helpers, interfaces, EF/HTTP/configuration, clocks, and random generators do not.

Cross-cutting code is grouped by its function, regardless of the domain that consumes it:

- `Extensions/` for DI and framework extension methods.
- `Options/` for `*Options` and their validators.
- `Factories/` for security or value factories.
- `Generators/` for secure token/password generators.
- `Helpers/`, `Configurations/`, `Concurrency/`, `Transactions/`, `Storage/`, `Seeding/`, and `Migrations/` for their corresponding infrastructure roles.

ADR 004 still requires interfaces at persistence/read-model seams injected by Services. A future approved seam-relocation batch may place those contracts under `Interfaces/<Domain>/` and their implementations under `Implementations/<Domain>/`; that is separate from the functional grouping above and does not require a mechanical move of domain files.

The layouts are intentional: domain folders express business ownership, while functional folders express shared technical responsibility. This is not an inconsistency that requires a future mechanical cleanup.

### File Placement Gate

Before creating source, the task must name the exact path and the Agent must inspect the existing target layer with `rg --files`. New files belong only in an existing domain folder or an established functional folder whose responsibility matches the type. Catch-all/root folders such as `Helpers`, `Utils`, `Common`, `Shared`, `Models`, `Managers`, `Interfaces`, and `Implementations` are not a fallback for uncertain ownership. Root `Interfaces/` and `Implementations/` placeholders change only through an approved seam-relocation batch; do not create speculative, duplicate, empty, or compatibility files.

Current Service examples after P1-72:

```text
RoadGuardSystem.Services/
  Extensions/
    AuthenticationServiceCollectionExtensions.cs
  Options/
    JwtOptions.cs
    PasswordChangeFingerprintOptions.cs
  Factories/
    AccessTokenFactory.cs
    PasswordChangeFingerprintFactory.cs
  Generators/
    RefreshTokenGenerator.cs
    TemporaryPasswordGenerator.cs
  Authentication/
    AuthService.cs
    AuthoritativeSessionValidator.cs
```
