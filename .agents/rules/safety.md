# Side effects and boundaries

Preserve pre-existing dirty files. Work in separate checkout/worktree when two people edit simultaneously. Anh coordinates DbContext, migration/snapshot, shared DI, canonical contract and integrated Postman. Reserve a named writer and integration order for shared files (also CI, fixtures and root guidance); never allow two simultaneous writers. Record reservations in the spec/PR discussion. Package proposals do not authorize route/schema changes or another writer's files.

Do not infer permission for public contract changes, schema migration/application, shared database writes, retention deletion, remote calls, package upgrades, push/merge/reset/clean or old-doc retirement from a task map. Obtain the scoped owner decision where needed. Never edit applied migrations to clean history. For schema/data work, plan row audit, additive compatibility, backfill, isolated migration test and recovery before execution.

Keep secrets, passwords, connection strings and personal data out of reports and fixtures. Inspect any script that may mutate source, database or external resources before running it. A missing SQL environment is `NOT RUN`, with the dependent gate still open.
