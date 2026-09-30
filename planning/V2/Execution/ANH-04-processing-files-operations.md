# ANH-04 - Processing, files and operational persistence

- Owner/branch: Anh / anh
- deliveryStatus: PARTIAL
- contractStatus: PROPOSED_DELTA
- implementationStatus: PARTIAL
- verificationStatus: PARTIAL_FOCUSED_SQL
- dependencyType: data-fixture
- sourceCheckpoint: HEAD 4586c8caa5aa8439c1ea9f9e385a8ee59359f0bb; canonical OpenAPI SHA-256 ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665; dirty baseline recorded 2026-09-30 03:13 +07:00

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
Build Repositories and IntegrationTests. Run focused processing, file, notification, idempotency, outbox, retention and migration lifecycle tests using SQL Server; record durable effects, replay/conflict results and provider provenance. The HUY-04 handoff reports facts and does not gate Anh's independent work; external provider and contract/schema gates remain explicit.

## Source evidence - 2026-09-30 03:13 +07:00

| Label | Source and heading/ID | Invariant used |
|---|---|---|
| TARGET_DOCUMENTED | `AGENTS.md` / V2 Ownership, Verification Ladder; `docs/adr/001-backend-boundary.md` / Decision; `docs/adr/004-n-layer-backend-structure.md` / Decision; `docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md` / Decision | Anh owns persistence/SQL; API policy and HTTP belong to Huy; no schema or migration effect without explicit scope. |
| TARGET_DOCUMENTED | `planning/V2/V2-3_DECISION_REGISTER.md` / D12-D13, 34A, 38, 41A-44; `docs/adr/003-backend-delivery-and-ai-boundary.md` / Decision | PM triggers ready dataset; versioned manifest and idempotent receipts are durable; AI cannot approve; legal hold blocks deletion. |
| TARGET_DOCUMENTED | `docs/design/02_Requirements/01_FRD_SRS.md` / FR-29, FR-31, FR-34..36; `docs/design/02_Requirements/02_Business_Rules.md` / BR-39..45 | Late AI result cannot overwrite current result; retry preserves identity; timeline avoids duplicate events; retention requires basis and audit. |
| TARGET_DOCUMENTED | `docs/design/05_Technical/05_Sequence_Diagrams.md` / SQ-04..06; `docs/design/05_Technical/07_State_Machines_V2.md` / Processing job/attempt, Sync operation | Job/manifest/outbox admission precedes acknowledgement; attempt fencing and per-operation sync ACK are target transitions. |
| PROPOSED_DELTA | `docs/design/03_Data/ERD_Processing_AI.md` / Target entities; `docs/design/03_Data/ERD_Durability_Ops.md` / Target entities; `docs/design/03_Data/01_Data_Dictionary.md` / 3.4, 3.7, 3.8, 9.5 | ProcessingInputManifest/EventReceipt, SyncOperation, LegalHold and DeletionRequest have open physical/schema gates. |
| TARGET_DOCUMENTED | `docs/design/03_Data/03_Domain_Model_V2.md` / Processing/AI, Durability/operations; `docs/design/05_Technical/openapi.yaml` / createProcessingJob and related operation IDs | Logical aggregates and wire operations guide comparison; OpenAPI hash above is a contract fingerprint, not runtime proof. |
| HISTORICAL | `planning/V2/Person_2/V2-P2-023_createUploadSession.md` through `V2-P2-062_getNotification.md` / V2(3) status and completion history, limited to ANH-04 operation trace | Prior DONE/PARTIAL/API evidence is preserved; it does not prove current checkout or SQL environment. |
| CURRENT_VERIFIED | `RoadGuardSystem.Repositories/Implementations/Processing/ProcessingV2PersistenceService.cs` / CreateAsync, RetryAsync, ReceiveResultAsync; `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs` / ExecuteAsync | Source creates job/attempt/outbox/audit in idempotent transaction; callback checks attempt membership but not latest attempt. Focused SQL confirms current processing tables/outbox lease behavior; direct create-job SQL remains unverified because ANH-02 dataset fixtures are still gated. |
| CURRENT_VERIFIED | `RoadGuardSystem.BusinessObjects/Processing/ProcessingJob.cs`, `ProcessingAttempt.cs`; `RoadGuardSystem.Repositories/Configurations/ProcessingJobConfiguration.cs`; `RoadGuardSystem.Repositories/Migrations/20260929184004_Baseline20260930.cs`, `Baseline20260930.Triggers.cs`, `RoadGuardDbContextModelSnapshot.cs`; `tests/RoadGuardSystem.IntegrationTests/Processing/P231ProcessingPersistenceTests.cs` | Current candidate baseline stores manifest, rowversion, attempt uniqueness and append-only attempt trigger; this uncommitted baseline must be verified on isolated SQL Server. |
| CURRENT_VERIFIED | `RoadGuardSystem.Repositories/Implementations/Messaging/NotificationPersistenceService.cs` / MarkReadAsync; `tests/RoadGuardSystem.IntegrationTests/Notifications/P207NotificationPersistenceTests.cs` / MarkRead_RecipientVersionAndReplay_PersistOneReadEffect | SQL Server test confirms recipient-scoped read, stale-version rejection, same-key replay, changed-payload conflict and one durable read/idempotency effect. |
| HISTORICAL | `planning/V2/Governance/DB-BASELINE-20260930.md` / Source evidence, Completion history | The uncommitted baseline replaces 37 prior migrations; its old-chain parity report belongs to a separate PARTIAL governance task. ANH-04 reran focused baseline lifecycle checks only. |

