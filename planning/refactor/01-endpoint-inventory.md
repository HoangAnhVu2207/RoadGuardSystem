# RF-01: HTTP endpoint inventory from source

## Fingerprint and method

- Branch `anh`; local HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; working tree dirty, including active survey source/test edits. This inventory describes the **local working tree**, not a clean remote commit.
- Source of truth for rows: `[Route]` + `[Http*]` attributes and action signatures in `RoadGuardSystem.API/Controllers/*Controller.cs`. All controllers declare `ApiVersion("1.0")`; below `/api/v1` is the concrete v1 substitution. `Program.cs` separately maps `/health`.
- Count: 17 production controllers, 57 action attributes, 57 rows below, plus `/health` = 58 HTTP surfaces. `tests/RoadGuardSystem.ApiTests/Controllers/ProbeController.cs` is test-only and excluded.
- `A` = `[Authorize]` (class or action); `P` = named policy; `Anon` = `[AllowAnonymous]`. Actual role/project decisions can occur in Services and are not fully expressed by the attribute.
- `K` = provisional KEEP; `N` = NEEDS-DECISION. All K rows have **medium** confidence: keep the observed public surface pending RF-02 business/consumer review. N rows have **high** confidence that a decision is needed, not that one route should be removed.
- `PM` means a matching method/path exists in the 84-request Postman collection; `-` means none. Matching normalized `{...:guid}` and `{{variable}}`, removed query strings, and matched HTTP method. This is static coverage, not a successful collection run.
- Response column lists success body type; `204`/`202 no body` and `file` are explicit. All error bodies should be compared to actual ProblemDetails, not inferred from DTO names.
- Non-controller endpoint: anonymous GET `/health` is mapped by `API/Program.cs` with `MapHealthChecks`; it has no request/response DTO, Service, Repository or entity. Postman has one preflight request. It is not counted among the 57 controller actions.

## Identity and authentication (19)

| # | Method/path | Controller.Action | Auth | Request -> success response | Seam | Test evidence / PM | Class |
|---:|---|---|---|---|---|---|---|
| 1 | POST `/auth/login` | `AuthController.Login` | Anon | `LoginRequestDto` -> `AuthTokenResponseDto` | IA | Auth API/unit / PM | K |
| 2 | POST `/auth/refresh` | `AuthController.Refresh` | Anon | `RefreshRequestDto` -> `AuthTokenResponseDto` | IA | Auth API/unit / PM | K |
| 3 | POST `/auth/forced-password-change` | `AuthController.ForcedPasswordChange` | Anon | `ForcedPasswordChangeRequestDto` -> `AuthTokenResponseDto` | IA | Auth API / PM | K |
| 4 | POST `/auth/logout` | `AuthController.Logout` | A | header key -> 204 | IA | Auth API / PM | K |
| 5 | POST `/auth/password-recovery-requests` | `AuthController.RequestPasswordRecovery` | Anon | `PasswordRecoveryRequestDto` -> 202 no body | IA | Auth API / PM | K |
| 6 | POST `/auth/change-password` | `AuthController.ChangePassword` | A | `ChangePasswordRequestDto` -> 204 | IA | Auth API / PM | K |
| 7 | POST `/auth/reporter-registrations` | `ReporterRegistrationsController.Register` | Anon | `RegisterReporterRequestDto` -> `RegistrationIntentResponseDto` | IO | Onboarding API/SQL / PM | K |
| 8 | POST `/auth/reporter-registrations/verify` | `ReporterRegistrationsController.Verify` | Anon | `VerifyReporterOtpRequestDto` -> `AuthTokenResponseDto` | IO | Onboarding API/SQL / PM | K |
| 9 | POST `/auth/reporter-registrations/resend` | `ReporterRegistrationsController.Resend` | Anon | `ResendReporterOtpRequestDto` -> `RegistrationIntentResponseDto` | IO | Onboarding API/SQL / PM | K |
| 10 | POST `/invitations` | `InvitationsController.Create` | A | `CreateInvitationRequestDto` -> `InvitationResponseDto` | IO | Onboarding API/SQL / PM | K |
| 11 | POST `/invitations/accept` | `InvitationsController.Accept` | Anon | `AcceptInvitationRequestDto` -> `AuthTokenResponseDto` | IO | Onboarding API/SQL / PM | K |
| 12 | GET `/profile` | `ProfileController.Get` | A | none -> `ProfileResponseDto` | IP | Profile API/SQL / PM | N |
| 13 | PUT `/profile` | `ProfileController.Update` | A | `ProfileUpdateRequestDto` -> `ProfileResponseDto` | IP | Profile API/SQL / PM | N |
| 14 | GET `/me` | `MeController.Get` | A | none -> `ActorResponseDto` | IV | V2 identity API / PM | N |
| 15 | PATCH `/me` | `MeController.Update` | A | `UpdateMeRequestDto` + key/If-Match -> `ActorResponseDto` | IV | V2 identity API / PM | N |
| 16 | POST `/admin/users/{userId}/password-reset` | `AdminUsersController.ResetPassword` | A | `AdminPasswordResetRequestDto` -> `AdminPasswordResetResponseDto` | IP | Profile/admin API / PM | N |
| 17 | GET `/users/{userId}` | `UsersController.Get` | A | none -> `ActorResponseDto` | IV | V2 account API / PM | K |
| 18 | PATCH `/users/{userId}` | `UsersController.Update` | A | `AccountUpdateRequestDto` + key/If-Match -> `ActorResponseDto` | IV | V2 account API / PM | K |
| 19 | POST `/users/{userId}/password-reset` | `UsersController.ResetPassword` | A | `AdminResetPasswordV2RequestDto` -> 204 | IV | V2 account API / PM | N |

