# Antigravity completion log

Create one copy when assigning a task, named `docs/worklogs/<TASK-ID>-completion.md`. Antigravity maintains implementation/self-review evidence; Codex appends mandatory acceptance review. Preserve prior rounds and do not overwrite this template. Use the [shared prompts](../prompts/RoadGuard_Task_Workflow.md).

## Identity and scope

- Task ID/title:
- Owner / self-reviewer:
- Implementer: Antigravity for the assigned Person.
- Mandatory acceptance reviewer / Done authority: Codex.
- Date / branch or commit:
- Trace (`US-*`, use case, acceptance criteria):
- In-scope behavior:
- Explicitly out of scope:
- Intended files / exclusive ownership check:
- Conflict warning: `None` or list affected files, task IDs, owner, sequencing, and resolution.

## Assignment and acceptance contract

- Assignment author/date and baseline revision:
- Dependencies and current-checkout evidence:
- Required checks and justified N/A cases:
- Ready for review gate: implementation, applicable checks, Antigravity self-review and evidence complete.
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

List each negative/edge case before positive cases. If a standard case is irrelevant, state why.

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

## Commands run

| Command | Exit code | Result/coverage | Timestamp |
|---|---:|---|---|
| `dotnet test ...` | | | |
| `dotnet format --verify-no-changes` | | | |
| `dotnet build --no-restore` | | | |

## Self-review and conflict report

- Observable demo/output:
- Known gaps, skipped tests, and reason:
- Residual risks:
- Self-review findings and resolution:
- Conflict warning final state:
- Antigravity submission revision/diff identity, including relevant untracked files:
- Handoff: Antigravity pauses submitted-artifact edits and yields task review/status sections to Codex.
- Exact next task/action:
- Implementation status: `Ready for review` or `Blocked`; Antigravity never marks `Done`.

## Codex acceptance review — append one section per round

- Reviewer / round / date:
- Reviewed commit or working-tree artifact identity / file scope:
- AC coverage, dependency integration and Antigravity self-review checked:
- Checks executed or verified evidence (command, exit code, environment, time, test counts):

| Finding ID | Priority / owner | Location and violated AC | Trigger / impact / closure condition | Antigravity fix evidence | Codex disposition |
|---|---|---|---|---|---|
| F-01 (omit row if no findings) | | | | | Open / Fixed awaiting verification / Verified |

- Verification gaps/blockers (separate from code defects):
- Optional out-of-scope follow-ups (do not block agreed AC):
- Conflict warning / shared-metadata ownership:
- Verdict: `Changes requested`, `Blocked` or `Done`; explain evidence.
- Next bounded fix/review action:
- Plan status update: task, plan, old -> new, actor Codex, time; or explicitly report-only/deferred due to ownership conflict.
- Final status: Only Codex records `Done` after mandatory acceptance passes. Preserve old rounds; changed implementation requires review again. Done does not authorize Git integration/publication.
