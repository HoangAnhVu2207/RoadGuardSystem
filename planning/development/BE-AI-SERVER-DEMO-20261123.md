# BE + AI server release and persistent FE demonstration data

Owner assigned 2026-11-23 in this chat. Writer/coordinator: Codex for the whole BE–AI slice. This package extends the former local-only assignment to preparing and executing a reviewed same-host server deployment plus additive demo data. Root operations still require the owner's terminal input. No second shared writer.

## Decisions and evidence

- TARGET_CONFIRMED: owner accepted the two smoothed reference centerlines on 2026-11-23; Kênh Đông and Đường số 2 remain separate persistent projects. Geometry is demo drone reference, not surveyed road/defect coordinates. Acceptance does not improve the approximate SRT alignment accuracy.
- TARGET_CONFIRMED: deploy both BE and AI to the current Ubuntu machine, provide richer persistent data for FE, clarify spec, plan tasks and execute. Preserve existing data, accounts and current public API hostname.
- CURRENT_VERIFIED 2026-11-23: host `roadguardsystem`, SSH through `100.81.253.113` with existing verified host key. Active BE `7e21430886049a98c0df33a6989acbb4fdc42479`; public `/health` Healthy. Root SSD approximately 87 GiB free, RAM approximately 13 GiB available. GPU PCI identifies NVIDIA GK208B GT 730; `nvidia-smi` absent. No GPU runtime readiness established.
- CURRENT_VERIFIED: disk sdb is 465.8 GiB with three existing partitions, including NTFS, none mounted. Do not format or count this capacity. Initial package uses existing `/srv/roadguard` filesystem within measured budget; mount/migration of sdb is a separate owner decision.
- CURRENT_VERIFIED: fresh remote fetch: BE develop `86e9f4e25f2e310f940e45c5cf60efd8d875a61a`; deploy `7e21430886049a98c0df33a6989acbb4fdc42479`; AI main `c689e544400fe326683c1c7f45115b3f7ec01daf`. BE workspace has 77 dirty/untracked status entries before this package. Existing work must be preserved and included only after scope review. AI worker code is local untracked work and must become an immutable version, not merely the current submodule pointer.
- CURRENT_VERIFIED: develop lacks `deploy/server` and `ServerDeployment.cs`, while the deployed branch owns Production maintenance, persistent DataProtection and CORS. Release must reconcile those paths explicitly. Generic develop alone is not deployable by the current server updater.
- UNKNOWN pending root census: live migration history, counts, current binding/config facts and account collisions. A successful health probe is not migration compatibility evidence.

## Runtime specification

1. Public client base remains `https://roadguardsystem.tail79aea8.ts.net`. Preserve current Funnel, TLS, approved CORS origins and host loopback binding. AI gets no public port/Funnel mapping. FE accesses business results and artifacts through BE authorization. SQL/MinIO remain private.
2. Pin a BE release SHA that contains the audited develop work, BE–AI slice and server composition; pin AI commit and FT95 model checksum `bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0`. Record both in one release manifest. Server updater requires deploy ancestry and exact SHA; no direct edits to an active release or mutable branch-tip deploy.
3. AI container runs the existing `src.api.be_worker_runtime:create_app_from_env` factory with one Uvicorn process and one inference runner. Initial runtime is CPU, matching tested `ft95-cpu-v1`; no CUDA/driver installation. Internal endpoint `http://ai:8999/`, shared service credential read from a mounted file, never logged. No SQL credentials in AI.
4. Persist AI admission SQLite/input/artifacts in `/srv/roadguard/ai/worker`, BE spool in `/srv/roadguard/ai/be-spool`, model read-only in `/srv/roadguard/ai/models`. AI worker has a 40 GiB task budget and at least 30 GiB free floor already enforced in code; reserve measured build/backup needs independently. Start at 4 CPU and 4 GiB AI memory, one active inference. Validate actual RSS/readiness before activation; do not silently raise limits.
5. Maintain API/SQL/MinIO existing resource limits initially. BE container gains FFprobe; AI includes FFmpeg/FFprobe and pinned Python inference dependencies. No paid provider calls or runtime dependency/model downloads. Build/rehearse image before stopping server API.
6. Survey assignment/acceptance, verified uploads and dataset submission remain canonical workflows. Eligible operator submission durably queues REAL work; the hosted worker must drain it without manual coordinator calls. BE streams source, polls progress, verifies artifacts and registers proposals into SQL/private storage. Queue/error/retry states remain observable.
7. Use 1 sampled frame/second and at most 180 sampled frames per supplied demo input. Validate the prepared 5 inputs against the configured bounds rather than silently truncating a 148-second clip to 120 frames. Six distinct source classes retained, `repair` maps to existing patch. AI never approves a defect, submits Crew measurements or completes repairs.
8. Preserve original GPS/SRT and record visually approximate offsets 0 ms / 95760 ms. Published road geometry uses the owner-approved smoothed GeoJSON. Drone position remains reference telemetry; Crew ground measurements remain separate. No pixel-to-metres or exact road-center claim.

