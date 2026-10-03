using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<WebSessionState?> TouchWebSessionAsync(Guid userId, Guid sessionId,
        UserRoleCode role, DateTimeOffset now, bool allowMustChangePassword = false,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            var user = await _context.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={userId}")
                .SingleOrDefaultAsync(cancellationToken);
            if (user is null || user.Status != UserStatus.Active || user.RoleCode != role ||
                user.MustChangePassword && !allowMustChangePassword)
                return null;
            var roleCode = role.ToDbCode();
            var currentRole = await _context.Roles.FromSqlInterpolated($"SELECT * FROM [Roles] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]={roleCode}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (currentRole is null || !currentRole.IsActive) return null;
            var session = await _context.Sessions.FromSqlInterpolated($"SELECT * FROM [Sessions] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={sessionId}")
                .SingleOrDefaultAsync(cancellationToken);
            if (session is null || session.UserId != userId || session.Transport != SessionTransport.Web || !session.IsActiveAt(now))
                return null;
            session.Touch(now);
            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new WebSessionState(new UserSecurityState(user.Id, user.UserName!, user.DisplayName,
                user.RoleCode, user.Status, user.MustChangePassword, user.RowVersion), session.Id,
                session.IssuedAt, session.ExpiresAt, session.LastActivityAt!.Value.AddMinutes(30));
        });
    }
}
