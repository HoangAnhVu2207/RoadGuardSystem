# C02 — Fix and regression evidence

Source baseline `07222494417969070e8c5d56dcc78f243f795afb`; candidate is the commit containing this report. Branch `huy-review`. Execution date 2026-10-15. No historical C01 totals are presented as C02 verification.

## Scope and deterministic regression

Six production files change only notification authority read lock modes, defaulting every other consumer to its original U lock. `tests/CI/sql-lanes.json` adds one class/five exact identities to lane 4, preserving all existing identities. No workflows, test exclusions, runtime API, schema/migration, contract, source fingerprint or business status changes.

New class: `tests/RoadGuardSystem.IntegrationTests/Notifications/H6AuthorityConcurrencySqlTests.cs`.

1. `OriginalWindowEligibilityAndRepairDispatchCompleteWithOppositeResourceOrder`: genuine FIELD/repair source and original-window fixture; actual repair notification worker holds responsibility, actual eligibility holds actor authority; two coordinated SQL connections release both gates. Require eligibility 200/empty missing reasons, dispatch COMMITTED, one durable delivery, completion receipt and replay.
2. `ClockExactReplayAndDeadlineDispatchCompleteWithoutRepeatingExtension`: real NORMAL proposal and appointed Supervisor create a genuine deadline breach; appointment recipient sorts first so its User differs from replay actor while both share Supervisor Role. Worker holds clock, replay holds Role, both gates release. Exact original key/body/version must return 200 with one extension, unchanged original due, one extension outbox event and one command receipt. Notification policy legitimately delivers once to each of two distinct Supervisors, with one completion receipt. This exercises the shared ClockDuty boundary; unchanged H3 FIELD HTTP tests independently verify the endpoint.
3. `AuthorityReadLocksBlockRevocationUntilDeliveryCommits` (user/role/membership): real FIELD notification paused before SaveChanges; another open connection runs the revocation under `LOCK_TIMEOUT 1000`. Require SQL 1222 while source proof/recipient authority locks are retained; identical write affects one row after commit, rolled back to preserve fixture state.

Gates wait at most 15/20 seconds; whole concurrent operations use 40/45-second cancellation. No sleeps and no retry-until-green. All new contexts use `IdentitySqlServerFixture.CreateDbContext` with `enableTestRetries: false`; the test-only retrying execution strategy is not selected. Two-session tests use separate scoped DbContexts against isolated fixture databases. H6 durable lease fence/attempt/expiry and receipt completion run through actual repository transactions.

## Before/after

`before/before-proof.trx` is the accepted before evidence: unchanged baseline production + final new tests = 2 SQL 1205 FAIL, 3 security PASS. Graphs prove the exact Users/responsibility and Roles/clock cycles. After restoring all six production edits and rebuilding, three predetermined rounds each execute all five tests.

Earlier harness-development runs (`before.trx`, `before-valid.trx`, `before-final.trx`, `before-clock-final.trx`) include incorrect test assumptions or gate ordering/disposal errors. They are excluded from the accepted red/green verdict; the final fixed recipient order is verified before and after. Earlier after-fix rounds are supplemental only. No failing harness result is relabeled as a production fix.

## Environment and commands

Windows, .NET SDK 10.0.401, net8.0 tests, SQL Server `2019-CU18-ubuntu-20.04`. Task-owned container `hhws-p5-ci-c02-sql`, loopback port 15439, per-fixture temporary databases. Existing staging SQL/MinIO were not used or changed. Credentials stay in private temporary files and are not committed. API Testcontainers use a separate disposable SQL instance; local API execution sets `xUnit.MaxParallelThreads=1` due to Docker memory limits and executes every selected test (no skip/filter on the full API lane).

Evidence root: `C:/Users/HoangAnhVu/AppData/Local/Temp/hhws-p5-ci-c02/`.

