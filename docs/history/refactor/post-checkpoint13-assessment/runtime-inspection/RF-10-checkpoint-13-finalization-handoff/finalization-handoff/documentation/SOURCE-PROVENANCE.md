# Checkpoint 13 Source Provenance Report

**Package:** RF-10-checkpoint-13-finalization-handoff  
**Date:** 2026-10-02  
**Writer:** BOX 3

---

## BOX 1: IdempotencyPerCommandCharacterizationTests

### Correction-04 Source Chain

**Before-build state (correction-04-tests-before.sha256):**
```
6f0bebf1bb2ad34abe8efce2b511556fdd4e901b118bc5e055fb57f2023aceb1 *IdempotencyPerCommandCharacterizationTests.cs
```

**Changes Applied:**
1. Lines 803-826: Postpone test - moved receipt baseline to snapshot1 BEFORE replay
2. Lines 860-883: Postpone test - added receipt immutability assertions in snapshot2
3. Lines 131-165: Upload test - added scoped effect-set count checks
4. Lines 245-275: Survey plan test - added scoped effect-set count checks

**After-build state (correction-04-tests-after.sha256):**
```
a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f *IdempotencyPerCommandCharacterizationTests.cs
```

**Build Output:**
- Status: SUCCESS
- Warnings: 180
- Errors: 0
- Log: correction-04-build.log (172,538 bytes)

**Test Execution:**
- Command: `dotnet test --filter TaskId~RF-10-08-C01 --logger "trx;LogFileName=RF-10-08-C01-correction-04-final.trx"`
- Result: 10/10 PASS
- Duration: 5s
- TRX: RF-10-08-C01-correction-04-final.trx (321,428 bytes)
- Log: correction-04-box1-run.log

**Archive Payload Hash:**
```
a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f *tests/IdempotencyPerCommandCharacterizationTests.cs
```

**Provenance Status:**
- ✓ Before-build hash captured from working directory
- ✓ After-build hash captured after build
- ✓ Archive payload matches after-build hash exactly
- ⚠ Before-build-to-test linkage NOT CONFIRMED
  - Before-build snapshot taken from working directory state
  - Test execution occurred from after-build state
  - Direct verification that test run used after-build source not established
  - Documented as historical limitation

---

## BOX 2: Rf1009NotificationInboxCharacterizationTests

### Correction-03 Source (Reused in Correction-04 and Finalization)

**Source Hash (unchanged across corrections):**
```
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs
```

**Hash Verification:**
- Correction-03 before-build: f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e
- Correction-03 after-build: f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e
- Correction-04 before-build: f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e
- Correction-04 after-build: f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e
- Finalization archive: f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e

**Test Evidence (from correction-03, reused):**
- TRX: RF-10-09-C01-correction-03-final.trx (51,483 bytes)
- Result: 5/5 PASS
- Duration: 2s

**Reuse Rationale:**
BOX 2 test file unchanged from correction-03. No BOX 2-specific fixes in correction-04. Evidence reused with hash verification confirming source identity.

**Provenance Status:**
- ✓ Source unchanged across corrections
- ✓ Hash verified consistent
- ✓ TRX from correction-03 run
- ✓ No rebuild/rerun required for finalization

---

## Correction-02 Historical Evidence

### Source Snapshots

**Note:** Correction-02 used placeholder hashes for before-build state. This historical limitation is preserved in finalization documentation.

**After-build state (correction-02-tests-after.sha256 - if captured):**
Historical evidence files from correction-02 not included in finalization package. Correction-04 supersedes correction-02 for BOX 1; correction-03 supersedes correction-02 for BOX 2.

---

## Correction-03 Historical Evidence

### BOX 2 Source (Current Evidence Base)

**Before-build state (correction-03-tests-before.sha256):**
```
cd29ebd5aae062273ba69f5ac8f165ec169b6e29969bdbe5eac8f07051c9d069 *IdempotencyPerCommandCharacterizationTests.cs (BOX 1 - superseded by C04)
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs (BOX 2 - current)
```

**After-build state (correction-03-tests-after.sha256):**
```
bda07010c41dba46004da2bf0994a577c7f2d3114458e57e083050f4b264537b *IdempotencyPerCommandCharacterizationTests.cs (BOX 1 - superseded by C04)
f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e *Rf1009NotificationInboxCharacterizationTests.cs (BOX 2 - current)
```

**BOX 2 Changes in Correction-03:**
BOX 2 test file unchanged between before-build and after-build in correction-03. All corrections applied to BOX 1 only in that cycle.

---

## Assembly Hashes

### Correction-04 Build Artifacts

**Test Assembly:**
Not captured in correction-04 evidence. Historical limitation documented.

**Future Improvement:**
For future corrections, capture assembly hashes post-build to establish complete build-to-test chain:
```
sha256sum tests/RoadGuardSystem.ApiTests/bin/Debug/net8.0/RoadGuardSystem.ApiTests.dll
```

---

## Provenance Summary

**BOX 1 (correction-04):**
- Before-build hash: 6f0bebf1bb2ad34abe8efce2b511556fdd4e901b118bc5e055fb57f2023aceb1
- After-build hash: a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f
- Archive payload hash: a7bbae78744e69ca028c4f6e727637ae3872c0144537b5f3c94619b9e5c3789f
- Status: Archive matches after-build exactly
- Limitation: Before-build-to-test linkage not confirmed

**BOX 2 (correction-03, reused):**
- Consistent hash: f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e
- Archive payload hash: f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e
- Status: Source unchanged, hash verified across corrections
- Evidence: TRX from correction-03 run

**Historical Limitations:**
- Correction-02: Before-build placeholder hashes (not retroactively fixed)
- Correction-04 BOX 1: Before-build-to-test linkage not established
- Assembly hashes: Not captured in correction-04

**Quality Assessment:**
Source payload in finalization archive is verified and traceable. Historical limitations documented transparently for reviewer evaluation of evidence chain completeness.
