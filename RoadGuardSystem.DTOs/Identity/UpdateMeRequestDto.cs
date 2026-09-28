using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateMeRequestDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string? DisplayName { get; init; }
}
