using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiVideoAttemptOutcomeTests
{
    [Theory]
    [InlineData(AiVideoWorkState.Ingested, null)]
    [InlineData(AiVideoWorkState.Cancelled, null)]
    [InlineData(AiVideoWorkState.RetryableFailed, "WORKER_UNAVAILABLE")]
    [InlineData(AiVideoWorkState.TerminalFailed, "INVALID_RESULT")]
    public void TerminalFactPreservesAttemptEvidence(AiVideoWorkState state, string? error)
    {
        var now = DateTimeOffset.UtcNow;
        var value = AiVideoAttemptOutcome.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), state, error, new string('a',64), now);
        value.State.Should().Be(state); value.ErrorCode.Should().Be(error); value.OccurredAt.Should().Be(now);
        typeof(AiVideoAttemptOutcome).GetProperties().Should().OnlyContain(p => p.SetMethod == null || !p.SetMethod.IsPublic);
    }

    [Theory]
    [InlineData(AiVideoWorkState.Pending, null)]
    [InlineData(AiVideoWorkState.Running, null)]
    [InlineData(AiVideoWorkState.ArtifactsReady, null)]
    [InlineData(AiVideoWorkState.Ingested, "INVALID_RESULT")]
    [InlineData(AiVideoWorkState.Cancelled, "CANCELLED")]
    [InlineData(AiVideoWorkState.RetryableFailed, null)]
    [InlineData(AiVideoWorkState.TerminalFailed, "private value")]
    public void RejectsNonterminalOrUnsafeOutcome(AiVideoWorkState state, string? error)
        => FluentActions.Invoking(() => AiVideoAttemptOutcome.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), state, error, new string('a',64), DateTimeOffset.UtcNow))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void WorkRetryCannotErasePriorTerminalFact()
    {
        var now = DateTimeOffset.UtcNow; var attempt = Guid.NewGuid();
        var work = AiVideoWorkItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), attempt, "config", "{}", now);
        work.AcquireLease("worker", now, now.AddMinutes(1));
        work.Transition("worker", attempt, AiVideoWorkState.RetryableFailed, now, errorCode: "WORKER_UNAVAILABLE");
        var outcome = AiVideoAttemptOutcome.Create(attempt, work.Id, work.ProcessingJobId, work.State, work.ErrorCode, new string('a',64), now);
        work.AdvanceAttempt(Guid.NewGuid(), "{\"attempt\":2}", now.AddSeconds(1));
        work.State.Should().Be(AiVideoWorkState.Pending); work.ErrorCode.Should().BeNull();
        outcome.AttemptId.Should().Be(attempt); outcome.State.Should().Be(AiVideoWorkState.RetryableFailed);
        outcome.ErrorCode.Should().Be("WORKER_UNAVAILABLE"); outcome.AdmissionHash.Should().Be(new string('a',64));
    }
}
