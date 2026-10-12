# Retention basis và legal hold

Baseline đọc nguồn: `cdeb1ca1037744d06995058af13d0582f6273c0a`, nhánh `huy-review`. `CURRENT_VERIFIED` trong trang này là đối chiếu source, không chứng nhận runtime. Nghiệp vụ là `TARGET_CONFIRMED`. Execution từng test lấy từ kết quả tập trung tại mục `09_Testing/01_Backend_Test_Strategy.md` của bộ tài liệu.

## Policy/state

TARGET_CONFIRMED PR41A là project policy, không national law: evidence/video/hồ sơ mọi warranty+5năm; technical log90d/export30d/backup35d; business audit theo hồ sơ. Không lấy upload date làm retention date; missing/multiple obligations phải waiting. ACTIVE hold PROJECT/FILE chặn eligibility; release giữ history.

WarrantyEligibleAfter = warrantyEndDate.AddYears(5).AddDays(1), midnight UTC+7→UTC, giữ trọn local day. Evaluate cần inventory complete/basis confirmed/current/>0 obligation/mọi boundary nonnull, dùng max. Active hold→BLOCKED_HOLD; thiếu proof→WAITING_RETENTION_BASIS; trước boundary→RETAIN_UNTIL; sau→ELIGIBLE_FOR_REVIEW. Không state nào authorize deletion.

## Actor/preconditions/API/DTO

Confirm basis/create/release Supervisor current; principal role phải actual account. RetentionController GET file/basis PUT/evaluation POST+GET/hold POST+GET/release POST; ConfirmRetentionBasisRequest/CreateRetentionEvaluationRequest/CreateRetentionHoldRequest/ReleaseRetentionHoldRequest. Commands Idempotency-Key; basis/release If-Match; reads trả ETag. FILE/PROJECT scope exact, reason required.

Main confirm pin inventory version/exact warranty set/head version→append revision/head/audit/supersedes. Inventory giữ current+immutable history Reporter/case/defect/FIELD/repair attempts/corrections/assessments/safety/offline capture/admission/package/export/processing theo registered contributor. Không chỉ live UI/FK links. AdditionalEligibleAfter cũng vào max proven boundary; unknown completeness giữ waiting.

Evaluation QUEUED→COMPLETE với serializable SQL application lock; known/selected project files→current inventory/basis/holds→item control snapshot/versions/reasons→one commit. GET evaluation current file visibility trước cursor để private ID không leak vào cursor. Historical snapshot không cấp perpetual read.

## Concurrency/alternative

If-Match thiếu428/malformed-stale412; stale/incomplete inventory409; basis mismatch422; conflicting key409. Current control/source locks chống concurrent hold/basis race. Changed source→BASIS_STALE, unresolved obligation→WAITING; active hold không bị basis mới cancel. Replay vẫn current role/scope.

## Gaps

Không purge scheduler được chứng minh. PROPOSED separate owner-scoped destructive design, complete cross-source inventory/final holds-obligations recheck/audit/recovery. UNKNOWN actual backup schedule/statutory extra obligations/live schema/trigger parity. Tests NOT_RUN; no deletion executed.

## Bằng chứng source

| File | Symbol / dòng | Trạng thái |
|---|---|---|
| [RetentionEligibilityEvaluator.cs](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionEligibilityEvaluator.cs) | `RetentionEligibilityInput` :4 | CURRENT_VERIFIED source |
| [RetentionRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs) | `RetentionRepository` :12 | CURRENT_VERIFIED source |
| [RetentionInventoryRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionInventoryRepository.cs) | `RetentionInventoryRepository` :9 | CURRENT_VERIFIED source |
| [ReporterIntakeRetentionInventoryContributor.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/ReporterIntakeRetentionInventoryContributor.cs) | `ReporterIntakeRetentionInventoryContributor` :9 | CURRENT_VERIFIED source |
| [RetentionRecords.cs](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionRecords.cs) | `RetentionBasisHead` :3 | CURRENT_VERIFIED source |
| [Anh02RetentionConfiguration.cs](../../../RoadGuardSystem.Repositories/Configurations/Anh02RetentionConfiguration.cs) | `Anh02RetentionConfiguration` :9 | CURRENT_VERIFIED source |

Nghiệp vụ: [quyết định32–44](../../../docs/product/confirmed-decisions.md) và [HUY-FINAL-INTEGRATION](../../../planning/development/HUY-FINAL-INTEGRATION.md), đặc biệt R01–R30, clock và persistent login mới nhất. Spec mới supersede package/RF cũ khi mâu thuẫn. [DOC-AUDIT-01](../../../docs/audit/DOC-AUDIT-01-REPORT.md) là HISTORICAL đối với execution trước; không sao chép PASS.

## Tests và giới hạn

Các test dưới đây là source references; class/method có trong executed-tests.json dùng fresh outcome, còn lại NOT_RUN. Tên test không chứng minh đủ coverage.

- [Anh02RetentionHttpTests.cs](../../../tests/RoadGuardSystem.ApiTests/Retention/Anh02RetentionHttpTests.cs)
- [Anh02RetentionPersistenceTests.cs](../../../tests/RoadGuardSystem.IntegrationTests/Retention/Anh02RetentionPersistenceTests.cs)

## Symbol checkpoints

- [RetentionEligibilityEvaluator.cs](../../../RoadGuardSystem.BusinessObjects/Retention/RetentionEligibilityEvaluator.cs) :11 — `WarrantyEligibleAfter` (`CURRENT_VERIFIED` source).
- [RetentionRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs) :56 — `CaptureAsync` (`CURRENT_VERIFIED` source).
- [RetentionRepository.cs](../../../RoadGuardSystem.Repositories/Implementations/Retention/RetentionRepository.cs) :204 — `ProcessOneAsync` (`CURRENT_VERIFIED` source).

## Tự rà soát

Pass1: đối chiếu nghiệp vụ, actor/scope, persistence, concurrency và giới hạn evidence. Pass2: đối chiếu caller/DI/consumer, route/DTO, link và writer scope. Đây là tự rà soát Codex, không review bên ngoài; không đổi source/test/migration/config/seed.
