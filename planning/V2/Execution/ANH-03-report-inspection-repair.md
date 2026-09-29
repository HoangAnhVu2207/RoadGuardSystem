# ANH-03 - Report, case, defect, inspection and repair persistence

- Owner/branch: Anh / anh
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: data-fixture
- sourceCheckpoint: record HEAD and canonical OpenAPI hash at task start

## Task goal
Establish persistence facts for the chain from reporter report through triage/case/defect assessment, inspection measurement, repair package/attempt, approval, curing, traffic release and acceptance. Every business state and evidence version must remain durable and auditable.

## Business context
A report may be outside warranty and still enter triage. A defect is not automatically repair authorization. BEFORE evidence remains durable; measurement completion, physical completion, traffic release and acceptance are distinct. PM/Supervisor decisions and safety work must not collapse into one status.

## Operation trace
Reports/cases/defects: V2-P1-024..042. Policy/inspection/repair: V2-P1-043..071, with V2-P2-036 and V2-P2-056 only as read/export facts consumed by Huy.

## Sources to read
- docs/design/02_Requirements/01_FRD_SRS.md FR-09..26; 02_Business_Rules.md BR-03..19 and BR-30..39.
- docs/design/03_Data/ERD_Report_Case_Defect.md, ERD_Inspection_Repair.md, DD sections 3.5-3.6.
- docs/design/05_Technical/02_Auth_Permission_Model.md, 05_Sequence_Diagrams.md SQ-01/02/05, 07_State_Machines_V2.md, Appendix E/F.
- Decision register D02-D11, D19, D32A-D35A and current Defect, Inspection, Repair, Warranty and policy source/tests.

## In scope
- Reconcile report/case/defect identity, report-to-defect visibility, severity/recurrence and warranty responsibility facts.
- Verify inspection assignment/accept/decline/submit, measurement provenance, immutable snapshots and fast-track eligibility.
- Verify repair package/item/attempt versions, assignment/reassignment, review/approval, emergency safety scope, curing and traffic-release evidence.
- Add focused SQL/concurrency tests and minimal persistence fixes within approved source/schema.
- Publish state transitions, ownership facts, version tokens, audit/outbox effects and fixtures to HUY-03.

## Out of scope
- Inventing physical repair thresholds, policy production values, AI matching, field/device evidence or UI.
- Service/API/DTO/HTTP/Postman edits.
- New operations for reopen, handover, incident or partial publication without an approved backlog contract.
- Migration/live DB changes without explicit scope.

## Exact files and hotspots
Primary areas: BusinessObjects/Reports, Cases, Defects, Inspections, Repairs, Warranties and policy entities; corresponding repositories/configurations and SQL tests. Shared DbContext, migrations/snapshot and seed remain single-writer hotspots.

## Stop conditions
Requires project/scope facts from ANH-02. Stop on state-machine versus current-schema conflict, missing durable BEFORE evidence, unresolved authorization/acceptance rule or a proposed physical threshold presented as production fact.

## Verification and acceptance
Build Repositories and IntegrationTests. Run focused defect, inspection, repair, warranty and concurrency SQL tests from fresh binaries; inspect durable state/audit/outbox records. Acceptance requires every changed transition covered, no state collapse and HUY-03 handoff VERIFIED or NO_CHANGE_NEEDED.

## Completion history
- TODO until all gates pass.





