using System.Security.Cryptography;

namespace RoadGuardSystem.BusinessObjects.Offline;

public sealed class OfflineKeyUnavailableException : CryptographicException
{
    public OfflineKeyUnavailableException() : base("The package cannot be decrypted with this device's keys.") { }
}

/// <summary>Authenticated decryption is not evidence readiness, time proof or business authorization.</summary>
public sealed class OfflineDecryptedPayload : IDisposable
{
    private readonly byte[] _plaintext;
    private bool _disposed;
    internal OfflineDecryptedPayload(byte[] plaintext) { _plaintext = plaintext; IntegrityVerified = true; }
    public ReadOnlyMemory<byte> Plaintext { get { ObjectDisposedException.ThrowIf(_disposed, this); return _plaintext; } }
    public bool IntegrityVerified { get; }
    public bool EvidenceVerified { get; }
    public bool TimeEstablished { get; }
    public bool BusinessAuthorized { get; }
    public void Dispose() { if (!_disposed) { CryptographicOperations.ZeroMemory(_plaintext); _disposed = true; } GC.SuppressFinalize(this); }
}

public static class OfflineHandoverCrypto
{
    public static OfflineEncryptedPackage Seal(OfflinePackageHeader header, ReadOnlySpan<byte> plaintext,
        OfflineDeviceKeys sender, IReadOnlyList<OfflineDevicePublicKeys> recipients)
    {
        ArgumentNullException.ThrowIfNull(sender); sender.EnsureAvailable();
        header = OfflineCryptoFormat.SnapshotHeader(header); ArgumentNullException.ThrowIfNull(recipients);
        if (plaintext.Length is < 1 or > OfflineCryptoFormat.MaxPayloadBytes) throw new ArgumentException("Payload exceeds the sealed-payload bound.", nameof(plaintext));
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(plaintext), OfflineCryptoFormat.HashBytes(header.PayloadSha256)))
            throw new ArgumentException("Payload hash does not describe the actual content.", nameof(header));
        if (header.OriginalActorId != sender.PublicKeys.ActorId || header.SourceDeviceId != sender.PublicKeys.DeviceId)
            throw new ArgumentException("Sender identity does not match the source device header.", nameof(header));
        var recipientKeys = RecipientKeys(recipients);
        var contentKey = RandomNumberGenerator.GetBytes(32);
        try
        {
            var aad = OfflineCryptoFormat.CanonicalHeader(header); var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length]; var tag = new byte[16];
            using (var aes = new AesGcm(contentKey, 16)) aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
            var wrappers = recipientKeys.Select(keys => Wrap(header, contentKey, keys)).OrderBy(SortKey, StringComparer.Ordinal).ToArray();
            var package = new OfflineEncryptedPackage(header, Convert.ToBase64String(nonce), Convert.ToBase64String(ciphertext),
                Convert.ToBase64String(tag), Array.AsReadOnly(wrappers), sender.PublicKeys.SigningPublicKey, "");
            var signature = sender.SignDigest(SHA256.HashData(OfflineCryptoFormat.CanonicalUnsignedPackage(package)));
            return package with { Signature = Convert.ToBase64String(signature) };
        }
        finally { CryptographicOperations.ZeroMemory(contentKey); }
    }
    public static OfflineDecryptedPayload Open(OfflineEncryptedPackage package, OfflineDeviceKeys recipient,
        string trustedSenderSigningPublicKey)
    {
        ArgumentNullException.ThrowIfNull(package); ArgumentNullException.ThrowIfNull(recipient); recipient.EnsureAvailable();
        package = SnapshotPackage(package);
        using var trustedSigner = OfflineCryptoFormat.ImportSigningKey(trustedSenderSigningPublicKey);
        if (!CryptographicOperations.FixedTimeEquals(OfflineCryptoFormat.Decode(package.SenderSigningPublicKey, 128),
            trustedSigner.ExportSubjectPublicKeyInfo())) throw new CryptographicException("The package signer does not match the trusted device key.");
        var signature = OfflineCryptoFormat.Decode(package.Signature, 64, 64);
        if (!trustedSigner.VerifyHash(SHA256.HashData(OfflineCryptoFormat.CanonicalUnsignedPackage(package)), signature,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) throw new CryptographicException("Offline package signature is invalid.");
        var fingerprint = OfflineCryptoFormat.Fingerprint(recipient.PublicKeys.EncryptionPublicKey);
        var wrap = package.Recipients.SingleOrDefault(x => x.ActorId == recipient.PublicKeys.ActorId && x.DeviceId == recipient.PublicKeys.DeviceId
            && x.RecipientKeyFingerprint == fingerprint) ?? throw new OfflineKeyUnavailableException();
        using var ephemeral = OfflineCryptoFormat.ImportEncryptionKey(wrap.EphemeralPublicKey);
        var secret = recipient.DeriveSecret(ephemeral.PublicKey);
        byte[]? wrappingKey = null; var contentKey = new byte[32]; byte[]? plaintext = null;
        try
        {
            var context = OfflineCryptoFormat.WrappingContext(package.Header, wrap.ActorId, wrap.DeviceId, wrap.RecipientKeyFingerprint, wrap.EphemeralPublicKey);
            wrappingKey = DeriveWrappingKey(secret, package.Header, context);
            using (var aes = new AesGcm(wrappingKey, 16)) aes.Decrypt(OfflineCryptoFormat.Decode(wrap.Nonce, 12, 12),
                OfflineCryptoFormat.Decode(wrap.WrappedKey, 32, 32), OfflineCryptoFormat.Decode(wrap.Tag, 16, 16), contentKey, context);
            var ciphertext = OfflineCryptoFormat.Decode(package.Ciphertext, OfflineCryptoFormat.MaxPayloadBytes);
            plaintext = new byte[ciphertext.Length];
            using (var aes = new AesGcm(contentKey, 16)) aes.Decrypt(OfflineCryptoFormat.Decode(package.PayloadNonce, 12, 12), ciphertext,
                OfflineCryptoFormat.Decode(package.PayloadTag, 16, 16), plaintext, OfflineCryptoFormat.CanonicalHeader(package.Header));
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(plaintext), OfflineCryptoFormat.HashBytes(package.Header.PayloadSha256)))
                throw new CryptographicException("Decrypted payload hash does not match its signed header.");
            var result = new OfflineDecryptedPayload(plaintext); plaintext = null; return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret); CryptographicOperations.ZeroMemory(contentKey);
            if (wrappingKey is not null) CryptographicOperations.ZeroMemory(wrappingKey);
            if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext);
        }
    }
    private static OfflineRecipientKeyWrap Wrap(OfflinePackageHeader header, byte[] contentKey, OfflineDevicePublicKeys recipient)
    {
        using var recipientKey = OfflineCryptoFormat.ImportEncryptionKey(recipient.EncryptionPublicKey);
        using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var ephemeralPublic = Convert.ToBase64String(ephemeral.ExportSubjectPublicKeyInfo());
        var fingerprint = OfflineCryptoFormat.Fingerprint(recipient.EncryptionPublicKey);
        var context = OfflineCryptoFormat.WrappingContext(header, recipient.ActorId, recipient.DeviceId, fingerprint, ephemeralPublic);
        var secret = ephemeral.DeriveRawSecretAgreement(recipientKey.PublicKey); byte[]? wrappingKey = null;
        try
        {
            wrappingKey = DeriveWrappingKey(secret, header, context); var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[32]; var tag = new byte[16];
            using (var aes = new AesGcm(wrappingKey, 16)) aes.Encrypt(nonce, contentKey, ciphertext, tag, context);
            return new OfflineRecipientKeyWrap(recipient.ActorId, recipient.DeviceId, fingerprint, ephemeralPublic,
                Convert.ToBase64String(nonce), Convert.ToBase64String(ciphertext), Convert.ToBase64String(tag));
        }
        finally { CryptographicOperations.ZeroMemory(secret); if (wrappingKey is not null) CryptographicOperations.ZeroMemory(wrappingKey); }
    }
    private static byte[] DeriveWrappingKey(byte[] secret, OfflinePackageHeader header, byte[] context)
        => HKDF.DeriveKey(HashAlgorithmName.SHA256, secret, 32, SHA256.HashData(OfflineCryptoFormat.CanonicalHeader(header)), context);
    private static OfflineDevicePublicKeys[] RecipientKeys(IReadOnlyList<OfflineDevicePublicKeys> recipients)
    {
        if (recipients.Count is < 1 or > OfflineCryptoFormat.MaxRecipients) throw new ArgumentException("Recipient set exceeds its bound.", nameof(recipients));
        var keys = recipients.ToArray(); foreach (var key in keys) OfflineCryptoFormat.ValidatePublicKeys(key);
        if (keys.Select(x => (x.ActorId, x.DeviceId)).Distinct().Count() != keys.Length
            || keys.Select(x => OfflineCryptoFormat.Fingerprint(x.EncryptionPublicKey)).Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new ArgumentException("Recipient identities and encryption keys must be unique.", nameof(recipients));
        return keys;
    }
    private static string SortKey(OfflineRecipientKeyWrap wrap) => $"{wrap.ActorId:N}|{wrap.DeviceId}|{wrap.RecipientKeyFingerprint}";
    internal static OfflineEncryptedPackage SnapshotPackage(OfflineEncryptedPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var header = OfflineCryptoFormat.SnapshotHeader(package.Header); ArgumentNullException.ThrowIfNull(package.Recipients);
        if (package.Recipients.Count is < 1 or > OfflineCryptoFormat.MaxRecipients) throw new ArgumentException("Recipient set exceeds its bound.", nameof(package));
        var recipients = package.Recipients.ToArray();
        foreach (var wrap in recipients)
        {
            ArgumentNullException.ThrowIfNull(wrap); OfflineCryptoFormat.Identity(wrap.ActorId, wrap.DeviceId); OfflineCryptoFormat.HashBytes(wrap.RecipientKeyFingerprint);
            using var key = OfflineCryptoFormat.ImportEncryptionKey(wrap.EphemeralPublicKey);
            OfflineCryptoFormat.Decode(wrap.Nonce, 12, 12); OfflineCryptoFormat.Decode(wrap.WrappedKey, 32, 32); OfflineCryptoFormat.Decode(wrap.Tag, 16, 16);
        }
        if (!recipients.Select(SortKey).SequenceEqual(recipients.OrderBy(SortKey, StringComparer.Ordinal).Select(SortKey))
            || recipients.Select(x => (x.ActorId, x.DeviceId)).Distinct().Count() != recipients.Length
            || recipients.Select(x => x.RecipientKeyFingerprint).Distinct(StringComparer.Ordinal).Count() != recipients.Length)
            throw new ArgumentException("Recipient wrappers must be a unique canonical set.", nameof(package));
        OfflineCryptoFormat.Decode(package.PayloadNonce, 12, 12); OfflineCryptoFormat.Decode(package.PayloadTag, 16, 16);
        OfflineCryptoFormat.Decode(package.Ciphertext, OfflineCryptoFormat.MaxPayloadBytes);
        OfflineCryptoFormat.Decode(package.Signature, 64, 64);
        using var signer = OfflineCryptoFormat.ImportSigningKey(package.SenderSigningPublicKey);
        return package with { Header = header, Recipients = Array.AsReadOnly(recipients) };
    }
}
