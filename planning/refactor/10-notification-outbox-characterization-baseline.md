# RF-10-09-C01: current notification/outbox baseline

2026-10-02, branch anh, HEAD 2efc8a5775f834c7f0fe37cc0ce703011649e1f1 plus dirty current source. Bounded characterization accepted per owner handoff. Docs correction ready for reviewer; parent remains Partial. Full historical baseline preserved in assessment before snapshots and runtime archives.

## Evidence and boundaries

| Surface | Source / behavior | Evidence and limit |
|---|---|---|
| GET /api/v1/notifications/{id} | NotificationsController.Get -> NotificationService.GetAsync -> NotificationPersistenceService.GetAsync: recipient/id filter; id/message/resourceId/read/occurredAtUtc/Base64 version, ETag; outsider 404 | SOURCE_INSPECTED plus reused BOX 2 correction-03 HTTP/SQL; asserted fields only |
| GET /api/v1/notifications | ListAsync default 25, max 100; Base64 JSON cursor OccurredAtUtc/Id; descending time/ID, less-than predicate, limit+1 | SOURCE_INSPECTED plus reused pagination case; no scale/version-stability proof |
| POST /api/v1/notifications/{id}/read | Authorize + actor; missing Idempotency-Key or If-Match 428. Service trims version quotes, SHA-256 notificationId:N + pipe + version; no separate Base64 decode. Receipt recipient + null project + NotificationRead + key. Handler stores NotFound/StaleConcurrency/Success; conflict 409, stale 412 | SOURCE_INSPECTED; reused runtime state after first success versus after replay + conflict chain, no separate snapshot after each operation |
| Consumer | NotificationOutboxConsumer.ConsumeAsync -> ConsumerEffectService.ProcessAsync; notification-inbox + message receipt returns stored EffectId | Correction-03 runtime: fresh DbContext, different candidate ID, original ID, candidate absent, one selected recipient/source notification and one consumer/message receipt. Repository-direct |
| Producer | SurveyAssignmentPersistenceService.ReassignAsync: assignments/audit/outbox/command receipt; survey_request.reassigned, correlation surveyRequestId, request/previous/current assignment payload | Correction-03 direct repository test, no reason in event payload, no HTTP authorization/automatic routing |
| Lease | OutboxWorkRepository.TryLeaseNextAsync: UPDLOCK/READPAST/READCOMMITTEDLOCK/ROWLOCK; due time, expiry, attempts, status; sets Leased/increments attempt | SOURCE_INSPECTED this turn; five C01 cases do not execute lease/retry; old RF-08 evidence remains historical |
| Retry/complete | CompleteAsync/RetryAsync separate transaction; OutboxMessage.EnsureLeaseOwner; supplied delay gives nextAttempt, cap gives DeadLetter | SOURCE_INSPECTED; no automatic exponential backoff or delivery timing proof |

## Production callers and registration

RoadGuardPersistenceExtensions registers NotificationOutboxConsumer and IOutboxWorkRepository. Local API/Services/Repositories C# searches (exclude Migrations/bin/obj, normal rg ignores) found declarations/registrations but no production invocation of these notification paths. Commands/results: assessment payload checks/outbox-reachability.json. This is a scoped search result, not deployment evidence.

ServiceCollectionExtensions registers ValidationRunWorker, which calls IProcessingV2Repository.CompleteNextValidationRunAsync. That method selects pending due validation_run.dispatch directly, acquires/completes/retries a lease and saves validation results. Repository-wide dispatcher absence cannot be inferred. UploadVerificationWorker calls upload verification directly and is conditionally registered in Program.

Other inspected producers: PrimaryProjectManagerPersistenceService.PersistAsync (request.CorrelationId, membership IDs), DetectionReviewPersistenceService.PersistAggregateAsync (provided event/correlation), SurveyDataValidationAdmissionPersistenceService.AdmitAsync (dataset correlation), ProcessingV2PersistenceService.CreateAsync/RetryAsync/CreateValidationAsync (job/run correlation). Reachability differs; caller matrix records it without claiming all producers are HTTP-reachable.

## Reused runtime and provenance

Test: tests/RoadGuardSystem.ApiTests/Notifications/Rf1009NotificationInboxCharacterizationTests.cs, SHA-256 f699530832800c4b247699e19a4fe56cdb88a0cf422966c07e5ce3bd339e986e. BOX 2 correction-03 5/5 reused; current source identity checked. BOX 1 correction-04 is separate 10/10. No new combined 15/15, build or rerun. Late F-C13-03/F-C13-04 labels refer to consumer/MarkRead correction-03; original IDs had different meanings, see finding mapping.

## Open limits and gates

Notification scheduler/provider/deployed backlog, Web/Android clients, retry timing, cursor boundary/performance, retention/report formulas and deletion/hold authority remain NOT_VERIFIED/UNKNOWN. [Assessment](reports/RF-10-09-assessment.md), [caller matrix](../../docs/history/refactor/post-checkpoint13-assessment/notification-matrix.md), [finding ledger](../../docs/history/refactor/post-checkpoint13-assessment/findings-ledger.md). No F/G, migration or DB work authorized by this baseline; no historical provenance gap is erased.
