---
name: roadguard-csharp-persistence
description: Use when implementing an assigned RoadGuard EF Core SQL Server mapping, migration, repository or atomic persistence operation involving spatial data, JSON, rowversion, idempotency or audit/outbox. Not for moving application policy into repositories.
---

# EF Core / SQL Server dành cho RoadGuard

Đọc [AGENTS](../../../AGENTS.md), task P2/dependency và quyền file trước khi sửa. P1 chỉ chỉnh persistence khi có ngoại lệ owner được ghi rõ. BusinessObjects entity shape là hotspot theo handoff; skill không cấp quyền mở lại schema đã bàn giao. Yêu cầu review/diagnose không tự thành fix.

## Mapping và query

- Đọc Data Dictionary theo field trước ERD/Domain/Use Case; giữ SQL type, nullability, length, precision, FK/delete behavior, enum numeric value và indexes. Conflict về dữ liệu/quy trình cần quyết định owner, không tự chọn. [Bản đồ persistence](references/persistence-map.md) chỉ ra helper hiện tại.
- Truy vấn giới hạn theo project/resource trước materialization; quyền nghiệp vụ vẫn do Services quyết định. Projection chỉ lấy field cần, `AsNoTracking` cho read không update, paging có thứ tự ổn định. Kiểm tra SQL translation và số query khi AC có rủi ro N+1; không trả `IQueryable` xuyên biên API hoặc materialize toàn bảng để lọc quyền.
- GPS dùng `geography(4326)`, engineering geometry dùng SRID UTM của project. Gán SRID không phải chuyển tọa độ. Validator SRID hiện có không chứng minh toàn bộ tọa độ/hình học hợp lệ; test đúng ràng buộc task bằng SQL Server/NetTopologySuite thật.
- JSON `nvarchar(max)` cần `ISJSON` và application schema validation. Dùng allowlist, giới hạn theo options/contract; valid JSON không đồng nghĩa payload hợp lệ. Exactly-one-target cần validation và DB constraint tương ứng.

## Transaction, concurrency và retry

Đọc implementation của primitive trước khi ghép chúng. Xác định ai sở hữu transaction, DbContext, `SaveChanges`, execution strategy và callback; không bọc các helper tự mở transaction lồng nhau theo thói quen.

Mutable aggregate dùng rowversion và expected version từ caller; stale update phải thất bại theo contract. SQL duplicate-key race khác concurrency update; chỉ phân loại lỗi cụ thể đã hiểu, không biến mọi DbUpdateException thành conflict hoặc thành công.

Atomic command lưu domain + audit/outbox + idempotency outcome cùng transaction khi cùng DB. Callback có thể chạy lại: không gửi email, gọi AI, ghi file bên ngoài hay phát event trực tiếp trong callback; ghi durable intent và dùng worker dedup. Không khẳng định exactly-once external delivery. Recheck access tại Services trước replay; cùng scoped key khác fingerprint trả conflict.

Ví dụ kiểm tra cách ghép: operation retryable đã dùng `IdempotencyOperationService` phải xem transaction nó mở trước khi thêm `RoadGuardTransactionService`. Test rollback domain/audit/outcome và hai context cạnh tranh cùng key; kiểm tra chỉ một effect và outcome theo contract. Test execution-strategy compatibility không tự chứng minh unknown-commit recovery.

## Migration và bằng chứng

Tạo migration mới trên schema dependency đã tích hợp; không sửa migration đã chia sẻ. Kiểm tra model/snapshot/up/down và phục hồi dữ liệu; chạy upgrade/recovery trên DB test cô lập theo task. Không chạy migration lên DB shared/production từ skill này. Giữ configuration/secret ngoài source và output.

Dùng [testing skill](../roadguard-csharp-testing/SKILL.md) cho constraints, spatial, rollback, concurrency và migration thật. EF InMemory/SQLite không thay SQL Server evidence. Bàn giao cả migration/recovery note, thay đổi entity shape và files/tests cho đúng Person; implementation dừng ở `Ready for review`.

Ví dụ gọi: `$roadguard-csharp-persistence Triển khai mapping và repository thuộc task P2 đã giao, kèm migration và kiểm chứng SQL Server.`
