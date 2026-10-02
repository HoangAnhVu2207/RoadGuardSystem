# RF-09 contract and data transition templates

Copy one checklist per approved RF-10 package. Fill each `TBD` with an evidence path, owner decision, command/result and version before advancing a gate. A proposed target is not the current contract. Do not relock the FE snapshot or edit an applied migration to make a check pass.

## A. Public contract package

| Field | Value to record |
|---|---|
| Package / owner / approval | ID; named owner and exact scoped approval; current and target status (`CURRENT_VERIFIED`, `TARGET_CONFIRMED`, `PROPOSED`, `UNKNOWN`). |
| Producer and consumers | BE operation/service; registry IDs, actual app/SDK versions, owner confirmation and request/response/contract-test samples. Unknown outside-repo consumer stays UNKNOWN. |
| Current -> target operation | Method, route/version, actor/project scope, security scheme, path/query/header/body, status and response schema for every affected operation. Link code and approved contract; attach parsed guard output. |
| Wire details | Error code/casing, content type, correlation/ETag/idempotency/deprecation headers; required versus nullable, enum values, integer range/format, arrays/empty arrays, ordering and pagination. Capture actual HTTP status/body/headers for current and target. |
| Compatibility | Breaking/additive/retirement classification, old/new reader and writer, coexistence/version route or feature flag, fallback, deprecation notice and **window: PROPOSED/TBD until owner confirms**. |
| Cutover and retirement | Client rollout sequence, dual-run comparison, telemetry/acknowledgement threshold (TBD), cutover trigger, rollback trigger, supported old endpoint duration and final retirement approval. |
| Gates | START: approved task scope + baseline. INTEGRATE: contract owner + real consumer fixtures + green parsed guard/HTTP tests. RELEASE: deployed consumer receipt and explicit public-change approval; no silent lock regeneration. |

`python tests/Tooling/rf09_transition_guard.py --old-openapi <old.yaml> --new-openapi <new.yaml> --old-schema <old-inventory.json> --new-schema <new-inventory.json> --tables <affected>` is a conservative review aid. Exit 2 means breaking, unsupported or invalid input; exit 0 only means no detected issue among modeled fields. It does not resolve `$ref` behavior, composition (`allOf`/`oneOf`), request/response direction, default semantics, auth scope or client compatibility. The existing FE hash checker remains separate and currently has a known lock mismatch (`00-baseline.md`).

## B. SQL/data package

| Field | Value to record |
|---|---|
| Current/target and approval | Current EF model, full migration chain/snapshot, owned migrated SQL catalog and authorized deployed catalog; target DDL/ERD delta with decision source. Deployed state UNKNOWN until inspected with permission. |
| Affected objects | Table/column SQL+CLR types, nullability, PK/alternate keys, FK/delete actions, index key order/include/unique/filter, check expression/enabled, default/computed/generation, trigger definition/events/side effects and dependent code/exports. Run focused catalog comparison for every affected object. |
| Old-row audit | Authorized aggregate counts and samples for null, duplicate, orphan, out-of-range, invalid enum and violated invariant; migration history and trigger state. No private rows or secrets in reports. |
| Expand | Only approved additive DDL or compatible widening; backup and schema/data fingerprint first. Test old reader/writer and new reader/writer against the *same* candidate schema. Stop if either supported binary breaks. |
| Backfill | Bounded batch size and key order; durable checkpoint/high-water mark; conditional update for idempotency; concurrent-write strategy; retry/deadlock policy; trigger side effects; per-batch counts and failures. No blanket dual-write unless compatibility actually requires it. |
| Verify | Re-run backfill, inject failure/restart at a checkpoint, compare ordered keys/values and cryptographic digest plus counts/constraints/FK/index/trigger behavior; verify outbox/audit and old/new reads. Synthetic proof and deployed row audit are distinct. |
| Cutover/contract | Consumer rollout and write flag; authorized maximum size/state change only after old binaries are drained; separate destructive drop/rename approval and retention hold check. Never delete an applied migration. |
| Recovery | SQL BACKUP before candidate and RESTORE to a *different owned database* for rehearsal; compare schema, keys, values and digest. Production restore needs separate authority and loses writes since backup unless replay exists. Prefer approved forward repair when new writes cannot fit old schema. Define stop-traffic/feature flag and RPO/RTO evidence. |
| Gates | START: owner scope, baseline, data/consumer questions. INTEGRATE: candidate SQL and old/new read/write tests in owned DB, replay/restart/restore. RELEASE: deployed audit, approved runbook, backup, monitoring and rollback feasibility. |

The RF-09 `Rf09TransitionRehearsalTests` implement a **generic** bounded backfill/restart and real SQL backup/restore on an owned migration-derived database, plus a separate test-only `Files` widening candidate. These tests do not prove a future production migration or an 8 GiB storage upload. Test-only SQL candidates must stay outside EF migration files.
