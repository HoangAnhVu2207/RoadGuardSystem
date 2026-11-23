# Real-video public server E2E design

Status: APPROVED DESIGN. Writer/branch: Codex / `anh`. Base SHA: `c89071242dd565b10274977b524f8c6164704b1f`. Scope is the Playwright server E2E harness and its documentation. Video files remain outside the repository. No BE, DbContext, migration, deployment or seed writer is included.

## Goal

Exercise the deployed RoadGuard API as a real FE-shaped client using the supplied videos:

- `D:\Project BE\Videotest\1123.mp4`
- `D:\Project BE\Videotest\11234.mp4`

For each video, the test authenticates as a Drone Operator, uploads the video through the BE-managed multipart workflow, submits a survey dataset, queues a REAL processing job, and reads the durable processing result. The run also performs role login checks so the report distinguishes authentication, authorization, upload, dataset and AI-worker evidence.

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
2. Use configured `E2E_PROJECT_ID`, `E2E_SURVEY_TASK_ID`, and scope IDs (`E2E_ROUTE_VERSION_ID`, `E2E_SEGMENT_SET_ID`, `E2E_SEGMENT_ID`) to create an upload session:
   `purpose=SURVEY_VIDEO`, the project and survey-task IDs, file name, `mediaType=video/mp4`, size, and checksum. Every mutating request has a unique `Idempotency-Key`.
3. Read the server `partSizeBytes`, request all part URLs, stream each file range to its signed URL with the returned part number, content length and the ETag returned by the PUT.
4. Complete the upload with the ETags, checksum and the quoted `If-Match` returned by the session. Poll the session until `VERIFIED` or a terminal failure.
5. Submit a dataset to the same survey task with the verified `videoFileIds`, an empty telemetry list unless a telemetry file is explicitly configured, `recordedAt`, optional `deviceId`, and the configured route/segment scope. Poll/read back the created dataset.
6. Create a processing job with the dataset ID, identical scope, configured `modelVersionId`, `preprocessingVersion`, `configVersion`, and `mode=REAL`. Poll `GET /api/v1/processing-jobs/{jobId}` until a terminal state.
7. Read any result/artifact, SRT/telemetry and location fields exposed by the BE response. Preserve the six FT95 labels independently if detections are returned; `repair` remains a separate label and is not treated as a new defect.

The runner does not call the AI callback route. A REAL job is considered AI-worker evidence only when the public BE readback shows a terminal state and durable result/artifact references. A zero-detection result is valid; missing worker, timeout, data failure or unavailable model is a recorded failure/unknown.

## Dry run and real run

`dry-run` validates both files, checksums, required environment names, numeric limits, and the server URL shape without making a mutating request. `real` requires `E2E_ALLOW_MUTATION=true`, the Operator credentials, all project/task/scope/model IDs, and a writable local results directory. The runner refuses placeholder values and refuses to start if either file is missing.

The run uses bounded upload, request and async polling timeouts. It does not retry a failed upload completion or processing job with a new identity automatically. A rerun uses a new run marker and leaves prior owned records for supported API cleanup or explicit demo retention.

## Evidence and redaction

The result is a redacted JSON/JUnit record containing the base URL host, commit if supplied, run marker, per-role login status, per-video file size/checksum prefix, upload/dataset/job ID prefixes, HTTP status, correlation ID, state transitions, elapsed times and terminal error codes. It excludes credentials, bearer/refresh tokens, signed URLs, connection strings, full file names when configured private, and raw response bodies.

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

The user supplies these only in the local PowerShell environment or ignored `.env` file: `E2E_BASE_URL`, Operator credentials, optional credentials for the other four roles, `E2E_PROJECT_ID`, `E2E_SURVEY_TASK_ID`, `E2E_ROUTE_VERSION_ID`, `E2E_SEGMENT_SET_ID`, `E2E_SEGMENT_ID`, `E2E_MODEL_VERSION_ID`, and optional `E2E_DEVICE_ID`, `E2E_RECORDED_AT`, `E2E_PREPROCESSING_VERSION`, `E2E_CONFIG_VERSION`. The two video paths are configurable but default to the two supplied absolute paths. No credential or secret is committed.

