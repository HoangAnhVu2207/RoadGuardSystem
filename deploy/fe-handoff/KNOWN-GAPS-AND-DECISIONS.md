# Release gates and known gaps

Disposition: BE_HANDOFF_PARTIAL. No independent REVIEW_PASS. No mandatory feature is silently excluded.

| Gate | State | Required owner/external decision or implementation |
|---|---|---|
| A AI issuer | BLOCKED_RULE | Owner must confirm issuer/minting principal, attempt/audience binding, rotation and failure protocol. Presets are synthetic; no real YOLO acceptance. |
| B native Fast Track notification | BLOCKED_RULE | B2 PM + Supervisor is confirmed; choose native trigger, all-vs-assigned/current resolution, dedup/fallback; existing legacy notifications do not prove native feature completion. |
| C publication | BLOCKED_RULE | C1 controlled approved representation/private originals is confirmed; choose approval actor, fields/media/redaction/audience and withdrawal. No inferred anonymous release. |
| D retention | BLOCKED_RULE | D1 no automatic orphan deletion is confirmed; choose duration/manual quarantine/delete/hold/regeneration parameters; no destructive cleanup enabled to make demo pass. |
| Web/FE consumers | EXTERNAL_NOT_VERIFIED | Secure-cookie/CSRF browser behavior over HTTPS, /me vs /profile compatibility, Vite, Expo Go/Development Build, emulator and real phone need consumer tests. Loopback binding is intentional. |
| Full workflow demo seed | IMPLEMENTATION_MISSING | Current pristine seeder creates actors, memberships and projects only; full confirmed road/incident lifecycle setup needs separate executable fixtures/acceptance. No fabricated business states inserted. |
| 241 contracts | PARTIAL | Historical 111 AUTH_ONLY,116 PARTIAL,14 FULL candidates; independent acceptance absent. Task31 does not rerun all or promote status. See per-API ledger and exact missing dimensions. |
| P1-02 | BOUNDED_REPAIRED_PENDING_REVIEW | Earlier stale callback fence retained; The historical atomic fence is retained; see Task31 master for focused fresh regression. Issuer gate remains separate. |
| P1-04 | TEMPORAL_BOUNDED_PENDING_REVIEW | TTL exactly 300 seconds; revocation blocks new access/new URL, existing links retain original expiry. Task30 multipart/private bytes tests are not a fresh temporal/indirect privacy closure. |
| External email | NOT_RUN | Mailpit local capture only; no Gmail, SMS or production SMTP. |
| Resources/portability | BOUNDED | First-boot rebuild needs Internet (NuGet/Go/Alpine/Microsoft). Runtime observed on 4 GiB/1 GiB WSL; not load testing or an unconditional minimum-resource guarantee. |
| KnowledgeSpace | DENY_ALL | Test-runner remains disabled; local evidence retrievability does not prove remote connector availability. |

Readiness checks SQL/private bucket, not external providers or complete worker readiness. /health is liveness. SQL SA is isolated behind an internal network; local Docker administrators can read private volumes. Demo is not production deployment guidance.

Task31 NEW-instance worker scope: upload verification, PDF export and non-deleting retention evaluation are enabled; AI mock worker remains disabled. Real worker acceptance is recorded separately from /ready. Existing persisted config is not overwritten. No full road/incident/evidence workflow seed is provided.
