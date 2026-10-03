using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Files;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

public sealed class PrivateMultipartRetrySqlTests(IdentitySqlServerFixture fixture) : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessThenSqlRollback_RetryConcurrentSameKeyAndFreshContext_ReusesOneMultipart(bool acknowledgement)
    {
        var fault = new MultipartCommitFault(acknowledgement);
        await using var db = fixture.CreateRetryingDbContext(fault);
        var storage = new MultipartStorage(() => { db.Database.CurrentTransaction.Should().BeNull(); fault.Armed = true; }) { Pause = true };
        var (actor, upload) = await SeedAsync(db, storage);
        var key = Guid.NewGuid().ToString();
        await using var other = fixture.CreateRetryingDbContext();
        var first = IssueAsync(db, storage, actor, upload, key);
        await storage.Initiated.Task.WaitAsync(TimeSpan.FromSeconds(20));
        var competing = IssueAsync(other, storage, actor, upload, key);
        storage.Release.TrySetResult();
        var results = await Task.WhenAll(first, competing);
        results.Count(x => x.Status == UploadPersistenceStatus.Replayed).Should().Be(acknowledgement ? 2 : 1);
        results.Count(x => x.Status == UploadPersistenceStatus.Success).Should().Be(acknowledgement ? 0 : 1);
        fault.Fired.Should().BeTrue();
        storage.Calls.Should().Be(1);
        await using var fresh = fixture.CreateRetryingDbContext();
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.Replayed);
        (await IssueAsync(fresh, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
        storage.Calls.Should().Be(1);
        var session = await fresh.UploadSessions.AsNoTracking().SingleAsync(x => x.Id == upload);
        session.StorageUploadId.Should().Be("multipart-1");
        session.FailureCode.Should().BeNull();
        (await fresh.UploadParts.CountAsync(x => x.UploadSessionId == upload)).Should().Be(1);
        (await fresh.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor && x.Operation == "UploadPartUrlsIssued" && x.IdempotencyKey == key)).Should().Be(1);
    }

    [Fact]
    public async Task LostStorageAcknowledgement_FreshContextFailsClosedWithoutAnotherInitiation_AndRechecksAuthority()
    {
        await using var db = fixture.CreateRetryingDbContext();
        var storage = new MultipartStorage(() => { }, loseAcknowledgement: true);
        var (actor, upload) = await SeedAsync(db, storage);
        var key = Guid.NewGuid().ToString();
        (await IssueAsync(db, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.StorageUnavailable);
        await using var fresh = fixture.CreateRetryingDbContext();
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.StorageUnavailable);
        (await IssueAsync(fresh, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.StorageUnavailable);
        (await fresh.UploadSessions.AsNoTracking().SingleAsync(x => x.Id == upload)).FailureCode.Should().StartWith("multipart_initiating:");
        storage.Calls.Should().Be(1);
        (await fresh.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor && x.Operation == "UploadPartUrlsIssued")).Should().Be(0);
        var user = await fresh.Users.SingleAsync(x => x.Id == actor); user.Status = UserStatus.Suspended; await fresh.SaveChangesAsync();
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.NotFound);
        storage.Calls.Should().Be(1);
    }

    [Fact]
    public async Task ExhaustedSqlRetries_AcknowledgedStorageDoesNotPermitFreshRequestToInitiateAgain()
    {
        var fault = new MultipartCommitFault(false, failures: 3);
        await using var db = fixture.CreateRetryingDbContext(fault);
        var storage = new MultipartStorage(() => fault.Armed = true);
        var (actor, upload) = await SeedAsync(db, storage);
        var key = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException>(() => IssueAsync(db, storage, actor, upload, key));
        storage.Calls.Should().Be(1);
        await using var fresh = fixture.CreateRetryingDbContext();
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.StorageUnavailable);
        storage.Calls.Should().Be(1);
        (await fresh.UploadParts.CountAsync(x => x.UploadSessionId == upload)).Should().Be(0);
        (await fresh.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor && x.Operation == "UploadPartUrlsIssued")).Should().Be(0);
    }

    [Fact]
    public async Task ClaimCommitAcknowledgementLoss_IsRecoveredBeforeOneStorageCall()
    {
        var fault = new MultipartCommitFault(true);
        await using var db = fixture.CreateRetryingDbContext(fault);
        var storage = new MultipartStorage(() => db.Database.CurrentTransaction.Should().BeNull());
        var (actor, upload) = await SeedAsync(db, storage);
        fault.Armed = true;
        (await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
        fault.Fired.Should().BeTrue(); storage.Calls.Should().Be(1);
    }

    private async Task<(Guid Actor, Guid Upload)> SeedAsync(RoadGuardDbContext db, IUploadObjectStorage storage)
    {
        await fixture.SeedRolesAsync(db);
        var actor = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(),
            DisplayName = "Synthetic private multipart fixture", PasswordHash = "non-login-fixture", RoleCode = UserRoleCode.Reporter,
            Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var result = await new UploadPersistenceService(db, new IdempotencyOperationService(db), storage).CreateAsync(
            new(actor, null, null, "REPORT_PHOTO", "test.jpg", "image/jpeg", 4, new string('a', 64), 8388608,
                DateTimeOffset.UtcNow.AddHours(24), Guid.NewGuid().ToString(), new string('b', 64), null));
        result.Status.Should().Be(UploadPersistenceStatus.Success);
        return (actor, result.Session!.Id);
    }
    private static Task<UploadPartUrlsPersistenceResult> IssueAsync(RoadGuardDbContext db, IUploadObjectStorage storage, Guid actor, Guid upload, string key)
        => new UploadPersistenceService(db, new IdempotencyOperationService(db), storage).GetPartUrlsAsync(actor, null, upload, [1], key,
            new string('c', 64), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15));

    private sealed class MultipartCommitFault(bool acknowledgement, int failures = 1) : DbTransactionInterceptor
    {
        public bool Armed { get; set; }
        public bool Fired { get; private set; }
        private int _remaining = failures;
        private void Fault() { if (Armed && _remaining-- > 0) { Fired = true; throw new TestTransientException("Synthetic multipart SQL commit fault"); } }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        { if (!acknowledgement) Fault(); return ValueTask.FromResult(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { if (acknowledgement) Fault(); return Task.CompletedTask; }
    }
    private sealed class MultipartStorage(Action initiated, bool loseAcknowledgement = false) : IUploadObjectStorage
    {
        public int Calls { get; private set; }
        public bool Pause { get; init; }
        public TaskCompletionSource Initiated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
        { Calls++; initiated(); Initiated.TrySetResult(); if (Pause) await Release.Task.WaitAsync(cancellationToken); if (loseAcknowledgement) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Synthetic lost acknowledgement after success"); return $"multipart-{Calls}"; }
        public Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PresignedUploadPart>>(partNumbers.Select(n => new PresignedUploadPart(n, $"https://storage.test/{uploadId}/{n}", expiresAt)).ToArray());
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
