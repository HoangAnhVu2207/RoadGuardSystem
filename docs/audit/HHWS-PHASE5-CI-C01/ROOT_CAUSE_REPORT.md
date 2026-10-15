# HHWS-PHASE5-CI-C01 — Root cause report

Baseline: `6ba9035f3cedb10d7f4167059520f869ec123b98`, branch `huy-review`.
Evidence collected 2026-10-15. Labels: `CURRENT_VERIFIED` for inspected source/executed observations, `HISTORICAL` for earlier commit results, `PROPOSED` for unfixed lock-order changes, `UNKNOWN` for unavailable historical plans/stacks. Scope: CI diagnosis, H6 claim-test correction and failure diagnostics; no production changes. Verdict: **PARTIAL**, pending resolution of the reproduced ND01/H3 deadlocks. Passing subsequent runs cannot by themselves close these intermittent defects.

## Historical CI evidence

Downloaded complete job logs, failed logs, job metadata and TRX artifacts using `gh run view <id> --log`, `gh run view <id> --log-failed`, `gh run view <id> --json jobs,headSha,attempt,status,conclusion` and `gh run download`.

| Run / attempt | Exact SHA | Final result and failures |
|---|---|---|
| [38691928986](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38691928986), 1 | `6daf4fe098495622d7294d998d485227cb9fbcee` | API FAIL: 479 passed, 2 failed, 1 skipped / 482; all other jobs successful |
| [38694140711](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38694140711), 1 | `6ba9035f3cedb10d7f4167059520f869ec123b98` | API 481/0/1, Unit 966/0/0, source PASS; SQL lanes 1–3 PASS (130,135,215); lane 4: 190 passed / 1 failed / 191; SQL aggregate FAIL |

The second run is completed, not an incomplete snapshot. Its only failing assertion is H6 concurrent claim. Failed aggregate reflects the failed lane, not an additional test failure.

## C01-01 — ND01 SQL deadlock: OPEN

Test: `ND01OriginalWindowHttpTests.ActualBearerSignedSyncCommitsEligibleOriginalWindowFtExactlyOnce`, [test](../../../tests/RoadGuardSystem.ApiTests/Offline/ND01OriginalWindowHttpTests.cs), baseline line 29. Expected: fixture prepares eligible original-window FT data, then signed bearer sync commits once. Actual: preparation throws `InvalidOperationException` wrapping SQL 1205 (historical victim process 66). Historical stack: [ND01OriginalWindowFixture.Prepare](../../../tests/Shared/ND01OriginalWindowFixture.cs):71 → FIELD acceptance / assignment guard. No signed HTTP batch has been sent at that point; this failure does not prove signature or sync authorization is wrong.

Local full API diagnostic run reproduces SQL 1205, process 75, at `ND01OriginalWindowFixture.Prepare`:100 → `RepairEligibilityRepository.ReadAsync` → `ObligationResponsibilityScope.ResolveAsync`. This is the same failing test/deadlock family but a different preparation stage; the exact historical graph was not available in GitHub artifacts.

Captured `system_health` event `2026-10-15T07:00:44.146Z`: both sessions serializable; process 75 owns Users U and waits on ObligationResponsibilities RangeS-U; process 55 owns ObligationResponsibilities RangeS-U and waits on Users U. Another graph at `07:00:41.645Z` contains Roles ↔ FieldInspectionAssignments, consistent with the historical assignment-guard stack, without proving historical victim equivalence.

Classification: **source-level lock-order contention observed through a fixture**, amplified by shared API database/background-worker activity. Not established as a fixture-only defect. Preparation uses a nonretrying context; retrying the entire multi-step preparation is not proven safe because earlier commands can already have committed. No blanket fixture retry added.

Actor/resource: PM, Crew, Supervisor, Reporter and a freshly seeded project; preparation uses current authority, not a cross-project account. Clock uses `TimeProvider.System`. Later signed sync K/fence assertions remain intact. Smallest safe follow-up: deterministic two-session reproduction and a consistent authority/source lock ordering across the implicated command and notification transactions, preserving membership/revocation checks.

## C01-02 — H3 clock-extension replay HTTP 500: OPEN