```powershell
dotnet build --no-restore --nologo -v q
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter FullyQualifiedName~H6AuthorityConcurrencySqlTests --logger 'trx;LogFileName=final-proof-N.trx' --results-directory <evidence>/focused --nologo -v q
# Exactly N=1,2,3, no retry-until-green.
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --filter 'FullyQualifiedName~ND01OriginalWindowHttpTests|FullyQualifiedName~H3FieldInspectionWorkflowHttpTests' --logger 'trx;LogFileName=http-focused-N.trx' --results-directory <evidence>/focused --nologo -v q -- xUnit.MaxParallelThreads=1
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter 'FullyQualifiedName~Clock|FullyQualifiedName~Idempotency|FullyQualifiedName~ND01|FullyQualifiedName~H3Field|FullyQualifiedName~H6NotificationClaim|FullyQualifiedName~H6NotificationRetry|FullyQualifiedName~Receipt' --logger 'trx;LogFileName=sql-focused.trx' --results-directory <evidence>/focused --nologo -v q
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --logger 'trx;LogFileName=unit.trx' --results-directory <evidence>/verification --nologo -v q
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --logger 'trx;LogFileName=api.trx' --results-directory <evidence>/verification --nologo -v q -- xUnit.MaxParallelThreads=1
pwsh -NoProfile -File tests/CI/Invoke-SqlLane.ps1 -Lane N -ResultsPath <evidence>/verification/sql
# N=1..4; schema inventory output redirected to task-owned temp path.
pwsh -NoProfile -File tests/CI/Verify-SqlLanes.ps1 -ResultsPath <evidence>/verification/sql
dotnet format --verify-no-changes --no-restore
git diff --check
```

## Executed totals

| TRX | Passed | Failed | Not executed | Total | Exit |
|---|---:|---:|---:|---:|---:|
| `before/before-proof.trx` | 3 | 2 | 0 | 5 | 1 |
| `focused/final-proof-1.trx` | 5 | 0 | 0 | 5 | 0 |
| `focused/final-proof-2.trx` | 5 | 0 | 0 | 5 | 0 |
| `focused/final-proof-3.trx` | 5 | 0 | 0 | 5 | 0 |
| `focused/http-focused-1.trx` | 4 | 0 | 0 | 4 | 0 |
| `focused/http-focused-2.trx` | 4 | 0 | 0 | 4 | 0 |
| `focused/http-focused-3.trx` | 4 | 0 | 0 | 4 | 0 |
| `focused/sql-focused.trx` | 166 | 0 | 0 | 166 | 0 |
| `verification/unit.trx` | 966 | 0 | 0 | 966 | 0 |
| `verification/api.trx` | 481 | 0 | 1 | 482 | 0 |
| `verification/sql/integration-lane-1.trx` | 130 | 0 | 0 | 130 | 0 |
| `verification/sql/integration-lane-2.trx` | 135 | 0 | 0 | 135 | 0 |
| `verification/sql/integration-lane-3.trx` | 215 | 0 | 0 | 215 | 0 |
| `verification/sql/integration-lane-4.trx` | 197 | 0 | 0 | 197 | 0 |

## Source/contract checks

All eleven commands below executed with exit 0 (individual `check-0.log` … `check-10.log`):

- `python planning/refactor/rf05/checks/test_readiness.py` (11 tests).
- `python planning/refactor/rf05/checks/test_guidance.py` (7 tests).
- `python planning/refactor/rf05/checks/test_repository_readiness.py` (4 tests).
- `python planning/refactor/rf05/checks/verify_repository_readiness.py --root .`.
- `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1`.
- `pwsh -NoProfile -File tests/Tooling/Verify-AgentSetup.ps1`.
- `pwsh -NoProfile -File tests/Operations/Verify-DockerCompose.ps1`.
- `pwsh -NoProfile -File tests/CI/Verify-CiWorkflow.ps1`.
- `pwsh -NoProfile -File tests/CI/Verify-TestResults.Tests.ps1`.
- `pwsh -NoProfile -File tests/CI/Verify-SqlLaneDiscovery.ps1` (103 compiled classes, exactly once).
- `pwsh -NoProfile -File tests/Security/Verify-DependencySecurity.ps1`.

