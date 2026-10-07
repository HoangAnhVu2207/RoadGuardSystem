using RoadGuardSystem.Services.Authentication;
using Xunit;
namespace RoadGuardSystem.UnitTests.Authentication;

public sealed partial class AuthServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("bad key")]
    public async Task Refresh_ExplicitInvalidOperationKey_FailsBeforeCredentialLookup(string key)
    {
        var result = await CreateService(new StubIdentityRepository(), new StubCredentialVerifier()).RefreshAsync(
            new RefreshCommand("supplied-refresh-material", OperationKey: key));
        Assert.Equal(AuthStatus.InvalidInput, result.Status);
    }
}
