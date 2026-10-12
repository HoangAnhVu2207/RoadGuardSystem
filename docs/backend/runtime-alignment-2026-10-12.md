# RoadGuard runtime documentation alignment — 2026-10-12

**Status:** SOURCE-INSPECTED / CI-OBSERVED; NOT a deployment or end-to-end certification.
**Baseline:** `develop` at `b70433c2e19431b4cfd27c3e237a2e8c31ba7497`. `anh`/`huy` are 67 commits behind this baseline (comparison observed on audit date). Historical documentation is retained.

## Evidence standard

- SOURCE-INSPECTED = exact Controller/Service/repository or migration exists and was examined; it is not proof of deployment or successful execution.
- TEST-PRESENT = test source exists; not proof that a specific test passed.
- CI-OBSERVED = GitHub Actions run conclusion observed at baseline, not proof of all provider/device paths.
- NOT-VERIFIED = no adequate evidence obtained during this review; do not silently label DONE.
- PROPOSED = document contract not adopted by running API merely because YAML exists.

## A. Reporter / IncidentCase / BE–AI

| Observed code evidence | Conclusion |
| --- | --- |
| `RoadGuardSystem.API/Controllers/ReportsController.cs`: GET/list, GET/read, POST/create, POST/supplements, evidence download | Report HTTP entry points exist; list/read/supplement resolve lifecycle via DI and can return 503 when unavailable. SOURCE-INSPECTED |
| `RoadGuardSystem.Services/Implementations/Reports/ReporterReportService.cs` and `ReporterLifecycleService.cs` | Report services exist. SOURCE-INSPECTED |
| `RoadGuardSystem.API/Controllers/CasesController.cs`: list/read, triage, links, splits, conclusions, publications | Case HTTP routes exist; `ICaseWorkflowService` is resolved via DI. SOURCE-INSPECTED |
| `RoadGuardSystem.Services/Implementations/Cases/CaseWorkflowService.cs` | Case workflow service uses repository, project-scope guard, producer and idempotency coordinator. SOURCE-INSPECTED |
| `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs`: create/get/retry processing jobs, internal AI result callback, validation runs | BE processing/callback routes exist; callback guarded by `AiCallback` policy. SOURCE-INSPECTED |
| `RoadGuardSystem.Services/Implementations/Processing/ProcessingV2Service.cs` | Processing Service validates actor/scope/inputs and delegates persistence. SOURCE-INSPECTED |
| `tests/RoadGuardSystem.ApiTests/Cases/Huy01CaseSqlTests.cs`, `tests/RoadGuardSystem.ApiTests/Processing/Anh02AiHttpTests.cs` | Relevant tests present, pass status not individually verified here. TEST-PRESENT |

**Unverified:** FastAPI worker deployment, BE-to-FastAPI network dispatch/poll/ack in a live environment, inference accuracy/real video-SRT, object-store provider, result replay against live SQL. `docs/diagram/V2/AI_Integration/README.md` and its separate OpenAPI remain `PROPOSED_DELTA_NOT_ENABLED` unless separately adopted; do not infer that all proposal endpoints run.

## B. Fast Track / repair

| Observed code evidence | Conclusion |
| --- | --- |
| `RepairPoliciesController.cs` and `RepairPolicyService.cs` | Policy draft/create/update, publish, read revision, revoke routes; Service explicitly restricts to `ProjectManager`, enforces idempotency/preconditions. SOURCE-INSPECTED |
| `RepairEligibilityController.cs`, `RepairEligibilityService.cs` | Eligibility read surface and Service present. SOURCE-INSPECTED |
| `RepairExecutionController.cs`, `RepairExecutionService.cs` | Assessment, start, finish, attempt, supplement, review routes and Service present. SOURCE-INSPECTED |
| `RepairLifecycleController.cs`, `RepairLifecycleService.cs` | Cancellation and normal-successor routes present. SOURCE-INSPECTED |
| `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairPolicyHttpTests.cs`, `H4RepairExecutionHttpAdmissionTests.cs`, `tests/RoadGuardSystem.IntegrationTests/Repairs/H4RepairExecutionSqlTests.cs` | Focused HTTP and SQL test sources present. TEST-PRESENT |

