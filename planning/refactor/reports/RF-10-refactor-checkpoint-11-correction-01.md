# RF-10 Checkpoint 11 Correction 01

## Scope and status

- Scope: `10-06-C01` test and documentation/evidence correction only.
- Branch: `anh`; surveyed HEAD: `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`.
- Working tree was dirty before the correction and remains dirty; no unrelated changes were overwritten.
- No production, contract, schema, migration, CI, provider protocol or shared database change was made.
- Bounded C01 status: **Done** for the current queued processing-job characterization. RF-10-06 parent and overall refactor remain **Partial**.

## F1-F5 mapping

| Finding | Correction | Evidence |
|---|---|---|
| F1 receipt scope | Keeps the create key and queries `ActorUserId + IdempotencyKey + Operation == ProcessingJobCreated + ProjectId == null`; asserts the real receipt exists before GET and compares its identity/content after every GET. | `Rf1006ReporterDefectCharacterizationTests.cs`, `ReceiptSnapshot` |
| F2 queued projection | Asserts the exact `ProcessingJobResponseDto` field set, types/nulls, queued values and ETag/version relationship. Reporter and wrong-project PM denial remains `403 application/problem+json` / `access_forbidden`. Completed/populated detection and broad privacy claims are `NOT_VERIFIED`. | focused test, DTO, controller/service/repository source |
| F3 durable snapshots | Fresh contexts project stable IDs, state, linkage and relevant content for job, attempts, detections, audit, outbox and exact create receipt. The conclusion is bounded to observed records/fields. | `ReadSnapshotAsync`, final TRX |
| F4 provenance | New before-build/after-test hashes, exact command metadata, raw console logs, TRX and output assembly hash are retained. Prior failed runs remain unchanged. | `planning/refactor/evidence/rf1006-c01-correction-01/` |
| F5 labels/wording | P232 is `SOURCE_INSPECTED` for this correction because runtime provenance is unavailable. Survey and processing wording distinguish POST 202 from GET 200. | baseline, acceptance matrix, reports |

## Verification

- Build command: `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly`; exit `0`, `0` errors, `108` warnings.
- Focused command: `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build -nologo --filter 'FullyQualifiedName~Rf1006ReporterDefectCharacterizationTests' --logger 'trx;LogFileName=rf1006-c01-correction-01-test.trx' --results-directory planning/refactor/evidence/rf1006-c01-correction-01`; exit `0`, 1 discovered/executed/passed, 0 failed/skipped.
- The build ran before `--no-build` test. Source hashes before build and after test are identical. Output assembly hash is recorded in `provenance-after-test.json`.
- SQL writes were limited to the isolated Testcontainers fixture owned by the API test; no shared database was used.

## Limits and open findings

- Completed-job projection with populated detections, general raw-detection/secret privacy, real external consumers, deployed rows, provider behavior and dispatcher reachability remain `NOT_VERIFIED`.
- Report/case, PM defect review and label/training export have no production path or caller found in this repository; no fake workflow was added.
- Q-RF02-07 remains open. `10-06-C02` and F/G remain unstarted. Historical survey checkpoint-08 provenance and other open ledger limitations are unchanged.
- Patch applicability is `NOT_VERIFIED`; no isolated historical baseline was reconstructed.

## Handoff

The adjacent `RF-10-refactor-checkpoint-11-correction-01-handoff.zip.sha256` sidecar is authoritative for the package hash. The package manifest covers every payload, including the nested pre-edit snapshot manifest; only the root manifest itself is excluded from its own listing.
