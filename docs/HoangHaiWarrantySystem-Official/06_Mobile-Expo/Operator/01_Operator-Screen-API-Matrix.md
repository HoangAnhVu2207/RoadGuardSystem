# Operator screen/API matrix

Actor: `DRONE_OPERATOR`; client: React Native + Expo; transport: bearer token. PM creates and assigns survey work on Web. Operator can read and act only on a current assignment, task, device and pinned project scope.

| Screen | Reads | Commands | DTO/headers/state | Status/evidence |
|---|---|---|---|---|
| O-01 Session | Auth bootstrap/login/refresh routes in [API auth catalog](../../04_API-Contracts/02_Full-API-Catalog.md) | Login/refresh/logout | Bearer access token; refresh token in OS secure storage | `CURRENT_IMPLEMENTED_BE`, `PROPOSED_EXPO`, `NOT_VERIFIED_EXPO_E2E` |
| O-02 Work list | API-225 `GET /api/v1/survey-tasks/{id}/work-package`, API-226 `GET /api/v1/survey-tasks/{taskId}` for assigned IDs | None | Render task id, status, project/route pins, version; loading/empty/retry | `CURRENT_IMPLEMENTED_BE`; assignment list aggregation is not a new endpoint (`SOURCE_GAP` if required) |
| O-03 Task detail/accept | API-226 | API-227 `POST /api/v1/survey-tasks/{taskId}/accept`, K/version as DTO requires | 401/403/404/409/412 must preserve task version and refetch | `CURRENT_IMPLEMENTED_BE`; `PROPOSED_EXPO` |
| O-04 Decline/supplement | API-226 | API-230 decline; PM owns API-232 supplement; Operator does not reassign | Keep server status and reason; no local state invention | `CURRENT_IMPLEMENTED_BE`; `TARGET_CONFIRMED` |
| O-05 Work package | API-225, API-104 geometry package | None | Display pinned geometry/segment scope and upload purpose; no projectwide map grant | `CURRENT_IMPLEMENTED_BE`; `PROPOSED_EXPO` |
| O-06 Capture/upload | API-233 `POST /api/v1/uploads`, API-236 part URLs, API-235 complete, API-234 status | Multipart commands; `SURVEY_VIDEO` and `TELEMETRY` only for current task | File URI/size/checksum/media metadata; complete is not VERIFIED | `CURRENT_IMPLEMENTED_BE`; `NOT_VERIFIED_EXPO_E2E` |
| O-07 Dataset submit | API-226 task and current dataset contract; API-229 `POST /api/v1/survey-tasks/{taskId}/datasets` | API-229 with exact dataset/version/file IDs | Verified video/SRT/GPS bindings, active device, route/version pins; missing telemetry is not coverage PASS | `CURRENT_IMPLEMENTED_BE`; `PROPOSED_EXPO` |
| O-08 History/recovery | API-226 and supported status reads | Repeat exact K only after unknown outcome; PM handles reassignment API-231 | Retryable vs validation/conflict UI; never change source pins locally | `CURRENT_IMPLEMENTED_BE`; `NOT_VERIFIED_EXPO_E2E` |

Operator must never call PM geometry mutation, processing decision, candidate/Defect decision, repair approval, Crew execution or offline grant commands. DJI SDK, automatic flight ingestion and real GPS capture are `NOT_VERIFIED_EXPO_E2E`; the app may accept user-selected files while preserving declared provenance.
