using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Reports;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.Services.Implementations.Reports;

public sealed class ReporterReportService(IAnhHuyProducerService producer, IReporterReportRepository repository,
    IdempotencyOperationService idempotency) : IReporterReportService
{
    public async Task<ReporterReportCommandResult> CreateAsync(Guid actorUserId, UserRoleCode role,
        CreateReporterReportRequestDto request, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (role != UserRoleCode.Reporter || actorUserId == Guid.Empty) return new(ReporterReportCommandStatus.Forbidden);
        if (!IsValid(request, idempotencyKey)) return new(ReporterReportCommandStatus.InvalidInput);

        var preflight = await ResolveEvidenceAsync(actorUserId, role, request.Evidence, cancellationToken);
        if (preflight.Status != ReporterReportCommandStatus.Created) return new(preflight.Status);

        var normalized = request with { Description = request.Description.Trim() };
        var fingerprint = Fingerprint(normalized);
        try
        {
            var execution = await idempotency.ExecuteAsync(actorUserId, null, "huy01.report.create.v1", idempotencyKey,
                fingerprint, async token =>
                {
                    var resolved = await ResolveEvidenceAsync(actorUserId, role, normalized.Evidence, token);
                    if (resolved.Status != ReporterReportCommandStatus.Created) throw new ReporterReportSourceException(resolved.Status);
                    var write = await repository.CreateAndSaveAsync(actorUserId, normalized.Description,
                        resolved.Evidence!.Select(item => item.Reference).ToArray(), token);
                    var response = ToDto(write);
                    return (write.Report.Id, JsonSerializer.Serialize(response));
                }, cancellationToken);

            if (execution.Status == IdempotencyOperationStatus.Conflict) return new(ReporterReportCommandStatus.IdempotencyConflict);
            var stored = JsonSerializer.Deserialize<ReporterReportResponseDto>(execution.OutcomeJson)!;
            return new(execution.Status == IdempotencyOperationStatus.Replayed ? ReporterReportCommandStatus.Replayed : ReporterReportCommandStatus.Created, stored);
        }
        catch (ReporterReportSourceException exception)
        {
            return new(exception.Status);
        }
    }

    private static bool IsValid(CreateReporterReportRequestDto? request, string? idempotencyKey)
        => request is not null && !string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length <= 1000
            && request.Evidence is { Count: > 0 } && request.Evidence.All(item => item.FileId != Guid.Empty && !string.IsNullOrWhiteSpace(item.FileVersion))
            && request.Evidence.Select(item => item.FileId).Distinct().Count() == request.Evidence.Count
            && !string.IsNullOrWhiteSpace(idempotencyKey) && idempotencyKey.Trim().Length <= 200;

    private async Task<(ReporterReportCommandStatus Status, ResolvedEvidenceFacts[]? Evidence)> ResolveEvidenceAsync(Guid actorUserId,
        UserRoleCode role, IReadOnlyList<ReportEvidenceInputDto> evidence, CancellationToken cancellationToken)
    {
        var resolved = new List<ResolvedEvidenceFacts>();
        foreach (var item in evidence)
        {
            var result = await producer.ResolvePrivateEvidenceAsync(actorUserId, role, item.FileId, Guid.NewGuid(), item.FileVersion, cancellationToken);
            if (result.Status != AnhHuyProducerStatus.Ready) return (result.Status switch
            {
                AnhHuyProducerStatus.Forbidden => ReporterReportCommandStatus.Forbidden,
                AnhHuyProducerStatus.NotFound => ReporterReportCommandStatus.NotFound,
                _ => ReporterReportCommandStatus.SourceNotReady
            }, null);
            resolved.Add(result.Facts!);
        }
        return (ReporterReportCommandStatus.Created, resolved.ToArray());
    }

    private static ReporterReportResponseDto ToDto(ReporterReportWriteResult result)
        => new(result.Report.Id, result.Report.Description, result.Report.ReceivedAt, result.Version,
            result.Report.OriginalEvidence.Select(item => item.Id).ToArray());

    private static string Fingerprint(CreateReporterReportRequestDto request)
    {
        var canonical = request.Description + "|" + string.Join(";", request.Evidence.Select(item =>
            $"{item.FileId:D}|{item.FileVersion!.Trim()}|{item.LocationSource?.Trim()}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private sealed class ReporterReportSourceException(ReporterReportCommandStatus status) : Exception
    {
        public ReporterReportCommandStatus Status { get; } = status;
    }
}
