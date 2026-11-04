# Upload, verification và evidence access contracts

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**Phase 2C · 15/10/2026 · implementation source pinned `36c3a1d273f17dd29b816136ae0182a821fd79e4` · independent JEV pending.**

[API entrance](README.md) · [Review/evidence](PHASE2C_REVIEW_REPORT.md) · [10 workflows](../02_Business-Workflows/README.md) · [Backend/Data](../03_Backend-and-Data/PHASE2B_REVIEW_REPORT.md).

`CURRENT_VERIFIED` = source/static contract discovery, không HTTP/E2E/deployment PASS. `CURRENT_IMPLEMENTED` trong phần AI là phân nhóm contract BE đã có, không nhãn real provider acceptance. `TARGET_CONFIRMED` client/business theo [owner decisions](../../product/confirmed-decisions.md), [latest HUY](../../../planning/development/HUY-FINAL-INTEGRATION.md) và owner Phase2C authorization; precedence theo [governance](../00_Governance/01_Authority-and-Source-of-Truth.md). `PROPOSED`, `NOT_VERIFIED`, `HISTORICAL` giữ phạm vi. Product HoangHaiWarrantySystem; technical RoadGuardSystem/routes giữ nguyên.

## Two scopes and actual endpoints

Reporter private uses API-211/214/213/212/208/209, publication API-210/report-evidence API-218. General project upload uses API-233/236/235/234/029/030; offline capture create API-043 adds admission/capture origin. Each exact method/path/header/body/success/error/schema is in [primary catalog](02_Full-API-Catalog.md), source DTO in chapter03. No raw photo/video payloads or provider endpoint examples invented.

Private Reporter FileScope is owner + null ProjectId/TargetId + REPORT_PHOTO. Current owner/role/verified checksum/version required. Project uploads resolve purpose-specific task/role/project scope: SURVEY_VIDEO/TELEMETRY assigned Operator; FIELD BEFORE/AFTER/MEASUREMENT current task/current Crew or bounded authorized source read. General authenticated upload route doesn't admit Reporter private photos or every purpose to every staff role.

## Request/response lifecycle

Create has metadata name/media/size/checksum/purpose/scope per DTO, returns immutable file/session identity and multipart admission; client never chooses server VERIFIED. Part URL command takes part number list under owned current session and returns presigned capabilities/expiry. Upload part bytes to returned object storage URLs with prescribed part ETag identity; don't use multipart ETag as full SHA256.

Complete supplies current version/completed part list as DTO requires;202/accepted means verify intent. Poll session/file metadata until VERIFIED before Report create/supplement or dataset/FIELD formal admission. Domain Pending/Uploading/Verifying/Verified/Failed transitions are source guarded, not client status assignments. Storage complete recovery can see NoSuchUpload after prior success and then check actual object. Verification streams actual size/SHA256/prefix MIME under current fence; object presence alone isn't admission.

Limits from UploadAdmissionPolicy: JPEG/PNG photos≤20MiB; MP4 video≤8GiB; application/x-subrip telemetry≤10MiB; dataset aggregate≤32GiB; DOCUMENT/ROUTE_SOURCE int.MaxValue source ceiling. UploadSession defaults8MiB parts/24h session/15m URL, configurable. Size long/bigint vs part int must not truncate video. Config/deployment values NOT_VERIFIED; SRT MIME doesn't prove GPS/time alignment.

Private download needs current ownership or publication recipient/source pin. Export downloads have separate immutable snapshot/expiry/temporal guards; P1-04 Reporter download risk remains pre-await check without equivalent post-await guard. Download response stream/content type and content-disposition handling follow controller source, not UI-granted blob access. Cache/file UUID/checksum isn't permanent permission. No fake public bucket URL.

## VERIFIED producer and deployment gate

[Program L25–29](../../../RoadGuardSystem.API/Program.cs#L25) registers UploadVerificationWorker only **Development AND nonempty MinioStorage:Endpoint**. Worker invokes IUploadService.ProcessOneVerificationInRoundAsync→UploadPersistenceService verification; public HTTP complete does not replace that processor. MultipartRecoveryWorker is separately gated by RecoveryEnabled and solves multipart admission/recovery, not a generic verifier promise. DI adapter registration alone doesn't schedule verification.

Production verifier/processor path and actual host topology/liveness are **NOT_VERIFIED**; source does not register this hosted verifier in Production, and no separately deployed caller was verified. Release gate retained under P1-05; no code fix or invented scheduler here. Isolated metadata capture removes hosted services, so it cannot prove file transition runtime. MinIO/TLS/ACL/large-video/kill/restart/restore native acceptance NOT_RUN.

Video/SRT/GPS/dataset/frame/model pins are integration provenance, not AI physical measurement. Retention basis/holds/review eligibility doesn't authorize delete; export/publication preserve source rights. Backend encrypted handover is unrelated to proof Expo local SQLite/photo/cache encryption.

Source: [UploadService](../../../RoadGuardSystem.Services/Implementations/Files/UploadService.cs), [ReporterEvidenceService](../../../RoadGuardSystem.Services/Implementations/Files/ReporterEvidenceService.cs), [persistence/recovery](../../../RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.Recovery.cs), [MinIO verification](../../../RoadGuardSystem.Repositories/Implementations/Storage/MinioUploadObjectStorage.cs), [limits](../../../RoadGuardSystem.BusinessObjects/Files/UploadAdmissionPolicy.cs). Tests in catalog NOT_RUN; P1-04/P1-05 remain open.
