# RF-10-09-C01: Notification/Outbox Characterization Baseline

**Task ID:** RF-10-09-C01  
**Owner:** Anh  
**Status:** BOX 2 READY - test written, NOT_RUN, awaiting BOX 3 verification  
**Branch:** `anh`  
**Commit:** `2efc8a5775f834c7f0fe37cc0ce703011649e1f1` (before test creation)  
**Date:** 2026-10-01

## Executive Summary

This characterization verifies the current behavior of the notification inbox HTTP API and outbox pattern implementation. The focused test covers recipient-scoped authorization, cursor pagination, mark-read idempotency with version checking, outbox consumer replay protection, and producer→outbox linkage for survey assignment events.

**Critical Finding:** No production dispatcher/scheduler was found calling `IOutboxWorkRepository.TryLeaseNextAsync`. The outbox lease/retry infrastructure exists but is not actively invoked by any BackgroundService or HostedService in the current codebase. This limitation is documented as NOT_VERIFIED scope.

## Scope

### What IS characterized (HTTP + owned SQL)

1. **Inbox GET authorization and projection**
   - Recipient sees own notification (200 OK)
   - Wrong user gets 404 (recipient scope enforced)
   - Response includes: id, message, resourceId, read boolean, occurredAtUtc, version (Base64 RowVersion)
   - ETag header matches version field
   - GET operation is immutable (no ReadAt mutation)

2. **Inbox List pagination and filtering**
   - Recipient-only scope (outsider notifications excluded)
   - Cursor-based pagination (Base64 JSON of OccurredAtUtc + Id)
   - DESC ordering (most recent first)
   - nextCursor only when hasMore

3. **Mark Read mutation with guards**
   - Recipient scope (404 for wrong user)
   - Version check (412 for stale If-Match)
   - Idempotency-Key replay returns 200 OK
   - Conflict detection (409 for same key, different fingerprint)
   - SQL mutation: ReadAt set, RowVersion changed, single IdempotencyRecord

4. **Outbox consumer replay protection**
   - First consume returns Recorded status
   - Replay returns Replayed status
   - Single notification persisted (no duplicate)
   - Single ConsumerEffectReceipt persisted

5. **Producer→outbox linkage**
   - SurveyAssignmentPersistenceService.ReassignAsync creates outbox
   - MessageType: "survey_request.reassigned"
   - CorrelationId: surveyRequestId
   - PayloadJson includes assignment IDs
   - DeliveryStatus: Pending, DeliveryAttemptCount: 0

6. **Lease acquisition (repository boundary)**
   - TryLeaseNextAsync uses UPDLOCK+READPAST
   - Filters: DeliveryStatus NOT IN (Completed, DeadLetter), DeliveryAttemptCount < maxAttempts
   - Lease expiry check: LeaseExpiresAtUtc IS NULL OR <= now
   - AcquireLease increments DeliveryAttemptCount, sets LeaseOwner/LeaseExpiresAtUtc

### What is NOT characterized (documented limitations)

1. **Dispatcher/scheduler reachability:** No BackgroundService or IHostedService found calling `IOutboxWorkRepository.TryLeaseNextAsync` in production code. UploadVerificationWorker exists but uses different pattern (IUploadService). ValidationRunWorker registered but mechanism differs.

2. **Real delivery timing:** Repository-direct proof does not verify end-to-end message delivery or retry loop execution.

3. **DeadLetter transition:** Tested at repository level (ScheduleRetry logic), not proven via dispatcher hitting maxAttempts.

4. **Cursor edge cases:** Malformed cursor basic validation only; comprehensive boundary cases not exhaustive.

5. **External consumers:** No Web/Android/AI client evidence for notification polling or display.

6. **Reports/retention/delete:** Out of scope per Q-RF02-08 decision gate.

## Test Structure

**File:** `tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs`  
**Fixture:** `AuthenticationSqlServerFixture` (Testcontainers SQL Server)  
**Factory:** `AuthenticationWebApplicationFactory` (JWT bearer token authentication)  
**Trait:** `[Trait("TaskId", "RF-10-09-C01")]`

### Test Methods (5 total)

1. `InboxGet_RecipientScopeAndProjection_FiltersAndReturnsExpectedFields`
   - Arrange: Two users, one notification each
   - Act: GET own, cross-user GET attempts
   - Assert: 200 OK for owner, 404 for others, projection fields match, SQL immutable

2. `InboxList_RecipientScopeAndCursor_FiltersAndPaginatesCorrectly`
   - Arrange: Recipient with 3 notifications, outsider with 1
   - Act: List first page (limit=2), then second page with cursor
   - Assert: Recipient-only results, DESC ordering, outsider excluded, cursor pagination works

