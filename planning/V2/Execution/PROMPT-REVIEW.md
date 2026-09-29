# Prompt review và fix trước khi báo cáo hoặc push

Dùng sau implementation, hoặc dùng độc lập khi owner yêu cầu review task hiện tại.

```text
Đóng vai Team Leader RoadGuard. Review và auto-fix đúng task hiện tại: <TASK_FILE>, owner <ANH/HUY>, branch <BRANCH>.

MỤC TIÊU REVIEW
- Kiểm tra code có đạt Task goal, BR/FR/UC/US/AC và contract đã duyệt không.
- Kiểm tra scope có bị làm lố, thiếu luồng nghiệp vụ, sai ownership hoặc claim evidence vượt quá thực tế không.
- Tìm và tự sửa lỗi rõ ràng trong scope; không tự quyết business/schema/authorization/compatibility delta chưa được duyệt.

QUY TRÌNH
1. Chạy `git status --short --branch`, `git rev-parse HEAD`, `git diff --name-status -M HEAD`, `git diff --cached --name-status -M`, và liệt kê untracked. Tách pre-existing dirty work khỏi task diff.
2. Đọc task checkpoint, AGENTS.md, TASK_LIFECYCLE.md, decision register, relevant ADR và đúng nguồn BR/FR/contract/state/ERD/source/test của task.
3. Lập review scope: base SHA, files/hunks được review, files có sẵn trước task, AC/BR/contract dùng làm chuẩn, open decisions và ownership boundary.
4. Review lớp 1 trên từng diff: compile/type, null/boundary, validation, async/cancellation, security/privacy, auth/scope, state transition, idempotency/retry/concurrency, serialization/error và test quality.
5. Mở rộng impact scope chỉ khi signature/DTO/API/auth/DI/mapping/schema/transaction thay đổi; đọc callers/implementations/contracts/tests liên quan, không quét cả repo.
6. Phân loại: RED tự sửa nếu lỗi rõ và cách sửa không cần decision; ORANGE chặn và hỏi decision nếu có nhiều cách hiểu hoặc đổi contract/schema/policy; YELLOW sửa nhỏ nếu không mở scope.
7. Tự sửa RED trong task, ghi ledger file/symbol trước-sau/rule/verification. Không sửa test expectation chỉ để xanh, không weaken assertion, swallow exception, skip gate hoặc dùng default để che thiếu policy.
8. Chạy lại checks bị invalidated. Persistence/migration/rowversion/spatial cần SQL Server; API cần build fresh + real HTTP smoke; API change cần Postman static check. Ghi pass/fail/skip/BLOCKED_ENV thật.
9. Kiểm tra handoff trong `planning/CROSS_OWNER_HANDOFFS.md`; chỉ VERIFIED/NO_CHANGE_NEEDED kèm integration evidence mới cho phép DONE/commit/push.
10. Append completion history và trả kết luận đúng một trong: ĐỦ ĐIỀU KIỆN PUSH; CẦN XÁC NHẬN; CHƯA ĐỦ ĐIỀU KIỆN PUSH.

KHÔNG ĐƯỢC
- Không review bằng cảm giác hoặc tên hàm; phải có source/rule/test evidence.
- Không tự thay đổi public contract, schema, migration, permission, TTL, threshold, provider behavior hay governance.
- Không stage all, stash/reset/clean, amend, force-push hoặc push tự động nếu prompt không cấp quyền.
- Không gọi repository test là API_VERIFIED và không gọi fake provider là external E2E.

Báo cáo theo bảng: finding severity/file/symbol/evidence, fixes, checks, handoff, blockers, unreviewed scope và kết luận push.
```
