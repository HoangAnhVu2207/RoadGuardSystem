# Antigravity completion log — P2-00

## Identity and scope

- **Task ID/title:** P2-00 / SQL Server + NetTopologySuite spatial persistence foundation
- **Owner / reviewer:** Person 2 (Huy) / Person 1 (Anh)
- **Date / branch or commit:** 2026-09-16 / `huy` / baseline commit `c48be5d`
- **Trace (`US-*`, use case, acceptance criteria):** TE-01 / TE-09 (Technical Enabler — persistence and spatial infrastructure; no business use case)
- **Status:** Ready for review

### In-scope behavior
- Added EF Core SQL Server & NetTopologySuite packages compatible with EF Core 8.0.17 to `RoadGuardSystem.Repositories`:
  - `Microsoft.EntityFrameworkCore.SqlServer` (8.0.17)
  - `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite` (8.0.17)
  - `Microsoft.EntityFrameworkCore.Design` (8.0.17, PrivateAssets=all)
  - `Microsoft.Extensions.Options` (8.0.2)
  - `Microsoft.Extensions.Configuration.Abstractions` (8.0.0)
  - `Microsoft.Extensions.Configuration.Binder` (8.0.2)
  - `Microsoft.Extensions.DependencyInjection.Abstractions` (8.0.2)
- Created `RoadGuardDbContext` with `ApplyConfigurationsFromAssembly(typeof(RoadGuardDbContext).Assembly)` mechanism.
- Created `RoadGuardDatabaseOptions` and `RoadGuardDatabaseOptionsValidator` with fail-fast validation:
  - Missing, empty, or whitespace connection string fails immediately.
  - Malformed connection string syntax fails immediately.
  - Connection string missing Data Source or Initial Catalog fails immediately.
  - `TrustServerCertificate=true` is strictly forbidden and fails fast in production options (`IsProduction = true`).
  - Command timeout and retry count bounds validation.
- Enforced spatial domain invariants per Data Dictionary sections 2.4 & 2.5:
  - Canonical constants in `SpatialConstants`: `GpsGeographySrid = 4326`, `UtmZone48NSrid = 32648`, `UtmZone49NSrid = 32649`, and `AllowedProjectUtmSrids = [32648, 32649]`.
  - Invariant validator in `SpatialValidation`:
    - `EnsureGpsGeography(geometry)`: rejects null, SRID `0`, and non-4326 geometries.
    - `EnsureProjectEngineeringGeometry(geometry, projectUtmSrid)`: rejects null, SRID `0`, unconfigured UTM SRIDs, and geometries whose SRID does not match the project's configured UTM SRID.
- Implemented `AddRoadGuardPersistence` extension method on `IServiceCollection` with fail-fast configuration validation and SQL Server + NetTopologySuite configuration.
- Created `tests/RoadGuardSystem.IntegrationTests` project and registered in `RoadGuardSystem.slnx`.
- Built collision-safe SQL Server test fixture (`SqlServerTestFixture`) supporting:
  - `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` environment variable
  - Detected local running SQL Server instances (`MSSQL$HANHNAV`, `localhost`, `(localdb)\mssqllocaldb`)
  - Testcontainers MsSql container (when Docker daemon is active)
  - Throws clear diagnostic `SqlTestEnvironmentUnavailableException` if no database environment is reachable (prevents silent false-greens).
  - Dynamic isolated test database creation (`RoadGuard_Test_{Guid:N}`) with automatic drop/cleanup in `DisposeAsync`.
- Kept testing probe entity (`SpatialProbeRecord`) and probe context (`SpatialProbeDbContext`) strictly inside integration test infrastructure; zero fake business entities in production.
- Executed Negative-First workflow: verified RED failure across all 17 options and spatial validation tests before implementing production logic, followed by 100% GREEN verification across 26 integration tests and all 54 solution-wide tests.

### Explicitly out of scope
- Business entities, tables, and migrations from `P2-10` onwards (owned by P2-10, P2-20, etc.).
- Docker Compose orchestration, database seeding framework, and CI pipeline (owned by P2-01).
- Audit, outbox, and concurrency interceptors (owned by P2-02).
- Business API endpoints, controllers, and authorization filters (owned by P1-01 / Wave 1).

---

## Preconditions and decisions

