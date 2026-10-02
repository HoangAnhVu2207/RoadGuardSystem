# RF-10-06: Reporter case, defect candidates and PM review

> Current execution 2026-10-01: Anh alone owns R/C refactor on `anh`. F/G is unassigned for later development; proposed A/B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** PARTIAL; `10-06-C01` is done for current implemented-path characterization. Report `planning/refactor/reports/RF-10-06-C01.md`; report/case, PM review and label/export F/G slices remain gated.
- **Goal:** reconcile R08-09/CG07-08 and Accepted 33A/34A while preserving human defect decisions and Reporter privacy.
- **In scope:** Reporter report/case ownership, out-of-warranty routing, candidate match within project, PM assessment/merge decisions, approved training-label export gate. **Out of scope:** AI auto-approval/merge, public image disclosure without policy, treating draft `IncidentCase` as deployed.
- **Dependencies/decisions:** RF-10-02 project/segment ownership; RF-10-04 files; RF-10-05 AI candidate provenance for that checkpoint; Q-RF02-07 for Reporter visibility/out-of-warranty routing and data mapping. Exact matching parameters/consumer contract need review.
- **Read first:** R08-09, CG07-08, 33A/34A full text, FR-11..14 and ADR 005 as historical reference, `ReporterRegistrationsController`, `Defect`/`AIDetection`/verification logs, draft OpenAPI report/case/label operations and tests.
- **Likely files:** future approved Reporter/Case/Defect Controller/Service/Repository/DTO/entity/config/migrations, authorized file projection, focused API/SQL tests, contract/Postman/`.http` and BE-AI training export fixtures. C01 changed only a focused API test and planning evidence.
- **Contract/data/consumer effect:** new public Reporter/PM routes need versioned auth/privacy, external Web/Android consumer review. New tables/backfill link reports to project/defect without changing old defect IDs; source provenance and actor retained. 33A candidate search is never an automatic merge; 34A requires PM-approved label before export.
- **Steps:** decide privacy/object ownership -> design state/actor matrix -> implement/report intake separately -> add candidate selection with version and no-GPS fallback -> add PM decision/audit -> gate training export on approval.
- **Verify:** wrong Reporter/project and PII/image non-leak API cases, SQL case/defect mapping and concurrent PM decision, match candidate ranking with/without GPS, no-auto-merge, only approved labels exported; Postman/consumer contract checks.
- **Done when:** each enabled workflow has owner-backed contract, authorization and durable audit proof; unknown public projection remains Blocked/Partial, not silently omitted from completion claim.
- **C01 evidence:** `10-reporter-defect-characterization-baseline.md`, `reports/RF-10-06-C01.md`, `reports/RF-10-06-acceptance-matrix.md`; one isolated API test passed for processing-job read. Report/case, PM review and label/export remain `NOT_IMPLEMENTED`.
- **Recovery:** disable new routes/export while retaining report/candidate records; restore from isolated backup or forward repair approved schema, never delete source evidence to undo an association.

## Two-developer delivery supplement

- **Proposed owner:** A. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
