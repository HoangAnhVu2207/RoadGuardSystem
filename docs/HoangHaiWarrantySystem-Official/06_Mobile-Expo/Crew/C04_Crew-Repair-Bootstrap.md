# C04 — Assigned Crew repair bootstrap

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

## Decision and evidence

Choice **B — EXISTING_CONTRACT_INCOMPLETE**. Owner-authorized task HHWS-BE-API-TEST-C04; base `f2c5f1adcbe610380533246f2082c0858ae079e5`. Current implementation is this change set; use Git history for its delivery SHA. This is an additive read projection, not a new route, role grant, schema migration or approval policy.

API-141/148 intentionally restrict repair item/history reads to PM/Supervisor (`RepairWorkflowRepository.ReadItemAsync` / `GuardAsync`). Their Crew 403 is correct. C03's PM-assisted version handoff was unsuitable for an independent Mobile client. Existing task list/detail exposed task concurrency, not repair item concurrency. Project work-package contains geometry/warranty/project version; survey work-package contains survey task/version; neither provides repair item version. Legacy `/me/inspection-tasks` is not a repair bootstrap contract.

## Source references

- [Task controller](../../../../RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs), `Get`.
- [Task repository](../../../../RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.cs), `GuardAsync`, `ReadAsync`, `ViewAsync`, serializable read transaction.
- [Assigned Crew projection](../../../../RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.CrewRepairContext.cs), `CrewRepairContextAsync`.
- [Persistence facts](../../../../RoadGuardSystem.BusinessObjects/PersistenceFacts/InspectionsFacts.cs), `FieldTaskViewFact`, `CrewRepairContextFact`.
- [Compatibility DTO](../../../../RoadGuardSystem.DTOs/Inspections/FieldInspectionWorkflowDtos.cs), `FieldTaskView`, `CrewRepairContext`, both implicit conversions.
- [Repair command authority](../../../../RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Execution.cs), `GuardExecutionResource`.
- [SQL regressions](../../../../tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairExecutionSqlTests.CrewBootstrap.cs), five `CrewBootstrap*` tests.
- [Swagger regression](../../../../tests/RoadGuardSystem.ApiTests/Authentication/ManualSwaggerContractTests.cs), `CrewTaskBootstrapDocumentsSeparateItemVersionWithoutChangingTaskVersion`.

## FE contract

Crew uses Mobile React Native Expo, bearer authentication and existing project scope. Discover assigned task IDs via API-078 `GET /api/v1/projects/{projectId}/field-inspection-tasks`, then API-080 `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}`. No Idempotency-Key or If-Match is required for these reads.

API-080 retains all existing fields. Its nullable `repairContext` covers the current assigned Crew on NORMAL and FastTrack tasks. Outer `projectId`, `id` (task ID), `assignmentId`, `firstStartOriginId`, `requiredMeasurementType`, `measurementScope`, `instructions` and `dueAt` retain their original semantics.

| Nested field | Source / meaning |
|---|---|
| `packageId` | Current item's parent package |
| `itemId` | Task-linked current repair item |
| `bindingId` | Item's current binding, matching task/assignment/Crew |
| `mode` | `NORMAL` or `FAST_TRACK`. Outer task mode remains `NORMAL` or `CONDITIONAL_FT`. |
| `status` | Exact `RepairItem.State.ToString()`; observed `Confirmed`, not `CONFIRMED` |
| `itemVersion` | Base64 SQL rowversion of **RepairItem**, not task |
| `repairPlan` | Current item's plan |
| `checklistVersion` | Item's checklist revision |
| `attempt` | Current attempt on this binding, including `attemptId` and `effectiveSubmissionId`. Null before a valid current intake is linked. |
| `attempt.review` | Current PM review of the effective submission. Contains `reviewId` and `decision` (`SUPPLEMENT` or `ACCEPT`). Null when there is no matching current review. |

Read task measurement requirements and the plan/checklist, then follow existing command admission and evidence DTOs. A null context supplies no current repair source for this task. PM/Supervisor and task list responses have no Crew repair context.

The summary follows `CurrentAttemptId`, `CurrentIntakeLinkId`, `EffectiveIntakeSubmissionId` and `CurrentReviewId` on the current repair item. The intake link and review must match the binding, attempt and exact submission. PM reason and reviewer identity are outside this projection.

After PM requests a supplement in API-158, read API-080 to see `SUPPLEMENT` for the effective submission. API-095 creates the FIELD revision. The effective repair submission changes when API-159 links that revision. On the next API-080 read, the attempt ID stays the same, the effective submission ID points to the revision and `review` is null. Once PM accepts that revision, API-080 returns its `ACCEPT` review. Refresh after the command or decision, and on opening or resuming the task screen.

