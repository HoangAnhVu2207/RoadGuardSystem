# RoadGuard C#/.NET stack contract

Use the existing supported solution target when one exists. If the solution is still a skeleton, select a currently supported .NET LTS SDK, pin the exact feature band in `global.json`, keep the corresponding default C# language version, and record the selection/support window in an ADR. Do not silently retarget an existing solution.

## Project and dependency rules

- Keep the boundaries `API -> Services -> Repositories -> BusinessObjects`; `DTOs` contains public contracts and does not expose EF entities.
- `BusinessObjects` has no dependency on API, Services, Repositories, DTOs, EF Core, or transport details.
- Use EF Core SQL Server plus NetTopologySuite. Configure SQL Server spatial columns explicitly (`geography(4326)` for GPS/raw points and the project UTM SRID for engineering geometry).
- Use explicit enum numeric values, `Unknown = 0`, and append-only additions. Map status/scope enums to `tinyint` when their range permits.
- Use `DateTimeOffset` persisted as UTC, `DateOnly` for calendar dates, `decimal(19,2)` for VND, SHA-256 lowercase hex for checksums, and JSON stored as `nvarchar(max)` with `ISJSON` plus application schema validation.
- Use options classes for limits, storage, queue, AI, and SRID configuration. Validate options at startup; no magic numbers or environment-specific constants in domain logic.

## API and security

- Controllers only bind/validate/authorize/dispatch/map results. Workflow, cross-aggregate checks, transactions, and state transitions live in Services/domain policies.
- Return DTOs, `ProblemDetails`, stable error codes, and a correlation ID. Do not leak EF tracking state, stack traces, passwords, tokens, or internal connection details.
- Every non-Supervisor query and command checks server-side project membership; never trust a project ID or role claim by itself.
- Use the repository's chosen authentication mechanism (baseline: ASP.NET Core Identity with short-lived JWT access tokens and hashed refresh-token records). Revoke sessions on suspend and password reset.

## Persistence and workers

- Migrations are immutable once shared. A new schema change gets a new migration, mapping tests, and a downgrade/recovery note.
- Add optimistic concurrency to mutable workflow aggregates and return a conflict error for stale writes.
- Retryable mobile/worker commands require an idempotency key or a database uniqueness constraint. Outbox/job handlers must be safe to run twice.
- Original files, submitted measurements, evidence, approved versions, and audit/retention records are append-only. New content means a new version/record.
- Keep file storage, clock, current user, notification, queue, and AI service behind interfaces. Phase 1 AI uses a deterministic fake with a versioned contract.

## Test and delivery gates

- Unit tests cover invariants and state transitions; integration tests cover SQL constraints, spatial mapping, transactions, authorization, and concurrency; API tests cover contracts and status/error codes; worker tests cover retry/idempotency; end-to-end tests cover one complete seeded flow.
- Use SQL Server for claims involving spatial, filtered unique indexes, check constraints, or transaction behavior. EF InMemory is not evidence for those claims.
- Before handoff run, as applicable: `dotnet restore`, `dotnet format --verify-no-changes`, `dotnet build --no-restore`, `dotnet test --no-build`, and coverage/report commands defined by CI. Record skipped commands and why.
- CI must fail on build, format, or test failure. Local secrets belong in user secrets/environment/secret store, never committed config.
