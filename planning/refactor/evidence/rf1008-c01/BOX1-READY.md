# RF-10-08-C01 BOX 1 READY Handoff

**Generated:** 2026-10-01  
**Box:** BOX 1 (characterization only, no build/test)  
**Branch:** anh  
**HEAD:** 2efc8a5775f834c7f0fe37cc0ce703011649e1f1  
**Status:** Ready for BOX 3 verification

## Work Completed

BOX 1 completed idempotency per-command characterization work per RF-10-08-C01 scope:
- Traced `IdempotencyOperationService` and production callers
- Created operation matrix with 10 evidence gaps
- Surveyed existing test coverage (P202, P120, P2, Upload)
- Analyzed precondition/authorization ordering patterns (Pattern A/B/C)
- Designed 11 focused tests for critical gaps
- Wrote test class: `IdempotencyPerCommandCharacterizationTests.cs`

**NOT executed:** All tests marked NOT_RUN. BOX 3 will run tests after BOX 1 and BOX 2 stop modifying source.

## Test Allowlist

**NEW test file created by BOX 1:**
```
tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs
```

**Test methods (11 total):**
1. `UploadCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome` (Gap 1)
2. `SurveyPlanCreate_SameKeySamePayload_ReplaysWithIdenticalOutcome` (Gap 1)
3. `UploadCreate_SameKeyDifferentPayload_ReturnsConflict` (Gap 2)
4. `SurveyPlanCreate_SameKeyDifferentPayload_ReturnsConflict` (Gap 2)
5. `UploadCreate_DifferentActorsSameKey_BothExecute` (Gap 3)
6. `UploadCreate_SameActorSameKeyDifferentProjects_BothExecute` (Gap 4)
7. `SurveyTaskCreate_SameActorSameKeyDifferentProjects_BothExecute` (Gap 4)
8. `SurveyPlanPostpone_StaleVersionThenReplay_ReturnsStoredPreconditionFailure` (Gap 5)
9. `ProjectCreate_ActorRoleRevokedAfterCreate_ReplayReturnsStoredOutcome` (Gap 6)
10. `UploadCreate_StatusMutatedAfterCreate_ReplayReturnsOriginalStatus` (Gap 9)
11. `CharacterizationUploadStorage` (mock storage helper class)

**Test trait:** `[Trait("TaskId", "RF-10-08-C01")]`  
**Test collection:** `[Collection(AuthenticationApiFixture.Name)]`

## Source Dependencies

**Core primitive:**
- `RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs` (210 lines)
- `RoadGuardSystem.BusinessObjects/Idempotency/IdempotencyRecord.cs` (112 lines)

**Callers analyzed:**
- `RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs` (CreateAsync, CompleteAsync)
- `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs` (CreatePlanAsync, PostponePlanAsync, CreateTaskAsync)
- `RoadGuardSystem.Repositories/Implementations/Projects/ProjectCreationPersistenceService.cs` (CreateAsync)

**Controllers:**
- `RoadGuardSystem.API/Controllers/UploadsController.cs` (Create, Complete)
- `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs` (CreatePlan, Postpone, CreateTask)
- `RoadGuardSystem.API/Controllers/ProjectsController.cs` (Create)

**Test infrastructure:**
- `tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs`
- `tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs`
- `RoadGuardSystem.Repositories/Storage/IUploadObjectStorage.cs`

## Source Hashes (SHA-256)

**NOTE:** Hashes are for CURRENT source at time of test writing, NOT modified by BOX 1.

Core primitive (not modified):
```
IdempotencyOperationService.cs: [hash not computed - file not modified]
IdempotencyRecord.cs: [hash not computed - file not modified]
```

Test file created by BOX 1:
```
tests/RoadGuardSystem.ApiTests/Idempotency/IdempotencyPerCommandCharacterizationTests.cs
SHA-256: f6dc771fa337a86a7d2bf4f259d9bf1ff8430522a9057b68ad834bdced8e8cce
Lines: 1045
```

## Test Execution Command

**BOX 3 must run:**
```bash
cd "D:\Project BE\RoadGuardSystem"
dotnet test --filter "TaskId=RF-10-08-C01" --logger "trx;LogFileName=RF-10-08-C01.trx" --no-build
```

**Prerequisites:**
- BOX 1 and BOX 2 must stop modifying test source
- Build entire solution first (BOX 3 responsibility)
- SQL Server test database available
- `AuthenticationSqlServerFixture` configured

**Expected outcome:** 11/11 tests PASS

## Fixture Requirements

**Database:**
- Isolated SQL Server database per `AuthenticationSqlServerFixture`
- Fresh database context per test via `_sql.CreateDbContext()`
- Test data created fresh per test (no shared state)

**Mock dependencies:**
- `CharacterizationUploadStorage` class provides in-memory upload storage mock
- No external MinIO/S3 required for these tests (except existing MinIO smoke test)

**Test isolation:**
- Each test creates unique users with GUID-suffixed names
- Each test creates unique projects/segments/plans
- No cross-test data dependencies

## Evidence Gaps Addressed

