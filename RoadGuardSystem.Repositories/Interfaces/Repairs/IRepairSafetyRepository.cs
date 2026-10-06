using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Repairs;

public sealed record SafetyCreateCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId, Guid ItemId,
    Guid FormalObligationId, Guid SafetyObligationId, Guid ResponsibleActorId, string CheckSchedule,
    string ReplacementCondition, string RemovalCondition, string Reason, string Key, string ExpectedItemVersion);
public sealed record SafetyInstallCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId, Guid ItemId,
    Guid MeasureId, DateTimeOffset FirstCheckDueAt, string Reason, string Key, string ExpectedVersion);
public sealed record SafetyCheckCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId, Guid ItemId,
    Guid MeasureId, string Result, string Findings, Guid[] EvidenceLinkIds, string Key, string ExpectedVersion);
public sealed record SafetyAckCommand(Guid ActorId, UserRoleCode Role, Guid ProjectId, Guid PackageId, Guid ItemId,
    Guid MeasureId, Guid WarningId, string Reason, string Key, string ExpectedVersion);
public sealed record SafetyFact(Guid Id, Guid ProjectId, Guid DefectId, Guid FormalObligationId, Guid SafetyObligationId,
    Guid ResponsibleActorId, DateTimeOffset? InstalledAt, DateTimeOffset? FirstCheckDueAt, Guid? CurrentCheckId,
    SafetyCheckFact[] Checks, SafetyWarningFact[] Warnings, SafetyAckFact[] Acknowledgements, string Version);
public sealed record SafetyCheckFact(Guid Id, Guid ActorId, DateTimeOffset At, string Result, string Findings, Guid[] EvidenceLinkIds);
public sealed record SafetyWarningFact(Guid Id, Guid SourceCheckId, Guid IntendedActorId, DateTimeOffset ServerReceivedAt,
    DateTimeOffset OriginalDueAt, string Reason);
public sealed record SafetyAckFact(Guid Id, Guid WarningId, Guid ActorId, DateTimeOffset At, bool AfterOriginalDue);
public sealed record SafetyResult(int Status, string? Code = null, SafetyFact? Value = null, bool Replayed = false);
public interface IRepairSafetyRepository
{
    Task<SafetyResult> ReadAsync(Guid actorId, UserRoleCode role, Guid projectId, Guid packageId, Guid itemId,
        Guid measureId, CancellationToken cancellationToken);
    Task<SafetyResult> CreateAsync(SafetyCreateCommand command, CancellationToken cancellationToken);
    Task<SafetyResult> InstallAsync(SafetyInstallCommand command, CancellationToken cancellationToken);
    Task<SafetyResult> CheckAsync(SafetyCheckCommand command, CancellationToken cancellationToken);
    Task<SafetyResult> AcknowledgeAsync(SafetyAckCommand command, CancellationToken cancellationToken);
}
