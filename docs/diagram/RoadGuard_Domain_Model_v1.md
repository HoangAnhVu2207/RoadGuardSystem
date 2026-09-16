# RoadGuard — Domain Model v1

Dựa trên `RoadGuard_Entity_List_v2.md`. Mục tiêu: xác định ranh giới giao dịch nhất quán (aggregate),
quy tắc bất biến (invariant), và các quy tắc **liên-aggregate** cần domain service thực thi thay vì FK.

Ký hiệu: **[R]** = Aggregate Root. Entity con liệt kê thụt vào dưới root, không có ID truy cập độc lập từ bên ngoài aggregate.

## Quyết định thiết kế đã chốt theo yêu cầu người dùng

Sáu điểm dưới đây là quyết định thiết kế đã chốt. Khi có khác biệt với các "Điểm mở" cũ trong tài liệu này, các quyết định này được ưu tiên:

1. Giữ riêng `PasswordResetLog` và `AccountStatusChangeLog`.
2. `Warranty` luôn là entity/aggregate riêng, hỗ trợ nhiều giai đoạn bảo hành trong một dự án; không nhúng thành field đơn trong `HandoverDocument`.
3. `QualityCheck` được hỗ trợ ở cấp `SurveyFile` và `SurveyDataVersion`, đồng thời phân biệt Drone App kiểm tra sơ bộ với Backend/System Worker kiểm tra chính thức; PM chỉ xem kết quả và quyết định bay bổ sung.
4. `SupplementarySurveyRequest` không thuộc aggregate `Survey`; là aggregate độc lập.
5. Phát hiện AI được PM giữ lại phải tạo `Defect OPEN` (Preliminary Defect), bắt buộc giao Repair Crew đo thực địa; chỉ sau khi PM chấp nhận kết quả đo mới được chuyển `VERIFIED` và đưa vào đợt sửa.
6. Khi lập đợt sửa, PM chỉ nhập chi phí dự kiến cho từng lỗi; không quản lý biện pháp, vật liệu, khối lượng hoặc ưu tiên trên `RepairItem`.

Mã truy vết: `UD-01` (audit log riêng), `UD-02` (Warranty), `UD-03` (QualityCheck hai cấp), `UD-04` (SupplementarySurveyRequest độc lập), `UD-05` (đo thực địa bắt buộc trước xác minh chính thức), `UD-06` (RepairItem chỉ nhập chi phí). Ma trận nguồn và cách hiện thực nằm trong Entity List.

## Yêu cầu nghiên cứu bắt buộc từ đề cương

Đề cương `RoadGuard_Contractor_Warranty_Inspection_phuonglhk.md` là nguồn yêu cầu bổ sung cho **Research Validation Track**, không phải User Story sản phẩm MVP. Track này bắt buộc phải có ground truth thực địa cho một mẫu depression/slab faulting và đối chiếu với số đo từ drone/surface model để tính sai số và measurement uncertainty.

- Có thể ghi nhận ngoài app bằng Excel/giấy trong đợt thực địa.
- Trước khi phân tích, dữ liệu phải được chuẩn hóa thành các aggregate nghiên cứu có `sample_id`, liên kết survey/road section version, người đo, dụng cụ, thời điểm, vị trí, giá trị, đơn vị và bằng chứng.
- Không dùng Research Validation Track để tự động kết luận trách nhiệm bảo hành. Workflow TN01–TN12/AI13 là phần bắt buộc của sản phẩm hiện tại và dùng chung cấu trúc số đo với Research Validation nhưng có mục đích, phân quyền và state machine riêng.

---

## 1. Auth & Access

### `User` [R]
- Không có entity con transactional.
- **Invariant:**
  - `role` ∈ {Supervisor, PM, DroneOperator, RepairCrew}, không đổi tùy tiện (US-01 mục 3: "không thể tự đổi vai trò").
  - `status = Suspended` → không đăng nhập được, không đặt lại mật khẩu được (US-01 mục 6).
  - Đặt lại mật khẩu → bắt buộc đổi mật khẩu ở lần đăng nhập kế tiếp (US-01 mục 6).
- **Domain Events:** `UserSuspended` (kích hoạt tạo danh sách bàn giao việc — quy tắc 17), `UserPasswordReset` (kích hoạt thu hồi toàn bộ Session).

