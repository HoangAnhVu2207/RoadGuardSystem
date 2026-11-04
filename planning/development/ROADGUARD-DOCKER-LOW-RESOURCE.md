# ROADGUARD-DOCKER-LOW-RESOURCE

Status: COMPLETE_LOCAL, independently reviewed by ChatGPT 2026-10-30. Owner authorized this task 2026-10-30; ChatGPT supplied the assignment in the existing RoadGuardSystem planning chat. Codex/Huy executes as sole writer. No commit/push/merge authorized or performed.

## Baseline and scope

Base/HEAD: `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`. Preserve 127 modified and 12 untracked preexisting files. Their hashes and exact paths are recorded outside the repository in `D:\RoadGuard-Review-Evidence\DOCKER-LOW-RESOURCE-20261030\dirty-hashes-before.json`. Reserved `deploy/fe-handoff/G02-G06-OWNER-DECISIONS.md` remains untouched.

TARGET_CONFIRMED: user requests running/testing Docker, limiting the VM to 4 GiB RAM/2 CPUs, reducing usage for weak PCs, removing unnecessary containers/images/cache and generated project junk. Preserve all named volumes, private state, keys, secrets, docs/history, audit evidence and unrelated changes. No business/API/schema/package changes.

Allowed files: this spec, `.dockerignore`, `docker-compose.yml`, `deploy/fe-handoff/compose.yaml`, and `tests/Operations/Verify-DockerLowResource.ps1`. Dockerfiles may change only if fresh build evidence requires reduced compiler concurrency. HTTPS profile is outside this runtime test; no HTTPS edits planned. Host `.wslconfig` is backed up outside Git before editing.

Iteration 1 corrective scope (ChatGPT reviewed 2026-10-30): add `.gitattributes` LF checkout policy for `deploy/fe-handoff/*.sh`; normalize working-tree/snapshot `sql-start.sh` and `tls-init.sh` without changing script semantics. Fresh uncached builds passed, but SQL runtime exited 127 because CR bytes became part of shell arguments/path. Low-resource validator reproduced the failure before the fix. No database/schema/credential change is implicated.

CURRENT_VERIFIED baseline: no running containers; 190 stopped containers (184 historical RoadGuard Compose containers, six disposable Testcontainers SQL fixtures), 172 named volumes, 144 images. Docker reports 4,061,790,208 bytes RAM and 8 CPUs. Build cache 23.83 GB. Actual baseline scenarios builds, including `--no-cache`, passed: explicit scenario allowlist entries are clarity, not a demonstrated missing-COPY defect.

## Implementation and acceptance plan

- [x] Inventory exact resources/paths and back up `.wslconfig`; retain dirty-file hashes and volume names.
- [x] Set WSL VM memory=4GB, processors=2; restart Docker/WSL and verify runtime CPU/memory. Preserve unrelated host settings.
- [x] Add per-container caps. Starting budget: SQL 2304 MiB/1 CPU (minimum 2 GiB Linux SQL startup requirement), API 512 MiB/0.75 CPU, MinIO 384 MiB/0.5 CPU, Mailpit 128 MiB/0.25 CPU, scenarios 256 MiB/0.5 CPU, init 128 MiB/0.25 CPU. Retain SQL engine memory=1024 MB. CPU quotas do not reserve cores. VM limit bounds their aggregate use.
- [x] Validate normalized Compose caps plus original secret/image/volume/health invariants. Explicitly allow four existing scenario scripts in the build context without admitting secrets or artifacts.
- [x] Create a source-only snapshot with matching hashes, build sequentially under the VM ceiling, use isolated project `rg-low-resource-20261030` and new volumes/secrets. Run real SQL/API/storage/PDF and synthetic AI/demo scenarios; inspect readiness, exit codes and OOM counters.
- [x] Restart this project and replay scenarios; verify retained owner/key/state and idempotent progress. Measure startup/scenario/idle/restart RAM/CPU and VM headroom/swap. Distinguish caps from sampled usage; never promise constant 2 GiB.
- [x] Remove exact old stopped resources and empty owned networks after checking current state. Never delete volumes. Remove only unused historical project images/cache after retaining current stack references. Do not use global indiscriminate prune.
- [x] Remove verified untracked generated bin/obj/cache and two synthetic 8 GiB test videos, preserving tracked/evidence/local config/data files. Exact 32-target manifest: external `deleted-generated.json` (16.702 GiB logical size).
- [x] Recheck all old volume names and dirty file hashes, inspect final scoped diff, complete two self-review passes, request independent ChatGPT diff/output review.

