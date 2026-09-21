using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Authentication;

public enum AuthoritativeSessionValidation
{
    Success,
    Unauthorized,
    SessionRevoked
}

public sealed class AuthoritativeSessionValidator
{
    private readonly IIdentityRepository _identityRepository;

    public AuthoritativeSessionValidator(IIdentityRepository identityRepository)
    {
        _identityRepository = identityRepository ?? throw new ArgumentNullException(nameof(identityRepository));
    }

    public async Task<AuthoritativeSessionValidation> ValidateAsync(
        Guid userId,
        Guid sessionId,
        UserRoleCode roleSnapshot,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || sessionId == Guid.Empty || roleSnapshot == UserRoleCode.Unknown)
        {
            return AuthoritativeSessionValidation.Unauthorized;
        }

        try
        {
            var session = await _identityRepository.GetSessionSecurityStateAsync(sessionId, cancellationToken);
            if (session is null ||
                session.UserId != userId ||
                session.RevokedAt is not null ||
                session.ExpiresAt <= now)
            {
                return AuthoritativeSessionValidation.SessionRevoked;
            }

            var user = await _identityRepository.GetUserSecurityStateAsync(userId, cancellationToken);
            if (user is null || user.Status != UserStatus.Active)
            {
                return AuthoritativeSessionValidation.Unauthorized;
            }

            if (user.RoleCode != roleSnapshot)
            {
                await _identityRepository.RevokeSessionAndFamilyAsync(sessionId, cancellationToken);
                return AuthoritativeSessionValidation.SessionRevoked;
            }

            return AuthoritativeSessionValidation.Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return AuthoritativeSessionValidation.Unauthorized;
        }
    }
}
