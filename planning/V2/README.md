# RoadGuard — kế hoạch giao việc theo API V2(3)

Bản phân công nền ngày 2026-09-27: **133 task = 133 operation**, Person 1 (anh): **71**, Person 2 (huy): **62**. V2(3) giữ các ID này làm baseline, không coi 133 là trần. Mỗi task có một owner chịu trách nhiệm toàn bộ endpoint và tự test được API đó. Số task không biểu thị cùng khối lượng; chưa có ước lượng đáng tin cậy trước khi đọc source.

## Overlay rà soát 2026-09-28

Đọc [decision register V2(3)](V2-3_DECISION_REGISTER.md) trước khi nhận task. Register áp dụng D01-D28, tách `contractStatus`, `implementationStatus`, `verificationStatus`, `dependencyType` và `workstream`, đồng thời ghi backlog ngoài 133. Đây là **overlay planning**, không phải ZIP đầy đủ docs; canonical design nằm ở `docs/diagram/V2`.

## Cách đưa vào dự án

Gói ZIP có hai thư mục gốc `planning/` và `docs/`. Giải nén ra thư mục tạm, đối chiếu rồi chép vào root dự án. Docs nằm đúng `docs/diagram/V2`; hai plan nằm trong `planning/`. Bản cũ nguyên văn được giữ trong `planning/V2/history/`. Không xóa docs/diagram cũ hoặc source hiện tại. Nếu đã sửa docs V2 sau REVIEW-01, merge chứ không ghi đè.

## Đọc và nhận task

1. Mỗi người mở plan của mình: [Person 1](../RoadGuard_Plan_Person_1.md), [Person 2](../RoadGuard_Plan_Person_2.md).
2. Chọn task theo dependency, đọc contract và source ứng viên; ghi trạng thái `REUSE_VERIFIED`, `IN_PROGRESS`, `PARTIAL`, `BLOCKED` hoặc `DONE` có bằng chứng. Ban đầu toàn bộ task mới là NEEDS_REPO_CHECK, có thể kèm BLOCKED_SLICE.
3. Hoàn thành một API từ controller → service → repository → SQL → HTTP smoke theo ADR 006. Owner task được sửa toàn bộ lát cắt cần thiết, kể cả entity/mapping/migration khi scope đã duyệt yêu cầu; không tạo task persistence thứ hai cho cùng endpoint.
4. Ghi thiếu contract vào [coverage và gap](COVERAGE_AND_GAPS.md), không tự biến giả định thành rule.

## Thay đổi so với plan cũ

P1 trước đây chủ yếu API/service, P2 chủ yếu persistence. Theo ADR 006, kế hoạch mới giao dọc theo từng API: owner được sửa mọi lớp cần thiết của endpoint mình nhận, bao gồm persistence/schema/migration khi scope đã duyệt yêu cầu. Đây là ownership giao việc, không thay kiến trúc `Controller -> IService -> IRepository`. Giữ nguyên history Done; không yêu cầu làm lại. Các task hạ tầng cũ không bị đổi thành API giả và vẫn ở checklist release.

Cây thư mục chỉ chứng minh tên file hiện diện, không chứng minh route, hành vi hoặc test đang pass. Các DTO/road section/legacy auth đã có phải được đối chiếu với contract draft. Đặc biệt `auth_unauthorized` và code uppercase, PagedResponse và cursor, road-section và route-version không được đổi phá tương thích âm thầm. Khi khác nhau: ghi current/proposed, ảnh hưởng FE, adapter/migration/versioning và người duyệt; giữ runtime hiện tại cho tới khi delta được chốt.

## Ownership và tránh xung đột

Person 1: auth/accounts, project/membership, report/case/defect, policy/inspection/repair. Person 2: spatial route/segment, survey/dataset, upload/file, sync, processing/research, analytics/export, catalog/notification/retention và vận hành tương ứng.

Mỗi API một owner; controller chung, DTO/common errors, DI, DbContext, mapping, model snapshot, migrations, seed và OpenAPI là shared hotspots. Trước sửa, ghi reservation trong worklog (file, task, owner, commit). Hai người không sửa cùng hotspot hoặc migration snapshot đồng thời. P2 điều phối thứ tự migrations/SQL integration/seed/Docker/CI/release evidence; P1 điều phối auth/business contract; người điều phối không thay trách nhiệm owner API. Merge migration của task trước rồi rebase task sau; không sửa/xóa migration đã áp dụng và không áp migration vào database live chỉ vì được phép tạo migration.

## Thứ tự làm

