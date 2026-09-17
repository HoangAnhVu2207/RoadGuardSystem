using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Seeding;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Seeding;

[Trait("TaskId", "P2-01")]
public sealed class SeederTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;

    public SeederTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "Negative: SeedAsync throws ArgumentNullException when DbContext is null")]
    public async Task SeedAsync_ThrowsArgumentNullException_WhenContextIsNull()
    {
        var seeder = new DatabaseSeeder(Array.Empty<ISeedStep>());

        var act = () => seeder.SeedAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Negative: SeedAsync fails fast with DatabaseNotReadyException when database is unreachable")]
    public async Task SeedAsync_FailsFast_WhenDatabaseIsUnreachable()
    {
        // ARRANGE: Point to a non-existent, unreachable SQL Server port
        var badOptions = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer("Server=127.0.0.1,59999;Database=master;Connect Timeout=1;TrustServerCertificate=True;")
            .Options;

        await using var badContext = new RoadGuardDbContext(badOptions);
        var seeder = new DatabaseSeeder(Array.Empty<ISeedStep>());

        // ACT
        var act = () => seeder.SeedAsync(badContext);

        // ASSERT: Must throw DatabaseNotReadyException without touching schema or attempting seed
        await act.Should().ThrowAsync<DatabaseNotReadyException>()
            .WithMessage("*readiness probe*");
    }

    [Fact(DisplayName = "Negative: SeedAsync throws OperationCanceledException when token is pre-canceled and does not execute steps")]
    public async Task SeedAsync_ThrowsOperationCanceledException_WhenTokenIsPreCanceled()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, x => x.UseNetTopologySuite())
            .Options;

        await using var context = new RoadGuardDbContext(options);

        var stepExecuted = false;
        var step = new TestSeedStep(1, "NeverRunStep", () => stepExecuted = true);
        var seeder = new DatabaseSeeder(new[] { step });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // ACT
        var act = () => seeder.SeedAsync(context, cts.Token);

        // ASSERT: Must throw OperationCanceledException (not DatabaseNotReadyException) and run zero steps
        await act.Should().ThrowAsync<OperationCanceledException>();
        stepExecuted.Should().BeFalse();
    }

    [Fact(DisplayName = "Positive: SeedAsync completes deterministically on healthy database")]
    public async Task SeedAsync_CompletesDeterministically_OnHealthyDatabase()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, x => x.UseNetTopologySuite())
            .Options;

        await using var context = new RoadGuardDbContext(options);
        var seeder = new DatabaseSeeder(Array.Empty<ISeedStep>());

        // ACT
        var result = await seeder.SeedAsync(context);

        // ASSERT
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StepsExecuted.Should().Be(0);
    }

    [Fact(DisplayName = "Positive: SeedAsync executes registered steps in strict numerical order")]
    public async Task SeedAsync_ExecutesSteps_InStrictOrder()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, x => x.UseNetTopologySuite())
            .Options;

        await using var context = new RoadGuardDbContext(options);

        var executionOrder = new List<string>();

        var step2 = new TestSeedStep(2, "StepTwo", () => executionOrder.Add("StepTwo"));
        var step1 = new TestSeedStep(1, "StepOne", () => executionOrder.Add("StepOne"));
        var step3 = new TestSeedStep(3, "StepThree", () => executionOrder.Add("StepThree"));

        // Register steps in disordered sequence (2, 1, 3)
        var seeder = new DatabaseSeeder(new[] { step2, step1, step3 });

        // ACT
        var result = await seeder.SeedAsync(context);

        // ASSERT: Execution must be strictly ordered by step.Order: 1, 2, 3
        result.Success.Should().BeTrue();
        result.StepsExecuted.Should().Be(3);
        result.ExecutedStepNames.Should().ContainInOrder("StepOne", "StepTwo", "StepThree");
        executionOrder.Should().ContainInOrder("StepOne", "StepTwo", "StepThree");
    }

    [Fact(DisplayName = "Positive: ISeedStep conforming to idempotency contract executes safely across repeated runs")]
    public async Task SeedStepContract_DemonstratesIdempotentStepExecution()
    {
        // ARRANGE: Idempotency is a contract invariant required of each ISeedStep implementation
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, x => x.UseNetTopologySuite())
            .Options;

        await using var context = new RoadGuardDbContext(options);

        var counter = 0;
        var idempotentStep = new TestSeedStep(1, "IdempotentStep", () =>
        {
            // Simulates idempotent logic implemented by a seed step (e.g. check-then-insert or upsert)
            if (counter == 0)
            {
                counter++;
            }
        });

        var seeder = new DatabaseSeeder(new[] { idempotentStep });

        // ACT: Run seed twice
        var run1 = await seeder.SeedAsync(context);
        var run2 = await seeder.SeedAsync(context);

        // ASSERT: Both runs succeed, and state counter remains 1
        run1.Success.Should().BeTrue();
        run2.Success.Should().BeTrue();
        counter.Should().Be(1);
    }

    [Fact(DisplayName = "Positive: Wave 0 no-op seed entry point runs repeatedly, executes 0 steps, and does not add or remove base tables")]
    public async Task Wave0_NoOpSeed_CanExecuteRepeatedly_WithoutMutatingBaseTables()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, x => x.UseNetTopologySuite())
            .Options;

        await using var context = new RoadGuardDbContext(options);
        var seeder = new DatabaseSeeder(Array.Empty<ISeedStep>());

        var tablesBefore = await GetDatabaseTableNamesAsync(context);

        // ACT: Run no-op seed twice consecutively on the same database
        var run1 = await seeder.SeedAsync(context);
        var run2 = await seeder.SeedAsync(context);

        var tablesAfter = await GetDatabaseTableNamesAsync(context);

        // ASSERT: Both runs succeed with 0 steps executed; verified evidence proves no base tables added or removed
        run1.Success.Should().BeTrue();
        run1.StepsExecuted.Should().Be(0);
        run2.Success.Should().BeTrue();
        run2.StepsExecuted.Should().Be(0);

        tablesAfter.Should().Equal(tablesBefore);
    }

    private static async Task<List<string>> GetDatabaseTableNamesAsync(DbContext context)
    {
        var tableNames = new List<string>();
        var conn = context.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync();
        }
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }
        return tableNames;
    }

    [Fact(DisplayName = "Negative: Seeder CLI returns code 1 when unsupported argument like -c is passed")]
    public async Task Cli_ReturnsCode1_WhenUnsupportedArgumentPassed()
    {
        var exitCodeDashC = await RoadGuardSystem.Seeder.Program.RunAsync(
            new[] { "-c", "Server=localhost;" },
            envLookup: _ => _fixture.ConnectionString);
        exitCodeDashC.Should().Be(1);

        var exitCodeLong = await RoadGuardSystem.Seeder.Program.RunAsync(
            new[] { "--connection-string", "Server=localhost;" },
            envLookup: _ => _fixture.ConnectionString);
        exitCodeLong.Should().Be(1);

        var exitCodeUnknown = await RoadGuardSystem.Seeder.Program.RunAsync(
            new[] { "--arbitrary-flag" },
            envLookup: _ => _fixture.ConnectionString);
        exitCodeUnknown.Should().Be(1);
    }

    [Fact(DisplayName = "Negative: Seeder CLI returns code 1 when empty or whitespace argument is passed")]
    public async Task Cli_ReturnsCode1_WhenEmptyOrWhitespaceArgumentPassed()
    {
        var exitCodeEmpty = await RoadGuardSystem.Seeder.Program.RunAsync(
            new[] { "" },
            envLookup: _ => _fixture.ConnectionString);
        exitCodeEmpty.Should().Be(1);

        var exitCodeWhitespace = await RoadGuardSystem.Seeder.Program.RunAsync(
            new[] { "   " },
            envLookup: _ => _fixture.ConnectionString);
        exitCodeWhitespace.Should().Be(1);
    }

    [Fact(DisplayName = "Negative: Seeder CLI returns code 1 when ROADGUARD_CONNECTION_STRING environment variable is missing or whitespace")]
    public async Task Cli_ReturnsCode1_WhenEnvironmentVariableIsMissing()
    {
        var exitCodeNull = await RoadGuardSystem.Seeder.Program.RunAsync(
            Array.Empty<string>(),
            envLookup: _ => null);
        exitCodeNull.Should().Be(1);

        var exitCodeWhitespace = await RoadGuardSystem.Seeder.Program.RunAsync(
            Array.Empty<string>(),
            envLookup: _ => "   ");
        exitCodeWhitespace.Should().Be(1);
    }

    [Fact(DisplayName = "Negative: Seeder CLI returns code 2 when database is unreachable via ROADGUARD_CONNECTION_STRING")]
    public async Task Cli_ReturnsCode2_WhenDatabaseIsUnreachable()
    {
        var exitCode = await RoadGuardSystem.Seeder.Program.RunAsync(
            Array.Empty<string>(),
            envLookup: key => key == "ROADGUARD_CONNECTION_STRING"
                ? "Server=127.0.0.1,59999;Database=master;Connect Timeout=1;TrustServerCertificate=True;"
                : null);

        exitCode.Should().Be(2);
    }

    [Fact(DisplayName = "Positive: Seeder CLI returns code 0 on healthy database via ROADGUARD_CONNECTION_STRING seam")]
    public async Task Cli_ReturnsCode0_WhenDatabaseIsHealthy()
    {
        var exitCode = await RoadGuardSystem.Seeder.Program.RunAsync(
            Array.Empty<string>(),
            envLookup: key => key == "ROADGUARD_CONNECTION_STRING" ? _fixture.ConnectionString : null);

        exitCode.Should().Be(0);
    }

    [Fact(DisplayName = "Positive: Seeder CLI returns code 0 when help flag is passed")]
    public async Task Cli_ReturnsCode0_WhenHelpRequested()
    {
        var exitCodeShort = await RoadGuardSystem.Seeder.Program.RunAsync(new[] { "-h" });
        exitCodeShort.Should().Be(0);

        var exitCodeLong = await RoadGuardSystem.Seeder.Program.RunAsync(new[] { "--help" });
        exitCodeLong.Should().Be(0);
    }

    private sealed class TestSeedStep : ISeedStep
    {
        private readonly Action _action;

        public TestSeedStep(int order, string name, Action action)
        {
            Order = order;
            Name = name;
            _action = action;
        }

        public int Order { get; }
        public string Name { get; }

        public Task SeedAsync(RoadGuardDbContext context, CancellationToken cancellationToken = default)
        {
            _action();
            return Task.CompletedTask;
        }
    }
}
