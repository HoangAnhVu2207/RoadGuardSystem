---
name: roadguard-csharp-testing
description: Use when writing or selecting tests and verification evidence for an assigned RoadGuard C# behavior slice using xUnit, API factories and real SQL Server fixtures. Also applies to diagnosing test discovery or choosing affected checks, not independent task acceptance.
---

# Kiểm thử C# RoadGuard

Giữ phạm vi task và [Lean TDD trong AGENTS](../../../AGENTS.md). Với yêu cầu read-only chỉ dùng kiểm tra hiện có, không viết test hoặc fix. Đọc [fixture và lệnh](references/test-map.md) khi cần chọn môi trường.

## Chọn test theo lời khẳng định cần chứng minh

| Lời khẳng định | Bằng chứng phù hợp |
|---|---|
| Guard, domain transition, deterministic application policy | Unit xUnit + FluentAssertions; fake boundary có kiểm soát |
| Binding, validation, status/error code, correlation, auth pipeline | API test qua WebApplicationFactory |
| FK/check/index, JSON/spatial, rowversion, transaction/idempotency | Integration trên SQL Server thật |
| Migration upgrade/recovery/model compatibility | Migration lifecycle trên DB cô lập, không chỉ EnsureCreated |
| Quyền thay đổi/revocation ảnh hưởng request kế tiếp | Request + current server state thật phù hợp AC; mock claims đơn thuần không đủ |

Viết negative/edge test liên quan đến một lát cắt và quan sát behavioral RED; thêm positive contract nhỏ, rồi implement GREEN. Compilation error, thiếu SDK/SQL hoặc zero discovered không phải behavioral RED. Không cần viết toàn bộ test matrix trước lát cắt đầu. Prose/tooling dùng verifier/link/scenario checks; không kiểm thử từng câu chữ.

Test có tên nêu trigger/kết quả và trace TaskId/AC theo convention hiện có. Chọn các case áp dụng: malformed/boundary, role/project sai, stale version, duplicate/changed-payload retry, timeout/failure, invalid transition, checksum/immutable/legal-hold gates. Nhóm case không liên quan thành một N/A có lý do trong worklog.

Positive test assert state, response contract, audit, version và số effect khi phù hợp; failure test assert không có write/audit thành công/side effect ngoài ý muốn. Kiểm tra retry sau mất quyền, request khác payload dùng lại key và concurrent winner/loser theo AC.

Ví dụ: stale update cần hai DbContext độc lập cùng đọc version ban đầu, writer thứ nhất commit, writer thứ hai gửi version cũ và nhận conflict; kiểm tra persisted state/audit không bị writer thứ hai ghi đè. Dùng synchronization có kiểm soát cho race, không dùng delay tùy ý để "tạo concurrency".

## Chạy và ghi bằng chứng

Inner loop chạy test/class/filter nhỏ với build bình thường. Dùng `--list-tests` khi filter chưa chắc chắn; count phải lớn hơn 0 và đúng test cần chạy. Chỉ dùng `--no-build` khi đã build đúng code/tests/configuration hiện tại.

Sau GREEN, chạy affected projects và required SQL/API gates. Submission production chạy restore, non-incremental build, format verification và affected tests theo AGENTS; full suite cho trigger kiến trúc/DI/schema/packages/security/cross-project hoặc yêu cầu task/CI. Reuse kết quả chỉ khi content/environment được bao phủ không đổi. Không chạy full suite sau mỗi edit nhỏ.

Ghi command, exit, thời điểm, môi trường, test counts/skip, RED/GREEN chronology và scoped content identity một lần trong worklog. Infra thiếu là gap/blocker, không skip rồi ghi pass. Local check không chứng minh hosted CI. Chỉ handoff `Ready for review` khi toàn bộ required checks/evidence và self-review đạt, kèm prompt reviewer độc lập. Nếu còn gate bắt buộc chưa chạy/không đạt, ghi blocker hoặc việc cần sửa và resume point; tiếp tục phần độc lập được phép, không tự acceptance.

Ví dụ gọi: `$roadguard-csharp-testing Chọn và chạy kiểm tra cho lát cắt hiện tại, ghi rõ SQL/API evidence và các gap.`
