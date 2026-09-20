using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Identity;

public sealed class AdminPasswordResetRequestDto
{
    [Required]
    public string? ExpectedTargetRowVersion { get; init; }

    public Guid OperationId { get; init; }
}
