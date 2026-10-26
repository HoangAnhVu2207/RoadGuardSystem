using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Cases;
namespace RoadGuardSystem.Services.Cases;

public sealed record PublicPublicationResult(int Status, string? Code = null, PublicPublicationDto? Publication = null);
public interface IPublicPublicationService
{
    Task<PublicPublicationResult> CreateAsync(Guid actor, UserRoleCode role, Guid project, Guid defect, string key, CancellationToken ct);
    Task<PublicPublicationResult> WithdrawAsync(Guid actor, UserRoleCode role, Guid project, Guid id, string key, CancellationToken ct);
    Task<PublicPublicationDto?> ReadAsync(Guid id, CancellationToken ct);
}