## Project, road, warranty, inspection (8)

| # | Method/path | Controller.Action | Auth | Request -> success response | Seam | Test evidence / PM | Class |
|---:|---|---|---|---|---|---|---|
| 20 | POST `/projects` | `ProjectsController.Create` | A | `CreateProjectRequestDto` -> `CreateProjectResponseDto` | PC | Project API / PM | K |
| 21 | PUT `/projects/{projectId}` | `ProjectsController.Update` | A | `UpdateProjectRequestDto` -> `UpdateProjectResponseDto` | PU | Project API / - | K |
| 22 | PUT `/projects/{projectId}/primary-project-manager` | `ProjectsController.ReassignPrimaryProjectManager` | A | `ReassignPrimaryProjectManagerRequestDto` -> corresponding response DTO | PMG | Project API/SQL / - | K |
| 23 | POST `/projects/{projectId}/road-sections` | `ProjectRoadSectionsController.Create` | A | `CreateRoadSectionRequestDto` -> `RoadSectionVersionResponseDto` | PR | Road API / - | K |
| 24 | POST `/projects/{projectId}/road-sections/{roadSectionId}/versions` | `ProjectRoadSectionsController.CreateVersion` | A | `CreateRoadSectionVersionRequestDto` -> `RoadSectionVersionResponseDto` | PR | Road API / - | K |
| 25 | POST `/projects/{projectId}/warranties` | `ProjectWarrantiesController.Create` | A | `CreateWarrantyRequestDto` -> `CreateWarrantyResponseDto` | PW | Warranty API / - | K |
| 26 | GET `/projects/{projectId}/work-package` | `ProjectWorkPackagesController.Get` | P: `WorkPackageRead` | none -> `ProjectWorkPackageResponseDto` | PWP | Authorization API/SQL / PM | K |
| 27 | GET `/me/inspection-tasks` | `InspectionTasksController.List` | A | cursor/limit -> `InspectionTaskPageResponseDto` | IQ | Inspection API/unit / PM | K |

## Survey/planning (15)

