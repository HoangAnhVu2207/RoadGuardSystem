# Approved contract special cases — preparation

Status: **NOT_VERIFIED / FE_INTEGRATION_PREVIEW**. These instructions preserve Owner decisions and distinguish current implementation from candidate acceptance. F07 is open until examples and assertions are reconciled against the final candidate. [OpenAPI](openapi.json) supplies the exact operation schemas; [the dimension ledger](review-evidence/milestone2-r1-closure/EXACT-DIMENSION-ACCEPTANCE.json) supplies current evidence and gaps.

## API-238: staff detail

`GET /api/v1/users/{userId}` is Supervisor-only. The Owner approved the frozen four-field boundary: `id`, `displayName`, `role`, `version`. `additionalProperties=false`: neither `email` nor `phoneNumber` belongs in this response. Do not derive a contact screen from this API. Contact persistence and other APIs are separate boundaries.

For a successful response, check the JSON property set as well as status200. Do not silently accept extra fields. Preserve existing authorization/error semantics for unauthenticated and wrong-role callers. Current implementation and executed evidence are recorded in [the API238 decision/execution record](review-evidence/milestone2-r1-closure/API238-OWNER-DECISION-AND-EXECUTION.json). That record is bounded source/HTTP evidence, not final candidate verification or FE acceptance.

## A1: internal AI callback

FE uses authorized job admission/read/retry and file metadata/content APIs. FE must not create service identities, mint JWTs, hold signing keys or call `/api/v1/internal/processing-jobs/{jobId}/results` as an end user. The internal callback uses an independent RS256 service JWT, Backend-controlled mint per job/attempt, issuer `roadguard-backend`, audience `roadguard-ai-callback`, TTL300 seconds and job/attempt claims. Signing keys and rotation configuration stay outside Git and FE assets.

An AI result is not a verified business Defect. Mock AI exercises the development producer/consumer contract; it does not prove an external provider or production deployment. AI frames use current-authority file metadata/content, with no new signed-frame URL.

**Current blocker:** six actual HTTP/SQL tests show that changing callback identity with the same operation key can replay a previous success. Exact replay of an identical completed callback must be distinguished from changed identity and stale attempts. [A1 replay analysis](review-evidence/milestone2-r1-closure/A1-REPLAY-BOUNDARY-OPEN.md) and [raw failure provenance](review-evidence/milestone2-r1-closure/A1-REPLAY-IDENTITY-FAILURE.json) remain open. Legacy-receipt compatibility is pending a new scoped decision. Do not use status200 here as proof of current payload acceptance, and do not claim A1 full acceptance.

## B2: notification-only Supervisor assignment

The exact item resource is:

`/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`

Current project PM manages the resource with GET/PUT/DELETE. Use the exact [Owner-approved assignment contract](B2-EXACT-ASSIGNMENT-PROPOSAL.md) for body, ETag, create/replace/delete preconditions, idempotency limits and status/error precedence. GET is not a write. Initial assignment and replacement have different preconditions; do not send an unconditional PUT or substitute the item's version for the assignment head's ETag. The DTO contains `assignmentId`, `supervisorId`, `status`, `assignedAt`, nullable `revokedAt`, and `version`.

Supervisor authority is global and does not inherently require ProjectMember. Delivery still requires the exact item assignment generation and current recipient authority. This is neither a role-wide broadcast nor a new Supervisor approval action. No Normal approval clock is reused.

The two immutable events are independent:

| Event | Recipient snapshot and client consequence |
|---|---|
| Genuine FastTrack SUBMITTED | Snapshot all current authorized project PMs and the valid assigned Supervisor generation at that commit. Missing Supervisor is unresolved; valid PM delivery continues. |
| Genuine PM final-confirmed information | Snapshot the current valid assignment again at final commit. This informs Supervisor; it does not ask for another approval. |

Replacing A with B after SUBMITTED does not resend the old event to B. Pending delivery to a retired generation is suppressed; the final event can snapshot B. Revocation suppresses new assignment-backed delivery/reads without deleting delivery/audit history. It does not remove other recipients' independent authority. Retry retains its immutable event snapshot; event/recipient/channel dedup prevents duplicate inbox effects. Final completion forbids creating/replacing an assignment, while approved revocation remains available.

