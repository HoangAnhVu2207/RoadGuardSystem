using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RoadGuardSystem.BusinessObjects.Identity;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Services.Options;

namespace RoadGuardSystem.Services.Authentication;

public sealed class ReporterPasswordResetService(IReporterPasswordResetRepository repository, UserManager<ApplicationUser> users,
    IIdentityMessageSender sender, IOptions<IdentityOnboardingOptions> options)
{
    private IdentityOnboardingOptions Settings => options.Value;
    private ReporterResetLimits Limits => new(TimeSpan.FromMinutes(Settings.OtpLifetimeMinutes),
        TimeSpan.FromSeconds(Settings.OtpResendCooldownSeconds), TimeSpan.FromMinutes(Settings.OtpResendWindowMinutes),
        Settings.OtpMaxResendsPerWindow + 1, Settings.OtpMaxAttempts);

    public async Task RequestAsync(string email, CancellationToken token)
    {
        var otp = RandomNumberGenerator.GetInt32(1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var target = await repository.IssueAsync(Normalize(email), user => Fingerprint(user, otp), Limits, token);
        if (target is not null) await sender.SendReporterResetOtpAsync(target, otp, token);
    }

    public Task<ReporterResetOutcome> ConfirmAsync(string email, string otp, string password, CancellationToken token) =>
        repository.ResetAsync(Normalize(email), user => Fingerprint(user, otp), async user =>
        {
            foreach (var validator in users.PasswordValidators)
                if (!(await validator.ValidateAsync(users, user, password)).Succeeded) return null;
            return users.PasswordHasher.HashPassword(user, password);
        }, Limits, token);

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();

    private string Fingerprint(ApplicationUser user, string otp)
    {
        var secret = Convert.FromBase64String(Settings.Secret);
        if (secret.Length < 32) throw new InvalidOperationException("IdentityOnboarding secret is too short.");
        return Convert.ToHexString(HMACSHA256.HashData(secret,
            Encoding.UTF8.GetBytes($"reporter-password-reset:v1:{user.Id:N}:{user.SecurityStamp}:{user.NormalizedEmail}:{otp}")));
    }
}
