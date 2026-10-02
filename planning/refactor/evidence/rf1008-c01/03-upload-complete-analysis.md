# RF-10-08-C01 Upload Complete Handler Analysis

**Generated:** 2026-10-01  
**Purpose:** Document CompleteAsync precondition/idempotency ordering

## UploadPersistenceService.CompleteAsync

**Location:** `RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs` lines 219-305

**Operation name:** `"UploadSessionCompleted"` (line 16 constant)

## Precondition Ordering Pattern

**Pattern:** Preconditions checked INSIDE idempotency handler (similar to Pattern A Survey Postponement)

### Handler Execution Flow (lines 225-268)

1. **Line 225-230:** Idempotency wrapper invoked with:
   - Scope: `(actorUserId, projectId, "UploadSessionCompleted", idempotencyKey)`
   - Request fingerprint from controller

2. **Lines 233-249:** Preconditions checked INSIDE handler:
   - Line 233-234: Session existence check
   - Line 236-239: Checksum validation
   - Line 241-245: Parts completeness validation  
   - Line 247-249: **RowVersion concurrency check** - throws `UploadConcurrencyException` if stale

3. **Lines 252-266:** Business logic execution:
   - Session.StartVerification mutation
   - UploadParts completion recording
   - AuditLog creation
   - SaveChanges

4. **Line 267:** Return outcome with view serialization

### Exception Handling (lines 280-304)

- **Line 288-290:** `UploadConcurrencyException` → `ConcurrencyConflict` status
- **Line 284-286:** `FormatException` (from Base64 decode) → `ConcurrencyConflict` status
- **Line 300-303:** `DbUpdateConcurrencyException` → `ConcurrencyConflict` status with ChangeTracker.Clear()

**KEY DIFFERENCE from Survey Postponement:**
- Upload Complete **throws** on concurrency conflict (line 249)
- Survey Postpone **stores** concurrency result in outcome JSON and commits (SurveyV2PersistenceService line 65)

## Stored Outcome Behavior

### Success Path (lines 252-267)
- Outcome JSON contains: `ToView(session, scope.ProjectId)` with **status=VERIFYING** at mutation time
- Stored status reflects the moment of execution, NOT current state

### Failure Path - Concurrency Conflict
- Exception thrown (line 249) → transaction rolls back
- NO outcome stored in IdempotencyRecord
- Retry with same key/fingerprint will execute handler again and re-check current RowVersion

**Contrast with Survey Postpone:**
```csharp
// Survey line 63-66: precondition checked, failure stored
if (!plan.RowVersion.SequenceEqual(Convert.FromBase64String(request.ExpectedVersion)))
{
    return (Guid.NewGuid(), JsonSerializer.Serialize(new StoredPlanOutcome(
        SurveyV2PersistenceStatus.ConcurrencyConflict, null)));
}
// Transaction commits with stored failure
```

```csharp
// Upload line 247-249: precondition checked, failure thrown
if (!session.RowVersion.SequenceEqual(Convert.FromBase64String(request.ExpectedVersion)))
{
    throw new UploadConcurrencyException();
}
// Transaction rolls back, no outcome stored
```

## Implications for Gap 5 Testing

**Gap 5:** Precondition changes before replay (Pattern A)

**Survey Postpone (TRUE Pattern A):**
1. First request: stale version → stored outcome = `ConcurrencyConflict`
2. Replay: same key/fingerprint → replayed status with stored `ConcurrencyConflict`, NO re-check

**Upload Complete (THROWS on precondition failure):**
1. First request: stale version → exception thrown, transaction rolled back, NO idempotency record
2. Retry: same key/fingerprint → NO replay (no record exists), executes handler, re-checks RowVersion

**TEST DESIGN:**
- Upload Complete does NOT demonstrate Gap 5 behavior (precondition stored in outcome)
- Survey Postpone is the ONLY Pattern A example for Gap 5
- Must write focused test: postpone with stale → 412 stored, replay with same key/fingerprint → 412 replayed WITHOUT re-checking current version

## Authorization Timing

**Line 225-230:** Idempotency called with actorUserId/projectId from controller
**Controller line 34:** Authorization extracted BEFORE service call

Authorization is BEFORE idempotency, same as all operations (Pattern A/B/C all check auth first).

## Operation Summary

| Aspect | Upload Create | Upload Complete | Survey Postpone |
|---|---|---|---|
| Precondition | None | RowVersion inside handler | RowVersion inside handler |
| Precondition failure handling | N/A | **THROWS** exception, rolls back | **STORES** in outcome, commits |
| Replay with stored precondition failure | N/A | No record exists (exception path) | Returns stored failure |
| Pattern | Simple authorization → idempotency | Modified Pattern A (throws) | Pure Pattern A (stores) |
| Gap 5 candidate | No | **No** | **Yes** |

## Next Step

Focus Gap 5 test on **Survey Postpone only** as the pure Pattern A example with stored precondition outcome.