| Đợt | Trọng tâm | Kết quả tích hợp |
|---|---|---|
| W1 | Đối chiếu auth, scope, GET cơ bản, file gateway | Token, fixture, contract delta rõ; tái dùng phần đã Done |
| W2 | Identity/project, route, catalog, upload, notification | Dữ liệu nền và file có quyền/verification |
| W3 | Report/case/defect, policy, survey/dataset | Snapshot và trạng thái nghiệp vụ làm đầu vào |
| W4 | Inspection, hai nhánh repair và sync | Online độc lập; offline durability trước, conflict sau quyết định |
| W5 | Async AI adapter, research, analytics/export/retention | Job thật, output có provenance, gate release |

Đợt là ưu tiên, không phải lịch cố định; dependency ghi trong từng task thắng số đợt. GET dùng seed không đợi POST. Async callback có thể seed job/manifest để test riêng. Không triển khai cả module mới có thể test một API. Không cam kết hoàn tất trong hai tuần từ số lượng task.

## Gate bắt buộc

- Q02: khung công ty và quyền PM đã được quyết định; Q03 vẫn cần hồ sơ/ngưỡng kỹ thuật theo method. Fail closed khi thiếu căn cứ, không tự thêm duyệt từng sửa Fast Track.
- Q04/Q17: conflict đổi đội và dữ liệu còn trên máy khi tài khoản bị khóa. Core revoke/dedup/durability vẫn test được; E2E rescue/conflict chưa thể nghiệm thu.
- BR-10: gom tuần là đề xuất; BR-09 core của FR-16 vẫn tách được.
- Sync phải mô tả đầy đủ INSPECTION_SUBMIT -> FAST_TRACK_EVALUATE -> REPAIR_START -> REPAIR_SUBMIT; tên kind mới chưa phải runtime support cho tới khi schema, ordering, replay và fixture được duyệt.
- Retention/legal hold, Q10/Q11/Q12/Q13/Q14/Q18 và GAP01: áp dụng gate chi tiết trong task. Không tự chọn TTL, threshold, approval hoặc delete rule.

Q07/Q08, Q09/33A, Q10/Q12/Q13/34A, Q11 và Q14 được crosswalk theo ý nghĩa trong register; không đóng gate chỉ vì số câu trùng nhau.

## Kiểm thử và Done

Mỗi task có input/output, role/policy, dependency, cách triển khai, HTTP template và test dương/âm theo risk. Template chưa có seed thật nên chưa phải request đã chạy. Test schema/status thôi chưa đủ; mutation phải kiểm effect bền vững và retry; read kiểm scope/projection. Chọn một breadth test phù hợp, build test project hiện tại trước khi dùng `--no-build`, và dùng lại bằng chứng chỉ khi input/binary/environment không đổi; full suite dành integration/release hoặc thay đổi shared rộng. Tuân theo AGENTS và ba skill endpoint-delivery/persistence/test-selection trong repo sau khi nhận source. Không nâng SDK theo suy đoán; cây hiện có net8.0, cần đọc global.json/csproj.

Contract guard trong docs: `python docs/diagram/V2/09_Frontend/contracts/check_contracts.py` (đọc hướng dẫn script trước dùng trong CI hiện tại). Đây là kiểm tra docs, không thay test backend.

## Đối chiếu source để chốt delta

Gói V2 hiện đã nằm trong checkout có source backend. Trước mỗi task, chỉ đọc lát cắt cần thiết từ:

- `AGENTS.md`, `.agents/rules/roadguard.md`, ba skill endpoint-delivery/persistence/test-selection, ADR auth/architecture/workflow-sync/ownership và `docs/api-errors.md`.
- Swagger/OpenAPI hiện hành nếu có, branch hiện tại, Controllers + Services/Interfaces + DTO liên quan.
- DbContext, entities/configurations, migration snapshot/migration mới nhất, repositories/interfaces và các test/fixtures liên quan.
- `global.json`, các csproj, DI/Program, worklog mới nhất P1-72 và các task đã Done.

Sự hiện diện của source không biến bản V2 thành code audit hoặc xác nhận 133 API còn thiếu. Mỗi task vẫn là `NEEDS_REPO_CHECK` cho tới khi đối chiếu route, contract, behavior, persistence và test hiện có; không bắt làm lại API đã đạt.

## Kiểm tra gói

[Coverage/gap](COVERAGE_AND_GAPS.md), [manifest](task_manifest.json), [validation](VALIDATION.md). Gói giữ snapshot docs REVIEW-01; source OpenAPI SHA-256: `dbde8b756bbc1bf83dabfbfe395eb11e53b378e7338815b1f3ca019aa7f3f806`.
