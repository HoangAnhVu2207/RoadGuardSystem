# Coverage, phần kế thừa và thiếu contract

> **V2(3) overlay — 2026-09-28:** D01-D28 và backlog ngoài baseline được theo dõi tại [decision register](V2-3_DECISION_REGISTER.md). Các trạng thái trong tài liệu này là trạng thái tài liệu/plan, không phải runtime PASS.

## Phạm vi kiểm chứng

133 operation trong OpenAPI REVIEW-01 được ánh xạ 1–1 vào 133 task. Không có task mới chỉ làm DB hoặc nhiều endpoint. API phụ thuộc xuất hiện trong nội dung để tích hợp, không là output thứ hai của task. Toàn bộ request/response/schema trích từ YAML draft, không suy ra từ tên class.

## Phần plan cũ chưa có operation tương đương rõ trong V2

Các mục này **không bị bỏ khỏi dự án**. Không tự bịa URL để đủ task. Sau khi nhận Swagger/source hoặc chốt amendment, thêm đúng một task cho mỗi operation còn thiếu và tăng manifest.

| Nhóm cũ / nguồn | Nghĩa vụ còn phải đối chiếu | Xử lý trước giao coding |
|---|---|---|
| P1-20/P2-20/21 | Warranty, handover, work-package và primary-PM route legacy | V2 project không tự bao trùm các API này; cần Swagger hiện tại và mapping FR/UC |
| P1-21/P2-21 | RoadSection/Version so với route/branch/segment/slab V2 | Chốt adapter/migration, không rename dữ liệu đã có |
| P1-22/P2-22 | Survey request, plan CRUD chi tiết | V2 có create/postpone plan; các route còn lại cần contract thực tế |
| P1-30/P2-30 | Upload resume/status, supplementary survey, QC/coverage worker | Các API đã tách; worker nội bộ là phần hỗ trợ endpoint, không tạo API giả |
| P1-32/P2-32 | Review detection keep/edit/reject cụ thể | V2 defect create/verify chưa chứng minh tương đương hoàn toàn; cần request/response và provenance mapping |
| P1-40/41/P2-40/41 | Kiểm chứng đo, reject/supplement riêng | Không mặc định defect verification thay mọi action của inspection |
| P1-50/51/52/53 và P2 tương ứng | Repair package draft/version/items, repair accept/decline, progress/notes, unplanned defects | V2 có 15 repair operation nhưng không đủ chứng minh phủ từng action cũ; cần contract amendment, không nhét vào submit |
| P1-60/P2-60 | Match/merge/hợp nhất lịch sử | report-links không mặc nhiên là merge API; chốt semantics không mất provenance |
| P1-62/P2-62 | Dry-run deletion và retention matrix | Gate bảo vệ dữ liệu trước delete; worker/checklist không phải API đã có |
| P1-63/P2-63 | Import/pair ground truth, exclusion, export research | Hai validation operation không tự bao gồm endpoint import/export; giữ RS-01..RS-06, yêu cầu contract bổ sung |
| P1-64/P2-64 | Severity rules, cause catalog, account lifecycle rộng hơn | So với Swagger hiện tại trước tách task bổ sung |
| P1-65/P2-65 | Model/device/labels quản trị mở rộng | Scope R3 chưa chắc phủ toàn CRUD; không tự mở CRUD không có nhu cầu |
| FE-GAP-05 | Offline evaluation kind | Là amendment trong sync/batches, không tạo endpoint HTTP thứ hai cho kind |
| Backlog BR/US | Route track drone Sprint 2; FR cho US-10; retention chi tiết | Chốt trace/scope trước tạo task API; không coi đã được 133 task bao phủ |

## Checklist nền tảng và release được giữ lại (không phải task API)

