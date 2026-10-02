# RF-10-08-C01 Existing Test Coverage Survey

**Generated:** 2026-10-01  
**Purpose:** Document existing idempotency test coverage to identify gaps

## Coverage Summary

Existing tests provide **partial HTTP-level** and **constraint-level** coverage but lack **comprehensive per-command idempotency characterization** across authorization/precondition timing, actor/project isolation, and stored outcome vs current state verification.

## Existing Test Files

### 1. P202TransactionAndIdempotencyTests.cs (Integration, 283 lines)

**Location:** `tests/RoadGuardSystem.IntegrationTests/Persistence/P202TransactionAndIdempotencyTests.cs`

**Coverage:**
- **Line 21-48:** `SameScopedKeyWithChangedFingerprint_IsRejected`
  - Verifies SQL unique constraint violation (error 2601/2627) when duplicate key with different fingerprint
  - Direct SQL insertion, NOT through HTTP/IdempotencyOperationService
  - Tests constraint layer, not production call chain
  
- **Line 50-68:** `InvalidIdempotencyOutcomeJson_IsRejected`
  - Verifies SQL check constraint rejects non-JSON outcome
  - Direct SQL constraint test
  
- **Line 70-169:** `TransactionService_ForcedFailureRollsBackStagedWrite`
  - Comprehensive transaction rollback test
  - Verifies idempotency record is rolled back with staged writes
  - Uses forced failure interceptors
  - Does NOT test post-commit recovery path (lines 150-159 in IdempotencyOperationService)

**Gaps:**
- No HTTP-level replay (same key + same fingerprint → Replayed status)
- No HTTP-level conflict (same key + different fingerprint → Conflict status)
- No actor/project scope isolation verification
- No authorization/precondition timing with replay
- No stored outcome vs current state distinction

### 2. UploadApiTests.cs (API, 300+ lines)

**Location:** `tests/RoadGuardSystem.ApiTests/Files/UploadApiTests.cs`

**Coverage:**
- **Line 30-76:** `UploadCreate_CurrentIntBoundaryOverflowsValidationWithoutWriting`
  - Tests int.MaxValue + 1 and 8 GiB overflow → 500 `internal_error`
  - Verifies NO file/session/scope created (line 73-75)
  - Uses unique idempotency key per attempt (line 64)
  - Bug finding, NOT idempotency characterization
  
- **Line 79-168:** `UploadEndpoints_EnforceScopeIdempotencyAndIfMatchBeforeVerifiedDownload`
  - **Idempotency:** Create request with header `Idempotency-Key` (line 114)
  - **Precondition:** Complete with `If-Match` (line 134, 142) - stale version → 412
  - **Authorization:** Outsider GET → 403 (line 159-160)
  - **Scope verification:** FileScope.ProjectId matches (line 165-166)
  - **Status verification:** Session.Status after stale/success complete (line 139, 164)
  - Uses unique keys for create/parts/complete - NO replay or conflict test
  
- **Line 172-242:** `UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`
  - Smoke test with real MinIO
  - Unique key per request (line 198, 209), NO idempotency characterization

**Gaps:**
- No upload creation replay test (same key, same payload → Created with identical body)
- No upload creation conflict test (same key, different payload → 409 `duplicate_request`)
- No upload complete replay test
- No actor isolation (two actors, same key)
- No project isolation (same actor/key, different projects)

### 3. P120ProjectCreationTests.cs (API, 427 lines)

**Location:** `tests/RoadGuardSystem.ApiTests/Projects/P120ProjectCreationTests.cs`

**Coverage:**
- **Line 27-127:** `CreateProjectSupervisorSuccessPersistsOneAtomicAggregateAndReplays`
  - **Replay:** Line 78-80 sends same request with same `operationId` → 201 Created with identical body text
  - **State change before replay:** Line 70-76 suspends PM, replay still returns original stored outcome
  - **Concurrent requests:** Line 104-111 sends same request twice → both 201, same projectId
  - **Commit failure recovery:** Line 119-126 tests both `FailOnceBeforeCommitInterceptor` and `FailOnceAfterCommitInterceptor`
  - **Verification:** Line 82-94 checks single project/member/document/audit/idempotency record
  - **Post-commit recovery:** Line 341-386 `AssertCommitFailureRecoveryAsync` - after-commit interceptor returns `Replayed` status (line 369-371)
  
- **Line 160-196:** `CreateProjectCanonicalRequestCreatesWarrantyAndReplaysByHeaderKey`
  - **Header-based key:** Uses `Idempotency-Key` header (line 168), not operationId in body
  - **Replay:** Line 193-195 sends same request with same header → 201 with identical response text
  - **Verification:** Line 189-191 checks warranty and primary member created
  
- **Line 222-324:** `CreateProjectInvalidOrChangedRequestReturnsStableErrorsWithoutExtraProject`
  - **Changed payload conflict:** Line 282-288 first request succeeds, changed payload with same operationId → 409 `duplicate_request`
  - **Delimiter fingerprint:** Line 299-313 shows pipe-delimited fingerprint detects ProjectCode "A|B" vs "A" + Name "B|C" as different
  - **Validation:** Missing handover/PM/file → 400/404 without persisting (line 315-323)

