---
name: roadguard-csharp-core
description: Use when writing or refactoring C# domain or application code in RoadGuard, choosing async/nullability conventions, or checking its project dependencies and SDK/target baseline. Not for acceptance reviews or unrelated C# repositories.
---

# C# dành cho RoadGuard

Skill này bổ sung kỹ thuật viết code; [AGENTS.md](../../../AGENTS.md) và task được giao vẫn quyết định phạm vi, quyền sở hữu, kiểm thử và bàn giao. Với yêu cầu chỉ giải thích hoặc review, chỉ đọc và chạy kiểm tra phù hợp; không tự sửa code.

## Xác định baseline trước khi viết

Đọc `git status --short --branch`, task/dependency trong plan, rồi [bản đồ source](references/source-map.md). Đọc `global.json`, file project và ADR thực tế; SDK mới hơn không đồng nghĩa với target hoặc ngôn ngữ mới. Giữ tên project có tiền tố `aBusinessObjects`, `bDTOs`, `cRepositories`, `dServices`, `eAPI`. Không thêm package, framework hay `LangVersion` chỉ để áp dụng một mẫu code.

## Viết một lát cắt C# nhỏ

- Đặt invariant cục bộ trong BusinessObjects; quyết định use case/quyền/cross-aggregate trong Services; EF/storage trong Repositories; HTTP trong API; public contract trong DTOs. Dùng dependency graph hiện tại, không áp một kiến trúc mới lên solution.
- Nullable phải phản ánh contract thực. Kiểm tra null/input ở biên, dùng guard và kiểu trả về hiện có; tránh `!`, nullable suppression hay default giả để che input thiếu. Giữ enum số ổn định; domain entity giữ identity, không đổi sang record chỉ để gọn cú pháp.
- Async xuyên suốt I/O: hậu tố `Async`, trả `Task`/`Task<T>`, truyền `CancellationToken` từ request/worker đến các lời gọi hỗ trợ nó. Không dùng `.Result`, `.Wait()`, `async void` hoặc `Task.Run` để bọc EF/HTTP I/O. Không chạy song song trên cùng DbContext. Tách timeout nghiệp vụ khỏi caller cancellation; không nuốt cancellation thành thành công.
- Dùng UTC `DateTimeOffset` cho thời điểm, `DateOnly` cho ngày lịch theo schema. Với logic phụ thuộc thời gian, dùng clock/TimeProvider đang có hoặc boundary nhỏ được task cho phép; test thời điểm hết hạn chính xác. Không chuyển timezone ngầm hay đổi precision dữ liệu.
- Giữ convention ở file lân cận và formatter hiện có. Ưu tiên constructor injection, phương thức nhỏ và tên theo nghiệp vụ. Chỉ thêm abstraction khi có boundary thực; không tự đưa MediatR, AutoMapper, generic repository hoặc base-service framework vào dự án.
- Log theo cấu trúc với correlation ID, dùng dữ liệu allowlist cho audit; không serialize entity/request/credential nguyên khối. Giữ nguyên immutable evidence/versioning khi refactor.

Ví dụ đối chiếu thực tế: [SpatialValidation](../../../RoadGuardSystem.BusinessObjects/Spatial/SpatialValidation.cs) giữ guard/SRID ở domain, [mapping/test spatial](../../../tests/RoadGuardSystem.IntegrationTests/Infrastructure/SpatialProbeDbContext.cs) giữ SQL ở persistence. Validator này không tự chứng minh tọa độ hợp lệ hoặc chuyển hệ tọa độ; chỉ dùng phần contract nó thực sự kiểm tra.

## Chọn hướng dẫn bổ sung khi cần

| Phần đang làm | Skill |
|---|---|
| DTO, controller, authorization, application service | [roadguard-csharp-api](../roadguard-csharp-api/SKILL.md) |
| EF Core, migration, SQL, concurrency/idempotency | [roadguard-csharp-persistence](../roadguard-csharp-persistence/SKILL.md) |
| Chọn test, fixture, lệnh và bằng chứng | [roadguard-csharp-testing](../roadguard-csharp-testing/SKILL.md) |

Chỉ đọc nhánh liên quan. Kết thúc implementation bằng evidence và self-review theo [workflow hiện có](../../../docs/prompts/RoadGuard_Task_Workflow.md), trạng thái tối đa `Ready for review`; acceptance thuộc task reviewer độc lập.

Ví dụ gọi: `$roadguard-csharp-core Triển khai lát cắt của task đã giao, giữ dependency và baseline C# hiện tại.`
