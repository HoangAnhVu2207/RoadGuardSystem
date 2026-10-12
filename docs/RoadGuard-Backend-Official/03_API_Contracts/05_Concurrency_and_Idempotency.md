# 05 Concurrency and Idempotency

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Concurrency protects the specific resource being changed. Strong version/ETag or DTO ExpectedRowVersion is captured from the authoritative read/command result for that aggregate; invalid/missing precondition differs from a valid stale version. A stale409 requires reload current state and human/business reconciliation, not replacing the old expectedVersion and retrying blindly.

Idempotency binds operation, actor/scope, request fingerprint and durable effect. Protected successful replay must recheck current permissions/resource/source where the assigned rule applies; a historic receipt never grants access. Same key plus different intent must conflict. Do not generate a fresh key after timeout while outcome is unknown; use same payload/key and available read/reconcile, retain both old intent and current state on conflict.

Repository transactions include business changes, source/proof, receipt, audit and outbox where implemented. Some operations use specialized serializable/lock retry infrastructure; upload/provider operations have external bytes and recovery obligations beyond SQL. Whole sync batch is not atomic: per-item durable result and dependencies matter. Cryptographic signature does not establish historical UTC or bypass current assignment/device/account rules.

Repair command Run helpers bind If-Match/Idempotency-Key and return ETag; source repository dispatchers require exact state and resource ownership before effects/replay. Staff roster membershipVersion is a membership-row version, primary-PM PUT consumes ExpectedCurrentMembershipRowVersion; projects GET list does not return a general project command version. Geometry/segment/policy snapshots are immutable source pins, not mutable labels.

Consult each operation catalog's controller calls/service/repository chain and guard snippets. Shared helper source may include multiple branch errors; no single global list of409 codes or expected version type is assumed. Tests here are selected focused cases, not exhaustive serializability/model checking.
