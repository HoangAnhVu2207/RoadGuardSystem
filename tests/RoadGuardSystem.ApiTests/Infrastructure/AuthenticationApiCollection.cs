using Xunit;

namespace RoadGuardSystem.ApiTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AuthenticationApiFixture : ICollectionFixture<AuthenticationSqlServerFixture>
{
    public const string Name = "Authentication API";
}
