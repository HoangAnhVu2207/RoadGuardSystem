# RF-10 Checkpoint 08 Case Matrix

| Case | Test/evidence | Final run | Assertion |
|---|---|---|---|
| Old create -> V2 GET | `Rf1003RequestTaskAssignmentCharacterizationTests` | `rf1003-c02-checkpoint08-final3.trx` | 404; old request has no assignment |
| V2 create -> assigned GET | same | same | 201 then 200; identity/operator/status match |
| Reporter wrong role | same | same | GET 403 |
| Wrong project create | same | same | POST 403; project/task counts unchanged |
| V2 create replay | same | same | same task ID; assignment count unchanged |
| Reassignment | same | same | POST 200; previous assignment ended; replacement operator active; audit +1; outbox unchanged at 0 |
| Stale reassignment | same | same | different key with old version returns 412 |
| Reassignment replay | same | same | same task result; assignment/audit/outbox counts unchanged |
| Missing V2 key | same | same | 428 |
| Geometry correction | `Rf1002ProjectGisCharacterizationTests` | checkpoint-07 final2 evidence | type/SRID/ordered coordinates preserved |
| Membership provenance | `P211ProjectMembershipReadModelTests` | checkpoint-07 final2 evidence | 4/4 fresh SQL tests |
