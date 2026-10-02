# Checkpoint 13 Correction-01 Summary

**Date:** 2026-10-01
**Branch:** anh
**HEAD:** 2efc8a5775f834c7f0fe37cc0ce703011649e1f1
**Writer:** BOX 3 (sole writer, continuation)
**Mandate:** Fix test logic to match current production implementation

## Previous State

**Package:** RF-10-checkpoint-13-three-box-handoff.zip
**SHA-256:** a72af4e20c52e1ad2aff5b2d2740212b40ebaa4f976fe1e43cdcfd3dda81efe5
**Previous Results:** 8/15 PASS (53%)
- BOX 1: 5/10 PASS, 5/10 FAIL
- BOX 2: 3/5 PASS, 2/5 FAIL

## Current State After Corrections

**Results:** 12/15 PASS (80%)
- BOX 1: 8/10 PASS, 2/10 FAIL
- BOX 2: 4/5 PASS, 1/5 FAIL

**Improvement:** +4 tests fixed (27% improvement)

---

## Corrections Applied

### 1. BOX 2 Compilation Fixes (5 errors → 0 errors)

**File:** tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs

**Fixes:**
- Line 6: Added `using RoadGuardSystem.BusinessObjects.Surveys;`
- Lines 233-251: Changed `DbSet<SurveyPlanScope>` to `DbSet<SurveyPlanScopes>` (table name)
- Lines 348-366: Replaced anonymous object with `SurveyAssignmentData` record
- Lines 356-366: Fixed `SurveyAssignment.Create()` call with 5 nullable parameters
- Lines 467-476: Replaced anonymous with `sealed record NotificationCorrelationData`

**Evidence:** See phase-b-build-corrected.txt

---

### 2. BOX 1 Upload Status Fixes (2 tests fixed)

**File:** tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs

**Issue:** Tests expected Status = "ACTIVE" but production returns "PENDING"

**Root Cause Analysis:**
- UploadSessionStatus enum: Pending = 1, Uploading = 2, Verifying = 3, Verified = 4
- Entity created with Status = Pending (line 63 of UploadSession.cs)
- ToView converts via `ToString().ToUpperInvariant()` → "PENDING"

**Production Source:**
```csharp
// RoadGuardSystem.BusinessObjects/Files/UploadSession.cs:63
Status = UploadSessionStatus.Pending

// RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs:397
session.Status.ToString().ToUpperInvariant()  // "PENDING"
```

**Fixes:**
1. Line 86: Changed `"ACTIVE"` → `"PENDING"`
2. Line 94: Changed `UploadSessionStatus.Uploading` → `UploadSessionStatus.Pending`
3. Line 937: Already correct `"PENDING"`
4. Line 948: Added `session.StartUploading()` before `StartVerification()` call

**Tests Fixed:**
- ✅ UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome
- ✅ UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus

**Evidence:** See correction-01-finding-upload-status.md

---

### 3. BOX 1 Stream Disposal Fix (1 test → new issue)

**File:** tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs

**Original Issue:** Reading response stream twice caused ObjectDisposedException

**Fix Applied (lines 735-738):**
```csharp
// Before: await response.Content.ReadFromJsonAsync<JsonElement>() twice
// After: Buffer content once
var planResponseText = await createdPlan.Content.ReadAsStringAsync();
var planBody = JsonDocument.Parse(planResponseText).RootElement;
var planId = planBody.GetProperty("id").GetGuid();
var version1 = planBody.GetProperty("version").GetString()!;
```

