# PLAN-V2-REBUILD

## Task metadata

- Owner/branch: `anh` / `anh`
- deliveryStatus: `DONE`
- sourceCheckpoint: `V2-ALIGN-2026-09-28`; manifest baseline `133` operations
- Scope: metadata/trace/lifecycle alignment for all 133 operation task files and manifest; no runtime implementation.

## Source evidence

| Source | Heading / ID | Evidence used | Revision/checkpoint |
|---|---|---|---|
| `planning/V2/task_manifest.json` | 133 task entries | ID/owner/op/path/dependency baseline | Before alignment |
| `TASK_LIFECYCLE.md` | fields/status/completion schema | Task metadata and status separation | Working tree |
| `V2-3_DECISION_REGISTER.md` | D01-D28/32-44 | Decision refs and remaining gates | V2-ALIGN-2026-09-28 |
| Canonical OpenAPI | operationId/path/schema | Contract snapshot source | SHA `65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab` |
| Task prose | Trace/How/Tests sections | Requirement and diagram refs | Per-task source checkpoint |

## Mechanical alignment rules

- Preserve 133 IDs, P1/P2 owners (71/62), method/path/operationId, dependency names, historical completion prose and bespoke contract sections.
- Populate requirement refs from real `FR-*`, `BR-*`, `US-*`, `UC-*`, `AC-*`/trace IDs present in each task; no placeholder IDs.
- Add decision refs by operation domain from the current register; keep technical blockers separate from business approval.
- Add diagram refs for PF/SQ/state families relevant to the operation and `DD/ERD` markers for persistence-sensitive tasks.
- Set `deliveryStatus=TODO` for the unstarted task metadata alignment; keep `implementationStatus=NEEDS_REPO_CHECK` and `verificationStatus=NOT_RUN` unless existing evidence explicitly says otherwise.
- Add `Source evidence` and `sourceCheckpoint` to every task; source evidence is a checkpoint, not a claim that runtime was verified.

## Delivery status

`DONE`

## Completion history

### 2026-09-28 - IN_PROGRESS

The 133 task files and manifest are being aligned mechanically with preserved IDs/owners/prose. Validation and a changed-file inventory will be recorded after the script completes.

### 2026-09-28 - DONE

- Scope/result: aligned all 133 task files and `task_manifest.json` without changing IDs, operationIds, methods/paths, owners or historical prose. Added `deliveryStatus`, decision/requirement/diagram refs and source checkpoints; all unstarted tasks remain `TODO/NEEDS_REPO_CHECK/NOT_RUN`.
- Acceptance: 133/133 files exist; 133 unique IDs and operations; owner totals P1=71/P2=62; requirementRefs=133/133; diagramRefs=133/133; source evidence/checkpoint markers=133/133; manifest source hash matches canonical OpenAPI `65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab`.
- Verification: `python docs/diagram/V2/ci/check_alignment.py` PASS; `check_contracts.py` PASS; `validate_package.py` `STRUCTURAL_CHECKS_PASS`; `test_contract_guard.py` 7/7 PASS; `git diff --check` PASS.
- Side effects: planning/docs metadata only; no endpoint implementation, package, migration, database, seed, API smoke or external action; uncommitted.
- Unverified: current source reuse/partial/new status for each operation and runtime tests remain `NEEDS_REPO_CHECK/NOT_RUN`; those are the `CODE-V2-RECON` audit, not silently marked here.
