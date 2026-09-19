using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace RoadGuardSystem.DTOs.Authentication;

public sealed class LoginRequestDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? Username { get; init; }

    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string? Password { get; init; }

    public JsonElement? DeviceMetadata { get; init; }
}
