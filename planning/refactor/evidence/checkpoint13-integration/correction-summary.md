# Checkpoint 13 Integration - Correction Summary

**Date:** 2026-10-01  
**Branch:** anh  
**HEAD:** 2efc8a5775f834c7f0fe37cc0ce703011649e1f1  
**Writer:** BOX 3 (sole writer)

---

## Corrections Applied

### BOX 1 Test (IdempotencyPerCommandCharacterizationTests.cs)

**1 fix applied:**
- **Line 233:** Changed `SurveyPlanScope` → `SurveyPlanScopes` (DbSet naming correction)

**CharacterizationUploadStorage mock:** Already correctly implemented. Previous Phase B error report was incorrect - no compilation errors existed in mock implementation.

### BOX 2 Test (Rf1009NotificationInboxCharacterizationTests.cs)

**4 fixes applied:**
1. **Line 6:** Added missing `using RoadGuardSystem.BusinessObjects.Surveys;`
2. **Lines 348-366:** Fixed `CreateSurveyAssignment` anonymous object → typed record pattern
3. **Lines 356-366:** Fixed `SurveyAssignment.Create` call - added 5 missing nullable parameters (acceptedAt, rejectedAt, rejectionReason, reassignmentReason, endedAt)
4. **Lines 467-476:** Replaced anonymous object with `SurveyAssignmentData` sealed record

---

## Build Result

✅ **Build succeeded:** 0 errors, 179 warnings

---

## Test Results

### BOX 1 (RF-10-08-C01): ⚠️ 5/10 PASS, 5 FAIL

**Passed (5):**
1. RoadCreateThenModify_ReplaysStoredOutcome
2. RoadCreateDifferentActors_BothSucceed
3. UploadCreateDifferentPayload_CreatesNewUpload
4. SurveyPlanCreate_SameKeySamePayload_ReplaysIdentical
5. SurveyPlanCreate_DifferentActorsSameKey_BothExecute

**Failed (5):**
1. **UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome**
   - Expected: Status "ACTIVE"
   - Actual: Status "PENDING"
   - Root cause: Test expects status=ACTIVE but production returns PENDING on create
   
2. **UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus**
   - Expected: originalStatus "ACTIVE"
   - Actual: originalStatus "PENDING"
   - Same root cause as #1
   
3. **UploadCreate_DifferentActorsSameKey_BothExecute**
   - Expected: actor2Response HttpStatusCode.Created (201)
   - Actual: HttpStatusCode.Forbidden (403)
   - Root cause: Test expects both actors can create uploads with same key, but production enforces single-actor ownership
   
4. **ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome**
   - Error: InvalidOperationException - Direct modification of ApplicationUser.RoleCode forbidden
   - Root cause: Test tries `user.RoleCode = UserRoleCode.Inspector` but production enforces atomic role change through IIdentityRepository.ChangeUserRoleAtomicAsync
   
5. **SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure**
   - Error: ObjectDisposedException - Cannot access closed Stream
   - Root cause: Test attempts to read response stream twice without rewinding

### BOX 2 (RF-10-09-C01): ⚠️ 3/5 PASS, 2 FAIL

**Passed (3):**
1. MarkRead_SameKeySamePayload_IdempotentNoSecondUpdate
2. MarkRead_DifferentRecipient_IndependentMarking
3. Consumer_ReplayPreventsDuplicate_UsesStoredProjection

**Failed (2):**
1. **InboxGet_RecipientScopeAndProjection_FiltersAndReturnsExpectedFields**
   - Error: KeyNotFoundException - key not present in dictionary
   - Root cause: Test expects response field that doesn't exist in actual API projection
   
2. **Producer_CreateOutbox_LinksCorrectCorrelationAndType**
   - Error: SqlException - Invalid column names: Address, WarrantyStartDate, WarrantyEndDate
   - Root cause: Raw SQL INSERT in CreateProjectWithMemberAsync uses obsolete Project schema columns

---

## Test Count Verification

✅ **BOX 1:** 10 [Fact] methods (not 11 as originally reported)  
✅ **BOX 2:** 5 [Fact] methods (matches report)

---

## Findings

### F-C13-01: BOX 1 test expectations mismatched with production behavior

**Severity:** High  
**Impact:** 3 tests fail due to incorrect assumptions about production implementation

1. **Upload status:** Tests expect ACTIVE but production returns PENDING
2. **Upload actor isolation:** Tests expect different actors can create uploads with same key, but production enforces single-actor ownership (returns 403)
3. **Role mutation:** Tests attempt direct RoleCode modification which violates production invariant

**Required action:** Update test expectations to match actual production behavior OR clarify RF-10-08-C01 scope if tests reveal genuine production defects.

### F-C13-02: BOX 1 test implementation error (stream disposal)

**Severity:** Medium  
**Impact:** 1 test fails due to test code bug (not production issue)

SurveyPlanPostpone test attempts to read HttpResponseMessage.Content stream twice without rewinding, causing ObjectDisposedException.

**Required action:** Fix test to buffer response or rewind stream before second read.

### F-C13-03: BOX 2 test schema mismatch

**Severity:** High  
**Impact:** 1 test fails due to obsolete schema columns in raw SQL

Producer_CreateOutbox test uses raw SQL INSERT with columns that no longer exist in Project table (Address, WarrantyStartDate, WarrantyEndDate).

**Required action:** Update CreateProjectWithMemberAsync raw SQL to use current Project schema.

### F-C13-04: BOX 2 test API contract assumption

**Severity:** Medium  
**Impact:** 1 test fails due to missing response field

InboxGet test expects a response field that doesn't exist in actual API projection (KeyNotFoundException on dictionary access).

**Required action:** Verify actual inbox GET response structure and update test assertions to match.

---

## Hash Verification

**Before correction:**
- BOX 1: `f6dc771fa337a86a7d2bf4f259d9bf1ff8430522a9057b68ad834bdced8e8cce`
- BOX 2: `1f233dc690351172812d5c5d413488457c7ebcc2e0b18193e042a0d07854f06f`

**After correction:** See `correction-tests-after.sha256`

---

## Constraints Observed

✅ No production/schema/contract/migration/CI modification  
✅ No commit/push/merge/reset/clean/stash  
✅ No shared database writes  
✅ Preserved dirty working tree outside allowlist  
✅ Only test files modified per correction mandate

---

## Next Steps

**Option A - Continue with current test results:**
- Package evidence as-is with 5/10 and 3/5 pass rates
- Document findings F-C13-01 through F-C13-04
- Mark integration PARTIAL SUCCESS with known limitations

**Option B - Fix tests to match production:**
- Update BOX 1 test expectations (PENDING status, 403 actor isolation, atomic role change)
- Fix BOX 1 stream disposal bug
- Update BOX 2 raw SQL schema
- Investigate BOX 2 API projection mismatch
- Re-run tests for higher pass rate

**Recommendation:** Option B - fix tests to achieve higher pass rate before final packaging, since user instruction was "sửa lỗi test hiện có, kiểm chứng đúng phạm vi RF-10-08-C01 và RF-10-09-C01" (fix existing test errors, verify correct scope).

---

**Status:** Compilation successful, tests executed, 8/15 tests pass, 7/15 fail with documented root causes.
