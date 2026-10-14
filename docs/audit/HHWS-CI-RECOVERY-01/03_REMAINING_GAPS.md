# HHWS-CI-RECOVERY-01 — Remaining Gaps

## Open

1. `H3FieldInspectionWorkflowHttpTests.Actual_FIELD_wire_unknown_intake_task_upload_supplement_and_current_authority_are_scoped(genericDownload: false)` still reports HTTP 500 at the task-scoped evidence download race. The generic download parameter passes. The downloaded TRX contains only the assertion stack and no server exception, so this needs a SQL-backed rerun with server logs before changing production code.
2. Run `38637687674` passes Unit, API, readiness/source-contract checks, SQL lanes 1–4 and the SQL aggregate identity gate. Run `38636706816` previously exposed an intermittent H3 task-scoped evidence download failure; it passed on the latest run, but the cause remains NOT_VERIFIED.
3. The Swagger evidence refresh is mechanical source anchoring, not proof of end-to-end API behavior. The latest API lane passed, while H3 reliability still needs diagnostic coverage if it recurs.

## Delivery state

The branch contains commits `e59027b`, `ced3910`, `c093ba6`, `73614b9`, `114f6fb`, `56e3281`, `694ecf6`, and `79dd5e0`. The latest state is **CI PASS** on run `38637687674`; H3 remains an intermittent NOT_VERIFIED root-cause gap for future diagnostics.

## Explicitly not changed

No workflow job was removed, no `continue-on-error`, skip/filter, source validator bypass, API contract deletion, migration, OpenAPI artifact, or unrelated untracked file was changed.
