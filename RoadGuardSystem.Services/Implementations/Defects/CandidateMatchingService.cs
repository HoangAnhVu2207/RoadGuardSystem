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
    ICaseWorkflowRepository cases, IAnhHuyProducerService producer, IProjectScopeGuard scope)
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
        if (sourceKind != "REPORT") return new(409, "source_not_ready");
        try
        {
            return await repository.ReadConsistentlyAsync(async ct =>
            {
                var initial = await producer.ResolveCandidateSourceAsync(actor, role, project,
                    CandidateSourceKind.Report, sourceId, cancellationToken: ct);
                if (initial.Status != AnhHuyProducerStatus.Ready) return Failure(initial.Status);
                await cases.GuardAsync(actor, role, [initial.Facts!.CaseId], project,
                    async (p, inner) => await scope.AuthorizeAsync(actor, role, p, inner) is not null, ct);
                await repository.LockSourceAsync(sourceId, initial.Facts.CaseId, ct);
                var current = await producer.ResolveCandidateSourceAsync(actor, role, project,
                    CandidateSourceKind.Report, sourceId, cancellationToken: ct);
                if (current.Status != AnhHuyProducerStatus.Ready) return Failure(current.Status);
                var source = current.Facts!;
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
                    if (decoded.SourceVersion != source.DomainFacts.Source.SourceVersion ||
                        decoded.GeometryVersion != source.DomainFacts.GeometryVersion ||
                        decoded.TargetHash != targetHash) return new(409, "candidate_stale");
                    offset = decoded.Offset;
                }
                var matched = CandidateMatcher.Match(project, [], [], targets.Select(target => new CandidateMatchFact(
                    target.DefectId, target.ProjectId, target.Version, target.SegmentId, null,
                    target.LinkedReportIds.Contains(sourceId))).ToArray(), expand, metricGpsAvailable: false);
                if (offset > matched.Count) return new(400, "validation_error");
                var page = matched.Skip(offset).Take(pageSize).Select(item =>
                {
                    var target = targets.Single(target => target.DefectId == item.DefectId);
                    return new CandidateMatchItemDto(item.DefectId, item.Version, item.SegmentId,
                        item.PriorityGroup, item.DistanceMeters, null, item.ReasonCodes,
                        target.LinkedReportIds.Contains(sourceId) ? source.EvidenceIds : [],
                        new(target.LinkedReportIds.Count));
                }).ToArray();
                var next = offset + page.Length < matched.Count
                    ? Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new Cursor(project, sourceId,
                        source.DomainFacts.Source.SourceVersion, source.DomainFacts.GeometryVersion,
                        expand, targetHash, offset + page.Length))) : null;
                return new CandidateMatchResult(200, Page: new(new("REPORT", sourceId,
                    source.DomainFacts.Source.SourceVersion), source.DomainFacts.GeometryVersion,
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
