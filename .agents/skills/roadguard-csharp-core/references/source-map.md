# Bản đồ source C#

Các đường dẫn là điểm bắt đầu để đọc, không phải bằng chứng task đã được nghiệm thu. Đối chiếu `git diff`, dependency và status trước khi dùng ví dụ; không coi code đang làm dở là contract đã chấp nhận.

| Cần biết | Nguồn trong checkout |
|---|---|
| SDK / roll-forward | [global.json](../../../../global.json) |
| Nullable, analyzers, warnings-as-errors | [Directory.Build.props](../../../../Directory.Build.props) |
| Target và packages từng tầng | [BusinessObjects](../../../../RoadGuardSystem.BusinessObjects/RoadGuardSystem.aBusinessObjects.csproj), [DTOs](../../../../RoadGuardSystem.DTOs/RoadGuardSystem.bDTOs.csproj), [Repositories](../../../../RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj), [Services](../../../../RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj), [API](../../../../RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj) |
| Lý do kiến trúc / target | [ADR 001](../../../../docs/adr/001-backend-boundary.md) |
| Các cạnh project bị cấm / package allowlist | [DependencyGraphChecker](../../../../tests/RoadGuardSystem.UnitTests/Architecture/DependencyGraphChecker.cs), [DependencyGraphTests](../../../../tests/RoadGuardSystem.UnitTests/Architecture/DependencyGraphTests.cs) |
| Contract stack và policy sẵn có | [csharp-dotnet-stack](../../../../.antigravity/skills/roadguard-agile-delivery/references/csharp-dotnet-stack.md) |
| Quyền task và dependency tích hợp | [Person 1](../../../../planning/RoadGuard_Plan_Person_1.md), [Person 2](../../../../planning/RoadGuard_Plan_Person_2.md) |
| Thứ tự đọc đặc tả | [AGENTS — Scope And Sources](../../../../AGENTS.md) |

Snapshot khi tạo skill: target `net8.0`, SDK `10.0.401` với `latestPatch`; không có `LangVersion` override hoặc `.editorconfig` ở root. Đọc lại các file thay vì khóa skill vào snapshot này. Target framework, SDK/compiler và mức analyzer là các lựa chọn khác nhau; không sửa chúng như một phần refactor thường lệ.

Dependency hiện tại: API → Services → Repositories; Repositories → BusinessObjects/DTOs; DTOs → BusinessObjects. Kiểm tra graph test và project thực tế khi cần thay đổi. BusinessObjects không phụ thuộc các tầng cao hơn; API không thêm direct ProjectReference tới Repositories. Quyền sở hữu entity shape thuộc P2 khi persistence task đang hoạt động; P1 chỉ thêm domain behavior sau handoff đã Done, trừ ngoại lệ owner ghi nhận rõ.

Khi cần tra cứu hành vi thư viện chưa chắc chắn, đọc [MCP reference](../../../../.antigravity/skills/roadguard-agile-delivery/references/mcp-tools.md), gửi câu hỏi kỹ thuật công khai theo version local. Không gửi source riêng, worklog hay secret. Tài liệu bên ngoài không tự cấp quyền upgrade hoặc mở rộng task.
