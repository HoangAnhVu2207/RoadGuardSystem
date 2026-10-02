# BOX 2 READY - RF-10-09-C01

**Date:** 2026-10-01  
**Owner:** Anh  
**Branch:** `anh`  
**Commit (before test creation):** `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`

## Allowlist Summary

### Files Read (source inspection only)

- RoadGuardSystem.API/Controllers/NotificationsController.cs
- RoadGuardSystem.Services/Implementations/Messaging/NotificationService.cs
- RoadGuardSystem.Services/Interfaces/Messaging/INotificationService.cs
- RoadGuardSystem.Repositories/Implementations/Messaging/NotificationPersistenceService.cs
- RoadGuardSystem.Repositories/Implementations/Messaging/NotificationOutboxConsumer.cs
- RoadGuardSystem.Repositories/Implementations/Messaging/OutboxWorkRepository.cs
- RoadGuardSystem.Repositories/Interfaces/Messaging/INotificationRepository.cs
- RoadGuardSystem.Repositories/Interfaces/Messaging/IOutboxWorkRepository.cs
- RoadGuardSystem.BusinessObjects/Messaging/Notification.cs
- RoadGuardSystem.BusinessObjects/Messaging/OutboxMessage.cs
- RoadGuardSystem.Repositories/Implementations/Surveys/SurveyAssignmentPersistenceService.cs
- RoadGuardSystem.Repositories/Implementations/Projects/PrimaryProjectManagerPersistenceService.cs
- RoadGuardSystem.Repositories/Implementations/Defects/DetectionReviewPersistenceService.cs
- tests/RoadGuardSystem.IntegrationTests/Notifications/P207NotificationPersistenceTests.cs
- tests/RoadGuardSystem.ApiTests/Notifications/P2NotificationApiTests.cs
- tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs
- tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs

### Files Written (BOX 2 scope)

1. **New test:**
   - tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs
   - SHA-256: 1f233dc690351172812d5c5d413488457c7ebcc2e0b18193e042a0d07854f06f
   - Status: NOT_RUN (BOX 2 does not execute tests)

2. **Documentation:**
   - planning/refactor/10-notification-outbox-characterization-baseline.md
   - SHA-256: 0d7474d6bf7cb5cbf09d8f73aadda40b95d34babd3fca432310623287ea76526
   
   - planning/refactor/reports/RF-10-09-C01.md
   - SHA-256: 4643ef3c1fc2cebcd6ed7e3f2bd178974c8dc418f3153c9ca1fb933ce9985097

3. **Evidence directory:**
   - planning/refactor/evidence/rf1009-c01/allowlist-before.txt
   - planning/refactor/evidence/rf1009-c01/scope-matrix.md
   - planning/refactor/evidence/rf1009-c01/before-baseline-hash.txt
   - planning/refactor/evidence/rf1009-c01/existing-test-hash.txt
   - planning/refactor/evidence/rf1009-c01/new-test-hash.txt
   - planning/refactor/evidence/rf1009-c01/BOX2-READY.md (this file)

### Files NOT Modified (out of scope)

- planning/refactor/10-refactor-checklist.md (BOX 3 updates after verification)
- planning/refactor/10-refactor-slices.md (BOX 3 updates after verification)
- All production source code (RoadGuardSystem.API/Services/Repositories/BusinessObjects)
- Shared test fixtures (tests/RoadGuardSystem.ApiTests/Infrastructure)
- Existing tests (P207, P2)

## Dependencies

### External Dependencies

- Docker Desktop (for Testcontainers SQL Server)
- .NET 8 SDK
- SQL Server 2019+ Docker image

### NuGet Packages (already present)

- Microsoft.AspNetCore.Mvc.Testing
- FluentAssertions
- Testcontainers
- xUnit

### Test Fixtures (reused)

- AuthenticationSqlServerFixture: SQL Server container, connection string, CreateDbContext, CreateUserAsync
- AuthenticationWebApplicationFactory: WebApplicationFactory with JWT authentication

## Test Execution Command (for BOX 3)

```bash
dotnet test tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs \
  --filter "TaskId=RF-10-09-C01" \
  --logger "trx;LogFileName=rf1009-c01-results.trx" \
  --verbosity normal
```

### Expected Results

- **Tests discovered:** 5
- **Tests passed:** 5
- **Tests failed:** 0
- **Tests skipped:** 0

### Test Methods

