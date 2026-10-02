# RF-06: Isolated API/SQL characterization baseline

- **Status/checkpoint:** DONE locally on `anh` HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1` (dirty tree); see `planning/refactor/reports/RF-06.md` and `planning/refactor/06-fixture-baseline.md`. Hosted CI and external provider smoke are NOT RUN. RF-07 has not started.
- **Goal:** make repeatable isolated API/SQL tests and characterize current wire/durable behavior before pilot or shared refactor (CG15).
- **In scope:** test-host configuration, deterministic seed fixture ownership, Testcontainers SQL, safe storage fake, focused characterization for pilot/auth/survey/upload/error. **Out of scope:** production business behavior, shared DB data, migration apply to live environment, broad test rewrites.
- **Dependencies/decisions:** RF-05 activated guidance; RF-00 fixture collision analyzed. If activation is pending, fixture design may proceed but code test-host changes wait for scoped approval. Use only test DB with unique name; normal fixtures ignore inherited shared SQL connection settings and use only fixture-owned resources.
- **Read first:** `00-baseline.md` failure stack, `CustomWebApplicationFactory`, `DbInitializer`, `PostmanScenarioSeedStep`, `Program.cs`, test fixture/SQL setup, RF-01 endpoint inventory and RF-02 CG15.
- **Likely files:** `tests/RoadGuardSystem.ApiTests/Infrastructure/**`, focused ApiTests/IntegrationTests, test-only configuration and fixture manifest; production `Program.cs` only if a separately approved narrow test seam is unavoidable.
- **Contract/data/consumer effect:** no public contract change. Test database is disposable/isolated; no writes to shared environment. Capture status/body/headers/SQL effects as baseline, including current errors that may be business-wrong.
- **Steps:** inspect configured DB/seed path without exposing credentials -> create isolated host and deterministic fixtures -> reproduce/diagnose 19 startup failures -> add focused characterization -> record failing pre-existing cases separately -> ensure tests cannot route to shared DB.
- **Verify:** fresh build of affected test projects; selected API tests reach HTTP assertions; SQL Testcontainers unique DB and 7 spatial baseline tests; negative wrong-scope and durable-effect checks; zero unexpected skipped/undiscovered tests. Do not claim full suite if not run.
- **Done when:** pilot tests run repeatably in isolated environment, CG15 cause/resolution recorded, current behavior fixtures versioned, all mandatory focused gates pass or explicit Partial with exact blocker.
- **Recovery:** revert test-host/fixture edits; tear down only verified isolated container/database owned by the test run; preserve all user/source data.

## Two-developer delivery supplement

- **Proposed owner:** A: shared isolated host; B: module-specific characterization tests. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.

## 2026-09-30 local completion checkpoint

- Owner/writer: current `anh` checkout, RF-06 shared test fixture and characterization slice. START: RF-05 active guide and baseline source verified. INTEGRATE: focused real SQL/HTTP evidence complete locally. RELEASE: hosted CI and any branch publication remain outside this task.
- Pre-existing dirty paths (RF-05 activation, survey source/test and V2 planning) were preserved. RF-06 changed only API/integration test fixtures and focused tests, plus this task, `06-fixture-baseline.md` and `reports/RF-06.md`.
- CG15: Development local configuration enabled startup migration and Postman seed in the old platform factory; the observed survey collision came from an existing fixture ID with incompatible ownership. Exact original row origin is unknown; isolated survey collision reproduction passes. Platform host now disables initialization/seed and fails closed. SQL fixtures use Testcontainers with generated DB and ownership checks, ignoring inherited shared SQL settings.
- Current-source build and focused results: API 66/66, spatial 7/7 twice, SQL diagnostics 8/8, Postman scenario seed 4/4, final combined SQL 19/19 after the last fixture guard. The 19 prior startup failures are included in API 66 and pass HTTP assertions. No unexpected skip; external MinIO smoke explicitly excluded. Repeat runs used new processes and fresh containers/DBs.
- Fingerprint: local HEAD above; SDK `10.0.401`, `net8.0`, Docker `29.6.1`, SQL image `2019-CU18-ubuntu-20.04`; inherited SQL variable present but not consumed. Connection values/passwords are not recorded. No production code, public contract, schema or migration changed. No commit/push/merge. Next task may review this baseline before RF-07; no automatic start.
