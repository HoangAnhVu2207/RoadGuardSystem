> TARGET_CONFIRMED — OWNER-FE-UI-AUTONOMY-01 supersedes screen-approval gating. FE designs screens/navigation. Use [business flows](FE-BUSINESS-FLOW-GUIDE.md), [functional contracts](FE-API-FUNCTIONAL-GUIDE.md) and [test instructions](FE-API-TEST-GUIDE.md). Historical mappings below remain references; missing screen labels are not acceptance failures. All 260 operations remain cataloged. Standalone capabilities keep explicit runtime limitations; no frozen certification status changes.

# FE API cookbook — preparation snapshot

Overall status: NOT_VERIFIED. Current candidate1 bounded examples and scope are recorded below; R1/R2/R3 and F01–F10 are not closed. This is navigation over the frozen OpenAPI, not a competing contract or a verified release.

Current preview supplements: [scope and exact missing predicates](review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json), [candidate-tested Postman/C1 instructions](FE-INTEGRATION-QUICKSTART.md#current-candidate1-postman-execution--2026-10-24). API-031 is BACKEND_INTERNAL and requires the service callback contract, never an FE credential/example. Per-operation historical NOT_VERIFIED labels below remain unchanged unless a bounded preview record explicitly attributes evidence.

260 unique method/path entries are indexed. Runtime examples, approved screen mapping and ordered prerequisites remain to be curated per operation. Do not treat inventory completeness as F02 PASS.

Schemas and constraints are included in [FE-API-CATALOG.json](FE-API-CATALOG.json); canonical definitions remain in [openapi.json](openapi.json).

## API-001 — POST `/api/v1/admin/users/{userId}/password-reset`

Reset password admin users resources. Permissions: Supervisor.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-002 — POST `/api/v1/auth/android/login`

Android login auth resources. Permissions: Anonymous credentials / current session.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-003 — POST `/api/v1/auth/android/refresh`

Android refresh auth resources. Permissions: Anonymous credentials / current session.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-004 — POST `/api/v1/auth/change-password`

Change password auth resources. Permissions: Current active user.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 204. Errors: 400, 401, 409, 403, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-005 — POST `/api/v1/auth/forced-password-change`

Forced password change auth resources. Permissions: Anonymous credentials / current session.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 204. Errors: 400, 401, 409, 403, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-006 — POST `/api/v1/auth/login`

Login auth resources. Permissions: Anonymous credentials / current session.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-007 — POST `/api/v1/auth/logout`

Logout auth resources. Permissions: Current active user.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 204. Errors: 401, 400, 403, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-008 — POST `/api/v1/auth/password-recovery-requests`

Request password recovery auth resources. Permissions: Anonymous credentials / current session.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 400, 401, 403, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-009 — POST `/api/v1/auth/refresh`

Refresh auth resources. Permissions: Anonymous credentials / current session.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-010 — POST `/api/v1/auth/reporter-registrations`

Register reporter registrations resources. Permissions: Anonymous Reporter applicant.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 400, 409, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-011 — POST `/api/v1/auth/reporter-registrations/resend`

Resend reporter registrations resources. Permissions: Anonymous Reporter applicant.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 400, 409, 404, 429, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-012 — POST `/api/v1/auth/reporter-registrations/verify`

Verify reporter registrations resources. Permissions: Anonymous Reporter applicant.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 409, 404, 429, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-013 — GET `/api/v1/auth/web/csrf`

Get the CSRF token for this browser session.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-014 — POST `/api/v1/auth/web/login`

Sign in to a browser session. Supply the CSRF token from the bootstrap endpoint.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 403, 400, 401, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-015 — POST `/api/v1/auth/web/logout`

Sign out of the current browser session. Supply its CSRF token and a new Idempotency-Key.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 204. Errors: 400, 401, 403, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-016 — POST `/api/v1/auth/web/renew`

Renew the browser session using its renewal cookie and CSRF token. No body or Authorization header.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 403, 400, 401, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-017 — GET `/api/v1/auth/web/session`

Read the current browser session. Authentication: RoadGuard web session cookie.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-018 — GET `/api/v1/cases`

List cases resources. Permissions: Supervisor / PM; unassigned readable only by Supervisor.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 503, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-019 — GET `/api/v1/cases/{caseId}`

Read cases resources. Permissions: Supervisor / PM; unassigned readable only by Supervisor.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 503, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-020 — POST `/api/v1/cases/{caseId}/conclusions`

Conclude cases resources. Permissions: PM.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 428, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-021 — POST `/api/v1/cases/{caseId}/publications`

Publish cases resources. Permissions: PM.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 428, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-022 — POST `/api/v1/cases/{caseId}/report-links`

Link cases resources. Permissions: PM.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 428, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-023 — POST `/api/v1/cases/{caseId}/report-splits`

Split cases resources. Permissions: PM.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 428, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-024 — POST `/api/v1/cases/{caseId}/triage`

Triage a case using the selected verification method. Requires Supervisor for an UNASSIGNED case or Project Manager where permitted. Use the current case ETag in If-Match and a new Idempotency-Key.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 428, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-025 — GET `/api/v1/datasets/{datasetId}/coverage`

Get dataset coverage survey v2 resources. Permissions: PM / Supervisor / assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-026 — GET `/api/v1/datasets/{id}`

Read dataset survey v2 resources. Permissions: PM / Supervisor / assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-027 — POST `/api/v1/datasets/{id}/assessments`

Create survey assessments resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-028 — GET `/api/v1/datasets/{id}/assessments/{assessmentId}`

Read survey assessments resources. Permissions: PM / Supervisor / assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-029 — GET `/api/v1/files/{fileId}`

Get file metadata uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator. AI frame reads require current AI-result project authority (PM or Supervisor); no signed URL is issued.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-030 — GET `/api/v1/files/{fileId}/content`

Download uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator. AI frame reads require current AI-result project authority (PM or Supervisor); no signed URL is issued.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 503, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-031 — POST `/api/v1/internal/processing-jobs/{jobId}/results`

Submit a processing result. Requires a dedicated RS256 AI service bearer token (issuer roadguard-backend, audience roadguard-ai-callback, TTL 300 seconds), bound to the current jobId and attemptId.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 428, 404, 409, 422, 500, 400, 415, 401, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-032 — POST `/api/v1/invitations`

Create invitations resources. Permissions: Supervisor.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 404, 409, 403, 503, 500, 415, 401.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-033 — POST `/api/v1/invitations/accept`

Accept invitations resources. Permissions: Anonymous invitation recipient.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-034 — POST `/api/v1/labels/{labelId}/review`

Review training labels resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 400, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-035 — GET `/api/v1/me`

Get me resources. Permissions: Current active user (self).

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-036 — PATCH `/api/v1/me`

Update me resources. Permissions: Current active user (self).

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-037 — GET `/api/v1/me/inspection-tasks`

List inspection tasks resources. Permissions: Crew; current own assignments.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-038 — GET `/api/v1/me/survey-tasks`

List my tasks survey v2 resources. Permissions: Assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-039 — GET `/api/v1/notifications`

List notifications resources. Permissions: Current recipient; project/duty scope applies.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-040 — GET `/api/v1/notifications/{notificationId}`

Get notifications resources. Permissions: Current recipient; project/duty scope applies.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 400, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-041 — POST `/api/v1/notifications/{notificationId}/read`

Mark read notifications resources. Permissions: Current recipient; project/duty scope applies.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 404, 409, 412, 428, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-042 — GET `/api/v1/notifications/{notificationId}/scope`

Scope h6 notification operations resources. Permissions: Current recipient; project/duty scope applies.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-043 — POST `/api/v1/offline/uploads`

Create offline uploads resources. Permissions: PM / Crew; valid offline artifact/source.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 415, 422, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-044 — POST `/api/v1/processing-jobs`

Create processing job processing v2 resources. Permissions: PM / Supervisor.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 401, 403, 404, 409, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-045 — GET `/api/v1/processing-jobs/{jobId}`

Get processing job processing v2 resources. Permissions: PM / Supervisor / assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-046 — POST `/api/v1/processing-jobs/{jobId}/retry`

Retry processing job processing v2 resources. Permissions: PM / Supervisor.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-047 — GET `/api/v1/profile`

Get profile resources. Permissions: Current active user (self).

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 400, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-048 — PUT `/api/v1/profile`

Update profile resources. Permissions: Current active user (self).

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 409, 403, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-049 — GET `/api/v1/projects`

Business description pending.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-050 — POST `/api/v1/projects`

Create projects resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-051 — PUT `/api/v1/projects/{projectId}`

Update projects resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-052 — GET `/api/v1/projects/{projectId}/ai-candidates/{detectionId}`

Candidate ai mock runs resources. Permissions: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-053 — POST `/api/v1/projects/{projectId}/ai-mock-runs`

Create ai mock runs resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 401, 403, 404, 428, 422, 409, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-054 — GET `/api/v1/projects/{projectId}/ai-mock-runs/{runId}`

Get ai mock runs resources. Permissions: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-055 — GET `/api/v1/projects/{projectId}/ai-mock-runs/{runId}/result`

Result ai mock runs resources. Permissions: PM / Supervisor; current source authorization (candidate reader uses synthetic fixture provenance).

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-056 — GET `/api/v1/projects/{projectId}/baseline-selections`

Current survey assessments resources. Permissions: PM / Supervisor.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-057 — POST `/api/v1/projects/{projectId}/baseline-selections`

Select survey assessments resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 409, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-058 — GET `/api/v1/projects/{projectId}/baseline-selections/{id}`

History survey assessments resources. Permissions: PM / Supervisor.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-059 — POST `/api/v1/projects/{projectId}/candidate-decisions`

Decide candidates resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 428, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-060 — GET `/api/v1/projects/{projectId}/candidate-decisions/{decisionId}`

Read candidates resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 503, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-061 — GET `/api/v1/projects/{projectId}/candidates`

Match candidates resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 503, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-062 — GET `/api/v1/projects/{projectId}/clocks`

Clocks h6 notification operations resources. Permissions: Current project/digest recipient; current duty scope.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-063 — POST `/api/v1/projects/{projectId}/clocks/{clockId}/appointments`

Appoint clock duty resources. Permissions: PM review duty: PM; Supervisor escalation/final duty: Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-064 — POST `/api/v1/projects/{projectId}/clocks/{clockId}/extensions`

Extend clock duty resources. Permissions: Supervisor: final/escalation; PM: supplement/sync; other kinds denied.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-065 — GET `/api/v1/projects/{projectId}/crs-profiles`

Profiles geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-066 — POST `/api/v1/projects/{projectId}/crs-profiles`

Create profile geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-067 — GET `/api/v1/projects/{projectId}/crs-profiles/{profileId}`

Profile geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-068 — GET `/api/v1/projects/{projectId}/defect-statistics`

Read defect statistics resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-069 — POST `/api/v1/projects/{projectId}/defect-statistics`

Confirm defect statistics resources. Permissions: PM.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 428, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-070 — GET `/api/v1/projects/{projectId}/defects`

List defects resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 400, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-071 — GET `/api/v1/projects/{projectId}/defects/{defectId}`

Read defects resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-072 — POST `/api/v1/projects/{projectId}/defects/{defectId}/assessments`

Assess defects resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 400, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-073 — POST `/api/v1/projects/{projectId}/defects/{defectId}/verification-decisions`

Verify defects resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 400, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-074 — POST `/api/v1/projects/{projectId}/exports`

Create exports resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 400, 401, 403, 404, 409, 422, 428, 503, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-075 — GET `/api/v1/projects/{projectId}/exports/{exportId}`

Get exports resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-076 — GET `/api/v1/projects/{projectId}/exports/{exportId}/content`

Content exports resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 410, 503, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-077 — GET `/api/v1/projects/{projectId}/exports/{exportId}/manifest`

Manifest exports resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-078 — GET `/api/v1/projects/{projectId}/field-inspection-tasks`

List field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-079 — POST `/api/v1/projects/{projectId}/field-inspection-tasks`

Create field inspection workflow resources. Permissions: PM.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-080 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}`

Get field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-081 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/accept`

