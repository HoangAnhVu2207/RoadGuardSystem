# RoadGuard Real-AI API Seed and E2E Design

## Intent

Create one API-driven seed and verification package for the Docker Desktop handoff. The package creates deterministic `DEMO` workflow records through the public BE API, runs the two real drone video/SRT uploads through the BE-to-AI worker path, and verifies the resulting role workflows before FE receives the stack.

The FE calls the BE origin only. The BE calls the private Docker service `http://ai:8999/`. The AI worker runs the pinned FT95 model and returns inference to the BE. SQL and MinIO are read back for verification, but the seed does not write them directly.

## Current evidence

- **CURRENT_VERIFIED** The supplied real-e2e state records four paired SRT jobs for `Duong Kenh Dong` and one paired SRT job for `Duong so 2`. PM readback and the cross-project `401`/`403`/`200` authorization matrix were recorded in `D:/RoadGuard-AI-LocalTest/server-demo-replay-20261124/real-e2e-20261125-linux-state.json`.
- **CURRENT_VERIFIED** The supplied retry state records a stopped attempt followed by one successful retry for `Duong so 2`.
- **CURRENT_VERIFIED** The Docker Desktop base compose at `D:/RoadGuard-AI-LocalTest/fe-local/compose.yaml` contains `roadguard/server-ai:goi4-e86255c`, keeps AI on the internal network, mounts the FT95 checkpoint at `/models/ft95.pt`, and exposes BE at `127.0.0.1:18081` and `127.0.0.1:18443`. The optional `compose.scenarios.yaml` enables the mock flag and is excluded from this design.
- **UNKNOWN** A complete live chain from PM review through Crew measurement, repair completion and Supervisor approval has not yet been demonstrated on the current server data.
- **NOT_RUN** FE Web/Mobile execution is outside this package.

## Goals

1. Run real BE-to-AI inference for one clip from each approved project and preserve the result lineage.
2. Create `DEMO` records through existing APIs for the role-specific screens and workflow states.
3. Exercise the same API calls used by the seed as tests for authorization, project scope, ETag/version, idempotency, retries and state transitions.
4. Produce a sanitized manifest and receipt that FE can use to locate each fixture without exposing credentials, signed URLs, private object keys or access tokens.
5. Support a resettable local Docker run and a separately invoked additive server run using the same API contracts and canonical seed manifest.

## Non-goals

- No AI mock is enabled for seed or acceptance.
- No direct SQL insertion is used to create workflow data.
- The AI service is not exposed as a FE endpoint.
- The package does not invent a missing production route or business threshold. An unavailable or unresolved route is recorded as `NOT_RUN`.
- FE browser, Android and iOS validation is not part of this package.
- Existing real project records, real AI results, accounts and server data are not reset or rewritten by the additive server mode.

## Canonical data set

### Identity and scope

The canonical account set has 2 Supervisors, 2 PMs, 2 Drone Operators, 4 Crews and 4 Reporters. Each project has one primary PM. Operators and Crews receive explicit project membership and assignments. Reporter records retain their source account and project scope. The FE handoff publishes a role-to-email catalog and a separate local credential file for the FE team. The credential file remains outside Git, Docker images, logs and receipts. Server credentials remain controlled separately and are not included in the local handoff.

The seed includes active accounts plus negative cases represented by inactive membership, expired assignment, wrong-project access and stale version. It does not require additional password-bearing accounts.

### Project foundations

Both projects receive or reuse current road section, route version, published segment set, geometry package, pavement layout, handover document and active warranty. The seed verifies that the PM, road, segment set and warranty all resolve to the same project before creating dependent records.

### Real AI lineage

The manifest identifies the approved real inputs. The Kênh Đông run uses `media/1123-part-00.mp4` with `telemetry/1123-part-00.gps-synced.srt`. The Đường số 2 run uses `media/11234.mp4` with `telemetry/11234.gps-synced.srt`. The driver creates a fresh logical Operator survey task for a clean local volume and creates or reuses the existing task in additive server mode. It uploads MP4 and SRT through multipart API routes, submits the paired dataset, waits for the BE processing job, and reads the result through BE routes. It records job, attempt, result, model version, telemetry status, result hash and frame file IDs. A detection count is observed from the model and is never padded to meet a fixture count. Missing telemetry remains an explicit missing-location state.

### DEMO workflow lineages

Additional deterministic records are labeled `DEMO` and remain separate from real AI detections.

- 20 Reporter reports wait for triage across the two projects. Additional examples cover resolved, rejected, missing-location and needs-information states.
- 20 DEMO defect lineages are Supervisor-approved for PM project scope. The approval history identifies the source report or field record and the acting Supervisor.
- 20 completed measurement tasks cover PM review accepted, needs supplement and rejected outcomes.
- 20 completed repair tasks cover Normal and FastTrack. The proposed split is 10 per mode, with 10 waiting for Supervisor approval and 10 already approved.
- Repair records include package, item, policy revision where required, assignment, due date, safety obligation, repair attempt, before photo, after photo and completion decision.
- Each Operator has 10 task examples covering planned, accepted, uploading, paired, missing telemetry, failed attempt, retry, overdue and completed states.
- Notifications include unread, read, acknowledged, pending outbox, delivered inbox, failed dispatch and retryable dispatch states. A local Mailpit receipt is used for email verification.
- Offline fixtures include registered device, active grant, snapshot, signed batch, durable import, idempotent replay, conflict and expired grant.
- Audit and idempotency records are read back for every mutation family. Stale ETag and cross-project requests are verified through the public API.

