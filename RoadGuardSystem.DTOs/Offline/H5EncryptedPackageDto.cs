using System.Text.Json.Serialization;
using RoadGuardSystem.BusinessObjects.Offline;

namespace RoadGuardSystem.DTOs.Offline;

// Local format foundation only. No HTTP route, current-authority grant or external client adoption is activated.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record H5PackageHeaderDto(int Version, string PayloadSchema, Guid PackageId, Guid ProjectId,
    Guid OriginalActorId, string SourceDeviceId, Guid[] OriginIds, string PayloadSha256);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record H5RecipientKeyWrapDto(Guid ActorId, string DeviceId, string RecipientKeyFingerprint,
    string EphemeralPublicKey, string Nonce, string WrappedKey, string Tag);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record H5EncryptedPackageDto(H5PackageHeaderDto Header, string PayloadNonce, string Ciphertext,
    string PayloadTag, H5RecipientKeyWrapDto[] Recipients, string SenderSigningPublicKey, string Signature)
{
    public static H5EncryptedPackageDto FromDomain(OfflineEncryptedPackage package)
    {
        ArgumentNullException.ThrowIfNull(package); ArgumentNullException.ThrowIfNull(package.Header);
        ArgumentNullException.ThrowIfNull(package.Recipients); ArgumentNullException.ThrowIfNull(package.Header.OriginIds);
        BoundCollections(package.Header.OriginIds.Count, package.Recipients.Count);
        var header = package.Header;
        WireDevice(header.SourceDeviceId); OfflineCryptoFormat.CanonicalHeader(header);
        foreach (var recipient in package.Recipients) { ArgumentNullException.ThrowIfNull(recipient); WireDevice(recipient.DeviceId); }
        return new(new(header.Version, header.PayloadSchema, header.PackageId, header.ProjectId, header.OriginalActorId,
            header.SourceDeviceId, header.OriginIds.ToArray(), header.PayloadSha256), package.PayloadNonce, package.Ciphertext,
            package.PayloadTag, package.Recipients.Select(r => new H5RecipientKeyWrapDto(r.ActorId, r.DeviceId,
                r.RecipientKeyFingerprint, r.EphemeralPublicKey, r.Nonce, r.WrappedKey, r.Tag)).ToArray(), package.SenderSigningPublicKey, package.Signature);
    }
    // Conversion preserves wire facts; Open must still verify the signature against an independently trusted device key.
    public OfflineEncryptedPackage ToDomain()
    {
        ArgumentNullException.ThrowIfNull(Header); ArgumentNullException.ThrowIfNull(Header.OriginIds); ArgumentNullException.ThrowIfNull(Recipients);
        BoundCollections(Header.OriginIds.Length, Recipients.Length);
        WireDevice(Header.SourceDeviceId);
        var header = new OfflinePackageHeader(Header.Version, Header.PayloadSchema, Header.PackageId, Header.ProjectId,
            Header.OriginalActorId, Header.SourceDeviceId, Array.AsReadOnly(Header.OriginIds.ToArray()), Header.PayloadSha256);
        OfflineCryptoFormat.CanonicalHeader(header);
        EncodedBound(PayloadNonce, 12); EncodedBound(PayloadTag, 16); EncodedBound(Ciphertext, OfflineCryptoFormat.MaxPayloadBytes);
        EncodedBound(SenderSigningPublicKey, 128); EncodedBound(Signature, 64);
        var recipients = Recipients.Select(r =>
        {
            ArgumentNullException.ThrowIfNull(r);
            WireDevice(r.DeviceId);
            if (r.ActorId == Guid.Empty || string.IsNullOrEmpty(r.DeviceId) || r.DeviceId.Length > 80 || r.RecipientKeyFingerprint is not { Length: 64 })
                throw new ArgumentException("Recipient metadata is outside its bounds.");
            EncodedBound(r.EphemeralPublicKey, 128); EncodedBound(r.Nonce, 12); EncodedBound(r.WrappedKey, 32); EncodedBound(r.Tag, 16);
            return new OfflineRecipientKeyWrap(r.ActorId, r.DeviceId, r.RecipientKeyFingerprint, r.EphemeralPublicKey, r.Nonce, r.WrappedKey, r.Tag);
        }).ToArray();
        return new(header, PayloadNonce, Ciphertext, PayloadTag, Array.AsReadOnly(recipients), SenderSigningPublicKey, Signature);
    }
    private static void BoundCollections(int origins, int recipients)
    {
        if (origins is < 1 or > OfflineCryptoFormat.MaxOrigins || recipients is < 1 or > OfflineCryptoFormat.MaxRecipients)
            throw new ArgumentException("Offline package collections exceed their bounds.");
    }
    private static void EncodedBound(string value, int maximumBytes)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 4L * ((maximumBytes + 2L) / 3L))
            throw new ArgumentException("Encoded package field exceeds its bound.", nameof(value));
    }
    // A well-formed GUID is only a transport identity; the later registry/grant admission must prove its actual binding.
    private static void WireDevice(string device)
    {
        if (!Guid.TryParseExact(device, "D", out var id) || id == Guid.Empty || id.ToString("D") != device)
            throw new ArgumentException("Wire device identity requires canonical non-empty lowercase GUID-D.", nameof(device));
    }
}
