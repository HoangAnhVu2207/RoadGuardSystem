# RF-06 isolated fixture and current-behavior baseline

## Fingerprint and ownership

- Branch `anh`, local HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`, dirty working tree. This is the local working tree, not a clean remote revision. Captured 2026-09-30 (Asia/Bangkok).
- .NET SDK `10.0.401`, test target `net8.0`, Docker server `29.6.1`, SQL image `mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04`, Testcontainers.MsSql `4.15.0`.
- The inherited `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` was present but its value was not read or recorded. Normal RF-06 API and integration fixtures now use Testcontainers. Each fixture creates a GUID-named `RoadGuard_ApiTest_*` or `RoadGuard_Test_*` database. The fixture owns its container/master connection and exact generated database name before create, migration, seed or drop. The API host accepts only a live registered API fixture connection. Integration subfixtures accept only the live container master connection.
- `AuthenticationSqlServerFixture` owns its container and drops its generated database before disposal. `SqlServerTestFixture` shares a container only among live fixtures in one process and disposes it when the final holder ends. Setup failures invoke the same cleanup path. A disposal failure is surfaced to the test runner.
- Platform/error tests use a disconnected sentinel connection, force startup initialization and development seed off, remove hosted services and keep storage endpoint empty. They require no SQL or Docker. Auth/project/survey/upload tests use the isolated SQL fixture; the upload characterization uses a deterministic storage fake. The explicit opt-in MinIO smoke test is outside this baseline and was not run.

## CG15 cause and reproduction

- `CURRENT_VERIFIED`: `Program.cs` invokes `DbInitializer.InitializeAsync` in Development when `RoadGuardDatabase:InitializeOnStartup=true`; `DbInitializer` migrates then invokes the seeder. `RoadGuardPersistenceExtensions.AddRoadGuardSeeding` includes `PostmanUserSeedStep` and `PostmanScenarioSeedStep` when `SeedDevelopmentUsers=true`. The local Development config has both switches true and a connection value (value withheld). The old `CustomWebApplicationFactory("Development")` did not override these switches; its fixed connection setting also did not prove isolation.
- RF-00 recorded 19 `ProblemDetailsNegativeTests` failing before HTTP assertion at `PostmanScenarioSeedStep.ValidateExistingFixtureAsync`: existing `SurveyId` had incompatible ownership/dependencies. The original database content was not inspected or changed. The exact stale row origin remains `UNKNOWN`.
- `SeederTests.PostmanScenarioSeed_RejectsSurveyOwnershipCollision_InIsolatedDatabase` reproduces the same survey collision condition on a disposable Testcontainers database. Three existing Postman seed tests establish idempotent seed, deliberate project collision and concurrent convergence. None of these tests justify changing production seed policy.
- First RF-06 guard run failed all 19 before HTTP because the Development local config still overrode `UseSetting`; `CustomWebApplicationFactory` now sets the resolved configuration before `app.Build` completes and refuses an unexpected state. The 19 tests then reached HTTP assertions and passed. No production startup seam was needed.

## Current HTTP and durable behavior

| Group / test | `CURRENT_VERIFIED` current behavior | Durable proof / limit |
|---|---|---|
| Pilot `GET /api/v1/projects/{projectId}/work-package`, `P112ProjectAuthorizationTests` | 200 projected project/road/warranty body for assigned member or supervisor; 401 unauthenticated; 403 expired/wrong project/forged scope; 404 missing project for supervisor. | Membership changes use separate context before next request. This is a read projection; no exhaustive SQL no-write audit was run. Draft work-package OpenAPI remains proposed. |
| Auth `/auth/login`, `/auth/refresh`, `/auth/logout`, `AuthenticationFlowTests` | 200 login/refresh, 204 logout and replay, 401 invalid/revoked credentials; invalid login is 400. | Separate contexts verify refresh hash, session revocation, password update and absence of session on failed login. |
| Survey `POST /projects/{id}/survey-plans`, `/survey-plans/{id}/postpone`, `/projects/{id}/survey-tasks`, `GET /survey-tasks/{id}`, `P2SurveyV2ApiTests` | 201 create/replay, 409 invalid scope or duplicate distinct operation, 200 postpone, 412 stale version, 403 other operator read; ETag/version present on created plan. | Fresh context confirms invalid scope created zero plans; replay left one plan, stale request left one postponement, task exists once. V2 behavior is observed, not presumed the target policy. |
| Upload `/uploads`, `/uploads/{id}/part-urls`, `/uploads/{id}/complete`, `/files/{id}`, `/files/{id}/content`, `UploadApiTests` deterministic-storage case | 201 create with ETag; 200 part/session; 412 stale completion; 202 verifying; 200 verified metadata/download; 403 outsider. | Fresh context confirms stale completion left `Uploading`, later completion/verification persisted `Verified` and file scope project ID. Real MinIO was not called. |
| Platform/error `ProblemDetailsNegativeTests` (19) | 400/404/405/415/500 cases emit `application/problem+json`, stable code, correlation ID/header and no stack details under tested paths. | No database accessed by this disconnected host. This characterizes tested error paths only. |

These results describe current implementation. Accepted product decisions 32-44 live in `docs/product/confirmed-decisions.md`; this baseline does not mark the HTTP contract or business workflows accepted.

## Verification commands and limits

| Command/filter | Executed / passed / failed / skipped | Meaning |
|---|---:|---|
| `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore -nologo -v q -clp:ErrorsOnly` | build PASS, 89 warnings, 0 errors | Fresh affected API test build before initial runtime. |
| `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore -nologo -v q -clp:ErrorsOnly` | build PASS, 322 warnings, 0 errors | Fresh affected integration test build before initial runtime. |
| `dotnet test ...ApiTests.csproj --filter FullyQualifiedName~ProblemDetailsNegativeTests` | 19 / 19 / 0 / 0 | RF-00 startup failure group now reaches HTTP assertions. |
| Focused API filter: `P112ProjectAuthorizationTests`, `AuthenticationFlowTests`, `P2SurveyV2ApiTests`, deterministic `UploadEndpoints_EnforceScopeIdempotencyAndIfMatchBeforeVerifiedDownload`, `Rf06IsolationTests`, `ApiStartupTests`, `ApiPlatformPositiveTests` | 47 / 47 / 0 / 0 | Fresh build/test after durable assertions; real Testcontainers for SQL groups. |
| Combined preceding API filter plus `ProblemDetailsNegativeTests`, new process | 66 / 66 / 0 / 0 | Repeatability after latest auth factory edit; test binaries built from current source. |
| `dotnet test ...IntegrationTests.csproj --filter FullyQualifiedName~SqlServerSpatialRoundTripTests`, separate processes | 7 / 7 / 0 / 0 on each of two runs | SQL Server spatial and isolated DB create/drop. |
| `dotnet test ...IntegrationTests.csproj --filter FullyQualifiedName~SqlServerDiagnosticTests` | 8 / 8 / 0 / 0 | Invalid configuration, arbitrary master rejection and cleanup failure handling. |
| `dotnet test ...IntegrationTests.csproj --filter FullyQualifiedName~PostmanScenarioSeed` | 4 / 4 / 0 / 0 | Includes exact survey collision reproduction on isolated DB. |
| Final combined SQL filter: `SqlServerSpatialRoundTripTests`, `SqlServerDiagnosticTests`, `PostmanScenarioSeed` | 19 / 19 / 0 / 0 | Fresh build after the final `CreateDbContext` ownership guard. |

`dotnet test` without `--no-build` was used after source edits. Later `--no-build` runs reused binaries built from unchanged test source. Full API/integration suites, live external MinIO/AI, hosted CI, FE lock and production database were **NOT RUN**. The RF-00 FE `CONTRACT_LOCK_MISMATCH` remains a separate baseline finding. No clean remote checkout claim is made.

## Re-run and recovery

Use the focused filters above from this checkout with Docker available. Do not set or copy a shared SQL connection: normal fixtures ignore the inherited variable and create their own container. A fixture that cannot prove ownership throws before database creation or host startup. The test runner disposes only its container and generated DB; if teardown reports failure, inspect that exact container/DB ownership before manual cleanup. Revert only RF-06 test-host/fixture/test edits to recover; no production migration or data rollback applies.
