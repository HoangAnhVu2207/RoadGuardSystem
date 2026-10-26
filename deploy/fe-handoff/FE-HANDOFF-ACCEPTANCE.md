# Task32 extracted release acceptance

This document defines the scope carried by the immutable ZIP. Actual timestamped commands/outcomes, ZIP SHA and complete evidence are in the external Task32 MASTER-HANDOFF and EVIDENCE-INDEX. Task30/31 tests are historical; do not add their counts to Task32.

Acceptance requires a fresh separately owned extraction, Compose build/up, SQL ownership, migrations, generated private accounts, real scenario initialization, exact Swagger schema, profile/auth/CORS, private MinIO read/write, inbox response, PDF worker completion, repeat-up/restart idempotency, dependency readiness failures and source/volume preservation. Resource constraints require sequential instance execution. Test harness project names are isolated from existing FE projects; they do not change the user's one-command startup.

Classification remains FE_INTEGRATION_PREVIEW if any required source-backed workflow demonstration or external consumer gate is not verified. Do not infer FULL_CONTRACT from smoke or worker health. A1/B2/C1/D1 pending parameters are disabled and listed separately. External FE browser/native and production readiness are NOT_RUN. Runner DENY_ALL.
