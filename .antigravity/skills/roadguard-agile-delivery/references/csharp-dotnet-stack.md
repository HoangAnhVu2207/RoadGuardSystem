# RoadGuard C#/.NET stack contract

## Verify the local baseline

Read global.json, Directory.Build.props, actual project files and accepted ADRs. This checkout targets net8.0 with SDK 10.0.401 and latestPatch roll-forward per ADR 001. A skeleton is still an existing solution: newer documentation does not authorize retargeting. Match libraries/examples to local versions.

## Layer boundaries

- [ADR 001](../../../../docs/adr/001-backend-boundary.md) records actual references: API -> Services -> Repositories; Repositories -> BusinessObjects and DTOs; DTOs -> BusinessObjects. Preserve names and supported references.
- BusinessObjects owns entity-local invariants and fixed enums, with no API/Services/Repositories/DTOs/EF dependency. EF mapping an entity does not move domain methods into Repositories.
- Services owns use-case decisions, current project authorization, cross-aggregate checks and transactions. API binds/dispatches/maps HTTP; return DTOs, not EF entities.
- Repositories owns SQL Server/NetTopologySuite, mappings, migrations, repository interfaces and storage.

## Authentication and integrity

- Apply [ADR 002](../../../../docs/adr/002-authentication.md): per-request authoritative user/session checks; JWT role is a snapshot. Supervisor bypass needs current server role; non-Supervisors need active/effective matching ProjectMember. Logout/reset/suspend/global role changes revoke credentials; membership changes apply on the next request.
- Use UTC DateTimeOffset and DateOnly for calendar dates. Preserve Data Dictionary precision, checksums, enum numbers/Unknown = 0, and JSON ISJSON plus application schema validation.
- GPS uses geography(4326); engineering geometry uses configured UTM SRID. Options own limits/thresholds; fixture constants are not engineering standards.
- Mutable decisions use concurrency tokens. Stale updates return documented conflicts; insertion uniqueness errors need separate handling. Scoped retry keys/fingerprints reject changed-payload reuse.
- Commit domain/audit/outbox intent atomically where they share a database. Delivery may repeat; handlers deduplicate effects. Only the backend worker confirms required files/checksums/server quality checks.
- Original evidence, submitted measurements and published/approved versions are immutable; new content appends. General Evidence targets follow the dictionary; measurement evidence_file_id does not justify inventing a polymorphic FK.

## AI and research

Implement the adapter/job contract and deterministic fake now, validating project/input/model/version provenance. Python inference, training and GPU deployment are external. Test research import/pairing/metrics with controlled data, preserve uncertainty method where present, and verify no Defect/Warranty transitions. A fake does not establish empirical accuracy.

## Verification

Use existing xUnit/assertion libraries, WebApplicationFactory and SQL Server fixtures. EF InMemory cannot prove SQL/spatial/transaction behavior. Migrations need mapping/constraint tests and upgrade/recovery evidence; shared history stays unchanged.

For production changes run task-filtered and affected tests, then required solution gates:

    dotnet restore RoadGuardSystem.slnx
    dotnet build RoadGuardSystem.slnx --no-restore --no-incremental
    dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore
    dotnet test RoadGuardSystem.slnx --no-build

Record SQL/container environments and failures without printing secrets. Unavailable infrastructure is not a pass. Documentation/tooling changes use relevant verifier/script checks with an explanation of unaffected runtime suites.
