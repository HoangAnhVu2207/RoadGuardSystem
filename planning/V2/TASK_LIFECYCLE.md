# RoadGuard V2 task lifecycle

This file defines durable task progress. It does not replace `contractStatus`, `implementationStatus` or `verificationStatus`.

## Authoritative fields

Every active V2 API or governance task records:

| Field | Meaning |
|---|---|
| `deliveryStatus` | Progress of the assigned task: `TODO`, `IN_PROGRESS`, `PARTIAL`, `BLOCKED`, `DONE`, `REOPENED` |
| `contractStatus` | Wire-contract maturity from the decision register vocabulary |
| `implementationStatus` | Runtime/source comparison result |
| `verificationStatus` | Evidence level actually executed |
| `sourceCheckpoint` | Base revision plus relevant source/contract revision or hash |

`deliveryStatus` is authoritative in the task file and mirrored in its index/manifest entry. A docs task lives under `planning/V2/Governance/` and governance index; it is not inserted into the 133-operation manifest.

The verification vocabulary includes `NOT_RUN`, `PASS_FOCUSED_AUTH`, `PASS_FOCUSED_SQL_AUTH`, `PASS_EXTERNAL_SMOKE`, and `PARTIAL_*` variants. `PASS_EXTERNAL_SMOKE` is an evidence status under `verificationStatus`; it is not a separate delivery status.

## Required source evidence

Before editing, append or refresh this table in the assigned task:

| Source | Heading / ID | Evidence used | Revision/checkpoint |
|---|---|---|---|
| AGENTS/ADR/decision | Exact path + heading/ID | Boundary/approval | Base commit or content checkpoint |
| BR/FR/UC/US/AC | Real IDs | Actor/rule/exception | Source revision |
| PF/SQ/state | Diagram/state ID | Transition/transaction | Source revision |
| OpenAPI | operationId + schema names | Wire contract | Canonical SHA-256 |
| DD/ERD | Concept/relationship | Current/target/proposed persistence | Model checkpoint |
| Source/tests | Symbols/test IDs | Current implementation evidence | Base commit |

Record only sources relevant to the task. After context compaction or resume, read the task, `sourceCheckpoint`, current Git status/diff and only invalidated sources.

## Status transitions

- `TODO -> IN_PROGRESS`: approved scope starts; record owner, branch, base revision, source evidence and pre-existing dirty paths.
- `IN_PROGRESS -> PARTIAL`: independent output delivered but one or more required gates remain; list each gate and next action.
- `IN_PROGRESS/PARTIAL -> BLOCKED`: a required gate cannot progress without a named decision, dependency or environment; never use `BLOCKED` for optional improvements.
- `IN_PROGRESS/PARTIAL/BLOCKED -> DONE`: every acceptance criterion in the approved task scope passes and completion evidence is recorded.
- `DONE -> REOPENED`: source/contract changed or evidence was invalidated; state exactly what changed. Do not erase the prior DONE entry.
- `REOPENED -> IN_PROGRESS/PARTIAL/BLOCKED/DONE`: follow the same evidence rules.

Task status changes immediately after the corresponding gate result. Do not wait for a separate user reminder and do not mark every task at the end of a batch. Historical completion entries are append-only in meaning.

## Completion history schema

Each entry uses:

```markdown
### YYYY-MM-DD HH:mm +07:00 - <STATUS>

- Scope/result: what changed and why.
- Files: exact changed paths.
- Acceptance criteria: passed/failed items and evidence.
- Verification: exact command/config/filter, executed/pass/fail/required-skip counts and environment.
- Reused/invalidated evidence: what remained valid and what was rerun after edits.
- Side effects: package, migration, schema, data, external system, commit/push.
- Unverified/blockers: explicit risks and affected tasks.
```

Use `UNCOMMITTED` when no commit exists; never invent a SHA. A docs validator is `DOCS_PASS`, not runtime `DONE` for an API. An API task cannot be `DONE` without required Postman/static evidence, fresh selected tests and real smoke/durable-effect evidence, unless its approved scope explicitly defines a different gate.

## Synchronization

After changing status or metadata:

1. Update the task file first.
2. Mirror `deliveryStatus`, separate contract/implementation/verification fields and blockers in `task_manifest.json` or the governance index.
3. Regenerate canonical snapshots only from the canonical contract tool; preserve bespoke prose and historical entries.
4. Run lifecycle/manifest/link/snapshot guards.
5. Report mismatches as failure; do not edit evidence text merely to satisfy a string check.
