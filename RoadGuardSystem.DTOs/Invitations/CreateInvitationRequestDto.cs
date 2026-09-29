using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Invitations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateInvitationRequestDto
{
    [Required, EmailAddress]
    public string? Email { get; init; }

    [Required, MinLength(1)]
    public string? Role { get; init; }

    [Required]
    public IReadOnlyList<Guid>? ProjectIds { get; init; }
}