Accept field inspection workflow resources. Permissions: Assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-082 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/assign`

Assign field inspection workflow resources. Permissions: PM.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-083 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/cancel`

Cancel field inspection workflow resources. Permissions: PM.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-084 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence-reuse`

Reuse field inspection workflow resources. Permissions: PM.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-085 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}`

Evidence field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-086 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}/content`

Download evidence field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500, 503.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-087 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/geometry`

Geometry field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-088 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/history`

History field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-089 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/location-impact-decisions`

Impact field inspection workflow resources. Permissions: PM.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-090 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reassign`

Reassign field inspection workflow resources. Permissions: PM.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-091 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reject`

Reject field inspection workflow resources. Permissions: Assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-092 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/reviews`

Review field inspection workflow resources. Permissions: PM.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-093 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start`

Start field inspection workflow resources. Permissions: Assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-094 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start-origin`

Origin field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-095 — POST `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions`

Submit field inspection workflow resources. Permissions: Assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-096 — GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId}`

Submission field inspection workflow resources. Permissions: PM / Supervisor / assigned Crew.

Workflow: W06. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-097 — GET `/api/v1/projects/{projectId}/geometry-location-impacts`

List impacts pavement workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-098 — POST `/api/v1/projects/{projectId}/geometry-location-impacts`

