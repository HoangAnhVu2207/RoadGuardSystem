using Microsoft.Data.SqlClient;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Infrastructure;

public sealed class ConfiguredSqlServerOwnershipTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _server;

    public ConfiguredSqlServerOwnershipTests(SqlServerTestFixture server) => _server = server;

    [Fact]
    public async Task ConfiguredServerCreatesDistinctDatabaseAndCannotDropAnotherFixturesDatabase()
    {
        var configured = SqlServerTestFixture.CreateWithEnvironmentAccessor(_ => _server.MasterConnectionString);
        await configured.InitializeAsync();
        var createdName = configured.DatabaseName;
        try
        {
            Assert.NotEqual(_server.DatabaseName, createdName);
            Assert.Equal("master", new SqlConnectionStringBuilder(configured.MasterConnectionString).InitialCatalog);
            await Assert.ThrowsAsync<InvalidOperationException>(() => configured.DropDatabaseByNameAsync(_server.DatabaseName));
            Assert.True(await _server.DatabaseExistsAsync(_server.DatabaseName));
            Assert.True(await _server.DatabaseExistsAsync(createdName));
        }
        finally
        {
            await configured.DisposeAsync();
        }

        Assert.False(await _server.DatabaseExistsAsync(createdName));
        Assert.True(await _server.DatabaseExistsAsync(_server.DatabaseName));
    }
}
