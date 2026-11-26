# RoadGuard real-user demo and CI lanes implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Lấy bản BE mới nhất từ `develop`, tối ưu CI theo 12 lane API và 10 lane SQL, tạo một dự án demo bổ sung bằng clip MP4/SRT cũ, kiểm thử các vai trò như người dùng thật, deploy BE–AI có backup/rollback, rồi bàn giao endpoint và dữ liệu để Web và Mobile sử dụng.

**Architecture:** CI chạy các lane độc lập bằng matrix, mỗi lane có fixture SQL/MinIO riêng và job tổng hợp đối soát đủ test identity. Dữ liệu demo được tạo additive qua seed/upload flow đã có, gắn nhãn `DEMO`, và kiểm tra qua public BE API. Deployment dùng release SHA đã duyệt, backup trước activation rồi kiểm tra health và live E2E.

**Tech Stack:** GitHub Actions, PowerShell, .NET 8 test projects, SQL Server 2019, private MinIO, Docker Compose, public RoadGuard BE, private AI worker, clip MP4/SRT đã có trong thư mục demo ngoài repository.

**Source spec:** [CI-API-SQL-LANES.md](CI-API-SQL-LANES.md)

**Base sau Gói 1** · `b29d7fade213cb0647ad323454f087854358321e`. TRX ghi 1.778 danh tính API (1.777 pass, 1 skip tùy chọn), 897 SQL pass và 1.312 unit pass. [Gói 1, run 45644468742](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45644468742). Mốc lịch sử [run 45598662474](https://github.com/HoangAnhVu2207/RoadGuardSystem/actions/runs/45598662474) dùng SHA `fbde7c43923f38f969af2ac75b82c56c4d0770f6`.

## Global constraints

- Chạy các gói theo phạm vi owner giao. Task G cần duyệt ở G2. Seed dùng chung tại Task H cần duyệt riêng ở H0 và backup đã xác minh.
- Lấy source từ `origin/develop`, ghi full SHA, không sửa trực tiếp release đang chạy.
- Dùng project demo additive. Tên đề xuất là `DEMO Duong Kenh Dong 2` hoặc `DEMO Duong so 2.2`. Owner xác nhận tên cuối trước Task D. Không xóa, đổi tên, reset hoặc seed đè hai project hiện có.
- Dữ liệu mô phỏng phải gắn nhãn `DEMO`. Clip không có GPS hợp lệ giữ trạng thái thiếu định vị, không gán GPS giả.
- Không commit password, token, connection string, object key hoặc log có credential. Credential chỉ đọc từ private local file hoặc user nhập trong terminal.
- Mỗi SQL/API lane dùng fixture riêng. Không chạy data-writing test vào database server RoadGuard thật.
- AI chỉ tạo đề xuất. PM là người duyệt, giao việc và quyết định sửa chữa.
- Mỗi task kết thúc bằng self-check. Chỉ chuyển task sau khi mọi tiêu chí của task đạt hoặc được ghi `NOT RUN` với lý do.

---

### Task A: Freeze source, scope and demo dataset

**Goal:** Chốt source SHA, worktree sạch và danh mục dữ liệu đầu vào để mọi task sau dùng cùng một revision.

**Files:**

- Read: `planning/development/ROADGUARD-REAL-USER-E2E.md`
- Read: `planning/development/CI-API-SQL-LANES.md`
- Read: `D:/RoadGuard-AI-LocalTest/server-demo-replay-20261124/manifest.json`
- Read: `D:/RoadGuard-AI-LocalTest/server-demo-replay-20261124/FE-QUICKSTART.md`

**Steps:**

- [ ] Fetch `origin/develop`, record SHA and compare changed paths with current worktree.
- [ ] Preserve unrelated dirty paths and work on `anh` from the merged SHA in a separate worktree.
- [ ] Validate that every selected MP4 has its paired SRT, checksum and readable file metadata. Pair the missing-GPS fixture with an SRT that has no valid GPS and record the expected unlocated result.
- [ ] Record the selected project name, role emails by role, project membership and clip-to-project mapping in a private execution manifest outside Git.
- [ ] Mark the existing two projects as historical/current evidence only. Do not alter them in this task.

**Self-check:**

- `git status --short --branch` shows only task-owned files.
- The source SHA, clip count, SRT pairing and project-name decision are recorded.
- No credential, token or private object key appears in tracked files, console output or the plan.

**Gate:** A missing clip/SRT pair or unresolved project name pauses the dependent preparation in Task D. CI and contract work may continue.

---

### Task B: Build the CI lane framework first

**Goal:** Implement and verify the approved 12 API lanes and 10 SQL lanes before server data work.

**Files:**

- Modify: `.github/workflows/ci.yml`
- Create or modify: `tests/CI/api-lanes.json`
- Modify: `tests/CI/sql-lanes.json`
- Create: `tests/CI/Invoke-ApiLane.ps1`
- Create: `tests/CI/Verify-ApiLanes.ps1`
- Modify: `tests/CI/Invoke-SqlLane.ps1`
- Modify: `tests/CI/Verify-SqlLaneDiscovery.ps1`
- Modify: `tests/CI/Verify-SqlLanes.ps1`
- Modify: `tests/CI/Verify-TestResults.ps1`
- Modify: `tests/CI/Verify-CiWorkflow.ps1`
- Modify: `tests/CI/Verify-TestResults.Tests.ps1`

**Steps:**

- [ ] Build a class and method inventory from the baseline TRX and compiled assemblies. Preserve the baseline inventory of 1,778 API and 897 SQL identities and retain the 1,312-unit baseline for comparison.
- [ ] Danh sách 12 lane API theo bảng trong [spec CI](CI-API-SQL-LANES.md#2-mục-tiêu-và-thiết-kế).
- [ ] Rebalance SQL classes and expensive Seeder methods across 10 lanes. Keep every theory method together and preserve the separate opt-in real-AI acceptance class.
- [ ] Add discovery validation that rejects unassigned classes, methods or duplicate assignments.
- [ ] Add TRX aggregation validation for missing lane, zero result, failed result, skipped result outside the existing MinIO exception, duplicate identity, unexpected identity and cancelled lane.
- [ ] Reuse the pinned MinIO source and cache key. Run independent restore/build/provider preparation concurrently where the workflow permits and preserve artifact names and coverage branches.
- [ ] Keep stable aggregate checks named `API tests` and `SQL integration tests`, with `fail-fast: false`.

**Self-check:**

- Run `pwsh -File tests/CI/Verify-CiWorkflow.ps1`.
- Run `pwsh -File tests/CI/Verify-TestResults.Tests.ps1`.
- Run discovery against the built API and integration assemblies.
- Run all adversarial TRX fixtures and confirm each invalid fixture fails for the intended reason.
- Run `git diff --check`.

**Gate:** The lane manifest must reconcile every baseline identity exactly once before any GitHub benchmark. A green workflow with missing identity coverage does not pass.

---

### Task C: Create the role-flow test contract

**Goal:** Define a repeatable, secret-free test contract that exercises each project role through the public BE API.

**Files:**

- Create: `tests/e2e-playwright/real-user-demo-manifest.json` as non-secret route and expected-state metadata only.
- Create or modify: `tests/e2e-playwright/real-user-demo.ps1` or the existing repository E2E entrypoint after inspecting its current conventions.
- Read and reuse: `D:/RoadGuard-AI-LocalTest/server-demo-replay-20261124/run-real-e2e.py`, `manifest.json`, `Seed-RoadGuardDemoProjects.ps1`.

**Contract to write for execution in Task E:**

- [ ] Login as Supervisor and verify project list, membership visibility and project-scoped reads.
- [ ] Login as PM and create or read the survey task for the selected demo project.
- [ ] Login as Drone Operator, accept the task, create multipart uploads for MP4 and SRT, complete them, wait for verification and submit the paired dataset with idempotency and ETag headers.
- [ ] Wait for BE to finish the AI job through the public API, then have PM read telemetry and candidate proposals. Record model/result IDs and statuses without storing access tokens.
- [ ] Verify Reporter, Crew and another PM receive the expected status for the selected project scope, including the existing 401/403 authorization matrix.
- [ ] Exercise PM review of AI proposals. Keep AI output pending until the PM decision step.
- [ ] Specify Crew inspection and PM repair decision only through routes confirmed by current controllers and API tests. If a route is absent or its business decision is unresolved, record `UNKNOWN` and stop that branch without fabricating a route.
- [ ] Repeat one submit/retry request with the same idempotency key and verify one durable result plus the retained failed attempt where the scenario requires it.

**Self-check:**

- Document the expected route, role, state, status and evidence for each action. Task E records the disposable run receipt.
- Every role has a documented login action and an expected authorization assertion.
- The contract specifies file checksum verification and paired telemetry status.
- No test calls the private AI endpoint directly from Web or Mobile flows.

**Gate:** Task C ends with the public contract and any `UNKNOWN` branches. Task E executes the contract and verifies project scope, idempotency and AI-result readback.

---

### Task D: Prepare the additive demo data package

**Goal:** Create one new demo project and fully populate its role memberships, road graph, warranty, survey, upload and processing input using old clips.

**Files:**

- Modify only the private execution manifest created in Task A.
- Use existing external scripts and source entrypoints. Do not add credentials to the repository.
- If a production seeder change is necessary, create a separate source change with its own focused test and do not mix it into the deployment commit.

**Steps:**

- [ ] Record the owner-confirmed `DEMO Duong Kenh Dong 2` or `DEMO Duong so 2.2` in the private execution manifest.
- [ ] Prefer the public BE project creation API when Task C confirms the route and caller. If related demo records require an external script, record its exact tables, storage actions and disposable target in Task D. The repository Seeder needs direct DB access and creates the fixed code `RG-CI-01-SYNTHETIC`. Use it only for its dedicated synthetic test fixture. Assign PM, Drone Operator, Repair Crew and Reporter as supported, and verify Supervisor visibility.
- [ ] Seed road sections and geometry only from supported fixtures. Mark synthetic geometry as `DEMO` and keep real GPS telemetry separate from road geometry.
- [ ] Create survey tasks for the operator and crew. Use existing warranty and survey service flows for related records.
- [ ] Upload paired MP4/SRT files from the old clip set. Verify object checksum, server verification status and persisted survey-file relation.
- [ ] Submit one normal dataset and one controlled retry dataset. Keep any missing-GPS case explicitly unlocated.
- [ ] Persist a manifest containing project ID, task IDs, dataset IDs and file IDs outside Git.

**Self-check:**

- Re-running the additive seed does not create duplicate project, membership or file records.
- The project contains at least one PM, one operator, one crew and one reporter membership, plus Supervisor visibility.
- Each survey file is attached to the intended project and survey only.
- SQL counts, object checksums and public API reads agree.
- Existing `Duong Kenh Dong` and `Duong so 2` record counts are unchanged.

**Gate:** Seed verification must pass before starting live role playback. Any duplicate or cross-project relation stops the task and triggers investigation.

---

### Task E: Run the complete role workflow against the disposable target

**Goal:** Validate the same role sequence locally or on a task-owned server target before touching the shared production server.

**Files:**

- Modify only task-owned receipts and private artifacts outside Git.
- Reuse the Task C runner and existing clip manifest.

**Steps:**

- [ ] Run Supervisor project visibility.
- [ ] Run PM survey planning and proposal review.
- [ ] Run Operator upload and dataset submission.
- [ ] Run AI job polling, telemetry read and candidate read.
- [ ] Run Reporter and Crew authorization checks.
- [ ] Run Crew field inspection, then PM Normal/FastTrack decision when current routes and fixtures support it.
- [ ] Verify replay, retry, outbox/audit records and no duplicate durable result.

**Self-check:**

- The workflow receipt has one result per role action, HTTP status, correlation ID, project ID and durable record ID.
- All expected successes and denials pass.
- The AI candidate is still subject to PM action.
- The run uses a disposable database/storage target and no production server data.

**Gate:** Review every `UNKNOWN` branch before treating the disposable role run as acceptance evidence. Task F can run independently.

---

### Task F: Benchmark CI and publish the candidate

**Goal:** Prove the lane design retains all test coverage and reaches the speed target.

**Steps:**

- [ ] Commit the two plans, CI files, manifests and focused verifier tests. Review twice and push the candidate to `anh` from the approved merged base.
- [ ] Use the ordinary push run and a sequential ordinary `workflow_dispatch` on the same SHA. Wait for one run to finish before starting the next. Collect created, start and completion timestamps.
- [ ] Verify unit, source/security, API lane and SQL lane aggregate checks. Verify every lane artifact and coverage artifact on scheduled/manual coverage mode.
- [ ] Require two consecutive ordinary runs on the same SHA to be green and under five minutes from run creation to final job completion.
- [ ] If the target misses, rebalance by measured lane duration or reduce setup duplication without reducing test identities. Repeat the benchmark.
- [ ] Record cache-hit and cache-miss timings separately.

**Self-check:**

- Both runs have the same SHA.
- API and SQL identity unions match the baseline exactly once.
- No test was converted to a silent skip.
- The measured time and queue time are recorded from GitHub run metadata.

**Gate:** Fast-forward `develop` after CI is green and the identity gates pass on the exact candidate SHA. Keep the separate speed target `PENDING` with measured timing if either run reaches five minutes.

---

### Task G: Deploy the approved BE release and verify private AI communication

**Goal:** Deploy the exact approved develop SHA and prove the public BE plus private AI path works.

**Server checkpoint:** Read `deploy/server/SERVER-RULES.md`, check active release, running heavy operation, RAM and disk. Deployment requires one owner-approved maintenance window that covers backup, activation, restart, health and live smoke.

**Steps:**

- [ ] Have the owner decide the release source at G2. SERVER-RULES selects `origin/deploy` and `update-reviewed.sh` defaults to `deploy`. Selecting a different approved branch requires `RG_SOURCE_BRANCH`. Pin the exact green SHA and its test identity evidence.
- [ ] Create and checksum SQL/storage/config backup. The current backup stops API and MinIO, so include backup and readiness recovery in the approved downtime window. Record prior SHA, migration path and restore procedure.
- [ ] Verify migration from the deployed schema on a disposable copy and demonstrate old-BE compatibility with the migrated schema before permitting application-only rollback. If incompatible, use the approved database/storage/config restore plan. Enable `RG_AI_ENABLED` only in the approved live activation after checking model registration, checkpoint and worker configuration.
- [ ] Verify API, AI, SQL and MinIO container health.
- [ ] Verify public login and project reads. Verify private worker readiness and connectivity. Run mutating upload, job completion and role smoke on the approved DEMO project in Task H.
- [ ] Roll back only if the approved health or live smoke criteria fail and the rollback is schema-compatible.

**Self-check:**

- Active release full SHA equals candidate SHA.
- Backup path and checksums are recorded.
- Health, login, project-scope reads and private worker connectivity checks pass.
- No secret is present in the receipt or logs.

**Gate:** Do not start Task H until deployment evidence has all required checks and the release is stable.

---

### Task H: Seed the server demo project and hand off to Web and Mobile

**Goal:** Make the additive demo project usable by Web and Mobile with role-specific IDs and a verified API contract.

**Steps:**

- [ ] Get explicit H0 approval for the named DEMO project and verify the pre-seed backup and rollback plan. Through the approved BE routes or the explicitly reviewed external script from Task D, create only the new project and its memberships, road sections/geometry, warranty and survey tasks.
- [ ] Use Playwright for live smoke with one old MP4/SRT pair and one controlled replay or retry. The permitted writes in that project are assignment and accept steps, multipart upload and completion, dataset submission and AI job, PM proposal review, Crew inspection and PM repair actions confirmed by Task C. Any additional write needs H0 approval.
- [ ] Capture a FE/Mobile handoff containing public API base URL, project ID, route IDs, non-secret role email labels, expected 401/403 matrix, upload requirements, telemetry status rules and AI proposal/PM decision states.
- [ ] Provide a private local credential retrieval procedure. Do not put passwords in the handoff.
- [ ] Verify Web/Mobile can use the public BE endpoint and never need SQL, MinIO console or private AI URLs.
- [ ] Record whether Crew inspection and PM repair completion were live verified, mock verified or still unknown.

**Self-check:**

- Web/Mobile handoff has only public routes and non-secret identifiers.
- Login, project read, upload, AI proposal read and role authorization are verified on the deployed SHA.
- The demo data is labeled `DEMO` and is isolated from the two original projects.
- The final report distinguishes BE verified, mock verified, external/deployment verified and unknown.

**Completion:** The package is complete only when Task F has exact-SHA green CI and a measured benchmark, Task G has deployment evidence and Task H has a deployed role-flow receipt. Record the speed target separately as `PASS` or `PENDING`.


---

### Task I: Build the browser test console and Playwright suite

**Goal:** Dùng Playwright làm runner E2E chính. Console trình duyệt là tùy chọn để quan sát từng bước qua public BE.

**Placement:**

- Create: `tests/e2e-playwright/console/index.html`
- Create: `tests/e2e-playwright/console/app.ts`
- Create: `tests/e2e-playwright/console/styles.css`
- Create: `tests/e2e-playwright/console/README.md`
- Create: `tests/e2e-playwright/real/role-flows.spec.ts`
- Modify: `tests/e2e-playwright/real/playwright.config.ts`
- Modify: `tests/e2e-playwright/package.json` only for scripts and packages already required by the existing setup

**Interface:**

- The console accepts the public BE base URL and a project ID through environment-backed test configuration. The browser must never receive SQL, MinIO console or private AI URLs.
- The console shows one row per role, one row per workflow stage and the latest HTTP status, correlation ID, business ID prefix and durable state.
- Password fields are entered at runtime through Playwright or a local prompt and are never written to local storage, the DOM after submission, screenshots, traces or receipts.
- The UI supports preflight, role login, project scope, task assignment, upload preparation, AI job polling, proposal review, Crew inspection, PM repair decision and authorization checks. Disabled controls explain when a prerequisite has not passed.
- The UI labels all seeded records `DEMO` and shows missing-GPS state without inventing a coordinate.

**Steps:**

- [ ] Reuse `tests/e2e-playwright/real/api.ts`, `contract.ts`, `video.ts` and the existing real-run safety guards.
- [ ] Add a state machine in the console that allows each step to be rerun with the same idempotency key where replay is expected.
- [ ] Add Playwright tests for the visible happy path and the expected 401/403 matrix across Supervisor, PM, Operator, Crew and Reporter.
- [ ] Add tests for MP4/SRT upload status, paired dataset submission, AI job completion, telemetry status, proposal visibility, PM review and retry.
- [ ] Add tests for one missing-GPS asset and assert that the UI displays missing telemetry without a fabricated position.
- [ ] Add Playwright trace, screenshot and JSON receipt output under a task-owned ignored directory. Scrub credentials, bearer tokens, signed URLs and full object keys before writing any artifact.
- [ ] Add a dry-run mode that validates configuration and route discovery without mutating server data. Require an explicit mutation flag for the live demo flow.
- [ ] Verify the console against a disposable local target first, then run the same Playwright project against the deployed public BE after Task G.

**Self-check:**

- `npm run typecheck` passes in `tests/e2e-playwright`.
- Dry-run Playwright passes with no server mutation.
- Live Playwright produces a receipt with every role and stage, expected status, project scope and no secret fields.
- A failed prerequisite prevents dependent controls from running.
- A replay produces one durable result and the UI reports the original and replay attempt states.
- The browser network log contains calls only to the configured public BE and the signed object URL returned for the current upload.

**Gate:** Task I is complete when the Playwright suite can run against the deployed SHA and produce a red/green result that a Web or Mobile developer can inspect without database access.

---

## Execution order and handoff

Hợp nhất Gói 1 → A → B → C → D → E → I dry-run → G → H. Task F chạy song song sau khi có ứng viên từ B. Phần live của I chạy sau G trong phạm vi H0. Review hai lượt và cập nhật `develop` bằng fast-forward sau khi CI xanh đủ ca trên đúng SHA của từng gói.

Each task returns a short handoff with source SHA, changed files, commands, pass/fail/skip counts, evidence paths, unresolved decisions and the next gate.