| # | Method/path | Controller.Action | Auth | Request -> success response | Seam | Test evidence / PM | Class |
|---:|---|---|---|---|---|---|---|
| 28 | POST `/projects/{projectId}/road-sections/{roadSectionId}/survey-plans` | `SurveyPlanningController.CreatePlan` | A | `CreateSurveyPlanRequestDto` -> `CreateSurveyPlanResponseDto` | SP | Legacy planning API/SQL / PM | N |
| 29 | POST `/projects/{projectId}/road-sections/{roadSectionId}/survey-requests` | `SurveyPlanningController.CreateRequest` | A | `CreateSurveyRequestRequestDto` -> `CreateSurveyRequestResponseDto` | SP | Legacy planning API/SQL / PM | N |
| 30 | POST `/projects/{projectId}/survey-plans/{surveyPlanId}/postpone` | `SurveyPlanningController.PostponePlan` | A | `PostponeSurveyPlanRequestDto` -> `PostponeSurveyPlanResponseDto` | SP | Legacy planning API/SQL / PM | N |
| 31 | POST `/projects/{projectId}/survey-plans` | `SurveyV2Controller.CreatePlan` | A | `CreateSurveyPlanV2RequestDto` + key -> `SurveyPlanV2ResponseDto` | SV | V2 survey API/SQL / PM | N |
| 32 | POST `/survey-plans/{planId}/postpone` | `SurveyV2Controller.Postpone` | A | `PostponeSurveyPlanV2RequestDto` + key/If-Match -> `SurveyPlanV2ResponseDto` | SV | V2 survey API/SQL / PM | N |
| 33 | POST `/projects/{projectId}/survey-tasks` | `SurveyV2Controller.CreateTask` | A | `CreateSurveyTaskV2RequestDto` + key -> `SurveyTaskV2ResponseDto` | SV | V2 survey API/SQL / PM | K |
| 34 | GET `/survey-tasks/{taskId}` | `SurveyV2Controller.GetTask` | A | none -> `SurveyTaskV2ResponseDto` | SV | V2 survey API/SQL / PM | K |
| 35 | GET `/me/survey-tasks` | `SurveyV2Controller.ListMyTasks` | A | cursor/limit -> `SurveyTaskPageV2ResponseDto` | SV | V2 survey API/SQL / PM | K |
| 36 | POST `/survey-tasks/{taskId}/datasets` | `SurveyV2Controller.SubmitDataset` | A | `SubmitDatasetRequestDto` + key/If-Match -> `DatasetResponseDto` | SV | V2 survey SQL dirty / PM | K |
| 37 | GET `/datasets/{datasetId}/coverage` | `SurveyV2Controller.GetDatasetCoverage` | A | none -> `DatasetCoverageResponseDto` | SV | V2 survey SQL dirty / PM | K |
| 38 | POST `/survey-tasks/{taskId}/accept` | `SurveyV2Controller.AcceptTask` | A | key/If-Match -> `SurveyTaskV2ResponseDto` | SV | V2 survey API/SQL / PM | K |
| 39 | POST `/survey-tasks/{taskId}/decline` | `SurveyV2Controller.DeclineTask` | A | `SurveyTaskReasonV2RequestDto` + headers -> `SurveyTaskV2ResponseDto` | SV | V2 survey API/SQL / PM | K |
| 40 | POST `/survey-tasks/{taskId}/cancel` | `SurveyV2Controller.CancelTask` | A | `SurveyTaskReasonV2RequestDto` + headers -> `SurveyTaskV2ResponseDto` | SV | V2 survey API/SQL / PM | K |
| 41 | POST `/survey-tasks/{taskId}/reassign` | `SurveyV2Controller.ReassignTask` | A | `ReassignSurveyTaskV2RequestDto` + headers -> `SurveyTaskV2ResponseDto` | SV | V2 survey API/SQL / PM | K |
| 42 | POST `/survey-tasks/{taskId}/supplements` | `SurveyV2Controller.RequestSupplement` | A | `SupplementSurveyTaskV2RequestDto` + headers -> `SurveyTaskV2ResponseDto` | SV | V2 survey API/SQL / PM | K |

