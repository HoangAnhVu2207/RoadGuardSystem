# Các điểm nối API đang có

Đọc file theo nhu cầu và kiểm tra diff/status task; source hiện hữu không mặc nhiên là artifact đã accepted.

| Nhu cầu | Nguồn |
|---|---|
| Pipeline/composition | [Program](../../../../RoadGuardSystem.API/Program.cs), [AddApiPlatformServices](../../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs) |
| ProblemDetails, invalid model state và sanitize | [ServiceCollectionExtensions](../../../../RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs), [negative contract tests](../../../../tests/RoadGuardSystem.ApiTests/Platform/ProblemDetailsNegativeTests.cs) |
| Stable machine code | [ApiErrorCodes](../../../../RoadGuardSystem.API/Constants/ApiErrorCodes.cs), [error policy](../../../../docs/api-errors.md) |
| Correlation ID | [CorrelationIdMiddleware](../../../../RoadGuardSystem.API/Middlewares/CorrelationIdMiddleware.cs) |
| Versioning / OpenAPI | [ApiVersioningValidationMiddleware](../../../../RoadGuardSystem.API/Middlewares/ApiVersioningValidationMiddleware.cs), [ConfigureSwaggerOptions](../../../../RoadGuardSystem.API/Extensions/ConfigureSwaggerOptions.cs) |
| HTTP test harness | [CustomWebApplicationFactory](../../../../tests/RoadGuardSystem.ApiTests/Infrastructure/CustomWebApplicationFactory.cs), [ProbeController](../../../../tests/RoadGuardSystem.ApiTests/Controllers/ProbeController.cs) |
| Authority của user/session/membership | [ADR 002](../../../../docs/adr/002-authentication.md) |
| Ownership/dependency | [P1 plan](../../../../planning/RoadGuard_Plan_Person_1.md), [P2 plan](../../../../planning/RoadGuard_Plan_Person_2.md) |

ProbeController là controller chỉ dành cho test, được nạp bằng ApplicationPart. Không copy probe/fault endpoint vào API production. Factory mặc định dùng Production; test riêng request malformed/validation và pipeline exception thực để chứng minh error không lộ chi tiết.

Snapshot khi tạo skill: P1-10 authentication đang In Progress; P1-12/P2-11 mới là các task về current membership/project authorization. Tìm helper lại trong checkout và kiểm tra acceptance trước khi gọi nó; skill không tuyên bố các task này đã xong. Nếu contract cần dependency chưa có, ghi blocker của lát cắt và tiếp tục phần độc lập được giao, không tự implement task của người khác.

Giữ route versioning qua pipeline Asp.Versioning hiện tại. Khi thêm code lỗi, kiểm tra constant, mapping, tài liệu và test đồng bộ trong phạm vi task. Không tự đổi wire format, policy enumeration-resource hoặc các HTTP status đã accepted.

HTTP happy path cần assert body/ID/version và kết quả state có ý nghĩa; khi AC đòi persistence/audit thật, xác minh trong SQL fixture phù hợp. Chỉ assert 200 không đủ. In-memory fake có ích cho service unit test nhưng không chứng minh constraint/transaction hoặc authorization freshness qua database.
