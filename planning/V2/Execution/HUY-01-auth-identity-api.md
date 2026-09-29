# HUY-01 - Identity, session and account API

- Owner/branch: Huy / huy
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- sourceCheckpoint: record HEAD and canonical OpenAPI hash at task start

## Task goal
Expose the approved identity flow through Services, DTOs and API: login/refresh/logout, password recovery/change, reporter registration/OTP, invitations, me/profile and account administration. Preserve stable errors, actor scope, privacy and current compatible clients.

## Business context
Reporter uses one-time email verification, not OTP on every login. Session policy differs by client and remains a compatibility decision. Identity endpoints are prerequisites for all project authorization.

## Operation trace
V2-P1-001..015. Historical operation owner fields do not override ADR 006; Huy owns Service/DTO/API behavior.

## Sources to read
Read the matching operation cards, docs/design requirements FR-01..03, BR identity rules, ERD_Auth_Identity, DD 3.1, Auth_Permission_Model 5.1/5.4/5.6, API Specification, Error Handling, SQ-06, ADR 002/006 and ANH-01 handoff.

## In scope
- Write a 5-8 line contract and current-versus-approved matrix for each operation.
- Map repository facts to DTOs and stable ProblemDetails without exposing OTP/password/token material.
- Verify wrong actor/scope, expired/replayed OTP, disabled account, stale version and idempotent replay behavior.
- Update API.http and existing Postman requests when wire behavior changes; add focused API/unit tests and real HTTP smoke.
- Record missing persistence facts to ANH-01; consume only VERIFIED facts.

## Out of scope
- BusinessObjects, repositories, EF, migrations, live DB, SMTP provider, SSO, new auth policy or web-cookie rollout without decision.
- Rewriting historical operation status or claiming external email delivery from a fake sender.

## Exact files and hotspots
Services/Implementations/Authentication, Services/Interfaces/Authentication, DTOs/Identity/Authentication, API authentication controllers, API.http, Postman and API/unit tests. Shared auth middleware, DI, errors and OpenAPI require one writer.

## Stop conditions
Stop on contract-lock mismatch, repository fact conflict, transport incompatibility, missing authorization rule or external SMTP gate. Mark PARTIAL with exact operations; do not silently choose a new status/header/error.

## Verification and acceptance
Build Services/API/ApiTests fresh. Run focused identity tests with a non-empty filter, then real HTTP smoke checking status, body, headers and durable session/account result. Static Postman validation is separate. Acceptance requires ANH-01 handoff VERIFIED or NO_CHANGE_NEEDED and every changed operation evidenced.

## Completion history
- TODO until all gates pass.



