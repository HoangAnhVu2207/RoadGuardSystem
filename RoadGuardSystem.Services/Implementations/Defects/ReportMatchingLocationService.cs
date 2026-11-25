using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Candidates;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Implementations.Defects;

public sealed class ReportMatchingLocationService(ICandidateDecisionRepository candidates,
    IAnhHuyFactsRepository facts, IAnhHuyProducerService producer, ICaseWorkflowRepository cases,
    IProjectScopeGuard scope, IReportMatchingLocationRepository locations, IdempotencyOperationService receipts,
    TimeProvider clock) : IReportMatchingLocationService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private sealed class ExistingReceipt : Exception { }

    public async Task<ReportMatchingLocationResult> ReadAsync(Guid actor, UserRoleCode role, Guid project,
        Guid report, CancellationToken token)
    {
        if (role != UserRoleCode.ProjectManager) return new(403, "access_forbidden");
        try
        {
            return await candidates.ReadConsistentlyAsync(async ct =>
            {
                var source = await Resolve(actor, role, project, report, ct);
                var row = await facts.GetReportSourceAsync(report, ct);
                return new ReportMatchingLocationResult(200, Value: View(report, source, row!));
            }, token);
        }
        catch (CaseWorkflowException error) { return new(error.Status, error.Code); }
    }

    public async Task<ReportMatchingLocationResult> ConfirmAsync(Guid actor, UserRoleCode role, Guid project,
        Guid report, ReportMatchingLocationInput input, string? key, CancellationToken token)
    {
        if (role != UserRoleCode.ProjectManager) return new(403, "access_forbidden");
        if (string.IsNullOrWhiteSpace(key)) return new(428, "precondition_required");
        if (actor == Guid.Empty || project == Guid.Empty || report == Guid.Empty || key.Length > 150 || key.Any(c => c is < '!' or > '~') ||
            input is null || string.IsNullOrWhiteSpace(input.SourceVersion) || string.IsNullOrWhiteSpace(input.GeometryVersion) ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 2000 || input.SegmentIds is null ||
            input.SegmentIds.Length > 200 || input.SegmentIds.Any(id => id == Guid.Empty) ||
            input.SegmentIds.Distinct().Count() != input.SegmentIds.Length || input.EvidenceId == Guid.Empty ||
            input.EvidenceId is null && input.SegmentIds.Length == 0) return new(400, "validation_error");
        var normalized = input with { Reason = input.Reason.Trim(), SegmentIds = input.SegmentIds.Order().ToArray() };
        var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { report, input = normalized }, Json))).ToLowerInvariant();
        try
        {
            var receipt = await receipts.ExecuteSerializableAsync(actor, project, "matching.location.confirm.v1", key, fingerprint,
                async ct =>
                {
                    var source = await Resolve(actor, role, project, report, ct);
                    await candidates.LockSourceAsync(report, source.CaseId, ct);
                    if (await locations.ReceiptExistsAsync(actor, project, key, ct)) throw new ExistingReceipt();
                    source = await Resolve(actor, role, project, report, ct);
                    if (normalized.SourceVersion != source.DomainFacts.Source.SourceVersion || normalized.GeometryVersion != source.Geometry.Version)
                        throw new CaseWorkflowException(409, "candidate_stale");
                    if (normalized.SegmentIds.Any(id => !source.Geometry.Segments.Any(s => s.Id == id)))
                        throw new CaseWorkflowException(409, "matching_scope_conflict");
                    var row = (await facts.GetReportSourceAsync(report, ct))!;
                    var evidence = normalized.EvidenceId is Guid evidenceId ? row.Evidence.SingleOrDefault(e => e.EvidenceId == evidenceId) : null;
                    if (normalized.EvidenceId is not null && (evidence is null || evidence.Latitude is null || evidence.Longitude is null))
                        throw new CaseWorkflowException(409, "matching_evidence_location_unavailable");
                    var location = ReportMatchingLocation.Create(report, (row.MatchingLocation?.Sequence ?? 0) + 1,
                        row.CaseId, project, actor, clock.GetUtcNow(), normalized.Reason, row.RouteVersionId!.Value,
                        row.SegmentSetId!.Value, source.Geometry.Version, row.ReportVersion, row.CaseVersion,
                        normalized.EvidenceId, normalized.SegmentIds, evidence?.Latitude, evidence?.Longitude, evidence?.AccuracyMeters);
                    await locations.SaveAsync(location, ct);
                    var updated = await Resolve(actor, role, project, report, ct);
                    return (location.Id, JsonSerializer.Serialize(View(report, updated, (await facts.GetReportSourceAsync(report, ct))!), Json));
                }, token, async ct =>
                {
                    // Receipt replay owns a short read transaction. Recheck and lock
                    // current Case/source authority without starting a nested snapshot.
                    var current = await Resolve(actor, role, project, report, ct);
                    await candidates.LockSourceAsync(report, current.CaseId, ct);
                });
            return receipt.Status == IdempotencyOperationStatus.Conflict ? new(409, "idempotency_key_reused")
                : new(200, Value: JsonSerializer.Deserialize<ReportMatchingLocationView>(receipt.OutcomeJson, Json));
        }
        catch (ExistingReceipt) { return await ConfirmAsync(actor, role, project, report, input, key, token); }
        catch (CaseWorkflowException error) { return new(error.Status, error.Code); }
        catch (ArgumentException) { return new(400, "validation_error"); }
    }

    private async Task<ResolvedCandidateSourceFacts> Resolve(Guid actor, UserRoleCode role, Guid project, Guid report, CancellationToken ct)
    {
        var result = await producer.ResolveCandidateSourceAsync(actor, role, project, CandidateSourceKind.Report, report, cancellationToken: ct);
        if (result.Status != AnhHuyProducerStatus.Ready)
            throw new CaseWorkflowException(result.Status == AnhHuyProducerStatus.Forbidden ? 403 : result.Status == AnhHuyProducerStatus.NotFound ? 404 : 409,
                result.Status == AnhHuyProducerStatus.Forbidden ? "access_forbidden" : result.Status == AnhHuyProducerStatus.NotFound ? "not_found" : "source_not_ready");
        await cases.GuardAsync(actor, role, [result.Facts!.CaseId], project,
            async (id, token) => await scope.AuthorizeAsync(actor, role, id, token) is not null, ct);
        return result.Facts;
    }

    private static ReportMatchingLocationView View(Guid report, ResolvedCandidateSourceFacts source, ReportCandidateFacts row)
        => new(report, source.DomainFacts.Source.SourceVersion, source.Geometry.Version, source.MatchingLocationStatus,
            row.MatchingLocation is { } location && location.ProjectId == source.DomainFacts.ProjectId ? new(location.Id, location.Sequence, location.ActorId, location.ConfirmedAt,
                location.Reason, location.EvidenceId, JsonSerializer.Deserialize<Guid[]>(location.SegmentIdsJson)!,
                location.Latitude, location.Longitude, location.AccuracyMeters) : null,
            row.Evidence.Select(e => new ReportMatchingEvidenceView(e.EvidenceId, e.Latitude, e.Longitude, e.AccuracyMeters, e.LocationSource)).ToArray());
}
