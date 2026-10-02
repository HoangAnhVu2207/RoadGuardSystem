# RF-10-04: File upload, video/SRT and storage provenance

> Current execution 2026-10-01: Anh alone owns R/C refactor on `anh`; C01 is reported separately. F/G is unassigned for later development; proposed B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** PARTIAL locally as of 2026-10-01: C01 current HTTP boundary characterization completed in `planning/refactor/reports/RF-10-04-C01.md`; R assessment and F/G implementation/release remain pending. Report parent `planning/refactor/reports/RF-10-04.md` is not yet a Done claim. Branch `anh`, local HEAD `2efc8a5` plus dirty tree. Test-only SQL rehearsal is RF-09 evidence, not Files migration proof.
- **Goal:** reconcile R13, CG10/17 and Accepted 38: image 20 MiB, video 8 GiB, SRT 10 MiB, dataset 32 GiB, multipart/resume, no lifetime segment video cap.
- **In scope:** upload/file/session/part verification, file scope, source video/SRT pair provenance and approved size-type/schema transition. **Out of scope:** fabricating SRT telemetry/CRS, changing external storage provider, deleting immutable source files or imposing undocumented segment cap.
- **Dependencies/decisions:** RF-10-02 project scope, RF-09 migration/consumer plan, isolated storage setup; actual video/SRT/route samples and consumer upload limits. CG17 is a static accepted-requirement mismatch, not yet an HTTP test result.
- **Read first:** R13, CG10/17, decision 38, `UploadsController`, `UploadService.cs:191`, `UploadPersistenceService.cs:35,58`, `StoredFile.SizeBytes`, file/upload mappings/migrations, storage adapter, Upload API/SQL tests.
- **Likely files:** upload Controller/Service/Repository/DTO/entity/config/migration, worker, focused API/SQL/storage tests; `contracts/`, FE/Postman/`.http` only with approved public delta.
- **Contract/data/consumer effect:** file size representation must support 8 GiB and existing `int` rows; plan `long` additive column/dual-read or safe type widening after schema inspection. Version error semantics and size ceilings for Android/web; preserve file ID/checksum/object key and existing downloads. Dataset 32 GiB enforcement is distinct from single-video limit.
- **Steps:** reproduce boundary in isolated tests -> inventory existing size data and deployed SQL type -> design additive compatible migration/backfill/rollback -> apply only approved test/prod step -> validate multipart/resume/verified bytes and dataset totals -> map SRT timing evidence when real samples exist.
- **Verify:** 20 MiB/8 GiB/10 MiB/32 GiB boundary cases without storing huge fixtures unnecessarily, overflow/part count and SQL `bigint` tests, retry/duplicate/failed verification, isolated object-storage smoke and checksum; capture HTTP status/body/headers and durable state.
- **Done when:** Accepted 38 limits enforced and tested in an approved environment, old files remain readable, data/consumer transition and recovery rehearsed; sample-dependent SRT parsing explicitly Partial if unavailable.
- **Recovery:** feature-flag size acceptance and retain old read representation during expansion; restore isolated backup or forward repair after migration, never delete stored evidence or applied migration.

## Two-developer delivery supplement

- **Proposed owner:** B. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
