using FluentAssertions;
using Xunit;

namespace RoadGuardSystem.ApiTests.Infrastructure;

public sealed class Rf06IsolationTests
{
    [Fact]
    public void ApiHost_RejectsConnectionNotIssuedByLiveFixture()
    {
        var connection = "Server=127.0.0.1,1;Database=RoadGuard_ApiTest_unowned;Integrated Security=true";

        var create = () => new AuthenticationWebApplicationFactory(connection);

        create.Should().Throw<InvalidOperationException>()
            .WithMessage("*not owned by an active SQL fixture*");
    }
}
