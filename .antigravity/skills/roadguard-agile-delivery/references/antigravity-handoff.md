# Antigravity task and review contract

Use the task assigned in one of the two plans. Root AGENTS.md is canonical; old logs and this reference cannot broaden the user's request.

## Before edits

1. Verify branch, dirty files, task status and completed dependencies. Explicit report-only requests require inspection/reporting without edits. Codex task acceptance includes task-scoped review/status records under AGENTS, not an implementation assignment.
2. Declare exclusive files/shared hotspots. Person 2 establishes entity/property/enum shape; after that task is Done, Person 1 may add domain invariants without reopening schema. Record reopening or overlap as a Conflict warning.
3. Create docs/worklogs/<TASK-ID>-completion.md from [the completion template](../../../../docs/diagram/Antigravity_Completion_Log_Template.md), or preserve and append to its existing history. Record actor, stable AC/use-case trace, transition, In scope, Out of scope, audit, retries/concurrency, exclusive files and test commands. Explain N/A business cases for tooling. Use [shared prompts](../../../../docs/prompts/RoadGuard_Task_Workflow.md).
4. Read relevant specs/references only. A missing material decision blocks that slice, not independent authorized checks.

## During edits

- Preserve unrelated changes using supported editing tools. Never discard working files to simulate a test-first history.
- New, unshared migrations may be regenerated or corrected within their assigned task, with mappings/model snapshot kept consistent. Shared/applied history gets a new corrective migration. Inspect sharing/application status rather than guessing.
- For a fix, record negative evidence, positive tests, then implementation. For existing code without evidence, add a regression exposing the bug and report chronology honestly; never invent an earlier RED.
- Entity-local methods/invariants belong in BusinessObjects. Cross-aggregate policy/transactions belong in Services, not EF configuration or controllers.

## Completion package

Record reviewed baseline/diff, explicit changed files, schema/API/config impacts, commands with exit codes/timestamps/environments, expected RED versus unexpected failures, positive outcomes and limitations.

Antigravity performs task-owner self-review of authorization, state transitions, immutability/versioning, idempotency, concurrency, audit and missing tests. Resolve self-review findings/conflicts and submit Ready for review; Antigravity never marks Done. Freeze submitted artifacts and yield task review/status sections to Codex. Missing required evidence leaves the task Blocked.

Codex acceptance is mandatory for both Persons under the current policy. Codex appends a review round with exact artifact identity, findings, checks and verdict, then updates the task's plan status. All AC, dependency integration, self-review, required checks and mandatory findings must be verified before Done. Explicit report-only requests produce chat evidence only. Serialize shared metadata writes; status authority never authorizes implementation fixes or another task's edits.

Use stable finding IDs through Changes requested -> In Progress -> Ready for review. Antigravity maps each fix to evidence; Codex verifies closure. Keep optional out-of-scope improvements separate from blockers. Changes to reviewed implementation require another acceptance pass; status-only bookkeeping does not. Preserve old rounds and historical Done records; retired review task IDs stay retired.

State whether hosted CI, live Compose, SQL integration, real AI and FE integration actually ran. Static checks, mock datasets and old logs cannot replace them. Tooling completion does not sign off a backend feature or another person's task.

Git commit/integration/push follows AGENTS.md; handoff alone does not authorize protected-branch commits or publication.
