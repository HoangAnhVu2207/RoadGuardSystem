# Private BE–AI worker v1

Status: implementation contract for the owner-authorized local BE–AI slice in
`planning/development/BE-AI-INTEGRATION-20261119.md`, owner checkpoint 2026-11-19.
This document and `worker-v1.schema.json` are dedicated private contracts, not a
replacement for public processing DTOs or historical worker contracts. Adoption
evidence is the Python validator and shared golden fixtures; actual BE transport,
SQL registration, real inference and deployment require their own executed checks.

## Admission and authentication

Every route requires a dedicated mounted-secret service bearer token. Compare
credentials in constant time; no tokens in URLs, logs, examples or fixtures. AI
does not receive SQL credentials, user JWTs, object-store URLs or local paths.
Configured internal destinations belong to BE, never admission JSON.

| Route | Response |
| --- | --- |
| POST `/internal/v1/jobs` | 202 after durable acceptance; 200 identical replay; 409 changed payload |
| PUT `/internal/v1/jobs/{attemptId}/input` | Stream the admitted video; verify exact bytes/hash before atomic publication |
| GET `/internal/v1/jobs/{attemptId}` | Durable state/progress and safe error code |
| GET `/internal/v1/jobs/{attemptId}/result` | Immutable result JSON; 409 before completion |
| GET `/internal/v1/jobs/{attemptId}/artifacts/{artifactId}` | Stream allowlisted attempt-owned artifact |
| POST `/internal/v1/jobs/{attemptId}/cancel` | Cooperative, idempotent cancellation |

All admission fields are required and additional keys are forbidden. UUIDs are
canonical lowercase, nonzero UUID strings; SHA256 values are 64 lowercase hex
characters. Identity comprises `contractVersion` (`rg-ai-worker-v1`), `projectId`,
`jobId`, `attemptId`, `datasetId`, `sourceVideoFileId`, `manifestHash`,
`modelVersionId`, `checkpointSha256`, `preprocessingVersion`, `configVersion`,
`inputSha256`, `inputBytes`, `inputWidth`, `inputHeight`, `inputDurationMs`,
`sampleFps`, `maxFrames`. Admission is per video: BE schedules every eligible
dataset video separately and must retain overflow work durably.

| Field | Strict local worker limit |
| --- | --- |
| inputBytes | Integer 1–9223372036854775807; not an upload entitlement |
| inputDurationMs | Integer 1–2147483647 |
| inputWidth / inputHeight | Integer 1–16384 |
| maxFrames | Integer 1–10000 |
| sampleFps | Finite number greater than zero and at most 30 (existing analyze_video limit) |
| preprocessingVersion / configVersion | 1–128 safe ASCII identifier characters; first alphanumeric, subsequent alphanumeric, dot, underscore or hyphen |

These are technical worker bounds, not production upload caps. Resource policy
may reject smaller inputs. Booleans/string coercions are forbidden for numeric
fields. Declared dimensions/duration are verified against a bounded probe after
upload; mismatch fails explicitly. Reservation is input bytes plus
`maxFrames * (width * height * 3 + 65536)` plus 64 MiB provenance and 2 GiB reserve.
The local total disk budget/free floor remain additional independent checks.

`validate_admission(dict)` returns a detached dict with `sampleFps` normalized to
a JSON floating number. `canonical_admission(dict)` returns sorted-key compact
UTF8 JSON bytes (`ensure_ascii=False`, `allow_nan=False`); replay stores/compares
these bytes or their SHA256. Integer and floating sampleFps representations share
normalization. Same attempt and canonical bytes replay without a new slot; any
field change conflicts. Never implement replay by comparing only model/hash.
Unknown JSON keys and noncanonical identifiers fail, rather than normalize away
meaningful identity. JSON duplicate property names must be rejected by the HTTP
decoder before constructing the dict.

## Durability and failure

States are AWAITING_INPUT, QUEUED, RUNNING, COMPLETED, FAILED, CANCELLED,
INTERRUPTED. One running plus three pending includes AWAITING_INPUT; identical
replay does not count again. Fifth live admission returns 429. No input for 15
minutes expires the admission visibly. Restart marks RUNNING as INTERRUPTED;
never silently rerun. Retry uses a new attemptId. COMPLETED means worker output
ready, not BE business acceptance.