### `Session` [R]
- Tham chiếu `user_id`.
- **Invariant:** phải bị thu hồi khi logout, hết hạn, hoặc khi `User.PasswordReset`/`Suspended` xảy ra (US-01 mục 2, mục 6) — **cross-aggregate: Session phải lắng nghe event từ User**.

### `Notification` [R]
- Tham chiếu `recipient_user_id` + `source_entity_type/id` (đọc, không phải nghiệp vụ nên polymorphic ref chấp nhận được ở đây, khác với `Evidence`).
- **Invariant:** chỉ tạo cho 7 loại sự kiện liệt kê ở US-01 mục 5 (khảo sát, xử lý, duyệt, trả sửa, từ chối/hủy, phân công lại, sắp hết hạn bảo hành).

### `PasswordResetLog` [R]
- Ghi nhận riêng từng lần đặt lại mật khẩu, tham chiếu `target_user_id` và `performed_by_user_id`.
- Là audit record độc lập theo yêu cầu thiết kế; không gộp vào `AuditLog`.
- **Fields audit bắt buộc:** `target_user_id`, `performed_by_user_id`, `occurred_at`, `reason`, `result`, `source`, `correlation_id`.
- **Security invariant:** không bao giờ lưu mật khẩu, token hoặc dữ liệu bí mật; chỉ lưu người thực hiện, thời điểm và kết quả.

### `AccountStatusChangeLog` [R]
- Ghi nhận riêng các lần ngừng/mở tài khoản và việc bàn giao việc liên quan, tham chiếu `target_user_id` và `changed_by_user_id`.
- Là audit record độc lập theo yêu cầu thiết kế; `AuditLog` chung không thay thế entity này.
- **Fields audit bắt buộc:** `target_user_id`, `changed_by_user_id`, `occurred_at`, `from_status`, `to_status`, `reason`, `source`, `correlation_id`.
- Nếu phát sinh bàn giao việc, lưu thêm `handover_reference` tới danh sách bàn giao; không xóa hoặc ghi đè bản ghi cũ.

---

## 2. Project

### `Project` [R]
- Entity con: `ProjectMember` (list), `HandoverDocument`.
- **Invariant:**
  - `project_code` duy nhất (US-03 mục 1).
  - Đúng 1 `ProjectMember` với `role = PM` đang active tại một thời điểm (US-03 mục 5, quy tắc 2 Use Case).
  - `status = Closed` → chặn tác nghiệp thông thường, giữ nguyên lịch sử (US-03 mục 7).
- **Domain Events:** `ProjectMemberReassigned` (kích hoạt `Notification`).

### `Warranty` [R]
- Aggregate riêng, bắt buộc có `project_id` và có thể có `road_section_id` khi bảo hành gắn với một đoạn đường cụ thể.
- **Fields nghiệp vụ:** `handover_date`, `warranty_start_date`, `warranty_end_date`, `retained_value`, `scope`, `source_document_id`.
- Một `Project` có thể có nhiều `Warranty` cho các giai đoạn/thời hạn/phạm vi khác nhau.
- Không được biểu diễn bằng một field đơn trong `HandoverDocument`; `HandoverDocument` chỉ quản lý hồ sơ bàn giao.

### `RoadSection` [R]
- Entity con: `RoadSectionVersion` (list, đúng 1 bản "current").
- **Invariant:** sửa hình học → tạo `RoadSectionVersion` mới, giữ bản cũ; **dữ liệu cũ (Survey, Defect) không tự gắn sang hình học mới** (US-03 mục 3).
- **⚠️ Hệ quả thiết kế quan trọng:** vì invariant trên, mọi entity lưu vị trí trên đoạn đường (`Survey`, `Defect`) phải tham chiếu `road_section_version_id` cụ thể tại thời điểm tạo — **không** chỉ tham chiếu `road_section_id`. Nếu chỉ dùng `road_section_id`, sửa hình học sẽ vô tình "kéo" dữ liệu cũ sang hình học mới, vi phạm đúng quy tắc mà US-03 mục 3 cấm.

---

## 3. Survey

### `SurveyPlan` [R]
- Entity con: `SurveyPlanPostponement` (list, append).
- **Invariant:** hoãn kế hoạch không hủy yêu cầu khảo sát đã tạo — nghĩa là **`SurveyPlan` và `SurveyRequest` là 2 aggregate độc lập**, chỉ liên kết lỏng qua `project_id` (US-04 mục 3, US-05 mục 6).

