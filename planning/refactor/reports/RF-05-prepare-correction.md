# RF-05 PREPARE correction report

## 1. Task and status

- RF-05 PREPARE correction: **Done**. ACTIVATION: **Not started, awaiting user acceptance of the corrected exact package**. RF-05 overall: **Partial**. RF-06 has not started.
- Branch: local `anh`; surveyed and unchanged HEAD: `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`. No change commit; remote freshness not rechecked.
- Working tree before: existing modified survey persistence/test and V2 planning/governance files, untracked survey dataset/Q11, docs/contracts and planning/refactor drafts. After: same pre-existing changes plus only RF-05 inactive drafts/checks/patches/manifest/report/checkpoint/ZIP. Active AGENTS.md, .agents guidance, CI, validators, production, schema, contracts, FE lock and data retain their original bytes; `checks/staging-result.json` records protected hash comparison.

## 2. Results and evidence

- Corrected inactive package: `planning/refactor/rf05/`, package version `rf05-prepare-correction-2`. Exact proposed targets and source/proposed SHA-256: `target-manifest.json`; corrected ZIP and per-file SHA-256: `planning/refactor/reports/RF-05-prepare-correction-handoff.zip` and `rf05/handoff-correction-manifest.json`. The prior ZIP is historical only.
- Changed from first package: readiness module and tests; guidance verifier and new guidance/real-data test scripts; delivery/root guide wording; generator and CI full targets/patches; generated manifest schema/version; staging harness; tooling-transition, activation plan and walkthrough; RF-05 checkpoint and this/addendum reports. The draft remains seven guidance paths, 15 target files and 22 enumerated retirement files. No old guide was moved.
- The accepted 32-44 table remains unchanged. Readiness decision source resolves to the Accepted heading and the exact ID row in `02-decision-register.md`, plus matching source/authority in RF-04 crosswalk. For MIXED/HISTORICAL requirements, accepted overlap may be recorded as an approved scope while whole-row READY is refused. `required_evidence` is a plan, never executed proof.

## 3. Findings and correction

| ID / module / impact | Before and evidence | Correction and check |
|---|---|---|
| RF05-C01 / authority / high | `drafts/rf05_readiness.py.draft` rejected only `docs/diagram/` for HISTORICAL_SOURCE_ONLY; a migrated `docs/product/` source could pass. | Decision ID, exact accepted wording, real heading/table row, mapped authority, related requirement and claim IDs required. MIXED/HISTORICAL whole-row READY fails; 9 readiness regressions include historical paths, Proposed/Unknown, unrelated claim, partial, missing source/anchor/scope and regeneration. |
| RF05-C02 / validator / high | `checks/verify_guidance.py` appended a missing path but did not call `verify` again. | Self-test runs valid `verify`, then calls the same `verify` with three bad fixtures and checks exact ValueError text; false success or other exception fails. Staged inactive and simulated-active invocations pass separately. |
| RF05-C03 / CI data / high | First CI draft ran only temporary readiness unit tests. | `verify_repository_readiness.py --root .` reads actual active registry, source mapping/IDs, decision source/scope, source hashes, crosswalk and generated readiness pages. Proposed CI sets Python 3.11, runs three unit scripts and this read-only check; unknown ID and stale output negative staging cases fail as expected. |
| RF05-C04 / reporting / medium | `drafts/agents/rules/delivery.md.draft` said to write the report template path. | Guide now says read the single seven-section template and write `reports/<TASK-ID>.md` or the named slice report. Draft/walkthrough search found no remaining write-template instruction. |
| RF05-C05 / requirements / medium | `verify_guidance.py` required exactly 148 rows. | 148 is reported as historical delta; actual source IDs, unique generated IDs, `SOURCE_TASK` mapping and output are compared. Four coverage regressions include valid new mapped ID, missing mapping, stale output and duplicate ID. |
| RF05-C06 / skills / medium | Guidance validator rejected any nonempty `.agents/skills`. | Retired paths are individually forbidden. New SKILL.md may be discovered only with guidance/skills manifest declaration and matching status; undeclared discoverable guides, including nested AGENTS.md, fail. Seven guidance regressions include old skill, valid new skill, undeclared skill and active metadata. |

