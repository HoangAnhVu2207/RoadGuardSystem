using System.Security.Cryptography;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineIdentityTests
{
    [Fact]
    public void DeviceRegistrationCapturesPublicKeysAndOriginalRoleWithoutPrivateRecoveryKeys()
    {
        using var keys = OfflineDeviceKeys.Generate(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        var row = OfflineDeviceRegistration.Register(Guid.NewGuid(), Guid.NewGuid(), keys.PublicKeys.ActorId,
            Guid.Parse(keys.PublicKeys.DeviceId), 1, UserRoleCode.RepairCrew, keys.PublicKeys.EncryptionPublicKey, keys.PublicKeys.SigningPublicKey, DateTimeOffset.UtcNow);
        Assert.Equal(keys.PublicKeys.SigningPublicKey, row.SigningPublicKey); Assert.Equal(UserRoleCode.RepairCrew, row.RoleSnapshot);
        Assert.Equal(64, row.KeyFingerprint.Length); Assert.DoesNotContain(typeof(OfflineDeviceRegistration).GetProperties(), x => x.Name.Contains("Private", StringComparison.Ordinal));
    }
    [Fact]
    public void DeviceRegistrationRejectsOneKeyReusedForEncryptionAndSigning()
    {
        using var keys = OfflineDeviceKeys.Generate(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        Assert.Throws<ArgumentException>(() => OfflineDeviceRegistration.Register(Guid.NewGuid(), Guid.NewGuid(), keys.PublicKeys.ActorId,
            Guid.Parse(keys.PublicKeys.DeviceId), 1, UserRoleCode.RepairCrew, keys.PublicKeys.SigningPublicKey, keys.PublicKeys.SigningPublicKey, DateTimeOffset.UtcNow));
    }
    [Fact]
    public void SnapshotHashBindsExactImmutablePinnedPayloadAndCurrentAssignment()
    {
        var json = "{\"taskMode\":\"MEASURE_ONLY\",\"routeVersionId\":\"11111111-1111-1111-1111-111111111111\"}";
        var assignment = Guid.NewGuid(); var row = OfflineTaskSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), assignment,
            Guid.NewGuid(), Guid.NewGuid(), Convert.ToBase64String(new byte[8]), new string('a', 64), json, DateTimeOffset.UtcNow);
        Assert.Equal(assignment, row.AssignmentId); Assert.Equal(json, row.SnapshotJson);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))).ToLowerInvariant(), row.ContentHash);
    }
    [Fact]
    public void OriginBindingRetainsOriginalActorEffectAndSeparateEnvelopeAndCoreHashes()
    {
        var origin = Guid.NewGuid(); var actor = Guid.NewGuid(); var row = OfflineOperationBinding.Bind(Guid.NewGuid(), Guid.NewGuid(), origin, origin, "FIELD_START", new string('a', 64), new string('b', 64), actor, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}", DateTimeOffset.UtcNow);
        Assert.Equal(origin, row.EffectId); Assert.Equal(actor, row.OriginalActorId); Assert.NotEqual(row.CorePayloadHash, row.EnvelopeHash);
    }
}
