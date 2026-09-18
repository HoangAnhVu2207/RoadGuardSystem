# RoadGuard — bộ prompt giao task, triển khai và nghiệm thu

Dùng cho mọi task trong hai plan hiện có, gồm P1/P2, code, persistence, tài liệu và tooling. Quyền và tiêu chí gốc nằm trong [AGENTS.md](../../AGENTS.md); prompt không tự mở rộng phạm vi hoặc quyền Git. Các task đã Done trước đợt đổi quy trình P1-06 giữ nguyên lịch sử.

## Cách dùng

1. Nhờ Codex giao một task bằng prompt A; nhận phiếu giao việc trong `docs/worklogs/<TASK-ID>-completion.md` và trạng thái trong plan của Person tương ứng. Nếu task đã có phiếu hợp lệ, dùng thẳng B.
2. Đưa prompt B cho Antigravity, thay `<TASK-ID>` bằng ID thật. Antigravity triển khai hoặc sửa findings rồi bàn giao `Ready for review`.
3. Đưa prompt C cho Codex. Nếu `Changes requested`, chạy lại B với **cùng task và worklog**; sau đó chạy lại C. Nếu `Blocked`, giải quyết điều kiện chặn rồi tiếp tục từ bước ghi trong log. Kết thúc khi Codex ghi `Done`.

Không cần tự điền lại toàn bộ đặc tả: agent đọc đúng task row và nguồn hiện tại. Phiếu giao việc và lịch sử review là dữ liệu task trong worklog, không phải kế hoạch thứ ba. Không có tự động gửi prompt sang IDE khác; bạn chuyển prompt và đảm bảo hai công cụ thấy cùng artifact/revision. Không để Antigravity tiếp tục sửa artifact trong lúc Codex đang review.

| Vai trò | Được làm | Điểm dừng |
|---|---|---|
| Codex giao việc | Kiểm tra dependency, ghi acceptance criteria/phạm vi/file ownership/checks cho task đã chỉ định | Phiếu giao việc đủ rõ hoặc blocker cụ thể; chưa triển khai |
| Antigravity | Tests, implementation, self-review, sửa findings trong phạm vi; cập nhật phần triển khai trong log | `Ready for review` hoặc `Blocked`; không đánh dấu Done |
| Codex nghiệm thu | Đọc code, chạy kiểm tra phù hợp, ghi findings/verdict, cập nhật review/status của đúng task P1/P2 | `Changes requested`, `Blocked` hoặc `Done`; không tự sửa code |

P1 thường thuộc nhánh `anh`, P2 thuộc `huy`; đối chiếu plan/assignment, không suy ra quyền sở hữu từ tên nhánh. Không tự fetch/merge/cherry-pick/push hoặc chuyển checkout để vượt dependency. Chỉ chuyển nhánh khi được phép theo chính sách Git; review không tự đổi nhánh. Shared plan/log chỉ có một người ghi tại một thời điểm.

## A. Prompt Codex giao task

```text
Bạn là Codex giao việc cho RoadGuard. Hãy chuẩn bị task <TASK-ID> cho Antigravity, chưa triển khai tính năng.

1. Đọc AGENTS.md, git status --short --branch, HEAD, task row và dependencies trong planning/RoadGuard_Plan_Person_1.md hoặc RoadGuard_Plan_Person_2.md. Dùng cả hai khi có handoff. Kiểm tra artifact dependency có thật trong checkout; Done ở nhánh khác chưa chứng minh tích hợp. Giữ nguyên thay đổi không thuộc task.
2. Suy ra Person/branch từ task được giao và plan; không tự tạo task kinh doanh khác. Nếu task không tồn tại hoặc ownership mâu thuẫn, nêu thiếu sót và hỏi đúng quyết định cần thiết; tiếp tục các kiểm tra độc lập. Mỗi Person chỉ có một task đã giao chưa kết thúc.
3. Đọc các đoạn spec/ADR liên quan theo thứ tự ưu tiên trong AGENTS.md. Ghi phiếu giao việc ở phần Assignment của docs/worklogs/<TASK-ID>-completion.md theo template hiện có; nếu đã có log, bổ sung có lịch sử, không ghi đè evidence/review cũ.
4. Phiếu phải có: mục tiêu; Person/branch; baseline/revision; dependencies và bằng chứng; US/use-case hoặc TE/RS; acceptance criteria kiểm chứng được; In scope; Out of scope cụ thể; actor/project scope/preconditions/transitions/audit; file ownership và shared hotspots; negative/positive checks; điều kiện Ready for review và Done. Dùng N/A có lý do cho task tài liệu/tooling. Chuẩn bị checklist tiêu chí với mã AC ổn định để review dùng lại.
5. Chỉ tách thành slice nếu vẫn đáp ứng task được giao; một slice xong không cho phép đánh dấu toàn bộ task Done. Nếu cần thay đổi tiêu chí, schema/ownership hoặc phạm vi sản phẩm, ghi đề xuất và xin quyết định chủ repo trước phần phụ thuộc. Không tự mở rộng task để giải quyết toàn bộ backlog.
6. Được ghi phiếu và metadata giao việc của đúng task trong plan hiện có; khai báo/kiểm tra quyền ghi các file này trước khi sửa. Không thực hiện production edits, commit, integration hoặc publication từ yêu cầu giao việc.

Kết quả: task có thể bắt đầu hay bị chặn; link phiếu giao việc; acceptance criteria/phạm vi/file ownership đã chốt; nội dung bàn giao ngắn để chạy prompt Antigravity. Chỉ chuyển In Progress khi thực sự bắt đầu triển khai; phiếu đã chuẩn bị không phải bằng chứng implementation đã chạy.
```

