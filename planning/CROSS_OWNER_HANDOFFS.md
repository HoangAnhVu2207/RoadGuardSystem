# Cross-owner handoffs

Bảng theo dõi mọi thay đổi ảnh hưởng layer của người kia.
Skill delivery và review PHẢI kiểm bảng này trước Done/commit/push.

## Publication checkpoint - 2026-09-30 03:34 +07:00

Owner approved an intermediate `anh` -> `develop` publication so Huy can inspect and respond to ANH-01..04. The four handoffs remain `SENT` and the four tasks remain `PARTIAL`; publication is not receiver verification or task completion. Only the reviewed survey repository change, two focused SQL tests, four task checkpoints and this ledger are included. The uncommitted candidate migration baseline and `docs/design` restructure are excluded; SQL evidence recorded below came from that dirty local checkout and must not be treated as verification of a clean `develop` checkout. Huy should use the committed V2 documents through the current index and report any missing source or fixture before claiming integration evidence.

## Active handoffs

| ID | Date | Sender -> Receiver | Task/commit | Files and symbols | Contract before -> after | Receiver action | Receiver outcome | Compatibility | Sender checks | Integration evidence | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|
| DB-BASELINE-01 | 2026-09-30 | Person 1 -> Person 2 | [DB-BASELINE-20260930](V2/Governance/DB-BASELINE-20260930.md); uncommitted | `Repositories/Migrations`: 37 migration IDs -> `20260929184004_Baseline20260930`; 20 SQL triggers retained; six EF configurations describe existing defaults | Fresh database bootstrap only; existing databases with old `__EFMigrationsHistory` are rejected before DDL | Confirm disposable database reset for API/dev environments, and update any Person 2 runbook or test setup that assumes old migration IDs | Awaiting receiver response | Breaking for existing database history; no API wire change | SQL integration 303/303; baseline lifecycle/rejection 3/3; UnitTests 170/170; reference/candidate schema parity | API tests 117 passed, 1 optional MinIO smoke skipped, after excluding local Development override; Seeder twice passed on disposable baseline database | SENT |
| ANH-04-OPS-01 | 2026-09-30 | Person 1 -> Person 2 | [ANH-04](V2/Execution/ANH-04-processing-files-operations.md); uncommitted | `IProcessingV2Repository`, `IUploadRepository`, `IFileRepository`, `INotificationRepository`; `NotificationPersistenceService.MarkReadAsync`; focused SQL tests | Existing facts: processing job has project/manifest hash/mode/rowversion and attempt identity; upload has project/file scope, checksum and version; notification read returns `Success`, `Replayed`, `NotFound`, `StaleConcurrency`, `IdempotentConflict`. Mark-read has one durable `ReadAt` effect and scoped idempotency receipt. No repository interface changed. Callback currently checks attempt membership but does not fence an older attempt; EventReceipt, SyncOperation, LegalHold and DeletionRequest remain proposed schema. | Huy confirmed notification consumer mapping; callback/retention/sync/export API claims remain gated until approved persistence contract and receiver integration evidence. | `PROCESSED / NEW_TASK_NEEDED:HUY-04-CALLBACK-FENCE-SYNC-RETENTION-CONTRACT`: notification mapping needs no change, but stale-attempt fencing/late receipt and proposed operational entities are absent. | No interface or wire change; stale callback and absent proposed entities block their slices | Repositories build PASS; SQL Server 16.0.1000.6 focused persistence 32/32 and migration lifecycle 3/3, 0 failed/skipped | Receiver API/SQL checks pass on `d2338dc`; no external AI/object-store/retention claim | PROCESSED |
| ANH-02-PROJECT-SURVEY-01 | 2026-09-30 | Person 1 -> Person 2 | [ANH-02](V2/Execution/ANH-02-project-survey.md); uncommitted | `SurveyV2PersistenceService.ResolveScopeAsync`; `ISurveyV2Repository`; `P2V2SurveyScopeConcurrencyTests`; project/membership, survey, upload and dataset repository facts | Root `RouteVersionId` must be present in requested scope; reads are scoped/projection-based; survey task and dataset writes preserve rowversion/idempotency; upload verification is distinct from dataset/quality/coverage/baseline; result statuses are `Success`, `Replayed`, `Conflict`, `ConcurrencyConflict`, `NotFound`, `InvalidInput`, `OperatorNotFound` | Huy consumed root-route/status semantics and found no API contract change required for current consumers; coverage/baseline target remains gated. | `PROCESSED / NO_CHANGE_NEEDED`: SurveyV2Service and controllers preserve actor/project scope, rowversion/replay mapping and explicit UNKNOWN coverage. | Repository-only behavior; no wire/schema/provider change | Repositories build PASS; IntegrationTests build PASS; focused SQL filter 50/50 PASS, 0 skipped on SQL Server 2022 Express `16.0.1000.6`; new root-route mismatch test passes | Receiver API/SQL checks pass on `d2338dc`; no migration applied; durable test DBs isolated | PROCESSED |
| ANH-03-PERSISTENCE-01 | 2026-09-30 | Person 1 -> Person 2 | [ANH-03](V2/Execution/ANH-03-report-inspection-repair.md); uncommitted | Current `Defect`, `DefectVerificationLog`, `FieldInspectionTask`, `FieldInspectionAssignment`, `FieldInspectionSession`, `GroundTruthMeasurement`, `Warranty`; `IDetectionReviewRepository`, `IInspectionTaskReadRepository` | Current facts: defect review write is idempotent and atomic with audit/outbox; replay returns `Replayed`, changed fingerprint returns conflict, duplicate retained detection returns duplicate status; inspection assigned read is bounded `AsNoTracking()` projection; task has SQL rowversion; measurements have immutable SQL backstops. Target report/case/link, policy snapshot, repair attempt/BEFORE, curing/release/acceptance entities are absent and not implemented. | Huy kept API/service claims limited to current defect/inspection facts; report/case/repair endpoints require a linked schema task and are not inferred from draft OpenAPI. | `PROCESSED / NEW_TASK_NEEDED:HUY-03-REPORT-CASE-REPAIR-CONTRACT`: current persistence lacks report/case/repair/BEFORE/release/acceptance facts; existing inspection read remains bounded. | No wire contract or authorization policy change; target report/case/repair schema remains blocked by approved compatibility/schema decision. | SQL receiver checks pass for defect 8/8 and inspection 6/6 on `d2338dc`; no API report/repair evidence | PROCESSED |