Fresh format/discovery checks also run after final test/manifest cleanup. No stale fingerprint or Swagger artifact was updated. Full API contract assertions remain part of the full API suite.

## Authorization and idempotency evidence

Default helper callers and current-authority policy predicates remain unchanged; see complete [consumer audit](01_ROOT_CAUSE_AND_LOCK_ORDER.md). Revocation blocking is asserted for each authority dimension; replay checks one command receipt and one extension/outbox effect. Existing ND01/H3 HTTP tests keep wrong actor/project/current authority, signed offline origin, ETag/version and replay assertions. Full SQL lanes include receipt mismatch, revoked devices, NORMAL/FT approval, notification source/recipient and H6 concurrent claim cases; their existing assertions were not relaxed.

## Self-review

Pass 1: audited source-first versus authority-first paths, read-only authority consumers, retained locks, role/project/assignment checks, atomic fence/receipt/delivery and replay boundaries. Corrected new test's appointment recipient ordering so the original source deterministically reproduces the exact Roles/clock graph; no production authorization change.

Pass 2: reviewed all changed paths and manifest identities; preserved preexisting untracked audit/frontend files; removed accidental manifest encoding/reordering churn before verification. Only six source files, one new test class, six manifest lines and four C02 reports are in scope. No migration, API, worker scheduling, retry or contract changes. Independent external JEV: PENDING.

## Evidence SHA-256

Private local evidence is retained for reproducibility; logs/credentials are not added to Git. Hashes pin the executed artifacts, not a claim of deployment verification.

