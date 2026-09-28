# Select only changed persistence risks

| Mechanism | Cases / durable assertions |
|---|---|
| Idempotency | Same key/payload sequential and concurrent; changed payload; another actor; commit then lost response; one business effect and stable replay |
| Rowversion | Two writes from same version; one winner; stale loser preserves history; agree existing body-token/409 vs proposed If-Match/412 contract before changing it |
| Transactions | Failure before commit leaves no partial rows/audit/outbox; retry after ambiguous commit does not duplicate effects |
| Outbox/worker | Duplicate delivery; lease expiry/race; process restart; receipt and effect atomic where applicable; notification is not sent inside domain SQL transaction |
| Offline sync | clientOperationId survives new batchId; partial per-operation outcomes; wrong assignment snapshot; revoked authority; ACK after durability; D05/D06/42A authority preserved while wire lifecycle/security remain gates |
| Migration | Upgrade representative prior schema/data; FK/index/default/nullability checks; retain persisted enum meanings; inspect generated SQL for loss; rollback only if safe and in approved scope |
| Spatial | Correct SRID, axis order and units; invalid geometry; query translation; immutable route versions; SQL Server test rather than in-memory geometry alone |
| File | Immutable content/checksum; wrong-owner attachment; incomplete upload; uncertain completion retry; no verified status from client assertion alone |
| Retention | 41A policy basis; hold added after approval; executor rechecks hold; bounded approved manifest; replay; unknown warranty end stays WAITING_RETENTION_BASIS |
| Read projection | Empty result, stable tie-break ordering, limit bounds, cross-project isolation, no EF entity leakage |

Find existing primitives under Transactions, Idempotency, Concurrency, Messaging, Storage and Configurations with `rg --files`. Folder names are search hints; inspect actual responsibility before adding a file. Do not invent TTLs or notification semantics to fill a missing decision.