## Receiver processing checkpoint - 2026-09-30

- Base: clean `huy` at `d2338dcffd8838198c2b50ee369b9678e7e06690`; `origin/develop`, `origin/huy`, local `huy` and `develop` point to the same commit.
- Source gaps: `docs/design/**` and candidate `20260929184004_Baseline20260930` migration are absent from the handoff commit and are recorded as `NOT_ENABLED`; committed `docs/diagram/V2` is used only for target/documented comparison.
- Receiver outcomes: ANH-01 `NO_CHANGE_NEEDED`; ANH-02 `NO_CHANGE_NEEDED`; ANH-03 `NEW_TASK_NEEDED:HUY-03-REPORT-CASE-REPAIR-CONTRACT`; ANH-04 `NEW_TASK_NEEDED:HUY-04-CALLBACK-FENCE-SYNC-RETENTION-CONTRACT`.
- Verification: sequential Services/API/UnitTests/ApiTests builds passed; fresh focused API/Unit/SQL counts are recorded in each HUY task. Postman static parse passed schema/variables/reference checks; no external provider, SMTP, hosted HTTP, migration rollout or destructive retention evidence is claimed.
- Lifecycle: handoffs are `PROCESSED`, not `VERIFIED`; HUY-01..04 remain `PARTIAL` and no commit/push was performed in this receiver pass.

## Receiver follow-up - 2026-09-30

