using System.Security.Cryptography;
using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineRecipientEndorsementTests
{
    [Theory]
    [InlineData("project")]
    [InlineData("package")]
    [InlineData("grant")]
    [InlineData("batch")]
    [InlineData("recipient")]
    [InlineData("payload")]
    public void RecipientSignatureCannotMoveToAnotherImportScope(string changedField)
    {
        using var recipient = OfflineDeviceKeys.Generate(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        var claim = new OfflineRecipientImportClaim(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), new string('a', 64));
        var signedBytes = OfflineRecipientEndorsement.CanonicalClaim(claim);
        var signature = OfflinePackageAuthentication.SignClaim(signedBytes, recipient);
        OfflinePackageAuthentication.VerifyClaim(signedBytes, signature, recipient.PublicKeys.SigningPublicKey);
        var changed = changedField switch
        {
            "project" => claim with { ProjectId = Guid.NewGuid() },
            "package" => claim with { PackageId = Guid.NewGuid() },
            "grant" => claim with { GrantId = Guid.NewGuid() },
            "batch" => claim with { ImportBatchId = Guid.NewGuid() },
            "recipient" => claim with { RecipientDeviceRegistrationId = Guid.NewGuid() },
            _ => claim with { AttachedSourcePayloadHash = new string('b', 64) }
        };
        Assert.Throws<CryptographicException>(() => OfflinePackageAuthentication.VerifyClaim(
            OfflineRecipientEndorsement.CanonicalClaim(changed), signature, recipient.PublicKeys.SigningPublicKey));
    }
}
