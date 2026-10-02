# RF-06A correction: trigger evidence and verification coverage

## 1. Kết quả và phạm vi

- **Status:** Done locally; docs/test-tooling correction. Owner/writer: current `anh` checkout; peer review of correction **NOT REQUESTED**. The owner's review findings are the input, not a claim of a completed peer review of these edits.
- All 20 observed triggers now have raw catalog definition, enabled/timing/events metadata, an observed-definition SHA-256, an ordered migration Up-operation trace, source file/line, a separate migration-SQL SHA-256 and an explicit text-comparison status. No trigger definition or migration mapping is UNKNOWN in this isolated database. All 20 comparisons are `OUTER_WHITESPACE_ONLY`; runtime trigger behavior is **NOT TESTED**.
- RF-06A counts did not change: 57 application tables, 517 columns, 116 FK constraints, 151 indexes, 130 checks, 20 triggers; `dbo.__EFMigrationsHistory` remains excluded. The current data baseline is sufficient for RF-07 to **read** project-road tables with the coverage limits below. RF-07 has not started.
- The original RF-06A report's statement that trigger bodies had already been inventoried was unsupported by its first artifact. [Original report](RF-06A.md) now points here; its earlier 2/2 result remains historical. This report records newly executed evidence only.

## 2. Baseline và quyền sửa

