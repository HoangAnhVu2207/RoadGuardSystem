> CURRENT_VERIFIED iteration 8 final (2026-10-23): exact Owner-approved B2 implemented and isolated verification executed. Genuine SUBMITTED and PM-final Supervisor delivery now have Backend SQL producer-to-inbox evidence: global authority, reassignment, recipient-specific revocation, retry/dedup, concurrency and rollback. No Supervisor approval transition or Normal clock reuse. RC7 build/cold-start, collections/AI frames, B2 HTTP, browser and restart persistence PASS within recorded bounds. OpenAPI documentation errata corrected against exact260-operation runtime; original failures retained. See B2-IMPLEMENTATION-ACCEPTANCE.md for evidence/limits. Independent implementation review PENDING; FE_INTEGRATION_PREVIEW, FULL_CONTRACT=0. Historical unsupported/pending/RC5/RC6-in-progress statements below are superseded only for current B2. No Git publication.

# Current collection entrypoint

See [FE-APPROVED-COLLECTIONS.md](FE-APPROVED-COLLECTIONS.md) for adopted G02-G06 routes, roles, wire, next actions and runtime limits. FE-GET-LIST-AUTH-MATRIX.md retains canonical241 source inventory. Eleven new operations are additive and separately attributed.

## Adopted discovery and current authority

| GAP/workflow/screen | Route suffix | Admission AND per-row authority | FE next action |
|---|---|---|---|
| G02/W07 packages | /api/v1/projects/{projectId}/repair-packages | PM/Supervisor, current project, every obligation currently readable | package id -> items |
| G02/G03/W07/W08 items | /api/v1/projects/{projectId}/repair-packages/{packageId}/items | existing repair ReadItem, currently readable parent obligation | detail/history/eligibility; fresh detail ETag before mutation |
| G04/W10 history | /api/v1/projects/{projectId}/exports | creator-only AND current export/snapshot source visibility | status -> manifest/content, each reauthorizes |
| G05/W05 plans | /api/v1/projects/{projectId}/survey-plans | PM/Supervisor + plan detail guard | /api/v1/projects/{projectId}/survey-plans/{id} |
| G05/W05 assignments | /api/v1/projects/{projectId}/survey-tasks | PM/Supervisor/Operator + task detail guard | /api/v1/projects/{projectId}/survey-tasks/{id}, work-package |
| G05/W05 processing | /api/v1/projects/{projectId}/processing-jobs | PM/Supervisor/Operator + job detail guard | /api/v1/projects/{projectId}/processing-jobs/{id} |
| G05/W05 mock/result | /api/v1/projects/{projectId}/ai-mock-runs | PM/Supervisor + Development mock/detail guard | run/result -> /files/{id}, /content |
| G06/W09 devices | /api/v1/projects/{projectId}/offline/devices | PM/Supervisor/Crew + device-get guard | existing detail; no keys in summary |
| G06/W09 packages | /api/v1/projects/{projectId}/offline/packages | PM/Supervisor/Crew + original actor/recipient grant/Supervisor guard | authorized detail, no ciphertext summary |
| G06/W09 grants | /api/v1/projects/{projectId}/offline/grants | PM/Supervisor/Crew + recipient-role/device/Supervisor guard | existing detail; no blanket PM grant |
| G06/W10 evaluations | /api/v1/projects/{projectId}/retention/evaluations | PM/Supervisor + retention guard | existing evaluation detail pagination; no deletion |


Every collection reapplies current project and per-row detail authority. Exports are creator-only; this does not grant download access. Items use current obligation responsibility; no cross-package Fast Track catalog. Revocation can shrink subsequent pages. Device/grant/package summaries contain no private key, ciphertext or grant-scope payload.

Cursor binds actor, project, resource, parent package and mode; it does not bind limit. Unknown/repeated query keys, invalid limits and invalid/foreign cursors return 400 after admission. Responses are private/no-store and exactly items/nextCursor; there is no totalCount, collection ETag or immutable snapshot. Exact error and DTO fields remain documented in FE-APPROVED-COLLECTIONS.md, G02-G06-OWNER-DECISIONS.md and OpenAPI.

Runtime: focused collection tests cover role/project/revocation, query/cursor rejection and device traversal. Populated workflow tests cover additional repair/export/survey/offline/retention/mock paths as indexed in FE-COLLECTION-EVIDENCE.json. Not every pagination/revocation combination has executed; all eleven remain PARTIAL_CONTRACT.

## Review correction: Web authentication

G02-G06 approved collection GETs accept HTTPS Web session cookie or Bearer, with current resource/project authority and unchanged pagination. C1 create/withdraw accept Web session cookie with session-bound CSRF token and antiforgery cookie, or Bearer. Anonymous C1 read remains public. See [FE-CLOSEOUT-POLICY-CONTRACT.md](FE-CLOSEOUT-POLICY-CONTRACT.md) for exact transport requirements and remaining acceptance limits.
