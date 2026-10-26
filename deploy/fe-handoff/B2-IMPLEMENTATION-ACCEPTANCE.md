> CURRENT_VERIFIED — iteration 9 (2026-10-23): B2 delayed FastTrack intake bug reproduced (6 PASS / 2 FAIL) and narrowly fixed: event/snapshot use genuine intake time; H3 receipt/attempt/link/review-clock provenance and Normal timing remain unchanged. Affected SQL 37 PASS. W07 same-item Normal HTTP to Supervisor final and W09 signed-sync host recreation: 3 PASS. W04 same fresh HTTP Report → Case → candidate → assessed/verified Defect → CONFIRMED conclusion → private publication → C1 public snapshot → withdrawal is now executed in policy-http (25 PASS total, includes C1/B2 authority suites). Upload verification uses a controlled storage adapter in W04; actual MinIO belongs to separate distribution checks. A1 HTTP 19 PASS; policy unit 22 PASS. Exact operation ledger covers260, FULL_CONTRACT=0; dimension-level independent acceptance remains open. New runtime candidate required after producer change; RC7/RC8 evidence is historical for this artifact. No Git publication or overall release PASS.

# B2 approved implementation — iteration 8

Owner authority: OWNER_B2_EXACT_CONTRACT_APPROVED, 2026-10-23, entire [exact proposal](B2-EXACT-ASSIGNMENT-PROPOSAL.md). Base/HEAD dd94aa697c411bb11d305973580055216f65ba73 on huy-review; inherited working tree retained. Huy/Codex sole writer. Independent implementation review PENDING. Release FE_INTEGRATION_PREVIEW; no Git publication or FULL_CONTRACT promotion.

## FE W08 sequence

Resource: `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/notification-supervisor`.

| FE action | Request | Result / next action |
|---|---|---|
| PM reads current assignment | GET; current project PM, Bearer or WebSession | 200 six fields: assignmentId, supervisorId, status, assignedAt, revokedAt, version; ETag, private/no-store. Never-created 404. No collection/pagination. |
| PM first assigns | PUT exactly `{supervisorId:uuid}`; key + If-None-Match:* | 201 + Location/ETag; recipient is current global Supervisor, no ProjectMember required. |
| PM replaces or reactivates | PUT same body; new key + current head If-Match | 200 new generation/ETag; same recipient still creates a new generation. |
| PM revokes | DELETE; key + current head If-Match; no body | 204 + new ETag. Retain history; subsequent assignment-backed reads denied. |
| Crew actually submits | Existing field/formal-intake/repair attempt APIs | Immutable SUBMITTED event snapshots all current PMs and current assignment. PMs continue if Supervisor absent/unresolved. |
| PM ACCEPT then final | Existing repair review and final APIs | Distinct final event snapshots current assignment; Supervisor receives information only. Durable resolution; no Supervisor approval or Normal clock. |
| Supervisor inbox | Existing notifications list/detail/scope/read | Current recipient/role/source/generation checks, before SQL pagination and successful read-receipt replay. Replacement never reroutes an old event. |

Cookie writes preserve CSRF. Current actor/project/resource checks precede command validation and historical replay. New command errors: 400 malformed, 428 missing conditional, 412 stale, 409 invalid state/revoked/reused key; 403 wrong actor/project, concealed404 wrong parent/recipient. Historical replay retains original status/body/ETag and never restores an assignment. GET has no command conditional errors. Existing auth/session errors remain unchanged.

## Executed evidence

Evidence root: `C:/Users/HoangAnhVu/AppData/Local/codex-with-chatgpt/handoffs/CLOSEOUT-B2-APPROVED-20261023`.

| Evidence | Actual result | Scope / limits |
|---|---|---|
| b2-authority-http.trx | 4 PASS / 0 FAIL / 0 SKIP | Actual Bearer and secure cookie requests, CSRF success/denial, session and PM revocation, wrong project/parent/recipient, six-field DTO, conditions/replay, generated Swagger. |
| b2-authority-sql-second.trx | 23 PASS / 0 FAIL / 0 SKIP | Assignment journeys + concurrency, retained Normal/legacy source, retry and protected inbox regressions. Owned disposable SQL. |
| b2-race-rollback-second.trx | 9 PASS / 0 FAIL / 0 SKIP | Genuine Crew→SUBMITTED→PM ACCEPT→PM final→durable resolved state, global A→B, independent origins/dedup, no retroactive delivery, revocation/read replay denial, suspended-recipient retry, assignment vs final race, final-event rollback and assignment audit rollback/immutability. |
| b2-unit-second.trx | 114 PASS / 0 FAIL / 0 SKIP | Notification unit suite including seven strict snapshot/final proof cases. |

Fixtures explicitly label controlled geometry and verified-upload lineage. SQL tests prove Backend producer/consumer, not real field accuracy, external channels or MinIO bytes. RC6 failed bootstrap retained; RC7 extracted Docker/storage/browser acceptance recorded separately. Earlier failed builds, fixture failures and Swagger Testing-environment404 are retained; no skip or disabled guard.

## Compatibility and deployment

Canonical241 + eleven approved collections retain separate attribution. Three new B2 operations raise packaged count257→260; handoff-only `/ready` is retained separately from Development test-host Swagger until RC runtime parity. FULL_CONTRACT=0. Existing schemaVersion1/legacy snapshot remains supported; new snapshot is explicit, never inferred/backfilled. Additive migration `20261022181921_B2NotificationSupervisorAssignment` adds heads/generations/audits and nullable scope proof. Immutable history and scope-proof SQL guards remain enabled.

Coordinated upgrade: quiesce the dispatcher, deploy upgraded readers/consumers and migrate before enabling new producer. No mixed old-worker/new-payload rolling compatibility is claimed. Do not restart shared workers under this local acceptance. RC5 remains immutable and is not evidence for changed B2 production bytes.

## Final verification supplement - 2026-10-23

- Model parity3 PASS, cache HTTP3 PASS, Python operations63 PASS. Cache denials narrowed to NotificationSupervisorController; CSRF/challenges preserved.
- RC6 fail-closed bootstrap exposed stale seed catalog after B2 migration. Updated table-coverage to195 tables/exact current signature, preserving guard. Seed regression23 PASS/1 FAIL retained; stale error-message assertion corrected using actual mutated signature; six negative variants + canonical bootstrap7 PASS. Schema export1 PASS.
- RC7 build/up/scenarios PASS; runtime78, collections/frames145, B2 HTTP14, OTP5, persistence18+20, corrected addon52 PASS. Initial addon failures: unused ProbePayloadDto and16 encoding-corrupted descriptions; corrected OpenAPI matches runtime. Original failures retained.
- Browser initial25 PASS then harness ERROR reading exited seed container. Read corrected using docker cp; actual browser29 PASS. OS TLS trust unchanged, isolated ignoreHTTPSErrors explicitly recorded. Browser assignment generation occurs after persistence check; no restart claim for that later generation.
- B2-OPERATION-ACCEPTANCE.json pins exact3 operations. Canonical241 ledger15 PARTIAL/226 NOT_ATTRIBUTED, FULL0. Current controller drift0 does not prove all dependency/test bindings reusable.
- Documentation-only successor archive must prove1019 production and seed files equivalent to RC7 before reusing bounded runtime evidence. No new binary build implied by repackaging.
- Controlled deployment: upgraded readers/consumer and additive migration before new producer; no mixed-worker rolling compatibility claim. Independent implementation review PENDING.