## Evidence and limitations

All raw inventories, command outputs, backup and cleanup manifests live in `D:\RoadGuard-Review-Evidence\DOCKER-LOW-RESOURCE-20261030`. Do not include private configuration or credentials. Record fresh PASS/FAIL/NOT_RUN results here after execution. BE runtime checks are separate from synthetic AI mocks; external AI/Android and deployment compatibility remain NOT_RUN. No historical test result substitutes for this acceptance. Docker logical reclaimed space does not imply automatic Windows VHD shrinkage.

Rollback: restore `C:\Users\HoangAnhVu\.wslconfig` from external `wslconfig.before`, stop Docker, shut down WSL, start Docker again. Restore only this task's config hunks if necessary; retain all named volumes. Removed disposable outputs/cache can be rebuilt; no rollback of shared data is required.

## Fresh results (CURRENT_VERIFIED, 2026-10-30)

- WSL host ceiling `memory=4GB`, `processors=2`, `swap=1GB`, `autoMemoryReclaim=gradual`; runtime Engine 4,107,382,784 usable bytes / 2 CPUs. Original `defaultVhdSize` preserved; debug console disabled. Linux reserves some memory from the 4 GiB ceiling.
- Source-only snapshot: 1122 files, 33.00 MiB, matching SHA256 before/after the LF correction. Uncached init/SQL/API/scenarios builds PASS; initial MinIO cached build PASS; additional uncached MinIO build PASS (Go build step 246.9 seconds). API image build completed in 143.74 seconds under the VM ceiling; no compiler concurrency changes needed. Uncached-built MinIO deployed and a second replay/PDF download/auth check PASS, with exact journal/private-state/20 collection identity hashes unchanged (`final-runtime.json`).
- Initial startup FAIL (SQL exit 127, CRLF), corrected by LF policy/normalization; rebuilt SQL without cache PASS. Regression validator failed on original CR bytes and passed after the correction. Both entrypoint scripts passed Linux shell syntax checks and `git ls-files --eol` reports index/working tree LF.
- Root operations validator PASS; seven negative fixtures intentionally return exit 1. Low-resource validator PASS (root/six handoff caps, SQL minimum/engine memory, loopback/private networking, persistent volumes, explicit build sources and four blocked negative budgets).
- Isolated Docker acceptance PASS in 239.05 seconds: SQL/API healthy; `/ready`, `/health`, Swagger and Mailpit HTTP 200; real uploads/private evidence, reports, project A/B PDF export workers, repair proposals, collection reads and synthetic AI scenarios completed. Both PDF content downloads HTTP 200 with `%PDF-` signatures (29,661 and 29,657 bytes); anonymous 401 and cross-project 403.
- Restart + scenario replay PASS: exact journal SHA256 unchanged (97 steps), complete/version markers retained, all private state files and data-protection key hashes unchanged; 20 complete collection ID sets unchanged. No OOM kills in inspected cgroups/container states; VM `oom_kill=0` since the fresh WSL boot. No automatic container restart loops.
- Sampled scenario aggregate container RAM 1365–1496 MiB; VM used estimate (`MemTotal - MemAvailable`, excluding reclaimable cache) 1661–1836 MiB. These are sampled observations, not a constant usage guarantee or production load test. Windows `vmmemWSL` can retain filesystem/build cache near the ceiling; reclaimable Linux cache differs from container working memory.
- Additional cold MinIO compile while the stack remained running: sampled VM used peak 2463.70 MiB, available minimum 1453.41 MiB, swap peak 93.06 MiB. Measured VM CPU peak 199.92% where 200% means the two allowed cores. Build CPU can fill that quota; the host still restricts Docker to two cores. Runtime container memory remained under the declared caps. `review-proof.json` provides compact complete results; nine `cleanup-review-*.txt` parts expose individual resource selections without output truncation.
- Stable idle after all rebuild/replay/PDF checks: 12 samples, container total 1081–1113 MiB, VM used estimate 1818–1842 MiB, VM available 2075–2100 MiB. Docker before/after: images 144 -> 10, containers 190 stopped -> six current containers (four running, two successful one-shots), volumes 172 -> 178, build cache 23.83 -> 9.073 GB after the fresh cold builds. Cache prune's 17.82 GB is its operation result; subsequent builds create new useful cache. Windows D: free 81.89 GiB at final measurement; no VHD compaction performed or claimed.
- Cleanup: 190 stopped historical project/Testcontainers containers; 56 empty owned networks; 139 old project/test image tags (one additional baseline tag already absent); targeted old cache prune reports 17.82 GB. Generated project cleanup 32 paths / 16.702 GiB. Named volume names: all 172 inherited retained plus six new isolated volumes. No volume pruning, shared data deletion or old-doc deletion.
- Preservation check: all 139 preexisting dirty-file SHA256 hashes unchanged, including reserved G02–G06 owner decisions. Current HEAD unchanged. Evidence: `verification-summary.json`, `acceptance.json`, `extra-checks.json`, `docker-cleanup-manifest.json`, `image-cleanup-manifest.json`, `cache-cleanup-selection.json`, `deleted-generated.json` and command logs in the external evidence directory.

