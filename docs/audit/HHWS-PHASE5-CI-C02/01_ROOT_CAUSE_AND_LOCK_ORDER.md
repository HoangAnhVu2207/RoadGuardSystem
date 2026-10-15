# C02 — Root cause and lock order

Baseline: `07222494417969070e8c5d56dcc78f243f795afb`; branch `huy-review`. Task owner: Codex, task-scoped writer. No API/schema/business policy changes. Labels: CURRENT_VERIFIED means observed source/executed SQL, HISTORICAL means C01 evidence.

## Judgment and evidence

Both failures are production lock-mode/order defects, not stale assertions. Notification source-first transactions and authority-first command/replay transactions request mutually incompatible update locks in the reverse order. C01 HTTP traces established ND01 SQL 1205 and H3 exact replay HTTP 500/SQL 1205; this task reproduces both cycles using real repositories and two SQL sessions.

| Transaction | Entry point / source | Isolation and retained lock order before fix | Conflicting transaction | Smallest change |
|---|---|---|---|---|
| ND01 eligibility | `RepairEligibilityRepository.ReadAsync`, lines 15–28; `ObligationResponsibilityScope.ResolveAsync`, lines 9–16 | Serializable: Users U → Roles U/range-U → Projects U → ProjectMembers range-U → ObligationResponsibilities range-U | Repair notification dispatch | Keep command guards unchanged |
| Repair notification | `H6NotificationDispatchRepository.CommitAsync`, lines 77–96; `H6RepairNotificationSourceAdapter.ResolveAsync`; `RecipientAuthorityAsync`, Delivery lines 113–151 | Serializable: Outbox U → item/binding/assignment/responsibility source locks → recipient Users/Roles/Projects/membership U | Eligibility retains authority and waits for responsibility | Read-only authority uses retained S/range-S |
| FIELD command versus FIELD dispatch (C01 additional graph) | `FieldInspectionWorkflowRepository.GuardAsync`, lines 131–153; `H6FieldNotificationSourceAdapter.ResolveAsync`, lines 28–46 | Command authority U then task/assignment U; worker Outbox/task/event/assignment U then recipient authority U | Roles ↔ FieldInspectionAssignments | Recipient authority now S; unchanged FIELD HTTP and negative source/assignment tests provide regression, separate deterministic graph reproduction NOT_RUN |
| H3 clock extension exact replay | `ClockDutyRepository.Guard`, lines 27–55; `Load`, lines 20–25; `ExecuteAsync`, lines 57–131; `IdempotencyOperationService` receipt access guard | Replay service-owned ReadCommitted transaction with explicit HOLDLOCK: Users U → Roles range-U → Projects/member U → DeadlineClocks U. New command remains Serializable | Deadline notification source-first dispatch | Keep receipt identity, current authority, version and body unchanged |
| Deadline notification | `H6DeadlineNotificationSourceAdapter.ResolveAsync`, lines 51–70; delivery recipient authority | Serializable: Outbox U → DeadlineClocks U → source proof/responsibility → Users U → Roles range-U | Replay retains Roles while waiting for clock | Same read-only authority change |
| Calendar / unresolved retry | `ObserveCalendarAsync`; `RetryUnresolvedAsync` → shared `RecipientAuthorityAsync` | Source proof then recipient authority | Same command family | Explicit read-only opt-in at source/recipient checks |

Current query locations: `RoadGuardSystem.Repositories/Idempotency/Anh02ReceiptAuthority.cs:9`, `RoadGuardSystem.Repositories/Implementations/Messaging/BusinessDutyRepository.cs:24`. Original command order remains Users → Roles → Projects → ProjectMembers. Source locks/outbox fence and atomic commit remain intact. No lock is released before transaction commit.

## Captured SQL graphs

Final baseline regression `before/before-proof.trx`: **2 FAIL (both SQL 1205), 3 PASS**. Production was temporarily restored byte-for-byte from baseline only for the red test; the six edited files were restored and the solution rebuilt before all after-fix verification.

### 2026-10-15T08:22:03.425Z

| Object / index | Owner mode | Waiter mode |
|---|---|---|
| `DeadlineClocks` / `PK_DeadlineClocks` | U | U |
| `Roles` / `PK_Roles` | RangeS-U | RangeS-U |

Processes: SPID 63 / read committed (2), SPID 62 / serializable (4).

### 2026-10-15T08:22:10.926Z

| Object / index | Owner mode | Waiter mode |
|---|---|---|
| `ObligationResponsibilities` / `PK_ObligationResponsibilities` | RangeS-U | RangeS-U |
| `Users` / `PK_Users` | U | U |

