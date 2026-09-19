---
name: roadguard-csharp-api
description: Use when implementing an assigned RoadGuard ASP.NET Core controller, DTO or application service, including authorization, ProblemDetails, versioning and retryable HTTP commands. Not for schema ownership or acceptance review.
---

# Application service và API RoadGuard

Áp dụng cùng [quy tắc repository](../../../AGENTS.md); skill không giao thêm task hoặc cấp quyền sửa schema. Kiểm tra task P1, dependency P2 đã tích hợp và exclusive paths trước khi chỉnh sửa. Yêu cầu chỉ đọc vẫn chỉ đọc.

## Chốt contract của lát cắt

Từ AC/use case, xác định actor, trạng thái trước/sau, project/resource scope, failure code, expected version, audit event và retry outcome. Đọc [điểm nối API](references/api-map.md) để dùng pipeline hiện tại. Phân biệt hướng dẫn cần thực hiện trong task với helper đã tồn tại; không giả định có project-membership guard chỉ vì ADR mô tả nó.

1. DTO biểu diễn input/output công khai, không trả EF entity hoặc nhận entity để bind hàng loạt. Chỉ cho phép các field client được sửa; phân trang/sort phải có giới hạn theo contract/options. Giữ kiểu, precision và nullability từ đặc tả.
2. Controller chỉ bind/validate/dispatch/map HTTP. Services quyết định quyền, cross-aggregate và transition; repository cung cấp truy vấn/lưu trữ có scope. Truyền `CancellationToken` từ HTTP xuống I/O. Đăng ký DI theo scope hiện có; Program/registration/project files là hotspot cần quyền task.
3. Với thao tác yêu cầu đăng nhập, kiểm tra User/Session hiện tại theo ADR 002; JWT role là snapshot. Với use case theo project, non-Supervisor cần membership còn active/effective và role từ `ProjectMember`; Supervisor bypass dựa trên role server hiện tại. Xác minh resource thuộc project được phép; project ID trong URL/token/body không tự chứng minh quyền. Login/identity flow không theo project giữ contract task riêng, không tự thêm membership dependency.
4. Lệnh retry phải recheck quyền hiện tại trước khi trả stored outcome. Key theo actor/project/operation, cùng key khác payload trả conflict. Không phát minh cơ chế retry mới khi persistence primitive đáp ứng contract; đọc skill persistence nếu phải ghép transaction.
5. Dùng transition/invariant rõ ràng; write + audit/outbox chung transaction khi cùng DB. Expected version phải tham gia kiểm tra concurrency, không chỉ được đọc rồi bỏ qua. Map stale version và uniqueness đúng contract; không retry mù quyết định nghiệp vụ đã stale.
6. Map kết quả/lỗi qua ProblemDetails hiện tại: HTTP status, stable `code`, `correlationId`, lỗi field đã sanitize. Không bắt mọi exception rồi trả 200/null; không lộ stack, SQL hoặc secret. Chọn 401/403/404/409 theo contract đã thống nhất, giữ quy tắc không tiết lộ resource ngoài scope.

## Ví dụ lát cắt nghiệp vụ

Khi task được giao là PM xác minh defect: chứng minh project access và measurement đã submit, chỉ cho phép transition theo đặc tả sang VERIFIED/REJECTED, kiểm tra expected version, commit audit cùng quyết định. Negative test gồm wrong-project, sai role, thiếu measurement và stale version; positive test kiểm tra state/audit/response thực. Đây là ví dụ thiết kế, không phải endpoint hay method đã tồn tại và không tự giao task defect.

## Kiểm chứng và bàn giao

Dùng [roadguard-csharp-testing](../roadguard-csharp-testing/SKILL.md): unit cho policy/transition, API qua factory cho contract/forbidden/invalid transition, SQL thật cho các đảm bảo persistence liên quan. Mock auth có thể kiểm tra mapping nhưng không chứng minh revocation/membership thật. Self-review chú ý overposting, cross-project reads, replay sau mất quyền, immutable content, double effects và credential leakage. Ghi packet theo workflow; không tự đánh dấu Done.

Ví dụ gọi: `$roadguard-csharp-api Triển khai endpoint thuộc task P1 đã giao, kèm DTO, service và test contract.`