1. InboxGet_RecipientScopeAndProjection_FiltersAndReturnsExpectedFields
2. InboxList_RecipientScopeAndCursor_FiltersAndPaginatesCorrectly
3. MarkRead_RecipientVersionIdempotency_EnforcesConstraintsAndReplays
4. OutboxConsumer_Replay_ReturnsDurableNotificationWithoutDuplicate
5. Producer_CreateOutbox_LinksCorrectCorrelationAndType

## Key Findings

### Verified Behavior

✅ **HTTP inbox authorization:** Recipient-scoped GET/List, 404 for wrong user  
✅ **Projection correctness:** id, message, resourceId, read, occurredAtUtc, version (Base64 RowVersion)  
✅ **Cursor pagination:** Base64 JSON encoding of (OccurredAtUtc, Id), DESC ordering  
✅ **Mark-read idempotency:** Idempotency-Key replay returns 200 OK, same-key conflict returns 409  
✅ **Version checking:** If-Match validation, 412 for stale version, RowVersion updated after mutation  
✅ **Outbox consumer replay:** ConsumerEffectService prevents duplicate notification, single receipt  
✅ **Producer→outbox linkage:** SurveyAssignment creates outbox with correct correlationId and messageType  
✅ **Lease acquisition:** UPDLOCK+READPAST at repository boundary, DeliveryAttemptCount incremented

### Critical Limitation: NO DISPATCHER FOUND

❌ **No production BackgroundService or IHostedService found calling IOutboxWorkRepository.TryLeaseNextAsync**

**Search performed:**
- `rg -i "BackgroundService|IHostedService" --type cs` → no matches calling outbox repository
- `rg "IOutboxWorkRepository" --type cs -A 5` → found interface/implementations, no active caller
- Inspected UploadVerificationWorker (uses IUploadService pattern, not outbox)
- Inspected ValidationRunWorker (different mechanism)

**Implications:**
- Outbox messages accumulate in Pending status without processing
- Lease expiry, retry loop, DeadLetter transitions not exercised in production
- Notification delivery timing cannot be characterized
- This is an architectural gap, not a characterization failure

**Documented as:** NOT_VERIFIED in scope-matrix.md and RF-10-09-C01.md

### NOT_VERIFIED (documented limitations)

- Dispatcher/scheduler execution of lease/retry loop
- Real notification delivery timing and backoff intervals
- DeadLetter transition via dispatcher reaching maxAttempts
- Cursor format comprehensive edge cases
- External consumer integration (Web/Android/AI)
- Notification reports, retention, delete (Q-RF02-08)

## BOX 2 Constraints Compliance

✅ **No production code modified:** Only new test file created  
✅ **No shared fixtures modified:** Reused existing AuthenticationSqlServerFixture, AuthenticationWebApplicationFactory  
✅ **No build/test executed:** Test marked NOT_RUN, BOX 3 will verify  
✅ **No commit/push:** Working tree changes only  
✅ **No shared database writes:** Test uses isolated Testcontainers SQL Server  
✅ **Allowlist documented:** allowlist-before.txt captured before changes  
✅ **Hashes captured:** Before-state hashes of ledger, existing tests, new test  
✅ **Evidence isolated:** rf1009-c01/ directory contains all BOX 2 artifacts

## Handoff to BOX 3

**BOX 2 Status:** READY

**BOX 2 has stopped modifying tests and documentation.**

**BOX 3 responsibilities:**
1. Build solution with new test file
2. Execute test: `dotnet test --filter "TaskId=RF-10-09-C01"`
3. Verify 5/5 tests pass
4. Capture TRX results to evidence directory
5. Update planning/refactor/10-refactor-checklist.md entry for 10-09-C01
6. Update planning/refactor/10-refactor-slices.md RF-10-09 status
7. Mark task as "Done locally" in slice ledger

**If tests fail:**
- Inspect TRX file for specific assertion failure
- Verify Docker/Testcontainers SQL Server image available
- Check migrations applied by AuthenticationSqlServerFixture
- DO NOT modify test without BOX 2 review (invalidates READY status)

## Package Creation Requirements

BOX 2 will create `RF-10-09-C01-box2-review.zip` containing:
- Changed tests: Rf1009NotificationInboxCharacterizationTests.cs
- Documentation: baseline, report, evidence files
- Source context: snippets from inspected source files
- Manifest: path, size, SHA-256 for each file

Package verification checks:
- No missing files (manifest vs actual)
- No hash mismatches (manifest SHA-256 vs actual)
- No unlisted files (actual vs manifest)
- No duplicates (unique paths)

---

**BOX 2 READY marker:** 2026-10-01  
**Next:** BOX 3 verification and ledger updates
