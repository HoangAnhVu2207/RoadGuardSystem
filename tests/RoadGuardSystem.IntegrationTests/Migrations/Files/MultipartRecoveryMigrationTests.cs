using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

[Trait("Package", "HUY-01")]
public sealed class MultipartRecoveryMigrationTests
{
    [Fact]
    public async Task FreshAndBaselineUpgrade_PreserveLegacyClaimAndFileIdentity_NoPendingModelChanges()
    {
        var fixture = new SqlServerTestFixture(createSpatialProbeSchema: false); await fixture.InitializeAsync();
        try
        {
            await using var db = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);

            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync();
            await migrator.MigrateAsync();
            db.Database.HasPendingModelChanges().Should().BeFalse();
            await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code='REPORTER') INSERT Roles(Code,Name,NormalizedName,IsActive) VALUES('REPORTER','Reporter','REPORTER',1)"); var actor = Guid.NewGuid();
            db.Users.Add(new ApplicationUser
            {
                Id = actor,
                UserName = actor.ToString(),
                NormalizedUserName = actor.ToString().ToUpperInvariant(),
                DisplayName = "Synthetic migration legacy",
                PasswordHash = "non-login",
                RoleCode = UserRoleCode.Reporter,
                Status = UserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            }); await db.SaveChangesAsync();
            var storage = new LegacyStorage(); var repo = new UploadPersistenceService(db, new IdempotencyOperationService(db), storage);
            var upload = Guid.NewGuid(); var file = Guid.NewGuid(); var objectKey = $"uploads/{file:N}";
            var now = DateTimeOffset.UtcNow;
            var project = Guid.NewGuid();
            db.Projects.Add(Project.Create(project, project.ToString(), "Multipart migration fixture", null, null, null, null, now));
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Files (Id,StorageUri,OriginalName,MimeType,SizeBytes,Checksum,UploadedByUserId,UploadedAt) VALUES ({file},{objectKey},'fixture.jpg','image/jpeg',4,{new string('a', 64)},{actor},{now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT UploadSessions (Id,FileId,OwnerUserId,ObjectKey,Purpose,MediaType,ExpectedSizeBytes,ExpectedChecksumSha256,PartSizeBytes,ExpiresAt,Status,FailureCode) VALUES ({upload},{file},{actor},{objectKey},'REPORT_PHOTO','image/jpeg',4,{new string('a', 64)},8388608,{now.AddHours(24)},1,'multipart_initiating:historic')");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT FileScopes (Id,FileId,ProjectId,OwnerUserId,Purpose,CreatedAt) VALUES ({Guid.NewGuid()},{file},{project},{actor},'REPORT_PHOTO',{now})");
            // Controlled historic-state fixture in the current schema; recovery must reject adoption and retain file identity.
            (await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id=OBJECT_ID('UploadSessions') AND name='MultipartFence'").SingleAsync()).Should().Be(1);
            await migrator.MigrateAsync(); db.ChangeTracker.Clear();
            var legacy = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload);
            legacy.FileId.Should().Be(file); legacy.FailureCode.Should().Be("multipart_initiating:historic"); legacy.MultipartFence.Should().BeNull();
            await repo.RecoverMultipartsAsync();
            legacy = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload);
            legacy.Status.Should().Be(UploadSessionStatus.Failed); legacy.StorageUploadId.Should().BeNull(); storage.Aborted.Should().BeTrue();
            (await db.Files.AsNoTracking().SingleAsync(f => f.Id == file)).StorageUri.Should().Be(objectKey);

            await db.Database.MigrateAsync();
            await using var finalModelCheck = new RoadGuardDbContext(new DbContextOptionsBuilder<RoadGuardDbContext>()
                .UseSqlServer(fixture.ConnectionString, sql => sql.UseNetTopologySuite()).Options);
            finalModelCheck.Database.HasPendingModelChanges().Should().BeFalse();
        }
        finally { await fixture.DisposeAsync(); }
    }
    private sealed class LegacyStorage : IUploadObjectStorage, IMultipartRecoveryStorage
    {
        public bool Aborted { get; private set; }
        public Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(["legacy-unknown"]);
        public Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Legacy adoption forbidden");
        public Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) { Aborted = true; return Task.CompletedTask; }
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Legacy re-initiation forbidden");
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
