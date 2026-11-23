# BE–AI execution ledger — 2026-11-20

Assigned spec: planning/development/BE-AI-INTEGRATION-20261119.md.
BE branch anh, base/HEAD 4934103d125be1caca4c6a588eed3768c9cafee4.
AI gitlink c689e544400fe326683c1c7f45115b3f7ec01daf plus uncommitted worker changes.
No commit/push, production migration, shared SQL write or deployment. Local SQL writes use fixture-owned disposable containers.

TARGET_CONFIRMED: Codex/Anh owns the whole BE–AI slice; Huy pauses these files. Preserve one writer/file. Six distinct labels; repair is an existing patch. PM decides proposals; operator submits video/supplements drone reference; crew retains ground-location authority.

## Small tasks

| ID | Task | State |
| --- | --- | --- |
| 01 | Fresh develop and pinned AI main | DONE; old supplied checkout preserved |
| 02 | Trace dataset/outbox/attempt/files/class consumers | DONE; multiple video inputs retained |
| 03 | Coordinates and actual-PTS timestamps | DONE; decimal edge regression included |
| 04 | Embedded SRT extraction and validation | DONE; real sample plus negative tests |
| 05 | Storage/checkpoint readiness | DONE; D-backed Docker, 40 GiB budget / 30 GiB floor |
| 06 | Wire schema and six-label mapping | DONE; fixture-owned released catalog |
| 07 | Work/artifact/telemetry/outcome persistence | VERIFIED; additive migrations isolated SQL only |
| 08 | Strict private worker HTTP | VERIFIED focused Python tests |
| 09 | SQLite replay/restart/one+three capacity | VERIFIED controlled failure/queue tests |
| 10 | Bounded verified binary streams | VERIFIED actual video/artifacts |
| 11 | FT95 and timeline artifacts | VERIFIED full one-minute video |
| 12 | Automatic per-video durable dispatch | VERIFIED actual worker plus SQL tests |
| 13 | Registration/scoped frames/PM facts | VERIFIED SQL/unit/HTTP and actual artifact checks |
| 14 | Operator SRT/estimate and discovery | VERIFIED unit/HTTP and final SQL suite |
| 15 | One-video BE/AI/MinIO/SQL acceptance | PASS; geometry package explicitly mocked |
| 16 | Five-video pilot | PASS; five distinct uploads of one repeated sample, not accuracy/diversity |
| 17 | Reviews/Knowledge/rollout proposal | DONE; local notes synchronized by SHA256, existing index not rebuilt |

## Execution rulings

- Ruling: private HTTP/JSON plus streamed bytes, BE pull-and-register — reuse installed stacks; no measured gRPC advantage. Cost if wrong: later transport migration.
- Ruling: explicit ai.real.dataset_submitted.v1 intent — enabling REAL never sweeps historical datasets. One work identity per dataset/video/config; retry uses a new attempt with immutable terminal outcome history.
- Ruling: four additive tables; do not mutate append-only ProcessingAttempt. No retention deletion enabled. Only fixture-owned catalog seeds were added locally.
- Ruling: six distinct active type codes and released model are dispatch readiness requirements; repair maps to REPAIR_PATCH. No implicit mock CRACK/POTHOLE mapping.
- Ruling: worker v1 extracts embedded SRT; BE resolves verified paired SRT with committed offset. External revisions > paired > embedded > estimate. Estimates leave measuredTelemetryStatus MISSING_TELEMETRY. No inference rerun or frozen source/detection/crew record rewrite.
- Ruling: require authorized, current, unambiguous geometry before freezing an immutable manifest. GEOMETRY_NOT_READY leaves source eligible; skip its dataset during this preparation scan so later ready datasets can proceed. No invented per-video assignment across route/set pairs. A future multi-route assignment contract is separate scope.
- Ruling: PM source version captures telemetry head/measured revision/paired+embedded identities. Concurrent correction invalidates stale PM review; accepted decision snapshots stay unchanged.
- Ruling: five logical uploads reuse the authorized sample for durable draining. Cost: five diverse scenes/detector accuracy are not verified.

## Executed evidence

