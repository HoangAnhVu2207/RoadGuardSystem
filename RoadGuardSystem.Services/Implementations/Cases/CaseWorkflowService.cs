using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Cases;
using RoadGuardSystem.Repositories.Cases;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Cases;
using RoadGuardSystem.Services.Integration;
using RoadGuardSystem.Services.Reports;

namespace RoadGuardSystem.Services.Implementations.Cases;

public sealed class CaseWorkflowService(ICaseWorkflowRepository repository, IProjectScopeGuard scope,
    IAnhHuyProducerService producer, IdempotencyOperationService idempotency) : ICaseWorkflowService
{
    public Task<CaseWorkflowResult> ReadAsync(Guid actor, UserRoleCode role, Guid id, CancellationToken ct)
        => Run(async () => new(200, Case: await repository.ReadAsync(actor, role, id, async (p, token) => await scope.AuthorizeAsync(actor, role, p, token) is not null, ct)));
    public Task<CaseWorkflowResult> ListAsync(Guid actor, UserRoleCode role, Guid? project, IncidentCaseStatus? status, int pageSize, string? cursor, CancellationToken ct)
        => Run(async () => pageSize is < 1 or > 100 ? Invalid("pageSize") : new(200,
            Page: await repository.ListAsync(actor, role, project, status, pageSize, cursor, async (p, token) => await scope.AuthorizeAsync(actor, role, p, token) is not null, ct)));

    public Task<CaseWorkflowResult> CommandAsync(Guid actor, UserRoleCode role, CaseCommand command, string key, Guid? correlation, CancellationToken ct)
        => Run(async () =>
        {
            if (command.CaseId == Guid.Empty || string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 1000) return Invalid("reason");
            if (!ValidVersion(command.ExpectedVersion)) return Invalid("If-Match");
            if (key is null || key.Any(c => c is < ' ' or > '~') || key.Trim(' ').Length is < 1 or > 200) return Invalid("Idempotency-Key");
            command = command with { Reason = command.Reason.Trim(), SourceCaseVersions = command.SourceCaseVersions?.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value) };
            bool ValidIds(IReadOnlyList<Guid>? ids, bool required = false) => (!required || ids is { Count: > 0 }) && (ids is null || ids.All(id => id != Guid.Empty) && ids.Distinct().Count() == ids.Count);
            if (!ValidIds(command.ReportIds, command.Action is "link" or "split" or "publish")) return Invalid("reportIds");
            if (!ValidIds(command.DefectIds)) return Invalid("defectIds");
            if (!ValidIds(command.EvidenceIds)) return Invalid("evidenceIds");
            if (command.Action == "triage" && (command.ProjectId is null || command.ProjectId == Guid.Empty || command.Method is null or CaseVerificationMethod.Unknown)) return Invalid("projectId");
            if (command.Action is "conclude" or "publish" && (command.DefectIds is null || command.EvidenceIds is null)) return Invalid("evidenceIds");
            if ((command.RouteVersionId is null) != (command.SegmentSetId is null) || command.RouteVersionId == Guid.Empty || command.SegmentSetId == Guid.Empty || command.RouteVersionId is not null && string.IsNullOrWhiteSpace(command.GeometryVersion)) return Invalid("geometryVersion");
            if (command.Action == "link" && (command.SourceCaseVersions is not { Count: > 0 } || command.SourceCaseVersions.Keys.Contains(command.CaseId) || command.SourceCaseVersions.Any(k => k.Key == Guid.Empty || !ValidVersion(k.Value)))) return Invalid("sourceCaseVersions");
            if (command.Action == "conclude" && command.Outcome is null) return Invalid("outcome");
            if (command.Action == "conclude" && command.Outcome == CaseConclusionOutcome.Confirmed && command.DefectIds is not { Count: > 0 }) return Invalid("defectIds");
            if (command.Action == "conclude" && command.Outcome == CaseConclusionOutcome.NoDefect && command.DefectIds is { Count: > 0 }) return Invalid("defectIds");
            if (command.Action == "conclude" && command.Outcome is CaseConclusionOutcome.NoDefect or CaseConclusionOutcome.Confirmed && command.EvidenceIds is not { Count: > 0 }) return Invalid("evidenceIds");
            var normalized = command;
            Task<bool> ProjectAccess(Guid project, CancellationToken token) => AuthorizeAsync(project, token);
            async Task<bool> AuthorizeAsync(Guid project, CancellationToken token) => await scope.AuthorizeAsync(actor, role, project, token) is not null;
            await repository.ReadAsync(actor, role, command.CaseId, ProjectAccess, ct);
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(command))).ToLowerInvariant();
            Func<CancellationToken, Task>? geometry = command.RouteVersionId is null ? null : async token =>
            {
                var facts = await producer.ResolveGeometryAsync(actor, role, normalized.ProjectId!.Value, normalized.RouteVersionId!.Value,
                    normalized.SegmentSetId!.Value, normalized.GeometryVersion, true, token);
                if (facts.Status != AnhHuyProducerStatus.Ready) throw new CaseWorkflowException(facts.Status == AnhHuyProducerStatus.Forbidden ? 403 : facts.Status == AnhHuyProducerStatus.NotFound ? 404 : 409,
                    facts.Status == AnhHuyProducerStatus.Forbidden ? "access_forbidden" : facts.Status == AnhHuyProducerStatus.NotFound ? "not_found" : facts.Status == AnhHuyProducerStatus.StaleGeometry ? "candidate_stale" : "source_not_ready");
            };
            var result = await Huy01CommandExecution.ExecuteAsync(handler => idempotency.ExecuteAsync(actor, null, $"huy01.case.{normalized.Action}.v1", key.Trim(' '), fingerprint,
                handler, ct, receiptAccessGuard: token => repository.GuardCommandAsync(actor, role, normalized, ProjectAccess, token)), async token =>
                {
                    var write = await repository.ApplyAsync(actor, role, normalized, ProjectAccess, geometry, correlation, token);
                    return (write.Publication?.Id ?? write.Case.Id, JsonSerializer.Serialize(write));
                }, exception => exception is CaseWorkflowException { Status: 412 });
            if (result.Status == IdempotencyOperationStatus.Conflict) return new(409, "idempotency_key_reused");
            return new(command.Action is "publish" or "split" ? 201 : 200, Write: JsonSerializer.Deserialize<CaseWriteResult>(result.OutcomeJson));
        });

    private static bool ValidVersion(string value)
    {
        try { return Convert.FromBase64String(value).Length == 8; } catch (FormatException) { return false; }
    }
    private static CaseWorkflowResult Invalid(string field) => new(400, "validation_error", Errors: new Dictionary<string, string[]> { [field] = ["Invalid command field."] });
    private static async Task<CaseWorkflowResult> Run(Func<Task<CaseWorkflowResult>> action)
    {
        try { return await action(); }
        catch (CaseWorkflowException e) { return new(e.Status, e.Code); }
    }
}
