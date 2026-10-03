using Xunit;

namespace RoadGuardSystem.IntegrationTests.Seeding;

public sealed class PostmanDisposableGuardTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("Server=.\\HANHNAV;Database=RoadGuardShared;Integrated Security=True")]
    [InlineData("Server=.\\OTHER;Database=RoadGuardPostmanTest;Integrated Security=True")]
    [InlineData("Server=.\\HANHNAV;Database=master;Integrated Security=True")]
    public async Task Recreate_RejectsMissingOrWrongConfiguredTarget_BeforeAnySqlConnection(string? connection)
    {
        var result = await RoadGuardSystem.Seeder.Program.RunAsync(["--postman-disposable", "--recreate"], _ => connection);
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task InvalidFlags_RejectBeforeReadingConnection()
    {
        var result = await RoadGuardSystem.Seeder.Program.RunAsync(["--postman-disposable", "--recreate", "--verify-only"],
            _ => throw new InvalidOperationException("Must not read connection"));
        Assert.Equal(1, result);
    }
}
