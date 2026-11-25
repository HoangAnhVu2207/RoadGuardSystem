# RoadGuard role-flow contract

This harness is an FE-shaped API check. It records evidence from the public BE and does not grant AI or business authority to the test runner.

## Role sessions

| Role | Client actions covered | Public routes used |
| --- | --- | --- |
| Supervisor | Login, profile, refresh, logout | `POST /api/v1/auth/login`, `GET /api/v1/profile`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout` |
| PM | Login, profile, refresh, logout | Same session contract |
| Reporter | Login, profile, refresh, logout | Same session contract |
| Repair crew | Login, profile, refresh, logout | Same session contract |
| Drone operator | Login, profile, refresh, logout, task read, eligible-device read | Same session contract, `GET /api/v1/survey-tasks/{taskId}`, `GET /api/v1/survey-tasks/{taskId}/eligible-drones` |

The harness rejects a reused actor ID, duplicate role email, an unexpected role in the profile response, a missing refresh token, and a session that remains authorized after logout.

## Video flow

The operator-only preflight reads the assigned task and scope, confirms the configured device is eligible, and checks the adopted REAL discovery route with an empty dataset ID. The mutation mode then follows the FE upload sequence.

1. Create a `SURVEY_VIDEO` upload with an idempotency key.
2. Request and PUT each signed multipart part to the configured storage origin.
3. Complete and poll verification until `VERIFIED`.
4. Submit a dataset with the task scope and recorded time.
5. Poll the project-scoped AI discovery route until the work is `Ingested`.
6. Read the processing job and telemetry, preserving the telemetry position meaning and missing-GPS status.

AI output remains a proposal. This harness does not approve defects, assign repair work, or complete repair actions.

## Safety contract

- `dry-run`, `auth`, and `preflight` do not mutate business data.
- `real` requires `E2E_ALLOW_MUTATION=true`, explicit clip paths, an HTTPS storage origin, and an external file in `E2E_PROTECTED_PROJECT_IDS_FILE` containing every existing project ID that must not receive writes.
- The protected-project file is read-only input and must be outside the repository. A target matching any protected ID is rejected before upload.
- Missing clip, target, storage, or protected-project input is reported as `NOT_RUN`. It is never treated as a successful real workflow.
- Reports contain statuses, correlation IDs, error codes, and ID prefixes only. They do not contain passwords, tokens, signed URLs, or full object keys.

## Evidence labels

`PUBLIC_E2E_VERIFIED` means the public BE was called. `BE_VERIFIED` means a BE contract assertion passed. Local contract tests are mock evidence. A live AI worker or FE/mobile client is not inferred from these tests and remains `NOT RUN` until executed on the approved target.
