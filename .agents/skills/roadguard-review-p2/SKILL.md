---
name: roadguard-review-p2
description: Use when Codex reviews RoadGuard Person 2 tasks (P2-*), EF Core SQL Server schema/migrations, storage/workers, integration tests and Docker/CI evidence for mandatory acceptance, or an explicitly read-only review. Not for implementing fixes or moving P1 policy into repositories.
---

# Review Person 2 tasks

Read the [shared review contract](../roadguard-review/references/review-contract.md) first, then the assigned row/dependencies in [the P2 plan](../../../planning/RoadGuard_Plan_Person_2.md). Apply the [task checklist](references/task-checklist.md) only to the requested slice. The shared contract supplies implementation/metadata boundaries, acceptance authority, evidence rules, severity and report structure; do not route recursively through the general skill.

Acceptance requires a separate Codex task/session that did not author the submission. Same-task self-acceptance is prohibited. Apply root AGENTS.md Lean TDD gates and evidence reuse rules.

## Follow data to durable effects

Trace entity/property/enum -> EF configuration -> migration SQL/model snapshot -> repository/transaction -> integration fixture -> worker/storage/outbox effect.

- Compare types, nullability, numeric enum values, UTC timestamps, precision, defaults, relationships and delete behavior with current canonical specifications. Preserve shared migration history; inspect upgrade and downgrade/recovery evidence.
- Check concurrency tokens, unique/filtered indexes and CHECK constraints under actual SQL Server semantics. Uniqueness can enforce at most one active PM; the transaction/domain contract must ensure at least one. An application-only check does not prevent a database race.
- Inspect geography(4326), project-configured geometry SRID, JSON `nvarchar(max)`/ISJSON plus application schema validation, exactly-one-target constraints and append-only records. Confirm nullable SQL CHECK behavior cannot admit forbidden combinations.
- Verify scoped queries, atomic state/audit/outbox, deduplication, lease ownership and stale results, retry exhaustion and partial external failures. Database transactions alone do not prove exactly-once external effects.
- Use real SQL Server integration evidence for constraints, spatial behavior, migrations and concurrency. InMemory/SQLite, generated SQL and mocked fixtures are not equivalent. Skipped/container-unavailable tests remain verification gaps.

Inspect existing fixtures before executing checks; use only isolated safe test resources. Never apply migrations, delete retained files or rehearse recovery on live/shared systems during a review. P2-01 synchronization and release obligations come from the current plan, not remembered branch tips.

For service/API contracts read [handoff checks](../roadguard-review/references/handoff.md) and flag the precise owner dependency. P2 persistence must support P1 orchestration without taking over its policy. Report findings, gaps and a scoped verdict. Codex records task review/status and may mark Done only after all acceptance gates; explicit report-only means no writes. Do not edit code or integrate branches during review.

```text
Dùng $roadguard-review-p2 review P2-30 trong diff hiện tại; kiểm tra storage, checksum, QualityCheck, migration và bằng chứng SQL Server, không tự sửa.
```
