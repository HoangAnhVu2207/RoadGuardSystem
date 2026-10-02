# RF-10 refactor checkpoint 03: survey same-row review package

Branch `anh`, local HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1` plus preserved dirty tree, 2026-10-01. Anh is the sole writer. The ZIP is a bounded source/evidence package, not a runnable checkout. No commit, push, merge, reset, clean, stash, branch switch, production/active-contract/schema/migration/CI edit or shared DB write occurred. Remote freshness was not checked.

## Read order

1. `current/planning/refactor/reports/RF-10-03-C01.md` gives the seven-section result. `current/planning/refactor/10-survey-coexistence-baseline.md` contains the pre-test scenario matrix and observed same-row matrix. Checklist, ledger, task and aggregate report show the current Partial parent status.
2. `current/tests/RoadGuardSystem.ApiTests/Surveys/Rf1003SameRowSurveyTests.cs` is the full new HTTP/SQL test. `context/` contains both route families through Controller, Service, persistence, entity/mapping, idempotency and guarded RF-06 host/SQL fixture, plus the earlier single-family tests and decision-gap source.
3. `baseline/` has exact pre-task dirty bytes of each edited Markdown file; `current/` has their final bytes. `evidence/task-specific-diff.patch` compares those snapshots (and empty bytes for new files) with repository-relative labels. Diff content is regenerated and checked during archive verification. Patch applicability in an isolated matching baseline is **NOT_VERIFIED**.
4. `evidence/` has raw console logs, TRX and start/end/exit sidecars for all runs. `*-final3.*` is the only run against the final test source. The initial 1/2 run failed at a test expectation about unchanged rowversion on repeat V2 postpone; later successful runs were invalidated by further test edits and remain as history. No log/TRX content was redacted; evidence is scanned for credential patterns. The ZIP SHA-256 is in the external `.zip.sha256` sibling.

Final fresh build exited 0 with 0 errors, 100 warnings. Final focused HTTP host plus Testcontainers SQL TRX has 2 discovered/executed/passed, 0 failed/not-executed; console skipped 0. Both family directions use the exact same `SurveyPlans.Id` per scenario. Old-created row -> V2 postpone commits a write but returns HTTP 422 because V2 cannot project old object-shaped requirements as band scope. V2-created row -> old postpone returns 200 and changes the same row. Exact replays add no business rows; stale V2 version returns 412; repeat V2 postpone on already-postponed row appends history without advancing rowversion. These are current local observations, not accepted semantics or real-client integration. `10-03-C01` local characterization is Done; R01 RETAIN, RF-10-03 parent and overall refactor Partial. `10-05-C01` reachable-state Done and CG11 unresolved remain unchanged.

To rerun, use the full repository, .NET SDK and Docker/Testcontainers. Build current test source before `--no-build`:

```powershell
dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v q -clp:ErrorsOnly --filter FullyQualifiedName~Rf1003SameRowSurveyTests --logger 'trx;LogFileName=rf1003-c01-final3.trx'
```

The ZIP manifest and external hash verify integrity only. Runtime retest requires the full repository/Docker. No external Web/Android/AI client, deployed row audit, concurrent race, request/task cross-family flow, Q11 dataset/coverage or full suite was verified. After independent review, choose the next local R/C slice from the ledger; no F/G or RF-06 follow-on was started here.
