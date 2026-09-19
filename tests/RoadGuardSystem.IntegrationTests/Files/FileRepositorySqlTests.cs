using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

[Trait("TaskId", "P2-04")]
public sealed class FileRepositorySqlTests : IClassFixture<IdentitySqlServerFixture>, IDisposable
{
    private readonly IdentitySqlServerFixture _fixture;
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"roadguard-p204-sql-{Guid.NewGuid():N}");

    public FileRepositorySqlTests(IdentitySqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StoreAsync_UnknownOwner_LeavesNoDatabaseOrStorageEffects()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = CreateRepository(context);
        var bytes = Pdf("unknown owner");
        var fileCount = await context.Files.CountAsync();
        var auditCount = await context.AuditLogs.CountAsync(audit => audit.EventType == "FileStored");
        var idempotencyCount = await context.IdempotencyRecords.CountAsync(record => record.Operation == "FileStored");

        var result = await repository.StoreAsync(Request(bytes, Guid.NewGuid(), "unknown-owner"));

        result.Status.Should().Be(FileStoreStatus.OwnerNotFound);
        result.ErrorCode.Should().Be(FileStorageErrorCodes.OwnerNotFound);
        (await context.Files.CountAsync()).Should().Be(fileCount);
        (await context.AuditLogs.CountAsync(audit => audit.EventType == "FileStored")).Should().Be(auditCount);
        (await context.IdempotencyRecords.CountAsync(record => record.Operation == "FileStored")).Should().Be(idempotencyCount);
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAsync_UserWriteReplayAndConflict_PersistExactlyOneSanitizedEffect()
    {
        await using var context = _fixture.CreateDbContext();
        var user = await AddUserAsync(context);
        var repository = CreateRepository(context);
        var bytes = Pdf("verified payload");
        var key = $"file-{Guid.NewGuid():N}";

        var stored = await repository.StoreAsync(Request(bytes, user.Id, key));
        var replayed = await repository.StoreAsync(Request(bytes, user.Id, key));
        var changed = Pdf("changed payload");
        var conflict = await repository.StoreAsync(Request(changed, user.Id, key));

        stored.Status.Should().Be(FileStoreStatus.Stored);
        replayed.Status.Should().Be(FileStoreStatus.Replayed);
        replayed.FileId.Should().Be(stored.FileId);
        conflict.Status.Should().Be(FileStoreStatus.Conflict);
        conflict.ErrorCode.Should().Be(FileStorageErrorCodes.IdempotencyConflict);
        (await context.Files.CountAsync(file => file.Id == stored.FileId)).Should().Be(1);
        (await context.IdempotencyRecords.CountAsync(record => record.Operation == "FileStored" && record.IdempotencyKey == key))
            .Should().Be(1);
        var audits = await context.AuditLogs.AsNoTracking()
            .Where(audit => audit.EventType == "FileStored" && audit.EntityId == stored.FileId)
            .ToListAsync();
        audits.Should().ContainSingle();
        audits[0].ActorUserId.Should().Be(user.Id);
        audits[0].CorrelationId.Should().NotBeNull();
        audits[0].AfterSnapshot.Should().Contain("checksum").And.Contain("size_bytes");
        audits[0].AfterSnapshot.Should().NotContain("verified payload").And.NotContain(_root);
        Directory.EnumerateFiles(Path.Combine(_root, "objects")).Should().ContainSingle();

        await using var read = await new LocalFileContentStore(StorageOptions()).OpenReadAsync(stored.StorageUri!);
        using var copy = new MemoryStream();
        await read.CopyToAsync(copy);
        copy.ToArray().Should().Equal(bytes);

        var deleteOwner = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [Users] WHERE [Id] = {user.Id}");
        await deleteOwner.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task StoreAsync_SystemWrite_AllowsNullOwnerAndRecordsSystemSource()
    {
        await using var context = _fixture.CreateDbContext();
        var repository = CreateRepository(context);
        var bytes = Pdf("system import");
        var request = Request(bytes, null, $"system-{Guid.NewGuid():N}") with { IsSystemOriginated = true };

        var result = await repository.StoreAsync(request);

        result.Status.Should().Be(FileStoreStatus.Stored);
        var file = await context.Files.AsNoTracking().SingleAsync(candidate => candidate.Id == result.FileId);
        file.UploadedByUserId.Should().BeNull();
        var audit = await context.AuditLogs.AsNoTracking().SingleAsync(entry => entry.EntityId == result.FileId);
        audit.ActorUserId.Should().BeNull();
        audit.Source.Should().Be("SYSTEM");
    }

    [Fact]
    public async Task StoreAsync_DatabaseFailure_RemovesStoredObjectAndRollsBackEffects()
    {
        var interceptor = new ThrowOnceCommandInterceptor(command => command.Contains("INSERT INTO [Files]", StringComparison.Ordinal));
        await using var context = _fixture.CreateDbContext(interceptor);
        var user = await AddUserAsync(context);
        var repository = CreateRepository(context);
        var bytes = Pdf("database failure");

        var store = () => repository.StoreAsync(Request(bytes, user.Id, $"failure-{Guid.NewGuid():N}"));

        var exception = await store.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<TestTransientException>();
        interceptor.FailureCount.Should().Be(1);
        Directory.EnumerateFiles(Path.Combine(_root, "objects")).Should().BeEmpty();
        await using var verification = _fixture.CreateDbContext();
        (await verification.Files.CountAsync(file => file.Checksum == Hash(bytes))).Should().Be(0);
        (await verification.AuditLogs.CountAsync(audit => audit.AfterSnapshot != null && audit.AfterSnapshot.Contains(Hash(bytes))))
            .Should().Be(0);
    }

    [Fact]
    public async Task DatabaseTrigger_BlocksDirectFileUpdateAndDelete()
    {
        await using var context = _fixture.CreateDbContext();
        var file = StoredFile.Create(
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            "immutable.pdf",
            "application/pdf",
            10,
            new string('a', 64),
            null,
            DateTimeOffset.UtcNow,
            null);
        context.Files.Add(file);
        await context.SaveChangesAsync();

        var update = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Files] SET [StorageUri] = {Guid.NewGuid().ToString("N")} WHERE [Id] = {file.Id}");
        var delete = () => context.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM [Files] WHERE [Id] = {file.Id}");

        await update.Should().ThrowAsync<SqlException>();
        await delete.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task DatabaseConstraints_RejectInvalidSizeChecksumOwnerAndDuplicateUri()
    {
        await using var context = _fixture.CreateDbContext();
        var validUri = Guid.NewGuid().ToString("N");
        await InsertRawFileAsync(
            context, Guid.NewGuid(), validUri, 10, new string('a', 64), null);

        var negativeSize = () => InsertRawFileAsync(
            context, Guid.NewGuid(), Guid.NewGuid().ToString("N"), -1, new string('b', 64), null);
        var uppercaseChecksum = () => InsertRawFileAsync(
            context, Guid.NewGuid(), Guid.NewGuid().ToString("N"), 10, new string('A', 64), null);
        var missingOwner = () => InsertRawFileAsync(
            context, Guid.NewGuid(), Guid.NewGuid().ToString("N"), 10, new string('c', 64), Guid.NewGuid());
        var duplicateUri = () => InsertRawFileAsync(
            context, Guid.NewGuid(), validUri, 10, new string('d', 64), null);

        await negativeSize.Should().ThrowAsync<SqlException>();
        await uppercaseChecksum.Should().ThrowAsync<SqlException>();
        await missingOwner.Should().ThrowAsync<SqlException>();
        await duplicateUri.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task StoreAsync_ConcurrentIdenticalRetry_ConvergesToOneEffect()
    {
        Guid userId;
        await using (var setup = _fixture.CreateDbContext())
        {
            userId = (await AddUserAsync(setup)).Id;
        }

        await using var firstContext = _fixture.CreateDbContext();
        await using var secondContext = _fixture.CreateDbContext();
        var firstRepository = CreateRepository(firstContext);
        var secondRepository = CreateRepository(secondContext);
        var bytes = Pdf("parallel retry");
        var key = $"parallel-{Guid.NewGuid():N}";

        var results = await Task.WhenAll(
            firstRepository.StoreAsync(Request(bytes, userId, key)),
            secondRepository.StoreAsync(Request(bytes, userId, key)));

        results.Select(result => result.Status).Should().BeEquivalentTo(
            [FileStoreStatus.Stored, FileStoreStatus.Replayed]);
        results.Select(result => result.FileId).Distinct().Should().ContainSingle();
        await using var verification = _fixture.CreateDbContext();
        (await verification.IdempotencyRecords.CountAsync(record =>
            record.Operation == "FileStored" && record.IdempotencyKey == key)).Should().Be(1);
        var fileId = results[0].FileId;
        (await verification.Files.CountAsync(file => file.Id == fileId)).Should().Be(1);
        (await verification.AuditLogs.CountAsync(audit => audit.EventType == "FileStored" && audit.EntityId == fileId))
            .Should().Be(1);
        Directory.EnumerateFiles(Path.Combine(_root, "objects")).Should().ContainSingle();
    }

    [Fact]
    public async Task MigrationLifecycle_FromP210_DowngradesAndReappliesFileSchema()
    {
        var fixture = new SqlServerTestFixture();
        await fixture.InitializeAsync();
        try
        {
            await using (var probe = fixture.CreateDbContext())
            {
                await probe.Database.EnsureDeletedAsync();
            }

            var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite())
                .Options;
            await using var context = new RoadGuardDbContext(options);
            var migrator = context.GetService<IMigrator>();
            const string previous = "20260918185738_EnforceSecurityLogSafeCodes";

            await migrator.MigrateAsync(previous);
            (await FileSchemaObjectCountAsync(context)).Should().Be(0);

            await context.Database.MigrateAsync();
            (await FileSchemaObjectCountAsync(context)).Should().Be(4);

            await migrator.MigrateAsync(previous);
            (await FileSchemaObjectCountAsync(context)).Should().Be(0);

            await context.Database.MigrateAsync();
            (await FileSchemaObjectCountAsync(context)).Should().Be(4);
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private FileRepository CreateRepository(RoadGuardDbContext context)
    {
        var contentStore = new LocalFileContentStore(StorageOptions());
        return new FileRepository(context, contentStore, new IdempotencyOperationService(context));
    }

    private FileStorageOptions StorageOptions() => new() { RootPath = _root, MaximumSizeBytes = 1024 * 1024 };

    private static StoreFileRequest Request(byte[] bytes, Guid? userId, string key)
        => new(
            new MemoryStream(bytes),
            "evidence.pdf",
            "application/pdf",
            Hash(bytes),
            bytes.Length,
            userId,
            IsSystemOriginated: false,
            key,
            Guid.NewGuid());

    private async Task<ApplicationUser> AddUserAsync(RoadGuardDbContext context)
    {
        await _fixture.SeedRolesAsync(context);
        var userName = $"file_user_{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            DisplayName = "File Owner",
            PasswordHash = "test-hash-value",
            RoleCode = UserRoleCode.DroneOperator,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static byte[] Pdf(string value) => Encoding.UTF8.GetBytes($"%PDF-1.7 {value}");

    private static string Hash(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static Task<int> FileSchemaObjectCountAsync(RoadGuardDbContext context)
        => context.Database.SqlQueryRaw<int>(
            """
            SELECT CAST(
                (SELECT COUNT(*) FROM sys.tables WHERE [name] = 'Files') +
                (SELECT COUNT(*) FROM sys.indexes WHERE [name] = 'UX_Files_StorageUri') +
                (SELECT COUNT(*) FROM sys.check_constraints WHERE [name] = 'CK_Files_Checksum_Sha256Lowercase') +
                (SELECT COUNT(*) FROM sys.triggers WHERE [name] = 'TR_Files_Immutable')
                AS int) AS [Value]
            """)
            .SingleAsync();

    private static Task<int> InsertRawFileAsync(
        RoadGuardDbContext context,
        Guid id,
        string storageUri,
        int sizeBytes,
        string checksum,
        Guid? uploaderId)
        => context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [Files]
                ([Id], [StorageUri], [OriginalName], [MimeType], [SizeBytes], [Checksum], [UploadedByUserId], [UploadedAt], [RetentionUntil])
            VALUES
                ({id}, {storageUri}, {"raw.pdf"}, {"application/pdf"}, {sizeBytes}, {checksum}, {uploaderId}, {DateTimeOffset.UtcNow}, {null})
            """);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
