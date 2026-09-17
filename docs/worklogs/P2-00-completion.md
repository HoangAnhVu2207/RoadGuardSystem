# Antigravity completion log — P2-00

## Identity and scope

- **Task ID/title:** P2-00 / SQL Server + NetTopologySuite spatial persistence foundation & dependency security remediation
- **Owner / reviewer:** Person 2 (Huy) / Person 1 (Anh)
- **Date / branch or commit:** 2026-09-17 / `huy` / baseline commit `9451632`
- **Trace (`US-*`, use case, acceptance criteria):** TE-01 / TE-09 (Technical Enabler — persistence and spatial infrastructure; no business use case)
- **Status:** Ready for cross-review

### In-scope behavior

- **Testcontainers & Database Lifecycle Hardening (Re-review Finding 1):**
  - Decoupled server/container ownership from isolated test database lifecycle.
  - Test database drop and verification operations (`DropDatabaseAsync`, `DropDatabaseByNameAsync`, `DatabaseExistsAsync`) are performed while the server/container remains alive.
  - Sub-fixtures can attach to a parent fixture's `MasterConnectionString` without owning or terminating the underlying container/server.
  - Eliminated post-dispose reconnection attempts to stopped containers.
  - Container disposal is guaranteed in a `finally` block in `SqlServerTestFixture.DisposeAsync`.
- **Zero-Leak Cleanup Failure Testing (Re-review Finding 2):**
  - Tested teardown failure observation without leaving orphaned databases.
  - Teardown failure is simulated via `SimulateDropFailure = true` on a sub-fixture, asserted, and then cleanly dropped in a `finally` block using the still-live server connection.
  - Verified 0 new database leaks across two consecutive test runs.
  - Existing pre-run orphaned databases (6 databases) are identified and reported to the repository owner with an explicit manual cleanup script.
- **Untrusted Configuration Decoupling (Re-review Finding 3):**
  - Removed `IsProduction` property from `RoadGuardDatabaseOptions` so production mode cannot be bound or toggled via configuration JSON.
  - Production mode is supplied exclusively from the trusted host environment / composition root via `AddRoadGuardPersistence(configuration, isProduction: true)`.
  - Added negative tests proving that configuration JSON setting `IsProduction=false` cannot bypass production security rules (`Encrypt=false`, `TrustServerCertificate=true`, `EnableSensitiveDataLogging=true`).
- **Deterministic Fixture Environment Injection (Re-review Finding 4):**
  - Injected `EnvironmentVariableAccessor` delegate into `SqlServerTestFixture` to enable deterministic tests without mutating process-wide environment variables during parallel runs.
  - Removed all hardcoded fallback connection strings (`.\HANHNAV`, `localhost`, `(localdb)\mssqllocaldb`).
  - Implemented strict fail-fast policy for `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING`: fails immediately without fallback if empty, whitespace, malformed, or unreachable.
  - Uses Testcontainers directly when the environment variable is unset.
- **Truly Immutable Spatial Reference Identifiers (Re-review Finding 5):**
  - Realized `SpatialConstants.AllowedProjectUtmSrids` using `System.Collections.Frozen.FrozenSet<int>` exposed as `IReadOnlySet<int>`.
  - Added domain helper `SpatialConstants.IsAllowedProjectUtmSrid(int srid)`.
  - Added negative test verifying that attempting to mutate `AllowedProjectUtmSrids` via mutable collection interfaces (`ICollection<int>.Add`, `Clear`, `Remove`) throws `NotSupportedException`.
- **Tightened Test Assertions & Check Constraints (Re-review Finding 6):**
  - Replaced generic `ThrowAsync<Exception>` assertions with exact types (`ArgumentException`, `ArgumentOutOfRangeException`, `SqlException`) and specific message patterns.
  - Validated exact database CHECK constraint names on violation:
    - `CK_SpatialProbeRecords_ProjectUtmSrid`: `[ProjectUtmSrid] IN (32648, 32649)`
    - `CK_SpatialProbeRecords_GpsLocation_Srid`: `[GpsLocation].[STSrid] = 4326`
    - `CK_SpatialProbeRecords_EngineeringGeometry_Srid`: `[EngineeringGeometry].[STSrid] = [ProjectUtmSrid]`
  - Added direct raw SQL test verifying `CK_SpatialProbeRecords_EngineeringGeometry_Srid` violation.

