# RF-05: Prepare and conditionally activate new guidance/tooling

- **Status/checkpoint:** RF-05 local activation DONE on `anh` at `2efc8a5775f834c7f0fe37cc0ce703011649e1f1` after explicit owner acceptance of manifest SHA-256 `764bec3cac0450fe4219fa0e41168795efb0bc7120569c691354e587540c0448`. Report: `planning/refactor/reports/RF-05-activation.md`; exact checks/hashes: `planning/refactor/rf05/activation-result.json`. GitHub Actions is NOT RUN because no push was authorized. RF-06 has not started per owner instruction.
- **Goal:** write proposed root `AGENTS.md`/`.agents/` guidance and switch document/tool validators/CI only after the user accepts the exact replacement set. One active authority at completion.
- **In scope:** guidance files, manifest, source-routing/report template, path validator, docs/contract guards, CI invocation and crosswalk. **Out of scope:** runtime code, migrations, business decisions, old docs/ADR deletion, framework/package upgrade.
- **Dependencies/decisions:** RF-04 content and old-path map complete; explicit user acceptance for activation and CI/guide replacement. If absent, finish drafts and mark Partial rather than activate. No Q-RF02 choice is made by guidance.
- **Read first:** `03-agent-design.md`, `03-documentation-design.md`, `00-tooling-dependencies.md`, RF-04 manifest; inspect existing `Verify-P102Docs.ps1`, `Verify-AgentSetup.ps1`, V2/FE guards, `.github/workflows/ci.yml` before editing/running.
- **Likely files:** `AGENTS.md`, `.agents/README.md`, `.agents/manifest.json`, `.agents/rules/**`, `.agents/modules/**`, `tests/Documentation/**`, `tests/Tooling/**`, `.github/workflows/ci.yml`, path links. These are proposed touch points, not RF-03 changes.
- **Contract/data/consumer effect:** no public wire/schema change. Agent/CI consumers change only after acceptance; FE snapshot/lock stays unchanged unless a separately approved contract package supplies the exact version.
- **Steps:** draft full content -> compare against old guidance dependencies -> present exact diff for acceptance -> stage validator/CI/path switch as one change -> record active version/date -> check there is no conflicting active entrypoint.
- **Verify:** old and new path/ownership/link guards where applicable, CI static verifier, parsed OpenAPI/FE lock and Postman path checks; distinguish existing `CONTRACT_LOCK_MISMATCH` from new regression. Documentation/tooling validators only; no runtime suite.
- **Done when:** user acceptance recorded, one active guidance set/manifest, all new links and CI commands work, old guidance preserved as history until RF-11, no business rule duplicated as independent agent authority.
- **Recovery:** revert coordinated guidance/CI/path switch to previous working invocation and keep draft material for review; do not delete user changes or historical guidance.

## Two-developer delivery supplement

- **Proposed owner:** A: guidance/tooling; B: module-map review fixtures. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.

## RF-05 PREPARE checkpoint

- Inactive package: planning/refactor/rf05/drafts/, patches/, checks/, target-manifest.json, tooling-transition.md, activation-plan.md and walkthroughs.md.
- Exact target map: 15 proposed files with source/proposed hashes; 22 old discoverable guidance files proposed for relocation only after acceptance. A/B remains proposed, not assigned.
- Staging: external temporary copy, exact-hash overlay and git apply --check PASS; RF-04 generator/verifier and five RF-04 regressions PASS; five readiness regressions PASS; proposed guidance guard, YAML parse and existing CI verifier PASS. Active protected files unchanged. See checks/staging-result.json.
- Remaining: owner acceptance; activation metadata/hash, coordinated file/path switch, real CI result. RF-06 has not started. Baseline FE lock mismatch and API seeder failure remain separate.

## RF-05 PREPARE correction checkpoint (2026-09-30)

- **PREPARE correction DONE; ACTIVATION NOT STARTED; RF-05 overall PARTIAL.** Corrected package version `rf05-prepare-correction-2` supersedes the earlier handoff for review. See `planning/refactor/reports/RF-05-prepare-correction.md` and `planning/refactor/rf05/target-manifest.json`.
- Readiness now resolves the Accepted decision heading/table ID, exact wording, mapped authority and related requirement/claim IDs. Partial overlap stays BLOCKED; historical/MIXED whole-row READY is refused. `required_evidence` is a verification plan only. Regeneration retains recorded decision entries.
- Guidance self-test invokes the actual inventory validator for valid and invalid fixtures. Discoverable new skills require manifest declaration/status; old retired skills and undeclared guidance fail. Requirement count 148 is a baseline delta; source IDs and mapping are enforced.
- Proposed CI adds Python 3.11 setup, unit regressions and read-only actual registry/crosswalk validation. External staging, active-mode simulation and protected-file hash checks are documented in `checks/staging-result.json`. No active paths have been replaced. RF-06 has not started.

## RF-05 PREPARE correction 3 checkpoint (2026-09-30)

- **PREPARE correction DONE; ACTIVATION NOT STARTED; RF-05 overall PARTIAL.** Package `rf05-prepare-correction-3` supersedes correction 2 for activation review; prior reports and ZIP remain historical evidence. See `planning/refactor/reports/RF-05-prepare-correction-3.md`.
- Decision index is an additional inactive target (16 total; retirement list remains 22). A new Accepted decision must resolve to a real source section/status/content/scope and exact historical claim hashes. Generator derives current authority and readiness; source authority remains historical. Existing 32-44 decisions and their 2026-09-30 provenance are retained.
- Temporary end-to-end fixture proves FR-02 goes BLOCKED -> READY via a simulated later decision, then stable regeneration; one-claim approval stays BLOCKED. Twelve exact-error negative scenarios and staged RF-04/RF-05 checks pass. Actual repository remains 148 BLOCKED, zero new indexed/registry decisions. Active guides, CI and validators retain original bytes.
- Remaining: review/acceptance of corrected exact manifest/activation plan, preflight source hashes, coordinated activation and actual CI. RF-06 has not started.

## RF-05 activation checkpoint (2026-09-30)

- User accepted exact package `rf05-prepare-correction-3` and manifest hash above. Preflight found no source/draft/retirement hash drift or destination conflict. Applied 16 targets, including metadata-only active manifest transform, and moved exactly 22 old guide files to `docs/history/agent-pre-rf05/` with preserved hashes. No commit, push or merge.
- Generator reran on local real data: FR37 BR48 US41 PF8 NFR14; 148 requirements remain BLOCKED with zero readiness registry decisions. RF-04 static verifier and five tests, 22 RF-05 unit tests, read-only repository validator, guidance positive/negative self-test, disposable decision-flow fixture, both PowerShell wrappers and CI security verifier passed. The five explicit keep-unchanged file hashes match preflight. See `activation-result.json`.
- Local activation and verification are complete. GitHub Actions, runtime/API/SQL, FE lock rerun, external consumers and peer review are NOT RUN. RF-06 remains NOT STARTED by explicit owner instruction. Next task requires a separate request; no automatic RF-06 handoff.
