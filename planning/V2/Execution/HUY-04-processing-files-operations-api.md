# HUY-04 - Processing, files and operational API

- Owner/branch: Huy / huy
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- sourceCheckpoint: record HEAD and canonical OpenAPI hash at task start

## Task goal
Implement or verify API behavior for processing job trigger/status/retry, file metadata/download/upload sessions, AI result/validation review, audit/export, notifications, sync, retention and operational jobs. Keep external AI and deletion claims explicitly bounded.

## Business context
PM triggers analysis only after readiness. Processing is asynchronous and 202/status does not mean completion. AI results retain provenance and require PM review; they never approve a defect or repair. Retention honors legal hold and unresolved dates.

## Operation trace
V2-P2-023..042, V2-P2-049..052, V2-P2-056..062. Callback/matching/provider operations remain gated by their own contract and fixtures.

## Sources to read
Read operation cards, FR-29..36, BR-39..46, ERD_Processing_AI, ERD_Durability_Ops, DD 3.4/3.7/9, permission/API/error specs, SQ-04..06, state machines, AI Integration, ADR 003/006, decisions D12-D13/D34A/D38/D41A-D44 and ANH-04 handoff.

## In scope
- Write contracts for actor/scope, readiness, input manifest, 202/status, stable errors, idempotency/version and durable effects.
- Verify job create/get/retry, upload/download scope, checksum/metadata privacy, validation/label review, audit/export and notification read state.
- Verify sync conflict semantics and retention/legal-hold decisions without executing destructive deletion.
- Add focused API/unit tests, update API.http/Postman, and run real HTTP smoke inspecting headers, body and SQL durable effects.
- Label fake provider evidence and record external provider gates separately.

## Out of scope
- Repository/EF/migration edits, FastAPI deployment, model precision/recall, production retention values, live deletion, provider credentials or invented callback schema.

## Exact files and hotspots
Services/Processing, Files, Notifications, Audit, Retention and Operations; DTOs/controllers, API.http, Postman and API/unit tests. Shared DI/errors/OpenAPI and background-job composition require one writer.

## Stop conditions
Stop on missing immutable manifest identity, provider/schema conflict, unknown retention basis, unresolved delete/legal-hold rule, contract-lock mismatch or any request to claim external E2E from a fake provider. Use PARTIAL with exact gates.

## Verification and acceptance
Build fresh Services/API/ApiTests. Run focused processing/file/notification/retention tests with non-empty filters, real HTTP smoke and SQL durable-effect inspection. Static Postman checks are separate. Acceptance requires ANH-04 handoff VERIFIED or NO_CHANGE_NEEDED and explicit provider/deletion limitations.

## Completion history
- TODO until all gates pass.



