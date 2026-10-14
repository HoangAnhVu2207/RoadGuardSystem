# HHWS-DOCS-REBUILD-01 — Phase0 executive JEV summary

## Status and delivery

**DONE: Phase0 report package prepared for the authorized commit/push. JEV readiness: PARTIAL. Phase1 NOT_STARTED.** HoangHaiWarrantySystem is product name; RoadGuardSystem remains legacy technical identifier. Root Codex is report writer. Production source, migration, OpenAPI, tests and old documents are unchanged. Actual push SHA/result is verified in final delivery after Git push, not embedded circularly in the commit itself.

Source baseline `huy-review` / `6fdd98d98b41311e3af07a7b8cb95c55d8d62bc5`; after `git fetch origin huy-review`, HEAD==origin/huy-review and ahead/behind0/0. User requested report-only commit message `docs(audit): inventory backend and documentation for HHWS`. Old untracked five loose HHWS reports, Reporter audit and Expo handoff are preserved and excluded from staging. No force push/merge/rebase/reset. This output folder has **six reports**, following explicit Output list (the return checklist's “five” is inconsistent with that list).

## J — Executive verdict

**0confirmedP0 /5P1 /7P2**. Counts include source limitations, risk not reproduced, documentation/contract debt and external acceptance gates; not12proven runtime defects. Backend currently has Reporter Mobile-compatible bearer/OTP/upload/report/supplement/publication and PM/no-fake-Survey FIELD source. No cookie-only Reporter barrier was found in inspected source. Existing implementation is the integration center; owner decisions remain behavioral authority.

**Web:** Supervisor/PM. **Mobile React Native+Expo:** Reporter/Drone Operator/Repair Crew. Current wire `/auth/android/*` retained. iOS production readiness NOT_VERIFIED. Source alone cannot certify actual FE build, local encryption or live YOLO/provider.

## E — Verified inventory totals

| Inventory | Result / scope |
|---|---|
| Controllers |47production controllers|
| Versioned API operations |241unique method/path pairs,223paths;236HTTP action declarations with5Geometry aliases|
| Operational health |GET `/health` counted separately;242API+health entries; mapping does not prove method restriction/readiness of SQL/AI/storage|
| OpenAPI |357schemas; freshly generated isolated MVC/Swagger document equals existing static OpenAPI,0route/schema/auth/text differences|
| Source integrity |1334recorded source file hashes revalidated;0drift. All1314current production/test C# files outside build outputs indexed|
| Production declarations |1987type declarations including partial/interface/helper/enums, not unique runtime entities;201recorded DI registrations|
| EF model |196mapped entity types/191tables/1945property mappings/1940distinct columns; current model capture only, no SQL calls|
| Documentation |3498text documentation/artifact files indexed; caches and this generated report package excluded from evidence inventory|
| Documentation debt signals |358unresolved relative-link occurrences;196identical-content groups; flags are signals, not all business defects|

Method: every production controller literal HTTP/class route expanded independently, version/constraints/absolute overrides/aliases handled; validated against hash-matched Roslyn map and fresh isolated production MVC ApiExplorer/ISwaggerProvider. Program other mappings reviewed. Every source file/type/DI and EF entity/property/key/FK inventoried in01; every API has source/method/range/request/response/header/guard/test locators in02. Historic corpus mechanically read/indexed for title/status/hash/baseline/link/signals; current workflows and key source branches semantically reviewed. Not a claim every historical paragraph or every business branch was dynamically tested.

## V — Important variances and recommendations

| Judgment / priority | Expected vs actual / impact | Evidence and recommendation |
|---|---|---|
|PARTIAL/P1 |Real FastAPI/video/checkpoint/context/artifact transport expected; synthetic ANH02 path is fixture, no actual provider execution established |ProcessingV2PersistenceService.CreateAsync L36–78 dispatch outbox; Anh02AiService.ProcessOneAsync L88–151 synthetic frame/CRACK fixture. Adopt real provider protocol/fixtures; no invented /predict route|
|PARTIAL/P1 |Compatibility callback checks job attempt membership, not latest/active; receipt fingerprint omits job/attempt/model/manifest binding |ProcessingV2PersistenceService.ReceiveResultAsync L117–148; bounded BE+AI compatibility design and late/cross-job replay tests; source gap, exploit NOT_VERIFIED|
|PARTIAL/P1 |Reporter protected download current rights/source checked before storage wait, no post-await revalidation |ReporterEvidenceService.DownloadAsync/DownloadPublicationAsync L160–187; delayed-open reproduce and bounded recheck proposal, no code edit|
|PARTIAL/P1 |Matching expected location/accuracy/image handling; current matcher receives nulldistance/metricGpsAvailable false |CandidateMatchingService L88–90; document partial source behavior, don't invent GIS accuracy/threshold|
|FAIL/P2 |Old ReporterWeb/AndroidKotlin/RoadGuard product entrances conflict owner MobileExpo/HHWS baseline |docs/product/README.md L13, V2 overview L609, module map L16–19; reference-first docs alignment after approval|
|FAIL/P2 |Swagger stored source-excerpt assertion and V2 FE contract lock checks fail |Fresh focused test16PASS/1FAIL; lock check/validator exit1. Do not alter tests/lock to disguise; record current debt|

Remaining P1 external evidence and localized P2 FT-final notification/pre-Defect producer/navigation gaps are in03/05. Road GIS segment/section is not pixel road-surface segmentation. Placement of image segmentation remains architecture/benchmark decision, no mandated service. AI detection candidates never certify Defect or physical dimensions; Crew measures. NORMAL and separate FastTrack rights remain unchanged.

## FE vs BE implementation boundaries

Reporter online Mobile uses existing OTP→Android login/bearer+refresh→private multipart photo VERIFIED→Report→own read/supplement/publication. OTP verify token is legacy transport, then existing Android login for correct transport; no new route necessary for online MVP. Reporter does not inherit signed Crew FIELD offline grants.

Crew authorized offline needs current per-project/actor device enrollment, client-generated private keys (BE stores registration/public keys), canonical P-256 ECDSA signature and correct wire encoding, task snapshots/pins, origin/hash/source/assignment revalidation, per-item durable ACK/reconciliation. Idempotency does not replace cryptographic signatures; signatures do not certify historical UTC or legal human presence. Key/device loss can block pending-data recovery, while synced records and authorized handover while device/key accessible remain separate.

SQLite SQLCipher/equivalent, encrypted external photo/file store, secure token storage, Keystore-backed lifecycle, backup/temp/cache/plaintext migration and Expo native feasibility are FE responsibilities/design proposals needing real client validation. Backend handover encryption does not prove app local encryption. No Android-only product claim or fully-working Expo claim.

## Tests actually executed / NOT_RUN

Fresh current-task isolated MVC/OpenAPI and EF model discovery exit0 with241/223/47/357 and196/191/1945/1940; no SQL/storage/inference requests or workers run. EF emitted configured default/sentinel warning for FieldInspectionTask.Purpose; warning is not a reproduced workflow defect.

Fresh command: `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --filter 'FullyQualifiedName~AuthenticationOpenApiTests|FullyQualifiedName~ManualSwaggerContractTests|FullyQualifiedName~ApiPlatformPositiveTests' --logger 'trx;LogFileName=hhws-phase0-push-openapi.trx' --results-directory "$env:TEMP/HHWS-Phase0-Capture" --verbosity quiet`: **16PASS/1FAIL/0SKIP**, exit1. Failure `ProductionInventoryAndEvidenceExcerptsMatchCurrentSource`, SWG-014 BusinessDutyRepository excerpt assertion at test L347; no disabling/fix. Existing read-only V2 checker and package validator each exit1 at CONTRACT_LOCK_MISMATCH; later validator checks not reached.

Earlier session96unit PASS at identical source baseline is **HISTORICAL for this push task**, not rerun or E2E. SQL/HTTP business workflow full suite **NOT_RUN**: Docker Linux daemon pipe unavailable; no shared DB fallback. Actual Web/browser, Expo device/native encryption, FastAPI checkpoint3classes/video/provenance/model metrics, MinIO/SMTP/GIS/deployment/SLO/restore **NOT_VERIFIED**.

## Report pack (six files)

1. This00_EXECUTIVE_SUMMARY.
2. [Source and docs inventory](01_SOURCE_AND_DOCS_INVENTORY.md).
3. [Full API inventory](02_FULL_API_INVENTORY.md).
4. [Business/platform JEV conflicts](03_BUSINESS_AND_PLATFORM_CONFLICTS.md).
5. [Docs migration matrix](04_DOCS_MIGRATION_MATRIX.md).
6. [Open decisions/gaps and future Phase1 gate](05_OPEN_DECISIONS_AND_GAPS.md).

Canonical structure/migration approval is only needed **before Phase1**, not to complete this authorized Phase0 push. Real AI provider protocol/local key policy and localized source decisions require scoped later checkpoints; old authority already confirmed is not re-requested. Literal external Project Docs, phiếu FA26SE219 and actual Web/Expo/AI repos were not supplied/found in scoped workspace; no fabricated comparison.

Two concise self-reviews: pass1 authority/transport/role/scope/source/runtime/AI/offline evidence; pass2 exact six-file scope, unchanged old/source hashes, complete inventory/ref counts, relative link rebase and reproducible controller snippet, test failure classification/commit staging. External review/E2E acceptance NOT_VERIFIED. **STOP after successful normal Phase0 push; no Phase1 work.**
