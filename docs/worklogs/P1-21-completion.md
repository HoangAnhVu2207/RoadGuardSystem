# P1-21 Completion Evidence

## Status

P1-21 completed on branch `anh` at the working-tree artifact based on `2fb2d54`. The feature adds Supervisor-only endpoints to create a logical RoadSection with immutable Version 1, and to replace current geometry with Version N+1.

## Delivered contract

- `POST /api/v1/projects/{projectId}/road-sections` creates the RoadSection and Version 1 atomically.
- `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions` creates a new immutable geometry version when `expectedCurrentVersionId` matches.
- The Service owns actor policy, input construction, and result mapping. The Repository owns SQL transactions, idempotency, the current-marker persistence backstop, and audit records.
- Project UTM SRID must be configured and match the supplied LineString SRID. A non-Supervisor actor is rejected before persistence.
- The next-version race returns `road_section_concurrency_conflict`; repeated matching operation IDs replay the stored response. Audit snapshots contain IDs, code, and version numbers only. Client-supplied change reasons remain on the immutable version entity and are not copied to audit reason/snapshots.

## Fresh verification

- `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly` completed with 0 errors; the 61 warnings are existing analyzer baseline warnings.
- `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v q --filter "FullyQualifiedName~P121RoadSectionVersionTests"` passed 5/5 with 0 failures and 0 skipped. These authenticated API tests use an isolated SQL Server Testcontainers database and cover initial creation/replay/audit privacy, N+1 marker transition, stale request, concurrent race, SRID mismatch, and authorization.
- `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj -nologo -v q -clp:ErrorsOnly` completed with 0 warnings and 0 errors.
- `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v q --filter "FullyQualifiedName~P221RoadWarrantySchemaTests"` passed 6/6 with 0 failures and 0 skipped.
- `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj -nologo -v q -clp:ErrorsOnly` completed with 0 warnings and 0 errors.
- `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --nologo -v q --filter "FullyQualifiedName~ServiceSourceBoundaryTests|FullyQualifiedName~RepositorySourceBoundaryTests|FullyQualifiedName~RoadSectionVersionServiceTests"` passed 3/3 with 0 failures and 0 skipped.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File tests/Documentation/Verify-P102Docs.ps1` passed.

## Scope limits

- No schema, migration, model snapshot, package, seed data, commit, merge, push, deployment, or hosted-CI change was made.
- The HTTP file contains both runnable endpoint requests. A standalone host smoke was not run because the worktree has no approved local SQL connection string or JWT credentials; the authenticated API suite verifies the same HTTP and SQL boundaries in isolation.
- P1-72 remains `In Progress` for its independent hosted-CI, timing, and broader integration verification handoff.