**Result:** Stream disposal fixed, but revealed business rule violation:
```
System.ArgumentException: New planned start must not be after the planned end.
at SurveyPlan.Postpone(Nullable`1 newPlannedStartAt)
```

**Status:** Test logic issue - postpone date violates SurveyPlan business rule
**Remaining Work:** Adjust test dates to satisfy start <= end constraint

**Test:** SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure (still FAIL)

---

### 4. BOX 1 Role Revocation Constructor Fix (compilation only)

**File:** tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs

**Issue:** `new IdentityRepository(roleContext, _timeProvider)` caused compilation error

**Production Source:** IdentityRepository constructor takes only DbContext parameter

**Fix Applied (line 862):**
```csharp
// Before: var repo = new IdentityRepository(roleContext, _timeProvider);
// After:  var repo = new IdentityRepository(roleContext);
```

**Result:** Compilation fixed, but test still fails with 403 Forbidden on replay

**Test:** ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome (still FAIL)

**Status:** OPEN - Requires production source inspection of IdempotencyOperationService

---

## Remaining Issues

### Issue 1: Role Revocation Replay (BOX 1)

**Test:** ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome
**Status:** ❌ FAIL
**Error:** Expected 201 Created, got 403 Forbidden

**Scenario:**
1. Supervisor creates project → 201 Created (stored in idempotency)
2. Test revokes supervisor role atomically
3. Replay with same key → 403 Forbidden (authorization fails)

**Question:** Does IdempotencyOperationService check authorization BEFORE or AFTER replay lookup?

**Options:**
- Production correct: Authorization always checked first (test expectation wrong)
- Production defect: Replay should return stored outcome regardless of current authorization
- Configuration missing: ProjectService.CreateAsync not configured for idempotency replay

**Requires:** Production source inspection of IdempotencyOperationService.ExecuteAsync

---

### Issue 2: SurveyPlan Postpone Date (BOX 1)

**Test:** SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure
**Status:** ❌ FAIL
**Error:** ArgumentException - New planned start must not be after planned end

**Current Logic:** Stream disposal fixed, now hits business rule validation

**Requires:** Adjust postpone request dates to satisfy SurveyPlan.Postpone constraints

---

### Issue 3: SurveyAssignmentData Entity (BOX 2)

**Test:** Producer_CreateOutbox_LinksCorrectCorrelationAndType
**Status:** ❌ FAIL
**Error:** Entity type 'SurveyAssignmentData' was not found in model

**Root Cause:** Test uses raw entity seeding instead of proper factory/service

**Fix Options:**
- Option A: Register SurveyAssignmentData in RoadGuardDbContext ModelBuilder
- Option B: Rewrite test to use SurveyAssignmentService or valid factory pattern

**User Mandate:** "đổi thành entity/factory hợp lệ, không dùng raw SQL với schema cũ"

---

## Source Hashes

**Tests After Corrections:**
```
21563b544d8403b0b925fb8339f30ed86d27a34c1cc43c68d581889b4d80850f  IdempotencyPerCommandCharacterizationTests.cs
714fa8780750be1748e131c959d1ab845e1b8d70a53bf43dfd332095d4462675  Rf1009NotificationInboxCharacterizationTests.cs
```

**Stored in:** correction-01-tests-after.sha256

---

## Build Evidence

**Build Command:**
```bash
dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-incremental
```

**Result:** 0 errors, 179 warnings
**Duration:** ~5-7 seconds

---

## Test Execution Evidence

### BOX 1 Sequential Run

**Command:**
```bash
dotnet test --filter "TaskId=RF-10-08-C01" --logger "trx;LogFileName=RF-10-08-C01-correction-final.trx" --no-build
```

**Results:**
- Discovered: 10 tests
- Passed: 8
- Failed: 2
- Duration: 4 seconds

**TRX File:** RF-10-08-C01-correction-final.trx
**Console Log:** correction-01-box1-final-console.txt

**Passing Tests:**
1. UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome ✅
2. UploadCreate_SameKeySameFingerprint_BlockedWithConflict ✅
3. UploadCreate_SameKeyDifferentPayload_BlockedWithConflict ✅
4. UploadCreate_DifferentActorsSameKey_BothExecute ✅
5. UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus ✅
6. UploadCreate_SameActorDifferentKeys_BothExecute ✅
7. UploadCreate_DifferentProjectsSameKey_BothExecute ✅
8. UploadCreate_ReplayBeforeFirstCommit_SeesNoRecord ✅

**Failing Tests:**
1. SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure ❌
2. ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome ❌

---

### BOX 2 Sequential Run

**Command:**
```bash
dotnet test --filter "TaskId=RF-10-09-C01" --logger "trx;LogFileName=RF-10-09-C01-correction-final.trx" --no-build
```

**Results:**
- Discovered: 5 tests
- Passed: 4
- Failed: 1
- Duration: 2 seconds

**TRX File:** RF-10-09-C01-correction-final.trx
**Console Log:** correction-01-box2-final-console.txt

**Passing Tests:**
1. Consumer_DeliveryReplayWithFreshContext_UsesNewNotificationId ✅
2. Consumer_MarkReadIdempotency_OnlyFirstSuccessChangesState ✅
3. InboxGet_BaselineAfterAuthentication_ReturnsEmptyItems ✅
4. InboxGet_RecipientScopeAndProjection_FiltersAndReturnsExpectedFields ✅

**Failing Tests:**
1. Producer_CreateOutbox_LinksCorrectCorrelationAndType ❌

---

## Constraints Compliance

✅ No production/schema/contract/migration/CI modification
✅ No commit/push/reset/clean/stash
✅ No shared database writes
✅ Test files modified within allowlist
✅ Isolated fixture DB only
✅ Before/after hashes captured
✅ Sequential test runs with TRX
✅ Full console logs preserved

---

## Summary

**Work Completed:**
- Fixed 5 compilation errors (BOX 2)
- Fixed 2 upload status tests (BOX 1)
- Fixed 1 stream disposal issue (BOX 1, revealed new issue)
- Fixed 1 constructor call (BOX 1, compilation only)
- Verified actor isolation logic (BOX 1, source inspection)

**Test Improvement:** 8/15 → 12/15 (+4 tests, +27%)

**Remaining Open Issues:** 3 test failures
- 2 in BOX 1 (role revocation replay, postpone dates)
- 1 in BOX 2 (entity registration)

**Next Steps:**
1. Inspect IdempotencyOperationService for authorization timing
2. Fix SurveyPlan postpone date validation
3. Replace SurveyAssignmentData raw seeding with factory

**Status:** Partial completion - 80% pass rate achieved, 3 issues documented for production investigation

---

**Generated:** 2026-10-01T14:10:00Z
**Evidence Directory:** planning/refactor/evidence/checkpoint13-integration/
