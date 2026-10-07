using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.Services.Authentication;

// Receipt values remain encrypted across process restarts and active signing-key rotation.
public sealed class RefreshCredentialProtection(JwtOptions options)
{
    private static readonly byte[] Purpose = Encoding.UTF8.GetBytes("RoadGuard:H1:refresh-rotation:v1");
    public string Protect(string credential)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var clear = Encoding.UTF8.GetBytes(credential);
        var encrypted = new byte[clear.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key(options.ActiveKeyId), 16);
        aes.Encrypt(nonce, clear, encrypted, tag, Purpose);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(options.ActiveKeyId)) + "." + Convert.ToBase64String(nonce.Concat(tag).Concat(encrypted).ToArray());
    }
    public string Unprotect(string value)
    {
        var split = value.IndexOf('.');
        if (split <= 0) throw new CryptographicException("Invalid protected credential.");
        var bytes = Convert.FromBase64String(value[(split + 1)..]);
        if (bytes.Length < 28) throw new CryptographicException("Invalid protected credential.");
        var clear = new byte[bytes.Length - 28];
        using var aes = new AesGcm(Key(Encoding.UTF8.GetString(Convert.FromBase64String(value[..split]))), 16);
        aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), clear, Purpose);
        return Encoding.UTF8.GetString(clear);
    }
    private byte[] Key(string id) => HMACSHA256.HashData(JwtOptionsValidator.DecodeKey(options.SigningKeys[id]), Purpose);
}
