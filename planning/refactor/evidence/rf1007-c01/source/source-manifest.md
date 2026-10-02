# RF-10-07-C01 Source File Manifest

**Status:** Complete
**Generated:** 2026-10-01 14:55 UTC+7
**Repository:** D:\Project BE\RoadGuardSystem
**Branch:** anh
**Commit:** 2efc8a5775f834c7f0fe37cc0ce703011649e1f1

## Purpose

This directory contains full source files exercised or inspected during RF-10-07-C01 characterization. Files preserve exact repository state at commit `2efc8a5` with repository-relative paths documented for verification.

## Production Source (Read-only, unchanged)

### Controller Layer
- `InspectionTasksController.cs`
  - Origin: `RoadGuardSystem.API/Controllers/InspectionTasksController.cs`
  - SHA-256: `2683dcd02af07b237c3078ca800d8ef2faea24276719a0db217b5d5125214b39`
  - Role: HTTP endpoint for `GET /api/v1/me/inspection-tasks`
  - Authorization: `[Authorize]` attribute; role check delegated to service

### Service Layer
- `InspectionTaskQueryService.cs`
  - Origin: `RoadGuardSystem.Services/Implementations/Inspections/InspectionTaskQueryService.cs`
  - SHA-256: `ee99cd48388d94e17b5fd22b1c1ddf7eae29aad0e8a9e05082b6b8883426bdf8`
  - Role: RepairCrew role enforcement, project scope filtering via IProjectScopeGuard
  - Hardcoded: `mode = "MEASURE_ONLY"`, `policyVersionId = null`

- `IInspectionTaskQueryService.cs`
  - Origin: `RoadGuardSystem.Services/Interfaces/Inspections/IInspectionTaskQueryService.cs`
  - SHA-256: `b8e23fb3b2ec654d0faf6d7155dc12ed435e552b8070db78fd086218a7531b93`
  - Role: Service interface contract

### Repository Layer
- `InspectionTaskReadRepository.cs`
  - Origin: `RoadGuardSystem.Repositories/Implementations/Inspections/InspectionTaskReadRepository.cs`
  - SHA-256: `da932d31580b44f3a0a4b23ea52a882d11ea8a68b84a6ddb3dfa83c0ba78eada`
  - Role: Query tasks assigned to user via active FieldInspectionAssignments
  - Pagination: cursor-based (DueAt ASC, Id ASC)

- `IInspectionTaskReadRepository.cs`
  - Origin: `RoadGuardSystem.Repositories/Interfaces/Inspections/IInspectionTaskReadRepository.cs`
  - SHA-256: `6d0b3bcd07799eb1b0015f849308b9b05d31dea2552f264b4cbfd95b414a44e0`
  - Role: Repository interface contract

### Authorization
- `ProjectScopeGuard.cs`
  - Origin: `RoadGuardSystem.Services/Implementations/Authorization/ProjectScopeGuard.cs`
  - SHA-256: `7296a2fdc1ebfde09c7475881b3bcfa4b383d1da78934d14c3f27844f12f2530`
  - Role: Per-project membership authorization check

- `ProjectScopeGuardContracts.cs`
  - Origin: `RoadGuardSystem.Services/Interfaces/Authorization/ProjectScopeGuardContracts.cs`
  - SHA-256: `fe039a14816835f56bc35254bbf4270059360e598c7dd0ea327129419294acf4`
  - Role: Authorization contracts and result types

### Domain Entities
- `FieldInspectionTask.cs`
  - Origin: `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs`
  - SHA-256: `e9764135ea84667717c1796004ddf5afd06275a084cd017501f9ed6b2abc6974`
  - Role: Task entity with RowVersion concurrency token

- `FieldInspectionAssignment.cs`
  - Origin: `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionAssignment.cs`
  - SHA-256: `a08aa350d98430a6e61fa8e84259e30650685c1fb162d60136409fcd2b089787`
  - Role: Task-to-crew assignment with unique active constraint

- `FieldInspectionSession.cs`
  - Origin: `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionSession.cs`
  - SHA-256: `4124af0961a13c31931e869fcf202d6512adb109b825b9c85b4dc0a56f31ca2d`
  - Role: Measurement session entity with purpose enum (DefectVerification, ResearchValidation)

