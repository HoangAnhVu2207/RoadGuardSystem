# Checkpoint 13 Correction-04 Summary

**Package:** RF-10-checkpoint-13-correction-04-handoff  
**Date:** 2026-10-01  
**Writer:** BOX 3  
**Purpose:** Close assertion gaps: postpone receipt snapshot timing, upload/survey scoped effect-sets

---

## Test Results: 15/15 PASS

**RF-10-08-C01** (IdempotencyPerCommandCharacterizationTests):
- Total: 10 tests
- Passed: 10
- Failed: 0
- Duration: 5s
- Evidence: RF-10-08-C01-correction-04-final.trx

**RF-10-09-C01** (Rf1009NotificationInboxCharacterizationTests):
- Total: 5 tests
- Passed: 5
- Failed: 0
- Duration: 2s
- Evidence: RF-10-09-C01-correction-03-final.trx (REUSED - source unchanged)

**Build:** 180 warnings / 0 errors  
**Test Run:** 2026-10-01 (correction-04)

---

## Assertion Gaps Closed

### 1. Postpone Receipt Snapshot Timing (F-C13-02)

**Gap:** Receipt baseline captured AFTER replay instead of BEFORE replay. Cannot prove immutability by comparing two snapshots both taken after replay.

**Fix Applied (Lines 803-826, 860-883):**

**Snapshot1 - BEFORE replay:**
```csharp
Guid firstOperationId;
string firstRequestFingerprint;
string firstOutcomeJson;
await using (var snapshot1 = _sql.CreateDbContext())
{
    var postponementCount = await snapshot1.SurveyPlanPostponements.AsNoTracking()
        .CountAsync(p => p.SurveyPlanId == planId);
    postponementCount.Should().Be(0, "first postpone was direct mutation");

    var receipt = await snapshot1.IdempotencyRecords.AsNoTracking().SingleAsync(
        r => r.ActorUserId == manager.Id &&
             r.ProjectId == null &&
             r.Operation == "SurveyPlanV2Postponed" &&
             r.IdempotencyKey == idempotencyKey);
    receipt.OutcomeJson.Should().Contain("\"Status\":7", "status 7 = ConcurrencyConflict");
    firstOperationId = receipt.OperationId;
    firstRequestFingerprint = receipt.RequestFingerprint;
    firstOutcomeJson = receipt.OutcomeJson;
}
```

**Snapshot2 - AFTER replay:**
```csharp
var receipt = await snapshot2.IdempotencyRecords.AsNoTracking().SingleAsync(
    r => r.ActorUserId == manager.Id &&
         r.ProjectId == null &&
         r.Operation == "SurveyPlanV2Postponed" &&
         r.IdempotencyKey == idempotencyKey);
receipt.OperationId.Should().Be(firstOperationId);
receipt.RequestFingerprint.Should().Be(firstRequestFingerprint);
receipt.OutcomeJson.Should().Be(firstOutcomeJson);
```

**Evidence Now Covers:**
- Receipt scope: ActorUserId + ProjectId null + "SurveyPlanV2Postponed" + key
- Baseline captured before replay (snapshot1)
- Immutability verified after replay (snapshot2)
- OperationId, RequestFingerprint, OutcomeJson all preserved
- Single receipt persisted (no duplicates)

---

### 2. Upload Scoped Effect-Set (F-C13-01a)

**Gap:** Test counted by uploadId only; could not detect candidate with different ID.

**Fix Applied (Lines 131-165):**
```csharp
// Scoped effect-set count checks
var sessionCount = await snapshot2.UploadSessions.AsNoTracking()
    .CountAsync(s => s.OwnerUserId == manager.Id && s.Purpose == UploadPurpose.RoadInspectionData);
sessionCount.Should().Be(1, "replay must not create duplicate session - scoped count check");

var session = await snapshot2.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == uploadId);
session.FileId.Should().Be(fileId, "replay returns stored uploadId and fileId");

var scopeCount = await snapshot2.FileScopes.AsNoTracking()
    .CountAsync(sc => sc.ProjectId == projectId && sc.OwnerUserId == manager.Id);
scopeCount.Should().Be(1, "replay must not create duplicate scope - scoped count check");

var auditCount = await snapshot2.AuditLogs.AsNoTracking()
    .CountAsync(a => a.EntityId == uploadId.ToString() && a.EventType == "UploadSessionCreated");
auditCount.Should().Be(1, "replay must not create duplicate audit log - scoped count check");

var idempotencyCount = await snapshot2.IdempotencyRecords.AsNoTracking()
    .CountAsync(r => r.ActorUserId == manager.Id && 
                     r.ProjectId == projectId && 
                     r.Operation == "UploadSessionCreated" && 
                     r.IdempotencyKey == idempotencyKey);
idempotencyCount.Should().Be(1, "exactly one idempotency record persisted");
```

**Evidence Now Covers:**
- Session count by OwnerUserId + Purpose (scoped to actor + scenario)
- Scope count by ProjectId + OwnerUserId (scoped to project + actor)
- Audit log count by EntityId + EventType (scoped to specific entity + event)
- Idempotency record count verification
- Existing receipt immutability assertions preserved

---

### 3. Survey Plan Scoped Effect-Set (F-C13-01b)

**Gap:** Test counted by planId only; could not detect candidate with different ID.

