# Backend to AI integration contract

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

## Backend authoritative routes

| API | Method/path | Auth and contract |
|---|---|---|
| API-044 | POST `/api/v1/processing-jobs` | Human PM/Supervisor, bearer, `Idempotency-Key`; `CreateProcessingJobRequestDto`; returns 202 and ETag |
| API-045 | GET `/api/v1/processing-jobs/{jobId}` | PM/Supervisor/Operator project scope |
| API-046 | POST `/api/v1/processing-jobs/{jobId}/retry` | PM/Supervisor, `Idempotency-Key` + `If-Match`, retryable state |
| API-031 | POST `/api/v1/internal/processing-jobs/{jobId}/results` | `AiCallback` policy; AI service identity; `ReceiveAiResultRequestDto` + `Idempotency-Key` |

The Backend creates the authoritative manifest/attempt and dispatch outbox. Existing source does not establish an adopted external provider route such as `/predict`, a queue protocol, artifact download protocol, or deployment URL. `worker_service.py` routes are repository-local AI routes: `POST /api/v1/jobs/submit`, `GET /api/v1/jobs/{job_id}/status`, and `GET /health`; they are `CURRENT_IMPLEMENTED_AI` source only and `NOT_VERIFIED_E2E` against Backend.

`ManifestModel` fields include manifest/job/block IDs, contract version, target band, optional road section/segment, width/centerline context, video intervals, model config, callback URL and mock flag. This is not evidence that the Backend sends this exact JSON; adoption requires an owner-approved provider contract preserving Backend pins.

The Backend worker currently records dispatch outbox data with attempt identity (`ProcessingV2PersistenceService.cs` lines 67 and 106). An implementation handoff must carry attempt, manifest hash, model version, mode, source artifact identity and checksum without weakening project scope.
