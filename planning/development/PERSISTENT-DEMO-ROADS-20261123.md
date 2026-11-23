# Persistent road demonstration data

Owner-assigned in chat, 2026-11-23. Writer: Codex. Data task, using existing adopted application workflows; no new public contract or schema is approved here.

## Confirmed scope

- Persist on Ubuntu `roadguardsystem`; retain for product demonstrations. Preserve existing records/accounts; no database reset or generic pristine sandbox seeder on the shared database.
- Separate projects: **Duong Kenh Dong** uses edited `1123.mp4`, source `DJI_0155.MP4`; **Duong so 2** uses edited `11234.mp4`, source `DJI_0151.MP4`. Latest name supersedes interim Duong so 8.
- Both: road width 3 m; slab length 3 m; expansion-joint spacing 12 m; joint opening 3 mm. One-year warranty starts 2026-11-23; anniversary 2027-11-23. Do not invent slab thickness or surveyed quantities.
- Owner permits original drone GPS as demonstration centerline reference. It remains unverified flight telemetry, not surveyed ground position. Crew determines physical defect position/measurements; AI proposes and PM decides.
- Provision one Supervisor, two PMs, ten Crew and ten Operators, with initial credentials privately handed to owner. Named identities are recorded in private local preparation, not committed with credentials. Count named Crew/Operator toward each group of ten. Do not reset or change an existing account to obtain a desired role.
- Proposed assignment: first named PM to Duong Kenh Dong; second named PM to Duong so 2. Use the named Operator for the first project and a newly provisioned Operator for the second; confirm by reading membership after apply.

## Observed preparation

- Read-only SSH reached the expected host; current release `7e21430886049a98c0df33a6989acbb4fdc42479`; public health returned Healthy. This is not the local Nov23 BE-AI revision. Compatibility and a separately reviewed release path must be established before claiming the server processes REAL AI.
- Both edited videos lack subtitle/data tracks. DJI originals contain extractable SRT. DJI_0155: 644 cues, 639 GPS-present-unverified, 5 missing. DJI_0151: 647 GPS-present-unverified cues. Extracted SRT matches provided output.srt/output2.srt respectively.
- Initial inspection could not map the edited timeline. Owner subsequently authorized visual synchronization. The local follow-up below supersedes UNKNOWN for the approximate demo alignment only; no exact editor cut list or surveyed location is inferred.
- Read-only inspection of the deployed `ProjectsController.Create`, `ProjectCreationService.CreateAsync` and `ProjectCreationPersistenceService.GetFactsAsync/CreateAggregateAsync` confirms a version difference: this release accepts an empty handover-file list, creates the project/primary-PM membership/handover/warranty together and uses actor-scoped idempotency. This is implementation evidence, not permission to bypass newer handover requirements. The preparation plan below retains genuine verified demo documentation; a current-local payload must not be assumed compatible with this release.
- Existing pavement input exposes `ExpansionJointGapMillimeters`, not a separate generic gap for every panel boundary. `PavementLayoutEngine.Plan` validates that metadata but partitions nominal slab cells by length/width; do not claim this proves a rendered 3 mm gap at every boundary. Preserve the owner's per-panel 3 mm requirement explicitly in demo metadata; a new geometry contract/implementation is outside this data-creation scope.
- Full original GPS tracks self-intersect and revisit coordinates. Raw full-flight lengths about 2246 m/2415 m include repeat flights, and must not become accepted route lengths, slab counts, or quantities. The synchronized clip-specific drafts below supersede the full-flight preview; owner acceptance of these concrete drafts remains pending.
- 1123 has four stream-copy processing pieces covering its edited timeline, each about 120 seconds or less; 11234 remains one approximately 148-second input. Five input hashes and segment time boundaries are in private preparation. Originals are unchanged.

Private artifacts: `D:/RoadGuard-AI-LocalTest/duong-kenh-dong/` (`demo-project-spec.md`, media readiness, source SRT, drone-reference GeoJSON, `video-input-manifest.json`). No credentials or raw GPS need to be committed to source.

## Smoothed route preview follow-up

- TARGET_CONFIRMED 2026-11-23: owner requests fewer GPS control points and a curved reference route instead of zigzags. This authorizes local preview refinement; final geometry acceptance/server application remains pending.
- CURRENT_VERIFIED local: retained 4/5 simplified route controls and rounded corners with tangent quadratic Bezier segments, trimming at most 25 m along adjoining segments (also limited to 25% of each segment). Dense output vertices render the curves; they are interpolated, not additional GPS observations.
- New `1123.centerline-smoothed.geojson` and `11234.centerline-smoothed.geojson` under the private synced folder: approximate lengths 1270.7 m / 551.5 m. Maximum sampled offset from the previous simplified routes is 0.853 m / 3.417 m; these are geometric comparisons, not surveyed accuracy bounds. Both endpoints are identical to previous drafts.
- Verification: `alignment/smooth-centerlines.py` exited 0, checked finite coordinates, endpoint identity, nonzero successive segments, sampled offset below 12 m, output JSON roundtrip and SHA256 preservation of prior draft, raw synchronized track and SRT. Preview `two-centerlines-smoothed.png` inspected. Two self-review passes checked scope/provenance and curve continuity/preservation; no server write or telemetry modification.