**Gaps:**
- No authorization change before replay (actor loses Supervisor role, then retries)
- No precondition change before replay (PM eligibility checked before idempotency - Pattern B - so precondition can't be stored in outcome)
- No project scope isolation (same actor/key, different projectId - but projectId is null for creation)
- No actor isolation (different actors, same key for different projects)

### 4. P2SurveyV2ApiTests.cs (API, 177 lines)

**Location:** `tests/RoadGuardSystem.ApiTests/Surveys/P2SurveyV2ApiTests.cs`

**Coverage:**
- **Line 23-127:** `SurveyV2Endpoints_CreateReplayPostponeWithConcurrencyAndReadScope`
  - **Plan creation:** Line 65-72 POST with `Idempotency-Key` → 201
  - **Duplicate key different payload:** Line 74-77 different scope with different key → 409 Conflict (scope validation, NOT idempotency)
  - **Plan replay:** Line 79-82 same payload with same key → 201 Created
  - **Postpone success:** Line 84-87 POST postpone with current version → 200, new version
  - **Postpone stale:** Line 89-90 POST postpone with old version → 412 Precondition Failed
  - **Task creation:** Line 92-109 POST with `Idempotency-Key` → 201 NEW_ASSIGNED
  - **Authorization:** Line 115-117 other operator GET task → 403
  - **Verification:** Line 118-126 checks single plan (no duplicate from replay), single postponement (stale not added), single request

**Gaps:**
- No plan creation conflict (same key, different payload → should be 409 `duplicate_request`, but line 74-77 is scope validation conflict)
- No plan postpone replay (same key, same payload after successful postpone)
- No task creation replay (same key, same payload)
- No task creation conflict (same key, different payload)
- No precondition stored in outcome test (postpone with stale version, then replay with same key/fingerprint → should return stored 412 outcome, NOT re-check current version)
- No actor isolation (different actors, same key)
- No project isolation (same actor/key, different projects)

## Evidence Gap Matrix

| Gap Category | Upload Coverage | Project Coverage | Survey Coverage |
|---|---|---|---|
| Same key + same fingerprint replay | **MISSING** | ✓ P120 line 78-80, 193-195 | ✓ P2 line 79-82 (plan only) |
| Same key + different fingerprint conflict | **MISSING** | ✓ P120 line 282-288 | **MISSING** (line 74-77 is scope conflict) |
| Actor isolation (different actors, same key) | **MISSING** | **MISSING** | **MISSING** |
| Project isolation (same actor/key, different projects) | **MISSING** | N/A (projectId null for creation) | **MISSING** |
| Authorization change before replay | **MISSING** | Partial: PM suspended before replay (line 70-76) but returns stored outcome | **MISSING** |
| Precondition stored in outcome (Pattern A) | **MISSING** | N/A (Pattern B - precondition before idempotency) | **MISSING** (postpone stale line 89-90, no replay test) |
| Stored outcome vs current state | Partial: PM suspended, replay returns original (P120 line 70-80) | Same as upload | **MISSING** |
| Concurrent requests (race condition) | **MISSING** | ✓ P120 line 104-111 | **MISSING** |
| Commit failure recovery | **MISSING** | ✓ P120 line 119-126, 341-386 | **MISSING** |

## Test Design Recommendations

### High Priority - Missing Critical Paths

1. **Upload creation replay and conflict** (Gap 1, 2)
   - Reuse P120 pattern: same request twice → verify identical body text
   - Changed payload → verify 409 `duplicate_request`

2. **Survey postpone replay with stored precondition outcome** (Gap 6)
   - First postpone with stale version → 412, stores outcome
   - Replay with same key/fingerprint → verify returns stored 412, does NOT re-check current version
   - This is Pattern A characteristic

3. **Actor isolation** (Gap 3)
   - Two actors, same idempotency key, different operations → both execute
   - Verify composite key scope includes ActorUserId

4. **Project isolation** (Gap 4)
   - Same actor, same key, different projectId → both execute
   - Upload and Survey operations (Project creation has null projectId)

### Medium Priority - Strengthen Existing Coverage

5. **Survey task creation replay and conflict**
   - Complete the P2 test coverage for task operations

6. **Upload complete replay and conflict**
   - Complete endpoint has If-Match precondition, but no replay/conflict test

7. **Authorization change before replay - stronger test**
   - P120 line 70-76 suspends PM but they're not the actor (Supervisor is)
   - Need: actor role changed (Supervisor → PM), replay still returns stored outcome

### Lower Priority - Infrastructure Edge Cases

8. **Race condition explicit test**
   - P120 line 104-111 shows both succeed, but doesn't verify unique constraint catch path (IdempotencyOperationService line 55-70)

9. **Operation name isolation**
   - Same actor/project/key, different operation name → separate scopes

## Reuse vs New Test Decision

**Reuse candidates:**
- P120 replay pattern (line 78-80) for new upload/survey tests
- P120 conflict pattern (line 282-288) for fingerprint verification
- P2 precondition pattern (line 84-90) for stored outcome tests

**Must write new:**
- Actor isolation (no existing pattern)
- Project isolation (no existing pattern)
- Pattern A stored precondition outcome with replay (P2 has stale test but no replay)
- Upload idempotency (no existing replay/conflict)

## Next Steps

1. Select representative operations:
   - Upload: CreateAsync (Pattern A-like with simple authorization)
   - Survey: PostponePlanAsync (Pattern A with stored precondition)
   - Project: CreateAsync (Pattern B for contrast, already has strong coverage)

2. Write focused tests per gap:
   - One test class: `IdempotencyPerCommandCharacterizationTests.cs`
   - Group by operation, then by gap category
   - Reuse P120/P2 helper patterns

3. Positive controls:
   - Fresh actor/PM/operator creation per test
   - Fresh project per test for scope isolation
   - Comprehensive snapshots: actor, project, idempotency record, audit log, domain entity

4. Distinguish stored vs current:
   - Change entity state after first request
   - Verify replay returns original stored outcome, not current state
