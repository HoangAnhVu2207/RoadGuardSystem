# API inventory quan sát tại baseline

**ACTIVE · IMPLEMENTED_SOURCE_VERIFIED · DOC-AUDIT-01**, `f0c8762509310b2779068891062267c5f520aff2`. Local `CustomWebApplicationFactory(Development)` GET `/swagger/v1/swagger.json` thành công; disconnected DB, storage disabled, workers removed. API Explorer lọc production assembly: **47 controllers, 241 operations, 223 paths**, cộng `/health` unversioned riêng. Test probe routes bị loại. Runtime OpenAPI **3.0.1**, khác V2 proposal3.1.1/133 operations. Capture không gọi tất cả business endpoints và không là deployment certification.

[Matrix](DOC-AUDIT-01-MATRIX.md) nối service/repository/DB/test; [source-of-truth index](DOC-AUDIT-01-DOCUMENT-INDEX.md) ghi adoption. Capture toàn schema và logs được bàn giao ngoài repo; SHA-256 ở evidence manifest. Bảng là observation, không thay backend schema hoặc FE lock.

## Projects/staff transport và recovery

GET projects: `search`, `status=PLANNING|ACTIVE|CLOSED`, `limit=20` (1..100), `cursor`; `items/nextCursor/asOf`, IDs và primary-PM display projection. Cursor CreatedAt+Id chỉ position, không signed/filter-bound; SQL reapplies current actor scope mỗi request. Không trả project command rowversion từ list.

GET users: Supervisor-only, internalstaff default; Reporter chỉ explicit role filter, statuses ACTIVE/SUSPENDED/PENDING. Search tên/email; default20/limit1..100. Users response `userId/displayName/email/phoneNumber/role/status`. Contacts nullable.

GET members: Supervisor global hoặc PM current effective membership (không bắt primary); default current membership/account ACTIVE và matching role; includeHistory Supervisor-only, status ACTIVE|ENDED chỉ history. Rows có `membershipId/userId/displayName/role/isPrimary/status/validFrom/validTo/membershipVersion`, không contact. Cursor binds actor/scope/project/history/filters, SQL GUID order.

GET primary-project-manager: envelope `primaryProjectManager` nullable; current eligible primary PM gồm membership identity/user/name/effective dates/base64 `membershipVersion`, không contact. Version dùng `ExpectedCurrentMembershipRowVersion` của PUT hiện có; không account/project ETag. Valid project không head →200/null; missing project404, PM wrong scope403.

GET user account detail: Supervisor-only `id/displayName/role/version/email/phoneNumber` + quoted ETag. `/me` và PATCH response không bị thêm contacts. Các GET trên có method `[WebCookieEligible]`; bearer/no-auth/session revocation đã chạy focused tests. HTTPS cookie lấy từ real web login trong TestHost; GET không body/không CSRF header. Authorization header ưu tiên; unsafe adopted cookie methods giữ CSRF.

Sau timeout/reload đọc đúng resource/task/primary head và guarded receipt/status trước retry. Same operation phải giữ key/payload; sau stale409/412 đọc lại, không thay version để replay cũ. Errors là actual controller ProblemDetails code/correlationId (không draft envelope). ETag không có ở mọi GET; rowversion-body và hash pin khác nhau.

## Production operations

