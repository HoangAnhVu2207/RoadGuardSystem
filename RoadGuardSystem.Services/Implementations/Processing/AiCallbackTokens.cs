using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace RoadGuardSystem.Services.Processing;

/// <summary>Private deployment configuration. Never expose through logs or FE responses.</summary>
public sealed class AiServiceTokenOptions
{
    public const string Section = "AiServiceJwt";
    public bool Enabled { get; set; }
    public string Subject { get; set; } = "roadguard-ai-service";
    public string ActiveKeyId { get; set; } = "";
    public string PrivateKeyPem { get; set; } = "";
    public Dictionary<string, string> PublicKeys { get; set; } = new(StringComparer.Ordinal);
}

public sealed class AiCallbackTokens(AiServiceTokenOptions options, TimeProvider clock)
{
    public const string Issuer = "roadguard-backend";
    public const string Audience = "roadguard-ai-callback";
    public const int LifetimeSeconds = 300;

    public string Mint(Guid jobId, Guid attemptId)
    {
        if (jobId == Guid.Empty || attemptId == Guid.Empty) throw new ArgumentException("Dispatch identity is required.");
        if (string.IsNullOrWhiteSpace(options.PrivateKeyPem) || string.IsNullOrWhiteSpace(options.Subject) ||
            !options.PublicKeys.TryGetValue(options.ActiveKeyId, out var activePublicKey)) throw new InvalidOperationException("AI signing configuration is unavailable.");
        using var rsa = RSA.Create(); rsa.ImportFromPem(options.PrivateKeyPem);
        if (rsa.KeySize < 2048) throw new InvalidOperationException("AI signing key is too small.");
        using var verifier = RSA.Create(); verifier.ImportFromPem(activePublicKey);
        if (!rsa.ExportSubjectPublicKeyInfo().SequenceEqual(verifier.ExportSubjectPublicKeyInfo()))
            throw new InvalidOperationException("AI active signing key does not match its verifier.");
        var now = clock.GetUtcNow().ToUnixTimeSeconds();
        var payload = new JwtPayload
        {
            ["iss"] = Issuer,
            ["aud"] = Audience,
            ["sub"] = options.Subject,
            ["iat"] = now,
            ["nbf"] = now,
            ["exp"] = now + LifetimeSeconds,
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["jobId"] = jobId.ToString(),
            ["attemptId"] = attemptId.ToString()
        };
        // The JWT provider cache can outlive this method's RSA handle. Give it independent
        // key parameters rather than a disposed handle on the second dispatch.
        var key = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = options.ActiveKeyId };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(new JwtHeader(new SigningCredentials(key, SecurityAlgorithms.RsaSha256)), payload));
    }

    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        NameClaimType = "sub",
        IssuerSigningKeyResolver = (_, _, kid, _) =>
        {
            if (string.IsNullOrWhiteSpace(kid) || !options.PublicKeys.TryGetValue(kid, out var pem)) return [];
            try
            {
                using var rsa = RSA.Create(); rsa.ImportFromPem(pem);
                return rsa.KeySize < 2048 ? [] : [new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = kid }];
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException) { return []; }
        },
        LifetimeValidator = (notBefore, expires, _, _) =>
        {
            var now = clock.GetUtcNow().UtcDateTime;
            return expires is not null && expires > now && (notBefore is null || notBefore <= now);
        }
    };

    public ClaimsPrincipal Validate(string bearer)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(bearer, ValidationParameters(), out _);
        ValidateClaims(principal);
        return principal;
    }

    public void ValidateClaims(ClaimsPrincipal principal)
    {
        string One(string type)
        {
            var values = principal.FindAll(type).ToArray();
            if (values.Length != 1 || string.IsNullOrWhiteSpace(values[0].Value)) throw new SecurityTokenValidationException("AI required claim is invalid.");
            return values[0].Value;
        }
        if (string.IsNullOrWhiteSpace(options.Subject) || One("sub") != options.Subject || One("iss") != Issuer || One("aud") != Audience ||
            !Guid.TryParse(One("jobId"), out var job) || job == Guid.Empty || !Guid.TryParse(One("attemptId"), out var attempt) || attempt == Guid.Empty ||
            !long.TryParse(One("iat"), out var issued) || !long.TryParse(One("exp"), out var expiry) ||
            issued > clock.GetUtcNow().ToUnixTimeSeconds() || expiry <= issued || expiry - issued > LifetimeSeconds)
            throw new SecurityTokenValidationException("AI identity or lifetime is invalid.");
        _ = One("jti");
    }

    public static bool Matches(ClaimsPrincipal principal, Guid jobId, Guid attemptId) =>
        Guid.TryParse(principal.FindFirst("jobId")?.Value, out var job) && job == jobId && job != Guid.Empty &&
        Guid.TryParse(principal.FindFirst("attemptId")?.Value, out var attempt) && attempt == attemptId && attempt != Guid.Empty;
}
