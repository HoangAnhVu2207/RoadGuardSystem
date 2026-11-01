using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<UserPhoneNumberWriteStatus> UpdateStaffPhoneNumberAtomicAsync(Guid actorUserId, Guid targetUserId,
        string phoneNumber, byte[] expectedRowVersion, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{targetUserId:N}:{phoneNumber}:{Convert.ToBase64String(expectedRowVersion)}"))).ToLowerInvariant();
        var idempotency = new IdempotencyOperationService(_context);
        try
        {
            var result = await idempotency.ExecuteSerializableAsync(actorUserId, null, "StaffPhoneNumberUpdated",
                idempotencyKey, fingerprint, async token =>
                {
                    await CheckPhoneAuthorityAsync(actorUserId, targetUserId, token);
                    var target = await _context.Users.SingleAsync(u => u.Id == targetUserId, token);
                    if (!target.RowVersion.SequenceEqual(expectedRowVersion))
                        throw new PhoneWriteException(UserPhoneNumberWriteStatus.StaleVersion);
                    if (!string.Equals(target.PhoneNumber, phoneNumber, StringComparison.Ordinal))
                    {
                        target.PhoneNumber = phoneNumber;
                        target.PhoneNumberConfirmed = false;
                        _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actorUserId, _timeProvider.GetUtcNow(),
                            "staff_phone_number_updated", "User", targetUserId, null, null,
                            "Supervisor staff contact update", "USERS_API", null));
                        await _context.SaveChangesAsync(token);
                    }
                    return (targetUserId, "{}");
                }, cancellationToken, token => CheckPhoneAuthorityAsync(actorUserId, targetUserId, token));
            return result.Status switch
            {
                IdempotencyOperationStatus.Executed => UserPhoneNumberWriteStatus.Success,
                IdempotencyOperationStatus.Replayed => UserPhoneNumberWriteStatus.Replay,
                _ => UserPhoneNumberWriteStatus.KeyConflict
            };
        }
        catch (PhoneWriteException exception)
        {
            return exception.Status;
        }
        catch (DbUpdateConcurrencyException)
        {
            return UserPhoneNumberWriteStatus.StaleVersion;
        }
    }

    private async Task CheckPhoneAuthorityAsync(Guid actorUserId, Guid targetUserId, CancellationToken token)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == actorUserId && u.RoleCode == UserRoleCode.Supervisor &&
            u.Status == UserStatus.Active && !u.MustChangePassword &&
            _context.Roles.Any(r => r.Code == u.RoleCode && r.IsActive), token))
            throw new PhoneWriteException(UserPhoneNumberWriteStatus.Forbidden);
        if (!await _context.Users.AnyAsync(u => u.Id == targetUserId && u.Status == UserStatus.Active &&
            (u.RoleCode == UserRoleCode.ProjectManager || u.RoleCode == UserRoleCode.DroneOperator ||
             u.RoleCode == UserRoleCode.RepairCrew) &&
            _context.Roles.Any(r => r.Code == u.RoleCode && r.IsActive), token))
            throw new PhoneWriteException(UserPhoneNumberWriteStatus.NotFound);
    }

    private sealed class PhoneWriteException(UserPhoneNumberWriteStatus status) : Exception
    {
        public UserPhoneNumberWriteStatus Status { get; } = status;
    }
}
