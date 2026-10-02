# RF-10 A08-02 one-week delivery

## Change

`ProcessingV2PersistenceService.CreateValidationAsync` now exposes `Replayed` only for a stored successful validation outcome. Stored `Conflict` and `InvalidInput` outcomes remain their original persistence status, so the existing controller keeps its 409/422 error mapping. No schema, migration, public contract, CI, fixture framework, shared database, or provider changed.

## Verification

Focused SQL Server Testcontainers test `A0802ValidationReplayTests` uses the existing `IdentitySqlServerFixture` and production `IdempotencyOperationService` path:

- Stored Conflict: first and replay are `Conflict`; one scoped receipt with unchanged fingerprint/outcome.
- Stored InvalidInput: first and replay are `InvalidInput`; one scoped receipt with unchanged fingerprint/outcome.
- Stored Success: replay is `Replayed`; validation run and `validation_run.dispatch` outbox counts do not increase; receipt remains scoped to actor/project/operation/key.

Final result: **3 passed, 0 failed, 0 skipped**. The first setup run failed before idempotency because test Roles were not seeded; that failed TRX is retained. After using the existing fixture role seeder, the project rebuilt successfully and the focused run passed.

## Scope limits

The HTTP controller mapping is unchanged and is source-consistent with 409/422; this slice does not add a new HTTP fixture or rerun A08-01. A08-01 remediation/legacy compatibility, A09-01, A09-02, RF-10/RF-11 aggregate audit and release/retirement work are `REMOVED_FROM_ONE_WEEK_SCOPE, chưa triển khai`; their findings remain OPEN/KNOWN_LIMITATION and do not block this one-week delivery.
