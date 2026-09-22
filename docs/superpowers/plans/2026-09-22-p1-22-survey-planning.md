# P1-22 Survey Planning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expose authorized API use cases for creating survey plans, creating survey requests, and postponing plans against the completed P2-22 persistence contract.

**Architecture:** Keep the accepted `Controller -> IService -> IRepository` flow. Services validate actor/project scope and map repository facts/statuses to DTOs and stable service results; the existing `ISurveyPlanningRepository` remains the only persistence seam. Controllers bind requests, extract authenticated identity, and serialize response DTOs or ProblemDetails.

**Tech Stack:** ASP.NET Core 8 MVC controllers, DTO records with validation attributes, existing Service/Repository interfaces, SQL Server-backed integration fixtures, xUnit API tests.

**Spec:** `planning/RoadGuard_Plan_Person_1.md` row `P1-22`, `docs/adr/001-backend-boundary.md`, `docs/adr/004-n-layer-backend-structure.md`, and `docs/api-errors.md`.

## Global Constraints

- Preserve `Controller -> IService -> IRepository`; Services must not reference EF Core, `RoadGuardDbContext`, MVC result types, or `HttpContext`.
- Do not modify `RoadGuardSystem.Repositories` migrations, mappings, `ISurveyPlanningRepository`, or `SurveyPlanningPersistenceService`; those are the completed P2-22 handoff owned by Huy.
- Use one public type per new file and keep modified files below 500 lines.
- Use UTC `DateTimeOffset`, `operationId` plus idempotency input, correlation IDs, and RFC 7807 ProblemDetails with lowercase stable error codes.
- A road-section-version anchor is required by the P1-22 acceptance criteria. The current P2-22 persistence request carries `RoadSectionId` but no version id; do not silently invent a version rule. Record this as a dependency decision before implementation and request a Huy contract update if the authoritative handoff requires a version id.

---

### Task 1: Freeze the endpoint contracts and dependency decision

**Files:**
- Modify: `docs/superpowers/plans/2026-09-22-p1-22-survey-planning.md`
- Inspect: `RoadGuardSystem.Repositories/Interfaces/Surveys/ISurveyPlanningRepository.cs`
- Inspect: `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyPlanningPersistenceService.cs`
- Inspect: `RoadGuardSystem.BusinessObjects/Surveys/SurveyPlan.cs`
- Inspect: `RoadGuardSystem.BusinessObjects/Surveys/SurveyRequest.cs`

**Interfaces:**
- Consumes: `CreatePlanAsync`, `CreateRequestAsync`, `PostponePlanAsync` and their persistence status/result/view records.
- Produces: approved routes, actor/scope rules, request/response field names, and an explicit version-anchor decision.

- [ ] **Step 1: Confirm the contracts below against the P2-22 handoff.**

  - `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans`: authenticated Project Manager for the project; body contains `plannedStartAt`, `plannedEndAt`, `surveyType`, `outputRequirements`, `operationId`; returns `201` with plan id, scope, dates, type, status and output requirements.
  - `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-requests`: authenticated Project Manager; body contains optional `surveyPlanId`, `surveyType`, optional `dueAt`, `outputRequirements`, `operationId`; returns `201` with request id, status `new_assigned`, requested/due timestamps and output requirements.
  - `POST /api/v1/projects/{projectId}/survey-plans/{surveyPlanId}/postpone`: authenticated Project Manager; body contains optional `newPlannedStartAt`, non-empty `reason`, `operationId`; returns `200` with plan id, status `postponed`, new start and postponement id.

- [ ] **Step 2: Resolve version anchoring before adding DTOs.** If P2-22 has no authoritative road-section-version field, keep API validation limited to project/road-section scope and record the missing-version acceptance gap for owner review; do not add a repository or migration in this task.

- [ ] **Step 3: Define stable mappings.** Map forbidden actor to `403 access_forbidden`, missing project/road section/plan to `404 survey_*_not_found`, scope mismatch to `403 project_access_denied` or the existing project-scope code, invalid domain input to `422 survey_validation_failed`, idempotency conflict to `409 duplicate_request`, and malformed model input to `400 validation_error`.

### Task 2: Add DTOs and the survey-planning service seam

