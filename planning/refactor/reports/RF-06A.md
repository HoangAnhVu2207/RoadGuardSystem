# RF-06A: Current ERD and data dictionary

**Correction note (2026-10-01):** The original report below says trigger bodies were inventoried, but the original inventory held only trigger name/table/disabled. [RF-06A-correction](RF-06A-correction.md) records newly executed extraction, migration trace, hashes, comparison limits and updated verification coverage. The original 2/2 evidence remains historical and is not relabeled as a correction run.

## 1. Kết quả và phạm vi

- **Task/status:** RF-06A **Done locally**, docs/tooling baseline; delivery ready for RF-07 reading. Current writer: this `anh` checkout. Peer reviewer **NOT REQUESTED**. RF-07 implementation has **not** started.
- Isolated migration/catalog comparison passed. Current application schema has **57 tables, 517 columns, 116 enforced FK constraints, 151 indexes, 130 check constraints and 20 active triggers**. `dbo.__EFMigrationsHistory` is the one excluded EF infrastructure table. There are 0 user-defined views in the isolated catalog. The inventory includes 368 input-file SHA-256 hashes; it captures dirty source contents, not merely HEAD.
- Output: [data index](../../../docs/backend/data/README.md), [ERD](../../../docs/backend/data/current-erd.md), [dictionary](../../../docs/backend/data/current-data-dictionary.md), [machine inventory](../../../docs/backend/data/current-schema.inventory.json), [persistence notes](../../../docs/backend/data/current-persistence-notes.md), [difference register](../../../docs/backend/data/current-differences.md), task/dependency updates and test-only extractor/generator.
- This is a current-checkout / disposable SQL baseline. Deployed schema, actual row populations, external consumers and business acceptance remain **UNKNOWN**. No production behavior, public contract, readiness registry, entity, mapping, migration or schema was changed.

## 2. Baseline và quyền sửa

