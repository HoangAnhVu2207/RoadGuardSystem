# RF-05 PREPARE correction 3: sourced readiness transition

## 1. Task and status

- **PREPARE correction Done. ACTIVATION Not started. RF-05 overall Partial. RF-06 Not started.** This report supersedes correction 2 for package review; previous reports remain historical.
- Branch: local `anh`; HEAD before and after `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`. No change commit or remote freshness check.
- Working tree before: modified survey persistence/test and V2 planning/governance files; untracked survey dataset/Q11, docs/contracts and planning/refactor. After: these changes preserved; only inactive RF-05 package, reports and task checkpoint changed. `stage_check.py` compares 28 protected active source/retirement file hashes before/after.

## 2. Results and evidence

- Root cause: old `rf05_readiness.py.draft` required `TARGET_CONFIRMED_2026-09-30` on a whole row, while RF-04 `sections()`/`nfr_rows()` always generated HISTORICAL/MIXED; decision resolver was fixed to 32-44. A later valid decision could never produce whole-row READY through the generator. Old positive tests manually changed row authority.
- Correction: new `drafts/decision-index.json.draft` is empty for real data. `rf05_readiness.py.draft` checks a new decision's source section, Accepted status with person/date, exact table interpretation/scope, indexed requirement, claim IDs and claim-content SHA-256. Generator derives `source_authority`, current row/claim authority and readiness; RF-04 historical text and 32-44 decisions remain intact. The registry requests state/scope but cannot alone confer authority. `required_evidence` remains planned checks, never executed proof.
- Generator/verifier full target and patches now use the decision index. `verify_repository_readiness.py` compares generated crosswalk, fingerprints and product-page row/claim authority markers without writing. Schema, activation plan, tooling transition and task checkpoint describe the data flow.
- Proposed target delta: **16 vs 15**, only new target `planning/refactor/04-decision-index.json`; five replacement patches and 22 proposed retirement paths unchanged. `target-manifest.json` contains exact source/proposed hashes. No active AGENTS.md, `.agents/`, CI, validators, production, schema, active contract, FE lock, Postman or data changed.
- Handoff: `planning/refactor/reports/RF-05-prepare-correction-3-handoff.zip` with per-file SHA-256 in `planning/refactor/rf05/handoff-correction-3-manifest.json`. The previous package/manifest remain historical and are not the activation candidate.

## 3. Findings

| ID / module / impact | Evidence and current state | Confirmed target and treatment |
|---|---|---|
| RF05-C07 / readiness / high | RF-04 generator `sections()`/`nfr_rows()` emits only historical/MIXED authority; old resolver hardcoded 32-44; positive tests supplied forged confirmed row authority. | `drafts/rf05_readiness.py.draft`, `patches/targets/planning/refactor/tools/build_rf04_sources.py.draft`: derive current authority from a sourced decision and claim hashes; preserve `source_authority`. End-to-end fixture proves transition. |
| RF05-C08 / decision scope / high | Old registry's `decision_claim`, `scope` and `claim_ids` could outlive changed historical content. | Decision index binds exact source status, table wording/scope and claim SHA-256; missing, Proposed, wrong anchor/scope, unrelated or changed claims fail with specific errors. |
| RF05-C09 / generated output / medium | Old actual-data check inspected readiness marker only on product pages. | `verify_repository_readiness.py` also checks row historical/current authority and each claim's origin/current marker; crosswalk and all source fingerprints must match. |

## 4. Verification

| Check | Result and limit |
|---|---|
| `python planning/refactor/rf05/checks/build_patches.py`; `prepare_manifest.py` | PASS: 5 applicable patches, 16 target hashes, 22 retirement hashes. |
| `python planning/refactor/rf05/checks/stage_check.py` | PASS in external disposable staging; active protected hashes unchanged. Staged RF-04 generator/verifier and 5 RF-04 tests pass. |
| RF-05 unit scripts | PASS: 11 readiness, 7 guidance, 4 repository coverage = 22 tests. No fixture authority was put in real data. |
| `test_readiness_flow.py` in staging | PASS: FR-02 baseline 4 historical claims/BLOCKED; fixture Accepted decision `RF05-FIXTURE-NEW` outside 32-44 makes current authority confirmed and READY; read-only validator PASS; second generator run byte-stable. Partial decision confirms one claim, leaves three unconfirmed and row BLOCKED; validator PASS. |
| End-to-end negative fixtures | PASS, exact errors: Proposed, Unknown, historical source, wrong anchor, missing source, unrelated decision, unrelated claim, absent source scope, changed claim, forged output, forged registry and stale output after decision-content change. These are expected rejections, not setup/import errors. |
| Staged real repository inputs | PASS: 148 mapped requirements, 0 new decision-index and 0 registry entries, 0 newly READY both before/after; actual-data validator read-only. Unknown ID and stale generated output deliberately rejected. Accepted 32-44 table remains unchanged. |
| Guidance/CI staging | PASS: inactive and simulated-active guidance positive/negative checks, proposed PowerShell wrappers, CI YAML parse and existing CI security verifier. GitHub Actions was NOT RUN. |

- API/SQL/migration/runtime tests were not run because this is an inactive documentation/tooling correction. Formal OpenAPI, FE lock rerun, external consumers and peer review were not performed. RF-00 API seeder failure and FE `CONTRACT_LOCK_MISMATCH` remain known baseline issues, not new regressions.

## 5. Decisions needed from user

- Activation still requires explicit acceptance of corrected package version `rf05-prepare-correction-3`, exact target-manifest hash, 16 targets, 22 old-guide relocation paths and `activation-plan.md`. This blocks ACTIVATION and RF-06, not PREPARE. A source-hash drift at activation requires a fresh reviewed patch.
- No new business decision was made. The fixture owner/decision/date are simulated. Real future Accepted decisions need actual owner source with exact content and scope; implementation/test status remains separate.

## 6. Checkpoint

- Complete: generic sourced decision resolver, generator-derived current authority, claim hash guard, partial/full readiness, real-data verifier, end-to-end success/negative tests, RF-04/RF-05 regressions, staging and protected hash check, 16-target manifest and handoff.
- Remaining: owner review/acceptance, activation preflight, coordinated guide/tool switch, actual CI and final active metadata. The next step is review of the corrected `target-manifest.json`, `activation-plan.md` and ZIP; do not start RF-06 before verified activation.
- Recovery: the current package is inactive planning material. Future activation rollback restores only manifest-named targets/old guide paths with their hashes; no database recovery applies.

## 7. Planner handoff summary

At local `anh` HEAD `2efc8a5`, RF-05 PREPARE correction 3 is Done as an inactive 16-target package; RF-05 overall is Partial and RF-06 has not begun. The former impossible historical-to-READY transition is now proven with an actual generator and read-only validator on a disposable repository, including partial confirmation and 12 exact-error rejections. Existing 148 requirements remain BLOCKED in staged real data; no new Accepted decision or active file was written. RF-04/05 static tests, package hashes and proposed CI wrappers pass in staging. GitHub CI, peer review and activation remain unverified. User acceptance of the exact v3 package is the next gate.