## Data specification

- Two retained demo projects: `Duong Kenh Dong`, `Duong so 2`. Width 3 m, slab length 3 m, expansion joint spacing 12 m, joint gap 3 mm; warranty 2026-11-23 to anniversary 2027-11-23. Keep supplied dimensions explicitly; never invent slab thickness, formal handover signature or measured repair quantity.
- Project reference geometry files and hashes live in the private data manifest, not public Git. Approximate reference lengths 1270.7 m and 551.5 m. Backend engineering CRS conversion must use the adopted geometry flow. GPS degrees are not engineering metres.
- Confirmed requested accounts: one Supervisor, two PMs, ten Crew and ten Operators, counting the named Crew/Operator in the totals. Existing accounts are inspected and reused only if role/identity matches; conflicting identity stops that item. No automatic reset, role upgrade or delete. Initial generated passwords go to a private handoff file and use the real password hasher/change-password policy.
- TARGET_CONFIRMED 2026-11-23 follow-up: owner accepted `HoangDo@gmail.com`, two additional Reporter demo accounts (25 total) and the separate synthetic `FE Workflow Demo` project. Prepare these scenarios with explicit DEMO provenance.
- PM assignment proposal: first named PM owns Kênh Đông, second named PM owns Đường số 2. Assign five Crew and five Operators to each as appropriate to existing task/membership semantics; role membership does not invent a project membership relation that code does not support.
- Real-video projects receive genuine labeled demo project documentation through verified upload, actual route/pavement/warranty, survey assignments and the five source videos with companion SRT. Inference outcomes are whatever the real model returns; zero detections is a valid result.
- If synthetic FE project is accepted, prepare a separate manifest of 10 Reporter examples, 10 survey assignments distributed over supported states, 10 inspection examples and 10 repair examples (5 NORMAL/5 FastTrack) only through valid domain transitions. These are scenario targets, not permission to forge eligibility, evidence, deadline snapshots or generic SQL rows. The manifest must enumerate exact realized states and distinguish planned from created. No simulated defects are mixed into real inference results.
- No blanket “10 rows per table”: identity, catalogs, ledgers, outbox and migrations are governed by workflows. Seed only missing scenario data with stable operation IDs and a receipt. Rerun performs readback/replay, never duplicates data. Notifications derive from actual scenario transitions; no external demo email to arbitrary recipients.

## Migration and recovery specification

- Read exact applied migration IDs and ownership first. Compare against candidate's complete history, including three new Nov19 AI migrations and intervening develop migrations. Replay upgrade from deployed schema with representative preserved data in disposable SQL before applying to server.
- Preserve existing deployment owner marker, runtime SQL account and private storage. The Production API must never initialize or seed on ordinary startup. Explicit maintenance performs the scoped upgrade only after backup passes.
- Build and validate candidate images before downtime. Deployment includes one coordinated backup/migration/restart interval; concrete expected duration and pinned manifest are presented when ready for the owner's root execution. No parallel heavy build/backup/test job on Ubuntu.
- Old-image rollback is allowed only if candidate schema compatibility is demonstrated. No AI Down migration on populated tables, no automatic shared database restore, no deletion of newly uploaded evidence. On failure keep receipts/config/backup and stop new jobs; select forward repair or separately approved restore based on observed state.
- Never activate AI dispatch before model catalog, six-class mapping, worker health and private storage are verified. Disabling AI dispatch is the first recovery lever; API can continue serving other business functions if compatible.

## Acceptance and delivery

1. Exact pinned BE and AI source/image identities; server healthy, intended bindings/private AI, CORS and persistent key settings preserved.
2. Real ordinary hosted workflow: operator verified upload + dataset submit => durable REAL work => AI => registered SQL/file proofs => PM-readable proposals. Cover replay and one failed/retried attempt in owned test data; no manual coordinator tick as deployment proof.
3. Upgrade rehearsal preserves representative preexisting records; root apply uses verified backup. Two self-review passes inspect behavior/data first and deployment/ownership/recovery second. External review is reported separately.
4. Demo accounts/projects/memberships read back through authorized APIs; 401/403 for anonymous/wrong-role/wrong-project requests. CORS checks include actual FE origin and a denied origin. No FE UI testing is required by this package.
5. Publish a compact FE handoff: base URL, API version/auth mode, project and scenario IDs, each role's available scenarios, upload/SRT/AI polling contracts and current unsupported cases. Credentials are delivered separately. Knowledge note updated with source/evidence and any index limitation.
6. Completion is not declared until server readback and inference are observed. Local build/tests, prepared seed and public health each retain their own evidence boundary.

## Current execution

Read-only hardware/release check completed. New census `tests/local-ai/server-readiness.py` syntax checked locally/on Ubuntu and hash-matched at `/tmp/roadguard-be-ai-readiness-20261123/readiness.py`; awaiting interactive sudo. No deployment/schema/account/project write yet.
