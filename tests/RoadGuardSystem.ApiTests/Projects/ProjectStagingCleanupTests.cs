using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.ApiTests.Infrastructure;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

[Collection(AuthenticationApiFixture.Name)]
public sealed class ProjectStagingCleanupTests(AuthenticationSqlServerFixture sql)
{
    [Theory]
    [InlineData(UploadSessionStatus.Pending)]
    [InlineData(UploadSessionStatus.Uploading)]
    [InlineData(UploadSessionStatus.Verifying)]
    [InlineData(UploadSessionStatus.Verified)]
    [InlineData(UploadSessionStatus.Failed)]
    public async Task ExpiredUnconsumedUploadsAreFencedAndCleanupRetries(UploadSessionStatus status)
    {
        var actor = await sql.CreateUserAsync("cleanup-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var file = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow.AddDays(-2);
        var key = $"uploads/{file:N}";
        var hash = new string('a', 64);
        var session = UploadSession.Create(Guid.NewGuid(), file, actor.Id, key, "DOCUMENT",
            "application/pdf", 100, hash, 8388608, now.AddHours(1));
        if (status != UploadSessionStatus.Pending) session.StartUploading("incomplete-multipart", now);
        await using (var db = sql.CreateDbContext())
        {
            db.AddRange(StoredFile.Create(file, key, "test.pdf", "application/pdf", 100, hash, actor.Id, now, null),
                session, FileScope.CreateHandoverStaging(Guid.NewGuid(), file, actor.Id, now));
            await db.SaveChangesAsync();
            if (status is UploadSessionStatus.Verifying or UploadSessionStatus.Verified or UploadSessionStatus.Failed)
            {
                session.StartVerification(Convert.ToBase64String(session.RowVersion), now);
                await db.SaveChangesAsync();
                if (status == UploadSessionStatus.Verified) session.MarkVerified();
                if (status == UploadSessionStatus.Failed) session.MarkFailed("file_checksum_mismatch");
                await db.SaveChangesAsync();
            }
        }

        var storage = new CleanupStorage { FailDeletionOnce = true };
        await Sweep(storage);
        await using (var db = sql.CreateDbContext())
        {
            var claimed = await db.UploadSessions.AsNoTracking().SingleAsync(row => row.Id == session.Id);
            Assert.Equal(UploadSessionStatus.Failed, claimed.Status);
            Assert.Equal("staging_expired", claimed.FailureCode);
            await db.UploadSessions.Where(row => row.Id == session.Id).ExecuteUpdateAsync(update =>
                update.SetProperty(row => row.MultipartNextCheckAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }
        await Sweep(storage);
        await using (var db = sql.CreateDbContext())
        {
            var cleaned = await db.UploadSessions.AsNoTracking().SingleAsync(row => row.Id == session.Id);
            Assert.Equal("staging_expired_cleaned", cleaned.FailureCode);
            Assert.Null(cleaned.MultipartNextCheckAt);
            Assert.True(await db.Files.AnyAsync(row => row.Id == file));
            Assert.True(await db.FileScopes.AnyAsync(row => row.FileId == file && row.ProjectId == null));
        }
        Assert.Equal(2, storage.Deletions.Count(keyToDelete => keyToDelete == key));
        Assert.Contains(key, storage.Aborted);
        await Sweep(storage);
        Assert.Equal(2, storage.Deletions.Count(keyToDelete => keyToDelete == key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FinalizeAndCleanupSerializeAndPreserveConsumedFiles(bool expired)
    {
        var owner = await sql.CreateUserAsync("cleanup-race-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.Supervisor);
        var pm = await sql.CreateUserAsync("cleanup-race-pm-" + Guid.NewGuid().ToString("N"), "Current1!", UserRoleCode.ProjectManager);
        var staged = await VerifiedProjectHandoverFixture.StageAsync(sql, owner.Id,
            expired ? DateTimeOffset.UtcNow.AddDays(-2) : DateTimeOffset.UtcNow);
        await using var factory = new AuthenticationWebApplicationFactory(sql.ConnectionString);
        using var web = await WebCookieTestClient.Login(factory, owner.Email!);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new
            {
                code = "CLEANUP-" + Guid.NewGuid().ToString("N"),
                name = "Cleanup race",
                primaryPmId = pm.Id,
                handoverDate = "2026-11-01",
                warrantyEndDate = "2027-11-01",
                handoverFileIds = new[] { staged.FileId }
            })
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        request.Headers.TryAddWithoutValidation("If-Match", staged.ETag);
        var storage = new CleanupStorage();
        var finalize = web.SendAsync(request);
        await Task.WhenAll(finalize, Sweep(storage));
        using var response = await finalize;
        Assert.Equal(expired ? HttpStatusCode.Conflict : HttpStatusCode.Created, response.StatusCode);
        if (expired) Assert.Contains($"uploads/{staged.FileId:N}", storage.Deletions);
        else Assert.DoesNotContain($"uploads/{staged.FileId:N}", storage.Deletions);
        await using var check = sql.CreateDbContext();
        Assert.Equal(!expired, await check.HandoverDocuments.AnyAsync(row => row.FileId == staged.FileId));
        if (!expired)
        {
            // A consumed file is not staging any more, even after its upload lease expires.
            await check.UploadSessions.Where(row => row.FileId == staged.FileId).ExecuteUpdateAsync(update =>
                update.SetProperty(row => row.ExpiresAt, DateTimeOffset.UtcNow.AddDays(-2)));
            await Sweep(storage);
            Assert.DoesNotContain($"uploads/{staged.FileId:N}", storage.Deletions);
            Assert.True(await check.HandoverDocuments.AnyAsync(row => row.FileId == staged.FileId));
        }
    }

    private async Task Sweep(CleanupStorage storage)
    {
        await using var db = sql.CreateDbContext();
        await new UploadPersistenceService(db, new IdempotencyOperationService(db), storage).RecoverMultipartsAsync();
    }

    private sealed class CleanupStorage : IUploadObjectStorage, IStagingCleanupStorage
    {
        public bool FailDeletionOnce { get; init; }
        public List<string> Deletions { get; } = [];
        public List<string> Aborted { get; } = [];

        public Task DeleteStagedObjectAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            Deletions.Add(objectKey);
            if (FailDeletionOnce && Deletions.Count(key => key == objectKey) == 1)
                throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Fixture deletion failure");
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(["incomplete-multipart"]);
        public Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default)
        {
            Aborted.Add(objectKey);
            return Task.CompletedTask;
        }
        public Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
