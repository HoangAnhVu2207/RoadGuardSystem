# ANH-01 fictional demo assets

## ANH-02 continuation and disposable runtime (2026-10-03)

CURRENT_VERIFIED: the existing backend HTTP/SQL fixtures exercise synthetic
video upload/dataset/AI mock, private Reporter intake, supported reporting,
frozen dossier export and fail-closed retention. This CLI continuation is
executed against a task-owned Linux API, SQL database and real MinIO; full
prepare/survey/anh02 and same-state reruns are **CURRENT_VERIFIED** locally. No Huy approval, conclusion,
matching result or complete inventory is seeded. Reporter intake stays
UNASSIGNED/private; project reporting does not count it as project-attributed.

Use the same local demo state and existing `prepare`/`survey` phases below.
`anh02` creates a separate PERIODIC survey task/dataset using the exact ANH-02 synthetic
video (the ANH-01 video is deliberately not accepted as that AI fixture), calls
VIDEO_ANALYSIS, uploads the synthetic PNG privately as Reporter, creates/replays
intake, captures project reporting, downloads PDF/ZIP and admits a project hold
and evaluation. Missing Huy inventory stays WAITING/BLOCKED; basis confirmation
and physical deletion are not attempted. Operator membership, ACTIVE device,
RELEASED model and active CRACK catalog entry must already exist via their
owners. No registry/model/catalog provisioning is hidden in the script.

Additional prerequisites: `ANH01_REPORTER_TOKEN`; enabled Development/Test
`Anh02__MockEnabled=true`, `Anh02__WorkersEnabled=true`, the existing upload
verification worker; deployed tiny fixtures in `Anh02__FixtureDirectory`.
Production never enables the AI mock. Current geometry package ETag is read
from HTTP and retained for command replay; no WGS84/GPX transform is guessed.
The metric-coordinate demo does not close the CRS/WGS84/GPX gate.

```powershell
python tools/demo/anh01/setup_demo.py --base-url http://localhost:5000 --isolated-local --phase anh02 --operator-id <existing-Operator-GUID> --device-id <ACTIVE-device-GUID> --model-version-id <RELEASED-model-GUID> --state <same-local-state.json>
python -m unittest discover -s tools/demo/anh01 -p test_setup_demo.py -v
```

The optional `storage.compose.yml` starts only disposable loopback MinIO, with
credentials supplied through `ANH02_MINIO_USER`/`ANH02_MINIO_PASSWORD` and an
explicit separate Compose project, e.g. `roadguard-anh02-disposable`.
`ANH02_MINIO_IMAGE` is required and must identify a verified official or
fixed-revision source-built MinIO image; there is no removed-tag default. Create a fresh private
bucket using that disposable instance's console/API and point `MinioStorage__*`
only to it. Do not reuse a shared bucket. Inspect configuration without logging
interpolated credentials (`docker compose ... config --quiet`). Clean up only
that project/resources you created; root SQL Compose configuration is separate.
Current task built and ran the official source at commit
`7aac2a2c5b7c882e68c1ce017d8256be2feea27f` after registry investigation.
See runtime acceptance below and the existing ANH-02 summary for provenance.

Large-byte acceptance is a separate command requiring an **actual valid MP4
of exactly 8,589,934,592 bytes**, existing accepted task/current Operator and a
new local download file. It hashes, PUTs and downloads with 64 KiB buffers,
requests signed URLs in batches of 64 and preserves original complete body/
If-Match before network submission. It refuses external signed PUT hosts,
records no credentials/signed URLs, and refuses a small file as 8 GiB evidence.
Reserve enough disk for source, downloaded copy and server verification/export
spools. Partial local downloads are retained for inspection on failure.

```powershell
python tools/demo/anh01/setup_demo.py --base-url http://localhost:5000 --isolated-local --phase large --project-id <demo-project-GUID> --task-id <accepted-task-GUID> --video-file <actual-8GiB.mp4> --download-file <new-local-download.mp4> --state <separate-large-state.json>
```

This 8 GiB command is **CURRENT_VERIFIED** locally with actual MP4 bytes,
real multipart PUT/verification and protected download. Both files are
8,589,934,592 bytes and SHA-256
`08ba3133e6dc0659a55862e692167b84e593aaa46f3725633dd41502909ea7e5`.
The measured run does not define an accepted performance threshold.

PDF runtime uses existing PDFsharp 6.2.3; no font/package is bundled or upgraded.
Mount a readable Unicode TTF font with verified embedding/distribution rights
and configure `Anh02__Export__UnicodeFontPath` to its target-host absolute path.
An absent/unreadable/different cached font fails `export_font_unavailable`;
restart the process when changing font bytes (PDFsharp has a global cache).
The font is bounded to 16 MiB. Tests require `ANH02_TEST_FONT_PATH` explicitly;
no personal-machine fallback is committed. CI config supplies DejaVu Sans via
the runner's `fonts-dejavu-core` package. Local Linux/container rendering with
official DejaVu 2.37, its license and fsType=0 is CURRENT_VERIFIED; missing and
non-root unreadable fonts fail even with a warm cache. Hosted CI and the
actual deployment target remain **NOT RUN**.

