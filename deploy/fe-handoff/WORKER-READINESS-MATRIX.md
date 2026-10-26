> Task33 CURRENT UPDATE (supersedes conflicting historical Task30–32 statements below): G01 SegmentSet collection approved by owner and implemented with existing detail authority, status filter and keyset pagination. New isolated deployments enable internal Development Mock AI and create A/B geometry/survey/dataset plus road/empty fixture results. External AI callback/issuer remains blocked. See FE-DISCOVERY-WORKFLOWS.md, FE-MOCK-AI-GUIDE.md, FE-241-API-COVERAGE-MATRIX.md and FE-FEATURE-AVAILABILITY.md. Historical counts below are not fresh Task33 evidence. Rendered243 = canonical241 + G01 + operational /ready.

# Task32 worker matrix

| Worker | Trigger/claim/effect/final | Acceptance scope |
|---|---|---|
| UploadVerificationWorker | API multipart complete202 → durable file verification → checksum/object validation → VERIFIED | Scenario setup waits for real PNG; private content tested after restart; dependency failures do not fabricate success. |
| ExportWorker | POST202 → durable QUEUED/lease RUNNING → PDF rendered/stored → SUCCEEDED | Seeded A/B jobs; admission replay, private download and restart. Lease/retry service/repository safeguards remain; final execution details external evidence. |
| H6NotificationWorker | confirmed calendar/clock/repair source admission → occurrence/outbox → dispatcher → inbox/delivery | Registered by AddApiPlatformServices, not container-only claim. Inbox reads tested; no source-backed demo event fabricated; native FastTrack remains gated. External delivery not accepted. |
| RetentionEvaluationWorker | API evaluation → durable record → snapshot/evaluation COMPLETE | Non-deleting WAITING_RETENTION_BASIS is an expected gate. No deletion job enabled. |
| ValidationRunWorker | existing source validation queue → durable processing | Registered; no complete survey/FIELD demo in kit. Not advertised as tested full lifecycle. |
| AiMockRunWorker | disabled in handoff configuration | Seven deterministic fixture presets reusable; no real AI/issuer acceptance. |

Readiness SQL+bucket+Mailpit is prerequisite readiness, not proof all queues are drained. Scenario container exited0 is separate setup completion. Current runtime commands/postconditions and failures are recorded in Task32 MASTER-HANDOFF/EVIDENCE-INDEX, not inferred from registration.