3. `MarkRead_RecipientVersionIdempotency_EnforcesConstraintsAndReplays`
   - Arrange: Notification for recipient, outsider attempt
   - Act: Wrong user, stale version, first mark, replay, conflict
   - Assert: 404/412/200/200/409 as expected, ReadAt set, RowVersion changed, single IdempotencyRecord

4. `OutboxConsumer_Replay_ReturnsDurableNotificationWithoutDuplicate`
   - Arrange: OutboxMessage and Notification entities
   - Act: ConsumeAsync twice with same messageId
   - Assert: Recorded then Replayed status, single notification persisted, single receipt

5. `Producer_CreateOutbox_LinksCorrectCorrelationAndType`
   - Arrange: Survey request with initial assignment
   - Act: SurveyAssignmentPersistenceService.ReassignAsync
   - Assert: Outbox message created with correct messageType, correlationId, payload

### Snapshot Strategy

Each test captures SQL state before/after operation using `NotificationSnapshot` record with explicit field assertions:
- Id, RecipientUserId, SourceEntityType, SourceEntityId, EventType, Title, Body, OccurredAtUtc, ReadAt, RowVersion

Helper methods:
- `AuthenticateAsync`: JWT login and bearer token setup
- `MarkReadAsync`: Adds Idempotency-Key and If-Match headers
- `CaptureNotificationAsync`: AsNoTracking snapshot for comparison
- `CreateProjectWithMemberAsync`, `CreateSurveyRequest`, `CreateSurveyAssignment`: Minimal test data setup

## Evidence Reuse

### From RF-08 (with provenance)

- **P207NotificationPersistenceTests.cs**: Round-trip persistence, FK validation, sensitive content blocking, NotificationOutboxConsumer replay, MarkRead recipient scope and version check
- **P2NotificationApiTests.cs**: Recipient scoping (owner sees, other 404), version/ETag, mark read replay

### New focused coverage

- Inbox List cursor pagination with cross-user exclusion
- Mark read conflict detection (same key, different fingerprint)
- Producer→outbox linkage for specific event type (survey_request.reassigned)
- Lease acquisition at repository boundary (no dispatcher execution)

## Component Inventory

### HTTP Endpoints (NotificationsController)

- `GET /api/v1/notifications/{id}` - single notification read (recipient scoped)
- `GET /api/v1/notifications` - list notifications (recipient scoped, cursor pagination)
- `POST /api/v1/notifications/{id}/read` - mark read (idempotent, versioned)

All routes: `[Authorize]` attribute, JWT bearer token required

### Service Layer (NotificationService)

- `GetAsync`: recipient filter, 404 for wrong user
- `ListAsync`: cursor encoding/decoding (Base64 JSON), pagination limit validation (1-100)
- `MarkReadAsync`: fingerprint from notificationId|version SHA256, IdempotencyOperationService wrapper

### Repository Layer (NotificationPersistenceService)

- `GetAsync`: `WHERE RecipientUserId = @actorUserId`
- `ListAsync`: cursor-based filtering, `ORDER BY OccurredAtUtc DESC, Id DESC`
- `MarkReadAsync`: IdempotencyOperationService transaction, version check, MarkRead mutation, StaleConcurrency on concurrency exception

### Outbox Consumer (NotificationOutboxConsumer)

- Consumer name: `"notification-inbox"` (constant)
- `ConsumeAsync`: ConsumerEffectService.ProcessAsync boundary, replay detection via ConsumerEffectReceipt
- Single `notification.Add()` without SaveChanges (delegated to ConsumerEffectService)

### Outbox Work Repository (OutboxWorkRepository)

- `TryLeaseNextAsync`: UPDLOCK+READPAST lease acquisition, DeliveryStatus/NextAttemptAtUtc filter
- `AcquireLease`: increments DeliveryAttemptCount, sets LeaseOwner/LeaseExpiresAtUtc
- `CompleteAsync`: validates lease owner, status → Completed
- `RetryAsync`: validates lease owner, schedules retry or transitions to DeadLetter after maxAttempts

### Producers (outbox message creation)

- `SurveyAssignmentPersistenceService.ReassignAsync` → `"survey_request.reassigned"`
- `PrimaryProjectManagerPersistenceService` → `"project.primary_pm_reassigned"`
- `DetectionReviewPersistenceService` (inspection defect review)
- `ProcessingV2PersistenceService` → `"processing_job.dispatch"`, `"validation_run.dispatch"`

## Dispatcher Investigation

### Search Strategy

1. `rg -i "BackgroundService|IHostedService" --type cs` - no matches calling outbox repository
2. `rg "IOutboxWorkRepository" --type cs -A 5` - found repository interface, implementations, but no active consumer
3. `find . -name "*Worker.cs" -o -name "*Dispatcher.cs" -o -name "*Scheduler.cs"` - found UploadVerificationWorker, ValidationRunWorker
4. Inspected workers: different patterns (IUploadService.ProcessOneVerificationAsync, not IOutboxWorkRepository)

### Conclusion