- Branch `anh`; HEAD before/after `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; no change commit. Working tree was dirty before and remains dirty after. Pre-existing RF-05 guidance/CI, survey source/tests, RF-06 fixture/tests, and RF-06A docs/tooling were preserved; no checkout, stash, reset or clean.
- Correction writer scope: `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf06aSchemaInventoryTests.cs`, `tests/Tooling/Write-Rf06aSchemaDocs.ps1`; `docs/backend/data/{current-schema.inventory.json,current-data-dictionary.md,current-triggers.md,README.md,current-differences.md}`; `planning/refactor/tasks/RF-06A-current-erd-data-dictionary.md`, `planning/refactor/reports/{RF-06A.md,RF-06A-correction.md}`. `current-erd.md` was mechanically regenerated from the same relationship inputs and rendered; no ERD relationship was added. Production model, migration chain, RF-06 SQL fixture, contract, FE lock and readiness files were read-only.
- Source fingerprint: local HEAD plus 368 SHA-256 entries for surveyed dirty BusinessObjects/Repositories `.cs` files. The inventory additionally hashes the current extractor, generator and owned SQL fixture (3 tooling hashes); it never hashes itself. Inventory SHA-256 after regeneration is `D9A07D493B3B7865F7BB541719EC829E3780FDDE2826AE7D6805B18A97FEF751`, identical across separate owned-container runs with unchanged inputs.
- START: active RF-05 manifest and RF-06A task/report checked; owner correction scope supplied. INTEGRATE: four focused tests, catalog/migration trace and docs checks pass locally. RELEASE: hosted CI, deployed SQL and runtime trigger behavior **NOT RUN**; no publication or RF-07 execution authorized.

## 3. Thay đổi và đối chiếu yêu cầu

| Finding / impact | Before | Correction and source | Status |
| --- | --- | --- | --- |
| RF06AC-01 / high, trigger evidence | Original `sql.triggers` had schema/table/name/disabled only; report said body was inventoried. | Extractor reads `sys.sql_modules.definition`, `is_instead_of_trigger`, replication flag and `sys.trigger_events`; raw definition stays under `sql.triggers`. Separate UTF-8/LF hashes and rule appear in `triggerEvidence` and [trigger page](../../../docs/backend/data/current-triggers.md). | `CURRENT_VERIFIED` on disposable migrated SQL; 20/20 definitions present. |
| RF06AC-02 / high, provenance | Dictionary told reader to inspect migration SQL without identifying the source. | Walks `IMigrationsAssembly.Migrations` and each migration's ordered `UpOperations` (`SqlOperation`), tracks CREATE/ALTER/DROP/recreate, records first active CREATE and latest definition migration, source path/line and history. Dictionary links trigger page and migration. | `CURRENT_VERIFIED` source trace 20/20; 0 unresolved. No semantic SQL equivalence claimed. |
| RF06AC-03 / medium, verifier | Baseline compared index/check mainly by name and did not compare trigger text. | Verifier requires every active migration trigger in catalog, no duplicate/missing definition/source, and distinguishes exact LF, outer-whitespace-only and changed content. Negative fixtures exercise missing definition, changed definition, unresolved source, duplicate and CREATE/ALTER/DROP/recreate. | 4/4 focused tests pass; 20/20 catalog/latest-migration text comparisons are `OUTER_WHITESPACE_ONLY`. |
| RF06AC-04 / medium, claim calibration | Original report could be read as broader schema agreement. | [Coverage matrix](../../../docs/backend/data/README.md#verification-coverage) and [difference register](../../../docs/backend/data/current-differences.md) label extracted, compared and NOT COMPARED separately. Column business meaning remains UNKNOWN without a sourced decision. | Docs corrected; RF-09 receives remaining comparison work. |
| RF06AC-05 / low, reproducibility | Inventory hashed surveyed production sources, but not the generator/extractor that produced it. | Inventory records SHA-256 of both tools and RF-06 fixture; generator rejects tool-hash or catalog-definition-hash mismatch before writing docs. | Two independent runs yielded the same inventory SHA-256; `-Check` passes. |

### Verification coverage

| Component | Extracted | Compared | Still NOT COMPARED / UNKNOWN |
| --- | --- | --- | --- |
| Tables/columns/type/nullability | EF model, snapshot, SQL catalog | 57/517 names; configured type/nullability model↔snapshot; base type/nullability model↔SQL | SQL type facets, deployed rows/schema |
| PK/alternate keys | Ordered SQL keys | No cross-source shape comparison | Key order/alternate-key semantic parity |
| FK | Model/snapshot and SQL columns, principal, delete action | 116 names and child/principal mapping | Delete action/disabled-state equality |
| Index | Catalog key order/include/unique/filter; model/snapshot names | 151 names | Index shape/filter equality |
| Check | Catalog expression/disabled; model/snapshot names | 130 names | Expression/state equality |
| Default/computed/value generation | Catalog and model metadata | No cross-source parity beyond column comparison | Expression/generation equality |
| Trigger | 20 raw definitions, state, timing/events, migration Up-operation SQL/source | Active names complete; 20/20 latest-source/catalog text differs in outer whitespace only after LF conversion | Runtime firing/side effects, semantic equivalence, deployed state |

- SQL Server stores a definition string with different outer whitespace than the EF migration `SqlOperation.Sql` for all 20 triggers. Both untrimmed LF-normalized hashes are retained and therefore differ. The comparison trims outer whitespace **only to classify that formatting difference**; it never modifies the raw catalog definition or interprets SQL semantics.
- No production data, public contract or consumer effect. Recovery is limited to RF-06A correction docs/test-tooling paths after inspecting the dirty worktree; the test fixture disposes only its owned GUID database and container.

## 4. Self-review và autofix

- Pass 1, evidence correctness: reread migration Up/Down SQL for representative append-only, scope and integrity triggers; checked 20 catalog definitions and 20 source path/line references, active-name completeness, latest-definition traversal, source/catalog hash separation, negative verifier reasons, stable generation and absence of blank definition fallback. The tool keeps raw SQL; only CRLF/CR→LF is used for hashing. All 20 comparisons remain text-level, not a runtime assertion.
- Pass 2, scope/authority: inspected corrected docs and task/report checkpoints, preserved the original RF-06A 2/2 history, distinguished source-interpreted descriptions from accepted business requirements, verified no production/schema/contract/readiness/RF-06 fixture edits, shared DB write, commit/push/merge or RF-07 start. No cross-owner coordination note was needed; peer review of this correction was NOT REQUESTED.

| Self-review finding | Severity | Correction | Recheck |
| --- | --- | --- | --- |
| RF06AC-R1 | medium | Direct byte comparison after LF normalization failed for all 20 because catalog text contains outer whitespace. Kept raw strings and distinct hashes; classified `OUTER_WHITESPACE_ONLY` only after checking full interior text. | SQL 4/4, text classifications 20/20, changed-body negative fixture. |
| RF06AC-R2 | medium | A trace that discarded history on DROP would hide an earlier CREATE/ALTER. Retained ordered operation history while resetting the active creation on recreate. | Timeline fixture CREATE→ALTER→DROP→CREATE passes. |
| RF06AC-R3 | low | Generator could accept an edited inventory definition after extraction. Added definition-hash and tooling-hash guards. | Generator regeneration and `-Check` pass with current fingerprint. |

- Self-review result **PASS within correction scope**. SQL trigger runtime semantics, deployed schema and the NOT COMPARED dimensions remain explicitly unverified.

## 5. Kiểm chứng

| Command/check | Environment and result | Limits |
| --- | --- | --- |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --nologo -v q -clp:ErrorsOnly --filter FullyQualifiedName~Rf06aSchemaInventoryTests` | Fresh build from current test source; **4 executed / 4 passed / 0 failed / 0 skipped**. Owned Testcontainers SQL Server 2019-CU18 applied 37 migrations and queried `sys.*`. | Only focused extractor/verifier; no production DB or application seeder. |
| Same test filter with `--no-build` after fresh build, unchanged source, separate process/container | **4/4 passed**, 0 failed/skipped; inventory SHA-256 unchanged at `D9A07D493B3B7865F7BB541719EC829E3780FDDE2826AE7D6805B18A97FEF751`. | Repeatability of this focused slice, not full IntegrationTests suite. |
| `pwsh -NoProfile -File tests/Tooling/Write-Rf06aSchemaDocs.ps1` then `-Check` | Generator/guard passes; 20 trigger links and 20 source entries, counts remain 57/517/116/151/130/20. | Business definitions are not validated by generation. |
| Mermaid CLI render of `current-erd.md` into temporary directory | **8 charts rendered**; relationship inputs unchanged. | Not a hosted docs preview. |
| Trigger source-location and link/count audit from inventory/Markdown | 20 source files/lines resolve; 20 definitions and mappings; 0 UNKNOWN; no prior schema objects lost. | Does not exercise trigger firing or SQL semantics. |
| `git diff --check` and final path/status review | Exit 0. Only pre-existing LF/CRLF advisories for CI/PowerShell files; no RF-06A correction whitespace error. New files were read back and generated docs checked directly. | Git diff alone does not include untracked files. |