## Uploads, processing, notifications (15)

| # | Method/path | Controller.Action | Auth | Request -> success response | Seam | Test evidence / PM | Class |
|---:|---|---|---|---|---|---|---|
| 43 | POST `/uploads` | `UploadsController.Create` | A | `UploadCreateRequestDto` + key -> `UploadSessionResponseDto` | UP | Upload API/SQL / PM | K |
| 44 | GET `/uploads/{uploadId}` | `UploadsController.GetSession` | A | none -> `UploadSessionResponseDto` | UP | Upload API/SQL / PM | K |
| 45 | POST `/uploads/{uploadId}/part-urls` | `UploadsController.GetPartUrls` | A | `UploadPartUrlsRequestDto` + key -> `UploadPartUrlsResponseDto` | UP | Upload API/SQL / PM | K |
| 46 | POST `/uploads/{uploadId}/complete` | `UploadsController.Complete` | A | `UploadCompleteRequestDto` + key/If-Match -> `UploadSessionResponseDto` | UP | Upload API/SQL / PM | K |
| 47 | GET `/files/{fileId}` | `UploadsController.GetFileMetadata` | A | none -> `FileMetadataResponseDto` | UP | Upload API/SQL / PM | K |
| 48 | GET `/files/{fileId}/content` | `UploadsController.Download` | A | none -> file body | UP | Upload API/SQL / PM | K |
| 49 | POST `/processing-jobs` | `ProcessingV2Controller.CreateProcessingJob` | A | `CreateProcessingJobRequestDto` + key -> `ProcessingJobResponseDto` | PV | Processing SQL/unit / PM | K |
| 50 | GET `/processing-jobs/{jobId}` | `ProcessingV2Controller.GetProcessingJob` | A | none -> `ProcessingJobResponseDto` | PV | Processing SQL/unit / PM | K |
| 51 | POST `/processing-jobs/{jobId}/retry` | `ProcessingV2Controller.RetryProcessingJob` | A | `RetryProcessingJobRequestDto` + headers -> `ProcessingJobResponseDto` | PV | Processing SQL/unit / PM | K |
| 52 | POST `/internal/processing-jobs/{jobId}/results` | `ProcessingV2Controller.ReceiveAiResult` | P: `AiCallback` | `ReceiveAiResultRequestDto` + key -> `ProcessingJobResponseDto` | PV | Processing SQL/unit / PM | K |
| 53 | POST `/projects/{projectId}/validation-runs` | `ProcessingV2Controller.CreateValidationRun` | A | `CreateValidationRunRequestDto` + key -> `ProcessingJobResponseDto` | PV | Validation SQL/unit / PM | K |
| 54 | GET `/validation-runs/{runId}` | `ProcessingV2Controller.GetValidationResult` | A | none -> `ValidationResultDto` | PV | Validation SQL/unit / PM | K |
| 55 | GET `/notifications/{notificationId}` | `NotificationsController.Get` | A | none -> `NotificationDto` | NT | Notification API/SQL / PM | K |
| 56 | GET `/notifications` | `NotificationsController.List` | A | cursor/limit -> `NotificationPageDto` | NT | Notification API/SQL / PM | K |
| 57 | POST `/notifications/{notificationId}/read` | `NotificationsController.MarkRead` | A | key/If-Match -> `NotificationDto` | NT | Notification API/SQL / PM | K |

## Seam and entity key

The named service is injected by each controller; the repository below is the directly related interface in its Service implementation/DI. Entity list is the main durable model, not an exhaustive SQL join list. Source: `API/Extensions/ServiceCollectionExtensions.cs`, `Services/Extensions/AuthenticationServiceCollectionExtensions.cs`, `Repositories/Extensions/RoadGuardPersistenceExtensions.cs`, the listed implementations, and `RoadGuardDbContext.cs`.

