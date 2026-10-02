# Checkpoint 13 Findings Ledger - Final Status

**Package:** RF-10-checkpoint-13-finalization-handoff  
**Date:** 2026-10-02  
**Writer:** BOX 3  
**Status:** FINALIZATION

---

## Findings Summary

| Finding ID | Status | Coverage Source | Evidence Run | Scope | Open Issues |
|------------|--------|----------------|--------------|-------|-------------|
| F-C13-01a | FIXED | Correction-04 | BOX 1 C04 | Runtime + Receipt + Scoped counts | None |
| F-C13-01b | FIXED | Correction-04 | BOX 1 C04 | Runtime + Receipt + Scoped counts | None |
| F-C13-01c | FIXED | Correction-02 | BOX 1 C02 | Runtime | NOT_VERIFIED: original JWT |
| F-C13-02 | FIXED | Correction-04 | BOX 1 C04 | Runtime + Receipt + Snapshot timing | None |
| F-C13-03 | FIXED | Correction-03 | BOX 2 C03 | Runtime + Receipt | None |
| F-C13-04 | FIXED | Correction-03 | BOX 2 C03 | Runtime + Receipt + State chain | None |

---

## F-C13-01a: Upload Create Replay

**Original Gap:** Test counted by uploadId only; could not detect candidate entity with different ID.

**Status:** FIXED (correction-04)

**Test Coverage (Lines 88-165 after correction-04):**

**Receipt Immutability (correction-02, preserved):**
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint)
- OperationId immutability: `receipt.OperationId.Should().Be(firstOperationId)`
- OutcomeJson immutability: `receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint)`
- RequestFingerprint captured
- OutcomeJson contains uploadId and fileId

**Scoped Count Assertions (correction-04, added):**
- Session count by OwnerUserId + Purpose: `sessionCount.Should().Be(1)`
- Scope count by ProjectId + OwnerUserId: `scopeCount.Should().Be(1)`
- Audit log count by EntityId + EventType: `auditCount.Should().Be(1)`
- Idempotency record count: `idempotencyCount.Should().Be(1)`

**Scope of Scoped Assertions:**
Scoped count assertions verify that replay does not create duplicate entities with different IDs within the isolated test scenario (specific actor + project + purpose). They are bounded to the current test run's effect-set and do NOT assert full database immutability across all historical data.

**Evidence:**
- Runtime: BOX 1 correction-04 (10/10 PASS)
- TRX: RF-10-08-C01-correction-04-final.trx
- Receipt scope: ActorUserId + ProjectId + "UploadSessionCreated" + key
- Replay returns stored outcome without re-executing handler

**Open Issues:** None

---

## F-C13-01b: SurveyPlan Create Replay

**Original Gap:** Test counted by planId only; could not detect candidate entity with different ID.

**Status:** FIXED (correction-04)

**Test Coverage (Lines 206-275 after correction-04):**

**Receipt Immutability (correction-02, preserved):**
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint)
- OperationId immutability: `receipt.OperationId.Should().Be(firstOperationId)`
- OutcomeJson immutability: `receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint)`
- RequestFingerprint captured

**Scoped Count Assertions (correction-04, added):**
- Plan count by ProjectId + Status: `planCount.Should().Be(1)`
- Scope count by SurveyPlanId: `scopeCount.Should().Be(1)`
- Idempotency record count: `idempotencyCount.Should().Be(1)`

**Scope of Scoped Assertions:**
Scoped count assertions verify that replay does not create duplicate entities with different IDs within the isolated test scenario (specific project + status). They are bounded to the current test run's effect-set and do NOT assert full database immutability across all historical data.

**Evidence:**
- Runtime: BOX 1 correction-04 (10/10 PASS)
- TRX: RF-10-08-C01-correction-04-final.trx
- Receipt scope: ActorUserId + ProjectId + "SurveyPlanV2Created" + key
- Replay returns stored outcome without re-executing handler

**Open Issues:** None

---

## F-C13-01c: Role Revocation Replay Authorization

