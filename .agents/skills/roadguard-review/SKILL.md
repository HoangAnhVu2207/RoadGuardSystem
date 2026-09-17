---
name: roadguard-review
description: Use when Codex reviews RoadGuard task code, a diff, completion evidence or a P1/P2 handoff for mandatory acceptance and task status updates, or when an explicitly read-only review is requested. Not for implementing tasks or automatically fixing findings.
---

# RoadGuard task review

Review the requested task against this checkout's contracts and observable behavior. Answer in the user's language. Codex is the mandatory acceptance reviewer, authorized by AGENTS to record task review/status and mark Done only after verified acceptance. Explicit read-only/report-only requests prohibit metadata writes. Review does not authorize implementation fixes, commits or publication.

Read [the shared review contract](references/review-contract.md). It defines scope, evidence, severity and the report format for all three review skills. Repository [AGENTS.md](../../../AGENTS.md) remains authoritative.

## Select only the needed depth

- P1 task: load `$roadguard-review-p1` from the sibling skill directory for domain, Services, DTO and API checks.
- P2 task: load `$roadguard-review-p2` from the sibling skill directory for entity shape, EF/SQL Server, storage, workers and delivery checks.
- Paired tasks or integration review: use both specialists plus [handoff checks](references/handoff.md). Inspect each owner's contribution without reallocating implementation work.
- Documentation/tooling: use the shared contract and relevant existing verifiers; load a specialist only if the changed content affects its contracts.

Task IDs, scope and dependencies come from the current person plans, not from numbering symmetry. Inspect actual code before treating a completion log as proof. Return substantiated findings first, verification gaps second, then a verdict limited to the reviewed scope. Zero findings is a valid result; do not manufacture issues to fill a checklist.

Antigravity implements, self-reviews and submits Ready for review. Codex returns Changes requested/Blocked or records Done, using stable finding IDs and the same acceptance scope through fix rounds. Read the [reusable task prompts](../../../docs/prompts/RoadGuard_Task_Workflow.md) for handoff and the full assignment/implementation/acceptance loop.

## Invocation examples

```text
Dùng $roadguard-review review P1-12 và P2-11 trong diff hiện tại; chỉ đọc, báo lỗi theo file/dòng và dependency còn thiếu.
Dùng $roadguard-review nghiệm thu task đã bàn giao, ghi findings/status vào đúng worklog và plan; chỉ mark Done khi đủ gate, không sửa code hoặc merge.
```

Use supplied refs only if they resolve locally. If a requested branch/commit is unavailable, report that limitation and continue useful local inspection; Git fetch/integration follows repository-owner authorization.
