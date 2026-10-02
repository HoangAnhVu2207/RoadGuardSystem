# RF-04: Draft product/backend docs, decisions and contract crosswalk

- **Status/checkpoint:** Done for corrected **inactive draft documentation only** on local `anh` at `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; delivery remains Draft. Original [RF-04 report](../reports/RF-04.md) is preserved; [correction report](../reports/RF-04-correction.md) records findings, new checks and handoff. Production, active contract, agent activation and peer review are separate future gates.
- **Goal:** create complete source-tagged draft content in `docs/product/`, `docs/backend/`, `contracts/`, `docs/decisions/`; reconcile all 57 current controller actions + `/health`, 133 draft OpenAPI operations, R01-18 and ADR 001-006. This task drafts content, not an active API change.
- **In scope:** requirement/data/actor glossary, backend module maps, current-vs-target contract index, decision transition, legacy-path crosswalk and link manifest. **Out of scope:** production source/schema, active OpenAPI relock, CI/agent activation and deletion of old docs.
- **Dependencies/decisions:** RF-03 review; 32-44 accepted text from `02-decision-register.md`; Q-RF02-02..08 remain marked unresolved. Source owner/contract approver must be named before any artifact becomes normative.
- **Read first:** RF-00..02 reports, `01-endpoint-inventory.md`, `02-requirements-traceability.md`, `02-adr-transition.md`, old scope/FR/BR/ERD/OpenAPI/Postman and source symbols cited there.
- **Likely files:** new `docs/product/**`, `docs/backend/**`, `contracts/**`, `docs/decisions/**`, `planning/refactor/` crosswalk. Old `docs/diagram/V2/**`, `docs/adr/**`, `AGENTS.md`, `.agents/**` are read-only inputs.
- **Contract/data/consumer effect:** none at runtime. The draft manifest records every current route and draft operation as implemented/proposed/deferred/needs-decision with web/Android/AI/Postman/`.http` consumers and outside-repo unknowns; records table/retention provenance without applying migration.
- **Steps:** inventory old IDs/links -> author product/backend source pages -> map decisions/ADRs -> parse OpenAPI operation IDs -> join current routes/tests/Postman -> assign owner/status and unresolved questions -> review generated-vs-hand-authored rules.
- **Verify:** parsed count 133 unique target operations, 57 current actions + health and 17 CG/18 R coverage; link/ID/duplicate-canonical checks, `git diff --check`; no API/SQL test required for docs-only task. Report pre-existing FE lock mismatch separately.
- **Done when:** every old normative item has a target/historical/deferred row with reason; every operation has a disposition and consumer status; drafts contain no invented Accepted rule; reviewer can identify one proposed canonical owner per concept.
- **Recovery:** revert only new draft files/checkpoint if rejected; old docs/contracts remain intact and active behavior unchanged.

## Two-developer delivery supplement

- **Proposed owner:** A: BE/contracts/crosswalk integration; B: product/requirements slices. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
