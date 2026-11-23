# BE–AI server and FE data implementation plan

> Agent execution: use superpowers:executing-plans task by task in the existing isolated worktree. Preserve one writer per shared file; do not deploy an unreviewed working directory.

**Goal:** deploy pinned BE and private CPU AI on roadguardsystem and retain approved road demo data for FE.

**Architecture:** existing public HTTPS BE, SQL and private MinIO; new private HTTP AI container with durable admission/artifacts. BE owns authorization, workflows and results. Operator input triggers real inference; PM/Crew retain decisions and measurements.

**Tech stack:** .NET 8, EF/SQL Server 2019, Docker Compose, Python/FastAPI/FT95, FFmpeg, existing Tailscale Funnel.

**Spec:** `planning/development/BE-AI-SERVER-DEMO-20261123.md`.

## Global constraints

- Host `roadguardsystem`, currently `7e21430886049a98c0df33a6989acbb4fdc42479`; fresh BE develop `86e9f4e25f2e310f940e45c5cf60efd8d875a61a`, AI main `c689e544400fe326683c1c7f45115b3f7ec01daf` plus audited local slice.
- One coordinated server maintenance job; interactive sudo. Preserve data/keys/current public address; no format of the partitioned 500 GB disk.
- CPU first; one inference, 1 FPS, 180 frames/input, pinned FT95 hash from spec. Existing 30 GiB free floor and 40 GiB worker budget.
- 2 accepted road routes; 25 confirmed account targets after owner accepted synthetic third-project/2 Reporter expansion and HoangDo@gmail.com.
- No generic sandbox seed, fake AI success, shared DB reset/Down, unsolicited external email or raw secret logs.

## Tasks

### T01 — Record approvals and obtain live schema/config census

Files: persistent demo spec, this spec, private synced route GeoJSON/manifest, `tests/local-ai/server-readiness.py`.

- [x] Fetch and record actual upstream revisions; preserve dirty work.
- [x] Read-only host/release/GPU/disk/health inspection.
- [x] Create narrow root census; run Python syntax checks and compare local/remote SHA256.
- [ ] Owner runs staged census; retrieve `readiness.json` through SSH. Require expected active SHA, healthy services and database owner marker. Read migration IDs, table/column catalog, counts and approved origins.
- [x] Mark only the selected smoothed routes owner-approved with date and hashes. Update private manifest and knowledge source note.

### T02 — Restore server composition into the current BE candidate

Files: `RoadGuardSystem.API/Extensions/ServerDeployment.cs`, `RoadGuardSystem.API/Program.cs`, `deploy/server/*`, focused `tests/RoadGuardSystem.ApiTests/Startup` tests.

- [ ] Compare `origin/deploy` server files with current develop; import only server-owned composition/operations files. Integrate Program registration/maintenance/CORS manually; retain current REAL and upload workers without duplicate hosted registration.
- [ ] Test Production disabled/enabled behavior, explicit origin validation, persistent DataProtection, no ordinary-startup migrations, one upload verifier and enabled REAL worker composition. Run affected API tests and Release build.
- [ ] Preserve server recovery's actual loopback/TLS configuration rather than reintroducing historical LAN-IP bindings.

### T03 — Package private AI CPU runtime and BE media tools

Files: new `deploy/server/Dockerfile.AI`, AI dependency lock, worker health probe, composed AI service; API Dockerfile gets FFprobe.

- [ ] Resolve exact installed dependency versions; pin CPU runtime dependencies and model hash. Keep weights/videos outside source image and credentials outside build context.
- [ ] Configure private `ai:8999`, single worker, persistence mounts and resource bounds. Health probe reads the mounted token internally and emits only status. AI receives no SQL config or public port.
- [ ] Build locally with measured D: capacity; verify Linux app startup and authenticated readiness with real checkpoint. Check no runtime internet/model dependency, persistent admission restart and missing-model failure.

### T04 — Rehearse schema upgrade and ordinary hosted REAL workflow

Files: focused migration/hosted acceptance tests, candidate schema script, private test results.

