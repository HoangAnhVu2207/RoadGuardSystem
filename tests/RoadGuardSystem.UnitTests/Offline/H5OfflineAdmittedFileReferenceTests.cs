using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineAdmittedFileReferenceTests
{
    [Fact]
    public void ActualFileOwnerAndUploaderAreRetainedWithoutImpersonatingOriginalActor()
    {
        var original = Guid.NewGuid(); var receiver = Guid.NewGuid(); var owner = Guid.NewGuid();
        var row = Capture(original, receiver, owner, owner, new string('a', 64));
        Assert.Equal(original, row.OriginalActorId); Assert.Equal(receiver, row.CurrentImporterId);
        Assert.Equal(owner, row.ActualFileOwnerId); Assert.Equal(owner, row.ActualUploadedById);
        Assert.Equal(new string('a', 64), row.ContentChecksum);
    }

    [Fact]
    public void MissingHistoricalUploaderRemainsUnknown()
    {
        var row = Capture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, new string('a', 64));
        Assert.Null(row.ActualUploadedById);
    }

    [Fact]
    public void ChangedActualFileChecksumCannotMasqueradeAsTheSignedSourceVersion()
        => Assert.Throws<ArgumentException>(() => Capture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), new string('b', 64)));

    private static OfflineAdmittedFileReference Capture(Guid original, Guid importer, Guid owner, Guid? uploader,
        string actualChecksum)
        => OfflineAdmittedFileReference.Capture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), original, importer, owner, uploader, "MEASUREMENT",
            new string('a', 64), actualChecksum, "{\"uploadState\":\"VERIFIED\"}", DateTimeOffset.UtcNow);
}
