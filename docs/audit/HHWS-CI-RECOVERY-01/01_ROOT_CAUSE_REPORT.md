# HHWS-CI-RECOVERY-01 — Root Cause Report

## Audit boundary

Audited GitHub Actions runs `38617481283`, `38619380656`, and `38624372563` on `huy-review`. The three runs reproduce the same failure groups. TRX artifacts were downloaded from run `38624372563` under `C:\Temp\hhws-ci-recovery\38624372563`; no test result was inferred without a failing assertion and stack trace.

## Findings

| Test / file | Expected / actual | Judgment | Root cause and smallest correction |
|---|---|---|---|
| `ManualSwaggerContractTests.ProductionInventoryAndEvidenceExcerptsMatchCurrentSource` / `tests/RoadGuardSystem.ApiTests/Authentication/ManualSwaggerContractTests.cs:308+` | Source excerpt absent from current file | PARTIAL | Contract evidence JSON contained 61 stale excerpts after source files were split/edited. Refreshed only the missing evidence anchors against the current file and retained route/count assertions. |
| readiness validator / `planning/refactor/rf05/checks/verify_repository_readiness.py` | stale fingerprints and then generated line drift | PASS after correction | The requirements documents changed in commit `cdeb1ca1037744d06995058af13d0582f6273c0a` (documentation alignment banner/line movement). Updated three SHA-256 entries and 107 generated line references; validator still runs and passes. |
| `LD06LifecycleHttpTests.ActualDeclarationConfirmationEnforcesRoleVersionConflictAndRevokedReplay` / `tests/RoadGuardSystem.ApiTests/Projects/LD06LifecycleHttpTests.cs:68` | `Forbidden` / `OK` | SOURCE-CONFIRMED TEST DRIFT | `ProjectLifecycleRepository.LD06.cs:31-39` checks active user/role, while the current lifecycle policy does not require project membership for Supervisor. Existing global Supervisor tests establish this boundary. Test expectation now documents `OK` replay after membership end. |
| `H3FieldInspectionWorkflowHttpTests... (genericDownload: False)` / `...H3FieldInspectionWorkflowHttpTests.cs:274` | `OK` / `InternalServerError` | NOT_RESOLVED | The failing branch is the task-scoped evidence download race. The generic file route passes in the same run. The TRX has no server exception/response body, and Docker/SQL is unavailable locally; no speculative runtime change was made. |
| `OwnerWeeklyRecoverySqlTests.Completed_source_records_only_periods_when_review_was_pending...` / `...OwnerWeeklyRecoverySqlTests.cs:34` | completion accepted / `ArgumentException: Completion precedes the origin` | FIXED FIXTURE | The fixture pinned `2026-10-12`, earlier than the real `UtcNow` origin. The boundary is now derived from `NotificationCalendarPolicy.NextWeeklyReview(DateTimeOffset.UtcNow)`. |
| `H6WeeklyCalendarSqlTests.Preplanned...` and `ActualOpenNormalProposal...` / helper line 122 | one digest / empty | FIXED FIXTURE | Same historical fixed-Monday defect. Both tests now use the next controlled weekly boundary. |
| `LD06ResponsibilitySqlTests.PendingIssue...` / `...:75` | `Forbidden` / `OK` | SOURCE-CONFIRMED TEST DRIFT | `RepairWorkflowRepository.cs:207-218` explicitly exempts Supervisor from project membership for read/approval authority; global Supervisor coverage exists in H4 tests. Expectation now records replay `200`. |
| `LD06ResponsibilitySqlTests.WeeklyDigest...` / `...:198` | null / notification visible | SOURCE-CONFIRMED TEST DRIFT | `H6WeeklyReviewSourceAdapter.cs:42-51` makes Supervisor digest visibility role-global. The test now preserves visibility after one captured membership ends. |
| `H4RepairCoreMigrationTests.DecisionHeadsCorrectionRollbackImmutableHistoryAndPopulatedDowngrade` / `...:193` | `forbidden` / `success` | FIXED PRODUCTION AUTHORIZATION | Export snapshots are private project evidence. `ExportRepository` previously required membership only for PM; it now requires current membership for PM and Supervisor. |

## Security and authorization impact

The export correction closes an actual private-evidence scope defect. The LD06 and notification expectation changes preserve the source's explicit global Supervisor policy; they do not grant new runtime authority. The FIELD task-scoped download remains open and is explicitly reported as unresolved rather than masked.

## Environment limitations

Docker SQL Server was unavailable locally (`docker info` could not open the Docker Desktop pipe). Therefore API and SQL integration tests were not rerun locally; remote CI is the authority for those lanes.