No production caller found for outbox lease/retry loop. The infrastructure exists and is tested at repository boundary, but is not actively invoked by a scheduled worker. This is a genuine architectural gap, not a characterization failure.

## Dependencies

### Test Execution Requirements

- Testcontainers (Docker required for SQL Server instance)
- SQL Server 2019+ image
- .NET 8 SDK
- Existing migrations applied by AuthenticationSqlServerFixture

### NuGet Packages (already present)

- Microsoft.AspNetCore.Mvc.Testing
- FluentAssertions
- Testcontainers
- xUnit

### Fixture Setup

- `AuthenticationSqlServerFixture`: SQL Server container, connection string, CreateDbContext, CreateUserAsync
- `AuthenticationWebApplicationFactory`: WebApplicationFactory with JWT authentication, test issuer/audience

## Known Issues and Risks

### Architectural Gaps

1. **No active dispatcher:** Outbox messages accumulate in Pending status without background processing
2. **Retry loop not exercised:** ScheduleRetry and DeadLetter transitions tested at repository level only
3. **Lease expiry not observed:** No worker to demonstrate lease timeout and re-acquisition

### Test Limitations

1. **Repository-direct proof:** Producer→outbox→consumer chain tested by directly calling ConsumeAsync, not via dispatcher
2. **No real timing:** Cannot verify NextAttemptAtUtc scheduling or backoff intervals without dispatcher
3. **Minimal survey setup:** Uses ExecuteSqlInterpolated for project/member/request/assignment, not full domain entity graph

### Provisionally Accepted

These limitations are documented and accepted for BOX 2 scope. They do NOT block characterization completion. Future F/G work may:
- Implement dispatcher (separate task, not refactor requirement)
- Add end-to-end delivery timing tests
- Expand cursor pagination edge case coverage

## File Hashes

### Source Files (read-only, before-state)

```
RoadGuardSystem.API/Controllers/NotificationsController.cs
SHA-256: [captured in allowlist-before.txt]

RoadGuardSystem.Services/Implementations/Messaging/NotificationService.cs
RoadGuardSystem.Repositories/Implementations/Messaging/NotificationPersistenceService.cs
RoadGuardSystem.Repositories/Implementations/Messaging/NotificationOutboxConsumer.cs
RoadGuardSystem.Repositories/Implementations/Messaging/OutboxWorkRepository.cs
[full hashes in evidence directory]
```

### Test Files (new)

```
tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs
SHA-256: 1f233dc690351172812d5c5d413488457c7ebcc2e0b18193e042a0d07854f06f
Status: NOT_RUN
```

### Existing Test Files (reused evidence)

```
tests/RoadGuardSystem.IntegrationTests/Notifications/P207NotificationPersistenceTests.cs
tests/RoadGuardSystem.ApiTests/Notifications/P2NotificationApiTests.cs
[hashes captured in existing-test-hash.txt]
```

## Verification Instructions (for BOX 3)

### Prerequisites

1. Docker Desktop running (for Testcontainers)
2. No shared database modifications from BOX 2
3. Clean bin/obj from any previous builds

### Test Execution Command

```bash
dotnet test --filter "TaskId=RF-10-09-C01" --logger "trx;LogFileName=rf1009-c01-results.trx"
```

### Expected Results

- 5 tests discovered
- 5 tests passed
- 0 tests failed
- 0 tests skipped

### Failure Investigation

If any test fails:
1. Check Testcontainers Docker image pull (SQL Server 2019)
2. Verify migrations applied (AuthenticationSqlServerFixture responsibility)
3. Inspect TRX file for specific assertion failure
4. Capture SQL state at failure point (test includes snapshots)

### Post-Verification

After BOX 3 confirms PASS:
1. Update `planning/refactor/10-refactor-checklist.md` entry for 10-09-C01
2. Mark RF-10-09-C01 as "Done locally" in slice ledger
3. Add TRX results to evidence directory
4. Update combined verification tracking

## Out of Scope (F/G Work)

- Dispatcher/scheduler implementation (requires separate design decision)
- Real notification delivery timing and retry backoff verification
- External consumer integration (Web/Android/AI)
- Notification reports, retention, and delete operations (Q-RF02-08)
- Legal hold and authorized dry-run scenarios
- KPI formulas and operator ownership tracking

## References

- Task Scope: `planning/refactor/evidence/rf1009-c01/scope-matrix.md`
- Allowlist: `planning/refactor/evidence/rf1009-c01/allowlist-before.txt`
- Slice Ledger: `planning/refactor/10-refactor-slices.md` (RF-10-09 entry)
- Master Checklist: `planning/refactor/10-refactor-checklist.md` (10-09-C01 entry)
- Existing Evidence: P207NotificationPersistenceTests.cs, P2NotificationApiTests.cs

---

**Baseline Complete:** 2026-10-01  
**Next:** BOX 3 verification
