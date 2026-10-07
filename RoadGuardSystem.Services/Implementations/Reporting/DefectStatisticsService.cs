using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Reporting;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Reporting;
namespace RoadGuardSystem.Services.Reporting;

public sealed class DefectStatisticsService(IDefectStatisticsRepository repository) : IDefectStatisticsService
{
    public Task<DefectStatisticsResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, ReportingFiltersDto filters, CancellationToken token)
        => repository.ReadAsync(actor, role, project, filters, token);
    public async Task<DefectStatisticsResult> ConfirmAsync(Guid actor, UserRoleCode role, Guid project, DefectStatisticsInputDto input,
        string? key, string? version, CancellationToken token)
    {
        if (input is null) return new(400, "validation_error");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version)) return new(428, "precondition_required");
        if (key.Length > 150 || key.Any(c => c < 33 || c > 126) || version.Length != 66 || version[0] != '"' || version[^1] != '"' ||
            version[1..^1].Any(c => !char.IsAsciiHexDigit(c) || char.IsUpper(c))) return new(400, "validation_error");
        return await repository.ConfirmAsync(new(actor, role, project, input.ToFact(), key, version[1..^1]), token);
    }
}
