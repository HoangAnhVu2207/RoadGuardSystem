using System.ComponentModel.DataAnnotations;

namespace RoadGuardSystem.DTOs.Projects;

public sealed record ReassignPrimaryProjectManagerRequestDto(
    Guid PrimaryProjectManagerUserId,
    [Required] DateOnly? EffectiveFrom,
    [Required, MaxLength(1000)] string Reason,
    [Required] string ExpectedCurrentMembershipRowVersion,
    Guid OperationId);
