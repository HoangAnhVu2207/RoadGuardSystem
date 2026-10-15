# HHWS-PHASE5-CI-C01 — Remaining failures

Status: **PARTIAL**. H6 claim assertion corrected with bounded recovery and ownership regression. ND01 and H3 deadlocks are reproduced and remain open; intermittent green focused/CI executions cannot establish their absence.

| Entry | Verified evidence | Remaining action / proposed owner |
|---|---|---|
| C01-01 ND01 fixture/worker deadlock | Historical SQL1205 during FIELD acceptance; local SQL1205 during eligibility; Users ↔ ObligationResponsibilities graph | Backend/test owners: deterministic two-session source regression and consistent lock order; prove fixture recovery is safe before adding retries |
| C01-02 H3 replay 500 | Local server exception SQL1205 in receipt authority guard; Roles ↔ DeadlineClocks graph, correlation ID recorded in root report | Backend concurrency/security owners: fix lock order across commands, notification dispatch/observation without weakening current authority or replay guard semantics |
| C01-03 H6 historical null claim | Deterministic locked-row null/recovery and concurrent persisted ownership checks | Independent JEV review of corrected test contract; no claim-SQL runtime change |
| Environment | 55 SQL-container startup failures in initial local full API run; 3.783 GiB Docker VM | Local test capacity/parallelism only; final bounded full-run result tracked separately |

Additional captured graphs include FieldInspectionTasks ↔ Users, FieldInspectionTasks ↔ H6NotificationEventReceipts, DeadlineClocks ↔ Outbox index ranges, and AiMockRuns contention. These are diagnostic leads, not independently closed defects or proof of additional failed CI assertions.

No test skip/filter was added to CI; no SQL lane, source check, authorization guard, idempotency check or expected HTTP result was removed. Existing one skipped API test is baseline behavior and is not new C01 suppression. No Phase 6, MVP GAP implementation or canonical document rewrite performed.

Exact delivery SHA and new GitHub CI job outcomes must be read from the final delivery response/linked Actions run; this report does not predict results before push. A green run would verify that commit's execution, while C01-01/C01-02 remain open until a source fix and deterministic regression demonstrate resolution.

See [root cause](ROOT_CAUSE_REPORT.md) and [commands/results](FIX_AND_REGRESSION_EVIDENCE.md).
