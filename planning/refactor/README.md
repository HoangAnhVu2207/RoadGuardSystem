# RoadGuardSystem refactor program — survey index

**Current RF-11 checkpoint (2026-10-02):** one writer performs repository cleanup on `anh`, without commit, merge, push or actions on `main`, per latest owner instruction. See [RF-11 report](reports/RF-11.md) and [branch handoff](11-handoff-baseline.md). Two historical evidence packages now reside in [the archive](../../docs/history/refactor/README.md). Common branch baseline and release remain pending; no Anh/Huy feature package is started. The earlier checkpoint below is historical.

**Current checkpoint (2026-10-01):** Anh alone executes refactor on `anh`; Huy is not assigned yet. See [R/C/F/G ledger](10-refactor-slices.md), [survey comparison](10-survey-coexistence-baseline.md), [refactor completion checklist](10-refactor-checklist.md), [future development draft](11-development-plan.md), [handoff baseline](11-handoff-baseline.md), and [post-RF-09 report](reports/RF-10-refactor.md). Historical two-developer tables are proposals for a later phase; future identities are Huy/A and Anh/B. The local refactor is Partial, not an authorization to implement F/G slices.

- **Survey timestamp:** 2026-09-30T13:47:13+07:00 (local machine time, Asia/Bangkok context)
- **Branch:** `anh`
- **Base commit surveyed:** `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`.
- **Working tree:** dirty before this report; pre-existing changes are listed in [RF-00-baseline.md](RF-00-baseline.md). This survey does not modify or attribute them.
- **Initial survey scope:** RF-00 through RF-04, documentation and planning only. Later execution checkpoints appear below; this line describes the original survey.
- **Initial survey exclusions:** production code, database schema/migrations, active contracts, CI, package/framework upgrades, deletion/renaming of old docs, and shared environment data. Later RF-05/06/06A changes are recorded by their own reports.

## Evidence labels

- CURRENT_VERIFIED: directly observed in source, project files, tests, Git, or a command recorded in the reports.
- TARGET_CONFIRMED: stated by the owner request or an existing product document, without implying implementation.
- PROPOSAL: a reversible planning recommendation requiring owner acceptance before adoption.
- UNKNOWN: requires a product, operational, or repository decision/check.
- Historical documents and completed rows are evidence only; they are not treated as proof of current correctness.

## Reports

RF-00 verified outputs: [00-baseline.md](00-baseline.md),
[00-tooling-dependencies.md](00-tooling-dependencies.md), and
[reports/RF-00.md](reports/RF-00.md).
The earlier [RF-00-baseline.md](RF-00-baseline.md) is a preliminary survey.

RF-01 source inventory: [01-code-map.md](01-code-map.md),
[01-endpoint-inventory.md](01-endpoint-inventory.md),
[01-legacy-register.md](01-legacy-register.md), and
[reports/RF-01.md](reports/RF-01.md).
The earlier [RF-01-module-refactor-plan.md](RF-01-module-refactor-plan.md) is a preliminary plan.

RF-02 reconciliation: [02-requirements-traceability.md](02-requirements-traceability.md),
[02-contract-gaps.md](02-contract-gaps.md),
[02-decision-register.md](02-decision-register.md),
[02-adr-transition.md](02-adr-transition.md), and
[reports/RF-02.md](reports/RF-02.md).
The RF-02 decision register includes the owner's 2026-09-30 reconfirmation of the full 32-44 table. Requirements are Accepted; implementation and test status remain separate.

RF-03 implementation plan: [03-target-architecture.md](03-target-architecture.md),
[03-documentation-design.md](03-documentation-design.md),
[03-agent-design.md](03-agent-design.md),
[03-master-plan.md](03-master-plan.md),
[tasks/](tasks/), and [reports/RF-03.md](reports/RF-03.md).
These were proposals at RF-03. The owner accepted the RF-05 activation package on 2026-09-30; `.agents/manifest.json` now records `ACTIVE_AFTER_OWNER_ACCEPTANCE`. RF-06 and RF-06A each report their own implementation and verification status.

RF-04 draft package: [report](reports/RF-04.md), [bidirectional operation crosswalk](04-operation-crosswalk.md) and [machine JSON](04-operation-crosswalk.json), [historical source map](04-source-crosswalk.md) and [machine JSON](04-source-crosswalk.json), [two-developer slices](04-delivery-slices.md), [coordination notes](coordination/), [product draft](../../docs/product/README.md), [backend draft](../../docs/backend/README.md), [contract draft](../../contracts/README.md), and [decision transition](../../docs/decisions/README.md). The active V2 OpenAPI, FE lock, Postman, CI and old guides remain unchanged.

RF-04 correction: [seven-section report](reports/RF-04-correction.md), exact [confirmed 32-44 wording](../../docs/product/confirmed-decisions.md), transferred [FR/BR](../../docs/product/historical-fr-br.md), [US](../../docs/product/historical-us.md), [PF](../../docs/product/historical-pf.md), [NFR](../../docs/product/historical-nfr.md), corrected owner map in `tools/rf04_ownership.py`, and regression checks in `tools/test_rf04.py`. RF-04 is Done for inactive draft content, pending RF-05 preparation and user acceptance before new guidance is activated.

RF-06A current data baseline: [task](tasks/RF-06A-current-erd-data-dictionary.md), [data index](../../docs/backend/data/README.md), [report](reports/RF-06A.md). This supplements RF-06 before RF-07; RF-09 reads it for target-schema transition planning. Its schema evidence is from a disposable isolated SQL Server, not a deployed database.

1. [RF-00-baseline.md](RF-00-baseline.md) — preliminary inventory before RF-00 verification.
2. [RF-01-module-refactor-plan.md](RF-01-module-refactor-plan.md) — module assessment and staged refactor plan.
3. [RF-02-documentation-contracts-adr-plan.md](RF-02-documentation-contracts-adr-plan.md) — replacement documentation system, contracts, ADR and planning design.
4. [RF-03-agent-guidance-plan.md](RF-03-agent-guidance-plan.md) — proposed new agent-guidance system and migration guardrails.
5. [RF-DECISIONS-OPEN.md](RF-DECISIONS-OPEN.md) — decisions that cannot be inferred safely from current code or old documents.

## Historical proposed execution order

1. Accept the new documentation and guidance model, including its authority boundary.
2. Freeze a clean baseline and reserve shared hotspots.
3. Reconcile contracts and ADRs before any structural code move.
4. Refactor one module at a time with characterization tests and data-preservation checks.
5. Retire old documentation only after links, ownership, and replacement evidence are complete.

## Updated planning package — two developers and self-review

- [Ownership, parallel slices and integration rules](03-two-developer-plan.md)
- [Revised task report](templates/task-report.md)
- [Cross-owner coordination note](templates/coordination-note.md)
- [Change log for this archive](CHANGELOG-two-developer.md)

Read these additions before assigning RF-04. Historical evidence reports are preserved. This update revises the planning package only; it does not update the repository, activate agent guidance or verify runtime.
