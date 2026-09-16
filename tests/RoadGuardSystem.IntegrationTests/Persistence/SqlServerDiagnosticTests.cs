using FluentAssertions;
using RoadGuardSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Persistence;

[Trait("TaskId", "P2-00")]
public sealed class SqlServerDiagnosticTests
{
    [Fact(DisplayName = "Negative: Unreachable SQL Server instance returns false on connection probe without throwing false-green")]
    public async Task Unreachable_SqlServer_Connection_Probe_Fails_Fast()
    {
        // ARRANGE: An unreachable server host with very short timeout
        const string unreachableConnStr = "Server=127.0.0.1,59999;Database=master;User Id=sa;Password=FakePassword123!;Connect Timeout=1;TrustServerCertificate=True";

        // ACT
        var canConnect = await SqlServerTestFixture.CanConnectAsync(unreachableConnStr);

        // ASSERT: Probe must return false, never claiming connection succeeded
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

    [Fact(DisplayName = "Negative: Configured env var when unreachable fails immediately without fallback")]
    public async Task Configured_EnvVar_When_Unreachable_FailsFast_Without_Fallback()
    {
        var originalEnv = Environment.GetEnvironmentVariable("ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING");
        try
        {
            // Set invalid/unreachable connection string
            Environment.SetEnvironmentVariable(
                "ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING",
                "Server=127.0.0.1,59998;Database=master;User Id=sa;Password=FakePassword123!;Connect Timeout=1;TrustServerCertificate=True");

            var fixture = new SqlServerTestFixture();
            var act = () => fixture.InitializeAsync();

            var ex = await act.Should().ThrowAsync<SqlTestEnvironmentUnavailableException>(
                "unreachable ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING must fail fast and not fallback to local SQL or Docker");
            ex.WithMessage("*ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING*refusing fallback*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING", originalEnv);
        }
    }

    [Fact(DisplayName = "Negative: Cleanup failure is observed and not swallowed")]
    public async Task Cleanup_Failure_Is_Observed_And_Not_Swallowed()
    {
        var fixture = new SqlServerTestFixture();
        await fixture.InitializeAsync();

        // Simulate corrupted master connection string prior to dispose
        fixture.CorruptMasterConnectionStringForTesting("Server=127.0.0.1,59997;Database=master;Connect Timeout=1;TrustServerCertificate=True");

        var act = () => fixture.DisposeAsync();

        await act.Should().ThrowAsync<Exception>("cleanup failure must not be swallowed; test runner must observe teardown errors");
    }
}
