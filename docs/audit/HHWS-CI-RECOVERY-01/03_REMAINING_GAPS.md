# HHWS-CI-RECOVERY-01 — Remaining Gaps

## Open

1. `H3FieldInspectionWorkflowHttpTests.Actual_FIELD_wire_unknown_intake_task_upload_supplement_and_current_authority_are_scoped(genericDownload: false)` still reports HTTP 500 at the task-scoped evidence download race. The generic download parameter passes. The downloaded TRX contains only the assertion stack and no server exception, so this needs a SQL-backed rerun with server logs before changing production code.
2. API and SQL lanes were not rerun on this workstation because Docker SQL Server is unavailable. A pushed CI run is required to classify whether any additional failures remain after these changes.
3. The Swagger evidence refresh is mechanical source anchoring, not proof of end-to-end API behavior; the live OpenAPI inventory test remains required.

## Explicitly not changed

No workflow job was removed, no `continue-on-error`, skip/filter, source validator bypass, API contract deletion, migration, OpenAPI artifact, or unrelated untracked file was changed.
