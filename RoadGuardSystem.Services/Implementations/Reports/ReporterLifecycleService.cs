using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Implementations.Reports;
using RoadGuardSystem.Repositories.Reports;
using RoadGuardSystem.Services.Files;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.Services.Implementations.Reports;

public sealed class ReporterLifecycleService(IReporterLifecycleRepository repository, IAnhHuyProducerService producer,
    IReporterEvidenceService files, IdempotencyOperationService idempotency) : IReporterLifecycleService
{
    public Task<ReporterLifecycleResult> ReadAsync(Guid actorId, UserRoleCode role, Guid reportId, CancellationToken ct)
        => RunAsync(role, async () => new(200, Report: await repository.ReadAsync(actorId, reportId, ct)));

    public Task<ReporterLifecycleResult> ListAsync(Guid actorId, UserRoleCode role, int pageSize, string? cursor, CancellationToken ct)
        => RunAsync(role, async () => pageSize is < 1 or > 100 ? new(400, "validation_error",
            Errors: new Dictionary<string, string[]> { ["pageSize"] = ["pageSize must be 1..100."] })
            : new(200, Page: await repository.ListAsync(actorId, pageSize, cursor, ct)));

    public Task<ReporterLifecycleResult> SupplementAsync(Guid actorId, UserRoleCode role, Guid reportId,
        CreateReporterReportRequestDto request, string key, string expectedVersion, Guid? correlationId, CancellationToken ct)
        => RunAsync(role, async () =>
        {
            if (!ReporterIntakeRequestNormalizer.TryNormalize(request, key, out var normalized, out var errors))
                return new(400, "validation_error", Errors: errors);
            await repository.ReadAsync(actorId, reportId, ct);
            var evidence = await ResolveAsync(actorId, normalized!.Evidence, ct);
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { reportId, expectedVersion, normalized.Fingerprint }))).ToLowerInvariant();
            var result = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteAsync(actorId, null, "huy01.report.supplement.v1", normalized.IdempotencyKey,
                fingerprint, handler, ct, receiptAccessGuard: token => repository.GuardAsync(actorId, reportId, evidence, token)), async token =>
                {
                    var fresh = await ResolveAsync(actorId, normalized.Evidence, token);
                    var report = await repository.SupplementAsync(actorId, reportId, expectedVersion,
                        normalized.Description, fresh, correlationId, token);
                    return (report.Id, JsonSerializer.Serialize(report));
                }, exception => exception is ReporterLifecycleException { Status: 412 });
            return result.Status == IdempotencyOperationStatus.Conflict ? new(409, "idempotency_key_reused")
                : new(200, Report: JsonSerializer.Deserialize<OwnReportDto>(result.OutcomeJson));
        });

    public async Task<UploadServiceResult> DownloadAsync(Guid actorId, UserRoleCode role, Guid reportId, Guid evidenceId, CancellationToken ct)
    {
        if (role != UserRoleCode.Reporter) return new(UploadServiceStatus.Forbidden);
        try
        {
            var reference = await repository.ResolveDownloadAsync(actorId, reportId, evidenceId, ct);
            if (reference.PublicationId is null)
            {
                var resolved = await producer.ResolvePrivateEvidenceAsync(actorId, role, reference.FileId, evidenceId, reference.FileVersion, ct);
                if (resolved.Status != AnhHuyProducerStatus.Ready) return new(resolved.Status switch
                {
                    AnhHuyProducerStatus.Forbidden => UploadServiceStatus.Forbidden,
                    AnhHuyProducerStatus.SourceNotReady => UploadServiceStatus.Conflict,
                    AnhHuyProducerStatus.StaleFile => UploadServiceStatus.PreconditionFailed,
                    _ => UploadServiceStatus.NotFound
                });
            }
            return reference.PublicationId is Guid publication
                ? await files.DownloadPublicationAsync(actorId, role, publication, reportId, evidenceId, ct)
                : await files.DownloadAsync(actorId, role, reference.FileId, ct);
        }
        catch (ReporterLifecycleException e) { return new(e.Status == 403 ? UploadServiceStatus.Forbidden : UploadServiceStatus.NotFound); }
        catch (ReporterIntakeFactsException) { return new(UploadServiceStatus.Forbidden); }
    }

    private async Task<VerifiedEvidenceReference[]> ResolveAsync(Guid actor, IReadOnlyList<NormalizedReporterEvidence> inputs, CancellationToken ct)
    {
        var evidence = new List<VerifiedEvidenceReference>();
        foreach (var item in inputs)
        {
            var result = await producer.ResolvePrivateEvidenceAsync(actor, UserRoleCode.Reporter, item.FileId, Guid.NewGuid(), item.FileVersion, ct);
            if (result.Status != AnhHuyProducerStatus.Ready) throw new ReporterLifecycleException(
                result.Status == AnhHuyProducerStatus.Forbidden ? 403 : result.Status == AnhHuyProducerStatus.NotFound ? 404 : result.Status == AnhHuyProducerStatus.StaleFile ? 412 : 409,
                result.Status == AnhHuyProducerStatus.Forbidden ? "access_forbidden" : result.Status == AnhHuyProducerStatus.NotFound ? "not_found" : result.Status == AnhHuyProducerStatus.StaleFile ? "concurrency_conflict" : "source_not_ready");
            var facts = result.Facts!.Reference;
            evidence.Add(VerifiedEvidenceReference.Create(facts.EvidenceId, facts.FileId, facts.FileVersion, facts.OwnerUserId, item.CaptureMetadata));
        }
        return evidence.ToArray();
    }

    private static async Task<ReporterLifecycleResult> RunAsync(UserRoleCode role, Func<Task<ReporterLifecycleResult>> action)
    {
        if (role != UserRoleCode.Reporter) return new(403, "access_forbidden");
        try { return await action(); }
        catch (ReporterLifecycleException e) { return new(e.Status, e.Code); }
        catch (ReporterIntakeFactsException e)
        {
            return new(e.Status == ReporterIntakeFactsStatus.StaleFile ? 412 : e.Status == ReporterIntakeFactsStatus.NotFound ? 404 : e.Status == ReporterIntakeFactsStatus.Forbidden ? 403 : 409,
            e.Status == ReporterIntakeFactsStatus.StaleFile ? "concurrency_conflict" : e.Status == ReporterIntakeFactsStatus.NotFound ? "not_found" : e.Status == ReporterIntakeFactsStatus.Forbidden ? "access_forbidden" : "source_not_ready");
        }
    }
}
