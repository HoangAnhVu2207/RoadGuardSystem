using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Identity;

public sealed class ProfileUpdateRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string? DisplayName { get; init; }

    [StringLength(254)]
    public string? Email { get; init; }

    [Required]
    public string? ExpectedRowVersion { get; init; }

    public Guid OperationId { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; init; }
}
