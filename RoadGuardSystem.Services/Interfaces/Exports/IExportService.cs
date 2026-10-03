using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Services.Exports;

public sealed record ExportResult<T>(string Code, T? Value = default);
public sealed record ExportContentDto(Stream Content, string MediaType, string FileName, string Sha256);
public interface IExportService
{
    Task<ExportResult<ExportJobViewDto>> CreateAsync(Guid actorId, Guid projectId, CreateExportRequestDto request, string key, Guid? correlationId, CancellationToken ct);
    Task<ExportResult<ExportJobViewDto>> GetAsync(Guid actorId, Guid projectId, Guid exportId, CancellationToken ct);
    Task<ExportResult<ExportManifestV1Dto>> ManifestAsync(Guid actorId, Guid projectId, Guid exportId, CancellationToken ct);
    Task<ExportResult<ExportContentDto>> ContentAsync(Guid actorId, Guid projectId, Guid exportId, CancellationToken ct);
    Task<bool> ProcessNextAsync(CancellationToken ct);
}
public sealed class ExportOptions
{
    public string UnicodeFontPath { get; set; } = "";
    public int WorkerPollSeconds { get; set; } = 5;
}