CURRENT_VERIFIED logs under D:/RoadGuard-AI-LocalTest (outside Git):

- be-unit-final5.log: 143 unit passed, zero failed/skipped, including geometry readiness, scanning past blocked datasets and recovery after geometry becomes ready.
- be-http-final.log: 17 HTTP passed, zero failed/skipped. Real HTTP/authentication/SQL-backed login; telemetry repository/storage explicitly fake. Covers 401/403/409/412/422/428, ETag, replay and discovery.
- be-http-final3.log: expanded regression 19 passed, zero failed/skipped, in 2m11s. Includes candidate read, existing AI flow and dossier/export admission with the required licensed Unicode test font. Earlier HTTP500 was a missing RG_TEST_UNICODE_FONT test prerequisite, reproduced and isolated; the test now validates its font at setup. No production service fix was needed. Candidate geometry/mock inference fixtures remain explicitly separate from the real FT95 pilot.
- be-sql-final3.log: 48 SQL passed, zero failed/skipped, in 7m52s. Earlier fixture UPDATE of append-only FileScope was corrected to seed invalid scope initially; no trigger disabled. Cases cover current-source fencing, retry/outcome history, auth, paired offsets, PM snapshots and migration up/down.
- python-final.log: 87 total, 86 passed, one Windows symlink-permission skip. Includes actual synthetic FFmpeg nonzero-start-time proof; error/restart/queue tests use controlled fixtures.
- vertical-real-run4.log: one actual local video passed in 1m52.91s, coordinator portion84.385s. CPU FT95, 60 frames, 63 proposals, 62 artifacts / 42,418,272 bytes. Every durable MinIO artifact rehashed, BE FileIds distinct worker UUIDs, SQL REAL Ingested/Completed, zero automatic Defects and stable replay. Geometry package is explicitly MOCK_VERIFIED; HTTP/inference/MinIO/SQL are actual. Evidence: be-vertical-6f80ea8decbf400cbe3bc6740103ae5a/evidence.json.
- vertical-real-five-run1.log: five-video pilot PASS, one acceptance test in 10m49s (runner total 10m51s), coordinator portion592.489s. Five distinct verified uploads of the same authorized one-minute clip; 300 frames, 315 proposals, 310 registered artifacts / 212,091,360 bytes. Every artifact verified against MinIO; zero automatic Defects and replay creates no new job. Geometry is MOCK_VERIFIED; FT95/HTTP/MinIO/SQL are actual. Evidence: be-vertical-7516dceaa9984002aa80ddcc0c487f0b/evidence.json. Fixture-owned SQL/MinIO containers disposed after acceptance.
- Earlier standalone runner189.296s uses same clip; this is not a controlled comparative performance benchmark.

Input SHA256: 8f8b0c2dc1bf5fb11579323c64281a8897cb8268b26667687ecffb4f25211932.
FT95 SHA256: bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0.
Embedded SRT60 valid cues/9405bytes; no GPS values saved into Knowledge. Original MP4/model unchanged; outputs/secrets remain outside Git.

Real failures corrected: missing test-readiness bearer; Windows native ffprobe reopening delete-pending spool; independent decimal conversion creating bbox edge1.0000000000000001. Convert normalized right/bottom before subtracting decimal origins; genuinely invalid edges still rejected.

## Reviews and remaining deployment boundary

Self-review1: actor/project/assignment, source+attempt fencing, append-only history, atomicity/idempotency, measured-vs-estimated telemetry, no automatic approval. Corrected uploaded paired-SRT storage reader and immutable outcome history.
Self-review2: producer/consumer JSON, spool lifecycle, decimal bounds, disabled startup, frame authorization, class map and migration scope. Preserve baseline14 explicit owned-FK Restrict annotations in generated snapshot/designers; no unrelated FK DDL.
Independent agent review found missing REAL decision lock, telemetry snapshot, paired resolver/discovery and geometry readiness/starvation; fixes passed the final focused checks above. GitHub/ChatGPT external diff review NOT REQUESTED; no PR created.

