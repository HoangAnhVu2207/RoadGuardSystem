# Survey video, SRT and upload

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

The supported source chain is PM plan/task → current Operator assignment → verified `SURVEY_VIDEO` and `TELEMETRY` files → dataset submission. API-229 does not authorize an unverified upload or a task from another project.

## Multipart sequence

1. Read API-225/226 and verify assignment, task status and version.
2. Create each purpose-specific upload with API-233 `POST /api/v1/uploads`; use the exact `UploadCreateRequestDto` from the catalog, checksum and declared media metadata. Do not set server `VERIFIED`.
3. Request part URLs with API-236. Upload bytes to returned presigned URLs and retain each returned ETag; ETag is not the complete SHA-256.
4. Complete with API-235 and current upload version/part list. A `202`/accepted result means verification intent only.
5. Poll API-234 until the server reports `VERIFIED` or a terminal failure. A worker/provider deployment is not proven by the route existing.
6. Submit API-229 with verified file IDs, task/dataset version, scope and telemetry binding. Preserve K and exact body on timeout.

Video, SRT and GPS are distinct evidence. An absent SRT/GPS file is not a coverage PASS. The app must not fabricate timestamps, route pins, model results or physical measurements. No DJI SDK or automatic telemetry integration is source-verified.

## Client responsibilities

Validate file URI accessibility, size/checksum before upload, foreground/background permission prompts, resumable part state and redacted logs. Store presigned URLs only in protected short-lived memory/queue records. Reconcile an unknown complete by GET status before creating another upload. `CURRENT_IMPLEMENTED_BE` is the Backend contract; upload byte transfer and native file handling are `PROPOSED_EXPO`/`NOT_VERIFIED_EXPO_E2E`.

Evidence: [upload contract](../../04_API-Contracts/06_File-Upload-and-Evidence-Contracts.md), [survey workflow](../../02_Business-Workflows/05_Drone-Survey-Dataset-and-Processing.md), API-225–236 crosswalk.
