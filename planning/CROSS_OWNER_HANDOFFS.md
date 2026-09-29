# Cross-owner handoffs

Bảng theo dõi mọi thay đổi ảnh hưởng layer của người kia.
Skill delivery và review PHẢI kiểm bảng này trước Done/commit/push.

## Publication checkpoint - 2026-09-30 03:34 +07:00

Owner approved an intermediate `anh` -> `develop` publication so Huy can inspect and respond to ANH-01..04. The four handoffs remain `SENT` and the four tasks remain `PARTIAL`; publication is not receiver verification or task completion. Only the reviewed survey repository change, two focused SQL tests, four task checkpoints and this ledger are included. The uncommitted candidate migration baseline and `docs/design` restructure are excluded; SQL evidence recorded below came from that dirty local checkout and must not be treated as verification of a clean `develop` checkout. Huy should use the committed V2 documents through the current index and report any missing source or fixture before claiming integration evidence.

## Active handoffs

| ID | Date | Sender -> Receiver | Task/commit | Files and symbols | Contract before -> after | Receiver action | Receiver outcome | Compatibility | Sender checks | Integration evidence | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|
| DB-BASELINE-01 | 2026-09-30 | Person 1 -> Person 2 | [DB-BASELINE-20260930](V2/Governance/DB-BASELINE-20260930.md); uncommitted | `Repositories/Migrations`: 37 migration IDs -> `20260929184004_Baseline20260930`; 20 SQL triggers retained; six EF configurations describe existing defaults | Fresh database bootstrap only; existing databases with old `__EFMigrationsHistory` are rejected before DDL | Confirm disposable database reset for API/dev environments, and update any Person 2 runbook or test setup that assumes old migration IDs | Awaiting receiver response | Breaking for existing database history; no API wire change | SQL integration 303/303; baseline lifecycle/rejection 3/3; UnitTests 170/170; reference/candidate schema parity | API tests 117 passed, 1 optional MinIO smoke skipped, after excluding local Development override; Seeder twice passed on disposable baseline database | SENT |
| ANH-01-IDENTITY-01 | 2026-09-30 | Person 1 -> Person 2 | [ANH-01](V2/Execution/ANH-01-auth-identity.md); uncommitted | `IIdentityRepository`, `IIdentityOnboardingRepository`, `IdentityRepository`, `IdentityOnboardingRepository`; `BusinessObjects/Identity`; identity SQL tests | Repository facts cover session/token rotation and family revocation, OTP issue/consume/resend/attempt limits, invitation replay, password/account invalidation, rowversion and idempotency outcomes. Services remain actor/scope and HTTP authority. | Huy reads result enums, absence/conflict/stale/replay semantics and fixture patterns; confirm API mapping needs no repository contract change | Awaiting receiver response | No source contract change; D25/36A/37 transport/runtime settings remain target documentation | Repositories build PASS; identity SQL filter 64/64 PASS, SQL Server fixture, 0 skipped | API smoke is receiver-owned | SENT |
| ANH-04-OPS-01 | 2026-09-30 | Person 1 -> Person 2 | [ANH-04](V2/Execution/ANH-04-processing-files-operations.md); uncommitted | `IProcessingV2Repository`, `IUploadRepository`, `IFileRepository`, `INotificationRepository`; `NotificationPersistenceService.MarkReadAsync`; focused SQL tests | Existing facts: processing job has project/manifest hash/mode/rowversion and attempt identity; upload has project/file scope, checksum and version; notification read returns `Success`, `Replayed`, `NotFound`, `StaleConcurrency`, `IdempotentConflict`. Mark-read has one durable `ReadAt` effect and scoped idempotency receipt. No repository interface changed. Callback currently checks attempt membership but does not fence an older attempt; EventReceipt, SyncOperation, LegalHold and DeletionRequest remain proposed schema. | Huy confirm consumer mapping and fixture needs; keep callback/retention/sync API claims gated until approved persistence contract and receiver integration evidence | Awaiting receiver response | No interface or wire change; stale callback and absent proposed entities block their slices | Repositories build PASS; SQL Server 16.0.1000.6 focused persistence 32/32 and migration lifecycle 3/3, 0 failed/skipped | API/provider smoke receiver-owned and not claimed; no cross-layer integration evidence yet | SENT |
| ANH-02-PROJECT-SURVEY-01 | 2026-09-30 | Person 1 -> Person 2 | [ANH-02](V2/Execution/ANH-02-project-survey.md); uncommitted | `SurveyV2PersistenceService.ResolveScopeAsync`; `ISurveyV2Repository`; `P2V2SurveyScopeConcurrencyTests`; project/membership, survey, upload and dataset repository facts | Root `RouteVersionId` must be present in requested scope; reads are scoped/projection-based; survey task and dataset writes preserve rowversion/idempotency; upload verification is distinct from dataset/quality/coverage/baseline; result statuses are `Success`, `Replayed`, `Conflict`, `ConcurrencyConflict`, `NotFound`, `InvalidInput`, `OperatorNotFound` | Huy consumes repository status/fact semantics and maps actor/scope/HTTP policy; confirm no API contract change is required for the root-route validation | Awaiting receiver response | Repository-only behavior; no wire/schema/provider change | Repositories build PASS; IntegrationTests build PASS; focused SQL filter 50/50 PASS, 0 skipped on SQL Server 2022 Express `16.0.1000.6`; new root-route mismatch test passes | No API smoke; no migration applied; durable test DBs are isolated and dropped by fixture | SENT |
| ANH-03-PERSISTENCE-01 | 2026-09-30 | Person 1 -> Person 2 | [ANH-03](V2/Execution/ANH-03-report-inspection-repair.md); uncommitted | Current `Defect`, `DefectVerificationLog`, `FieldInspectionTask`, `FieldInspectionAssignment`, `FieldInspectionSession`, `GroundTruthMeasurement`, `Warranty`; `IDetectionReviewRepository`, `IInspectionTaskReadRepository` | Current facts: defect review write is idempotent and atomic with audit/outbox; replay returns `Replayed`, changed fingerprint returns conflict, duplicate retained detection returns duplicate status; inspection assigned read is bounded `AsNoTracking()` projection; task has SQL rowversion; measurements have immutable SQL backstops. Target report/case/link, policy snapshot, repair attempt/BEFORE, curing/release/acceptance entities are absent and not implemented. | Huy must keep API/service claims limited to these current repository facts; report/case/repair endpoints require a linked schema task and must not infer target entities from draft OpenAPI. Reply `NO_CHANGE_NEEDED` if no current API mapping change, or create linked task for missing persistence. | Awaiting receiver response | No wire contract or authorization policy change; target report/case/repair schema remains blocked by approved compatibility/schema decision. | Repositories build PASS; IntegrationTests build PASS; focused SQL 18/18 PASS, 0 skipped, Testcontainers SQL Server 2019-CU18; no API evidence. | SQL tests inspected durable Defect/inspection rows, audit/outbox, constraints/triggers and rollback/replay effects; no live migration applied. | SENT |

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
7. Thay đổi liên quan chỉ được đánh dấu Done/commit/push khi `Status = VERIFIED`. Việc tạo commit vẫn cần quyền theo `AGENTS.md`; `SENT`, `RECEIVED`, `PROCESSED` không đủ.
8. Gửi thông báo không phải bằng chứng phần tích hợp đã hoàn thành.

## Completed handoffs

_(Move rows here after VERIFIED. Keep as append-only evidence.)_
