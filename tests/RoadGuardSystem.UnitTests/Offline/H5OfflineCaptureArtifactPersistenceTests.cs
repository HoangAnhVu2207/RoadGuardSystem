using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineCaptureArtifactPersistenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetainedArtifactRequiresExactParentAndRegisteredSourceDevice(bool wrongDevice)
    {
        using var sender = OfflineDeviceKeys.Generate(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        using var recipient = OfflineDeviceKeys.Generate(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        var at = new DateTimeOffset(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
        var project = Guid.NewGuid(); var capture = Guid.NewGuid(); var artifact = Guid.NewGuid();
        var source = OfflineDeviceRegistration.Register(Guid.NewGuid(), project, sender.PublicKeys.ActorId,
            wrongDevice ? Guid.NewGuid() : Guid.Parse(sender.PublicKeys.DeviceId), 1, UserRoleCode.RepairCrew,
            sender.PublicKeys.EncryptionPublicKey, sender.PublicKeys.SigningPublicKey, at);
        var bytes = Encoding.UTF8.GetBytes("synthetic-photo-byte-fixture");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var chunk = OfflineHandoverCrypto.Seal(OfflinePackageHeader.Create(artifact, project,
            sender.PublicKeys.ActorId, sender.PublicKeys.DeviceId, [capture], hash), bytes, sender, [recipient.PublicKeys]);
        var parent = OfflineEncryptedPackageRecord.Capture(Guid.NewGuid(), project, sender.PublicKeys.ActorId,
            source.Id, Guid.NewGuid(), "{}", "{}", Convert.ToBase64String(new byte[64]), hash,
            new string('a', 64), at);
        var manifest = new OfflineCaptureManifest(1, parent.Id, parent.ManifestHash, project, parent.OriginalActorId,
            source.Id, capture, Guid.NewGuid(), "MEASUREMENT", "image/jpeg", bytes.Length, hash,
            [new(artifact, 0, 0, bytes.Length, hash, OfflineCryptoFormat.PackageContentFingerprint(chunk))]);
        var signature = OfflinePackageAuthentication.SignClaim(OfflineCaptureArtifacts.CanonicalManifest(manifest), sender);
        if (wrongDevice)
        {
            Assert.Throws<ArgumentException>(() => OfflineEncryptedCaptureArtifact.Register(parent, source, manifest,
                signature, 0, chunk, recipient.PublicKeys.ActorId, at));
            return;
        }
        var row = OfflineEncryptedCaptureArtifact.Register(parent, source, manifest, signature, 0, chunk,
            recipient.PublicKeys.ActorId, at);
        Assert.Equal(artifact, row.Id); Assert.Equal(parent.Id, row.ParentPackageId);
        Assert.Equal(recipient.PublicKeys.ActorId, row.RegisteredBy);
        Assert.Equal(source.ActorId, row.OriginalActorId);
        Assert.DoesNotContain(Encoding.UTF8.GetString(bytes), row.CipherEnvelopeJson, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => OfflineEncryptedCaptureArtifact.Register(parent, source,
            manifest with { ParentManifestHash = new string('f', 64) }, signature, 0, chunk, recipient.PublicKeys.ActorId, at));
    }
}
