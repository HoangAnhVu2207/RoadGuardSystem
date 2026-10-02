# RF-10 Refactor Checkpoint 10 Acceptance Matrix

| Finding | Test/source assertion | Evidence | Status |
|---|---|---|---|
| F1 stale snapshot | Fresh SQL immediately after 412 and separately before/after same-key replay; identity/project/state/version, assignment IDs/operators and audit IDs compared by content | C02 test, checkpoint-10 final4 TRX | RUNTIME_TESTED |
| F2 old request row | Request row fields and project request-id set are queried before/after exact old payload + `operationId` replay | C02 test, checkpoint-10 final4 TRX | RUNTIME_TESTED |
| F3 receipts | Real `IdempotencyRecords` queried for old create, V2 create, stale reassign and reassign replay by actor/project/operation/key | C02 test, source context | RUNTIME_TESTED; stale receipt creation is observed only |
| F4 V2 replay projection | Replay response asserts task ID, project, operator A, `NEW_ASSIGNED`, original version; fresh SQL asserts reassigned operator B | C02 test, checkpoint-10 final4 TRX | RUNTIME_TESTED |
| F5 authorization wording | Assigned operator GET 200, Reporter GET 403 and manager A wrong-project POST 403 are distinct assertions; project-B positive control remains | C02 test, checkpoint-10 final4 TRX | RUNTIME_TESTED |
| Outbox | C02 call chain ends in `SurveyV2PersistenceService.MutateTaskAsync`, which has no enqueue; `SurveyAssignmentPersistenceService` is a separate path with outbox behavior | `SurveyV2Controller.cs`, `SurveyV2Service.cs`, `SurveyV2PersistenceService.cs`, separate repository context | SOURCE_INSPECTED; no C02 runtime claim |
| Historical provenance | Checkpoint 08 pre-edit snapshot was absent | checkpoint-08 package/report | LIMITATION retained |
