using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.Services.Factories;

public sealed class PasswordChangeFingerprintFactory
{
    private static readonly byte[] Purpose = Encoding.UTF8.GetBytes(
        "roadguard:forced-password-change:idempotency:v1");
    private readonly byte[] _fingerprintKey;

    public PasswordChangeFingerprintFactory(PasswordChangeFingerprintOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _fingerprintKey = HMACSHA256.HashData(
            PasswordChangeFingerprintOptionsValidator.DecodeKey(options),
            Purpose);
    }

    public string Create(Guid userId, string newPassword)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("A user and replacement password are required.");
        }

        var payload = Encoding.UTF8.GetBytes($"{userId:N}:{newPassword}");
        return Convert.ToHexString(HMACSHA256.HashData(_fingerprintKey, payload)).ToLowerInvariant();
    }
}
