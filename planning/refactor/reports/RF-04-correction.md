# RF-04 correction report: source content, ownership and static contract draft

## 1. Task và trạng thái

- **Task:** correction of `tasks/RF-04-documentation-contract-crosswalk.md`. **Implementation status:** Done for the complete inactive documentation/tooling draft. **Delivery stage:** Draft; neither agent guidance nor public contract is active. One performer completed the correction; A/B remain proposed, not Assigned.
- **Branch/HEAD:** local `anh`, `2efc8a5775f834c7f0fe37cc0ce703011649e1f1` before/after. No change commit, fetch, push, merge, checkout, reset or clean. Remote freshness was not reverified in correction.
- **Working tree before:** modified survey persistence, V2 governance/plan and survey integration test; untracked survey dataset source, Q11 note, all earlier `planning/refactor/` and RF-04 draft directories. **After:** those entries remain, with this correction inside RF-04 draft directories. Git's top-level short status cannot separate old/new untracked files; the prior RF-04 report and this file identify correction changes. No production/config/CI/active contract file was edited.

## 2. Kết quả chính

- `tools/rf04_ownership.py` now names one primary task per current controller and every historical requirement ID, plus explicit operationId overrides and cross-module context reasons. C027/current and `listMyInspectionTasks` both map to RF-10-07/A. BR-46 temporary safety maps to RF-10-07/A, with RF-10-09-A notification coordination. Other broad `x-fr` links are marked `CONTEXT` rather than treated as operation ownership.
- Operation generator retains raw route, prefix, declared API version, normalized route, normalization rule, source line, match kind, transition owner, branch/HEAD/dirty state and SHA-256 input fingerprints. It reads `/health` from `Program.cs`; action and route counts are separate. A new route absent from RF-01 is reported as a source delta, rather than forced to historical counts.
- All 148 historical FR/BR/US/PF/NFR sections, including acceptance conditions, were transferred into four draft product pages as 701 individually addressable source excerpts. Each ID has source line, content status, historical authority, confirmed overlap if any, proposed primary/coordinating task, missing decision and module checkpoint. Complete confirmed 32-44 interpretation is copied verbatim from the first RF-02 decision table into `docs/product/confirmed-decisions.md`; original 28/09 questions/options remain unavailable.
- `docs/product/workflows.md` now lists conceptual transitions with actors, guards, evidence, retry/failure behavior and source/gate; `data-and-quality.md` defines 13 business concepts and unresolved integrity rules. These are draft meanings, not wire enums, Fast Track thresholds or accepted schema.
- Pilot `contracts/http/work-package.proposed.yaml` now has referenced project/road/warranty/error schemas, nested fields, nullability based on DTO declarations, observed enum mapping, security, response statuses and correlation header. Source-only and HTTP-pending parts are labeled. `contracts/events/README.md` names each producer/consumer, evidence, missing decision, proposed task and acceptance test. Backend/ADR draft text and delivery slices were synchronized; old ADRs and all active surfaces remain untouched.
- Main changed outputs: `docs/product/*`, `docs/backend/*`, `docs/decisions/D-WORKFLOW-proposed.md`, `contracts/*`, `planning/refactor/04-*`, `tools/build_rf04_*`, `tools/rf04_ownership.py`, `tools/verify_rf04.py`, `tools/test_rf04.py`, `04-delivery-slices.md`, RF-04 task/report and this report/index. No old docs were removed.

## 3. Phát hiện và disposition

| ID / impact | Evidence and prior state | Correction and remaining gate |
|---|---|---|
| RF04-C01 / inspection ownership, Medium | RF-01 C027 `InspectionTasksController.List`; draft `listMyInspectionTasks`, `x-fr` FR-17/22; prior task RF-10-02 vs RF-10-07. | Both RF-10-07/A. Validator and mutation test reject old C027 mapping. No A/B person assigned. |
| RF04-C02 / Emergency ownership, Medium | BR-46 at old `02_Business_Rules.md:375`; prior numeric-range map RF-10-09. | RF-10-07/A primary, RF-10-09-A coordination for Supervisor notification. Source validator and mutation test reject old assignment. |
| RF04-C03 / broad draft trace, Medium | `x-fr` FR-36 covers model/admin/catalog while tags span identity, survey, AI and audit; route match alone did not prove same responsibility. | 13 explicit operationId owner overrides and 46 `CONTEXT` links with named source task/reason; one proposed primary task per operation. Cross A/B effects in CN-003; RF-10 owners review before START. |
| RF04-C04 / source content, High for future retirement | Original crosswalk located 148 headings but did not transfer full conditions/AC. | 148 full sections/701 excerpts, exact source cross-check and content links. Historical details are reviewable now; unconfirmed details block their module checkpoint, while RF-11 checks retirement later. No historical CHỐT label automatically becomes current acceptance. |
| RF04-C05 / route evidence, Medium | Old generator hardcoded branch/HEAD/controllers and collapsed `/api/v{version}` before classifying `exact`. | Actual Git/source metadata and hashes; all 44 matches are `normalized`, zero raw-exact; explicit version comparison, action/route counts and baseline delta. Candidate match does not certify DTO/auth/header/status/behavior. |
| RF04-C06 / pilot schema, Medium | `roadSections` and `warranties` were `items: {type: object}`. | Four referenced schemas statically checked against DTO names/nullable declarations; response/error/header/security source mapped. HTTP serialization, middleware wire, role values and decimal precision remain RF-06/07. No active OpenAPI change. |
| RF04-C07 / inherited runtime risks, High | PR-38 8 GiB vs current `int` size/limit CG17; AI callback active-attempt and PM review CG08/11. | CN-001/CN-002 remain Open with B producer/A integration proposals. No code/data/provider action taken. |