| Gap | Tests | Expected Evidence |
|---|---|---|
| Gap 1: Same key + same fingerprint replay | Test 1, 2 | 201 Created with identical body text, single entity/idempotency record |
| Gap 2: Same key + different fingerprint conflict | Test 3, 4 | 409 Conflict with "duplicate_request", no second entity created |
| Gap 3: Actor isolation | Test 5 | Two actors with same key → both execute, separate idempotency records |
| Gap 4: Project isolation | Test 6, 7 | Same actor/key, different projects → both execute, separate records |
| Gap 5: Stored precondition outcome replay | Test 8 | Stale version → 412 stored, replay → same 412 without re-checking current version |
| Gap 6: Authorization change before replay | Test 9 | Actor role changed → replay returns stored success outcome |
| Gap 9: Stored outcome vs current state | Test 10 | Entity status mutated → replay returns original status from OutcomeJson |

**Gaps NOT tested (lower priority or existing coverage):**
- Gap 7: Race condition - P120 line 104-111 already demonstrates concurrent requests
- Gap 8: Commit failure recovery - P120 line 119-126, 341-386 already demonstrates
- Gap 10: Operation name isolation - implied by composite key, lower priority

## Findings

### F-RF-10-08-C01-01: Upload Complete Precondition Pattern Differs from Survey Postpone

**Location:** `UploadPersistenceService.CompleteAsync` line 247-249

**Description:**
Upload Complete throws `UploadConcurrencyException` on stale RowVersion (line 249), causing transaction rollback with NO idempotency record stored. Survey Postpone stores concurrency failure in outcome JSON and commits (SurveyV2PersistenceService line 65). This means Upload Complete is NOT a pure Pattern A example for Gap 5 testing.

**Impact:**
- Upload Complete retry with same key/fingerprint will execute handler again (no replay, no record exists)
- Survey Postpone retry with same key/fingerprint will return stored failure (replay, does not re-check)
- Gap 5 test MUST use Survey Postpone only

**Evidence:**
```
Upload: line 249 throw UploadConcurrencyException → rollback → no record
Survey: line 65 return stored ConcurrencyConflict → commit → record exists
```

### F-RF-10-08-C01-02: Three Distinct Precondition/Authorization Ordering Patterns

**Pattern A (Survey Postpone):**
Authorization → Idempotency → Precondition inside handler → Store precondition failure in outcome

**Pattern B (Project Create):**
Authorization → Precondition check → Idempotency (only if precondition passes)

**Pattern C (Survey Task Create):**
Authorization → Idempotency → Eligibility check inside handler → Throw exception if fails (rollback)

**Impact:**
Different patterns have different replay behavior for precondition failures. Only Pattern A stores precondition failures in outcome JSON for replay.

## Limitations

1. **No build/test execution:** BOX 1 did not run tests (BOX 3 responsibility)
2. **No production source modification:** All production code remains unchanged
3. **No schema/migration changes:** Test uses existing database schema
4. **No commit failure recovery test:** Gap 8 already covered by P120
5. **No race condition explicit test:** Gap 7 already covered by P120
6. **No operation name isolation test:** Gap 10 lower priority, implied by composite key

## Documentation Artifacts

**Created in `planning/refactor/evidence/rf1008-c01/`:**
1. `00-WORK-START.md` - Initial work tracking
2. `01-operation-matrix.md` - Complete operation matrix with 10 gaps
3. `02-existing-coverage-survey.md` - Existing test coverage analysis
4. `03-upload-complete-analysis.md` - Upload Complete vs Survey Postpone pattern analysis
5. `04-test-design-plan.md` - Detailed test design specifications
6. `BOX1-READY.md` - This handoff document

## Next Steps for BOX 3

1. **Verify BOX 1 and BOX 2 stopped modifying tests**
2. **Build entire solution:**
   ```bash
   dotnet build RoadGuardSystem.sln --configuration Debug
   ```
3. **Run RF-10-08-C01 tests:**
   ```bash
   dotnet test --filter "TaskId=RF-10-08-C01" --logger "trx;LogFileName=RF-10-08-C01.trx" --no-build
   ```
4. **Verify 11/11 PASS**
5. **Create report:** `planning/refactor/reports/RF-10-08-C01.md` with:
   - Test execution evidence (TRX, console output)
   - All 11 tests PASS confirmation
   - Source hashes at execution time
   - SQL verification snapshots (if needed)
   - Findings F-RF-10-08-C01-01 and F-RF-10-08-C01-02 confirmed

6. **Package final deliverable:** `RF-10-08-C01-box1-review.zip` with:
   - Changed test file
   - Documentation artifacts (6 markdown files)
   - Test execution logs
   - Manifest with SHA-256 hashes

## BOX 1 Constraints Observed

✓ No production/schema/contract/migration/CI modification  
✓ No commit/push/merge/reset/clean/stash/branch change  
✓ No DB writes to shared database  
✓ No build/test execution in BOX 1  
✓ No ledger/checklist modification  
✓ No F/G implementation or Android sync work  
✓ Test writes only for new RF-10-08-C01 tests  
✓ Documentation in dedicated `rf1008-c01/` directory  

## Handoff Complete

BOX 1 characterization work is complete. All tests are NOT_RUN and ready for BOX 3 verification after BOX 2 completes and both boxes stop modifying test source.
