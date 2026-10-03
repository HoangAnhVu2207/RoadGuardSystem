using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

public sealed class MultipartRecoveryMigrationTests
{
    [Fact]
    public async Task FreshAndBaselineUpgrade_PreserveLegacyClaimAndFileIdentity_NoPendingModelChanges()
    {
        var fixture = new IdentitySqlServerFixture(); await fixture.InitializeAsync();
        try
        {
            await using var db = fixture.CreateRetryingDbContext();
            await using (var modelCheck = fixture.CreateDbContext()) modelCheck.Database.HasPendingModelChanges().Should().BeFalse();
            await fixture.SeedRolesAsync(db); var actor = Guid.NewGuid();
            db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(),
                DisplayName = "Synthetic migration legacy", PasswordHash = "non-login", RoleCode = UserRoleCode.Reporter, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow }); await db.SaveChangesAsync();
            var storage = new LegacyStorage(); var repo = new UploadPersistenceService(db, new IdempotencyOperationService(db), storage);
            var created = await repo.CreateAsync(new(actor, null, null, "REPORT_PHOTO", "fixture.jpg", "image/jpeg", 4, new string('a', 64), 8388608,
                DateTimeOffset.UtcNow.AddHours(24), Guid.NewGuid().ToString(), new string('b', 64), null));
            var upload = created.Session!.Id; var file = created.Session.FileId;
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET FailureCode='multipart_initiating:historic' WHERE Id={upload}");
            var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            var baseline = migrations[^2];
            await db.GetService<IMigrator>().MigrateAsync(baseline);
            // This is a real old-schema row before upgrade, not a newly fabricated ID/adoption.
            (await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id=OBJECT_ID('UploadSessions') AND name='MultipartFence'").SingleAsync()).Should().Be(0);
            await db.Database.MigrateAsync(); db.ChangeTracker.Clear();
            var legacy = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload);
            legacy.FileId.Should().Be(file); legacy.FailureCode.Should().Be("multipart_initiating:historic"); legacy.MultipartFence.Should().BeNull();
            await repo.RecoverMultipartsAsync();
            legacy = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload);
            legacy.Status.Should().Be(UploadSessionStatus.Failed); legacy.StorageUploadId.Should().BeNull(); storage.Aborted.Should().BeTrue();
            (await db.Files.AsNoTracking().SingleAsync(f => f.Id == file)).StorageUri.Should().Be(created.Session.ObjectKey);
            var down = () => db.GetService<IMigrator>().MigrateAsync(baseline);
            await down.Should().ThrowAsync<Microsoft.Data.SqlClient.SqlException>().WithMessage("*populated downgrade*");
            await using (var finalModelCheck = fixture.CreateDbContext()) finalModelCheck.Database.HasPendingModelChanges().Should().BeFalse();
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
