using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using RoadGuardSystem.Repositories.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.IntegrationTests.Repairs;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Reporting;

// Persistence characterization; the owned-MinIO cases cover specific durable boundaries,
// not HTTP admission, renderer acceptance or a deployed storage service.
public sealed class ExportLeaseRecoverySqlTests(IdentitySqlServerFixture sql) : IClassFixture<IdentitySqlServerFixture>
{
    private readonly Clock clock = new();
    private ExportRepository Repository(RoadGuardDbContext db) => new(db, new IdempotencyOperationService(db), clock,
        Options.Create(new ExportRepositoryOptions { LeaseDurationSeconds = 60, RetryBackoffSeconds = 10 }));

    private async Task<Guid> Seed()
    {
        await using var db = sql.CreateDbContext();
        var source = await H4GenuineRepairSource.Seed(db, sql);
        var snapshot = ExportSnapshot.Create(Guid.NewGuid(), source.Project, "{\"leaseFixtureOnly\":true}",
            ExportSerialization.Hash("{\"leaseFixtureOnly\":true}"), clock.GetUtcNow());
        var job = ExportJob.Create(Guid.NewGuid(), source.Project, source.Pm, snapshot.Id, "DOSSIER", "ZIP", clock.GetUtcNow());
        db.Add(snapshot); db.Add(job); await db.SaveChangesAsync(); return job.Id;
    }

