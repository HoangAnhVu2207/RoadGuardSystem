# RF-10-09-C01 Scope Matrix - BOX 2

## Traced components

### HTTP inbox (NotificationsController)
- GET /api/v1/notifications/{id} - single notification read (recipient scoped)
- GET /api/v1/notifications - list notifications (recipient scoped, cursor pagination)
- POST /api/v1/notifications/{id}/read - mark read (idempotent, versioned)

### Service layer (NotificationService)
- GetAsync: recipient filter, 404 for wrong user
- ListAsync: cursor encoding/decoding, pagination limit validation
- MarkReadAsync: idempotency key + version fingerprint, replay detection

### Repository layer (NotificationPersistenceService)
- GetAsync: recipient scope WHERE clause
- ListAsync: cursor-based pagination (OccurredAtUtc DESC, Id DESC)
- MarkReadAsync: IdempotencyOperationService transaction, version check, MarkRead mutation

### Outbox consumer (NotificationOutboxConsumer)
- ConsumeAsync: ConsumerEffectService boundary, receipt replay detection
- Consumer name: "notification-inbox"
- Single Add without SaveChanges (delegated to ConsumerEffectService)

### Outbox work repository (OutboxWorkRepository)
- TryLeaseNextAsync: UPDLOCK+READPAST lease acquisition, DeliveryStatus/NextAttemptAtUtc filter
- CompleteAsync: lease owner validation, status → Completed
- RetryAsync: lease owner validation, retry scheduling, DeadLetter after maxAttempts

### Producers (outbox message creation)
- SurveyAssignmentPersistenceService.ReassignAsync → "survey_request.reassigned"
- PrimaryProjectManagerPersistenceService → "project.primary_pm_reassigned"
- DetectionReviewPersistenceService (inspection defect review)
- ProcessingV2PersistenceService → "processing_job.dispatch", "validation_run.dispatch"

## Characterization dimensions

### HTTP inbox authorization and projection
- Positive: recipient GET/List returns own notifications
- Negative: other user GET returns 404, List excludes others' notifications
- Projection: id, body/message, resourceId/sourceEntityId, read boolean, occurredAtUtc, version/RowVersion Base64

### SQL lease/receipt/retry
- TryLeaseNextAsync: acquire lease with UPDLOCK+READPAST, increment DeliveryAttemptCount
- CompleteAsync: status → Completed, clear lease owner
- RetryAsync: status → Pending or DeadLetter if maxAttempts reached
- NotificationOutboxConsumer replay: ConsumerEffectReceipt prevents duplicate Add

### Dispatcher/scheduler/provider reachability
- NO DISPATCHER FOUND: rg/find show no BackgroundService or HostedService consuming IOutboxWorkRepository
- UploadVerificationWorker exists but calls IUploadService.ProcessOneVerificationAsync (different pattern)
- ValidationRunWorker registered but uses different mechanism
- Outbox lease/retry repository exists but has no active caller in production code

## Minimal characterization scope (BOX 2)

### Must verify (focused HTTP + owned SQL)
1. Inbox GET: correct recipient 200 OK, wrong recipient 404, projection fields match
2. Inbox List: recipient scope filter, cursor pagination boundary
3. MarkRead: recipient scope, version check, idempotency replay, SQL ReadAt mutation
4. NotificationOutboxConsumer: replay returns Replayed status, single notification persisted
5. Producer→outbox linkage: SurveyAssignment creates outbox with correct correlationId/messageType
6. Lease acquisition: TryLeaseNextAsync respects DeliveryStatus filter and lease expiry

### NOT_VERIFIED (documented scope limitation)
- Dispatcher caller: no production BackgroundService found calling TryLeaseNextAsync
- Retry scheduling: ScheduleRetry tested at repository level, no dispatcher exercising retry loop
- DeadLetter transition: maxAttempts boundary tested, no end-to-end dispatcher proof
- Real notification delivery: outbox → inbox tested via manual ConsumeAsync, no scheduler proof
- Cursor pagination edge cases: malformed cursor basic validation only
- External consumers: no Web/Android/AI client evidence

### Out of scope (F/G work, blocked by decisions)
- Reports/retention/delete (Q-RF02-08)
- Dispatcher implementation
- Real external provider integration
- Legal hold and authorized dry-run
- KPI formulas and operator ownership

## Evidence approach
- Reuse P207 existing SQL evidence where applicable (consumer replay, recipient FK)
- Reuse P2NotificationApiTests existing HTTP coverage where applicable (owner scope, read replay)
- NEW focused test: producer→outbox→consumer chain for one event type
- NEW focused test: lease acquisition/complete/retry at repository boundary
- Repository-direct proof is NOT claimed as HTTP proof or real delivery proof
- Fresh snapshots per operation with exact field assertions

Status: Matrix complete, ready for test implementation
Captured: 2026-10-01
