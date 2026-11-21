# Mobile Crew/Operator lists and status

Status: owner assigned by attachment `80118161-5d3b-40f1-8268-f70c1b5bb628` (2026-11-21). Authorized normal push huy-review then fast-forward develop without waiting for CI; no deployment or FE database migration.

Base: `c0c99ca6241421ea7ffd5f806935df05cdc3cd27` after fetch; both remote branches matched. Writer Huy/Codex in separate worktree `D:\RG-web-pm-contracts`, branch `codex/mobile-crew-operator-lists`. Preserve unrelated main checkout at 7be7ffc.

Scope: Crew/Operator list controller-service-repository seams, additive summaries and filters/cursor binding; fixed assignment-derived history scope; independent processing summary; existing repair readback; nullable eventCode/task navigation in protected notification scope; source-backed Survey notification outbox/registry/adapter/DI, focused tests, Swagger source evidence/runtime snapshot and common handoff. No FE edits, workflow change, migration, shared data mutation, package upgrade or privilege broadening.

Compatibility: preserve existing task status wire; nullable mode instead of fabricated MEASURE_ONLY when binding cannot be proved; rejected history never authorizes detail/commands; all filters and project admission precede pagination. Cursor query context changes require restart. Processing does not approve a dataset; PM review does not replace final confirmation.

Authority: owner-requested notification additions bind persisted Survey assignment/dataset/task-child relations; no new task completion workflow, invented message-driven state or blanket recipients. Existing FIELD lifecycle/correction audit-only behavior remains explicit.

Verification: production HTTP/Bearer with isolated SQL; more than two pages and equal timestamp keys; scope/reassignment/history, status/mode/search combinations and cursor mismatch; actual repair assignment journeys, final/correction readback; Survey outbox dedupe/inbox/read/current assignment; dataset/assessment/processing layers; build/analyzer, format, source evidence and runtime Swagger. No SQL integration test identity added/renamed, no manifest changes.

Two self-review passes and exact results/limits are recorded in [handoff](../../deploy/fe-handoff/MOBILE-CREW-OPERATOR-LISTS-AND-STATUS.md). External review pending. Local evidence is not FE/runtime acceptance.
