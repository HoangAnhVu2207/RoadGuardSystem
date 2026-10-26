using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding.Sandbox;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Seeding;

public sealed class SandboxSyntheticSeedTests
{
    [Fact]
    public async Task CanonicalBootstrapIsAcceptedWithoutChangingRolesOrConcurrencyStamps()
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed();
        await fixture.InitializeAsync();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture);
            await using var db = SandboxSeedTestSupport.CreateSafeContext(fixture);
            var bootstrap = new RoadGuardSystem.Repositories.Seeding.IdentityRoleSeedStep();
            await bootstrap.SeedAsync(db);
            var baseline = await db.Roles.AsNoTracking().OrderBy(r => r.Code)
                .Select(r => new { r.Code, r.Name, r.NormalizedName, r.IsActive, r.ConcurrencyStamp }).ToArrayAsync();
            Assert.Equal(5, baseline.Length);
            Assert.All(baseline, r => Assert.False(string.IsNullOrWhiteSpace(r.ConcurrencyStamp)));
            Assert.Empty(await db.Users.ToListAsync());
            var catalog = LoadCatalog();
            var credentials = Credentials(catalog);
            Assert.True((await SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, credentials)).Initialized);
            await bootstrap.SeedAsync(db);
            Assert.False((await SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, credentials)).Initialized);
            Assert.Equal(baseline, await db.Roles.AsNoTracking().OrderBy(r => r.Code)
                .Select(r => new { r.Code, r.Name, r.NormalizedName, r.IsActive, r.ConcurrencyStamp }).ToArrayAsync());
            Assert.Equal(11, await db.Users.CountAsync());
            Assert.Equal(2, await db.Projects.CountAsync());
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Theory]
    [InlineData("name")]
    [InlineData("normalized-name")]
    [InlineData("inactive")]
    [InlineData("missing")]
    [InlineData("null-stamp")]
    [InlineData("empty-stamp")]
    public async Task AlteredCanonicalBootstrapIsRejectedWithoutSeedDataOrReadyMarker(string mutation)
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed();
        await fixture.InitializeAsync();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture);
            await using var db = SandboxSeedTestSupport.CreateSafeContext(fixture);
            await new RoadGuardSystem.Repositories.Seeding.IdentityRoleSeedStep().SeedAsync(db);
            var role = await db.Roles.FirstAsync();
            switch (mutation)
            {
                case "name": role.Name = "Unexpected role"; break;
                case "normalized-name": role.NormalizedName = "UNEXPECTED"; break;
                case "inactive": role.IsActive = false; break;
                case "missing": db.Roles.Remove(role); break;
                case "null-stamp": role.ConcurrencyStamp = null; break;
                case "empty-stamp": role.ConcurrencyStamp = " "; break;
            }
            await db.SaveChangesAsync();
            var catalog = LoadCatalog();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, Credentials(catalog)));
            Assert.Equal("NonPristine", error.Message);
            Assert.Equal(0, await db.Users.CountAsync());
            Assert.Equal(0, await db.Projects.CountAsync());
            Assert.Null(await SandboxSeedGuard.ScalarAsync(db,
                "SELECT CONVERT(nvarchar(4000),value) FROM sys.extended_properties WHERE class=0 AND name=N'RoadGuardSandboxSeed';", default));
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task RuntimeSchemaAndExtendedPropertyRollbackGate()
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed();
        await fixture.InitializeAsync();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture);
            await using var db = SandboxSeedTestSupport.CreateSafeContext(fixture);
            await SandboxSeedGuard.ValidateTargetAsync(db, target);
            var schema = await SandboxSeedGuard.ReadSchemaAsync(db);
            Assert.Equal(195, schema.Tables.Length);
            // Each execution owns its output; CI must not depend on a developer's historical handoff directory.
            var evidence = Path.Combine(Path.GetTempPath(), "RoadGuardSystem-test-evidence", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(evidence);
            var output = Path.Combine(evidence, "runtime-schema.json");
            await File.WriteAllTextAsync(output, System.Text.Json.JsonSerializer.Serialize(schema));
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using (var tx = await db.Database.BeginTransactionAsync())
                {
                    await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_addextendedproperty @name=N'SbxRollbackGate', @value=N'test';");
                    await tx.RollbackAsync();
                }
                Assert.Null(await SandboxSeedGuard.ScalarAsync(db, "SELECT CONVERT(nvarchar(128),value) FROM sys.extended_properties WHERE class=0 AND name=N'SbxRollbackGate';", default));
            });
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task CompletedVersionPreservesDeveloperEdits()
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed();
        await fixture.InitializeAsync();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture);
            var catalog = LoadCatalog();
            var credentials = Credentials(catalog);
            await using var db = SandboxSeedTestSupport.CreateSafeContext(fixture);
            Assert.True((await SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, credentials)).Initialized);
            Assert.Equal(11, await db.Users.CountAsync()); Assert.Equal(2, await db.Projects.CountAsync());
            var user = await db.Users.FirstAsync(); user.DisplayName = "Developer edit"; await db.SaveChangesAsync();
            var hash = user.PasswordHash;
            Assert.False((await SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, credentials)).Initialized);
            Assert.Equal("Developer edit", (await db.Users.SingleAsync(u => u.Id == user.Id)).DisplayName);
            Assert.Equal(hash, (await db.Users.SingleAsync(u => u.Id == user.Id)).PasswordHash);
            var altered = new SandboxCredentialInput(credentials.BundleId, credentials.SeedVersion,
                credentials.Passwords.ToDictionary(p => p.Key, p => p.Value + "altered"));
            var mismatch = await Assert.ThrowsAsync<InvalidOperationException>(() => SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, altered));
            Assert.Equal("CredentialMismatch", mismatch.Message);
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Theory]
    [InlineData(@".\HANHNAV", "RoadGuardSystem")]
    [InlineData("localhost,1", "RoadGuardSystem")]
    [InlineData("sql", "RoadGuard_FE_unowned")]
    public async Task UnknownTargetRejectedBeforeConnect(string server, string database)
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer($"Server={server};Database={database};Integrated Security=True", o => o.UseNetTopologySuite()).Options;
        await using var db = new RoadGuardDbContext(options);
        var target = new SandboxSeedTarget("Session", database, "expected", Guid.NewGuid(), new string('a', 64));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SandboxSeedGuard.ValidateTargetAsync(db, target));
        Assert.Equal("TargetRejected", error.Message);
        Assert.Equal(System.Data.ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData("owner", "TargetRejected")]
    [InlineData("extra-table", "SchemaMismatch")]
    [InlineData("row", "NonPristine")]
    [InlineData("partial-role", "NonPristine")]
    [InlineData("version", "VersionMismatch")]
    [InlineData("schema", "SchemaMismatch")]
    public async Task WrongTargetSchemaVersionAndNonPristineAreRefused(string kind, string expected)
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed();
        await fixture.InitializeAsync();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture);
            await using var db = SandboxSeedTestSupport.CreateSafeContext(fixture);
            if (kind == "owner") target = target with { OwnerId = Guid.NewGuid() };
            if (kind == "extra-table") await db.Database.ExecuteSqlRawAsync("CREATE TABLE dbo.Unreviewed(Id int);");
            if (kind == "row") db.Projects.Add(RoadGuardSystem.BusinessObjects.Projects.Project.Create(Guid.NewGuid(), "EXISTING", "Existing", null, null, null, null, DateTimeOffset.UtcNow));
            if (kind == "partial-role") db.Roles.Add(new RoadGuardSystem.BusinessObjects.Identity.ApplicationRole(RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode.Supervisor, "Supervisor"));
            if (kind is "row" or "partial-role") await db.SaveChangesAsync();
            if (kind == "version") await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_addextendedproperty @name=N'RoadGuardSandboxSeed',@value=N'{{}}';");
            if (kind == "schema") await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.Projects ADD Unreviewed nvarchar(10) NULL;");
            var catalog = LoadCatalog();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, Credentials(catalog)));
            if (kind == "schema")
                Assert.Equal(expected + ": structure signature " + (await SandboxSeedGuard.ReadSchemaAsync(db)).Signature, error.Message);
            else Assert.Equal(expected, error.Message);
            Assert.Equal(0, await db.Users.CountAsync());
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task ConcurrentInitializationCommitsOnceAndWrongBundleIsRefused()
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed();
        await fixture.InitializeAsync();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture);
            var catalog = LoadCatalog(); var credentials = Credentials(catalog);
            await using var first = SandboxSeedTestSupport.CreateSafeContext(fixture);
            await using var second = SandboxSeedTestSupport.CreateSafeContext(fixture);
            var outcomes = await Task.WhenAll(SandboxSyntheticSeeder.InitializeAsync(first, target, catalog, credentials), SandboxSyntheticSeeder.InitializeAsync(second, target, catalog, credentials));
            Assert.Single(outcomes.Where(o => o.Initialized)); Assert.Single(outcomes.Where(o => !o.Initialized));
            Assert.Equal(11, await first.Users.CountAsync());
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SandboxSyntheticSeeder.InitializeAsync(first, target, catalog, Credentials(catalog)));
            Assert.Equal("VersionMismatch", error.Message);
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task IndependentFixturesRejectCrossDrop()
    {
        var first = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed();
        var second = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed();
        await first.InitializeAsync(); await second.InitializeAsync();
        try
        {
            Assert.NotEqual(first.DatabaseName, second.DatabaseName);
            Assert.NotEqual(first.GetOwnedSeedDatabaseIdentity().OwnerId, second.GetOwnedSeedDatabaseIdentity().OwnerId);
            await Assert.ThrowsAsync<InvalidOperationException>(() => first.DropDatabaseByNameAsync(second.DatabaseName));
            Assert.True(await second.DatabaseExistsAsync(second.DatabaseName));
        }
        finally { await first.DisposeAsync(); await second.DisposeAsync(); }
    }

    [Fact]
    public async Task FaultBeforeMarkerRollsBackGraphAndMarker()
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed(); await fixture.InitializeAsync();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture); var catalog = LoadCatalog();
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString, o => o.UseNetTopologySuite().EnableRetryOnFailure()).AddInterceptors(new MarkerFault()).Options;
            await using (var db = new RoadGuardDbContext(options))
                await Assert.ThrowsAsync<InvalidOperationException>(() => SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, Credentials(catalog)));
            await using var verify = SandboxSeedTestSupport.CreateSafeContext(fixture);
            Assert.Equal(0, await verify.Users.CountAsync()); Assert.Equal(0, await verify.Projects.CountAsync()); Assert.Equal(0, await verify.Roles.CountAsync());
            Assert.Null(await SandboxSeedGuard.ScalarAsync(verify, "SELECT CONVERT(nvarchar(4000),value) FROM sys.extended_properties WHERE class=0 AND name=N'RoadGuardSandboxSeed';", default));
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task NonCooperatingWriterCannotEnterBetweenPristineCheckAndCommit()
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed(); await fixture.InitializeAsync();
        var barrier = new InsertBarrier();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture); var catalog = LoadCatalog();
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString, o => o.UseNetTopologySuite().EnableRetryOnFailure()).AddInterceptors(barrier).Options;
            await using var db = new RoadGuardDbContext(options);
            var seed = SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, Credentials(catalog));
            await barrier.Entered.Task.WaitAsync(TimeSpan.FromSeconds(40));
            try
            {
                await using var outsider = new Microsoft.Data.SqlClient.SqlConnection(fixture.ConnectionString); await outsider.OpenAsync();
                await using var cmd = outsider.CreateCommand(); cmd.CommandTimeout = 5;
                cmd.CommandText = "SET LOCK_TIMEOUT 1000; UPDATE dbo.Projects SET Name=N'Noncooperating writer';";
                var error = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => cmd.ExecuteNonQueryAsync());
                Assert.Equal(1222, error.Number);
            }
            finally { barrier.Release.TrySetResult(); }
            Assert.True((await seed).Initialized);
            Assert.DoesNotContain(await db.Projects.ToListAsync(), p => p.Name == "Noncooperating writer");
        }
        finally { barrier.Release.TrySetResult(); await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task LostCommitAcknowledgementRetriesAgainstMatchingMarker()
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed(); await fixture.InitializeAsync();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture); var catalog = LoadCatalog();
            var fault = new CommitAcknowledgementFault();
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString, o => o.UseNetTopologySuite().EnableRetryOnFailure()).AddInterceptors(fault).Options;
            await using var db = new RoadGuardDbContext(options);
            Assert.False((await SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, Credentials(catalog))).Initialized);
            Assert.Equal(1, fault.Faults); Assert.Equal(11, await db.Users.CountAsync()); Assert.Equal(2, await db.Projects.CountAsync());
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task CancellationBeforeInsertLeavesPristineDatabaseAndNoMarker()
    {
        var fixture = RoadGuardSystem.IntegrationTests.Infrastructure.SqlServerTestFixture.CreateForSandboxSeed(); await fixture.InitializeAsync();
        var barrier = new InsertBarrier(); using var cancellation = new CancellationTokenSource();
        try
        {
            var target = await SandboxSeedTestSupport.CreateOwnedTargetAsync(fixture); var catalog = LoadCatalog();
            var options = new DbContextOptionsBuilder<RoadGuardDbContext>().UseSqlServer(fixture.ConnectionString, o => o.UseNetTopologySuite().EnableRetryOnFailure()).AddInterceptors(barrier).Options;
            await using var db = new RoadGuardDbContext(options);
            var seed = SandboxSyntheticSeeder.InitializeAsync(db, target, catalog, Credentials(catalog), cancellation.Token);
            await barrier.Entered.Task.WaitAsync(TimeSpan.FromSeconds(40)); await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => seed);
            await using var verify = SandboxSeedTestSupport.CreateSafeContext(fixture);
            Assert.Equal(0, await verify.Users.CountAsync()); Assert.Equal(0, await verify.Projects.CountAsync());
            Assert.Null(await SandboxSeedGuard.ScalarAsync(verify, "SELECT CONVERT(nvarchar(4000),value) FROM sys.extended_properties WHERE class=0 AND name=N'RoadGuardSandboxSeed';", default));
        }
        finally { barrier.Release.TrySetResult(); await fixture.DisposeAsync(); }
    }

    private sealed class CommitAcknowledgementFault : Microsoft.EntityFrameworkCore.Diagnostics.DbTransactionInterceptor
    {
        public int Faults { get; private set; }
        public override Task TransactionCommittedAsync(System.Data.Common.DbTransaction transaction, Microsoft.EntityFrameworkCore.Diagnostics.TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Faults == 0) { Faults++; throw new TimeoutException("Synthetic lost commit acknowledgement"); }
            return Task.CompletedTask;
        }
    }

    private sealed class MarkerFault : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> NonQueryExecutingAsync(
            System.Data.Common.DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("sp_addextendedproperty", StringComparison.Ordinal) && command.CommandText.Contains("RoadGuardSandboxSeed", StringComparison.Ordinal))
                throw new InvalidOperationException("Synthetic transaction fault");
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
    private sealed class InsertBarrier : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("[Users]", StringComparison.Ordinal) && (command.CommandText.Contains("INSERT", StringComparison.Ordinal) || command.CommandText.Contains("MERGE", StringComparison.Ordinal)))
            { Entered.TrySetResult(); await Release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken); }
            return result;
        }
    }

    internal static SandboxSeedCatalog LoadCatalog()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))) dir = dir.Parent;
        return SandboxSeedCatalog.Load(Path.Combine(dir!.FullName, "sandbox", "seeds", "v1"));
    }
    internal static SandboxCredentialInput Credentials(SandboxSeedCatalog catalog) => new(Guid.NewGuid(), catalog.Version,
        catalog.Actors.ToDictionary(a => a.Id, _ => "Sbx1!" + Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))));
}
