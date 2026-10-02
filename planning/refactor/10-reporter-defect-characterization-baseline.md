# RF-10-06-C01 Reporter/Defect Characterization Baseline

## Provenance

- Branch: `anh`; local HEAD: `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; `origin/anh` was not refreshed for this slice. The working tree was already dirty and all pre-existing paths were preserved.
- Scope: current implemented Reporter onboarding, processing-job read authorization/projection, defect/AI entity provenance, and caller inventory. No production, schema, migration, contract, CI or shared database change.
- Evidence labels: `SOURCE_INSPECTED` means source/caller search; `RUNTIME_TESTED` means the focused isolated SQL Server API test; `NOT_IMPLEMENTED` means no production path was found in this repository; `NOT_VERIFIED` means an external consumer/deployment or unexercised behavior was not proved.

## Current inventory

| Area | Current source and callers | Result |
|---|---|---|
| Reporter onboarding | `ReporterRegistrationsController` -> `IIdentityOnboardingService`; register/verify/resend routes are present. Existing onboarding tests cover OTP and resend. | `SOURCE_INSPECTED`; historical/current onboarding evidence is separate from this C01. |
| Processing-job create/read | `ProcessingV2Controller.CreateProcessingJob`/`GetProcessingJob` -> `ProcessingV2Service` -> `ProcessingV2PersistenceService`. Service accepts Supervisor, ProjectManager and DroneOperator for reads, then applies project scope. | `RUNTIME_TESTED`: production POST returned 202 Accepted; Supervisor GET returned 200; Reporter and a PM with membership only in project B returned 403 with `access_forbidden`. |
| Projection/privacy | Queued response fields observed: `id`, `status`, nullable `resultId`, `attemptNumber`, `version`, `jobType`, nullable `error`; `ETag` equals the response `version`. | `RUNTIME_TESTED` for this queued schema and values only; completed-job projection, populated detections and broad secret/raw-detection privacy remain `NOT_VERIFIED`. |
| Durable effects of read | Fresh contexts compared stable identity/state/content for ProcessingJob, attempts, detections, ProcessingJob audit rows, job-correlated outbox rows, and the exact create receipt (`ActorUserId + IdempotencyKey + ProcessingJobCreated + ProjectId == null`) before/after each GET. | `RUNTIME_TESTED`: no change observed for the bounded records/fields after success or denied reads; not a whole-database no-write claim. |
| Reporter report/case | No `Report`/`IncidentCase` controller or production service caller was found; draft operation IDs D034-D044 have no implementation. | `NOT_IMPLEMENTED`; no HTTP coverage claimed. |
| Defect/PM review | `Defect`, `AIDetection`, verification entities and `DetectionReviewPersistenceService` exist. No production controller/service caller for PM defect decision was found. | `SOURCE_INSPECTED` / `NOT_IMPLEMENTED` for PM HTTP workflow. P232 source/test location was inspected, but correction-01 has no runtime provenance for it; it is not PM HTTP authorization evidence. |
| Label/training export | No production label approval or training-export implementation/caller was found. | `NOT_IMPLEMENTED`; 34A remains a target requirement only. |

## Characterized current behavior

The processing-job create route returns `202 Accepted`; the subsequent read route returns `200 application/json` for a scoped Supervisor and includes an `ETag` equal to the queued response `version`. The queued field set is exactly `id`, `status`, `resultId`, `attemptNumber`, `version`, `jobType`, and `error`; `resultId` and `error` are null in this fixture. A Reporter is rejected before repository read with `403 application/problem+json` and code `access_forbidden`. A ProjectManager whose active membership is only in another project receives the same `403`/`access_forbidden`. This records implementation behavior; it does not decide whether future Reporter visibility should be 403, 404, or another policy.

The test creates its job through the production HTTP create path. The dataset/model/road/survey rows are isolated fixture prerequisites; no job state or defect result was fabricated by direct SQL. The read assertions do not require a completed detection, so no callback is invented for this C01.

## Findings and limits

- `RF1006-C01-F01` (medium): processing-job read projection and scope behavior are implemented and characterized for three roles. Evidence: `ProcessingV2Controller.cs`, `ProcessingV2Service.cs`, `ProcessingV2PersistenceService.cs`, and the focused API test.
- `RF1006-C01-F02` (high): report/case, PM defect review and label/export HTTP paths are absent from the repository search. Do not count their absence as a successful workflow; feature slices remain open.
- `RF1006-C01-F03` (medium): P232 source/test material is not a correction-01 runtime proof; no production caller or HTTP PM authorization was found.
- Accepted 33A/34A define requirements (project-scoped candidate matching and PM-approved labels) but do not establish a wire contract, visibility policy, or implementation.
- External Web/Android/AI consumers, deployed rows, provider behavior, completed-job projection with populated detections, raw-detection/secret privacy beyond the asserted queued body, and production dispatcher reachability remain `NOT_VERIFIED`.

## Next slice

Keep current onboarding and processing read behavior provisionally. RF-10-06-F/G work requires Q-RF02-07 privacy/routing and matching decisions, an approved case/label contract, and consumer ownership. No F/G implementation or 10-06-C02 is started by this baseline.