API-158 continues to return `reviewId`, `itemId`, `submissionId`, `decision` and `receiptActivation`. API-159 continues to return `attemptId`, `linkId`, `itemId`, `submissionId`, `reviewClockId`, `originalReviewDueAt` and `intakeReadiness`. Both use repair item ETag, with 201 on first success and 200 on receipt replay.

### Response excerpts from local fixtures

NORMAL after PM accepted the effective submission. Captured from the API-080 projection in the SQL test.

```json
{
  "mode": "NORMAL",
  "version": "AAAAAAAACIE=",
  "repairContext": {
    "packageId": "da2734a8-68a2-4430-8407-ec65c6656c3c",
    "itemId": "019622e1-6a4e-4745-90c5-ccfe187dda70",
    "bindingId": "90648165-9d74-4422-ac23-dadbc7bb4874",
    "mode": "NORMAL",
    "status": "Reviewed",
    "itemVersion": "AAAAAAAACIQ=",
    "repairPlan": "actual repair plan",
    "checklistVersion": "checklist-v1",
    "attempt": {
      "attemptId": "e5241fc4-6390-4fc7-943d-b23e6439f445",
      "effectiveSubmissionId": "8d6ce58d-dabf-4544-82ec-72b79fc123fa",
      "review": {
        "reviewId": "a8ec8a8b-683b-467b-90ed-939d4c640756",
        "decision": "ACCEPT"
      }
    }
  }
}
```

FastTrack before an attempt exists. Captured from the authenticated API-080 HTTP response. The task ETag is `"AAAAAAAAB/E="`.

```json
{
  "mode": "CONDITIONAL_FT",
  "version": "AAAAAAAAB/E=",
  "repairContext": {
    "packageId": "2720572b-c21e-4739-8e4a-42799bc55e88",
    "itemId": "9f525438-57b7-490b-bd82-e9627b78f8ed",
    "bindingId": "a4990fc1-c81d-4efb-a90d-b4d9eebf17c7",
    "mode": "FAST_TRACK",
    "status": "Assigned",
    "itemVersion": "AAAAAAAAB/o=",
    "repairPlan": "conditional plan",
    "checklistVersion": "nd01-checklist",
    "attempt": null
  }
}
```

### Version and command sequence

Let `R=/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}` and `T=R/tasks/{taskId}`. The full exact routes remain in the [API catalog](../../04_API-Contracts/02_Full-API-Catalog.md).

| API | Action / actor | Version input |
|---|---|---|
| API-080 | Read current assigned task / Crew | None; response outer version and ETag are task version |
| API-156 `POST T/assessment` | Crew immutable BEFORE assessment | Fresh `repairContext.itemVersion` |
| API-161 `POST T/execution-start` | Crew physical execution-start claim | Fresh itemVersion |
| API-160 `POST T/execution-finish` | Crew finish claim | Fresh itemVersion |
| API-095 `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions` | FIELD intake or revision / Crew | **Outer task version**, not itemVersion |
| API-157 `POST T/attempts` | Bind immutable FIELD submission / Crew | Fresh itemVersion |
| API-158 `POST T/attempts/reviews` | SUPPLEMENT or ACCEPT / PM | Current PM item version |
| API-159 `POST T/attempts/supplements` | Bind FIELD revision to same repair attempt / Crew | Fresh itemVersion |
| API-147 `POST R/final-confirmation` | NORMAL final / Supervisor | Current item version |

For commands send bearer token, JSON body, stable `Idempotency-Key`, and `If-Match: "<base64-version>"` with quotes. Reread API-080 immediately before each Crew repair command and after another actor's decision. Item changes can leave outer task ETag unchanged; do not rely on task-only cache invalidation. Backend current-authority/concurrency checks remain authoritative after any read.

Bodies are existing DTOs: assessment carries `originId`, `fieldFirstStartId`, measurements, evidence and locationProof; start carries origin/firstStart/assessment/claimedAt; finish carries origin/executionStart/claimedAt; attempt carries `fieldSubmissionId`, `expectedContentHash`, `executionFinishId`; repair supplement carries revision `fieldSubmissionId`/`expectedContentHash`. FIELD revision is a separate submission with `parentSubmissionId`; API-159 is not a generic FIELD supplement. See executable examples in the external C04 Postman handoff.

