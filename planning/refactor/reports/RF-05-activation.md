# RF-05 activation report

## 1. Task and status

- **RF-05 local activation: Done.** RF-06: Not started, as instructed. Branch: local `anh`; HEAD before/after `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; no change commit, push or merge. Remote freshness was not rechecked.
- Owner acceptance source: user message dated 2026-09-30 explicitly accepting `rf05-prepare-correction-3`, target manifest SHA-256 `764bec3cac0450fe4219fa0e41168795efb0bc7120569c691354e587540c0448`, 16 targets and 22 old guide relocations. The exact active manifest SHA-256 is `db1f44ae4198c32507c93a284c691cfc4b7477edaeeb763ea52f9a7f4e305410` after the approved metadata transform.
- Working tree before: modified survey persistence/test and V2 planning/governance files; untracked survey dataset/Q11, docs/contracts and planning/refactor. After: these pre-existing paths remain; RF-05 activation adds/replaces only manifest targets, moves 22 named old guide files to history, regenerates seven RF-04 crosswalk/product outputs, and writes this checkpoint/result. Git status is still dirty; no unrelated path was reset or cleaned.

## 2. Results and evidence

- Read-only preflight found no branch/HEAD, source/draft hash or retirement-destination drift. `planning/refactor/rf05/checks/activate_package.py` applied exactly 16 manifest targets and moved 22 old guide files. `planning/refactor/rf05/activation-result.json` records every target hash, old-guide count, acceptance source and executed checks. The proposed manifest itself remains at the accepted hash.
- Root `AGENTS.md` and seven declared guidance paths are active through `.agents/manifest.json` status `ACTIVE_AFTER_OWNER_ACCEPTANCE`, with `accepted_at=2026-09-30` and the acceptance source. No old discoverable `SKILL.md` remains under `.agents/`; `.agents/mcp_config.json` is preserved. The 22 old file bytes are at `docs/history/agent-pre-rf05/` with matching original hashes.
- Generator refreshed `04-source-crosswalk.json/.md` and five product history/decision pages. FR37, BR48, US41, PF8, NFR14 still map to 148 requirements. The active decision index and readiness registry are empty; no real requirement gained READY. The confirmed 32-44 decision content remains unchanged.
- The five explicit keep-unchanged paths in `target-manifest.json`, including FE lock, Postman and old V2 validators, match preflight hashes. Production code, schema, active public contract and data were not changed by this activation.

## 3. Findings

| ID / module / impact | Evidence and current state | Sourced target and treatment |
|---|---|---|
| RF05-A01 / guidance / high | Previous `.agents/` discovery paths were 22 old guide files; `activation-result.json` verifies their source absence and byte-identical archived destinations. | Accepted manifest names exact relocations. New root guide/manifest and seven paths validate in active mode; history remains traceable. |
| RF05-A02 / readiness / high | Previously generated RF-04 rows used historical/MIXED authority and BLOCKED_* labels; active generator and read-only validator now use decision index plus registry. | Accepted package changes the authority mechanism without asserting new business acceptance. Actual 148 rows remain BLOCKED; simulated later decision succeeds only in a disposable fixture. |
| RF05-A03 / tooling / medium | `.github/workflows/ci.yml` and two PowerShell wrappers changed as package targets; CI security verifier passes locally. | CI invokes guidance and read-only repository checks. Hosted GitHub Actions execution remains NOT RUN because no push was authorized. |

## 4. Verification

| Command/check | Result and limit |
|---|---|
| `python planning/refactor/rf05/checks/activate_package.py` | PASS preflight: accepted SHA, local branch/HEAD, 16 draft/source hashes, 22 retirement hashes, no target conflicts. |
| `python planning/refactor/rf05/checks/activate_package.py --apply` | PASS: 16 targets, 22 relocations, 12 recorded commands. Scoped restoration was available on error and was not needed. |
| `python planning/refactor/tools/build_rf04_sources.py`; `verify_rf04.py`; `test_rf04.py` | PASS: 148 mapped requirements, 13 preserved decisions, 5 RF-04 tests. |
| RF-05 `test_readiness.py`, `test_guidance.py`, `test_repository_readiness.py` | PASS: 11 + 7 + 4 = 22 unit regressions. |
| `verify_repository_readiness.py --root .`; `verify_guidance.py --root . --mode all --self-test` | PASS: actual repository read-only check reports 148 mapped and 0 registry decisions; active guidance and three deliberate negative checks pass. |
| `test_readiness_flow.py` on a temporary fixture | PASS: historical FR-02 BLOCKED -> READY through a simulated Accepted decision and real generator/validator; partial scope remains BLOCKED; 12 precise rejection cases pass. Fixture decision never entered real data. |
| Both new PowerShell wrappers with self-tests; `tests/CI/Verify-CiWorkflow.ps1 -RepoRoot .` | PASS locally. Hosted GitHub Actions NOT RUN. |
| `git diff --check`; target/archived/keep-unchanged hash review | PASS without whitespace errors; Git reports LF/CRLF advisory warnings for three touched text files. |

- Not run: hosted GitHub CI, API/SQL/migration/runtime tests, FE lock rerun, external consumers and peer review. These are not reported as PASS. RF-00 API seeder failure and existing FE `CONTRACT_LOCK_MISMATCH` remain baseline issues; neither was retested in this activation.

## 5. Decisions needed from user

- None to complete this authorized local activation. A later push or hosted CI run needs a separate instruction; no push was inferred from acceptance. Any future public contract, schema, data or RF-06 change remains outside this task.

## 6. Checkpoint

- Complete: accepted-manifest preflight, exact target application and old-guide relocation, active metadata, real generator output, local static/regression/validator checks, hash and discovery review, two-pass self-review. Pass 1 checked sourced authority/readiness and unchanged 32-44 semantics; pass 2 checked scope, old-guide traceability, CI callers and recovery. No peer reviewer was requested.
- Remaining: hosted CI evidence and future module work. The next action for RF-05, when separately authorized to publish the branch, is run the GitHub verify job and record its result; do not start RF-06 automatically. Recovery follows `activation-plan.md` and the source/target hashes in `target-manifest.json` and `activation-result.json`.

## 7. Planner handoff summary

The owner accepted exact RF-05 package v3 on 2026-09-30. Local `anh` HEAD `2efc8a5` now has one active new agent guide, 16 applied targets and 22 hash-preserved old guides under history. RF-04 generation and 5 tests, 22 RF-05 regressions, actual read-only readiness check, active guidance negative self-tests, temporary decision-flow test and CI wrapper/security checks passed. All 148 real historical requirements remain BLOCKED; no real decision was created. GitHub Actions and runtime checks were not run, and no commit/push/merge occurred. RF-06 remains unstarted by instruction.