### `SurveyRequest` [R]
- Entity con: `SurveyAssignment` (trạng thái phân công hiện tại + lịch sử).
- **Invariant (state machine chính, US-05):**
  - Operator chỉ được từ chối khi trạng thái = `Mới giao`; đã `Đã nhận` thì không tự từ chối được (mục 3).
  - Phân công lại → lưu người cũ/mới/lý do (mục 4).
  - **[Cross-aggregate]** Chỉ hủy được khi **chưa** có `SurveyDataVersion` được server xác nhận toàn vẹn (mục 5) — domain service phải query aggregate `SurveyDataVersion` trước khi cho phép hủy.
- **Domain Events:** `SurveyRequestAssigned`, `SurveyRequestRejected`, `SurveyRequestCancelled`.

### `SupplementarySurveyRequest` [R]
- Aggregate độc lập cho yêu cầu khảo sát/bay bổ sung; không phải entity con của `Survey`.
- Có thể tham chiếu `survey_id` để chỉ khảo sát phát sinh yêu cầu và tham chiếu `survey_request_id` nếu được tạo từ một yêu cầu khảo sát hiện hữu.
- Vòng đời, phê duyệt và phân công của yêu cầu bổ sung không làm thay đổi ownership của aggregate `Survey`.

### `Survey` [R] *(entity mới từ review vòng 2)*
- Entity con: `Flight` (list), `SurveyFile` (list).
- Field: `survey_type` (ORIGINAL/PERIODIC/SUPPLEMENTARY), `is_baseline_confirmed`, `road_section_version_id`.
- **Invariant:**
  - `is_baseline_confirmed = true` chỉ được set qua hành động PM xác nhận riêng (US-04 mục 5), **không** tự động dù dữ liệu đã xử lý xong.
  - **[Cross-aggregate]** Xác nhận baseline bị chặn nếu dữ liệu chưa toàn vẹn/thiếu định vị, `ProcessingJob` chưa hoàn tất, hoặc còn phát hiện chưa được PM xử lý dứt điểm. Mỗi phát hiện phải bị loại có lý do hoặc ánh xạ tới `Defect`; mọi `Defect OPEN` phải hoàn tất `FieldInspectionTask` và chuyển `VERIFIED`/`REJECTED` trước khi xác nhận baseline.

### `SurveyDataVersion` [R]
- Tham chiếu `survey_id`.
- **Invariant:** chuyển `Đã đồng bộ an toàn` chỉ khi server xác nhận đủ tệp + checksum pass (US-02 mục 4, US-06 mục 6).
- Chỉ Backend/System Worker được chuyển `status = SERVER_CONFIRMED`; Drone Operator và PM không được tự xác nhận version.
- **Domain Events:** `SurveyDataConfirmed` → kích hoạt tạo `ProcessingBlock`/`ProcessingJob` (aggregate khác).

### `FieldInspectionTask` [R]
- Aggregate nghiệp vụ cho nhiệm vụ PM bắt buộc giao Repair Crew đo một `Defect OPEN`.
- Entity con: `FieldInspectionAssignment` (lịch sử giao, từ chối và bàn giao; đúng một assignment active tại một thời điểm).
- Tham chiếu `project_id`, `defect_id`, `survey_id`, `road_section_version_id`, PM giao việc, yêu cầu đo, phạm vi, thời hạn và Repair Crew hiện tại.
- **State machine:** `NEW_ASSIGNED` → `ACCEPTED` hoặc `REJECTED`; `ACCEPTED` → `IN_PROGRESS` → `SUBMITTED`; PM đánh giá `SUBMITTED` thành `SUPPLEMENT_REQUIRED` hoặc `COMPLETED`. Khi bổ sung, tạo session/phép đo mới và quay lại `IN_PROGRESS`, không ghi đè dữ liệu cũ.
- **Invariant:** một `Defect OPEN` phải có ít nhất một task chưa bị thay thế; Repair Crew chỉ được từ chối trước khi nhận; sau khi nhận, PM thực hiện điều chuyển.
- Quyết định cuối của PM là `DEFECT_CONFIRMED` hoặc `NO_DEFECT`; quyết định, người, thời điểm và lý do phải được lưu trước khi task `COMPLETED`.

