# Isolated REAL vertical acceptance

`AiRealVerticalTests` is opt-in. It starts fixture-owned SQL Server and MinIO
containers and a loopback private Python worker. It ignores configured SQL
connection strings. Requirements include the cached disposable MinIO image,
actual pinned FT95 checkpoint, Python runtime and authorized sample paths in
the test source; this is deliberately a local acceptance harness.

After a coordinated integration build, run from the repository root:

```powershell
$env:ROADGUARD_RUN_REAL_AI = '1'
$env:ROADGUARD_REAL_REPO_ROOT = (Get-Location).Path
$env:TEMP = 'D:/RoadGuard-AI-LocalTest'
$env:TMP = 'D:/RoadGuard-AI-LocalTest'
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --no-restore --filter FullyQualifiedName~AiRealVerticalTests --nologo -v normal
```

Mutable worker data, BE spools, logs and successful `evidence.json` remain in a
unique `D:/RoadGuard-AI-LocalTest/be-vertical-*` directory. Tokens are removed
after owned-child shutdown. The test checks free space and test-root footprint;
the existing worker/coordinator budgets preserve 30 GiB free. Docker backing
storage must already be verified on D:. No disk relocation or unrelated cleanup
occurs. Containers are disposed by fixture ownership.

Checks include source hash, ffprobe metadata, actual FT95 inference over HTTP,
60 evidence frames, nonzero proposals, completed REAL BE job, durable MinIO
artifact bytes/hashes, fresh BE FileIds, zero automatic Defects, and replay
without a second job. This is neither deployment nor accuracy evaluation.

Set `ROADGUARD_REAL_PILOT_COUNT=5` for five distinct verified uploads of the
same authorized one-minute sample in one submitted dataset. The actual worker
performs five admissions/inferences, sequentially through the coordinator;
all five jobs and every artifact are checked and replayed. This demonstrates
repeated-sample throughput and durable draining, not five diverse samples or
queue-full pressure. Leave the variable unset for the one-video acceptance.
The current harness supplies an explicit deterministic geometry package fixture
matching the SQL dataset scope: geometry is MOCK_VERIFIED. Its inference,
HTTP transport, object storage, artifact verification and persistence are real.
Geometry SQL authorization is covered by separate focused tests, not this test.

CURRENT_VERIFIED one-video run: `vertical-real-run4.log`, 1 passed in 1m52.91s;
60 frames, 63 proposals, 62 artifacts / 42,418,272 bytes, completed REAL SQL job,
zero automatic Defects, no duplicate job on replay. Evidence is outside Git at
`D:/RoadGuard-AI-LocalTest/be-vertical-6f80ea8decbf400cbe3bc6740103ae5a/evidence.json`.

CURRENT_VERIFIED five-upload pilot: `vertical-real-five-run1.log`, 1 passed in
10m51.15s; coordinator processing/verification 592.489s. Each of five actual
worker runs completed with 60 frames and 63 proposals: totals 300 frames,
315 proposals, 310 artifacts / 212,091,360 bytes, five completed REAL SQL jobs,
zero automatic Defects. All five admission replays and repeat BE ticks created
no additional work. Geometry was the explicitly labeled package fixture.
Evidence: `D:/RoadGuard-AI-LocalTest/be-vertical-7516dceaa9984002aa80ddcc0c487f0b/evidence.json`.
Owned SQL/MinIO containers were disposed, owned Python children stopped and
token file removed; D: retained 70.785 GiB free after the run.

## AI Test Studio (local UI, 2026-11-23)

From the BE repository root in PowerShell:

```powershell
& ./tests/local-ai/start-studio.ps1
```

Open http://127.0.0.1:8765. The existing D: virtualenv, pinned FT95 weights
and FFmpeg paths are launcher parameters; no dependency installation is implied.
Mutable videos, frames, SQLite history and logs stay in
`D:/RoadGuard-AI-LocalTest/studio`. Limits: 2 GiB/video, 180 seconds,
1 FPS, 180 sampled frames, one running and three waiting; 40 GiB total studio
budget including worker reservations, with at least 30 GiB free disk required.
The launcher binds loopback only. Do not expose this test UI through Funnel.

Select a video, preview, run analysis, then open completed history. Filter the
six source labels and confidence; 0% includes frames with no detections.
Download the original worker JSON and extracted SRT. Bounding boxes are per-frame
proposals, not unique defects or approved repair work. `repair` means an existing
patch. Local external-SRT/operator notes never change the immutable worker result
or a BE telemetry revision. Cancel/retry act on worker jobs; retry creates a new
attempt and is available only after failure/interruption/cancellation.