Postman retains existing requests/variables. Dossier admission stores
`anh02AdmittedSnapshotHash`; poll/manifest assert immutable identity, explicit
Reporter missing section, strong ETag and 30-day expiry after success. Use a new
`anh02DossierKey` when changing PDF/ZIP/filter; poll until SUCCEEDED before
protected download. Postman HTTP runner remains NOT RUN. Live storage is
CURRENT_VERIFIED at the runtime checkpoint below.

All facts and media are fictional. The five-page `dossier-demo.pdf` carries
`DỮ LIỆU DEMO — KHÔNG CÓ GIÁ TRỊ PHÁP LÝ` on every page. No real identities,
stamps, signatures, construction records or field footage are included.

`facts.json` defines the 200 m EPSG:32648 route, 7 m road / 9 m survey width,
station origin 1000 m, two 100 m segments, handover/warranty dates and survey
schedule. Runtime entity GUIDs come from APIs; demo command IDs use a fixed
UUID namespace. `asset-manifest.json` records exact bytes and SHA-256 hashes.

The 640x360 H.264 MP4 is 4 seconds at 10 fps. Seconds 0-2 show a synthetic
surface; seconds 2-4 have an intentional obstruction. The SRT contains
synthetic EPSG:32648 overlays for the same route. These are fixture evidence,
not real position/coverage measurements. PASS/UNKNOWN/FAIL are explicitly
fictional PM review scenarios under `pm-evidence-review.v1`.

## Research provenance