- HUY-04 changed only `RoadGuardSystem.API/RoadGuardSystem.API.http` to add notification list/get/mark-read examples using the existing actor, idempotency and `If-Match` contract. No Service/DTO/Repository/schema behavior changed.
- Recheck: alignment guards PASS; Postman static parse PASS (83 requests, 0 unresolved references, 0 duplicate names/IDs); fresh builds PASS; focused API `16 passed, 1 optional MinIO skip`, Unit `159/159`, Integration SQL `55/55` passed.
- Status remains `PROCESSED`, not `VERIFIED`; hosted HTTP/SMTP/external-provider smoke and missing persistence/design/baseline evidence remain outside the committed handoff.

- HUY-01 follow-up: added the existing forced-password-change request to `RoadGuardSystem.API/RoadGuardSystem.API.http` and Postman with disposable-account/destructive-run guards. No runtime contract or repository change; handoff remains `PROCESSED`, not `VERIFIED`.
- Full-solution verification before publication: `dotnet test RoadGuardSystem.slnx --nologo -v minimal` passed Unit `170/170`, API `117/118` with one optional MinIO skip, and Integration `317/317`; no failures. This is not hosted HTTP/SMTP/external-provider or clean-baseline evidence.

## Owner handoff decision - 2026-09-30 11:37 +07:00

- Owner Anh accepted Huy's ANH-01..04 receiver responses as the handoff decision. `NO_CHANGE_NEEDED` closes the consumer-mapping question for ANH-01/02; `NEW_TASK_NEEDED` records the missing contract/schema dependencies for ANH-03/04. Acceptance of receipt does not claim technical integration `VERIFIED` or turn the grouped tasks `DONE`.
- On clean `anh` HEAD `15444975988f77f585e7350a1467668781aad120`, Repositories and IntegrationTests builds passed (51 and 316 warnings respectively, 0 errors). A fresh combined identity/project/survey SQL filter executed 122 tests: 1 passed, 121 failed, 0 skipped because the configured SQL Server connection could not connect before fixture setup. The local SQL service and Docker are stopped; attempting to start `MSSQL$HANHNAV` was denied. ANH-01/02 remain `PARTIAL` and their handoffs `PROCESSED` until the required clean-checkout gate passes.
- Exact SQL command (Debug/net8.0): `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~IntegrationTests.Identity|FullyQualifiedName~P2V2SurveyScopeConcurrencyTests|FullyQualifiedName~P211ProjectMembershipReadModelTests|FullyQualifiedName~P220ProjectMembershipSchemaTests|FullyQualifiedName~P221RoadWarrantySchemaTests|FullyQualifiedName~P222SurveyPlanningSchemaTests|FullyQualifiedName~P223SurveyAssignmentSchemaTests|FullyQualifiedName~P219DatasetContractTests|FullyQualifiedName~P230FlightSurveyFileSchemaTests|FullyQualifiedName~P230DataVersionQualityCheckSchemaTests|FullyQualifiedName~UploadPersistenceSqlTests|FullyQualifiedName~FileRepositorySqlTests"`.
- ANH-03/04 remain `PARTIAL` with the linked contract/schema gaps recorded by Huy. `DB-BASELINE-01` is a separate `SENT` handoff: the candidate migration and its governance task are absent from this checkout, and Huy has not supplied its receiver response. No baseline, schema or live database action is implied by this decision.
- Documentation checks: `python docs/diagram/V2/ci/check_alignment.py` PASS (133 tasks), `git diff --check` PASS. `python docs/diagram/V2/09_Frontend/contracts/check_contracts.py` fails `CONTRACT_LOCK_MISMATCH`: the unchanged lock records `dd991f20...`, while the unchanged canonical OpenAPI hashes to `ada7f48f...`. This existing mismatch is not resolved by the handoff decision.

## ANH-01 verification checkpoint - 2026-09-30 11:52 +07:00

- Owner clarification: handoff records what the sender delivered and what the receiver may need; it is not a wait-for-confirmation gate. The ANH-01 persistence task passed its own source and SQL acceptance gates. Huy's `NO_CHANGE_NEEDED` remains a consumer report, and the `VERIFIED` completed row is supported by current focused integration evidence rather than permission from Huy.
- Current clean-base source at `15444975988f77f585e7350a1467668781aad120` with an untracked focused SQL test (SHA-256 recorded in ANH-01): Identity SQL 69/69 and focused auth/onboarding API 8/8 passed with no failures or skips on isolated SQL Server 2022 Express. No production contract, migration, provider or live database changed. External SMTP and HUY-01 hosted rollout are not claimed.

