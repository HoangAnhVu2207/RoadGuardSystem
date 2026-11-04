# Video, SRT and frame pipeline

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

The AI repository's `src/pipeline/pipeline.py::run_video_analysis_task` accepts video, optional SRT, model path, confidence, duration, sampling FPS, tiling and output directory. `src/ingestion/srt_parser.py::DJISRTParser` parses DJI SRT blocks; `video_extractor.py` associates telemetry with sampled frames. The linear pipeline declares seven stages: segment, detect, filter, deduplicate, measure, inspect and report.

These are repository capabilities (`CURRENT_IMPLEMENTED_AI`), not deployed acceptance. Missing/invalid SRT, timestamp mismatch, unreadable video or absent model must produce an explicit failure or missing-telemetry result; it must not be treated as complete GPS coverage. The Backend must preserve source file IDs, checksums, dataset/route/segment/band scope and manifest/model pins.

The worker API's `ManifestModel` carries `video_intervals`, checksum, SRT URI, time bounds, road section/segment context, band, model config and callback URL. The current worker does not demonstrate downloading private object storage artifacts, validating SRT alignment, or invoking a real model in its background branch. Those are `SOURCE_GAP`/`NOT_VERIFIED_E2E`.

Image frames and bbox timestamps are observation provenance. A road GIS segment/section is a geographic planning entity; a pixel road mask is an image operation. One must not be substituted for the other.
