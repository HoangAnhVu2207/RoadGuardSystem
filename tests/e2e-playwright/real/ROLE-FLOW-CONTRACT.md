# RoadGuard role-flow contract

This harness exercises the RoadGuard API from a client's perspective. It can use a disposable loopback stack or an explicitly configured HTTPS endpoint.

## Role sessions

| Role | Client actions covered | Public routes used |
| --- | --- | --- |
| Supervisor | Session, survey task read (200) | `POST /api/v1/auth/login`, `GET /api/v1/profile`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`, `GET /api/v1/survey-tasks/{taskId}` |
| PM | Session, survey task read in scope (200) | Same session and task read routes |
| PM outside project | Session, survey task read denied (403) | Same session and task read routes |
| Reporter | Session, survey task read denied (403) | Same session and task read routes |
| Repair crew | Session, survey task read denied (403) | Same session and task read routes |
| Drone operator | Login, profile, refresh, logout, task read, eligible-device read | Same session contract, `GET /api/v1/survey-tasks/{taskId}`, `GET /api/v1/survey-tasks/{taskId}/eligible-drones` |

The disposable run exercises the task access matrix and verifies 401 on unauthenticated profile and survey task reads. The harness rejects a reused actor ID, duplicate role email, an unexpected role in the profile response, a missing refresh token, and a session that remains authorized after logout.

## Video flow

The operator-only preflight reads the assigned task and scope, confirms the configured device is eligible, and checks the adopted REAL discovery route with an empty dataset ID. The mutation mode then follows the FE upload sequence.

1. Accept a `NEW_ASSIGNED` task using its ETag. A task already accepted by the configured operator starts at upload.
2. Create a `SURVEY_VIDEO` upload and, when configured, a separate `TELEMETRY` upload. Request, PUT and complete signed multipart parts, then poll each upload until `VERIFIED`.
3. Submit the dataset with task scope, recorded time and optional MP4/SRT pairing. Replay the same command with the same idempotency key and verify the dataset ID and source checksums.
4. Poll the project-scoped AI discovery route until a single work item for the clip reaches `Ingested`.
5. Read the processing job and telemetry. Telemetry positions represent the drone reference. A missing telemetry status is retained in the receipt.

AI output remains a proposal. PM candidate decisions and Crew inspection use the separate local acceptance flow in `tests/local-ai`. The Reporter defect submission journey and the actual Web/Mobile clients are outside this harness. Client compatibility for those flows remains `UNKNOWN` until exercised with their clients.

## Safety contract

- `dry-run` only reads local inputs. `auth` and `preflight` exercise sessions and read business data.
- `real` requires `E2E_ALLOW_MUTATION=true`, explicit clip paths, a configured storage origin, and an external file in `E2E_PROTECTED_PROJECT_IDS_FILE` containing every existing project ID that must stay read-only.
- With `E2E_DISPOSABLE=true`, the API and storage origins must use `http://127.0.0.1`. Shared environments require HTTPS.
- The protected-project file is read-only input and must be outside the repository. A target matching any protected ID is rejected before upload.
- Missing clip, target, storage, or protected-project input is reported as `NOT_RUN`. It is never treated as a successful real workflow.
- Reports contain statuses, correlation IDs, error codes, and ID prefixes only. They do not contain passwords, tokens, signed URLs, or full object keys.

## Evidence labels

`PUBLIC_E2E_VERIFIED` identifies assertions run through the configured HTTPS API. `DISPOSABLE_E2E_VERIFIED` identifies assertions run through the disposable loopback API. `BE_VERIFIED` identifies read-only preflight assertions. `AI_WORKER_VERIFIED` requires an ingested work item and successful job and telemetry readback. Local contract tests exercise mock HTTP or pure input validation.
