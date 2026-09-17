# Shared review contract

## Establish the review boundary

1. Start with `git status --short --branch` and `git rev-parse HEAD`. Read root [AGENTS.md](../../../../AGENTS.md), the assigned rows in [Person 1](../../../../planning/RoadGuard_Plan_Person_1.md) and/or [Person 2](../../../../planning/RoadGuard_Plan_Person_2.md), their dependencies and exclusive ownership. A branch name or historical Done log is not integration proof.
2. Honor the user's task IDs and comparison. For a current diff, inspect `git diff`, `git diff --cached` and explicitly relevant untracked files (`git ls-files --others --exclude-standard`); ordinary diff omits untracked files. For a supplied commit use `git show`; for a supplied local base/head use `git diff <base>...<head>`. Resolve both refs before comparison. Read surrounding callers, guards, mappings and tests so the diff is not judged in isolation.
3. If the task is not stated, infer it only when changed paths and plan rows establish a unique match; disclose the assumption. Otherwise ask for the task/range while continuing read-only inventory. Do not treat all planned future functionality as missing from the current slice. Do not switch branches or overwrite the dirty working tree to review another ref; inspect it with read-only Git tools.
4. Record reviewed HEAD/base/head, staged/unstaged/untracked scope and known dirty-file attribution. For working-tree reviews record the scoped file list and diff identity (or content hashes) when saving evidence. Verify dependency artifacts actually exist in this revision. Distinguish a dependency blocked on integration from a proven defect in the task.
5. Read the assignment's stable AC, In scope, Out of scope, submission and previous findings. Codex is the mandatory acceptance actor after Antigravity implementation/self-review. Confirm the handoff before updating task-scoped plan/worklog sections. Explicit report-only/read-only requests prohibit all such writes; ordinary task acceptance has standing authorization under AGENTS.

## Recover acceptance criteria

Read only relevant sections, using this precedence: [Data Dictionary](../../../../docs/diagram/RoadGuard_Data_Dictionary_v1.md), [ERD](../../../../docs/diagram/RoadGuard_ERD_v1.md) and [Domain Model](../../../../docs/diagram/RoadGuard_Domain_Model_v1.md), [Use Cases](../../../../docs/diagram/Dac_ta_UseCase_v2.md), then [User Stories](../../../../docs/diagram/User_Stories_Acceptance_Criteria_v2.md). Inspect accepted decisions under `docs/adr/` in the current checkout.

Build a compact mapping: task -> US/use-case or TE/RS trace -> changed behavior -> tests/evidence. For each applicable command/query identify authorized actor, current project membership, preconditions, allowed transition, immutable content, audit, retry and stale-version outcomes. A material specification conflict is an open product/schema decision; cite the conflict, describe options, and stop only that affected conclusion. Do not silently resolve it or invent an acceptance criterion.

## Verify proportionally

- Read existing tests before choosing commands. Confirm actual project filenames with `rg --files`; do not assume a task filter exists. Zero discovered tests, skipped SQL tests and compilation alone are not passing behavioral evidence.
- Review requests may run relevant existing safe checks without changing implementation, adding tests or repairing code. Task-scoped review/status records are the only standing write exception; report-only requests have no write exception. Inspect fixture/config requirements first. Use isolated test resources; do not point tests at live/shared databases or execute destructive migrations/retention/backup operations as a review shortcut.
- Before Done for a production task, verify repository-required restore, non-incremental build, formatting and all affected tests for the submitted artifacts. Narrow report-only reviews may run affected checks and disclose the unexecuted wider gate but cannot approve Done. Commands: `dotnet restore RoadGuardSystem.slnx`, `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental`, `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore`, then the discovered affected `dotnet test` projects/filters. The P2 plan can impose additional synchronization/release gates.
- Prose/tooling changes use relevant existing verifiers, such as `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` and `pwsh -NoProfile -File tests/Tooling/Verify-AntigravitySetup.ps1`. Run only checks applicable to the changed scope. Do not create tests that match prose wording. Live MCP checks are relevant only when connection behavior is under review.
- Report exact command, exit code, time/environment, passed/failed/skipped test counts and reviewed revision when available. If SQL Server, containers, a package source or hosted CI is unavailable, identify the blocked check. Static YAML, EF InMemory, historical logs and mock results do not prove SQL, hosted-CI or real AI behavior.
- Inspect negative-first evidence in the task log: negative/edge cases before positive contracts before implementation. Passing current tests cannot reconstruct that chronology; missing evidence is a gap, not proof that the owner violated the order. Recommend missing tests without writing them during a review-only request.
- For library uncertainty inspect pinned SDK/packages and ADRs, then focused official Microsoft Learn/Context7 references if available. Do not upload source, credentials or logs. Research does not authorize upgrades.

