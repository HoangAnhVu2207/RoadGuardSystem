using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

public sealed class H5OfflineRuntimeDomainTests
{
    private static readonly DateTimeOffset Origin = new(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes("{\"kind\":\"FIELD_START\",\"timeState\":\"CLAIMED\"}");

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void SupervisorGrantHasExactIndependentTwentyFourHourBoundary(int seconds, bool allowed)
    {
        var grant = Grant(UserRoleCode.Supervisor);
        Assert.Equal(Origin.AddHours(24), grant.ExpiresAt);
        Assert.Equal(allowed, grant.AllowsNewAdmissionAt(grant.ExpiresAt.AddSeconds(seconds)));
    }
    [Fact]
    public void ProjectManagerCannotIssueDeviceDataHandoverGrant()
        => Assert.Throws<ArgumentException>(() => Grant(UserRoleCode.ProjectManager));
    [Fact]
    public void DataGrantDoesNotEncodeSessionOrExecutionPermission()
    {
        var grant = Grant(UserRoleCode.Supervisor);
        Assert.Equal(UserRoleCode.ProjectManager, grant.RecipientRole);
        Assert.Contains("FIELD_START", grant.ScopeJson);
        Assert.DoesNotContain("executionAuthorization", grant.ScopeJson, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void RegisteredSignatureAndAttachedHashDoNotClaimServerDecryptionTimeOrEvidenceProof()
    {
        using var source = Keys(); using var receiver = Keys();
        var package = Package(source, receiver);
        var result = OfflinePackageAuthentication.VerifyAttachedPayload(package, Payload, source.PublicKeys.SigningPublicKey);
        Assert.Equal("SIGNATURE_AND_ATTACHED_HASH_VERIFIED", result.AuthenticationState);
        Assert.False(result.ServerDecrypted); Assert.False(result.TimeVerified); Assert.False(result.EvidenceVerified);
    }
    [Fact]
    public void AttachedPlaintextMismatchCannotBeAdmittedEvenWithValidEncryptedSignature()
    {
        using var source = Keys(); using var receiver = Keys(); var package = Package(source, receiver);
        Assert.Throws<CryptographicException>(() => OfflinePackageAuthentication.VerifyAttachedPayload(package,
            Encoding.UTF8.GetBytes("changed"), source.PublicKeys.SigningPublicKey));
    }
    [Fact]
    public void SelfDeclaredSignerIsNotIndependentDeviceRegistration()
    {
        using var source = Keys(); using var receiver = Keys(); using var stranger = Keys(); var package = Package(source, receiver);
        Assert.Throws<CryptographicException>(() => OfflinePackageAuthentication.VerifyAttachedPayload(package,
            Payload, stranger.PublicKeys.SigningPublicKey));
    }
    [Fact]
    public void SignedDirectSyncClaimsBindExactPayloadAndSeparateRegisteredDeviceKey()
    {
        using var source = Keys(); using var stranger = Keys();
        var signature = OfflinePackageAuthentication.SignClaim(Payload, source);
        OfflinePackageAuthentication.VerifyClaim(Payload, signature, source.PublicKeys.SigningPublicKey);
        Assert.Throws<CryptographicException>(() => OfflinePackageAuthentication.VerifyClaim(Encoding.UTF8.GetBytes("changed"), signature, source.PublicKeys.SigningPublicKey));
        Assert.Throws<CryptographicException>(() => OfflinePackageAuthentication.VerifyClaim(Payload, signature, stranger.PublicKeys.SigningPublicKey));
    }
    private static OfflineDeviceKeys Keys() => OfflineDeviceKeys.Generate(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
    private static OfflineEncryptedPackage Package(OfflineDeviceKeys source, OfflineDeviceKeys receiver)
        => OfflineHandoverCrypto.Seal(OfflinePackageHeader.Create(Guid.NewGuid(), Guid.NewGuid(), source.PublicKeys.ActorId,
            source.PublicKeys.DeviceId, [Guid.NewGuid()], Convert.ToHexString(SHA256.HashData(Payload)).ToLowerInvariant()), Payload, source, [receiver.PublicKeys]);
    private static OfflineHandoverGrant Grant(UserRoleCode role)
        => OfflineHandoverGrant.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), UserRoleCode.ProjectManager, Guid.NewGuid(), role, Origin, new string('a', 64),
            "{\"actions\":[\"FIELD_START\"],\"taskIds\":[\"11111111-1111-1111-1111-111111111111\"],\"originIds\":[\"22222222-2222-2222-2222-222222222222\"]}", "lost-device data handover");
}
