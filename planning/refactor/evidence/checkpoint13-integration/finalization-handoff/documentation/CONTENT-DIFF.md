# Checkpoint 13 Correction Content Diff

**Package:** RF-10-checkpoint-13-finalization-handoff  
**Date:** 2026-10-02  
**Writer:** BOX 3

---

## Correction-04: BOX 1 Changes

### File: IdempotencyPerCommandCharacterizationTests.cs

**Hash Change:**
- Before: 6f0bebf1bb2ad34abe8efce2b511556fdd4e901b118bc5e055fb57f2023aceb1
- After: a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f

---

### Change 1: Postpone Receipt Snapshot Timing - Snapshot1 Before Replay

**Location:** Lines 803-826

**Purpose:** Move receipt baseline capture from after replay to before replay

**Before (correction-03):**
Receipt baseline captured in snapshot after replay SendAsync, comparing two snapshots both taken after replay.

**After (correction-04):**
```csharp
// Snapshot receipt BEFORE replay to establish baseline
Guid firstOperationId;
string firstRequestFingerprint;
string firstOutcomeJson;
await using (var snapshot1 = _sql.CreateDbContext())
{
    var postponementCount = await snapshot1.SurveyPlanPostponements.AsNoTracking()
        .CountAsync(p => p.SurveyPlanId == planId);
    postponementCount.Should().Be(0, "first postpone was direct mutation, not through API, so no postponement record exists yet");

    var receipt = await snapshot1.IdempotencyRecords.AsNoTracking().SingleAsync(
        r => r.ActorUserId == manager.Id &&
             r.ProjectId == null &&
             r.Operation == "SurveyPlanV2Postponed" &&
             r.IdempotencyKey == idempotencyKey);
    receipt.OutcomeJson.Should().Contain("\"Status\":7", "status 7 = ConcurrencyConflict");
    firstOperationId = receipt.OperationId;
    firstRequestFingerprint = receipt.RequestFingerprint;
    firstOutcomeJson = receipt.OutcomeJson;
    firstRequestFingerprint.Should().NotBeNullOrWhiteSpace("receipt captures request fingerprint");
}
```

**Rationale:** Proper before/after comparison requires baseline captured before the operation being tested (replay).

---

### Change 2: Postpone Receipt Immutability Verification - Snapshot2 After Replay

**Location:** Lines 860-883

**Purpose:** Verify receipt immutability by comparing snapshot2 after replay against snapshot1 baseline

**Before (correction-03):**
Only instance ID comparison without operation fingerprint verification.

**After (correction-04):**
```csharp
// Verify receipt immutability after replay
await using (var snapshot2 = _sql.CreateDbContext())
{
    var receipt = await snapshot2.IdempotencyRecords.AsNoTracking().SingleAsync(
        r => r.ActorUserId == manager.Id &&
             r.ProjectId == null &&
             r.Operation == "SurveyPlanV2Postponed" &&
             r.IdempotencyKey == idempotencyKey);
    receipt.OperationId.Should().Be(firstOperationId, "replay returns stored operation ID");
    receipt.RequestFingerprint.Should().Be(firstRequestFingerprint, "replay preserves request fingerprint");
    receipt.OutcomeJson.Should().Be(firstOutcomeJson, "replay returns stored outcome without re-executing handler");
    receipt.OutcomeJson.Should().Contain("\"Status\":7", "stored outcome contains ConcurrencyConflict status");
}
```

**Rationale:** Immutability assertions prove replay returns stored receipt without modification.

---

### Change 3: Upload Scoped Effect-Set Count Checks

**Location:** Lines 131-165

**Purpose:** Add scoped count assertions to detect duplicate entities with different IDs

**Before (correction-03):**
```csharp
var sessionCount = await snapshot2.UploadSessions.AsNoTracking().CountAsync(s => s.Id == uploadId);
sessionCount.Should().Be(1, "replay must not create duplicate session");
```

**After (correction-04):**
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

**Rationale:** Scoped queries by actor + project + purpose can detect if replay creates new entity with different ID. Count by uploadId alone cannot detect this.

**Scope Note:** These assertions verify counts within the isolated test scenario (specific manager.Id, projectId, Purpose). They do NOT assert full database immutability across all historical data.

---

### Change 4: Survey Plan Scoped Effect-Set Count Checks

**Location:** Lines 245-275

**Purpose:** Add scoped count assertions to detect duplicate entities with different IDs

**Before (correction-03):**
```csharp
var planCount = await snapshot2.SurveyPlans.AsNoTracking().CountAsync(p => p.Id == planId);
planCount.Should().Be(1, "replay must not create duplicate plan");
```

**After (correction-04):**
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

**Rationale:** Scoped queries by projectId + status can detect if replay creates new entity with different ID. Count by planId alone cannot detect this.

**Scope Note:** These assertions verify counts within the isolated test scenario (specific projectId, status Planned). They do NOT assert full database immutability across all historical data.

---

## Correction-03: BOX 2 Changes

### File: Rf1009NotificationInboxCharacterizationTests.cs

**Hash:** f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e (unchanged in correction-04)

**Changes Applied in Correction-03:**

### F-C13-03: Consumer Replay with Candidate ID

**Location:** Lines 320-397

**Changes:**
- Added fresh DbContext (context2) and consumer (consumer2) for replay
- Used different candidate notification ID (candidateNotificationId ≠ notification.Id)
- Added assertions: replay returns stored ID, candidate NOT persisted
- Verified receipt links to original notification

### F-C13-04: MarkRead Three-Stage Snapshot

**Location:** Lines 235-288

**Changes:**
- Added baseline snapshot (ReadAt = null)
- Added snapshot1 after first mark-read (ReadAt set, RowVersion changed)
- Added snapshot2 after replay (ReadAt/RowVersion unchanged)
- Added immutability assertions for ReadAt, RowVersion, receipt fields

**Note:** BOX 2 source unchanged in correction-04. Evidence from correction-03 reused.

---

## Correction-02: Historical Changes (Superseded)

### BOX 1 Changes in Correction-02:

**F-C13-01a/01b:** Added receipt immutability assertions (OperationId, OutcomeJson)
**F-C13-01c:** Role revocation test with new JWT

**Status:** Superseded by correction-04 for BOX 1 (which preserves correction-02 receipt assertions and adds scoped counts)

### BOX 2 Changes in Correction-02:

**None** - BOX 2 changes implemented in correction-03, not correction-02

**Attribution Correction:** Previous documentation incorrectly attributed F-C13-03 and F-C13-04 to correction-02. Source review confirms these fixes were in correction-03.

---

## Summary

**Correction-04 Scope:**
- BOX 1 only: 4 changes addressing 3 findings (F-C13-01a, F-C13-01b, F-C13-02)
- BOX 2: No changes (source unchanged, evidence reused from correction-03)

**Total Findings Addressed:**
- Correction-02: F-C13-01a (partial), F-C13-01b (partial), F-C13-01c
- Correction-03: F-C13-03, F-C13-04
- Correction-04: F-C13-01a (complete), F-C13-01b (complete), F-C13-02

**Final Status:** All 6 findings closed within bounded scope, with documented historical limitations.
