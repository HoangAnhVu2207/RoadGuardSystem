# FIELD measurement workflow

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

FIELD is a formal inspection and provenance workflow, not a repair approval. PM creates/assigns through API-079/082; Crew operates an assigned task through API-080/081/091/093/095/096. Supervisor and PM read scoped evidence with API-085/086/088/094.

## Flow

1. Load task detail, geometry, history and start-origin. Confirm current Crew assignment, Defect/source chain, route/version pins and task version.
2. Accept or reject with API-081/091. Rejection preserves history; it does not reassign itself.
3. Start with API-093. The original/first-start origin is server-owned and must not be reset by a supplement or clock change.
4. Measure the physical defect in the field. Record dimensions and method as the request DTO requires. AI bbox/confidence/frame/timestamp is evidence context only and cannot populate physical dimensions.
5. Capture evidence for the FIELD submission using API-233/236/235 and poll API-234 to `VERIFIED`. Attach verified, task-scoped evidence through API-095 (`FieldSubmissionInput`); purpose and measurement validation remain server authority.
6. PM reviews through API-092. When the FIELD task is `SupplementRequired`, Crew resubmits through API-095 with `ParentSubmissionId` equal to the latest submission, a new K and current task version. Read the submission through API-096. Preserve the first-start origin.
7. FIELD review does not finish a repair. Repair assessment/attempt/supplement/finish (API-156/157/159/160) belong to the separate [repair workflow](03_NORMAL-and-FastTrack-Execution.md), with its own item, assignment and version guards.

## Failure boundaries

Inactive membership, wrong assignment, stale task/item version, unverified file, wrong checksum, missing Defect/source, revoked policy or missing road coverage must fail closed. Preserve original actor, task, source file IDs, snapshot/version and receipt. No last-write-wins and no client-side state transition.

`CURRENT_IMPLEMENTED_BE`: controllers/services/repositories and DTOs documented in workflows 06/07. `PROPOSED_EXPO_UI`: camera/location/file capture and form UX. `NOT_VERIFIED_EXPO_E2E`: native sensors, background capture, actual physical measurement accuracy and device build.
