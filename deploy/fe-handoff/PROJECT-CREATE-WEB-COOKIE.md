# Create project from Web

Status: current source contract for `POST /api/v1/projects` on `huy-review` after this change. The older packaged `openapi.json` is a frozen preview snapshot; the Development runtime `/swagger/v1/swagger.json` contains the current security alternatives and example.

Only an active Supervisor may create a project. The endpoint accepts **either** a Bearer access token **or** the HTTPS Web session cookie. Cookie requests require the browser-managed `__Host-RoadGuardSession` and `__Host-RoadGuardCsrf` cookies plus `X-CSRF-TOKEN`. Obtain `requestToken` from `GET /api/v1/auth/web/csrf` in the same browser session, then send it in that header. Use `credentials: "include"` for cross-origin Web fetches when the server's existing CORS/cookie settings permit that origin. The login flow is `/api/v1/auth/web/login`; refresh the CSRF token after login. Bearer requests do not need CSRF. Do not put Bearer and cookie credentials from different users in one request.

Send `Idempotency-Key` with a new nonblank value for a new create action, or supply body `operationId`. When both are present, body `operationId` takes precedence. Reuse the same key only for an intentional replay of the same payload. The server returns `201` with the same project on replay; a changed fingerprint returns `409 duplicate_request`.

```http
POST /api/v1/projects
Content-Type: application/json
Idempotency-Key: project-create-unique-key
X-CSRF-TOKEN: <requestToken from /api/v1/auth/web/csrf, cookie transport only>

{
  "code": "RG-2026-001",
  "name": "Road maintenance project",
  "primaryPmId": "<existing active PM user ID>",
  "handoverDate": "2026-11-01",
  "warrantyEndDate": "2027-11-01",
  "handoverFileIds": []
}
```

`handoverFileIds: []` means no file is attached. It does **not** remove the handover document, primary PM assignment, warranty, or required dates. `handoverDate` and `warrantyEndDate` remain required; the warranty end cannot precede handover. Replace the example PM ID with an eligible account. The existing project validation, response body, `Location`, `ETag`, and idempotency behavior remain the same for Bearer and cookie clients.

Expected errors are `400 validation_error` (invalid body, missing required dates or key), `401` for an invalid/expired session or Bearer token, `403 access_forbidden` for PM or another unauthorized role, `403 csrf_failed` for a cookie request with missing/invalid antiforgery pair, `404` for an ineligible PM or referenced handover file, and `409` for project-code or idempotency conflict. Responses use `application/problem+json` with `code` and `correlationId`. `PUT /api/v1/projects/{projectId}` and other Project actions do not gain cookie access from this action-level opt-in.

The local HTTP tests exercise real login, cookie selection and CSRF middleware with an isolated SQL database. They do not prove the FE browser's deployed origin/cookie settings. A code push does not update a running API instance; the owner of the runtime FE calls must build and roll out that instance using its existing deployment procedure.
