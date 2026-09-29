# ANH-04 - Processing, files and operational persistence

- Owner/branch: Anh / anh
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: data-fixture
- sourceCheckpoint: record HEAD and canonical OpenAPI hash at task start

## Task goal
Verify durable persistence for processing jobs and AI receipts, file metadata/upload boundaries, notifications, audit, idempotency, outbox, offline sync, exports and retention/legal hold. This task proves backend durability and identity; it does not prove an external model or field result.

## Business context
PM explicitly triggers analysis after dataset readiness. AI receives an immutable manifest and may suggest results; it never approves defects or repair. Delivery can repeat, so effects must be idempotent and auditable. Retention and legal hold govern deletion; missing warranty end dates remain waiting states.

## Operation trace
Processing/AI: V2-P2-030..036, V2-P2-058 and V2-P2-061. Files/operations: V2-P2-023..029, V2-P2-038..042, V2-P2-049..052, V2-P2-056..057 and V2-P2-060..062.

## Sources to read
- docs/design/02_Requirements/01_FRD_SRS.md FR-29..36; 02_Business_Rules.md BR-39..46.
- docs/design/03_Data/ERD_Processing_AI.md, ERD_Durability_Ops.md, DD sections 3.4/3.7/9.
- docs/design/05_Technical/02_Auth_Permission_Model.md, 05_Sequence_Diagrams.md SQ-04..06, 07_State_Machines_V2.md, AI_Integration/.
- docs/adr/003-backend-delivery-and-ai-boundary.md, decisions D12-D13, D34A, D38, D41A-D44 and current processing/file/outbox/audit/notification/retention source/tests.

## In scope
- Verify job identity, readiness gate, same-key replay, stale version, immutable input manifest and outbox/receipt atomicity.
- Verify file scope, checksum/metadata immutability, notification read state, audit actor/source, export metadata and idempotency records.
- Verify sync command scope/conflict facts and retention/legal-hold/delete decision persistence without executing deletion.
- Add focused SQL tests and minimal persistence fixes for missing durability/concurrency invariants.
- Publish facts, receipt identities, fixtures and provider boundary to HUY-04.

## Out of scope
- Running/deploying FastAPI, claiming AI precision/recall, accepting callback payloads without a contract or declaring production retention values.
- Service/API/DTO/HTTP/Postman edits, live deletion, migration application or provider credentials.
- Adding backlog endpoints for candidate matching, rescue/handover or legal policy without an approved contract.

## Exact files and hotspots
Primary areas: BusinessObjects/Processing, Files, Notifications, Audit, Idempotency, Outbox and Retention; matching repositories/configurations and SQL tests. DbContext, migrations/snapshot, seed, Docker and CI are shared hotspots.

## Stop conditions
Requires dataset facts from ANH-02 and policy/defect identity from ANH-03. Stop on provider/schema/retention conflict, absent immutable manifest identity or any request to turn fake-provider evidence into external verification.

## Verification and acceptance
Build Repositories and IntegrationTests. Run focused processing, file, notification, idempotency, outbox, retention and migration lifecycle tests using SQL Server; record durable effects, replay/conflict results and provider provenance. Acceptance requires HUY-04 handoff VERIFIED or NO_CHANGE_NEEDED; external provider gates remain explicit.

## Completion history
- TODO until all gates pass.