- **Actor and project-scope rule:** N/A — Technical enabler establishing persistence infrastructure. No business actor or project-scope authorization applies at this layer.
- **State before / allowed state after:** N/A — Technical enabler; no business state machine or domain state transition.
- **Data/version/immutability rules:** Database column mappings strictly conform to Data Dictionary sections 2.4 & 2.5: GPS coordinates map to SQL Server `geography` with SRID 4326; engineering geometries map to SQL Server `geometry` with project UTM SRID 32648 or 32649. SRID 0 is strictly forbidden.
- **Audit event and stable error codes:** N/A — Business audit logs are not emitted for DB infrastructure initialization. Configuration and spatial validation fail fast with descriptive standard exception messages (`ArgumentException`, `ArgumentNullException`, `ArgumentOutOfRangeException`).
- **Idempotency/concurrency behavior:** N/A for business idempotency. Test fixture guarantees collision-safe execution across concurrent test runs by creating uniquely named isolated databases (`RoadGuard_Test_{Guid:N}`) and cleaning them up on disposal.
- **Assumptions, ADRs, or specification conflicts:**
  - Specifications: Data Dictionary v1 (sections 2.4, 2.5, 6.2, 6.7), Dac_ta_UseCase_v2 (rule 19).
  - Production database configuration strictly rejects `TrustServerCertificate=true` to comply with security requirements.
  - Test environment supports runtime configuration via `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING`, or detected local running SQL Server instance (`MSSQL$HANHNAV`), or Testcontainers when Docker is active.
  - If no SQL Server instance is reachable, integration tests fail with an explicit, actionable diagnostic exception rather than false-greening.
  - No specification conflicts encountered.

---

## Files changed

| Change | File | Purpose |
|---|---|---|
| Modified | `RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj` | Added EF Core SQL Server, NetTopologySuite, Design, Options, Configuration.Abstractions, and Configuration.Binder packages |
| Added | `RoadGuardSystem.Repositories/RoadGuardDbContext.cs` | Root DbContext with ApplyConfigurationsFromAssembly mechanism |
| Added | `RoadGuardSystem.Repositories/Options/RoadGuardDatabaseOptions.cs` | Database connection and execution options POCO |
| Added | `RoadGuardSystem.Repositories/Options/RoadGuardDatabaseOptionsValidator.cs` | Fail-fast validator for database options (prohibits TrustServerCertificate in production) |
| Added | `RoadGuardSystem.Repositories/Spatial/SpatialConstants.cs` | Canonical SRID definitions (4326, 32648, 32649) and allowed project UTM set |
| Added | `RoadGuardSystem.Repositories/Spatial/SpatialValidation.cs` | Invariant validation for GPS geography and engineering geometry |
| Added | `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs` | Service collection registration with immediate fail-fast options validation |
| Modified | `RoadGuardSystem.slnx` | Registered IntegrationTests project in solution |
| Added | `tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj` | Integration test project with xUnit, FluentAssertions, EF Core SQL Server + NetTopologySuite, and Testcontainers |
| Added | `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlTestEnvironmentUnavailableException.cs` | Diagnostic exception preventing false-greens when SQL Server is unreachable |
| Added | `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SpatialProbeRecord.cs` | Test probe entity mapping geography and geometry columns |
| Added | `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SpatialProbeDbContext.cs` | Test DbContext inheriting from RoadGuardDbContext configuring spatial probe mappings |
| Added | `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs` | Isolated, collision-safe SQL Server test database lifecycle fixture |
| Added | `tests/RoadGuardSystem.IntegrationTests/Configuration/DatabaseOptionsValidationTests.cs` | Negative-first and positive tests for options and DI configuration |
| Added | `tests/RoadGuardSystem.IntegrationTests/Spatial/SpatialInvariantTests.cs` | Negative-first and positive tests for spatial SRID invariants |
| Added | `tests/RoadGuardSystem.IntegrationTests/Persistence/SqlServerDiagnosticTests.cs` | Negative diagnostic tests for unreachable SQL Server instances |
| Added | `tests/RoadGuardSystem.IntegrationTests/Persistence/SqlServerSpatialRoundTripTests.cs` | Positive spatial round-trip tests and catalog metadata verification on real SQL Server |
| Added | `docs/worklogs/P2-00-completion.md` | Completion log for task P2-00 |

---

## Package/Version Selections and Rationale

| Package | Version | Rationale |
|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.17 | Aligned with solution EF Core baseline 8.0.17 on `net8.0` |
| `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite` | 8.0.17 | Provides spatial SQL Server mappings for NetTopologySuite `Point`, `LineString`, `Geometry` |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.17 | Tooling support for future migrations; PrivateAssets=all |
| `Microsoft.Extensions.Options` | 8.0.2 | Options pattern and validation abstractions |
| `Microsoft.Extensions.Configuration.Abstractions` | 8.0.0 | Configuration abstraction for DI setup |
| `Microsoft.Extensions.Configuration.Binder` | 8.0.2 | Binding `IConfigurationSection` to `RoadGuardDatabaseOptions` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0.2 | `IServiceCollection` extension methods |
| `NetTopologySuite` | 2.5.0 | Standard .NET spatial geometry library |
| `Testcontainers.MsSql` | 3.10.0 | Testcontainers SQL Server provider for containerized integration testing |

---