Upload BEFORE/AFTER actual bytes through multipart upload, preserve object ETag/checksum, complete and poll boundedly for VERIFIED. Complete 202 is not VERIFIED. References use verified `fileId`/`uploadSessionId`, hash, capture time, provenance and coordinates as required by the existing submission DTO. C04 uses synthetic PNG/measurements in an isolated Development environment, not actual physical repair evidence.

### Authorization and recovery

The existing read guard checks current actor, project membership and active task assignment. The projection also checks agreement across the current item, binding, obligation, package, task and assigned Crew. FastTrack checks authorization against the current task, assignment, location and policy. It checks the pinned policy hash, checklist and active policy. Execution separately checks measurement, road coverage, first start and time window. A valid read supplies data and the item version for command admission. Historical FIELD reads retain their existing access.

401: renew/login using existing session policy, never log tokens. 403: stop command and refresh membership/assignment; no PM token fallback. 409 `concurrency_conflict`: reread API-080, reconcile and ask user to retry with new logical action/key only when appropriate. Preserve original key/body/version on unknown outcome replay. 412/428: use existing precondition error handling, never synthesize version; no claim these were executed in C04. Network failures: read state before creating a duplicate operation. API-141/148 and Supervisor/PM decisions remain unavailable to Crew.

## Earlier C04 verification checkpoint

CURRENT_VERIFIED: five new SQL bootstrap regressions; 71 related SQL tests; 966 Unit tests; 16 focused HTTP API tests; real isolated HTTP NORMAL chain through PM supplement/ACCEPT and Supervisor final, with Crew versions exclusively from API-080. Unassigned/wrong-project/revoked/ended/superseded and stale-version denial are covered by SQL regressions. Wrong-project, Reporter, Crew item/history/approval/final and PM final denials are also covered by actual HTTP. SQL fixture authority changes are isolated regression arrangements, never used to bypass the HTTP chain.

Runtime OpenAPI remains 241 operations; additive context schema is generated from source. Historical Phase2C snapshots remain historical; catalog has a scoped amendment. The Swagger source-evidence golden excerpt is updated only for the changed task GET branch, not regenerated indiscriminately. External C04 report/evidence records final Newman and exact-SHA CI results.

NOT_VERIFIED: Expo/native execution, native encrypted storage, production upload verification, deployment, real physical measurement and external AI. No FT/AI GAP is closed by C04. P5-G02 is CLOSED_CANDIDATE only for assigned NORMAL Crew Backend contract, subject to independent review; global MVP/E2E remains unverified.

## Current G02/G03 verification

Implemented locally on `huy-review` from `3b2f189760785f8034ce9a3bf1840a2a78cf4a65`. At fetch, `develop` was `d810cc52668a6f0a3272f8b20b095ac6fef122b2` with the same tree. The checkout started clean.

- SQL fixtures exercise the NORMAL source chain through PM SUPPLEMENT, FIELD revision, API-159 linking and PM ACCEPT. They also check stale repair version, wrong actor/project, ended assignment or membership and superseded binding.
- The FastTrack SQL fixture reads the item version from the Crew task projection and uses it to start execution with valid fixture sources. The HTTP fixture also checks FastTrack API-080 and separate task ETag/item version.
- Runtime Swagger at `/swagger/v1/swagger.json` describes the two modes, nullable summaries and version sources. The packaged OpenAPI file records the earlier preview.

| Executed check | Result |
| --- | --- |
| Integration filters `CrewBootstrap`, `IncompleteFormalFieldIntake`, `FastTrackReviewNotificationSqlTests` | 14 passed, 0 failed, 0 skipped in `crew-read-sql.trx`. |
| API filters `ManualSwaggerContractTests`, `ND01OriginalWindowHttpTests`, `H4RepairExecutionHttpAdmissionTests` | 29 passed, 0 failed, 0 skipped in `crew-read-http.trx`. |
| Rerun `CrewBootstrapSuppliesItemVersionForCommandAndRefreshWithoutPmRead` after asserting both item and history denials | 1 passed, 0 failed, 0 skipped in `crew-read-rights-final.trx`. |
| Scoped whitespace format, documentation guidance, JSON samples and diff check | Passed. |

TRX files are under `TestResults/CrewRead` in the local checkout. The first integration compile exposed a fixture variable still used later in the FastTrack test. That variable now takes the item version from the Crew projection, and the targeted integration run passed.

Both self-review passes checked the current IDs, source agreement, response compatibility and data exposure. Local fixtures use isolated SQL. Device UI, runtime deployment and real repair measurements remain outside this verification.
