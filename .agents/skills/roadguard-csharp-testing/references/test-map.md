# Chọn fixture và chạy kiểm tra

Chạy từ repository root. Các filter dưới đây là ví dụ có thật, không phải test thay thế cho task được giao. Đọc class/trait của task trước khi chọn filter.

## Fixture

| Fixture / source | Dùng cho / giới hạn |
|---|---|
| [CustomWebApplicationFactory](../../../../tests/RoadGuardSystem.ApiTests/Infrastructure/CustomWebApplicationFactory.cs) | HTTP platform test, mặc định Production; nạp probe controller từ test assembly. Không tự cung cấp SQL-backed authorization. |
| [SqlServerTestFixture](../../../../tests/RoadGuardSystem.IntegrationTests/Infrastructure/SqlServerTestFixture.cs) | DB cô lập cho spatial probe; khởi tạo bằng EnsureCreated, không phải migration proof. |
| [IdentitySqlServerFixture](../../../../tests/RoadGuardSystem.IntegrationTests/Infrastructure/IdentitySqlServerFixture.cs) | DB cô lập dùng migrations của RoadGuardDbContext, seed role gọi riêng khi cần. |
| [P202TestDbContext / P202SqlServerFixture](../../../../tests/RoadGuardSystem.IntegrationTests/Infrastructure/P202TestDbContext.cs) | Probe cho transaction/rowversion/outbox/idempotency; đọc test để phân biệt probe schema với migration thật. |
| [Migration lifecycle tests](../../../../tests/RoadGuardSystem.IntegrationTests/Persistence/P202MigrationLifecycleTests.cs) | Ví dụ upgrade/recovery, không chứng minh migration mới chưa chạy. |
| [Negative-first workflow](../../../../.antigravity/skills/roadguard-agile-delivery/references/negative-first-workflow.md) | Workflow/evidence đang áp dụng; không tạo bản policy thứ hai. |

SQL fixture đọc `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING`. Nếu variable không được đặt thì dùng Testcontainers; nếu đã đặt nhưng empty/malformed/unreachable thì fail, không tự fallback. Design-time migration dùng biến riêng `ROADGUARD_MIGRATION_CONNECTION_STRING`; đọc factory trước khi chạy lệnh migration.

Fixture có thể tạo/xóa DB GUID riêng và cần quyền phù hợp. Dùng môi trường test/local được phép; không trỏ tới production/shared database để "chạy thử". Không in connection string hoặc enumerate environment secrets. Một số fixture bật sensitive-data logging: chỉ đưa dữ liệu tổng hợp vào test, không dùng credential/customer data thật, không công bố raw output nhạy cảm.

## Inner loop (ví dụ có thật)

```powershell
dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter "FullyQualifiedName~UserRoleCodeTests"
dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --filter "TaskId=P1-01"
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --list-tests --filter "TaskId=P2-02"
dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --filter "TaskId=P2-02"
```

Chạy tuần tự các lệnh tác động chung build output/fixture; không chạy nhiều `dotnet test` song song trên cùng checkout chỉ để nhanh hơn. Concurrency được điều khiển bên trong test có fixture/context độc lập.

## Submission production

```powershell
dotnet restore RoadGuardSystem.slnx
dotnet build RoadGuardSystem.slnx --no-restore --no-incremental
dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore
```

Sau build, chạy affected test projects với `--no-build` cùng configuration. Khi AGENTS/task yêu cầu full solution, dùng:

```powershell
dotnet test RoadGuardSystem.slnx --no-build
```

Theo dõi exit code từng lệnh; dừng dependent checks nếu build fail, không dùng binary cũ. Test count, failed/skipped và identity phải được ghi, không chỉ chép dòng cuối "passed". Nếu command exit 0 nhưng test bắt buộc không được discover/chạy, gate chưa đạt. Runtime/framework/SQL/container và covered inputs thay đổi thì xem lại tính hợp lệ của evidence.

## Prose/tooling phù hợp với repository

```powershell
pwsh -NoProfile -File tests/Tooling/Verify-AntigravitySetup.ps1
pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1
```

Hai verifier kiểm tra setup/docs hiện có; không tự chứng minh mọi skill mới hợp lệ. Với skill mới, thêm frontmatter/UI metadata/local-link validation và scenario sử dụng phù hợp. Không chạy runtime SQL/build suite chỉ vì thêm hướng dẫn Markdown.
