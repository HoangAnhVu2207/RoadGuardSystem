# Huy final release candidate runbook

Status: IN_PROGRESS preparation under [HUY-FINAL-INTEGRATION](../../planning/development/HUY-FINAL-INTEGRATION.md). This is an operator procedure, not evidence of deployment, benchmark or recovery success. Final candidate SHA, hosted CI run and actual environment must be filled after H0–H7 verification. No production migration, deployment or main/develop merge is authorized by this document.

## Candidate and evidence

Record the exact Git SHA, image/artifact digest, source comparison, applied migration list, configuration revision and executing operator. Use the same SHA for hosted CI, build artifact and smoke checks. Preserve logs and test results with timestamps and environment identity, without credentials or personal data. Local SQL and mock storage/client tests are classified separately from real infrastructure checks.

Current owner activation (Anh07/10/2026) and package checkpoints are in the assigned spec and summary. LD02 business ACK, LD03 ordinary extension authority/history, LD04 eligible substitute duty and LD05 latest-missed weekly recovery have production paths; acceptance is recorded per capability. LD06–LD08a are authorized ongoing implementation, not pending owner authority. Official CRS, extension numerical limits/additional offline execution, real coverage/statistical source facts and public-period formulas remain localized gates. Do not activate those dependent behaviors by configuration defaults. Correction authority remains current project PM for FT/current project Supervisor for normal. Candidate/sample geometry and test repair policies are explicitly labeled fixtures.

## Configuration and transport

Resolve the actual host, SQL Server, object storage, reverse proxy, TLS certificate, client origins and worker ownership before deployment. Keep secrets in the deployment secret store. The observed application binds `RoadGuardDatabase`, `Jwt`, `MinioStorage`, `UploadSession` and `Anh02` options; recheck the final runtime bindings before supplying the environment configuration. SQL sensitive-data logging must remain disabled outside isolated diagnostic work.

Persist the ASP.NET Data Protection key ring across restarts and replicas. Retain configured JWT signing-key IDs and the H1 refresh-retry protector key-ring entries needed to validate/decode outstanding credentials and receipts. Rotate keys through the implemented key-ID protocol; do not remove an old key while valid protected material still depends on it. Server authentication keys are not device-data recovery keys.

Use HTTPS for `__Host-RoadGuardSession`, `__Host-RoadGuardRenewal` and CSRF cookies, with Secure, HttpOnly where applicable, root path and no Domain attribute. Validate cookie behavior through the actual proxy and browser, including finite ticket expiry, protected renewal, restart, logout/revoke and actor-bound CSRF. Configure exact permitted origins and credential behavior through the actual hosting policy; this document does not claim a CORS policy exists merely from a proposed deployment value. Confirm trusted proxy forwarding and original HTTPS scheme without accepting arbitrary client forwarding headers.

Test bearer precedence and mixed-identity denial on the exact adopted routes. Web renewal must never expose access/refresh secrets in JSON. Token expiry or failed renewal must not delete a client's unsynchronized evidence queue.

Integrated H7 transport review must check the finite producing H2–H7 route set against the cookie selector before claiming Web workflow coverage. Current H2/H3 local contracts describe bearer transport; existing cookie adoption for inspection-task list/inbox/Defect routes does not establish cookie adoption for new FIELD/repair/map routes. Verify exact methods, GUIDs, trailing slash behavior, actor-bound CSRF and denied unrelated paths for any adopted extension.

## Storage, uploads and rendering

Record object endpoint, TLS, bucket, access policy, retention and checksum/versioning behavior. Preserve private Reporter evidence separation and task-scoped FIELD/repair access; a snapshot or handover grant does not grant general file access. Validate signed URL expiry, multipart resume, server verification and interrupted completion/recovery with real storage.

Set proxy/API upload limits, timeouts and buffering against the adopted upload contracts and part sizes. Stream bodies without materializing complete large files in memory. Reuse historical large-file evidence only after recording that its byte/stream/proxy/storage path is unchanged; otherwise execute a targeted large-file test. A metadata/domain edit alone does not require repeating an unrelated 8GiB experiment.