- Intermediate compile/text-comparison failures were correction-tool defects, resolved before the fresh passing run. RF-06 API 66/66, RF-06 SQL 19/19 and original RF-06A 2/2 are **historical evidence, not rerun here**. Full API suite, external storage/AI, hosted CI, FE lock and deployed SQL are **NOT RUN** for this docs/test-tooling correction.

## 6. Phối hợp và quyết định

- No new business or public-contract decision was needed to correct evidence. No shared production writer or cross-owner coordination note was required. RF-09 should prioritize the NOT COMPARED columns of the matrix for any approved schema transition and test trigger behavior on an isolated migration rehearsal. RF-10 module owners must source business meaning separately; column/trigger names and THROW messages are technical observations, not policy approval.
- Deployed migration history/trigger state requires separately authorized environment evidence. Until then it is UNKNOWN and does not block RF-07's read-only current-data inspection.

## 7. Checkpoint và bàn giao

- Completed: raw trigger catalog definitions and hashes, ordered migration provenance, negative verifier and lifecycle fixture, generated trigger reference, dictionary links, coverage matrix, original-report/task checkpoint, two-pass review and focused checks. Next exact step: RF-07 owner reads [project-road ERD/dictionary](../../../docs/backend/data/README.md) and RF-06 characterization before scoping its read-only pilot; RF-09 later takes the matrix and trigger page for an approved transition plan. RF-07 has **not** begun.
- Remaining limits: key/index/check/default/delete-action cross-source equality, trigger runtime effects and deployed schema are not verified. No finding is marked Accepted as a business decision. Keep current-side baseline and future target ERD distinct.
- Recovery: remove only correction-owned documentation/tooling edits after a path review; fixture cleanup is limited to the test-created DB/container. Existing uncommitted changes must be preserved. No production rollback applies.
- **Tóm tắt gửi người lập kế hoạch:** RF-06A correction Done locally on `anh` HEAD `2efc8a5` plus 368 dirty-source and 3 tooling hashes. Review's trigger-body claim is fixed: 20/20 catalog definitions and migration source/line mappings are now present; 20/20 differ from latest migration SQL only by outer whitespace after LF conversion, with separate raw-text hashes. Four focused tests pass twice; inventory and generated docs are stable; 8 ERDs render. Counts remain 57 tables/517 columns/116 FK/151 indexes/130 checks/20 triggers. Index shape/filter, check expression, FK delete action, PK/alternate-key parity, default/computed/generation parity, trigger runtime behavior and deployed SQL remain unverified. RF-07 may read the current project-road map within these limits; RF-09 owns future schema comparison/transition. No production/schema/contract change, shared DB write, commit/push/merge or RF-07 execution occurred.
