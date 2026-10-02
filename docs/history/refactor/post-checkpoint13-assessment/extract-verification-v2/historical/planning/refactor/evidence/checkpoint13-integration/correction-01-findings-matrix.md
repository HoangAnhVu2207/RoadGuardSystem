# Checkpoint 13 Correction-01 Findings Matrix

**Date:** 2026-10-01
**Branch:** anh
**HEAD:** 2efc8a5775f834c7f0fe37cc0ce703011649e1f1
**Writer:** BOX 3

## Test Results Summary

**BOX 1 (RF-10-08-C01):** 8/10 PASS, 2/10 FAIL
**BOX 2 (RF-10-09-C01):** 4/5 PASS, 1/5 FAIL
**Overall:** 12/15 PASS (80%)

## Findings Matrix

| Finding ID | Severity | Component | Status | Description | Tests Affected |
|------------|----------|-----------|--------|-------------|----------------|
| F-C13-01a | High | BOX 1 | FIXED | Upload initial status mismatch (expected ACTIVE, actual PENDING) | 2 |
| F-C13-01b | High | BOX 1 | SOURCE_INSPECTED | Actor isolation requires valid project membership for both actors | 0 (passes after membership setup) |
| F-C13-01c | High | BOX 1 | OPEN | Role revocation replay expects 201 but gets 403 (idempotency vs authorization boundary) | 1 |
| F-C13-02 | Medium | BOX 1 | FIXED | Stream disposal bug reading response twice | 1 (new business rule violation) |
| F-C13-03 | High | BOX 2 | OPEN | SurveyAssignmentData not registered as EF entity | 1 |
| F-C13-04 | Medium | BOX 2 | SOURCE_INSPECTED | API projection verified correct | 0 (passes) |

## Detailed Findings

### F-C13-01a: Upload Status [FIXED]

**Component:** IdempotencyPerCommandCharacterizationTests.cs
**Original Issue:** Tests expected Status = "ACTIVE" but production returns "PENDING"
**Root Cause:** UploadSessionStatus enum starts with Pending = 1, converted to "PENDING" string
**Fix Applied:**
- Line 86: Changed assertion from "ACTIVE" to "PENDING"
- Line 94: Changed entity assertion from Uploading to Pending
- Line 937: Already corrected to "PENDING"
- Line 948: Added StartUploading() call before StartVerification()

**Tests Fixed:**
1. UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome - PASS
2. UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus - PASS

**Evidence:** See correction-01-finding-upload-status.md

---

### F-C13-01b: Actor Isolation [SOURCE_INSPECTED]

**Component:** IdempotencyPerCommandCharacterizationTests.cs
**Original Issue:** Second actor received 403 Forbidden
**Root Cause:** manager2 not added to project membership
**Inspection Result:** Test already includes CreateMembershipAsync(client, projectId, manager2.Id, ProjectMemberRole.Manager)
**Current Status:** Test passes after proper setup verification

**Production Logic Verified:**
- Idempotency scope includes actorUserId + projectId + operation + key
- Authorization checked via ProjectScopeGuard.AuthorizeAsync
- Both actors with valid membership can create separate uploads

**Test:** UploadCreate_DifferentActorsSameKey_BothExecute - PASS

**Evidence:** See correction-01-finding-actor-isolation.md

---

### F-C13-01c: Role Revocation Replay [OPEN]

**Component:** IdempotencyPerCommandCharacterizationTests.cs  
**Test:** ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome
**Issue:** Replay expects 201 Created but receives 403 Forbidden

**Current Behavior:**
1. Supervisor creates project → 201 Created (stored in idempotency)
2. Test revokes supervisor role via ChangeUserRoleAtomicAsync
3. Replay with same key → 403 Forbidden (authorization fails)

**Expected Behavior (per test):** Replay should return 201 with stored outcome regardless of current authorization

**Production Question:** Does IdempotencyOperationService.ExecuteAsync check authorization BEFORE replay lookup, or does it replay stored outcome regardless of current actor state?

**Status:** OPEN - Requires production source inspection to determine if this is:
- Test expectation incorrect (authorization always checked first)
- Production defect (replay should bypass authorization)
- Missing idempotency configuration (replay not configured for ProjectService.CreateAsync)

**Fix Attempted:** Changed IdentityRepository constructor call to remove _timeProvider parameter (compilation fix only)

---

### F-C13-02: Stream Disposal [FIXED → NEW ISSUE]

**Component:** IdempotencyPerCommandCharacterizationTests.cs
**Test:** SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure
**Original Issue:** Stream disposed after first ReadFromJsonAsync, second call failed

**Fix Applied:** Line 735-738, buffered response content before parsing:
```csharp
var planResponseText = await createdPlan.Content.ReadAsStringAsync();
var planBody = JsonDocument.Parse(planResponseText).RootElement;
var planId = planBody.GetProperty("id").GetGuid();
var version1 = planBody.GetProperty("version").GetString()!;
```

