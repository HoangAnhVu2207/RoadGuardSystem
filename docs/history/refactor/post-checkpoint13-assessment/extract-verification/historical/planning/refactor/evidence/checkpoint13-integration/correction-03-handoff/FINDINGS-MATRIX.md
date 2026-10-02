# Checkpoint 13 Correction-03 Findings Matrix

**Package:** RF-10-checkpoint-13-correction-03-handoff  
**Date:** 2026-10-01  
**Writer:** BOX 3  
**Status:** COMPLETE

---

## Findings Status Summary

| Finding ID | Status | Test Coverage | Evidence Type | Open Issues |
|------------|--------|---------------|---------------|-------------|
| F-C13-01a | VERIFIED | Upload OperationId + OutcomeJson immutability | Runtime + Receipt | None |
| F-C13-01b | VERIFIED | SurveyPlan OperationId + OutcomeJson immutability | Runtime + Receipt | None |
| F-C13-01c | FIXED | Authorization with new JWT returns 403 | Runtime | NOT_VERIFIED: original JWT |
| F-C13-02 | FIXED | Postpone OperationId + OutcomeJson immutability | Runtime + Receipt | None |
| F-C13-03 | VERIFIED | Consumer replay with candidate ID + fresh DbContext | Runtime + Receipt | None |
| F-C13-04 | VERIFIED | MarkRead ReadAt/RowVersion + receipt immutability | Runtime + Receipt | None |

---

## F-C13-01a: Upload Create Replay

**Original Gap:** Count by uploadId insufficient to detect candidate with different ID.

**Status:** VERIFIED (correction-02)

**Test Coverage (Lines 88-157):**
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint)
- OperationId immutability: stored vs replay comparison
- OutcomeJson immutability: contains uploadId and fileId, exact match
- RequestFingerprint captured
- Scoped effect-set snapshots: session count, scope count, audit log count remain 1
- Single IdempotencyRecord persisted

**Evidence:**
- Runtime: 10/10 PASS (RF-10-08-C01)
- Receipt scope: ActorUserId + ProjectId + "UploadSessionCreated" + key
- Replay returns stored outcome without re-executing handler

**Open Issues:** None

---

## F-C13-01b: SurveyPlan Create Replay

**Original Gap:** Count by planId insufficient to detect candidate with different ID.

**Status:** VERIFIED (correction-02)

**Test Coverage (Lines 206-262):**
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint)
- OperationId immutability: stored vs replay comparison
- OutcomeJson immutability: exact match
- RequestFingerprint captured
- Scoped effect-set snapshots: plan count, scope count remain 1
- Single IdempotencyRecord persisted

**Evidence:**
- Runtime: 10/10 PASS (RF-10-08-C01)
- Receipt scope: ActorUserId + ProjectId + "SurveyPlanV2Created" + key
- Replay returns stored outcome without re-executing handler

**Open Issues:** None

---

## F-C13-01c: Role Revocation Replay Authorization

**Original Gap:** Test expected idempotency replay to bypass current authorization.

**Corrected Claim:** Test characterizes that replay authorization uses current JWT claims at replay time. Test obtains NEW JWT after role change (line 933: `await AuthenticateAsync(client, supervisor.UserName!)`), not using original JWT.

**Status:** FIXED (correction-02)

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

**Original Gap:** Test did not prove whether replay re-evaluated version check or returned stored result without re-checking.

**Status:** FIXED (correction-03)

**Test Coverage (Lines 833-883):**
- Stale version request: returns 412, stores idempotency record
- Snapshot after first request: capture firstOperationId, firstOutcomeJson
- Replay with same payload: returns 412
- OperationId immutability: replay returns stored operation ID
- OutcomeJson immutability: replay returns stored outcome without re-executing handler
- Receipt scope: ActorUserId + ProjectId null + "SurveyPlanV2Postponed" + key
- OutcomeJson contains status code 7 (ConcurrencyConflict)
- Single IdempotencyRecord persisted

**Evidence:**
- Runtime: 10/10 PASS (RF-10-08-C01)
- Receipt fingerprint comparison proves replay skips handler re-invocation
- Zero postponements persisted (no state mutation)
- Plan version unchanged (version3 preserved)

**Open Issues:** None

---

## F-C13-03: Consumer Replay with Candidate ID

**Original Gap:** Replay used same DbContext/consumer and same notification ID.

**Status:** VERIFIED (correction-02)

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
- Runtime: 5/5 PASS (RF-10-09-C01)
- Consumer effect scope: MessageId + ConsumerName
- Replay with different candidate ID does not persist candidate

**Open Issues:** None

---

## F-C13-04: MarkRead Idempotency

**Original Gap:** Snapshot timing insufficient to prove ReadAt/RowVersion immutability.

**Status:** VERIFIED (correction-02)

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
- Runtime: 5/5 PASS (RF-10-09-C01)
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

**BOX 2 (RF-10-09-C01):** 5/5 PASS
- Rf1009NotificationInboxCharacterizationTests
- Duration: 2s

---

## Source Provenance

**Before-build hashes:**
```
cd29ebd5aae062273ba69f5ac8f165ec169b6e29969bdbe5eac8f07051c9d069 *IdempotencyPerCommandCharacterizationTests.cs
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**After-build hashes:**
```
bda07010c41dba46004da2bf0994a577c7f2d3114458e57e083050f4b264537b *IdempotencyPerCommandCharacterizationTests.cs
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**Changed:** IdempotencyPerCommandCharacterizationTests.cs (postpone test fix)  
**Unchanged:** Rf1009NotificationInboxCharacterizationTests.cs

---

## Package Quality

✓ All assertion gaps closed  
✓ 15/15 tests pass  
✓ Source hashes verified  
✓ Evidence complete  
✓ Historical limitations documented  
✓ NOT_VERIFIED items clearly marked

**Checkpoint 13 Status:** COMPLETE  
**RF-10 Status:** PARTIAL (checkpoint 13 complete, parent RF-10 continues)
