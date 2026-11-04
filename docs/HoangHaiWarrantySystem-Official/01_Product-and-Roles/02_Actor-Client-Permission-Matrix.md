# Actor, client and permission matrix

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**Primary client OWNER_CONFIRMED · source authority references preserved · Phase 1.** [Canonical entrance](../README.md), [source evidence](../../audit/HHWS-DOCS-REBUILD-01/02_FULL_API_INVENTORY.md).

## Primary clients and responsibilities

| Actor | Primary client | Business responsibility | Current guard boundary / limitation |
|---|---|---|---|
| Supervisor | Web | Supervisor-global administration/triage/allowed approvals; NORMAL approval/final; scoped safety/FT information | Current account/session + exact Supervisor operation/project/resource; global does not authorize PM/Crew commands or arbitrary private evidence |
| Project Manager / PM | Web | Effective project workflow, candidate KEEP_NEW/LINK_EXISTING/REJECT, labels, assignment/review, policy/FT final and explicit publication | Current effective membership/project/source/version; unassigned Case requires Supervisor triage first |
| Reporter | Mobile React Native+Expo | Register/OTP, own report/photo/capture metadata, own read/supplement, recipient publication | Current Reporter + owner/recipient guards; no internal Case notes, other-owner photos or PM/repair approval; MVP online |
| Drone Operator | Mobile React Native+Expo | Assigned survey capture/video/SRT/dataset and source provenance | Current assigned survey/project/resource; not Crew FIELD/repair; no blanket signed H5 grant |
| Repair Crew | Mobile React Native+Expo | Assigned FIELD physical measurement, allowed repair/evidence, signed offline flows | Current assignment/binding/project/source/policy; measure-only not execution right; loaded snapshot not permanent authority |
| AI service / model worker | Separate FastAPI Python logical service (target boundary) | Actual-video frame inference and candidate metadata/provenance | Service credential/attempt/source contract; actual provider NOT_VERIFIED; cannot approve Defect/training label/repair or replace Crew dimensions |

Không gán tên người/ETA/client repo khi chưa được cung cấp. UI navigation và hiding buttons là tiện ích; BE vẫn quyết authorization mỗi command/read/replay.

## Transport is separate from actor

| Path family / admission | Consumers | Evidence |
|---|---|---|
| `/api/v1/auth/web/*` cookie/CSRF/session/renew | Browser Web transport | [WebAuthController](../../../RoadGuardSystem.API/Controllers/WebAuthController.cs), [cookie selector](../../../RoadGuardSystem.API/Authentication/WebCookieConfiguration.cs) |
| `/api/v1/auth/android/login`, `/refresh` access/rotating refresh | Existing Mobile bearer wire | [AuthController](../../../RoadGuardSystem.API/Controllers/AuthController.cs), [AuthService](../../../RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs) |
| Legacy `/auth/login`, `/auth/refresh`, OTP verify token pair | Compatibility/onboarding paths | Existing transport remains; Reporter OTP verify→existing Android login for correct Mobile session, not silently treat verify token as Android transport |
| Protected Report/evidence APIs | Reporter bearer; cookie eligibility may also exist | [ReportsController](../../../RoadGuardSystem.API/Controllers/ReportsController.cs), [ReporterEvidenceController](../../../RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs) |
| Internal processing callback | Dedicated AiCallback service credential | [ProcessingV2Controller](../../../RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs), [AI bearer configuration](../../../RoadGuardSystem.API/Authentication/AiServiceJwtBearerConfiguration.cs) |

Actor primary client is product scope; cookie eligibility is not ReporterWeb assignment. `/auth/android/*` route name does not claim product Android-only or iOS accepted. Every role/scope entry remains bounded by current implementation; [Phase0 inventory](../../audit/HHWS-DOCS-REBUILD-01/02_FULL_API_INVENTORY.md) records per-operation guards/errors/headers and test limits.

## Cross-module invariants

Reporter supplies source independently of drone/AI. Supervisor triage→PM decision creates/links Defect only when source meets current conditions; FIELD from Reporter has no fake Survey. PM/AI route visibility does not give Reporter permission.

NORMAL repair: PM proposal→Supervisor approval→PM assignment→Crew before/after/repair→PM review→Supervisor final. Fast Track: separate policy/coverage/authorization, PM final and Supervisor information; [current gaps](../00_Governance/03_Implementation-and-Verification-Status.md) retain source limits without adding Supervisor FT approval.

Crew physical measurements retain source/unit/method/UNKNOWN; YOLO bbox/confidence and drone GPS are not dimensions or accurate Defect coordinates. GIS segment/section and image road-mask are different abstractions. Segmentation placement needs benchmark, not a mandated service.

Device enrollment is per actor/project/device revision with client-generated keys. K receipts do not replace ECDSA signatures; signatures do not prove historical UTC/human presence. Original actor, pinned source and per-item durable ACK survive sync/handover; pending/unacknowledged data is preserved. [Source definitions](../00_Governance/01_Authority-and-Source-of-Truth.md#glossary) keep ACK/publication/UNKNOWN meanings distinct.
