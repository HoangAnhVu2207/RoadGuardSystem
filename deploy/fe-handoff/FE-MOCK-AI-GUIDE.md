# Development Mock AI

Owner-approved frame access: take `frameFileId` from an authorized AI result/candidate, then GET `/api/v1/files/{frameFileId}` for existing metadata and GET `/api/v1/files/{frameFileId}/content` for PNG bytes. These routes resolve completed worker provenance without creating an UploadSession. They reuse current AI-result authority: active Supervisor, or active PM with current project membership; operator and unrelated PM access are denied. Authorization is checked again after opening the content stream. No signed URL or storage key is returned. Previously observed404 is historical pre-patch evidence, retained in the Task34 addendum. Never send storage credentials to FE or fabricate upload sessions.

Task34 source candidate; independent review NOT_PERFORMED. No FULL_CONTRACT promotion. Task34 runtime verification is reported separately in its evidence index.

Fresh handoff deployments explicitly enable Anh02.MockEnabled in Development only. HandoffBootstrap creates one fixed synthetic model baseline, verifies collisions and uses a serializable transaction inside the EF execution strategy. Existing configurations are never silently opted in or upgraded.

scenarios/demo_ai.py drives real geometry, plan/task acceptance, multipart SURVEY_VIDEO upload, checksum verification, dataset creation and AI run APIs. The internal worker reads the verified local synthetic MP4 and persists actual result/frame provenance through SQL/MinIO. Journal keys ai-result-A/B-* identify output; no passwords, tokens or signed URLs are recorded there.

Three demo presets on Project A/B: synthetic-road-v1 (one CRACK), synthetic-empty-v1 (no detections), and synthetic-multiclass-v1 (CRACK at 500 ms and POTHOLE at 1500 ms). The two samples intentionally reuse the pinned synthetic image bytes; distinct timestamps, frame identities, object keys and persisted provenance demonstrate deterministic fixture handling, not visual inference. The seven general mock HTTP presets remain separate fixture contracts. No authoritative diagnosis/approval/publication is fabricated.

The worker is gated to Development/Testing/Test plus explicit MockEnabled; ordinary Production cannot run it. The preview denies external AI callback authentication. A1 external issuer, token TTL/protocol and transport acceptance remain BLOCKED_RULE. This internal deterministic mock does not implement real YOLO. P1-02 active-attempt fencing and P1-04 TTL300 remain unchanged.

Setup completion means scenarios exited0, not merely API healthy. Failure/retry/stale behavior must be evaluated from separately executed focused tests, never inferred from a SUCCEEDED journal row. Existing seed/version ownership controls are mandatory.
# A1 receipt compatibility amendment — 2026-10-24

Owner-approved callback retry policy: a new receipt binds job, attempt, manifest,
model, mode, raw result file, checksum and validated detections. Exact replay
returns the stored result only with valid AI JWT and the current attempt.
Changed identity or an identity-incomplete legacy receipt returns HTTP 409
`duplicate_request`. Legacy receipts and processing results are preserved.

On legacy 409, stop automatic callback retry and reconcile the job/result through
the authorized operational path. Do not use a new key to rerun a completed
callback or edit/delete the historical receipt. A legitimate new processing
attempt follows the existing retry workflow and receives its own credential.
RS256/kid, issuer/audience and TTL 300 seconds remain required for MOCK and REAL.
This amendment changes replay compatibility, not the external AI deployment
verification status. See
[Owner decision](review-evidence/milestone2-r1-closure/OWNER-A1-REPLAY-RECEIPT-COMPATIBILITY.md).