Create impact pavement workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-099 — GET `/api/v1/projects/{projectId}/geometry-location-impacts/{impactId}`

Get impact pavement workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-100 — POST `/api/v1/projects/{projectId}/geometry-location-impacts/{impactId}/decisions`

Decide impact pavement workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-101 — POST `/api/v1/projects/{projectId}/geometry-map-publications`

Publish pavement workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-102 — GET `/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}`

Manifest pavement workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-103 — GET `/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}/layers/{layer}`

Read geometry features for the selected layer. Supply expectedContentHash in the query or a quoted If-Match hash. If both are supplied, they must agree.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-104 — GET `/api/v1/projects/{projectId}/geometry-package`

Package geometry workflow resources. Permissions: PM / Supervisor / assigned Operator.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-105 — GET `/api/v1/projects/{projectId}/labels`

List training labels resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 400, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-106 — POST `/api/v1/projects/{projectId}/labels`

Create training labels resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 400, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-107 — GET `/api/v1/projects/{projectId}/labels/{labelId}`

Read training labels resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-108 — POST `/api/v1/projects/{projectId}/labels/{labelId}/revisions`

Revise training labels resources. Permissions: PM.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 400, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-109 — GET `/api/v1/projects/{projectId}/lifecycle`

Read project lifecycle resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-110 — POST `/api/v1/projects/{projectId}/lifecycle/{command}`

