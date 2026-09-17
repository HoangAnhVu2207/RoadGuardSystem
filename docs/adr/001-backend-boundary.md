# ADR 001: Backend Architecture Boundaries, Layer Responsibilities, and Runtime Baseline

## Status

Accepted (2026-09-17)

- **Date:** 2026-09-17
- **Author / Owner:** Person 1 (Antigravity)
- **Reviewer:** Person 2
- **Approved by:** Product Owner (Decision P1-02)
- **Trace:** Architecture / Task P1-02 (Foundation post P1-00 and P1-01)

---

## Context

The RoadGuard System is an automated road infrastructure inspection, defect detection, and warranty tracking platform. The backend is designed as a modular, testable, and maintainable ASP.NET Core solution adhering to Clean Architecture principles.

During Task P1-00 ("Executable Foundation") and its review, architectural boundaries were codified to prevent coupling, leaky abstractions, and circular dependencies across the five solution projects:
1. `RoadGuardSystem.aBusinessObjects` (`BusinessObjects`)
2. `RoadGuardSystem.bDTOs` (`DTOs`)
3. `RoadGuardSystem.cRepositories` (`Repositories`)
4. `RoadGuardSystem.dServices` (`Services`)
5. `RoadGuardSystem.eAPI` (`API`)

Furthermore, clear product and system boundaries must be established between the central backend service and external components, including mobile inspection clients, web dashboards, drone inspection capture workflows, and the artificial intelligence (AI) defect analysis service.

---

## Decision

### 1. System Scope Boundaries

1. **ASP.NET Core Backend is the Sole In-Scope Component:**
   - The current solution repository contains only the ASP.NET Core backend Web API, domain model, application services, and persistence layers.
   - External clients and services—specifically the Android Mobile Application (`RoadGuard Mobile`), Web Management Dashboard (`RoadGuard Dashboard`), and Python AI Defect Analysis Service—are external systems interacting with the backend strictly via well-defined public API contracts or adapter interfaces.

2. **Phase 1 AI Adapter Architecture:**
   - In Phase 1, road inspection defect analysis uses an in-process, deterministic mock adapter contract (to be formally defined in task **P1-31** under US-07 and KS10–KS13) that produces predictable, repeatable detection results for development, testing, and CI validation.
   - Domain logic and application services in `RoadGuardSystem` must depend exclusively on domain abstractions defined in `BusinessObjects` or `Services`, and must **never** depend directly on the mock implementation or on Python-specific transports (such as raw sockets, Python interop, or gRPC endpoints).
   - In subsequent phases, integration with the external Python AI service will be accomplished by introducing an out-of-process adapter implementing the contract defined in P1-31 without altering domain workflows.

3. **Drone Operations Boundary:**
   - The backend **never directly controls, navigates, or monitors drones**.
   - Flight planning, hardware control, and image/sensor capture are handled by external drone hardware and mobile field applications.
   - The backend receives only finalized, geotagged survey captures and associated metadata submitted by authorized field personnel via standard API endpoints.

4. **Warranty Liability Boundary:**
   - The backend **never automatically concludes or infers contractual warranty liability**.
   - The system tracks defect lifecycles, computes measurements, stores maintenance history, and provides decision-support reports to Project Managers and Engineers.
   - Formal determination of contractor warranty liability remains an authoritative human decision (Project Manager review) and is not automated by state transitions.

5. **Research Validation Boundary:**
   - Research validation surveys conducted for academic or model calibration purposes are logically isolated from operational defect management.
   - Research surveys must never automatically create or transition operational `Defect` or `Warranty` records.

---

### 2. Layer Ownership and Clean Architecture Invariants

The solution enforces project references and dependency directions as implemented in the project files:

```
[ API (eAPI) ]
      │
      ▼
[ Services (dServices) ]
      │
      ▼
[ Repositories (cRepositories) ]
     ╱                   ╲
    ▼                     ▼
[ DTOs (bDTOs) ] ───> [ BusinessObjects (aBusinessObjects) ]
```

#### Actual Project Reference Graph
- Full project reference chain: `API -> Services -> Repositories; Repositories -> BusinessObjects và DTOs; DTOs -> BusinessObjects`.
- `API` references `Services` (`API -> Services`).
- `Services` references `Repositories` (`Services -> Repositories`).
- `Repositories` references `BusinessObjects` and `DTOs` (`Repositories -> BusinessObjects và DTOs`).
- `DTOs` references `BusinessObjects` (`DTOs -> BusinessObjects`).
- `BusinessObjects` has zero project references (pure domain core).

#### Layer Responsibilities and Policy Rules (AGENTS.md)

