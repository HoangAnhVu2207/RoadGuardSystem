# HUY-04 - Processing, files and operational API

- Owner/branch: Huy / huy
- deliveryStatus: PARTIAL
- contractStatus: PROPOSED_DELTA
- implementationStatus: PARTIAL_CURRENT_FACTS
- verificationStatus: PARTIAL_FOCUSED_API_SQL
- dependencyType: contract
- sourceCheckpoint: HEAD d2338dcffd8838198c2b50ee369b9678e7e06690; canonical OpenAPI SHA-256 ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665

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

## Source evidence - 2026-09-30 receiver review

| Label | Exact source / heading or ID | Evidence used |
|---|---|---|
| CURRENT_VERIFIED | `RoadGuardSystem.Services/Implementations/Processing/ProcessingV2Service.cs`; `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs` | Create/get/retry/validation services enforce actor/project scope, idempotency and If-Match; callback input is validated and delegated to repository, but service/API does not prove latest-attempt fencing. |
| CURRENT_VERIFIED | `RoadGuardSystem.Services/Implementations/Messaging/NotificationService.cs`; `RoadGuardSystem.API/Controllers/NotificationsController.cs` | Notification Get/List/MarkRead maps `Success`, `Replayed`, `NotFound`, `StaleConcurrency`, `IdempotentConflict` with scoped actor and ETag. |
| CURRENT_VERIFIED | `RoadGuardSystem.Services/Implementations/Files/UploadService.cs`; `RoadGuardSystem.API/Controllers/UploadsController.cs` | Upload/file routes consume project/file scope, checksum, rowversion and repository replay facts; object-store/provider execution is separate. |
| CURRENT_VERIFIED | `tests/RoadGuardSystem.ApiTests/Notifications/P2NotificationApiTests.cs`, `Surveys/P2SurveyV2ApiTests.cs`; SQL `P231ProcessingPersistenceTests`, `P207NotificationPersistenceTests`, `P230FlightSurveyFileSchemaTests` | Fresh API notification 1/1, processing SQL 5/5, notification SQL 5/5 and file SQL 5/5 pass; Unit/architecture 162/162. |
| TARGET_DOCUMENTED | `planning/V2/Person_2/V2-P2-023..042,049..052,056..062`; FR-29..36; BR-39..46; SQ-04..06; state machines | Job/manifest, notification, file, audit, export, sync and retention contract references; proposed target behavior is not automatically runtime truth. |
| PROPOSED_DELTA | `docs/diagram/V2/03_Data/ERD_Processing_AI.md`, `ERD_Durability_Ops.md`, DD §3.4/3.7/9; EventReceipt/SyncOperation/LegalHold/DeletionRequest/exports | Callback receipt/fencing, sync, retention/legal hold/deletion and export persistence are absent or gated. |
| NOT_ENABLED | `docs/design/**`, candidate baseline migration, external AI/object-store/SMTP providers | Sources/providers are not enabled in `d2338dc`; fake provider/TestServer/SQL facts do not establish external E2E or production deletion. |

## Contract checkpoint for routes actually consumed or gated

1. `POST/GET /api/v1/processing-jobs` and `POST /processing-jobs/{id}/retry`: PM/Supervisor project scope; validate immutable dataset scope, mode, manifest inputs, Idempotency-Key and If-Match; create returns 202 and job/ETag; replay/stale/conflict map distinctly; repository durable job/outbox facts are required before acknowledgement; 202 never means completed AI.
2. `POST /api/v1/internal/processing-jobs/{id}/results`: AI-service policy and immutable manifest/result checksum inputs; validate attempt/model/detections; success/error mapping remains bounded to repository facts; current callback checks membership but does not fence an older attempt or persist a late receipt; external provider and target EventReceipt are not claimed.
3. `POST/GET /api/v1/uploads...` and `/files/{id}`: authenticated project/file scope; validate part/checksum/If-Match; success returns metadata or content only after repository/provider facts; stale/replay/conflict are stable; no checksum or metadata path implies dataset quality/baseline.
4. `GET /api/v1/notifications`, `/notifications/{id}`, `POST /notifications/{id}/read`: recipient actor scope; list/get are projections, mark-read requires Idempotency-Key + If-Match; success/replay returns one DTO+ETag; NotFound=404, stale=412, changed-key=409; one durable `ReadAt` effect is repository evidence.
5. `POST/GET /api/v1/projects/{id}/validation-runs` and related async-job reads: PM project scope and idempotency; success is 202/result projection; validation metrics are not defect/repair approval and no AI precision claim is made.
6. Sync/export/reminder/retention/legal-hold/deletion/model-label operations in P2-029/036..061: actor/scope and destructive policy would require approved persistence facts; absent `SyncOperation`, `LegalHold`, `DeletionRequest`, export/model records remain blocked; no route or deletion behavior is inferred.

