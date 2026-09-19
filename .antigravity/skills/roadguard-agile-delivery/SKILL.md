---
name: roadguard-agile-delivery
description: Use when planning, implementing, diagnosing or reviewing an assigned RoadGuard ASP.NET Core backend task, its SQL Server persistence, tests, delivery evidence or agent tooling.
---

# RoadGuard backend delivery

Follow the user's requested scope and [repository rules](../../../AGENTS.md). This skill routes work; it does not authorize extra features, Git integration or external writes.

## Start with the current checkout

1. Inspect git status --short --branch; read the assigned row and dependencies in the applicable existing plan under planning/. Read both plans for cross-person changes. Completion on another branch does not prove integration here.
2. Read relevant specifications in docs/diagram/: Data Dictionary, then ERD/Domain Model, then Use Cases, then User Stories. Accepted decisions are in docs/adr/; check that an ADR exists on this branch before referencing it.
3. For edits, declare task ID and exclusive paths in docs/worklogs/<TASK-ID>-completion.md using docs/diagram/Antigravity_Completion_Log_Template.md. Preserve unrelated changes. Person 1's paired implementation waits for the Person 2 schema task to be Done.

## Route the request

| Request | Action and supporting reference |
|---|---|
| Review, explain or diagnose | Inspect/report evidence, impact and locations; run relevant existing checks. Codex task acceptance additionally records task review/status under the standing AGENTS authorization. Explicit report-only requests remain read-only. Do not implement findings during review. |
| Plan/refine | Record actor, AC/trace, prerequisites, files, ownership, tests and decisions in the existing plan. Use observed capacity; two weeks is a target, not measured velocity. |
| Implement/fix behavior | Read [negative-first workflow](references/negative-first-workflow.md) and [stack contract](references/csharp-dotnet-stack.md); execute one assigned AC slice. |
| Schema/migration | Read the stack reference and Data Dictionary field-by-field; prove SQL/spatial/concurrency behavior on SQL Server. |
| Documentation/tooling | Verify references, discovery and script/config behavior. Human prose does not need tests asserting wording. |
| Complete/handoff | Read [handoff contract](references/antigravity-handoff.md); Codex Implementer implementation/self-review ends at Ready for review; mandatory Codex acceptance alone can mark Done. |
| Library research/MCP | Read [documentation MCP tools](references/mcp-tools.md); use local versions and authoritative sources. |

## Backend boundaries

Android/Web belongs to FE. Real AI training/inference/DSM comes later; use a deterministic adapter fake with validated provenance. Test research software with controlled imported pairs; synthetic metrics do not establish field accuracy. Research Validation never automatically transitions operational Defect/Warranty records.

Services owns orchestration/cross-aggregate decisions; BusinessObjects owns entity-local invariants; Repositories owns EF/storage; API owns HTTP; DTOs owns public contracts. Preserve enum values, immutable evidence, project scope and worker-only confirmation.

## Completion example

For a defect-review slice: confirm persistence readiness, declare files and In scope/Out of scope, write wrong-PM/cross-project/missing-measurement/stale-version tests, observe RED, add the accepted-decision test, implement, verify state/audit/retry outcomes, then record Codex Implementer owner self-review and submit Ready for review. Codex verifies the submitted artifacts, returns scoped findings until resolved, and records Done only after the acceptance gate. A missing product/schema decision blocks its affected slice; continue independent authorized work.

Use the [shared task prompts](../../../docs/prompts/RoadGuard_Task_Workflow.md) for assignment, Codex Implementer implementation/fixes and Codex acceptance. Preserve historical approvals as history; never reuse them as fresh proof. Mandatory Codex review is a stage of each existing task, not a revived retired review task or a transfer of P1/P2 ownership.
