# Real-video public server E2E design

Status: APPROVED DESIGN. Writer/branch: Codex / `anh`. Base SHA: `c89071242dd565b10274977b524f8c6164704b1f`. Scope is the Playwright server E2E harness and its documentation. Video files remain outside the repository. No BE, DbContext, migration, deployment or seed writer is included.

## Goal

Exercise the deployed RoadGuard API as a real FE-shaped client using the supplied videos:

- `D:\Project BE\Videotest\1123.mp4`
- `D:\Project BE\Videotest\11234.mp4`

For each video, the test authenticates as a Drone Operator, uploads the video through the BE-managed multipart workflow, submits a survey dataset, and reads the durable result of the REAL processing job that the server automatically creates. The run also performs role login checks so the report distinguishes authentication, authorization, upload, dataset and AI-worker evidence.

## Authority and transport

The existing server E2E design, current controllers/DTOs, and `RoadGuardSystem.API.http` are the contract sources. The test uses Playwright `APIRequestContext` for application routes. It uses the signed part URLs returned by `POST /api/v1/uploads/{uploadId}/part-urls` for the client PUTs; this models FE upload behavior and is not a MinIO administration call. The test never connects to SQL, MinIO admin APIs, or the private AI endpoint.

The public base URL is supplied through `E2E_BASE_URL` and must be HTTPS. The expected demo URL is `https://roadguardsystem.tail79aea8.ts.net`, but the runner has no hard-coded production fallback. All API paths use `/api/v1`.

## Authentication and role use

The real run requires local-only credentials for the roles that are available. The required role for the video flow is `OPERATOR`; `PM`, `SUPERVISOR`, `REPORTER`, and `CREW` are used for login/scope smoke when credentials are supplied. Each role calls `POST /api/v1/auth/login` in its own API client and receives an access token; tokens are held in memory only. The runner must never print passwords, tokens, refresh tokens, or complete response bodies.

For each supplied role, the test verifies:

1. login returns a successful response and an access token;
2. an authenticated profile or role-scoped read route is reachable;
3. the token cannot be silently reused for another configured role;
4. refresh and logout are exercised only when those routes and credentials are present in the deployed inventory.

Missing optional role credentials are reported as `NOT_VERIFIED`, while missing Operator credentials block the real-video slice. Anonymous `/health` and anonymous business access remain part of the read-only preflight.

## Video workflow

Each video is processed sequentially with a unique `E2E-{UTC}-{runId}-{video}` marker.

1. Validate the local file exists, is a regular file, is `video/mp4`, and compute its lowercase SHA-256 without copying it.
2. Use a configured project and assigned survey task per clip to create an upload session:
   `purpose=SURVEY_VIDEO`, the project and survey-task IDs, file name, `mediaType=video/mp4`, size, and checksum. Every mutating request has a unique `Idempotency-Key`.
3. Read the server `partSizeBytes`. Request each part URL shortly before streaming the exact file range. Require an explicit trusted HTTPS storage origin, preserve the signed URL, send no BE bearer, and capture the PUT ETag.
4. Read the session's current quoted ETag after signing the parts. Complete with that `If-Match`, all part ETags and SHA-256. Poll the upload until `VERIFIED` or failure.
5. Read the current task scope and ETag. Submit to a submittable assigned task with `videoFileIds`, an empty telemetry file list, the actual `recordedAt`, an eligible `deviceId`, and the observed route/segment band scope. Read back the dataset and source file identity.
6. Dataset submission emits `ai.real.dataset_submitted.v1`. Poll `GET /api/v1/projects/{projectId}/ai-videos?datasetId={datasetId}` for that exact file. Require `Ingested`, a `SUCCEEDED` job, and scoped telemetry readback tied to the same attempt.
7. Record the telemetry state and interval count as drone reference evidence. Preserve an available legacy job result ID as a reference. Per-detection labels and PM decisions require separate evidence.

The public readback must show the durable ingested REAL work, its successful job and the matching telemetry view before the report claims AI-worker evidence. A zero-detection result can legitimately have no legacy result ID. Missing measured telemetry and an empty revision ID can both occur on a successful run.

## Dry run and real run

`dry-run` verifies both MP4 headers, sizes and SHA-256 hashes offline. It validates configured origins and any supplied task configuration. `auth` exercises only health, login, profile, refresh and logout. `preflight` also checks both survey tasks, eligible drones and the presence of the REAL discovery route. `real` requires `E2E_ALLOW_MUTATION=true`, Operator credentials, two distinct projects and tasks, active drone IDs, recording timestamps, an explicit storage origin and a writable results directory.

The run uses bounded upload, request and async polling timeouts. It does not retry a failed upload completion or processing job with a new identity automatically. A rerun uses a new run marker and leaves prior owned records for supported API cleanup or explicit demo retention.

## Evidence and redaction

The result is a redacted JSON record containing the base URL host, expected release if supplied, run marker, per-role login status, per-video file size/checksum prefix, upload/dataset/job ID prefixes, HTTP status, correlation ID, state transitions, elapsed times and terminal error codes. It excludes credentials, bearer/refresh tokens, signed URLs, connection strings, local file paths and raw response bodies.

Each case is labelled `PASS`, `FAIL`, or `NOT_VERIFIED` and one of `BE_VERIFIED`, `AI_WORKER_VERIFIED`, `PUBLIC_E2E_VERIFIED`, or `MOCK_VERIFIED`. Public health alone never proves AI processing. If the server does not expose a required readback field or adopted route, the report records `NOT_VERIFIED` rather than inventing a contract.

## Acceptance criteria

- Both supplied videos pass local validation and are uploaded through the public BE-managed signed-part workflow.
- Operator login, token isolation and required role scope are verified without exposing secrets.
- Each verified file is attached to the configured survey task and a durable dataset is read back.
- Each dataset queues a REAL processing job and the job reaches a terminal state or reports a classified, reproducible failure.
- Durable BE readback contains the processing state and any available artifact/result/location evidence.
- No direct SQL, MinIO-admin, private AI or SSH call is made by the runner.
- The report contains no password, token or signed URL and remains usable as FE handoff evidence.

## Required local inputs

Use the local PowerShell environment for `E2E_BASE_URL`, `E2E_STORAGE_ORIGIN`, and credentials collected via `Get-Credential`. For each `E2E_KENH_DONG` and `E2E_DUONG_SO_2` prefix set `_PROJECT_ID`, `_TASK_ID`, `_DEVICE_ID` and `_RECORDED_AT`. Optional `_VIDEO` paths override the two supplied absolute paths. Server configuration selects the model.

