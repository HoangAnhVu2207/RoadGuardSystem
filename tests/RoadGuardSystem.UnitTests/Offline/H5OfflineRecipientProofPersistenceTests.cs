using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineRecipientProofPersistenceTests
{
    private static readonly string Signature = Convert.ToBase64String(new byte[64]);

    [Fact]
    public void HandoverBatchRetainsActualRecipientRegistrationAndSignatureSeparatelyFromSource()
    {
        var recipient = Guid.NewGuid(); var row = Receive(true, recipient, Signature);
        Assert.Equal(recipient, row.RecipientDeviceRegistrationId); Assert.Equal(Signature, row.RecipientSignature);
        Assert.Equal(Signature, row.SourceSignature);
    }

    [Fact]
    public void ImportedBatchCannotOmitRecipientProof()
        => Assert.Throws<ArgumentException>(() => Receive(true, null, null));

    [Fact]
    public void MalformedRecipientSignatureCannotBeRetainedAsAnAdmittedProof()
        => Assert.Throws<ArgumentException>(() => Receive(true, Guid.NewGuid(), "not base64"));

    [Fact]
    public void SelfSyncCannotAcquireHandoverRecipientProofWithoutItsActualPackageAndGrant()
        => Assert.Throws<ArgumentException>(() => Receive(false, Guid.NewGuid(), Signature));

    [Fact]
    public void SelfSyncRetainsOnlyItsOriginalSourceSignature()
    {
        var row = Receive(false, null, null);
        Assert.Null(row.RecipientDeviceRegistrationId); Assert.Null(row.RecipientSignature);
        Assert.Equal(Signature, row.SourceSignature);
    }

    private static OfflineSyncBatch Receive(bool handover, Guid? registration, string? recipientSignature)
        => OfflineSyncBatch.Receive(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            handover ? Guid.NewGuid() : null, handover ? Guid.NewGuid() : null, "{}", Signature,
            DateTimeOffset.UtcNow, registration, recipientSignature);
}
