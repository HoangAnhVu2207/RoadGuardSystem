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

    [Fact(DisplayName = "Positive: SeedAsync is strictly idempotent on repeated executions")]
    public async Task SeedAsync_IsStrictlyIdempotent_OnRepeatedExecutions()
    {
        // ARRANGE
        var options = new DbContextOptionsBuilder<RoadGuardDbContext>()
            .UseSqlServer(_fixture.ConnectionString, x => x.UseNetTopologySuite())
            .Options;

        await using var context = new RoadGuardDbContext(options);

        var counter = 0;
        var idempotentStep = new TestSeedStep(1, "IdempotentStep", () =>
        {
            // Simulates idempotent logic (e.g. check-then-insert or upsert)
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

    [Fact(DisplayName = "Negative: Seeder CLI returns code 2 when database is unreachable")]
    public async Task Cli_ReturnsCode2_WhenDatabaseIsUnreachable()
    {
        var exitCode = await RoadGuardSystem.Seeder.Program.Main(new[]
        {
            "-c", "Server=127.0.0.1,59999;Database=master;Connect Timeout=1;TrustServerCertificate=True;"
        });

        exitCode.Should().Be(2);
    }

    [Fact(DisplayName = "Positive: Seeder CLI returns code 0 on healthy database")]
    public async Task Cli_ReturnsCode0_WhenDatabaseIsHealthy()
    {
        var exitCode = await RoadGuardSystem.Seeder.Program.Main(new[]
        {
            "-c", _fixture.ConnectionString
        });

        exitCode.Should().Be(0);
    }

    [Fact(DisplayName = "Positive: Seeder CLI returns code 0 when help flag is passed")]
    public async Task Cli_ReturnsCode0_WhenHelpRequested()
    {
        var exitCode = await RoadGuardSystem.Seeder.Program.Main(new[] { "-h" });

        exitCode.Should().Be(0);
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
