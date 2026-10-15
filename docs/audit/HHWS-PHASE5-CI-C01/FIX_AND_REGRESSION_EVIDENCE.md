# HHWS-PHASE5-CI-C01 — Fix and regression evidence

Source baseline `6ba9035f3cedb10d7f4167059520f869ec123b98`; branch `huy-review`. New delivery SHA is in git history/final response (a file cannot contain its own commit hash).

Initial tracked working tree was clean at baseline; `git fetch origin huy-review` and `git rev-list --left-right --count HEAD...origin/huy-review` returned `0 0`. Pre-existing untracked inventory/audit files and `docs/frontend/` are preserved and excluded from staging. Delivery stages only the five test/manifest files and three C01 reports.

## Changes

1. H6 concurrent claim test retains simultaneous workers, but distinguishes an idle READPAST poll from lease ownership. At most one additional poll per null result after both transactions finish; no loop or production retry. Adds persisted ID/fence/owner/expiry/attempt checks.
2. New `LockedQueueReturnsIdleThenRecoversWithoutDuplicateLease` holds an update lock in a separate transaction, observes idle, rolls back, claims once and proves no duplicate unexpired lease. SQL lane 4 manifest includes this exact identity; expected union increases from 671 to 672, no previous identity removed.
3. API fixture can query the last five retained system_health deadlock events from its owned SQL container. ND01 preparation failure prints graphs; H3 replay failure includes HTTP body/correlation ID, server logs and graphs. Diagnostics failures preserve the original exception/assertion.
4. Three C01 audit documents. No production, migration, OpenAPI, workflow configuration or canonical Phase 5 document changes.

## Environment and commands

.NET SDK 10.0.401; SQL Server 2019 CU18 Docker. Focused tests use fixture-owned disposable SQL containers. Full SQL lanes use a task-only SQL server bound to loopback port 15439 with isolated per-test databases; fixture ownership checks retained. Existing staging SQL/MinIO are not test targets. Ephemeral task password is outside git and is not reproduced here. Local SQL environment variable `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` points to this task server.

All commands run from repository root. Raw results retained under `%TEMP%/hhws-p5-ci-c01`. `--no-build` commands used freshly built test assemblies. Full API/SQL jobs remain enabled in unchanged CI.

```powershell
# Before changes: three rounds each, bounded
# API filter selects ND01 class and all H3 HTTP cases (4 tests per round).
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --filter "FullyQualifiedName~ND01OriginalWindowHttpTests|FullyQualifiedName~H3FieldInspectionWorkflowHttpTests" --logger "trx;LogFileName=api-before-N.trx" --results-directory <evidence>/focused --nologo -v q
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter FullyQualifiedName~H6NotificationClaimSqlTests --logger "trx;LogFileName=h6-before-N.trx" --results-directory <evidence>/focused --nologo -v q
# After changes: three rounds each; corresponding api-final-focused-N / h6-after-N TRX.
# Build API diagnostics before API final-focused/full runs:
dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore --nologo -v q
# Unit suite:
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --logger "trx;LogFileName=unit.trx" --results-directory <evidence>/verification --nologo -v q
# Full API diagnostic run with default runner parallelism:
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --logger "trx;LogFileName=api-diagnostic-full.trx" --results-directory <evidence>/diagnostic --nologo -v q
# Full API final run, all tests, bounded local SQL-container concurrency:
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --logger "trx;LogFileName=api-final-full.trx" --results-directory <evidence>/verification --nologo -v q -- xUnit.MaxParallelThreads=1
# Full SQL lanes, each invoked in a child pwsh so a failure does not suppress later lanes:
pwsh -NoProfile -File tests/CI/Invoke-SqlLane.ps1 -Lane N -ResultsPath <evidence>/verification/sql
pwsh -NoProfile -File tests/CI/Verify-SqlLanes.ps1 -ResultsPath <evidence>/verification/sql
python planning/refactor/rf05/checks/verify_repository_readiness.py --root .
python planning/refactor/rf05/checks/test_readiness.py
python planning/refactor/rf05/checks/test_guidance.py
python planning/refactor/rf05/checks/test_repository_readiness.py
pwsh -NoProfile -File tests/CI/Verify-CiWorkflow.ps1
pwsh -NoProfile -File tests/CI/Verify-SqlLaneDiscovery.ps1
dotnet format --verify-no-changes --no-restore
git diff --check
```

## Results

Final local result inventory is recorded below after execution; interrupted pre-resume SQL attempt produced no TRX and is not counted as a completed run. These local checks do not establish real Expo/FastAPI/storage E2E or production deployment readiness.

## Self-review

