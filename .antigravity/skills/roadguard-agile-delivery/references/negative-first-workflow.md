# Negative-First test workflow

Use this sequence for every task, even when the task is documentation or infrastructure (replace business cases with the nearest observable failure cases).

## Phase 1 — negative and edge tests first

Write failing tests for the cases that could cause unsafe data or unauthorized behavior:

- `null`, empty, whitespace, malformed IDs/JSON/coordinates, unsupported enum values, invalid units, and invalid file metadata;
- lower/upper boundary, oversized payload/file/chunk, duplicate item, duplicate idempotency key, and replayed message;
- unauthenticated user, wrong role, suspended account, user outside project, and forged project/role input;
- invalid state transition, missing prerequisite, stale concurrency token, immutable/submitted/approved record update;
- checksum/integrity failure, timeout, storage/DB/queue failure, retryable versus permanent processing failure;
- legal hold, retention scope mismatch, wrong `RoadSectionVersion`, missing evidence, and exactly-one-target violations where relevant.

Name the expected stable error code and verify that no state, audit record, notification, or file metadata is incorrectly created.

## Phase 2 — positive tests

Add the smallest valid request and one representative full path. Assert the resulting state, response DTO, audit event, version, and downstream/outbox effect—not just the status code.

## Phase 3 — implementation

Implement the smallest change that satisfies the tests and the source acceptance criteria. Keep domain decisions out of controllers and persistence entities. Preserve immutable history and project scope.

## Phase 4 — execution and self-repair

Run the narrow test first, then the affected suite, then format/build. Read the first failure, fix the cause, rerun, and repeat until green. Do not weaken or delete a test to obtain a green build. Record commands, exit codes, and any environment-limited tests in the handoff log.

## Minimum evidence matrix

| Layer | Required evidence |
|---|---|
| Domain | invalid transition and valid transition |
| Service | role/project gate, cross-aggregate rule, audit/idempotency/concurrency |
| Repository | mapping/constraint/spatial/transaction behavior on SQL Server |
| API | validation, 401/403/409/422/404 as applicable, stable error code, DTO shape |
| Worker | duplicate delivery, retry classification, no duplicate side effects |
| End-to-end | one traceable user story flow with seeded data |
