using FluentAssertions;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Projects;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

public sealed class PrimaryProjectManagerServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReassignAsync_FutureEffectiveDate_IsRejectedWithoutWrite()
    {
        var repository = new RecordingRepository();
        var service = new PrimaryProjectManagerService(repository, new FixedTimeProvider(Now));

        var result = await service.ReassignAsync(
            Guid.NewGuid(),
            UserRoleCode.Supervisor,
            repository.ProjectId,
            Command(repository.ReplacementId, new DateOnly(2026, 9, 30)));

        result.Status.Should().Be(PrimaryProjectManagerReassignmentStatus.InvalidInput);
        repository.WriteRequest.Should().BeNull();
    }

    [Fact]
    public async Task ReassignAsync_ExistingReplay_IsAllowedAfterEffectiveDate()
    {
        var repository = new RecordingRepository { ReplayExists = true };
        var service = new PrimaryProjectManagerService(repository, new FixedTimeProvider(Now.AddDays(1)));

        var result = await service.ReassignAsync(
            Guid.NewGuid(),
            UserRoleCode.Supervisor,
            repository.ProjectId,
            Command(repository.ReplacementId, new DateOnly(2026, 9, 29)));

        result.Status.Should().Be(PrimaryProjectManagerReassignmentStatus.Replayed);
        repository.WriteRequest.Should().NotBeNull();
    }

    private static ReassignPrimaryProjectManagerCommand Command(Guid replacementId, DateOnly effectiveFrom) =>
        new(replacementId, effectiveFrom, "same-day handover", Convert.ToBase64String(new byte[8]), Guid.NewGuid(), null);

    private sealed class RecordingRepository : IPrimaryProjectManagerRepository
    {
        public Guid ProjectId { get; } = Guid.NewGuid();
        public Guid ReplacementId { get; } = Guid.NewGuid();
        public bool ReplayExists { get; init; }
        public PrimaryProjectManagerWriteRequest? WriteRequest { get; private set; }

        public Task<PrimaryProjectManagerFacts?> GetFactsAsync(Guid projectId, Guid replacementProjectManagerUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PrimaryProjectManagerFacts?>(new(ProjectId, true, false, true, Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 1), new byte[8]));

        public Task<bool> HasReplayAsync(Guid actorUserId, Guid projectId, string idempotencyKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(ReplayExists);

        public Task<PrimaryProjectManagerWriteResult> ReassignAsync(PrimaryProjectManagerWriteRequest request, CancellationToken cancellationToken = default)
        {
            WriteRequest = request;
            return Task.FromResult(new PrimaryProjectManagerWriteResult(
                ReplayExists ? PrimaryProjectManagerWriteStatus.Replayed : PrimaryProjectManagerWriteStatus.Success,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new byte[8]));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