Execute project lifecycle resources. Permissions: PM: declarations/recurrences; Supervisor: confirmations/closures/transfers.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-111 — POST `/api/v1/projects/{projectId}/lifecycle/renewed-handling-scope`

Renew project lifecycle resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-112 — GET `/api/v1/projects/{projectId}/members`

Business description pending.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: .

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-113 — POST `/api/v1/projects/{projectId}/offline/artifacts`

Register artifact offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-114 — GET `/api/v1/projects/{projectId}/offline/artifacts/{artifactId}`

Get artifact offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-115 — GET `/api/v1/projects/{projectId}/offline/batches/{batchId}`

Get batch offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-116 — POST `/api/v1/projects/{projectId}/offline/devices`

Register device offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-117 — GET `/api/v1/projects/{projectId}/offline/devices/{registrationId}`

Get device offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-118 — POST `/api/v1/projects/{projectId}/offline/devices/{registrationId}/revoke`

Revoke device offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-119 — POST `/api/v1/projects/{projectId}/offline/grants`

Issue grant offline workflow resources. Permissions: Supervisor.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-120 — GET `/api/v1/projects/{projectId}/offline/grants/{grantId}`

Get grant offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-121 — POST `/api/v1/projects/{projectId}/offline/grants/{grantId}/revoke`

Revoke grant offline workflow resources. Permissions: Supervisor.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-122 — POST `/api/v1/projects/{projectId}/offline/import`

Import package offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-123 — GET `/api/v1/projects/{projectId}/offline/origins/{originId}`

Get origin offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-124 — GET `/api/v1/projects/{projectId}/offline/packages/{packageId}`

Get package offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-125 — POST `/api/v1/projects/{projectId}/offline/packages/export`

