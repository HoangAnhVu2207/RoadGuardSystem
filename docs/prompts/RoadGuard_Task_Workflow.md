# RoadGuard — Codex triển khai và Codex review độc lập

Quyền và gate nằm trong [AGENTS.md](../../AGENTS.md). Dùng một worklog cho mỗi task trong hai plan hiện có. Evidence ghi một lần trong worklog; prompt và câu trả lời chỉ dẫn tới evidence.

## Cách dùng

1. Trong task Codex triển khai, dùng B với task ID. Agent tự chuẩn bị assignment nếu thiếu; không cần một phiên giao việc riêng. A chỉ dùng khi bạn muốn giao việc mà chưa code.
2. Codex code, test, self-review, trả Ready for review cùng review packet và prompt C đã điền.
3. Mở **task Codex mới độc lập**, cùng checkout/artifact, dán prompt C. Reviewer không phải phiên đã code và không sửa implementation.
4. Nếu Changes requested, chuyển finding IDs về task triển khai ban đầu dùng B; sau khi sửa, trả packet mới cho reviewer. Chỉ reviewer độc lập ghi Done.

Không chỉnh submitted artifacts trong khi review. Một task review mới không tạo task ID nghiệp vụ mới. Branch/worktree mới, merge/push vẫn theo quyền Git trong AGENTS.

## Chuyển từ workflow cũ

| Trạng thái | Áp dụng |
|---|---|
| Done | Giữ lịch sử và evidence; không review lại chỉ vì đổi workflow. |
| Ready for review | Review artifact đã bàn giao, kể cả Antigravity tạo; không yêu cầu viết lại. |
| In Progress / Blocked / Changes requested | Giữ evidence; chuyển vòng code/sửa tiếp theo cho Codex Implementer khi người đang sửa đã nhường quyền. Ghi handoff một lần. |
| Not started | Codex Implementer và Codex Reviewer độc lập. |

P1-07 thay chính sách vai trò hiện hành của P1-06. Tên file/template và thư mục Antigravity giữ để tương thích; nội dung lịch sử không bị đổi thành chứng nhận mới.

## A. Chỉ giao việc

```text
Chuẩn bị task RoadGuard <TASK-ID>, chưa triển khai. Đọc AGENTS.md, status/HEAD, task row và dependency thực có trong checkout. Ghi assignment vào worklog hiện có: Person/branch, AC và trace, In scope/Out of scope, exclusive files/shared hotspots, checks và gate. Giữ evidence cũ. Hỏi khi thiếu quyết định sản phẩm/schema/ownership; tự xử lý lựa chọn kỹ thuật thông thường. Không bắt đầu task khác hoặc thực hiện Git integration/publication.
```

## B. Codex triển khai / sửa findings / self-review

```text
Triển khai/sửa RoadGuard <TASK-ID> theo AGENTS.md, roadguard-agile-delivery
và assignment/review mới nhất trong docs/worklogs/<TASK-ID>-completion.md.
Áp dụng quy tắc giảm context/output và Lean TDD của AGENTS; kiểm tra checkout,
dependency/ownership trước edits. Nếu thiếu assignment, ghi từ task row trước.
Giữ AC/scope; xử lý finding theo ID. Không mở lại Done nếu acceptance còn khớp.

Ghi complete review packet một lần vào worklog: baseline/exact identity (cả
untracked), files, AC/evidence, command/exit/time/environment/counts, RED/GREEN,
self-review, finding IDs và gaps/risks. Đủ gate thì Ready for review, đóng băng
artifacts; thiếu prerequisite thì Blocked kèm resume point. Không tự mark Done.

Trả status, identity, finding/gap còn lại, link packet và prompt C đã điền.
Không lặp danh sách file/hash/log đã có trong packet. Không commit/merge/push/deploy.
```

## C. Codex reviewer — dán vào task độc lập

```text
Review độc lập RoadGuard <TASK-ID>, Person <PERSON>, branch <BRANCH>.
Baseline: <BASELINE>. Submission: <COMMIT-OR-CONTENT-IDENTITY>.
Worklog: docs/worklogs/<TASK-ID>-completion.md.
Theo AGENTS.md và roadguard-review; thêm specialist P1/P2 phù hợp.
Reviewer phải là task/session riêng, không author submitted artifacts.

Kiểm tra checkout, assignment, acceptance mới nhất và exact artifact identity
(cả untracked) trước khi dùng handoff này. Nếu Done còn khớp và không có risk
mới, báo acceptance hiện có rồi dừng; không nhận là review mới. Nếu identity
không khớp, xác định submission cần review, không dùng evidence cũ làm pass.

Đọc phần liên quan và theo evidence links để bao phủ mọi AC/diff/dependency,
self-review và finding. Rerun regression mới/high-risk; kiểm tra evidence reuse
theo content/environment. Giữ mọi gate SQL/security/CI bắt buộc; phân biệt
rerun/inspected. Dùng quy tắc output gọn của AGENTS; thiếu evidence không là pass.

Không sửa code/tests. Chỉ append review/status đúng task theo shared contract:
Changes requested, Blocked hoặc Done sau đủ gate; report-only thì không sửa file.
Giữ lịch sử/ID findings, không mở rộng AC. Không commit/merge/push/deploy.
Trả findings/gaps, checks, verdict/status và link worklog ngắn gọn.
```

## Chọn lệnh kiểm tra

Tìm test project/filter có thật trước khi chạy. Ví dụ cho một P1 unit slice:

```powershell
# Inner loop: có build; thay filter bằng class/test thực đã kiểm tra.
dotnet test tests/RoadGuardSystem.UnitTests --filter "FullyQualifiedName~YourTestClass"
# Affected: toàn project liên quan.
dotnet test tests/RoadGuardSystem.UnitTests --no-restore
# Submission production: chạy sau khi nội dung ổn định.
dotnet restore RoadGuardSystem.slnx
dotnet build RoadGuardSystem.slnx --no-restore --no-incremental
dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore
# Sau build thành công, dùng --no-build cho affected suites đã xác định.
# Full solution khi có trigger theo AGENTS:
dotnet test RoadGuardSystem.slnx --no-build --no-restore
```

Đây là công thức chọn lệnh, không phải script chạy tất cả mỗi vòng. Với docs/tooling, dùng verifier phù hợp thay runtime suites. Dừng và xử lý exit khác 0 trước bước phụ thuộc. Log dài giữ ngoài Git trong artifacts/; worklog giữ kết quả và bằng chứng cần cho review.
