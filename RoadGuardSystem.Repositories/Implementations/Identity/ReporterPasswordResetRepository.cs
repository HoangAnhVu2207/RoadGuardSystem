using System.Data;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Identity;

namespace RoadGuardSystem.Repositories.Implementations.Identity;

public sealed class ReporterPasswordResetRepository(RoadGuardDbContext db, TimeProvider clock) : IReporterPasswordResetRepository
{
    public Task<string?> IssueAsync(string normalizedEmail, Func<ApplicationUser, string> fingerprint,
        ReporterResetLimits limits, CancellationToken token) =>
        WithReporter<string?>(normalizedEmail, null, async (user, now) =>
        {
            var row = await db.Set<ReporterPasswordResetChallenge>().FindAsync([user.Id], token);
            if (row is null)
            {
                row = new ReporterPasswordResetChallenge { UserId = user.Id, WindowStartedAt = now };
                db.Add(row);
            }
            if (now < row.NextSendAt) return null;
            if (now - row.WindowStartedAt >= limits.SendWindow)
            {
                row.WindowStartedAt = now;
                row.SendsInWindow = 0;
            }
            if (row.SendsInWindow >= limits.MaxSends) return null;

            row.SendsInWindow++;
            row.NextSendAt = now + limits.Cooldown;
            row.ExpiresAt = now + limits.Lifetime;
            row.OtpHash = fingerprint(user);
            row.FailedAttempts = 0;
            row.Consumed = false;
            return user.Email;
        }, token);

    public Task<ReporterResetOutcome> ResetAsync(string normalizedEmail, Func<ApplicationUser, string> fingerprint,
        Func<ApplicationUser, Task<string?>> hashPassword, ReporterResetLimits limits, CancellationToken token) =>
        WithReporter(normalizedEmail, ReporterResetOutcome.InvalidOtp, async (user, now) =>
        {
            var row = await db.Set<ReporterPasswordResetChallenge>().FindAsync([user.Id], token);
            if (row is null || row.Consumed || now >= row.ExpiresAt || row.FailedAttempts >= limits.MaxAttempts)
                return ReporterResetOutcome.InvalidOtp;
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(row.OtpHash), Convert.FromHexString(fingerprint(user))))
            {
                row.FailedAttempts++;
                return ReporterResetOutcome.InvalidOtp;
            }

            var newHash = await hashPassword(user);
            if (newHash is null) return ReporterResetOutcome.InvalidPassword;
            row.Consumed = true;
            user.PasswordHash = newHash;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.MustChangePassword = false;
            var sessions = db.Sessions.Where(session => session.UserId == user.Id);
            await sessions.Where(session => session.RevokedAt == null)
                .ExecuteUpdateAsync(update => update.SetProperty(session => session.RevokedAt, now), token);
            await db.RefreshTokens.Where(refresh => refresh.RevokedAt == null && sessions.Any(session => session.Id == refresh.SessionId))
                .ExecuteUpdateAsync(update => update.SetProperty(refresh => refresh.RevokedAt, now), token);
            return ReporterResetOutcome.Success;
        }, token);

    private Task<T> WithReporter<T>(string email, T rejected,
        Func<ApplicationUser, DateTimeOffset, Task<T>> run, CancellationToken token) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var user = await db.Users.FromSqlInterpolated($"""
                SELECT * FROM [Users] WITH (UPDLOCK, HOLDLOCK)
                WHERE [NormalizedEmail] = {email}
                """).SingleOrDefaultAsync(token);
            if (user is not { RoleCode: UserRoleCode.Reporter, Status: UserStatus.Active, EmailConfirmed: true, Email: not null } ||
                !await db.Roles.AnyAsync(role => role.Code == UserRoleCode.Reporter && role.IsActive, token))
                return rejected;
            var result = await run(user, clock.GetUtcNow());
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return result;
        });
}
