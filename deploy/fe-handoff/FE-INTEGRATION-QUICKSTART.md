> Current closeout integration: [FE-CLOSEOUT-POLICY-CONTRACT.md](FE-CLOSEOUT-POLICY-CONTRACT.md). Earlier A1/B2/C1 pending-policy language below is HISTORICAL. Release remains preview pending full journey/contract acceptance and independent review.

> Task33 CURRENT UPDATE (supersedes conflicting historical Task30–32 statements below): G01 SegmentSet collection approved by owner and implemented with existing detail authority, status filter and keyset pagination. New isolated deployments enable internal Development Mock AI and create A/B geometry/survey/dataset plus road/empty fixture results. External AI callback/issuer remains blocked. See FE-DISCOVERY-WORKFLOWS.md, FE-MOCK-AI-GUIDE.md, FE-241-API-COVERAGE-MATRIX.md and FE-FEATURE-AVAILABILITY.md. Historical counts below are not fresh Task33 evidence. Rendered243 = canonical241 + G01 + operational /ready.

# Task32 FE integration preview

Requirement: Windows, Docker Desktop in Linux-container mode and Internet for first build. Extract the complete RoadGuardBackend folder. In RoadGuardBackend/deploy/fe-handoff run:

```powershell
docker compose up -d --build
```

No host SDK, Python, SQL tools, manual database or .env is required. Wait for api/sql healthy and scenarios exited with code 0 (`docker compose ps -a`). A detached up returning is not completion of the scenario setup. Check `docker compose logs --tail 50 scenarios` for its non-secret completion message. `/health` means process liveness; `/ready` requires SQL, the private bucket and Mailpit SMTP. Scenario setup is separately observable and must complete before expecting demo records.

API: http://localhost:18081 ; readiness /ready ; Swagger /swagger ; schema /swagger/v1/swagger.json. Mailpit http://localhost:18026 ; private object endpoint http://localhost:19001 . SQL has no host port. Browser clients call localhost addresses, never api/sql/minio internal DNS.

Read generated local accounts privately:

```powershell
docker compose exec -T api cat /handoff/demo-accounts.json
```

Keep the output out of Git, tickets, logs and KnowledgeSpace. Passwords, JWT keys and storage credentials are generated per deployment in the private state volume. Docker operators can access them. Host disk encryption/access controls are required for sensitive local state; this is not production secret management.

Use POST /api/v1/auth/android/login with the selected demo email/password; retain accessToken privately and send Authorization: Bearer. GET /api/v1/me and /api/v1/profile have different DTOs (FE-API-CONTRACT.md). GET /api/v1/projects returns the caller's authorized projects. Supervisor can read both project work-packages; pm-a/pm-b belong to A/B respectively. Project IDs end c001-4000-8000-000000000001 and ...0002 with prefix 72f84201. The full IDs and scenario results are available through:

```powershell
docker compose run --rm --no-deps --entrypoint cat scenarios /progress/scenarios.json
```

The one-shot scenario container exits after setup; this read-only command reads its named progress volume. The journal contains synthetic IDs, versions and input DTOs, no passwords/tokens/signed links. Do not edit it to bypass initialization.

New Task32 deployments create roads A/B (synthetic EPSG:32648), a verified PNG in private MinIO, a reporter-a incident and A/B PDF export jobs. Find file/report/export IDs in that journal. Poll API job/file status before content access. Existing older private configurations are not automatically opted in or upgraded. Do not attach old state or FE volumes to a new deployment to force setup.

For project PDF, GET /api/v1/projects/{id}/exports/{jobId}, then /content or /manifest. For reporter media, GET /api/v1/reporter-evidence/files/{fileId}/content as its uploader. Wrong reporter receives concealment; object keys do not grant public access. Notifications inbox GET /api/v1/notifications may be empty: no artificial business event is inserted. Approved survey/FIELD/repair/Fast Track/publication examples are not pre-seeded. See FEATURE-LIMITS.md.

Stop/start or recreate containers without `-v`; state/sql-data/objects/mail/keys/scenario-progress named volumes persist. Repeated up reuses completed seed; it does not reset developer edits. Never use down -v for an active FE session. To run a separate instance use a different Compose project plus free RG_FE_API_PORT/RG_FE_OBJECT_PORT/RG_FE_MAIL_PORT **before first startup**. Its volumes and credentials are independent. Do not change an existing instance's public object port without a reviewed config migration.

For a failed request record status, problem code/correlationId and route; never paste tokens, OTPs or signed URLs. For failed setup inspect service health, API/worker/scenario logs; correct the dependency and rerun up. Setup fails closed on owner/version mismatch. It never resets an existing DB. SQL/storage/mail outages return readiness503 while liveness may remain200.

Web Secure cookies/CSRF, browser HTTPS trust, native/LAN Expo and actual FE consumer compatibility are external gates. Vite localhost5173/Expo8081 loopback origins are configured; HTTP bearer acceptance does not establish cookie browser acceptance. No Gmail, YOLO or external FE repo is used. Knowledge runner DENY_ALL. This is a bounded preview candidate, not BE_COMPLETE or independent review approval.


## Current candidate1 Postman execution — 2026-10-24

The existing [FE-API.postman_collection.json](FE-API.postman_collection.json) was executed with Newman 6.2.2 against isolated candidate1: seven items, eight HTTP requests (including the post-withdraw probe), 18 assertions, zero failures. This covers readiness, Android login, current profile, projects and the three C1 publication requests; it does not cover every FE operation.

Import the collection into Postman. Set private local variables `baseUrl`, `demoEmail`, `demoPassword`, `projectId` and `defectId`. Use an authorized project PM and a genuine eligible Defect from the W04 recipe: verified CRACK/POTHOLE with active catalog, verification provenance, a CONCLUDED/CONFIRMED Case containing that Defect and verified same-source evidence. No pre-seeded public publication or direct database edits are required or permitted by this example. Run the collection in order. Login captures the token; create captures `publicId`; withdraw checks that a new anonymous read returns404. Do not export populated secret environments.

For a CLI execution with an existing private local environment file:

```powershell
newman run deploy/fe-handoff/FE-API.postman_collection.json -e <private-local-environment.json> --reporters cli
```

The environment must provide the same variables and an unused eligible W04 graph. The actual recorded execution used the same installed Newman library through `review-evidence/fe-preview-closeout/run_postman_candidate.cjs`, with secret variables passed via stdin and a sanitized aggregate reporter. The runner does not print credentials or private responses.

C1 create/withdraw use a current PM Bearer token and a UUID `Idempotency-Key`, with no JSON body. For an intentional retry, retain the original key; the collection's dynamic GUID starts a distinct command, so pin it manually before testing replay. Browser clients instead need the documented eligible Web cookie and CSRF headers. Anonymous GET needs no credential and must return exactly `publicId`, `damageCategory`, `publicStatus`, `publishedAt` with `Cache-Control: no-store`. Expected create201/read200/withdraw200/post-withdraw404. A401 requires session recovery; a403 requires resolving current actor/project authority; a409 requires inspecting the exact ProblemDetails code and current source state rather than blindly retrying.

Raw command/time/hash and aggregate evidence are indexed in [RAW-EVIDENCE-INDEX.json](review-evidence/fe-preview-closeout/RAW-EVIDENCE-INDEX.json). Candidate1 remains immutable; this collection and documentation supplement is hashed separately. FE acceptance, external AI, native Android and overall independent review remain pending.
