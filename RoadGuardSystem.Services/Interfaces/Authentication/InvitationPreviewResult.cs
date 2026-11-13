namespace RoadGuardSystem.Services.Authentication;

public sealed record InvitationPreviewProject(Guid Id, string Name);

public sealed record InvitationPreviewResult(
    IdentityOnboardingStatus Status,
    string? Role = null,
    IReadOnlyList<InvitationPreviewProject>? Projects = null,
    DateTimeOffset? ExpiresAt = null,
    string? InviterName = null);
