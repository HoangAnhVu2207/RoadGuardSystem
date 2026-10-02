# RF-10-07-C01 Comprehensive Evidence Manifest

**Package:** RF-10-refactor-checkpoint-12-correction-01-handoff
**Status:** Partial pending review
**Generated:** 2026-10-01 15:00 UTC+7
**Repository:** D:\Project BE\RoadGuardSystem
**Branch:** anh
**Commit:** 2efc8a5775f834c7f0fe37cc0ce703011649e1f1
**Working tree:** Dirty (pre-existing changes preserved)

## Package Purpose

This checkpoint documents RF-10-07-C01 characterization of inspection task list endpoint (`GET /api/v1/me/inspection-tasks`). It establishes current behavior baseline for RepairCrew authorization, project scope filtering, response projection, and SQL immutability. Package includes full source files, build/test provenance, and historical documentation corrections.

## Package Structure

```
rf1007-c01/
├── COMPREHENSIVE-MANIFEST.md          (this file)
├── manifest.txt                       (initial manifest, superseded by this)
├── evidence-checksums.txt             (SHA-256 for all evidence files)
├── before-snapshot.md                 (source state before test creation)
├── build-metadata.txt                 (build provenance with timestamps)
├── build-output.txt                   (full build console output)
├── test-run-metadata.txt              (test execution provenance)
├── test-output.txt                    (first run console - 3/5 passed)
├── test-output-final.txt              (final run console - 5/5 passed)
├── rf1007-c01-test.trx                (first run TRX)
├── rf1007-c01-test-final.trx          (final run TRX)
├── test-summary.txt                   (execution summary)
├── test-file-hash.txt                 (test file SHA-256)
├── source-hashes-before.txt           (production source hashes)
├── changed-files-hashes.txt           (all changed file hashes)
└── source/                            (full source files)
    ├── source-manifest.md             (source file documentation)
    ├── source-files-checksums.txt     (source file SHA-256)
    ├── Rf1007InspectionMeasurementCharacterizationTests.cs
    ├── InspectionTasksController.cs
    ├── InspectionTaskQueryService.cs
    ├── IInspectionTaskQueryService.cs
    ├── InspectionTaskReadRepository.cs
    ├── IInspectionTaskReadRepository.cs
    ├── InspectionTaskResponseDto.cs
    ├── InspectionTaskPageResponseDto.cs
    ├── FieldInspectionTask.cs
    ├── FieldInspectionAssignment.cs
    ├── FieldInspectionSession.cs
    ├── GroundTruthMeasurement.cs
    ├── FieldInspectionTaskConfiguration.cs
    ├── FieldInspectionAssignmentConfiguration.cs
    ├── FieldInspectionSessionConfiguration.cs
    ├── GroundTruthMeasurementConfiguration.cs
    ├── ProjectScopeGuard.cs
    ├── ProjectScopeGuardContracts.cs
    ├── AuthenticationSqlServerFixture.cs
    └── AuthenticationWebApplicationFactory.cs
```

## Evidence Categories

### 1. Test Source (New)
- **Rf1007InspectionMeasurementCharacterizationTests.cs**
  - Location: `source/Rf1007InspectionMeasurementCharacterizationTests.cs`
  - Repository origin: `tests/RoadGuardSystem.ApiTests/Inspections/`
  - SHA-256: `9566e0ec69e05398417e100930900fb4c5c93ac2378a34bc6ef3ada3ba8ffdb0`
  - Size: 13,084 bytes
  - Status: Created during C01
  - Test cases: 5 (RepairCrew success, PM/Supervisor forbidden, project filtering, measurement provenance)
  - Corrections: 2 (invalid EF Include removed, JSON read reordered)

### 2. Production Source (Read-only, unchanged)
All files preserved at commit `2efc8a5` state. See `source/source-manifest.md` for full inventory.

**Controller (1 file):**
- InspectionTasksController.cs (3,980 bytes)

**Services (4 files):**
- InspectionTaskQueryService.cs (4,192 bytes)
- IInspectionTaskQueryService.cs (653 bytes)
- ProjectScopeGuard.cs (1,751 bytes)
- ProjectScopeGuardContracts.cs (475 bytes)

**Repositories (2 files):**
- InspectionTaskReadRepository.cs (2,687 bytes)
- IInspectionTaskReadRepository.cs (334 bytes)

**Entities (4 files):**
- FieldInspectionTask.cs (5,996 bytes)
- FieldInspectionAssignment.cs (3,081 bytes)
- FieldInspectionSession.cs (4,542 bytes)
- GroundTruthMeasurement.cs (5,274 bytes)

**EF Configurations (4 files):**
- FieldInspectionTaskConfiguration.cs (4,098 bytes)
- FieldInspectionAssignmentConfiguration.cs (3,030 bytes)
- FieldInspectionSessionConfiguration.cs (4,028 bytes)
- GroundTruthMeasurementConfiguration.cs (4,436 bytes)

