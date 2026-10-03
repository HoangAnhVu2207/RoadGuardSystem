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

[Trait("Package", "HUY-01")]
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
        results.Should().Contain(x => x.Status == UploadPersistenceStatus.Success);
        results.Should().OnlyContain(x => x.Status == UploadPersistenceStatus.Success || x.Status == UploadPersistenceStatus.Replayed || x.Status == UploadPersistenceStatus.StorageUnavailable);
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
    public async Task LostStorageAcknowledgement_FreshContextRecoversWithoutAnotherInitiation_AndRechecksAuthority()
    {
        await using var db = fixture.CreateRetryingDbContext();
        var storage = new MultipartStorage(() => { }, loseAcknowledgement: true);
        var (actor, upload) = await SeedAsync(db, storage);
        var key = Guid.NewGuid().ToString();
        (await IssueAsync(db, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.StorageUnavailable);
        await using var fresh = fixture.CreateRetryingDbContext();
        await MakeDueAsync(fresh, upload);
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.Success);
        (await IssueAsync(fresh, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
        (await fresh.UploadSessions.AsNoTracking().SingleAsync(x => x.Id == upload)).MultipartPhase.Should().Be("DURABLE");
        storage.Calls.Should().Be(1);
        (await fresh.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor && x.Operation == "UploadPartUrlsIssued")).Should().BeGreaterThan(0);
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
        await MakeDueAsync(fresh, upload);
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.Success);
        storage.Calls.Should().Be(1);
        (await fresh.UploadParts.CountAsync(x => x.UploadSessionId == upload)).Should().Be(1);
        (await fresh.IdempotencyRecords.CountAsync(x => x.ActorUserId == actor && x.Operation == "UploadPartUrlsIssued")).Should().BeGreaterThan(0);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiptRollbackOrLostAck_AfterDurableId_ReplaysWithoutAnotherHandlerSideEffect(bool acknowledgement)
    {
        var fault = new MultipartCommitFault(acknowledgement, receiptOnly: true);
        await using var db = fixture.CreateRetryingDbContext(fault);
        var storage = new MultipartStorage(() => fault.Armed = true);
        var (actor, upload) = await SeedAsync(db, storage); var key = Guid.NewGuid().ToString();
        (await IssueAsync(db, storage, actor, upload, key)).Status.Should().Be(acknowledgement ? UploadPersistenceStatus.Replayed : UploadPersistenceStatus.Success);
        await using var fresh = fixture.CreateRetryingDbContext();
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.Replayed);
        fault.Fired.Should().BeTrue(); storage.Calls.Should().Be(1);
        (await fresh.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor && r.Operation == "UploadPartUrlsIssued")).Should().Be(1);
        (await fresh.AuditLogs.CountAsync(a => a.ActorUserId == actor && a.EventType == "upload_session_created")).Should().Be(1);
    }

    private static Task<int> MakeDueAsync(RoadGuardDbContext db, Guid upload)
        => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartNextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={upload}");

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

    private sealed class MultipartCommitFault(bool acknowledgement, int failures = 1, bool receiptOnly = false) : DbTransactionInterceptor
    {
        public bool Armed { get; set; }
        public bool Fired { get; private set; }
        private int _remaining = failures;
        private void Fault(DbContext? context)
        {
            if (receiptOnly && (context is not RoadGuardDbContext db || !db.IdempotencyRecords.Local.Any(r => r.Operation == "UploadPartUrlsIssued"))) return;
            if (Armed && _remaining-- > 0) { Fired = true; throw new TestTransientException("Synthetic multipart SQL commit fault"); }
        }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        { if (!acknowledgement) Fault(eventData.Context); return ValueTask.FromResult(result); }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        { if (acknowledgement) Fault(eventData.Context); return Task.CompletedTask; }
    }
    private sealed class MultipartStorage(Action initiated, bool loseAcknowledgement = false) : IUploadObjectStorage, IMultipartRecoveryStorage
    {
        public Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(Calls == 0 ? [] : ["multipart-1"]);
        public Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
