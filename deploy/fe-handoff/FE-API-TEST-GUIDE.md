> Current Candidate3 execution: [bounded status and limitations](FE-STARTER-CANDIDATE3-STATUS.md). Candidate1 results below retain their original artifact identity.

# Test Backend integration from FE

Use [quickstart](FE-INTEGRATION-QUICKSTART.md), [HTTPS setup](FE-HTTPS-LOCAL.md), [authentication](FE-AUTH-AND-CLIENT-GUIDE.md), [business flows](FE-BUSINESS-FLOW-GUIDE.md) and [functional contracts](FE-API-FUNCTIONAL-GUIDE.md). FE chooses its own screens. No actual FE, native Android or external AI acceptance is claimed.

## Environment and credentials

Create an independently owned Compose environment using free ports and its own state/SQL/object volumes. Never point acceptance commands at shared SQL or MinIO. Wait for scenario completion and /ready200; /health alone proves only process liveness. Preserve named volumes across restarts. Do not use down -v or regenerate an existing environment's credentials.

Candidate1 evidence uses project rg-fe-preview-20261024a, HTTP18584 and HTTPS18946. These describe the recorded local run, not portable credentials or a production deployment. Candidate archive and source hashes are in [the manifest](review-evidence/fe-preview-closeout/CLOSEOUT-MANIFEST.json). The immutable archive precedes this documentation supplement; use the separately hashed current collection/guides when reviewing examples.

Read generated accounts privately as described in quickstart. Keep demoEmail/demoPassword/accessToken, OTPs and cookies out of exports/logs/issues. Read actual synthetic IDs from the scenario journal and subsequent authorized API responses. Uploaded evidence must reach Backend verified state before dependent commands.

## Run the maintained collection

From repository root, with installed Newman and a private local environment file:

```powershell
newman run deploy/fe-handoff/FE-API.postman_collection.json -e <private-local-environment.json> --reporters cli
```

Import the same collection into Postman for interactive use. Set baseUrl, demoEmail, demoPassword, projectId and defectId locally. The account must be the current PM of that project. defectId must be a genuine W04 eligible verified CRACK/POTHOLE with active catalog, verification log, CONCLUDED/CONFIRMED Case containing the Defect and valid verified linked evidence. C1 setup is not a synthetic SQL insertion. Login captures accessToken; publication captures publicId; the withdrawal probe is anonymous.

Recorded on candidate1: Newman6.2.2, seven items/eight requests/18 assertions/zero failures. Readiness, Android login, profile, projects and C1 create/read/withdraw are covered. C1 has nine additional checks, including missing/wrong-project/wrong-role/unauthorized and privacy. These counts are not 260 operation acceptance. Raw references and exact hashes: [evidence index](review-evidence/fe-preview-closeout/RAW-EVIDENCE-INDEX.json).

For intended replay keep the original Idempotency-Key and payload. Dynamic collection GUIDs represent fresh commands; pin a key before testing replay. Read the returned ETag and send its exact quoted value as If-Match where declared. Do not reuse an already withdrawn publication as a new positive read fixture.

## Web and Android checks

Android uses /auth/android/login and Bearer over validated TLS. Web uses HTTPS, credentials:include, CSRF bootstrap/login/bootstrap/session and explicit cookie-eligible actions. Protected cookie mutations require the identity-matched X-CSRF-TOKEN; GETs do not. Use the browser module in examples/roadguard-web-client.mjs. A CLI Bearer success does not certify browser cookies/CORS. Browser-v2 evidence is bounded; its isolated test ignored TLS trust inside the test context, without changing OS trust.

Test current valid actor, wrong role, wrong project/owner, revoked session/assignment and unauthenticated access using distinct owned fixtures. Use the operation's exact status/error contract; do not assume every denial is403. For C1 anonymous GET, missing/withdrawn404 and published200/no-store/exact four fields are intentional privacy behavior. API-238 must never expose contact fields. A1 service callback is Backend-only and is not part of the FE collection.

## Verify state and report failures

Follow the ordered W01–W10 flows and inspect the authoritative detail/result after writes. A202/enqueue or inbox record alone does not establish completed work, external delivery or durable final resolution. Refresh after409/412 and reconcile intent; never automatically replace versions or idempotency keys. After a restart, verify the same resource/receipt persists before retrying.

Not all W01–W10 sequences have current candidate acceptance. [Operation scope](review-evidence/fe-preview-closeout/FE-PREVIEW-OPERATION-SCOPE.json) and [gate inputs](review-evidence/fe-preview-closeout/PREVIEW-GATE-INPUTS.json) distinguish source-matched reuse, missing predicate, executed example and source-provenance gaps. Do not treat a documentation link as an executed assertion.

Report method/path, environment/artifact SHA, timestamp, sanitized correlation ID, expected/actual HTTP status/problem code, role and minimal ordered reproduction. Use [the issue template](FE-ISSUE-REPORT-TEMPLATE.md). Omit JWTs/passwords/cookies/OTP, private URLs/media/storage keys and personal data. Full R1 certification, production release, external integrations and actual FE sign-off remain separate gates.
