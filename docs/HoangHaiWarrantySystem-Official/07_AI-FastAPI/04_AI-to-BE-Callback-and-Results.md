# AI callback and result admission

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

The callback route is `POST /api/v1/internal/processing-jobs/{jobId}/results` (API-031), protected by named `AiCallback` authentication. `AiServiceBearer` requires issuer, audience `roadguard-be-ai`, lifetime, signing key/kid, non-empty subject and `client_type=AI_SERVICE`; a human PM token is not a callback credential.

`ReceiveAiResultRequestDto` carries `JobAttemptId`, `ManifestHash`, `ModelVersionId`, `Mode`, `RawResultFileId`, `ChecksumSha256` and `Detections`. Each detection is validated as a GUID ID, frame file ID, non-negative timestamp, non-empty type, confidence 0..1 and four normalized bbox values contained within the image. The Backend checks attempt belongs to job, exact manifest/model/mode, verified raw-result checksum and project before persistence.

The accepted result creates `AIDetections` and completes the job; it does not create a Defect or physical dimensions. PM still decides candidate disposition and Crew measures in FIELD.

**P1-02 PARTIAL:** current repository admission does not fully fence latest/active attempt and receipt identity; late, out-of-order or replay exploitation is `NOT_VERIFIED`. A provider must send the exact attempt and canonical result identity and receive a deterministic duplicate/conflict response. Callback retries must use bounded backoff and preserve the original payload; no retry success is claimed here.

The AI worker's callback implementation posts a JSON result to `callback_url` with a ten-second timeout and records a local error string. It does not show service bearer injection, attempt headers, response admission, durable retry or dead-letter handling. This is a concrete integration gap.

Phase 5 source clarification: AI snapshot `5d44980a1177abd219cf488c84c365d01cea49e8`, `src/api/worker_service.py` and its formatter, is not wire-compatible with `ReceiveAiResultRequestDto`. Snake-case result fields, pixel `raw_box` and estimated dimensions do not supply the Backend attempt/model/manifest/verified raw-file/frame/normalized-box contract. Both worker branches still call `MockAIAdapter`; this does not prove real YOLO inference or provider adoption. P1-01/P1-02 remain open. Repeating a callback transport request preserves its attempt/payload; a Backend processing-job retry may allocate a new attempt and must not reuse the old callback identity.
