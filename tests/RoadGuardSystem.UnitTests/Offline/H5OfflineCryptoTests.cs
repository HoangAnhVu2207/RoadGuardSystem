using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Offline;
using RoadGuardSystem.DTOs.Offline;
using Xunit;

namespace RoadGuardSystem.UnitTests.Offline;

[Trait("TaskId", "H5")]
public sealed class H5OfflineCryptoTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("{\"operation\":\"sampleOnly\",\"claimedStartedAt\":\"2099-01-01T00:00:00Z\"}");

    [Fact]
    public void RecipientRoundtripUsesIndependentEncryptionAndSigningKeys()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        sender.PublicKeys.EncryptionPublicKey.Should().NotBe(sender.PublicKeys.SigningPublicKey);
        var package = Seal(sender, recipient);
        using var opened = OfflineHandoverCrypto.Open(package, recipient, sender.PublicKeys.SigningPublicKey);
        opened.Plaintext.ToArray().Should().Equal(Plaintext);
        package.Ciphertext.Should().NotBe(Convert.ToBase64String(Plaintext));
        Convert.FromBase64String(package.PayloadNonce).Should().HaveCount(12);
        Convert.FromBase64String(package.PayloadTag).Should().HaveCount(16);
        Convert.FromBase64String(package.Signature).Should().HaveCount(64);
    }

    [Fact]
    public void MultipleRecipientsReceiveSamePayloadWithoutSharingPrivateKeys()
    {
        using var sender = Keys("sender"); using var first = Keys("first"); using var second = Keys("second");
        var package = OfflineHandoverCrypto.Seal(Header(sender), Plaintext, sender, [first.PublicKeys, second.PublicKeys]);
        using var firstOpen = OfflineHandoverCrypto.Open(package, first, sender.PublicKeys.SigningPublicKey);
        using var secondOpen = OfflineHandoverCrypto.Open(package, second, sender.PublicKeys.SigningPublicKey);
        firstOpen.Plaintext.ToArray().Should().Equal(secondOpen.Plaintext.ToArray());
        package.Recipients.Select(x => x.WrappedKey).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public void EachSealGeneratesFreshContentNonceAndEphemeralRecipientKey()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient"); var header = Header(sender);
        var first = OfflineHandoverCrypto.Seal(header, Plaintext, sender, [recipient.PublicKeys]);
        var second = OfflineHandoverCrypto.Seal(header, Plaintext, sender, [recipient.PublicKeys]);
        first.PayloadNonce.Should().NotBe(second.PayloadNonce);
        first.Ciphertext.Should().NotBe(second.Ciphertext);
        first.Recipients[0].EphemeralPublicKey.Should().NotBe(second.Recipients[0].EphemeralPublicKey);
    }

    [Fact]
    public void DifferentDeviceCannotRecoverLostRecipientKeys()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        var package = Seal(sender, recipient);
        using var replacement = OfflineDeviceKeys.Generate(recipient.PublicKeys.ActorId, recipient.PublicKeys.DeviceId);
        Action act = () => OfflineHandoverCrypto.Open(package, replacement, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<OfflineKeyUnavailableException>();
    }

    [Fact]
    public void CiphertextMutationIsRejected()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        var package = Seal(sender, recipient); var corrupt = package with { Ciphertext = Flip(package.Ciphertext) };
        Action act = () => OfflineHandoverCrypto.Open(corrupt, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void ProjectMutationIsRejectedBeforeDecryption()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        var package = Seal(sender, recipient);
        var changed = package with { Header = package.Header with { ProjectId = Guid.NewGuid() } };
        Action act = () => OfflineHandoverCrypto.Open(changed, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void OriginMutationIsRejectedBeforeDecryption()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient"); var package = Seal(sender, recipient);
        var changed = package with { Header = package.Header with { OriginIds = [Guid.NewGuid()] } };
        Action act = () => OfflineHandoverCrypto.Open(changed, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void RecipientWrapMutationIsRejected()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient"); var package = Seal(sender, recipient);
        var changed = package with { Recipients = [package.Recipients[0] with { WrappedKey = Flip(package.Recipients[0].WrappedKey) }] };
        Action act = () => OfflineHandoverCrypto.Open(changed, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void PackageEmbeddedSignerCannotReplaceTrustedSenderKey()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient"); using var imposter = Keys("imposter");
        var package = Seal(imposter, recipient);
        Action act = () => OfflineHandoverCrypto.Open(package, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void HeaderCanonicalizationIsStableAcrossOriginInputOrder()
    {
        var actor = Guid.NewGuid(); var project = Guid.NewGuid(); var package = Guid.NewGuid();
        var first = Guid.NewGuid(); var second = Guid.NewGuid(); var hash = Hash(Plaintext);
        var a = OfflinePackageHeader.Create(package, project, actor, "device", [first, second], hash);
        var b = OfflinePackageHeader.Create(package, project, actor, "device", [second, first], hash);
        OfflineCryptoFormat.CanonicalHeader(a).Should().Equal(OfflineCryptoFormat.CanonicalHeader(b));
    }

    [Fact]
    public void HeaderRejectsDuplicateOrigins()
    {
        var origin = Guid.NewGuid();
        Action act = () => OfflinePackageHeader.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "device", [origin, origin], Hash(Plaintext));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HeaderRejectsUnboundedOriginSet()
    {
        var origins = Enumerable.Range(0, OfflineCryptoFormat.MaxOrigins + 1).Select(_ => Guid.NewGuid()).ToArray();
        Action act = () => OfflinePackageHeader.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "device", origins, Hash(Plaintext));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SealRejectsHashThatDoesNotDescribeActualPayload()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        Action act = () => OfflineHandoverCrypto.Seal(Header(sender) with { PayloadSha256 = new string('0', 64) }, Plaintext, sender, [recipient.PublicKeys]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UnsupportedVersionDoesNotFallBackToLegacyCrypto()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        Action act = () => OfflineHandoverCrypto.Seal(Header(sender) with { Version = 2 }, Plaintext, sender, [recipient.PublicKeys]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SealRejectsDuplicateRecipientKey()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        Action act = () => OfflineHandoverCrypto.Seal(Header(sender), Plaintext, sender, [recipient.PublicKeys, recipient.PublicKeys]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void P384RecipientIsRejectedInsteadOfSilentlyChangingSuite()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        using var wrongCurve = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
        var wrong = recipient.PublicKeys with { EncryptionPublicKey = Convert.ToBase64String(wrongCurve.ExportSubjectPublicKeyInfo()) };
        Action act = () => OfflineHandoverCrypto.Seal(Header(sender), Plaintext, sender, [wrong]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SigningAndEncryptionCannotReuseOneKeyPair()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        var shared = recipient.PublicKeys with { SigningPublicKey = recipient.PublicKeys.EncryptionPublicKey };
        Action act = () => OfflineHandoverCrypto.Seal(Header(sender), Plaintext, sender, [shared]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvalidNonceLengthIsRejectedBeforeCryptographicUse()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        var package = Seal(sender, recipient) with { PayloadNonce = Convert.ToBase64String(new byte[11]) };
        Action act = () => OfflineHandoverCrypto.Open(package, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NoncanonicalBase64IsRejected()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        var package = Seal(sender, recipient); package = package with { PayloadNonce = " " + package.PayloadNonce };
        Action act = () => OfflineHandoverCrypto.Open(package, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AuthenticatedPayloadStillDoesNotProveTimeEvidenceOrPermission()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        using var opened = OfflineHandoverCrypto.Open(Seal(sender, recipient), recipient, sender.PublicKeys.SigningPublicKey);
        opened.IntegrityVerified.Should().BeTrue(); opened.TimeEstablished.Should().BeFalse();
        opened.EvidenceVerified.Should().BeFalse(); opened.BusinessAuthorized.Should().BeFalse();
    }

    [Fact]
    public void DisposedRecipientDoesNotRecoverFromPublicMetadata()
    {
        using var sender = Keys("sender"); var recipient = Keys("recipient"); var package = Seal(sender, recipient); recipient.Dispose();
        Action act = () => OfflineHandoverCrypto.Open(package, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void FiniteWireDtoRoundtripPreservesAuthenticatedPackageWithoutPlaintextOrPrivateKeys()
    {
        using var sender = Keys(Guid.NewGuid().ToString("D")); using var recipient = Keys(Guid.NewGuid().ToString("D")); var package = Seal(sender, recipient);
        var json = JsonSerializer.Serialize(H5EncryptedPackageDto.FromDomain(package), Json);
        json.Should().NotContain("privateKey").And.NotContain("claimedStartedAt").And.NotContain("sampleOnly");
        var decoded = JsonSerializer.Deserialize<H5EncryptedPackageDto>(json, Json)!;
        using var payload = OfflineHandoverCrypto.Open(decoded.ToDomain(), recipient, sender.PublicKeys.SigningPublicKey);
        payload.Plaintext.ToArray().Should().Equal(Plaintext);
    }

    [Fact]
    public void FiniteWireDtoRejectsUnknownMetadataRatherThanSilentlyDroppingIt()
    {
        using var sender = Keys(Guid.NewGuid().ToString("D")); using var recipient = Keys(Guid.NewGuid().ToString("D"));
        var json = JsonSerializer.Serialize(H5EncryptedPackageDto.FromDomain(Seal(sender, recipient)), Json);
        var injected = json[..^1] + ",\"serverEscrowKey\":\"unsupported\"}";
        Action act = () => JsonSerializer.Deserialize<H5EncryptedPackageDto>(injected, Json);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void WireConversionDoesNotInventDeviceIdentityFromOpaqueInternalText()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        Action act = () => H5EncryptedPackageDto.FromDomain(Seal(sender, recipient));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WireConversionRejectsUppercaseGuidInsteadOfNormalizingSignedMetadata()
    {
        const string device = "a1111111-1111-1111-1111-111111111111";
        using var sender = Keys(device); using var recipient = Keys(Guid.NewGuid().ToString("D"));
        var dto = H5EncryptedPackageDto.FromDomain(Seal(sender, recipient));
        Action act = () => (dto with { Header = dto.Header with { SourceDeviceId = device.ToUpperInvariant() } }).ToDomain();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WireConversionRejectsEmptyGuidDeviceInsteadOfTreatingItAsRegistered()
    {
        using var sender = Keys(Guid.Empty.ToString("D")); using var recipient = Keys(Guid.NewGuid().ToString("D"));
        Action act = () => H5EncryptedPackageDto.FromDomain(Seal(sender, recipient));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ContentFingerprintExcludesSignatureButDoesNotAuthenticateItsReplacement()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient"); var package = Seal(sender, recipient);
        var alteredSignature = package with { Signature = Flip(package.Signature) };
        OfflineCryptoFormat.PackageContentFingerprint(package).Should().Be(OfflineCryptoFormat.PackageContentFingerprint(alteredSignature));
        Action act = () => OfflineHandoverCrypto.Open(alteredSignature, recipient, sender.PublicKeys.SigningPublicKey);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void ContentFingerprintBindsImmutableProjectFacts()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient"); var package = Seal(sender, recipient);
        var changed = package with { Header = package.Header with { ProjectId = Guid.NewGuid() } };
        OfflineCryptoFormat.PackageContentFingerprint(package).Should().NotBe(OfflineCryptoFormat.PackageContentFingerprint(changed));
    }

    [Fact]
    public void CanonicalSignatureTranscriptUsesFixedOrderAndLiteralBase64Ascii()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        var package = Seal(sender, recipient) with { Ciphertext = "+///" }; var wrap = package.Recipients[0];
        var expected = "{\"header\":" + Encoding.UTF8.GetString(OfflineCryptoFormat.CanonicalHeader(package.Header))
            + ",\"payloadNonce\":\"" + package.PayloadNonce + "\",\"ciphertext\":\"+///\",\"payloadTag\":\"" + package.PayloadTag
            + "\",\"recipients\":[{\"actorId\":\"" + wrap.ActorId.ToString("D") + "\",\"deviceId\":\"" + wrap.DeviceId
            + "\",\"recipientKeyFingerprint\":\"" + wrap.RecipientKeyFingerprint + "\",\"ephemeralPublicKey\":\"" + wrap.EphemeralPublicKey
            + "\",\"nonce\":\"" + wrap.Nonce + "\",\"wrappedKey\":\"" + wrap.WrappedKey + "\",\"tag\":\"" + wrap.Tag
            + "\"}],\"senderSigningPublicKey\":\"" + package.SenderSigningPublicKey + "\"}";
        OfflineCryptoFormat.PackageContentFingerprint(package).Should().Be(Hash(Encoding.UTF8.GetBytes(expected)));
    }

    [Fact]
    public void DisposingDecryptedPayloadClearsOwnedMemoryAndRejectsLaterAccess()
    {
        using var sender = Keys("sender"); using var recipient = Keys("recipient");
        var opened = OfflineHandoverCrypto.Open(Seal(sender, recipient), recipient, sender.PublicKeys.SigningPublicKey);
        var retainedView = opened.Plaintext; opened.Dispose();
        retainedView.ToArray().Should().OnlyContain(x => x == 0);
        Action act = () => _ = opened.Plaintext;
        act.Should().Throw<ObjectDisposedException>();
    }

    private static OfflineDeviceKeys Keys(string device) => OfflineDeviceKeys.Generate(Guid.NewGuid(), device);
    private static OfflinePackageHeader Header(OfflineDeviceKeys sender) => OfflinePackageHeader.Create(Guid.NewGuid(), Guid.NewGuid(),
        sender.PublicKeys.ActorId, sender.PublicKeys.DeviceId, [Guid.NewGuid()], Hash(Plaintext));
    private static OfflineEncryptedPackage Seal(OfflineDeviceKeys sender, OfflineDeviceKeys recipient)
        => OfflineHandoverCrypto.Seal(Header(sender), Plaintext, sender, [recipient.PublicKeys]);
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string Flip(string value) { var bytes = Convert.FromBase64String(value); bytes[0] ^= 1; return Convert.ToBase64String(bytes); }
}