Verify export rendering with production-equivalent fonts, storage and runtime. An immutable export hash must remain stable after a live repair correction. Download access continues to require current resource permission.

## Workers and recovery

Inventory every enabled worker, source event registry, queue lease, retry policy, dead-letter record and external effect. `UploadSession:RecoveryEnabled` and `Anh02:WorkersEnabled` control observed existing worker registrations; recheck the final H6 notification scheduler/dispatcher configuration and ownership. Mock AI work remains explicitly non-production and is not real provider acceptance.

Start one registered consumer for each owned event family. Notification leasing must exclude processing/validation dispatch events. Unsupported or unresolved events remain visible and cannot be marked successfully delivered by a no-op handler. Crash/restart retries use the same occurrence and durable recipient effect, without duplicate business commands or new clock origins. Verify stale-lease fencing and current-authority receipt access.

Check readiness, database/object reachability and worker progress through the actual deployed monitoring surface. Record bounded queue age, retries, dead letters, unresolved recipients, overdue clocks and awaiting-receipt obligations. Delivery/read events cannot acknowledge a danger warning or receiving request; their production business ACK actions are separate. Weekly recovery emits one latest missed aggregate and keeps older periods as history. Logs use correlation IDs and safe reason codes, without tokens, keys, personal evidence or connection strings.

## Migration and recovery procedure

1. Review exact forward migration SQL, database schema/history and row audit before applying it. Existing applied migration identities must not be rewritten or duplicated. Validate fresh and populated upgrade on an isolated restored copy first.
2. Take and verify a SQL backup plus the corresponding object metadata/content, configuration and required server key-ring recovery material. Record a recoverable point and all write/worker coordination needed for consistency. Client-owned lost device keys remain outside organizational recovery capability.
3. Apply the reviewed forward migration in its recorded order under the deployment owner's authorization. Capture schema/migration history and rejected scope/backfill records. Unknown notification scope remains fail-closed; legacy zero measurements and expired/revoked credentials retain their actual meaning.
4. Run targeted authorization, intake/clock, receipt recovery, map pin, worker and export smoke checks. Release traffic only through the deployment owner's approved process.
5. If a populated Down migration refuses historical data loss, preserve the database and use a reviewed forward fix or tested restore procedure. Never drop history, truncate evidence or reset migration tables to bypass that guard.

Recovery rehearsals must include database, object references/content and server key rings, then verify protected credentials, hashes, immutable submissions, correction history, origin dedup, fan-out receipts and worker recovery. Record restore start/end, newest recoverable committed timestamp and data reconciliation results. A backup file's presence alone is not a successful restore.

## Required operational acceptance

| Target | Required measured evidence | Current status |
|---|---|---|
| 50 concurrent users; metadata server p95 ≤2s | Approved representative actor/project/workload mix, duration, dataset, error rate and measured server latency distribution at candidate SHA | NOT_VERIFIED |
| RPO ≤15min | Actual backup/log/object schedule and restore drill proving the newest recoverable consistent committed point | NOT_VERIFIED |
| RTO ≤4h | Timed end-to-end recovery including SQL, objects, keys, workers and business integrity smoke | NOT_VERIFIED |
| Actual browser/mobile behavior | Finite ticket renewal, persistent queue through kill/reboot, storage/camera failure, offline time uncertainty, network recovery and no deletion before durable ACK | NOT_VERIFIED |
| Exact final-SHA hosted CI | Successful hosted run whose head SHA equals the final delivered commit; preserve run URL and job outcomes | a56 attempt 1 FAILED (SQL607/41, API377/3/1 skip, unit907/2, full format); later documentation checkpoint requires its own SHA/run mapping |

Final RC delivery records these actual results and any remaining localized gates. Reviewable source and local passing checks do not imply deployed or full external acceptance.
