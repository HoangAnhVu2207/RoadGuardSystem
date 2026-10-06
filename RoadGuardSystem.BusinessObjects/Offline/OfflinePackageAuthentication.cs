using System.Security.Cryptography;
using System.Text;

namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed record OfflinePackageAuthenticationResult(string AuthenticationState, string PayloadHash,
    bool ServerDecrypted, bool TimeVerified, bool EvidenceVerified);

public static class OfflinePackageAuthentication
{
    private static readonly byte[] ClaimPurpose = Encoding.UTF8.GetBytes("roadguard.h5.direct-sync.v1\0");
    public static OfflinePackageAuthenticationResult VerifyAttachedPayload(OfflineEncryptedPackage package,
        ReadOnlySpan<byte> attachedPayload, string independentlyRegisteredSigningPublicKey)
    {
        if (attachedPayload.Length is < 1 or > OfflineCryptoFormat.MaxPayloadBytes)
            throw new ArgumentException("Attached payload is outside its versioned bound.", nameof(attachedPayload));
        package = VerifyEncryptedSignature(package, independentlyRegisteredSigningPublicKey);
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(attachedPayload), OfflineCryptoFormat.HashBytes(package.Header.PayloadSha256)))
            throw new CryptographicException("Attached payload differs from the signed source declaration.");
        return new("SIGNATURE_AND_ATTACHED_HASH_VERIFIED", package.Header.PayloadSha256, false, false, false);
    }
    // This proves the registered source signed ciphertext and public claims, without decrypting it.
    public static OfflineEncryptedPackage VerifyEncryptedSignature(OfflineEncryptedPackage package,
        string independentlyRegisteredSigningPublicKey)
    {
        package = OfflineHandoverCrypto.SnapshotPackage(package);
        using var signer = OfflineCryptoFormat.ImportSigningKey(independentlyRegisteredSigningPublicKey);
        if (!CryptographicOperations.FixedTimeEquals(OfflineCryptoFormat.Decode(package.SenderSigningPublicKey, 128), signer.ExportSubjectPublicKeyInfo()) ||
            !signer.VerifyHash(SHA256.HashData(OfflineCryptoFormat.CanonicalUnsignedPackage(package)),
                OfflineCryptoFormat.Decode(package.Signature, 64, 64), DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            throw new CryptographicException("Package signature does not match the independently registered device.");
        return package;
    }
    public static string SignClaim(ReadOnlySpan<byte> canonicalClaim, OfflineDeviceKeys sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        return Convert.ToBase64String(sender.SignDigest(ClaimDigest(canonicalClaim)));
    }
    public static void VerifyClaim(ReadOnlySpan<byte> canonicalClaim, string signature, string independentlyRegisteredSigningPublicKey)
    {
        using var signer = OfflineCryptoFormat.ImportSigningKey(independentlyRegisteredSigningPublicKey);
        if (!signer.VerifyHash(ClaimDigest(canonicalClaim), OfflineCryptoFormat.Decode(signature, 64, 64), DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            throw new CryptographicException("Sync claims do not match the independently registered device.");
    }
    public static void ValidateRegistration(OfflineDevicePublicKeys keys) => OfflineCryptoFormat.ValidatePublicKeys(keys);
    private static byte[] ClaimDigest(ReadOnlySpan<byte> claim)
    {
        if (claim.Length is < 1 or > OfflineCryptoFormat.MaxPayloadBytes)
            throw new ArgumentException("Sync claim is outside its versioned bound.", nameof(claim));
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(ClaimPurpose); hash.AppendData(claim); return hash.GetHashAndReset();
    }
}
