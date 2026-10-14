# HHWS-CI-RECOVERY-01 — Fix and Test Evidence

## Changes

- Updated `planning/refactor/04-source-crosswalk.json` for the three requirements documents changed by commit `cdeb1ca1037744d06995058af13d0582f6273c0a`; no fingerprint check was removed.
- Refreshed 61 stale source-evidence entries in `tests/RoadGuardSystem.ApiTests/Contracts/swagger-source-expectations.json` using exact current source anchors.
- Changed fixed historical weekly test boundaries to `NotificationCalendarPolicy.NextWeeklyReview(DateTimeOffset.UtcNow)`.
- Required current project membership for export snapshot admission/replay in `RoadGuardSystem.Repositories/Implementations/Exports/ExportRepository.cs`.
- Aligned three test expectations with the source-confirmed global Supervisor policy and documented why inline.

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