To stop, first ensure no upload/inference is active. Inspect the process whose
ID is in `D:/RoadGuard-AI-LocalTest/studio/studio-server.pid`, confirm its command
line contains `src.api.test_studio:create_app_from_env`, then stop that PID with
`Stop-Process -Id <verified PID>`. The separate `studio.pid` can identify the
virtualenv launcher, not the actual listener. Restart with the same command;
completed history survives. Logs are `studio.err.log` and `studio.out.log`.

Optional `-BackendUrl http://127.0.0.1:<BE-port>` enables a restricted API
workbench against an already running local BE. It forwards a user bearer held
only in page memory, JSON, If-Match and Idempotency-Key. It does not automate
MinIO multipart file transfers or convert standalone results into BE candidates.
Without that option it explicitly displays BE unconfigured and disables sends.
Only configure a local database intended for the manual operations being sent.

CURRENT_VERIFIED: 19 focused studio/runtime tests passed; JS syntax passed.
Two real uploads of the same authorized one-minute sample completed, including
one through browser file selection and upload: each 60 frames, 63 detection
boxes, embedded SRT extracted. JPEG/SRT downloads matched descriptor hashes.
Desktop/narrow rendering, completed history after restart, filtering and the
unconfigured BE state were checked in the browser, with no warning/error console
entries observed. Pilot evidence and screenshot are in the D: studio root.
Two self-review passes and independent bridge/UI-contract review completed;
whole-root reservation accounting and dataset-read allowlist issues were fixed.
This UI session did not run a live local BE workbench request, deploy Ubuntu,
evaluate accuracy, or prove five diverse videos. Earlier real BE vertical-test
evidence above remains separate from standalone UI evidence.

## PM → Crew local workflow

The opt-in `PmCrewHost` keeps a separate SQL/MinIO fixture alive for manual testing.
It runs the production API in TestServer behind a loopback HTTP bridge, including
normal authentication and authorization. Two fixture accounts (PM and Crew) have
random credentials stored only in the ACL-restricted D: runtime directory. Studio
logs in server-side; no fixture password/token is sent to browser JavaScript.

```powershell
dotnet build tests/local-ai/PmCrewHost/PmCrewHost.csproj -p:RunAnalyzers=false
powershell -NoProfile -File tests/local-ai/start-pm-crew.ps1
# Wait for PM_CREW_READY in D:/RoadGuard-AI-LocalTest/pm-crew/host.out.log.
# Stop an existing Studio only after checking its actual PID/command and no active jobs.
powershell -NoProfile -File tests/local-ai/start-studio.ps1 -BackendUrl http://127.0.0.1:5087 -FixtureFile D:/RoadGuard-AI-LocalTest/pm-crew/fixture.json
```

Open `http://127.0.0.1:8765/`, tab **03 PM → Crew đo đạc**:

1. Select an ingested REAL candidate. Inspect its selected bounding box and
   time-aligned drone GPS from embedded SRT. PM chooses severity and keeps a new
   defect. Existing `repair` patches cannot be kept as new defects here.
2. PM chooses measurement type, deadline and instructions, then assigns the
   fixture's project Crew. Existing tasks can be selected after page reload.
3. Crew accepts and starts. Enter test measurements, separate ground position
   (or an explicit unknown-location reason), and a chainage within 0–100 m.
   Confirm the segment-marker checklist for this synthetic local geometry.
   Select a JPEG/PNG ≤20 MiB and upload; Studio uses normal BE multipart upload,
   MinIO part PUTs and BE verification before adding the evidence to a submission.
4. Submit, inspect BE readiness/missing reasons, then PM confirms, rejects as
   no defect, or requests supplementation. The API enforces task state, versions,
   role and scope; UI reports actual errors instead of simulating acceptance.

The geometry is a synthetic straight 100 m line persisted through the real geometry
repository. Measurements/photos entered for testing are not field survey evidence.
Drone GPS alone cannot prove ground damage position. No shared/Ubuntu database,
deployment, or production login credentials are used by this host.

Stop the fixture gracefully using `tests/local-ai/stop-pm-crew.ps1`. It verifies
the owned host PID, requests shutdown, and the host disposes its private worker,
SQL and MinIO. Data in those containers is disposable; a new host run reseeds a
new fixture and reruns the authorized minute sample. Stop/restart Studio with
the new fixture after any host restart. Never share `fixture.json`, worker token
or private logs. Do not expose either local listener through Funnel.

## Repeatable Playwright browser acceptance

Owner-requested test tooling is isolated from production dependencies. Playwright
1.65.2 is installed in the existing D: test venv; its Chromium/FFmpeg downloads and
pip cache are also under D:/RoadGuard-AI-LocalTest. The PM/Crew host and Studio
must already be running with the current disposable fixture. Install only when
setting up a new test environment:

