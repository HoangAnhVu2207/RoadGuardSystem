# Primitive và giới hạn cần kiểm tra

| Nhu cầu | Source / test hiện tại |
|---|---|
| DbContext / validation tại save boundary | [RoadGuardDbContext](../../../../RoadGuardSystem.Repositories/RoadGuardDbContext.cs) |
| Mapping JSON / immutable metadata | [UserSessionConfiguration](../../../../RoadGuardSystem.Repositories/Configurations/UserSessionConfiguration.cs), [SessionDeviceMetadataValidator](../../../../RoadGuardSystem.BusinessObjects/Identity/SessionDeviceMetadataValidator.cs) |
| Atomic transaction | [RoadGuardTransactionService](../../../../RoadGuardSystem.Repositories/Transactions/RoadGuardTransactionService.cs) |
| Scoped idempotency / fingerprint / outcome | [IdempotencyOperationService](../../../../RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationService.cs), [result](../../../../RoadGuardSystem.Repositories/Idempotency/IdempotencyOperationResult.cs), [SQL tests](../../../../tests/RoadGuardSystem.IntegrationTests/Persistence/P202TransactionAndIdempotencyTests.cs) |
| Consumer effect dedup | [ConsumerEffectService](../../../../RoadGuardSystem.Repositories/Messaging/ConsumerEffectService.cs), [concurrency/outbox tests](../../../../tests/RoadGuardSystem.IntegrationTests/Persistence/P202ConcurrencyAndOutboxTests.cs) |
| Rowversion | [IHasRowVersion](../../../../RoadGuardSystem.BusinessObjects/Concurrency/IHasRowVersion.cs), [RowVersionConvention](../../../../RoadGuardSystem.Repositories/Concurrency/RowVersionConvention.cs) |
| Audit sanitization | [AuditSnapshotBuilder](../../../../RoadGuardSystem.Repositories/Auditing/AuditSnapshotBuilder.cs), [SensitiveJsonSanitizer](../../../../RoadGuardSystem.BusinessObjects/Auditing/SensitiveJsonSanitizer.cs), [validation/redaction tests](../../../../tests/RoadGuardSystem.IntegrationTests/Persistence/P202ValidationAndRedactionTests.cs) |
| SRID guard / SQL spatial proof | [SpatialValidation](../../../../RoadGuardSystem.BusinessObjects/Spatial/SpatialValidation.cs), [SQL round trip](../../../../tests/RoadGuardSystem.IntegrationTests/Persistence/SqlServerSpatialRoundTripTests.cs), [negative persistence](../../../../tests/RoadGuardSystem.IntegrationTests/Persistence/SpatialPersistenceNegativeTests.cs) |
| Transaction contracts / lifecycle | [P202ServiceContractTests](../../../../tests/RoadGuardSystem.IntegrationTests/Persistence/P202ServiceContractTests.cs), [P202MigrationLifecycleTests](../../../../tests/RoadGuardSystem.IntegrationTests/Persistence/P202MigrationLifecycleTests.cs) |

## Các quyết định không được suy ra từ tên helper

- `RoadGuardTransactionService` dùng execution strategy khi chưa có transaction; khi có transaction, callback và SaveChanges tham gia transaction của caller. Kiểm tra scope/lifetime thật trước khi dùng; không suy ra rằng hai DbContext bất kỳ tự chia sẻ transaction.
- `IdempotencyOperationService` tự mở transaction cho first execution, so fingerprint khi replay và xử lý một số uniqueness races. Kiểm tra nullable actor/project scope có đúng AC không. Stored outcome có thể chứa dữ liệu cần bảo vệ: kiểm tra authorization hiện tại trước khi trả, và kiểm soát payload theo contract.
- Retry-enabled test chỉ chứng minh các tình huống test thực sự chạy. Nếu AC cần connection loss, uncertain commit hoặc crash recovery, cần tái hiện đúng tình huống đó; không dùng tên test thay bằng chứng.
- Concurrency token không thay thế expected-version comparison ở application/contract. Một context đọc lại version mới rồi update mà bỏ token của client không chứng minh stale-client rejection.
- P202 và spatial probe là synthetic aggregates trong test; không copy bảng test vào model production. `EnsureCreated` của probe không kiểm tra migration của `RoadGuardDbContext`.
- Save-boundary guards phải được xét riêng khi dùng raw SQL/bulk APIs; các đường đi này có thể không chạy validation/tracking như thao tác EF thông thường. Chỉ chọn khi task cần và có kiểm chứng integrity tương ứng.

Khi cần DbContext cho schema production, đọc [IdentitySqlServerFixture](../../../../tests/RoadGuardSystem.IntegrationTests/Infrastructure/IdentitySqlServerFixture.cs): fixture tạo DB cô lập, thay probe schema bằng migrations của `RoadGuardDbContext`. Tên Identity không có nghĩa nó chứng minh tất cả aggregate hoặc migration tương lai.
