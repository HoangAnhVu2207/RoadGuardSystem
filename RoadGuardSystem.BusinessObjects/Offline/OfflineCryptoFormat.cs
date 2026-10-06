using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Offline;

public static class OfflineCryptoFormat
{
    public const int Version = 1;
    public const string PayloadSchema = "huy-final.offline.bundle.v1";
    public const int MaxOrigins = 1000;
    public const int MaxRecipients = 32;
    public const int MaxPayloadBytes = 16 * 1024 * 1024;
    private static readonly JsonWriterOptions CanonicalWriter = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    // Structural content identity excludes potentially varying ECDSA signature bytes. It is never authentication.
    public static string PackageContentFingerprint(OfflineEncryptedPackage package)
    {
        var snapshot = OfflineHandoverCrypto.SnapshotPackage(package);
        return Convert.ToHexString(SHA256.HashData(CanonicalUnsignedPackage(snapshot))).ToLowerInvariant();
    }
    // Technical bound for one sealed payload, not a product limit for an entire offline evidence collection.
    public static byte[] CanonicalHeader(OfflinePackageHeader header)
    {
        ValidateHeader(header);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, CanonicalWriter)) WriteHeader(writer, header);
        return buffer.ToArray();
    }
    internal static byte[] CanonicalUnsignedPackage(OfflineEncryptedPackage package)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, CanonicalWriter))
        {
            writer.WriteStartObject(); writer.WritePropertyName("header"); WriteHeader(writer, package.Header);
            writer.WriteString("payloadNonce", package.PayloadNonce); writer.WriteString("ciphertext", package.Ciphertext);
            writer.WriteString("payloadTag", package.PayloadTag); writer.WritePropertyName("recipients"); writer.WriteStartArray();
            foreach (var recipient in package.Recipients)
            {
                writer.WriteStartObject(); WriteRecipientIdentity(writer, recipient.ActorId, recipient.DeviceId, recipient.RecipientKeyFingerprint);
                writer.WriteString("ephemeralPublicKey", recipient.EphemeralPublicKey); writer.WriteString("nonce", recipient.Nonce);
                writer.WriteString("wrappedKey", recipient.WrappedKey); writer.WriteString("tag", recipient.Tag); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteString("senderSigningPublicKey", package.SenderSigningPublicKey); writer.WriteEndObject();
        }
        return buffer.ToArray();
    }
    internal static byte[] WrappingContext(OfflinePackageHeader header, Guid actor, string device, string fingerprint, string ephemeralPublicKey)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, CanonicalWriter))
        {
            writer.WriteStartObject(); writer.WriteString("purpose", "roadguard.h5.recipient-key.v1");
            writer.WritePropertyName("header"); WriteHeader(writer, header); WriteRecipientIdentity(writer, actor, device, fingerprint);
            writer.WriteString("ephemeralPublicKey", ephemeralPublicKey); writer.WriteEndObject();
        }
        return buffer.ToArray();
    }
    private static void WriteRecipientIdentity(Utf8JsonWriter writer, Guid actor, string device, string fingerprint)
    {
        writer.WriteString("actorId", actor.ToString("D")); writer.WriteString("deviceId", device);
        writer.WriteString("recipientKeyFingerprint", fingerprint);
    }
    private static void WriteHeader(Utf8JsonWriter writer, OfflinePackageHeader header)
    {
        writer.WriteStartObject(); writer.WriteNumber("version", header.Version); writer.WriteString("payloadSchema", header.PayloadSchema);
        writer.WriteString("packageId", header.PackageId.ToString("D")); writer.WriteString("projectId", header.ProjectId.ToString("D"));
        writer.WriteString("originalActorId", header.OriginalActorId.ToString("D")); writer.WriteString("sourceDeviceId", header.SourceDeviceId);
        writer.WritePropertyName("originIds"); writer.WriteStartArray();
        foreach (var origin in header.OriginIds) writer.WriteStringValue(origin.ToString("D"));
        writer.WriteEndArray(); writer.WriteString("payloadSha256", header.PayloadSha256); writer.WriteEndObject();
    }
    internal static OfflinePackageHeader SnapshotHeader(OfflinePackageHeader header)
    {
        ArgumentNullException.ThrowIfNull(header); ArgumentNullException.ThrowIfNull(header.OriginIds);
        if (header.OriginIds.Count is < 1 or > MaxOrigins) throw new ArgumentException("The origin set is outside its bounds.", nameof(header));
        var snapshot = header with { OriginIds = Array.AsReadOnly(header.OriginIds.ToArray()) };
        ValidateHeader(snapshot); return snapshot;
    }
    internal static void ValidateHeader(OfflinePackageHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        if (header.Version != Version || header.PayloadSchema != PayloadSchema) throw new ArgumentException("Unsupported offline crypto schema.", nameof(header));
        Id(header.PackageId); Id(header.ProjectId); Identity(header.OriginalActorId, header.SourceDeviceId);
        ArgumentNullException.ThrowIfNull(header.OriginIds);
        if (header.OriginIds.Count is < 1 or > MaxOrigins || header.OriginIds.Any(x => x == Guid.Empty)
            || header.OriginIds.Distinct().Count() != header.OriginIds.Count
            || !header.OriginIds.SequenceEqual(header.OriginIds.OrderBy(x => x.ToString("N"), StringComparer.Ordinal)))
            throw new ArgumentException("Origins require a nonempty bounded canonical unique set.", nameof(header));
        HashBytes(header.PayloadSha256);
    }
    internal static void Id(Guid id) { if (id == Guid.Empty) throw new ArgumentException("A non-empty identity is required.", nameof(id)); }
    internal static void Identity(Guid actor, string device)
    {
        Id(actor); ArgumentException.ThrowIfNullOrWhiteSpace(device);
        if (device.Length > 80 || device.Any(x => !(x is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_')))
            throw new ArgumentException("Device identity must be a bounded opaque ASCII identifier.", nameof(device));
    }
    internal static byte[] HashBytes(string hash)
    {
        if (hash is not { Length: 64 } || hash.Any(x => !(x is >= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("SHA256 requires canonical lowercase hexadecimal.", nameof(hash));
        return Convert.FromHexString(hash);
    }
    internal static byte[] Decode(string value, int maximumBytes, int? exactBytes = null)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 4L * ((maximumBytes + 2L) / 3L))
            throw new ArgumentException("Encoded crypto field is empty or exceeds its bound.", nameof(value));
        byte[] decoded;
        try { decoded = Convert.FromBase64String(value); }
        catch (FormatException ex) { throw new ArgumentException("Malformed base64 crypto field.", nameof(value), ex); }
        if (decoded.Length > maximumBytes || exactBytes.HasValue && decoded.Length != exactBytes
            || Convert.ToBase64String(decoded) != value)
            throw new ArgumentException("Crypto field length or base64 representation is not canonical.", nameof(value));
        return decoded;
    }
    internal static ECDiffieHellman ImportEncryptionKey(string value)
    {
        var bytes = Decode(value, 128); var key = ECDiffieHellman.Create();
        try { key.ImportSubjectPublicKeyInfo(bytes, out var used); P256(key.ExportParameters(false), used == bytes.Length); return key; }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        { key.Dispose(); throw new ArgumentException("Encryption key must be a complete P256 SPKI public key.", nameof(value), ex); }
    }
    internal static ECDsa ImportSigningKey(string value)
    {
        var bytes = Decode(value, 128); var key = ECDsa.Create();
        try { key.ImportSubjectPublicKeyInfo(bytes, out var used); P256(key.ExportParameters(false), used == bytes.Length); return key; }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        { key.Dispose(); throw new ArgumentException("Signing key must be a complete P256 SPKI public key.", nameof(value), ex); }
    }
    private static void P256(ECParameters parameters, bool complete)
    {
        if (!complete || parameters.Curve.Oid.Value != "1.2.840.10045.3.1.7" || parameters.Q.X?.Length != 32 || parameters.Q.Y?.Length != 32)
            throw new ArgumentException("Only the versioned P256 curve is supported.");
    }
    internal static void ValidatePublicKeys(OfflineDevicePublicKeys keys)
    {
        ArgumentNullException.ThrowIfNull(keys); Identity(keys.ActorId, keys.DeviceId);
        using var encryption = ImportEncryptionKey(keys.EncryptionPublicKey);
        using var signing = ImportSigningKey(keys.SigningPublicKey);
        if (CryptographicOperations.FixedTimeEquals(encryption.ExportSubjectPublicKeyInfo(), signing.ExportSubjectPublicKeyInfo()))
            throw new ArgumentException("Signing and encryption require separate key pairs.", nameof(keys));
    }
    internal static string Fingerprint(string publicKey) => Convert.ToHexString(SHA256.HashData(Decode(publicKey, 128))).ToLowerInvariant();
}

public sealed record OfflinePackageHeader(int Version, string PayloadSchema, Guid PackageId, Guid ProjectId,
    Guid OriginalActorId, string SourceDeviceId, IReadOnlyList<Guid> OriginIds, string PayloadSha256)
{
    public static OfflinePackageHeader Create(Guid packageId, Guid projectId, Guid originalActorId, string sourceDeviceId,
        IReadOnlyList<Guid> originIds, string payloadSha256)
    {
        ArgumentNullException.ThrowIfNull(originIds);
        if (originIds.Count is < 1 or > OfflineCryptoFormat.MaxOrigins) throw new ArgumentException("The origin set is outside its bounds.", nameof(originIds));
        var header = new OfflinePackageHeader(OfflineCryptoFormat.Version, OfflineCryptoFormat.PayloadSchema, packageId, projectId,
            originalActorId, sourceDeviceId, Array.AsReadOnly(originIds.OrderBy(x => x.ToString("N"), StringComparer.Ordinal).ToArray()), payloadSha256);
        OfflineCryptoFormat.ValidateHeader(header); return header;
    }
}

public sealed record OfflineDevicePublicKeys(Guid ActorId, string DeviceId, string EncryptionPublicKey, string SigningPublicKey);
public sealed record OfflineRecipientKeyWrap(Guid ActorId, string DeviceId, string RecipientKeyFingerprint,
    string EphemeralPublicKey, string Nonce, string WrappedKey, string Tag);
public sealed record OfflineEncryptedPackage(OfflinePackageHeader Header, string PayloadNonce, string Ciphertext,
    string PayloadTag, IReadOnlyList<OfflineRecipientKeyWrap> Recipients, string SenderSigningPublicKey, string Signature);