## Current versus target and outcome

ANH-04 notification read semantics map cleanly to the current API. Processing and file routes are bounded to existing durable facts. Callback attempt fencing/late receipt and sync/retention/export persistence are explicitly not enabled, so HUY-04 remains `PARTIAL` and makes no external-provider or deletion claim.

## Completion history

### 2026-09-30 - PARTIAL (ANH-04 receiver processing)

- Scope/result: Received ANH-04 and confirmed notification read status mapping plus processing/file service boundaries. No production code change was necessary.
- Files: `planning/V2/Execution/HUY-04-processing-files-operations-api.md`; receiver ledger row in `planning/CROSS_OWNER_HANDOFFS.md`. No Services/DTOs/API source changed.
- Acceptance: Notification mapping covers Success/Replayed/NotFound/StaleConcurrency/IdempotentConflict; processing callback, sync, retention, export and provider claims remain gated exactly as the handoff states.
- Verification: sequential `dotnet build RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj -nologo -v q -clp:ErrorsOnly`, `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly`, `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj -nologo -v q -clp:ErrorsOnly`, and `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly` PASS; `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P2NotificationApiTests"` PASS 1/1; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P231ProcessingPersistenceTests"` PASS 5/5; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P207NotificationPersistenceTests"` PASS 5/5; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P230FlightSurveyFileSchemaTests"` PASS 5/5. No external AI/object-store/retention smoke was claimed.
- Handoff outcome: `PROCESSED / NEW_TASK_NEEDED:HUY-04-CALLBACK-FENCE-SYNC-RETENTION-CONTRACT` because old-attempt fencing/late receipt and proposed sync/retention/export persistence are not present; notification consumer itself needs no change.
- Side effects/risk: no package, migration, schema, live data, provider, commit or push; missing `docs/design` and candidate baseline remain explicit blockers.

### 2026-09-30 - PARTIAL (notification API example and verification refresh)

- Scope/result: Added runnable `GET /api/v1/notifications`, `GET /api/v1/notifications/{notificationId}` and `POST /api/v1/notifications/{notificationId}/read` examples to `RoadGuardSystem.API/RoadGuardSystem.API.http`. The examples use the existing actor token, idempotency and ETag/If-Match contract; no Service/DTO/Repository behavior changed.
- Verification: `python docs/diagram/V2/ci/check_alignment.py` PASS (`ALIGNMENT_GUARDS_PASS`, 133 tasks); Postman static parse PASS (v2.1, 83 requests, 0 unresolved references, 0 duplicate names/IDs); fresh sequential Services/API/UnitTests/ApiTests builds PASS (0 warnings, 0 errors); focused API `16 passed, 1 optional MinIO smoke skipped`, Unit `159/159`, Integration SQL `55/55` passed. Hosted HTTP, SMTP, external AI/object-store and retention smoke remain `NOT_RUN`.
- Acceptance/status: notification read mapping remains `Success/Replayed/NotFound/StaleConcurrency/IdempotentConflict`; callback fencing, sync, retention and export persistence remain gated. Task stays `PARTIAL`; handoff stays `PROCESSED / NEW_TASK_NEEDED:HUY-04-CALLBACK-FENCE-SYNC-RETENTION-CONTRACT`.
- Side effects/risk: documentation/request-example change only; no package, migration, schema, live data, provider, commit or push.

- Full-solution gate after this slice: `dotnet test RoadGuardSystem.slnx --nologo -v minimal` passed Unit `170/170`, API `117/118` with one optional MinIO skip, and Integration `317/317`; no failures. Hosted HTTP, external provider and missing callback/sync/retention/export persistence evidence remain unverified.