### `FieldInspectionSession` [R]
- Đại diện một phiên đo hiện trường dùng cho workflow sản phẩm hoặc Research Validation; `purpose` ∈ {`DEFECT_VERIFICATION`, `RESEARCH_VALIDATION`}.
- Với `DEFECT_VERIFICATION`, bắt buộc tham chiếu `field_inspection_task_id`; người đo phải là Repair Crew thuộc assignment hiện hành.
- Với `RESEARCH_VALIDATION`, `field_inspection_task_id` phải null và có thể nhập từ Excel/giấy theo quy trình nghiên cứu.
- **Invariant:** session phải ghi thời điểm, điều kiện hiện trường, người đo, phương pháp, bằng chứng và danh sách mẫu; session đã gửi/khóa không update-in-place.

### `GroundTruthMeasurement` [R]
- Aggregate độc lập cho một phép đo vật lý tại một `sample_id` duy nhất.
- `measurement_type` tối thiểu gồm `DEPRESSION_DEPTH` và `SLAB_FAULTING_HEIGHT`; có thể thêm `SHOULDER_EROSION_EXTENT` khi thực hiện giả thuyết nghiên cứu.
- Bắt buộc có `field_inspection_session_id`, `road_section_version_id`, vị trí, giá trị, đơn vị, dụng cụ, phương pháp, người đo, thời điểm và bằng chứng.
- Với session `DEFECT_VERIFICATION`, `defect_id` bắt buộc trỏ đúng `Defect OPEN` của task. Với `RESEARCH_VALIDATION`, `defect_id` có thể null để không biến sample nghiên cứu thành Defect sản phẩm.

### `DerivedMeasurement` [R]
- Aggregate cho số đo do DSM/surface model hoặc pipeline xử lý tạo ra để ghép với ground truth.
- Tham chiếu `survey_data_version_id`, `road_section_version_id`, tùy chọn `defect_id`, `measurement_type`, giá trị, đơn vị, uncertainty và phiên bản thuật toán/model.
- Immutable sau khi công bố; chạy lại pipeline tạo bản ghi/version mới, không ghi đè kết quả cũ.

### `MeasurementValidationRun` [R]
- Đại diện một lần phân tích đối chiếu cho một tập mẫu và một phương pháp/model cụ thể.
- Lưu phạm vi mẫu, `derived_measurement_source`, tiêu chí loại outlier, số mẫu hợp lệ, bias, MAE, RMSE, độ không chắc chắn và phương pháp tính.
- Kết quả chỉ là bằng chứng đánh giá khoa học; không tự chuyển trạng thái Defect hoặc kết luận thuộc/ngoài Warranty.

### `MeasurementValidationSample`
- Entity con của `MeasurementValidationRun`, ghép đúng một `GroundTruthMeasurement` với đúng một `DerivedMeasurement`.
- Lưu sai số có dấu, sai số tuyệt đối, trạng thái sử dụng (`INCLUDED`, `EXCLUDED`, `OUTLIER`) và lý do loại mẫu nếu có.

### `QualityCheck` [R]
- Aggregate độc lập, có `scope` phân biệt `SURVEY_FILE` và `SURVEY_DATASET`.
- `execution_stage = CLIENT_PRECHECK`: Drone App kiểm tra sơ bộ định dạng, định vị, thời gian, độ rõ/ánh sáng và cảnh báo vùng phủ trước hoặc trong khi tải; kết quả này không xác nhận toàn vẹn máy chủ.
- `execution_stage = SERVER_VALIDATION`: Backend/System Worker kiểm tra MIME thực, kích thước, malware, checksum, quan hệ nguồn, completeness, coverage/overlap và tính nhất quán; đây là kết quả chính thức dùng để chặn/mở xử lý.
- `SURVEY_FILE` dùng cho định dạng, định vị, đồng bộ của từng `SurveyFile`.
- `SURVEY_DATASET` dùng cho vùng phủ, chồng lấn và các kiểm tra cần toàn bộ dữ liệu; **neo canonical vào `survey_data_version_id`**. `SurveyDataVersion` tham chiếu `survey_id`, nên không cần lưu thêm `survey_id` trên `QualityCheck`.
- Nếu cần kiểm tra trước khi nộp, tạo `SurveyDataVersion` ở trạng thái nháp trước khi chạy kiểm tra; chỉ bản đã server-confirm mới được chuyển sang xử lý.
- Ràng buộc dữ liệu: `scope = SURVEY_FILE` → đúng `survey_file_id`; `scope = SURVEY_DATASET` → đúng `survey_data_version_id`; không được có cả hai hoặc không có đích.
- PM chỉ đọc/tổng hợp `QualityCheck` và tạo `SupplementarySurveyRequest` khi cần; PM không đổi kết quả kỹ thuật từ `FAILED` thành `PASSED`.