Current-vs-target gate: `ReceiveResultAsync` accepts an older attempt associated with a job, whereas FR-29/BR-42/SQ-04 require attempt fencing and late-source preservation. ERD EventReceipt is only `PROPOSED_DELTA`; no schema or callback behavior change is authorized in this task. Sync/retention entity additions have the same schema gate. Independent current-schema SQL verification continues.

| Slice | Current source/SQL evidence | Target and decision gate |
|---|---|---|
| Processing V2-P2-030..035 | Job manifest/hash, attempt, outbox and audit are persisted; baseline SQL tables/triggers and lease tests pass. Callback accepts any attempt belonging to the job. | SQ-04/BR-42 require stale-attempt fencing plus a retained late receipt. Approve receipt/schema/provider behavior before modifying callback. |
| Files V2-P2-023..028 | Upload/file SQL tests pass scope, checksum, immutable file, replay and rollback checks. | External object-store/provider and API behavior are Huy/external gates; no new persistence delta selected. |
| Notifications V2-P2-062 | Recipient-scoped Get/List and MarkRead use projection, bounded ordering, rowversion and durable idempotency. Focused SQL test added. | Huy maps repository facts to API; no wire or service policy decided here. |
| Audit/idempotency/outbox | SQL checks pass append-only audit, scoped duplicate rejection, rollback, lease and consumer receipt. | Timeline/export projection and additional async job types require consumer contracts. |
| Sync, export, retention V2-P2-029/038..042/049..052/056..058/060..061 | No SyncOperation, LegalHold, DeletionRequest, ReportExport or TrainingDatasetExport entity/repository found in current source. | ERD/DD describe proposed schema; approve physical contract, retention basis and fixtures before writes or deletion. |

## Completion history
- TODO until all gates pass.

### 2026-09-30 03:20 +07:00 - PARTIAL

- Scope/result: Reconciled current persistence with ANH-04 target. Added one focused SQL test for notification read scope, rowversion and idempotency. No production contract or schema changed. Published existing facts and gaps in `ANH-04-OPS-01` handoff to Huy; receiver outcome is pending.
- Files: `planning/V2/Execution/ANH-04-processing-files-operations.md`; `tests/RoadGuardSystem.IntegrationTests/Notifications/P207NotificationPersistenceTests.cs`; `planning/CROSS_OWNER_HANDOFFS.md`.
- Acceptance criteria: Current SQL tests prove scoped notification read, one `ReadAt` update and idempotency row, file/upload replay and rollback, outbox lease/dedup, audit immutability and candidate baseline lifecycle. Processing callback attempt fencing and late-source receipt are not implemented; SyncOperation, LegalHold, DeletionRequest and export persistence remain proposed/absent. Provider and API evidence are not claimed.
- Verification: `dotnet build RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj -nologo -v q -clp:ErrorsOnly` PASS (0 errors); `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj -nologo -v q -clp:ErrorsOnly` PASS (0 errors, 304 warnings after test edit); `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build -nologo -v q --filter "FullyQualifiedName~P231ProcessingPersistenceTests|FullyQualifiedName~P230ProcessingJobSqlServerTests|FullyQualifiedName~UploadPersistenceSqlTests|FullyQualifiedName~FileRepositorySqlTests|FullyQualifiedName~P207NotificationPersistenceTests|FullyQualifiedName~P202TransactionAndIdempotencyTests|FullyQualifiedName~P202ConcurrencyAndOutboxTests"` PASS 32/32, fail 0, skip 0; `--filter "FullyQualifiedName~P202MigrationLifecycleTests"` PASS 3/3, fail 0, skip 0. Debug/net8.0, SQL Server 16.0.1000.6 using `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` and isolated fixture databases. Source-based callback gap has no SQL acceptance proof.
- Reused/invalidated evidence: Initial 31/31 and 3/3 runs were invalidated by the new test binary; both filters reran after the final test edit. The first new-test failure used a malformed fixture fingerprint; after SHA-256 fixture correction the test passed. Historical operation-card/API and baseline governance results were not reused as current task proof.
- Side effects: Isolated SQL fixture databases created and disposed by tests. No package, production code, migration/schema, live data, external provider, commit or push; `UNCOMMITTED`.
- Unverified/blockers: ANH-02/ANH-03 fixture dependencies remain TODO, including a ready dataset/project fixture for a direct processing-create SQL test; callback needs approved late-result receipt/fencing schema and provider contract; sync and retention/deletion need approved physical contracts; export/model/reminder facts lack implemented persistence; `ANH-04-OPS-01` receiver has not recorded `VERIFIED` or `NO_CHANGE_NEEDED`. These block ANH-04 DONE.