1. **`BusinessObjects` (`RoadGuardSystem.aBusinessObjects`):**
   - **Owns:** Domain entities (e.g., `Survey`, `Defect`, `RoadSection`, `RoadSectionVersion`), aggregate roots, value objects, domain invariants, and fixed numeric enums (all enums must define `Unknown = 0` and maintain immutable published numeric values).
   - **Identity Model:** Defines domain user and role types (`ApplicationUser`, `ApplicationRole`).
   - **Allowed Dependencies:** Foundational libraries without persistence runtime:
     - `Microsoft.Extensions.Identity.Stores` (v8.0.17) for `IdentityUser<Guid>` and `IdentityRole<Guid>` abstractions.
     - `NetTopologySuite` (v2.5.0) for standard GIS spatial types (`Point`, `LineString`, `Polygon`).
   - **Strict Prohibitions:** Must never reference `DTOs`, `Repositories`, `Services`, `API`, or any Entity Framework Core runtime packages (`Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`).

2. **`DTOs` (`RoadGuardSystem.bDTOs`):**
   - **Owns:** Public API request and response data transfer objects, query filter parameters, pagination metadata models, and ProblemDetails error contracts.
   - **Allowed Dependencies:** References only `BusinessObjects` (for shared enums/value types where appropriate).
   - **Strict Prohibitions:** Must never reference Entity Framework Core, persistence libraries, ASP.NET Core HTTP context packages, or expose domain entities directly to API consumers.

3. **`Repositories` (`RoadGuardSystem.cRepositories`):**
   - **Owns:** EF Core, `DbContext` (`RoadGuardDbContext`), entity type configurations (`IEntityTypeConfiguration<T>`), database migrations, SQL Server provider configurations with NetTopologySuite spatial extensions, storage implementations, and **repository interfaces used by Services**.
   - **Identity Persistence:** In accordance with P1-00 review finding F1 and the repository division of responsibility, EF Core Identity persistence and IdentityDbContext configuration are owned exclusively by `Repositories` and assigned to Person 2 under task **P2-10** (without asserting an unconfirmed class name in advance).
   - **Strict Prohibitions:** Must never reference `API` or external transport layers.

4. **`Services` (`RoadGuardSystem.dServices`):**
   - **Owns:** Use-case orchestration, business workflow policies, cross-aggregate domain invariants, state machine transitions, server-side project membership authorization checks, and transaction boundaries.
   - **External Boundaries:** Per `AGENTS.md`, abstractions are introduced only at real external boundaries such as file storage, time/clock, notifications, processing queue, and AI service.
   - **Strict Prohibitions:** Must never depend on `API`, HTTP transport primitives, or controller contexts.

5. **`API` (`RoadGuardSystem.eAPI`):**
   - **Owns:** ASP.NET Core Web API host, thin controllers, URL route templates, API versioning (`/api/v{version:apiVersion}`), correlation ID middleware (`X-Correlation-ID`), standard ProblemDetails error serialization (`application/problem+json`), authentication/authorization middleware composition, OpenAPI/Swagger specifications, and dependency injection composition roots.
   - **Invariants:** Controllers remain thin orchestrators delegating immediately to application services. Direct references to `Repositories` or `DbContext` are forbidden; data access must be mediated through `Services`.

---

### 3. Runtime and Toolchain Baseline

1. **Target Framework:**
   - All solution projects target **`net8.0`** (Long Term Support).

2. **Pinned SDK Toolchain:**
   - The repository pins the .NET SDK via `global.json` to feature band **`10.0.401`** with `rollForward: "latestPatch"`.
   - **Rationale and Status:** As reviewed in P1-00 (finding F5 / R2-02) and formally approved by the repository owner and Product Owner, this configuration reflects the accepted workstation environment across the development team. The .NET SDK supports building target frameworks earlier than the SDK version (e.g., SDK 10 building `net8.0`).

3. **CI Replication Requirement:**
   - Task **P2-01** is explicitly mandated to reproduce this exact SDK toolchain (`10.0.401`) on CI pipelines (GitHub Actions / containerized runner) and provide clean restore, build, and test verification proof.
   - The SDK version will **not** be downgraded by default to `8.0.xxx`.

4. **Time-Bound Follow-Up Policy:**
   - Prior to .NET 8 reaching End of Support (November 2026) or prior to commercial release (whichever occurs first), the engineering team must conduct a dedicated architectural review to:
     - Formally evaluate upgrading the target framework to the active LTS release (e.g., .NET 10 LTS), or
     - Standardize on a dedicated .NET 8 LTS SDK container once CI pipelines are fully established.
   - Framework retargeting is strictly prohibited within foundational tasks P1-00, P1-01, and P1-02.

