# Survey type, conflicts and explicit plan completion

Status: ASSIGNED. Authority: owner attachment `75bc4c82-3f45-480f-a37a-ff97de1d642b`, 22/11/2026 (`TARGET_CONFIRMED`). Writer: Huy/Codex, one writer per file. Local branch `codex/survey-plan-completion`; delivery `huy-review` then fast-forward `develop` as explicitly authorized. Base `2ab6539f21a69b935e491008d64c21fcc050152a`.

Main checkout `D:\DO_AN\RoadGuardSystem` has unrelated Case Report work at `7be7ffc`, preserved. Implementation uses `D:\RG-web-pm-contracts`, initially clean. Shared reservations: Survey V2 controller/service/repository/DTOs, collection summary projection, Swagger metadata/evidence/runtime snapshot, Survey HTTP/unit tests and route inventory. No other writer, package upgrade, workflow change or production data operation.

## Scope and evidence

- Persisted SurveyType mapped explicitly in plan/task list/detail and shared mutation views; persisted parent ID in Web task summary. Receipt compatibility defaults UNKNOWN and requires current GET for authoritative readback.
- Exact `UX_SurveyPlans_ActiveScope` retains project/roadSection/type and active statuses. Repository distinguishes business key, source scope, plan mismatch/state and receipt fingerprint conflicts. Conflict metadata requires current PM authority in a short transaction after rollback.
- PM explicitly confirms IN_PROGRESS BAND_V1 plan with named task/dataset/current assessment sources. Persisted root/supplement lineage, exact route/set/segment/band coverage and resolved branches required. No task status propagation or baseline change.
- New command uses serializable idempotency transaction and current receipt authority; plan rowversion, immutable audit basis and receipt commit together. Basis is read from the existing immutable AuditLogs table, avoiding schema change/backfill. No migration or new SQL test identity.
- Acceptance sources: current SurveyPlan/SurveyRequest, SurveyV2PersistenceService and existing task completion, DatasetAssessment repository, collection read conventions and owner request. Code proves implementation, not broader business approval.

## Verification and handoff

Focused production-auth HTTP on disposable SQL: all wire types, supplements/parent IDs, create/replay and active-index race, explicit plan completion from partial parent + supplement, stale assessment/version, denied actor/project/CSRF, unresolved branch and cancellation, independent task state, immutable basis after new Web session, period recreation, audit/baseline SQL assertions. Unit receipt compatibility; Swagger/source/schema/inventory and repository validators. Storage fixture is mocked; no FE runtime/browser acceptance or shared DB writes.

Self-review 1: fixed post-rollback conflict transaction/execution-strategy boundary, mapped current permission denial, exact index detection and branch lineage. Self-review 2: checked recursive audit allow-list preserves source IDs, parent/child scope containment, unresolved REJECTED branches, current write authority under transaction locks, old receipt UNKNOWN, required headers and OpenAPI parity. Executed results and remaining limits are recorded in the FE handoff. External GitHub diff review remains separate.

No missing business decision for this scope. Original correlation-ID request remains UNKNOWN unless the correct runtime logs are available. No inference from a reproduction to the original incident.