### 2026-09-30 03:22 +07:00 - PARTIAL REVIEW_FIX

- Scope/result: Diff-first Team Leader review of the ANH-04 task delta. Pre-existing migration/configuration/documentation changes were excluded from this task review. Corrected the stale source-evidence sentence that said SQL had not run after the focused SQL evidence was recorded.
- Files: `planning/V2/Execution/ANH-04-processing-files-operations.md` only for the review fix; notification test and handoff were reviewed without further changes.
- Acceptance criteria: Review found no RED production defect in the task delta. Notification test assertions cover recipient scope, stale rowversion, replay, changed fingerprint conflict and durable effect. API/provider claims remain explicitly absent. The processing callback fence, proposed receipt/sync/retention entities and receiver handoff remain blockers.
- Verification: `git diff --check -- planning/V2/Execution/ANH-04-processing-files-operations.md tests/RoadGuardSystem.IntegrationTests/Notifications/P207NotificationPersistenceTests.cs` PASS; prior fresh-build and SQL evidence remains valid because only task documentation changed after those runs. No API smoke/Postman run: no API files changed.
- Reused/invalidated evidence: SQL/build evidence from 32/32 focused persistence tests and 3/3 migration lifecycle tests remains valid; documentation-only correction did not invalidate binaries or database evidence. Current review base is `a9c78e1`, with task base checkpoint preserved as `4586c8c`.
- Side effects: No package, migration, schema, data, external system, stage, commit or push.
- Unverified/blockers: Handoff `ANH-04-OPS-01` is `SENT`, not `VERIFIED`/`NO_CHANGE_NEEDED`; task remains `PARTIAL` and is not eligible for push.

### 2026-09-30 03:34 +07:00 - PARTIAL (handoff publication)

- Scope/result: Owner approved publishing the reviewed notification SQL test and current-facts checkpoint to `develop` for Huy's `ANH-04-OPS-01` receiver review; status remains `SENT`.
- Files: `P207NotificationPersistenceTests.cs`, this task and `planning/CROSS_OWNER_HANDOFFS.md`; no processing or notification production code changed.
- Verification: Fresh Repositories and IntegrationTests builds PASS; focused identity/survey/notification SQL filter 73 passed, 0 failed, 0 skipped on the dirty local checkout; selected diff check PASS.
- Reused/invalidated evidence: Earlier 32/32 processing/files/notification and 3/3 migration lifecycle results remain local candidate-baseline evidence; clean `develop` must be verified independently.
- Side effects: Owner-approved Git publication only; no package, migration application, live data or provider effect.
- Unverified/blockers: Receiver outcome, stale-callback receipt/fencing, sync/retention/export schema and provider evidence remain open; deliveryStatus stays `PARTIAL`.

### 2026-09-30 11:37 +07:00 - PARTIAL (owner handoff decision)

- Scope/result: Owner accepted Huy's processed handoff; the current notification consumer needs no repository change. `HUY-04-CALLBACK-FENCE-SYNC-RETENTION-CONTRACT` remains needed for stale-attempt fencing, late receipt and absent sync/retention/export persistence. `deliveryStatus` stays `PARTIAL`.
- Files: this checkpoint and `planning/CROSS_OWNER_HANDOFFS.md`; no production, schema or test source changed.
- Verification: `python docs/diagram/V2/ci/check_alignment.py` PASS (133 tasks), `git diff --check` PASS; `python docs/diagram/V2/09_Frontend/contracts/check_contracts.py` fails `CONTRACT_LOCK_MISMATCH` on unchanged contract files. Huy recorded notification API 1/1, processing SQL 5/5, notification SQL 5/5 and file SQL 5/5 at `d2338dc`. No external AI/object-store/retention or current local SQL proof is claimed.
- Reused/invalidated evidence: receiver results remain bound to their checkpoint. Side effects: no package, migration, schema, data, provider, commit or push. Blockers: approved callback and operational physical contracts, fixture integration and external-provider evidence.