- P1/P2-00..09 và hạng mục nền tảng đã Done: reuse boundary, Identity, transaction, audit/outbox, idempotency, file abstraction, catalog, device, notification; không chạy lại chỉ vì đổi plan.
- P1-70 đã Done trong plan cũ. P1-72 đang In Progress: SQL timing/hosted CI và bàn giao còn phải kiểm tra worklog; không đánh Done dựa vào build.
- P1/P2-HF-01 vẫn Proposed; không suy đoán đã triển khai.
- P2-67/release: migration từ DB hiện tại, SQL integration/concurrency, backup/restore, CI, secret/config, job recovery, rollback/release evidence. Giao người tích hợp P2, P1 cùng kiểm contract/business acceptance.
- Giữ IDs retired P2-12/24/33/43/54/66; ID mới dùng tiền tố V2 để không tái sử dụng.
- Wireframe/FE offline app implementation không được biến thành backend endpoint task. FE test phải dựa trên API đã có và quyết định đã chốt.

## V2(3) capability gaps outside the baseline

The following remain explicit workstreams rather than being forced into a misleading 133-operation mapping: company policy framework and exceptions; authority-specific reopen; partial publication and Report-Defect projection; PM detection group/split/match review; handover/conflict/rescue records; BEFORE-loss incident, curing and traffic release; BE-AI service manifest/candidate/artifact receipts; FastAPI workers; Web/Android/ops delivery. See the register for owner boundary and dependency type.

The following baseline tasks have independent fixtures but require a later end-to-end path: `getMe`, project/route/segment GETs, inspection snapshot, repair/task reads, job reads and retention reads. A missing producer is a data-fixture or integration dependency, not a reason to invent a cycle in the operation graph.

## Crosswalk theo operation

Đây là mapping chức năng đề xuất; một task cũ có thể được tách thành nhiều API. Lịch sử hoàn thành chỉ nằm trong plan gốc, không lan tự động sang task mới.

