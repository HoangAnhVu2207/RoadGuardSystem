# P1-20 Completion Evidence

## Status

P1-20 API and SQL verification completed on branch `anh` at `2fb2d54` on 2026-09-21. The implementation covers project creation, metadata update, warranty creation, and primary-PM reassignment through the accepted `Controller -> IService -> IRepository` flow.

## Fresh verification

- `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly` completed with 0 errors. The 86 warnings are the existing analyzer baseline; no new warning policy exception was added.
- `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v q --filter "TaskId=P1-20"` passed 9/9 with 0 failures and 0 skipped. These authenticated API tests execute HTTP requests through `AuthenticationWebApplicationFactory` against an isolated SQL Server Testcontainers database and verify persisted project, membership, warranty, audit, concurrency, and idempotency effects.
- `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj -nologo -v q -clp:ErrorsOnly` completed with 0 errors. The 102 warnings are the existing analyzer baseline.
- `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --nologo -v q --filter "TaskId=P1-72|FullyQualifiedName~SessionDeviceMetadataValidatorTests"` passed 30/30 with 0 failures and 0 skipped. This is supplemental P1-72 boundary evidence and does not close P1-72.

## Scope limits

- `RoadGuardSystem.API/RoadGuardSystem.API.http` contains runnable P1-20 requests. A standalone local host smoke was not run because this worktree intentionally has no approved local SQL connection string or JWT credentials; the authenticated API suite supplies the isolated HTTP and SQL verification for this closure.
- P1-72 remains `In Progress`: hosted-CI, timing, and the broader affected Integration suite remain assigned verification work. No claim about those gates is made here.
- No production source, schema, migration, package, seed data, commit, merge, push, deployment, or external CI state changed during this verification closure.