Processes: SPID 62 / serializable (4), SPID 63 / serializable (4).

These are exact `PK_Users ↔ PK_ObligationResponsibilities` and `PK_Roles ↔ PK_DeadlineClocks` cycles. C01 also captured Roles ↔ FieldInspectionAssignments; it uses the same recipient-authority boundary, but no separate new deterministic assignment-cycle case is claimed. Index names and modes come from SQL Extended Events, not inferred from EF naming. `ProjectMemberConfiguration` defines `IX_ProjectMembers_ProjectId_UserId`; no index/migration was added.

## Why the change breaks these cycles

`Anh02ReceiptAuthority.LockAsync(..., readOnly: true)` selects the same parameterized rows with HOLDLOCK, retaining shared/range shared authority locks until the caller commits. Only notification recipient/source-proof reads opt in; default remains UPDLOCK,HOLDLOCK. A worker authority S lock is compatible with a command authority U lock. Thus the worker can complete and release its business-resource U lock while the command waits for that resource. Authority revocation needs X and still waits for worker commit. This changes read lock mode at a proven read-only boundary; it does not remove validation or lower isolation.

The compatibility argument is limited to these observed U/S cycles. It is not a proof against concurrent authority writes, lock escalation, broad scans or every multi-party graph. SQL locking semantics: [Microsoft SQL Server locking guide](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-transaction-locking-and-row-versioning-guide?view=sql-server-ver17).

## Shared helper consumer audit

All 23 direct helper call sites and qualified/unqualified CurrentAuthority consumers were inspected. Only the explicit opt-ins below change behavior. Calendar and transferred appointment checks read authority; neither writes Users/Roles/Projects/ProjectMembers. Source adapters called by commands preserve any U locks already held by those commands; S acquisition does not downgrade an existing U lock.

| Consumer group | Source files / call sites | Result |
|---|---|---|
| Exports | `RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs`:45 | Default U unchanged |
| Inspections | `RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.Business.cs`:96 | Default U unchanged |
| Inspections | `RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.cs`:134 | Default U unchanged |
| Messaging | `RoadGuardSystem.Repositories/Implementations/Messaging/BusinessDutyRepository.cs`:27 | Default U unchanged (BusinessDuty forwards explicit caller mode) |
| Messaging | `RoadGuardSystem.Repositories/Implementations/Messaging/ClockDutyRepository.cs`:29,50 | Default U unchanged |
| Messaging | `RoadGuardSystem.Repositories/Implementations/Messaging/H6DeadlineNotificationSourceAdapter.cs`:82 | Read-only S opt-in; policy/source validation retained |
| Messaging | `RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Calendar.cs`:89 | Read-only S opt-in; policy/source validation retained |
| Messaging | `RoadGuardSystem.Repositories/Implementations/Messaging/H6NotificationDispatchRepository.Delivery.cs`:124,130 | Read-only S opt-in; policy/source validation retained |
| Messaging | `RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.cs`:127 | Default U unchanged |
| Messaging | `RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Operations.cs`:23 | Default U unchanged |
| Messaging | `RoadGuardSystem.Repositories/Implementations/Messaging/H6RepairNotificationSourceAdapter.cs`:52 | Read-only S opt-in; policy/source validation retained |
| Offline | `RoadGuardSystem.Repositories/Implementations/Offline/OfflineAdmissionValidator.cs`:93 | Default U unchanged |
| Offline | `RoadGuardSystem.Repositories/Implementations/Offline/OfflineWorkflowRepository.cs`:31 | Default U unchanged |
| Processing | `RoadGuardSystem.Repositories/Implementations/Processing/Anh02AiRepository.cs`:160 | Default U unchanged |
| Projects | `RoadGuardSystem.Repositories/Implementations/Projects/GeometryWorkflowPersistenceService.cs`:334 | Default U unchanged |
| Projects | `RoadGuardSystem.Repositories/Implementations/Projects/PavementWorkflowRepository.cs`:41 | Default U unchanged |
| Projects | `RoadGuardSystem.Repositories/Implementations/Projects/ProjectLifecycleRepository.cs`:38 | Default U unchanged |
| Projects | `RoadGuardSystem.Repositories/Implementations/Projects/ProjectLifecycleRepository.LD06.cs`:36 | Default U unchanged |
| Repairs | `RoadGuardSystem.Repositories/Implementations/Repairs/RepairEligibilityRepository.cs`:19 | Default U unchanged |
| Repairs | `RoadGuardSystem.Repositories/Implementations/Repairs/RepairPolicyRepository.cs`:27 | Default U unchanged |
| Repairs | `RoadGuardSystem.Repositories/Implementations/Repairs/RepairSafetyRepository.cs`:240 | Default U unchanged |
| Repairs | `RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.cs`:209 | Default U unchanged |
| Repairs | `RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.Producing.cs`:689,752 | Default U unchanged |
| Repairs | `RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.ReviewRequests.cs`:89 | Default U unchanged |
| Repairs | `RoadGuardSystem.Repositories/Implementations/Repairs/RoadCoverageRepository.cs`:23 | Default U unchanged |
| Reporting | `RoadGuardSystem.Repositories/Implementations/Reporting/DefectStatisticsRepository.cs`:24 | Default U unchanged |
| Retention | `RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs`:260,276 | Default U unchanged |