**New Issue After Fix:**
```
System.ArgumentException : New planned start must not be after the planned end. 
(Parameter 'newPlannedStartAt')
at RoadGuardSystem.BusinessObjects.Surveys.SurveyPlan.Postpone(Nullable`1 newPlannedStartAt)
```

**Status:** Test logic issue - postpone date violates business rule (start > end)
**Requires:** Test date adjustment to satisfy SurveyPlan.Postpone validation

---

### F-C13-03: SurveyAssignmentData Entity [OPEN]

**Component:** Rf1009NotificationInboxCharacterizationTests.cs
**Test:** Producer_CreateOutbox_LinksCorrectCorrelationAndType
**Issue:** `context.AddRange(project, route, segment, assignment)` fails with:
```
System.InvalidOperationException : The entity type 'SurveyAssignmentData' was not found. 
Ensure that the entity type has been added to the model.
```

**Root Cause:** SurveyAssignmentData is a sealed record used for notifications but not registered in DbContext

**Production Impact:** Test uses raw entity seeding instead of proper factory/service
**Status:** OPEN - Requires either:
- Option A: Register SurveyAssignmentData in RoadGuardDbContext
- Option B: Rewrite test to use SurveyAssignmentService or valid factory

**User Instruction:** "Producer test: đổi thành entity/factory hợp lệ, không dùng raw SQL với schema cũ"

---

### F-C13-04: Inbox GET Projection [SOURCE_INSPECTED]

**Component:** Rf1009NotificationInboxCharacterizationTests.cs
**Test:** InboxGet_RecipientScopeAndProjection_FiltersAndReturnsExpectedFields
**Original Issue:** Suspected missing field in API projection
**Inspection Result:** Test passes - all expected fields present

**Current Status:** PASS (no fix needed)

---

## Test-by-Test Status

### BOX 1 (RF-10-08-C01)

| # | Test | Status | Finding |
|---|------|--------|---------|
| 1 | UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome | ✅ PASS | F-C13-01a FIXED |
| 2 | UploadCreate_SameKeySameFingerprint_BlockedWithConflict | ✅ PASS | - |
| 3 | UploadCreate_SameKeyDifferentPayload_BlockedWithConflict | ✅ PASS | - |
| 4 | UploadCreate_DifferentActorsSameKey_BothExecute | ✅ PASS | F-C13-01b inspected |
| 5 | UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus | ✅ PASS | F-C13-01a FIXED |
| 6 | UploadCreate_SameActorDifferentKeys_BothExecute | ✅ PASS | - |
| 7 | UploadCreate_DifferentProjectsSameKey_BothExecute | ✅ PASS | - |
| 8 | UploadCreate_ReplayBeforeFirstCommit_SeesNoRecord | ✅ PASS | - |
| 9 | SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure | ❌ FAIL | F-C13-02 new issue |
| 10 | ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome | ❌ FAIL | F-C13-01c OPEN |

### BOX 2 (RF-10-09-C01)

| # | Test | Status | Finding |
|---|------|--------|---------|
| 1 | Consumer_DeliveryReplayWithFreshContext_UsesNewNotificationId | ✅ PASS | - |
| 2 | Consumer_MarkReadIdempotency_OnlyFirstSuccessChangesState | ✅ PASS | - |
| 3 | InboxGet_BaselineAfterAuthentication_ReturnsEmptyItems | ✅ PASS | - |
| 4 | InboxGet_RecipientScopeAndProjection_FiltersAndReturnsExpectedFields | ✅ PASS | F-C13-04 verified |
| 5 | Producer_CreateOutbox_LinksCorrectCorrelationAndType | ❌ FAIL | F-C13-03 OPEN |

---

## Corrections Applied

### Compilation Fixes (BOX 2)
- Line 6: Added `using RoadGuardSystem.BusinessObjects.Surveys;`
- Lines 348-366: Replaced anonymous object with SurveyAssignmentData record
- Lines 356-366: Fixed SurveyAssignment.Create with 5 nullable parameters
- Lines 467-476: Replaced anonymous object with sealed record

### Test Logic Fixes (BOX 1)
- Upload status: 2 tests fixed (ACTIVE → PENDING, Uploading → Pending)
- Upload status mutation: Added StartUploading() before StartVerification()
- Stream disposal: Buffered response before parsing (introduced new business rule issue)
- Role revocation: Fixed constructor call (compilation only, test still fails)

---

## Summary

**Progress:** 8/15 → 12/15 tests passing (53% → 80%)
**Fixes Applied:** 4 findings addressed (2 fully fixed, 2 source inspected)
**Open Issues:** 2 findings requiring production investigation
**New Issues:** 1 test logic error revealed after stream fix

**Remaining Work:**
1. F-C13-01c: Inspect IdempotencyOperationService authorization timing
2. F-C13-02: Fix postpone date in test to satisfy business rule
3. F-C13-03: Replace raw entity seeding with proper factory/service

---

**Generated:** 2026-10-01T14:00:00Z
**Evidence Package:** RF-10-checkpoint-13-correction-01-handoff.zip (pending)
