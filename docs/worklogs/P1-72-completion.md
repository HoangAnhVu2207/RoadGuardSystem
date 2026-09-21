# P1-72 Implementation Handoff

## Status

Implementation is complete on branch `anh` for the approved source changes. The Team Lead approved a combined scope covering P1-20 completion work, A0, S1-S6, and the Phase 1 architecture-boundary follow-ups. P1-72 remains `In Progress` in planning until the deferred Huy verification handoff supplies runtime, SQL, timing, and hosted-CI evidence.

## Constraints honored

- No schema, migration, model snapshot, package, data, merge, push, or deployment change.
- No `dotnet test`, API smoke, Docker test, coverage run, or hosted CI run was performed during implementation.
- `dotnet build RoadGuardSystem.slnx --no-restore -nologo -v q -clp:ErrorsOnly` is the only runtime-adjacent verification performed.

## Functional layout

Cross-cutting Service files were grouped by function: DI registration in `Extensions`, configuration contracts in `Options`, cryptographic construction in `Factories`, and secure credential/token generation in `Generators`. Domain files remain in their feature folders. The accepted directory layout is documented in `docs/architecture/backend-structure.md`.

## Completed boundary moves

- `RoadSectionVersion` no longer receives a Project SRID. `RoadSectionVersionService` obtains project/current-version facts through `IRoadSectionVersionRepository`, applies SRID and increasing-version policy, and the repository retains only persistence transaction and race backstops.
- `SurveyAssignmentService` validates the reassignment command, idempotency scope, and active-assignment facts before persistence. `ISurveyAssignmentRepository` supplies facts and atomically persists the replacement, audit, outbox, and durable idempotency outcome.
- Session metadata validation remains outside BusinessObjects; cross-cutting Services are grouped into `Extensions`, `Options`, `Factories`, and `Generators` without moving domain files.
- Static seam review found Services depend on persistence/read-model interfaces, have no EF/HTTP dependencies, and API has no direct repository/DbContext calls.
- `BusinessObjects` is now entity-only: domain entities are one per file, while `Common/Enums.cs` and `Common/Extensions/` contain the allowed enum definitions and enum extensions. Options, validators, sanitizers, spatial helpers/constants, persistence conventions, and security-log codes moved to the owning Repository/Service area.
- Entity factories no longer call clocks or random generators. Persistence supplies generated IDs/timestamps, and session/token derived state accepts an explicit `now` value.
- `DTOs` is contract-only: every DTO now has one public type per file, legacy batch/pagination shapes no longer generate timestamps, clamp values, or compute business status, and the DTO namespace matches its project name.
- Repositories retain the ADR 001 project reference to DTOs but have no DTO namespace use in source; `RepositorySourceBoundaryTests` locks that rule. Role eligibility/authorization policy moved from `IdentityRepository` to `IdentityService`, while Repository retains role existence, transaction, revocation, audit, and idempotency persistence work.
- Services are locked as the use-case policy layer: `ServiceSourceBoundaryTests` scans for HTTP/MVC, EF Core, and `RoadGuardDbContext` dependencies; none are present in current source. Controllers use `IService` interfaces and Service persistence seams use repository interfaces.
- The direct `ProjectReference` matrix is verified and documented without amending ADRs: BusinessObjects has no project references; DTOs references BusinessObjects; Repositories references BusinessObjects and DTOs under ADR 001; Services references Repositories; API references Services.

## Build evidence

`dotnet build RoadGuardSystem.slnx --no-restore -nologo -v q -clp:ErrorsOnly` completed with `0 Error(s)` after the final boundary changes. Compiler warnings remain baseline or test-project analyzer warnings and require separate triage.

## Huy verification handoff

Run focused API/SQL characterization for P1-20, Project/Warranty/Identity behavior, and the moved session metadata validator. Then run the affected Unit, API, and Integration suites with `--no-build`. Verify CI YAML in a hosted run before accepting S6. Measure S2-S4 before claiming a timing improvement.

## Known limits

- The integration baseline retained one known P2-23 replay failure before P1-72; P1-72 did not change or re-run that scenario.
- S2 shares a Testcontainers SQL instance per test process while retaining database isolation. Schema-once/reset behavior requires SQL verification before further consolidation.
- S3 host reuse and S4 hash-cost benefit require measured verification; no timing claim is made by this handoff.
