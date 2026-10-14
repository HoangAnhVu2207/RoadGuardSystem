# HHWS-CI-RECOVERY-01 — Remaining Gaps

## Open

1. `H3FieldInspectionWorkflowHttpTests.Actual_FIELD_wire_unknown_intake_task_upload_supplement_and_current_authority_are_scoped(genericDownload: false)` still reports HTTP 500 at the task-scoped evidence download race. The generic download parameter passes. The downloaded TRX contains only the assertion stack and no server exception, so this needs a SQL-backed rerun with server logs before changing production code.
2. Final remote run `38636706816` passes Unit, readiness/source-contract checks, SQL lanes 1–4 and the SQL aggregate identity gate. API is PARTIAL: 481 passed, 1 failed, 1 skipped; the remaining H3 task-scoped evidence download is unresolved.
3. The Swagger evidence refresh is mechanical source anchoring, not proof of end-to-end API behavior; the final API run passed all other API tests, but this does not close the H3 runtime gap.

## Delivery state

The branch contains commits `e59027b`, `ced3910`, `c093ba6`, `73614b9`, `114f6fb`, `56e3281`, and `694ecf6`. The final state is **PARTIAL**, not full CI PASS, because the H3 HTTP runtime defect still needs a server-diagnostic fix.

## Explicitly not changed

No workflow job was removed, no `continue-on-error`, skip/filter, source validator bypass, API contract deletion, migration, OpenAPI artifact, or unrelated untracked file was changed.
