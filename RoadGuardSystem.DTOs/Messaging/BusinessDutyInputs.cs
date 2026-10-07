namespace RoadGuardSystem.DTOs.Messaging;

public sealed record BusinessRequestAckInput(DateTimeOffset? ClaimedDeviceAt);
public sealed record BusinessDutyAppointmentInput(Guid AssigneeId, string Reason);
public sealed record ClockExtensionInput(DateTimeOffset NewDueAt, string Reason);
