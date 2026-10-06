using System.Text.Json;
using RoadGuardSystem.DTOs.Messaging;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.Services.Messaging;
using Xunit;

namespace RoadGuardSystem.UnitTests.Notifications;

public sealed class H6NotificationDispatchServiceTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly DateTimeOffset At = new(2026, 10, 6, 1, 0, 0, TimeSpan.Zero);
    [Fact]
    public async Task FiniteClaimSetExcludesOtherConsumersAndPassesParsedSourceToDurableRepository()
    {
        var repo = new RecordingRepository(Claim());
        var result = await new H6NotificationDispatchService(repo).ProcessOneAsync();
        Assert.Equal("COMMITTED", result.Status);
        Assert.Equal(H6NotificationCatalog.MessageTypes, repo.RegisteredTypes);
        Assert.DoesNotContain("Anh02.Export.Create", repo.RegisteredTypes!);
        Assert.DoesNotContain("processing.requested.v1", repo.RegisteredTypes!);
        Assert.Equal(repo.Claim!.Id, repo.Plan!.Source.EventId);
        Assert.Equal(repo.Claim.MessageType, repo.Plan.MessageType);
        Assert.Null(repo.RejectedReason);
    }
    [Fact]
    public async Task InvalidEnvelopeUsesSanitizedFailureBoundaryAndNeverDispatches()
    {
        var repo = new RecordingRepository(Claim() with { PayloadJson = "{\"credential\":\"private-value\"}" });
        var result = await new H6NotificationDispatchService(repo).ProcessOneAsync();
        Assert.Equal("REJECTED", result.Status);
        Assert.Equal("notification_envelope_invalid", repo.RejectedReason);
        Assert.Null(repo.Plan);
        Assert.DoesNotContain("private-value", result.ReasonCode!);
    }
    [Fact]
    public async Task CancellationIsNotRecordedAsAProtocolFailure()
    {
        var repo = new RecordingRepository(Claim()); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new H6NotificationDispatchService(repo).ProcessOneAsync(cancellation.Token));
        Assert.Null(repo.RejectedReason); Assert.Null(repo.Plan);
    }
    [Fact]
    public async Task EmptyOwnedQueueDoesNotCreateAnEffectOrFailure()
    {
        var repo = new RecordingRepository(null);
        var result = await new H6NotificationDispatchService(repo).ProcessOneAsync();
        Assert.Equal("IDLE", result.Status); Assert.NotNull(repo.RegisteredTypes);
        Assert.Null(repo.Plan); Assert.Null(repo.RejectedReason);
    }
    private static H6Claim Claim()
    {
        var value = new H6NotificationEventDto(1, Guid.NewGuid(), "ASSIGNED", Guid.NewGuid(), "FieldTask",
            Guid.NewGuid(), Guid.NewGuid(), At, ResponsibleUserId: Guid.NewGuid());
        return new(value.EventId, "field.task.assigned.v1", At, JsonSerializer.Serialize(value, Json), Guid.NewGuid(), At.AddMinutes(1), 1);
    }
    // This records only the service-to-repository protocol. It does not prove SQL delivery or producer authority.
    private sealed class RecordingRepository(H6Claim? claim) : IH6NotificationDispatchRepository
    {
        public H6Claim? Claim { get; } = claim;
        public IReadOnlyList<string>? RegisteredTypes { get; private set; }
        public H6DispatchPlan? Plan { get; private set; }
        public string? RejectedReason { get; private set; }
        public Task<H6Claim?> ClaimAsync(IReadOnlyList<string> registeredTypes, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); RegisteredTypes = registeredTypes; return Task.FromResult(Claim); }
        public Task<H6DispatchOutcome> DispatchAsync(H6Claim claim, H6DispatchPlan plan, CancellationToken cancellationToken)
        { Plan = plan; return Task.FromResult(new H6DispatchOutcome("COMMITTED", claim.Id)); }
        public Task<H6DispatchOutcome> RejectAsync(H6Claim claim, string reasonCode, CancellationToken cancellationToken)
        { RejectedReason = reasonCode; return Task.FromResult(new H6DispatchOutcome("REJECTED", claim.Id, ReasonCode: reasonCode)); }
        public Task<int> RetryUnresolvedAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> ObserveClocksAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int> ObserveCalendarAsync(Guid schedulerRunId,
            RoadGuardSystem.BusinessObjects.Messaging.NotificationScheduledCallbackWitness? witness,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
