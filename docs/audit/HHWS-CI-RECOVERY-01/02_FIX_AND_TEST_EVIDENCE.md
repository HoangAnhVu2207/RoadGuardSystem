# HHWS-CI-RECOVERY-01 — Fix and Test Evidence

## Changes

- Updated `planning/refactor/04-source-crosswalk.json` for the three requirements documents changed by commit `cdeb1ca1037744d06995058af13d0582f6273c0a`; no fingerprint check was removed.
- Refreshed 61 stale source-evidence entries in `tests/RoadGuardSystem.ApiTests/Contracts/swagger-source-expectations.json` using exact current source anchors.
- Changed fixed historical weekly test boundaries to `NotificationCalendarPolicy.NextWeeklyReview(DateTimeOffset.UtcNow)`.
- Required current project membership for export snapshot admission/replay in `RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs`.
- Aligned three test expectations with the source-confirmed global Supervisor policy and documented why inline.
- Added the repository proxy method handling required by unit test doubles and pinned `System.Security.Cryptography.Pkcs` to `8.0.2` to resolve the CI advisory.
- Corrected whitespace violations and refreshed the SQL lane identity manifest for renamed/new tests.
- Changed the H4 reader fixture to assert the source's fail-closed `UnauthorizedAccessException` after membership end; this does not weaken production authorization.

## Checks executed

| Check | Result |
|---|---|
| Swagger source-evidence scan (all 237 rows / 22,934 excerpts) | PASS; 0 missing after refresh |
| `python planning/refactor/rf05/checks/verify_repository_readiness.py --root .` | PASS; 148 mapped requirements |
| `git diff --check` | PASS (line-ending warnings only) |
| `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --verbosity minimal` | PASS; 966 passed, 0 failed, 0 skipped |
| API focused tests | NOT_RUN locally; SQL/Docker unavailable |
| SQL integration lanes 1–4 | NOT_RUN locally; SQL/Docker unavailable |
| Full CI | NOT_RUN locally; remote workflow required |

## Remote CI evidence

| Run | Result |
|---|---|
| `38634499556` (`114f6fb`) | FAIL: API transient SQL deadlock; SQL identity manifest stale; individual SQL lanes passed |
| `38635768303` (`56e3281`) | FAIL: API PASS; all SQL lanes PASS; aggregate identity gate found one lane-3 renamed test |
| `38636706816` (`694ecf6`) | PARTIAL: Unit PASS, Verify source contracts PASS, SQL aggregate and lanes 1–4 PASS, API 481/482 passed with one H3 FIELD task-scoped download failure |
| `38637687674` (`79dd5e0`) | PASS: API, Unit, Verify source contracts, SQL lanes 1–4 and SQL aggregate all passed |

The latest remote workflow is PASS. The prior H3 failure remains recorded as an intermittent stability gap because no root-cause server evidence was captured.
