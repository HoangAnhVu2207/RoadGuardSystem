# Checkpoint 13 Integration - Phase B Readiness

**Date:** 2026-10-01  
**Status:** READY TO PROCEED - Both boxes signaled

## BOX Status Verification

### BOX 1: ✅ READY
- **Signal:** `planning/refactor/evidence/rf1008-c01/BOX1-READY.md` exists
- **Task:** RF-10-08-C01 (Per-command replay/idempotency characterization)
- **Test file:** `tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs`
- **Test hash:** `f6dc771fa337a86a7d2bf4f259d9bf1ff8430522a9057b68ad834bdced8e8cce`
- **Test count:** 11 methods
- **Expected:** 11/11 PASS
- **Key findings:**
  - F-RF-10-08-C01-01: Upload Complete pattern differs (rollback, no record)
  - F-RF-10-08-C01-02: Three patterns (A: store failure, B: check before idempotency, C: throw exception)
- **Gaps addressed:** 1, 2, 3, 4, 5, 6, 9 (7/10 gaps)
- **BOX 1 stopped test edits:** ✅ Confirmed in READY signal

### BOX 2: ✅ READY
- **Signal:** `planning/refactor/evidence/rf1009-c01/BOX2-READY.md` exists
- **Task:** RF-10-09-C01 (Notification/outbox characterization)
- **Test file:** `tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs`
- **Test hash:** `1f233dc690351172812d5c5d413488457c7ebcc2e0b18193e042a0d07854f06f`
- **Test count:** 5 methods
- **Expected:** 5/5 PASS
- **Key findings:**
  - Inbox authorization (recipient-scoped)
  - Projection correctness (6 fields)
  - Cursor pagination (Base64 JSON)
  - Mark-read idempotency (Idempotency-Key replay)
  - Outbox consumer replay (prevents duplicate)
  - Producer linkage (correct correlationId/messageType)
  - **CRITICAL:** NO DISPATCHER FOUND (documented NOT_VERIFIED)
- **BOX 2 stopped test edits:** ✅ Confirmed in READY signal

## Phase A Completion Summary

✅ **All 8 documentation findings remediated**
✅ **RF-11-C01 preliminary audit complete** (CONDITIONAL PASS, A11-01 remediated)
✅ **Before/after snapshots captured** (9 files, 8 changed)
✅ **Phase A evidence packaged** (12 artifacts in checkpoint13-integration/)

## Phase B Execution Plan

### Build Sequence (Sequential, not parallel)

**Step B1: Build BOX 1 test assembly**
```bash
cd "D:\Project BE\RoadGuardSystem"
dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj \
  --configuration Debug \
  --no-incremental \
  2>&1 | tee planning/refactor/evidence/checkpoint13-integration/box1-build-console.txt
```

**Step B2: Run BOX 1 tests**
```bash
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj \
  --filter "TaskId=RF-10-08-C01" \
  --logger "trx;LogFileName=RF-10-08-C01.trx" \
  --results-directory planning/refactor/evidence/checkpoint13-integration \
  --no-build \
  2>&1 | tee planning/refactor/evidence/checkpoint13-integration/box1-test-console.txt
```

**Step B3: Verify BOX 1 results**
- Expected: 11 discovered, 11 executed, 11 passed, 0 failed, 0 skipped
- If failed: capture TRX, stop Phase B, report to user

**Step B4: Build BOX 2 test assembly** (same build, already done in B1)
- Skip separate build if BOX 1/2 in same assembly

**Step B5: Run BOX 2 tests**
```bash
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj \
  --filter "TaskId=RF-10-09-C01" \
  --logger "trx;LogFileName=RF-10-09-C01.trx" \
  --results-directory planning/refactor/evidence/checkpoint13-integration \
  --no-build \
  2>&1 | tee planning/refactor/evidence/checkpoint13-integration/box2-test-console.txt
```

**Step B6: Verify BOX 2 results**
- Expected: 5 discovered, 5 executed, 5 passed, 0 failed, 0 skipped
- If failed: capture TRX, stop Phase B, report to user

**Step B7: Capture provenance**
- Build metadata: timestamp, exit code, warnings/errors count
- Test metadata: timestamp, duration, exit code
- Assembly hashes: BOX 1 test DLL, BOX 2 test DLL
- Source hashes: Both test files (verify unchanged from READY hashes)

### Hash Verification Requirements

**BOX 1 test file:**
- Expected hash: `f6dc771fa337a86a7d2bf4f259d9bf1ff8430522a9057b68ad834bdced8e8cce`
- Verify: Hash before build matches READY hash

**BOX 2 test file:**
- Expected hash: `1f233dc690351172812d5c5d413488457c7ebcc2e0b18193e042a0d07854f06f`
- Verify: Hash before build matches READY hash

**Both files must be unchanged from READY state. If hash mismatch, STOP and report.**

## Phase B Success Criteria

✅ Build exit 0 (warnings OK, errors NOT OK)
✅ BOX 1: 11/11 tests PASS
✅ BOX 2: 5/5 tests PASS
✅ Source hashes match READY hashes (no edits after READY signal)
✅ Complete provenance chain captured (build + test metadata)
✅ TRX files captured for both boxes

## Phase B Failure Handling

**If build fails:**
- Capture full console output
- Identify error location (file:line)
- STOP Phase B
- Report to user: "Build failed, cannot proceed. Review error and fix outside three-box coordination."

**If tests fail:**
- Capture TRX with failure details
- Identify which test(s) failed
- STOP Phase B
- Report to user: "Tests failed, cannot proceed. BOX [1/2] must review failure and revise test."

**If hash mismatch:**
- STOP Phase B immediately
- Report to user: "Test source modified after READY signal. BOX [1/2] violated coordination protocol."

## Next Action

**Proceed to Phase B execution:**
1. Verify test file hashes match READY
2. Build test assembly
3. Run BOX 1 tests (11 expected)
4. Run BOX 2 tests (5 expected)
5. Capture complete provenance
6. On success: proceed to Phase C (packaging)
7. On failure: STOP and report

---

**Phase A:** COMPLETE ✅  
**Phase B:** READY TO START - All gates passed  
**Phase C:** PENDING - After Phase B success