    [Fact]
    public async Task ExpiredClaimCannotRenewFailOrCompleteAfterRecoveryAndAuthorizationIsRechecked()
    {
        var id = await Seed(); await using var firstDb = sql.CreateDbContext();
        var first = Assert.IsType<ExportClaim>(await Repository(firstDb).ClaimAsync(default));
        Assert.Equal(id, first.Id);
        await using var verify = sql.CreateDbContext();
        var frozen = await verify.Set<ExportSnapshot>().AsNoTracking().SingleAsync(x => x.Id == first.Snapshot.Id);
        var version = (await verify.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id)).RowVersion;
        clock.Advance(TimeSpan.FromSeconds(61));
        await using var recoveredDb = sql.CreateDbContext(); var repository = Repository(recoveredDb);
        var recovered = Assert.IsType<ExportClaim>(await repository.ClaimAsync(default));
        Assert.NotEqual(first.Token, recovered.Token);
        Assert.False(await Repository(firstDb).RenewAsync(id, first.Token, default));
        await Repository(firstDb).FailAsync(first, "stale-worker", true, default);
        var metadata = new Anh02ArtifactMetadata("synthetic/export.zip", 10, new string('a', 64), "application/zip");
        var staleAuthorityCalled = false;
        Assert.False(await Repository(firstDb).CompleteAsync(first, metadata, _ => { staleAuthorityCalled = true; return Task.FromResult(true); }, default));
        Assert.False(staleAuthorityCalled);
        Assert.False(await repository.CompleteAsync(recovered, metadata, _ => Task.FromResult(false), default));
        Assert.False(await verify.Set<GeneratedArtifact>().AnyAsync(x => x.ExportJobId == id));
        Assert.True(await repository.RenewAsync(id, recovered.Token, default));
        Assert.True(await repository.CompleteAsync(recovered, metadata, _ => Task.FromResult(true), default));
        Assert.False(await repository.CompleteAsync(recovered, metadata, _ => Task.FromResult(true), default));
        var final = await verify.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("SUCCEEDED", final.Status); Assert.NotEqual(version, final.RowVersion);
        Assert.Equal(1, await verify.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id));
        var snapshot = await verify.Set<ExportSnapshot>().AsNoTracking().SingleAsync(x => x.Id == frozen.Id);
        Assert.Equal(frozen.PayloadJson, snapshot.PayloadJson); Assert.Equal(frozen.Hash, snapshot.Hash);
        Assert.Null(await repository.ClaimAsync(default));
    }

    [Fact]
    public async Task RolledBackFailureLeavesLeaseIntactThenTransientFailureHonorsBackoff()
    {
        var id = await Seed(); await using var db = sql.CreateDbContext(); var repository = Repository(db);
        var claim = Assert.IsType<ExportClaim>(await repository.ClaimAsync(default));
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await repository.FailAsync(claim, "temporary", false, default);
            Assert.Equal("QUEUED", (await db.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id)).Status);
            await tx.RollbackAsync();
        }
        await using var verify = sql.CreateDbContext();
        var unchanged = await verify.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("RUNNING", unchanged.Status); Assert.Equal(claim.Token, unchanged.LeaseToken); Assert.Null(unchanged.ErrorCode);
        await repository.FailAsync(claim, "temporary", false, default);
        Assert.Null(await repository.ClaimAsync(default));
        clock.Advance(TimeSpan.FromSeconds(10));
        var next = Assert.IsType<ExportClaim>(await repository.ClaimAsync(default)); Assert.NotEqual(claim.Token, next.Token);
        await repository.FailAsync(next, "permanent", true, default);
        clock.Advance(TimeSpan.FromHours(1)); Assert.Null(await repository.ClaimAsync(default));
        var final = await verify.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("FAILED", final.Status); Assert.Equal("permanent", final.ErrorCode);
        Assert.False(await verify.Set<GeneratedArtifact>().AnyAsync(x => x.ExportJobId == id));
    }

    [Fact]
    public async Task ConcurrentClaimsYieldOneLeaseAndOnlyItsCompletionCreatesOneArtifact()
    {
        var id = await Seed();
        await using var left = sql.CreateDbContext();
        await using var right = sql.CreateDbContext();
        var claims = await Task.WhenAll(Repository(left).ClaimAsync(default), Repository(right).ClaimAsync(default));
        var winner = Assert.Single(claims.Where(x => x is not null))!;
        Assert.Equal(id, winner.Id);
        await using var verify = sql.CreateDbContext();
        var job = await verify.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id);
        Assert.Equal("RUNNING", job.Status); Assert.Equal(winner.Token, job.LeaseToken);
        Assert.False(await verify.Set<GeneratedArtifact>().AnyAsync(x => x.ExportJobId == id));
        await using var worker = sql.CreateDbContext();
        var repository = Repository(worker);
        var metadata = new Anh02ArtifactMetadata("synthetic/concurrent.zip", 10, new string('b', 64), "application/zip");
        Assert.True(await repository.CompleteAsync(winner, metadata, _ => Task.FromResult(true), default));
        Assert.False(await repository.CompleteAsync(winner, metadata, _ => Task.FromResult(true), default));
        Assert.Equal(1, await verify.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id));
        Assert.Equal("SUCCEEDED", (await verify.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id)).Status);
        Assert.Null(await repository.ClaimAsync(default));
    }

    [Fact]
    public async Task AbandonedWorkerContextRecoversAfterLeaseExpiryWithoutPublishingPartialArtifact()
    {
        var id = await Seed(); ExportClaim abandoned;
        await using (var process = sql.CreateDbContext())
        {
            abandoned = Assert.IsType<ExportClaim>(await Repository(process).ClaimAsync(default));
            Assert.Equal(id, abandoned.Id);
            // Simulate termination after the durable claim commit: no completion/failure callback.
        }
        await using (var restart = sql.CreateDbContext())
        {
            Assert.Null(await Repository(restart).ClaimAsync(default));
            Assert.False(await restart.Set<GeneratedArtifact>().AnyAsync(x => x.ExportJobId == id));
            var running = await restart.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("RUNNING", running.Status); Assert.Equal(abandoned.Token, running.LeaseToken);
        }
        clock.Advance(TimeSpan.FromSeconds(61));
        await using var recovered = sql.CreateDbContext(); var repository = Repository(recovered);
        var claim = Assert.IsType<ExportClaim>(await repository.ClaimAsync(default));
        Assert.Equal(abandoned.Snapshot.Hash, claim.Snapshot.Hash);
        Assert.Equal(abandoned.Snapshot.PayloadJson, claim.Snapshot.PayloadJson);
        Assert.NotEqual(abandoned.Token, claim.Token);
        Assert.False(await repository.CompleteAsync(abandoned,
            new("synthetic/abandoned.zip", 4, new string('c', 64), "application/zip"), _ => throw new InvalidOperationException("stale authorization must not run"), default));
        Assert.True(await repository.CompleteAsync(claim,
            new("synthetic/recovered.zip", 4, new string('d', 64), "application/zip"), _ => Task.FromResult(true), default));
        await using var post = sql.CreateDbContext();
        Assert.Equal("SUCCEEDED", (await post.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id)).Status);
        Assert.Equal(1, await post.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id));
        Assert.Null(await repository.ClaimAsync(default));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedMinioObjectSurvivesUnacknowledgedOrRolledBackCompletionAndConvergesOnce(bool sqlRolledBack)
    {
        // No configured external storage is accepted: both provider and SQL belong to disposable fixtures.
        var image = Environment.GetEnvironmentVariable("RG_MINIO_IMAGE")
            ?? throw new InvalidOperationException("An approved disposable MinIO image is required.");
        var owner = Guid.NewGuid().ToString("N");
        var access = "sbx" + owner[..12]; var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using var minio = new ContainerBuilder(image)
            .WithLabel("roadguard.sandbox.owner", owner).WithLabel("roadguard.sandbox.mode", "task17-test")
            .WithEnvironment("MINIO_ROOT_USER", access).WithEnvironment("MINIO_ROOT_PASSWORD", secret)
            .WithCommand("server", "/data").WithPortBinding(9000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                request => request.ForPort(9000).ForPath("/minio/health/ready"),
                strategy => strategy.WithTimeout(TimeSpan.FromSeconds(30)))).Build();
        await minio.StartAsync();
        var endpoint = $"http://{minio.Hostname}:{minio.GetMappedPublicPort(9000)}";
        var bucket = "sbx-" + owner;
        using var s3 = new AmazonS3Client(access, secret, new AmazonS3Config
        { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        var storage = new MinioAnh02ArtifactStore(Options.Create(new MinioStorageOptions
        { Endpoint = endpoint, BucketName = bucket, AccessKey = access, SecretKey = secret }));
        var id = await Seed(); ExportClaim first;
        await using (var db = sql.CreateDbContext()) first = Assert.IsType<ExportClaim>(await Repository(db).ClaimAsync(default));
        Assert.Equal(id, first.Id);
        var key = $"anh02/exports/{id:D}/{first.Snapshot.Hash}.zip";
        byte[] bytes = [1, 2, 3, 4];
        // A rejected size fails before PUT, leaving a recoverable RUNNING job and no object/artifact.
        await Assert.ThrowsAsync<IOException>(() => storage.WriteAsync(key, new MemoryStream(bytes), 5, "application/zip"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => storage.OpenReadAsync(key));
        var metadata = await storage.WriteAsync(key, new MemoryStream(bytes), bytes.Length, "application/zip");
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), metadata.Sha256);
        if (sqlRolledBack)
        {
            await using var db = sql.CreateDbContext(new FailFirstCommitInterceptor());
            // Repository owns its transaction: inject before its commit, after SQL SaveChanges.
            await Assert.ThrowsAsync<TestTransientException>(() =>
                Repository(db).CompleteAsync(first, metadata, _ => Task.FromResult(true), default));
        }
        // Other case models termination after PUT without ever calling SQL completion.
        await using (var post = sql.CreateDbContext())
        {
            var pending = await post.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Equal("RUNNING", pending.Status); Assert.Equal(first.Token, pending.LeaseToken);
            Assert.False(await post.Set<GeneratedArtifact>().AnyAsync(x => x.ExportJobId == id));
        }
        await using (var read = await storage.OpenReadAsync(key))
        { using var buffer = new MemoryStream(); await read.Content.CopyToAsync(buffer); Assert.Equal(bytes, buffer.ToArray()); }
        // Same logical key is immutable; repeated retry cannot overwrite a durable previous object.
        Assert.Equal(metadata, await storage.WriteAsync(key, new MemoryStream(bytes), 4, "application/zip"));
        await Assert.ThrowsAsync<IOException>(() => storage.WriteAsync(key, new MemoryStream([4, 3, 2, 1]), 4, "application/zip"));
        clock.Advance(TimeSpan.FromSeconds(61));
        await using var recovery = sql.CreateDbContext(); var repository = Repository(recovery);
        var current = Assert.IsType<ExportClaim>(await repository.ClaimAsync(default));
        Assert.Equal(first.Snapshot.Hash, current.Snapshot.Hash); Assert.NotEqual(first.Token, current.Token);
        Assert.False(await repository.CompleteAsync(first, metadata, _ => throw new InvalidOperationException("stale claim"), default));
        await using (var read = await storage.OpenReadAsync(key))
            Assert.Equal(metadata, read.Metadata);
        Assert.True(await repository.CompleteAsync(current, metadata, _ => Task.FromResult(true), default));
        Assert.False(await repository.CompleteAsync(current, metadata, _ => Task.FromResult(true), default));
        await using var final = sql.CreateDbContext();
        Assert.Equal("SUCCEEDED", (await final.Set<ExportJob>().AsNoTracking().SingleAsync(x => x.Id == id)).Status);
        Assert.Equal(1, await final.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == id));
        Assert.Single((await s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket })).S3Objects);
        Assert.Null(await repository.ClaimAsync(default));
        // Controlled durable boundaries, not an OS crash or complete renderer/API acceptance.
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan delta) => now += delta;
    }
}