**Original Gap:** Test expected idempotency replay to bypass current authorization.

**Corrected Claim:** Test characterizes that replay authorization uses current JWT claims at replay time. Test obtains NEW JWT after role change (line 933: `await AuthenticateAsync(client, supervisor.UserName!)`), not using original JWT.

**Status:** FIXED (correction-02)

**Test Coverage (Lines 891-957 after correction-02):**
- First request as Supervisor: returns 201 Created
- Role change: Supervisor → ProjectManager via IdentityRepository
- Re-authenticate to obtain NEW JWT with ProjectManager claims
- Replay request with NEW JWT: returns 403 Forbidden
- Project persisted exactly once (first operation succeeded)
- IdempotencyRecord persisted with stored outcome

**Evidence:**
- Runtime: BOX 1 correction-02 (10/10 PASS)
- New JWT after role change returns 403 (replay enforces current authorization)
- Does NOT prove original JWT revocation or behavior with stale claims

**Open Issues:**
- NOT_VERIFIED: Original JWT behavior after role change (F-C13-01c-HISTORICAL)
- Test demonstrates NEW JWT enforcement, not original JWT invalidation
- Reviewer confirmed NOT_VERIFIED status acceptable for bounded characterization

---

## F-C13-02: SurveyPlan Postpone Replay

**Original Gap:** Receipt baseline captured AFTER replay instead of BEFORE replay. Cannot prove immutability without proper before/after comparison.

**Status:** FIXED (correction-04)

**Test Coverage (Lines 803-883 after correction-04):**

**Snapshot1 - BEFORE replay:**
- Postponement count: 0 (no postponement record persisted)
- Receipt baseline captured: firstOperationId, firstRequestFingerprint, firstOutcomeJson
- Receipt scope: ActorUserId + ProjectId null + "SurveyPlanV2Postponed" + key
- OutcomeJson contains status 7 (ConcurrencyConflict)
- RequestFingerprint captured

**Replay execution:**
- Stale version request with same idempotency key
- Returns 412 Precondition Failed

**Snapshot2 - AFTER replay:**
- Receipt immutability verified:
  - `receipt.OperationId.Should().Be(firstOperationId)`
  - `receipt.RequestFingerprint.Should().Be(firstRequestFingerprint)`
  - `receipt.OutcomeJson.Should().Be(firstOutcomeJson)`
- Postponement count: still 0 (no state mutation)
- Plan version: unchanged (version3 preserved)
- Single receipt persisted

**Evidence:**
- Runtime: BOX 1 correction-04 (10/10 PASS)
- TRX: RF-10-08-C01-correction-04-final.trx
- Receipt fingerprint comparison before/after proves replay skips handler re-invocation
- Zero postponements persisted confirms no state mutation

**Open Issues:** None

---

## F-C13-03: Consumer Replay with Candidate ID

**Original Gap:** Replay used same DbContext/consumer and same notification ID.

**Status:** FIXED (correction-03)

**Test Coverage (Lines 320-397 after correction-03):**
- First consume with notification.Id: returns Recorded
- Fresh DbContext (context2) and consumer (consumer2) for replay
- Different candidate notification ID (candidateNotificationId ≠ notification.Id)
- Replay returns stored notification ID, not candidate ID
- Notification count: exactly 1 (scoped effect-set snapshot)
- Candidate notification NOT persisted (AnyAsync returns false)
- Receipt unchanged: ID, EffectId link to original notification
- Single ConsumerEffectReceipt persisted

**Attribution Correction:**
Previous documentation incorrectly attributed F-C13-03 coverage to correction-02. Source review confirms fix was implemented in correction-03 with corresponding test evidence in RF-10-09-C01-correction-03-final.trx.

**Evidence:**
- Runtime: BOX 2 correction-03 (5/5 PASS)
- TRX: RF-10-09-C01-correction-03-final.trx
- Consumer effect scope: MessageId + ConsumerName
- Replay with different candidate ID does not persist candidate

