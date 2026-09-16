using FluentAssertions;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-00")]
public sealed class SqlServerDiagnosticTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;

    public SqlServerDiagnosticTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }
    [Fact(DisplayName = "Negative: Unreachable SQL Server instance returns false on connection probe without throwing false-green")]
    public async Task Unreachable_SqlServer_Connection_Probe_Fails_Fast()
    {
        const string unreachableConnStr = "Server=127.0.0.1,59999;Database=master;User Id=sa;Password=FakePassword123!;Connect Timeout=1;TrustServerCertificate=True";

        var canConnect = await SqlServerTestFixture.CanConnectAsync(unreachableConnStr);

        canConnect.Should().BeFalse("unreachable SQL server must report connection failure");
    }

    [Fact(DisplayName = "Negative: SqlTestEnvironmentUnavailableException contains actionable diagnostic message")]
    public void SqlTestEnvironmentUnavailableException_Contains_Diagnostic_Guidance()
    {
        var ex = new SqlTestEnvironmentUnavailableException(
            "SQL Server integration test environment is unavailable. Neither Docker/Testcontainers nor an accessible " +
            "local SQL Server instance could be reached. Set ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING or ensure " +
            "SQL Server / Docker is running before executing integration tests.");

        ex.Message.Should().Contain("SQL Server integration test environment is unavailable");
        ex.Message.Should().Contain("ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING",
            because: "the diagnostic exception message should guide developers on how to configure the connection string");
    }

    [Fact(DisplayName = "Negative: Configured env var when empty fails immediately without fallback")]
    public async Task Configured_EnvVar_When_Empty_FailsFast_Without_Fallback()
    {
        var fixture = new SqlServerTestFixture(_ => string.Empty);
        var act = () => fixture.InitializeAsync();

        var ex = await act.Should().ThrowAsync<SqlTestEnvironmentUnavailableException>(
            "empty connection string env var must fail fast without fallback");
        ex.WithMessage("*empty or whitespace*");
    }

    [Fact(DisplayName = "Negative: Configured env var when whitespace fails immediately without fallback")]
    public async Task Configured_EnvVar_When_Whitespace_FailsFast_Without_Fallback()
    {
        var fixture = new SqlServerTestFixture(_ => "   \t\n  ");
        var act = () => fixture.InitializeAsync();

        var ex = await act.Should().ThrowAsync<SqlTestEnvironmentUnavailableException>(
            "whitespace connection string env var must fail fast without fallback");
        ex.WithMessage("*empty or whitespace*");
    }

    [Fact(DisplayName = "Negative: Configured env var when malformed fails immediately without fallback")]
    public async Task Configured_EnvVar_When_Malformed_FailsFast_Without_Fallback()
    {
        var fixture = new SqlServerTestFixture(_ => "NotAValidConnectionString;;==123");
        var act = () => fixture.InitializeAsync();

        var ex = await act.Should().ThrowAsync<SqlTestEnvironmentUnavailableException>(
            "malformed connection string env var must fail fast without fallback");
        ex.WithMessage("*malformed*");
    }

    [Fact(DisplayName = "Negative: Configured env var when unreachable fails immediately without fallback")]
    public async Task Configured_EnvVar_When_Unreachable_FailsFast_Without_Fallback()
    {
        var fixture = new SqlServerTestFixture(
            _ => "Server=127.0.0.1,59998;Database=master;User Id=sa;Password=FakePassword123!;Connect Timeout=1;TrustServerCertificate=True");
        var act = () => fixture.InitializeAsync();

        var ex = await act.Should().ThrowAsync<SqlTestEnvironmentUnavailableException>(
            "unreachable connection string env var must fail fast without fallback");
        ex.WithMessage("*could not connect to SQL Server*refusing fallback*");
    }

    [Fact(DisplayName = "Negative: Cleanup failure is observed, not swallowed, and does not leak test database")]
    public async Task Cleanup_Failure_Is_Observed_And_Does_Not_Leak_Database()
    {
        var tempFixture = new SqlServerTestFixture(masterConnectionString: _fixture.MasterConnectionString);
        await tempFixture.InitializeAsync();
        var dbName = tempFixture.DatabaseName;

        try
        {
            // Simulate drop failure on fixture
            tempFixture.SimulateDropFailure = true;
            var act = () => tempFixture.DisposeAsync();

            await act.Should().ThrowAsync<InvalidOperationException>(
                "cleanup failure must be observed and not swallowed");
        }
        finally
        {
            // Clean up cleanly so no database leaks
            tempFixture.SimulateDropFailure = false;
            await tempFixture.DropDatabaseAsync();

            var exists = await _fixture.DatabaseExistsAsync(dbName);
            exists.Should().BeFalse("database must be dropped cleanly and never leaked");
        }
    }
}