### Explicitly out of scope
- Business entities, tables, and migrations from `P2-10` onwards (owned by P2-10, P2-20, etc.).
- Docker Compose orchestration, database seeding framework, and CI pipeline (owned by P2-01).
- Audit, outbox, and concurrency interceptors (owned by P2-02).
- Business API endpoints, controllers, and authorization filters (owned by P1-01 / Wave 1).

---

## Preconditions and decisions

- **Actor and project-scope rule:** N/A — Technical enabler establishing persistence infrastructure. No business actor or project-scope authorization applies at this layer.
- **State before / allowed state after:** N/A — Technical enabler; no business state machine or domain state transition.
- **Data/version/immutability rules:** Database column mappings strictly conform to Data Dictionary sections 2.4 & 2.5: GPS coordinates map to SQL Server `geography` with SRID 4326; engineering geometries map to SQL Server `geometry` with project UTM SRID 32648 or 32649. SRID 0 is strictly forbidden. Check constraints enforce this at the database level. `AllowedProjectUtmSrids` is backed by `FrozenSet<int>`.
- **Audit event and stable error codes:** N/A — Business audit logs are not emitted for DB infrastructure initialization. Configuration and spatial validation fail fast with descriptive standard exception messages (`ArgumentException`, `ArgumentNullException`, `ArgumentOutOfRangeException`, `SqlTestEnvironmentUnavailableException`).
- **Idempotency/concurrency behavior:** N/A for business idempotency. Test fixture guarantees collision-safe execution across concurrent test runs by creating uniquely named isolated databases (`RoadGuard_Test_{Guid:N}`) and cleaning them up on disposal.
- **Assumptions, ADRs, or specification conflicts:**
  - Specifications: Data Dictionary v1 (sections 2.4, 2.5, 6.2, 6.7), Dac_ta_UseCase_v2 (rule 19).
  - Production database configuration strictly rejects `TrustServerCertificate=true`, `Encrypt=false`, and `EnableSensitiveDataLogging=true`.
  - In non-production/test, when `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` is unset, `SqlServerTestFixture` launches a Testcontainers MsSql container. When set, it connects directly without fallback.
  - No specification conflicts encountered.

---

## Re-Review Findings (Commit 07ca766) and Resolutions

| Finding | Severity | Description | Resolution |
|---|---|---|---|
| **RRF-1** | High | Testcontainers lifecycle issue: `Database_Is_Dropped_On_Fixture_Dispose` reconnected after container was disposed, failing when run purely on Testcontainers | Decoupled server/container ownership via `masterConnectionString` sub-fixture. Database drop is tested and verified while server is alive. Container disposal is guaranteed in `finally`. |
| **RRF-2** | High | Cleanup failure test leaked databases on real SQL Server because failure stopped teardown before drop occurred | Updated cleanup failure test to catch simulated failure and cleanly drop the database in a `finally` block on the active server. Verified 0 leaked databases across two runs. |
| **RRF-3** | Medium | `IsProduction` was bound from `RoadGuardDatabaseOptions` configuration, allowing callers to bypass security checks | Removed `IsProduction` property from `RoadGuardDatabaseOptions`. Enforced that production mode is passed explicitly from trusted composition root. Added negative bypass test. |
| **RRF-4** | Medium | Tests mutated `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` process-wide, risking race conditions in parallel test runs, and used hardcoded fallbacks | Injected `EnvironmentVariableAccessor` into `SqlServerTestFixture`. Added deterministic tests for unset, empty, whitespace, malformed, and unreachable. Removed all hardcoded fallbacks. |
| **RRF-5** | Medium | `AllowedProjectUtmSrids` could be modified if exposed as mutable collection | Realized backing store as `FrozenSet<int>`. Exposed as `IReadOnlySet<int>`. Added test asserting mutation attempts throw `NotSupportedException`. |
| **RRF-6** | Medium | Negative persistence tests used loose `ThrowAsync<Exception>` assertions and lacked direct SQL check constraint tests | Tightened all `SaveChangesAsync` assertions to exact exception types and messages. Added direct raw SQL test for `CK_SpatialProbeRecords_EngineeringGeometry_Srid`. |

---

## Dependency Security Remediation (GHSA-q939-rpr3-3284 / CVE-2026-48798)