| Key | Service -> repository | Main entities |
|---|---|---|
| IA | `IAuthService`/`AuthService` -> `IIdentityRepository`/`IdentityRepository` | `ApplicationUser`, `UserSession`, `RefreshToken`, `PasswordRecoveryRequest`, audit/security logs |
| IO | `IIdentityOnboardingService`/`IdentityOnboardingService` -> `IIdentityOnboardingRepository`/`IdentityOnboardingRepository`, `IIdentityRepository` | `ReporterRegistrationIntent`, `StaffInvitation`, `StaffInvitationProject`, `ApplicationUser`, sessions |
| IP | `IIdentityService`/`IdentityService` -> `IIdentityRepository`/`IdentityRepository` | `ApplicationUser`, idempotency/audit/password reset logs |
| IV | `IIdentityV2Service`/`IdentityV2Service` -> `IIdentityRepository` + `IIdentityV2Repository` (both `IdentityRepository`) | same user/session/password model as IP |
| PC/PU/PMG | `IProjectCreationService`/`IProjectUpdateService`/`IPrimaryProjectManagerService` -> matching project repository interfaces | `Project`, `ProjectMember`, audit/idempotency |
| PR/PW/PWP | `IRoadSectionVersionService`/`IWarrantyCreationService`/`IProjectWorkPackageService` -> matching repository interfaces | `RoadSection`, `RoadSectionVersion`, `Warranty`, `HandoverDocument`, `ProjectMember` |
| IQ | `IInspectionTaskQueryService` -> `IInspectionTaskReadRepository` | `FieldInspectionTask`, assignment/project membership |
| SP | `ISurveyPlanningService` -> `ISurveyPlanningRepository`/`SurveyPlanningPersistenceService` | `SurveyPlan`, `SurveyRequest`, `SurveyPlanPostponement`, road/project |
| SV | `ISurveyV2Service` -> `ISurveyV2Repository`/`SurveyV2PersistenceService` | same `SurveyPlan`/`SurveyRequest` plus scopes/assignments, `Survey`, `SurveyDataVersion`, files |
| UP | `IUploadService` -> `IUploadRepository`/`UploadPersistenceService` + `IUploadObjectStorage` | `UploadSession`, `UploadPart`, `StoredFile`, `FileScope` |
| PV | `IProcessingV2Service` -> `IProcessingV2Repository`/`ProcessingV2PersistenceService` | `ProcessingJob`, `ProcessingAttempt`, `AIModelVersion`, `ValidationRun`, derived measurements |
| NT | `INotificationService` -> `INotificationRepository`/`NotificationPersistenceService` | `Notification`, idempotency/outbox |

`IProjectScopeGuard`/`IProjectMembershipRepository` is an additional scope dependency of SV, PV, UP, and several project flows. Authentication middleware also reads authoritative sessions. The table does not imply that every entity named is loaded by every action.

## Test evidence key (source presence, not pass verdict)