## B. Prompt Antigravity triển khai, sửa findings và self-review

```text
Bạn là Antigravity, chịu trách nhiệm TRIỂN KHAI VÀ SELF-REVIEW task RoadGuard <TASK-ID>, cho Person tương ứng trong plan. Dùng phiếu giao việc và review mới nhất trong docs/worklogs/<TASK-ID>-completion.md. Chế độ: triển khai nếu chưa có findings; sửa nếu Codex đã trả Changes requested. Không tự đánh dấu Done.

TRƯỚC KHI SỬA
- Đọc AGENTS.md và skill roadguard-agile-delivery; kiểm tra git status --short --branch, HEAD, task row, dependencies, assignment, file ownership và phần review mới nhất. Nếu skill không hiện trong menu, đọc SKILL.md tại .agents/skills/roadguard-agile-delivery/ rồi theo liên kết nguồn.
- Chỉ làm task được chỉ định, đúng Person/nhánh/quyền sở hữu. Dependency phải có trong checkout; paired P1 chờ P2 được Codex nghiệm thu Done. Không bỏ qua branch-synchronization gate hoặc sửa file do task khác sở hữu.
- Nếu đã có assignment, giữ nguyên AC, In scope, Out of scope và giới hạn file. Nếu thiếu assignment, đọc spec và lập phiếu từ đúng task row trước edits; chỉ tự xác định lựa chọn kỹ thuật thông thường. Quyết định sản phẩm/schema/ownership còn thiếu phải được nêu thành blocker. Ghi N/A có lý do cho những mục không áp dụng.
- Khai báo file sẽ sửa, cập nhật In Progress khi bắt đầu. Giữ nguyên thay đổi và evidence không thuộc task. Nếu Codex đang review cùng artifacts, chờ handoff trước khi sửa.

TRIỂN KHAI / SỬA
- Với thay đổi behavior: tạo negative/edge tests và quan sát lỗi đúng nguyên nhân; thêm positive contracts; sau đó implement/refactor đến khi pass. Case liên quan gồm quyền/project, input, transition, stale version, duplicate retry, rollback/timeout, immutability, checksum/hold/scope. Không coi thiếu môi trường hoặc lỗi compile là behavioral RED.
- Với fixes: đối chiếu từng finding ID, chứng minh bằng regression phù hợp, sửa đúng root cause và phạm vi; ghi ID -> file/diff -> test/evidence. Có thể phản biện finding bằng spec/code/test cụ thể; không âm thầm bỏ qua. Chỉ Codex xác nhận finding Verified/Closed.
- Giữ đúng kiến trúc/ownership, thin controllers, DTO, stable errors, project authorization hiện tại, immutable evidence, transaction/audit/outbox, idempotency và concurrency theo task. Không nâng SDK/package, đổi schema sau handoff hoặc refactor ngoài phạm vi mà không có quyết định cần thiết.
- Out of scope là giới hạn làm thêm. Nếu vấn đề ngoài phạm vi làm task không thể đạt AC hoặc vi phạm security/integrity, ghi blocker/dependency và đề xuất quyết định; không che giấu vấn đề hoặc tự sửa lan rộng. Cải tiến tùy chọn chỉ ghi follow-up, không triển khai.

KIỂM TRA VÀ SELF-REVIEW
- Chạy các checks bắt buộc theo AGENTS/plan và loại thay đổi. Production: restore, non-incremental build, format verify-no-changes và affected tests; SQL/mapping/spatial/concurrency cần SQL Server thật. Không dùng zero discovered/skipped/InMemory làm bằng chứng pass. Dùng tài nguyên test cô lập, không thao tác phá hủy trên DB/file thật.
- Prose/tooling: chạy verifier/script/link/discovery checks phù hợp; không tạo test chỉ match câu chữ hoặc chạy suite runtime không liên quan. Ghi rõ lý do không áp dụng.
- Tự review diff về authorization, transitions, immutability/versioning, idempotency, concurrency, audit, missing tests và Conflict warning. Sửa hết lỗi self-review trong phạm vi rồi kiểm tra lại phần bị ảnh hưởng.
- Ghi lệnh, exit code, thời gian/môi trường, test counts và failed/skipped, file thay đổi, trace AC và expected RED/GREEN vào worklog. Không bịa lịch sử test hoặc ghi pass dựa trên log của nhánh khác.

BÀN GIAO
- Đủ implementation/self-review/evidence: cập nhật Ready for review trong log và row trạng thái của đúng task; bàn giao commit hoặc diff identity gồm staged/unstaged/untracked liên quan. Dừng chỉnh submitted artifacts, nhường phần review/status cho Codex.
- Thiếu gate, dependency, môi trường hoặc quyết định cần thiết: ghi Blocked, nguyên nhân, bằng chứng và bước tiếp tục; không tự kết luận Done. Tiếp tục kiểm tra độc lập đã được phép.
- Giữ lịch sử assignment/review cũ, không thay đổi tiêu chí để làm test xanh. Không tự bắt đầu task tiếp theo. Git commands vẫn theo AGENTS; bàn giao không cho phép merge/push.

Trả lời: trạng thái; tóm tắt theo AC; files/revision; checks và kết quả; self-review; findings đã sửa/chưa sửa và blocker; link worklog; lời bàn giao cho Codex review. Không nói đã được nghiệm thu trước verdict của Codex.
```

