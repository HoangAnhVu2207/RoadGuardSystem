using System.Collections.Concurrent;
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
public sealed class MultipartRecoveryStateSqlTests(IdentitySqlServerFixture fixture) : IClassFixture<IdentitySqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestartFromDurableCalling_TwoReconcilersAndTwoKeys_AuthorizeOneUpload(bool sameKey)
    {
        await using var db = fixture.CreateRetryingDbContext();
        var storage = new RecoveryStorage();
        var (actor, upload, objectKey) = await SeedAsync(db, storage);
        storage.Uploads[objectKey] = ["owned-1"];
        await CallingAsync(db, upload);
        await using var other = fixture.CreateRetryingDbContext();
        var key = Guid.NewGuid().ToString();
        var results = await Task.WhenAll(IssueAsync(db, storage, actor, upload, key), IssueAsync(other, storage, actor, upload, sameKey ? key : Guid.NewGuid().ToString()));
        results.Should().Contain(x => x.Status == UploadPersistenceStatus.Success);
        results.Should().OnlyContain(x => x.Status == UploadPersistenceStatus.Success || x.Status == UploadPersistenceStatus.Replayed || x.Status == UploadPersistenceStatus.StorageUnavailable);
        await using var fresh = fixture.CreateRetryingDbContext();
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().BeOneOf(UploadPersistenceStatus.Success, UploadPersistenceStatus.Replayed);
        storage.Initiations.Should().Be(0);
        var row = await fresh.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload);
        row.StorageUploadId.Should().Be("owned-1"); row.MultipartPhase.Should().Be("DURABLE");
        (await fresh.UploadParts.CountAsync(s => s.UploadSessionId == upload)).Should().Be(1);
        (await fresh.Files.CountAsync(s => s.UploadedByUserId == actor)).Should().Be(1);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("multiple")]
    [InlineData("parts")]
    [InlineData("legacy")]
    [InlineData("expired")]
    public async Task AmbiguousOrTerminal_RequiresNewSession_CleansOnlyExactOwnedKey_AndLateOrphans(string scenario)
    {
        await using var db = fixture.CreateRetryingDbContext();
        var storage = new RecoveryStorage();
        var (actor, upload, key) = await SeedAsync(db, storage);
        storage.Uploads[key + "-neighbor"] = ["foreign"];
        storage.Uploads[key] = scenario == "none" ? [] : scenario == "multiple" ? ["one", "two"] : ["one"];
        storage.Parts = scenario == "parts";
        await CallingAsync(db, upload, expiredDeadline: true);
        if (scenario == "legacy")
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartPhase=NULL, MultipartFence=NULL, MultipartDeadline=NULL, FailureCode='multipart_initiating:legacy' WHERE Id={upload}");
        if (scenario == "expired")
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET ExpiresAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={upload}");
        await Repo(db, storage).RecoverMultipartsAsync();
        (await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Conflict);
        var row = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload);
        row.Status.Should().Be(UploadSessionStatus.Failed); row.FailureCode.Should().Be("multipart_restart_required"); row.StorageUploadId.Should().BeNull();
        (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor && r.Operation == "UploadPartUrlsIssued")).Should().Be(0);
        storage.Uploads[key].Should().BeEmpty(); storage.Uploads[key + "-neighbor"].Should().Equal("foreign");
        // A timeout never proves the old worker stopped. Tombstones stay sweepable.
        storage.Uploads[key] = ["late"];
        await DueAsync(db, upload);
        await Repo(db, storage).RecoverMultipartsAsync();
        storage.Uploads[key].Should().BeEmpty();
        var restarted = await CreateAsync(db, storage, actor);
        restarted.Session!.Id.Should().NotBe(upload); restarted.Session.FileId.Should().NotBe(row.FileId);
        (await IssueAsync(db, storage, actor, restarted.Session.Id, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
        storage.Initiations.Should().Be(1);
    }

    [Fact]
    public async Task LateWorkerAfterFenceTransfer_CannotIssueStaleUrl_RecoveryContinues()
    {
        await using var db = fixture.CreateRetryingDbContext();
        var storage = new RecoveryStorage { PauseInitiation = true };
        var (actor, upload, key) = await SeedAsync(db, storage);
        var first = IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString());
        await storage.Initiated.Task.WaitAsync(TimeSpan.FromSeconds(20));
        await using var other = fixture.CreateRetryingDbContext();
        await DueAsync(other, upload);
        (await IssueAsync(other, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
        storage.Release.TrySetResult();
        (await first).Status.Should().Be(UploadPersistenceStatus.StorageUnavailable);
        storage.Presigns.Should().Be(1); storage.Initiations.Should().Be(1);
        (await other.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload)).StorageUploadId.Should().Be("owned-1");
        storage.Uploads[key].Should().Equal("owned-1");
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("deactivated")]
    [InlineData("cancel")]
    [InlineData("outage")]
    [InlineData("abort-ack")]
    public async Task InterruptedRecovery_RemainsDurableAndRetryableWithoutProtectedEffects(string fault)
    {
        await using var db = fixture.CreateRetryingDbContext();
        var storage = new RecoveryStorage();
        var (actor, upload, key) = await SeedAsync(db, storage);
        storage.Uploads[key] = fault == "abort-ack" ? ["one", "two"] : ["one"];
        await CallingAsync(db, upload);
        if (fault is "revoked" or "deactivated")
        {
            storage.Listed = async () =>
            {
                await using var revoke = fixture.CreateDbContext();
                if (fault == "revoked") await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status={(byte)UserStatus.Suspended} WHERE Id={actor}");
                else await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE Roles SET IsActive={false} WHERE Code={UserRoleCode.Reporter.ToDbCode()}");
            };
            (await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.NotFound);
        }
        else if (fault == "cancel")
        {
            storage.Listed = () => throw new OperationCanceledException("Synthetic caller cancellation after durable recovery claim");
            await Assert.ThrowsAsync<OperationCanceledException>(() => IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString()));
        }
        else
        {
            storage.Unavailable = fault == "outage"; storage.LoseAbortAck = fault == "abort-ack";
            (await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.StorageUnavailable);
        }
        storage.Presigns.Should().Be(0); storage.Initiations.Should().Be(0);
        (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor && r.Operation == "UploadPartUrlsIssued")).Should().Be(0);
        storage.Listed = null; storage.Unavailable = false; storage.LoseAbortAck = false;
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status={(byte)UserStatus.Active} WHERE Id={actor}");
        if (fault == "deactivated") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Roles SET IsActive={true} WHERE Code={UserRoleCode.Reporter.ToDbCode()}");
        await DueAsync(db, upload);
        await Repo(db, storage).RecoverMultipartsAsync();
        var row = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload);
        row.MultipartPhase.Should().Be(fault == "abort-ack" ? "TERMINAL" : "DURABLE");
        if (fault == "abort-ack") storage.Uploads[key].Should().BeEmpty();
        else (await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
    }

    [Fact]
    public async Task CancellationAfterClaimCommit_BeforeRemoteCall_HasBoundedTerminalRestart()
    {
        await using var db = fixture.CreateRetryingDbContext(new CancelClaimCommit()); var storage = new RecoveryStorage();
        var (actor, upload, _) = await SeedAsync(db, storage);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString()));
        storage.Initiations.Should().Be(0);
        await using var fresh = fixture.CreateRetryingDbContext();
        (await fresh.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload)).MultipartPhase.Should().Be("CLAIMED");
        await fresh.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartNextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)}, MultipartDeadline={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={upload}");
        await Repo(fresh, storage).RecoverMultipartsAsync();
        (await fresh.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload)).MultipartPhase.Should().Be("TERMINAL");
        var restarted = await CreateAsync(fresh, storage, actor);
        (await IssueAsync(fresh, storage, actor, restarted.Session!.Id, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
        storage.Initiations.Should().Be(1);
    }

    [Fact]
    public async Task ProjectRecovery_UsesCurrentMembershipBeforeAdoptionReplayAndConflict()
    {
        await using var db = fixture.CreateRetryingDbContext(); await fixture.SeedRolesAsync(db);
        var storage = new RecoveryStorage(); var actor = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(),
            DisplayName = "Synthetic project recovery", PasswordHash = "non-login", RoleCode = UserRoleCode.ProjectManager, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow });
        var project = new RoadGuardSystem.BusinessObjects.Projects.Project { Id = Guid.NewGuid(), ProjectCode = actor.ToString(), Name = "Recovery fixture", Status = ProjectStatus.Active, CreatedAt = DateTimeOffset.UtcNow };
        db.Projects.Add(project);
        var member = RoadGuardSystem.BusinessObjects.Projects.ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project.Id, actor, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        db.ProjectMembers.Add(member); await db.SaveChangesAsync();
        var create = await Repo(db, storage).CreateAsync(new(actor, project.Id, null, "DOCUMENT", "fixture.pdf", "application/pdf", 4,
            new string('a', 64), 8388608, DateTimeOffset.UtcNow.AddHours(24), Guid.NewGuid().ToString(), new string('b', 64), null));
        create.Status.Should().Be(UploadPersistenceStatus.Success); var upload = create.Session!.Id; var key = Guid.NewGuid().ToString();
        await CallingAsync(db, upload); storage.Uploads[create.Session.ObjectKey] = ["owned-1"];
        (await Repo(db, storage).GetPartUrlsAsync(actor, Guid.NewGuid(), upload, [1], key, new string('c', 64), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15))).Status.Should().Be(UploadPersistenceStatus.NotFound);
        (await Repo(db, storage).GetPartUrlsAsync(actor, project.Id, upload, [1], key, new string('c', 64), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15))).Status.Should().Be(UploadPersistenceStatus.Success);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProjectMembers SET Status={(byte)ProjectMemberStatus.Ended} WHERE Id={member.Id}");
        foreach (var fingerprint in new[] { new string('c', 64), new string('d', 64) })
            (await Repo(db, storage).GetPartUrlsAsync(actor, project.Id, upload, [1], key, fingerprint, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15))).Status.Should().Be(UploadPersistenceStatus.NotFound);
        storage.Initiations.Should().Be(0); storage.Presigns.Should().Be(1);
        (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor && r.Operation == "UploadPartUrlsIssued")).Should().Be(1);
    }

    [Fact]
    public async Task ExpiredAcceptedUpload_IsFencedAndAborted_WithoutResurrectingReceipts()
    {
        await using var db = fixture.CreateRetryingDbContext(); var storage = new RecoveryStorage();
        var (actor, upload, key) = await SeedAsync(db, storage); var receiptKey = Guid.NewGuid().ToString();
        (await IssueAsync(db, storage, actor, upload, receiptKey)).Status.Should().Be(UploadPersistenceStatus.Success);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET ExpiresAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={upload}");
        await Repo(db, storage).RecoverMultipartsAsync();
        var row = await db.UploadSessions.AsNoTracking().SingleAsync(s => s.Id == upload);
        row.Status.Should().Be(UploadSessionStatus.Failed); row.MultipartPhase.Should().Be("TERMINAL");
        storage.Uploads[key].Should().BeEmpty();
        (await IssueAsync(db, storage, actor, upload, receiptKey)).Status.Should().Be(UploadPersistenceStatus.Conflict);
        (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor && r.Operation == "UploadPartUrlsIssued")).Should().Be(1);
    }

    [Fact]
    public async Task CallerCancellationAfterRemoteCreate_FreshContextRecoversIndependently()
    {
        await using var db = fixture.CreateRetryingDbContext(); var storage = new RecoveryStorage { PauseInitiation = true };
        var (actor, upload, _) = await SeedAsync(db, storage); var key = Guid.NewGuid().ToString();
        using var cancellation = new CancellationTokenSource();
        var pending = IssueAsync(db, storage, actor, upload, key, cancellation.Token);
        await storage.Initiated.Task.WaitAsync(TimeSpan.FromSeconds(20)); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await using var fresh = fixture.CreateRetryingDbContext(); await DueAsync(fresh, upload);
        (await IssueAsync(fresh, storage, actor, upload, key)).Status.Should().Be(UploadPersistenceStatus.Success);
        storage.Initiations.Should().Be(1); storage.Presigns.Should().Be(1);
    }

    [Fact]
    public async Task RevokeDuringPresign_WithholdsCommittedProtectedReceipt()
    {
        await using var db = fixture.CreateRetryingDbContext(); var storage = new RecoveryStorage();
        var (actor, upload, _) = await SeedAsync(db, storage);
        storage.Presigned = async () => { await using var revoke = fixture.CreateDbContext(); await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status={(byte)UserStatus.Suspended} WHERE Id={actor}"); };
        var result = await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString());
        result.Status.Should().Be(UploadPersistenceStatus.NotFound); result.Parts.Should().BeEmpty();
        (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor && r.Operation == "UploadPartUrlsIssued")).Should().Be(1);
        storage.Initiations.Should().Be(1);
    }

    [Fact]
    public async Task AcknowledgedIdWithTransportDuplicate_SweepAbortsExtrasWithoutDriftingVerifiedVersion()
    {
        await using var db = fixture.CreateRetryingDbContext();
        var storage = new RecoveryStorage();
        var (actor, upload, key) = await SeedAsync(db, storage);
        (await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
        storage.Uploads[key] = ["owned-1", "transport-duplicate"];
        var before = (await Repo(db, storage).GetSessionAsync(upload))!.Version;
        await Repo(db, storage).RecoverMultipartsAsync();
        storage.Uploads[key].Should().Equal("owned-1");
        (await Repo(db, storage).GetSessionAsync(upload))!.Version.Should().Be(before);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET Status={(byte)UploadSessionStatus.Verified} WHERE Id={upload}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadMultipartSweeps SET NextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE UploadSessionId={upload}");
        before = (await Repo(db, storage).GetSessionAsync(upload))!.Version;
        storage.Uploads[key] = ["late-transport-duplicate"];
        await Repo(db, storage).RecoverMultipartsAsync();
        storage.Uploads[key].Should().BeEmpty();
        (await Repo(db, storage).GetSessionAsync(upload))!.Version.Should().Be(before);
        (await db.IdempotencyRecords.CountAsync(r => r.ActorUserId == actor && r.Operation == "UploadPartUrlsIssued")).Should().Be(1);
    }

    [Fact]
    public async Task VerifiedAndVerifyingSessions_AreNotResurrectedOrVersionDriftedByWorker()
    {
        await using var db = fixture.CreateRetryingDbContext();
        var storage = new RecoveryStorage();
        var (actor, upload, _) = await SeedAsync(db, storage);
        (await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Success);
        var view = (await Repo(db, storage).GetSessionAsync(upload))!;
        (await Repo(db, storage).CompleteAsync(new(actor, null, upload, view.Version, [new(1, "etag")], new string('a', 64), Guid.NewGuid().ToString(), new string('d', 64), null))).Status.Should().Be(UploadPersistenceStatus.Success);
        var before = (await Repo(db, storage).GetSessionAsync(upload))!;
        await Repo(db, storage).RecoverMultipartsAsync();
        (await Repo(db, storage).GetSessionAsync(upload))!.Version.Should().Be(before.Version);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET Status={(byte)UploadSessionStatus.Verified} WHERE Id={upload}");
        before = (await Repo(db, storage).GetSessionAsync(upload))!;
        await Repo(db, storage).RecoverMultipartsAsync();
        (await Repo(db, storage).GetSessionAsync(upload))!.Version.Should().Be(before.Version);
        (await IssueAsync(db, storage, actor, upload, Guid.NewGuid().ToString())).Status.Should().Be(UploadPersistenceStatus.Conflict);
        storage.Initiations.Should().Be(1);
    }

    private static UploadPersistenceService Repo(RoadGuardDbContext db, IUploadObjectStorage storage) => new(db, new IdempotencyOperationService(db), storage);
    private static Task<UploadPartUrlsPersistenceResult> IssueAsync(RoadGuardDbContext db, IUploadObjectStorage storage, Guid actor, Guid upload, string key, CancellationToken cancellationToken = default)
        => Repo(db, storage).GetPartUrlsAsync(actor, null, upload, [1], key, new string('c', 64), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15), cancellationToken);
    private static Task<int> DueAsync(RoadGuardDbContext db, Guid upload)
        => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET MultipartNextCheckAt={DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE Id={upload}");
    private static async Task CallingAsync(RoadGuardDbContext db, Guid upload, bool expiredDeadline = false)
    {
        var s = await db.UploadSessions.SingleAsync(s => s.Id == upload);
        s.BeginMultipart(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(expiredDeadline ? -1 : 5), DateTimeOffset.UtcNow.AddMinutes(-1));
        s.SetMultipartPhase(s.MultipartFence!.Value, "CALLING", DateTimeOffset.UtcNow.AddMinutes(-1)); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    }
    private async Task<(Guid Actor, Guid Upload, string Key)> SeedAsync(RoadGuardDbContext db, IUploadObjectStorage storage)
    {
        await fixture.SeedRolesAsync(db); var actor = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(), DisplayName = "Synthetic recovery fixture", PasswordHash = "non-login", RoleCode = UserRoleCode.Reporter, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow }); await db.SaveChangesAsync();
        var result = await CreateAsync(db, storage, actor); result.Status.Should().Be(UploadPersistenceStatus.Success);
        return (actor, result.Session!.Id, result.Session.ObjectKey);
    }
    private static Task<UploadMutationPersistenceResult> CreateAsync(RoadGuardDbContext db, IUploadObjectStorage storage, Guid actor)
        => Repo(db, storage).CreateAsync(new(actor, null, null, "REPORT_PHOTO", "fixture.jpg", "image/jpeg", 4, new string('a', 64), 8388608, DateTimeOffset.UtcNow.AddHours(24), Guid.NewGuid().ToString(), new string('b', 64), null));

    private sealed class RecoveryStorage : IUploadObjectStorage, IMultipartRecoveryStorage
    {
        public ConcurrentDictionary<string, string[]> Uploads { get; } = new(StringComparer.Ordinal);
        public bool PauseInitiation { get; init; }
        public bool Parts { get; set; }
        public bool Unavailable { get; set; }
        public bool LoseAbortAck { get; set; }
        public Func<Task>? Listed { get; set; }
        public Func<Task>? Presigned { get; set; }
        public int Initiations { get; private set; }
        public int Presigns { get; private set; }
        public TaskCompletionSource Initiated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
        { Initiations++; Uploads[objectKey] = ["owned-1"]; Initiated.TrySetResult(); if (PauseInitiation) await Release.Task.WaitAsync(cancellationToken); return "owned-1"; }
        public async Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default)
        { if (Unavailable) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Synthetic outage"); if (Listed is not null) await Listed(); return Uploads.GetValueOrDefault(exactObjectKey) ?? []; }
        public Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default) => Task.FromResult(Parts);
        public Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default)
        { Uploads.AddOrUpdate(objectKey, [], (_, ids) => ids.Where(x => x != uploadId).ToArray()); if (LoseAbortAck) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Synthetic lost abort ack"); return Task.CompletedTask; }
        public async Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(string objectKey, string uploadId, IReadOnlyList<int> partNumbers, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        { Presigns++; if (Presigned is not null) await Presigned(); return partNumbers.Select(n => new PresignedUploadPart(n, $"https://storage.invalid/{uploadId}/{n}", expiresAt)).ToArray(); }
        public Task<UploadObjectVerification> CompleteAndVerifyAsync(string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class CancelClaimCommit : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is RoadGuardDbContext db && db.UploadSessions.Local.Any(s => s.MultipartPhase == "CLAIMED"))
                throw new OperationCanceledException("Synthetic cancellation after durable claim commit, before remote create");
            return Task.CompletedTask;
        }
    }
}
