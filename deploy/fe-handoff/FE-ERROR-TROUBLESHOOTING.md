# Safe request troubleshooting

Use the exact method/path/schema in openapi.json. Capture status, problem code and correlation ID; omit tokens, OTPs, signed URLs and credentials. Read actual problem codes: 400 DTO validation,401 authentication,403 current authority,404 possible concealment,409 lifecycle/stale/idempotency conflicts,422 semantic validation. Preserve quoted ETag in If-Match where required; body rowVersion is a separate representation. Retry only with the documented idempotency contract.

CORS requires exact allowed Origin and host port. Cookie requests require credentials, Secure-cookie HTTPS and the actual CSRF token/header. HTTP bearer success does not prove browser acceptance. Do not disable CSRF or enable broad origins to hide consumer configuration issues.

For /ready503, inspect bounded redacted service health/logs. SQL must be the owned internal target. Stop on ownership uncertainty or fresh EXT4/JBD2 I/O. Scenarios nonzero requires preserving journal/volumes and diagnosing the failed operation, not resetting data. OTPs are local Mailpit, expiring and single-use. MinIO403 requires a fresh authorized capability, never a public bucket. Worker recovery follows existing leases/receipts; do not fabricate SUCCEEDED or automatically delete orphans.
