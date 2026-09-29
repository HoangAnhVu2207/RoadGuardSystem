# Prompt dùng khi bắt đầu task mới

Dùng prompt này sau khi chọn đúng một file trong `planning/V2/Execution/`.

```text
Bạn đang làm RoadGuard V2. Thực hiện đúng task file sau: <TASK_FILE>

Task ID / owner / branch: <TASK_ID> / <ANH hoặc HUY> / <BRANCH>
Mục tiêu bắt buộc: <nêu kết quả người dùng/nghiệp vụ phải đạt>
Operation IDs liên quan: <liệt kê operationId hoặc ghi N/A>
Decision/BR/FR/AC đã chấp thuận: <IDs>
Git permission: <none | commit only | commit and push origin/<branch>>

Yêu cầu thực hiện:
1. Bắt đầu bằng `git status --short --branch`, `git rev-parse HEAD`, kiểm tra dirty paths và đọc AGENTS.md, task file, TASK_LIFECYCLE.md, current owner plan, decision register, ADR 001/004/006 và đúng docs được task trỏ tới.
2. Trước khi sửa, trả scope card gồm: Task/owner/branch, Goal, business flow, In scope, Out of scope, exact files/shared hotspots, dependencies/fixtures, verification commands và package/migration/data/external side effects.
3. Lập Source evidence trước khi sửa. Mỗi nguồn phải ghi exact path + heading/ID + invariant và nhãn CURRENT_VERIFIED, TARGET_DOCUMENTED, PROPOSED_DELTA, HISTORICAL hoặc NOT_ENABLED.
4. Viết contract 5-8 dòng cho từng operation: route/method, actor/scope, input/validation, success output/status, stable errors, state rule, persistence/idempotency/concurrency và audit/privacy.
5. Mô tả luồng nghiệp vụ từ actor -> scope -> service/repository -> state change -> durable effect -> response. Không suy ra business rule từ tên file hoặc task metadata.
6. Chỉ sửa đúng owner layer và exact scope. Không tự tạo endpoint, table, migration, package, provider, policy, threshold hoặc refactor ngoài scope.
7. Nếu gặp conflict về contract, schema, authorization, state, compatibility, provider hoặc dữ liệu thật: ghi current-vs-target, nêu decision cần có, dừng phần bị chặn và tiếp tục phần độc lập đã được duyệt.
8. Thực hiện workflow IMPLEMENT -> UPDATE_POSTMAN (nếu API) -> REVIEW_FIX -> VERIFY -> REPORT. Review phải dùng diff-first và tự sửa lỗi rõ ràng trong scope.
9. Chạy verification đủ breadth, build test binaries trước `--no-build`, ghi command/config/filter, pass/fail/skip, SQL/HTTP smoke và durable effects. Test repository không được báo là API_VERIFIED.
10. Ghi handoff vào `planning/CROSS_OWNER_HANDOFFS.md`; receiver phải có VERIFIED hoặc NO_CHANGE_NEEDED. Append completion history và chỉ dùng deliveryStatus TODO/IN_PROGRESS/PARTIAL/BLOCKED/DONE/REOPENED.
11. Chỉ stage explicit paths, commit/push đúng quyền đã ghi. Không push task PARTIAL/BLOCKED và không đụng unrelated dirty work.

Kết thúc bằng báo cáo: changed files, mục tiêu đạt/chưa đạt, commands và kết quả thật, handoff, side effects, reused/invalidated evidence, blockers và commit SHA nếu có.
```
