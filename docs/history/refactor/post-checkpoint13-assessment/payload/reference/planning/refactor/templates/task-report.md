# Báo cáo <TASK-ID>-<SLICE>-<A|B>

## 1. Kết quả và phạm vi
- Mục tiêu / acceptance criteria:
- Owner chính / reviewer (chưa có thì ghi chưa có):
- Implementation: Planned | In progress | Partial | Blocked | Done
- Delivery: Draft | Ready for integration | Integrated | Released
- Kết quả chính, phần còn thiếu và ảnh hưởng với người còn lại (3–5 dòng):
- Class: docs-only | structure-only | behavior | public-contract | schema/data

## 2. Baseline và quyền sửa
- Branch / worktree / base commit / HEAD / change commit (nếu có):
- Working tree trước/sau; thay đổi có sẵn được bảo toàn:
- File/symbol được sửa; file chung đã đặt lượt; vùng chỉ đọc:
- Producer/consumer task; điểm nối/version/nguồn thống nhất:
- Dependency START / INTEGRATE / RELEASE và tình trạng từng mục:

## 3. Thay đổi và đối chiếu yêu cầu
| AC / requirement / gap ID | Nguồn mục tiêu | File/symbol thay đổi | Hành vi trước → sau | Trạng thái / bằng chứng |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |
- File tạo/sửa/xóa và lý do; phân biệt verified/proposed/unknown.
- Contract/data/consumer effect; compatibility, migration/recovery nếu có. Không có thì ghi N/A.
- Không gom thay đổi ngoài scope hoặc thay đổi có sẵn vào thành tích task.

## 4. Self-review và autofix
- Diff/range đã review, có bao gồm staged/unstaged/new files; thời điểm/diff cuối:
- Lượt correctness: kiểm tra nào áp dụng, N/A có lý do:
- Lượt phối hợp: shared file/symbol, consumer, schema, conflict hành vi:
| Finding ID | Severity | File/symbol + bằng chứng | Tác động | Đã sửa / Deferred / Coordination ID | Kiểm chứng sau sửa |
|---|---|---|---|---|---|
| ... | ... | ... | ... | ... | ... |
- Kết luận self-review: PASS / BLOCKED / NOT-RUN, lý do:
- Peer review: NOT-REQUESTED / PENDING / REVIEWED; người và nhận xét có nguồn:

## 5. Kiểm chứng
| Command/check | Commit/diff + môi trường | PASS/FAIL/SKIP/NOT-RUN | Số ca + evidence/log | Giới hạn |
|---|---|---|---|---|
| ... | ... | ... | ... | ... |
- Lỗi nền riêng với regression mới; test chưa chạy và lý do.
- Local/fake contract verification riêng với integration SQL/storage/provider/consumer thật.
- Không ghi PASS cho test chưa chạy hoặc zero-test run; không lộ secret.

## 6. Phối hợp và quyết định
| Coordination ID/link | Task/owner bị ảnh hưởng | Blocker ở checkpoint nào | Đề xuất người sửa chính | Đã thống nhất? / nguồn | Bước tiếp |
|---|---|---|---|---|---|
| ... | ... | ... | ... | ... | ... |
- Business/contract/data decision cần người dùng: câu hỏi, phương án, khuyến nghị, tác động.
- Không có note thì ghi không có sau khi kiểm tra, không bỏ mục.

## 7. Checkpoint và bàn giao
- Hoàn thành / còn lại / exact next step / người nhận:
- Điều kiện được tích hợp; thứ tự producer-consumer; shared file reservation cần trả:
- Rollback/phục hồi; rủi ro và phần chưa xác minh:
- Báo cáo task cha cần cập nhật bởi owner tổng hợp:
- Tóm tắt gửi người lập kế hoạch (tự đủ ngữ cảnh): task, owner, status/stage, kết quả, self-review, tests, coordination ID, việc cần thảo luận và bước tiếp theo.
