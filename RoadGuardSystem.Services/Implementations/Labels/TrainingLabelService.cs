using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Labels;
using RoadGuardSystem.DTOs.Labels;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Defects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Labels;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Labels;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.Services.Implementations.Labels;

public sealed class TrainingLabelService(ITrainingLabelRepository labels, ICandidateDecisionRepository candidates,
    ICaseWorkflowRepository cases, IAnhHuyProducerService producer, IProjectScopeGuard scope,
    IdempotencyOperationService idempotency) : ITrainingLabelService
{
    public Task<TrainingLabelResult> CreateAsync(Guid actor, UserRoleCode role, Guid project,
        CreateTrainingLabelDto request, string key, Guid? correlation, CancellationToken token)
        => Run(async () =>
        {
            if (role != UserRoleCode.ProjectManager) return Failure(403, "access_forbidden");
            if (project == Guid.Empty || request.SourceId == Guid.Empty || request.FileId == Guid.Empty ||
                request.SourceKind is not ("REPORT" or "AI_DETECTION") || string.IsNullOrWhiteSpace(request.SourceVersion) ||
                request.SourceVersion.Length > 200) return Invalid("sourceId");
            var invalid = ValidateCommon(key, request.Annotation, request.DefectTypeCode, request.Reason);
            if (invalid is not null) return invalid;
            if (request.SourceKind == "AI_DETECTION") return Failure(409, "source_not_ready");
            var initial = await ResolveAsync(actor, role, project, request.SourceId, token);
            FileVersion(initial, request.FileId);
            var normalizedKey = key.Trim(' ');
            var reason = request.Reason!.Trim();
            var fingerprint = Fingerprint(new { project, request = request with { Reason = reason, SourceVersion = request.SourceVersion.Trim() } });
            async Task<ResolvedCandidateSourceFacts> Guard(CancellationToken ct)
            {
                await GuardScopeAsync(actor, role, project, initial.CaseId, request.SourceId, ct);
                var fresh = await ResolveAsync(actor, role, project, request.SourceId, ct);
                FileVersion(fresh, request.FileId);
                return fresh;
            }
            var execution = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteAsync(actor, project,
                "huy01.label.create.v1", normalizedKey, fingerprint, handler, token,
                receiptAccessGuard: async ct => { await Guard(ct); }), async ct =>
            {
                var fresh = await Guard(ct);
                if (fresh.DomainFacts.Source.SourceVersion != request.SourceVersion!.Trim())
                    throw new CaseWorkflowException(409, "candidate_stale");
                var annotation = request.Annotation!;
                var domain = TrainingLabel.Create(Guid.NewGuid(), project, request.SourceId, "REPORT",
                    fresh.DomainFacts.Source.SourceVersion, request.FileId, FileVersion(fresh, request.FileId),
                    annotation.X, annotation.Y, annotation.Width, annotation.Height,
                    request.DefectTypeCode!, reason);
                var view = await labels.CreateAsync(actor, domain, reason, correlation, ct);
                return (view.Id, JsonSerializer.Serialize(view));
            }, exception => exception is CaseWorkflowException { Code: "candidate_stale" or "concurrency_conflict" });
            return execution.Status == IdempotencyOperationStatus.Conflict ? Failure(409, "idempotency_key_reused")
                : new(201, Label: JsonSerializer.Deserialize<TrainingLabelViewDto>(execution.OutcomeJson));
        });

    public Task<TrainingLabelResult> ReviseAsync(Guid actor, UserRoleCode role, Guid project, Guid label,
        ReviseTrainingLabelDto request, string key, string? ifMatch, Guid? correlation, CancellationToken token)
        => Run(async () =>
        {
            if (role != UserRoleCode.ProjectManager) return Failure(403, "access_forbidden");
            var invalid = ValidateCommon(key, request.Annotation, request.DefectTypeCode, request.Reason);
            if (invalid is not null) return invalid;
            if (request.FileId == Guid.Empty) return Invalid("fileId");
            var version = Version(ifMatch);
            if (version is null) return Failure(ifMatch is null ? 428 : 400,
                ifMatch is null ? "precondition_required" : "validation_error");
            var identity = await labels.IdentifyAsync(label, token);
            if (identity is null || identity.ProjectId != project || identity.SourceKind != "REPORT")
                return Failure(404, "not_found");
            var initial = await ResolveAsync(actor, role, project, identity.SourceId, token);
            FileVersion(initial, request.FileId);
            var reason = request.Reason!.Trim();
            var fingerprint = Fingerprint(new { project, label, version, request = request with { Reason = reason } });
            async Task<ResolvedCandidateSourceFacts> Guard(CancellationToken ct)
            {
                await GuardScopeAsync(actor, role, project, initial.CaseId, identity.SourceId, ct);
                var fresh = await ResolveAsync(actor, role, project, identity.SourceId, ct);
                FileVersion(fresh, request.FileId);
                return fresh;
            }
            var execution = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteAsync(actor, project,
                "huy01.label.revise.v1", key.Trim(' '), fingerprint, handler, token,
                receiptAccessGuard: async ct => { await Guard(ct); }), async ct =>
            {
                var fresh = await Guard(ct);
                var view = await labels.ReviseAsync(actor, project, label, version,
                    fresh.DomainFacts.Source.SourceVersion, request.FileId, FileVersion(fresh, request.FileId),
                    request.Annotation!, request.DefectTypeCode!, reason, correlation, ct);
                return (Guid.NewGuid(), JsonSerializer.Serialize(view));
            }, exception => exception is CaseWorkflowException { Code: "concurrency_conflict" });
            return execution.Status == IdempotencyOperationStatus.Conflict ? Failure(409, "idempotency_key_reused")
                : new(201, Label: JsonSerializer.Deserialize<TrainingLabelViewDto>(execution.OutcomeJson));
        });

    public Task<TrainingLabelResult> ReviewAsync(Guid actor, UserRoleCode role, Guid label,
        ReviewTrainingLabelDto request, string key, string? ifMatch, Guid? correlation, CancellationToken token)
        => Run(async () =>
        {
            if (role != UserRoleCode.ProjectManager) return Failure(403, "access_forbidden");
            if (!ValidKey(key)) return Invalid("Idempotency-Key");
            if (request.Decision is not ("APPROVE" or "REJECT") || string.IsNullOrWhiteSpace(request.Reason) ||
                request.Reason.Trim().Length > 1000) return Invalid("decision");
            var version = Version(ifMatch);
            if (version is null) return Failure(ifMatch is null ? 428 : 400,
                ifMatch is null ? "precondition_required" : "validation_error");
            var identity = await labels.IdentifyAsync(label, token);
            if (identity is null || identity.SourceKind != "REPORT") return Failure(404, "not_found");
            var initial = await ResolveAsync(actor, role, identity.ProjectId, identity.SourceId, token);
            var reason = request.Reason.Trim();
            var fingerprint = Fingerprint(new { label, identity.ProjectId, version, decision = request.Decision, reason });
            async Task Guard(CancellationToken ct)
            {
                await GuardScopeAsync(actor, role, identity.ProjectId, initial.CaseId, identity.SourceId, ct);
                var fresh = await ResolveAsync(actor, role, identity.ProjectId, identity.SourceId, ct);
                var current = await labels.CurrentAsync(identity.ProjectId, label, ct)
                    ?? throw new CaseWorkflowException(404, "not_found");
                if (FileVersion(fresh, current.FileId) != current.FileVersion)
                    throw new CaseWorkflowException(412, "concurrency_conflict");
            }
            var execution = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteAsync(actor,
                identity.ProjectId, "huy01.label.review.v1", key.Trim(' '), fingerprint, handler, token,
                receiptAccessGuard: Guard), async ct =>
            {
                await Guard(ct);
                var view = await labels.ReviewAsync(actor, identity.ProjectId, label, version,
                    request.Decision == "APPROVE" ? TrainingLabelReviewStatus.Approved : TrainingLabelReviewStatus.Rejected,
                    reason, correlation, ct);
                return (Guid.NewGuid(), JsonSerializer.Serialize(view));
            }, exception => exception is CaseWorkflowException { Code: "concurrency_conflict" });
            return execution.Status == IdempotencyOperationStatus.Conflict ? Failure(409, "idempotency_key_reused")
                : new(200, Label: JsonSerializer.Deserialize<TrainingLabelViewDto>(execution.OutcomeJson));
        });

    public Task<TrainingLabelResult> ReadAsync(Guid actor, UserRoleCode role, Guid project,
        Guid label, CancellationToken token) => Run(async () =>
        {
            if (role != UserRoleCode.ProjectManager) return Failure(403, "access_forbidden");
            var view = await labels.ReadAsync(project, label, ct => cases.GuardAsync(actor, role, [], project,
                async (p, inner) => await scope.AuthorizeAsync(actor, role, p, inner) is not null, ct), token);
            return view is null ? Failure(404, "not_found") : new(200, Label: view);
        });

    public async Task<TrainingLabelPageResult> ListAsync(Guid actor, UserRoleCode role, Guid project,
        int pageSize, string? cursor, CancellationToken token)
    {
        if (role != UserRoleCode.ProjectManager) return new(403, "access_forbidden");
        if (project == Guid.Empty || pageSize is < 1 or > 200) return new(400, "validation_error");
        Guid? afterId = null;
        if (cursor is not null)
        {
            try
            {
                var bytes = Convert.FromBase64String(cursor);
                if (bytes.Length != 16) return new(400, "validation_error");
                afterId = new Guid(bytes);
            }
            catch (FormatException) { return new(400, "validation_error"); }
        }
        try
        {
            var page = await labels.ListAsync(project, pageSize, afterId,
                ct => cases.GuardAsync(actor, role, [], project,
                    async (p, inner) => await scope.AuthorizeAsync(actor, role, p, inner) is not null, ct), token);
            return new(200, Page: page);
        }
        catch (CaseWorkflowException error) { return new(error.Status, error.Code); }
    }

    private async Task GuardScopeAsync(Guid actor, UserRoleCode role, Guid project, Guid caseId,
        Guid reportId, CancellationToken token)
    {
        await cases.GuardAsync(actor, role, [caseId], project,
            async (p, ct) => await scope.AuthorizeAsync(actor, role, p, ct) is not null, token);
        await candidates.LockSourceAsync(reportId, caseId, token);
    }

    private async Task<ResolvedCandidateSourceFacts> ResolveAsync(Guid actor, UserRoleCode role,
        Guid project, Guid report, CancellationToken token)
    {
        var result = await producer.ResolveCandidateSourceAsync(actor, role, project,
            CandidateSourceKind.Report, report, cancellationToken: token);
        if (result.Status != AnhHuyProducerStatus.Ready)
        {
            var failure = result.Status switch
            {
                AnhHuyProducerStatus.Forbidden => Failure(403, "access_forbidden"),
                AnhHuyProducerStatus.NotFound => Failure(404, "not_found"),
                AnhHuyProducerStatus.StaleSource or AnhHuyProducerStatus.StaleGeometry => Failure(409, "candidate_stale"),
                _ => Failure(409, "source_not_ready")
            };
            throw new CaseWorkflowException(failure.Status, failure.Code!);
        }
        return result.Facts!;
    }

    private static string FileVersion(ResolvedCandidateSourceFacts facts, Guid file)
    {
        try { return TrainingLabelPolicy.ResolveSourceFileVersion(facts, file); }
        catch (InvalidOperationException) { throw new CaseWorkflowException(409, "source_not_ready"); }
    }

    private static TrainingLabelResult? ValidateCommon(string key, LabelAnnotationDto? annotation,
        string? type, string? reason)
    {
        if (!ValidKey(key)) return Invalid("Idempotency-Key");
        if (annotation is null || annotation.Kind != "BBOX" || annotation.CoordinateSpace != "NORMALIZED" ||
            annotation.X < 0 || annotation.Y < 0 || annotation.Width <= 0 || annotation.Height <= 0 ||
            annotation.X + annotation.Width > 1 || annotation.Y + annotation.Height > 1) return Invalid("annotation");
        if (string.IsNullOrWhiteSpace(type) || type.Trim().Length > 80) return Invalid("defectTypeCode");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) return Invalid("reason");
        return null;
    }
    private static bool ValidKey(string? key) => key is not null && key.All(c => c is >= ' ' and <= '~') &&
        key.Trim(' ').Length is >= 1 and <= 200;
    private static string? Version(string? etag)
    {
        if (etag is null || etag.Length < 3 || etag[0] != '"' || etag[^1] != '"') return null;
        try { var value = etag[1..^1]; return Convert.FromBase64String(value).Length == 8 ? value : null; }
        catch (FormatException) { return null; }
    }
    private static string Fingerprint(object payload) => Convert.ToHexString(
        SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload))).ToLowerInvariant();
    private static TrainingLabelResult Invalid(string field) => new(400, "validation_error", Errors:
        new Dictionary<string, string[]> { [field] = ["Invalid command field."] });
    private static TrainingLabelResult Failure(int status, string code) => new(status, code);
    private static async Task<TrainingLabelResult> Run(Func<Task<TrainingLabelResult>> action)
    {
        try { return await action(); }
        catch (CaseWorkflowException error) { return Failure(error.Status, error.Code); }
        catch (ArgumentException) { return Failure(400, "validation_error"); }
    }
}