Final independent re-review found no remaining actionable issue in the corrected slice. Integration build be-integration-final5-build.log succeeded with zero errors (665 analyzer/compiler warnings reported); ef-model-final.log reports no pending model changes. Final API-only build candidate-dossier-final-build.log succeeded with zero errors/warnings. git diff --check passed in BE and AI repositories.

Knowledge: spec, plan, execution ledger and both wire contracts copied to the LOCAL_ONLY RoadGuardSystem-Development notes and SHA256 compared. Business decisions were retrieved from the copies (six labels/repair, PM authority, resource floor and telemetry precedence). Existing SQLite catalog was not rebuilt; notes are local supplements, not an indexed or team-shared update. No videos, model weights, actual GPS, credentials or runtime artifacts copied. Test-owned worker token removed, children stopped and containers disposed; successful evidence retained outside Git. D: free after the pilot was 70.785 GiB.

PROPOSED separate Ubuntu rollout: inspect actual GPU/driver and unused disk without formatting; verify mount/capacity; pin images/model; provision private mounted token; review migration SQL and backup/restore; enable only private worker network after readiness. Rollback disables REAL dispatch/worker and preserves accepted results/revisions; never blindly migrate down over accepted business data. No Ubuntu changes occurred.

NOT VERIFIED: GPU/CUDA, actual Ubuntu budgets, transport p50/p95 or memory benchmarks, five diverse scenes, detector accuracy, production FE and full company-role workflows. Local acceptance does not certify these.

## Fresh continuation verification — 2026-11-23

CURRENT_VERIFIED on branch `anh`, root HEAD `86e9f4e25f2e310f940e45c5cf60efd8d875a61a`, AI gitlink `c689e544400fe326683c1c7f45115b3f7ec01daf`; pre-existing dirty paths were preserved. The AI change in `src/api/mvp_video.py` releases loopback service leases during ASGI shutdown. A root-cause regression in `src/inference/video_models.py` was also corrected: the trusted model directory is resolved before comparing it with resolved children, avoiding Windows 8.3-path mismatches. The legacy mapping test now creates a temporary checkpoint fixture and still asserts fail-closed class validation.

Fresh commands and results:

- `AI`: `python -m unittest discover -s tests -p 'test_be_worker*.py' -v` → 71 run, 69 passed, 2 skipped (host symlink permission), exit 0.
- `AI`: browser trio with `ROADGUARD_NODE_MODULES=D:/RoadGuard-AI-LocalTest/node_modules` and `PLAYWRIGHT_BROWSERS_PATH=D:/RoadGuard-AI-LocalTest/playwright-browsers` → 3/3 passed in 6.288s, exit 0.
- `AI`: full suite with the same local Playwright paths → 206 run, 206 passed, 2 skipped, exit 0.
- `BE`: `dotnet build RoadGuardSystem.slnx --nologo -p:RunAnalyzers=false -p:UseSharedCompilation=false` → succeeded, 0 warnings, 0 errors.
- `BE`: focused API tests `AiVideoTelemetryHttpTests|ServerDeploymentTests|UploadWorkerCompositionTests` → 33/33 passed, 0 skipped.
- `BE`: processing unit lane → 226/226 passed, 0 skipped.
- `git diff --check` → no whitespace errors (Git only reports the existing LF/CRLF normalization notice for `.dockerignore`).

The first parallel build attempt was invalidated by a compiler output lock; the sequential build with shared compilation disabled is the authoritative build evidence. The broad API audit process was stopped after it hung without producing a new result; it is recorded as NOT RUN/INCONCLUSIVE rather than pass. External/deployment evidence remains NOT VERIFIED: no Ubuntu deploy, GPU/CUDA check, public transport benchmark, shared DB write or production migration was performed.

Self-review pass 1 (fresh): lease release is limited to orderly local ASGI shutdown; worker/model changes preserve private scope, fail-closed checkpoint/class validation, six-label FT95 mapping and no automatic PM approval. The model-directory fix changes only canonical path comparison and does not widen the configured root. Self-review pass 2 (fresh): changed AI files stay within the existing local-video/worker slice; the tests use temporary fixtures; no contract, migration, dependency, deployment or shared-writer boundary was broadened. No in-scope findings remain.
