using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.Services.Implementations.Repairs;

public sealed class RoadCoverageService(IRoadCoverageRepository repository) : IRoadCoverageService
{
    public async Task<RoadCoverageServiceResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token)
    {
        if (actor == Guid.Empty || project == Guid.Empty) return new(400, "validation_error");
        var result = await repository.ReadAsync(actor, role, project, token); return new(result.Status, result.Code, result.Value);
    }
    public async Task<RoadCoverageServiceResult> ConfirmAsync(Guid actor, UserRoleCode role, Guid project, RoadCoverageInputDto input,
        string? key, string? version, CancellationToken token)
    {
        if (actor == Guid.Empty || project == Guid.Empty || input is null) return new(400, "validation_error");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version)) return new(428, "precondition_required");
        if (key.Length > 150 || key.Any(c => c < 33 || c > 126) || version.Length != 66 || version[0] != '"' || version[^1] != '"' ||
            version[1..^1].Any(c => !char.IsAsciiHexDigit(c) || char.IsUpper(c))) return new(400, "validation_error");
        var result = await repository.ConfirmAsync(new(actor, role, project, input.ToFact(), key, version[1..^1]), token);
        return new(result.Status, result.Code, result.Value);
    }
}
