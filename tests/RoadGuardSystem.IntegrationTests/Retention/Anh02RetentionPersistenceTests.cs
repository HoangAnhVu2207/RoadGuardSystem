using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Retention;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.DTOs.Retention;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Retention;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;
namespace RoadGuardSystem.IntegrationTests.Retention;

public sealed class Anh02RetentionPersistenceTests : IAsyncLifetime
{
    private readonly SqlServerTestFixture fixture = new(createSpatialProbeSchema: false);
    public async Task InitializeAsync() { await fixture.InitializeAsync(); await using var db = Db(); await db.Database.MigrateAsync(); }
    public Task DisposeAsync() => fixture.DisposeAsync();
    private RoadGuardDbContext Db(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors) => new(new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString, o => o.UseNetTopologySuite()).AddInterceptors(interceptors).Options);
    private static RetentionRepository Repository(RoadGuardDbContext db, bool completeFixture = false) => new(db, new IdempotencyOperationService(db), Inventory(db, completeFixture), TimeProvider.System);
    private static RetentionInventoryRepository Inventory(RoadGuardDbContext db, bool completeFixture) => new(db, completeFixture ? [new AvailableEmptyContributor("HUY"), new AvailableEmptyContributor("AI"), new AvailableEmptyContributor("EXPORT")] : []);
    private async Task<(Guid Supervisor, Guid Pm, Guid Project, Guid File, Guid Warranty)> SeedAsync()
    {
        await using var db = Db();
        await db.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code='SUPERVISOR') INSERT INTO Roles (Code,Name,NormalizedName,IsActive) VALUES ('SUPERVISOR','Supervisor','SUPERVISOR',1); IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code='PM') INSERT INTO Roles (Code,Name,NormalizedName,IsActive) VALUES ('PM','Project manager','PM',1);");
        var supervisor = Guid.NewGuid(); var pm = Guid.NewGuid(); var project = Guid.NewGuid(); var file = Guid.NewGuid(); var warranty = Guid.NewGuid();
        db.Users.AddRange(User(supervisor, UserRoleCode.Supervisor), User(pm, UserRoleCode.ProjectManager));
        db.Projects.Add(Project.Create(project, project.ToString(), "Isolated retention fixture", null, null, null, null, DateTimeOffset.UtcNow));
        db.ProjectMembers.Add(ProjectMember.CreatePrimaryProjectManager(Guid.NewGuid(), project, pm, new DateOnly(2000, 1, 1)));
        db.Files.Add(StoredFile.Create(file, $"fixture/{file:N}", "evidence.jpg", "image/jpeg", 10, new string('a', 64), supervisor, DateTimeOffset.UtcNow, null));
        db.FileScopes.Add(FileScope.Create(Guid.NewGuid(), file, project, null, supervisor, "SURVEY_MEDIA", DateTimeOffset.UtcNow));
        db.Warranties.Add(Warranty.Create(warranty, project, null, null, new DateOnly(2000, 1, 1), new DateOnly(2000, 1, 1), new DateOnly(2001, 1, 1), null, WarrantyScope.Project, null, null, WarrantyStatus.Expired));
        await db.SaveChangesAsync(); return (supervisor, pm, project, file, warranty);
    }
    private static ApplicationUser User(Guid id, UserRoleCode role) => new() { Id = id, UserName = id.ToString(), NormalizedUserName = id.ToString().ToUpperInvariant(), DisplayName = "Isolated retention fixture", RoleCode = role, Status = UserStatus.Active, CreatedAt = DateTimeOffset.UtcNow, SecurityStamp = id.ToString(), PasswordHash = "isolated-test-only-no-login" };
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Basis_and_release_deny_replay_or_conflict_after_preflight(bool release, bool conflict)
    {
        var seed = await SeedAsync(); var key = Guid.NewGuid().ToString(); Guid holdId = default; string version = "none"; string inventoryVersion = "";
        await using (var first = Db())
        {
            var repo = Repository(first, completeFixture: true);
            if (release)
            {
                var hold = await repo.CreateHoldAsync(seed.Supervisor, new("FILE", seed.File, "Review"), Guid.NewGuid().ToString(), default);
                holdId = hold.Id; version = hold.Version;
                await repo.ReleaseHoldAsync(seed.Supervisor, holdId, new("Released"), key, version, default);
            }
            else
            {
                inventoryVersion = (await repo.GetFileAsync(seed.Supervisor, seed.Project, seed.File, default)).InventoryVersion;
                await repo.ConfirmBasisAsync(seed.Supervisor, seed.Project, seed.File, new([seed.Warranty], inventoryVersion, "Confirmed"), key, "none", default);
            }
        }
        await using var verify = Db();
        var audits = await verify.AuditLogs.CountAsync(x => x.ActorUserId == seed.Supervisor);
        var receipts = await verify.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().CountAsync(x => x.ActorUserId == seed.Supervisor);
        var hook = new ReceiptHook(async (_, token) =>
        {
            await using var revoke = Db(); await revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status=2 WHERE Id={seed.Supervisor}", token);
        });
        await using var db = Db(hook); var current = Repository(db, completeFixture: true);
        var error = await Assert.ThrowsAsync<RetentionRequestException>(async () =>
        {
            if (release) await current.ReleaseHoldAsync(seed.Supervisor, holdId, new(conflict ? "Changed" : "Released"), key, version, default);
            else await current.ConfirmBasisAsync(seed.Supervisor, seed.Project, seed.File, new([seed.Warranty], inventoryVersion, conflict ? "Changed" : "Confirmed"), key, "none", default);
        });
        Assert.Equal(403, error.Status); Assert.Equal("access_forbidden", error.Code); Assert.Equal(1, hook.Calls);
        Assert.Equal(audits, await verify.AuditLogs.CountAsync(x => x.ActorUserId == seed.Supervisor));
        Assert.Equal(receipts, await verify.Set<RoadGuardSystem.BusinessObjects.Idempotency.IdempotencyRecord>().CountAsync(x => x.ActorUserId == seed.Supervisor));
        Assert.Equal(release ? 0 : 1, await verify.Set<RetentionBasisRevision>().CountAsync(x => x.FileId == seed.File));
        Assert.Equal(release ? 2 : 0, await verify.Set<RetentionHoldHistory>().CountAsync(x => x.HoldId == holdId));
    }
    [Fact]
    public async Task Actual_inventory_missing_Huy_is_waiting_and_cannot_confirm_subset()
    {
        var seed = await SeedAsync(); await using var db = Db(); var repo = Repository(db);
        var file = await repo.GetFileAsync(seed.Pm, seed.Project, seed.File, default);
        Assert.False(file.InventoryComplete); Assert.Contains("HUY_REFERENCE_INVENTORY_UNAVAILABLE", file.InventoryReasonCodes); Assert.Single(file.ApplicableObligations);
        Assert.Equal("WAITING_RETENTION_BASIS", file.Evaluation.Eligibility);
        var error = await Assert.ThrowsAsync<RetentionRequestException>(() => repo.ConfirmBasisAsync(seed.Supervisor, seed.Project, seed.File, new([seed.Warranty], file.InventoryVersion, "Confirm all known references"), Guid.NewGuid().ToString(), "none", default));
        Assert.Equal("retention_inventory_incomplete", error.Code);
        Assert.Equal(0, await db.Set<RetentionBasisRevision>().CountAsync(x => x.FileId == seed.File));
    }
    [Fact]
    public async Task Independent_holds_atomic_history_replay_stale_and_revoked_authority()
    {
        var seed = await SeedAsync(); await using var db = Db(); var repo = Repository(db);
        var request = new CreateRetentionHoldRequest("FILE", seed.File, "Independent legal review"); var key = Guid.NewGuid().ToString();
        var first = await repo.CreateHoldAsync(seed.Supervisor, request, key, default);
        Assert.Equal(first, await repo.CreateHoldAsync(seed.Supervisor, request, key, default));
        var second = await repo.CreateHoldAsync(seed.Supervisor, request, Guid.NewGuid().ToString(), default);
        var releaseKey = Guid.NewGuid().ToString();
        var released = await repo.ReleaseHoldAsync(seed.Supervisor, first.Id, new("Review completed"), releaseKey, first.Version, default);
        Assert.Equal(released, await repo.ReleaseHoldAsync(seed.Supervisor, first.Id, new("Review completed"), releaseKey, first.Version, default));
        var current = await repo.GetFileAsync(seed.Pm, seed.Project, seed.File, default);
        Assert.Equal(1, current.ActiveHoldCount); Assert.Equal("BLOCKED_HOLD", current.Evaluation.Eligibility);
        var stale = await Assert.ThrowsAsync<RetentionRequestException>(() => repo.ReleaseHoldAsync(seed.Supervisor, first.Id, new("Another release"), Guid.NewGuid().ToString(), first.Version, default));
        Assert.Equal(412, stale.Status);
        var terminal = await Assert.ThrowsAsync<RetentionRequestException>(() => repo.ReleaseHoldAsync(seed.Supervisor, first.Id, new("Terminal release"), Guid.NewGuid().ToString(), released.Version, default)); Assert.Equal("invalid_transition", terminal.Code);
        Assert.Equal(3, await db.Set<RetentionHoldHistory>().CountAsync(x => x.HoldId == first.Id || x.HoldId == second.Id));
        var actor = await db.Users.SingleAsync(x => x.Id == seed.Supervisor); actor.Status = UserStatus.Suspended; await db.SaveChangesAsync();
        var denied = await Assert.ThrowsAsync<RetentionRequestException>(() => repo.CreateHoldAsync(seed.Supervisor, request, key, default));
        Assert.Equal(403, denied.Status);
    }
    [Fact]
    public async Task Fresh_hold_locks_current_authority_and_disabled_role_cannot_read_history()
    {
        var seed = await SeedAsync();
        var guard = new AuthorityLockProbe(async token =>
        {
            await using var revoke = Db();
            await revoke.Database.OpenConnectionAsync(token);
            await revoke.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 500", token);
            var blocked = await Assert.ThrowsAsync<SqlException>(() => revoke.Database.ExecuteSqlInterpolatedAsync($"UPDATE Users SET Status=2 WHERE Id={seed.Supervisor}", token));
            Assert.Equal(1222, blocked.Number);
        });
        await using var db = Db(guard);
        var hold = await Repository(db).CreateHoldAsync(seed.Supervisor, new("FILE", seed.File, "Fresh authority race"), Guid.NewGuid().ToString(), default);
        Assert.Equal(1, guard.Calls);
        await using var verify = Db();
        await verify.Database.ExecuteSqlRawAsync("UPDATE Roles SET IsActive=0 WHERE Code='SUPERVISOR'");
        var denied = await Assert.ThrowsAsync<RetentionRequestException>(() => Repository(verify).GetHoldAsync(seed.Supervisor, hold.Id, default));
        Assert.Equal(404, denied.Status);
        Assert.Equal(1, await verify.Set<RetentionHoldHistory>().CountAsync(row => row.HoldId == hold.Id));
    }

    [Fact]
    public async Task Concurrent_same_key_creates_one_hold_and_one_history()
    {
        var seed = await SeedAsync(); var request = new CreateRetentionHoldRequest("PROJECT", seed.Project, "Concurrent review"); var key = Guid.NewGuid().ToString();
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<RetentionHoldView> SendAsync() { await using var db = Db(); await barrier.Task; return await Repository(db).CreateHoldAsync(seed.Supervisor, request, key, default); }
        var a = SendAsync(); var b = SendAsync(); barrier.SetResult(); var results = await Task.WhenAll(a, b);
        Assert.Equal(results[0].Id, results[1].Id);
        await using var verify = Db(); Assert.Equal(1, await verify.Set<RetentionHold>().CountAsync(x => x.ScopeId == seed.Project)); Assert.Equal(1, await verify.Set<RetentionHoldHistory>().CountAsync(x => x.HoldId == results[0].Id));
    }
    [Fact]
    public async Task Basis_revisions_and_evaluation_snapshots_drift_without_source_writes()
    {
        // Empty contributor fixtures establish the SQL command/snapshot seam only, not live Huy integration.
        var seed = await SeedAsync(); await using var db = Db(); var repo = Repository(db, true);
        var view = await repo.GetFileAsync(seed.Supervisor, seed.Project, seed.File, default);
        var basis = await repo.ConfirmBasisAsync(seed.Supervisor, seed.Project, seed.File, new([seed.Warranty], view.InventoryVersion, "Confirmed fixture inventory"), Guid.NewGuid().ToString(), "none", default);
        var job = await repo.AdmitEvaluationAsync(seed.Pm, seed.Project, new([seed.File]), Guid.NewGuid().ToString(), default);
        Assert.True(await repo.ProcessOneAsync(default));
        var result = await repo.GetEvaluationAsync(seed.Pm, seed.Project, job.Id, null, 50, default);
        Assert.Equal("COMPLETE", result.Status); Assert.Single(result.Items); Assert.True(result.Items[0].IsCurrent); Assert.Equal("ELIGIBLE_FOR_REVIEW", result.Items[0].Eligibility);
        var source = await db.Files.AsNoTracking().SingleAsync(x => x.Id == seed.File);
        await repo.CreateHoldAsync(seed.Supervisor, new("FILE", seed.File, "New hold after snapshot"), Guid.NewGuid().ToString(), default);
        var old = await repo.GetEvaluationAsync(seed.Pm, seed.Project, job.Id, null, 50, default);
        Assert.Equal(result.EvaluatedAt, old.EvaluatedAt); Assert.Equal(result.Items[0].Eligibility, old.Items[0].Eligibility); Assert.False(old.Items[0].IsCurrent);
        Assert.False(await repo.ProcessOneAsync(default));
        var after = await db.Files.AsNoTracking().SingleAsync(x => x.Id == seed.File); Assert.Equal(source.Checksum, after.Checksum); Assert.Equal(source.RetentionUntil, after.RetentionUntil); Assert.Equal(source.StorageUri, after.StorageUri);
        Assert.Equal(1, await db.Set<RetentionBasisRevision>().CountAsync(x => x.Id == basis.Id)); Assert.Equal(1, await db.Set<RetentionEvaluationItem>().CountAsync(x => x.EvaluationId == job.Id));
        await Assert.ThrowsAsync<SqlException>(() => db.Set<RetentionBasisRevision>().Where(x => x.Id == basis.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Reason, "Forbidden historical rewrite")));
        await Assert.ThrowsAsync<SqlException>(() => db.Set<RetentionEvaluationItem>().Where(x => x.EvaluationId == job.Id).ExecuteUpdateAsync(x => x.SetProperty(y => y.Eligibility, "BLOCKED_HOLD")));
    }
    [Fact]
    public async Task PM_cannot_mutate_or_see_private_hold()
    {
        var seed = await SeedAsync(); await using var db = Db(); var repo = Repository(db);
        var privateFile = Guid.NewGuid(); db.Files.Add(StoredFile.Create(privateFile, $"fixture/{privateFile:N}", "private.jpg", "image/jpeg", 10, new string('b', 64), seed.Supervisor, DateTimeOffset.UtcNow, null)); db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), privateFile, seed.Supervisor, DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
        var hold = await repo.CreateHoldAsync(seed.Supervisor, new("FILE", privateFile, "Private hold"), Guid.NewGuid().ToString(), default);
        Assert.Equal(404, (await Assert.ThrowsAsync<RetentionRequestException>(() => repo.GetHoldAsync(seed.Pm, hold.Id, default))).Status);
        Assert.Equal(403, (await Assert.ThrowsAsync<RetentionRequestException>(() => repo.CreateHoldAsync(seed.Pm, new("FILE", seed.File, "Forbidden mutation"), Guid.NewGuid().ToString(), default))).Status);
    }
    [Fact]
    public async Task Concurrent_same_key_basis_has_one_head_revision_and_replay_outcome()
    {
        var seed = await SeedAsync(); string version;
        await using (var read = Db()) version = (await Repository(read, true).GetFileAsync(seed.Supervisor, seed.Project, seed.File, default)).InventoryVersion;
        var request = new ConfirmRetentionBasisRequest([seed.Warranty], version, "Concurrent basis confirmation"); var key = Guid.NewGuid().ToString();
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<RetentionBasisView> SendAsync() { await using var db = Db(); await barrier.Task; return await Repository(db, true).ConfirmBasisAsync(seed.Supervisor, seed.Project, seed.File, request, key, "none", default); }
        var a = SendAsync(); var b = SendAsync(); barrier.SetResult(); var rows = await Task.WhenAll(a, b); Assert.Equal(rows[0].Id, rows[1].Id);
        await using var verify = Db(); Assert.Equal(1, await verify.Set<RetentionBasisHead>().CountAsync(x => x.FileId == seed.File)); Assert.Equal(1, await verify.Set<RetentionBasisRevision>().CountAsync(x => x.FileId == seed.File));
    }
    [Fact]
    public async Task Hold_race_linearizes_after_snapshot_and_old_result_becomes_stale()
    {
        var seed = await SeedAsync(); Guid jobId;
        await using (var admission = Db())
        {
            var repo = Repository(admission, true); var file = await repo.GetFileAsync(seed.Supervisor, seed.Project, seed.File, default);
            await repo.ConfirmBasisAsync(seed.Supervisor, seed.Project, seed.File, new([seed.Warranty], file.InventoryVersion, "Race fixture confirmation"), Guid.NewGuid().ToString(), "none", default);
            jobId = (await repo.AdmitEvaluationAsync(seed.Pm, seed.Project, new([seed.File]), Guid.NewGuid().ToString(), default)).Id;
        }
        await using var workerDb = Db();
        var gate = new BlockingInventory(Inventory(workerDb, true));
        var worker = new RetentionRepository(workerDb, new IdempotencyOperationService(workerDb), gate, TimeProvider.System);
        var evaluating = worker.ProcessOneAsync(default); await gate.Entered.Task;
        await using var holdDb = Db(); var holding = Repository(holdDb, true).CreateHoldAsync(seed.Supervisor, new("FILE", seed.File, "Concurrent hold"), Guid.NewGuid().ToString(), default);
        gate.Resume.SetResult(); Assert.True(await evaluating); await holding;
        await using var verify = Db(); var result = await Repository(verify, true).GetEvaluationAsync(seed.Pm, seed.Project, jobId, null, 50, default);
        Assert.Equal("ELIGIBLE_FOR_REVIEW", result.Items.Single().Eligibility); Assert.False(result.Items.Single().IsCurrent);
        Assert.Equal("BLOCKED_HOLD", (await Repository(verify, true).GetFileAsync(seed.Pm, seed.Project, seed.File, default)).Evaluation.Eligibility);
    }
    [Fact]
    public async Task Actual_generated_export_uses_completedAt_thirty_days_and_evidence_adds_longer_warranty()
    {
        var seed = await SeedAsync(); await using var db = Db();
        var outputFile = Guid.NewGuid(); var snapshotId = Guid.NewGuid(); var artifactId = Guid.NewGuid(); var completedAt = DateTimeOffset.Parse("2026-09-01T00:00:00Z"); var lease = Guid.NewGuid();
        var job = ExportJob.Create(Guid.NewGuid(), seed.Project, seed.Supervisor, snapshotId, "DOSSIER", "ZIP", completedAt);
        job.TryClaim(lease, completedAt, TimeSpan.FromHours(1)); db.Add(ExportSnapshot.Create(snapshotId, seed.Project, "{}", new string('c', 64), completedAt)); db.Add(job); await db.SaveChangesAsync();
        db.Files.Add(StoredFile.Create(outputFile, $"fixture/{outputFile}", "dossier.zip", "application/zip", 10, new string('d', 64), seed.Supervisor, completedAt.AddYears(-10), null)); db.Add(GeneratedArtifact.Create(artifactId, job.Id, snapshotId, outputFile, completedAt)); await db.SaveChangesAsync();
        job.Complete(lease, artifactId, completedAt); await db.SaveChangesAsync();
        db.Warranties.Add(Warranty.Create(Guid.NewGuid(), seed.Project, null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), new DateOnly(2027, 10, 2), null, WarrantyScope.Project, null, null, WarrantyStatus.Active)); await db.SaveChangesAsync();
        var reader = new RetentionInventoryRepository(db, [new AvailableEmptyContributor("HUY"), new AvailableEmptyContributor("AI"), new ExportRetentionInventoryContributor(db)]);
        var initial = await reader.ReadAsync(outputFile, default); Assert.NotNull(initial); Assert.True(initial!.Complete); Assert.Equal("TEMPORARY_EXPORT", initial.Classification); Assert.Empty(initial.Warranties); Assert.Equal(completedAt.AddDays(30), initial.AdditionalEligibleAfter!.Single());
        var repo = new RetentionRepository(db, new IdempotencyOperationService(db), reader, new FixedTimeProvider(DateTimeOffset.Parse("2026-10-02T00:00:00Z")));
        var basis = await repo.ConfirmBasisAsync(seed.Supervisor, seed.Project, outputFile, new([], initial.Version, "Confirmed temporary export"), Guid.NewGuid().ToString(), "none", default);
        Assert.Equal("ELIGIBLE_FOR_REVIEW", (await repo.GetFileAsync(seed.Pm, seed.Project, outputFile, default)).Evaluation.Eligibility);
        db.FileScopes.Add(FileScope.Create(Guid.NewGuid(), outputFile, seed.Project, null, seed.Supervisor, "SURVEY_MEDIA", DateTimeOffset.UtcNow)); await db.SaveChangesAsync();
        var relinked = await reader.ReadAsync(outputFile, default); Assert.NotNull(relinked); Assert.Equal("EVIDENCE", relinked!.Classification); Assert.Equal(2, relinked.Warranties.Count); Assert.NotEqual(initial.Version, relinked.Version);
        Assert.Equal("WAITING_RETENTION_BASIS", (await repo.GetFileAsync(seed.Pm, seed.Project, outputFile, default)).Evaluation.Eligibility);
        var subset = await Assert.ThrowsAsync<RetentionRequestException>(() => repo.ConfirmBasisAsync(seed.Supervisor, seed.Project, outputFile, new([seed.Warranty], relinked.Version, "Omitted later warranty"), Guid.NewGuid().ToString(), basis.Version, default)); Assert.Equal("basis_invalid", subset.Code);
        await repo.ConfirmBasisAsync(seed.Supervisor, seed.Project, outputFile, new(relinked.Warranties.Select(x => x.Id).ToArray(), relinked.Version, "Evidence obligation supersedes temporary classification"), Guid.NewGuid().ToString(), basis.Version, default);
        var evidence = await repo.GetFileAsync(seed.Pm, seed.Project, outputFile, default); Assert.Equal("RETAIN_UNTIL", evidence.Evaluation.Eligibility); Assert.Equal(DateTimeOffset.Parse("2032-10-02T17:00:00Z"), evidence.Evaluation.EligibleAfter);
    }
    [Fact]
    public async Task Evaluation_pagination_never_returns_private_file_as_cursor()
    {
        // Materialized historical results exercise current visibility independently of admission.
        var seed = await SeedAsync(); await using var db = Db(); var job = new RetentionEvaluation { Id = Guid.NewGuid(), ProjectId = seed.Project, RequestedBy = seed.Supervisor, CreatedAt = DateTimeOffset.UtcNow, EvaluatedAt = DateTimeOffset.UtcNow, Status = "COMPLETE" };
        db.Add(job);
        foreach (var file in new[] { seed.File, Guid.NewGuid(), Guid.NewGuid() })
        {
            if (file != seed.File)
            {
                db.Files.Add(StoredFile.Create(file, $"fixture/{file}", "private.jpg", "image/jpeg", 10, new string('e', 64), seed.Supervisor, DateTimeOffset.UtcNow, null));
                db.FileScopes.Add(FileScope.CreatePrivate(Guid.NewGuid(), file, seed.Supervisor, DateTimeOffset.UtcNow));
            }
            db.Add(new RetentionEvaluationItem { Id = Guid.NewGuid(), EvaluationId = job.Id, FileId = file, Eligibility = "WAITING_RETENTION_BASIS", InventoryVersion = new string('f', 64), HoldVersion = new string('f', 64) });
        }
        await db.SaveChangesAsync(); var result = await Repository(db).GetEvaluationAsync(seed.Pm, seed.Project, job.Id, null, 1, default);
        Assert.Single(result.Items); Assert.Equal(seed.File, result.Items[0].FileId); Assert.Null(result.NextCursor);
    }
    private sealed class AvailableEmptyContributor(string name) : IRetentionInventoryContributor
    {
        public string Name => name;
        public Task<RetentionInventoryContribution> ReadAsync(Guid fileId, CancellationToken token) => Task.FromResult(new RetentionInventoryContribution(name, true, [], []));
        public Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token) => Task.FromResult<IReadOnlyList<Guid>>([]);
    }
    private sealed class BlockingInventory(IRetentionInventoryRepository inner) : IRetentionInventoryRepository
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<RetentionInventory?> ReadAsync(Guid fileId, CancellationToken token) { Entered.TrySetResult(); await Resume.Task.WaitAsync(token); return await inner.ReadAsync(fileId, token); }
        public Task<IReadOnlyList<Guid>> KnownProjectFilesAsync(Guid projectId, CancellationToken token) => inner.KnownProjectFilesAsync(projectId, token);
    }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ReceiptHook(Func<System.Data.Common.DbCommand, CancellationToken, Task> callback)
        : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public int Calls { get; private set; }
        public override async ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (Calls == 0 && command.CommandText.Contains("FROM [IdempotencyRecords]", StringComparison.Ordinal))
            {
                Calls++;
                await callback(command, cancellationToken);
            }
            return result;
        }
    }

    private sealed class AuthorityLockProbe(Func<CancellationToken, Task> callback)
        : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public int Calls { get; private set; }
        public override async ValueTask<System.Data.Common.DbDataReader> ReaderExecutedAsync(
            System.Data.Common.DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandExecutedEventData eventData,
            System.Data.Common.DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (Calls == 0 && command.CommandText.Contains("[Users] WITH (UPDLOCK,HOLDLOCK)", StringComparison.Ordinal))
            {
                Calls++;
                await callback(cancellationToken);
            }
            return result;
        }
    }
}
