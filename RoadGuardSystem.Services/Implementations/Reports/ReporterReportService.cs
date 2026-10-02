using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Reports;
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
        CreateReporterReportRequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default)
    {
        if (role != UserRoleCode.Reporter || actorUserId == Guid.Empty) return new(ReporterReportCommandStatus.Forbidden);
        if (!ReporterIntakeRequestNormalizer.TryNormalize(request, idempotencyKey, out var normalized, out var validationErrors))
            return new(ReporterReportCommandStatus.InvalidInput, null, validationErrors);

        var preflight = await ResolveEvidenceAsync(actorUserId, role, normalized!.Evidence, cancellationToken);
        if (preflight.Status != ReporterReportCommandStatus.Created) return new(preflight.Status);
        try
        {
            var execution = await idempotency.ExecuteAsync(actorUserId, null, "huy01.report.create.v1", normalized!.IdempotencyKey,
                normalized.Fingerprint, async token =>
                {
                    var resolved = await ResolveEvidenceAsync(actorUserId, role, normalized.Evidence, token);
                    if (resolved.Status != ReporterReportCommandStatus.Created) throw new ReporterReportSourceException(resolved.Status);
                    var write = await repository.CreateAndSaveAsync(actorUserId, normalized.Description,
                        resolved.Evidence!.Select(item => item.Reference).ToArray(), correlationId, token);
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
        catch (ReporterIntakeFactsException exception)
        {
            return new(exception.Status switch
            {
                ReporterIntakeFactsStatus.Forbidden => ReporterReportCommandStatus.Forbidden,
                ReporterIntakeFactsStatus.NotFound => ReporterReportCommandStatus.NotFound,
                ReporterIntakeFactsStatus.StaleFile => ReporterReportCommandStatus.StaleFile,
                _ => ReporterReportCommandStatus.SourceNotReady
            });
        }
    }

    private async Task<(ReporterReportCommandStatus Status, ResolvedEvidenceFacts[]? Evidence)> ResolveEvidenceAsync(Guid actorUserId,
        UserRoleCode role, IReadOnlyList<NormalizedReporterEvidence> evidence, CancellationToken cancellationToken)
    {
        var resolved = new List<ResolvedEvidenceFacts>();
        foreach (var item in evidence)
        {
            var result = await producer.ResolvePrivateEvidenceAsync(actorUserId, role, item.FileId, Guid.NewGuid(), item.FileVersion, cancellationToken);
            if (result.Status != AnhHuyProducerStatus.Ready) return (result.Status switch
            {
                AnhHuyProducerStatus.Forbidden => ReporterReportCommandStatus.Forbidden,
                AnhHuyProducerStatus.NotFound => ReporterReportCommandStatus.NotFound,
                AnhHuyProducerStatus.StaleFile => ReporterReportCommandStatus.StaleFile,
                _ => ReporterReportCommandStatus.SourceNotReady
            }, null);
            var facts = result.Facts!;
            resolved.Add(new ResolvedEvidenceFacts(VerifiedEvidenceReference.Create(facts.Reference.EvidenceId, facts.Reference.FileId,
                facts.Reference.FileVersion, facts.Reference.OwnerUserId, item.CaptureMetadata), facts.ProjectId, facts.Purpose,
                facts.ChecksumSha256, facts.SizeBytes, facts.MediaType, facts.UploadedAt));
        }
        return (ReporterReportCommandStatus.Created, resolved.ToArray());
    }

    private static ReporterReportResponseDto ToDto(ReporterReportWriteResult result)
        => new(result.Report.Id, result.Report.Description, result.Report.ReceivedAt, result.Version,
            result.Report.OriginalEvidence.Select(item => item.Id).ToArray());

    private sealed class ReporterReportSourceException(ReporterReportCommandStatus status) : Exception
    {
        public ReporterReportCommandStatus Status { get; } = status;
    }
}
