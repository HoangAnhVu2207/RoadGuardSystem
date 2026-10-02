# RF-10-03-C02 case matrix

| Case | Test/evidence | Final run | Assertion |
|---|---|---|---|
| Old create -> V2 GET same ID | `Rf1003RequestTaskAssignmentCharacterizationTests` | `rf1002-rf1003-correction-final.trx` | 404; SQL old request exists, no assignment |
| V2 create -> assigned GET | same | `rf1002-rf1003-correction-final.trx` | 201 then 200; same task/operator/status |
| Wrong role | same | `rf1002-rf1003-correction-final.trx` | Reporter GET 403 |
| Wrong project | same | `rf1002-rf1003-correction-final.trx` | manager posting to second project 403; SQL counts unchanged |
| V2 replay | same | `rf1002-rf1003-correction-final.trx` | same task ID; request/assignment counts unchanged |
| Missing precondition | same | `rf1002-rf1003-correction-final.trx` | missing idempotency key 428 |
| Assignment/reassignment/stale precondition lifecycle | `Rf1003RequestTaskAssignmentCharacterizationTests` | `rf1003-c02-checkpoint08-final3.trx` | reassign POST ends operator A assignment, creates operator B assignment, records audit; stale `If-Match` 412; same-key replay leaves assignment/audit counts unchanged; no outbox row is produced by this path |
| Historical plan-postpone precondition | `P2SurveyV2ApiTests.SurveyV2Endpoints_CreateReplayPostponeWithConcurrencyAndReadScope` | historical runtime evidence | plan-postpone 412 only; not assignment lifecycle evidence |
| Geometry correction | `Rf1002ProjectGisCharacterizationTests` | `rf1002-rf1003-correction-final.trx` | prior type/SRID/ordered coordinates preserved |
| Membership provenance | `P211ProjectMembershipReadModelTests` | `rf1002-membership-correction-final.trx` | 4/4 fresh SQL tests |