**Open Issues:** None

---

## F-C13-04: MarkRead Idempotency

**Original Gap:** Snapshot timing insufficient to prove ReadAt/RowVersion immutability.

**Status:** FIXED (correction-03)

**Test Coverage (Lines 235-288 after correction-03):**

**Three-stage snapshot comparison:**
- **Baseline:** ReadAt = null
- **After first mark-read (snapshot1):** ReadAt set, RowVersion changed
- **After replay (snapshot2):** ReadAt unchanged, RowVersion unchanged

**Immutability Assertions:**
- ReadAt immutability: `afterReplay.ReadAt.Should().Be(afterFirstMark.ReadAt)`
- RowVersion immutability: `afterReplay.RowVersion.Should().Equal(afterFirstMark.RowVersion)`
- Receipt identity fields unchanged: Id, RecipientUserId, SourceEntityType, SourceEntityId, Body
- OperationId immutability: replay returns stored operation ID
- OutcomeJson immutability: replay returns stored outcome
- Single IdempotencyRecord persisted

**State Chain Characterization:**
Test proves that after first mark-read creates state (ReadAt + RowVersion mutation), subsequent replays preserve that state without further mutation. This characterizes the replay behavior after conflict resolution in the state chain: replay → conflict check → skip mutation when receipt exists.

**Attribution Correction:**
Previous documentation incorrectly attributed F-C13-04 coverage to correction-02. Source review confirms fix was implemented in correction-03 with corresponding test evidence in RF-10-09-C01-correction-03-final.trx.

**Evidence:**
- Runtime: BOX 2 correction-03 (5/5 PASS)
- TRX: RF-10-09-C01-correction-03-final.trx
- Receipt scope: ActorUserId + "NotificationRead" + key
- Replay does not mutate ReadAt or RowVersion after initial state established

**Open Issues:** None

---

## Historical Limitations

**NOT_VERIFIED (permanent for bounded characterization):**

1. **F-C13-01c-HISTORICAL: Original JWT behavior after role change**
   - Test obtains NEW JWT after role change (line 933)
   - Does not characterize original JWT validity, revocation, or stale claim behavior
   - Reviewer confirmed NOT_VERIFIED status acceptable

2. **Survey checkpoint 08 inspection provenance**
   - Not addressed in checkpoint 13 characterization scope

3. **Dispatcher/delivery workflow**
   - Not yet implemented in production code

4. **Before-build provenance for correction-02**
   - Correction-02 package used placeholder hashes
   - Historical limitation preserved; not retroactively fixed

5. **Before-build-to-test linkage for correction-04**
   - Before-build hashes captured from working directory state
   - Test execution and TRX generation occurred from after-build state
   - Direct linkage between before-build snapshot and test run not confirmed
   - Source payload in archive matches after-build hash exactly

---

## Evidence Source Mapping

**BOX 1 (IdempotencyPerCommandCharacterizationTests):**
- Correction-02: F-C13-01a (receipt only), F-C13-01b (receipt only), F-C13-01c
- Correction-04: F-C13-01a (receipt + scoped counts), F-C13-01b (receipt + scoped counts), F-C13-02

**BOX 2 (Rf1009NotificationInboxCharacterizationTests):**
- Correction-03: F-C13-03, F-C13-04

**Test Evidence Files:**
- RF-10-08-C01-correction-02-final.trx (historical, not in finalization package)
- RF-10-08-C01-correction-04-final.trx (BOX 1 current, 10/10 PASS)
- RF-10-09-C01-correction-03-final.trx (BOX 2 current, 5/5 PASS)

---

## Checkpoint Status

**Checkpoint 13:** COMPLETE (all findings closed within bounded scope)  
**RF-10 Parent:** PARTIAL (checkpoint 13 complete, parent RF-10 continues)

**Findings Closed:** 6/6  
**NOT_VERIFIED Items:** 5 (documented as historical limitations)  
**Pass Rate:** 100% (BOX 1: 10/10, BOX 2: 5/5)
