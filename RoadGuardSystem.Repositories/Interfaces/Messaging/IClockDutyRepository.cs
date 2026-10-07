using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Messaging;

public sealed record ClockDutyCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid ClockId,
    string Action, string Key, string ExpectedVersion, string Reason, DateTimeOffset? NewDueAt, Guid? AssigneeId);
public interface IClockDutyRepository
{
    Task<BusinessDutyResult> ExecuteAsync(ClockDutyCommand command, CancellationToken token);
}