## 4. Verification

| Check | Result and limit |
|---|---|
| `python planning/refactor/rf05/checks/build_patches.py`; `prepare_manifest.py` | PASS: five applicable unified patches/full targets, 15 proposed targets and 22 retirement paths. |
| `python planning/refactor/rf05/checks/stage_check.py` | PASS in external temporary staging; exact source/proposed hashes and `git apply --check` pass; protected active hashes unchanged. |
| Staged `build_rf04_sources.py`, `verify_rf04.py`, `test_rf04.py` | PASS: FR37 BR48 US41 PF8 NFR14; 13 accepted decision rows; 5 RF-04 regressions. Static only. |
| Staged `test_readiness.py`, `test_guidance.py`, `test_repository_readiness.py` | PASS: 9 + 7 + 4 unit regressions. Fixture authority is simulated, not owner acceptance. |
| Staged `verify_guidance.py --staging --self-test` | PASS positive inactive plus three exact-error negative cases through `verify`. |
| Staged `verify_repository_readiness.py --root <stage>` | PASS on actual staged RF-04 output: 148 mapped, 0 readiness registry decisions; intentionally unknown registry ID and stale generated output each return nonzero with the expected error. Read-only checker. |
| Proposed PowerShell wrappers in simulated active staging; YAML parse and existing `Verify-CiWorkflow.ps1` | PASS; placeholder acceptance metadata exists only in disposable staging. GitHub Actions was NOT RUN. |
| `git diff --check` | PASS on tracked changes; new files checked through staging/package bytes. |

- Not run: API/SQL/migration/runtime tests (not applicable to docs/tooling correction), actual GitHub CI, FE lock rerun, formal OpenAPI, external consumers or peer review. Existing RF-00 API seeder failure and FE `CONTRACT_LOCK_MISMATCH` remain baseline issues, not correction regressions. No PASS is claimed for these checks.

## 5. Decisions needed from user

- Activation requires explicit acceptance of the **corrected** `target-manifest.json` version/hash and `activation-plan.md`: 15 target files/content, exact 22 old guidance relocation paths, and actual acceptance metadata transform. The old package hash does not identify this package. A changed source hash requires a refreshed reviewed diff. This decision blocks activation and RF-06; it does not block PREPARE correction.
- No new business decision was made. The unresolved RF-02 public-contract/data-policy questions remain on their module tasks. Decision IDs 32-44 keep their 2026-09-30 Accepted requirement status without implying code or test completion.

## 6. Checkpoint

- Complete: six review findings addressed; readiness authority/scope and planned-evidence semantics; real positive/negative self-test; CI actual-data check; expandable requirements and skill discovery; guide/report path; staging and two-pass self-review; corrected manifest and handoff.
- Remaining: user review/acceptance; preflight then coordinated activation/archive; active manifest final metadata/hash; actual CI verification; only then RF-06 eligibility. The exact next step is review of `target-manifest.json`, `activation-plan.md` and corrected ZIP. No active guidance/tooling was switched.
- Recovery now affects only new inactive planning/refactor artifacts. Future activation rollback uses manifest-named path hashes; it must preserve unrelated dirty work and old docs/ADR.

## 7. Planner handoff summary

At local `anh` HEAD `2efc8a5`, RF-05 PREPARE correction is Done as an inactive 15-target package with 22 old guidance files reserved for later relocation; RF-05 overall is Partial and RF-06 has not begun. Six review findings are corrected. Staging passes RF-04 generation/verification, 20 RF-05 unit regressions, positive/negative guidance validation, actual staged registry/crosswalk validation, proposed wrappers/CI static checks and protected-file hashes. Accepted 32-44 remain requirement authority only; historical/MIXED rows cannot become whole-row READY. GitHub CI, runtime and peer review remain unverified. Next gate is explicit user acceptance of the corrected manifest and activation plan, followed by drift preflight and coordinated activation.
