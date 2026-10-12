namespace RoadGuardSystem.DTOs.Staff;

public sealed record StaffPageResponseDto<T>(IReadOnlyList<T> Items, string? NextCursor, DateTimeOffset AsOf);

public sealed record StaffUserResponseDto(Guid UserId, string DisplayName, string? Email, string? PhoneNumber, string Role, string Status);

// Separate from ActorResponseDto: contacts belong only to Supervisor account reads.
public sealed record StaffAccountResponseDto(Guid Id, string DisplayName, string Role, string Version, string? Email, string? PhoneNumber);

public sealed record ProjectMemberResponseDto(Guid MembershipId, Guid UserId, string DisplayName, string Role, bool IsPrimary,
    string Status, DateOnly ValidFrom, DateOnly? ValidTo, string MembershipVersion);

public sealed record PrimaryProjectManagerResponseDto(PrimaryProjectManagerReadDto? PrimaryProjectManager);

public sealed record PrimaryProjectManagerReadDto(Guid MembershipId, Guid UserId, string DisplayName,
    DateOnly ValidFrom, DateOnly? ValidTo, string MembershipVersion);
