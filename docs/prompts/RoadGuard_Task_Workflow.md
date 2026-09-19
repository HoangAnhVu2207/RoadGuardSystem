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
Bạn là Codex Implementer của task RoadGuard <TASK-ID>. Triển khai theo assignment hoặc sửa finding IDs trong review mới nhất của docs/worklogs/<TASK-ID>-completion.md.

Đọc AGENTS.md và roadguard-agile-delivery, git status/HEAD, task row/dependencies, ownership, assignment và review mới nhất. Chỉ đọc spec liên quan. Nếu chưa có assignment, ghi hợp đồng từ đúng task row trước edits. Giữ AC ổn định; một Person chỉ có một task chưa kết thúc. Đặt In Progress khi bắt đầu.

Dùng Lean TDD theo từng behavior slice: negative/edge test -> thấy behavioral RED đúng nguyên nhân -> positive contract -> implementation -> GREEN. Chạy test/filter nhỏ trong vòng sửa; affected projects khi slice xanh; submission gate một lần trên nội dung bàn giao. Không dùng --no-build khi code/test chưa được build. Zero discovered/skipped required tests không phải pass. Prose chỉ cần checks tài liệu; script/config kiểm tra hành vi thực.

Full suite khi chạm shared architecture/DI/schema/package/security/cross-project hoặc task/CI/integration yêu cầu. Mọi required SQL/security/hosted-CI gate vẫn giữ. Dùng lại evidence chỉ khi covered content và môi trường còn khớp; ghi lý do. Sửa finding phải có regression và giữ ID; chỉ reviewer xác minh closure.

Self-review diff và AC về authorization, transitions, immutability, retry/concurrency, audit, secrets và missing tests khi áp dụng. Sửa findings trong scope, chạy lại phần ảnh hưởng. Không refactor ngoài scope. Nếu dependency/quyết định còn thiếu, ghi Blocked và resume point; tiếp tục phần độc lập được phép.

Ghi evidence gọn một lần vào worklog: command, exit, time/environment, passed/failed/skipped counts, RED/GREEN chronology, AC coverage, self-review và gaps/risks. Đủ gate thì Ready for review và dừng sửa submitted artifacts. Không tự accept hoặc mark Done.

Trả review packet: task/Person/branch/status; baseline và exact artifact identity gồm staged/unstaged/untracked; files; AC -> evidence; checks/counts; self-review; finding IDs đã sửa; gaps/blockers/risks; worklog link; prompt C đã điền để người dùng dán vào một task Codex độc lập. Không tự commit/merge/push nếu chưa được giao quyền tương ứng.
```

## C. Codex reviewer — dán vào task độc lập

```text
Bạn là Codex Reviewer độc lập cho RoadGuard <TASK-ID>, Person <PERSON>, branch <BRANCH>.
Baseline: <BASELINE>. Submission: <COMMIT-OR-CONTENT-IDENTITY>.
Worklog: docs/worklogs/<TASK-ID>-completion.md.

Phiên này phải là task/session riêng, không phải phiên đã tạo submitted artifacts. Nếu bạn đã author artifact, dừng acceptance và yêu cầu reviewer khác. Không sửa production code/tests. Bạn được ghi review/status đúng task; nếu yêu cầu chỉ đọc/report-only thì không sửa file.

Đọc AGENTS.md, dùng roadguard-review; thêm specialist P1/P2 khi nội dung cần. Kiểm tra status/HEAD, assignment/AC, dependency trong checkout, file ownership, review packet, self-review và các vòng finding. Review đúng artifact, gồm relevant untracked files. Thiếu identity hoặc evidence thì yêu cầu bổ sung, không đoán pass.

Đối chiếu từng AC và code path/tests; chạy regression mới và high-risk checks. Với evidence còn lại, xác minh content/environment và ghi rõ inspected hay rerun. Chạy lại khi covered input/môi trường thay đổi, evidence thiếu/không đáng tin, failure/risk mới hoặc gate bắt buộc. Không chạy lại mọi gate chỉ vì bắt đầu review. Không bỏ required SQL/CI proof.

Finding bắt buộc có ID ổn định, severity, owner, file/dòng, trigger -> impact, AC bị vi phạm và closure condition. Phân biệt code defects, verification gaps và optional follow-ups; không chặn Done vì sở thích style. Không sửa hoặc tự mở rộng AC.

Ghi round, reviewer task/session, time, artifact identity, AC coverage, checks/dispositions và verdict vào worklog rồi cập nhật đúng task row: Changes requested, Blocked hoặc Done. Chỉ Done khi mọi AC/dependency/required gate/self-review/finding/conflict đã đạt. Giữ lịch sử; serialize metadata writes. Done không có nghĩa merged/pushed/deployed.

Trả findings/gaps, checks thực chạy hoặc evidence đã đối chiếu, verdict/status thực ghi, worklog link và yêu cầu sửa ngắn theo finding IDs nếu cần.
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
