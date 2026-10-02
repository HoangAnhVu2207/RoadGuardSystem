# RF-10-07: Inspection, measurement, Fast Track and repair

> Current execution 2026-10-01: Anh alone owns R/C refactor on `anh`. F/G is unassigned for later development; proposed A/B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** PLANNED; re-record branch/HEAD/status. Report `planning/refactor/reports/RF-10-07.md`; checkpoint measurement/reminder, temporary safety, policy evaluation and repair/acceptance separately.
- **Goal:** reconcile R10-12/CG09 and Accepted 32A/35A with evidence-backed measurement, PM batch, temporary safety and human acceptance workflow.
- **In scope:** field inspection/measurement provenance, PM weekly review reminder and self-grouped batch, policy version/evaluator, repair proposal/attempt/rework/traffic release/acceptance states after approval. **Out of scope:** invented Fast Track numeric/material thresholds, auto task assignment/edit grant, temporary action closing a defect or replacing acceptance.
- **Dependencies/decisions:** RF-10-06 defect ownership, RF-10-04 file evidence, RF-09 schema; Q-RF02-04 approved method/material dossier/threshold and Q-RF02-07 generic proposal versus technical policy boundary. Q-RF02-05 may affect survey-derived evidence.
- **Read first:** R10-12, CG09, decisions 32A/35A, prompt Appendix E as mixed confirmed/proposed, ADR 005 historical generic repair summary, inspection/measurement entities, validation worker, draft repair operations and relevant SQL tests.
- **Likely files:** inspection/measurement Services/Repositories/DTOs/entities, new policy/repair slices and migrations only when approved, notification event producer, API/SQL/state tests, contracts/Postman/`.http`.
- **Contract/data/consumer effect:** new PM/Supervisor/crew workflows need actor/state/versioned contract and Android/Web handoff. Additive policy/repair schema with source dossier ID/version, BEFORE evidence and immutable attempts; old measurements remain readable. No policy activated from a template.
- **Steps:** characterize measurement integrity -> implement PM-only batch/reminder without auto assignment -> model temporary safety action/Supervisor notice -> seek approved dossier -> evaluate fail-closed -> implement proposal/attempt/acceptance slices with transaction/outbox boundaries.
- **Verify:** project/role tests, reminder dedup, no unauthorized task grant, SQL measurement immutability and concurrency, safety action not closing defect, policy missing-input UNKNOWN/INELIGIBLE handling, BEFORE/traffic-release/acceptance and rework transitions. No field accuracy claim from fixtures.
- **Done when:** approved slices pass API/SQL/audit tests and threshold/dossier source is recorded; if Q-RF02-04 is unanswered, policy activation and repair acceptance remain Partial/Blocked while independent measurement work may finish.
- **Recovery:** disable new evaluation/assignment path, preserve measurement/evidence/attempt history, revert only additive route; schema forward repair/approved backup restore, never erase accepted audit.

## Two-developer delivery supplement

- **Proposed owner:** A. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
