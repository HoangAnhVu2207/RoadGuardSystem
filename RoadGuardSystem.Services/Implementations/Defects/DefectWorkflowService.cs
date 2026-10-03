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

public sealed class DefectWorkflowService(IDefectWorkflowRepository repository,
    ICandidateDecisionRepository candidates, ICaseWorkflowRepository cases,
    IAnhHuyProducerService producer, IProjectScopeGuard scope,
    IdempotencyOperationService idempotency) : IDefectWorkflowService
{
    public Task<DefectWorkflowResult> ReadAsync(Guid actor, UserRoleCode role, Guid project,
        Guid defect, CancellationToken token) => Run(async () =>
    {
        if (role != UserRoleCode.ProjectManager) return Failure(403, "access_forbidden");
        var view = await repository.ReadAsync(project, defect,
            ct => GuardProjectAsync(actor, role, project, ct), token);
        return view is null ? Failure(404, "not_found") : new(200, Defect: view);
    });

    public Task<DefectWorkflowResult> ListAsync(Guid actor, UserRoleCode role, Guid project,
        string? status, string? type, Guid? segment, int pageSize, string? cursor,
        CancellationToken token) => Run(async () =>
    {
        if (role != UserRoleCode.ProjectManager) return Failure(403, "access_forbidden");
        if (project == Guid.Empty || pageSize is < 1 or > 200 || segment == Guid.Empty ||
            type?.Length > 80) return Invalid("filter");
        DefectStatus? parsedStatus = null;
        if (status is not null)
        {
            if (!Enum.TryParse<DefectStatus>(status, true, out var parsed) ||
                parsed == DefectStatus.Unknown || !Enum.IsDefined(parsed)) return Invalid("status");
            parsedStatus = parsed;
        }
        Guid? afterId = null;
        if (cursor is not null)
        {
            try
            {
                var bytes = Convert.FromBase64String(cursor);
                if (bytes.Length != 16) return Invalid("cursor");
                afterId = new Guid(bytes);
            }
            catch (FormatException) { return Invalid("cursor"); }
        }
        var page = await repository.ListAsync(project, parsedStatus, type, segment, pageSize, afterId,
            ct => GuardProjectAsync(actor, role, project, ct), token);
        return new(200, Page: page);
    });

    public Task<DefectWorkflowResult> AssessAsync(Guid actor, UserRoleCode role, Guid project, Guid defect,
        DefectAssessmentRequestDto request, string key, string? ifMatch, Guid? correlation,
        CancellationToken token) => Run(async () =>
    {
        if (role != UserRoleCode.ProjectManager) return Failure(403, "access_forbidden");
        if (!ValidKey(key)) return Invalid("Idempotency-Key");
        var expected = Version(ifMatch);
        if (expected is null) return Failure(ifMatch is null ? 428 : 400,
            ifMatch is null ? "precondition_required" : "validation_error");
        if (project == Guid.Empty || defect == Guid.Empty || string.IsNullOrWhiteSpace(request.DefectTypeCode) ||
            request.DefectTypeCode.Trim().Length > 80 || request.CauseCategoryCode?.Trim().Length > 80 ||
            request.Severity is not ("LOW" or "MEDIUM" or "HIGH" or "CRITICAL") ||
            !ValidReason(request.Reason) || !ValidEvidence(request.EvidenceIds)) return Invalid("assessment");
        var type = request.DefectTypeCode.Trim();
        var cause = string.IsNullOrWhiteSpace(request.CauseCategoryCode) ? null : request.CauseCategoryCode.Trim();
        var reason = request.Reason!.Trim();
        var evidence = request.EvidenceIds!;
        var fingerprint = Fingerprint(new { project, defect, expected, type, cause, request.Severity, reason, evidence });
        async Task Guard(CancellationToken ct) => await GuardDefectAsync(actor, role, project, defect, evidence, ct);
        var outcome = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteAsync(actor,
            project, "huy01.defect.assess.v1", key.Trim(' '), fingerprint, handler, token,
            receiptAccessGuard: Guard), async ct =>
        {
            await Guard(ct);
            var severity = request.Severity switch
            {
                "LOW" => DefectSeverity.Low, "MEDIUM" => DefectSeverity.Medium,
                "HIGH" => DefectSeverity.High, "CRITICAL" => DefectSeverity.Critical,
                _ => DefectSeverity.Unknown
            };
            var view = await repository.ApplyAssessmentAsync(actor, project, defect, expected,
                type, cause, severity, evidence, reason, correlation, ct);
            return (Guid.NewGuid(), JsonSerializer.Serialize(view));
        }, error => error is CaseWorkflowException { Code: "concurrency_conflict" });
        return outcome.Status == IdempotencyOperationStatus.Conflict ? Failure(409, "idempotency_key_reused")
            : new(200, Defect: JsonSerializer.Deserialize<DefectViewDto>(outcome.OutcomeJson));
    });

    public Task<DefectWorkflowResult> VerifyAsync(Guid actor, UserRoleCode role, Guid project, Guid defect,
        DefectVerificationRequestDto request, string key, string? ifMatch, Guid? correlation,
        CancellationToken token) => Run(async () =>
    {
        if (role != UserRoleCode.ProjectManager) return Failure(403, "access_forbidden");
        if (!ValidKey(key)) return Invalid("Idempotency-Key");
        var expected = Version(ifMatch);
        if (expected is null) return Failure(ifMatch is null ? 428 : 400,
            ifMatch is null ? "precondition_required" : "validation_error");
        if (project == Guid.Empty || defect == Guid.Empty ||
            request.Decision is not ("CONFIRM" or "REJECT") ||
            request.VerificationMethod is not ("EXISTING_EVIDENCE" or "FIELD" or "DRONE") ||
            !ValidReason(request.Reason) || !ValidEvidence(request.EvidenceIds) ||
            request.EvidenceIds!.Length == 0) return Invalid("verification");
        if (request.VerificationMethod != "EXISTING_EVIDENCE") return Failure(409, "source_not_ready");
        var reason = request.Reason!.Trim();
        var evidence = request.EvidenceIds!;
        var fingerprint = Fingerprint(new { project, defect, expected, request.Decision, request.VerificationMethod, reason, evidence });
        async Task<IReadOnlyCollection<Guid>> Guard(CancellationToken ct)
            => await GuardDefectAsync(actor, role, project, defect, evidence, ct);
        var outcome = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteAsync(actor,
            project, "huy01.defect.verify.v1", key.Trim(' '), fingerprint, handler, token,
            receiptAccessGuard: async ct => { await Guard(ct); }), async ct =>
        {
            var verified = await Guard(ct);
            var action = request.Decision == "CONFIRM" ? DefectVerificationAction.Confirm : DefectVerificationAction.Reject;
            var view = await repository.ApplyVerificationAsync(actor, project, defect, expected,
                action, evidence, verified, reason, correlation, ct);
            return (Guid.NewGuid(), JsonSerializer.Serialize(view));
        }, error => error is CaseWorkflowException { Code: "concurrency_conflict" });
        return outcome.Status == IdempotencyOperationStatus.Conflict ? Failure(409, "idempotency_key_reused")
            : new(200, Defect: JsonSerializer.Deserialize<DefectViewDto>(outcome.OutcomeJson));
    });

    private async Task<IReadOnlyCollection<Guid>> GuardDefectAsync(Guid actor, UserRoleCode role,
        Guid project, Guid defect, IReadOnlyCollection<Guid> selected, CancellationToken token)
    {
        await GuardProjectAsync(actor, role, project, token);
        var reports = await repository.LinkedReportsAsync(project, defect, token);
        if (reports.Count == 0) throw new CaseWorkflowException(409, "source_not_ready");
        var verified = new HashSet<Guid>();
        foreach (var report in reports)
        {
            var initial = await ResolveAsync(actor, role, project, report, token);
            await cases.GuardAsync(actor, role, [initial.CaseId], project,
                async (p, ct) => await scope.AuthorizeAsync(actor, role, p, ct) is not null, token);
            await candidates.LockSourceAsync(report, initial.CaseId, token);
            var fresh = await ResolveAsync(actor, role, project, report, token);
            verified.UnionWith(fresh.EvidenceIds);
        }
        await repository.LockTargetAsync(project, defect, token);
        if (selected.Any(id => !verified.Contains(id))) throw new CaseWorkflowException(409, "source_not_ready");
        return verified;
    }

    private async Task GuardProjectAsync(Guid actor, UserRoleCode role, Guid project, CancellationToken token)
        => await cases.GuardAsync(actor, role, [], project,
            async (p, ct) => await scope.AuthorizeAsync(actor, role, p, ct) is not null, token);

    private async Task<ResolvedCandidateSourceFacts> ResolveAsync(Guid actor, UserRoleCode role,
        Guid project, Guid report, CancellationToken token)
    {
        var source = await producer.ResolveCandidateSourceAsync(actor, role, project,
            CandidateSourceKind.Report, report, cancellationToken: token);
        if (source.Status != AnhHuyProducerStatus.Ready)
            throw source.Status switch
            {
                AnhHuyProducerStatus.Forbidden => new CaseWorkflowException(403, "access_forbidden"),
                AnhHuyProducerStatus.NotFound => new CaseWorkflowException(404, "not_found"),
                _ => new CaseWorkflowException(409, "source_not_ready")
            };
        return source.Facts!;
    }

    private static bool ValidReason(string? reason) => !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length <= 1000;
    private static bool ValidEvidence(Guid[]? ids) => ids is not null && ids.All(id => id != Guid.Empty) && ids.Distinct().Count() == ids.Length;
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
    private static DefectWorkflowResult Invalid(string field) => new(400, "validation_error", Errors:
        new Dictionary<string, string[]> { [field] = ["Invalid command field."] });
    private static DefectWorkflowResult Failure(int status, string code) => new(status, code);
    private static async Task<DefectWorkflowResult> Run(Func<Task<DefectWorkflowResult>> action)
    {
        try { return await action(); }
        catch (CaseWorkflowException error) { return Failure(error.Status, error.Code); }
        catch (ArgumentException) { return Failure(400, "validation_error"); }
    }
}
