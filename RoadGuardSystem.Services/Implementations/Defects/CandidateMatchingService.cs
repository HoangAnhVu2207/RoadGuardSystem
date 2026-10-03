using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Candidates;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Implementations.Defects;

public sealed class CandidateMatchingService(ICandidateDecisionRepository repository,
    ICaseWorkflowRepository cases, IAnhHuyProducerService producer, IAiCandidateFactsReader ai,
    IProjectScopeGuard scope)
    : ICandidateMatchingService
{
    private sealed record Cursor(Guid ProjectId, Guid SourceId, string SourceVersion,
        string GeometryVersion, bool Expand, string TargetHash, int Offset);

    public async Task<CandidateMatchResult> MatchAsync(Guid actor, UserRoleCode role, Guid project,
        string? sourceKind, Guid sourceId, bool expand, int pageSize, string? cursor,
        CancellationToken token)
    {
        if (role != UserRoleCode.ProjectManager) return new(403, "access_forbidden");
        if (project == Guid.Empty || sourceId == Guid.Empty || pageSize is < 1 or > 200 ||
            sourceKind is not ("REPORT" or "AI_DETECTION" or "FIELD_OBSERVATION"))
            return new(400, "validation_error");
        if (sourceKind == "FIELD_OBSERVATION") return new(409, "source_not_ready");
        try
        {
            return await repository.ReadConsistentlyAsync(async ct =>
            {
                var initialReport = sourceKind == "REPORT" ? await producer.ResolveCandidateSourceAsync(actor, role, project,
                    CandidateSourceKind.Report, sourceId, cancellationToken: ct) : null;
                var initialAi = sourceKind == "AI_DETECTION" ? await ai.ResolveAsync(actor, role, project, sourceId,
                    cancellationToken: ct) : null;
                var initialStatus = initialReport?.Status ?? initialAi!.Status;
                if (initialStatus != AnhHuyProducerStatus.Ready) return Failure(initialStatus);
                await cases.GuardAsync(actor, role, initialReport is null ? [] : [initialReport.Facts!.CaseId], project,
                    async (p, inner) => await scope.AuthorizeAsync(actor, role, p, inner) is not null, ct);
                if (initialReport is not null) await repository.LockSourceAsync(sourceId, initialReport.Facts!.CaseId, ct);
                else await repository.LockAiSourceAsync(sourceId, ct);
                var currentReport = initialReport is null ? null : await producer.ResolveCandidateSourceAsync(actor, role, project,
                    CandidateSourceKind.Report, sourceId, cancellationToken: ct);
                var currentAi = initialAi is null ? null : await ai.ResolveAsync(actor, role, project, sourceId,
                    cancellationToken: ct);
                var currentStatus = currentReport?.Status ?? currentAi!.Status;
                if (currentStatus != AnhHuyProducerStatus.Ready) return Failure(currentStatus);
                var sourceVersion = currentReport?.Facts!.DomainFacts.Source.SourceVersion ?? currentAi!.Facts!.SourceVersion;
                var geometryVersion = currentReport?.Facts!.DomainFacts.GeometryVersion ?? currentAi!.Facts!.GeometryVersion;
                var geometry = currentReport?.Facts!.Geometry;
                if (currentAi is not null)
                {
                    var resolved = await producer.ResolveGeometryAsync(actor, role, project, currentAi.Facts!.RouteVersionId,
                        currentAi.Facts.SegmentSetId, currentAi.Facts.GeometryVersion, true, ct);
                    if (resolved.Status != AnhHuyProducerStatus.Ready) return Failure(resolved.Status);
                    geometry = resolved.Facts;
                }
                var assigned = currentAi?.Facts?.SegmentId is Guid segment ? new[] { segment } : [];
                var neighbors = geometry!.Segments.Where(item => assigned.Contains(item.Id))
                    .SelectMany(item => new[] { item.PreviousId, item.NextId }).Where(id => id.HasValue)
                    .Select(id => id!.Value).ToArray();
                var targets = await repository.MatchTargetsAsync(project, ct);
                var targetHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                    targets.OrderBy(target => target.DefectId).Select(target => new
                    {
                        target.DefectId, target.Version, target.RouteVersionId,
                        target.SegmentId, target.LinkedReportIds
                    })))).ToLowerInvariant();
                var offset = 0;
                if (cursor is not null)
                {
                    Cursor? decoded;
                    try { decoded = JsonSerializer.Deserialize<Cursor>(Convert.FromBase64String(cursor)); }
                    catch (Exception error) when (error is FormatException or JsonException) { return new(400, "validation_error"); }
                    if (decoded is null || decoded.ProjectId != project || decoded.SourceId != sourceId ||
                        decoded.Expand != expand || decoded.Offset < 0) return new(400, "validation_error");
                    if (decoded.SourceVersion != sourceVersion ||
                        decoded.GeometryVersion != geometryVersion ||
                        decoded.TargetHash != targetHash) return new(409, "candidate_stale");
                    offset = decoded.Offset;
                }
                var matched = CandidateMatcher.Match(project, assigned, neighbors, targets.Select(target => new CandidateMatchFact(
                    target.DefectId, target.ProjectId, target.Version, target.SegmentId, null,
                    target.LinkedReportIds.Contains(sourceId))).ToArray(), expand, metricGpsAvailable: false);
                if (offset > matched.Count) return new(400, "validation_error");
                var page = matched.Skip(offset).Take(pageSize).Select(item =>
                {
                    var target = targets.Single(target => target.DefectId == item.DefectId);
                    return new CandidateMatchItemDto(item.DefectId, item.Version, item.SegmentId,
                        item.PriorityGroup, item.DistanceMeters, null, item.ReasonCodes,
                        target.LinkedReportIds.Contains(sourceId) && currentReport is not null ? currentReport.Facts!.EvidenceIds : [],
                        new(target.LinkedReportIds.Count));
                }).ToArray();
                var next = offset + page.Length < matched.Count
                    ? Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new Cursor(project, sourceId,
                        sourceVersion, geometryVersion,
                        expand, targetHash, offset + page.Length))) : null;
                return new CandidateMatchResult(200, Page: new(new(sourceKind, sourceId,
                    sourceVersion), geometryVersion,
                    "huy01-1", page, next));
            }, token);
        }
        catch (CaseWorkflowException error) { return new(error.Status, error.Code); }
    }

    private static CandidateMatchResult Failure(AnhHuyProducerStatus status) => status switch
    {
        AnhHuyProducerStatus.Forbidden => new(403, "access_forbidden"),
        AnhHuyProducerStatus.NotFound => new(404, "not_found"),
        AnhHuyProducerStatus.StaleSource or AnhHuyProducerStatus.StaleGeometry => new(409, "candidate_stale"),
        _ => new(409, "source_not_ready")
    };
}