**DTOs (2 files):**
- InspectionTaskResponseDto.cs (262 bytes)
- InspectionTaskPageResponseDto.cs (199 bytes)

**Test Infrastructure (2 files):**
- AuthenticationSqlServerFixture.cs (5,451 bytes)
- AuthenticationWebApplicationFactory.cs (5,340 bytes)

Total: 20 source files, all checksums in `source/source-files-checksums.txt`

### 3. Build Evidence
- **build-metadata.txt**: Build provenance with exact commands, timestamps, working directory, exit codes
  - First build: 2026-10-01 after test creation, 7.6s, 113 warnings, 0 errors
  - Verification build: 2026-10-01 before final test, 4s incremental
  - Assembly: `tests/RoadGuardSystem.ApiTests/bin/Debug/net8.0/RoadGuardSystem.ApiTests.dll`
  - Framework: .NETCoreApp v8.0

- **build-output.txt**: Full console output from first build (113 warnings documented, consistent with project baseline)

### 4. Test Execution Evidence
- **test-run-metadata.txt**: Complete test provenance
  - First run: 5 discovered, 5 executed, 3 passed, 2 failed (test bugs), exit 1
  - Final run: 5 discovered, 5 executed, 5 passed, 0 failed, exit 0
  - Duration: ~2s first run, ~4s final run
  - Both runs used same assembly with --no-build flag

- **test-output.txt**: First run console showing 2 test failures
  - Invalid EF Include on scalar DefectId property
  - Premature JSON deserialization before status check

- **test-output-final.txt**: Final run console showing 5/5 passed

- **rf1007-c01-test.trx**: First run TRX file (XML test results)

- **rf1007-c01-test-final.trx**: Final run TRX file (XML test results)

- **test-summary.txt**: Human-readable test execution summary

### 5. Source State Evidence
- **before-snapshot.md**: Repository state before test creation
  - Commit: 2efc8a5
  - Working tree: dirty (pre-existing changes preserved)
  - Test file: did not exist
  - Production source: unchanged

- **source-hashes-before.txt**: SHA-256 of production source before C01
  - InspectionTasksController.cs
  - InspectionTaskQueryService.cs
  - InspectionTaskReadRepository.cs

- **test-file-hash.txt**: SHA-256 of created test file

- **changed-files-hashes.txt**: SHA-256 of all files changed during C01
  - Test file (new)
  - Baseline documentation (new)
  - Reports (new/updated)
  - Checklist/slices (updated)

### 6. Integrity Verification
- **evidence-checksums.txt**: SHA-256 for all evidence files in this package

## Verification Commands

### Extract and verify archive integrity
```bash
cd "D:\Project BE\RoadGuardSystem\planning\refactor\evidence"
tar -tzf RF-10-refactor-checkpoint-12-correction-01-handoff.tar.gz | wc -l
tar -xzf RF-10-refactor-checkpoint-12-correction-01-handoff.tar.gz
cd rf1007-c01
sha256sum -c evidence-checksums.txt
```

### Verify source file checksums
```bash
cd source
sha256sum -c source-files-checksums.txt
```

### Verify test file against repository
```bash
cd "D:\Project BE\RoadGuardSystem"
sha256sum tests/RoadGuardSystem.ApiTests/Inspections/Rf1007InspectionMeasurementCharacterizationTests.cs
# Compare with: 9566e0ec69e05398417e100930900fb4c5c93ac2378a34bc6ef3ada3ba8ffdb0
```

### Re-run tests (requires SQL Server fixture)
```bash
cd "D:\Project BE\RoadGuardSystem"
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj \
  --filter 'FullyQualifiedName~Rf1007InspectionMeasurementCharacterizationTests' \
  --logger 'console;verbosity=detailed'
```

## Findings Summary

| ID | Impact | Finding |
|---|---|---|
| RF1007-C01-F01 | Low | RepairCrew-only authorization confirmed; PM/Supervisor receive 403 Forbidden |
| RF1007-C01-F02 | Low | Response mode hardcoded to "MEASURE_ONLY"; policyVersionId always null |
| RF1007-C01-F03 | Medium | Project membership filtering works at service layer after repository fetch |
| RF1007-C01-F04 | Low | GET endpoint performs no SQL writes; read-side immutability confirmed |
| RF1007-C01-F05 | Medium | GroundTruthMeasurement provenance links to session, defect, survey, road version; SRID 4326 enforced |
| RF1007-C01-F06 | Low | Test correction: Invalid EF Include on scalar property DefectId removed |
| RF1007-C01-F07 | Low | Test correction: JSON read moved after status assertion to avoid KeyNotFoundException |

## Known Limitations

### NOT_IMPLEMENTED (no production endpoint found)
- Task creation/assignment/mutation
- PM batch review workflow
- Temporary safety action recording
- Policy evaluation and versioning
- Repair proposal/attempt/acceptance
- Fast Track dossier application

### NOT_VERIFIED (out of scope for C01)
- Cursor pagination boundary conditions
- Concurrency during active assignment changes
- RowVersion optimistic locking on updates
- Measurement submission via HTTP endpoint
- External consumer usage (Web/Android)
- Deployed database state

