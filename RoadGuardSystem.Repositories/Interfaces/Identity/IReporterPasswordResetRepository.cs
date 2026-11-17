using RoadGuardSystem.BusinessObjects.Identity;

namespace RoadGuardSystem.Repositories.Identity;

public sealed record ReporterResetLimits(TimeSpan Lifetime, TimeSpan Cooldown, TimeSpan SendWindow, int MaxSends, int MaxAttempts);

public enum ReporterResetOutcome { Success, InvalidOtp, InvalidPassword }

public interface IReporterPasswordResetRepository
{
    Task<string?> IssueAsync(string normalizedEmail, Func<ApplicationUser, string> fingerprint,
        ReporterResetLimits limits, CancellationToken token);

    Task<ReporterResetOutcome> ResetAsync(string normalizedEmail, Func<ApplicationUser, string> fingerprint,
        Func<ApplicationUser, Task<string?>> hashPassword, ReporterResetLimits limits, CancellationToken token);
}