---

## 4. Processing

### `ProcessingBlock` [R]
- Tham chiếu `survey_data_version_id`. Backend tự chia khối để giới hạn tài nguyên — **khác `RoadSection`** (quy tắc 4 Use Case).
- Quan hệ: 1 `SurveyDataVersion` → N `ProcessingBlock` → mỗi Block có 1 `ProcessingJob` hiện hành.

### `ProcessingJob` [R]
- Tham chiếu `processing_block_id`, `model_version_id`.
- Entity con: `ProcessingAttempt` (list).
- **Invariant:** phải phân biệt lỗi hạ tầng (retry được trên dữ liệu nguyên vẹn, tạo `ProcessingAttempt` mới) và lỗi dữ liệu (không dùng retry, chuyển PM quyết định bổ sung — US-07 mục 4-5, QT08 mục 5). Đây là **business rule dễ bị agent code sai nhất** trong nhóm Processing nếu không tách rõ 2 loại lỗi ngay từ enum `error_type` trên `ProcessingAttempt`.

### `AIModelVersion` [R] *(config, Admin quản lý)*
- **Invariant:** đổi phiên bản mô hình **không** cascade update lên `AIDetection.model_version_id` cũ — tham chiếu đó bất biến vĩnh viễn (QT06 mục 2).

---

## 5. AI & Defect

### `AIDetection` [R]
- **Immutable** sau khi tạo — không có action update/delete trong domain model này.
- Tham chiếu `processing_job_id`, `model_version_id`.
- MVP có thể giữ field ước lượng 2D: `estimated_width`, `estimated_length`, `is_2d_estimate = true`.
- Số đo vật lý và số đo surface model phục vụ đề cương không đặt trực tiếp vào `AIDetection`; dùng `GroundTruthMeasurement` và `DerivedMeasurement` để giữ nguồn gốc, uncertainty và cặp validation.

### `Defect` [R]
- Entity con: `DefectVerificationLog` (list, append).
- **Invariant:**
  - Khi PM giữ lại hoặc hiệu chỉnh một `AIDetection`, tạo `Defect.status = OPEN`; đây là `Preliminary Defect`, chưa phải hư hỏng chính thức.
  - PM có thể loại `AIDetection` rõ ràng sai ngay ở bước rà soát mà không tạo `Defect`; quyết định được ghi log nhắm tới detection. Khi đã tạo `Defect OPEN`, chỉ được chuyển `VERIFIED` hoặc `REJECTED` sau khi `FieldInspectionTask` hoàn tất.
  - Chuyển `OPEN` → `VERIFIED` chỉ khi task có kết luận `DEFECT_CONFIRMED`, có session đã gửi/khóa và ít nhất một phép đo/bằng chứng hợp lệ. Chuyển `OPEN` → `REJECTED` sau đo phải có kết luận `NO_DEFECT`.
  - Mọi sửa loại/mức độ/vị trí → lưu trước/sau trong `DefectVerificationLog` (US-08 mục 3).
  - **Không** tự chuyển trạng thái "Đã loại bỏ" chỉ vì kích thước nhỏ hoặc confidence thấp — phải có hành động PM tường minh (US-08 mục 5).
  - `DefectVerificationLog` phải nhắm đúng một trong `ai_detection_id` hoặc `defect_id`. `action` phân biệt `PRELIMINARY_KEEP`, `ADJUST`, `CONFIRM`, `REJECT`, `MERGE`; log `CONFIRM` và log `REJECT` từ trạng thái `OPEN` phải nhắm `Defect` và tham chiếu `field_inspection_task_id`.
  - Severity tính theo `SeverityRuleVersion` tại thời điểm xác minh chính thức; lưu snapshot `severity_rule_version_id` trên `DefectVerificationLog`, không tính lại khi rule đổi version.

### `DefectMergeDecision`
- Ghi lại hành động gộp/giữ riêng — **thao tác thực sự (merge 2 Defect thành 1, giữ liên kết AIDetection nguồn)** nên coi là 1 **operation trong `Defect` aggregate**, không phải aggregate riêng có logic riêng (US-09 mục 1-2).