### SOURCE_INSPECTED (not runtime tested)
- FieldInspectionSession domain logic
- GroundTruthMeasurement immutability triggers
- Session purpose gates (DefectVerification vs ResearchValidation)

## SQL Immutability Scope

Fresh DbContext snapshots captured before/after GET request verified NO changes to:
- FieldInspectionTask (identity, status, task code, RowVersion)
- FieldInspectionAssignment (status, assigned user)
- Defect (status, identity)
- GroundTruthMeasurement (count unchanged)

Snapshots covered ONLY records directly exercised by test. Full database audit was not performed. No audit logs or outbox records generated by GET endpoint.

## Historical Documentation Corrections

### RF-10-06-C01 corrections (applied during this checkpoint)
1. Build warnings count: corrected "0 warnings" to "108 warnings" to match evidence
2. Checkpoint sequence: clarified checkpoint-11 (failed baseline) vs correction-01 (successful fix)
3. Summary: updated to reference both retained runs as separate historical evidence

Location: `planning/refactor/reports/RF-10-06-C01.md`

### Survey baseline provenance gap (documented, no correction needed)
Survey C01/C02 reports correctly noted stored create replay returns original projection (operator A, NEW_ASSIGNED) even when SQL shows later reassignment to operator B. This is observed behavior, not a characterization bug. Limitation already documented in both baselines.

## Related Documentation

**Baseline:**
- `planning/refactor/10-inspection-measurement-characterization-baseline.md`
  - SHA-256: `9ba87887e52aa41aed00bb4c3b87030a3980e0748bba513aee36d180e6fc0b2d`

**Report:**
- `planning/refactor/reports/RF-10-07-C01.md`
  - SHA-256: `47bcc350f8fc4bebab1f2625afb79b7b1fee5959ed2d3a4a218dcf338dd4705e`

**Ledger updates:**
- `planning/refactor/10-refactor-slices.md` (10-07-C01 status: DONE locally)
  - SHA-256: `19607ee62bbcb40229708d0f2e2830ea24515ec430c1dbc4250b56269494fdaf`

- `planning/refactor/10-refactor-checklist.md` (10-07-C01 marked complete for local characterization)
  - SHA-256: `37a3f2911cd9005518975406087294aa63d02b81e2221891b92224befcaa8579`

## Decisions Blocking F/G Work

- **Q-RF02-04**: Fast Track method/material dossier and numeric thresholds
- **Q-RF02-07**: PM visibility scope, report-to-defect mapping, out-of-warranty routing, public image/PII projection
- **Q-RF02-05**: May affect survey-derived measurement evidence

These block future F/G implementation, NOT the completed local C01 characterization.

## Package Integrity

### Self-verification checklist
- [x] Full source files included (not just hashes)
- [x] Test file with exact corrections documented
- [x] Build provenance with timestamps and exit codes
- [x] Test provenance for both runs (failed and passed)
- [x] Console output and TRX files for both runs
- [x] Source state snapshot before changes
- [x] SHA-256 checksums for all files
- [x] Repository paths preserved for cross-reference
- [x] NOT_IMPLEMENTED vs NOT_VERIFIED distinction clear
- [x] SQL immutability scope explicitly bounded
- [x] Historical corrections applied and documented

### Missing or NOT_VERIFIED
- [ ] Real byte-level SQL snapshots (fresh DbContext reads captured as C# objects, not raw bytes)
- [ ] Build cache state (incremental build warnings may differ from clean build)
- [ ] Test fixture SQL transaction isolation level (not documented)
- [ ] Exact SQL queries generated by EF Core (not logged)

These limitations are acceptable for current characterization scope. Byte-level snapshots would require raw SQL dump, which was not performed. EF Core query logging would require configuration changes.

## Status and Next Steps

**Current status:** Partial pending review

**C01 complete for:** Current inspection task list endpoint characterization

**Remaining RF-10-07 work:**
- R01 assessment: Evaluate current read implementation for extraction (provisional RETAIN expected)
- F/G workflows: Require approved Q-RF02-04 and Q-RF02-07 decisions
- Measurement submission: If HTTP endpoint exists, characterize separately

**Handoff constraint:** "Dừng sau handoff để reviewer kiểm; không tự nâng Done hoặc chuyển task"

This package is ready for reviewer verification. Do not promote to Done or assign next task until review complete.

## Package Metadata

- **Archive:** `RF-10-refactor-checkpoint-12-correction-01-handoff.tar.gz`
- **Archive SHA-256:** `fc14cb21b3f4969cc345203a8b4e6eeef4d46471f3880d77ca04cd964882fbd7` (to be updated after repackaging)
- **Total files:** 35+ (including nested source directory)
- **Total size:** ~150 KB
- **Compression:** gzip
- **Working tree state:** Preserved dirty changes; no commits created
- **Production changes:** None
- **Shared database changes:** None

Generated by: Kiro (Claude Code)
Handoff from: Codex → Anh → Reviewer