```powershell
$env:PIP_CACHE_DIR='D:/RoadGuard-AI-LocalTest/pip-cache'
& 'D:/RoadGuard-AI-LocalTest/venv/Scripts/python.exe' -m pip install -r tests/local-ai/requirements-playwright.txt
$env:PLAYWRIGHT_BROWSERS_PATH='D:/RoadGuard-AI-LocalTest/playwright-browsers'
& 'D:/RoadGuard-AI-LocalTest/venv/Scripts/python.exe' -m playwright install chromium
```

Run `./tests/local-ai/run-studio-playwright.ps1` to watch Chromium, or add
`-Headless` for unattended acceptance. Each run selects an undecided non-repair
proposal, creates one real local defect/task, uploads a labelled test JPEG,
submits measurements, confirms as PM and rereads persisted Completed/READY state.
It checks Crew decision denial, missing-input guards, embedded SRT/frame rendering,
mobile width and browser errors. It does not launch fresh inference, measure model
accuracy, deploy Ubuntu or write to shared data. All 63 proposals were previously
produced by the actual local worker. When undecided proposals run out, restart
the disposable host and Studio as above; the test never resets the database.

Reports, screenshots, Playwright trace and WebM are kept in unique subdirectories
of D:/RoadGuard-AI-LocalTest/pm-crew/playwright. They can contain private sample
images/GPS, so keep them in that protected local folder. Passwords/tokens are not
read by the test or stored in browser state. A failed run may leave its partial
local task for inspection; no automatic cleanup or retry of mutations occurs.

CURRENT_VERIFIED 2026-11-23: headed Chromium acceptance passed 15 checkpoints,
including real MinIO VERIFIED → READY → CONFIRM → Completed and reload readback;
zero browser error entries. Report run 20261123T091124732529Z. First run stopped
at Playwright's default 5s assertion timeout during asynchronous photo verification;
the test now explicitly waits up to 60s (independent from action timeouts).
`pip check` reports no broken requirements. Two self-review passes covered local
scope/credentials/mutations and repeatability/artifact privacy/resource limits.

## Private worker runtime configuration details

CURRENT_VERIFIED 2026-11-23 PM/Crew extension: the actual browser workflow completed
PM keep-new → assign → Crew accept/start → verified MinIO photo → READY submission
→ PM CONFIRM → Completed. The rebuilt local host processed 63 proposals and read
60 embedded-SRT intervals. PM/Crew use distinct real authentication; Crew cannot
decide an AI candidate (403). Studio/runtime tests: 22 passed; SRT parser: 8;
candidate reader: 39. SQL provenance: 44; telemetry class: 17 plus the expanded
two-case retry/replay check. These supersede the earlier standalone-only scope
for tab 03. No Ubuntu deployment or field-accuracy validation was performed.

An already-completed example task remains visible. To try a new run, choose an
unhandled proposal from the dropdown (for example proposal 2), then follow the
four cards. Only a 100 m synthetic route is seeded; use chainage 0–100 m.
The fixture is disposable and remains available only while its host is running.
Evidence and screenshot: D:/RoadGuard-AI-LocalTest/pm-crew/.

The worker factory requires five absolute paths: `ROADGUARD_WORKER_DATA_ROOT`
(mutable isolated data), `ROADGUARD_WORKER_TOKEN_FILE` (private ASCII token),
`ROADGUARD_WORKER_CHECKPOINT` (pinned FT95 weights),
`ROADGUARD_WORKER_FFMPEG`, and `ROADGUARD_WORKER_FFPROBE` (executables).
Start only with explicitly selected settings:

```text
python -m uvicorn src.api.be_worker_runtime:create_app_from_env --factory --host 127.0.0.1 --port 8999
```

BE `AiRealWorker` options are disabled by default. Opt-in settings must match
the private endpoint, token file, released SQL model ID/checkpoint SHA256,
preprocessing/config versions and bounded sampling/resource limits. Configure
six distinct active released-model class mappings: alligator crack,
longitudinal crack, oblique crack, pothole, repair and transverse crack;
repair must map explicitly to `REPAIR_PATCH`. Local catalog seeds belong only
to disposable fixtures. No secrets or production catalog mutation are implied.
Ubuntu compose/provisioning and deployment were not performed.

The expanded HTTP regression suite additionally requires an explicitly
licensed embeddable Unicode TTF, e.g. `RG_TEST_UNICODE_FONT=C:/Windows/Fonts/arial.ttf`,
and `TEMP`/`TMP=D:/RoadGuard-AI-LocalTest/tmp` (create that directory first).
This font prerequisite belongs to HTTP export rendering, not FT95 inference;
the observed missing-font HTTP 500 was a test-environment failure.
