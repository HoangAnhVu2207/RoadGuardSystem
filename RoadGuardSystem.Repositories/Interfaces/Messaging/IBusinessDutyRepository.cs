using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed record BusinessDutyCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid RequestId,
    string Action, string Key, string ExpectedVersion, Guid? AssigneeId, string? Reason, DateTimeOffset? ClaimedDeviceAt);
public sealed record BusinessDutyResult(int Status, string? Code = null, object? Value = null, string? Version = null, bool Replayed = false);
public sealed record BusinessDutyView(Guid Id, Guid ProjectId, string Kind, string SourceKind, Guid SourceId,
    string SourceVersion, Guid ScopeId, Guid? ResponsibleActorId, string ResponsibleRole, DateTimeOffset RequestedAt,
    string ReceivingState, double WaitingSeconds, Guid? AcknowledgmentId, Guid? AcknowledgedBy,
    DateTimeOffset? AcknowledgedAt, DateTimeOffset? ClaimedDeviceAt, Guid? ClockId, DateTimeOffset? OriginalDueAt,
    DateTimeOffset? CurrentDueAt, DateTimeOffset? CompletedAt, string Version, object[] Appointments);
public interface IBusinessDutyRepository
{
    Task<BusinessDutyResult> ExecuteAsync(BusinessDutyCommand c, CancellationToken token);
    Task<BusinessDutyResult> ReadAsync(Guid actor, UserRoleCode role, Guid project, Guid request, CancellationToken token);
    Task<BusinessDutyResult> ListAsync(Guid actor, UserRoleCode role, Guid project, Guid? scope, Guid? after, int limit, CancellationToken token);
}