Current source-backed producer/inbox results supersede the historical unsupported-source checkpoint only within the later approved implementation scope. See [B2 implementation acceptance](B2-IMPLEMENTATION-ACCEPTANCE.md). Reconcile current source hashes and current candidate evidence before claiming release acceptance.

## C1: controlled public representation

| Action | Method/path | Required boundary |
|---|---|---|
| Publish | `POST /api/v1/projects/{projectId}/defects/{defectId}/publications` | Current authorized PM in that project; operation key; Web CSRF when cookie-authenticated. |
| Anonymous read | `GET /api/v1/public/publications/{publicId}` | Read only the approved sanitized snapshot. |
| Withdraw | `POST /api/v1/projects/{projectId}/publications/{publicId}/withdraw` | Current authorized PM; operation key; Web CSRF when applicable. |

Anonymous status200 has exactly `publicId`, `damageCategory`, `publicStatus`, `publishedAt`. The approved category allowlist maps active catalog `CRACK` and `POTHOLE` to `ROAD_SURFACE_DAMAGE`; fixture `RG_POSTMAN_POTHOLE` and all other codes fail closed. There is no `OTHER` fallback. `publicStatus` is `PUBLISHED`, independently of DefectStatus.

Publication requires Verified Defect with verification provenance, a valid linked CONCLUDED/CONFIRMED Case whose conclusion includes that Defect, and Backend-verified correctly scoped evidence. A candidate/detection or open Case does not qualify. The Backend sets the immutable snapshot's publication time; the client does not invent it.

New anonymous reads of withdrawn and missing publications both return404. Public reads use `Cache-Control: no-store`; the client must not retain a public cache after withdrawal. Do not expect a public withdrawn payload. Authenticated write responses are a separate boundary and can report the stored withdrawn status. Originals remain private and are retained.

Anonymous DTOs must never contain report/defect/evidence IDs, identities, exact geometry/location, free text, file keys, URLs or media. Private recipient-scoped CasePublication remains separate. There is no anonymous list/search/feed or public signed-media URL.

## G02–G06: discovery collections

Use the exact catalog entry for each route. The added discovery controller uses `limit` (default25, range1–100), opaque `cursor`, and `mode` only for repair-item collection (`NORMAL` or `FAST_TRACK`). Duplicate/unknown query parameters are rejected. Do not copy legacy label `pageSize` parameters into these routes.

The response page is `items` and nullable `nextCursor`. Empty visibility produces the appropriate authorized empty page; denial or hidden parent remains an error, not a fabricated empty success. Cursors bind actor/project/resource/parent/filter and are protected by Backend keys. Do not transfer them between users, projects, parents or filters. Restart pagination after changing that context. Do not rely on a client-decoded cursor value or invent a cursor TTL.

Current authority precedes query/cursor validation. Keep exact ordering from the approved per-resource contract; do not infer newest-first ordering or sort pages locally as if all records were loaded. Collection responses use `private, no-store`. Web-cookie eligibility and project isolation still require exact operation evidence. See [discovery contract/workflows](FE-DISCOVERY-WORKFLOWS.md), [catalog](FE-API-CATALOG.json) and [current evidence](review-evidence/milestone2-r1-closure/EXACT-DIMENSION-ACCEPTANCE.json).
# A1 callback receipt compatibility — approved 2026-10-24

`POST /api/v1/internal/processing-jobs/{jobId}/results` remains AI-service-only.
New receipts bind full callback identity; exact replay requires current attempt
and authentication. Changed identity and unverifiable legacy receipts return
409 `duplicate_request`, preserving receipts/results without additional effects.
Do not interpret that legacy 409 as permission to resubmit under another key.
See [AI retry guidance](FE-MOCK-AI-GUIDE.md) and the separate
[Owner amendment](review-evidence/milestone2-r1-closure/OWNER-A1-REPLAY-RECEIPT-COMPATIBILITY.md).
No new FE route, credential protocol or DTO field is introduced. Current R1/R2/R3
acceptance remains dependent on executable evidence and independent review.
