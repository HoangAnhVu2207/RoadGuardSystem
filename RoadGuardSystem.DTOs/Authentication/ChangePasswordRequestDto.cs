using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Authentication;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChangePasswordRequestDto
{
    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string? CurrentPassword { get; init; }

    [Required]
    [StringLength(1024, MinimumLength = 1)]
    public string? NewPassword { get; init; }
}
