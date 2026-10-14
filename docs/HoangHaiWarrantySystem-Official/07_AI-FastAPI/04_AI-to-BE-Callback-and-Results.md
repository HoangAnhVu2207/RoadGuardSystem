# AI callback and result admission

The callback route is `POST /api/v1/internal/processing-jobs/{jobId}/results` (API-031), protected by named `AiCallback` authentication. `AiServiceBearer` requires issuer, audience `roadguard-be-ai`, lifetime, signing key/kid, non-empty subject and `client_type=AI_SERVICE`; a human PM token is not a callback credential.

`ReceiveAiResultRequestDto` carries `JobAttemptId`, `ManifestHash`, `ModelVersionId`, `Mode`, `RawResultFileId`, `ChecksumSha256` and `Detections`. Each detection is validated as a GUID ID, frame file ID, non-negative timestamp, non-empty type, confidence 0..1 and four normalized bbox values contained within the image. The Backend checks attempt belongs to job, exact manifest/model/mode, verified raw-result checksum and project before persistence.

The accepted result creates `AIDetections` and completes the job; it does not create a Defect or physical dimensions. PM still decides candidate disposition and Crew measures in FIELD.

**P1-02 PARTIAL:** current repository admission does not fully fence latest/active attempt and receipt identity; late, out-of-order or replay exploitation is `NOT_VERIFIED`. A provider must send the exact attempt and canonical result identity and receive a deterministic duplicate/conflict response. Callback retries must use bounded backoff and preserve the original payload; no retry success is claimed here.

The AI worker's callback implementation posts a JSON result to `callback_url` with a ten-second timeout and records a local error string. It does not show service bearer injection, attempt headers, response admission, durable retry or dead-letter handling. This is a concrete integration gap.