| Artifact | SHA-256 |
|---|---|
| `api.log` | `0af28c81a6fec897c87d88fd78bf647138f1bbab4c5ef56e2e5eb9a3f80df68d` |
| `before/before-clock-final.trx` | `9f4fe7b6481cc582f4ebbad3f369ee3799ee5fae7f9701fdfdc0542e2912cc39` |
| `before/before-final.trx` | `0f64f0e6b176bfa0666a8c3de83091575c057111ddb9f68796ccb9875f6a4278` |
| `before/before-proof.trx` | `885ee8c86a9e55dcc4593616e49e4a2954b4ec6599a5fc45d3f7cc52bc5f7b89` |
| `before/before-valid.trx` | `357d12f8fdb2e6687a2c6726a03bfae34d117037d12bd9955be0bcb15be13fc9` |
| `before/before.trx` | `cda44e7daf5047b2b364dd3ae20bfa467dc8dd527355413987802166aa7627cb` |
| `before-clock-final.log` | `65ee65f6ab127af7bb42b7b1f6fd55c8b60af83090c8426ece4dee1a588e581f` |
| `before-final.log` | `396ade5743c0e78b0295c7e2f0a34867326275d669ade29d79fe366d871ecd10` |
| `before-proof-deadlocks.xml.txt` | `c8da2bedb8e19439945fb040f0ef08d1da2f637c3c93526f3ed54ef7c0b99603` |
| `before-proof.log` | `5a88e7ea00c91063010478f5ab2f9fb68053b0a5c36de755272918b3990adc45` |
| `before-valid.log` | `7a7f4e4a0e81040e1acee4e720ea81701f4b4ba66bb5f7156211f13e679e92e1` |
| `before.log` | `b5bb033db6ccc0fb5a8797eafc8bb3dbcd64168dbc29ab8f713afc6ee2043436` |
| `build-after.log` | `b3775c7a154f27f0624b23008e443e277cc8cbbe02c97f19983da9eff2165ef4` |
| `build-baseline-final-tests.log` | `aad75c4b9a1845f21af6a54e8166869a23adce3698accc432118ab9ec224a339` |
| `build-before-clock.log` | `f8762da02ed51d51e25f79fd1700fb6213f0233561106ede1124cbf8cf1c6d61` |
| `build-before-proof.log` | `13319577262f4a2f07e19a140ad736f20220e2fd3c9e3044f7fddb060851963d` |
| `build-final.log` | `0f0a1e067d141c96b3030cac06ffb9db046f75baed5efccb36af4b6e24e2ace4` |
| `build-full.log` | `e42443a79f2d7cecf913ef684cdf5774322507b9c3c98b7e91e594808f66169d` |
| `build-restored-proof.log` | `6aeeaf0c853525b04f13585ad40120f2a160d103dc42f6c25da6ccee1de697c0` |
| `build-restored.log` | `7540acbc4d04ea6f223894def630a5a94f75de22bef20f21f8dac6eec4360ff0` |
| `build-tests-before.log` | `4bb76c45eefce8a34910beaec0c49ee3336e2dc6bae1780eac15a26915e0410e` |
| `check-0.log` | `e83ca042401880a114de7075bcbfed388df35f78a1b5d70959f724004f44cb03` |
| `check-1.log` | `14736da4d617e19f974018f9caf3e5292336a62a82482b090f06d874ca3c65ce` |
| `check-10.log` | `9d948c75afa7ebe814c1589f8c2ce047139dfc3fd19350bfc0dc404f0206e8c7` |
| `check-2.log` | `4a203f958ef9d765ff1f89816631e1bdf553b4acf1d56449f1d10ad92bb46ff3` |
| `check-3.log` | `477fff7b101b8cb961b37716c60a40381d2a6defb07096c116cc1738db85e3bb` |
| `check-4.log` | `c14dee5f5dba5bb44247bc76b33cf80c84678552e31a13da3b0b42bbc34d42cb` |
| `check-5.log` | `0c566504e6ae9344d06a969061336cc8423781e1755031365a264c19e24f85a6` |
| `check-6.log` | `af88bdb46d2e5a134df8c021bdae28ed8fbd70ee6f89d84d7ffce8156c6933b7` |
| `check-7.log` | `c71b9f31458b8b6fee5701ddd2a6e3c29398abd0815222aa96b8e7f807df5eef` |
| `check-8.log` | `6cba4b6aecb7a73fead46c8f00764517af1a5d764162976bb851a51a6746cf79` |
| `check-9.log` | `80f05320681e5710cd6716da5ac55a98883174be982688199403e75ee5611f3d` |
| `discovery-final.log` | `80f05320681e5710cd6716da5ac55a98883174be982688199403e75ee5611f3d` |
| `final-proof-1.log` | `0f79d388767f27471990c48e83ffdf6758aad2ef2438e438d287d4c26df37340` |
| `final-proof-2.log` | `cc7ca1416d76b2bfa3eb4b624287a833bd8fbd25548e0602f18dc79f72e3f1f6` |
| `final-proof-3.log` | `5b985bd73f69ea16d7f2c50fa206b72344070b68d5cdec6fd576f014483d23a3` |
| `focused/final-proof-1.trx` | `62e01ca0562deb48d2ec0d4cb76b70ff1ba1d79316a00e5deabac653ac9bc21f` |
| `focused/final-proof-2.trx` | `d4b1322b9686fc77031539c9967f7142b55449dc6591949060f50add795e6846` |
| `focused/final-proof-3.trx` | `49369f1cc402bdd96ae7e9cf4f6179baf415b09f79c3d3b4aaeaf0e001260fd9` |
| `focused/http-focused-1.trx` | `165b97af766ba4fcac8628c10e02d873498b21caa63c2b22ea25f0abd9d4c0b6` |
| `focused/http-focused-2.trx` | `be16edb6ef0408fedd90832a474d509b3e43cd67dd613a4d63f468fec28de84d` |
| `focused/http-focused-3.trx` | `3d80a4a05f3ed176ad822e36ec09ebe7a33bbdba36576deb6b5862077d4b18cd` |
| `focused/sql-focused.trx` | `43174b249151e900c861066f64b1e49db3e10bb8af50885432f6011cb1a1db78` |
| `format-final.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `http-focused-1.log` | `fe712655128c382aae786ed29a6386be0e0fe1e3c4dd16b1a07ff5ae26075bc1` |
| `http-focused-2.log` | `4854343c86772b72aee04bdb0c6e18d204c11318f443f6915f7d46721995c365` |
| `http-focused-3.log` | `0a908dd186e36ac1784969b47982013501f1554446b5eb2ff01d6ace965b4fe3` |
| `sql-aggregate.log` | `77ff5c02e7ceb3050d3e5bd286d07350d97c9d2637592a51030b08c0dff1891f` |
| `sql-focused.log` | `383caebd4d34d953c98a0478f4c9e268411cb3e3bc058d316d02958653f96032` |
| `sql-lane-1.log` | `de0ea67e62a2b5619a45bde1c91630735cc9b2c8831f89b08e4d6ec50f5e7a35` |
| `sql-lane-2.log` | `4d357da2804b7c3a11a22ba646f016b1e862622959e4f77968992dcc22ab75db` |
| `sql-lane-3.log` | `40efc7d4c599a28e5da94cb292a2360c8a8708f95ae5c7675c558b1402daa2d8` |
| `sql-lane-4.log` | `ba140924b5ed670bc700c8fb17d9ba74fac757066d21a313052d4bf401f2cde3` |
| `unit.log` | `4b98915fe0a8c0c45e1a0ce736678fe6d441b5da6161ef66938c30577e42899b` |
| `verification/api.trx` | `c07a554e50952d848ff82794d9bc9c1ebc013465f1ef3131f89649bdf1c96282` |
| `verification/sql/integration-lane-1.trx` | `8c1e6b798eb0bc4c98c67cac718cb516d97143bdd4772404f3393273e05a61ad` |
| `verification/sql/integration-lane-2.trx` | `803ed5cd8c2847a8cb579bdac6fdb99c6f6fdff7123e33036fd30370e0180c15` |
| `verification/sql/integration-lane-3.trx` | `dce362dd873a22b5e3579095b7d66bea5e6f409ca673079c829872a40929b6d4` |
| `verification/sql/integration-lane-4.trx` | `2e5e6e4fc460ae85dded054e4ce439732ad5f34593f436d6ff1c7ce7a3c4debb` |
| `verification/unit.trx` | `ca52788c9d31113160e045609b843a2db7937a3022e700a15829897c1d684624` |

Exact-SHA GitHub CI is recorded in [closure recommendation](04_PHASE5_CLOSURE_RECOMMENDATION.md); local PASS is not a substitute for CI. External/native/deployment E2E: NOT_RUN.

## Final local reconciliation

Build `build-restored-proof.log`: exit 0, 0 errors, 1168 analyzer warnings. Unit 966/966 PASS; API 481 PASS/1 optional SKIP (482 total); SQL lanes 130 + 135 + 215 + 197 = 677 PASS, 677 unique executed identities, aggregate exit 0. Source/contract commands, fresh format, discovery and `git diff --check`: exit 0. Report link check: 0 broken links; 28 explicit source references verified (paths exist and referenced lines in bounds). Read-only opt-ins restricted to four intended notification files. All existing manifest identities remain, with exactly five new tests.

The only API skip is `RoadGuardSystem.ApiTests.Files.UploadApiTests.UploadEndpoints_CompleteMultipartUploadAgainstConfiguredMinio`; real MinIO smoke NOT_RUN. Android/Expo/Web/FastAPI/deployment E2E NOT_RUN.

## Git preservation

At entry and before delivery, `huy-review` HEAD and `origin/huy-review` equaled baseline (ahead/behind 0/0 after fetch). Preexisting untracked paths were preserved and excluded from staging: `docs/audit/HHWS-00-REPO-AND-DOCS-INVENTORY.md`, `HHWS-01-FULL-API-INVENTORY.md`, `HHWS-02-CONFLICT-AND-GAP-MATRIX.md`, `HHWS-03-DOCS-MIGRATION-CROSSWALK.md`, `HHWS-04-REBUILD-DECISIONS-NEEDED.md`, `RG-REPORTER-ANDROID-AUDIT-01.md`, and `docs/frontend/`. No reset, history rewrite, force push or cross-branch merge.

## Exact source-fix GitHub CI evidence

Run [38707582788](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788), attempt **1**, exact commit **`77777bb5d144cf12e3a8f264c00b2726a5511ae7`**, conclusion **SUCCESS**. `gh run watch --exit-status` returned 0. Run JSON, full job logs and downloaded TRX artifacts are retained under the evidence directory. This verifies the production/test fix commit; the later evidence-only commit does not change source, tests, manifests or workflow.

| Job | Conclusion |
|---|---|
| [Verify source contracts](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788/job/116330715920) | SUCCESS |
| [SQL integration lane 2](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788/job/116330716058) | SUCCESS |
| [Unit tests](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788/job/116330716285) | SUCCESS |
| [SQL integration lane 4](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788/job/116330716309) | SUCCESS |
| [API tests](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788/job/116330716340) | SUCCESS |
| [SQL integration lane 1](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788/job/116330716368) | SUCCESS |
| [SQL integration lane 3](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788/job/116330716430) | SUCCESS |
| [SQL integration tests](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/38707582788/job/116332744511) | SUCCESS |

TRX outcomes are counted from actual `UnitTestResult` entries, including the optional MinIO `NotExecuted` result.

| Artifact / TRX | Passed | Failed | Not executed | SHA-256 |
|---|---:|---:|---:|---|
| `ci-38707582788-artifacts\api-results\api.trx` | 481 | 0 | 1 | `18f7bdbde9485e28f22c60928f00b2ff617a3a6ccdc6cfac33cb18d4fe3714df` |
| `ci-38707582788-artifacts\integration-results-lane-1\integration-lane-1.trx` | 130 | 0 | 0 | `a39037865675ee54377a9874693eaa4893cece899034e5ffa08a5f6c75befbbd` |
| `ci-38707582788-artifacts\integration-results-lane-2\integration-lane-2.trx` | 135 | 0 | 0 | `f0c161970054cb52af5f3a3e790442fda58998019cc83c77b883762ee7dd8f8c` |
| `ci-38707582788-artifacts\integration-results-lane-3\integration-lane-3.trx` | 215 | 0 | 0 | `ce430ed26701e328ebcc8752af062f0a5b03fa410d8e0637fb005955420a237a` |
| `ci-38707582788-artifacts\integration-results-lane-4\integration-lane-4.trx` | 197 | 0 | 0 | `410f540fef6c712fcf41427f91c11e24f08d588cbb617b1ae3d4fc92604f8280` |
| `ci-38707582788-artifacts\unit-results\unit.trx` | 966 | 0 | 0 | `2f449127ee902c392bbf0471a841b3017ab24b163dc852eafecdb4197f0c3f73` |

Totals: **Unit 966 PASS; API 481 PASS / 1 optional MinIO SKIP; SQL 130 + 135 + 215 + 197 = 677 PASS**, with 677 unique SQL identities. Five new deterministic cases and the existing H6 concurrent-worker claim regression are PASS in CI. Source contracts and SQL aggregate SUCCESS. No disabled jobs, changed expectations or retry-until-green.

- `ci-38707582788.json` SHA-256: `3b468e8442c953abd5a7098fdac9d5b667fcef02ddbe7f0c0593e2f1505ba7f9`.
- `ci-38707582788.log` SHA-256: `f187c6ccaf666ceeb22f190100077dd6e78a45e25068ab95945a1422e72e0121`.