CURRENT_VERIFIED on 2026-10-02: accessed the [official publication](https://vanban.chinhphu.vn/?pageid=27160&docid=202585)
and downloaded/visually inspected printed pages 34-36 of the [signed decree](https://datafiles.chinhphu.vn/cpp/files/vbpq/2021/01/nd06.signed.pdf).
Articles 26-29 inform document grouping and handover/warranty terminology.
Only structure is borrowed; fictional dates and dimensions are not presented
as legal requirements. This is not an opinion on current legal applicability.

## Local fixture producer

Read `tools/demo/anh01/setup_demo.py` before running. It calls production HTTP
APIs only and refuses non-loopback base URLs. `--isolated-local` explicitly
attests that the local API uses an isolated DB and object store. Localhost
alone does not establish isolation. It never resets a DB, writes SQL, marks
files VERIFIED, provisions devices, or directly seeds assessment/baseline.

Prerequisites: API with ANH-01 schema and verification worker; reachable
configured object store; existing active Supervisor/PM/Operator users;
Operator current membership in the demo project; ACTIVE shared DroneDevice
registry ID. Supply bearer tokens through environment variables
`ANH01_SUPERVISOR_TOKEN`, `ANH01_PM_TOKEN`, `ANH01_OPERATOR_TOKEN`; do not commit
them or pass them on the command line. The local state file stores IDs and
results only, defaults to the OS temporary directory, and must be retained
for resuming the same demo.

```powershell
python tools/demo/anh01/setup_demo.py --validate-only
python tools/demo/anh01/setup_demo.py --base-url http://localhost:5000 --isolated-local --phase prepare --pm-id <existing-PM-GUID>
# After current Operator membership is available through its assigned owner:
python tools/demo/anh01/setup_demo.py --base-url http://localhost:5000 --isolated-local --phase survey --operator-id <existing-Operator-GUID> --device-id <ACTIVE-registry-GUID>
```

Prepare creates project, uploads/verifies the dossier, creates warranty with
the dossier as source, confirms PM geometry through Supervisor, and publishes
two segments through PM. Survey creates plan/task, accepts as Operator,
uploads/verifies MP4/SRT, submits immutable dataset, creates manual assessment,
selects only S01/SURFACE baseline, creates a supplement child for missing or
failed scope, then reads coverage. Failure is not promoted to success.

Limitations: initial project create accepts at most one existing handover
file, but DOCUMENT upload requires an existing project and current update has
no handover-attachment operation. Prepare creates an empty attachment list,
then uploads the project-scoped PDF and links it as warranty source; it does
not claim attachment to the original handover. No public device allocation or
membership producer is invented. Survey pauses at that prerequisite; supplied
registry/membership setup is external to this demo script.

## Rebuild and verification

`build_assets.py` requires ReportLab, Pillow, an Arial font directory, and an
explicit local FFmpeg executable for media. These are artifact runtime tools;
no solution dependencies are added.

```powershell
python tools/demo/anh01/build_assets.py --font-dir C:/Windows/Fonts --ffmpeg <local-ffmpeg-exe>
pdftoppm -png docs/demo/anh01/dossier-demo.pdf <temporary-output-prefix>
ffmpeg -v error -i docs/demo/anh01/survey-synthetic.mp4 -f null NUL
python tools/demo/anh01/setup_demo.py --validate-only
```

CURRENT_VERIFIED artifact checks: five rendered PDF pages inspected; all have
readable Vietnamese labels and no clipping/overlap. MP4 decoded without error;
two representative frames inspected; 4 s, H.264, yuv420p, 640x360, 10 fps.
Facts and all three file sizes/hashes validated.

Self-review 1: checked facts consistency, legal labels/provenance, scope,
three independent assessment dimensions, partial baseline and protected file
flow. Fixed survey type to existing `BASELINE` wire value and warranty scope
to existing `PROJECT` wire value.
Aligned the dossier number with canonical project creation's
`PROJECT-{projectCode}` producer.
Self-review 2: checked own file allowlist, local isolation guard, no credentials
in state and signed-URL error redaction, no database writes/reset scripts,
resume/key semantics and handover dependency. Fixed object-store error
diagnostics to omit signed URLs.

Historical artifact-task checkpoint: production API/SQL/MinIO demo workflow
was NOT RUN at that checkpoint. No BE,
live external, deployed compatibility or actual field-quality claim follows
from artifact validation. External review: NOT REQUESTED.

## Runtime acceptance checkpoint — 2026-10-03

Local live storage, PDF/ZIP, CLI and actual 8 GiB gates passed; full packages
remain Partial for Huy/CRS/external review. There was a preflight incident: a
published personal local config overrode environment and startup applied
migrations to RoadGuardPostmanTest. Exact observed impact and owner recovery
checkpoint are recorded in ANH-02-summary. Do not treat this run as compliance
with the initial DB-isolation boundary. Subsequent acceptance used only two
explicit task-owned databases and a private disposable bucket.

Development now reapplies environment/command-line precedence after local
JSON, and publish excludes appsettings.Development.local.json. Verify the
actual resolved DB/bucket before opting into InitializeOnStartup. Publish
into a new task directory; never copy personal local config into it. Reuse
IdentityRoleSeedStep, DroneDeviceSeedStep and PostmanUserSeedStep for minimal
synthetic identities; provision only task-owned current membership, the
synthetic RELEASED model and active CRACK entry. The broader legacy
PostmanScenarioSeedStep collided during preflight and was not used in the
isolated acceptance environments. No Huy approval/triage/conclusion is seeded.

The official [MinIO source README](https://github.com/minio/minio/blob/7aac2a2c5b7c882e68c1ce017d8256be2feea27f/README.md)
records source-only distribution. Docker Hub repository/tag returned404;
Quay anonymous bearer negotiation returned401. Docker desktop-linux was
x86_64, no daemon proxy, empty registry-auth entries; official MCR pull and
GitHub/Go downloads worked. No credentials/daemon/TLS settings changed.
Build the unmodified source at that exact revision with a local Go toolchain
(`GOOS=linux GOARCH=amd64 CGO_ENABLED=0 go build -trimpath -o <context>/minio .`).
The upstream Dockerfile depends on the unavailable minio/minio base, so this
local-only static build used a scratch Dockerfile copying the binary and
upstream LICENSE, with ENTRYPOINT ["/minio"]. Retain the AGPL license and
source provenance; this is a local acceptance build, not deployment adoption.
Set ANH02_MINIO_IMAGE to that verified local image before Compose config/up.

Command journal persists role/method/path/body and original If-Match before
transport. Same-state reruns preserve them; changed payload/scope under the
same key is rejected locally. Old states lacking a command journal cannot
recover a lost original version automatically; retain the original request
or use a fresh disposable fixture. The original complete journal is retained
for every upload. Large verification polling permits up to30minutes; it does
not mark a pending file verified or introduce a backend time threshold.

Live fault acceptance uses the existing export HTTP test with an explicit
MinIO gate: set ROADGUARD_MINIO_SMOKE=1, task-owned MinioStorage__* and
ANH02_TEST_FONT_PATH, then run:

```powershell
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --filter 'FullyQualifiedName~Live_MinIO_export|FullyQualifiedName~UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio'
```

The export wrapper injects temporary unavailability and acknowledgement loss
after actual PUT/durable readback. Retry verifies/reuses that same real object,
with one GeneratedArtifact, snapshot, admission/completion audit and receipt.
These are injected failures, not naturally occurring network incidents.

Current runtime details, exact commands/counts, source/video/font hashes,
observed memory and NOT RUN gates belong in the existing ANH-02-summary.
Runtime credentials, tokens, media, downloads and scratch probes remain ignored
under artifacts/anh02-live; no font/video binary or new dependency is committed.
