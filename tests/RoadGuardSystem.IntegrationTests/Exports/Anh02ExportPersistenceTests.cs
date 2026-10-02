using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Exports;

public sealed class Anh02ExportPersistenceTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture _fixture = new(createSpatialProbeSchema: false);
    public async Task InitializeAsync() { await _fixture.InitializeAsync(); await using var db = Db(); await db.Database.MigrateAsync(); }
    public Task DisposeAsync() => _fixture.DisposeAsync();
    private RoadGuardDbContext Db() => new(new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(_fixture.ConnectionString, o => o.UseNetTopologySuite()).Options);
    private static ExportRepository Repo(RoadGuardDbContext db) => new(db, new IdempotencyOperationService(db), TimeProvider.System);
    private async Task<(Guid Actor, Guid Project)> SeedAsync()
    {
        await using var db = Db();
        await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code='SUPERVISOR') INSERT INTO Roles (Code,Name,NormalizedName,IsActive) VALUES ('SUPERVISOR','Supervisor','SUPERVISOR',1)");
        var actor = Guid.NewGuid(); var project = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = actor, UserName = actor.ToString(), NormalizedUserName = actor.ToString().ToUpperInvariant(), DisplayName = "Isolated export fixture", PasswordHash = "non-login-export-sql-fixture", SecurityStamp = actor.ToString(), RoleCode = UserRoleCode.Supervisor, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow });
        db.Projects.Add(Project.Create(project, project.ToString(), "Isolated export", null, null, null, null, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(); return (actor, project);
    }
    private static ExportCaptureResult Capture(Guid snapshot, Guid actor, Guid project, DateTimeOffset now, string version = "fixture.v1")
        => new(null, new(new("anh02.export.v1", snapshot, project, "DOSSIER", "PDF", actor, now, now, [version], new("DOSSIER", "PDF"), [], [], [new("huy", "UNAVAILABLE", ["PRODUCER_NOT_READY"])], null, ""), null));
    [Fact]
    public async Task Admission_is_serializable_atomic_and_replay_retains_admission_snapshot()
    {
        var seed = await SeedAsync(); var key = Guid.NewGuid().ToString(); await using var db = Db(); var repo = Repo(db); var captures = 0;
        Task<ExportCaptureResult> Read(Guid snapshot, DateTimeOffset now, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Assert.Equal(System.Data.IsolationLevel.Serializable, db.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel);
            captures++; return Task.FromResult(Capture(snapshot, seed.Actor, seed.Project, now));
        }
        var first = await repo.AdmitAsync(seed.Actor, seed.Project, new("DOSSIER", "PDF"), key, new string('a', 64), null, Read, default);
        Assert.NotNull(first.Export); var original = first.Export.Snapshot.PayloadJson;
        var second = await repo.AdmitAsync(seed.Actor, seed.Project, new("DOSSIER", "PDF"), key, new string('a', 64), null, (_, _, _) => throw new InvalidOperationException("Producer changed after commit."), default);
        Assert.Equal(first.Export.Job.Id, second.Export!.Job.Id); Assert.Equal(original, second.Export.Snapshot.PayloadJson); Assert.Equal(1, captures);
        var changed = await repo.AdmitAsync(seed.Actor, seed.Project, new("DOSSIER", "ZIP"), key, new string('b', 64), null, Read, default); Assert.Equal("idempotency_conflict", changed.ErrorCode);
        Assert.Equal(1, await db.Set<ExportJob>().CountAsync(x => x.ProjectId == seed.Project)); Assert.Equal(1, await db.Set<ExportSnapshot>().CountAsync(x => x.ProjectId == seed.Project));
        Assert.Equal(1, await db.Set<AuditLog>().CountAsync(x => x.EntityId == first.Export.Job.Id)); Assert.Equal(1, await db.Set<IdempotencyRecord>().CountAsync(x => x.ProjectId == seed.Project && x.Operation == "Anh02.Export.Create"));
    }
    [Fact]
    public async Task Capture_failure_rolls_back_every_admission_row_and_same_key_can_retry()
    {
        var seed = await SeedAsync(); var key = Guid.NewGuid().ToString(); await using var db = Db(); var repo = Repo(db);
        var unavailable = await repo.AdmitAsync(seed.Actor, seed.Project, new("DOSSIER", "PDF"), key, new string('a', 64), null, (_, _, _) => Task.FromResult(new ExportCaptureResult("producer_unavailable", null)), default);
        Assert.Equal("producer_unavailable", unavailable.ErrorCode); Assert.Equal(0, await db.Set<ExportJob>().CountAsync(x => x.ProjectId == seed.Project)); Assert.Equal(0, await db.Set<ExportSnapshot>().CountAsync(x => x.ProjectId == seed.Project)); Assert.Equal(0, await db.Set<IdempotencyRecord>().CountAsync(x => x.ProjectId == seed.Project));
        var accepted = await repo.AdmitAsync(seed.Actor, seed.Project, new("DOSSIER", "PDF"), key, new string('a', 64), null, (id, now, _) => Task.FromResult(Capture(id, seed.Actor, seed.Project, now)), default); Assert.NotNull(accepted.Export);
    }
    [Fact]
    public async Task Concurrent_admission_and_worker_claim_create_one_receipt_and_one_current_lease()
    {
        var seed = await SeedAsync(); var key = Guid.NewGuid().ToString(); var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<ExportAdmissionResult> Send()
        {
            await using var db = Db(); await start.Task;
            return await Repo(db).AdmitAsync(seed.Actor, seed.Project, new("DOSSIER", "PDF"), key, new string('a', 64), null, (id, now, _) => Task.FromResult(Capture(id, seed.Actor, seed.Project, now)), default);
        }
        var a = Send(); var b = Send(); start.SetResult(); var admitted = await Task.WhenAll(a, b); Assert.Equal(admitted[0].Export!.Job.Id, admitted[1].Export!.Job.Id);
        var claimStart = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<ExportClaim?> Claim() { await using var db = Db(); await claimStart.Task; return await Repo(db).ClaimAsync(default); }
        var c = Claim(); var d = Claim(); claimStart.SetResult(); var claims = await Task.WhenAll(c, d); Assert.Single(claims.Where(x => x is not null));
        await using var verify = Db(); Assert.Equal(1, await verify.Set<ExportJob>().CountAsync(x => x.ProjectId == seed.Project)); Assert.Equal(1, await verify.Set<IdempotencyRecord>().CountAsync(x => x.ProjectId == seed.Project));
    }
    [Fact]
    public async Task Durable_renewal_restart_and_terminal_compare_prevent_stale_worker_publication()
    {
        var seed = await SeedAsync(); var clock = new ManualClock(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var firstDb = Db(); var first = new ExportRepository(firstDb, new IdempotencyOperationService(firstDb), clock);
        var admission = await first.AdmitAsync(seed.Actor, seed.Project, new("DOSSIER", "PDF"), Guid.NewGuid().ToString(), new string('a', 64), null, (id, now, _) => Task.FromResult(Capture(id, seed.Actor, seed.Project, now)), default);
        var oldClaim = await first.ClaimAsync(default); Assert.NotNull(oldClaim);
        clock.Now += TimeSpan.FromMinutes(4); Assert.True(await first.RenewAsync(oldClaim.Id, oldClaim.Token, default));
        await using var restartedDb = Db(); var restarted = new ExportRepository(restartedDb, new IdempotencyOperationService(restartedDb), clock);
        clock.Now += TimeSpan.FromMinutes(1); Assert.Null(await restarted.ClaimAsync(default));
        clock.Now += TimeSpan.FromMinutes(4); var recovered = await restarted.ClaimAsync(default); Assert.NotNull(recovered); Assert.NotEqual(oldClaim.Token, recovered.Token); Assert.Equal(oldClaim.Snapshot.Hash, recovered.Snapshot.Hash);
        var metadata = new Anh02ArtifactMetadata($"anh02/exports/{recovered.Id:D}/fixture.pdf", 5, new string('b', 64), "application/pdf");
        Assert.False(await first.CompleteAsync(oldClaim, metadata, _ => Task.FromResult(true), default));
        Assert.True(await restarted.CompleteAsync(recovered, metadata, _ => Task.FromResult(true), default));
        var published = await restarted.GetAsync(seed.Project, admission.Export!.Job.Id, default); Assert.NotNull(published); Assert.Equal("SUCCEEDED", published.Job.Status); Assert.Equal(clock.Now.AddDays(30), published.Job.ExpiresAt);
        // SQL persistence fixture only; storage integrity has independent actual-byte transport tests.
        Assert.Equal(1, await restartedDb.Set<GeneratedArtifact>().CountAsync(x => x.ExportJobId == recovered.Id)); Assert.Equal(0, await restartedDb.FileScopes.CountAsync()); Assert.Equal(0, await restartedDb.UploadSessions.CountAsync());
    }
    private sealed class ManualClock(DateTimeOffset initial) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = initial;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
