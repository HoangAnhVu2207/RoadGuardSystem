using System.Net.Mail;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;

namespace RoadGuardSystem.Repositories.Identity;

public sealed partial class IdentityRepository
{
    public async Task<UserProfileState?> GetUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserProfileState(
                user.Id,
                user.UserName!,
                user.DisplayName,
                user.Email,
                user.RoleCode,
                user.Status,
                user.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserProfileUpdateResult> UpdateUserProfileAtomicAsync(
        Guid userId,
        string displayName,
        string? email,
        byte[] expectedRowVersion,
        Guid operationId,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedDisplayName = displayName?.Trim();
        var normalizedEmail = NormalizeEmail(email);
        if (userId == Guid.Empty || operationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(normalizedDisplayName) || normalizedDisplayName.Length > 200 ||
            expectedRowVersion.Length == 0 || !IsValidEmail(normalizedEmail))
        {
            return new UserProfileUpdateResult(UserProfileUpdateStatus.InvalidInput);
        }

        var operation = "UserProfileUpdated";
        var idempotencyKey = operationId.ToString();
        var requestFingerprint = HashSha256(
            $"userId:{userId:N};displayName:{normalizedDisplayName};email:{normalizedEmail ?? "<null>"}");
        var existingRecord = await FindIdempotencyRecordAsync(
            userId,
            operation,
            idempotencyKey,
            cancellationToken);
        if (existingRecord is not null)
        {
            return await MapProfileReplayAsync(existingRecord, requestFingerprint, userId, cancellationToken);
        }

        var user = await _context.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return new UserProfileUpdateResult(UserProfileUpdateStatus.UserNotFound);
        }

        if (!user.RowVersion.SequenceEqual(expectedRowVersion))
        {
            return new UserProfileUpdateResult(UserProfileUpdateStatus.StaleConcurrency);
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var beforeSnapshot = JsonSerializer.Serialize(new
                {
                    display_name = user.DisplayName,
                    email = user.Email
                });

                user.DisplayName = normalizedDisplayName;
                user.Email = normalizedEmail;
                user.NormalizedEmail = normalizedEmail?.ToUpperInvariant();

                var now = DateTimeOffset.UtcNow;
                _context.AuditLogs.Add(AuditLog.Create(
                    id: Guid.NewGuid(),
                    actorUserId: userId,
                    occurredAt: now,
                    eventType: "user_profile_updated",
                    entityType: "User",
                    entityId: userId,
                    beforeSnapshot: beforeSnapshot,
                    afterSnapshot: JsonSerializer.Serialize(new
                    {
                        display_name = normalizedDisplayName,
                        email = normalizedEmail
                    }),
                    reason: "Authenticated profile update",
                    source: "IdentityRepository",
                    correlationId: correlationId,
                    snapshotAllowedPropertyNames: ["display_name", "email"]));

                _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
                    actorUserId: userId,
                    projectId: null,
                    operation: operation,
                    idempotencyKey: idempotencyKey,
                    requestFingerprint: requestFingerprint,
                    operationId: operationId,
                    outcomeJson: "{\"status\":\"success\"}",
                    createdAt: now));

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new UserProfileUpdateResult(
                    UserProfileUpdateStatus.Success,
                    new UserProfileState(
                        user.Id,
                        user.UserName!,
                        user.DisplayName,
                        user.Email,
                        user.RoleCode,
                        user.Status,
                        user.RowVersion));
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new UserProfileUpdateResult(UserProfileUpdateStatus.StaleConcurrency);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                await transaction.RollbackAsync(CancellationToken.None);
                var winner = await FindIdempotencyRecordAsync(
                    userId,
                    operation,
                    idempotencyKey,
                    CancellationToken.None);
                return winner is not null
                    ? await MapProfileReplayAsync(winner, requestFingerprint, userId, CancellationToken.None)
                    : new UserProfileUpdateResult(UserProfileUpdateStatus.EmailConflict);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }

    private async Task<UserProfileUpdateResult> MapProfileReplayAsync(
        IdempotencyRecord record,
        string requestFingerprint,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(record.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            return new UserProfileUpdateResult(
                UserProfileUpdateStatus.IdempotentConflict,
                Message: "Idempotency key was previously executed with a different payload.");
        }

        return new UserProfileUpdateResult(
            UserProfileUpdateStatus.IdempotentReplay,
            await GetUserProfileAsync(userId, cancellationToken));
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim();
    }

    private static bool IsValidEmail(string? email)
    {
        if (email is null)
        {
            return true;
        }

        if (email.Length > 254 || !email.Contains('@', StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