**Fix Applied (Lines 245-275):**
```csharp
// Scoped effect-set count checks
var planCount = await snapshot2.SurveyPlans.AsNoTracking()
    .CountAsync(p => p.ProjectId == projectId && p.Status == SurveyPlanStatus.Planned);
planCount.Should().Be(1, "replay must not create duplicate plan - scoped count check");

var plan = await snapshot2.SurveyPlans.AsNoTracking().SingleAsync(p => p.Id == planId);
plan.ProjectId.Should().Be(projectId, "replay returns stored planId");

var scopeCount = await snapshot2.SurveyPlanScopes.AsNoTracking()
    .CountAsync(sc => sc.SurveyPlanId == planId);
scopeCount.Should().Be(1, "replay must not create duplicate scope - scoped count check");

var idempotencyCount = await snapshot2.IdempotencyRecords.AsNoTracking()
    .CountAsync(r => r.ActorUserId == manager.Id && 
                     r.ProjectId == projectId && 
                     r.Operation == "SurveyPlanV2Created" && 
                     r.IdempotencyKey == idempotencyKey);
idempotencyCount.Should().Be(1, "exactly one idempotency record persisted");
```

**Evidence Now Covers:**
- Plan count by ProjectId + Status (scoped to project + state)
- Scope count by SurveyPlanId (scoped to specific plan)
- Idempotency record count verification
- Existing receipt immutability assertions preserved

---

## Gaps Already Covered (No Changes Required)

### Consumer Replay with Candidate ID (F-C13-03)

**Status:** VERIFIED in correction-02, unchanged in correction-04

Test lines 320-397 already implement:
- Fresh DbContext (context2) and consumer (consumer2) for replay
- Different candidate notification ID (candidateNotificationId ≠ notification.Id)
- Assertions verify replay returns stored ID, not candidate ID
- Candidate notification NOT persisted to database
- Single notification persisted (count check)
- Receipt unchanged and links to original notification

**No edit required.**

---

### MarkRead Idempotency (F-C13-04)

**Status:** VERIFIED in correction-02, unchanged in correction-04

Test lines 235-288 already implement:
- Three-stage snapshot: baseline → afterFirstMark → afterReplay
- ReadAt immutability: `afterReplay.ReadAt.Should().Be(afterFirstMark.ReadAt)`
- RowVersion immutability: `afterReplay.RowVersion.Should().Equal(afterFirstMark.RowVersion)`
- Receipt identity fields unchanged
- OperationId and OutcomeJson immutability
- Single IdempotencyRecord persisted

**No edit required.**

---

### Upload/Survey Create Replay Receipts (F-C13-01a/01b)

**Status:** VERIFIED in correction-02, enhanced in correction-04

Correction-02 already implemented:
- Operation fingerprint verification (firstOperationId, firstOutcomeFingerprint)
- OperationId immutability: `receipt.OperationId.Should().Be(firstOperationId)`
- OutcomeJson immutability: `receipt.OutcomeJson.Should().Be(firstOutcomeFingerprint)`
- RequestFingerprint captured

Correction-04 added:
- Scoped effect-set count checks to detect duplicate entities with different IDs
- Receipt assertions remain intact alongside new effect-set checks

---

## Source Provenance

**Before-build hashes** (correction-04-tests-before.sha256):
```
6f0bebf1bb2ad34abe8efce2b511556fdd4e901b118bc5e055fb57f2023aceb1 *IdempotencyPerCommandCharacterizationTests.cs
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd39<bE986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**After-build hashes** (correction-04-tests-after.sha256):
```
a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f *IdempotencyPerCommandCharacterizationTests.cs
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**Changes:**
- IdempotencyPerCommandCharacterizationTests.cs modified (3 edits)
- Rf1009NotificationInboxCharacterizationTests.cs unchanged

**Build log:** correction-04-build.log (180 warnings / 0 errors)  
**Test logs:** correction-04-box1-run.log (BOX 1), correction-04-box2-reuse-note.txt (BOX 2)

---

## Historical Limitations Preserved

**NOT_VERIFIED in this correction:**
- Original JWT behavior after role change (F-C13-01c-HISTORICAL)
- Survey checkpoint 08 inspection provenance
- Dispatcher/delivery workflow (not yet implemented)
- Before-build provenance for correction-02 (placeholder hashes)

**Reviewer confirmed NOT_VERIFIED for original JWT** - not a blocker for this bounded characterization.

---

## Findings Status After Correction-04

| Finding | Status | Coverage After C04 | Open Issues |
|---------|--------|-------------------|-------------|
| F-C13-01a | FIXED | Upload OperationId + OutcomeJson + scoped effect-set | None |
| F-C13-01b | FIXED | SurveyPlan OperationId + OutcomeJson + scoped effect-set | None |
| F-C13-01c | VERIFIED | Authorization with new JWT | NOT_VERIFIED: original JWT |
| F-C13-02 | FIXED | Receipt snapshot timing + OperationId + OutcomeJson immutability | None |
| F-C13-03 | VERIFIED | Consumer replay with candidate ID + fresh DbContext | None |
| F-C13-04 | VERIFIED | MarkRead ReadAt/RowVersion + receipt identity immutability | None |

---

## Package Readiness

✓ Assertion gaps closed (postpone snapshot timing, upload/survey effect-sets)  
✓ 15/15 tests pass  
✓ Source hashes captured before/after  
✓ Build log + test logs captured  
✓ TRX files generated (BOX 1 new, BOX 2 reused)  
✓ BOX 2 reuse documented with hash verification  
✓ Historical limitations documented  
✓ NOT_VERIFIED items clearly marked  
✓ Manifest generated from actual file bytes  
✓ Payload inventory with sizes and SHA-256

**Next:** Archive package and generate external SHA-256 for handoff.
