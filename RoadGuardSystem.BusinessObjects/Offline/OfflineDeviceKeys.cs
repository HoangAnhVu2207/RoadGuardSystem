using System.Security.Cryptography;

namespace RoadGuardSystem.BusinessObjects.Offline;

/// <summary>Client-owned keys. No server registration, escrow, storage or recovery capability is created here.</summary>
public sealed class OfflineDeviceKeys : IDisposable
{
    private readonly ECDiffieHellman _encryption;
    private readonly ECDsa _signing;
    private bool _disposed;
    private OfflineDeviceKeys(ECDiffieHellman encryption, ECDsa signing, OfflineDevicePublicKeys publicKeys)
    { _encryption = encryption; _signing = signing; PublicKeys = publicKeys; }
    public OfflineDevicePublicKeys PublicKeys { get; }
    public static OfflineDeviceKeys Generate(Guid actorId, string deviceId)
    {
        OfflineCryptoFormat.Identity(actorId, deviceId);
        var encryption = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        try
        {
            var signing = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            try
            {
                var keys = new OfflineDevicePublicKeys(actorId, deviceId, Convert.ToBase64String(encryption.ExportSubjectPublicKeyInfo()),
                    Convert.ToBase64String(signing.ExportSubjectPublicKeyInfo()));
                return new OfflineDeviceKeys(encryption, signing, keys);
            }
            catch { signing.Dispose(); throw; }
        }
        catch { encryption.Dispose(); throw; }
    }
    internal void EnsureAvailable() => ObjectDisposedException.ThrowIf(_disposed, this);
    internal byte[] SignDigest(byte[] digest)
    {
        EnsureAvailable(); return _signing.SignHash(digest, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }
    internal byte[] DeriveSecret(ECDiffieHellmanPublicKey publicKey)
    { EnsureAvailable(); return _encryption.DeriveRawSecretAgreement(publicKey); }
    public void Dispose()
    {
        if (!_disposed) { _encryption.Dispose(); _signing.Dispose(); _disposed = true; }
        GC.SuppressFinalize(this);
    }
}