| Method/path | Controller.action | Request DTO/schema | Required headers (Swagger) | Success/errors (Swagger) | Security (Swagger) |
|---|---|---|---|---|---|
| `POST /api/v1/admin/users/{userId}/password-reset` | `AdminUsersController.ResetPassword` | AdminPasswordResetRequestDto | - | 200, 400, 401, 403, 404, 409, 500, 415 | Bearer |
| `POST /api/v1/auth/android/login` | `AuthController.AndroidLogin` | LoginRequestDto | - | 200, 400, 401, 409, 500, 415 | anonymous/route-specific |
| `POST /api/v1/auth/android/refresh` | `AuthController.AndroidRefresh` | RefreshRequestDto | - | 200, 400, 401, 500, 415 | anonymous/route-specific |
| `POST /api/v1/auth/change-password` | `AuthController.ChangePassword` | ChangePasswordRequestDto | Idempotency-Key | 204, 400, 401, 409, 403, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/auth/forced-password-change` | `AuthController.ForcedPasswordChange` | ForcedPasswordChangeRequestDto | - | 204, 400, 401, 409, 403, 500, 415 | anonymous/route-specific |
| `POST /api/v1/auth/login` | `AuthController.Login` | LoginRequestDto | - | 200, 400, 401, 409, 500, 415 | anonymous/route-specific |
| `POST /api/v1/auth/logout` | `AuthController.Logout` | none | Idempotency-Key | 204, 401, 400, 403, 409, 500 | Bearer |
| `POST /api/v1/auth/password-recovery-requests` | `AuthController.RequestPasswordRecovery` | PasswordRecoveryRequestDto | - | 202, 400, 401, 403, 409, 500, 415 | anonymous/route-specific |
| `POST /api/v1/auth/refresh` | `AuthController.Refresh` | RefreshRequestDto | - | 200, 400, 401, 409, 500, 415 | anonymous/route-specific |
| `POST /api/v1/auth/reporter-registrations` | `ReporterRegistrationsController.Register` | RegisterReporterRequestDto | Idempotency-Key | 202, 400, 409, 503, 500, 415 | anonymous/route-specific |
| `POST /api/v1/auth/reporter-registrations/resend` | `ReporterRegistrationsController.Resend` | ResendReporterOtpRequestDto | Idempotency-Key | 202, 400, 409, 404, 429, 503, 500, 415 | anonymous/route-specific |
| `POST /api/v1/auth/reporter-registrations/verify` | `ReporterRegistrationsController.Verify` | VerifyReporterOtpRequestDto | Idempotency-Key | 200, 400, 409, 404, 429, 500, 415 | anonymous/route-specific |
| `GET /api/v1/auth/web/csrf` | `WebAuthController.Csrf` | none | - | 200, 500 | anonymous/route-specific |
| `POST /api/v1/auth/web/login` | `WebAuthController.Login` | LoginRequestDto | X-CSRF-TOKEN | 200, 403, 400, 401, 500, 415 | CsrfToken+AntiforgeryCookie |
| `POST /api/v1/auth/web/logout` | `WebAuthController.Logout` | none | Idempotency-Key, X-CSRF-TOKEN | 204, 400, 401, 403, 409, 500 | WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/auth/web/renew` | `WebAuthController.Renew` | none | X-CSRF-TOKEN | 200, 403, 400, 401, 500 | WebRenewal+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/auth/web/session` | `WebAuthController.Session` | none | - | 200, 401, 500, 403 | WebSession |
| `GET /api/v1/cases` | `CasesController.List` | none | - | 200, 400, 401, 403, 404, 409, 503, 500 | Bearer OR WebSession |
| `GET /api/v1/cases/{caseId}` | `CasesController.Read` | none | - | 200, 401, 403, 404, 409, 412, 503, 500 | Bearer OR WebSession |
| `POST /api/v1/cases/{caseId}/conclusions` | `CasesController.Conclude` | CaseConclusionDto | Idempotency-Key, If-Match | 200, 400, 401, 403, 404, 409, 412, 428, 503, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/cases/{caseId}/publications` | `CasesController.Publish` | CasePublicationDto | Idempotency-Key, If-Match | 201, 400, 401, 403, 404, 409, 412, 428, 503, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/cases/{caseId}/report-links` | `CasesController.Link` | CaseLinkDto | Idempotency-Key, If-Match | 200, 400, 401, 403, 404, 409, 412, 428, 503, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/cases/{caseId}/report-splits` | `CasesController.Split` | CaseSplitDto | Idempotency-Key, If-Match | 201, 400, 401, 403, 404, 409, 412, 428, 503, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/cases/{caseId}/triage` | `CasesController.Triage` | CaseTriageDto | Idempotency-Key, If-Match | 200, 400, 401, 403, 404, 409, 412, 428, 503, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/datasets/{datasetId}/coverage` | `SurveyV2Controller.GetDatasetCoverage` | none | - | 200, 401, 403, 404, 422, 500 | Bearer |
| `GET /api/v1/datasets/{id}` | `SurveyV2Controller.ReadDataset` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `POST /api/v1/datasets/{id}/assessments` | `SurveyAssessmentsController.Create` | CreateAssessmentDto | Idempotency-Key, If-Match | 201, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/datasets/{id}/assessments/{assessmentId}` | `SurveyAssessmentsController.Read` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `GET /api/v1/files/{fileId}` | `UploadsController.GetFileMetadata` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `GET /api/v1/files/{fileId}/content` | `UploadsController.Download` | none | - | 200, 401, 403, 404, 503, 500 | Bearer |
| `POST /api/v1/internal/processing-jobs/{jobId}/results` | `ProcessingV2Controller.ReceiveAiResult` | ReceiveAiResultRequestDto | Idempotency-Key | 200, 428, 404, 409, 422, 500, 400, 415, 401, 403 | AiServiceBearer |
| `POST /api/v1/invitations` | `InvitationsController.Create` | CreateInvitationRequestDto | Idempotency-Key | 201, 400, 404, 409, 403, 503, 500, 415, 401 | Bearer |
| `POST /api/v1/invitations/accept` | `InvitationsController.Accept` | AcceptInvitationRequestDto | Idempotency-Key | 200, 400, 404, 409, 500, 415 | anonymous/route-specific |
| `POST /api/v1/labels/{labelId}/review` | `TrainingLabelsController.Review` | ReviewTrainingLabelDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 400, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/me` | `MeController.Get` | none | - | 200, 401, 404, 500, 403 | Bearer OR WebSession |
| `PATCH /api/v1/me` | `MeController.Update` | UpdateMeRequestDto | Idempotency-Key, If-Match | 200, 400, 401, 403, 404, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/me/inspection-tasks` | `InspectionTasksController.List` | none | - | 200, 400, 401, 403, 500 | Bearer OR WebSession |
| `GET /api/v1/me/survey-tasks` | `SurveyV2Controller.ListMyTasks` | none | - | 200, 400, 401, 403, 500 | Bearer |
| `GET /api/v1/notifications` | `NotificationsController.List` | none | - | 200, 400, 401, 500, 403 | Bearer OR WebSession |
| `GET /api/v1/notifications/{notificationId}` | `NotificationsController.Get` | none | - | 200, 401, 404, 400, 500, 403 | Bearer OR WebSession |
| `POST /api/v1/notifications/{notificationId}/read` | `NotificationsController.MarkRead` | none | Idempotency-Key, If-Match | 200, 400, 401, 404, 409, 412, 428, 500, 403 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/notifications/{notificationId}/scope` | `H6NotificationOperationsController.Scope` | none | - | 200, 401, 404, 500, 403 | Bearer OR WebSession |
| `POST /api/v1/offline/uploads` | `UploadsController.CreateOffline` | OfflineUploadCreateRequestDto | Idempotency-Key | 201, 400, 401, 403, 404, 409, 415, 422, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/processing-jobs` | `ProcessingV2Controller.CreateProcessingJob` | CreateProcessingJobRequestDto | Idempotency-Key | 202, 401, 403, 404, 409, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/processing-jobs/{jobId}` | `ProcessingV2Controller.GetProcessingJob` | none | - | 200, 401, 403, 404, 409, 500 | Bearer |
| `POST /api/v1/processing-jobs/{jobId}/retry` | `ProcessingV2Controller.RetryProcessingJob` | RetryProcessingJobRequestDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/profile` | `ProfileController.Get` | none | - | 200, 401, 400, 500, 403 | Bearer OR WebSession |
| `PUT /api/v1/profile` | `ProfileController.Update` | ProfileUpdateRequestDto | - | 200, 400, 401, 409, 403, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects` | `ProjectsController.List` | none | - | 200, 400, 401, 403 | Bearer OR WebSession |
| `POST /api/v1/projects` | `ProjectsController.Create` | CreateProjectRequestDto | - | 201, 400, 401, 403, 404, 409, 500, 415 | Bearer |
| `PUT /api/v1/projects/{projectId}` | `ProjectsController.Update` | UpdateProjectRequestDto | - | 200, 400, 401, 403, 404, 409, 500, 415 | Bearer |
| `GET /api/v1/projects/{projectId}/ai-candidates/{detectionId}` | `AiMockRunsController.Candidate` | none | - | 200, 401, 403, 404, 409, 500 | Bearer |
| `POST /api/v1/projects/{projectId}/ai-mock-runs` | `AiMockRunsController.Create` | CreateAiMockRunRequest | Idempotency-Key | 202, 401, 403, 404, 428, 422, 409, 500, 400, 415 | Bearer |
| `GET /api/v1/projects/{projectId}/ai-mock-runs/{runId}` | `AiMockRunsController.Get` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `GET /api/v1/projects/{projectId}/ai-mock-runs/{runId}/result` | `AiMockRunsController.Result` | none | - | 200, 401, 403, 404, 409, 500 | Bearer |
| `GET /api/v1/projects/{projectId}/baseline-selections` | `SurveyAssessmentsController.Current` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `POST /api/v1/projects/{projectId}/baseline-selections` | `SurveyAssessmentsController.Select` | CreateBaselineDto | Idempotency-Key | 201, 401, 403, 404, 409, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/projects/{projectId}/baseline-selections/{id}` | `SurveyAssessmentsController.History` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `POST /api/v1/projects/{projectId}/candidate-decisions` | `CandidatesController.Decide` | CandidateDecisionRequestDto | Idempotency-Key | 201, 400, 401, 403, 404, 409, 412, 428, 503, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/candidate-decisions/{decisionId}` | `CandidatesController.Read` | none | - | 200, 401, 403, 404, 503, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/candidates` | `CandidatesController.Match` | none | - | 200, 400, 401, 403, 404, 409, 412, 503, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/clocks` | `H6NotificationOperationsController.Clocks` | none | - | 200, 400, 401, 403, 409, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/clocks/{clockId}/appointments` | `ClockDutyController.Appoint` | BusinessDutyAppointmentInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/clocks/{clockId}/extensions` | `ClockDutyController.Extend` | ClockExtensionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/crs-profiles` | `GeometryWorkflowController.Profiles` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/crs-profiles` | `GeometryWorkflowController.CreateProfile` | CrsProfileInput | Idempotency-Key | 201, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/crs-profiles/{profileId}` | `GeometryWorkflowController.Profile` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/defect-statistics` | `DefectStatisticsController.Read` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/defect-statistics` | `DefectStatisticsController.Confirm` | DefectStatisticsInputDto | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 428, 409, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/defects` | `DefectsController.List` | none | - | 200, 401, 403, 404, 400, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/defects/{defectId}` | `DefectsController.Read` | none | - | 200, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/defects/{defectId}/assessments` | `DefectsController.Assess` | DefectAssessmentRequestDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 400, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/defects/{defectId}/verification-decisions` | `DefectsController.Verify` | DefectVerificationRequestDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 400, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/exports` | `ExportsController.Create` | CreateExportRequestDto | Idempotency-Key | 202, 400, 401, 403, 404, 409, 422, 428, 503, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/exports/{exportId}` | `ExportsController.Get` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/exports/{exportId}/content` | `ExportsController.Content` | none | - | 200, 400, 401, 403, 404, 409, 410, 503, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/exports/{exportId}/manifest` | `ExportsController.Manifest` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/field-inspection-tasks` | `FieldInspectionWorkflowController.List` | none | - | 200, 400, 401, 403, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks` | `FieldInspectionWorkflowController.Create` | FieldTaskCreateInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}` | `FieldInspectionWorkflowController.Get` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/accept` | `FieldInspectionWorkflowController.Accept` | FieldTaskActionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/assign` | `FieldInspectionWorkflowController.Assign` | FieldTaskActionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/cancel` | `FieldInspectionWorkflowController.Cancel` | FieldTaskActionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence-reuse` | `FieldInspectionWorkflowController.Reuse` | FieldEvidenceReuseInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}` | `FieldInspectionWorkflowController.Evidence` | none | - | 200, 400, 401, 403, 404, 409, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}/content` | `FieldInspectionWorkflowController.DownloadEvidence` | none | - | 200, 400, 401, 403, 404, 409, 500, 503 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/geometry` | `FieldInspectionWorkflowController.Geometry` | none | - | 200, 400, 401, 403, 404, 409, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/history` | `FieldInspectionWorkflowController.History` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/location-impact-decisions` | `FieldInspectionWorkflowController.Impact` | FieldLocationImpactActionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reassign` | `FieldInspectionWorkflowController.Reassign` | FieldTaskActionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reject` | `FieldInspectionWorkflowController.Reject` | FieldTaskActionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reviews` | `FieldInspectionWorkflowController.Review` | FieldReviewInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start` | `FieldInspectionWorkflowController.Start` | FieldStartInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start-origin` | `FieldInspectionWorkflowController.Origin` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions` | `FieldInspectionWorkflowController.Submit` | FieldSubmissionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId}` | `FieldInspectionWorkflowController.Submission` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/geometry-location-impacts` | `PavementWorkflowController.ListImpacts` | none | - | 200, 400, 401, 403, 412, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/geometry-location-impacts` | `PavementWorkflowController.CreateImpact` | GeometryImpactInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/geometry-location-impacts/{impactId}` | `PavementWorkflowController.GetImpact` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/geometry-location-impacts/{impactId}/decisions` | `PavementWorkflowController.DecideImpact` | GeometryImpactDecisionInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/geometry-map-publications` | `PavementWorkflowController.Publish` | GeometryMapPublishInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/geometry-map-publications/{publicationId}` | `PavementWorkflowController.Manifest` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/geometry-map-publications/{publicationId}/layers/{layer}` | `PavementWorkflowController.Page` | none | - | 200, 400, 401, 403, 404, 409, 412, 422, 428, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/geometry-package` | `GeometryWorkflowController.Package` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/labels` | `TrainingLabelsController.List` | none | - | 200, 401, 403, 404, 400, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/labels` | `TrainingLabelsController.Create` | CreateTrainingLabelDto | Idempotency-Key | 201, 401, 403, 404, 400, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/labels/{labelId}` | `TrainingLabelsController.Read` | none | - | 200, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/labels/{labelId}/revisions` | `TrainingLabelsController.Revise` | ReviseTrainingLabelDto | Idempotency-Key, If-Match | 201, 401, 403, 404, 400, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/lifecycle` | `ProjectLifecycleController.Read` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/lifecycle/{command}` | `ProjectLifecycleController.Execute` | LD06LifecycleInputDto | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/lifecycle/renewed-handling-scope` | `ProjectLifecycleController.Renew` | RenewedHandlingScopeInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/members` | `StaffDirectoryController.Members` | none | - | 200 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/offline/artifacts` | `OfflineWorkflowController.RegisterArtifact` | OfflineCaptureArtifactInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/offline/artifacts/{artifactId}` | `OfflineWorkflowController.GetArtifact` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/offline/batches/{batchId}` | `OfflineWorkflowController.GetBatch` | none | - | 200, 400, 401, 403, 404, 409, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/offline/devices` | `OfflineWorkflowController.RegisterDevice` | OfflineDeviceRegisterInput | Idempotency-Key | 201, 200, 400, 401, 403, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/offline/devices/{registrationId}` | `OfflineWorkflowController.GetDevice` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/offline/devices/{registrationId}/revoke` | `OfflineWorkflowController.RevokeDevice` | OfflineDeviceRevokeInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/offline/grants` | `OfflineWorkflowController.IssueGrant` | OfflineHandoverGrantInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/offline/grants/{grantId}` | `OfflineWorkflowController.GetGrant` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/offline/grants/{grantId}/revoke` | `OfflineWorkflowController.RevokeGrant` | OfflineGrantRevokeInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/offline/import` | `OfflineWorkflowController.ImportPackage` | OfflinePackageImportInput | Idempotency-Key | 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/offline/origins/{originId}` | `OfflineWorkflowController.GetOrigin` | none | - | 200, 400, 401, 403, 404, 409, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/offline/packages/{packageId}` | `OfflineWorkflowController.GetPackage` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/offline/packages/export` | `OfflineWorkflowController.ExportPackage` | OfflinePackageExportInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/offline/packages/prepare` | `OfflineWorkflowController.PreparePackage` | OfflinePackageExportInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/offline/snapshots` | `OfflineWorkflowController.CaptureSnapshot` | OfflineSnapshotInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/offline/snapshots/{snapshotId}` | `OfflineWorkflowController.GetSnapshot` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/offline/starts/reconcile` | `OfflineWorkflowController.ReconcileStart` | OfflineStartReconcileInput | Idempotency-Key | 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/offline/sync` | `OfflineWorkflowController.Synchronize` | OfflineSignedBatchInput | Idempotency-Key | 200, 400, 401, 403, 404, 409, 415, 428, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/pavement-layouts/{layoutId}` | `PavementWorkflowController.GetLayout` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/pavement-layouts/as-built` | `PavementWorkflowController.CreateAsBuilt` | AsBuiltLayoutInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/primary-project-manager` | `StaffDirectoryController.Primary` | none | - | 200 | Bearer OR WebSession |
| `PUT /api/v1/projects/{projectId}/primary-project-manager` | `ProjectsController.ReassignPrimaryProjectManager` | ReassignPrimaryProjectManagerRequestDto | - | 200, 400, 401, 403, 404, 409, 500, 415 | Bearer |
| `GET /api/v1/projects/{projectId}/receiving-requests` | `BusinessDutyController.List` | none | - | 200, 400, 401, 403, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/receiving-requests/{requestId}` | `BusinessDutyController.Read` | none | - | 200, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/receiving-requests/{requestId}/ack` | `BusinessDutyController.Ack` | BusinessRequestAckInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/receiving-requests/{requestId}/appointment` | `BusinessDutyController.Appoint` | BusinessDutyAppointmentInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages` | `RepairPackagesController.Create` | RepairPackageCreateInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items` | `RepairPackagesController.Propose` | RepairItemProposeInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}` | `RepairsController.Item` | none | - | 200, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/approve` | `RepairPackagesController.Approve` | RepairDecisionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/assign` | `RepairPackagesController.Assign` | RepairItemAssignInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/cancellations` | `RepairLifecycleController.Cancel` | RepairCancellationInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/corrections` | `RepairsController.Correct` | RepairCorrectionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/eligibility` | `RepairEligibilityController.Get` | none | - | 200, 400, 401, 403, 404, 409, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/final-confirmation` | `RepairPackagesController.ConfirmFinal` | RepairDecisionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/history` | `RepairsController.History` | none | - | 200, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/normal-successors` | `RepairLifecycleController.ContinueNormally` | RepairNormalContinuationInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/review-requests` | `RepairsController.RequestReview` | RepairReviewRequestInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures` | `RepairSafetyController.Create` | RepairSafetyCreateInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}` | `RepairSafetyController.Read` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/checks` | `RepairSafetyController.Check` | RepairSafetyCheckInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/installation` | `RepairSafetyController.Install` | RepairSafetyInstallInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/warnings/{warningId}/acknowledgement` | `RepairSafetyController.Acknowledge` | RepairSafetyAcknowledgementInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/assessment` | `RepairExecutionController.Assess` | RepairMeasurementAssessmentInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts` | `RepairExecutionController.SubmitAttempt` | RepairAttemptSubmitInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts/reviews` | `RepairExecutionController.ReviewAttempt` | RepairAttemptReviewInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts/supplements` | `RepairExecutionController.SupplementAttempt` | RepairAttemptSupplementInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/execution-finish` | `RepairExecutionController.Finish` | RepairExecutionFinishInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/execution-start` | `RepairExecutionController.Start` | RepairExecutionStartInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-policies/drafts` | `RepairPoliciesController.Create` | RepairPolicyDefinitionInput | Idempotency-Key | 201, 200, 400, 401, 403, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/repair-policies/drafts/{draftId}` | `RepairPoliciesController.Draft` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `PUT /api/v1/projects/{projectId}/repair-policies/drafts/{draftId}` | `RepairPoliciesController.Update` | RepairPolicyDefinitionInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/repair-policies/drafts/{draftId}/publish` | `RepairPoliciesController.Publish` | RepairPolicyReasonInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}` | `RepairPoliciesController.Revision` | none | - | 200, 400, 401, 403, 404, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}/revoke` | `RepairPoliciesController.Revoke` | RepairPolicyReasonInput | Idempotency-Key, If-Match | 201, 200, 400, 401, 403, 404, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/reports/items` | `ReportingController.Items` | none | - | 200, 400, 401, 403, 404, 422, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/reports/summary` | `ReportingController.Summary` | none | - | 200, 400, 401, 403, 404, 422, 500 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/reports/timeline` | `ReportingController.Timeline` | none | - | 200, 400, 401, 403, 404, 422, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/retention/evaluations` | `RetentionController.Evaluate` | CreateRetentionEvaluationRequest | Idempotency-Key | 202, 401, 403, 404, 409, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/projects/{projectId}/retention/evaluations/{id}` | `RetentionController.Evaluation` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `GET /api/v1/projects/{projectId}/retention/files/{fileId}` | `RetentionController.File` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `PUT /api/v1/projects/{projectId}/retention/files/{fileId}/basis` | `RetentionController.Basis` | ConfirmRetentionBasisRequest | Idempotency-Key, If-Match | 200, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/projects/{projectId}/road-coverage` | `RoadCoverageController.Read` | none | - | 200, 401, 403, 400, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/road-coverage` | `RoadCoverageController.Confirm` | RoadCoverageInputDto | Idempotency-Key, If-Match | 201, 200, 401, 403, 400, 409, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/road-geometry-drafts` | `GeometryWorkflowController.CreateDraft` | GeometryDraftInput | Idempotency-Key | 201, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}` | `GeometryWorkflowController.Draft` | none | - | 200, 400, 401, 403, 404, 412, 422, 500 | Bearer OR WebSession |
| `PUT /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}` | `GeometryWorkflowController.EditDraft` | GeometryDraftInput | Idempotency-Key, If-Match | 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/confirm` | `GeometryWorkflowController.Confirm` | GeometryConfirmInput | Idempotency-Key, If-Match | 201, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/preview` | `GeometryWorkflowController.Preview` | none | - | 200, 400, 401, 403, 404, 412, 422, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/readiness` | `GeometryWorkflowController.Readiness` | none | - | 200, 400, 401, 403, 404, 412, 422, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/road-sections` | `ProjectRoadSectionsController.Create` | CreateRoadSectionRequestDto | - | 201, 400, 401, 403, 404, 409, 500, 415 | Bearer |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts` | `GeometryWorkflowController.CreateDraft` | GeometryDraftInput | Idempotency-Key | 201, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}` | `GeometryWorkflowController.Draft` | none | - | 200, 400, 401, 403, 404, 412, 422, 500 | Bearer OR WebSession |
| `PUT /api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}` | `GeometryWorkflowController.EditDraft` | GeometryDraftInput | Idempotency-Key, If-Match | 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}/confirm` | `GeometryWorkflowController.Confirm` | GeometryConfirmInput | Idempotency-Key, If-Match | 201, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}/preview` | `GeometryWorkflowController.Preview` | none | - | 200, 400, 401, 403, 404, 412, 422, 500 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans` | `SurveyPlanningController.CreatePlan` | CreateSurveyPlanRequestDto | - | 201, 400, 401, 403, 404, 409, 422, 500, 415 | Bearer |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-requests` | `SurveyPlanningController.CreateRequest` | CreateSurveyRequestRequestDto | - | 201, 400, 401, 403, 404, 409, 422, 500, 415 | Bearer |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions` | `ProjectRoadSectionsController.CreateVersion` | CreateRoadSectionVersionRequestDto | - | 201, 400, 401, 403, 404, 409, 500, 415 | Bearer |
| `GET /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/geometry` | `GeometryWorkflowController.Geometry` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets` | `GeometryWorkflowController.CreateSet` | SegmentDefinition | Idempotency-Key | 201, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}` | `GeometryWorkflowController.Set` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `PUT /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}` | `GeometryWorkflowController.EditSet` | SegmentDefinition | Idempotency-Key, If-Match | 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}/publish` | `GeometryWorkflowController.Publish` | SegmentPublishInput | Idempotency-Key, If-Match | 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/preview` | `GeometryWorkflowController.PreviewSet` | SegmentDefinition | - | 200, 400, 401, 403, 404, 412, 422, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/projects/{projectId}/route-systems` | `GeometryWorkflowController.Systems` | none | - | 200, 400, 401, 403, 404, 412, 500 | Bearer OR WebSession |
| `POST /api/v1/projects/{projectId}/route-systems` | `GeometryWorkflowController.CreateSystem` | RouteSystemInput | Idempotency-Key | 201, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/route-versions/{routeVersionId}/segment-sets/{segmentSetId}/pavement-plans` | `PavementWorkflowController.CreatePlan` | PavementPlanCreateInput | Idempotency-Key | 201, 200, 400, 401, 403, 404, 409, 412, 422, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/projects/{projectId}/survey-plans` | `SurveyV2Controller.CreatePlan` | CreateSurveyPlanV2RequestDto | Idempotency-Key | 201, 401, 403, 404, 409, 422, 428, 500, 400, 415 | Bearer |
| `POST /api/v1/projects/{projectId}/survey-plans/{surveyPlanId}/postpone` | `SurveyPlanningController.PostponePlan` | PostponeSurveyPlanRequestDto | - | 200, 400, 401, 403, 404, 409, 422, 500, 415 | Bearer |
| `POST /api/v1/projects/{projectId}/survey-tasks` | `SurveyV2Controller.CreateTask` | CreateSurveyTaskV2RequestDto | Idempotency-Key | 201, 401, 403, 404, 409, 422, 428, 500, 400, 415 | Bearer |
| `POST /api/v1/projects/{projectId}/validation-runs` | `ProcessingV2Controller.CreateValidationRun` | CreateValidationRunRequestDto | Idempotency-Key | 202, 401, 403, 409, 422, 428, 500, 400, 415 | Bearer |
| `POST /api/v1/projects/{projectId}/warranties` | `ProjectWarrantiesController.Create` | CreateWarrantyRequestDto | - | 201, 400, 401, 403, 404, 409, 500, 415 | Bearer |
| `GET /api/v1/projects/{projectId}/weekly-digests/{digestId}` | `H6NotificationOperationsController.WeeklyDigest` | none | - | 200, 401, 404, 500, 403 | Bearer OR WebSession |
| `GET /api/v1/projects/{projectId}/work-package` | `ProjectWorkPackagesController.Get` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `GET /api/v1/reporter-evidence/files/{fileId}` | `ReporterEvidenceController.GetFileMetadata` | none | - | 200, 401, 404, 500, 403 | Bearer |
| `GET /api/v1/reporter-evidence/files/{fileId}/content` | `ReporterEvidenceController.Download` | none | - | 200, 401, 404, 409, 503, 500, 403 | Bearer |
| `GET /api/v1/reporter-evidence/publications/{publicationId}/reports/{reportId}/evidence/{evidenceId}/content` | `ReporterEvidenceController.DownloadPublication` | none | - | 200, 401, 404, 409, 503, 403, 500 | Bearer |
| `POST /api/v1/reporter-evidence/uploads` | `ReporterEvidenceController.Create` | ReporterEvidenceCreateRequestDto | Idempotency-Key | 201, 401, 403, 404, 409, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/reporter-evidence/uploads/{uploadId}` | `ReporterEvidenceController.GetSession` | none | - | 200, 401, 404, 500, 403 | Bearer |
| `POST /api/v1/reporter-evidence/uploads/{uploadId}/complete` | `ReporterEvidenceController.Complete` | UploadCompleteRequestDto | Idempotency-Key, If-Match | 202, 401, 404, 409, 412, 422, 428, 500, 400, 415, 403 | Bearer |
| `POST /api/v1/reporter-evidence/uploads/{uploadId}/part-urls` | `ReporterEvidenceController.GetPartUrls` | UploadPartUrlsRequestDto | Idempotency-Key | 200, 401, 404, 409, 422, 428, 503, 500, 400, 415, 403 | Bearer |
| `GET /api/v1/reports` | `ReportsController.List` | none | - | 200, 401, 403, 404, 503, 400, 500 | Bearer OR WebSession |
| `POST /api/v1/reports` | `ReportsController.Create` | CreateReporterReportRequestDto | Idempotency-Key | 201, 401, 403, 404, 503, 400, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `GET /api/v1/reports/{reportId}` | `ReportsController.Read` | none | - | 200, 401, 403, 404, 503, 500 | Bearer OR WebSession |
| `GET /api/v1/reports/{reportId}/evidence/{evidenceId}/download` | `ReportsController.Download` | none | - | 200, 401, 403, 404, 503, 409, 412, 500 | Bearer OR WebSession |
| `POST /api/v1/reports/{reportId}/supplements` | `ReportsController.Supplement` | CreateReporterReportRequestDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 503, 400, 409, 412, 428, 500, 415 | Bearer OR WebSession+CsrfToken+AntiforgeryCookie |
| `POST /api/v1/retention/holds` | `RetentionController.CreateHold` | CreateRetentionHoldRequest | Idempotency-Key | 201, 401, 403, 404, 409, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/retention/holds/{id}` | `RetentionController.Hold` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `POST /api/v1/retention/holds/{id}/release` | `RetentionController.Release` | ReleaseRetentionHoldRequest | Idempotency-Key, If-Match | 200, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/survey-plans/{id}` | `SurveyV2Controller.ReadPlan` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `POST /api/v1/survey-plans/{planId}/postpone` | `SurveyV2Controller.Postpone` | PostponeSurveyPlanV2RequestDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `GET /api/v1/survey-tasks/{id}/work-package` | `SurveyV2Controller.ReadWorkPackage` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `GET /api/v1/survey-tasks/{taskId}` | `SurveyV2Controller.GetTask` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `POST /api/v1/survey-tasks/{taskId}/accept` | `SurveyV2Controller.AcceptTask` | none | Idempotency-Key, If-Match | 200, 401, 403, 404, 409, 412, 422, 428, 500 | Bearer |
| `POST /api/v1/survey-tasks/{taskId}/cancel` | `SurveyV2Controller.CancelTask` | SurveyTaskReasonV2RequestDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `POST /api/v1/survey-tasks/{taskId}/datasets` | `SurveyV2Controller.SubmitDataset` | SubmitDatasetRequestDto | Idempotency-Key, If-Match | 201, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `POST /api/v1/survey-tasks/{taskId}/decline` | `SurveyV2Controller.DeclineTask` | SurveyTaskReasonV2RequestDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `POST /api/v1/survey-tasks/{taskId}/reassign` | `SurveyV2Controller.ReassignTask` | ReassignSurveyTaskV2RequestDto | Idempotency-Key, If-Match | 200, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `POST /api/v1/survey-tasks/{taskId}/supplements` | `SurveyV2Controller.RequestSupplement` | SupplementSurveyTaskV2RequestDto | Idempotency-Key, If-Match | 201, 401, 403, 404, 409, 412, 422, 428, 500, 400, 415 | Bearer |
| `POST /api/v1/uploads` | `UploadsController.Create` | UploadCreateRequestDto | Idempotency-Key | 201, 428, 401, 403, 409, 422, 404, 500, 400, 415 | Bearer |
| `GET /api/v1/uploads/{uploadId}` | `UploadsController.GetSession` | none | - | 200, 401, 403, 404, 500 | Bearer |
| `POST /api/v1/uploads/{uploadId}/complete` | `UploadsController.Complete` | UploadCompleteRequestDto | Idempotency-Key, If-Match | 202, 428, 401, 403, 404, 409, 422, 412, 500, 400, 415 | Bearer |
| `POST /api/v1/uploads/{uploadId}/part-urls` | `UploadsController.GetPartUrls` | UploadPartUrlsRequestDto | Idempotency-Key | 200, 428, 401, 403, 404, 409, 422, 503, 500, 400, 415 | Bearer |
| `GET /api/v1/users` | `StaffDirectoryController.Users` | none | - | 200 | Bearer OR WebSession |
| `GET /api/v1/users/{userId}` | `UsersController.Get` | none | - | 200, 400, 403, 404, 409, 500, 401 | Bearer OR WebSession |
| `PATCH /api/v1/users/{userId}` | `UsersController.Update` | AccountUpdateRequestDto | Idempotency-Key, If-Match | 200, 400, 403, 404, 409, 412, 428, 500, 415, 401 | Bearer |
| `POST /api/v1/users/{userId}/password-reset` | `UsersController.ResetPassword` | AdminResetPasswordV2RequestDto | Idempotency-Key | 204, 400, 403, 404, 409, 500, 415, 401 | Bearer |
| `GET /api/v1/validation-runs/{runId}` | `ProcessingV2Controller.GetValidationResult` | none | - | 200, 401, 404, 500, 403 | Bearer |

## Static-contract caveats

237 legacy `x-roadguard-api-id` operations remain instrumented; four additive GETs bring production total to241. Existing ManualSwagger source-excerpt test fails: **11 of2,533 unique excerpts** are stale after project/Supervisor/staff changes; expected route identity tests can pass while evidence strings drift. Do not present that failure as241 functional API failures. Source expectations/backend embedded Swagger files were not edited (G07).

V2 YAML/FE snapshot bytes currently match each other, but FE lock hash differs (G06); successful133-task alignment check only proves that old draft manifest, not runtime route equivalence. Runtime Swagger schema is observation; consumer adoption/public compatibility requires review.