Test: `H3FieldInspectionWorkflowHttpTests.Actual_FIELD_wire_unknown_intake_task_upload_supplement_and_current_authority_are_scoped(genericDownload: False)`, [test](../../../tests/RoadGuardSystem.ApiTests/Inspections/H3FieldInspectionWorkflowHttpTests.cs), baseline line 274. Expected replay HTTP 200; historical actual HTTP 500. Historical TRX did not expose the server exception, so historical root cause alone was UNKNOWN.

New diagnostics reproduce the same step with HTTP 500, `code=internal_error`, correlation ID `b4b13e42-d562-4f21-b5c7-964c9df84022`, route `/api/v1/projects/27e75971-e344-447c-83c5-26419c8ecea2/clocks/1e1f33e6-bd27-4f5d-b1ce-74fb7a16ccf0/extensions`. Server stack: SQL 1205 victim 53 → `ClockDutyRepository.Load`:22 → `Guard`:30 → `IdempotencyOperationService.MapAuthorizedAsync`:283 → `ExecuteCoreAsync`:102 → clock repository/controller. This establishes the cause of the local reproduction; it cannot retroactively prove the historical exception.

Graph `2026-10-15T07:00:59.773Z`: replay session 53 is read committed, holds Roles RangeS-U through explicit HOLDLOCK, waits for DeadlineClocks U. Opposing serializable session 56 holds DeadlineClocks U and waits on Roles RangeS-U. The actor is the current Supervisor; K, request body and original review-clock version are identical to the successful first extension. Original due time remains unchanged; requested current due = original due + 4 hours. This is not a version/auth denial to be changed to success artificially.

Source ordering evidence at baseline:

- [ClockDutyRepository.Guard](../../../RoadGuardSystem.Repositories/Implementations/Messaging/ClockDutyRepository.cs):27–30 locks current authority before clock; `ExecuteAsync`:67 delegates to receipt service.
- [Anh02ReceiptAuthority.LockAsync](../../../RoadGuardSystem.Repositories/Idempotency/Anh02ReceiptAuthority.cs):10–25 locks Users → Roles → Projects → ProjectMembers.
- [H6NotificationDispatchRepository.CommitAsync](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Commit.cs):75–95 starts serializable transaction, locks Outbox, resolves source, then delivery checks recipient authority.
- [H6DeadlineNotificationSourceAdapter.ResolveAsync](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6DeadlineNotificationSourceAdapter.cs):51 and [RecipientAuthorityAsync](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Delivery.cs):115 show the opposite clock/source → authority ordering. Graph identifies SQL statements, not a managed stack for the opposing session; its exact worker call site is inferred from these source paths.
- [H6NotificationWorker](../../../RoadGuardSystem.API/Workers/H6NotificationWorker.cs):27–39 observes clocks and processes notifications; it is registered at [ServiceCollectionExtensions](../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs):138, also in test hosts.
- [IdempotencyOperationService](../../../RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs):271–297 wraps receipt guard exceptions to prevent execution-strategy retries. [ReceiptAccessGuardSqlTests](../../../tests/RoadGuardSystem.IntegrationTests/Persistence/ReceiptAccessGuardSqlTests.cs):120 explicitly requires retryable guard exceptions to propagate without retry.

Classification: **reproduced source concurrency defect**, not a missing fake clock or wrong expected HTTP code. Proposed fix requires consistent lock order, including notification clock observation/outbox access; changing only one authority lock or adding catch/retry would leave other captured cycles and may alter security guarantees. No production fix claimed in C01.

## C01-03 — H6 concurrent claim assertion: CORRECTED TEST CONTRACT

Test: `H6NotificationClaimSqlTests.ConcurrentWorkersCannotOwnTheSameUnexpiredMessage`, [test](../../../tests/RoadGuardSystem.IntegrationTests/Notifications/H6NotificationClaimSqlTests.cs), baseline line 71. Expected both simultaneous polls non-null; actual one null in run 38694140711. No SQL exception or duplicated lease in that TRX.

