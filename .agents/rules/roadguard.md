# RoadGuard discovery

Use the canonical repository policy at `../../AGENTS.md` and the accepted ownership decision in `../../docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md`.
For V2 work, also read `../../planning/V2/TASK_LIFECYCLE.md`, the assigned task and its `Source evidence`/checkpoint before loading feature-specific sources.

- For one endpoint plan, implementation, fix, review, or smoke test, load `../skills/roadguard-endpoint-delivery/SKILL.md`.
- When that endpoint touches EF/SQL, mappings, schema, migrations, concurrency, idempotency, outbox, storage, or offline durability, also load `../skills/roadguard-persistence/SKILL.md`.
- Before choosing or running verification, load `../skills/roadguard-test-selection/SKILL.md`.
- For API additions/changes/removals or manual/collection testing, load `../skills/roadguard-postman/SKILL.md`.
- For requested pre-push self-review/fix, load `../skills/roadguard-review-autofix/SKILL.md` after Postman update and before final verification.

The assigned V2 task owner owns the complete approved endpoint slice through all N-layers. Shared DbContext, mappings, migrations/snapshot, DI, errors, OpenAPI, seed, Docker, and CI require explicit reservation and one writer. Person 2 coordinates migration order, SQL integration, seed, Docker, CI, and release evidence without exclusively owning endpoint persistence.

Repository rules and accepted ADRs win over skills and proposed V2 task text. Skills do not approve contract deltas, open product gates, packages, schema effects, migration execution, or destructive operations.
