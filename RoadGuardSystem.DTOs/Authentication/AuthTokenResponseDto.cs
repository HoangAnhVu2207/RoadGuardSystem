namespace RoadGuardSystem.DTOs.Authentication;

public sealed record AuthTokenResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    string TokenType = "Bearer");
