namespace RoadGuardSystem.DTOs.Authentication;

public sealed record AuthTokenResponseDto(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    bool MustChangePassword,
    AuthActorResponseDto User);