## Apply plan and gates

1. Verify host, exact active release, owned maintenance lock, database/schema and capacity. Read only scoped account/project existence and role facts; no secret/config dump.
2. Resolve valid account identifiers and existing-account conflicts. Prepare an additive manifest with stable identifiers, separate generated credentials, collision refusal and transaction receipts. Preview counts before applying.
3. Prepare owner-visible route drafts from the correct original GPS, retaining the raw track and transformations. Do not confirm a self-intersecting repeat flight as a road. If gaps or route alternatives cannot be established, leave draft pending instead of fabricating a centerline.
4. Use an actual verified project handover upload in the existing project creation flow. A demo information sheet must be explicitly labeled demonstration data; never fabricate an official signed/approved handover. Do not copy SQL test fixtures that fake verified files.
5. Establish a scoped recoverable data backup and apply accounts/projects/memberships additively. Existing backup procedure stops API/MinIO; an unapproved downtime window is not implied. Never use restore-over-existing as routine recovery.
6. Create geometry, pavement and warranty through adopted workflows with idempotency and concurrency fences. Read back project separation, exact dates, role membership and layout. Never store the 3 mm opening as 3 m.
7. Operator accepts assignment, uploads each input through private MinIO verification and forms datasets. Exact source lineage and original/edited time mapping are recorded. Missing synchronized GPS remains missing; original track used for route construction does not establish a frame location.
8. Submit REAL processing only on a compatible deployed worker/backend; observe durable job/attempt/artifacts and proposals. Do not replace inference with synthetic detections to make a demo appear successful. PM KEEP/LINK/REJECT and Crew FIELD actions remain human-role decisions.
9. Verify persistence after application/job restart only within the approved operating window. Publish project IDs, assigned actors and observed job outcomes. Deliver account passwords separately, never in reports/logs/Git. Do not mark prepared artifacts as server-created records.

## Current state

TARGET_CONFIRMED follow-up 2026-11-23: owner explicitly accepted both smoothed centerlines and requested server BE/AI deployment and persistent demo data. This supersedes route-acceptance-pending statements below. Private smoothed GeoJSON files now carry ownerApproved=true; video-input-manifest.json records selected route hashes and approval date. Approximate alignment and drone-reference accuracy limitations remain. Server application is not yet performed. Deployment execution is governed by BE-AI-SERVER-DEMO-20261123.md.

Preparation only. No project/account/shared database writes, server code/schema changes, video uploads or REAL server inference performed by this task yet. Remaining gates include accepting the concrete approximate alignment/route drafts below, existing account inspection, compatible server release and the concrete reviewed apply package.

## Visual synchronization follow-up — 2026-11-23

Owner explicitly requested synchronization and reimport of SRT into the edited clips. Local artifact work only; originals unchanged. No application code/interface/schema change.

- CURRENT_VERIFIED: source/edited keyframes extracted using the existing FFmpeg binary; ORB feature matches with RANSAC checked 96 Kênh Đông and 30 Đường 2 samples at approximately 5-second spacing. Fine source-frame searches checked 8 control frames per clip, including near the end. A six-pair contact sheet was visually inspected; the edited image crops correspond to their source scenes.
- Mapping used for demo: `DJI_0155 sourceSeconds = 1123 editedSeconds`; `DJI_0151 sourceSeconds = 11234 editedSeconds + 95.760`. No cut discontinuity observed in the sampled sequence. This is `VISUALLY_ALIGNED_APPROXIMATE`, not an exact editor timeline or an exhaustive frame-by-frame proof. Fine control residuals were under 0.1 seconds; static scenes can match multiple adjacent frames. Keep that limitation with the artifacts.
- Exported 478 Kênh Đông and 149 Đường 2 SRT cues, clipped to the edited video duration; every exported cue contains the source GPS text. Four Kênh Đông part-specific SRT files use the existing edited-segment boundaries. Source cue provenance is retained in `*.srt-lineage.json`.
- New `1123.gps-synced.mp4` and `11234.gps-synced.mp4` contain a `mov_text` track. Video/audio packet stream hashes match the original edited files; stream starts and video duration are unchanged. MP4 audio duration metadata rounds by less than 0.1 ms. Extracting SRT back out reproduces the exported cues/times/text exactly.
- Clip-only reference route drafts remove consecutive duplicates/exact coordinate loops and simplify at 12 m, consistent with the limited four-decimal GPS source. Approximate reference lengths are 1271.1 m and 555.0 m. They preserve original synchronized raw tracks separately; draft endpoints and all transforms are recorded. This does not establish physical centerline accuracy, 3 m slab placement, billable quantities or final owner-approved geometry.
- Private outputs: `D:/RoadGuard-AI-LocalTest/duong-kenh-dong/synced/` (MP4, SRT, route GeoJSON, preview, alignment summary, verification); matching evidence/scripts under sibling `alignment/`. `video-input-manifest.json` now references synchronized companion SRTs and remains NOT_UPLOADED.
