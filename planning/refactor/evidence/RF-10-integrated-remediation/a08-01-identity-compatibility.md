# A08-01 identity and compatibility design

## Current path

`ProcessingV2Controller.ReceiveAiResult` accepts route `jobId`, `Idempotency-Key`, and `ReceiveAiResultRequestDto`. `ProcessingV2Service.ReceiveResultAsync` validates the DTO and serializes detections to Web JSON. `ProcessingV2PersistenceService.ReceiveResultAsync` loads the route job before idempotency, scopes the receipt as `(ActorUserId=null, ProjectId=job.ProjectId, Operation=ProcessingAiResultReceived, Key)`, and currently hashes only `DetectionsJson + ChecksumSha256`. `IdempotencyOperationService.ExecuteAsync` probes the receipt before invoking the handler and maps a same-scope fingerprint match to stored outcome. Attempt, manifest, model, mode, and raw-file checks are inside the handler.

## Identity matrix

| Field | Current normalization | Current fingerprint | Validation location | Receipt/effect implication |
|---|---|---|---|---|
| Route JobId | Guid route binding | omitted | route + repository lookup | changes scope lookup before receipt; same project/key can hit existing receipt |
| AttemptId | DTO Guid, non-empty | omitted | service and handler membership | identity check is after replay probe |
| ManifestHash | lowercase 64-hex DTO rule | omitted | service + handler equality | cannot be reconstructed from receipt |
| ModelVersionId | DTO non-empty Guid | omitted | service + handler equality | receipt has no model field |
| Mode | exact `MOCK` or `REAL` | omitted | service + handler equality | casing is contract-sensitive |
| RawResultFileId | DTO non-empty Guid | omitted | service + verified upload/project/checksum query | receipt has no file field |
| ChecksumSha256 | lowercase 64-hex | included, concatenated without field delimiter | service + file query | changing it changes legacy fingerprint |
| Detections | validated then Web JSON array | included, concatenated without version/delimiter | service + JSON parser | ordering and numeric serialization follow current serializer, not a declared protocol |

## Recommended v2 design, pending owner approval

Use a versioned, length-delimited UTF-8 envelope with fixed order: `v2\0jobId\0attemptId\0manifestHash\0modelVersionId\0mode\0rawResultFileId\0checksum\0detectionsJson`. GUIDs are lowercase `D`-format without braces; mode and hashes retain the current uppercase/lowercase contract; nulls are encoded as an explicit `-` only if the owner permits optional fields. Detections retain source order and current Web JSON number formatting until a provider contract says ordering is semantic or canonical sorting is safe. Hash the exact UTF-8 bytes with SHA-256 and store the version in the fingerprint prefix or a new receipt field only after schema approval.

Do not silently reorder detections, change casing, or reinterpret decimals in this slice. JobId and payload identity must match before a callback is eligible. Same key + same v2 bytes replays the original outcome; same key + different v2 bytes returns the existing 409 duplicate error and creates no second business effect.

## Legacy compatibility options

| Option | Legacy success/rejection retry | Legacy identity mismatch | Concurrent/mixed rollout | Risk |
|---|---|---|---|---|
| A. Same operation/key, version-aware fingerprint | Exact old bytes can replay only through explicit legacy branch; old rejection remains old rejection. New request with same key but v2 bytes conflicts. | Cannot prove omitted fields from receipt; reject or require caller migration. | Safest key scope, but fallback can preserve the identity gap if broad. Rollback must keep both readers. | Requires schema/version marker or deterministic legacy discriminator; no duplicate effect if legacy branch is read-only. |
| B. New operation namespace | Old receipt retries under old operation; v2 uses new receipt. | Old key can execute again under v2 and may duplicate a completed effect. | Mixed clients are easy to route, rollback is dangerous after v2 effects. | Not recommended without persisted completed-effect guard and owner-approved migration. |
| C. Reject legacy callbacks after cutover | Old success/rejection gets explicit migration error; no replay ambiguity. | All unversioned identity mismatch is rejected. | Operationally clear but requires provider cutover and retry horizon. | Caller change required; may lose retries unless old route remains read-only. |

**Recommendation:** Option A with an explicit compatibility window and a narrow legacy reader that accepts only exact legacy fingerprint + persisted success/rejection outcome; never infer missing identity from mutable job state. If schema cannot add a version marker, choose C rather than namespace B. Owner must decide retention horizon, legacy rejection status, provider key uniqueness, and whether raw-file identity or content identity is authoritative.

Late attempts, completed jobs, provider retries, and concurrent races remain unverified by this package. SQL unique scope and transaction behavior are retained as existing primitives; no claim is made that they fence a stale attempt.
