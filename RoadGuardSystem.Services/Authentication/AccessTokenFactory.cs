using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Authentication;

public sealed class AccessTokenFactory
{
    private readonly JwtOptions _options;
    private readonly byte[] _activeKey;

    public AccessTokenFactory(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var validation = new JwtOptionsValidator().Validate(JwtOptions.SectionName, options);
        if (validation.Failed)
        {
            throw new OptionsValidationException(JwtOptions.SectionName, typeof(JwtOptions), validation.Failures!);
        }

        _options = options;
        _activeKey = JwtOptionsValidator.DecodeKey(options.SigningKeys[options.ActiveKeyId]);
    }

    public string Create(Guid userId, Guid sessionId, UserRoleCode roleCode, DateTimeOffset issuedAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Session id cannot be empty.", nameof(sessionId));
        }

        var role = roleCode.ToDbCode();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_activeKey) { KeyId = _options.ActiveKeyId },
            SecurityAlgorithms.HmacSha256);

        var identity = new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("sid", sessionId.ToString()),
            new Claim("role", role)
        ]);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = identity,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = issuedAt.AddMinutes(_options.AccessTokenLifetimeMinutes).UtcDateTime,
            SigningCredentials = credentials
        };

        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }
}
