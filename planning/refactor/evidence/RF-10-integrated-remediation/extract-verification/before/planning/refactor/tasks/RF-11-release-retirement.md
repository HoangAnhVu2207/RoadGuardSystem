# RF-11: Release reconciliation and historical-document retirement

> Current execution 2026-10-01: Anh alone owns non-destructive C audit on `anh`; G retirement/release is unassigned and gated. Proposed A/B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** PARTIAL local audit C01 on 2026-10-01; `planning/refactor/reports/RF-11-C01.md` records link/index/validator evidence. Parent RF-11 release/retirement remains pending: no old docs/ADR/routes/types retired, no deployed/consumer gate passed. Branch `anh`, local HEAD `2efc8a5` plus dirty tree; no change commit.
- **Goal:** verify integrated behavior and deliberately remove superseded docs/ADR/agent files from the active tree only after replacement and user authorization; keep Git provenance.
- **In scope:** full crosswalk closure, stale-link/tool path cleanup, final build/test/contract/consumer evidence, old document retirement and release/restore runbook. **Out of scope:** sweeping business redesign, dropping applied migrations, deleting data as doc cleanup or silently closing deferred modules.
- **Dependencies/decisions:** RF-04/05/06/07/08/09 complete; included RF-10 slices Done or explicitly deferred with owner/impact; user authorizes removal/retirement paths. External consumer deprecation windows completed where relevant.
- **Read first:** all RF reports/checkpoints, 57-action/133-draft operation manifest, R01-18/CG01-17/L01-16 ledgers, ADR transition, CI/validator scripts, Git history, migration inventory and consumer registry.
- **Likely files:** old `docs/**`, `docs/adr/**`, old `AGENTS.md`/`.agents/**` only as explicitly approved retirement targets; replacement `docs/product/**`, `docs/backend/**`, `contracts/**`, `docs/decisions/**`, `.github/workflows/ci.yml`, validators and final reports.
- **Contract/data/consumer effect:** no new public contract or data deletion in retirement step. Any approved endpoint retirement is a separate versioned RF-10/09 gate with consumer notice and fallback. Applied migrations remain. Git commit IDs map old -> new historical paths.
- **Steps:** prove replacement completeness -> run link/operation/tool guard on tree with old paths present -> approve exact retire list -> remove only approved old active files -> rerun guards/CI/tests -> record Git provenance and restoration instructions.
- **Verify:** clean-source solution build, affected/full test suites as warranted by integrated scope on isolated SQL, FE/Postman/OpenAPI alignment, docs links, agent manifest, CI workflow, no dangling old paths, operational restore drill if changed data is in release. Separate pre-existing failures from new regressions.
- **Done when:** every included requirement/gap/task has evidence or explicit deferred owner/reason; one active docs/contract/agent authority; no broken tool path; no unsupported PASS; old files recoverable from Git.
- **Recovery:** restore retired documents from the recorded pre-retirement Git commit and revert path/CI switch as one unit; runtime data recovery follows each approved RF-10 migration plan, never Git-only rollback of applied schema.

## Two-developer delivery supplement

- **Proposed owner:** A: release integration; B: module evidence and consumer checks. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
