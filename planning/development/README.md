# RoadGuard development workflow

Reference baseline: `21223aa1d18f510d012f9f25081b8c4bcf87b7a8` (RF-11). Check actual HEAD/dirty paths before each task; preserve later progress. RF tasks/reports remain historical checkpoints and evidence. This plan supersedes the old draft [development plan](../refactor/11-development-plan.md) for development routing; it does not reopen RF remediation or claim release/deployment acceptance.

ChatGPT provides technical direction, writes the spec and reviews the GitHub diff. Codex delivers the whole assigned package, two concise self-review passes, fixes and focused verification. Owner resolves missing business, public compatibility and data policy. Steps within an assigned spec need no repeated approval. A package assignment does not approve an undesigned endpoint, wire contract or schema.

## Proposed packages

All four packages are **PROPOSED, not assigned for implementation**. Activate each only after receiving its corresponding scoped spec using [the template](spec-template.md). These scopes retain functionality; they do not define routes, schemas, thresholds or unresolved decisions.

| Package | Proposed scope | Dependency |
|---|---|---|
| ANH-01 | Project/road/segment, upload, survey/dataset | Supplies geometry/version and verified-file references. |
| HUY-01 | Identity transport, Reporter/case, PM candidate/label | Supplies actor/case/defect/label references; consumes geometry/version and file references where needed. |
| ANH-02 | BE-AI contract/mock, reporting/export, retention/hold | Uses agreed ANH-01/HUY-01 interfaces and actual BE producers/consumers. |
| HUY-02 | Inspection/repair/Fast Track, notification, offline/handover BE | Uses agreed ANH-01/HUY-01 interfaces and actual BE producers/consumers. |

Freeze interface semantics, versions, scope, errors and acceptance fixtures in the relevant spec before dependent implementation. Verify real producers/consumers within BE; external AI/Android is checked with fixtures/mocks. Record BE verified, mock verified and external/deployment not verified separately. Current code is implementation evidence, not business authority; see [confirmed decisions](../../docs/product/confirmed-decisions.md) and [module source routes](../../.agents/modules/README.md). Draft OpenAPI remains draft unless separately adopted.

Excluded: real AI provider integration, retry/late-attempt, compatibility callback, and previously excluded A09 remediation. A08-01/A09 and other RF-11 known limitations remain recorded; this plan does not reopen them. Unresolved decisions in included functionality stay open for the affected spec rather than silently removing functionality.

## Delivery and shared files

Use `anh-review` and `huy-review` for implementation/handoff. One spec and a short PR summary suffice: resulting behavior/scope, base and HEAD, changed files, two self-review passes, focused checks with counts/results, open decisions and handoff conditions. ChatGPT reviews the GitHub diff; owner decisions remain separate. No RF-* identifier, seven-part report or separate coordination file is mandatory.

For the assigned HUY-FINAL-INTEGRATION execution, Huy coordinates DbContext/migration/snapshot, canonical contract, shared DI and integrated Postman. Record a named writer and integration order for these and other shared files in the spec/PR; never have two writers edit a shared file simultaneously. Keep complete vertical slices within assigned scope. No framework expansion, package upgrade or unrelated refactor. Use isolated SQL fixtures; docs-only tasks run affected guidance/docs checks without rebuilding/runtime tests. No real providers or deployment are implied by mock/BE verification.
