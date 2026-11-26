using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RepairSourceEvidenceFact(Guid EvidenceId, string Kind, DateTimeOffset? CapturedAt,
    FieldTaskEvidenceFile File, AiCandidateReadFacts? AiProof = null);
public sealed record RepairSourceEvidenceResult(int Status, string? Code = null, RepairSourceEvidenceFact[]? Items = null,
    string[]? MissingReasons = null);
public interface IRepairSourceEvidenceRepository
{
    Task<RepairSourceEvidenceResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid package,
        Guid item, Guid? evidence, CancellationToken token);
}