Export package offline workflow resources. Permissions: Source Crew.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-126 — POST `/api/v1/projects/{projectId}/offline/packages/prepare`

Prepare package offline workflow resources. Permissions: Supervisor.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-127 — POST `/api/v1/projects/{projectId}/offline/snapshots`

Capture snapshot offline workflow resources. Permissions: Assigned Crew.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-128 — GET `/api/v1/projects/{projectId}/offline/snapshots/{snapshotId}`

Get snapshot offline workflow resources. Permissions: PM / Supervisor / Crew; resource-owner restrictions.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-129 — POST `/api/v1/projects/{projectId}/offline/starts/reconcile`

Reconcile start offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-130 — POST `/api/v1/projects/{projectId}/offline/sync`

Synchronize offline workflow resources. Permissions: PM / Crew; original and recipient authority also checked.

Workflow: W09. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 415, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-131 — GET `/api/v1/projects/{projectId}/pavement-layouts/{layoutId}`

Get layout pavement workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-132 — POST `/api/v1/projects/{projectId}/pavement-layouts/as-built`

Create as built pavement workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-133 — GET `/api/v1/projects/{projectId}/primary-project-manager`

Business description pending.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: .

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-134 — PUT `/api/v1/projects/{projectId}/primary-project-manager`

Reassign primary project manager projects resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-135 — GET `/api/v1/projects/{projectId}/receiving-requests`

List business duty resources. Permissions: Current responsible actor / PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-136 — GET `/api/v1/projects/{projectId}/receiving-requests/{requestId}`

Read business duty resources. Permissions: Current responsible actor / PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-137 — POST `/api/v1/projects/{projectId}/receiving-requests/{requestId}/ack`

Ack business duty resources. Permissions: Responsible actor (ack).

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-138 — POST `/api/v1/projects/{projectId}/receiving-requests/{requestId}/appointment`

Appoint business duty resources. Permissions: PM for CrewSupplement; Supervisor otherwise.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-139 — POST `/api/v1/projects/{projectId}/repair-packages`

Create repair packages resources. Permissions: PM.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-140 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items`

Propose repair packages resources. Permissions: PM.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-141 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}`

Item repairs resources. Permissions: PM / Supervisor.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-142 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/approve`

Approve repair packages resources. Permissions: Supervisor.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-143 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/assign`

Assign repair packages resources. Permissions: PM.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-144 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/cancellations`

Cancel repair lifecycle resources. Permissions: PM.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-145 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/corrections`

Correct repairs resources. Permissions: Normal: Supervisor; FT: PM (current final-approver role).

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-146 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/eligibility`

Get repair eligibility resources. Permissions: Current scoped PM / Supervisor / assigned Crew; repository authority.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-147 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/final-confirmation`

Confirm final repair packages resources. Permissions: Normal: Supervisor; FT: PM.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-148 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/history`

History repairs resources. Permissions: PM / Supervisor.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-149 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/normal-successors`

Continue normally repair lifecycle resources. Permissions: PM.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-150 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/review-requests`

Request review repairs resources. Permissions: PM / assigned Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-151 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures`

Create repair safety resources. Permissions: PM.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-152 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}`

Read repair safety resources. Permissions: PM / Supervisor / responsible Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-153 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/checks`

Check repair safety resources. Permissions: Responsible Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-154 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/installation`

Install repair safety resources. Permissions: Responsible Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-155 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}/warnings/{warningId}/acknowledgement`

Acknowledge repair safety resources. Permissions: Responsible Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-156 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/assessment`

Assess repair execution resources. Permissions: Assigned Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-157 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts`

Submit attempt repair execution resources. Permissions: Assigned Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-158 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts/reviews`

Review attempt repair execution resources. Permissions: PM.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-159 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/attempts/supplements`

