using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Defects;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.Services.Implementations.Defects;

public sealed class CandidateDecisionService(ICandidateDecisionRepository repository, ICaseWorkflowRepository cases,
    IAnhHuyProducerService producer, IEnumerable<IAiCandidateFactsReader> aiReaders, IProjectScopeGuard scope,
    IdempotencyOperationService idempotency) : ICandidateDecisionService
{
    private sealed record DecisionSource(CandidateSourceFacts DomainFacts, ProjectGeometryContext Geometry, Guid? CaseId);
    public Task<CandidateDecisionResult> ReadAsync(Guid actor, UserRoleCode role, Guid projectId, Guid decisionId, CancellationToken ct)
        => Run(async () =>
        {
            if (role != UserRoleCode.ProjectManager) return new(403, "access_forbidden");
            var value = await repository.ReadAsync(projectId, decisionId, token => cases.GuardAsync(actor, role, [], projectId,
                async (project, inner) => await scope.AuthorizeAsync(actor, role, project, inner) is not null, token), ct);
            return value is null ? new(404, "not_found") : new(200, Decision: value);
        });

    public Task<CandidateDecisionResult> DecideAsync(Guid actor, UserRoleCode role, Guid projectId, CandidateDecisionRequestDto request, string key, Guid? correlation, CancellationToken ct)
        => Run(async () =>
        {
            if (role != UserRoleCode.ProjectManager) return new(403, "access_forbidden");
            if (projectId == Guid.Empty || request.SourceId == Guid.Empty) return Invalid("sourceId");
            if (key is null || key.Any(c => c is < ' ' or > '~') || key.Trim(' ').Length is < 1 or > 200) return Invalid("Idempotency-Key");
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000) return Invalid("reason");
            if (string.IsNullOrWhiteSpace(request.SourceVersion) || request.SourceVersion.Length > 200 || string.IsNullOrWhiteSpace(request.GeometryVersion) || request.GeometryVersion.Length > 200) return Invalid("sourceVersion");
            var kind = request.SourceKind switch { "REPORT" => CandidateSourceKind.Report, "AI_DETECTION" => CandidateSourceKind.AiDetection, "FIELD_OBSERVATION" => CandidateSourceKind.FieldObservation, _ => CandidateSourceKind.Unknown };
            if (kind == CandidateSourceKind.Unknown) return Invalid("sourceKind");
            var decision = request.Decision switch { "REJECT" => CandidateDecisionKind.Reject, "KEEP_NEW" => CandidateDecisionKind.KeepNew, "LINK_EXISTING" => CandidateDecisionKind.LinkExisting, _ => CandidateDecisionKind.Unknown };
            if (decision == CandidateDecisionKind.Unknown) return Invalid("decision");
            if (request.TargetDefectId == Guid.Empty || request.SupersedesDecisionId == Guid.Empty) return Invalid("targetDefectId");
            var hasVersion = !string.IsNullOrWhiteSpace(request.TargetVersion);
            if (decision == CandidateDecisionKind.Reject && (request.TargetDefectId is not null || request.TargetVersion is not null || request.Classification is not null) ||
                decision == CandidateDecisionKind.LinkExisting && (request.TargetDefectId is null || !hasVersion || request.Classification is not null) ||
                decision == CandidateDecisionKind.KeepNew && (request.TargetDefectId is not null || request.TargetVersion is not null || request.Classification is null)) return Invalid("decision");
            if (decision == CandidateDecisionKind.LinkExisting && !RowVersion(request.TargetVersion!)) return Invalid("targetVersion");
            if ((request.SupersedesDecisionId is null) != (request.PreviousDecisionVersion is null)) return Invalid("previousDecisionVersion");
            if (request.PreviousDecisionVersion is not null && !RowVersion(request.PreviousDecisionVersion)) return Invalid("previousDecisionVersion");
            if (request.Classification is { } classification && (classification.RoadSectionVersionId == Guid.Empty || classification.SegmentId == Guid.Empty ||
                string.IsNullOrWhiteSpace(classification.DefectTypeCode) || classification.DefectTypeCode.Trim().Length > 80 || classification.Severity is not ("LOW" or "MEDIUM" or "HIGH" or "CRITICAL"))) return Invalid("classification");
            request = request with { Reason = request.Reason.Trim(), SourceVersion = request.SourceVersion.Trim(), GeometryVersion = request.GeometryVersion.Trim() };
            if (kind == CandidateSourceKind.FieldObservation) return new(409, "source_not_ready");
            var initial = await ResolveAsync(actor, role, projectId, kind, request.SourceId, null, null, null, ct);
            if (initial.Status != AnhHuyProducerStatus.Ready) return Failure(initial.Status);
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { projectId, request }))).ToLowerInvariant();
            var normalized = request;
            async Task<DecisionSource> Guard(CancellationToken token)
            {
                await cases.GuardAsync(actor, role, initial.Facts!.CaseId is Guid caseId ? [caseId] : [], projectId,
                    async (project, inner) => await scope.AuthorizeAsync(actor, role, project, inner) is not null, token);
                if (initial.Facts.CaseId is Guid reportCase)
                    await repository.LockSourceAsync(normalized.SourceId, reportCase, token);
                else await repository.LockAiSourceAsync(normalized.SourceId, token);
                var current = await ResolveAsync(actor, role, projectId, kind, normalized.SourceId, null, null, null, token);
                if (current.Status != AnhHuyProducerStatus.Ready) Throw(current.Status);
                return current.Facts!;
            }
            var execution = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteAsync(actor, projectId, "huy01.candidate.decide.v1", key.Trim(' '), fingerprint,
                handler, ct, receiptAccessGuard: async token => { await Guard(token); }), async token =>
                {
                    await Guard(token);
                    var fresh = await ResolveAsync(actor, role, projectId, kind, normalized.SourceId,
                        normalized.SourceVersion, normalized.GeometryVersion, normalized.PreviousDecisionVersion, token);
                    if (fresh.Status != AnhHuyProducerStatus.Ready) Throw(fresh.Status);
                    var correction = normalized.SupersedesDecisionId is Guid supersedes ? CandidateCorrection.Create(supersedes, normalized.PreviousDecisionVersion!) : null;
                    CandidateDecisionResponseDto write;
                    if (decision == CandidateDecisionKind.Reject)
                        write = await repository.SaveRejectAsync(actor, fresh.Facts!.DomainFacts,
                            correction, normalized.Reason!, correlation, token);
                    else
                    {
                        var source = fresh.Facts!;
                        CandidateClassification? classification = null;
                        if (decision == CandidateDecisionKind.KeepNew)
                        {
                            var input = normalized.Classification!;
                            if (input.RoadSectionVersionId != source.Geometry.RouteVersionId ||
                                input.SegmentId is { } segment && !source.Geometry.Segments.Any(item => item.Id == segment))
                                throw new CaseWorkflowException(409, "candidate_stale");
                            classification = CandidateClassification.Create(input.RoadSectionVersionId,
                                input.DefectTypeCode!, input.CauseCategoryCode,
                                input.Severity switch
                                {
                                    "LOW" => DefectSeverity.Low,
                                    "MEDIUM" => DefectSeverity.Medium,
                                    "HIGH" => DefectSeverity.High,
                                    "CRITICAL" => DefectSeverity.Critical,
                                    _ => DefectSeverity.Unknown
                                }, input.SegmentId);
                        }
                        write = await repository.SaveAcceptedAsync(actor, source.DomainFacts, decision,
                            classification, normalized.TargetDefectId, normalized.TargetVersion,
                            correction, normalized.Reason!, correlation, token);
                    }
                    return (write.Id, JsonSerializer.Serialize(write));
                }, exception => exception is CaseWorkflowException { Code: "candidate_stale" or "concurrency_conflict" });
            return execution.Status == IdempotencyOperationStatus.Conflict ? new(409, "idempotency_key_reused")
                : new(201, Decision: JsonSerializer.Deserialize<CandidateDecisionResponseDto>(execution.OutcomeJson));
        });

    private static bool RowVersion(string input) { try { return Convert.FromBase64String(input).Length == 8; } catch (FormatException) { return false; } }
    private async Task<AnhHuyProducerResult<DecisionSource>> ResolveAsync(Guid actor, UserRoleCode role,
        Guid project, CandidateSourceKind kind, Guid sourceId, string? sourceVersion,
        string? geometryVersion, string? dispositionVersion, CancellationToken ct)
    {
        if (kind == CandidateSourceKind.Report)
        {
            var report = await producer.ResolveCandidateSourceAsync(actor, role, project, kind, sourceId,
                sourceVersion, geometryVersion, dispositionVersion, ct);
            return report.Status == AnhHuyProducerStatus.Ready
                ? new(report.Status, new(report.Facts!.DomainFacts, report.Facts.Geometry, report.Facts.CaseId))
                : new(report.Status);
        }
        var ai = aiReaders.SingleOrDefault();
        if (ai is null) return new(AnhHuyProducerStatus.SourceNotReady);
        var result = await ai.ResolveAsync(actor, role, project, sourceId, sourceVersion,
            geometryVersion, dispositionVersion, ct);
        if (result.Status != AnhHuyProducerStatus.Ready) return new(result.Status);
        var facts = result.Facts!;
        var geometry = await producer.ResolveGeometryAsync(actor, role, project, facts.RouteVersionId,
            facts.SegmentSetId, facts.GeometryVersion, true, ct);
        if (geometry.Status != AnhHuyProducerStatus.Ready) return new(geometry.Status);
        var domain = CandidateSourceFacts.Create(CandidateSourceIdentity.Create(kind, sourceId,
            facts.SourceVersion), project, facts.GeometryVersion, facts.Disposition);
        return new(AnhHuyProducerStatus.Ready, new(domain, geometry.Facts!, null));
    }
    private static CandidateDecisionResult Failure(AnhHuyProducerStatus status) => status switch
    {
        AnhHuyProducerStatus.Forbidden => new(403, "access_forbidden"), AnhHuyProducerStatus.NotFound => new(404, "not_found"),
        AnhHuyProducerStatus.StaleDisposition or AnhHuyProducerStatus.StaleFile => new(412, "concurrency_conflict"),
        AnhHuyProducerStatus.StaleGeometry or AnhHuyProducerStatus.StaleSource => new(409, "candidate_stale"), _ => new(409, "source_not_ready")
    };
    private static void Throw(AnhHuyProducerStatus status) { var failure = Failure(status); throw new CaseWorkflowException(failure.Status, failure.Code!); }
    private static CandidateDecisionResult Invalid(string field) => new(400, "validation_error", Errors: new Dictionary<string, string[]> { [field] = ["Invalid command field."] });
    private static async Task<CandidateDecisionResult> Run(Func<Task<CandidateDecisionResult>> action)
    { try { return await action(); } catch (CaseWorkflowException e) { return new(e.Status, e.Code); } }
}
