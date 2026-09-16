# Antigravity task and review contract

Antigravity receives one task ID from `planning/RoadGuard_Plan_Person_1.md` or `planning/RoadGuard_Plan_Person_2.md` and must not silently expand its scope.

## Before editing

1. Read `AGENTS.md`, the assigned plan row, and only the relevant specification sections.
2. Write a short task manifest: trace codes, actor, preconditions, allowed transition, data impact, authorization, audit, idempotency/concurrency, output files, and test commands.
3. Create the negative tests and confirm they fail for the expected reason before implementing.

## During editing

- Preserve unrelated working-tree changes.
- Use `apply_patch` or the repository's normal code-editing mechanism; do not hand-edit generated migrations or generated clients unless the task explicitly owns them.
- Keep changes within the assigned output list. If another file is required, add it to the handoff log and explain the dependency.
- Stop and record an ADR/open decision for a material specification conflict, new external service, schema compatibility break, or security-sensitive behavior.

## Handoff package

Complete a copy of `Skill-plan-agents/Antigravity_Completion_Log_Template.md` with:

- task ID/trace and owner/reviewer;
- files added/modified/deleted and a one-line purpose for each;
- migrations, configuration, API contract, seed-data, and docs impact;
- negative tests, positive tests, command lines, exit codes, and coverage/known gaps;
- decisions, assumptions, conflicts, residual risks, and the exact next action.

The reviewer repeats the relevant tests and checks authorization, state transitions, versioning/immutability, idempotency, concurrency, audit, and sensitive logging. A reviewer finding is not closed by explanation alone; the code or test evidence must change, or the Product Owner must accept the documented risk.
