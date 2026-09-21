using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Projects;

public sealed class PrimaryProjectManagerService : IPrimaryProjectManagerService
{
    private readonly IPrimaryProjectManagerRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PrimaryProjectManagerService(
        IPrimaryProjectManagerRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PrimaryProjectManagerReassignmentResult> ReassignAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        Guid projectId,
        ReassignPrimaryProjectManagerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (actorRole != UserRoleCode.Supervisor)
        {
            return new(PrimaryProjectManagerReassignmentStatus.Forbidden);
        }

        if (actorUserId == Guid.Empty || projectId == Guid.Empty || command.PrimaryProjectManagerUserId == Guid.Empty ||
            command.EffectiveFrom == default || command.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(command.Reason) ||
            !TryDecodeRowVersion(command.ExpectedCurrentMembershipRowVersion, out var expectedRowVersion))
        {
            return new(PrimaryProjectManagerReassignmentStatus.InvalidInput);
        }

        var facts = await _repository.GetFactsAsync(projectId, command.PrimaryProjectManagerUserId, cancellationToken);
        if (facts is null || !facts.ProjectExists || facts.CurrentMembershipId is null || facts.CurrentValidFrom is null)
        {
            return new(PrimaryProjectManagerReassignmentStatus.ProjectNotFound);
        }

        if (facts.ProjectIsClosed)
        {
            return new(PrimaryProjectManagerReassignmentStatus.ProjectClosed);
        }

        if (!facts.ReplacementUserIsActiveProjectManager || facts.CurrentProjectManagerUserId == command.PrimaryProjectManagerUserId ||
            command.EffectiveFrom <= facts.CurrentValidFrom)
        {
            return new(PrimaryProjectManagerReassignmentStatus.ReplacementProjectManagerNotFound);
        }

        var persisted = await _repository.ReassignAsync(new PrimaryProjectManagerWriteRequest(
            actorUserId, projectId, command.PrimaryProjectManagerUserId, command.EffectiveFrom,
            command.Reason, expectedRowVersion, command.OperationId, command.CorrelationId, _timeProvider.GetUtcNow()), cancellationToken);
        return persisted.Status switch
        {
            PrimaryProjectManagerWriteStatus.Success or PrimaryProjectManagerWriteStatus.Replayed when
                persisted.PreviousMembershipId is Guid previous && persisted.CurrentMembershipId is Guid current && persisted.CurrentRowVersion is not null =>
                new(persisted.Status == PrimaryProjectManagerWriteStatus.Success
                        ? PrimaryProjectManagerReassignmentStatus.Success
                        : PrimaryProjectManagerReassignmentStatus.Replayed,
                    new PrimaryProjectManagerReassignmentView(previous, current, Convert.ToBase64String(persisted.CurrentRowVersion))),
            PrimaryProjectManagerWriteStatus.StaleConcurrency => new(PrimaryProjectManagerReassignmentStatus.StaleConcurrency),
            PrimaryProjectManagerWriteStatus.IdempotentConflict => new(PrimaryProjectManagerReassignmentStatus.IdempotentConflict),
            PrimaryProjectManagerWriteStatus.NotFound => new(PrimaryProjectManagerReassignmentStatus.ProjectNotFound),
            _ => new(PrimaryProjectManagerReassignmentStatus.InvalidInput)
        };
    }

    private static bool TryDecodeRowVersion(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        try
        {
            rowVersion = string.IsNullOrWhiteSpace(value) ? [] : Convert.FromBase64String(value);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
