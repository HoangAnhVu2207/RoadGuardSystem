# RF-10-03-C02 Checkpoint 09 Acceptance Matrix

| Acceptance item | Test/assertion | Evidence | Status/limit |
|---|---|---|---|
| Old replay | Same old payload and `operationId`; same `requestId`; actual request row, project request-id set, assignment/audit snapshot and receipt unchanged | `Rf1003RequestTaskAssignmentCharacterizationTests`, checkpoint-10 final4 TRX | RUNTIME_TESTED |
| Wrong-project B durable proof | Project-B positive create by scoped manager; manager A POST with valid project-B scope returns 403; fresh project-B request/assignment/audit snapshot unchanged | same test | RUNTIME_TESTED |
| V2 create replay side effects | Snapshot task identity/project/operator/status/version, assignment/audit and `SurveyTaskV2Created` receipt before/after same-key replay | same test, checkpoint-10 final4 TRX | RUNTIME_TESTED; response is the stored create projection (`operatorId` A, `NEW_ASSIGNED`, original version) while durable SQL is reassigned |
| GET after reassign | Operator B GET returns 200 with task ID, project, operator B and `REASSIGNED` status; fresh SQL active assignment agrees | same test | RUNTIME_TESTED |
| Outbox evidence | Read current persistence path and message model; no reliable operation correlation query exists for this mutation | `SurveyV2PersistenceService.cs`, `OutboxMessage.cs` | SOURCE_INSPECTED; no runtime enqueue claim |
| Stale/reassign replay snapshots | Separate fresh SQL reads after stale request and after same-key replay; task identity/project/state/version, assignment IDs/operators, audit IDs and reassign receipt are compared | same test, checkpoint-10 final4 TRX | RUNTIME_TESTED; stale different-key receipt creation is recorded as observed behavior |
| Historical provenance | Checkpoint 08 required pre-edit snapshot was absent | checkpoint-08 report/package | LIMITATION remains; not reconstructed |
