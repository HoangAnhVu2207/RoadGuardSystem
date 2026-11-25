using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.DTOs.Web;
using RoadGuardSystem.Repositories.Repairs;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Services.Processing.Anh02;

namespace RoadGuardSystem.Services.Repairs;

public sealed record RepairSourceEvidenceRead(int Status, string? Code = null,
    DefectSourceEvidence[]? Evidence = null, FieldTaskEvidenceFile? File = null, string[]? MissingReasons = null);
public sealed class RepairSourceEvidenceService(IRepairSourceEvidenceRepository repository)
{
    public async Task<RepairSourceEvidenceRead> ReadAsync(Guid actor, UserRoleCode role, Guid project,
        Guid package, Guid item, Guid? evidence, CancellationToken token)
    {
        if (role is not (UserRoleCode.ProjectManager or UserRoleCode.Supervisor)) return new(403, "access_forbidden");
        if (new[] { actor, project, package, item }.Any(id => id == Guid.Empty) || evidence == Guid.Empty) return new(400, "validation_error");
        var result = await repository.ReadAsync(actor, role, project, package, item, evidence, token);
        if (result.Code is not null) return new(result.Status, result.Code);
        var items = result.Items ?? [];
        var verified = items.Where(row => row.AiProof is null || Anh02AiService.ValidCandidateProof(row.AiProof, row.AiProof.Detection.Id)).ToArray();
        var missing = (result.MissingReasons ?? []).ToList();
        if (verified.Length != items.Length) missing.Add("AI_SOURCE_UNAVAILABLE");
        if (evidence is not null && verified.Length == 0) return new(404, "not_found");
        var root = $"/api/v1/projects/{project:D}/repair-packages/{package:D}/items/{item:D}/source-evidence";
        return new(200, Evidence: verified.Select(row => new DefectSourceEvidence(row.EvidenceId,
            $"{root}/{row.EvidenceId:D}/content", row.Kind, row.CapturedAt)).ToArray(),
            File: evidence is not null ? verified.Single().File : null, MissingReasons: missing.Distinct().ToArray());
    }
}
