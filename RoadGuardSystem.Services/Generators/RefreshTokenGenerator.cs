using System.Security.Cryptography;
using System.Text;

namespace RoadGuardSystem.Services.Generators;

public sealed record RefreshTokenMaterial(string Plaintext, string HashHex);

public static class RefreshTokenGenerator
{
    private const int TokenByteLength = 32;

    public static RefreshTokenMaterial Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        var plaintext = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return new RefreshTokenMaterial(plaintext, Hash(plaintext));
    }

    public static string Hash(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new ArgumentException("Refresh token cannot be empty.", nameof(plaintext));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext))).ToLowerInvariant();
    }
}