Unqualified BusinessDuty command/list/read/assignee checks (lines 89, 99, 171, 187) continue default U. ClockDuty actor/assignee, repair producer transferred duty and protected notification operations also retain default U. Offline admission/device/provenance checks are unchanged. Neither NORMAL Supervisor final nor FT PM final authority changes. IdempotencyOperationService, rowversion checks and H6 claim/lease code have no production diff.

## Security and remaining risk

Three two-session tests separately attempt user, role and membership revocation while the worker is paused before SaveChanges/commit: SQL 1222 at a bounded 1-second lock timeout, then the same write succeeds after worker commit (rolled back to preserve fixtures). Existing full-suite revoked actor/device, payload mismatch, wrong project, repair approval and lease ownership cases remain required; see [verification](02_FIX_AND_REGRESSION_EVIDENCE.md).

See [remaining risks](03_REMAINING_RISKS.md). Independent JEV is pending; this is implementation/self-review evidence.

## Exact graph process evidence

### 2026-10-15T08:22:03.425Z

- SPID 63, transaction user_transaction, isolation read committed (2), wait KEY: 5:72057594044809216 (8c01370439ec).

```sql
(@p0 uniqueidentifier)SELECT TOP(2) [r].[Id], [r].[AcknowledgedAt], [r].[AcknowledgedByUserId], [r].[AcknowledgmentEventId], [r].[AppointedActorId], [r].[AppointedRole], [r].[CompletedAt], [r].[CurrentDueAt], [r].[Kind], [r].[OriginAt], [r].[OriginEventId], [r].[OriginalDueAt], [r].[ProjectId], [r].[RowVersion], [r].[TargetId]
FROM (
    SELECT * FROM [DeadlineClocks] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]=@p0
) AS [r]
ORDER BY [r].[Id]
```

- SPID 62, transaction user_transaction, isolation serializable (4), wait KEY: 5:72057594043629568 (f09274f7438e).

```sql
(@p0 nvarchar(4000))SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]=@p0
```

### 2026-10-15T08:22:10.926Z

- SPID 62, transaction user_transaction, isolation serializable (4), wait KEY: 5:72057594053525504 (ffffffffffff).

```sql
(@p0 uniqueidentifier)SELECT TOP(2) [r].[ObligationId], [r].[AcceptanceActionId], [r].[CurrentProjectId], [r].[OriginProjectId], [r].[RowVersion]
FROM (
    SELECT * FROM [ObligationResponsibilities] WITH (UPDLOCK,HOLDLOCK) WHERE [ObligationId]=@p0
) AS [r]
```

- SPID 63, transaction user_transaction, isolation serializable (4), wait KEY: 5:72057594044219392 (983121dc560c).

```sql
(@p0 uniqueidentifier)SELECT TOP(2) [r].[Id], [r].[AccessFailedCount], [r].[ConcurrencyStamp], [r].[CreatedAt], [r].[DisplayName], [r].[Email], [r].[EmailConfirmed], [r].[LastLoginAt], [r].[LockoutEnabled], [r].[LockoutEnd], [r].[MustChangePassword], [r].[NormalizedEmail], [r].[NormalizedUserName], [r].[PasswordHash], [r].[PhoneNumber], [r].[PhoneNumberConfirmed], [r].[RoleCode], [r].[RowVersion], [r].[SecurityStamp], [r].[Status], [r].[SuspendedAt], [r].[TwoFactorEnabled], [r].[UserName]
FROM (
    SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]=@p0
) AS [r]
```


Historical inputs: [C01 root cause](../HHWS-PHASE5-CI-C01/ROOT_CAUSE_REPORT.md), [C01 verification](../HHWS-PHASE5-CI-C01/FIX_AND_REGRESSION_EVIDENCE.md), [C01 remaining failures](../HHWS-PHASE5-CI-C01/REMAINING_FAILURES.md). C02 uses current source and its own red/green tests to verify the fix.
