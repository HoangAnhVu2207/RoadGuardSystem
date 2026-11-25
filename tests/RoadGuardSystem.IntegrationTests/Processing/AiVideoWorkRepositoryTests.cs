using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using RoadGuardSystem.Repositories;
using NetTopologySuite.Geometries;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Catalogs;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories.Implementations.Processing;
using RoadGuardSystem.Repositories.Processing;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

// No configured/shared server is eligible: every database belongs to this fixture's container.
public sealed class AiVideoWorkRepositoryTests : IAsyncLifetime
{
    private readonly IdentitySqlServerFixture _fixture = new(new SqlServerTestFixture(_ => null, createSpatialProbeSchema: false));
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly string[] Labels = ["alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack"];
    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task ConfiguredSqlRetries_SupportEveryRealWorkerTransaction()
    {
        var seed = await SeedAsync();
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, x => { x.UseNetTopologySuite(); x.EnableRetryOnFailure(3); }).Options;
        await using var db = new RoadGuardDbContext(options);
        var repo = new AiVideoWorkRepository(db);
        await repo.PrepareAsync(seed.Sources[0], Prepared(seed.Sources[0], seed.ModelId), seed.Map, Ct);
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var work = (await repo.ClaimAsync("retry-worker", now, TimeSpan.FromMinutes(1), Ct))!;
        await repo.ChangeAsync(work.Id, "retry-worker", work.CurrentAttemptId, AiVideoWorkState.Admitted, null, now, now, Ct);
        work = (await repo.ClaimAsync("retry-worker", now, TimeSpan.FromMinutes(1), Ct))!;
        await repo.ChangeAsync(work.Id, "retry-worker", work.CurrentAttemptId, AiVideoWorkState.ArtifactsReady, null, now, now, Ct);
        work = (await repo.ClaimAsync("retry-worker", now, TimeSpan.FromMinutes(1), Ct))!;
        var raw = new AiRegisteredArtifact(Guid.NewGuid(), new string('b', 64), 40, "application/json", "RAW_RESULT", "fixture/result.json", null, null);
        await repo.CompleteAsync(work.Id, "retry-worker", work.CurrentAttemptId, [raw], [], seed.Map, now, Ct);
        (await db.AiVideoWorkItems.AsNoTracking().SingleAsync()).State.Should().Be(AiVideoWorkState.Ingested);
        (await db.AiVideoAttemptOutcomes.CountAsync()).Should().Be(1);
    }

    [Theory]
    [InlineData("prepare", false)]
    [InlineData("claim", false)]
    [InlineData("change", false)]
    [InlineData("complete", false)]
    [InlineData("prepare", true)]
    [InlineData("claim", true)]
    [InlineData("change", true)]
    [InlineData("complete", true)]
    public async Task LostCommitAcknowledgement_RecoversOneDurableOperation(string operation, bool beforeCommit)
    {
        var seed = await SeedAsync();
        AiVideoWorkItem? ready = operation == "complete" ? await ReadyAsync(seed) : null;
        await using (var setup = _fixture.CreateDbContext())
        {
            if (operation is "claim" or "change")
                await new AiVideoWorkRepository(setup).PrepareAsync(seed.Sources[0], Prepared(seed.Sources[0], seed.ModelId), seed.Map, Ct);
            if (operation == "change")
                ready = await new AiVideoWorkRepository(setup).ClaimAsync("worker", DateTimeOffset.UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), Ct);
        }
        var loss = new WorkCommitAckLoss(beforeCommit);
        await using var db = _fixture.CreateRetryingDbContext(loss);
        var repo = new AiVideoWorkRepository(db);
        if (operation == "prepare") await repo.PrepareAsync(seed.Sources[0], Prepared(seed.Sources[0], seed.ModelId), seed.Map, Ct);
        if (operation == "claim") (await repo.ClaimAsync("worker", DateTimeOffset.UtcNow.AddSeconds(1), TimeSpan.FromMinutes(1), Ct)).Should().NotBeNull();
        if (operation == "change") await repo.ChangeAsync(ready!.Id, "worker", ready.CurrentAttemptId, AiVideoWorkState.Admitted, null, ready.UpdatedAt, ready.UpdatedAt, Ct);
        if (operation == "complete")
        {
            var raw = new AiRegisteredArtifact(Guid.NewGuid(), new string('b', 64), 40, "application/json", "RAW_RESULT", "fixture/result.json", null, null);
            var frame = new AiRegisteredArtifact(Guid.NewGuid(), new string('c', 64), 20, "image/jpeg", "FRAME", "fixture/frame.jpg", 100, 100);
            var proposal = new AiProposedDetection(Guid.NewGuid(), frame.ArtifactId, Labels[0], seed.Map[Labels[0]], .91234567m, "{}");
            await repo.CompleteAsync(ready!.Id, "worker", ready.CurrentAttemptId, [raw, frame], [proposal], seed.Map, ready.UpdatedAt.AddSeconds(1), Ct);
        }
        loss.Fired.Should().BeTrue();
        await using var verify = _fixture.CreateDbContext();
        (await verify.AiVideoWorkItems.CountAsync()).Should().Be(1);
        (await verify.ProcessingJobs.CountAsync()).Should().Be(1);
        (await verify.ProcessingAttempts.CountAsync()).Should().Be(1);
        (await verify.AiWorkerArtifacts.CountAsync()).Should().Be(operation == "complete" ? 2 : 0);
        (await verify.AIDetections.CountAsync()).Should().Be(operation == "complete" ? 1 : 0);
        (await verify.AiVideoAttemptOutcomes.CountAsync()).Should().Be(operation == "complete" ? 1 : 0);
    }

    private sealed class WorkCommitAckLoss(bool beforeCommit) : DbTransactionInterceptor
    {
        public bool Fired { get; private set; }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (beforeCommit && !Fired) { Fired = true; throw new TestTransientException("Injected REAL work failure before commit."); }
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!beforeCommit && !Fired) { Fired = true; throw new TestTransientException("Injected REAL work commit acknowledgement loss."); }
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task WorkerTransaction_DoesNotSaveUnrelatedCallerChanges()
    {
        var seed = await SeedAsync();
        await using var db = _fixture.CreateDbContext();
        var unrelated = Intent(Guid.NewGuid());
        db.OutboxMessages.Add(unrelated);
        await new AiVideoWorkRepository(db).PrepareAsync(seed.Sources[0], Prepared(seed.Sources[0], seed.ModelId), seed.Map, Ct);
        db.Entry(unrelated).State.Should().Be(EntityState.Added);
        await using var verify = _fixture.CreateDbContext();
        (await verify.OutboxMessages.AnyAsync(x => x.Id == unrelated.Id)).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ReceiptReadFailure_RetriesWholeRolledBackOperation(int successfulReads)
    {
        var seed = await SeedAsync();
        var fault = new ReceiptReadFault(successfulReads);
        await using var db = _fixture.CreateRetryingDbContext(new ArmReceiptReads(fault), fault);
        var id = await new AiVideoWorkRepository(db).PrepareAsync(seed.Sources[0],
            Prepared(seed.Sources[0], seed.ModelId), seed.Map, Ct);
        fault.Fired.Should().BeTrue();
        await using var verify = _fixture.CreateDbContext();
        (await verify.AiVideoWorkItems.Select(x => x.Id).ToListAsync()).Should().Equal(id);
        (await verify.ProcessingJobs.CountAsync()).Should().Be(1);
        (await verify.ProcessingAttempts.CountAsync()).Should().Be(1);
    }

    private sealed class ArmReceiptReads(ReceiptReadFault fault) : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            fault.Armed = true;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ReceiptReadFault(int successfulReads) : DbCommandInterceptor
    {
        public bool Armed { get; set; }
        public bool Fired { get; private set; }
        private int _reads;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Armed && !Fired && _reads++ == successfulReads)
            {
                Fired = true;
                throw new TestTransientException("Injected persisted receipt read failure.");
            }
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task Discovery_RequiresSubmissionIntentAndPreparesEveryVideoExactlyOnce()
    {
        var seed = await SeedAsync(5, intent: false);
        await using var db = _fixture.CreateDbContext();
        var repo = new AiVideoWorkRepository(db);
        (await repo.NextSourceAsync("config", Ct)).Should().BeNull();
        db.OutboxMessages.Add(Intent(seed.Sources[0].DatasetId));
        await db.SaveChangesAsync();
        var seen = new HashSet<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var source = await repo.NextSourceAsync("config", Ct);
            source.Should().NotBeNull();
            seen.Add(source!.FileId).Should().BeTrue();
            var prepared = Prepared(source, seed.ModelId);
            var id = await repo.PrepareAsync(source, prepared, seed.Map, Ct);
            (await repo.PrepareAsync(source, prepared, seed.Map, Ct)).Should().Be(id);
        }
        (await repo.NextSourceAsync("config", Ct)).Should().BeNull();
        (await db.AiVideoWorkItems.CountAsync()).Should().Be(5);
        (await db.ProcessingJobs.CountAsync()).Should().Be(5);
        (await db.ProcessingAttempts.CountAsync()).Should().Be(5);
    }

    [Theory]
    [InlineData("project")]
    [InlineData("file")]
    [InlineData("checksum")]
    [InlineData("unverified")]
    public async Task SourceScope_RejectsAlteredOrUnverifiedInputs(string mutation)
    {
        var seed = await SeedAsync();
        var source = seed.Sources[0];
        await using var db = _fixture.CreateDbContext();
        if (mutation == "project") source = source with { ProjectId = Guid.NewGuid() };
        if (mutation == "file") source = source with { FileId = Guid.NewGuid() };
        if (mutation == "checksum") source = source with { Sha256 = new string('b', 64) };
        if (mutation == "unverified")
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET Status = {(byte)UploadSessionStatus.Uploading} WHERE FileId = {source.FileId}");
        var repo = new AiVideoWorkRepository(db);
        await FluentActions.Awaiting(() => repo.PrepareAsync(source, Prepared(source, seed.ModelId), seed.Map, Ct))
            .Should().ThrowAsync<InvalidOperationException>();
        (await db.AiVideoWorkItems.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("manifest")]
    [InlineData("draft")]
    [InlineData("missing-class")]
    [InlineData("collapsed")]
    [InlineData("inactive")]
    public async Task Preparation_RejectsIdentityAndCatalogMismatchesAtomically(string mutation)
    {
        var seed = await SeedAsync();
        var source = seed.Sources[0];
        var p = Prepared(source, seed.ModelId);
        var map = new Dictionary<string, string>(seed.Map);
        await using var db = _fixture.CreateDbContext();
        if (mutation == "identity") p = p with { JobId = Guid.NewGuid() };
        if (mutation == "manifest") p = p with { ManifestJson = "{\"different\":true}" };
        if (mutation == "draft") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE AIModelVersions SET Status = {(byte)AIModelVersionStatus.Draft} WHERE Id = {seed.ModelId}");
        if (mutation == "missing-class") map.Remove(Labels[0]);
        if (mutation == "collapsed") map[Labels[0]] = map[Labels[1]];
        if (mutation == "inactive") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DefectTypes SET IsActive = 0 WHERE Code = {map[Labels[0]]}");
        var repo = new AiVideoWorkRepository(db);
        await FluentActions.Awaiting(() => repo.PrepareAsync(source, p, map, Ct)).Should().ThrowAsync<InvalidOperationException>();
        (await db.ProcessingBlocks.CountAsync()).Should().Be(0);
        (await db.ProcessingJobs.CountAsync()).Should().Be(0);
        (await db.AiVideoWorkItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConcurrentPreparation_DeduplicatesWithoutLosingCaller()
    {
        var seed = await SeedAsync();
        var source = seed.Sources[0];
        async Task<Guid> Prepare()
        {
            await using var db = _fixture.CreateDbContext();
            return await new AiVideoWorkRepository(db).PrepareAsync(source, Prepared(source, seed.ModelId), seed.Map, Ct);
        }
        var ids = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Prepare()));
        ids.Distinct().Should().ContainSingle();
        await using var verify = _fixture.CreateDbContext();
        (await verify.AiVideoWorkItems.CountAsync()).Should().Be(1);
        (await verify.ProcessingJobs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentClaims_LeaseOneOwnerAndFenceExpiredWriter()
    {
        var seed = await SeedAsync();
        await using (var db = _fixture.CreateDbContext())
            await new AiVideoWorkRepository(db).PrepareAsync(seed.Sources[0], Prepared(seed.Sources[0], seed.ModelId), seed.Map, Ct);
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        async Task<AiVideoWorkItem?> Claim(string owner, DateTimeOffset at)
        {
            await using var db = _fixture.CreateDbContext();
            return await new AiVideoWorkRepository(db).ClaimAsync(owner, at, TimeSpan.FromMinutes(1), Ct);
        }
        var claims = await Task.WhenAll(Claim("one", now), Claim("two", now));
        var winner = claims.Where(x => x is not null).Single()!;
        var reclaimed = await Claim("replacement", now.AddMinutes(2));
        reclaimed!.Id.Should().Be(winner.Id);
        await using var stale = _fixture.CreateDbContext();
        await FluentActions.Awaiting(() => new AiVideoWorkRepository(stale).ChangeAsync(winner.Id, winner.LeaseOwner!, winner.CurrentAttemptId,
            AiVideoWorkState.Admitted, null, now.AddMinutes(2), now.AddMinutes(2), Ct)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Quarantine_SkipsOnlyRejectedVideoAndKeepsRemainingVideosEligible()
    {
        var seed = await SeedAsync(3);
        await using var db = _fixture.CreateDbContext();
        var repo = new AiVideoWorkRepository(db);
        var bad = await repo.NextSourceAsync("config", Ct);
        await repo.RecordSourceFailureAsync(bad!, "config", "SOURCE_CHECKSUM_MISMATCH", Ct);
        var second = await repo.NextSourceAsync("config", Ct);
        second!.FileId.Should().NotBe(bad!.FileId);
        await repo.PrepareAsync(second, Prepared(second, seed.ModelId), seed.Map, Ct);
        var third = await repo.NextSourceAsync("config", Ct);
        third!.FileId.Should().NotBe(bad.FileId).And.NotBe(second.FileId);
        await repo.PrepareAsync(third, Prepared(third, seed.ModelId), seed.Map, Ct);
        (await repo.NextSourceAsync("config", Ct)).Should().BeNull();
        (await repo.NextSourceAsync("different-config", Ct))!.FileId.Should().Be(bad.FileId);
    }

    [Fact]
    public async Task Completion_RegistersPrivateIdentitiesAndPreservesSixProposalLabels()
    {
        var seed = await SeedAsync();
        var work = await ReadyAsync(seed);
        var raw = new AiRegisteredArtifact(Guid.NewGuid(), new string('b', 64), 40, "application/json", "RAW_RESULT", "fixture/result.json", null, null);
        var frame = new AiRegisteredArtifact(Guid.NewGuid(), new string('c', 64), 20, "image/jpeg", "FRAME", "fixture/frame.jpg", 100, 100);
        var detections = Labels.Select(label => new AiProposedDetection(Guid.NewGuid(), frame.ArtifactId, label, seed.Map[label], .9m, "{\"bbox\":[0.1,0.1,0.2,0.2]}")).ToArray();
        await using var db = _fixture.CreateDbContext();
        await new AiVideoWorkRepository(db).CompleteAsync(work.Id, "worker", work.CurrentAttemptId, [raw, frame], detections, seed.Map, work.UpdatedAt.AddSeconds(1), Ct);
        db.ChangeTracker.Clear();
        var proofs = await db.AiWorkerArtifacts.ToListAsync();
        proofs.Should().HaveCount(2);
        foreach (var proof in proofs)
        {
            proof.FileId.Should().NotBe(Guid.Parse(proof.ArtifactId));
            (await db.Files.AnyAsync(x => x.Id == proof.FileId)).Should().BeTrue();
        }
        var rows = await db.AIDetections.ToListAsync();
        rows.Should().HaveCount(6);
        rows.Select(x => x.DefectTypeCode).Should().BeEquivalentTo(seed.Map.Values);
        foreach (var row in rows)
        {
            using var payload = JsonDocument.Parse(row.RawPayload);
            payload.RootElement.GetProperty("sourceClass").GetString().Should().BeOneOf(Labels);
            payload.RootElement.GetProperty("frameFileId").GetGuid().Should().Be(proofs.Single(x => x.Kind == "FRAME").FileId!.Value);
            payload.RootElement.GetProperty("groundLocationStatus").GetString().Should().Be("NOT_MEASURED");
        }
        (await db.AiVideoWorkItems.SingleAsync()).State.Should().Be(AiVideoWorkState.Ingested);
        (await db.Defects.CountAsync()).Should().Be(0);
        // Metadata-only seam performs no object-store operations.
        var files = new RoadGuardSystem.Repositories.Implementations.Files.UploadPersistenceService(db,
            new RoadGuardSystem.Repositories.Idempotency.IdempotencyOperationService(db), null!);
        var frameId = proofs.Single(x => x.Kind == "FRAME").FileId!.Value;
        var metadata = await files.GetFileMetadataAsync(frameId, Ct);
        metadata.Should().NotBeNull();
        metadata!.ProjectId.Should().Be(seed.Sources[0].ProjectId);
        metadata.Purpose.Should().Be("AI_REAL_FRAME");
        (await files.GetFileMetadataAsync(proofs.Single(x => x.Kind == "RAW_RESULT").FileId!.Value, Ct)).Should().BeNull();
        var mismatched = StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}", "mismatch.jpg", "image/jpeg", frame.Bytes, new string('d', 64), null, work.UpdatedAt, null);
        var mismatchProof = AiWorkerArtifact.Create(Guid.NewGuid(), work.Id, work.CurrentAttemptId, Guid.NewGuid().ToString("D"), frame.Sha256, frame.Bytes, "FRAME", 100, 100);
        mismatchProof.MarkRegistered(mismatched.Id);
        db.AddRange(mismatched, mismatchProof);
        await db.SaveChangesAsync();
        (await files.GetFileMetadataAsync(mismatched.Id, Ct)).Should().BeNull();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProcessingJobs SET ProjectId = {Guid.NewGuid()} WHERE Id = {work.ProcessingJobId}");
        (await files.GetFileMetadataAsync(frameId, Ct)).Should().BeNull();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProcessingJobs SET ProjectId = {work.ProjectId} WHERE Id = {work.ProcessingJobId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE AiVideoWorkItems SET State = {(byte)AiVideoWorkState.ArtifactsReady} WHERE Id = {work.Id}");
        (await files.GetFileMetadataAsync(frameId, Ct)).Should().BeNull();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE AiVideoWorkItems SET State = {(byte)AiVideoWorkState.Ingested} WHERE Id = {work.Id}");
        db.ProcessingAttempts.Add(ProcessingAttempt.Create(Guid.NewGuid(), work.ProcessingJobId, 2, work.UpdatedAt.AddMinutes(1), null, null, null));
        await db.SaveChangesAsync();
        (await files.GetFileMetadataAsync(frameId, Ct)).Should().BeNull();
    }

    [Theory]
    [InlineData("attempt")]
    [InlineData("owner")]
    [InlineData("scope")]
    [InlineData("source-version")]
    public async Task Completion_RejectsStaleAttemptOwnerOrScopeWithoutArtifacts(string mutation)
    {
        var seed = await SeedAsync();
        var work = await ReadyAsync(seed);
        await using var db = _fixture.CreateDbContext();
        if (mutation == "scope") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ProcessingJobs SET ProjectId = {Guid.NewGuid()} WHERE Id = {work.ProcessingJobId}");
        if (mutation == "source-version") await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE UploadSessions SET FailureCode = {"fixture-version-change"} WHERE FileId = {work.SourceVideoFileId}");
        var raw = new AiRegisteredArtifact(Guid.NewGuid(), new string('b', 64), 40, "application/json", "RAW_RESULT", "fixture/result.json", null, null);
        await FluentActions.Awaiting(() => new AiVideoWorkRepository(db).CompleteAsync(work.Id, mutation == "owner" ? "wrong" : "worker",
            mutation == "attempt" ? Guid.NewGuid() : work.CurrentAttemptId, [raw], [], seed.Map, work.UpdatedAt.AddSeconds(1), Ct)).Should().ThrowAsync<InvalidOperationException>();
        db.ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified).Should().BeEmpty("stale completion must not poison a reusable context");
        db.ChangeTracker.Clear();
        (await db.AiWorkerArtifacts.CountAsync()).Should().Be(0);
        (await db.AiVideoWorkItems.SingleAsync()).State.Should().Be(AiVideoWorkState.ArtifactsReady);

    }

    [Fact]
    public async Task DiscoveryMalformedEntryDoesNotBlockOtherVerifiedVideos()
    {
        var seed = await SeedAsync(2, malformed: true);
        await using var db = _fixture.CreateDbContext();
        var valid = seed.Sources[1];
        (await new AiVideoWorkRepository(db).NextSourceAsync("config", Ct))!.FileId.Should().Be(valid.FileId);
    }
    [Fact]
    public async Task FailureProjectsDurableLedgerToPublicJobState()
    {
        var seed = await SeedAsync();
        var work = await ReadyAsync(seed);
        await using var db = _fixture.CreateDbContext();
        var now = work.UpdatedAt.AddSeconds(1);
        await new AiVideoWorkRepository(db).ChangeAsync(work.Id, "worker", work.CurrentAttemptId,
            AiVideoWorkState.TerminalFailed, "MODEL_UNAVAILABLE", now, now, Ct);
        db.ChangeTracker.Clear();
        var job = await db.ProcessingJobs.SingleAsync(x => x.Id == work.ProcessingJobId);
        job.Status.Should().Be(ProcessingJobStatus.DataFailure);
        job.ErrorCode.Should().Be("MODEL_UNAVAILABLE");
        (await db.AiVideoWorkItems.SingleAsync()).State.Should().Be(AiVideoWorkState.TerminalFailed);
    }
    [Fact]
    public async Task ChangeRejectsNewerAttemptBeforeMutatingTrackedLedger()
    {
        var seed = await SeedAsync();
        var work = await ReadyAsync(seed);
        await using var db = _fixture.CreateDbContext();
        db.ProcessingAttempts.Add(ProcessingAttempt.Create(Guid.NewGuid(), work.ProcessingJobId, 2, work.UpdatedAt, null, null, null));
        await db.SaveChangesAsync();
        var now = work.UpdatedAt.AddSeconds(1);
        await FluentActions.Awaiting(() => new AiVideoWorkRepository(db).ChangeAsync(work.Id, "worker", work.CurrentAttemptId,
            AiVideoWorkState.TerminalFailed, "MODEL_UNAVAILABLE", now, now, Ct)).Should().ThrowAsync<InvalidOperationException>();
        db.ChangeTracker.Entries().Where(x => x.State is EntityState.Added or EntityState.Modified).Should().BeEmpty();
        (await db.AiVideoWorkItems.AsNoTracking().SingleAsync()).State.Should().Be(AiVideoWorkState.ArtifactsReady);
    }

    [Fact]
    public async Task RealCandidate_ReadsAppendOnlyAttemptAndLocksWithoutMockProvenance()
    {
        var seed = await SeedAsync(); var work = await ReadyAsync(seed);
        var raw = new AiRegisteredArtifact(Guid.NewGuid(), new string('b', 64), 40, "application/json", "RAW_RESULT", "fixture/result.json", null, null);
        var frame = new AiRegisteredArtifact(Guid.NewGuid(), new string('c', 64), 20, "image/jpeg", "FRAME", "fixture/frame.jpg", 100, 100);
        var detection = new AiProposedDetection(Guid.NewGuid(), frame.ArtifactId, Labels[0], seed.Map[Labels[0]], .9m, "{\"bbox\":[0.1,0.1,0.2,0.2]}");
        await using var db = _fixture.CreateDbContext();
        var workRepo = new AiVideoWorkRepository(db);
        await workRepo.CompleteAsync(work.Id, "worker", work.CurrentAttemptId, [raw, frame], [detection], seed.Map, work.UpdatedAt.AddSeconds(1), Ct);
        db.ChangeTracker.Clear();
        var attempt = await db.ProcessingAttempts.AsNoTracking().SingleAsync(x => x.Id == work.CurrentAttemptId);
        attempt.EndedAt.Should().BeNull(); attempt.ErrorType.Should().BeNull();
        (await new RealAiCandidateRepository(db, workRepo).ReadAsync(work.ProjectId, detection.Id, Ct)).Should().NotBeNull();
        (await db.Set<AiDetectionProvenance>().CountAsync()).Should().Be(0);
        var version = $"r1:{new string('a', 64)}:{Guid.Empty:N}:{Guid.Empty:N}:{Guid.Empty:N}:{Guid.Empty:N}";
        var repository = new RoadGuardSystem.Repositories.Implementations.Defects.CandidateDecisionRepository(db);
        Guid decisionId;
        await using (var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, Ct))
        {
            await repository.LockAiSourceAsync(detection.Id, Ct);
            var facts = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(CandidateSourceKind.AiDetection, detection.Id, version), work.ProjectId, "fixture-geometry");
            var saved = await repository.SaveRejectAsync(seed.Sources[0].OwnerId, facts, null, "Repository snapshot test", null, Ct);
            decisionId = saved.Id;
            await tx.CommitAsync(Ct);
        }
        var revision = AiVideoTelemetryRevision.Create(Guid.NewGuid(), work.Id, 1, null, seed.Sources[0].OwnerId, DateTimeOffset.UtcNow,
            seed.Sources[0].Sha256, "OPERATOR_ESTIMATE", "[{\"startMs\":0,\"endMs\":1000,\"latitude\":10,\"longitude\":106}]", 1000,
            null, null, null, 0, "Later estimate", new string('d', 64), new string('e', 64));
        db.AiVideoTelemetryRevisions.Add(revision); await db.SaveChangesAsync(Ct); db.ChangeTracker.Clear();
        (await new RealAiCandidateRepository(db, workRepo).ReadAsync(work.ProjectId, detection.Id, Ct))!.TelemetryRevisions!.Single().Id.Should().Be(revision.Id);
        (await db.SourceDecisions.AsNoTracking().SingleAsync(x => x.Id == decisionId)).Source.SourceVersion.Should().Be(version);
    }

    private async Task<AiVideoWorkItem> ReadyAsync(Seed seed)
    {
        await using var db = _fixture.CreateDbContext();
        var repo = new AiVideoWorkRepository(db);
        var source = (await repo.NextSourceAsync("config", Ct))!;
        await repo.PrepareAsync(source, Prepared(source, seed.ModelId), seed.Map, Ct);
        var now = DateTimeOffset.UtcNow.AddSeconds(1);
        var work = (await repo.ClaimAsync("worker", now, TimeSpan.FromMinutes(1), Ct))!;
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.Admitted, now);
        work.Transition("worker", work.CurrentAttemptId, AiVideoWorkState.ArtifactsReady, now);
        await db.SaveChangesAsync();
        return work;
    }

    private static OutboxMessage Intent(Guid id) => OutboxMessage.Create(Guid.NewGuid(), "ai.real.dataset_submitted.v1", DateTimeOffset.UtcNow, id, "{}");
    private static AiPreparedVideo Prepared(AiVideoSource source, Guid model)
    {
        var manifest = source.SourceVideoVersion is null ? "{\"source\":\"verified-video\"}" : JsonSerializer.Serialize(new { source = "verified-video", sourceVideoVersion = source.SourceVideoVersion });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        var job = Guid.NewGuid(); var attempt = Guid.NewGuid();
        var admission = JsonSerializer.Serialize(new
        {
            projectId = source.ProjectId,
            datasetId = source.DatasetId,
            sourceVideoFileId = source.FileId,
            jobId = job,
            attemptId = attempt,
            modelVersionId = model,
            inputSha256 = source.Sha256,
            inputBytes = source.Bytes,
            manifestHash = hash
        });
        return new(job, attempt, model, "config", admission, manifest, hash);
    }

    private sealed record Seed(AiVideoSource[] Sources, Guid ModelId, Dictionary<string, string> Map);
    private async Task<Seed> SeedAsync(int count = 1, bool intent = true, bool malformed = false)
    {
        await using var db = _fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        await _fixture.SeedRolesAsync(db);
        var owner = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = owner, UserName = $"worker-{owner:N}", NormalizedUserName = $"worker-{owner:N}".ToUpperInvariant(), DisplayName = "Worker fixture", PasswordHash = "fixture-password-hash", RoleCode = UserRoleCode.DroneOperator, Status = UserStatus.Active, CreatedAt = now });
        var project = new Project { Id = Guid.NewGuid(), ProjectCode = $"WORKER-{Guid.NewGuid():N}", Name = "Isolated worker repository", EngineeringUtmSrid = 32648, Status = ProjectStatus.Active, CreatedAt = now };
        var road = RoadSection.Create(Guid.NewGuid(), project.Id, $"ROAD-{Guid.NewGuid():N}");
        var geometry = new GeometryFactory(new PrecisionModel(), 32648).CreateLineString([new Coordinate(500000, 1200000), new Coordinate(500100, 1200000)]);
        var version = RoadSectionVersion.Create(Guid.NewGuid(), road.Id, 1, true, geometry, now, "fixture");
        var survey = Survey.Create(Guid.NewGuid(), null, project.Id, version.Id, SurveyType.Periodic, SurveyStatus.InProgress, false, null, null);
        var files = Enumerable.Range(0, count).Select(_ => StoredFile.Create(Guid.NewGuid(), $"fixture/{Guid.NewGuid():N}", "video.mp4", "video/mp4", 10, new string('a', 64), owner, now, null)).ToArray();
        var manifest = JsonSerializer.Serialize(files.Select(f => new { fileId = f.Id, checksumSha256 = f.Checksum, sizeBytes = f.SizeBytes, purpose = "SURVEY_VIDEO" }));
        if (malformed) manifest = JsonSerializer.Serialize(new object[] { new { purpose = "SURVEY_VIDEO", fileId = "invalid" }, new { purpose = "SURVEY_VIDEO", fileId = files[1].Id.ToString("D"), checksumSha256 = files[1].Checksum, sizeBytes = "invalid" }, new { purpose = "SURVEY_VIDEO", fileId = files[1].Id.ToString("D"), checksumSha256 = files[1].Checksum, sizeBytes = files[1].SizeBytes } });
        var dataset = SurveyDataVersion.Create(Guid.NewGuid(), survey.Id, 1, SurveyDataVersionStatus.ServerConfirmed, SurveyDataIntegrityStatus.Passed, now, SurveyDataConfirmationActor.Backend, manifest);
        var model = AIModelVersion.Create(Guid.NewGuid(), "FT95", Guid.NewGuid().ToString("N"), "fixture/model", null, null, AIModelVersionStatus.Released, null, null);
        var map = Labels.Select((label, i) => (label, code: $"FT95_{i}")).ToDictionary(x => x.label, x => x.code);
        db.AddRange(project, road, version, survey, dataset, model);
        foreach (var code in map.Values) db.DefectTypes.Add(DefectType.Create(code, code));
        foreach (var file in files)
        {
            var upload = UploadSession.Create(Guid.NewGuid(), file.Id, owner, file.StorageUri, "SURVEY_VIDEO", "video/mp4", 10, file.Checksum, 10, now.AddHours(1));
            db.AddRange(file, FileScope.Create(Guid.NewGuid(), file.Id, project.Id, null, owner, "SURVEY_VIDEO", now), upload);
            await db.SaveChangesAsync();
            upload.StartUploading("fixture", now); await db.SaveChangesAsync();
            upload.StartVerification(Convert.ToBase64String(upload.RowVersion), now); await db.SaveChangesAsync();
            upload.MarkVerified(); await db.SaveChangesAsync();
        }
        if (intent) { db.OutboxMessages.Add(Intent(dataset.Id)); await db.SaveChangesAsync(); }
        return new(files.Select(f => new AiVideoSource(dataset.Id, project.Id, f.Id, owner, f.StorageUri, f.Checksum, f.SizeBytes)).ToArray(), model.Id, map);
    }
}
