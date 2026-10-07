using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.Repositories.Projects;

namespace RoadGuardSystem.Services.Projects;

public sealed partial class ProjectLifecycleService
{
    public async Task<ProjectLifecycleMutationResult> ExecuteAsync(Guid actor, UserRoleCode role, Guid project,
        string action, LD06LifecycleInputDto input, string? key, string? expectedVersion, CancellationToken cancellationToken)
    {
        LD06ActionKind? kind = action switch
        {
            "construction-declarations" => LD06ActionKind.DeclareConstruction,
            "construction-confirmations" => LD06ActionKind.ConfirmConstruction,
            "defect-closures" => LD06ActionKind.CloseDefect,
            "operational-closures" => LD06ActionKind.OperationalClose,
            "recurrences" => LD06ActionKind.LinkRecurrence,
            "obligation-transfers" => LD06ActionKind.IssueTransfer,
            "obligation-transfer-acceptances" => LD06ActionKind.AcceptTransfer,
            _ => null
        };
        if (kind is null) return new(404, "not_found");
        var required = kind is LD06ActionKind.DeclareConstruction or LD06ActionKind.LinkRecurrence ? UserRoleCode.ProjectManager : UserRoleCode.Supervisor;
        if (role != required) return new(403, "access_forbidden");
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(expectedVersion)) return new(428, "precondition_required");
        if (actor == Guid.Empty || project == Guid.Empty || key.Length > 150 || key.Any(c => c is < '!' or > '~') ||
            expectedVersion.Length != 66 || expectedVersion[0] != '"' || expectedVersion[^1] != '"' ||
            expectedVersion[1..^1].Any(c => c is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')) ||
            input is null || !Text(input.Reason, 2000) || input.EvidenceFileIds is null || input.EvidenceFileIds.Length > 100 ||
            input.EvidenceFileIds.Any(x => x == Guid.Empty) || input.EvidenceFileIds.Distinct().Count() != input.EvidenceFileIds.Length)
            return new(400, "validation_error");
        if (new[] { input.SourceActionId, input.DefectId, input.LinkedDefectId, input.ObligationId, input.ReceivingProjectId, input.PriorRepairDecisionId }.Any(x => x == Guid.Empty))
            return new(400, "validation_error");
        var exactShape = kind switch
        {
            LD06ActionKind.DeclareConstruction => input.SourceActionId is null && input.DefectId is null && input.ObligationId is null,
            LD06ActionKind.ConfirmConstruction => input.SourceActionId is not null && input.DefectId is null && input.ObligationId is null,
            LD06ActionKind.CloseDefect => input.DefectId is not null && input.SourceActionId is null && input.ObligationId is null,
            LD06ActionKind.OperationalClose => input.DefectId is null && input.SourceActionId is null && input.ObligationId is null,
            LD06ActionKind.IssueTransfer => input.ObligationId is not null && input.ReceivingProjectId is not null && input.SourceActionId is null && input.ScopeHash?.Length == 64,
            LD06ActionKind.AcceptTransfer => input.ObligationId is not null && input.SourceActionId is not null && input.ReceivingProjectId is null && input.ScopeHash?.Length == 64,
            LD06ActionKind.LinkRecurrence => input.SourceActionId is null && input.ObligationId is null && input.LinkedDefectId is null &&
                input.ReceivingProjectId is null && input.ScopeHash is null && input.NewDefect?.Recurrence is null,
            _ => false
        };
        if (!exactShape || kind != LD06ActionKind.LinkRecurrence && (input.LinkedDefectId is not null || input.PriorRepairDecisionId is not null || input.NewDefect is not null) ||
            kind != LD06ActionKind.IssueTransfer && input.ReceivingProjectId is not null ||
            kind is not (LD06ActionKind.IssueTransfer or LD06ActionKind.AcceptTransfer) && input.ScopeHash is not null ||
            kind is LD06ActionKind.IssueTransfer or LD06ActionKind.AcceptTransfer && input.DefectId is not null)
            return new(400, "validation_error");
        if (kind == LD06ActionKind.LinkRecurrence)
        {
            if (candidateDecisions is null || input.NewDefect is null || input.NewDefect.Decision != "KEEP_NEW" ||
                input.DefectId is null || input.PriorRepairDecisionId is null || input.LinkedDefectId is not null || input.EvidenceFileIds.Length == 0)
                return new(400, "validation_error");
            var request = input.NewDefect with { Recurrence = new(input.DefectId.Value, input.PriorRepairDecisionId.Value, input.EvidenceFileIds, expectedVersion[1..^1]) };
            var produced = await candidateDecisions.DecideAsync(actor, role, project, request, key, null, cancellationToken);
            if (produced.Code is not null) return new(produced.Status, produced.Code);
            var current = await repository.ReadAsync(actor, project, cancellationToken);
            return new(produced.Status, Value: current is null ? null : Map(current));
        }
        var result = await repository.ExecuteAsync(new(actor, role, project, kind.Value,
            new(input.Reason, input.EvidenceFileIds, input.SourceActionId, input.DefectId, input.LinkedDefectId,
                input.ObligationId, input.ReceivingProjectId, input.PriorRepairDecisionId, input.ScopeHash), key, expectedVersion[1..^1]), cancellationToken);
        return new(result.Status, result.Code, result.Value is null ? null : Map(result.Value));
    }
}
