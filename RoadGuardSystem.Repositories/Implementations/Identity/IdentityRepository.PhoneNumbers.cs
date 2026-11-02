using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.Repositories.Idempotency;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<(string? PhoneNumber, bool Confirmed, byte[] Version)?> GetSelfPhoneNumberAsync(Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking()
            .Where(u => u.Id == actorUserId && u.Status == UserStatus.Active && !u.MustChangePassword &&
                _context.Roles.Any(r => r.Code == u.RoleCode && r.IsActive))
            .Select(u => new { u.PhoneNumber, u.PhoneNumberConfirmed, u.RowVersion })
            .SingleOrDefaultAsync(cancellationToken);
        return user is null ? null : (user.PhoneNumber, user.PhoneNumberConfirmed, user.RowVersion);
    }

    public async Task<UserPhoneNumberWriteStatus> UpdateStaffPhoneNumberAtomicAsync(Guid actorUserId, Guid targetUserId,
        string phoneNumber, byte[] expectedRowVersion, string idempotencyKey, CancellationToken cancellationToken = default)
        => await UpdatePhoneNumberAtomicAsync(actorUserId, targetUserId, phoneNumber, expectedRowVersion,
            idempotencyKey, false, cancellationToken);

    public Task<UserPhoneNumberWriteStatus> UpdateSelfPhoneNumberAtomicAsync(Guid actorUserId,
        string phoneNumber, byte[] expectedRowVersion, string idempotencyKey, CancellationToken cancellationToken = default)
        => UpdatePhoneNumberAtomicAsync(actorUserId, actorUserId, phoneNumber, expectedRowVersion,
            idempotencyKey, true, cancellationToken);

    private async Task<UserPhoneNumberWriteStatus> UpdatePhoneNumberAtomicAsync(Guid actorUserId, Guid targetUserId,
        string phoneNumber, byte[] expectedRowVersion, string idempotencyKey, bool self, CancellationToken cancellationToken)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{targetUserId:N}:{phoneNumber}:{Convert.ToBase64String(expectedRowVersion)}"))).ToLowerInvariant();
        var idempotency = new IdempotencyOperationService(_context);
        try
        {
            var result = await idempotency.ExecuteSerializableAsync(actorUserId, null,
                self ? "SelfPhoneNumberUpdated" : "StaffPhoneNumberUpdated",
                idempotencyKey, fingerprint, async token =>
                {
                    await CheckPhoneAuthorityAsync(actorUserId, targetUserId, self, token);
                    var target = await _context.Users.SingleAsync(u => u.Id == targetUserId, token);
                    if (!target.RowVersion.SequenceEqual(expectedRowVersion))
                        throw new PhoneWriteException(UserPhoneNumberWriteStatus.StaleVersion);
                    if (!string.Equals(target.PhoneNumber, phoneNumber, StringComparison.Ordinal))
                    {
                        target.PhoneNumber = phoneNumber;
                        target.PhoneNumberConfirmed = false;
                        _context.AuditLogs.Add(AuditLog.Create(Guid.NewGuid(), actorUserId, _timeProvider.GetUtcNow(),
                            self ? "self_phone_number_updated" : "staff_phone_number_updated", "User", targetUserId, null, null,
                            self ? "Self contact update" : "Supervisor staff contact update", self ? "ME_API" : "USERS_API", null));
                        await _context.SaveChangesAsync(token);
                    }
                    return (targetUserId, "{}");
                }, cancellationToken, token => CheckPhoneAuthorityAsync(actorUserId, targetUserId, self, token));
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

    private Task<bool> HasCurrentPhoneSelfAuthorityAsync(Guid actorUserId, CancellationToken token) =>
        _context.Users.AnyAsync(u => u.Id == actorUserId && u.Status == UserStatus.Active &&
            !u.MustChangePassword && _context.Roles.Any(r => r.Code == u.RoleCode && r.IsActive), token);

    private async Task CheckPhoneAuthorityAsync(Guid actorUserId, Guid targetUserId, bool self, CancellationToken token)
    {
        if (self)
        {
            if (actorUserId != targetUserId || !await HasCurrentPhoneSelfAuthorityAsync(actorUserId, token))
                throw new PhoneWriteException(UserPhoneNumberWriteStatus.Forbidden);
            return;
        }
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
