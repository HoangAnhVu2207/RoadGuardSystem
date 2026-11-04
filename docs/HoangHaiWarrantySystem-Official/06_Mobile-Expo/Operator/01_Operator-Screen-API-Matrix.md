# Operator screen/API matrix

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

Actor: `DRONE_OPERATOR`; client: React Native + Expo; transport: bearer token. PM creates and assigns survey work on Web. Operator can read and act only on a current assignment, task, device and pinned project scope.

| Screen | Reads | Commands | DTO/headers/state | Status/evidence |
|---|---|---|---|---|
| O-01 Session | none | API-002 login; API-003 refresh; API-007 logout | Bearer access token; refresh token in OS secure storage | `CURRENT_IMPLEMENTED_BE`, `PROPOSED_EXPO`, `NOT_VERIFIED_EXPO_E2E` |
| O-02 Work list | API-038 `GET /api/v1/me/survey-tasks` (`cursor`, `limit`); API-226 task detail | None | `SurveyTaskPageV2ResponseDto`; current Operator and project scope; loading/empty/retry | `CURRENT_IMPLEMENTED_BE`; source `SurveyV2Controller.ListMyTasks` / `SurveyV2Service.ListMyTasksAsync` |
| O-03 Task detail/accept | API-226 | API-227 `POST /api/v1/survey-tasks/{taskId}/accept`, K/version as DTO requires | 401/403/404/409/412 must preserve task version and refetch | `CURRENT_IMPLEMENTED_BE`; `PROPOSED_EXPO` |
| O-04 Decline/supplement | API-226 | API-230 decline (K + If-Match, `SurveyTaskReasonV2RequestDto`) | PM owns API-232 supplement; Operator reads the returned/assigned supplement task; no reassignment | `CURRENT_IMPLEMENTED_BE`; `TARGET_CONFIRMED` |
| O-05 Work package | API-225, API-104 geometry package | None | Display pinned geometry/segment scope and upload purpose; no projectwide map grant | `CURRENT_IMPLEMENTED_BE`; `PROPOSED_EXPO` |
| O-06 Capture/upload | API-234 upload status; API-029 file metadata | API-233 create, API-236 part URLs, API-235 complete; raw storage PUT | `SURVEY_VIDEO`/`TELEMETRY` current task; file URI/size/checksum/ETag; complete is not VERIFIED | `CURRENT_IMPLEMENTED_BE`; `NOT_VERIFIED_EXPO_E2E` |
| O-07 Dataset submit | API-226 task; API-026 dataset read; API-025 coverage after submission | API-229 `POST /api/v1/survey-tasks/{taskId}/datasets` (K + If-Match, `SubmitDatasetRequestDto`) | Verified files, active device, route/version pins and optional video/telemetry pairs; missing telemetry is not coverage PASS | `CURRENT_IMPLEMENTED_BE`; `PROPOSED_EXPO` |
| O-08 History/recovery | API-226 task; API-234 upload status | Replay original command only with exact K/payload after unknown outcome | PM handles API-231 reassignment; Operator cannot call it. 401 relogin; 403 stop; 409/412 refetch and reconcile | `CURRENT_IMPLEMENTED_BE`; `NOT_VERIFIED_EXPO_E2E` |

Operator must never call PM geometry mutation, processing decision, candidate/Defect decision, repair approval, Crew execution or offline grant commands. DJI SDK, automatic flight ingestion and real GPS capture are `NOT_VERIFIED_EXPO_E2E`; the app may accept user-selected files while preserving declared provenance.
