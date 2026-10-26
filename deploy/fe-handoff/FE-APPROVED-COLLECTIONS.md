# Approved G02-G06 collections â€” Task34 V9

TARGET_CONFIRMED: G02_G06_COLLECTION_CONTRACT_APPROVAL, 2026-10-21. Source: DiscoveryCollectionsController, CollectionReadService, CollectionReadRepository, Collections/CollectionPages.cs. Eleven additive GET operations; canonical241 coverage is not promoted.

All routes below use `/api/v1/projects/{projectId}`. Current bearer or web cookie required. Responses use `private, no-store`. Existing mutation CSRF and ETag rules remain unchanged.

| GAP/workflow/screen | Route suffix | Admission AND per-row authority | FE next action |
|---|---|---|---|
| G02/W07 packages | /repair-packages | PM/Supervisor, current project, every obligation currently readable | package id -> items |
| G02/G03/W07/W08 items | /repair-packages/{packageId}/items | existing repair ReadItem, currently readable parent obligation | detail/history/eligibility; fresh detail ETag before mutation |
| G04/W10 history | /exports | creator-only AND current export/snapshot source visibility | status -> manifest/content, each reauthorizes |
| G05/W05 plans | /survey-plans | PM/Supervisor + plan detail guard | /survey-plans/{id} |
| G05/W05 assignments | /survey-tasks | PM/Supervisor/Operator + task detail guard | /survey-tasks/{id}, work-package |
| G05/W05 processing | /processing-jobs | PM/Supervisor/Operator + job detail guard | /processing-jobs/{id} |
| G05/W05 mock/result | /ai-mock-runs | PM/Supervisor + Development mock/detail guard | run/result -> /files/{id}, /content |
| G06/W09 devices | /offline/devices | PM/Supervisor/Crew + device-get guard | existing detail; no keys in summary |
| G06/W09 packages | /offline/packages | PM/Supervisor/Crew + original actor/recipient grant/Supervisor guard | authorized detail, no ciphertext summary |
| G06/W09 grants | /offline/grants | PM/Supervisor/Crew + recipient-role/device/Supervisor guard | existing detail; no blanket PM grant |
| G06/W10 evaluations | /retention/evaluations | PM/Supervisor + retention guard | existing evaluation detail pagination; no deletion |

## Exact new wire

Only lowercase `limit` optional default25 integer1-100, and `cursor` optional nonempty opaque token. Items also supports `mode` omitted(all), NORMAL or FAST_TRACK. No search/status/date/sort/actor filters. Unknown/repeated keys fail400. Output mode retains existing Normal/FastTrack spelling.

Response exactly `{ "items": [TypedSummary], "nextCursor": null|string }`; empty200 is []/null. Cursor binds actor/project/resource/package/mode, not limit. SQL uniqueidentifier ascending keyset, not client UUID byte order. Encode returned cursor as a query value. Authority reapplied each request. No totalCount/asOf/immutable snapshot/collection ETag. Current detail ETag remains authoritative for mutation.

New-route errors: application/problem+json with type about:blank, title Collection request failed, integer status, instance, code, correlationId. 400 validation_error;401 unauthorized;403 access_forbidden;404 not_found for non-visible parent under existing disclosure rules. Admission precedes token validation. Infrastructure failures are not empty success. Existing route envelopes/defaults/errors are unchanged.

Exact typed summary fields/nullability are in G02-G06-OWNER-DECISIONS.md and generated OpenAPI. No private media/storage paths, ciphertext, signatures, public keys or grant scope payload in summaries.

## Current authority and limitations

Repair visibility resolves current obligation responsibility, including transfer; projectId remains origin provenance. A mixed-responsibility package is omitted to avoid leaking cross-project aggregate completion. Individually readable items remain reachable by known package id. Split-package fresh-session discovery requires a separately designed safe aggregate projection; no permission expansion is inferred. Full transfer is tested through actual issue/accept lifecycle APIs.

Creator-only history can omit jobs readable through existing detail; list grants no download authority. Revocation can shrink subsequent pages. Existing signed capabilities retain natural expiry, with no new signed URL route.

## Executed evidence versus demo

SQL/HTTP evidence: DiscoveryCollectionsHttpTests (empty, role/project/revocation, query/token, duplicate-free device traversal); H4RepairWorkflowHttpAdmissionTests (populated FastTrack, mutation refresh); H6ProjectLifecycleHttpTests (actual transfer); P2SurveyV2ApiTests (plan/task); H5OfflinePositiveHttpTests (signed package/grant); Anh02RetentionHttpTests; Anh02AiHttpTests (mock/job/frame current read/revocation); ExportReadyContentHttpTests (creator history/current content). External evidence index records exact TRX and commands. This does not claim every route's pagination/revocation combination or FULL_CONTRACT.

Development scenarios add explicit single synthetic-video retention evaluations through existing POST and journaled idempotency, then validate collection envelopes. Existing A/B plans/tasks/jobs/mock runs/exports are reused. Devices/packages/grants/repairs can legitimately remain empty until supported workflows create them. No fabricated approvals/repair states/grants are inserted; disposable SQL tests provide populated examples.

A1/B2/C1 exact compatibility and C1 data policy are owner-confirmed and implemented; see FE-CLOSEOUT-POLICY-CONTRACT.md and FE-CLOSEOUT-WORKFLOW-ACCEPTANCE.md for executed evidence and limits. Remaining gates concern complete journey/contract acceptance and independent review, not a repeated policy approval. D1 no automatic orphan deletion. Runner DENY_ALL. Release remains FE_INTEGRATION_PREVIEW until mandatory gates close. External FE UI/native/deployed E2E not verified.