[H6NotificationDispatchRepository.ClaimAsync](../../../RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.cs):21–56 owns a short read-committed transaction and uses `UPDLOCK,READPAST,READCOMMITTEDLOCK,ROWLOCK`, TOP 1 and ordering. READPAST skips locked rows/keys: an idle result is allowed even when another transaction is claiming work. A sort/scan may lock more than the selected row; the historical execution plan was not captured, so that precise plan is NOT_VERIFIED. See [Microsoft SQL Server table hints](https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table?view=sql-server-ver17).

Classification: **fixture/assertion contract bug**. Preserve simultaneous claims, require at least one claim, then allow exactly one additional poll for each initially null result after both transactions have completed (maximum two calls, no retry loop). Still require both seeded IDs, distinct fences, persisted matching lease owners/expiries, one attempt per message and an idle third claim. Added a deterministic separate-transaction locked-queue test: null while row is held, recovery after rollback, no duplicate unexpired claim. This proves null is not lease ownership; it does not claim to capture the historical query plan.

No business actor, project authorization, receipt K or deadline participates in this protocol-only claim fixture. Real UTC and a two-minute lease remain. No production claim SQL or lease policy changed.

## Local environment distinction

One full diagnostic API execution: 424 passed, 57 failed, 1 skipped / 482. Of the failures, 55 are SQL-container startup `ContainerNotRunningException` exit 255; the other two are the reproduced ND01/H3 failures above. Docker VM has 3.783 GiB and existing staging workloads. Resource pressure is a supported environmental explanation, not a proven root cause of every exit 255 (OOM events not retained). A subsequent full API run uses local runner `xUnit.MaxParallelThreads=1` to bound concurrent SQL containers; it does not filter tests or change CI configuration.

Raw logs/TRX/XE graphs remain outside git under `%TEMP%/hhws-p5-ci-c01`; hashes and executed results are in [regression evidence](FIX_AND_REGRESSION_EVIDENCE.md). No credentials/connection strings are copied into this report. Existing staging containers and unrelated untracked files are preserved.

## Portable deadlock evidence

Reduced extracts from the retained local XE file, with database prefix omitted and SQL payloads omitted. Process IDs, isolation and lock owners/waiters are copied from the graphs, not reconstructed from assumptions. Full-file SHA-256 is in the evidence report.

```xml
<deadlock-extract timestamp="2026-10-15T07:00:59.773Z">
  <victim id="processd80063468" />
  <process id="processd80063468" spid="53" isolationlevel="read committed (2)" lockMode="U" waitresource="KEY: 5:72057594044809216 (0810127b3f17)" />
  <process id="processd80063088" spid="56" isolationlevel="serializable (4)" lockMode="RangeS-U" waitresource="KEY: 5:72057594043629568 (f09274f7438e)" />
  <keylock objectname="DeadlineClocks" indexname="PK_DeadlineClocks" mode="U">
    <owner id="processd80063088" mode="U" />
    <waiter id="processd80063468" mode="U" requestType="wait" />
  </keylock>
  <keylock objectname="Roles" indexname="PK_Roles" mode="RangeS-U">
    <owner id="processd80063468" mode="RangeS-U" />
    <waiter id="processd80063088" mode="RangeS-U" requestType="wait" />
  </keylock>
</deadlock-extract>
<deadlock-extract timestamp="2026-10-15T07:00:44.146Z">
  <victim id="processd916264e8" />
  <process id="processd916264e8" spid="75" isolationlevel="serializable (4)" lockMode="RangeS-U" waitresource="KEY: 5:72057594053525504 (ffffffffffff)" />
  <process id="processd91626108" spid="55" isolationlevel="serializable (4)" lockMode="U" waitresource="KEY: 5:72057594044219392 (82162bc65587)" />
  <keylock objectname="ObligationResponsibilities" indexname="PK_ObligationResponsibilities" mode="RangeS-U">
    <owner id="processd91626108" mode="RangeS-U" />
    <waiter id="processd916264e8" mode="RangeS-U" requestType="wait" />
  </keylock>
  <keylock objectname="Users" indexname="PK_Users" mode="U">
    <owner id="processd916264e8" mode="U" />
    <waiter id="processd91626108" mode="U" requestType="wait" />
  </keylock>
</deadlock-extract>
```