## 4. Kiểm chứng

| Command/check | Result | Limit |
|---|---|---|
| `python planning/refactor/tools/build_rf04_crosswalk.py` | PASS: 17 controllers, 57 distinct actions and 57 controller routes, one source health route, 133 unique draft operations, 44 candidates, 84 Postman requests. | Static source; no HTTP equivalence claim. |
| `python planning/refactor/tools/build_rf04_sources.py` | PASS: 37 FR, 48 BR, 41 US, eight PF, 14 NFR, six ADR, 13 decisions and 85 old document paths. | Historical content transferred, owner approval of unconfirmed detail remains pending. |
| `python planning/refactor/tools/verify_rf04.py` | PASS: dynamic IDs/counts, input hashes, bidirectional candidates/owners/content/anchors, 26 Markdown pages and four pilot schemas/refs. | Structural OpenAPI validation only; full formal validator not installed/run. Human semantic review still required. |
| `python planning/refactor/tools/test_rf04.py` | PASS: five mutation regression tests for C027, BR-46, version mismatch, count delta and missing/broken nested schema. | Tool behavior, not API behavior. |
| `git diff --check` | PASS for tracked diff; verifier checks correction Markdown whitespace/links. | Git does not inspect untracked files alone. |

**Current counts and delta:** 57 actions / 58 current routes including health / 133 draft operations, each delta 0 against the historical RF-04 baseline. These are two linked sets of 58 and 133 rows, not 191 separate implemented APIs. 44 draft operations have normalized current route candidates; 89 are draft-only and 14 current routes have no draft candidate. Exact raw/parameter alias/ambiguous matches: 0/0/0. The current snapshot has no cross-task current-to-draft ownership transition; the generator and validator require a reason when one appears.

**Not run:** API/SQL/migration/FE-lock/Postman runner/external AI/Android tests, because this correction only changes documents and RF-04 tooling. Full formal OpenAPI validator and peer review were not run; do not report PASS for them. **Inherited baseline:** RF-00 API group 19/19 failed at seeder before HTTP assertions; FE guard `CONTRACT_LOCK_MISMATCH`. They were not rerun or fixed. No new runtime failure is attributed to correction.

## 5. Quyết định cần người dùng

| Question and options | Recommendation / impact | Blocked task/checkpoint |
|---|---|---|
| For each historical detail beyond confirmed PR-32..44, accept, revise or keep outside module scope? | Review the transferred content per module; preserve old sources until claim-level decision. | Relevant RF-10 task START for that detail, RF-11 retirement. |
| Q-RF02-02..08: transport/consumer, survey row/method, Fast Track dossier, AI provider, Reporter privacy and retention deletion authority. | Keep RF-02 options and proposed gates; do not infer from historical CHỐT or V2 route. | RF-09/10 public/data effects as named in RF-02. |
| Assign actual A/B writers and acknowledge CN-001/002? | Proposed ownership is review material; reserve shared migration/AI candidate interface before implementation. | RF-09/10-04/05/06 integration. |
| Agree domain ownership of draft admin/model/device/label operations in CN-003? | Proposed B owns model/job and survey device, A owns PM label review/export gate; agree actor/fixture handoff. | RF-10-03/05/06 integration; RF-05 cites only as proposal. |

No repeat confirmation is requested for 32-44; their complete interpretation was reconfirmed on 2026-09-30. Future missing decisions do not negate completion of this inactive draft.

## 6. Checkpoint

- **Completed:** all named review findings corrected, generated outputs renewed, two self-review passes and five tool regression cases. Pass 1 checked source meaning/authority/operation responsibility/schema; pass 2 checked A/B ownership, CN-001/002/003, shared surfaces, inactive status and preserved dirty files. Self-review: PASS for static draft and scope. Peer review/integration: NOT RUN.
- **Remaining:** module owner authority review for unconfirmed historical detail; RF-06/07 HTTP capture; external Web/Android/AI consumers; formal OpenAPI validation before any canonical adoption; RF-05 user acceptance before guidance activation.
- **Next exact step:** hand this ZIP and crosswalk to the planner/owner for RF-05 preparation review. Do not activate agent files, switch CI/FE lock, or begin RF-05 from this correction task.
- **Handoff artifact:** `planning/refactor/reports/RF-04-correction-handoff.zip`, with repository-relative entries and `planning/refactor/04-handoff-manifest.json` (file SHA-256 and check results).
- **Recovery:** only draft docs/tools/ZIP were added or revised; revert these review artifacts if rejected. Old active docs/ADR/agent/contracts and production code remain available. No migration or data restore is needed.

## 7. Tóm tắt gửi người lập kế hoạch

RF-04 correction on local `anh` at `2efc8a5` is Done for inactive draft documentation, stage Draft. It fixes C027 inspection and BR-46 Emergency ownership, explicitly distinguishes 46 cross-module `x-fr` context links, transfers 148 historical requirement sections with 701 source excerpts and complete confirmed 32-44 wording, and completes the source-level nested work-package pilot schema. Current/draft inventory remains 57 actions, 58 current routes and 133 draft operations, with 44 normalized candidates and zero count delta. Static generator/validator and five regression tests pass; runtime, full formal OpenAPI, peer and external consumer checks are not claimed. Historical detail authority, Q-RF02-02..08, PR-38 data migration, AI provider contract and CN-003 ownership agreement remain future gates. RF-05 may prepare inactive guidance/tooling for owner review, but no activation or RF-05 implementation was authorized here.
