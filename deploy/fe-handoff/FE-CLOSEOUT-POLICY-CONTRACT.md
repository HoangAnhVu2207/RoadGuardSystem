> CURRENT_VERIFIED iteration 8 final (2026-10-23): exact Owner-approved B2 implemented and isolated verification executed. Genuine SUBMITTED and PM-final Supervisor delivery now have Backend SQL producer-to-inbox evidence: global authority, reassignment, recipient-specific revocation, retry/dedup, concurrency and rollback. No Supervisor approval transition or Normal clock reuse. RC7 build/cold-start, collections/AI frames, B2 HTTP, browser and restart persistence PASS within recorded bounds. OpenAPI documentation errata corrected against exact260-operation runtime; original failures retained. See B2-IMPLEMENTATION-ACCEPTANCE.md for evidence/limits. Independent implementation review PENDING; FE_INTEGRATION_PREVIEW, FULL_CONTRACT=0. Historical unsupported/pending/RC5/RC6-in-progress statements below are superseded only for current B2. No Git publication.

# Closeout policy and integration contract

Current implementation and bounded runtime evidence; independent acceptance pending. Earlier Task34 gate language is HISTORICAL where superseded here. Release remains FE_INTEGRATION_PREVIEW / PARTIAL.

| Policy | Implemented behavior | Verified boundary / limitation |
|---|---|---|
| A1 | Dedicated RS256/kid keyring, roadguard-backend → roadguard-ai-callback, 300 seconds, separate service subject, current job/attempt claims; internal dispatch mints only committed current attempt | Unit and real authentication/SQL callback regression. No public mint. External AI transport NOT_VERIFIED. |
| B2 | CURRENT_VERIFIED: exact item-scoped GET/PUT/DELETE notification assignment; global Supervisor authority without ProjectMember; versioned snapshots and recipient-specific revocation. Genuine FastTrack intake snapshots PMs at committed SUBMITTED; PM-final emits a separate immutable informational event. | Current source and isolated producer-to-inbox SQL support genuine Supervisor delivery for both events, reassignment, dedup/retry and revocation. See B2-IMPLEMENTATION-ACCEPTANCE.md and review-evidence/iteration10-complete/current-test-results.json. H3 receipt/attempt/link/review-clock timing remains original; SUBMITTED event time is genuine intake. No Supervisor approval or Normal clock reuse. These bounded results do not grant overall release acceptance. |
| C1 | Separate public publication, strict current PM/project and verified Defect/confirmed concluded Case/verified evidence; CRACK/POTHOLE active catalog allowlist; immutable four-field sanitized snapshot | 18 C1 SQL/HTTP cases and additive migration compatibility PASS; private-publication regression PASS. No private CasePublication semantics changed. |
| D1 | No automatic orphan deletion | Remains disabled. |

## A1 deployment

`AiServiceJwt` is separate from human `Jwt` configuration. Enable only with deployment-owned `Subject`, `ActiveKeyId`, `PrivateKeyPem` and `PublicKeys` map keyed by kid. Supply keys outside Git through protected configuration; never copy keys into FE .env or responses. Use RSA >=2048 bits, retain verification keys during rotation, and issue tokens only through the internal current-attempt dispatch adapter. There is no public token exchange endpoint. Default independent Development distribution keeps external callback disabled; internal synthetic Mock AI has its existing separate Development guard. Do not turn Mock AI into production authentication bypass.

## C1 FE screen → API → next action

| Screen/action | Request | Response/next |
|---|---|---|
| Project PM publication approval | POST /api/v1/projects/{projectId}/defects/{defectId}/publications; authenticated current PM, Idempotency-Key header, no JSON body | 201 + public DTO/Location; 400 invalid key, 403 authority, 404 wrong/missing project target, 409 source not ready or key conflict, 412 stale evidence, 428 missing key. Read publicId through anonymous route. |
| Public detail | GET /api/v1/public/publications/{publicId}; anonymous | 200 exactly publicId, damageCategory, publicStatus=PUBLISHED, publishedAt; missing/withdrawn404; no-store. No list/search/media API. |
| Project PM withdraw | POST /api/v1/projects/{projectId}/publications/{publicId}/withdraw; current PM + Idempotency-Key, no body | 200; subsequent anonymous GET404. Originals retained. Replays still recheck current PM authority. |

Publication accepts only exact active CRACK/POTHOLE → ROAD_SURFACE_DAMAGE, Defect strictly Verified with Confirm verification log, valid Case current CONCLUDED/latest CONFIRMED including same Defect and verified linked current evidence. Every other code including RG_POSTMAN_POTHOLE denies. No fallback category, public coordinates, IDs of private records, file URLs, names or summary. Publication is explicit; seed does not auto-publish data. Current strict source eligibility is also rechecked on create replay.

C1 routes are three approved additive operations outside canonical241 and the eleven G02–G06 collection operations. No FULL_CONTRACT promotion is inferred. Test fixtures are controlled Backend lineage; full W01–W10 browser journeys remain a separate acceptance gate.

## Web transport correction — independent review Finding 01

The eleven approved G02-G06 collection GETs accept either Bearer or the existing HTTPS Web session cookie. GET requires no CSRF token. C1 create/withdraw accept Bearer or Web session cookie plus the session-bound X-CSRF-TOKEN and antiforgery cookie. Obtain a fresh CSRF token after Web login. Idempotency-Key and current PM/project authority still apply. Invalid Bearer never falls back to cookie. Revoked sessions deny access. Anonymous public GET is unchanged and requires neither authentication nor CSRF; no-store and the four-field DTO remain in force.

This corrects transport eligibility only: no new route, DTO, role, publication eligibility, collection cursor profile or B2 assignment source. The OpenAPI security alternatives are generated from the same endpoint metadata. Bounded focused HTTP evidence is recorded in the review-fix handoff; this is not independent acceptance or full browser W01-W10 journey acceptance.

## HISTORICAL B2 source boundary before exact assignment approval

Before OWNER_B2_EXACT_CONTRACT_APPROVED and its implementation, genuine FastTrack had no authoritative Supervisor assignment producer; Supervisor-positive was NOT_SUPPORTED_BY_CURRENT_SOURCE. That limitation described the earlier clock-only source. It is superseded by the approved notification-only assignment and the separate SUBMITTED/PM-final events; it is not the current API behavior. Original evidence remains retained.