Pass 1: source/contract review checked READPAST idle semantics against claim SQL, bounded recovery, exact seeded IDs, persisted ownership and untouched receipt/auth expectations. Removed an unverified assertion about the historical query plan from the test comment; the report marks scan/sort explanation as unverified.

Pass 2: scope/security review checked owned-container diagnostics, preservation of original failures, no added production retry, no changed HTTP expected codes, all original SQL identities retained, and exclusion of pre-existing untracked documents. No external independent review performed; next checkpoint is independent JEV.

## Bounded focused execution ledger

Each TRX is a distinct execution; three before/three after per group. No retries-until-green. API covers four cases, H6 five baseline/six after addition.

| TRX | Total | Passed | Failed | Exit |
|---|---:|---:|---:|---:|
| `api-before-1.trx` | 4 | 4 | 0 | 0 |
| `api-before-2.trx` | 4 | 4 | 0 | 0 |
| `api-before-3.trx` | 4 | 4 | 0 | 0 |
| `api-final-focused-1.trx` | 4 | 4 | 0 | 0 |
| `api-final-focused-2.trx` | 4 | 4 | 0 | 0 |
| `api-final-focused-3.trx` | 4 | 4 | 0 | 0 |
| `h6-after-1.trx` | 6 | 6 | 0 | 0 |
| `h6-after-2.trx` | 6 | 6 | 0 | 0 |
| `h6-after-3.trx` | 6 | 6 | 0 | 0 |
| `h6-before-1.trx` | 5 | 5 | 0 | 0 |
| `h6-before-2.trx` | 5 | 5 | 0 | 0 |
| `h6-before-3.trx` | 5 | 5 | 0 | 0 |

The lane 3 schema-inventory test writes its default output to tracked `docs/backend/data/current-schema.inventory.json` (Rf06aSchemaInventoryTests lines 300 and 409). The generated output was retained privately as `current-schema.generated.json`; the previously clean tracked file was restored byte-for-byte from baseline. This generated artifact is excluded from delivery; no canonical schema evidence was refreshed merely to pass tests.

## Completed full local verification

| Execution | Total | Passed | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|---|
| `unit.trx` | 966 | 966 | 0 | 0 | 0 |
| `api-final-full.trx` | 482 | 481 | 0 | 1 | 0 |
| `integration-lane-1.trx` | 130 | 130 | 0 | 0 | 0 |
| `integration-lane-2.trx` | 135 | 135 | 0 | 0 | 0 |
| `integration-lane-3.trx` | 215 | 215 | 0 | 0 | 0 |
| `integration-lane-4.trx` | 192 | 192 | 0 | 0 | 0 |
| `api-diagnostic-full.trx` (earlier diagnostic) | 482 | 424 | 57 | 1 | NOT_CAPTURED; TRX Failed |

SQL aggregate: exit 0, 672 unique executed identities. Source-readiness: exit 0, 148 mapped historical requirements, zero registry decisions. Source validator unit suites: 11 + 7 + 4 tests, all PASS, exit 0 each. Verify-P102Docs, Verify-AgentSetup, Verify-DockerCompose, Verify-CiWorkflow, Verify-SqlLaneDiscovery, Verify-TestResults.Tests, Verify-DependencySecurity: exit 0 each. Final `dotnet format --verify-no-changes --no-restore`: exit 0. `git diff --check`: exit 0. C01 local link check: 17 links, zero broken. Credential-pattern scan of the three reports: PASS; manually reviewed changed code/report payloads, no secrets copied.

Full GitHub CI at the delivery commit is triggered by push and monitored separately; its exact SHA/run/job results are supplied in the final response. **No CI PASS predicted here.** Backend production/deployment, real external services, Web/Expo/FastAPI E2E: NOT_RUN. A passing local/full CI execution does not close the reproduced C01-01/C01-02 source deadlocks.

## Private evidence SHA-256 inventory

Paths relative to `%TEMP%/hhws-p5-ci-c01`; raw artifacts stay outside git.

