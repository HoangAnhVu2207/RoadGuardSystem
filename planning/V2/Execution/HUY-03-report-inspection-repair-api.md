# HUY-03 - Report, case, defect, inspection and repair API

- Owner/branch: Huy / huy
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- sourceCheckpoint: record HEAD and canonical OpenAPI hash at task start

## Task goal
Expose the operational workflow from report and triage through defect assessment, inspection measurement, fast-track/repair decisions, attempts, safety work, traffic release and acceptance while preserving authorization, privacy, evidence versions and state-machine boundaries.

## Business context
Reports can be received outside warranty. Defect publication is per authorized report/defect projection, not whole-case leakage. Measurement, physical completion, traffic release and acceptance are different decisions; AI suggestions never approve work.

## Operation trace
V2-P1-024..071 plus read/export operations V2-P2-036 and V2-P2-056 where the accepted contract requires them. Consume ANH-03 facts.

## Sources to read
Read operation cards, FR-09..26, BR-03..19/30..39, ERD_Report_Case_Defect, ERD_Inspection_Repair, DD 3.5-3.6, permission/API/error specs, SQ-01/02/05, state machines, Appendix E/F, decisions D02-D11/D19/D32A-D35A and ANH-03 handoff.

## In scope
- Write per-operation contracts and current-versus-target matrices before edits.
- Verify actor/project scope, report privacy, defect mapping/publication, severity/recurrence, assignment accept/decline/submit, versioned measurement, repair assignment/reassignment, review/approval and emergency safety boundaries.
- Keep curing, traffic release and acceptance separate in DTO/state mapping and stable errors.
- Add focused API/unit tests, update API.http/Postman and run real HTTP smoke with durable state/audit evidence.
- Ask for a linked decision when reopen/handover/incident/partial-publication behavior is not an approved contract.

## Out of scope
- Persistence/EF/migration edits, inventing repair thresholds/material policies, external field/device proof, AI accuracy or UI.

## Exact files and hotspots
Services/Reports, Cases, Defects, Inspections, Repairs and policy interfaces; DTOs, controllers, API.http, Postman and API/unit tests. Shared authorization/errors/OpenAPI require reservation.

## Stop conditions
Stop on state-machine conflict, missing durable BEFORE evidence, privacy projection ambiguity, unresolved approval authority or proposed production threshold. Mark exact operation PARTIAL/BLOCKED.

## Verification and acceptance
Build fresh Services/API/ApiTests. Run focused authorization/state/privacy tests and real smoke for wrong scope, state transition, response/header and durable audit/outbox effect. Acceptance requires ANH-03 handoff VERIFIED or NO_CHANGE_NEEDED.

## Completion history
- TODO until all gates pass.