## Findings versus verification gaps

A code finding needs a concrete triggering input/interleaving, observed code path, consequence, violated contract and precise location. Follow the path through existing guards before claiming a bypass. For diff reviews identify newly introduced or worsened defects; label pre-existing blockers separately. Prioritize correctness, authorization, loss/corruption, immutability, retries, concurrency and audit over cosmetic preferences.

| Severity | Meaning |
|---|---|
| `[P0]` | Immediate, broad critical failure, data loss or security exposure demonstrable without speculative assumptions. |
| `[P1]` | High-impact defect to fix before integration, such as cross-project access or invalid persistent history. |
| `[P2]` | Concrete bounded correctness or maintainability defect worth fixing. |
| `[P3]` | Low-impact actionable issue; omit stylistic preferences unsupported by a contract. |

Severity `[P1]` is distinct from task owner **Person 1**. State the owner/task separately. Deduplicate one root cause across layers. Missing runs, absent dependency evidence and unknown scope belong in verification gaps, without invented code lines or severity. A missing test can be a finding when its precise omission demonstrably defeats a required regression gate; do not turn every unrun test into a code bug.

Assign stable IDs (F-01, F-02) and closure conditions. Preserve IDs across rounds and distinguish Open, Fixed awaiting verification and Verified. Antigravity supplies fix evidence; Codex verifies closure, including regressions. Optional out-of-scope improvements do not block Done. A defect preventing the agreed AC/security/integrity outcome is a concrete blocker or scope/dependency decision, not an excuse for silent scope growth. Keep AC stable; a completed slice is not the whole task.

## Report contract

1. **Findings**, highest severity first. Each: stable ID, `[priority] actionable title`, file and tight line location, trigger -> faulty behavior -> impact, violated AC/invariant, owner/task, closure condition and minimal correction direction or regression scenario. Use absolute clickable local links; cite verified changed lines for PR diff links. Never invent a line from an excerpt that omits it.
2. **Open questions / verification gaps**: ambiguous specification or ownership, missing dependencies, unavailable environment, tests not run or skipped, and exact next proof needed. These remain separate from code defects.
3. **Scope and verdict**: revision/diff, checks/results and task verdict `Changes requested`, `Blocked` or `Done`. Explain which gate passed or failed. For report-only reviews state findings/readiness without writing a status. Mixed outcomes can state both defects and incomplete verification. Do not call a task integration-ready while a required gate is missing.

If there are no findings, say so explicitly and retain testing limitations. Codex may mark either Person's assigned task Done only after all AC, required checks, dependency integration, Antigravity self-review, mandatory findings and conflict resolutions are verified. Missing required evidence yields Changes requested for missing deliverables or Blocked for an unavailable gate/decision; it never becomes a pass because the code has no findings. Review does not authorize implementation fixes, Git integration, remote publication or messages to others.

For task acceptance, append reviewer/round/time, exact reviewed artifacts, AC coverage, checks, findings/dispositions, gaps and verdict to the task's worklog using [the existing template](../../../../docs/diagram/Antigravity_Completion_Log_Template.md), then update only that task's status in the applicable existing plan. If needed add a status row for an already assigned plan task. Standing owner authorization covers these records without repeated permission. Confirm exclusive metadata handoff and serialize shared-file writes; an ownership conflict defers the write and must be reported, not described as an already-recorded Done. Explicit report-only returns chat only. No third plan or retired task IDs.

Return a bounded fix request for Antigravity when not Done; do not silently implement it. Preserve all rounds. Changed implementation requires fresh review of affected behavior; metadata-only verdict bookkeeping does not invalidate acceptance. See [reusable prompts](../../../../docs/prompts/RoadGuard_Task_Workflow.md) for assignment, fix and acceptance inputs.
