# Current data baseline (RF-06A)

**CURRENT_VERIFIED for this checkout only:** local branch `anh`, HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`, with dirty source. The [inventory](current-schema.inventory.json) includes SHA-256 hashes for every surveyed BusinessObjects/Repositories `.cs` input, so HEAD alone is not its fingerprint. EF Core SQL Server 8.0.17 and Testcontainers SQL Server 2019-CU18 applied all 37 migrations to a fresh, owned database, ending at `20260929125522_P2ValidationMeasurementProvenance`. The SQL catalog was read after migration and the database/container disposed. This does **not** establish deployed or shared-environment schema.

| Module | ERD | Dictionary | Source route |
| --- | --- | --- | --- |
| Identity | [diagram](current-erd.md#identity) | [tables](current-data-dictionary.md#identity) | `RoadGuardDbContext` identity sets; `Configurations/*Identity*`, session/invitation mappings |
| Project/road | [diagram](current-erd.md#project-road) | [tables](current-data-dictionary.md#project-road) | Project/road/warranty configurations and entities |
| Survey | [diagram](current-erd.md#survey) | [tables](current-data-dictionary.md#survey) | Survey configurations and entities; old/V2 persistence services |
| File/upload | [diagram](current-erd.md#files) | [tables](current-data-dictionary.md#files) | StoredFile/Upload configurations and entities |
| Processing | [diagram](current-erd.md#processing) | [tables](current-data-dictionary.md#processing) | Processing/AI model configurations and entities |
| Defect/inspection | [diagram](current-erd.md#defect-inspection) | [tables](current-data-dictionary.md#defect-inspection) | Defect and inspection configurations/entities |
| Messaging | [diagram](current-erd.md#messaging) | [tables](current-data-dictionary.md#messaging) | Audit/idempotency/outbox/notification configurations |

The [ERD](current-erd.md) shows enforced SQL FKs only; [dictionary](current-data-dictionary.md) contains all observed application tables/columns, ordered keys, FKs, indexes, filters and checks. [Trigger definitions](current-triggers.md) preserve raw catalog SQL and migration provenance. [Persistence notes](current-persistence-notes.md) distinguish SQL constraints from application policy and transaction code. [Difference register](current-differences.md) separates schema drift from target gaps. The [RF-06A correction report](../../../planning/refactor/reports/RF-06A-correction.md) supersedes the original report's trigger-body claim.

## Evidence and exclusions

- `CURRENT_VERIFIED`: EF design-time model, migration snapshot, and actual `sys.*` catalog in the owned disposable database; the inventory stores these separately. SQL catalog is authoritative only for that migrated database. The extractor verifies the applied migration list and the specific comparisons below, not semantic equivalence of the whole schema.
- `TARGET_CONFIRMED`: only requirements explicitly linked from [confirmed decisions](../../product/confirmed-decisions.md) or owner messages. This page does not turn those requirements into schema acceptance.
- `PROPOSED`: module ownership and future schema/contract transitions in RF-03/RF-09. `UNKNOWN`: deployed migration history/rows, outside-repo consumers, field meanings lacking an authoritative product source.
- `dbo.__EFMigrationsHistory` is EF infrastructure, excluded from application table/column totals. No application views, keyless or owned tables were observed; verify anew after mapping changes. Migration SQL creates 20 active triggers, which are cataloged separately from EF metadata. No production database was queried.

## Verification coverage

| Component | Extracted | Compared sources | Not verified |
| --- | --- | --- | --- |
| Tables/columns, SQL type/nullability | EF design model, snapshot, isolated SQL catalog; 57 tables/517 columns | Names across all three; model/snapshot configured type and nullability; model/catalog base type and nullability (`rowversion`/`timestamp` alias) | Full SQL type facets, row data and deployed schema |
| PK/alternate keys | Ordered SQL `sys.key_constraints` rows | **NOT COMPARED** with EF/snapshot key shape | Key order/alternate-key semantic parity |
| FK columns/principal/delete action | EF/snapshot FK metadata; ordered catalog mapping and delete action; 116 constraints | Names and child/principal columns across model/snapshot/catalog | Delete-action equality and disabled-state parity **NOT COMPARED** |
| Index keys/order/include/unique/filter | Catalog rows and model/snapshot index names; 151 indexes | Index names across model/snapshot/catalog | Key order, include, uniqueness and filter equality **NOT COMPARED** |
| Check expression/state | Catalog SQL definition/state and EF/snapshot names; 130 checks | Check names across model/snapshot/catalog | SQL expression equivalence and disabled-state parity **NOT COMPARED** |
| Default/computed/value generation | Catalog default/computed/identity and EF model/snapshot metadata | **NOT COMPARED** beyond column name/type/nullability | Expression and generation parity |
| Trigger definition/source/state | 20 raw catalog definitions, SHA-256, timing/events/enabled state, ordered EF migration Up-operations and source path/line | Active trigger names complete across migration operations/catalog; latest migration SQL versus catalog text: 20/20 `OUTER_WHITESPACE_ONLY` after LF conversion | Runtime firing/side effects and semantic equivalence **NOT TESTED**; deployed trigger state **UNKNOWN** |

Raw catalog definition text is retained as returned by the SQL client, without trimming. Its SHA-256 is computed over UTF-8 after CRLF/CR to LF conversion, without trimming or rewriting SQL. The migration SQL has a separate hash. `OUTER_WHITESPACE_ONLY` means the two LF-normalized strings become equal after trimming whitespace **outside** the statement; it is a text observation, not runtime proof. The [inventory](current-schema.inventory.json) also hashes the extractor, generator and SQL fixture, independently of its 368 production-source hashes. The inventory never hashes itself.

## Reproduce and update

1. From this checkout, with Docker available, run `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --nologo -v q -clp:ErrorsOnly --filter FullyQualifiedName~Rf06aSchemaInventoryTests`. The test ignores inherited shared SQL settings, starts its own Testcontainers SQL Server and GUID database, applies existing migrations, extracts model/snapshot/catalog, writes inventory and disposes its resources. It does not seed application data.
2. Run `pwsh -NoProfile -File tests/Tooling/Write-Rf06aSchemaDocs.ps1` to regenerate dictionary and diagrams; then rerun with `-Check` for byte-stable output. Inspect the source-hash and schema diff before accepting regeneration. Do not change model/snapshot/SQL facts merely to get a green check.
3. Render the eight Mermaid charts with `npx --yes @mermaid-js/mermaid-cli -i docs/backend/data/current-erd.md -o <temporary-output>.md -a <temporary-assets-directory>`. Render output stays outside the repository. Check diagram labels/cardinality and [difference register](current-differences.md) manually.
4. For a future persistence task, update the module mapping/purpose in `Write-Rf06aSchemaDocs.ps1` when tables are added/removed, rerun isolated migration/catalog comparison, review generated docs and source hashes, then record the new branch/commit/dirty fingerprint and findings. RF-07 reads project-road tables and persistence notes as **current baseline**. RF-09 uses them as input for a separately approved target ERD and migration/backfill plan.
