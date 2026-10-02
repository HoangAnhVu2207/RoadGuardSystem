# ANH-01 fictional demo assets

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

Production API/SQL/MinIO demo workflow: NOT RUN in this artifact task. No BE,
live external, deployed compatibility or actual field-quality claim follows
from artifact validation. External review: NOT REQUESTED.
