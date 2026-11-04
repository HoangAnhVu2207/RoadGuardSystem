# Report and IncidentCase lifecycle

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

## Online Reporter journey

1. Reporter completes OTP and Android bearer login.
2. Reporter creates and waits for private evidence to become `VERIFIED`.
3. API-216 `POST /api/v1/reports` accepts `CreateReporterReportRequestDto` with description and one or more verified owned files. GPS is optional; `UNKNOWN` must not include a location. The server resolves ownership and creates the Report, an unassigned `IncidentCase` and its active report link. Reporter does not send `projectId`, `ownerId`, `defectId`, status or trusted uploader fields.
4. API-215 lists only the current Reporter’s own reports. API-217 returns the own projection with server `version`/ETag, evidence metadata, `routingStatus`, supplements and explicitly published updates.
5. Supervisor triages an unassigned Case using Web workflow. PM later decides Candidate/Defect and repair workflow. Reporter cannot see or mutate those internal resources.
6. API-219 supplements an owned report with a new verified evidence set, a fresh K and the latest strong report `If-Match`. A 412 requires reread/review, not blind overwrite.
7. Published updates are read from the own report projection. API-210 may download a published evidence item using the authorized publication/report/evidence tuple. Publication is not an internal Case conclusion and does not imply repair acceptance.

## DTO and privacy rules

API-215 uses `OwnReportPageDto`; API-217 uses `OwnReportDto`; API-216/219 use the catalog request DTOs. Render fields supplied by the response (`id`, `description`, server `createdAt`, `version`, evidence, `routingStatus`, `publicUpdates`, supplements). Do not derive “Defect approved”, “repaired”, “accepted” or percentage progress from `routingStatus`; `UNASSIGNED/ASSIGNED` only describes routing.

API-218 private report evidence download, API-209 private file content and API-210 publication content must use authenticated requests. Never call internal `/cases`, candidate, defect, survey or repair routes with a Reporter token. A missing report must not reveal whether another owner has it.

## Boundaries and gaps

Reporter intake is independent of Drone Survey. PM/Supervisor/Crew authority remains unchanged. Reporter online MVP is not device enrollment, signed batch sync, snapshot handover or physical measurement. There is no source-verified dedicated “request supplement from Reporter” command; the app may let the Reporter append a supplement through API-219. Push/FCM delivery is also not proven; polling own reports is the source-backed read path.

**Evidence:** `ReportsController`, `ReporterLifecycleRepository`, `ReporterEvidenceController`, `CandidateDecisionService`, `CasesController`, `Defect.CreateFromReport`; [API-018](../../04_API-Contracts/02_Full-API-Catalog.md#api-018), [API-019](../../04_API-Contracts/02_Full-API-Catalog.md#api-019), [API-021](../../04_API-Contracts/02_Full-API-Catalog.md#api-021), [API-215–219](../../04_API-Contracts/02_Full-API-Catalog.md#api-215). PM triage/Defect and downstream repair are source boundaries, not Mobile acceptance.
