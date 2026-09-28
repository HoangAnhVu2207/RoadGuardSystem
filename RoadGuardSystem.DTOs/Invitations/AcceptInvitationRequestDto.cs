using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RoadGuardSystem.DTOs.Invitations;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AcceptInvitationRequestDto
{
    [Required, MinLength(1)]
    public string? InvitationToken { get; init; }

    [Required, MinLength(1)]
    public string? DisplayName { get; init; }

    [Required, MinLength(1)]
    public string? Password { get; init; }
}
