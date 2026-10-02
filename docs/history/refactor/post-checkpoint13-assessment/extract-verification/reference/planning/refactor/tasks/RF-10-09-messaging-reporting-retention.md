# RF-10-09: Notification, outbox, audit, reporting and retention

> Current execution 2026-10-01: Anh alone owns R/C refactor on `anh`. F/G is unassigned for later development; proposed A/B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** PLANNED; re-record branch/HEAD/status. Report `planning/refactor/reports/RF-10-09.md`; checkpoint notification dispatcher, reporting and retention/deletion separately.
- **Goal:** reconcile R17-18, L12/L13, CG13-14 and Accepted 40/41A without treating a registered consumer as a running worker or a target SLO as measured.
- **In scope:** inbox/outbox dispatch ownership, consumer receipt/dedup, audit/event taxonomy, dashboard/export read models, project evidence retention/legal hold after authority is established. **Out of scope:** deleting shared/live data, treating technical logs like business audit, incidental Quartz removal, claiming 50-user/p95/RPO/RTO targets verified without measurements.
- **Dependencies/decisions:** RF-08 outbox transaction baseline, RF-09 migration/consumer plan; source events from RF-10 modules for reports. Q-RF02-08 determines per-record warranty basis/hold/delete authority and backup mechanics; deployment scheduler owner must be identified.
- **Read first:** R17-18, CG13-14, 40/41A full text, `NotificationsController`, `NotificationOutboxConsumer`, `OutboxWorkRepository`, `AuditLog`, `RoadGuardDbContext.cs:270`, file retention fields, current worker/CI/deployment configuration and report analytics proposal.
- **Likely files:** notification/outbox Repository/Service/worker and registration, report/export/read-model slices, retention/hold entities/config/migrations only when approved, contracts/Postman/`.http`, focused API/SQL/operations tests.
- **Contract/data/consumer effect:** inbox wire preserved first; new dashboard/export/delete/hold routes need actor/privacy and Web/Android consumer approval. Additive retention basis/hold fields and backfill must preserve files with unknown end date as WAITING_RETENTION_BASIS; only all obligations expired and no hold permit deletion. Source video and business audit follow case evidence, unlike 90-day technical logs.
- **Steps:** locate actual dispatcher/deployment -> characterize backlog/lease/receipt -> implement approved scheduler -> define report metric numerator/denominator/source -> establish warranty/hold/backups -> dry-run retention decisions -> allow actual deletion only under separately approved operational gate.
- **Verify:** SQL outbox atomicity/restart/duplicate/lease tests, inbox API auth/read state, audit redaction, report totals against fixtures, retention dry-run and hold/no-warranty/multiple-obligation cases; benchmark 50 users/p95 and restore RPO/RTO in isolated approved environment before VERIFIED claim.
- **Done when:** notification delivery and reporting slices have runtime evidence; retention deletion remains Blocked/Partial until Q-RF02-08 and restore approval; SLO/restore status reported as measured or unverified, never inferred.
- **Recovery:** stop dispatcher and replay leased events from receipts; disable new report/export/deletion paths; restore only from approved backup when necessary, preserve audit/holds and never remove an applied migration.

## Two-developer delivery supplement

- **Proposed owner:** A: notification/outbox/audit/retention orchestration; B: reporting/export projections. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
