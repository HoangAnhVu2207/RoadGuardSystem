namespace RoadGuardSystem.DTOs.Identity;

public sealed record ActorResponseDto(
    Guid Id,
    string DisplayName,
    string Role,
    string Version);