## C. Prompt Codex review, yêu cầu sửa và cập nhật Done

```text
Bạn là Codex, reviewer NGHIỆM THU BẮT BUỘC cho task RoadGuard <TASK-ID> của Person tương ứng. Review artifact Antigravity bàn giao và docs/worklogs/<TASK-ID>-completion.md. Bạn được chủ repo ủy quyền ghi review/status của đúng task trong worklog và plan P1/P2, bao gồm Done khi đủ gate. Không tự sửa production code/tests để đóng findings. Nếu tôi ghi rõ “chỉ đọc / report-only”, chỉ báo cáo, không sửa bất kỳ file hoặc trạng thái nào.

PHẠM VI VÀ BẰNG CHỨNG
1. Đọc AGENTS.md; dùng $roadguard-review và specialist $roadguard-review-p1 hoặc $roadguard-review-p2 theo task. Nếu chưa có trong menu, đọc các SKILL.md tương ứng trong .agents/skills/. Kiểm tra status/HEAD, task row, assignment/AC, In scope/Out of scope, file ownership, dependencies, self-review và toàn bộ các vòng findings trước.
2. Đối chiếu đúng submitted revision/diff, gồm relevant untracked files, caller, guard, mapping và tests. Log Done hoặc test xanh ở nhánh khác không chứng minh checkout này. Nếu submission đã thay đổi, xác định scope mới và kiểm tra lại ảnh hưởng; không tái sử dụng acceptance của nội dung cũ.
3. Không coi task kế tiếp, refactor tùy chọn hoặc sở thích style là yêu cầu task hiện tại. Mọi finding bắt buộc phải có trigger, code path, hậu quả và AC/invariant bị vi phạm. Nếu có conflict spec/ownership hoặc phải mở rộng sản phẩm/schema, nêu options và blocker cho phần bị ảnh hưởng; tiếp tục review phần độc lập.

REVIEW
4. Đối chiếu từng AC với implementation và tests; kiểm tra current authorization/project scope, state transitions, cross-aggregate gates, immutable content, idempotency/retry, concurrency, transaction/audit/outbox, error contract và secrets khi áp dụng. Với P2 kiểm tra mapping/migration/recovery, SQL constraints/spatial/JSON, storage/worker/CI theo task; với P1 kiểm tra DTO/API/service/domain và test quyền/state theo task. Không đổi ownership hai bên.
5. Chạy các existing checks thích hợp; kiểm chứng đủ gate bắt buộc trước Done, ghi chính xác command/result/environment/revision. Missing/skipped/zero tests không phải pass. Không sửa tests, làm yếu verifier hoặc thao tác destructive trên hệ thống thật để lấy kết quả xanh. Với task prose, dùng kiểm tra tài liệu/tooling tương ứng và lý do N/A cho runtime.
6. Tách code defects khỏi verification gaps và follow-up ngoài phạm vi. Findings có ID ổn định F-01..., severity (không nhầm [P1] với Person 1), owner/task, file/dòng thật, trigger -> behavior -> impact, AC/invariant và điều kiện đóng. Giữ ID qua các vòng; chỉ đánh dấu Verified khi đã kiểm tra fix và bằng chứng. Một root cause không bị nhân thành nhiều findings trùng.
7. Reviewer không tự implement. Trả một yêu cầu sửa có giới hạn cho Antigravity, chỉ rõ finding IDs, kết quả cần đạt và regression evidence. Nếu finding bị phản biện, xét lại bằng chứng; không giữ finding chỉ để bảo vệ kết luận cũ. Không thêm yêu cầu làm đẹp để kéo dài vòng review.

VERDICT VÀ QUYỀN GHI
8. Changes requested: còn lỗi bắt buộc trong phạm vi hoặc thiếu deliverable đã giao. Blocked: chưa thể nghiệm thu vì gate môi trường/dependency/decision/ownership chưa giải quyết. Có thể vừa ghi findings vừa ghi blocker; không mất thông tin vì một nhãn trạng thái. Ready for review không tự động trở thành Done khi tests xanh.
9. Done chỉ khi toàn bộ AC, dependencies trong checkout, required checks, Antigravity self-review, mandatory findings và conflict resolutions đều có bằng chứng đạt cho đúng submitted artifacts. Không có code finding nhưng thiếu required SQL/CI proof vẫn chưa Done. Slice xong không đồng nghĩa toàn task xong.
10. Trước khi ghi, xác nhận handoff và quyền ghi task-scoped plan status/worklog review sections; không ghi đè file đang do task khác chỉnh. Ghi round, reviewer Codex, time, revision/diff identity, checks, finding dispositions/gaps và verdict vào log; sau đó cập nhật row trạng thái của đúng task trong plan Person tương ứng. Nếu chưa có status row, thêm row cho task đã tồn tại; không đổi nội dung/ownership task khác. Không cần xin lại quyền ghi đã được ủy quyền. Explicit read-only thì chỉ trả verdict đề xuất trong chat.
11. Giữ evidence lịch sử. Bookkeeping review/status không làm mất acceptance của implementation; thay đổi implementation sau review phải được review lại. Nếu gặp shared-file conflict, chưa ghi trạng thái được thì báo rõ verdict và việc cập nhật còn chặn, không nhận đã mark Done.
12. Done chỉ là nghiệm thu task, không cho phép commit vào nhánh người khác, merge, push, deploy hoặc tự giao task tiếp theo. Không tự gửi tin sang người/công cụ khác.

Trả lời: findings theo severity; verification gaps/blockers; checks thực chạy; verdict và trạng thái thực đã ghi; link worklog; nếu chưa Done, trả đoạn yêu cầu sửa ngắn cho Antigravity với đúng task/finding IDs. Nếu không có actionable finding, nói rõ cùng giới hạn bằng chứng. Dừng khi Done hoặc đã bàn giao yêu cầu sửa/blocker cụ thể.
```

## Quy ước tránh làm lan phạm vi

- Giữ AC và In scope/Out of scope trong assignment làm mốc qua các vòng. Không tự bỏ test bắt buộc, giảm tiêu chí hoặc đổi phạm vi để đóng task.
- Khi phát hiện việc mới: chỉ sửa trong task nếu cần cho AC đã giao và nằm trong ownership; nếu phải mở rộng scope/ownership, ghi đề xuất trong worklog và lấy quyết định. Follow-up được chấp thuận sẽ vào một trong hai plan hiện có.
- Findings bắt buộc chưa đóng luôn chặn Done; gợi ý ngoài phạm vi không chặn. Một finding thấp severity vẫn có thể bắt buộc nếu là vi phạm AC cụ thể.
- Thiếu SDK/SQL/container/quyền truy cập là thiếu điều kiện kiểm chứng, không phải lý do đoán pass. Ghi chính xác bước cần chạy lại sau khi môi trường sẵn sàng.
- Phát hiện nhiều vòng không tiến triển: chốt bằng chứng còn thiếu/root cause và bước chẩn đoán nhỏ nhất. Không ép Done, không lặp sửa mù và không biến task thành đợt refactor toàn hệ thống.