**Files:**
- Create: `RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanRequestDto.cs`
- Create: `RoadGuardSystem.DTOs/Surveys/CreateSurveyPlanResponseDto.cs`
- Create: `RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestRequestDto.cs`
- Create: `RoadGuardSystem.DTOs/Surveys/CreateSurveyRequestResponseDto.cs`
- Create: `RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanRequestDto.cs`
- Create: `RoadGuardSystem.DTOs/Surveys/PostponeSurveyPlanResponseDto.cs`
- Create: `RoadGuardSystem.Services/Interfaces/Surveys/ISurveyPlanningService.cs`
- Create: `RoadGuardSystem.Services/Implementations/Surveys/SurveyPlanningServiceCommand.cs`
- Create: `RoadGuardSystem.Services/Implementations/Surveys/SurveyPlanningServiceResult.cs`
- Create: `RoadGuardSystem.Services/Implementations/Surveys/SurveyPlanningService.cs`
- Modify: `RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: authenticated actor id/role, project/road-section route ids, DTO values, and `ISurveyPlanningRepository`.
- Produces: service methods `CreatePlanAsync`, `CreateRequestAsync`, `PostponePlanAsync` returning explicit service status plus DTO-ready views.

- [ ] **Step 1: Add validation-only DTOs.** Require `operationId`; require non-empty JSON `outputRequirements` through model validation or service validation; require plan end not before start; require postponement reason; permit nullable due/start values only where the persistence contract permits them.
- [ ] **Step 2: Add command/result types with actor id, actor role, route scope, correlation id and DTO values.** Keep HTTP types out of Services.
- [ ] **Step 3: Implement service authorization and mapping.** Allow only active Project Manager scope according to the existing project authorization seam; reject invalid role/scope before repository invocation; map each repository status without exposing EF or HTTP types.
- [ ] **Step 4: Register `ISurveyPlanningService` and ensure the existing repository registration is consumed through its interface.** Do not register a second persistence implementation.
- [ ] **Step 5: Build `RoadGuardSystem.Services` and `RoadGuardSystem.DTOs` with `-nologo -v q -clp:ErrorsOnly`.**

### Task 3: Add thin API controller endpoints

**Files:**
- Create: `RoadGuardSystem.API/Controllers/SurveyPlanningController.cs`
- Modify: `RoadGuardSystem.API/Constants/ApiErrorCodes.cs`
- Modify: `RoadGuardSystem.API/RoadGuardSystem.API.http`

**Interfaces:**
- Consumes: `ISurveyPlanningService`, route ids, authenticated claims, correlation middleware item, and request DTOs.
- Produces: the three routes from Task 1 with `201`/`200` success responses and RFC 7807 failures.

- [ ] **Step 1: Implement claim extraction and `401 auth_unauthorized` handling consistent with existing controllers.**
- [ ] **Step 2: Implement plan and request `POST` actions.** Call exactly one service method per action, return `Created` with a stable resource location, and map service statuses to the approved error codes.
- [ ] **Step 3: Implement postpone `POST` action.** Return `Ok` for success/replay and `409 invalid_state_transition` for completed/cancelled plans or other state conflicts.
- [ ] **Step 4: Add API examples for all three requests, including operation ids, JSON output requirements, due date and postpone reason.**
- [ ] **Step 5: Build `RoadGuardSystem.API` and run the relevant `.http` smoke requests against the configured SQL-backed environment.**

### Task 4: Turn the RED API contract into focused evidence

**Files:**
- Create or update: `tests/RoadGuardSystem.ApiTests/Surveys/P122SurveyPlanningTests.cs`
- Inspect: `tests/RoadGuardSystem.IntegrationTests/Surveys/P122SurveyPlanningPersistenceTests.cs`

**Interfaces:**
- Consumes: the three API routes and the existing P2-22 persistence fixture/evidence.
- Produces: focused P1-22 API coverage for positive creation/replay and negative authorization/validation/state cases.

- [ ] **Step 1: Preserve the existing RED assertions for plan creation, request creation and postpone; verify they fail with `404` before the controller exists.**
- [ ] **Step 2: Add the smallest positive assertions for `201`/`200`, response ids/statuses, `DueAt` and `OutputRequirements`, and replay behavior.**
- [ ] **Step 3: Add negative assertions for unauthenticated caller, non-PM caller, empty scope, invalid dates/JSON, missing postpone reason, and invalid postponed state.**
- [ ] **Step 4: Build the changed API/test projects, then run one selected breadth: focused P1-22 API tests if the test host is self-contained; otherwise the affected API test project. Do not run both as routine sequence.**
- [ ] **Step 5: Re-run the `.http` smoke request and record status/body plus persisted audit/idempotency effect in the P1-22 worklog.**

## Self-review checklist

- [ ] No task changes Huy-owned repository/schema files.
- [ ] Every route has actor, input, success, stable errors, business transition, persistence/idempotency and audit behavior defined.
- [ ] The missing road-section-version anchor is explicitly resolved or reported, never silently guessed.
- [ ] DTOs contain no business logic; Services contain no HTTP/EF dependencies; Controllers contain no repository calls.
- [ ] Test breadth is selected once from known impact and zero discovered tests is not accepted as a pass.
