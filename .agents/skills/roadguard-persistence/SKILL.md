---
name: roadguard-persistence
description: Use when a RoadGuard task changes EF Core queries, mappings, atomic writes, migrations, SQL Server concurrency, idempotency, outbox, storage, or offline durability.
---

# RoadGuard persistence

Read applicable AGENTS, `planning/V2/TASK_LIFECYCLE.md`, the assigned task `Source evidence`, current decision/DD/ERD rows and only its entity, repository interface/implementation, mapping and SQL fixture. Record current EF/migration evidence separately from target/proposed model. Read [risk cases](references/risk-cases.md) only for touched mechanisms. Reuse existing persistence primitives instead of generating parallel transaction/idempotency frameworks.

## Preserve boundaries and ownership

Use `RoadGuardSystem.Repositories/Interfaces/<Domain>/` and `Implementations/<Domain>/`; put technical mechanisms in their existing functional folders. Keep entities in the matching BusinessObjects domain and mappings/migrations in Repositories. Preserve actual namespaces and persisted enum values. A Service-injected persistence/read model needs an interface. Do not expose EF, DbContext or IQueryable to Services; do not reference DTOs in repository source despite the permitted project reference.

Return persisted facts and outcomes. Services decide identity/scope permissions, business calculations, transitions and stable business result codes; API maps HTTP. Repositories may enforce uniqueness, expected versions and other data-integrity backstops, but must not become a second business-policy engine. Receive authorized scope/commands from the service; do not ignore scope in the SQL predicate.

The approved V2 task owner owns the directly required persistence slice under ADR 006. Verify that the scope names any shared DbContext, entity shape, mapping, migration, snapshot, seed, DI, or schema effect. Coordinate one writer per shared hotspot and sequence concurrent migrations through Person 2. Do not infer permission to run a migration against a live database from permission to draft it.

## Implement the smallest durable change

1. Inspect the nearest existing repository and SQL test plus relevant DbContext mapping/snapshot. Select only required columns and use AsNoTracking for read-only queries; keep tracking when needed for mutation. Avoid N+1/unbounded materialization; preserve agreed pagination/order and SQL translation.
2. Define the atomic unit: domain writes, expected version, durable idempotency result, audit and required outbox records. Reuse existing transaction/execution strategy conventions. Keep network calls outside the database transaction; record work for a worker instead.
3. Guard service-validated preconditions against races with concurrency tokens/conditional writes and constraints. Recheck required facts inside the atomic boundary or require matching versions. An earlier authorized read alone cannot protect a later update from concurrent reassignment.
4. Scope idempotency by existing actor/project/operation rules. Fingerprint relevant payload; same identity+same payload replays, mismatched payload conflicts. Persist outcome with effects; handle concurrent duplicate and commit-before-response loss. Never authorize replay from another caller or treat an unknown commit as a safe new operation.
5. Apply new migrations only when schema changes are needed and authorized. Inspect generated SQL/snapshot for accidental drop/rename/namespace churn; do not edit applied migrations. Keep file relocation separate from model changes and preserve persisted IDs/enums.
6. Propagate cancellation; let established exception/result translation handle expected SQL conflicts. Do not leak provider exceptions/secrets, swallow cancellation, or wrap every error into success.

Use `roadguard-test-selection` for sufficient SQL Server tests. Require actual database evidence for uniqueness, rollback, rowversion and spatial behavior. Append persistence evidence and remaining gates to the assigned task completion history; do not claim endpoint completion solely because a repository test passed or a target ERD exists.