### `DefectMatch` [R]
- Tham chiếu `defect_id` (A), `defect_id` (B, kỳ khác), `match_confidence`, `match_method`, `reviewed_by`.
- Không cần strong consistency với `Defect` — chỉ là liên kết đối sánh, aggregate độc lập nhẹ.

### `SeverityRuleVersion` [R] *(config, versioned)*
### `TrainingLabelApproval` [R], `TrainingDatasetExport` [R] *(workflow riêng, độc lập)*

---

## 6. Repair

### `RepairBatch` [R]
- Field: `current_version_id` (con trỏ tới `RepairBatchVersion` hiện hành). Bản thân `RepairBatch` là aggregate "mỏng" — chỉ giữ định danh + trỏ tới version hiện tại.

### `RepairBatchVersion` [R] *(aggregate nặng nhất hệ thống)*
- Entity con: `RepairItem` (list — **copy mới khi tạo version mới, không share với version cũ**), `RepairApprovalDecision` (list, per item).
- **Invariant:**
  - **[Cross-aggregate]** Tạo bản nháp → chặn nếu `Defect` được chọn đã thuộc một `RepairBatchVersion` khác đang ở trạng thái "đang thực hiện" (SC01 mục 1: "chặn lỗi bị giao trùng") — phải query cross-aggregate trước khi thêm `RepairItem`.
  - **[Cross-aggregate]** Chỉ tạo `RepairItem` khi `Defect.status = VERIFIED` và tồn tại `FieldInspectionTask COMPLETED` có quyết định `DEFECT_CONFIRMED`; chặn `OPEN`, `REJECTED`, task chưa hoàn tất hoặc đang yêu cầu bổ sung.
  - Mỗi `RepairItem` chỉ lưu `defect_id`, `estimated_cost` và trạng thái hệ thống; không lưu biện pháp, vật liệu, khối lượng hoặc ưu tiên. Tổng dự toán là tổng `estimated_cost` của các item trong version (UD-06).
  - Trình → khóa version, chuyển `Chờ duyệt`, snapshot toàn bộ danh sách lỗi/chi phí tại thời điểm trình (SC03).
  - Duyệt → chỉ version hiện tại chuyển `Đã duyệt`; **chỉ version này** đủ điều kiện phân công (SC04).
  - Trả chỉnh sửa → **không** tự triển khai phần lỗi đã được chấp thuận riêng lẻ (SC05) — toàn đợt quay về PM cùng lúc.
  - Trình lại → tạo version mới, giữ nguyên version cũ, yêu cầu duyệt lại **toàn bộ** đợt, không duyệt từng phần (SC06).

### `RepairAssignment` [R]
- **Invariant:**
  - Chỉ `RepairBatchVersion.status = Đã duyệt` (và là version hiện tại) mới cho phép phân công (US-12 mục 1).
  - Thay đổi người được phân công không làm thay đổi version đã duyệt. Nếu thay đổi danh sách lỗi hoặc chi phí thì bắt buộc tạo `RepairBatchVersion` mới (US-12 mục 5, UD-06).

### `RepairProgress` [R], `RepairEvidence` [R] *(append-only, không ghi đè bản gốc — US-13 mục 5)*
### `RepairInspectionResult` [R]
- **Invariant:** 1 lỗi "Đạt" **không** bị kéo lùi trạng thái chỉ vì lỗi khác trong cùng đợt "Chưa đạt" — mỗi `RepairItem` có trạng thái nghiệm thu độc lập (US-14 mục 2).

### `UnplannedDefectReport` [R]
- **Invariant:** phát hiện lỗi ngoài phạm vi tại hiện trường → tạo báo cáo riêng, **không** tự thêm vào `RepairBatchVersion` đã duyệt (US-13 mục 6, quy tắc 14 Use Case) — phải qua flow xác minh/phê duyệt phạm vi riêng (có thể dẫn tới `Defect` mới + `RepairBatchVersion` mới sau này, ngoài phạm vi domain model v1 này).

---

## 7. Evidence / File / Audit

### `File` [R], `Evidence` [R]
- `Evidence`: cột FK nullable riêng từng loại đích (`defect_id`, `repair_item_id`, `handover_document_id`...) + CHECK constraint đúng 1 cột non-null. Append-only.

