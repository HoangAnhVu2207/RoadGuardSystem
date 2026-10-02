# D-WORKFLOW: Distinct source, decision and acceptance states

- **Status:** PROPOSED, RF-04 draft.
- **Sources:** historical ADR 005 (itself Proposed), V2 FR/BR/process; TARGET_CONFIRMED PR-32A/33A/34A/35A/39A/41A/42A/43A.
- **Proposal:** retain independent report/AI/field provenance; PM decisions on candidate/label; survey dataset and quality/coverage separately assessed; measurement batch, temporary safety, technical policy, repair proposal, execution and acceptance as distinct transitions. Version route/segment and policy references; offline replay preserves actor and evidence. Hold-aware retention is calculated from valid obligations, never upload date alone.
- **Unknown:** Reporter visibility/out-of-warranty routing, old/V2 survey row interpretation, Fast Track thresholds/material dossiers, Q11 coverage method and retention deletion authority. These are Q-RF02-03..05/07/08, not Accepted by this proposal.
- **Gate:** domain owner decisions, populated-row audit, approved contracts and isolated transition tests. Do not infer target from a V2 label.
- **Correction evidence:** [transition inventory](../product/workflows.md) distinguishes conceptual states and acceptance evidence; [BR-46](../product/historical-fr-br.md#br-46) temporary safety is RF-10-07 primary with RF-10-09-A notification coordination. This ADR remains Proposed.
