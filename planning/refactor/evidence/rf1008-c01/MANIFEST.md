# RF-10-08-C01 BOX 1 Deliverable Manifest

**Generated:** 2026-10-01  
**Task:** RF-10-08-C01 per-command idempotency characterization  
**Branch:** anh  
**HEAD:** 2efc8a5775f834c7f0fe37cc0ce703011649e1f1  
**Status:** BOX 1 complete - ready for BOX 3 verification

## Test Files

| File | Lines | SHA-256 |
|---|---|---|
| tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs | 1045 | f6dc771fa337a86a7d2bf4f259d9bf1ff8430522a9057b68ad834bdced8e8cce |

## Documentation Files

| File | Lines | Purpose |
|---|---|---|
| planning/refactor/evidence/rf1008-c01/00-WORK-START.md | 58 | Initial work tracking |
| planning/refactor/evidence/rf1008-c01/01-operation-matrix.md | 258 | Operation matrix with 10 evidence gaps |
| planning/refactor/evidence/rf1008-c01/02-existing-coverage-survey.md | 209 | Existing test coverage analysis |
| planning/refactor/evidence/rf1008-c01/03-upload-complete-analysis.md | 113 | Upload Complete vs Survey Postpone pattern analysis |
| planning/refactor/evidence/rf1008-c01/04-test-design-plan.md | 345 | Detailed test specifications |
| planning/refactor/evidence/rf1008-c01/BOX1-READY.md | 222 | Handoff document for BOX 3 |
| planning/refactor/evidence/rf1008-c01/MANIFEST.md | (this file) | Deliverable manifest |

**Total documentation:** 1205 lines  
**Total test code:** 1045 lines  
**Grand total:** 2250 lines

## Final Report

| File | Lines |
|---|---|
| planning/refactor/reports/RF-10-08-C01.md | 255 |

## Test Methods (11 total)

1. `UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome` - Gap 1
2. `SurveyPlanCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome` - Gap 1
3. `UploadCreate_SameKeyDifferentPayload_ReturnsConflict` - Gap 2
4. `SurveyPlanCreate_SameKeyDifferentPayload_ReturnsConflict` - Gap 2
5. `UploadCreate_DifferentActorsSameKey_BothExecute` - Gap 3
6. `UploadCreate_SameActorSameKeyDifferentProjects_BothExecute` - Gap 4
7. `SurveyTaskCreate_SameActorSameKeyDifferentProjects_BothExecute` - Gap 4
8. `SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure` - Gap 5
9. `ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome` - Gap 6
10. `UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus` - Gap 9
11. `CharacterizationUploadStorage` - mock storage helper

**Status:** All tests NOT_RUN - BOX 3 verification required

## Findings

**F-RF-10-08-C01-01:** Upload Complete Precondition Pattern Differs from Survey Postpone  
- Upload Complete throws on stale RowVersion (transaction rollback, no record)
- Survey Postpone stores concurrency failure in outcome JSON (transaction commits)
- Severity: Moderate (affects test design)

**F-RF-10-08-C01-02:** Three Distinct Authorization/Precondition Ordering Patterns  
- Pattern A (Survey Postpone): Auth → Idempotency → Precondition inside, stores failure
- Pattern B (Project Create): Auth → Precondition → Idempotency
- Pattern C (Survey Task): Auth → Idempotency → Eligibility inside, throws
- Severity: Low (design observation)

## BOX 3 Verification Command

```bash
cd "D:\Project BE\RoadGuardSystem"
dotnet build RoadGuardSystem.sln --configuration Debug
dotnet test --filter "TaskId=RF-10-08-C01" --logger "trx;LogFileName=RF-10-08-C01.trx" --no-build
```

**Expected:** 11/11 tests PASS

## Constraints Observed

✓ No production/schema/contract/migration/CI modification  
✓ No commit/push/merge/reset/clean/stash/branch change  
✓ No DB writes to shared database  
✓ No build/test execution in BOX 1  
✓ No ledger/checklist modification  
✓ No F/G implementation or Android sync work  
✓ Test writes only for RF-10-08-C01  
✓ Documentation in isolated rf1008-c01/ directory

## Handoff

**BOX 1 complete.**  
**Gửi cho BOX 3:** `planning/refactor/evidence/rf1008-c01/BOX1-READY.md`
