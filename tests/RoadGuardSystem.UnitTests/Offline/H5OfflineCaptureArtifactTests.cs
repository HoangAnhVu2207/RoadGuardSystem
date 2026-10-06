using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineCaptureArtifactTests
{
    [Fact]
    public void SignedManifestBindsParentCaptureAndExactContiguousChunks()
    {
        var manifest = Manifest();
        var bytes = OfflineCaptureArtifacts.CanonicalManifest(manifest);
        using var sender = Keys();
        var signature = OfflinePackageAuthentication.SignClaim(bytes, sender);
        OfflinePackageAuthentication.VerifyClaim(bytes, signature, sender.PublicKeys.SigningPublicKey);
        Assert.Throws<CryptographicException>(() => OfflinePackageAuthentication.VerifyClaim(
            OfflineCaptureArtifacts.CanonicalManifest(manifest with { ParentPackageId = Guid.NewGuid() }),
            signature, sender.PublicKeys.SigningPublicKey));
        Assert.Contains("capture", Encoding.UTF8.GetString(bytes), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("gap")]
    [InlineData("overlap")]
    [InlineData("order")]
    [InlineData("size")]
    [InlineData("duplicate")]
    [InlineData("private-purpose")]
    public void MalformedArtifactLayoutCannotBeSignedAsAValidCapture(string invalid)
    {
        var manifest = Manifest();
        manifest = invalid switch
        {
            "gap" => manifest with { Chunks = [manifest.Chunks[0], manifest.Chunks[1] with { Offset = 4 }] },
            "overlap" => manifest with { Chunks = [manifest.Chunks[0], manifest.Chunks[1] with { Offset = 2 }] },
            "order" => manifest with { Chunks = [manifest.Chunks[1], manifest.Chunks[0]] },
            "size" => manifest with { FileSize = 7 },
            "duplicate" => manifest with { Chunks = [manifest.Chunks[0], manifest.Chunks[1] with { ArtifactId = manifest.Chunks[0].ArtifactId }] },
            _ => manifest with { Purpose = "REPORT_PHOTO" }
        };
        Assert.Throws<ArgumentException>(() => OfflineCaptureArtifacts.CanonicalManifest(manifest));
    }

    [Fact]
    public void ArtifactEnvelopeMustMatchRegisteredSourceAndSignedChunkWithoutServerDecryption()
    {
        using var sender = Keys(); using var receiver = Keys(); using var stranger = Keys();
        var plaintext = Encoding.UTF8.GetBytes("photo bytes");
        var capture = Guid.NewGuid(); var project = Guid.NewGuid(); var artifact = Guid.NewGuid();
        var hash = Convert.ToHexString(SHA256.HashData(plaintext)).ToLowerInvariant();
        var package = OfflineHandoverCrypto.Seal(OfflinePackageHeader.Create(artifact, project,
            sender.PublicKeys.ActorId, sender.PublicKeys.DeviceId, [capture], hash), plaintext, sender, [receiver.PublicKeys]);
        var manifest = new OfflineCaptureManifest(1, Guid.NewGuid(), new string('a', 64), project,
            sender.PublicKeys.ActorId, Guid.NewGuid(), capture, Guid.NewGuid(), "MEASUREMENT", "image/jpeg",
            plaintext.Length, hash, [new(artifact, 0, 0, plaintext.Length, hash, OfflineCryptoFormat.PackageContentFingerprint(package))]);
        OfflineCaptureArtifacts.ValidateEnvelope(manifest, 0, package, sender.PublicKeys.SigningPublicKey);
        Assert.Throws<CryptographicException>(() => OfflineCaptureArtifacts.ValidateEnvelope(manifest, 0,
            package, stranger.PublicKeys.SigningPublicKey));
        Assert.Throws<ArgumentException>(() => OfflineCaptureArtifacts.ValidateEnvelope(manifest with
        { CaptureOriginId = Guid.NewGuid() }, 0, package, sender.PublicKeys.SigningPublicKey));
        using var recovered = OfflineHandoverCrypto.Open(package, receiver, sender.PublicKeys.SigningPublicKey);
        Assert.Equal(plaintext, recovered.Plaintext.ToArray());
    }

    private static OfflineDeviceKeys Keys() => OfflineDeviceKeys.Generate(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
    private static OfflineCaptureManifest Manifest() => new(1, Guid.NewGuid(), new string('a', 64),
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "BEFORE", "image/png", 6,
        new string('b', 64), [new(Guid.NewGuid(), 0, 0, 3, new string('c', 64), new string('d', 64)),
            new(Guid.NewGuid(), 1, 3, 3, new string('e', 64), new string('f', 64))]);
}