| Inventory label | Concrete test files |
|---|---|
| Auth API/unit | `ApiTests/Authentication/AuthenticationFlowTests.cs`, `AuthenticationSessionFlowTests.cs`, `V2AuthenticationFlowTests.cs`; `UnitTests/Authentication/AuthServiceTests.cs`, `AuthServiceV2Tests.cs` |
| Onboarding API/SQL | `ApiTests/Authentication/V2IdentityOnboardingFlowTests.cs`; `IntegrationTests/Identity/IdentityOnboardingPersistenceTests.cs` |
| Profile/admin API/SQL | `ApiTests/Identity/P111ProfileReadTests.cs`, `P111ProfileUpdateTests.cs`, `P111PasswordResetTests.cs`; `IntegrationTests/Identity/P111ProfileUpdatePersistenceTests.cs`, `P111PasswordResetPersistenceTests.cs` |
| V2 identity/account API | `ApiTests/Identity/V2IdentityEndpointContractTests.cs`, `V2AccountFlowTests.cs`; `ApiTests/Authentication/V2AuthenticationFlowTests.cs` |
| Project/Road/Warranty API/SQL | `ApiTests/Projects/P120ProjectCreationTests.cs`, `P120ProjectUpdateTests.cs`, `P121RoadSectionVersionTests.cs`, `ApiTests/Warranties/P120WarrantyCreationTests.cs`; `IntegrationTests/Projects/P220ProjectMembershipSchemaTests.cs`, `P221RoadWarrantySchemaTests.cs` |
| Authorization API/SQL | `ApiTests/Authorization/P112ProjectAuthorizationTests.cs`; `IntegrationTests/Projects/P211ProjectMembershipReadModelTests.cs` |
| Inspection API/unit | `ApiTests/Inspections/V2P1063InspectionTaskListTests.cs`; `UnitTests/Inspections/InspectionTaskQueryServiceTests.cs` |
| Legacy planning API/SQL | `ApiTests/Surveys/P122SurveyPlanningTests.cs`; `IntegrationTests/Surveys/P122SurveyPlanningPersistenceTests.cs` |
| V2 survey API/SQL | `ApiTests/Surveys/P2SurveyV2ApiTests.cs`; `UnitTests/Surveys/P2V2SurveyServiceTests.cs`; `IntegrationTests/Surveys/P2V2SurveyScopeConcurrencyTests.cs` (dirty local test file) |
| Upload API/SQL | `ApiTests/Files/UploadApiTests.cs`; `IntegrationTests/Files/UploadPersistenceSqlTests.cs` |
| Processing/Validation SQL/unit | `IntegrationTests/Processing/P231ProcessingPersistenceTests.cs`, `P234ValidationRunContractTests.cs`; `UnitTests/Processing/P234ValidationAuthorizationTests.cs` |
| Notification API/SQL | `ApiTests/Notifications/P2NotificationApiTests.cs`; `IntegrationTests/Notifications/P207NotificationPersistenceTests.cs`; `UnitTests/Notifications/NotificationServiceTests.cs` |

For each listed path, expand its leading `ApiTests/`, `IntegrationTests/`, or `UnitTests/` to `tests/RoadGuardSystem.ApiTests/`, `tests/RoadGuardSystem.IntegrationTests/`, or `tests/RoadGuardSystem.UnitTests/` respectively. A test file may cover only a subset of the group's actions; no per-action executed/pass claim is made. Runtime API tests remain blocked by RF-00's test-host seed collision.

## Count and coverage reconciliation

- Controller attribute count by source: AdminUsers 1, Auth 6, InspectionTasks 1, Invitations 2, Me 2, Notifications 3, ProcessingV2 6, Profile 2, ProjectRoadSections 2, Projects 3, ProjectWarranties 1, ProjectWorkPackages 1, ReporterRegistrations 3, SurveyPlanning 3, SurveyV2 12, Uploads 6, Users 3 = **57**. Tables: identity 19 + project 8 + survey 15 + upload/processing/notification 15 = **57**.
- Static Postman method/path match = **52/57** actions; five absent are PUT project, PUT primary PM, POST road section, POST road section version, POST warranty. All five have examples in `RoadGuardSystem.API/RoadGuardSystem.API.http` and related API tests. Postman has 84 requests because paths have multiple positive/negative/fixture scenarios; 84 is not endpoint count.
- `docs/diagram/V2/05_Technical/openapi.yaml` has 138 `operationId:` lines; `planning/V2/task_manifest.json` has 133 tasks. Neither number proves implementation. RF-02 must reconcile operation-by-operation, including documented `syncOperations` with no production controller action found.
- Existing test files are named in seam groups above. Presence is static evidence only. RF-00 API platform group failed at test-host seeding before HTTP assertions; no endpoint behavior was freshly verified in RF-01.
- External consumers (mobile/web clients, deployed AI service, integrations, bookmarks, third-party scripts) were not accessible and remain `UNKNOWN`. No endpoint is a removal decision from repository caller search alone.
