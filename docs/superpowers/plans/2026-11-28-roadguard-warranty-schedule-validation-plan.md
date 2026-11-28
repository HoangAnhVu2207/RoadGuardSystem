# RoadGuard Warranty Schedule Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce warranty-bounded survey scheduling and return stable validation errors that FE can render.

**Architecture:** Add one domain time-policy component that resolves the applicable warranty and validates half-open UTC intervals against Vietnam calendar dates. Route project, warranty, survey, operator and field-task writes through that component. Keep existing routes, idempotency and ETags, adding only the recurrence payload and occurrence projection needed for weekly and monthly schedules.

**Tech Stack:** .NET, ASP.NET Core, EF Core with SQL Server, existing ProblemDetails mapping, xUnit API tests and SQL integration tests.

**Spec:** [2026-11-28-roadguard-warranty-schedule-validation-design.md](../specs/2026-11-28-roadguard-warranty-schedule-validation-design.md)

## Global Constraints

- Persist instants in UTC and apply `Asia/Ho_Chi_Minh` only to the inclusive warranty end-date boundary.
- Preserve old and V2 survey routes and their idempotency and concurrency headers.
- Do not change retention, authorization, notification ownership or AI behavior.
- Do not add mock AI, package upgrades or unrelated refactors.
- Preserve unrelated dirty paths in the current worktree.
- A rejected recurrence writes no plan or occurrence rows.

---

### Task 1: Establish the time-policy contract and failing tests

**Files:**
- Create `RoadGuardSystem.Services/Interfaces/Surveys/SurveyWarrantyTimePolicy.cs`.
- Create `tests/RoadGuardSystem.UnitTests/Surveys/SurveyWarrantyTimePolicyTests.cs`.
- Modify `tests/RoadGuardSystem.ApiTests/Surveys/Anh01SurveyFlowTests.cs`.

**Interfaces:**
- `SurveyWarrantyWindow(DateOnly StartDate, DateOnly EndDate, Guid WarrantyId)`.
- `SurveyTimeValidationResult(bool IsValid, string? Code, string? Field, DateTimeOffset? FirstInvalidAt)`.
- `ResolveAsync(Guid projectId, Guid roadSectionId, Guid? warrantyId, CancellationToken token)`.
- `ValidateInterval(DateTimeOffset start, DateTimeOffset end, SurveyWarrantyWindow window)`.

- [ ] Add unit cases for a valid one-year boundary, a valid two-year boundary and a half-open interval ending at the local end-date boundary.
- [ ] Add failing cases for start before warranty, end after warranty, end before start, no applicable warranty and multiple applicable warranties.
- [ ] Add API assertions for `application/problem+json`, `code`, `field`, boundary dates and `correlationId`.
- [ ] Run `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter FullyQualifiedName~SurveyWarrantyTimePolicyTests` and record the expected failure.
- [ ] Commit with `git commit -m "test: define warranty bounded survey time rules"`.

### Task 2: Implement shared warranty resolution and FE error mapping

