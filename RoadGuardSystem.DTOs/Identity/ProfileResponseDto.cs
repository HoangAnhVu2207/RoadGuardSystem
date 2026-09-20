namespace RoadGuardSystem.DTOs.Identity;

public sealed record ProfileResponseDto(
    Guid UserId,
    string Username,
    string DisplayName,
    string? Email,
    string RoleCode,
    string Status,
    string RowVersion);