### `AuditLog`
- **Không phải business aggregate** — là event sink chung, nhận domain event từ **mọi** aggregate khác. **Fields audit chuẩn:** `actor_user_id`, `occurred_at`, `event_type`, `entity_type`, `entity_id`, `before_snapshot`, `after_snapshot`, `reason`, `source`, `correlation_id`.
- Append-only, chỉ đọc; không lưu mật khẩu/token. `PasswordResetLog` và `AccountStatusChangeLog` vẫn được lưu riêng; `AuditLog` không thay thế chúng.

### `ReportExport` [R]

---

## 8. Administration & Retention

### `ReminderRule` [R] *(config)*
### `DataRetentionRequest` [R]
- **Invariant [Cross-aggregate]:** chặn lập/duyệt xóa nếu tồn tại `LegalHold` active trên phạm vi liên quan (US-19 mục 3) — phải query aggregate `LegalHold` trước khi duyệt.
### `LegalHold` [R]
### `RetentionDeletionLog` [R] *(compliance record — giữ riêng khỏi AuditLog vì có ý nghĩa pháp lý khác biệt)*

---

## Tổng hợp: Cross-Aggregate Invariants (quan trọng nhất cho AI agent)

Đây là các quy tắc **FK/schema không tự enforce được** — bắt buộc có domain service/application-layer check, nếu bỏ sót thì DB vẫn "hợp lệ" nhưng sai nghiệp vụ:

| # | Quy tắc | Aggregate cần đọc chéo | Nguồn |
|---|---|---|---|
| 1 | `Survey`/`Defect` phải neo vào `RoadSectionVersion` cụ thể, không phải `RoadSection` chung | `RoadSection` | US-03 mục 3 |
| 2 | Hủy `SurveyRequest` bị chặn nếu `SurveyDataVersion` đã server-confirm | `SurveyDataVersion` | US-05 mục 5 |
| 3 | Xác nhận baseline cần `SurveyDataVersion` toàn vẹn, `ProcessingJob` hoàn tất và không còn `Defect OPEN`/phát hiện chưa xử lý | `Survey`, `ProcessingJob`, `Defect`, `FieldInspectionTask` | US-04 mục 6, UD-05 |
| 4 | Tạo `RepairItem` chỉ cho `Defect VERIFIED` có task đo hoàn tất/kết luận `DEFECT_CONFIRMED`, đồng thời chặn lỗi đã thuộc đợt khác đang thực hiện | `Defect`, `FieldInspectionTask`, `RepairBatchVersion` | SC01, US-11, UD-05 |
| 5 | `RepairAssignment` yêu cầu `RepairBatchVersion.status = Đã duyệt` | `RepairBatchVersion` | US-12 mục 1 |
| 6 | Thay đổi danh sách lỗi hoặc chi phí đã duyệt → chặn, bắt buộc version mới | `RepairBatchVersion` | US-12 mục 5, UD-06 |
| 7 | Duyệt `DataRetentionRequest` chặn nếu có `LegalHold` active | `LegalHold` | US-19 mục 3 |
| 8 | `Session` phải bị thu hồi khi `User` bị suspend hoặc reset password | `User` | US-01 mục 2, 6 |
| 9 | Ground truth nghiên cứu phải ghép đúng mẫu với số đo derived trước khi tính uncertainty | `FieldInspectionSession`, `GroundTruthMeasurement`, `DerivedMeasurement`, `MeasurementValidationRun` | Đề cương, RS01–RS06 |
| 10 | Research validation không tự tạo Defect hoặc kết luận trách nhiệm Warranty | `MeasurementValidationRun`, `Defect`, `Warranty` | Đề cương; tách mục đích bằng `FieldInspectionSession.purpose` |
| 11 | `Defect OPEN` chỉ chuyển `VERIFIED`/`REJECTED` sau khi PM đánh giá task đo; session/phép đo đã gửi không bị ghi đè | `Defect`, `FieldInspectionTask`, `FieldInspectionSession`, `GroundTruthMeasurement` | AI13, TN01-TN06, US-20, UD-05 |

---

## Trạng thái quyết định

Các quyết định ở đầu tài liệu đã được chốt theo hệ thống hiện tại. Workflow đo thực địa TN01–TN12/AI13 thuộc MVP sản phẩm; Research Validation là nhánh mục đích riêng nhưng dùng chung cấu trúc phiên/phép đo có kiểm soát.
