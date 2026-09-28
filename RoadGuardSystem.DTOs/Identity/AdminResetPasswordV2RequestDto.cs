using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AdminResetPasswordV2RequestDto
{
    [Required, MinLength(1)]
    public string? TemporaryPassword { get; init; }

    [Required, MinLength(1)]
    public string? Reason { get; init; }
}