Return 401 for bad service credentials, 404 for absent attempt/artifact, 409 for
identity conflict/not-ready, 422 for invalid input/hash/model/configuration,
429 for queue capacity and 507 for operational disk budget. Safe error strings
must not include caller field values. Stable error codes distinguish
INPUT_CHECKSUM_MISMATCH, INPUT_METADATA_MISMATCH, MODEL_MISSING,
MODEL_HASH_MISMATCH, UNKNOWN_SOURCE_CLASS, DISK_BUDGET_EXCEEDED,
ADMISSION_EXPIRED and INTERRUPTED. Transport faults remain retryable with bounded
backoff; data/model failures require correction and a new attempt. The BE bridge
maps operational failure to existing RetryableFailure and invalid source/model
data to DataFailure, retaining the safe code; this mapping is not an authorization
to alter the public callback DTO or skip current-attempt checks.

## Immutable result and conversion

The result echoes every admission field exactly and adds `resultId`, `rawArtifact`,
`frameArtifacts`, `detections`, `telemetry`, `roadMaskStatus`, `limitations`.
Artifact metadata is `{artifactId, sha256, bytes, mediaType}`; frames add positive
`width,height`. Raw is application/json, frames image/jpeg and derived SRT
application/x-subrip. Artifact and detection IDs are nonzero UUIDs generated
once, persisted with completed output and reused on every read/restart/replay.
IDs are private worker identities, never registered BE FileIds. All artifact
IDs are unique within one result. BE verifies streamed hashes/sizes, registers
actual files and persists the private-to-BE ID association before ingesting.

Each detection is `{detectionId,frameArtifactId,timestampMs,sourceClass,confidence,bbox}`.
Frame reference must exist. Confidence is finite in [0,1]. Timestamp is an integer
in [0,inputDurationMs]. Convert actual observation seconds using decimal half-up
milliseconds, never frameIndex/FPS for VFR input. Pixel xyxy becomes normalized
xywh `[x1/W,y1/H,(x2-x1)/W,(y2-y1)/H]`; reject negative, non-finite,
inverted, zero-area or out-of-frame boxes. Never clamp. Normalized origin plus
extent must remain within [0,1] on each axis. Maximum frame count is the admitted
maxFrames; maximum detections is maxFrames times 1000. Duplicate detection IDs
are rejected. Schema describes individual JSON shapes; the Python validator
also checks admission equality, cross references, uniqueness and sum/time bounds.

Preserve exact source labels: `alligator crack`, `longitudinal crack`,
`oblique crack`, `pothole`, `repair`, `transverse crack`. Released-model mapping
must resolve six distinct active BE codes; unknown classes fail, with no CRACK
fallback or subclass collapse. `repair` describes an already patched region.
Every observation is a proposal; no automatic defect approval or repair creation.

PROPOSED isolated released-model catalog mapping (must be explicitly activated
and verified before dispatch; this table does not itself seed/adopt a catalog):

| Exact FT95 label | Distinct BE code |
| --- | --- |
| alligator crack | ALLIGATOR_CRACK |
| longitudinal crack | LONGITUDINAL_CRACK |
| oblique crack | OBLIQUE_CRACK |
| pothole | POTHOLE |
| repair | REPAIR_PATCH |
| transverse crack | TRANSVERSE_CRACK |

`telemetry` contains `source` (EMBEDDED, PAIRED_SRT, NONE), `outcome`
(EXTRACTED_VALID, NO_SUBTITLE_STREAM, UNSUPPORTED_STREAM, INVALID_TELEMETRY,
TELEMETRY_AMBIGUOUS, EXTRACTION_FAILED), `sourceVideoSha256`, nullable nonnegative
`streamIndex`, `extractorVersion` and nullable `srtArtifact`. Source hash must
match admission. Valid extraction requires a real derived SRT artifact and a
source other than NONE. Stream identity and extractor version preserve origin;
separately paired verified SRT takes precedence. Failed extraction remains
distinct from absence. SRT does not mutate the original manifest. Measured drone
position, operator estimate and crew ground-defect position remain distinct;
this result never invents GPS or physical dimensions.

Current producer: worker v1 receives only the video, so it emits embedded/absent
telemetry; `PAIRED_SRT` is reserved vocabulary, not an implemented upload route.
BE separately resolves paired verified survey uploads and their committed offset
before embedded telemetry in the read view. Operator supplementation uses the
public project-scoped API documented in `video-telemetry-v1.md`; it never rewrites
this immutable worker result or re-runs inference.

`roadMaskStatus` is AVAILABLE or NOT_AVAILABLE. `limitations` contains at most
32 safe uppercase codes; missing road mask/telemetry must be represented
explicitly. The golden fixture is synthetic metadata, not actual media or
checkpoint inference evidence. Golden result intentionally contains all six
classes and missing measured telemetry. Python tests validate both fixtures and
negative admissions. Public DTO compatibility and real artifact registration
are separate evidence gates.