| Evidence file | SHA-256 |
|---|---|
| `38691928986-full.log` | `63ff7c1d55029d4edb96375668df09d175c6fa772ba2c5dd2c500ad446f46c6f` |
| `38691928986-failed.log` | `38786942750ad819350cbfa806385c106227268f859620e43112b94f8c30fcfd` |
| `38691928986-jobs.json` | `515895d9a0e92955a88e862bbafe8c152c1e37ab328a549364bef870bae288c1` |
| `38694140711-full.log` | `2d26f9a1678e821662a7b8444c8a7e50e755e756bc782dff1999d9ef8002180e` |
| `38694140711-failed.log` | `d848c886dd2064755d93e43a781662528e9973b4b396ab6bb000bff64c554dd1` |
| `38694140711-jobs.json` | `cdf3166aceaf84c560a083bf3383ede41ece44a65f99ae55992be59334a16e97` |
| `local-system-health.txt` | `60599fa19dac44469251d1e0cfda5d69e902649c7898bb465c887337378b74f0` |
| `diagnostic/api-diagnostic-full.trx` | `0d0bcf4f6836eda005c11efb53097cf49aa85feddd192dab1e7dcc5e52714087` |
| `verification/unit.trx` | `a71d47fa454df3a7149755eec672e07d1c1849a9943043815bb0b8c7eb6e46b1` |
| `verification/api-final-full.trx` | `4f1fbabc4aa922d7704101532c0ddb1715d5e0605f1abf7fd77a34ee542158b7` |
| `verification/sql/integration-lane-1.trx` | `8bd7f3267c06085485cbb2878c0732075c3e7b131c7219bd67e3de0ca39ba3ba` |
| `verification/sql/integration-lane-2.trx` | `17d3c20975bfd612736194b5a4cb9706166dccc8e89e6a76a85293271c507870` |
| `verification/sql/integration-lane-3.trx` | `0a80c3c5f2696c801fb1884aec6f7f20d63d7074b1bb1447e8626dfa0c564fb9` |
| `verification/sql/integration-lane-4.trx` | `c642b0bc9158f25b564a759201ce314f96ca1a9d315c2997fc6d93da9aa1b1ab` |
| `focused/api-before-1.trx` | `7e7bf0c2826d690cfc3c078570c42a022e87c33bfc0cc083a3ddb4baaf01f761` |
| `focused/api-before-2.trx` | `d4b5b6b5abc54cf795d9ae3dc3c222d01901d7284023f9567cda9f1bb8c895d0` |
| `focused/api-before-3.trx` | `b9dedfe6cbe7c27ce6e091d46d6a559aa8fa1b34dbe91add0579b64bef2a9d6c` |
| `focused/api-final-focused-1.trx` | `fdc304d252d5bfe77b2f03b2274a94626c30fe445c28a54bc0a587ab18c4266a` |
| `focused/api-final-focused-2.trx` | `00cab3b9a232f9c2a12422bd3f80cfd402af2bd528174a4fe5ddbc330f10d238` |
| `focused/api-final-focused-3.trx` | `3f5858096a4cbfa70890c7a33b162e41cb99acb7d38bed348e269034cfe7de5e` |
| `focused/h6-after-1.trx` | `dfd821f46961a1475ad02b8ea0c332fdf27035e51820dbd9ea113d86dcf70240` |
| `focused/h6-after-2.trx` | `e982867bfdc3b6e6bd730e7af87d2e713ce03c4df60ffb022ee29d1e787770f3` |
| `focused/h6-after-3.trx` | `84a844dd5b4ba5d6bfbcc4b45c9ade1ab41203a5049cba7eb9e7c9243f3f15cc` |
| `focused/h6-before-1.trx` | `89e540dfc39df3b971c7c384b71a57a97cfd26888cb48bbe0a35a61e8a00f915` |
| `focused/h6-before-2.trx` | `3962ef7846009abb505bfecfb41bc6d3ff8592b4c281b05904f017d1325cfddb` |
| `focused/h6-before-3.trx` | `ea913a8922d99d71404090669b3b7505aea4aa7374770aad9fcc4deced009494` |
| `38691928986/api.trx` | `712f3f7aa21daa4e0d6bfad9ce554e58b0766d77a71a14a8b23ba7f252689207` |
| `38694140711/api-results/api.trx` | `2f3ee10200c04c17d016a9f78efcfbb3546f518649b4fc22fb5d6d81d659a29c` |
| `38694140711/integration-results-lane-1/integration-lane-1.trx` | `70c03041cf94d4f069468b05645f83744809cee3b4ae7c61804380e8d92f3e48` |
| `38694140711/integration-results-lane-2/integration-lane-2.trx` | `a2f2d21d43fd086e9aac5477e8f44b4b40b8f3af9969cc55245556e8069c3321` |
| `38694140711/integration-results-lane-3/integration-lane-3.trx` | `ad382891e712b428d390b5c174aafba0a5684c066d4a3e92ce1e587fe12ee183` |
| `38694140711/integration-results-lane-4/integration-lane-4.trx` | `4fef21398aef8543045c9305ac235e3b9ba0dc0b0ada7063e3f028acf1352ee9` |
| `38694140711/unit-results/unit.trx` | `e79092bfbdce85a394398fac083e2749011129eeffdcfe5d3c19733a8b47a77a` |