Images for Crew and repair evidence come from frames extracted from the real videos when the frame is suitable. Other required states use separately checksummed `DEMO` images. A `DEMO` image is never presented as an AI detection frame.

## API E2E sequence

1. Validate Docker services, real AI configuration, model checksum and `Anh02__MockEnabled=false`.
2. Authenticate every role from the private credential file and create a short-lived in-memory token map.
3. Verify project, membership, road, segment set, warranty and storage prerequisites.
4. Run the two real Operator upload flows and poll processing through BE routes.
5. Verify AI result readback, candidate visibility for the owning PM, denial for the wrong PM, Reporter and Crew, and Supervisor visibility.
6. Review real candidates with the documented PM/Supervisor routes where those routes are present in the current controller and contract inventory.
7. Create the DEMO Reporter, defect, measurement, repair, notification and offline lineages through APIs, using a new idempotency key for each logical mutation and the current ETag for each versioned command.
8. Re-read every created lineage as its intended role and as a wrong role/project. Verify durable state, response status, version and audit/outbox effects.
9. Produce a sanitized receipt containing counts, statuses, resource IDs allowed for FE navigation, model/result hashes, checksum references and `PASS`, `FAIL`, `NOT_RUN` or `UNKNOWN` per workflow.
10. Stop the local Docker stack only after receipt creation. The server additive mode is a separate invocation after backup and owner approval.

## Seed modes

### Local resettable mode

The local mode starts from the real-AI compose base, resets only the dedicated Docker volumes, provisions the private account catalog required by the local stack, seeds both named projects and writes a local manifest. It never loads the mock overlay. Re-running the same logical operation on an unchanged volume replays through idempotency and preserves fixture identities. A clean volume reset may generate different resource UUIDs. The manifest therefore uses stable logical keys and records the UUIDs returned by that run.

### Server additive mode

The server mode uses the same API driver, input assets, logical fixture keys and expected-state manifest with a different base URL and an additive project-scoped manifest. It first verifies the active release, backup/checksum and maintenance preconditions. It does not reset projects, existing AI results or shared storage. Account creation or modification is a separate owner-approved decision. A server run produces its own receipt with environment-specific UUIDs and is required before claiming that local fixtures are available to FE against the deployed server.

## Constraints and safety

- AI runs the pinned FT95 model on CPU unless an explicitly approved GPU configuration is added later.
- FE receives only the BE origin. AI has no public port.
- Secrets stay in a private account file and process memory. Logs redact passwords, bearer tokens, OTPs, signed URLs and full object keys.
- Real AI data and `DEMO` data have separate provenance labels.
- The canonical seed manifest is shared between local Docker and server additive runs. Environment-specific IDs are resolved into separate receipts.
- No data-writing test runs against a shared database without the server additive gate.
- Existing dirty files and unrelated project data are preserved.

## Acceptance evidence

- Docker health for BE, AI, SQL, MinIO and Mailpit.
- Two real video/SRT jobs complete through BE-to-AI and are readable through BE.
- Model checksum and real worker configuration are verified.
- API seed and API verification produce stable receipts on a clean local reset and a replayed local run.
- Every required role state has a fixture or is explicitly marked `NOT_RUN` with the route and reason.
- No mock flag, mock result or direct SQL fixture is used in the acceptance path.
- The handoff includes the compose command, BE endpoint, Mailpit endpoint, sanitized account-role map and fixture manifest.

## Approaches considered

- **A, selected** Real AI first, then API-created `DEMO` workflows. This keeps real inference evidence and tests the same API paths that FE will call.
- **B, rejected** Real AI followed by bulk SQL. It is faster, but it bypasses authorization, ETag, idempotency and outbox behavior.
- **C, deferred** Full API replay for every record including the real AI lineage. It provides strong API evidence but adds no value for the AI worker beyond the two real uploads and makes the seed unnecessarily fragile.

## Decisions still needed

The proposed fixture distribution is twenty records total for each major DEMO category, split ten per project. Each project has five Normal and five FastTrack repair tasks. Across both projects, ten repair tasks await Supervisor approval and ten are already approved. The owner may change this distribution before implementation without changing the architecture.

The local account bootstrap currently has a smaller sandbox catalog than the requested fourteen-account set. The local reset creates the fourteen accounts from the private catalog before API seeding, then hands the role catalog and local credentials to the FE team through the protected handoff path. The server additive mode will never create or modify accounts in this package.

The local reset will use the exact project names `Duong Kenh Dong` and `Duong so 2`. The additive server mode will resolve the existing project IDs by name and stop if ownership or project scope differs from the manifest.

The acceptance gate will require every route that exists in the current controller and contract inventory. A workflow with no adopted route or unresolved business rule will be recorded as `NOT_RUN` with its route and reason. Notification, offline and repair states follow this rule unless the owner explicitly makes them hard gates.
