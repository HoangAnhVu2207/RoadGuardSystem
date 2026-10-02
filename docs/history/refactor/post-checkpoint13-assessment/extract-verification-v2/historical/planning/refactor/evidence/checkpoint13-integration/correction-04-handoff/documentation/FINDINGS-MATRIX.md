# Checkpoint 13 Correction-04 Findings Matrix

**Package:** RF-10-checkpoint-13-correction-04-handoff  
**Date:** 2026-10-01  
**Writer:** BOX 3  
**Status:** COMPLETE

---

## Findings Status Summary

| Finding ID | Status | Test Coverage | Evidence Type | Open Issues |
|------------|--------|---------------|---------------|-------------|
| F-C13-01a | FIXED | Upload OperationId + OutcomeJson immutability + scoped effect-set | Runtime + Receipt | None |
| F-C13-01b | FIXED | SurveyPlan OperationId + OutcomeJson immutability + scoped effect-set | Runtime + Receipt | None |
| F-C13-01c | VERIFIED | Authorization with new JWT returns 403 | Runtime | NOT_VERIFIED: original JWT |
| F-C13-02 | FIXED | Postpone receipt snapshot timing + OperationId + OutcomeJson immutability | Runtime + Receipt | None |
| F-C13-03 | VERIFIED | Consumer replay with candidate ID + fresh DbContext | Runtime + Receipt | None |
| F-C13-04 | VERIFIED | MarkRead ReadAt/RowVersion + receipt immutability | Runtime + Receipt | None |

---

## F-C13-01a: Upload Create Replay

**Original Gap:** Count by uploadId insufficient to detect candidate with different ID.

**Status:** FIXED (correction-04)

**Test Coverage (Lines 88-165):**
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint from snapshot1)
- OperationId immutability: `receipt.OperationId.Should().Be(firstOperationId)`
- OutcomeJson immutability: `receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint)`
- RequestFingerprint captured
- OutcomeJson contains uploadId and fileId
- **NEW:** Scoped effect-set count checks:
  - Session count by OwnerUserId + Purpose: must remain 1
  - Scope count by ProjectId + OwnerUserId: must remain 1
  - Audit log count by EntityId + EventType: must remain 1
  - Single IdempotencyRecord persisted

**Evidence:**
- Runtime: 10/10 PASS (RF-10-08-C01)
- Receipt scope: ActorUserId + ProjectId + "UploadSessionCreated" + key
- Replay returns stored outcome without re-executing handler
- Scoped queries detect duplicate entities with different IDs

**Open Issues:** None

---

## F-C13-01b: SurveyPlan Create Replay

**Original Gap:** Count by planId insufficient to detect candidate with different ID.

**Status:** FIXED (correction-04)

**Test Coverage (Lines 206-275):**
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint from snapshot1)
- OperationId immutability: `receipt.OperationId.Should().Be(firstOperationId)`
- OutcomeJson immutability: `receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint)`
- RequestFingerprint captured
- **NEW:** Scoped effect-set count checks:
  - Plan count by ProjectId + Status: must remain 1
  - Scope count by SurveyPlanId: must remain 1
  - Single IdempotencyRecord persisted

**Evidence:**
- Runtime: 10/10 PASS (RF-10-08-C01)
- Receipt scope: ActorUserId + ProjectId + "SurveyPlanV2Created" + key
- Replay returns stored outcome without re-executing handler
- Scoped queries detect duplicate entities with different IDs

**Open Issues:** None

---

## F-C13-01c: Role Revocation Replay Authorization

**Original Gap:** Test expected idempotency replay to bypass current authorization.

**Corrected Claim:** Test characterizes that replay authorization uses current JWT claims at replay time. Test obtains NEW JWT after role change (line 933: `await AuthenticateAsync(client, supervisor.UserName!)`), not using original JWT.

**Status:** VERIFIED (correction-02, no changes in correction-04)

**Test Coverage (Lines 891-957):**
- First request as Supervisor: returns 201 Created
- Role change: Supervisor → ProjectManager via IdentityRepository
- Re-authenticate to obtain NEW JWT with ProjectManager claims
- Replay request with NEW JWT: returns 403 Forbidden
- Project persisted exactly once (first operation succeeded)
- IdempotencyRecord persisted with stored outcome

**Evidence:**
- Runtime: 10/10 PASS (RF-10-08-C01)
- New JWT after role change returns 403 (replay enforces current authorization)
- Does NOT prove original JWT revocation

**Open Issues:**
- NOT_VERIFIED: Original JWT behavior after role change (F-C13-01c-HISTORICAL)
- User mandate: Mark as NOT_VERIFIED, not a blocker for bounded characterization

---

## F-C13-02: SurveyPlan Postpone Replay

**Original Gap:** Test did not prove whether replay re-evaluated version check or returned stored result without re-checking. Receipt snapshot captured AFTER replay, not before.

**Status:** FIXED (correction-04)

**Test Coverage (Lines 729-895):**
- **NEW:** Receipt baseline captured in snapshot1 BEFORE replay:
  - firstOperationId, firstRequestFingerprint, firstOutcomeJson
  - Receipt scope: ActorUserId + ProjectId null + "SurveyPlanV2Postponed" + key
  - OutcomeJson verification: contains status 7 (ConcurrencyConflict)
  - Postponement count check: 0 (no postponement record persisted)
- Replay with same payload: returns 412
- **NEW:** Receipt immutability in snapshot2 AFTER replay:
  - `receipt.OperationId.Should().Be(firstOperationId)`
  - `receipt.RequestFingerprint.Should().Be(firstRequestFingerprint)`
  - `receipt.OutcomeJson.Should().Be(firstOutcomeJson)`
- Single IdempotencyRecord persisted
- Zero postponements persisted (no state mutation)
- Plan version unchanged (version3 preserved)

