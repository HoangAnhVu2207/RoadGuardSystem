# RoadGuard task completion log

Create one copy when assigning a task, named `docs/worklogs/<TASK-ID>-completion.md`. Codex Implementer maintains implementation/self-review evidence; Codex appends mandatory acceptance review. Preserve prior rounds and do not overwrite this template. Use the [shared prompts](../prompts/RoadGuard_Task_Workflow.md).

## Identity and scope

- Task ID/title:
- Owner / self-reviewer:
- Implementer: Codex Implementer for the assigned Person.
- Mandatory acceptance reviewer / Done authority: Codex Reviewer in a separate task/session that did not author the artifacts.
- Date / branch or commit:
- Reviewed baseline and exact change scope (commit or working-tree diff):
- Trace (`US-*`, use case, acceptance criteria):
- In-scope behavior:
- Explicitly out of scope:
- Intended files / exclusive ownership check:
- Conflict warning: `None` or list affected files, task IDs, owner, sequencing, and resolution.

## Assignment and acceptance contract

- Assignment author/date and baseline revision:
- Dependencies and current-checkout evidence:
- Required checks and justified N/A cases:
- Ready for review gate: implementation, applicable checks, Codex Implementer self-review and evidence complete.
- Done gate: Codex verifies all AC, dependencies, required checks, mandatory findings and conflict resolution for the submitted artifacts.

| AC ID | Trace / observable acceptance criterion | In-scope behavior | Required test/evidence |
|---|---|---|---|
| AC-01 | | | |

Record In scope, Out of scope and exclusive files above before edits. Keep this contract stable; record approved scope changes with the decision and impact rather than silently changing criteria.

## Preconditions and decisions

- Actor and project-scope rule:
- State before / allowed state after:
- Data/version/immutability rules:
- Audit event and stable error codes:
- Idempotency/concurrency behavior:
- Assumptions, ADRs, or specification conflicts:

## Files changed

| Change | File | Purpose |
|---|---|---|
| Added/Modified/Deleted | `path/to/file` | One-line reason |

## Database, API, config, and operations impact

- Migration added and recovery/downgrade note:
- API/OpenAPI compatibility impact:
- Configuration/secret/environment impact:
- Seed/data migration impact:
- Worker/storage/queue impact:

## Negative-first evidence

Keep only relevant rows per behavior slice. Group irrelevant categories into one justified N/A entry. Prose uses consistency/link/scenario checks without artificial RED.

| Test | Layer | Expected failure/code | Result |
|---|---|---|---|
| Null/empty/malformed | Unit/API | | |
| Boundary/oversize/memory-safe streaming | Unit/API/component | | |
| Unauthorized/wrong project | Service/API | | |
| Invalid transition/prerequisite | Domain/service | | |
| Duplicate retry/idempotency | Integration/worker | | |
| Stale concurrency | Integration/API | | |
| DB/storage/queue timeout or disconnect | Component | | |
| Integrity/checksum/immutable history | Integration | | |

## Positive evidence

| Test | Layer | Expected state/output | Result |
|---|---|---|---|
| Smallest valid path | | | |
| Representative full path | | | |

## Review packet and evidence reuse

- Implementer task/session and independent reviewer task/session identity:
- Ready-to-run reviewer prompt: prompt C in shared workflow, filled with task, baseline, content identity and worklog.
- Changed files including untracked, AC -> evidence, addressed finding IDs, gaps/risks:
- Gate selection and full-suite trigger or N/A reason:
- Reused evidence: covered content/environment, original command/time and why still valid; distinguish reviewer reruns from inspected results.

## Commands run

Keep RED, positive-contract and GREEN runs in chronological order. Distinguish owner-recorded history from checks rerun for the reviewed scope. Report unexecuted CI/container/SQL checks as gaps, not as passes; static configuration validation is not runtime proof.

| Command | Exit code | Result/coverage | Timestamp |
|---|---:|---|---|
| `dotnet test ...` | | | |
| `dotnet format --verify-no-changes` | | | |
| `dotnet build --no-restore` | | | |

## Self-review and conflict report

- Observable demo/output:
- Known gaps, skipped tests, and reason:
- Unexecuted environments / external acceptance dependencies (FE, real AI, field data):
- Residual risks:
- Self-review findings and resolution:
- Conflict warning final state:
- Codex Implementer submission revision/diff identity, including relevant untracked files:
- Handoff: Codex Implementer pauses submitted-artifact edits and yields task review/status sections to Codex.
- Exact next task/action:
- Latest status assessment date and evidence; supersedes earlier handoff where applicable:
- Implementation status: `Ready for review` or `Blocked`; Codex Implementer never marks `Done`.

## Codex acceptance review — append one section per round

- Reviewer / round / date:
- Reviewed commit or working-tree artifact identity / file scope:
- AC coverage, dependency integration and Codex Implementer self-review checked:
- Checks executed or verified evidence (command, exit code, environment, time, test counts):

| Finding ID | Priority / owner | Location and violated AC | Trigger / impact / closure condition | Codex Implementer fix evidence | Codex disposition |
|---|---|---|---|---|---|
| F-01 (omit row if no findings) | | | | | Open / Fixed awaiting verification / Verified |

- Verification gaps/blockers (separate from code defects):
- Optional out-of-scope follow-ups (do not block agreed AC):
- Conflict warning / shared-metadata ownership:
- Verdict: `Changes requested`, `Blocked` or `Done`; explain evidence.
- Next bounded fix/review action:
- Plan status update: task, plan, old -> new, actor Codex, time; or explicitly report-only/deferred due to ownership conflict.
- Final status: Only Codex records `Done` after mandatory acceptance passes. Preserve old rounds; changed implementation requires review again. Done does not authorize Git integration/publication.
