# ANH-01 - Identity, session and account persistence

- Owner/branch: Anh / anh
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: data-fixture
- sourceCheckpoint: record HEAD and canonical OpenAPI hash at task start

## Task goal
Establish verified persistence facts for reporter onboarding, login/refresh/logout, password recovery/change, invitation acceptance, profile/account reads and account administration. Anh proves entity, repository, constraint, transaction, idempotency and concurrency behavior so Huy can implement the API without reaching into EF.

## Business context
Identity is the trust boundary for every later project operation. Reporter OTP verifies email once; it is not an OTP-per-login flow. Sessions, password changes, invitation replay, disabled users and account scope must preserve actor identity and audit evidence.

## Operation trace
V2-P1-001..015: login, refresh, logout, password recovery/change, reporter registration/verify/resend, invitation accept/create, me/account read/update/admin reset. Historical cards remain the individual contract references.

## Sources to read
- docs/design/02_Requirements/01_FRD_SRS.md FR-01..03; 02_Business_Rules.md identity rules.
- docs/design/03_Data/ERD_Auth_Identity.md, 01_Data_Dictionary.md section 3.1, 03_Domain_Model_V2.md.
- docs/design/05_Technical/02_Auth_Permission_Model.md sections 5.1, 5.4, 5.6; 05_Sequence_Diagrams.md SQ-06.
- docs/adr/002-authentication.md and docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md.
- Current BusinessObjects/Identity, Repositories/Interfaces/Identity, Repositories/Implementations/Identity, DbContext/configuration and Identity SQL tests.

## In scope
- Reconcile current identity entities, repository interfaces and SQL constraints with operation cards and decisions D25, 36A and 37.
- Verify atomic OTP issue/consume/resend, expiry, attempt limit, replay and duplicate email behavior.
- Verify session rotation/revocation, password fingerprint invalidation, invitation token replay and account status transitions.
- Add 1-3 focused SQL tests and minimal persistence fixes only where a failing invariant is inside the approved contract.
- Publish repository fact semantics, version/concurrency tokens, absence/conflict outcomes and fixtures to HUY-01.

## Out of scope
- Service, DTO, controller, HTTP, Postman or API test edits.
- Choosing web-cookie versus token transport, changing TTLs, SMTP/provider delivery or adding SSO.
- New schema/migration or live database application without separately approved scope.
- Rewriting historical auth completion records.

## Exact files and hotspots
Primary: RoadGuardSystem.BusinessObjects/Identity, Repositories/Interfaces/Identity, Repositories/Implementations/Identity, matching configurations and focused tests/RoadGuardSystem.IntegrationTests/Identity. Shared hotspots are RoadGuardDbContext, migrations/snapshot, seed, shared errors and DI; one writer only.

## Stop conditions
Start after Wave 0 and the common base. Stop and record BLOCKED when ERD, migration and current source disagree on a key/constraint; a new policy is needed; SMTP/runtime transport is required; or a schema change is necessary. Do not infer production behavior from a unit or mock test.

## Verification and acceptance
Build Repositories and IntegrationTests from current source. Run focused identity SQL tests with isolated SQL Server and record SQL version, discovered/pass/fail/skip counts, durable rows and replay outcomes. Acceptance requires source evidence, focused SQL evidence for every changed invariant and HUY-01 receiver outcome VERIFIED or NO_CHANGE_NEEDED.

## Completion history
- TODO until all gates pass. Append evidence using TASK_LIFECYCLE.md; do not delete prior history.