Supplement attempt repair execution resources. Permissions: Assigned Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-160 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/execution-finish`

Finish repair execution resources. Permissions: Assigned Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-161 — POST `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/tasks/{taskId}/execution-start`

Start repair execution resources. Permissions: Assigned Crew.

Workflow: W07. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-162 — POST `/api/v1/projects/{projectId}/repair-policies/drafts`

Create repair policies resources. Permissions: PM.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-163 — GET `/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}`

Draft repair policies resources. Permissions: PM.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-164 — PUT `/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}`

Update repair policies resources. Permissions: PM.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-165 — POST `/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}/publish`

Publish repair policies resources. Permissions: PM.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-166 — GET `/api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}`

Revision repair policies resources. Permissions: PM.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-167 — POST `/api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}/revoke`

Revoke repair policies resources. Permissions: PM.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-168 — GET `/api/v1/projects/{projectId}/reports/items`

Items reporting resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-169 — GET `/api/v1/projects/{projectId}/reports/summary`

Summary reporting resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-170 — GET `/api/v1/projects/{projectId}/reports/timeline`

Timeline reporting resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-171 — POST `/api/v1/projects/{projectId}/retention/evaluations`

Evaluate retention resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 401, 403, 404, 409, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-172 — GET `/api/v1/projects/{projectId}/retention/evaluations/{id}`

Evaluation retention resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-173 — GET `/api/v1/projects/{projectId}/retention/files/{fileId}`

File retention resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-174 — PUT `/api/v1/projects/{projectId}/retention/files/{fileId}/basis`

Basis retention resources. Permissions: Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-175 — GET `/api/v1/projects/{projectId}/road-coverage`

Read road coverage resources. Permissions: PM / Supervisor.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 400, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-176 — POST `/api/v1/projects/{projectId}/road-coverage`

Confirm road coverage resources. Permissions: Supervisor.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 401, 403, 400, 409, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-177 — POST `/api/v1/projects/{projectId}/road-geometry-drafts`

Create draft geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-178 — GET `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}`

Draft geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-179 — PUT `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}`

Edit draft geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-180 — POST `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/confirm`

Confirm geometry workflow resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-181 — POST `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/preview`

Preview geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-182 — GET `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/readiness`

Readiness geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-183 — POST `/api/v1/projects/{projectId}/road-sections`

Create project road sections resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-184 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts`

Create draft geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-185 — GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}`

Draft geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-186 — PUT `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}`

Edit draft geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-187 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}/confirm`

Confirm geometry workflow resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-188 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}/preview`

Preview geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 422, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-189 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-plans`

Create plan survey planning resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 422, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-190 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/survey-requests`

Create request survey planning resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 422, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-191 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions`

Create version project road sections resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-192 — GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/geometry`

Geometry geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-193 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets`

Create set geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-194 — GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}`

Set geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-195 — PUT `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}`

Edit set geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-196 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}/publish`

Publish geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-197 — POST `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/preview`

Preview set geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 422, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-198 — GET `/api/v1/projects/{projectId}/route-systems`

Systems geometry workflow resources. Permissions: PM / Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-199 — POST `/api/v1/projects/{projectId}/route-systems`

Create system geometry workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-200 — POST `/api/v1/projects/{projectId}/route-versions/{routeVersionId}/segment-sets/{segmentSetId}/pavement-plans`

Create plan pavement workflow resources. Permissions: PM.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 412, 422, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-201 — POST `/api/v1/projects/{projectId}/survey-plans`

Create plan survey v2 resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 409, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-202 — POST `/api/v1/projects/{projectId}/survey-plans/{surveyPlanId}/postpone`

Postpone plan survey planning resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404, 409, 422, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-203 — POST `/api/v1/projects/{projectId}/survey-tasks`

Create task survey v2 resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 409, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-204 — POST `/api/v1/projects/{projectId}/validation-runs`

Create validation run processing v2 resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 401, 403, 409, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-205 — POST `/api/v1/projects/{projectId}/warranties`

Create project warranties resources. Permissions: Supervisor.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 400, 401, 403, 404, 409, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-206 — GET `/api/v1/projects/{projectId}/weekly-digests/{digestId}`

Weekly digest h6 notification operations resources. Permissions: Current project/digest recipient; current duty scope.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-207 — GET `/api/v1/projects/{projectId}/work-package`

Get project work packages resources. Permissions: Supervisor / current project member; WorkPackageRead policy.

Workflow: W02. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-208 — GET `/api/v1/reporter-evidence/files/{fileId}`

Get file metadata reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-209 — GET `/api/v1/reporter-evidence/files/{fileId}/content`

