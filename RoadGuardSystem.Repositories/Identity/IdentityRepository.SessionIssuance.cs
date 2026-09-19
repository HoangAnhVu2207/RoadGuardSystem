using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<IssueSessionResult> IssueSessionWithRefreshTokenAsync(
        Guid userId,
        byte[] expectedUserRowVersion,
        UserSession session,
        RefreshToken initialRefreshToken,
        DateTimeOffset successfulLoginAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedUserRowVersion);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(initialRefreshToken);

        if (userId == Guid.Empty ||
            expectedUserRowVersion.Length == 0 ||
            session.Id == Guid.Empty ||
            session.UserId != userId ||
            initialRefreshToken.Id == Guid.Empty ||
            initialRefreshToken.SessionId != session.Id ||
            string.IsNullOrWhiteSpace(initialRefreshToken.TokenHash) ||
            initialRefreshToken.TokenHash.Trim().Length < 32 ||
            session.IssuedAt.ToUniversalTime() != successfulLoginAt.ToUniversalTime())
        {
            return new IssueSessionResult(IssueSessionStatus.InvalidInput, null, "Initial credential material is invalid.");
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        try
        {
            return await executionStrategy.ExecuteInTransactionAsync(
                async attemptCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var user = await _context.Users.SingleOrDefaultAsync(
                        candidate => candidate.Id == userId,
                            attemptCancellationToken);
                    if (user is null)
                    {
                        return new IssueSessionResult(IssueSessionStatus.UserNotFound);
                    }

                    if (user.Status != UserStatus.Active || user.MustChangePassword)
                    {
                        return new IssueSessionResult(IssueSessionStatus.UserNotEligible);
                    }

                    if (!user.RowVersion.SequenceEqual(expectedUserRowVersion))
                    {
                        return new IssueSessionResult(IssueSessionStatus.StaleConcurrency);
                    }

                    user.LastLoginAt = successfulLoginAt.ToUniversalTime();
                    var attemptSession = new UserSession
                    {
                        Id = session.Id,
                        UserId = session.UserId,
                        IssuedAt = session.IssuedAt,
                        DeviceMetadataJson = session.DeviceMetadataJson,
                        ExpiresAt = session.ExpiresAt,
                        RevokedAt = session.RevokedAt
                    };
                    var attemptRefreshToken = new RefreshToken
                    {
                        Id = initialRefreshToken.Id,
                        SessionId = initialRefreshToken.SessionId,
                        TokenHash = initialRefreshToken.TokenHash,
                        ExpiresAt = initialRefreshToken.ExpiresAt,
                        RevokedAt = initialRefreshToken.RevokedAt
                    };
                    _context.Sessions.Add(attemptSession);
                    _context.RefreshTokens.Add(attemptRefreshToken);

                    await _context.SaveChangesAsync(attemptCancellationToken);
                    return new IssueSessionResult(IssueSessionStatus.Success, session.Id);
                },
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    return await _context.Sessions
                        .AsNoTracking()
                        .Where(candidate =>
                            candidate.Id == session.Id &&
                            candidate.UserId == userId)
                        .AnyAsync(candidate => candidate.RefreshTokens.Any(token =>
                            token.Id == initialRefreshToken.Id &&
                            token.TokenHash == initialRefreshToken.TokenHash), verificationCancellationToken);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return new IssueSessionResult(IssueSessionStatus.StaleConcurrency);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            return new IssueSessionResult(IssueSessionStatus.DuplicateCredential);
        }
    }
}