- **Vulnerability Details:**
  - Advisory: [GHSA-q939-rpr3-3284](https://github.com/advisories/GHSA-q939-rpr3-3284)
  - CVE: CVE-2026-48798
  - Severity: High (CVSS 7.1)
  - Description: Path traversal vulnerability in `SSH.NET` `ScpClient.Download` recursive download method allowing malicious/compromised servers to overwrite arbitrary files outside intended destination.
  - Affected versions: `SSH.NET < 2026.0.0`
  - Fixed version: `SSH.NET 2026.0.0`
- **Dependency Paths (Before vs After):**
  - **Before:**
    `tests/RoadGuardSystem.IntegrationTests.csproj`
    └── `Testcontainers.MsSql 3.10.0`
        └── `Testcontainers 3.10.0`
            └── `SSH.NET 2023.0.0` (**Vulnerable**, Severity: High)
  - **After:**
    `tests/RoadGuardSystem.IntegrationTests.csproj`
    └── `Testcontainers.MsSql 4.15.0`
        └── `Testcontainers 4.15.0`
            └── `SSH.NET 2026.0.0` (**Remediated**, 0 vulnerable packages)
- **Integration Fixture API Adjustment:**
  `Testcontainers.MsSql 4.15.0` obsoleted parameterless `MsSqlBuilder()` constructor (CS0618) which triggers compiler error under `TreatWarningsAsErrors=true`. Explicit image `mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04` was passed to the constructor. This retains the exact pre-cached Testcontainers 3.x baseline image in local Docker, avoids unneeded multi-gigabyte downloads, and strictly adheres to net8.0 compatibility.
- **Dependency Security Gate:**
  Added reusable script `tests/Security/Verify-DependencySecurity.ps1` that automates `dotnet list <ProjectPath> package --vulnerable --include-transitive` and fails fast with non-zero exit code upon encountering any High/Critical advisory.
- **Recovery / Downgrade Note:**
  If `Testcontainers.MsSql 4.15.0` must ever be downgraded to `3.10.0`, revert `RoadGuardSystem.IntegrationTests.csproj` and `SqlServerTestFixture.cs`, and add an explicit direct package reference `<PackageReference Include="SSH.NET" Version="2026.0.0" />` to keep `GHSA-q939-rpr3-3284` remediated.

---

## Existing Orphaned Databases Notice & Cleanup Script

During investigation, 6 lingering test databases created prior to this harden pass were detected on the local SQL Server instance:
- `RoadGuard_Test_0465cf9aee6f429190d0a39862e6edd6`
- `RoadGuard_Test_0949e7c3b9ec4a70a4a095ecebf23136`
- `RoadGuard_Test_3a1c9276346e45d3ad6d950b591973f5`
- `RoadGuard_Test_60df3fe4843641fe98715103ceed3e47`
- `RoadGuard_Test_80bcf8205745406f914f4a214a2306bc`
- `RoadGuard_Test_c4daf0dde203494eb5f0d83871294590`

Per repository owner instructions, these were **NOT** automatically dropped. To clean them up manually upon owner approval, run:

```powershell
powershell -Command @"
$dbs = @(
    'RoadGuard_Test_0465cf9aee6f429190d0a39862e6edd6',
    'RoadGuard_Test_0949e7c3b9ec4a70a4a095ecebf23136',
    'RoadGuard_Test_3a1c9276346e45d3ad6d950b591973f5',
    'RoadGuard_Test_60df3fe4843641fe98715103ceed3e47',
    'RoadGuard_Test_80bcf8205745406f914f4a214a2306bc',
    'RoadGuard_Test_c4daf0dde203494eb5f0d83871294590'
)
foreach ($db in $dbs) {
    sqlcmd -S '.\HANHNAV' -E -Q "IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$db') BEGIN ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$db]; END"
}
"@
```

---

## Files changed (21 files in diff from merge-base develop)

| Change | File | Purpose |
|---|---|---|
| Modified | `RoadGuardSystem.BusinessObjects/RoadGuardSystem.aBusinessObjects.csproj` | Added NetTopologySuite and package configuration for BusinessObjects |
| Added | `RoadGuardSystem.BusinessObjects/Spatial/SpatialConstants.cs` | Realized AllowedProjectUtmSrids as FrozenSet<int> exposed as IReadOnlySet<int>; added IsAllowedProjectUtmSrid |
| Added | `RoadGuardSystem.BusinessObjects/Spatial/SpatialValidation.cs` | Domain validation rules for GPS geography (SRID 4326) and Engineering geometry (UTM SRID 32648/32649) |
| Added | `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs` | Added isProduction parameter defaulting to true from composition root |
| Added | `RoadGuardSystem.Repositories/Options/RoadGuardDatabaseOptions.cs` | Removed IsProduction property to prevent untrusted configuration binding |
| Added | `RoadGuardSystem.Repositories/Options/RoadGuardDatabaseOptionsValidator.cs` | Validates production security rules based on explicit isProduction parameter |
| Added | `RoadGuardSystem.Repositories/RoadGuardDbContext.cs` | EF Core DbContext with NetTopologySuite support and base configuration for RoadGuard persistence |
| Modified | `RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj` | Added EF Core SQL Server and NetTopologySuite package references |
| Modified | `RoadGuardSystem.slnx` | Solution configuration registering all solution projects and dependencies |
| Added | `docs/worklogs/P2-00-completion.md` | Recorded vulnerability remediation, negative-first scan/gate evidence, dependency paths before/after, gate commands, and review handoff |
| Added | `tests/RoadGuardSystem.IntegrationTests/Configuration/DatabaseOptionsValidationTests.cs` | Added test proving configuration cannot bypass production security via IsProduction=false |
| Added | `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SpatialProbeDbContext.cs` | Test DbContext modeling spatial probe records for geography/geometry persistence testing |
| Added | `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SpatialProbeRecord.cs` | Test entity containing GPS location (geography 4326), Engineering geometry (geometry UTM SRID), and project SRID |
| Added | `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs` | Specified explicit cached SQL Server 2019 image in MsSqlBuilder constructor (CS0618 compliance); decoupled container ownership, injected env accessor, removed hardcoded fallbacks, guaranteed container disposal in finally |
| Added | `tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlTestEnvironmentUnavailableException.cs` | Typed exception for SQL Server environment unavailable diagnostics |
| Added | `tests/RoadGuardSystem.IntegrationTests/Persistence/SpatialPersistenceNegativeTests.cs` | Tightened assertions to exact exception types/messages; added direct raw SQL constraint tests |
| Added | `tests/RoadGuardSystem.IntegrationTests/Persistence/SqlServerDiagnosticTests.cs` | Added deterministic tests for unset/empty/whitespace/malformed/unreachable env var; safe cleanup failure test |
| Added | `tests/RoadGuardSystem.IntegrationTests/Persistence/SqlServerSpatialRoundTripTests.cs` | Updated Database_Is_Dropped_On_Fixture_Dispose to use sub-fixture on active server |
| Added | `tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj` | Upgraded Testcontainers.MsSql from 3.10.0 to 4.15.0 to remediate SSH.NET GHSA-q939-rpr3-3284 |
| Added | `tests/RoadGuardSystem.IntegrationTests/Spatial/SpatialInvariantTests.cs` | Added test verifying AllowedProjectUtmSrids is truly immutable (mutation throws NotSupportedException) |
| Added | `tests/Security/Verify-DependencySecurity.ps1` | Reusable JSON-based dependency security gate with internal parser, strict schema validation, and 12-case regression suite (-SelfTest) |

---

## Negative-first evidence

| Test | Layer | Expected failure/code | Result |
|---|---|---|---|
| Missing connection string | Repositories/Options | `ValidateOptionsResult.Failed` ("ConnectionString is required") | PASS |
| Whitespace connection string | Repositories/Options | `ValidateOptionsResult.Failed` | PASS |
| Malformed connection string | Repositories/Options | `ValidateOptionsResult.Failed` ("ConnectionString is malformed") | PASS |
| `TrustServerCertificate=true` in production | Repositories/Options | `ValidateOptionsResult.Failed` ("strictly forbidden in production") | PASS |
| `Encrypt=false` in production | Repositories/Options | `ValidateOptionsResult.Failed` ("Encrypt=false is strictly forbidden") | PASS |
| `EnableSensitiveDataLogging=true` in production | Repositories/Options | `ValidateOptionsResult.Failed` ("EnableSensitiveDataLogging=true is strictly forbidden") | PASS |
| Configuration cannot bypass production security with `IsProduction=false` | Repositories/Extensions | `ArgumentException` on DI registration | PASS |
| ValidateOrThrow with empty options | Repositories/Options | `ArgumentException` | PASS |
| AddRoadGuardPersistence with missing configuration | Repositories/Extensions | `ArgumentException` fail-fast on DI registration | PASS |
| Spatial geography with null | BusinessObjects/Spatial | `ArgumentNullException` | PASS |
| Spatial geography with SRID 0 | BusinessObjects/Spatial | `ArgumentException` ("Spatial SRID 0 is forbidden") | PASS |
| Spatial geography with non-4326 SRID | BusinessObjects/Spatial | `ArgumentException` ("GPS geography requires SRID 4326") | PASS |
| Spatial geometry with null | BusinessObjects/Spatial | `ArgumentNullException` | PASS |
| Spatial geometry with SRID 0 | BusinessObjects/Spatial | `ArgumentException` ("Spatial SRID 0 is forbidden") | PASS |
| Spatial geometry with unconfigured UTM SRID (3857) | BusinessObjects/Spatial | `ArgumentOutOfRangeException` ("Project UTM SRID not supported") | PASS |
| Spatial geometry with mismatched project SRID | BusinessObjects/Spatial | `ArgumentException` ("Geometry SRID does not match configured project UTM SRID") | PASS |
| AllowedProjectUtmSrids mutation throws NotSupportedException | BusinessObjects/Spatial | `NotSupportedException` on mutating collections | PASS |
| SaveChangesAsync rejects GPS geography with SRID 0 | IntegrationTests/Persistence | `ArgumentException` ("Spatial SRID 0 is forbidden") | PASS |
| SaveChangesAsync rejects GPS geography with non-4326 SRID | IntegrationTests/Persistence | `ArgumentException` ("GPS geography requires SRID 4326") | PASS |
| SaveChangesAsync rejects Engineering Geometry with SRID 0 | IntegrationTests/Persistence | `ArgumentException` ("Spatial SRID 0 is forbidden") | PASS |
| SaveChangesAsync rejects unsupported ProjectUtmSrid | IntegrationTests/Persistence | `ArgumentOutOfRangeException` ("Project UTM SRID 3857 is not supported") | PASS |
| SaveChangesAsync rejects mismatched EngineeringGeometry SRID | IntegrationTests/Persistence | `ArgumentException` ("does not match configured project UTM SRID") | PASS |
| Database CHECK constraint rejects invalid ProjectUtmSrid directly | SQL Server DB Check Constraint | `SqlException` ("CK_SpatialProbeRecords_ProjectUtmSrid") | PASS |
| Database CHECK constraint rejects invalid GpsLocation SRID directly | SQL Server DB Check Constraint | `SqlException` ("CK_SpatialProbeRecords_GpsLocation_Srid") | PASS |
| Database CHECK constraint rejects EngineeringGeometry SRID mismatch directly | SQL Server DB Check Constraint | `SqlException` ("CK_SpatialProbeRecords_EngineeringGeometry_Srid") | PASS |
| Unreachable SQL Server instance probe | IntegrationTests/Infrastructure | `CanConnectAsync` returns `false` (no false-green) | PASS |
| SQL Server unavailable diagnostic message | IntegrationTests/Infrastructure | `SqlTestEnvironmentUnavailableException` contains actionable guidance | PASS |
| Configured env var when empty fails immediately without fallback | IntegrationTests/Infrastructure | `SqlTestEnvironmentUnavailableException` ("empty or whitespace") | PASS |
| Configured env var when whitespace fails immediately without fallback | IntegrationTests/Infrastructure | `SqlTestEnvironmentUnavailableException` ("empty or whitespace") | PASS |
| Configured env var when malformed fails immediately without fallback | IntegrationTests/Infrastructure | `SqlTestEnvironmentUnavailableException` ("malformed") | PASS |
| Configured env var when unreachable fails immediately without fallback | IntegrationTests/Infrastructure | `SqlTestEnvironmentUnavailableException` ("could not connect...refusing fallback") | PASS |
| Cleanup failure is observed and does not leak test database | IntegrationTests/Infrastructure | `InvalidOperationException` thrown; database dropped in finally | PASS |
| `dotnet list ... package --vulnerable --include-transitive` (pre-fix baseline) | Tests/Dependencies | Flags `SSH.NET 2023.0.0` (High, GHSA-q939-rpr3-3284) | PASS (vulnerability detected) |
| `tests/Security/Verify-DependencySecurity.ps1` (pre-fix baseline) | Security Gate | Exits with code 1; reports High advisory GHSA-q939-rpr3-3284 on SSH.NET 2023.0.0 | PASS (RED confirmed) |
| Standard CLI rejects test-only `-JsonInput` parameter | Security Gate | Pre-fix: returned 0 with false SUCCESS; Post-fix: fails with exit code 1 (`NamedParameterNotFound`), no SUCCESS emitted | PASS (RED confirmed) |
| `Verify-DependencySecurity.ps1 -ProjectPath AGENTS.md` (non-project path) | Security Gate | Exits with code 1; scanner error caught; does not print SUCCESS | PASS (RED confirmed) |
| Schema: empty JSON object `{}` | Security Gate | Pre-fix: exited 0 with false SUCCESS; Post-fix: exits 1 ("missing or invalid schema version"), no SUCCESS | PASS (RED confirmed) |
| Schema: missing version `{"projects":[{"path":"test.csproj"}]}` | Security Gate | Exits 1 ("missing or invalid schema version"), no SUCCESS | PASS (RED confirmed) |
| Schema: empty projects array `{"version":1,"projects":[]}` | Security Gate | Pre-fix: exited 0 with false SUCCESS; Post-fix: exits 1 ("missing or empty 'projects' array"), no SUCCESS | PASS (RED confirmed) |
| Schema: project missing path `{"version":1,"projects":[{}]}` | Security Gate | Pre-fix: exited 0 with false SUCCESS; Post-fix: exits 1 ("missing required 'path' property"), no SUCCESS | PASS (RED confirmed) |
| Parser: corrupt JSON `{ invalid json ...` | Security Gate | Exits 1 ("Failed to parse vulnerability scanner JSON output"), no SUCCESS | PASS (RED confirmed) |
| Parser: scanner problems array | Security Gate | Exits 1 ("Scanner reported problem(s)"), no SUCCESS | PASS (RED confirmed) |
| Findings: topLevelPackages High/Critical blocked | Security Gate | Exits 1 ("Detected 1 vulnerable package(s) matching severities... [Top-level]"), no SUCCESS | PASS (RED confirmed) |
| Findings: transitivePackages High/Critical blocked (`SSH.NET 2023.0.0`) | Security Gate | Exits 1 ("Detected 1 vulnerable package(s) matching severities... [Transitive]"), no SUCCESS | PASS (RED confirmed) |

---

## Positive evidence

| Test | Layer | Expected state/output | Result |
|---|---|---|---|
| Valid connection string passes options validation | Repositories/Options | `ValidateOptionsResult.Succeeded` is true | PASS |
| AddRoadGuardPersistence succeeds with valid configuration | Repositories/Extensions | ServiceCollection contains registered DbContext and Options | PASS |
| Geography Point with SRID 4326 succeeds | BusinessObjects/Spatial | No exception thrown | PASS |
| Engineering geometry with UTM SRID 32648 succeeds | BusinessObjects/Spatial | No exception thrown | PASS |
| Engineering geometry with UTM SRID 32649 succeeds | BusinessObjects/Spatial | No exception thrown | PASS |
| Fixture creates isolated, collision-safe database | IntegrationTests | Database named `RoadGuard_Test_{Guid:N}` | PASS |
| Round-trip Point as geography(4326) on real SQL Server | IntegrationTests | Persisted and queried back with exact coords (105.854444, 21.028511) and SRID 4326 | PASS |
| Round-trip LineString with UTM SRID 32648 on real SQL Server | IntegrationTests | Persisted and queried back with exact coords and SRID 32648 | PASS |
| Round-trip LineString with UTM SRID 32649 on real SQL Server | IntegrationTests | Persisted and queried back with exact coords and SRID 32649 | PASS |
| SQL Server catalog column types confirmed via `sys.columns` | IntegrationTests | `GpsLocation` is `geography`, `EngineeringGeometry` is `geometry` | PASS |
| Database is reliably dropped on fixture dispose | IntegrationTests | Confirmed database count in `sys.databases` on same instance is 0 after `DisposeAsync` | PASS |
| `dotnet list ... package --vulnerable --include-transitive --no-restore` (post-fix) | Tests/Dependencies | `RoadGuardSystem.IntegrationTests has no vulnerable packages` | PASS |
| `tests/Security/Verify-DependencySecurity.ps1` (post-fix standard run) | Security Gate | Exits with code 0; reports 0 vulnerable packages; SUCCESS emitted | PASS (GREEN confirmed) |
| Real clean JSON without frameworks (clean scanner report) | Security Gate | Exits with code 0; SUCCESS emitted | PASS (GREEN confirmed) |
| Clean JSON with frameworks and Low-only vulnerabilities | Security Gate | Exits with code 0; SUCCESS emitted | PASS (GREEN confirmed) |
| `Verify-DependencySecurity.ps1 -SelfTest` (Full regression suite) | Security Gate | All 12 regression tests pass (12/12) | PASS (GREEN confirmed) |
| Testcontainers isolated run after 4.15.0 upgrade | IntegrationTests | 43 passed, 0 failed in 14s | PASS |
| Solution-wide tests after upgrade (Unit + API + Integration) | All test suites | 71 passed, 0 failed in 14s | PASS |
| Code formatting verification | Solution | `dotnet format --verify-no-changes --no-restore` clean | PASS |
| Git diff whitespace and conflict check | Solution | `git diff --check` clean | PASS |

---

## Commands run

| Command | Exit code | Result/coverage | Timestamp |
|---|---:|---|---|
| `powershell -Command "Remove-Item env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING -ErrorAction SilentlyContinue; dotnet test tests/RoadGuardSystem.IntegrationTests --filter 'TaskId=P2-00'"` | 0 | Testcontainers isolated run: 43 passed, 0 failed in 15s | 2026-09-16T22:42:51 |
| `powershell -Command "[System.Environment]::SetEnvironmentVariable('ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING', 'Server=.\HANHNAV;Database=master;Integrated Security=True;TrustServerCertificate=True', 'Process'); dotnet test tests/RoadGuardSystem.IntegrationTests --filter 'TaskId=P2-00'"` | 0 | Local SQL Server run 1: 43 passed, 0 failed in 8s | 2026-09-16T22:44:21 |
| `powershell -Command "[System.Environment]::SetEnvironmentVariable('ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING', 'Server=.\HANHNAV;Database=master;Integrated Security=True;TrustServerCertificate=True', 'Process'); dotnet test tests/RoadGuardSystem.IntegrationTests --filter 'TaskId=P2-00'"` | 0 | Local SQL Server run 2: 43 passed, 0 failed in 8s | 2026-09-16T22:45:15 |
| `powershell -Command "sqlcmd -S '.\HANHNAV' -E -Q 'SELECT name FROM sys.databases WHERE name LIKE ''RoadGuard_Test_%'''"` | 0 | Zero leaked databases: database count remained strictly at 6 (pre-existing) | 2026-09-16T22:45:46 |
| `dotnet restore RoadGuardSystem.slnx` | 0 | Gate 1: Clean restore across all 8 projects | 2026-09-16T22:46:09 |
| `dotnet test tests/RoadGuardSystem.IntegrationTests --filter "TaskId=P2-00"` | 0 | Gate 2: 43 passed, 0 failed in 15s | 2026-09-16T22:46:42 |
| `powershell -Command "Remove-Item env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING -ErrorAction SilentlyContinue; dotnet test tests/RoadGuardSystem.IntegrationTests --filter 'TaskId=P2-00'"` | 0 | Gate 3: Testcontainers path without explicit connection: 43 passed, 0 failed in 15s | 2026-09-16T22:47:33 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | Gate 4: Clean build across all projects (0 warnings, 0 errors) | 2026-09-16T22:48:01 |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Gate 5: Clean formatting verified (0 changes) | 2026-09-16T22:48:35 |
| `dotnet test RoadGuardSystem.slnx --no-build` | 0 | Gate 6: All 71 tests passed solution-wide (26 Unit, 2 Api, 43 Integration) | 2026-09-16T22:49:20 |
| `git diff --check` | 0 | Gate 7: Clean diff check (no whitespace errors or conflict markers) | 2026-09-16T22:49:48 |
| `powershell -Command "sqlcmd -S '.\HANHNAV' -E -Q 'SELECT name FROM sys.databases WHERE name LIKE ''RoadGuard_Test_%'''"` | 0 | Gate 8: Verified database count still exactly 6 after all gate executions | 2026-09-16T22:50:04 |
| `dotnet list tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj package --vulnerable --include-transitive` | 0 | Remediation RED evidence: identified SSH.NET 2023.0.0 (High, GHSA-q939-rpr3-3284) | 2026-09-17T16:00:19 |
| `powershell -ExecutionPolicy Bypass -File tests/Security/Verify-DependencySecurity.ps1` | 1 | Remediation RED evidence: security gate fails fast and detects SSH.NET 2023.0.0 High | 2026-09-17T16:03:29 |
| `dotnet restore RoadGuardSystem.slnx` | 0 | Restored solution with Testcontainers.MsSql 4.15.0; SSH.NET resolves to 2026.0.0 | 2026-09-17T16:03:41 |
| `dotnet list tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj package --vulnerable --include-transitive --no-restore` | 0 | Remediation GREEN evidence: 0 vulnerable packages reported | 2026-09-17T16:03:48 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | Clean build with 0 errors and 0 warnings (CS0618 resolved by explicit image) | 2026-09-17T16:06:23 |
| `powershell -ExecutionPolicy Bypass -File tests/Security/Verify-DependencySecurity.ps1 -JsonInput "{}"` | 0 | RED regression finding: test-only parameter accepted by CLI, bypassed scanner with false SUCCESS | 2026-09-17T16:40:40 |
| `powershell -ExecutionPolicy Bypass -File tests/Security/Verify-DependencySecurity.ps1 -JsonInput "{}"` | 1 | GREEN regression verification: parameter removed; CLI rejects JsonInput (NamedParameterNotFound), no SUCCESS | 2026-09-17T16:42:11 |
| `powershell -ExecutionPolicy Bypass -File tests/Security/Verify-DependencySecurity.ps1 -ProjectPath AGENTS.md` | 1 | GREEN regression verification: non-project path fails scanner with code 1, prints FAILURE, no SUCCESS | 2026-09-17T16:42:14 |
| `powershell -ExecutionPolicy Bypass -File tests/Security/Verify-DependencySecurity.ps1 -SelfTest` | 0 | Hardened security gate regression suite: all 12 test cases pass | 2026-09-17T16:42:04 |
| `powershell -ExecutionPolicy Bypass -File tests/Security/Verify-DependencySecurity.ps1` | 0 | Standard JSON scan on IntegrationTests: 0 vulnerable dependencies, SUCCESS emitted | 2026-09-17T16:42:09 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | Verification clean build: 0 errors, 0 warnings | 2026-09-17T16:24:42 |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Verification format check: clean (0 changes) | 2026-09-17T16:24:54 |
| `powershell -Command "Remove-Item env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING -ErrorAction SilentlyContinue; dotnet test tests/RoadGuardSystem.IntegrationTests --filter 'TaskId=P2-00'"` | 0 | Testcontainers path rerun: 43 passed, 0 failed in 14s | 2026-09-17T16:25:20 |
| `powershell -Command "Remove-Item env:ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING -ErrorAction SilentlyContinue; dotnet test RoadGuardSystem.slnx --no-build"` | 0 | Solution-wide test rerun: 71 passed, 0 failed across Unit, API, and Integration | 2026-09-17T16:25:46 |
| `git diff --check` | 0 | Verification diff check: 0 errors | 2026-09-17T16:26:03 |

---

## Review handoff

- **Observable demo/output:** 43 integration tests executing against both local configured SQL Server and pure Testcontainers demonstrating:
  1. Options validation and rejection of `TrustServerCertificate=true`, `Encrypt=false`, and `EnableSensitiveDataLogging=true` in production; decoupling from configuration binding (`IsProduction` cannot be set via JSON).
  2. Truly immutable spatial constants (`FrozenSet<int>`) and domain validation homed in `BusinessObjects.Spatial`.
  3. Strict fail-fast behavior on empty, whitespace, malformed, or unreachable `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` without fallback.
  4. Automatic Testcontainers fallback only when the environment variable is completely unset.
  5. Decoupled fixture lifecycle ensuring safe database dropping and teardown error observation with zero database leaks.
  6. Persistence-level validation in `SaveChangesAsync` and database CHECK constraints on SQL Server (`CK_SpatialProbeRecords_ProjectUtmSrid`, `CK_SpatialProbeRecords_GpsLocation_Srid`, `CK_SpatialProbeRecords_EngineeringGeometry_Srid`).
  7. Real SQL Server round-trip for geography and geometry columns on an isolated test database.
  8. Hardened JSON-based dependency vulnerability gate (`tests/Security/Verify-DependencySecurity.ps1`) with built-in regression test suite (`-SelfTest`), checking scanner exit code, topLevelPackages, and transitivePackages, fail-closed on corrupt output, and remediation of High advisory `GHSA-q939-rpr3-3284` via `Testcontainers.MsSql 4.15.0` (transitive `SSH.NET 2026.0.0`).
- **Proven test environments:** Both local configured SQL Server (`ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` fail-fast validation) and dynamic Testcontainers MsSql container verified green with zero lingering databases.
- **Known gaps, skipped tests, and reason:** None. Zero skipped tests.
- **Residual risks:** Docker Desktop is required to run `RoadGuardSystem.IntegrationTests` when `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` is unset. Upstream P1 projects (`RoadGuardSystem.API`, `RoadGuardSystem.Services`, `RoadGuardSystem.ApiTests`) retain independent transitive dependency `System.Security.Cryptography.Xml 8.0.2` which is outside Person 2 scope and tracked under P1-00/P1-01.
- **Reviewer findings and resolution:** Findings RRF-1 through RRF-6 resolved and verified; GHSA-q939-rpr3-3284 remediated; gate script hardened with JSON parser, scanner exit-code assertion, and regression suite.
- **Exact next task/action:** Hand off task `P2-00` updates to Person 1 for re-cross-review before Wave 0 merge into `develop`.
- **Final status:** `Ready for cross-review`
