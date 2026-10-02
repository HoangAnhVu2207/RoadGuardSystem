# RF-04: Operation crosswalk (draft)

Generated from checked-in controllers, RF-01 inventory, parsed V2 OpenAPI, Postman and API.http by `tools/build_rf04_crosswalk.py`.
`CURRENT_SOURCE_OBSERVED` is static source evidence; `PROPOSED_DRAFT` is not an active contract. JSON carries DTO, Service/Repository, test, Postman and consumer fields for every row.

- Source: `anh` at `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; dirty=True; SHA-256 fingerprints in JSON.
- Current: 57 controller actions, 57 route attributes, 1 health route(s); 17 controllers.
- Draft: 133 parsed operations and 133 unique operation IDs.
- Delta from historical 57/58/133: actions +0, current routes +0, draft operations +0.
- These are two sets of rows linked by route candidates, not a count of distinct implemented APIs. Matching method/path does not establish DTO, authorization, headers, status or behavior equivalence.
- Current controller raw template, route prefix, declared API version, normalized path and normalization rule are in JSON. A raw exact match requires identical path text; prefix/constraint removal is `normalized`; parameter-name equivalence is `parameter_alias`; multiple candidates are `ambiguous`. Explicit different API major versions are incompatible with controller declarations.
- `x-fr` is a historical trace hint. Each draft row has requirement links marked PRIMARY or CONTEXT with an explicit cross-module reason; the operation has one proposed task owner. No link proves requirement approval.
- Draft operations matching current source: 44; current actions with draft match: 44.
- Postman requests parsed: 84. Presence is a static example only; external web/Android/AI consumers are unknown.

## Current source to draft

| ID | Method and path | Controller/action | Draft operation | Source disposition | Test / Postman | Task/owner |
|---|---|---|---|---|---|---|
| C001 | POST `/auth/login` | `AuthController.Login` | login | COMPARE_WIRE_AND_KEEP_CURRENT | Auth API/unit; PM 7 | RF-10-01/A |
| C002 | POST `/auth/refresh` | `AuthController.Refresh` | refreshTokens | COMPARE_WIRE_AND_KEEP_CURRENT | Auth API/unit; PM 1 | RF-10-01/A |
| C003 | POST `/auth/forced-password-change` | `AuthController.ForcedPasswordChange` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Auth API; PM 1 | RF-10-01/A |
| C004 | POST `/auth/logout` | `AuthController.Logout` | logout | COMPARE_WIRE_AND_KEEP_CURRENT | Auth API; PM 1 | RF-10-01/A |
| C005 | POST `/auth/password-recovery-requests` | `AuthController.RequestPasswordRecovery` | requestPasswordRecovery | COMPARE_WIRE_AND_KEEP_CURRENT | Auth API; PM 1 | RF-10-01/A |
| C006 | POST `/auth/change-password` | `AuthController.ChangePassword` | changePassword | COMPARE_WIRE_AND_KEEP_CURRENT | Auth API; PM 1 | RF-10-01/A |
| C007 | POST `/auth/reporter-registrations` | `ReporterRegistrationsController.Register` | registerReporter | COMPARE_WIRE_AND_KEEP_CURRENT | Onboarding API/SQL; PM 3 | RF-10-01/A |
| C008 | POST `/auth/reporter-registrations/verify` | `ReporterRegistrationsController.Verify` | verifyReporterOtp | COMPARE_WIRE_AND_KEEP_CURRENT | Onboarding API/SQL; PM 3 | RF-10-01/A |
| C009 | POST `/auth/reporter-registrations/resend` | `ReporterRegistrationsController.Resend` | resendReporterOtp | COMPARE_WIRE_AND_KEEP_CURRENT | Onboarding API/SQL; PM 1 | RF-10-01/A |
| C010 | POST `/invitations` | `InvitationsController.Create` | createInvitation | COMPARE_WIRE_AND_KEEP_CURRENT | Onboarding API/SQL; PM 2 | RF-10-01/A |
| C011 | POST `/invitations/accept` | `InvitationsController.Accept` | acceptInvitation | COMPARE_WIRE_AND_KEEP_CURRENT | Onboarding API/SQL; PM 1 | RF-10-01/A |
| C012 | GET `/profile` | `ProfileController.Get` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Profile API/SQL; PM 2 | RF-10-01/A |
| C013 | PUT `/profile` | `ProfileController.Update` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Profile API/SQL; PM 1 | RF-10-01/A |
| C014 | GET `/me` | `MeController.Get` | getMe | COMPARE_WIRE_AND_KEEP_CURRENT | V2 identity API; PM 2 | RF-10-01/A |
| C015 | PATCH `/me` | `MeController.Update` | updateMe | COMPARE_WIRE_AND_KEEP_CURRENT | V2 identity API; PM 2 | RF-10-01/A |
| C016 | POST `/admin/users/{userId}/password-reset` | `AdminUsersController.ResetPassword` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Profile/admin API; PM 1 | RF-10-01/A |
| C017 | GET `/users/{userId}` | `UsersController.Get` | getAccount | COMPARE_WIRE_AND_KEEP_CURRENT | V2 account API; PM 2 | RF-10-01/A |
| C018 | PATCH `/users/{userId}` | `UsersController.Update` | updateAccount | COMPARE_WIRE_AND_KEEP_CURRENT | V2 account API; PM 1 | RF-10-01/A |
| C019 | POST `/users/{userId}/password-reset` | `UsersController.ResetPassword` | adminResetPassword | COMPARE_WIRE_AND_KEEP_CURRENT | V2 account API; PM 1 | RF-10-01/A |
| C020 | POST `/projects` | `ProjectsController.Create` | createProject | COMPARE_WIRE_AND_KEEP_CURRENT | Project API; PM 1 | RF-10-02/A |
| C021 | PUT `/projects/{projectId}` | `ProjectsController.Update` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Project API; PM 0 | RF-10-02/A |
| C022 | PUT `/projects/{projectId}/primary-project-manager` | `ProjectsController.ReassignPrimaryProjectManager` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Project API/SQL; PM 0 | RF-10-02/A |
| C023 | POST `/projects/{projectId}/road-sections` | `ProjectRoadSectionsController.Create` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Road API; PM 0 | RF-10-02/A |
| C024 | POST `/projects/{projectId}/road-sections/{roadSectionId}/versions` | `ProjectRoadSectionsController.CreateVersion` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Road API; PM 0 | RF-10-02/A |
| C025 | POST `/projects/{projectId}/warranties` | `ProjectWarrantiesController.Create` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Warranty API; PM 0 | RF-10-02/A |
| C026 | GET `/projects/{projectId}/work-package` | `ProjectWorkPackagesController.Get` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Authorization API/SQL; PM 1 | RF-10-02/A |
| C027 | GET `/me/inspection-tasks` | `InspectionTasksController.List` | listMyInspectionTasks | COMPARE_WIRE_AND_KEEP_CURRENT | Inspection API/unit; PM 4 | RF-10-07/A |
| C028 | POST `/projects/{projectId}/road-sections/{roadSectionId}/survey-plans` | `SurveyPlanningController.CreatePlan` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Legacy planning API/SQL; PM 3 | RF-10-03/B |
| C029 | POST `/projects/{projectId}/road-sections/{roadSectionId}/survey-requests` | `SurveyPlanningController.CreateRequest` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Legacy planning API/SQL; PM 1 | RF-10-03/B |
| C030 | POST `/projects/{projectId}/survey-plans/{surveyPlanId}/postpone` | `SurveyPlanningController.PostponePlan` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | Legacy planning API/SQL; PM 1 | RF-10-03/B |
| C031 | POST `/projects/{projectId}/survey-plans` | `SurveyV2Controller.CreatePlan` | createSurveyPlan | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 2 | RF-10-03/B |
| C032 | POST `/survey-plans/{planId}/postpone` | `SurveyV2Controller.Postpone` | postponeSurveyPlan | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 2 | RF-10-03/B |
| C033 | POST `/projects/{projectId}/survey-tasks` | `SurveyV2Controller.CreateTask` | createSurveyTask | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 2 | RF-10-03/B |
| C034 | GET `/survey-tasks/{taskId}` | `SurveyV2Controller.GetTask` | getSurveyTask | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 1 | RF-10-03/B |
| C035 | GET `/me/survey-tasks` | `SurveyV2Controller.ListMyTasks` | listMySurveyTasks | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 2 | RF-10-03/B |
| C036 | POST `/survey-tasks/{taskId}/datasets` | `SurveyV2Controller.SubmitDataset` | submitDataset | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey SQL dirty; PM 1 | RF-10-03/B |
| C037 | GET `/datasets/{datasetId}/coverage` | `SurveyV2Controller.GetDatasetCoverage` | getDatasetCoverage | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey SQL dirty; PM 1 | RF-10-03/B |
| C038 | POST `/survey-tasks/{taskId}/accept` | `SurveyV2Controller.AcceptTask` | acceptSurveyTask | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 2 | RF-10-03/B |
| C039 | POST `/survey-tasks/{taskId}/decline` | `SurveyV2Controller.DeclineTask` | declineSurveyTask | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 1 | RF-10-03/B |
| C040 | POST `/survey-tasks/{taskId}/cancel` | `SurveyV2Controller.CancelTask` | cancelSurveyTask | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 1 | RF-10-03/B |
| C041 | POST `/survey-tasks/{taskId}/reassign` | `SurveyV2Controller.ReassignTask` | reassignSurveyTask | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 2 | RF-10-03/B |
| C042 | POST `/survey-tasks/{taskId}/supplements` | `SurveyV2Controller.RequestSupplement` | requestSurveySupplement | COMPARE_WIRE_AND_KEEP_CURRENT | V2 survey API/SQL; PM 1 | RF-10-03/B |
| C043 | POST `/uploads` | `UploadsController.Create` | createUploadSession | COMPARE_WIRE_AND_KEEP_CURRENT | Upload API/SQL; PM 1 | RF-10-04/B |
| C044 | GET `/uploads/{uploadId}` | `UploadsController.GetSession` | getUploadSession | COMPARE_WIRE_AND_KEEP_CURRENT | Upload API/SQL; PM 1 | RF-10-04/B |
| C045 | POST `/uploads/{uploadId}/part-urls` | `UploadsController.GetPartUrls` | getUploadPartUrls | COMPARE_WIRE_AND_KEEP_CURRENT | Upload API/SQL; PM 1 | RF-10-04/B |
| C046 | POST `/uploads/{uploadId}/complete` | `UploadsController.Complete` | completeUpload | COMPARE_WIRE_AND_KEEP_CURRENT | Upload API/SQL; PM 2 | RF-10-04/B |
| C047 | GET `/files/{fileId}` | `UploadsController.GetFileMetadata` | getFileMetadata | COMPARE_WIRE_AND_KEEP_CURRENT | Upload API/SQL; PM 2 | RF-10-04/B |
| C048 | GET `/files/{fileId}/content` | `UploadsController.Download` | downloadFile | COMPARE_WIRE_AND_KEEP_CURRENT | Upload API/SQL; PM 1 | RF-10-04/B |
| C049 | POST `/processing-jobs` | `ProcessingV2Controller.CreateProcessingJob` | createProcessingJob | COMPARE_WIRE_AND_KEEP_CURRENT | Processing SQL/unit; PM 1 | RF-10-05/B |
| C050 | GET `/processing-jobs/{jobId}` | `ProcessingV2Controller.GetProcessingJob` | getProcessingJob | COMPARE_WIRE_AND_KEEP_CURRENT | Processing SQL/unit; PM 1 | RF-10-05/B |
| C051 | POST `/processing-jobs/{jobId}/retry` | `ProcessingV2Controller.RetryProcessingJob` | retryProcessingJob | COMPARE_WIRE_AND_KEEP_CURRENT | Processing SQL/unit; PM 1 | RF-10-05/B |
| C052 | POST `/internal/processing-jobs/{jobId}/results` | `ProcessingV2Controller.ReceiveAiResult` | receiveAiResult | COMPARE_WIRE_AND_KEEP_CURRENT | Processing SQL/unit; PM 1 | RF-10-05/B |
| C053 | POST `/projects/{projectId}/validation-runs` | `ProcessingV2Controller.CreateValidationRun` | createValidationRun | COMPARE_WIRE_AND_KEEP_CURRENT | Validation SQL/unit; PM 2 | RF-10-05/B |
| C054 | GET `/validation-runs/{runId}` | `ProcessingV2Controller.GetValidationResult` | getValidationResult | COMPARE_WIRE_AND_KEEP_CURRENT | Validation SQL/unit; PM 1 | RF-10-05/B |
| C055 | GET `/notifications/{notificationId}` | `NotificationsController.Get` | getNotification | COMPARE_WIRE_AND_KEEP_CURRENT | Notification API/SQL; PM 1 | RF-10-09-A/A |
| C056 | GET `/notifications` | `NotificationsController.List` | listNotifications | COMPARE_WIRE_AND_KEEP_CURRENT | Notification API/SQL; PM 3 | RF-10-09-A/A |
| C057 | POST `/notifications/{notificationId}/read` | `NotificationsController.MarkRead` | readNotification | COMPARE_WIRE_AND_KEEP_CURRENT | Notification API/SQL; PM 1 | RF-10-09-A/A |
| C058 | GET `/health` | `Program.MapHealthChecks` | none | CURRENT_ONLY_PRESERVE_PENDING_CONSUMER_CHECK | RF-01 static preflight; PM 1 | RF-06/A |

## Draft contract to current source

| ID | Method and path | operationId | Current action | Match / disposition | FR source | Task/owner |
|---|---|---|---|---|---|---|
| D001 | POST `/auth/login` | `login` | AuthController.Login | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01 | RF-10-01/A |
| D002 | POST `/auth/refresh` | `refreshTokens` | AuthController.Refresh | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01 | RF-10-01/A |
| D003 | POST `/auth/logout` | `logout` | AuthController.Logout | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01 | RF-10-01/A |
| D004 | POST `/auth/password-recovery-requests` | `requestPasswordRecovery` | AuthController.RequestPasswordRecovery | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01 | RF-10-01/A |
| D005 | POST `/auth/change-password` | `changePassword` | AuthController.ChangePassword | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01 | RF-10-01/A |
| D006 | POST `/auth/reporter-registrations` | `registerReporter` | ReporterRegistrationsController.Register | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-03 | RF-10-01/A |
| D007 | POST `/auth/reporter-registrations/verify` | `verifyReporterOtp` | ReporterRegistrationsController.Verify | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-03 | RF-10-01/A |
| D008 | POST `/auth/reporter-registrations/resend` | `resendReporterOtp` | ReporterRegistrationsController.Resend | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-03 | RF-10-01/A |
| D009 | POST `/invitations/accept` | `acceptInvitation` | InvitationsController.Accept | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-02 | RF-10-01/A |
| D010 | POST `/invitations` | `createInvitation` | InvitationsController.Create | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-02 | RF-10-01/A |
| D011 | GET `/me` | `getMe` | MeController.Get | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01 | RF-10-01/A |
| D012 | PATCH `/me` | `updateMe` | MeController.Update | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01 | RF-10-01/A |
| D013 | POST `/users/{userId}/password-reset` | `adminResetPassword` | UsersController.ResetPassword | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01, FR-36* | RF-10-01/A |
| D014 | PATCH `/users/{userId}` | `updateAccount` | UsersController.Update | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-36* | RF-10-01/A |
| D015 | GET `/users/{userId}` | `getAccount` | UsersController.Get | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-36* | RF-10-01/A |
| D016 | GET `/notifications` | `listNotifications` | NotificationsController.List | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-34* | RF-10-09-A/A |
| D017 | POST `/notifications/{notificationId}/read` | `readNotification` | NotificationsController.MarkRead | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-34* | RF-10-09-A/A |
| D018 | GET `/projects` | `listProjects` | none | none; DRAFT_TARGET_ONLY | FR-01*, FR-04 | RF-10-02/A |
| D019 | POST `/projects` | `createProject` | ProjectsController.Create | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-04 | RF-10-02/A |
| D020 | GET `/projects/{projectId}` | `getProject` | none | none; DRAFT_TARGET_ONLY | FR-04 | RF-10-02/A |
| D021 | PATCH `/projects/{projectId}` | `updateProject` | none | none; DRAFT_TARGET_ONLY | FR-04 | RF-10-02/A |
| D022 | POST `/projects/{projectId}/close` | `closeProject` | none | none; DRAFT_TARGET_ONLY | FR-04 | RF-10-02/A |
| D023 | POST `/projects/{projectId}/memberships` | `setMembership` | none | none; DRAFT_TARGET_ONLY | FR-01, FR-04* | RF-10-01/A |
| D024 | GET `/projects/{projectId}/crews` | `listCrews` | none | none; DRAFT_TARGET_ONLY | FR-20* | RF-10-02/A |
| D025 | POST `/projects/{projectId}/crews` | `createCrew` | none | none; DRAFT_TARGET_ONLY | FR-20* | RF-10-01/A |
| D026 | POST `/projects/{projectId}/route-drafts` | `createRouteDraft` | none | none; DRAFT_TARGET_ONLY | FR-05, FR-06 | RF-10-02/A |
| D027 | GET `/route-versions/{routeVersionId}` | `getRouteVersion` | none | none; DRAFT_TARGET_ONLY | FR-05, FR-06 | RF-10-02/A |
| D028 | PUT `/route-versions/{routeVersionId}/draft` | `updateRouteDraft` | none | none; DRAFT_TARGET_ONLY | FR-06 | RF-10-02/A |
| D029 | POST `/route-versions/{routeVersionId}/confirm` | `confirmRoute` | none | none; DRAFT_TARGET_ONLY | FR-07 | RF-10-02/A |
| D030 | POST `/projects/{projectId}/segment-set-previews` | `previewSegmentSet` | none | none; DRAFT_TARGET_ONLY | FR-08 | RF-10-02/A |
| D031 | POST `/segment-sets/{segmentSetId}/publish` | `publishSegmentSet` | none | none; DRAFT_TARGET_ONLY | FR-08 | RF-10-02/A |
| D032 | POST `/projects/{projectId}/branches` | `createBranch` | none | none; DRAFT_TARGET_ONLY | FR-09 | RF-10-02/A |
| D033 | POST `/projects/{projectId}/slabs` | `createSlab` | none | none; DRAFT_TARGET_ONLY | FR-10 | RF-10-02/A |
| D034 | POST `/reports` | `createReport` | none | none; DRAFT_TARGET_ONLY | FR-11 | RF-10-06/A |
| D035 | GET `/reports` | `listOwnReports` | none | none; DRAFT_TARGET_ONLY | FR-11 | RF-10-06/A |
| D036 | GET `/reports/{reportId}` | `getOwnReport` | none | none; DRAFT_TARGET_ONLY | FR-25* | RF-10-06/A |
| D037 | POST `/reports/{reportId}/supplements` | `supplementReport` | none | none; DRAFT_TARGET_ONLY | FR-11 | RF-10-06/A |
| D038 | GET `/projects/{projectId}/cases` | `listCases` | none | none; DRAFT_TARGET_ONLY | FR-13 | RF-10-06/A |
| D039 | GET `/cases/{caseId}` | `getCase` | none | none; DRAFT_TARGET_ONLY | FR-13 | RF-10-06/A |
| D040 | POST `/cases/{caseId}/triage` | `triageCase` | none | none; DRAFT_TARGET_ONLY | FR-13 | RF-10-06/A |
| D041 | POST `/cases/{caseId}/report-links` | `linkReports` | none | none; DRAFT_TARGET_ONLY | FR-12 | RF-10-06/A |
| D042 | POST `/cases/{caseId}/conclusion` | `concludeCase` | none | none; DRAFT_TARGET_ONLY | FR-13 | RF-10-06/A |
| D043 | POST `/cases/{caseId}/publish` | `publishCase` | none | none; DRAFT_TARGET_ONLY | FR-25* | RF-10-06/A |
| D044 | POST `/cases/{caseId}/close` | `closeMixedCase` | none | none; DRAFT_TARGET_ONLY | FR-23* | RF-10-06/A |
| D045 | GET `/projects/{projectId}/defects` | `listDefects` | none | none; DRAFT_TARGET_ONLY | FR-10*, FR-13 | RF-10-06/A |
| D046 | POST `/projects/{projectId}/defects` | `createPreliminaryDefect` | none | none; DRAFT_TARGET_ONLY | FR-13 | RF-10-06/A |
| D047 | GET `/defects/{defectId}` | `getDefect` | none | none; DRAFT_TARGET_ONLY | FR-13 | RF-10-06/A |
| D048 | POST `/defects/{defectId}/assessments` | `assessDefect` | none | none; DRAFT_TARGET_ONLY | FR-14 | RF-10-06/A |
| D049 | POST `/defects/{defectId}/verification` | `verifyDefect` | none | none; DRAFT_TARGET_ONLY | FR-13 | RF-10-06/A |
| D050 | POST `/defects/{defectId}/recurrence-assessments` | `assessRecurrence` | none | none; DRAFT_TARGET_ONLY | FR-24* | RF-10-06/A |
| D051 | PUT `/projects/{projectId}/work-order` | `setWorkOrder` | none | none; DRAFT_TARGET_ONLY | FR-14* | RF-10-07/A |
| D052 | GET `/projects/{projectId}/work-order` | `getWorkOrder` | none | none; DRAFT_TARGET_ONLY | FR-14* | RF-10-07/A |
| D053 | POST `/projects/{projectId}/policy-versions` | `createPolicyVersion` | none | none; DRAFT_TARGET_ONLY | FR-15 | RF-10-07/A |
| D054 | GET `/policy-versions/{policyVersionId}` | `getPolicyVersion` | none | none; DRAFT_TARGET_ONLY | FR-15 | RF-10-07/A |
| D055 | POST `/policy-versions/{policyVersionId}/activate` | `activatePolicyVersion` | none | none; DRAFT_TARGET_ONLY | FR-15 | RF-10-07/A |
| D056 | POST `/projects/{projectId}/inspection-tasks` | `createInspectionTask` | none | none; DRAFT_TARGET_ONLY | FR-17, FR-18, FR-20 | RF-10-07/A |
| D057 | POST `/projects/{projectId}/inspection-batches` | `createInspectionBatch` | none | none; DRAFT_TARGET_ONLY | FR-16 | RF-10-07/A |
| D058 | GET `/inspection-tasks/{taskId}` | `getInspectionTask` | none | none; DRAFT_TARGET_ONLY | FR-17, FR-22* | RF-10-07/A |
| D059 | POST `/inspection-tasks/{taskId}/accept` | `acceptInspectionTask` | none | none; DRAFT_TARGET_ONLY | FR-17 | RF-10-07/A |
| D060 | POST `/inspection-tasks/{taskId}/decline` | `declineInspectionTask` | none | none; DRAFT_TARGET_ONLY | FR-17 | RF-10-07/A |
| D061 | POST `/inspection-tasks/{taskId}/sessions` | `submitInspection` | none | none; DRAFT_TARGET_ONLY | FR-17 | RF-10-07/A |
| D062 | POST `/inspection-tasks/{taskId}/evaluations` | `evaluateFastTrack` | none | none; DRAFT_TARGET_ONLY | FR-18 | RF-10-07/A |
| D063 | POST `/projects/{projectId}/repair-packages` | `createRepairPackage` | none | none; DRAFT_TARGET_ONLY | FR-19 | RF-10-07/A |
| D064 | POST `/repair-packages/{packageId}/submit` | `submitRepairPackage` | none | none; DRAFT_TARGET_ONLY | FR-19 | RF-10-07/A |
| D065 | POST `/repair-items/{itemId}/decisions` | `decideRepairItem` | none | none; DRAFT_TARGET_ONLY | FR-19 | RF-10-07/A |
| D066 | POST `/repair-items/{itemId}/assignments` | `assignRepairItem` | none | none; DRAFT_TARGET_ONLY | FR-20 | RF-10-07/A |
| D067 | POST `/repair-items/{itemId}/reassignments` | `reassignRepairItem` | none | none; DRAFT_TARGET_ONLY | FR-20 | RF-10-07/A |
| D068 | POST `/repair-attempts` | `startRepairAttempt` | none | none; DRAFT_TARGET_ONLY | FR-18, FR-21 | RF-10-07/A |
| D069 | POST `/repair-attempts/{attemptId}/submit` | `submitRepairAttempt` | none | none; DRAFT_TARGET_ONLY | FR-21 | RF-10-07/A |
| D070 | POST `/repair-attempts/{attemptId}/review` | `reviewRepairAttempt` | none | none; DRAFT_TARGET_ONLY | FR-23 | RF-10-07/A |
| D071 | POST `/repair-attempts/{attemptId}/acceptance` | `acceptApprovalAttempt` | none | none; DRAFT_TARGET_ONLY | FR-23 | RF-10-07/A |
| D072 | POST `/projects/{projectId}/emergency-tasks` | `createEmergencyTask` | none | none; DRAFT_TARGET_ONLY | FR-37 | RF-10-07/A |
| D073 | POST `/projects/{projectId}/survey-tasks` | `createSurveyTask` | SurveyV2Controller.CreateTask | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D074 | GET `/survey-tasks/{taskId}` | `getSurveyTask` | SurveyV2Controller.GetTask | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D075 | POST `/survey-tasks/{taskId}/accept` | `acceptSurveyTask` | SurveyV2Controller.AcceptTask | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D076 | POST `/survey-tasks/{taskId}/decline` | `declineSurveyTask` | SurveyV2Controller.DeclineTask | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D077 | POST `/survey-tasks/{taskId}/cancel` | `cancelSurveyTask` | SurveyV2Controller.CancelTask | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D078 | POST `/survey-tasks/{taskId}/reassign` | `reassignSurveyTask` | SurveyV2Controller.ReassignTask | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D079 | POST `/survey-tasks/{taskId}/supplements` | `requestSurveySupplement` | SurveyV2Controller.RequestSupplement | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D080 | PUT `/survey-tasks/{taskId}/access-point` | `setSurveyAccessPoint` | none | none; DRAFT_TARGET_ONLY | FR-32* | RF-10-03/B |
| D081 | POST `/survey-tasks/{taskId}/datasets` | `submitDataset` | SurveyV2Controller.SubmitDataset | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-27* | RF-10-03/B |
| D082 | GET `/datasets/{datasetId}/coverage` | `getDatasetCoverage` | SurveyV2Controller.GetDatasetCoverage | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-28 | RF-10-03/B |
| D083 | POST `/projects/{projectId}/baselines` | `confirmBaseline` | none | none; DRAFT_TARGET_ONLY | FR-30 | RF-10-03/B |
| D084 | POST `/projects/{projectId}/mission-exports` | `exportMission` | none | none; DRAFT_TARGET_ONLY | FR-33* | RF-10-03/B |
| D085 | POST `/uploads` | `createUploadSession` | UploadsController.Create | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-21*, FR-27 | RF-10-04/B |
| D086 | POST `/uploads/{uploadId}/part-urls` | `getUploadPartUrls` | UploadsController.GetPartUrls | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-22*, FR-27 | RF-10-04/B |
| D087 | POST `/uploads/{uploadId}/complete` | `completeUpload` | UploadsController.Complete | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-21*, FR-27 | RF-10-04/B |
| D088 | GET `/uploads/{uploadId}` | `getUploadSession` | UploadsController.GetSession | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-22*, FR-27 | RF-10-04/B |
| D089 | GET `/files/{fileId}` | `getFileMetadata` | UploadsController.GetFileMetadata | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-21*, FR-35* | RF-10-04/B |
| D090 | GET `/files/{fileId}/content` | `downloadFile` | UploadsController.Download | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-01*, FR-35* | RF-10-04/B |
| D091 | POST `/sync/batches` | `syncOperations` | none | none; DRAFT_TARGET_ONLY | FR-22 | RF-10-08/B |
| D092 | POST `/processing-jobs` | `createProcessingJob` | ProcessingV2Controller.CreateProcessingJob | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-29 | RF-10-05/B |
| D093 | GET `/processing-jobs/{jobId}` | `getProcessingJob` | ProcessingV2Controller.GetProcessingJob | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-29 | RF-10-05/B |
| D094 | POST `/processing-jobs/{jobId}/retry` | `retryProcessingJob` | ProcessingV2Controller.RetryProcessingJob | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-29 | RF-10-05/B |
| D095 | POST `/internal/processing-jobs/{jobId}/results` | `receiveAiResult` | ProcessingV2Controller.ReceiveAiResult | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-29 | RF-10-05/B |
| D096 | POST `/projects/{projectId}/validation-runs` | `createValidationRun` | ProcessingV2Controller.CreateValidationRun | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-31 | RF-10-05/B |
| D097 | GET `/validation-runs/{runId}` | `getValidationResult` | ProcessingV2Controller.GetValidationResult | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-31 | RF-10-05/B |
| D098 | POST `/labels/{labelId}/review` | `reviewTrainingLabel` | none | none; DRAFT_TARGET_ONLY | FR-36* | RF-10-06/A |
| D099 | GET `/projects/{projectId}/dashboard` | `getDashboard` | none | none; DRAFT_TARGET_ONLY | FR-34 | RF-10-09-B/B |
| D100 | GET `/projects/{projectId}/timeline` | `getProjectTimeline` | none | none; DRAFT_TARGET_ONLY | FR-34 | RF-10-09-B/B |
| D101 | POST `/exports` | `createExport` | none | none; DRAFT_TARGET_ONLY | FR-35 | RF-10-09-B/B |
| D102 | GET `/exports/{exportId}` | `getExport` | none | none; DRAFT_TARGET_ONLY | FR-35 | RF-10-09-B/B |
| D103 | GET `/audit-events` | `listAuditEvents` | none | none; DRAFT_TARGET_ONLY | FR-36* | RF-10-09-A/A |
| D104 | POST `/model-versions` | `createModelVersion` | none | none; DRAFT_TARGET_ONLY | FR-36 | RF-10-05/B |
| D105 | POST `/model-versions/{modelVersionId}/activate` | `activateModelVersion` | none | none; DRAFT_TARGET_ONLY | FR-36 | RF-10-05/B |
| D106 | POST `/model-versions/{modelVersionId}/retire` | `retireModelVersion` | none | none; DRAFT_TARGET_ONLY | FR-36 | RF-10-05/B |
| D107 | POST `/devices` | `createDevice` | none | none; DRAFT_TARGET_ONLY | FR-36* | RF-10-03/B |
| D108 | GET `/catalog/defect-types` | `listDefectTypes` | none | none; DRAFT_TARGET_ONLY | FR-36* | RF-10-06/A |
| D109 | PUT `/catalog/defect-types/{code}` | `updateDefectType` | none | none; DRAFT_TARGET_ONLY | FR-36* | RF-10-06/A |
| D110 | GET `/reminder-configuration` | `getReminderConfig` | none | none; DRAFT_TARGET_ONLY | FR-36* | RF-10-09-A/A |
| D111 | PUT `/reminder-configuration` | `setReminderConfig` | none | none; DRAFT_TARGET_ONLY | FR-36* | RF-10-09-A/A |
| D112 | POST `/retention/deletion-requests` | `requestDeletion` | none | none; DRAFT_TARGET_ONLY | FR-35* | RF-10-09-A/A |
| D113 | POST `/retention/deletion-requests/{requestId}/decision` | `decideDeletion` | none | none; DRAFT_TARGET_ONLY | FR-35* | RF-10-09-A/A |
| D114 | PUT `/projects/{projectId}/legal-hold` | `setLegalHold` | none | none; DRAFT_TARGET_ONLY | FR-35* | RF-10-09-A/A |
| D115 | GET `/me/inspection-tasks` | `listMyInspectionTasks` | InspectionTasksController.List | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-17, FR-22* | RF-10-07/A |
| D116 | GET `/me/repair-items` | `listMyRepairItems` | none | none; DRAFT_TARGET_ONLY | FR-20 | RF-10-07/A |
| D117 | GET `/me/survey-tasks` | `listMySurveyTasks` | SurveyV2Controller.ListMyTasks | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D118 | GET `/inspection-tasks/{taskId}/snapshot` | `getInspectionSnapshot` | none | none; DRAFT_TARGET_ONLY | FR-15, FR-18, FR-22* | RF-10-07/A |
| D119 | PUT `/defects/{defectId}/slab-links` | `setDefectSlabLinks` | none | none; DRAFT_TARGET_ONLY | FR-10* | RF-10-06/A |
| D120 | POST `/projects/{projectId}/survey-plans` | `createSurveyPlan` | SurveyV2Controller.CreatePlan | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26, FR-30 | RF-10-03/B |
| D121 | POST `/survey-plans/{planId}/postpone` | `postponeSurveyPlan` | SurveyV2Controller.Postpone | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-26 | RF-10-03/B |
| D122 | POST `/training-exports` | `exportApprovedLabels` | none | none; DRAFT_TARGET_ONLY | FR-36* | RF-10-06/A |
| D123 | GET `/admin/processing-jobs` | `listAdminJobs` | none | none; DRAFT_TARGET_ONLY | FR-36 | RF-10-05/B |
| D124 | GET `/jobs/{jobId}` | `getAsyncJob` | none | none; DRAFT_TARGET_ONLY | FR-29, FR-31, FR-33*, FR-35*, FR-36 | RF-10-05/B |
| D125 | GET `/repair-items/{itemId}` | `getRepairItem` | none | none; DRAFT_TARGET_ONLY | FR-20, FR-23 | RF-10-07/A |
| D126 | GET `/repair-attempts/{attemptId}` | `getRepairAttempt` | none | none; DRAFT_TARGET_ONLY | FR-21, FR-23 | RF-10-07/A |
| D127 | GET `/repair-packages/{packageId}` | `getRepairPackage` | none | none; DRAFT_TARGET_ONLY | FR-19 | RF-10-07/A |
| D128 | GET `/segment-sets/{segmentSetId}` | `getSegmentSet` | none | none; DRAFT_TARGET_ONLY | FR-08 | RF-10-02/A |
| D129 | GET `/retention/deletion-requests/{requestId}` | `getDeletionRequest` | none | none; DRAFT_TARGET_ONLY | FR-35* | RF-10-09-A/A |
| D130 | GET `/model-versions/{modelVersionId}` | `getModelVersion` | none | none; DRAFT_TARGET_ONLY | FR-36 | RF-10-05/B |
| D131 | GET `/notifications/{notificationId}` | `getNotification` | NotificationsController.Get | normalized; COMPARE_WIRE_AND_KEEP_CURRENT | FR-34* | RF-10-09-A/A |
| D132 | POST `/repair-items/{itemId}/revisions` | `reviseRepairItem` | none | none; DRAFT_TARGET_ONLY | FR-19 | RF-10-07/A |
| D133 | POST `/repair-attempts/{attemptId}/rework-attempts` | `startReworkAttempt` | none | none; DRAFT_TARGET_ONLY | FR-24 | RF-10-07/A |

## Reconciliation limits

- `*` after an FR marks CONTEXT_ONLY: the FR's primary module differs from the operation owner. JSON records the named source task and reason. No current/draft primary owner transition was detected in this snapshot; the field is mandatory if one appears later.
- A shape/parameter-alias match requires manual route parameter and wire review. A draft-only operation is a proposed target, not a missing accepted requirement by itself.
- Test file labels come from RF-01 group inventory; they are not per-operation passing tests. RF-00 API tests failed before HTTP assertions.
- The current inventory is verified against controller route attributes at generation time; `Program.MapHealthChecks` is separately counted. No deployed consumer or runtime response was checked.
- Requirement approval is per decision in `02-decision-register.md`, not inferred from an OpenAPI `x-fr` or operation name. `UNKNOWN` remains explicit in JSON.
