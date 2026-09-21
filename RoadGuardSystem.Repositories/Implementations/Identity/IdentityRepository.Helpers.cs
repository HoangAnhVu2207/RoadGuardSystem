using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Idempotency;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private Task<IdempotencyRecord?> FindIdempotencyRecordAsync(
        Guid? actorUserId,
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        _context.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                record =>
                    record.ActorUserId == actorUserId &&
                    record.ProjectId == null &&
                    record.Operation == operation &&
                    record.IdempotencyKey == idempotencyKey,
                cancellationToken);

    private static ForcedPasswordChangeResult MapForcedPasswordChangeReplay(
        IdempotencyRecord record,
        string requestFingerprint) =>
        new(record.RequestFingerprint == requestFingerprint
            ? ForcedPasswordChangeStatus.IdempotentReplay
            : ForcedPasswordChangeStatus.IdempotentConflict);

    private static string HashSha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
