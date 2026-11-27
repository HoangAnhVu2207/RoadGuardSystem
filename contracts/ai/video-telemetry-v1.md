# BE video telemetry API v1

Status: implementation contract for the owner-authorized local BE–AI slice.
Adoption: `AiVideoTelemetryController`, `AiVideoTelemetryService`, SQL repository
and focused HTTP/SQL tests. No production compatibility claim is implied.

All routes support Bearer and enforce current project authorization. Video work
discovery and the REAL detection collection also accept the existing HTTPS Web
session cookie. These GET actions require no CSRF token.
No service token, object key, private artifact URL or database credential appears
in these DTOs. Existing candidate/PM decision routes remain authoritative.

| Method and path | Meaning |
| --- | --- |
| GET `/api/v1/projects/{projectId}/ai-videos?datasetId={id}&offset=0&limit=50` | Discover per-video work, including pending/failed/zero-detection work; limit 1–100 |
| GET `/api/v1/projects/{projectId}/ai-jobs/{jobId}/detections?offset=0&limit=50` | Enumerate detections for the successfully ingested current attempt of a REAL video job |
| GET `/api/v1/projects/{projectId}/ai-videos/{workId}/telemetry` | Read current measured/estimated drone-reference intervals and revision history |
| POST `/api/v1/projects/{projectId}/ai-videos/{workId}/telemetry-revisions` | Append an immutable operator supplementation/correction |

Discovery items contain `workId`, `sourceVideoFileId`, `jobId`,
`currentAttemptId`, `state`, `errorCode`; page contains `items`, `offset`, `limit`,
`hasMore`. Discovery does not imply that pending work already has readable results.

The detection collection uses `jobId` from a discovery item. The response has
`jobId`, `attemptId`, `items`, `offset`, `limit`, and `hasMore`. Items have
`detectionId`, `typeCode`, and `confidence` and are ordered by the detection ID
in SQL Server. Advance by `offset + limit` while `hasMore` is true. A successful
job with zero detections returns an empty page with `hasMore: false`.

The collection uses the candidate detail role and scope checks (current project
PM or Supervisor). It returns 401 for an unauthenticated request, 403 for a
denied actor, 404 for an absent project or a job outside the project's REAL
video work, 409 `result_not_ready` while the current attempt has no successful
ingestion, and 422 `invalid_detection_query` for out-of-range paging. Read
`GET /api/v1/projects/{projectId}/ai-candidates/{detectionId}` for the
`boundingBox`, `frameFileId`, timestamp, and current review versions.

Current project PM and Supervisor can read; DroneOperator must retain active
project membership and accepted, unended assignment to the survey. A password
change requirement or inactive account blocks access. Only the assigned operator
can append; PM/Supervisor do not gain operator write authority.

GET telemetry returns `workId`, `attemptId`, `sourceVideoFileId`,
`headRevisionId`, `status`, `measuredTelemetryStatus`, `positionMeaning`,
`intervals`, `revisions`. The ETag is the quoted head revision UUID, initially
`"00000000-0000-0000-0000-000000000000"`. Intervals have `startMs`, `endMs`,
`latitude`, `longitude`; intervals are half-open and bounded by media duration.
`positionMeaning` is always `DRONE_REFERENCE_NOT_GROUND_LOCATION`.

Operators can submit the original DJI video with `telemetryFileIds: []`. The
AI worker uses FFmpeg to extract its embedded SRT after inference. Once the
current attempt has been ingested, GET telemetry returns HTTP 200 with
`warning: "error when get srt"` when `measuredTelemetryStatus` is
`MISSING_TELEMETRY`. The warning is omitted when measured intervals are
available. The warning is independent of job status and detection availability.

POST requires a nonempty `Idempotency-Key` (at most 200 characters) and `If-Match`
with the exact quoted head UUID. Request body is one of these shapes:

```json
{
  "kind": "EXTERNAL_SRT",
  "srtText": "<UTF-8 SRT text with measured GPS cues>",
  "offsetMs": 0,
  "estimate": null,
  "reason": null,
  "correctionReason": null
}
```

```json
{
  "kind": "OPERATOR_ESTIMATE",
  "srtText": null,
  "offsetMs": 0,
  "estimate": {
    "startMs": 0,
    "endMs": 1000,
    "latitude": 0,
    "longitude": 0
  },
  "reason": "Approximate drone reference supplied by operator",
  "correctionReason": null
}
```

The second example is structural, not a real location. Coordinates must be finite
and geographically bounded. Estimate reason is required; later revisions require
an explicit correction reason. An SRT is limited to 2 MiB and strict UTF-8, valid
ordered nonoverlapping cues, GPS and adjusted time bounds. HTTP body cap is 3 MiB.
No interpolation, ground-location inference, automatic defect or inference rerun.

Precedence: latest external SRT revision, then verified explicitly paired SRT with
committed offset, then valid embedded SRT, then latest operator estimate. Estimates
leave measured status `MISSING_TELEMETRY`. Valid measured telemetry blocks an
initial fallback append. Revisions retain source hash, current attempt, actor,
time, parent, original private SRT hash and idempotency fingerprint. History is
append-only; committed video/dataset/detection/PM/crew records are unchanged.

Success is HTTP 200 with `revisionId` and the new ETag. Identical key/payload
replays the same revision. Missing preconditions: 428; invalid payload/version:
422; changed key payload or provenance: 409; stale head: 412; wrong scope/role:
403/404; anonymous: 401. Errors use safe problem JSON with `code`/`correlationId`.

PM candidate facts capture revision head, measured revision and paired/embedded
file identities. Their source version changes with telemetry so a concurrent
correction invalidates a stale PM decision. Previously accepted PM snapshots are
not rewritten. `repair` remains an already patched region proposal.