| API operation | Task mới | Nhóm cũ gợi ý | Owner |
|---|---|---|---|
| login | [V2-P1-001](Person_1/V2-P1-001_login.md) | P1-10/P2-10 | P1 |
| refreshTokens | [V2-P1-002](Person_1/V2-P1-002_refreshTokens.md) | P1-10/P2-10 | P1 |
| logout | [V2-P1-003](Person_1/V2-P1-003_logout.md) | P1-10/P2-10 | P1 |
| requestPasswordRecovery | [V2-P1-004](Person_1/V2-P1-004_requestPasswordRecovery.md) | P1-10/P2-10 | P1 |
| changePassword | [V2-P1-005](Person_1/V2-P1-005_changePassword.md) | P1-10/P2-10 | P1 |
| registerReporter | [V2-P1-006](Person_1/V2-P1-006_registerReporter.md) | P1-13/P2-13 | P1 |
| verifyReporterOtp | [V2-P1-007](Person_1/V2-P1-007_verifyReporterOtp.md) | P1-13/P2-13 | P1 |
| resendReporterOtp | [V2-P1-008](Person_1/V2-P1-008_resendReporterOtp.md) | P1-13/P2-13 | P1 |
| acceptInvitation | [V2-P1-009](Person_1/V2-P1-009_acceptInvitation.md) | P1-10/P2-10 | P1 |
| createInvitation | [V2-P1-010](Person_1/V2-P1-010_createInvitation.md) | P1-64/P2-64 | P1 |
| getMe | [V2-P1-011](Person_1/V2-P1-011_getMe.md) | P1-11 / P2-10 | P1 |
| updateMe | [V2-P1-012](Person_1/V2-P1-012_updateMe.md) | P1-11 / P2-10 | P1 |
| adminResetPassword | [V2-P1-013](Person_1/V2-P1-013_adminResetPassword.md) | P1-11 / P2-10 | P1 |
| updateAccount | [V2-P1-014](Person_1/V2-P1-014_updateAccount.md) | P1-64/P2-64 | P1 |
| getAccount | [V2-P1-015](Person_1/V2-P1-015_getAccount.md) | P1-64/P2-64 | P1 |
| listNotifications | [V2-P2-001](Person_2/V2-P2-001_listNotifications.md) | P1-64/P2-64 | P2 |
| readNotification | [V2-P2-002](Person_2/V2-P2-002_readNotification.md) | P1-64/P2-64 | P2 |
| listProjects | [V2-P1-016](Person_1/V2-P1-016_listProjects.md) | P1-20/P2-20 | P1 |
| createProject | [V2-P1-017](Person_1/V2-P1-017_createProject.md) | P1-20/P2-20 | P1 |
| getProject | [V2-P1-018](Person_1/V2-P1-018_getProject.md) | P1-20/P2-20 | P1 |
| updateProject | [V2-P1-019](Person_1/V2-P1-019_updateProject.md) | P1-20/P2-20 | P1 |
| closeProject | [V2-P1-020](Person_1/V2-P1-020_closeProject.md) | P1-24 / P2-53 (open-work persistence) | P1 |
| setMembership | [V2-P1-021](Person_1/V2-P1-021_setMembership.md) | P1-12, P1-20 / P2-11, P2-20 | P1 |
| listCrews | [V2-P1-022](Person_1/V2-P1-022_listCrews.md) | P1-20/P2-20 | P1 |
| createCrew | [V2-P1-023](Person_1/V2-P1-023_createCrew.md) | P1-64/P2-64 | P1 |
| createRouteDraft | [V2-P2-003](Person_2/V2-P2-003_createRouteDraft.md) | P1-21/P2-21 | P2 |
| getRouteVersion | [V2-P2-004](Person_2/V2-P2-004_getRouteVersion.md) | P1-20/P2-20 | P2 |
| updateRouteDraft | [V2-P2-005](Person_2/V2-P2-005_updateRouteDraft.md) | P1-21/P2-21 | P2 |
| confirmRoute | [V2-P2-006](Person_2/V2-P2-006_confirmRoute.md) | P1-21/P2-21 | P2 |
| previewSegmentSet | [V2-P2-007](Person_2/V2-P2-007_previewSegmentSet.md) | P1-21/P2-21 | P2 |
| publishSegmentSet | [V2-P2-008](Person_2/V2-P2-008_publishSegmentSet.md) | P1-21/P2-21 | P2 |
| createBranch | [V2-P2-009](Person_2/V2-P2-009_createBranch.md) | P1-21/P2-21 | P2 |
| createSlab | [V2-P2-010](Person_2/V2-P2-010_createSlab.md) | P1-21/P2-21 | P2 |
| createReport | [V2-P1-024](Person_1/V2-P1-024_createReport.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| listOwnReports | [V2-P1-025](Person_1/V2-P1-025_listOwnReports.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| getOwnReport | [V2-P1-026](Person_1/V2-P1-026_getOwnReport.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| supplementReport | [V2-P1-027](Person_1/V2-P1-027_supplementReport.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| listCases | [V2-P1-028](Person_1/V2-P1-028_listCases.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| getCase | [V2-P1-029](Person_1/V2-P1-029_getCase.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| triageCase | [V2-P1-030](Person_1/V2-P1-030_triageCase.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| linkReports | [V2-P1-031](Person_1/V2-P1-031_linkReports.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| concludeCase | [V2-P1-032](Person_1/V2-P1-032_concludeCase.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| publishCase | [V2-P1-033](Person_1/V2-P1-033_publishCase.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| closeMixedCase | [V2-P1-034](Person_1/V2-P1-034_closeMixedCase.md) | P1-HF-01 / P2-HF-01 (report/case bổ sung) | P1 |
| listDefects | [V2-P1-035](Person_1/V2-P1-035_listDefects.md) | P1-32/P2-32 | P1 |
| createPreliminaryDefect | [V2-P1-036](Person_1/V2-P1-036_createPreliminaryDefect.md) | P1-32/P2-32 | P1 |
| getDefect | [V2-P1-037](Person_1/V2-P1-037_getDefect.md) | P1-32/P2-32 | P1 |
| assessDefect | [V2-P1-038](Person_1/V2-P1-038_assessDefect.md) | P1-32/P2-32 | P1 |
| verifyDefect | [V2-P1-039](Person_1/V2-P1-039_verifyDefect.md) | P1-32/P2-32 | P1 |
| assessRecurrence | [V2-P1-040](Person_1/V2-P1-040_assessRecurrence.md) | P1-32/P2-32 | P1 |
| setWorkOrder | [V2-P1-041](Person_1/V2-P1-041_setWorkOrder.md) | P1-60/P2-60 | P1 |
| getWorkOrder | [V2-P1-042](Person_1/V2-P1-042_getWorkOrder.md) | P1-60/P2-60 | P1 |
| createPolicyVersion | [V2-P1-043](Person_1/V2-P1-043_createPolicyVersion.md) | P1-HF-01 / P2-HF-01 (V2 policy bổ sung) | P1 |
| getPolicyVersion | [V2-P1-044](Person_1/V2-P1-044_getPolicyVersion.md) | P1-HF-01 / P2-HF-01 (V2 policy bổ sung) | P1 |
| activatePolicyVersion | [V2-P1-045](Person_1/V2-P1-045_activatePolicyVersion.md) | P1-HF-01 / P2-HF-01 (V2 policy bổ sung) | P1 |
| createInspectionTask | [V2-P1-046](Person_1/V2-P1-046_createInspectionTask.md) | P1-40/P2-40 | P1 |
| createInspectionBatch | [V2-P1-047](Person_1/V2-P1-047_createInspectionBatch.md) | P1-40/P2-40 | P1 |
| getInspectionTask | [V2-P1-048](Person_1/V2-P1-048_getInspectionTask.md) | P1-40/P2-40 | P1 |
| acceptInspectionTask | [V2-P1-049](Person_1/V2-P1-049_acceptInspectionTask.md) | P1-40/P2-40 | P1 |
| declineInspectionTask | [V2-P1-050](Person_1/V2-P1-050_declineInspectionTask.md) | P1-40/P2-40 | P1 |
| submitInspection | [V2-P1-051](Person_1/V2-P1-051_submitInspection.md) | P1-40/P2-40 | P1 |
| evaluateFastTrack | [V2-P1-052](Person_1/V2-P1-052_evaluateFastTrack.md) | P1-40/P2-40 | P1 |
| createRepairPackage | [V2-P1-053](Person_1/V2-P1-053_createRepairPackage.md) | P1-50/P2-50 | P1 |
| submitRepairPackage | [V2-P1-054](Person_1/V2-P1-054_submitRepairPackage.md) | P1-50/P2-50 | P1 |
| decideRepairItem | [V2-P1-055](Person_1/V2-P1-055_decideRepairItem.md) | P1-51/P2-51 | P1 |
| assignRepairItem | [V2-P1-056](Person_1/V2-P1-056_assignRepairItem.md) | P1-52/P2-52 | P1 |
| reassignRepairItem | [V2-P1-057](Person_1/V2-P1-057_reassignRepairItem.md) | P1-52/P2-52 | P1 |
| startRepairAttempt | [V2-P1-058](Person_1/V2-P1-058_startRepairAttempt.md) | P1-53/P2-53 | P1 |
| submitRepairAttempt | [V2-P1-059](Person_1/V2-P1-059_submitRepairAttempt.md) | P1-53/P2-53 | P1 |
| reviewRepairAttempt | [V2-P1-060](Person_1/V2-P1-060_reviewRepairAttempt.md) | P1-53/P2-53 | P1 |
| acceptApprovalAttempt | [V2-P1-061](Person_1/V2-P1-061_acceptApprovalAttempt.md) | P1-53/P2-53 | P1 |
| createEmergencyTask | [V2-P1-062](Person_1/V2-P1-062_createEmergencyTask.md) | P1-53/P2-53 | P1 |
| createSurveyTask | [V2-P2-011](Person_2/V2-P2-011_createSurveyTask.md) | P1-23/P2-23 | P2 |
| getSurveyTask | [V2-P2-012](Person_2/V2-P2-012_getSurveyTask.md) | P1-23/P2-23 | P2 |
| acceptSurveyTask | [V2-P2-013](Person_2/V2-P2-013_acceptSurveyTask.md) | P1-23/P2-23 | P2 |
| declineSurveyTask | [V2-P2-014](Person_2/V2-P2-014_declineSurveyTask.md) | P1-23/P2-23 | P2 |
| cancelSurveyTask | [V2-P2-015](Person_2/V2-P2-015_cancelSurveyTask.md) | P1-23/P2-23 | P2 |
| reassignSurveyTask | [V2-P2-016](Person_2/V2-P2-016_reassignSurveyTask.md) | P1-23/P2-23 | P2 |
| requestSurveySupplement | [V2-P2-017](Person_2/V2-P2-017_requestSurveySupplement.md) | P1-23/P2-23 | P2 |
| setSurveyAccessPoint | [V2-P2-018](Person_2/V2-P2-018_setSurveyAccessPoint.md) | P1-23/P2-23 | P2 |
| submitDataset | [V2-P2-019](Person_2/V2-P2-019_submitDataset.md) | P1-23/P2-23 | P2 |
| getDatasetCoverage | [V2-P2-020](Person_2/V2-P2-020_getDatasetCoverage.md) | P1-23/P2-23 | P2 |
| confirmBaseline | [V2-P2-021](Person_2/V2-P2-021_confirmBaseline.md) | P1-42/P2-42 | P2 |
| exportMission | [V2-P2-022](Person_2/V2-P2-022_exportMission.md) | P1-23/P2-23 | P2 |
| createUploadSession | [V2-P2-023](Person_2/V2-P2-023_createUploadSession.md) | P1-30/P2-30 | P2 |
| getUploadPartUrls | [V2-P2-024](Person_2/V2-P2-024_getUploadPartUrls.md) | P1-30/P2-30 | P2 |
| completeUpload | [V2-P2-025](Person_2/V2-P2-025_completeUpload.md) | P1-30/P2-30 | P2 |
| getUploadSession | [V2-P2-026](Person_2/V2-P2-026_getUploadSession.md) | P1-30/P2-30 | P2 |
| getFileMetadata | [V2-P2-027](Person_2/V2-P2-027_getFileMetadata.md) | P1-30/P2-30 | P2 |
| downloadFile | [V2-P2-028](Person_2/V2-P2-028_downloadFile.md) | P1-30/P2-30 | P2 |
| syncOperations | [V2-P2-029](Person_2/V2-P2-029_syncOperations.md) | P1-40/P2-40 | P2 |
| createProcessingJob | [V2-P2-030](Person_2/V2-P2-030_createProcessingJob.md) | P1-31/P2-31 | P2 |
| getProcessingJob | [V2-P2-031](Person_2/V2-P2-031_getProcessingJob.md) | P1-31/P2-31 | P2 |
| retryProcessingJob | [V2-P2-032](Person_2/V2-P2-032_retryProcessingJob.md) | P1-31/P2-31 | P2 |
| receiveAiResult | [V2-P2-033](Person_2/V2-P2-033_receiveAiResult.md) | P1-31/P2-31 | P2 |
| createValidationRun | [V2-P2-034](Person_2/V2-P2-034_createValidationRun.md) | P1-63/P2-63 | P2 |
| getValidationResult | [V2-P2-035](Person_2/V2-P2-035_getValidationResult.md) | P1-63/P2-63 | P2 |
| reviewTrainingLabel | [V2-P2-036](Person_2/V2-P2-036_reviewTrainingLabel.md) | P1-65/P2-65 | P2 |
| getDashboard | [V2-P2-037](Person_2/V2-P2-037_getDashboard.md) | P1-60/P2-60 | P2 |
| getProjectTimeline | [V2-P2-038](Person_2/V2-P2-038_getProjectTimeline.md) | P1-60/P2-60 | P2 |
| createExport | [V2-P2-039](Person_2/V2-P2-039_createExport.md) | P1-61/P2-61 | P2 |
| getExport | [V2-P2-040](Person_2/V2-P2-040_getExport.md) | P1-61/P2-61 | P2 |
| listAuditEvents | [V2-P2-041](Person_2/V2-P2-041_listAuditEvents.md) | P1-64/P2-64 | P2 |
| createModelVersion | [V2-P2-042](Person_2/V2-P2-042_createModelVersion.md) | P1-65/P2-65 | P2 |
| activateModelVersion | [V2-P2-043](Person_2/V2-P2-043_activateModelVersion.md) | P1-65/P2-65 | P2 |
| retireModelVersion | [V2-P2-044](Person_2/V2-P2-044_retireModelVersion.md) | P1-65/P2-65 | P2 |
| createDevice | [V2-P2-045](Person_2/V2-P2-045_createDevice.md) | P1-65/P2-65 | P2 |
| listDefectTypes | [V2-P2-046](Person_2/V2-P2-046_listDefectTypes.md) | P1-64/P2-64 | P2 |
| updateDefectType | [V2-P2-047](Person_2/V2-P2-047_updateDefectType.md) | P1-64/P2-64 | P2 |
| getReminderConfig | [V2-P2-048](Person_2/V2-P2-048_getReminderConfig.md) | P1-64/P2-64 | P2 |
| setReminderConfig | [V2-P2-049](Person_2/V2-P2-049_setReminderConfig.md) | P1-64/P2-64 | P2 |
| requestDeletion | [V2-P2-050](Person_2/V2-P2-050_requestDeletion.md) | P1-62/P2-62 | P2 |
| decideDeletion | [V2-P2-051](Person_2/V2-P2-051_decideDeletion.md) | P1-62/P2-62 | P2 |
| setLegalHold | [V2-P2-052](Person_2/V2-P2-052_setLegalHold.md) | P1-62/P2-62 | P2 |
| listMyInspectionTasks | [V2-P1-063](Person_1/V2-P1-063_listMyInspectionTasks.md) | P1-40/P2-40 | P1 |
| listMyRepairItems | [V2-P1-064](Person_1/V2-P1-064_listMyRepairItems.md) | P1-53/P2-53 | P1 |
| listMySurveyTasks | [V2-P2-053](Person_2/V2-P2-053_listMySurveyTasks.md) | P1-23/P2-23 | P2 |
| getInspectionSnapshot | [V2-P1-065](Person_1/V2-P1-065_getInspectionSnapshot.md) | P1-40/P2-40 | P1 |
| setDefectSlabLinks | [V2-P1-066](Person_1/V2-P1-066_setDefectSlabLinks.md) | P1-32/P2-32 | P1 |
| createSurveyPlan | [V2-P2-054](Person_2/V2-P2-054_createSurveyPlan.md) | P1-22/P2-22 | P2 |
| postponeSurveyPlan | [V2-P2-055](Person_2/V2-P2-055_postponeSurveyPlan.md) | P1-22/P2-22 | P2 |
| exportApprovedLabels | [V2-P2-056](Person_2/V2-P2-056_exportApprovedLabels.md) | P1-65/P2-65 | P2 |
| listAdminJobs | [V2-P2-057](Person_2/V2-P2-057_listAdminJobs.md) | P1-65/P2-65 | P2 |
| getAsyncJob | [V2-P2-058](Person_2/V2-P2-058_getAsyncJob.md) | P1-31/P2-31 | P2 |
| getRepairItem | [V2-P1-067](Person_1/V2-P1-067_getRepairItem.md) | P1-53/P2-53 | P1 |
| getRepairAttempt | [V2-P1-068](Person_1/V2-P1-068_getRepairAttempt.md) | P1-53/P2-53 | P1 |
| getRepairPackage | [V2-P1-069](Person_1/V2-P1-069_getRepairPackage.md) | P1-50/P2-50 | P1 |
| getSegmentSet | [V2-P2-059](Person_2/V2-P2-059_getSegmentSet.md) | P1-20/P2-20 | P2 |
| getDeletionRequest | [V2-P2-060](Person_2/V2-P2-060_getDeletionRequest.md) | P1-62/P2-62 | P2 |
| getModelVersion | [V2-P2-061](Person_2/V2-P2-061_getModelVersion.md) | P1-64/P2-64 | P2 |
| getNotification | [V2-P2-062](Person_2/V2-P2-062_getNotification.md) | P1-64/P2-64 | P2 |
| reviseRepairItem | [V2-P1-070](Person_1/V2-P1-070_reviseRepairItem.md) | P1-51/P2-51 | P1 |
| startReworkAttempt | [V2-P1-071](Person_1/V2-P1-071_startReworkAttempt.md) | P1-53/P2-53 | P1 |
