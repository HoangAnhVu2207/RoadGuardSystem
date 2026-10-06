using System.Security.Cryptography;
using RoadGuardSystem.Services.Authentication;
using RoadGuardSystem.Services.Options;
using Xunit;
namespace RoadGuardSystem.UnitTests.Authentication;
public sealed class H1CredentialProtectionTests
{
    [Fact]
    public void EncryptedRetry_KeyIdsWithDots_RetainedKeyRotationAndTamper()
    {
        var ring = new Dictionary<string,string> { ["prod.2026"] = Convert.ToBase64String(Enumerable.Repeat((byte)31,32).ToArray()),
            ["next.2027"] = Convert.ToBase64String(Enumerable.Repeat((byte)57,32).ToArray()) };
        var options = new JwtOptions { ActiveKeyId="prod.2026", SigningKeys=ring };
        var protector = new RefreshCredentialProtection(options);
        var protectedValue=protector.Protect("cryptographic-refresh-credential");
        Assert.DoesNotContain("cryptographic-refresh-credential",protectedValue);
        options.ActiveKeyId="next.2027";
        Assert.Equal("cryptographic-refresh-credential",protector.Unprotect(protectedValue));
        var split=protectedValue.IndexOf('.');var cipher=Convert.FromBase64String(protectedValue[(split+1)..]); cipher[^1]^=1;
        Assert.ThrowsAny<CryptographicException>(()=>protector.Unprotect(protectedValue[..(split+1)]+Convert.ToBase64String(cipher)));
        ring.Remove("prod.2026");
        Assert.Throws<KeyNotFoundException>(()=>protector.Unprotect(protectedValue));
    }
}