---

## External Boundaries and Integration Contracts

1. **Spatial Geometry Conventions:**
   - In accordance with the Data Dictionary, raw GPS survey locations use the WGS 84 geographic coordinate reference system (`geography(4326)`).
   - Project engineering geometry and spatial calculations use the project-configured UTM projected coordinate system (SRID `32648` or `32649`).

2. **File and Media Storage:**
   - Original survey files, inspection imagery, and repair evidence are stored in an external immutable object store.
   - The backend computes and verifies server-side cryptographic checksums (SHA-256) prior to confirming uploads or transitioning survey records to `SERVER_CONFIRMED`.

3. **Append-Only Auditing and Immutability:**
   - Domain audit records (`AuditLog`), submitted inspection measurements, original evidence files, and repair verification evidence are strictly append-only. Updates or deletions in place are architecturally forbidden.

---

## Rejected Alternatives

1. **Monolithic Single-Project Architecture:**
   - *Rejected:* A single-project structure eliminates compile-time dependency enforcement, allowing domain logic to directly reference persistence and HTTP concerns, leading to tight coupling and unmaintainable code.

2. **Direct EF Core Entity Exposure in API Responses:**
   - *Rejected:* Exposing database entities directly causes severe security vulnerabilities (mass assignment / over-posting), serialization circular references, and breaks API contract stability when database schemas evolve.

3. **Embedding EF Core Identity into `BusinessObjects`:**
   - *Rejected during P1-00 (Finding F1):* Referencing `Microsoft.AspNetCore.Identity.EntityFrameworkCore` inside `BusinessObjects` forces the domain model to depend on Entity Framework Core runtime libraries, violating Clean Architecture. `BusinessObjects` references only the lightweight `Microsoft.Extensions.Identity.Stores`.

4. **Direct Python AI Transport Coupling:**
   - *Rejected:* Coupling backend services directly to Python-specific communication protocols or libraries impedes Phase 1 development, prevents rapid local testing, and complicates CI/CD workflows. The adapter pattern isolates this dependency.

5. **Direct Drone Telemetry and Control in Backend:**
   - *Rejected:* Managing real-time drone telemetry, connection drops, and hardware safety mechanisms in the backend introduces unnecessary complexity and safety hazards. Telemetry and control are client/edge responsibilities.

6. **Default Downgrade of SDK to 8.0.xxx:**
   - *Rejected:* Forcing an immediate SDK downgrade would disrupt developer workstations where SDK 10 is established, contrary to the repository owner's baseline acceptance during P1-00.

---

## Consequences

### Positive
- **Strict Boundary Enforcement:** Architectural tests (`DependencyGraphChecker`) automatically verify that no forbidden references or packages enter the codebase.
- **Testability & Determinism:** The Phase 1 deterministic AI adapter contract enables comprehensive unit, integration, and API testing without relying on external Python services or GPU hardware.
- **Clear Team Division:** Distinct ownership boundaries between Person 1 (Domain, Services, API) and Person 2 (Repositories, SQL Server, EF Core Migrations, Identity persistence) prevent merge conflicts and coordination overhead.

### Trade-offs & Operational Costs
- **Mapping Overhead:** Data must be explicitly mapped between domain entities and DTOs using strongly typed mappers.
- **Dual Identity Separation:** Maintaining domain `ApplicationUser` in `BusinessObjects` while implementing EF Core `UserStore` in `Repositories` (P2-10) requires disciplined coordination between Person 1 and Person 2.

---

## Unresolved Decisions

1. **Production AI Inter-Service Protocol:** Selection of the production communication protocol between the ASP.NET Core backend and Python AI service (e.g., gRPC streaming vs. asynchronous message broker vs. REST) is deferred to Phase 2.
2. **Object Storage Provider Selection:** The production cloud object storage provider (e.g., Azure Blob Storage, AWS S3, or MinIO) will be finalized in the media storage task; local file system storage abstraction is used initially.
3. **Formal Target Retargeting Date:** The exact calendar milestone for upgrading target framework from `net8.0` to the next LTS will be reviewed in Q3 2026 prior to release.

---

## Compliance and Verification

- **Automated Dependency Checks:** Validated continuously via `tests/RoadGuardSystem.UnitTests/Architecture/DependencyGraphTests.cs` using `DependencyGraphChecker`.
- **Build Cleanliness:** Verified via `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` yielding 0 warnings and 0 errors across all projects.
- **Formatting Verification:** Verified via `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore`.
