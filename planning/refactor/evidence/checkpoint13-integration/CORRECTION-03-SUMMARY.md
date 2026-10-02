# Checkpoint 13 Correction-03 Summary

**Package:** RF-10-checkpoint-13-correction-03-handoff  
**Date:** 2026-10-01  
**Writer:** BOX 3  
**Purpose:** Close assertion gaps identified in source supplement

---

## Test Results: 15/15 PASS

**RF-10-08-C01** (IdempotencyPerCommandCharacterizationTests):
- Total: 10 tests
- Passed: 10
- Failed: 0
- Duration: 5s

**RF-10-09-C01** (Rf1009NotificationInboxCharacterizationTests):
- Total: 5 tests
- Passed: 5
- Failed: 0
- Duration: 2s

**Build:** 180 warnings / 0 errors  
**Test Run:** 2026-10-01 (correction-03)

---

## Assertion Gap Closed

### Postpone Receipt Scope (F-C13-02)

**Gap:** Test compared instance IDs only, did not prove replay skipped re-evaluation.

**Fix Applied:**
```csharp
// Lines 833-860: Added receipt scope verification with operation fingerprint comparison
Guid firstOperationId;
string firstOutcomeJson;
await using (var snapshot1 = _sql.CreateDbContext())
{
    var receipt = await snapshot1.IdempotencyRecords.AsNoTracking().SingleAsync(
        r => r.ActorUserId == manager.Id &&
             r.ProjectId == null &&
             r.Operation == "SurveyPlanV2Postponed" &&
             r.IdempotencyKey == idempotencyKey);
    firstOperationId = receipt.OperationId;
    firstOutcomeJson = receipt.OutcomeJson;
    receipt.RequestFingerprint.Should().NotBeNullOrWhiteSpace();
    receipt.OutcomeJson.Should().Contain("\"Status\":7", "stored outcome contains ConcurrencyConflict status");
}

// After replay:
receipt.OperationId.Should().Be(firstOperationId, "replay returns stored operation ID");
receipt.OutcomeJson.Should().Be(firstOutcomeJson, "replay returns stored outcome without re-executing handler");
```

**Evidence Now Covers:**
- Receipt scope: ActorUserId + ProjectId null + "SurveyPlanV2Postponed" + key
- OutcomeJson immutability (stored outcome contains status code 7)
- OperationId immutability (replay returns stored operation ID)
- RequestFingerprint captured
- Single receipt persisted (no duplicates)

---

## Gaps Already Covered (No Changes Required)

### 1. Consumer Replay with Candidate ID (F-C13-03)

**Status:** ALREADY COVERED in correction-02

Test lines 320-397 already implement:
- Fresh DbContext (context2) and consumer (consumer2) for replay
- Different candidate notification ID (candidateNotificationId ≠ notification.Id)
- Assertions verify replay returns stored ID, not candidate ID
- Candidate notification NOT persisted to database (AnyAsync returns false)
- Single notification persisted (count check)
- Receipt unchanged and links to original notification

**No edit required.**

---

### 2. MarkRead Idempotency (F-C13-04)

**Status:** ALREADY COVERED in correction-02

Test lines 235-288 already implement:
- Three-stage snapshot: baseline → afterFirstMark (snapshot1) → afterReplay (snapshot2)
- ReadAt immutability: `afterReplay.ReadAt.Should().Be(afterFirstMark.ReadAt)`
- RowVersion immutability: `afterReplay.RowVersion.Should().Equal(afterFirstMark.RowVersion)`
- Receipt identity fields unchanged (Id, RecipientUserId, SourceEntityType, SourceEntityId, Body)
- OperationId immutability
- OutcomeJson immutability
- Single IdempotencyRecord persisted

**No edit required.**

---

### 3. Upload Create Replay (F-C13-01a)

**Status:** ALREADY COVERED in correction-02

Test lines 88-157 already implement:
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint from snapshot1)
- OperationId immutability: `receipt.OperationId.Should().Be(firstOperationId)`
- OutcomeJson immutability: `receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint)`
- RequestFingerprint captured
- OutcomeJson contains uploadId and fileId
- Session, scope, audit log counts remain 1 (scoped effect-set snapshots)

**No edit required.**

---

### 4. SurveyPlan Create Replay (F-C13-01b)

**Status:** ALREADY COVERED in correction-02

Test lines 206-262 already implement:
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint from snapshot1)
- OperationId immutability: `receipt.OperationId.Should().Be(firstOperationId)`
- OutcomeJson immutability: `receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint)`
- RequestFingerprint captured
- Plan and scope counts remain 1 (scoped effect-set snapshots)

**No edit required.**

---

## Source Provenance

**Before-build hashes** (correction-03-tests-before.sha256):
```
cd29ebd5aae062273ba69f5ac8f165ec169b6e29969bdbe5eac8f07051c9d069 *IdempotencyPerCommandCharacterizationTests.cs
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**After-build hashes** (correction-03-tests-after.sha256):
```
bda07010c41dba46004da2bf0994a577c7f2d3114458e57e083050f4b264537b *IdempotencyPerCommandCharacterizationTests.cs
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**Change:** IdempotencyPerCommandCharacterizationTests.cs modified (postpone test fix)  
**Unchanged:** Rf1009NotificationInboxCharacterizationTests.cs (no changes needed)

**Build log:** correction-03-build.log (180 warnings / 0 errors)  
**Test logs:** correction-03-box1-run.log, correction-03-box2-run.log

---

## Historical Limitations Preserved

**NOT_VERIFIED in this correction:**
- Original JWT behavior after role change (F-C13-01c-HISTORICAL)
- Survey checkpoint 08 inspection provenance
- Dispatcher/delivery workflow (not yet implemented)
- Before-build provenance for correction-02 (placeholder hashes)

**Reviewer confirmed NOT_VERIFIED for original JWT** - not a blocker for this bounded characterization.

---

## Findings Status After Correction-03

| Finding | Status | Coverage After C03 | Open Issues |
|---------|--------|-------------------|-------------|
| F-C13-01a | VERIFIED | Upload OperationId + OutcomeJson immutability | None |
| F-C13-01b | VERIFIED | SurveyPlan OperationId + OutcomeJson immutability | None |
| F-C13-01c | FIXED | Authorization with new JWT | NOT_VERIFIED: original JWT |
| F-C13-02 | FIXED | Receipt scope + OperationId + OutcomeJson immutability | None |
| F-C13-03 | VERIFIED | Consumer replay with candidate ID + fresh DbContext | None |
| F-C13-04 | VERIFIED | MarkRead ReadAt/RowVersion + receipt identity immutability | None |

---

## Package Readiness

✓ Assertion gap closed (postpone test)  
✓ 15/15 tests pass  
✓ Source hashes captured before/after  
✓ Build log + test logs captured  
✓ TRX files generated  
✓ Historical limitations documented  
✓ NOT_VERIFIED items clearly marked

**Next:** Package full handoff with source + evidence + reports + manifest.
