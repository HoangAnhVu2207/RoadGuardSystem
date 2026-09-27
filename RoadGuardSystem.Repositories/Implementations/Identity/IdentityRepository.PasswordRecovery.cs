using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.BusinessObjects.Messaging;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<PasswordRecoveryPersistenceResult> CreatePasswordRecoveryRequestAsync(
        Guid requestId,
        string normalizedEmail,
        DateTimeOffset requestedAtUtc,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var target = await _context.Users
            .AsNoTracking()
            .Where(user => user.NormalizedEmail == normalizedEmail && user.Status == UserStatus.Active)
            .Select(user => new { user.Id })
            .SingleOrDefaultAsync(cancellationToken);

        var request = new PasswordRecoveryRequest
        {
            Id = requestId,
            TargetUserId = target?.Id,
            RequestedAtUtc = requestedAtUtc.ToUniversalTime(),
            CorrelationId = correlationId
        };
        _context.PasswordRecoveryRequests.Add(request);

        var notificationCount = 0;
        if (target is not null)
        {
            var supervisorIds = await _context.Users
                .AsNoTracking()
                .Where(user => user.RoleCode == UserRoleCode.Supervisor && user.Status == UserStatus.Active)
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);
            foreach (var supervisorId in supervisorIds)
            {
                _context.Notifications.Add(Notification.Create(
                    Guid.NewGuid(),
                    supervisorId,
                    "PasswordRecoveryRequest",
                    requestId,
                    "password_recovery_requested",
                    "Password recovery requested",
                    "An active account requires password recovery review."));
            }

            notificationCount = supervisorIds.Count;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new PasswordRecoveryPersistenceResult(requestId, target is not null, notificationCount);
    }
}