- [ ] Compare census migration IDs to deployed and candidate sources. Restore the deployed schema in owned disposable SQL, populate representative account/project/upload data, upgrade candidate and verify preservation/model alignment.
- [ ] Run actual Program hosted upload verification and REAL dispatch with owned SQL/MinIO/AI; submit through adopted operator workflow, poll normal status, verify artifact hashes and PM proposal readback. No manual coordinator ticks in this lane.
- [ ] Verify idempotent replay and wrong actor/project. Keep worker/catalog disabled if any precondition fails. Document compatible code recovery and prohibit populated Down migrations.

### T05 — Prepare additive data manifest and private credentials

Files: private demo manifest/receipts/handover files, narrow seed runner under deployment tooling and isolated tests.

- [ ] Read only requested identity/project collisions; settle dependent email/synthetic scope answer. Generate missing account credentials privately; use actual domain password hashing and initial-password-change behavior.
- [ ] Enumerate stable IDs/operation IDs for 2 roads, PM assignments, required memberships, geometry/layout/warranty, verified handover artifacts, survey datasets and 5 video/SRT inputs. Record evidence hashes and accepted geometry provenance.
- [ ] If accepted, enumerate the separate synthetic FE scenarios with explicit transitions/preconditions and labels. Refuse unmet business facts instead of inserting invalid state directly.
- [ ] Dry-run against owned candidate DB twice; second run must create zero duplicates. Validate role boundaries, scenario readback and no unrelated account/data mutation.

### T06 — Freeze release and complete review

Files: release manifest containing exact BE/AI SHA, source/image/model hashes, migration list, config changes, backup/recovery steps and task receipts.

- [ ] Commit reviewed AI slice and BE candidate in the appropriate repositories, preserving unrelated work; satisfy deploy ancestry and immutable AI source extraction. Never push main. Any required branch publication follows the scoped release workflow.
- [ ] Self-review pass 1: authority/workflow, source fencing, migration/data and credentials. Pass 2: deployment composition, private network, budgets, producer/consumer and recovery.
- [ ] Run only checks invalidated by final changes. Present concrete pinned apply package/downtime to owner for interactive sudo; missing sudo is not bypassed.

### T07 — Deploy BE and AI sequentially on Ubuntu

- [ ] Check expected release, root-owned checksummed scripts, maintenance lock, capacities and no competing job.
- [ ] Build/pull pinned images before downtime; verify backup, then scoped migration and API/AI activation. Stop on unexpected schema, ownership or checksum mismatch.
- [ ] Verify exact active image/release, dependency health, private AI, unchanged public HTTPS and approved CORS. Exercise non-destructive authorization probes.

### T08 — Apply demo data, upload videos and observe real inference

- [ ] Apply additive manifest with collision guard and receipts, through adopted workflows. Read back counts/identities and second-run no-op.
- [ ] Operator uploads five real inputs plus synchronized SRT; verify object checksums before submission. Observe REAL work through final state and record actual detections/limitations.
- [ ] Leave PM decisions/Crew measurements pending for the real-video projects. Synthetic scenarios retain explicit demo provenance if accepted. Verify FE-facing role reads.

### T09 — FE handoff and Knowledge

- [ ] Deliver base URL/auth/roles/projects/scenarios/API examples and separate private credential file.
- [ ] Update knowledge source notes with observed server commit, configuration, data receipt and actual inference outcomes. Rebuild index only via its established tool/workflow; otherwise explicitly record index pending.
- [ ] Report local vs server evidence, failed/skipped checks and unresolved constraints. Do not mark T07/T08 complete from preparation or health alone.

## Execution status

T01 in progress; T02–T09 pending. No shared database/deployment mutations yet. Owner requested execution in this session; proceed through independent local tasks while census/decisions are pending.

Execution follow-up: first census failed because sqlcmd -h and -y 0 are mutually exclusive; reproduced on owned SQL2019, corrected flags passed JSON read. r2 staged and checksum matched (c8f881d7537ff87ff9729786fb319ed03b9bf82e7bda3289fff776eb7fb5ece8), pending interactive sudo. T02/T03 implementation and T04 hosted acceptance preparation in progress.
