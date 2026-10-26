namespace RoadGuardSystem.DTOs.Cases;

public sealed record PublicPublicationDto(Guid PublicId, string DamageCategory, string PublicStatus, DateTimeOffset PublishedAt);