## SQL Server / Docker Prerequisites

- **SQL Server Requirements:**
  - SQL Server 2019+ or Azure SQL Database supporting SQL Server Spatial (`geography` and `geometry` types).
  - TCP/IP enabled or local instance accessible via Windows Integrated Security.
- **Runtime Environment Options (Supported by `SqlServerTestFixture`):**
  1. `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING`: Custom connection string to an administrative SQL Server instance with rights to create/drop databases.
  2. Local running SQL Server instance: Auto-detected (e.g. `.\HANHNAV` on developer machine or `localhost`).
  3. Docker: Docker Desktop running with Linux containers (Testcontainers spins up `mcr.microsoft.com/mssql/server:2022-latest` automatically).
- If none of these environments are reachable, tests fail fast with `SqlTestEnvironmentUnavailableException` (zero false-green).

---

## Database, API, config, and operations impact

- **Migration added and recovery/downgrade note:** No business migration added in P2-00 per specification ("Không tạo entity hoặc migration nghiệp vụ thuộc P2-10 trở đi"). Test schema is generated dynamically in isolated test databases using `EnsureCreatedAsync()`.
- **API/OpenAPI compatibility impact:** None (persistence infrastructure and test layer only).
- **Configuration/secret/environment impact:** `RoadGuardDatabaseOptions` introduced under configuration section `RoadGuardDatabase`. No secrets, passwords, or credentials committed.
- **Seed/data migration impact:** None (seed framework is owned by P2-01).
- **Worker/storage/queue impact:** None.

---

## Negative-first evidence

List each negative/edge case before positive cases. If a standard case is irrelevant, state why.

| Test | Layer | Expected failure/code | Result |
|---|---|---|---|
| Missing connection string | Repositories/Options | `ValidateOptionsResult.Failed` ("ConnectionString is required") | PASS (RED observed initially) |
| Whitespace connection string | Repositories/Options | `ValidateOptionsResult.Failed` | PASS (RED observed initially) |
| Malformed connection string | Repositories/Options | `ValidateOptionsResult.Failed` ("ConnectionString is malformed") | PASS (RED observed initially) |
| `TrustServerCertificate=true` in production | Repositories/Options | `ValidateOptionsResult.Failed` ("strictly forbidden in production") | PASS (RED observed initially) |
| ValidateOrThrow with empty options | Repositories/Options | `ArgumentException` | PASS (RED observed initially) |
| AddRoadGuardPersistence with missing configuration | Repositories/Extensions | `ArgumentException` fail-fast on DI registration | PASS (RED observed initially) |
| Spatial geography with null | Repositories/Spatial | `ArgumentNullException` | PASS (RED observed initially) |
| Spatial geography with SRID 0 | Repositories/Spatial | `ArgumentException` ("Spatial SRID 0 is forbidden") | PASS (RED observed initially) |
| Spatial geography with non-4326 SRID | Repositories/Spatial | `ArgumentException` ("GPS geography requires SRID 4326") | PASS (RED observed initially) |
| Spatial geometry with null | Repositories/Spatial | `ArgumentNullException` | PASS (RED observed initially) |
| Spatial geometry with SRID 0 | Repositories/Spatial | `ArgumentException` ("Spatial SRID 0 is forbidden") | PASS (RED observed initially) |
| Spatial geometry with unconfigured UTM SRID (3857) | Repositories/Spatial | `ArgumentOutOfRangeException` ("Project UTM SRID not supported") | PASS (RED observed initially) |
| Spatial geometry with mismatched project SRID | Repositories/Spatial | `ArgumentException` ("Geometry SRID does not match configured project UTM SRID") | PASS (RED observed initially) |
| Unreachable SQL Server instance probe | IntegrationTests/Infrastructure | `CanConnectAsync` returns `false` (no false-green) | PASS |
| SQL Server unavailable diagnostic message | IntegrationTests/Infrastructure | `SqlTestEnvironmentUnavailableException` contains actionable guidance | PASS |

### Irrelevant negative cases for P2-00
- **Unauthorized / wrong project:** N/A — Technical persistence enabler; no user security context or claims evaluation at this level.
- **Invalid transition / prerequisite:** N/A — No domain entity state machine exists in P2-00.
- **Duplicate retry / idempotency:** N/A — No business commands, domain events, or outbox workers are processed in P2-00.
- **Stale concurrency:** N/A — Concurrency tokens and row versions belong to P2-02.
- **Boundary / oversize streaming:** N/A — File storage streaming belongs to P2-30.
- **Integrity / checksum history:** N/A — Audit log and evidence immutability belongs to P2-02 / P2-30.

---

## Positive evidence

