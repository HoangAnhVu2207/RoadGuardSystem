using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Repairs;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record RepairPolicyDefinition(string DefectTypeCode, string ChecklistVersion,
    RepairMeasurementRule[] Measurements, string[] StopConditions, string Reason);
public sealed record RepairPolicyCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, string Action,
    Guid? ResourceId, RepairPolicyDefinition? Definition, string? Reason, string? Key, string? ExpectedVersion);
public sealed record RepairPolicyRecord(Guid Id, Guid ProjectId, Guid? CurrentChangeId, Guid? PublishedRevisionId,
    int? Revision, string DefectTypeCode, string ChecklistVersion, RepairMeasurementRule[] Measurements,
    string[] StopConditions, string State, Guid ActorId, DateTimeOffset At, string Reason, string Version);
public sealed record RepairPolicyResult(int Status, string? Code = null, RepairPolicyRecord? Value = null,
    bool Replayed = false);
public interface IRepairPolicyRepository
{
    Task<RepairPolicyResult> ExecuteAsync(RepairPolicyCommand command, CancellationToken cancellationToken);
}
