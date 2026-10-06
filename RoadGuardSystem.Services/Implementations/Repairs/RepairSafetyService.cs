using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Services.Repairs;

namespace RoadGuardSystem.Services.Implementations.Repairs;

public sealed class RepairSafetyService(IRepairSafetyRepository repository) : IRepairSafetyService
{
    public Task<RepairSafetyServiceResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid measure, CancellationToken token)
        => actor == Guid.Empty || project == Guid.Empty || package == Guid.Empty || item == Guid.Empty || measure == Guid.Empty
            ? Error(400, "validation_error")
            : Map(repository.ReadAsync(actor, role, project, package, item, measure, token));

    public Task<RepairSafetyServiceResult> CreateAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, RepairSafetyCreateInput input, string? key, string? version, CancellationToken token)
    {
        var error = Admission(actor, role, UserRoleCode.ProjectManager, project, package, item, key, version);
        if (error is not null) return Task.FromResult(error);
        if (input is null || input.FormalRepairObligationId == Guid.Empty || input.SafetyObligationId == Guid.Empty ||
            input.ResponsibleActorId == Guid.Empty || !Text(input.CheckSchedule) ||
            !Text(input.ReplacementCondition) || !Text(input.RemovalCondition) || !Text(input.Reason))
            return Error(400, "validation_error");
        return Map(repository.CreateAsync(new(actor, role, project, package, item, input.FormalRepairObligationId,
            input.SafetyObligationId, input.ResponsibleActorId, input.CheckSchedule, input.ReplacementCondition,
            input.RemovalCondition, input.Reason, key!, version![1..^1]), token));
    }

    public Task<RepairSafetyServiceResult> InstallAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid measure, RepairSafetyInstallInput input, string? key, string? version, CancellationToken token)
    {
        var error = Admission(actor, role, UserRoleCode.RepairCrew, project, package, item, key, version);
        if (error is not null) return Task.FromResult(error);
        if (measure == Guid.Empty || input is null || input.FirstCheckDueAt == default || !Text(input.Reason))
            return Error(400, "validation_error");
        return Map(repository.InstallAsync(new(actor, role, project, package, item, measure, input.FirstCheckDueAt,
            input.Reason, key!, version![1..^1]), token));
    }

    public Task<RepairSafetyServiceResult> CheckAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid measure, RepairSafetyCheckInput input, string? key, string? version, CancellationToken token)
    {
        var error = Admission(actor, role, UserRoleCode.RepairCrew, project, package, item, key, version);
        if (error is not null) return Task.FromResult(error);
        if (measure == Guid.Empty || input is null || input.Result is not ("Safe" or "NeedsReplacement" or "Danger") ||
            !Text(input.Findings) || input.EvidenceIds is null || input.EvidenceIds.Length is < 1 or > 20 ||
            input.EvidenceIds.Any(id => id == Guid.Empty) || input.EvidenceIds.Distinct().Count() != input.EvidenceIds.Length)
            return Error(400, "validation_error");
        return Map(repository.CheckAsync(new(actor, role, project, package, item, measure, input.Result,
            input.Findings, input.EvidenceIds, key!, version![1..^1]), token));
    }

    public Task<RepairSafetyServiceResult> AcknowledgeAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid measure, Guid warning, RepairSafetyAcknowledgementInput input,
        string? key, string? version, CancellationToken token)
    {
        var error = Admission(actor, role, UserRoleCode.RepairCrew, project, package, item, key, version);
        if (error is not null) return Task.FromResult(error);
        if (measure == Guid.Empty || warning == Guid.Empty || input is null || !Text(input.Reason))
            return Error(400, "validation_error");
        return Map(repository.AcknowledgeAsync(new(actor, role, project, package, item, measure, warning,
            input.Reason, key!, version![1..^1]), token));
    }

    private static RepairSafetyServiceResult? Admission(Guid actor, UserRoleCode actual, UserRoleCode required,
        Guid project, Guid package, Guid item, string? key, string? version)
    {
        if (actual != required) return new(403, "access_forbidden");
        if (actor == Guid.Empty || project == Guid.Empty || package == Guid.Empty || item == Guid.Empty)
            return new(400, "validation_error");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version)) return new(428, "precondition_required");
        if (key.Length > 150 || key.Any(ch => ch is < '!' or > '~') || !Version(version))
            return new(400, "validation_error");
        return null;
    }

    private static bool Version(string value)
    {
        if (value.Length < 3 || value[0] != '"' || value[^1] != '"') return false;
        Span<byte> bytes = stackalloc byte[8]; var token = value[1..^1];
        return Convert.TryFromBase64String(token, bytes, out var count) && count == 8 &&
            Convert.ToBase64String(bytes) == token;
    }

    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 2000;
    private static Task<RepairSafetyServiceResult> Error(int status, string code)
        => Task.FromResult(new RepairSafetyServiceResult(status, code));

    private static async Task<RepairSafetyServiceResult> Map(Task<SafetyResult> pending)
    {
        var result = await pending;
        if (result.Value is not SafetyFact fact) return new(result.Status, result.Code);
        var view = new RepairSafetyView(fact.Id, fact.ProjectId, fact.DefectId, fact.FormalObligationId,
            fact.SafetyObligationId, fact.ResponsibleActorId, fact.InstalledAt, fact.FirstCheckDueAt,
            fact.CurrentCheckId, fact.Checks.Select(row => new RepairSafetyCheckView(row.Id, row.ActorId,
                row.At, row.Result, row.Findings, row.EvidenceLinkIds)).ToArray(),
            fact.Warnings.Select(row => new RepairSafetyWarningView(row.Id, row.SourceCheckId,
                row.IntendedActorId, row.ServerReceivedAt, row.OriginalDueAt, row.Reason)).ToArray(),
            fact.Acknowledgements.Select(row => new RepairSafetyAcknowledgementView(row.Id, row.WarningId,
                row.ActorId, row.At, row.AfterOriginalDue)).ToArray(), fact.Version);
        return new(result.Status, result.Code, view);
    }
}
