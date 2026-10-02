# Closure matrix

| Finding/task | Current status | Evidence | Blocker or next action |
|---|---|---|---|
| A08-01 callback identity/fingerprint | OPEN; bounded runtime confirmed | Historical handoff final TRX 13/13; current source inventory and design matrix | Owner must approve identity domain, canonical fingerprint v2, legacy receipt policy, provider key scope, and rollout/rollback before production change |
| A08-02 stored validation failure replay | FIXED in source; focused verification required/recorded | `CreateValidationAsync` now preserves stored failure status on replay; focused tests/build logs in package | Add HTTP+SQL success/failure replay regression to normal gate; no schema/contract change |
| A09-01 lease exhaustion/recovery | BLOCKED, source inspected | `OutboxWorkRepository.TryLeaseNextAsync`; `OutboxMessage.AcquireLease/ScheduleRetry`; assessment matrix | Owner must choose crash-after-final-lease policy and identify dispatcher before implementation |
| A09-02 lease ownership/fencing | BLOCKED, source inspected | `OutboxMessage.EnsureLeaseOwner`, `CompleteLease`, `ScheduleRetry` | Owner must approve generation/token and expiry semantics; likely schema change is outside allowlist |
| RF-10-05 parent | PARTIAL | `RF-10-05-C01`, A08-01 evidence, this packet | Provider/CG11/late attempt/external ownership remain |
| RF-10-09 parent | PARTIAL | notification/outbox assessment and this packet | Dispatcher, reporting, retention decision gates remain |
| RF-11 parent | PARTIAL | RF-11-C01 and task definition | Release/consumer/retirement gates not complete |

Statuses are task-level dispositions, not R01 or parent Done claims.