## Review and use

Self-review pass 1: inspected configuration diff against assignment; image pins, passwords/guards, ports, private networking, mounts, healthchecks, dependency order and business behavior retained. Root SQL gains a separate 1024 MB engine budget and 2304 MiB container ceiling; handoff retains its preexisting engine budget. LF correction changes bytes only, not shell semantics.

Self-review pass 2: inspected file scope, shared-writer boundaries, source snapshot hashes, volume/dirty-file preservation and rollback. No migration/app/dependency/history changes. Existing reports remain untouched. External ChatGPT final review PASS (`STATE: DONE`, iteration 3, independent local review); all 11 released outputs 1348–1358 read without truncation. Reviewer independently reconciled all nine manifest parts (190 unique containers, 56 networks, 140 evaluated image tags/139 removals/one already absent, 245 selected cache IDs, 32 generated paths), configuration diff, LF correction, validator and runtime proof. No material defect blocks local Docker acceptance. This is separate from Codex's two self-reviews. GitHub diff review NOT_RUN (no publication requested). No production-load, external-consumer or release certification.

The running instance uses Compose project `rg-low-resource-20261030` with its own six volumes. API/Swagger: `http://127.0.0.1:18081/swagger/index.html`; Mailpit: `http://127.0.0.1:18026`. For this retained instance, run from the repository:

```powershell
$env:COMPOSE_PARALLEL_LIMIT='1'
$env:COMPOSE_BAKE='false'
docker compose -p rg-low-resource-20261030 -f deploy/fe-handoff/compose.yaml build
docker compose -p rg-low-resource-20261030 -f deploy/fe-handoff/compose.yaml up -d
docker compose -p rg-low-resource-20261030 -f deploy/fe-handoff/compose.yaml stop
```

Do not start a second project on the same published ports or add `-v` to teardown commands. Resource caps are in tracked Compose files, so future instances inherit them. The `.wslconfig` ceiling is local to this machine; other Windows machines must set the same WSL limits and restart Docker/WSL. Root `docker-compose.yml` runs SQL only; the handoff Compose runs the full demo. On a weak machine run one of these stacks at a time, because two SQL instances compete for the same 4 GiB VM ceiling. HTTPS/TLS profile execution, external AI/Android and production deployment/load compatibility remain NOT_RUN. AI outcomes here are mock verified; SQL/API/storage/PDF/restart behavior is real local BE runtime evidence.

References: [WSL configuration](https://learn.microsoft.com/windows/wsl/wsl-config), [SQL Linux requirements](https://learn.microsoft.com/sql/linux/sql-server-linux-setup?view=sql-server-linux-ver15), [Compose service resource limits](https://docs.docker.com/reference/compose-file/services/), [targeted cache filters](https://docs.docker.com/reference/cli/docker/buildx/prune/).
