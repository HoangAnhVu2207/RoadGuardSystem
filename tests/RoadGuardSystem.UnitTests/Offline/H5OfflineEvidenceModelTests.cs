using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineEvidenceModelTests
{
    [Fact]
    public void CapturedEvidenceSeparatesOriginalActorFromActualReceiverUploader()
    {
        var actor=Guid.NewGuid(); var receiver=Guid.NewGuid(); var file=Guid.NewGuid(); var capture=Guid.NewGuid();
        var row=OfflineEvidenceCaptureReference.Bind(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),
            capture,actor,receiver,Guid.NewGuid(),file,Guid.NewGuid(),"MEASUREMENT",new string('a',64),"image/jpeg",null,"{}",DateTimeOffset.UtcNow);
        Assert.Equal(actor,row.OriginalActorId); Assert.Equal(receiver,row.ActualUploaderId); Assert.Equal(file,row.FileId);
        Assert.Equal(capture,row.CaptureOriginId); Assert.Null(row.DeclaredCapturedAt);
    }
    [Theory]
    [InlineData("REPORT_PHOTO")]
    [InlineData("DOCUMENT")]
    public void OfflineCaptureCannotPromotePrivateOrUnrelatedPurpose(string purpose)
    {
        Assert.Throws<ArgumentException>(()=>OfflineEvidenceCaptureReference.Bind(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),
            Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),purpose,new string('a',64),"image/jpeg",null,"{}",DateTimeOffset.UtcNow));
    }
    [Fact]
    public void GrantRevocationPreservesActualRevokerAndReasonWithoutExtendingValidity()
    {
        var grant=Guid.NewGuid();var actor=Guid.NewGuid();var at=DateTimeOffset.UtcNow;
        var row=OfflineHandoverGrantRevocation.Record(Guid.NewGuid(),Guid.NewGuid(),grant,actor,"handover revoked",at);
        Assert.Equal(grant,row.GrantId);Assert.Equal(actor,row.RevokedBy);Assert.Equal(at,row.RevokedAt);
    }
    [Fact]
    public void EncryptedPackageRootRetainsOnlyCipherAndSignedPublicManifest()
    {
        var json="{\"ciphertext\":\"sealed fixture\"}";var payloadHash=new string('a',64);
        var row=OfflineEncryptedPackageRecord.Capture(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),
            json,"{}",Convert.ToBase64String(new byte[64]),payloadHash,new string('b',64),DateTimeOffset.UtcNow);
        Assert.Equal(json,row.CipherPackageJson);Assert.Equal(payloadHash,row.PayloadHash);
        Assert.DoesNotContain(typeof(OfflineEncryptedPackageRecord).GetProperties(),x=>x.Name.Contains("Private",StringComparison.Ordinal) || x.Name.Contains("Decrypted",StringComparison.Ordinal));
    }
    [Fact]
    public void AssociatedPackageFileKeepsItsOwnContentVersionSeparateFromPlaintextHash()
    {
        var file=Guid.NewGuid();var hash=new string('c',64);
        var row=OfflinePackageFileReference.Capture(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),file,hash,"{\"purpose\":\"DOCUMENT\"}");
        Assert.Equal(file,row.FileId);Assert.Equal(hash,row.ContentChecksum);
    }
}
