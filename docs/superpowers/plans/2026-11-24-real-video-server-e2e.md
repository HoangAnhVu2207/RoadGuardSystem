# RoadGuard public real-video E2E

Owner-approved test scope, branch `anh`, based on `9198b39`. Covers Playwright API requests, two supplied MP4 files and redacted local evidence. The owner runs live commands and enters credentials. Server deployment, configuration, seed and business record cleanup are outside this package.

## Contract checkpoints

- `CURRENT_VERIFIED` in source. Login uses `POST /api/v1/auth/login` and profile uses `GET /api/v1/profile`. Each supplied identity must match its canonical role and profile. Exercise refresh and logout on the same isolated session.
- `CURRENT_VERIFIED` in source. Use two distinct, assigned, submittable BAND_V1 tasks. Read each task's project, scope and quoted ETag from the task endpoint. Confirm the selected active drone through eligible-drones. Require explicit recording timestamps.
- `CURRENT_VERIFIED` in source. Create a SURVEY_VIDEO upload, get each signed URL immediately before its exact-range PUT, then refresh the session ETag and complete with the returned ETags and SHA-256. Poll until VERIFIED before submitting. Validate the storage origin against the operator's explicit HTTPS allowlist and use no bearer token for PUT.
- `CURRENT_VERIFIED` in source. Dataset submission enqueues `ai.real.dataset_submitted.v1`. The server creates one REAL work item per video. Discover via `GET /api/v1/projects/{projectId}/ai-videos?datasetId={datasetId}`. Poll the matching source file's work state, query its job and telemetry, and verify durable dataset identity. The runner never creates an extra legacy processing job. Confirmed zero detections are valid even when the legacy result ID is absent.
- `UNKNOWN` on the running server. The last confirmed release predates this REAL integration. The server deployment config historically signed uploads against LAN MinIO. Public HTTPS health neither proves the current release has REAL discovery nor establishes external reachability of the signed storage endpoint.

## Tasks

1. Add local contract tests for the mutation gate, explicit storage origin, byte ranges and REAL terminal states.
2. Add dedicated Playwright API runner with offline validation, per-role login/refresh/logout, both-task preflight, and two sequential real video workflows. Fail closed on incompatible routes, source mismatches and terminal errors. Record only controlled fields, with no traces or request logs.
3. Run dependency installation, typecheck, mocked contract checks and offline checks against both supplied videos. Review contract alignment and operational scope/redaction separately.
4. Give the owner PowerShell for authenticated read-only preflight and then the guarded video run. Review JSON evidence and classify each live stage independently.

## Local review and evidence

- Review against current code covered the login-to-profile role mapping, assigned-task preconditions, upload ETag freshness, dataset source proof and the REAL discovery and telemetry states. A valid readback may have no user telemetry revision.
- Review of operational scope covered separate sessions, HTTPS and redirect handling, status-only output, one video at a time, no automatic retries of writes and redacted results.
- Local verification on 2026-11-24: typecheck passed, seven real-runner contract tests passed, and both MP4 files passed offline validation. The older generic harness ran three local tests, with its 21 live cases skipped.
- Public HTTPS verification on 2026-11-24: `GET /health` returned 200 and anonymous `GET /api/v1/profile` returned 401. A request to the project REAL discovery route with validation-only empty IDs returned 404 `not_found` before login. Hold video writes until the active release and authenticated preflight confirm that the route is available. Live login, upload and inference still require owner execution.
- Playwright API requests model the FE HTTP contract. The actual FE browser UI and its CORS handling are separate checks.