- Branch `anh`, local HEAD before/after `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; no change commit. Working tree dirty before and after. Pre-existing RF-05 activation, survey source/tests, RF-06 fixtures/tests, V2 planning and untracked docs/contracts were preserved. No checkout, stash, reset or clean. The 2026-10-01 local time rollover does not alter the source fingerprint.
- RF-06A writer scope: `docs/backend/data/**`, `docs/backend/README.md`; `planning/refactor/{README.md,03-master-plan.md,tasks/RF-06A-current-erd-data-dictionary.md,tasks/RF-06-isolated-characterization.md,tasks/RF-07-work-package-pilot.md,tasks/RF-09-transition-mechanics.md,reports/RF-06A.md}`; `tests/RoadGuardSystem.IntegrationTests/Infrastructure/Rf06aSchemaInventoryTests.cs`; `tests/Tooling/Write-Rf06aSchemaDocs.ps1`. The one RF-06 edit corrects wording about inherited SQL settings; historical results stay intact.
- Read-only sources: `RoadGuardDbContext`, BusinessObjects entities, EF configurations, 37 migrations plus snapshot, RF-06 fixture and report, active RF-05 manifest/rules, RF-03 plan, RF-02 gaps/decisions. No shared production/config/CI/contract file was written by RF-06A.
- START: user RF-06A scope and active RF-05 manifest verified. INTEGRATE: current model/snapshot/owned migrated SQL plus generated docs validated. RELEASE: hosted CI/publication **NOT RUN** and no authority granted to start RF-07, commit or push. RF-07 consumes project-road map; RF-09 consumes schema baseline for a later target/migration plan. No second writer was assigned to these files.

## 3. Thay đổi và đối chiếu yêu cầu

| ID / impact | Current evidence and source | Target/authority and disposition |
| --- | --- | --- |
| RF06A-S01 / schema baseline | `Rf06aSchemaInventoryTests` compared EF design model, `RoadGuardDbContextModelSnapshot` and SQL catalog after all 37 migrations in a fixture-owned container. 57/517 table/column names, SQL type/nullability, 116 FK names and mappings, 151 index names, 130 check names matched. Inventory sections retain all three sources. | `CURRENT_VERIFIED` only for this checkout and isolated SQL. No unexplained difference in compared dimensions; index shape/filter, check expression normalization, FK delete action and trigger body were inventoried but not fully equality-asserted. See [difference register](../../../docs/backend/data/current-differences.md). |
| RF06A-S02 / migration SQL | `migrationBuilder.Sql` source and isolated `sys.triggers` show 20 enabled triggers; no user-defined views. `dbo.__EFMigrationsHistory` excluded with reason. | `CURRENT_VERIFIED` source difference from EF metadata, not drift. RF-09 should preserve/test trigger semantics during approved migrations. |
| RF06A-T01 / upload high | `StoredFile.SizeBytes` is CLR/SQL `int`; `UploadSession.ExpectedSizeBytes` is `long`/`bigint`; `UploadService.ValidateCreate` restricts to `int.MaxValue`. SQL check only enforces nonnegative stored size. | Decision 38 is `TARGET_CONFIRMED` for 8 GiB pilot, implementation gap CG17. RF-09 plans compatible transition; RF-10-04 implements only with data/consumer approval. No target schema was inserted into current ERD. |
| RF06A-T02 / spatial medium | Catalog has four spatial columns. RoadSectionVersion `geometry` has SRID 32648/32649 check; GroundTruth `geography` has SRID 4326 check. Defect/AIDetection `geometry` lack cataloged SRID check. | `CURRENT_VERIFIED` constraint scope; broader unit/SRID policy `UNKNOWN`. RF-10-02/06 need business/source confirmation before RF-09 plans any check. |
| RF06A-T03 / ownership high | Survey old/V2 paths share SurveyPlan/SurveyRequest physical tables; project actor scope also uses ProjectMembers and application guard. FK alone does not establish workflow ownership/authorization. | RF-02 CG04/Q-RF02-03 and RF-07/10-03 must characterize/decide behavior. No application edge is fabricated in ERD. |
| RF06A-U01 / deployed SQL | No deployed DB, migration history or row data was inspected. | `UNKNOWN`. RF-09 needs separately authorized copy/audit before a live schema/data transition. |

- Generated files are deterministic from the machine inventory; purpose labels are source-interpreted from entity/configuration, not Accepted business rules. All 57 table headings link to actual entity/configuration paths. Column business semantics remain `UNKNOWN` where no authoritative source was established.
- Contract/data/consumer effect: **none**. The extractor uses only a disposable SQL container and GUID database, applies existing migrations, reads `sys.*`, writes documentation metadata and disposes resources. Recovery is removal of RF-06A-only docs/tooling/checkpoints after path review; no production rollback applies.

## 4. Self-review và autofix

- Correctness pass: checked SQL catalog counts against generated dictionary and inventory, source links, SQL-backed FK edges/optional relationships, exact schema/table labels, filtered unique index treatment, spatial SRID and file-size distinctions, model/snapshot/catalog authority, and source-hash coverage. Mermaid CLI rendered all eight charts; overview and survey diagrams were visually inspected. No relationship was inferred from a `...Id` name.
- Coordination/scope pass: verified RF-06 pre-existing edits were preserved, report labels do not turn historical/V2 docs into target authority, RF-07/09 dependency is acyclic, and production/schema/contracts/readiness files were untouched by this slice. Peer review **NOT REQUESTED**; live producer/consumer integration **NOT RUN**.

| Finding | Severity | Evidence and correction | Recheck |
| --- | --- | --- | --- |
| RF06A-R1 | medium | Initial generator emitted literal PowerShell expressions in Markdown and placed ERD optionality on the wrong side. Corrected interpolation/cardinality; exact `dbo.Table` labels added. | 8/8 Mermaid renders, manual diagram inspection, regenerated docs check. |
| RF06A-R2 | medium | Runtime EF model omits check-constraint metadata. Used `IDesignTimeModel` for model-side extraction instead of treating this as schema drift. | Focused SQL comparison 2/2 PASS. |
| RF06A-R3 | medium | Initial extractor wrote JSON before assertions. Moved write after all shape/type/check comparisons so a failing run cannot label a new inventory verified. | Focused SQL comparison 2/2 PASS; stable SHA-256 across separate containers. |
| RF06A-R4 | low | Enum `Unknown` in `UserRoleCode` converter throws, while other enum underlying values may still be barred by SQL check. Recorded `REJECTED_BY_CONVERTER` and clarified the distinction in dictionary. | Focused SQL comparison 2/2 PASS; dictionary regeneration check. |

- Self-review **PASS within RF-06A scope**. The remaining semantic comparison limits are explicit RF-09 inputs, not unreported green claims.

## 5. Kiểm chứng

| Command/check | Diff/environment | Result and count | Limit |
| --- | --- | --- | --- |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --nologo -v q -clp:ErrorsOnly --filter FullyQualifiedName~Rf06aSchemaInventoryTests` | Current dirty source, .NET 8, EF SQL Server 8.0.17, Docker 29.6.1, owned SQL image `2019-CU18-ubuntu-20.04`; fresh build | **PASS 2/2**, 0 fail/skip; 37 migrations applied and catalog compared. Negative fixture detects missing table/column and wrong FK. | No application data seed or deployed DB. Build produced no errors; warning totals were not used as a gate. |
| Same filter with `--no-build` in separate process after fresh build | New owned SQL container/database | **PASS 2/2**, 0 fail/skip; inventory SHA-256 unchanged: `DAD4B137AFA6F7CC706B983D7254315CF1C134B6C945F22F0134E05829B12277`. | Reuses binaries only while source unchanged. |
| `pwsh -NoProfile -File tests/Tooling/Write-Rf06aSchemaDocs.ps1 -Check` | Current inventory and generated docs | **PASS**: 57/517/116 coverage; generated content byte-stable. | Does not prove business meaning. |
| `npx --yes @mermaid-js/mermaid-cli -i docs/backend/data/current-erd.md -o <temp>.md -a <temp-assets> -j 2` | CLI 12.0.0, temp output outside repo | **PASS**, eight charts parsed/rendered; overview and survey PNG inspected. | Visual inspection is local, not a hosted docs preview. |
| Source-link and object-row audit using PowerShell `ConvertFrom-Json`, regex and `Test-Path` | Current docs/inventory | **PASS**: 57 headings, 116 FK rows, 151 index rows, 130 check rows, 20 trigger rows; 114 entity/config links, 0 broken. | Other repository-wide links not audited. |
| `git diff --check`, `git status --short --branch` | Dirty checkout | Exit 0 for diff check; only pre-existing LF/CRLF advisories from CI/PowerShell paths. | Git does not include untracked RF-06A docs in that diff check; generator/readback and link checks cover generated files. |

- Earlier RF-06 API 66/66 and SQL 19/19 are historical checkpoint evidence, **not rerun** here. Full API/integration suites, external storage/AI, hosted CI, FE lock and production SQL were **NOT RUN** because RF-06A changed docs/test tooling only. RF-00 FE `CONTRACT_LOCK_MISMATCH` remains a separate baseline issue. Intermediate extractor compile/runtime/generator failures were RF-06A tooling defects and were fixed; they are not reported as pre-existing product failures.

## 6. Phối hợp và quyết định

- No coordination note was needed: this slice changed no shared production file or RF-06 fixture. RF-07 reads this baseline after handoff; RF-09 takes RF06A-T01/T02/U01 into its transition plan. Proposed task ownership does not assign a new writer or approve migration.
- **No new business decision blocks RF-06A.** For later tasks: RF-10-02/06 need authoritative spatial units/SRID for Defect/AIDetection before adding a DB constraint; RF-09 needs authorization and a deployed-data copy/audit before schema migration planning becomes executable. These are future gates, not Accepted decisions here.

## 7. Checkpoint và bàn giao

- Complete: RF-06A task/dependencies, source-hashed inventory, current ERD/dictionary, notes/difference register, negative verifier fixture, SQL comparison, deterministic generation, Mermaid render and two-pass self-review. Next exact step: RF-07 owner reads [project-road current map](../../../docs/backend/data/README.md) and RF-06 HTTP/SQL baseline, then scopes the read-only work-package pilot; RF-09 later uses RF-06A as current-side data input. RF-07 was not started in this task.
- Release/integration limits: deployed SQL may differ; check expression/index/FK delete/trigger body semantics are documented but not fully automated equality comparisons. Keep consumer/data approvals for RF-09/10. No commit, push or merge; no shared DB write; no production/schema/contract change.
- Recovery: remove only RF-06A paths listed in section 2 after inspecting the dirty worktree. Testcontainers teardown owns only its generated database/container. No other task's dirty changes may be discarded.
- **Tóm tắt gửi người lập kế hoạch:** RF-06A Done locally on branch `anh`, HEAD `2efc8a5` plus 368 hashed dirty-source inputs. A fresh owned SQL Server applied 37 migrations; model/snapshot/catalog checks and a negative comparator test pass 2/2. Current map covers 57 tables, 517 columns, 116 FK, 151 indexes, 130 checks and 20 triggers; one EF history table excluded. Eight diagrams render, dictionary/source links and deterministic regeneration pass. Important follow-ups are CG17 file-size mismatch, spatial DB-check scope, shared old/V2 survey rows, and unknown deployed schema. No current-schema drift was found in compared dimensions; no business/contract/schema change was made. RF-07 can consume the project-road baseline; RF-09 must handle target ERD/migration/data and deployed-state proof. Hosted CI and production DB remain unverified; no commit/push/merge or RF-07 execution occurred.

## Correction checkpoint (append-only)

- See [RF-06A-correction.md](RF-06A-correction.md) for the separately executed 2026-10-01 correction. The original claim that trigger body was already captured is withdrawn for the first inventory. The new inventory includes all 20 raw SQL definitions, separate catalog/migration hashes, source migration/line and timing/events. All 20 text comparisons are `OUTER_WHITESPACE_ONLY`; runtime behavior remains NOT TESTED.
- The original 2/2 test count, inventory hash and review findings above describe the pre-correction artifact only. New 4/4 results and verification matrix are in the correction report. No production/schema/contract edit, shared DB write, commit/push/merge or RF-07 execution was introduced by the correction.