## Status values

- `SENT` — người gửi đã ghi, chưa xác nhận người nhận đã đọc
- `RECEIVED` — người nhận xác nhận đã đọc
- `PROCESSED` — người nhận ghi `NO_CHANGE_NEEDED`, `FIXED_IN_TASK:<ID>`, hoặc `NEW_TASK_NEEDED:<ID>` với lý do
- `VERIFIED` — kiểm tra tích hợp thích hợp đã pass, hoặc kiểm tra tĩnh đủ cho thay đổi chỉ có tài liệu; ghi lệnh, kết quả và commit/checkpoint

## Receiver response

Người nhận ghi vào cột "Receiver outcome":
- `NO_CHANGE_NEEDED` — phần mình không bị ảnh hưởng
- `FIXED_IN_TASK:V2-Pn-xxx` — đã sửa trong task liên kết
- `NEW_TASK_NEEDED:<ID>` — đã tạo task mới cho đúng owner; handoff vẫn mở đến khi task đó xử lý xong

## Rules

1. **P1 → P2:** Khi P1 đổi entity, enum, repository interface, query result, schema hoặc migration → báo P2 về tác động lên Service, DTO, API, mapping và tests.
2. **P2 → P1:** Khi P2 đổi nghiệp vụ Service, DTO, API contract, quyền hoặc validation → báo P1 về tác động lên entity, repository interface, query/write, constraint và SQL tests.
3. Nếu chỉ là điều chỉnh nhỏ, đã nằm trong scope và contract được duyệt → xử lý qua mốc bàn giao của cặp task hiện tại.
4. Nếu cần thay đổi nhiều file/domain, thay đổi contract/schema/quyền/workflow, hoặc khiến task hiện tại không thể hoàn thành độc lập → tạo task liên kết cho đúng owner.
5. Task liên kết phải nêu: nguồn thay đổi, dependency, file/symbol, acceptance criteria, kiểm thử và thời điểm tích hợp.
6. Không tự sửa vùng sở hữu của người kia.
7. Handoff là báo cáo phần đã làm, ảnh hưởng và phần còn thiếu. Sender tiếp tục và cập nhật `deliveryStatus` theo gate của task mình, không chờ receiver xác nhận; `Status = VERIFIED` chỉ mô tả bằng chứng tích hợp đã chạy. Commit/push vẫn theo quyền trong `AGENTS.md`.
8. Gửi thông báo không phải bằng chứng phần tích hợp đã hoàn thành; receiver ghi phản hồi và task liên kết khi phần mình có thay đổi.

## Completed handoffs

| ID | Date | Sender -> Receiver | Task/commit | Files and symbols | Contract before -> after | Receiver action | Receiver outcome | Compatibility | Sender checks | Integration evidence | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|
| ANH-01-IDENTITY-01 | 2026-09-30 | Person 1 -> Person 2 | [ANH-01](V2/Execution/ANH-01-auth-identity.md); uncommitted | `IIdentityRepository`, `IIdentityOnboardingRepository`, `IdentityRepository`, `IdentityOnboardingRepository`; `BusinessObjects/Identity`; identity SQL tests | Repository facts cover session/token rotation and family revocation, OTP issue/consume/resend/attempt limits, invitation replay, password/account invalidation, rowversion and idempotency outcomes. Services remain actor/scope and HTTP authority. | Huy read result enums, absence/conflict/stale/replay semantics and fixture patterns; no repository contract change required for current consumers. | `NO_CHANGE_NEEDED`: AuthService, IdentityOnboardingService and IdentityV2Service map current repository facts; SMTP/hosted smoke remains unverified. | No source contract change; D25/36A/37 transport/runtime settings remain target documentation | Original identity SQL 64/64; current SQL 69/69 including three new onboarding tests, SQL Server 16.0.1000.6, 0 skipped | Current focused API 8/8 and Identity SQL 69/69 on `anh` HEAD `1544497`; no external SMTP claim | VERIFIED |
