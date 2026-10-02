# Persistence, integrations and verification (RF-04 draft)

**CURRENT_VERIFIED source pointers:** `RoadGuardSystem.Repositories/RoadGuardDbContext.cs`, `Implementations/Surveys/SurveyPlanningPersistenceService.cs`, `SurveyV2PersistenceService.cs` plus dirty `.Dataset.cs`, `Implementations/Files/UploadPersistenceService.cs`, `Implementations/Processing/ProcessingV2PersistenceService.cs`, `Implementations/Messaging/NotificationOutboxConsumer.cs`, and [RF-01 code map](../../planning/refactor/01-code-map.md). This page is not an active runbook.

## Durable transaction boundary

- **CURRENT_VERIFIED:** both old and V2 survey flows touch `SurveyPlan`/`SurveyRequest` data; idempotency key placement and 409/412/428 differ (CG04/L16). File metadata uses an `int` size and upload admission rejects over `int.MaxValue` (CG17). A processing callback checks attempt membership, but the static source review did not find a latest-attempt guard before completion (CG11). The notification consumer is registered; no production dispatcher caller was found (CG13).
- **PROPOSED:** a command's business rows, idempotency outcome, audit and outbox intent commit in one SQL transaction. External object storage and AI work through durable states, retryable dispatch and receipts; their effects cannot be included in the SQL commit. A callback must be authenticated, correlated to job/source/model/attempt and fenced against stale attempts once provider protocol is accepted. These are design checks, not claims about current implementation.
- **UNKNOWN:** deployed SQL row populations/migrations, production outbox invocation, provider retry/receipt contract, actual storage failure/recovery and retention hold authority. RF-06/09 need isolated fixtures and approved data audit before any migration or cleanup.

## Storage and offline

Source bytes, checksum, media type, project/file scope, SRT pairing and dataset snapshot must be independently traceable. The accepted pilot limits are defined only in [PR-38](../product/requirements.md); this page does not define another numeric source. The expansion plan for `StoredFile.SizeBytes` must retain old readers until data is backfilled and checked; never edit an applied migration. Unknown warranty basis stays `WAITING_RETENTION_BASIS`, and a hold bars deletion per [PR-41A](../product/requirements.md).

The [business data definitions](../product/data-and-quality.md) and [transition inventory](../product/workflows.md) are draft meaning sources for module review. The transferred [FR/BR content](../product/historical-fr-br.md) retains acceptance conditions and its prior source label; these labels do not approve an EF mapping or transaction rule. RF-09 owns compatible schema/data transitions, while each RF-10 module must resolve its own unconfirmed rule detail before implementation.

Android owns its local queue under PR-44. The backend's proposed sync receipt verifies current permission, preserves original actor in supervised handover, returns replay/conflict results and never treats access-token expiry as permission to discard local evidence. The `syncOperations` wire, key ownership and conflict policy need Android owner agreement before implementation (CG12/Q-RF02-07).

## Test and operations ladder

RF-00 passed restore/build and 170 unit tests and seven isolated spatial SQL tests. Its selected API platform group failed 19/19 during `PostmanScenarioSeedStep` startup before HTTP assertions; this is a fixture baseline issue, not 19 endpoint failures. FE contract lock checker fails `CONTRACT_LOCK_MISMATCH`; do not relock to make it pass. RF-04 is docs-only, so these were **not rerun**.

RF-06 should build an isolated API host and SQL database before contract characterization. Per changed use case, verify real status/body/headers, wrong actor/project, durable SQL/outbox and replay/concurrency. SQL spatial/migration claims require isolated SQL Server; fake AI/storage proves only local adapter behavior. Performance target PR-40 needs an agreed workload/benchmark and restore drill. No shared database, seeder or external provider is used by this draft.

Runtime configuration currently comes from `Program.cs`, `appsettings*.json`, environment and DI options; environment-specific secrets must stay outside reports. Future operations docs should document option names and validation with sanitized examples after deployment owner review. The existing Docker/CI/scripts and hard-coded old paths remain active until RF-05/RF-11 transition gates in [tooling dependency map](../../planning/refactor/00-tooling-dependencies.md).