Download reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 409, 503, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-210 — GET `/api/v1/reporter-evidence/publications/{publicationId}/reports/{reportId}/evidence/{evidenceId}/content`

Download publication reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 409, 503, 403, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-211 — POST `/api/v1/reporter-evidence/uploads`

Create reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 409, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-212 — GET `/api/v1/reporter-evidence/uploads/{uploadId}`

Get session reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-213 — POST `/api/v1/reporter-evidence/uploads/{uploadId}/complete`

Complete reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 401, 404, 409, 412, 422, 428, 500, 400, 415, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-214 — POST `/api/v1/reporter-evidence/uploads/{uploadId}/part-urls`

Get part urls reporter evidence resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 409, 422, 428, 503, 500, 400, 415, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-215 — GET `/api/v1/reports`

List reports resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 503, 400, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-216 — POST `/api/v1/reports`

Create reports resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 503, 400, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-217 — GET `/api/v1/reports/{reportId}`

Read reports resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 503, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-218 — GET `/api/v1/reports/{reportId}/evidence/{evidenceId}/download`

Download reports resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 503, 409, 412, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-219 — POST `/api/v1/reports/{reportId}/supplements`

Supplement reports resources. Permissions: Reporter; own record / named publication recipient.

Workflow: W03. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 503, 400, 409, 412, 428, 500, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-220 — POST `/api/v1/retention/holds`

Create hold retention resources. Permissions: Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 409, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-221 — GET `/api/v1/retention/holds/{id}`

Hold retention resources. Permissions: PM / Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-222 — POST `/api/v1/retention/holds/{id}/release`

Release retention resources. Permissions: Supervisor.

Workflow: W10. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-223 — GET `/api/v1/survey-plans/{id}`

Read plan survey v2 resources. Permissions: PM / Supervisor.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-224 — POST `/api/v1/survey-plans/{planId}/postpone`

Postpone survey v2 resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-225 — GET `/api/v1/survey-tasks/{id}/work-package`

Read work package survey v2 resources. Permissions: PM / Supervisor / assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-226 — GET `/api/v1/survey-tasks/{taskId}`

Get task survey v2 resources. Permissions: PM / Supervisor / assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-227 — POST `/api/v1/survey-tasks/{taskId}/accept`

Accept task survey v2 resources. Permissions: Assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 422, 428, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-228 — POST `/api/v1/survey-tasks/{taskId}/cancel`

Cancel task survey v2 resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-229 — POST `/api/v1/survey-tasks/{taskId}/datasets`

Submit dataset survey v2 resources. Permissions: Assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-230 — POST `/api/v1/survey-tasks/{taskId}/decline`

Decline task survey v2 resources. Permissions: Assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-231 — POST `/api/v1/survey-tasks/{taskId}/reassign`

Reassign task survey v2 resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-232 — POST `/api/v1/survey-tasks/{taskId}/supplements`

Request supplement survey v2 resources. Permissions: PM.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 401, 403, 404, 409, 412, 422, 428, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-233 — POST `/api/v1/uploads`

Create uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: 428, 401, 403, 409, 422, 404, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-234 — GET `/api/v1/uploads/{uploadId}`

Get session uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404, 500.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-235 — POST `/api/v1/uploads/{uploadId}/complete`

Complete uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 202. Errors: 428, 401, 403, 404, 409, 422, 412, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-236 — POST `/api/v1/uploads/{uploadId}/part-urls`

Get part urls uploads resources. Permissions: Scoped Supervisor / PM / Operator / Crew; survey upload requires assigned Operator.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 428, 401, 403, 404, 409, 422, 503, 500, 400, 415.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-237 — GET `/api/v1/users`

Business description pending.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: .

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-238 — GET `/api/v1/users/{userId}`

Get users resources. Permissions: Supervisor.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 403, 404, 409, 500, 401.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-239 — PATCH `/api/v1/users/{userId}`

Update users resources. Permissions: Supervisor.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 403, 404, 409, 412, 428, 500, 415, 401.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-240 — POST `/api/v1/users/{userId}/password-reset`

Reset password users resources. Permissions: Supervisor.

