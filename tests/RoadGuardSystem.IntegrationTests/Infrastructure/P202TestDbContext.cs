using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.Repositories;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class P202TransactionProbe
{
    public Guid Id { get; set; }

    public string Value { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];
}

public sealed class P202TestDbContext : RoadGuardDbContext
{
    public P202TestDbContext(DbContextOptions<P202TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<P202TransactionProbe> TransactionProbes => Set<P202TransactionProbe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<P202TransactionProbe>(builder =>
        {
            builder.ToTable("P202TransactionProbes");
            builder.HasKey(probe => probe.Id);
            builder.Property(probe => probe.Value).HasMaxLength(200).IsRequired();
        });
    }
}

public sealed class P202SqlServerFixture : IAsyncLifetime
{
    private readonly SqlServerTestFixture _database = new();

    public string ConnectionString => _database.ConnectionString;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        await using (var baselineContext = _database.CreateDbContext())
        {
            await baselineContext.Database.EnsureDeletedAsync();
        }

        await using (var productionContext = CreateProductionDbContext())
        {
            await productionContext.Database.MigrateAsync();
        }

        await using var context = CreateDbContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE [P202TransactionProbes]
            (
                [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_P202TransactionProbes] PRIMARY KEY,
                [Value] nvarchar(200) NOT NULL,
                [RowVersion] rowversion NOT NULL
            )
            """);
    }

    public P202TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<P202TestDbContext>()
            .UseSqlServer(ConnectionString)
            .EnableDetailedErrors()
            .Options;

        return new P202TestDbContext(options);
    }

    private RoadGuardDbContext CreateProductionDbContext()
    {
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new RoadGuardDbContext(options);
    }

    public Task DisposeAsync() => _database.DisposeAsync();
}
