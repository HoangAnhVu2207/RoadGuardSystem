# Side effects and boundaries

Preserve pre-existing dirty files. Work in separate checkout/worktree when two people edit simultaneously; one writer at a time owns DbContext, migration/snapshot, shared DI, canonical contract, CI, fixtures and root guidance. A proposed A/B label does not assign a person or authorize another owner's file edit.

Do not infer permission for public contract changes, schema migration/application, shared database writes, retention deletion, remote calls, package upgrades, push/merge/reset/clean or old-doc retirement from a task map. Obtain the scoped owner decision where needed. Never edit applied migrations to clean history. For schema/data work, plan row audit, additive compatibility, backfill, isolated migration test and recovery before execution.

Keep secrets, passwords, connection strings and personal data out of reports and fixtures. Inspect any script that may mutate source, database or external resources before running it. A missing SQL environment is `NOT RUN`, with the dependent gate still open.
