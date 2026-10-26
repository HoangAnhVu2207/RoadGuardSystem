using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AdminPasswordResetRequestDto
{
    [Required]
    public string? ExpectedTargetRowVersion { get; init; }

    public Guid OperationId { get; init; }
}
