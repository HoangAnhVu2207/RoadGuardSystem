namespace RoadGuardSystem.DTOs.Identity;

public sealed record AdminPasswordResetResponseDto(
    Guid UserId,
    bool MustChangePassword,
    string? TemporaryPassword);