**Unverified:** complete scenario acceptance across permissions, BEFORE/AFTER provenance, policy snapshots, curing, traffic reopening, rework, supervisor notification, field offline execution and actual deployed configuration. An HTTP route is not by itself proof that the whole business workflow is production-ready.

## C. Offline / Sync

| Observed code evidence | Conclusion |
| --- | --- |
| `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs` | Device register/get/revoke; snapshot create/get; sync; package export/prepare/get; grant issue/get/revoke; import; batch/origin get; start reconcile; artifact register/get. SOURCE-INSPECTED |
| `RoadGuardSystem.Services/Implementations/Offline/OfflineWorkflowService.cs` | Read/write action whitelist; checks role, project ID, Idempotency-Key and project-scope guard, delegates to repository. SOURCE-INSPECTED |
| `tests/RoadGuardSystem.ApiTests/Offline/H5OfflinePositiveHttpTests.cs`, `tests/RoadGuardSystem.IntegrationTests/Offline/H5OfflineRepairSqlTests.cs`, `tests/RoadGuardSystem.UnitTests/Offline/H5OfflineCryptoTests.cs` | HTTP/SQL/crypto test sources present. TEST-PRESENT |

**Unverified:** actual Android device reboot/reconnect, intermittent network, device key loss, conflicts under concurrent writers, end-to-end real object-store uploads, and all offline kinds with live credentials. The September FE-GAP list is historical gap discovery, not a current statement that every item is unimplemented; close each gap only with a mapping and test result.

## D. Persistence / API contract

- `RoadGuardSystem.Repositories/Migrations/20261008171739_BaselineCurrentSchema.cs` exists. `docs/backend/data/README.md` correctly scopes the consolidated baseline to **new isolated dev/test databases only**, not existing shared/team DB upgrades.
- `docs/diagram/V2/03_Data/04_Data_Model_Code_Map.md` (audit 2026-09-28) and `docs/backend/README.md` (RF-04 old checkout) are historical snapshots; neither is the October implementation catalog.
- `docs/diagram/V2/05_Technical/openapi.yaml` self-identifies `0.2.0-draft-alignment`, `x-contract-status: PROPOSED_DELTA`, `x-runtime-status: NOT_ENABLED`. Do not treat it as generated swagger from running Controllers. It requires a per-route/DTO/status/header diff before any promotion.
- At audit time, GitHub Actions `RoadGuard Continuous Integration` run `38177660594` for baseline SHA showed `completed/success`. This is a CI result, not an assertion that all scenarios or external integrations passed.

## Follow-up acceptance gates (not yet closed)

1. Generate runtime route inventory directly from `ApiExplorer`/Swagger on a clean build, compare method/path/DTO/authorization/status/headers against proposed V2 OpenAPI; report exact additions, removals and breaking mismatches. Do not replace the draft YAML without API owner approval.
2. Run focused Report/Case, Processing/AI, Repair and H5 Offline tests against isolated disposable SQL; publish commands, exact counts, outcomes, test artifacts and commit SHA. Test files existing is not evidence of green tests.
3. Perform real BE↔FastAPI / MinIO / Android offline E2E on a declared environment. Report dependencies and missing infrastructure separately.
4. Refresh current Data Dictionary/ERD from the single 2026-10-08 baseline in owned dev/test SQL and reconcile domain mappings; preserve historical inventory snapshots.
5. After tests and consumer sign-off, update current endpoint catalog and FE contract types; never silently flip `PROPOSED` to `ACTIVE`.

No implementation, migration, OpenAPI or business policy has been modified in this audit.
