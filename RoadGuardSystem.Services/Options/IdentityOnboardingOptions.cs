namespace RoadGuardSystem.Services.Options;

public sealed class IdentityOnboardingOptions
{
    public const string SectionName = "IdentityOnboarding";

    public string Secret { get; set; } = string.Empty;

    public int OtpLifetimeMinutes { get; set; } = 10;

    public int OtpMaxAttempts { get; set; } = 5;

    public int OtpResendCooldownSeconds { get; set; } = 60;

    public int InvitationLifetimeHours { get; set; } = 72;

    public string FrontendBaseUrl { get; set; } = string.Empty;

    public string BrandName { get; set; } = "Hoàng Hải Warranty System";

    public string SenderDisplayName { get; set; } = "Công Ty TNHH Xây Dựng Bê Tông Hoàng Hải";

    public string SmtpHost { get; set; } = "smtp.gmail.com";

    public int SmtpPort { get; set; } = 587;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string LogoPath { get; set; } = "EmailAssets/hoanghai-logo.png";
}