**Files:**
- Modify `RoadGuardSystem.Services/Interfaces/Surveys/SurveyWarrantyTimePolicy.cs`.
- Create `RoadGuardSystem.Services/Implementations/Surveys/SurveyWarrantyTimePolicy.cs`.
- Modify `RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs`.
- Modify `RoadGuardSystem.Services/Implementations/Surveys/SurveyPlanningService.cs`.
- Modify `RoadGuardSystem.API/Constants/ApiErrorCodes.cs`.
- Modify `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.
- Modify `RoadGuardSystem.API/Controllers/SurveyPlanningController.cs`.

- [ ] Resolve an explicitly selected warranty after checking project and road scope. Without a selection, resolve one active applicable warranty. Return `survey_warranty_selection_required` when more than one qualifies.
- [ ] Convert the local end date to the exclusive next-day boundary in `Asia/Ho_Chi_Minh`, then compare UTC instants.
- [ ] Map policy failures to status `422` with `code`, `field`, `warrantyId`, `warrantyStartDate`, `warrantyEndDate`, requested instants and `correlationId`.
- [ ] Preserve existing 401, 403, 404, 409, 412 and 428 mappings.
- [ ] Run `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter FullyQualifiedName~SurveyWarrantyTimePolicyTests` and the survey API lane.
- [ ] Commit with `git commit -m "feat: add warranty bounded survey time policy"`.

### Task 3: Complete project and warranty date validation

**Files:**
- Modify `RoadGuardSystem.Services/Implementations/Projects/ProjectCreationService.cs`.
- Modify `RoadGuardSystem.Services/Implementations/Projects/ProjectUpdateService.cs`.
- Modify `RoadGuardSystem.Services/Implementations/Warranties/WarrantyCreationService.cs`.
- Modify `RoadGuardSystem.Repositories/Implementations/Projects/ProjectCreationPersistenceService.cs`.
- Modify `RoadGuardSystem.Repositories/Implementations/Warranties/WarrantyPersistenceService.cs`.
- Modify `tests/RoadGuardSystem.ApiTests/Projects/P120ProjectCreationTests.cs`.
- Modify `tests/RoadGuardSystem.ApiTests/Warranties/P120WarrantyCreationTests.cs`.

- [ ] Add tests for reversed warranty dates, warranty end before handover, and project dates that exclude the warranty.
- [ ] Return `warranty_dates_invalid` with `field` set to `warrantyStartDate`, `warrantyEndDate`, `handoverDate`, `startDate` or `endDate`.
- [ ] Keep existing source-file checks, project status checks, operation replay and concurrency behavior.
- [ ] Run `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter FullyQualifiedName~P120ProjectCreationTests|FullyQualifiedName~P120WarrantyCreationTests`.
- [ ] Commit with `git commit -m "feat: validate project and warranty date ranges"`.

### Task 4: Add bounded weekly and monthly occurrences

**Files:**
- Modify `RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanV2RequestDto.cs`.
- Modify `RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanResponseDto.cs`.
- Create `RoadGuardSystem.BusinessObjects/Surveys/SurveyRecurrence.cs`.
- Modify `RoadGuardSystem.BusinessObjects/Surveys/SurveyPlan.cs`.
- Modify `RoadGuardSystem.Repositories/Configurations/SurveyPlanConfiguration.cs`.
- Modify `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs`.
- Modify `RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs`.
- Modify `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`.
- Add the EF migration with `dotnet ef migrations add WarrantyScheduleValidation --project RoadGuardSystem.Repositories --startup-project RoadGuardSystem.API`.
- Create `tests/RoadGuardSystem.IntegrationTests/Surveys/SurveyRecurrenceWarrantySqlTests.cs`.

**Interfaces:**
- Optional request `recurrence` contains `frequency` with `NONE`, `WEEKLY` or `MONTHLY`, and `untilWarrantyEnd`.
- Response adds `occurrenceCount`, `firstPlannedAt`, `lastPlannedAt` and `occurrences` with ID, start, end and status.

- [ ] Add SQL tests for one-year weekly, two-year monthly, day 31, February, leap year, first occurrence outside the window and idempotent replay.
- [ ] Implement a deterministic generator using the first occurrence's local weekday, local day and local time. Monthly dates clamp to the last day of a short month and use the original day in later months.
- [ ] Store occurrences with a unique key on plan, route and planned start. Keep the existing active-plan uniqueness rule.
- [ ] Create the plan and all occurrences in one transaction. Return `survey_recurrence_empty` without writes when no occurrence fits.
- [ ] Preserve the old response shape for requests without `recurrence`.
- [ ] Run `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter FullyQualifiedName~SurveyRecurrenceWarrantySqlTests`.
- [ ] Commit with `git commit -m "feat: bound survey recurrence by warranty"`.

### Task 5: Apply the policy to Operator and Crew work

**Files:**
- Modify `RoadGuardSystem.DTOs/Surveys/CreateSurveyTaskV2RequestDto.cs`.
- Modify `RoadGuardSystem.DTOs/Surveys/ReassignSurveyTaskV2RequestDto.cs`.
- Modify `RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs`.
- Modify `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs`.
- Modify `RoadGuardSystem.Services/Implementations/Surveys/SurveyPlanningService.cs`.
- Modify `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanningPersistenceService.cs`.
- Modify `RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.Business.cs`.
- Create `tests/RoadGuardSystem.IntegrationTests/Inspections/FieldTaskWarrantyTimeSqlTests.cs`.

- [ ] Add cases for Operator `dueAt`, Crew measurement `DueAt`, reassignment with a valid due date, postponement beyond warranty and duplicate retry.
- [ ] Resolve the warranty from the task route and project before creating or changing due dates.
- [ ] Return `survey_outside_warranty` with `field` set to `plannedAt`, `dueAt` or `newDueAt`.
- [ ] Keep the approved Crew reassignment behavior for `MEASURE_ONLY`. Crew A's old assignment remains immutable and blocked, Crew B owns new evidence, and no handover payload is required.
- [ ] Run the affected API and SQL lanes.
- [ ] Commit with `git commit -m "feat: validate survey task deadlines against warranty"`.

### Task 6: Validate capture time while accepting late synchronization

**Files:**
- Modify `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs`.
- Modify `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanningPersistenceService.cs`.
- Modify `RoadGuardSystem.Repositories/Implementations/Repairs/RoadCoverageRepository.cs`.
- Modify `tests/RoadGuardSystem.IntegrationTests/Surveys/P230SurveyDataValidationAdmissionTests.cs`.
- Modify `tests/RoadGuardSystem.IntegrationTests/Surveys/P230FlightSurveyFileSchemaTests.cs`.

- [ ] Add cases for capture time before the warranty, capture time after the exclusive end boundary and valid capture uploaded later.
- [ ] Validate capture time against the pinned warranty and route during admission. Keep upload time separate from capture time.
- [ ] Return `survey_outside_warranty` for invalid capture time while retaining upload idempotency and file integrity behavior.
- [ ] Run the P230 and road-coverage integration tests.
- [ ] Commit with `git commit -m "feat: validate survey capture time"`.

### Task 7: Update FE handoff and contract checks

**Files:**
- Create `deploy/fe-handoff/FE-WARRANTY-SCHEDULE-VALIDATION.md`.
- Modify `deploy/fe-handoff/FE-API-COVERAGE-MATRIX.md`.
- Modify `deploy/fe-handoff/FE-API-COOKBOOK.md`.
- Modify `deploy/fe-handoff/FE-SCREEN-API-MAPPING.md`.
- Modify `tests/RoadGuardSystem.ApiTests/Contracts/ApiContractInventoryTests.cs`.

- [ ] Document request and response examples without credentials or real identifiers.
- [ ] Document each error code, status, field value, Vietnamese display text and date boundary.
- [ ] Document that FE displays server-calculated `lastPlannedAt` and does not decide validity locally.
- [ ] Add contract assertions for recurrence enum values and error fields.
- [ ] Run source-contract and contract-inventory tests.
- [ ] Commit with `git commit -m "docs: publish warranty schedule validation contract"`.

### Task 8: Verify, self-review and gate release

**Files:**
- Review all files changed in Tasks 1–7.
- Create `docs/superpowers/reports/2026-11-28-roadguard-warranty-schedule-validation-verification.md`.

- [ ] Run focused unit, API and SQL lanes.
- [ ] Run `dotnet build` and `dotnet format --verify-no-changes --no-restore`.
- [ ] Run `git diff --check` and existing source and contract checks.
- [ ] Perform self-review pass one for business rules, old-route compatibility, time-zone boundaries and transaction safety.
- [ ] Perform self-review pass two for idempotency, FE error fields, migration compatibility and approved Crew reassignment behavior.
- [ ] Record PASS, FAIL and NOT RUN separately, including server deployment, Gmail delivery and real Mobile runtime.
- [ ] Push only after CI is green on the exact SHA. Do not deploy or seed from this plan without a separate approved server operation.

## Verification Matrix

| Area | Required cases |
| --- | --- |
| Warranty | Reversed dates, one-year boundary, two-year boundary, multiple applicable warranties |
| One-off schedule | Start before, end at boundary, end after boundary, invalid range |
| Weekly recurrence | One-year and two-year generation, last valid week, empty result |
| Monthly recurrence | Day 31, February, leap year, last valid month |
| Tasks | Operator due date, Crew measurement due date, reassignment, postponement |
| Offline | Valid capture with late upload, invalid capture timestamp, duplicate replay |
| API contract | Every error code, field, boundary date and correlation ID |
| Data safety | Atomic rejection, migration from current schema and no duplicate occurrence |
