# ADR 006: V2 Endpoint Ownership And Persistence Coordination

## Status

Accepted (2026-09-27)

- **Date:** 2026-09-27
- **Owner:** Repository owner
- **Decision:** Adopt the `planning/V2` per-endpoint ownership model.
- **Approval evidence:** `Duyet spec SKILL-V2-ALIGN` in the owner session.
- **Supersedes:** Person-exclusive implementation ownership in ADR 001, ADR 002, ADR 003, and ADR 004 where those sections conflict with this decision.

## Context

The original plans divided implementation horizontally: Person 1 primarily owned API, Services, and DTOs while Person 2 primarily owned entities, EF Core, SQL Server, and migrations. The V2 plan instead assigns each OpenAPI operation to one owner who must be able to deliver and verify the complete endpoint independently.

Keeping both models active makes a V2 task ambiguous whenever it requires a repository write, entity adjustment, mapping, migration, or SQL-specific test. It also makes the project skills disagree with the task cards and encourages either unauthorized cross-task edits or incomplete endpoints.

## Decision

### 1. Vertical endpoint ownership

The owner named in a `planning/V2/Person_*/V2-*.md` task owns the complete approved endpoint slice:

`Controller -> IService -> IRepository`

That ownership includes the endpoint's directly required DTOs, service policy, repository interface and implementation, entity shape, mapping, migration, SQL test, API test, and HTTP example when the approved scope requires them.

Ownership does not make every layer mandatory. The task owner first compares the proposed contract with current source and changes only the smallest missing or incompatible slice. Existing valid implementation is reused and recorded as evidence.

### 2. Architecture remains N-layer

Vertical ownership is a delivery model, not a vertical-slice runtime architecture. ADR 001 and ADR 004 remain authoritative for project references and responsibilities. Controllers do not access repositories or `RoadGuardDbContext`; Services do not use EF Core or HTTP types; Repositories do not decide authorization, workflow policy, calculations, or HTTP errors.

### 3. Shared persistence coordination

The following are shared hotspots: `RoadGuardDbContext`, entity mappings, migration files and snapshot, project references, shared DI, shared error contracts, OpenAPI, seed data, Docker, and CI.

- A task scope card names every shared hotspot before editing.
- Only one active task writes a particular shared hotspot at a time.
- Concurrent schema tasks reserve and sequence migrations before either generates one.
- A task owner may create or edit a migration only when the approved task scope explicitly requires a schema change.
- Permission to draft a migration is not permission to apply it to a live database.
- Applied migrations are never rewritten to simplify a later task.

Person 2 coordinates migration order, SQL integration, release migration evidence, seed, Docker, and CI. Coordination does not transfer endpoint ownership or create an exclusive persistence boundary.

### 4. Contract and decision gates

V2 task files and `docs/diagram/V2/05_Technical/openapi.yaml` are proposed delivery contracts until source comparison and task approval. `NEEDS_REPO_CHECK` requires an explicit current-versus-proposed comparison. `BLOCKED_SLICE` permits only independently approved work and cannot be marked Done while its named gate remains open.

When sources conflict, use this precedence:

1. Current explicit owner request and approval.
2. `AGENTS.md` and applicable nested repository rules.
3. Accepted ADRs, with later accepted amendments winning.
4. Current source, migrations, tests, and runtime contract evidence.
5. The assigned V2 task and V2 OpenAPI draft.
6. Historical plans and worklogs as evidence only.

Compatibility, schema, authorization, workflow, error-code, pagination, idempotency, and concurrency deltas require approval in the endpoint scope. An agent must not resolve an open product gate by implementation guesswork.

### 5. Verification ownership

The endpoint owner supplies the complete evidence selected for the task's risk: changed-project build, fresh selected test binaries, focused or affected-project tests, SQL Server evidence where provider behavior matters, and an endpoint smoke call for endpoint work. Person 2 coordinates release-level SQL and migration evidence; this does not replace the endpoint owner's focused evidence.

## Consequences

- One owner can complete a V2 operation without a second implementation task for persistence.
- Shared database files require explicit reservation and sequencing.
- Task estimates may vary because an operation can include schema and SQL work.
- Historical task ownership remains evidence of completed work but does not control new V2 implementation.
- The three RoadGuard skills must route endpoint, persistence, and test concerns consistently with this ADR.

## Rejected Alternatives

### Keep horizontal person ownership

Rejected because it conflicts with every V2 task card's complete endpoint responsibility and can leave an endpoint partially delivered across owners.

### Allow unrestricted concurrent vertical ownership

Rejected because concurrent changes to DbContext, mappings, snapshots, shared DI, or OpenAPI create ordering and merge hazards. Vertical ownership therefore requires serialized shared hotspots.

### Replace N-layer with direct DbContext vertical slices

Rejected by ADR 004. Ownership and runtime architecture solve different problems.

## Compliance

- `AGENTS.md` is the canonical working rule and references this ADR.
- `.agents/rules/roadguard.md` routes work to the three project skills.
- `planning/V2/task_manifest.json` remains the operation-to-owner inventory.
- V2 task boilerplate states full endpoint ownership and shared-hotspot constraints.
- ADR 001-005 preserve their historical rationale and link to this superseding ownership decision where applicable.
