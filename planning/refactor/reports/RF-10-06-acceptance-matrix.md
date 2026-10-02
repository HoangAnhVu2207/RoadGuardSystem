# RF-10-06-C01 Acceptance Matrix

| Case | Source/fixture | HTTP assertion | Durable assertion | Status |
|---|---|---|---|---|
| A positive processing-job read | Supervisor creates a valid dataset/model job through `POST /api/v1/processing-jobs` (202 Accepted); receipt is queried by actor/key/operation with `ProjectId == null` | GET same queued job returns 200 JSON; exact DTO field set, types/nulls, id/status/attempt/jobType/version and ETag relation asserted | Stable identity/state/content snapshots for job, attempts, detections, audit, job-correlated outbox and exact create receipt unchanged | RUNTIME_TESTED |
| B Reporter role | Same job and Reporter bearer token | GET returns 403 problem JSON, code `access_forbidden` | Snapshot unchanged | RUNTIME_TESTED |
| B wrong project | PM has active membership only in project B; job belongs to project A | GET returns 403 problem JSON, code `access_forbidden` | Snapshot unchanged | RUNTIME_TESTED |
| C projection/privacy | Success body from A | Queued DTO schema/value assertions are grounded in `ProcessingJobResponseDto`; no broad privacy claim from string sentinels | N/A beyond A snapshot | RUNTIME_TESTED, bounded queued claim; completed/populated detection privacy NOT_VERIFIED |
| D missing workflows | Controller/service/caller search | Report/case, PM review, label/export have no route | N/A | SOURCE_INSPECTED / NOT_IMPLEMENTED |
| Defect persistence | `P232DetectionDefectSchemaTests`, repository source | No PM HTTP assertion | Repository-direct entity/review evidence only | SOURCE_INSPECTED; runtime provenance for P232 not available in this correction |