- `GroundTruthMeasurement.cs`
  - Origin: `RoadGuardSystem.BusinessObjects/Inspections/GroundTruthMeasurement.cs`
  - SHA-256: `2e2dfa816b3bc6f63fdedf5bf302eac06f45e1b37689e9f3512aa8f2f27c0fb6`
  - Role: Measurement entity with DefectId, SurveyId, RoadSectionVersionId provenance
  - Constraints: SRID 4326 geography, unit in ('mm', 'cm', 'm')

### EF Core Configurations
- `FieldInspectionTaskConfiguration.cs`
  - Origin: `RoadGuardSystem.Repositories/Configurations/FieldInspectionTaskConfiguration.cs`
  - SHA-256: `2060b3cf02949a6aab3749d5b72a0e0f37fa6a114fdf2d827094e41dfca8526e`
  - Role: Task table schema with RowVersion

- `FieldInspectionAssignmentConfiguration.cs`
  - Origin: `RoadGuardSystem.Repositories/Configurations/FieldInspectionAssignmentConfiguration.cs`
  - SHA-256: `6bbfc4da3080740947faa04aefa25dd195314d77dd812b4d898ba824249809e8`
  - Role: Assignment table schema with unique active constraint

- `FieldInspectionSessionConfiguration.cs`
  - Origin: `RoadGuardSystem.Repositories/Configurations/FieldInspectionSessionConfiguration.cs`
  - SHA-256: `930c6468d86258a55e221b9918e8fbdea57ba618936499dc2c575945b02ccb98`
  - Role: Session table schema

- `GroundTruthMeasurementConfiguration.cs`
  - Origin: `RoadGuardSystem.Repositories/Configurations/GroundTruthMeasurementConfiguration.cs`
  - SHA-256: `c469aad16a7058b809f815e3e660845191883cae46e1372ddcce0b9298dbd241`
  - Role: Measurement table schema with immutability trigger reference

### DTOs
- `InspectionTaskResponseDto.cs`
  - Origin: `RoadGuardSystem.DTOs/Inspections/InspectionTaskResponseDto.cs`
  - SHA-256: `bb333f162b96ed5960b6ead15324b0224b0b9a16029e39b97949b4668205de16`
  - Role: Single task response projection

- `InspectionTaskPageResponseDto.cs`
  - Origin: `RoadGuardSystem.DTOs/Inspections/InspectionTaskPageResponseDto.cs`
  - SHA-256: `3049bc70ad54241e378363a16cb8d7ed08ef0f9d204efb5134a287277e9c1356`
  - Role: Paginated response wrapper

## Test Source (New, created during C01)

- `Rf1007InspectionMeasurementCharacterizationTests.cs`
  - Origin: `tests/RoadGuardSystem.ApiTests/Inspections/Rf1007InspectionMeasurementCharacterizationTests.cs`
  - SHA-256: `9566e0ec69e05398417e100930900fb4c5c93ac2378a34bc6ef3ada3ba8ffdb0`
  - Role: Characterization test suite (5 test cases)
  - Coverage: RepairCrew/PM/Supervisor authorization, project filtering, SQL immutability, measurement provenance

## Test Infrastructure (Unchanged)

- `AuthenticationSqlServerFixture.cs`
  - Origin: `tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs`
  - SHA-256: `2eda4969413db8b2b50f84ca922085b5339283e4f6b4941e5c8caa2690c4bb29`
  - Role: xUnit shared SQL Server fixture with seeded users/projects/memberships

- `AuthenticationWebApplicationFactory.cs`
  - Origin: `tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationWebApplicationFactory.cs`
  - SHA-256: `62eef5274095e9dbf59082d3db4f4caacedfdb025c6f0dbdc1be59d4aa9490a5`
  - Role: WebApplicationFactory with JWT token generation

## Verification

Total files: 20
- Production unchanged: 18
- Test created: 1
- Test infrastructure: 1

All checksums in `source-files-checksums.txt` match files copied from repository at commit `2efc8a5`.

## Notes

- Files preserve exact state from repository; no test-specific modifications
- Repository paths documented for cross-reference with git history
- Source files support evidence traceability per RF-10-07-C01 report findings
- No production code was changed during characterization