**Evidence:**
- Runtime: 10/10 PASS (RF-10-08-C01)
- Receipt fingerprint comparison proves replay skips handler re-invocation
- Snapshot timing corrected: baseline before replay, immutability after replay

**Open Issues:** None

---

## F-C13-03: Consumer Replay with Candidate ID

**Original Gap:** Replay used same DbContext/consumer and same notification ID.

**Status:** VERIFIED (correction-02, no changes in correction-04)

**Test Coverage (Lines 320-397):**
- First consume with notification.Id: returns Recorded
- Fresh DbContext (context2) and consumer (consumer2) for replay
- Different candidate notification ID (candidateNotificationId ≠ notification.Id)
- Replay returns stored notification ID, not candidate ID
- Notification count: exactly 1 (scoped effect-set snapshot)
- Candidate notification NOT persisted (AnyAsync returns false)
- Receipt unchanged: ID, EffectId link to original notification
- Single ConsumerEffectReceipt persisted

**Evidence:**
- Runtime: 5/5 PASS (RF-10-09-C01) - REUSED from correction-03
- Consumer effect scope: MessageId + ConsumerName
- Replay with different candidate ID does not persist candidate

**Open Issues:** None

---

## F-C13-04: MarkRead Idempotency

**Original Gap:** Snapshot timing insufficient to prove ReadAt/RowVersion immutability.

**Status:** VERIFIED (correction-02, no changes in correction-04)

**Test Coverage (Lines 235-288):**
- Three-stage snapshot comparison:
  - Baseline: ReadAt = null
  - After first mark-read (snapshot1): ReadAt set, RowVersion changed
  - After replay (snapshot2): ReadAt unchanged, RowVersion unchanged
- ReadAt immutability: `afterReplay.ReadAt.Should().Be(afterFirstMark.ReadAt)`
- RowVersion immutability: `afterReplay.RowVersion.Should().Equal(afterFirstMark.RowVersion)`
- Receipt identity fields unchanged: Id, RecipientUserId, SourceEntityType, SourceEntityId, Body
- OperationId immutability: replay returns stored operation ID
- OutcomeJson immutability: replay returns stored outcome
- Single IdempotencyRecord persisted

**Evidence:**
- Runtime: 5/5 PASS (RF-10-09-C01) - REUSED from correction-03
- Receipt scope: ActorUserId + "NotificationRead" + key
- Replay does not mutate ReadAt or RowVersion

**Open Issues:** None

---

## Historical Limitations

**NOT_VERIFIED in this correction:**

1. **F-C13-01c-HISTORICAL:** Original JWT behavior after role change
   - Test obtains NEW JWT after role change
   - Does not characterize original JWT validity, revocation, or stale claim behavior
   - Reviewer confirmed NOT_VERIFIED, not a blocker

2. **Survey checkpoint 08 inspection provenance**
   - Not addressed in checkpoint 13 characterization

3. **Dispatcher/delivery workflow**
   - Not yet implemented

4. **Before-build provenance for correction-02**
   - Placeholder hashes in correction-02 package

---

## Test Results

**Overall:** 15/15 PASS (100% pass rate)

**BOX 1 (RF-10-08-C01):** 10/10 PASS
- IdempotencyPerCommandCharacterizationTests
- Duration: 5s
- Evidence: RF-10-08-C01-correction-04-final.trx

**BOX 2 (RF-10-09-C01):** 5/5 PASS
- Rf1009NotificationInboxCharacterizationTests
- Duration: 2s
- Evidence: RF-10-09-C01-correction-03-final.trx (REUSED - source unchanged)

---

## Source Provenance

**Before-build hashes:**
```
6f0bebf1bb2ad34abe8efce2b511556fdd4e901b118bc5e055fb57f2023aceb1 *IdempotencyPerCommandCharacterizationTests.cs
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**After-build hashes:**
```
a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f *IdempotencyPerCommandCharacterizationTests.cs
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**Changes:**
- IdempotencyPerCommandCharacterizationTests.cs: 
  - Postpone receipt snapshot timing fix (lines 803-826, 860-883)
  - Upload scoped effect-set count checks (lines 131-165)
  - Survey plan scoped effect-set count checks (lines 245-275)
- Rf1009NotificationInboxCharacterizationTests.cs: UNCHANGED

---

## Correction-04 Scope

**Fixes Applied:**

1. **Postpone receipt snapshot (F-C13-02):**
   - Moved receipt baseline capture from after replay to before replay (snapshot1)
   - Added receipt immutability assertions in snapshot2 after replay
   - Receipt scope: ActorUserId + ProjectId null + "SurveyPlanV2Postponed" + key

2. **Upload scoped effect-set (F-C13-01a):**
   - Added session count by OwnerUserId + Purpose
   - Added scope count by ProjectId + OwnerUserId
   - Added audit log count by EntityId + EventType
   - Added idempotency record count check

3. **Survey plan scoped effect-set (F-C13-01b):**
   - Added plan count by ProjectId + Status
   - Added scope count by SurveyPlanId
   - Kept existing receipt immutability assertions

**Unchanged from Correction-02/03:**
- F-C13-01c (role revocation authorization)
- F-C13-03 (consumer replay with candidate ID)
- F-C13-04 (MarkRead idempotency)

---

## Package Quality

✓ All assertion gaps closed  
✓ 15/15 tests pass  
✓ Source hashes verified  
✓ Evidence complete  
✓ Historical limitations documented  
✓ NOT_VERIFIED items clearly marked  
✓ BOX 2 reuse documented with hash verification

**Checkpoint 13 Status:** COMPLETE  
**RF-10 Status:** PARTIAL (checkpoint 13 complete, parent RF-10 continues)