| Test | Layer | Expected state/output | Result |
|---|---|---|---|
| Valid connection string passes options validation | Repositories/Options | `ValidateOptionsResult.Succeeded` is true | PASS |
| AddRoadGuardPersistence succeeds with valid configuration | Repositories/Extensions | ServiceCollection contains registered DbContext and Options | PASS |
| Geography Point with SRID 4326 succeeds | Repositories/Spatial | No exception thrown | PASS |
| Engineering geometry with UTM SRID 32648 succeeds | Repositories/Spatial | No exception thrown | PASS |
| Engineering geometry with UTM SRID 32649 succeeds | Repositories/Spatial | No exception thrown | PASS |
| Fixture creates isolated, collision-safe database | IntegrationTests | Database named `RoadGuard_Test_{Guid:N}` | PASS |
| Round-trip Point as geography(4326) on real SQL Server | IntegrationTests | Persisted and queried back with exact coords (105.854444, 21.028511) and SRID 4326 | PASS |
| Round-trip LineString with UTM SRID 32648 on real SQL Server | IntegrationTests | Persisted and queried back with exact coords and SRID 32648 | PASS |
| Round-trip LineString with UTM SRID 32649 on real SQL Server | IntegrationTests | Persisted and queried back with exact coords and SRID 32649 | PASS |
| SQL Server catalog column types confirmed via `sys.columns` | IntegrationTests | `GpsLocation` is `geography`, `EngineeringGeometry` is `geometry` | PASS |
| Database is reliably dropped on fixture dispose | IntegrationTests | Confirmed database count in `sys.databases` is 0 after `DisposeAsync` | PASS |

---

## Commands run

| Command | Exit code | Result/coverage | Timestamp |
|---|---:|---|---|
| `git status --short --branch` | 0 | Branch `anh`, clean worktree | 2026-09-16T21:19:54 |
| `git switch huy` | 0 | Switched to working branch `huy` | 2026-09-16T21:21:40 |
| `git status --short --branch` | 0 | Branch `huy`, clean worktree | 2026-09-16T21:21:42 |
| `dotnet restore RoadGuardSystem.slnx` | 0 | Restored all solution packages including IntegrationTests | 2026-09-16T21:22:48 |
| `dotnet test tests/RoadGuardSystem.IntegrationTests --filter "TaskId=P2-00"` | 1 | **RED phase observed:** 17 failed, 7 passed (stubs threw `NotImplementedException`) | 2026-09-16T21:25:16 |
| `dotnet test tests/RoadGuardSystem.IntegrationTests --filter "TaskId=P2-00"` | 0 | **GREEN phase:** 26 passed, 0 failed, 0 skipped in 23s | 2026-09-16T21:28:15 |
| `dotnet test tests/RoadGuardSystem.UnitTests` | 0 | 26 passed, 0 failed (architecture & smoke tests green) | 2026-09-16T21:28:56 |
| `dotnet test tests/RoadGuardSystem.ApiTests` | 0 | 2 passed, 0 failed (API startup tests green) | 2026-09-16T21:29:24 |
| `dotnet restore RoadGuardSystem.slnx` | 0 | Gate 1: Clean restore across all 8 projects | 2026-09-16T21:29:48 |
| `dotnet test tests/RoadGuardSystem.IntegrationTests --filter "TaskId=P2-00"` | 0 | Gate 2: 26 passed, 0 failed | 2026-09-16T21:30:39 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | Gate 3: Clean build (0 warnings, 0 errors) | 2026-09-16T21:31:18 |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Gate 4: Clean formatting (0 changes) | 2026-09-16T21:31:38 |
| `dotnet test RoadGuardSystem.slnx --no-build` | 0 | Gate 5: All 54 tests passed solution-wide | 2026-09-16T21:32:14 |
| `git diff --check` | 0 | Clean diff check (no whitespace errors or conflict markers) | 2026-09-16T21:32:36 |

---

## Review handoff

- **Observable demo/output:** 26 integration tests executing against real SQL Server demonstrating:
  1. Options fail-fast validation and rejection of `TrustServerCertificate=true` in production.
  2. Spatial SRID invariants (strict requirement of 4326 for geography; 32648 or 32649 for engineering geometry; rejection of SRID 0).
  3. Isolated database creation and teardown.
  4. Real SQL Server round-trip for geography and geometry columns.
  5. Catalog verification confirming column types are `sys.geography` and `sys.geometry`.
- **Known gaps, skipped tests, and reason:** None. Zero skipped tests.
- **Residual risks:** An accessible SQL Server instance (`ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING`, local SQL Server service, or Docker Desktop) is required to run `RoadGuardSystem.IntegrationTests`.
- **Reviewer findings and resolution:** Pending review by Person 1.
- **Exact next task/action:** Hand off task `P2-00` diff to Person 1 for review. Person 2 will proceed to `P2-01` (Docker Compose, seed framework, and CI pipeline) after `P2-00` review sign-off.
- **Final status:** `Ready for review`