Workflow: W01. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 204. Errors: 400, 403, 404, 409, 500, 415, 401.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## API-241 — GET `/api/v1/validation-runs/{runId}`

Get validation result processing v2 resources. Permissions: PM / Supervisor.

Workflow: W05. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 404, 500, 403.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-01 — GET `/api/v1/projects/{projectId}/ai-mock-runs`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-02 — GET `/api/v1/projects/{projectId}/repair-packages`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-03 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-04 — GET `/api/v1/projects/{projectId}/exports`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-05 — GET `/api/v1/projects/{projectId}/survey-plans`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-06 — GET `/api/v1/projects/{projectId}/survey-tasks`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-07 — GET `/api/v1/projects/{projectId}/processing-jobs`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-08 — GET `/api/v1/projects/{projectId}/offline/devices`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-09 — GET `/api/v1/projects/{projectId}/offline/packages`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-10 — GET `/api/v1/projects/{projectId}/offline/grants`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-11 — GET `/api/v1/projects/{projectId}/retention/evaluations`

Owner-approved additive discovery. Unknown/repeated query keys return 400. Typed summaries exclude private payloads; use current detail authority and ETag for subsequent operations. Responses use private, no-store.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 400, 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-12 — GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets`

Business description pending.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: .

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-13 — GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`

Current project PM manages one notification-only FastTrack Supervisor assignment. Supervisor authority is global; no ProjectMember is required for the recipient. No approval duty is granted. Current authority/resource checks precede envelope validation and historical replay.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: 401, 403, 404.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-14 — PUT `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`

Current project PM manages one notification-only FastTrack Supervisor assignment. Supervisor authority is global; no ProjectMember is required for the recipient. No approval duty is granted. Current authority/resource checks precede envelope validation and historical replay.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201, 200. Errors: 400, 401, 403, 404, 409, 412, 428.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-15 — DELETE `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`

Current project PM manages one notification-only FastTrack Supervisor assignment. Supervisor authority is global; no ProjectMember is required for the recipient. No approval duty is granted. Current authority/resource checks precede envelope validation and historical replay.

Workflow: W08. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 204. Errors: 400, 401, 403, 404, 409, 412, 428.

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-16 — POST `/api/v1/projects/{projectId}/defects/{defectId}/publications`

Business description pending.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 201. Errors: .

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-17 — POST `/api/v1/projects/{projectId}/publications/{publicId}/withdraw`

Business description pending.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: .

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-18 — GET `/api/v1/public/publications/{publicId}`

Business description pending.

Workflow: W04. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: .

Ordered prerequisites, runnable example and runtime response evidence: pending.

## ADD-19 — GET `/ready`

Business description pending.

Workflow: CROSS_WORKFLOW. Screen: `SCREEN_MAPPING_PENDING`.

Status: `NOT_VERIFIED`. See the catalog entry with this operationId for exact parameters, request media/schema, response/error schemas, headers, source pins and literal test references.

Success: 200. Errors: .

Ordered prerequisites, runnable example and runtime response evidence: pending.


## Optional object responses — Owner compatibility decision 2026-10-24

`OWNER-NULL-RESPONSE-COMPATIBILITY-01` preserves the packaged OpenAPI. When no value exists, these optional object properties are absent rather than JSON null:

| Operations | Optional properties |
|---|---|
| API-070/071 | `geometry` |
| API-192 | `wgs84Centerline`, `chainageCalibration` |
| API-194 | definition `nativeAlignment`/`chainageCalibration`; segment `metricGeometry`/`wgs84Geometry` |
| API-215/217 | evidence item `location` |

FE clients should test property presence before rendering geometry, calibration or location. Populated objects retain their declared shape; absence does not mean an empty object or permission to manufacture coordinates. Required properties remain required. This changes response serialization only; stored data and input validation are preserved. `SegmentDefinition` is shared with request DTOs: its null properties are also omitted when a .NET caller serializes it, while JSON deserialization remains unchanged.

Backend-local candidate2 has executed schema checks for all six routes and focused populated/absent-value HTTP/DTO regression. No Web/Android consumer implementation was found in this Backend workspace; actual external consumer compatibility remains pending. These bounded checks do not close full contract certification or grant FE acceptance.
