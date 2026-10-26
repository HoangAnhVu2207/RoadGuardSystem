# Task31 supported synthetic scenarios

These are authenticated API operations against this kit only. Obtain generated accounts locally as described in README-FE-START-HERE.md; keep passwords, bearer tokens, OTP and signed URLs out of shared evidence. No business rows are fabricated directly in SQL. Preserve the FE Session database; do not reset it to replay a scenario.

Project A is `72f84201-c001-4000-8000-000000000001`; Project B ends `0002`. Accounts pm-a/pm-b and reporter-a/reporter-b exercise separate scopes. Use the exported Swagger DTOs and response rowVersion/version verbatim. Generate a new UUID per distinct logical operation and retain its key/body for replay.

## Profile and project road

1. Login through POST /api/v1/auth/android/login with the local synthetic account. Use returned accessToken as bearer for these examples. Web cookie/CSRF remains a separate transport.
2. GET /api/v1/profile, then PUT with displayName, email, expectedRowVersion from that response and operationId. Replay the original stale version with a different operationId: 409 is expected.
3. As supervisor, GET /api/v1/projects/{A}/work-package. PUT /api/v1/projects/{A} preserving name/description/startDate/endDate, using current rowVersion as expectedRowVersion, engineeringUtmSrid 32648 and a new operationId. This CRS is for these synthetic coordinates only.
4. POST /api/v1/projects/{A}/road-sections with code (unique synthetic value), name, srid 32648, coordinates [{"x":500000,"y":1200000},{"x":500250,"y":1200000}], effectiveFrom (UTC), changeReason and operationId. Current success is 201. Exact replay returns the same result; a reporter cannot create roads (403).

## Private reporter evidence and incident

Use reporter-a. POST /api/v1/reporter-evidence/uploads with only fileName, mediaType, sizeBytes and checksumSha256; unknown fields are rejected. Follow the returned upload identity and current Swagger part-URL/complete DTOs. Upload actual synthetic bytes to the signed part URL, preserve ETag and current version, complete with checksum/parts/If-Match and idempotency key, and wait for VERIFIED.

GET /api/v1/reporter-evidence/files/{id}/content as the uploader returns the bytes. Another reporter gets 404 concealment; anonymous API access gets 401 and unsigned private MinIO object access gets 403. Do not publish object keys or signed links. POST /api/v1/reports with description, evidence [{fileId, fileVersion}], locationSource UNKNOWN and idempotency key. Current synthetic intake returns 201 and exact replay returns the same result. The full repair/Fast Track lifecycle is not pre-seeded or certified by this intake.

## Export, retention and local OTP

PM-a can POST /api/v1/projects/{A}/exports with kind DOSSIER, format PDF, includeOriginalFiles false and idempotency key. Admission is 202; poll the job until actual terminal status SUCCEEDED or FAILED. The success string is SUCCEEDED, not COMPLETE. Authenticated content returns a private PDF with no-store; pm-b is denied across projects. The image provides the DejaVu Unicode font. Replaying admission must retain the same job identity.

POST /api/v1/projects/{A}/retention/evaluations with fileIds and idempotency key. COMPLETE can contain WAITING_RETENTION_BASIS. Evaluation is not deletion permission: private bytes remain. This does not prove storage-orphan discovery or approved retention duration.

Register a unique synthetic @sandbox.example.test reporter through /api/v1/auth/reporter-registrations, read its OTP privately in this kit's Mailpit inbox, and POST intentId/otp to /verify. No Gmail is used. Existing registrations and generated credentials are persistent; do not log them or expect registration to create a project authority.

## Feature gates

AI callbacks return 403 in this preview even for locally signed AI_SERVICE credentials. Native Fast Track PM+Supervisor, controlled-publication approval and retention deletion remain parameter-gated. NORMAL notifications, incident intake or a completed retention evaluation must not be described as those gated features. Full workflow fixtures, HTTPS/browser/mobile consumer acceptance and independent release review remain open.
