using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RoadGuardSystem.BusinessObjects.Processing.Worker;
using RoadGuardSystem.Repositories.Configurations.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class AiWorkerLedgerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LedgerMappingFencesConcurrencyAndImmutableIdentityWithoutCascades()
    {
        var model = new ModelBuilder();
        new AiVideoWorkItemConfiguration().Configure(model.Entity<AiVideoWorkItem>());
        new AiWorkerArtifactConfiguration().Configure(model.Entity<AiWorkerArtifact>());
        var item = model.Entity<AiVideoWorkItem>().Metadata;
        item.GetIndexes().Should().Contain(i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == nameof(AiVideoWorkItem.DispatchKey));
        item.FindProperty(nameof(AiVideoWorkItem.DatasetId))!.GetAfterSaveBehavior().Should().Be(PropertySaveBehavior.Throw);
        item.FindProperty(nameof(AiVideoWorkItem.RowVersion))!.IsConcurrencyToken.Should().BeTrue();
        item.GetForeignKeys().Should().OnlyContain(fk => fk.DeleteBehavior == DeleteBehavior.Restrict);
        var artifact = model.Entity<AiWorkerArtifact>().Metadata;
        artifact.FindProperty(nameof(AiWorkerArtifact.Sha256))!.GetAfterSaveBehavior().Should().Be(PropertySaveBehavior.Throw);
        artifact.GetForeignKeys().Should().OnlyContain(fk => fk.DeleteBehavior == DeleteBehavior.Restrict);
        artifact.GetIndexes().Should().Contain(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(new[] { "WorkItemId", "AttemptId", "ArtifactId" }));
    }

    [Fact]
    public void DispatchIdentityIgnoresJobAndAttemptButSeparatesVideoAndConfiguration()
    {
        var dataset = Guid.NewGuid(); var video = Guid.NewGuid();
        var first = New(dataset, video);
        New(dataset, video).DispatchKey.Should().Be(first.DispatchKey);
        New(dataset, Guid.NewGuid()).DispatchKey.Should().NotBe(first.DispatchKey);
        New(dataset, video, "config-2").DispatchKey.Should().NotBe(first.DispatchKey);
    }

    [Fact]
    public void AdmissionIsCanonicalAndRejectsDuplicateProperties()
    {
        New(json: "{\"z\":1,\"a\":{\"y\":2,\"x\":3}}").AdmissionJson.Should().Be("{\"a\":{\"x\":3,\"y\":2},\"z\":1}");
        Action duplicate = () => New(json: "{\"x\":1,\"x\":2}");
        duplicate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LeaseTakeoverFencesExpiredOwnerAndWrongAttempt()
    {
        var item = New();
        item.AcquireLease("first", Now, Now.AddMinutes(1));
        Action occupied = () => item.AcquireLease("second", Now, Now.AddMinutes(2));
        occupied.Should().Throw<InvalidOperationException>();
        item.AcquireLease("second", Now.AddMinutes(1), Now.AddMinutes(2));
        Action stale = () => item.Transition("first", item.CurrentAttemptId, AiVideoWorkState.Admitted, Now.AddMinutes(1));
        Action wrongAttempt = () => item.Transition("second", Guid.NewGuid(), AiVideoWorkState.Admitted, Now.AddMinutes(1));
        stale.Should().Throw<InvalidOperationException>();
        wrongAttempt.Should().Throw<InvalidOperationException>();
        item.Transition("second", item.CurrentAttemptId, AiVideoWorkState.Admitted, Now.AddMinutes(1));
        item.State.Should().Be(AiVideoWorkState.Admitted);
    }

    [Fact]
    public void CompletionRequiresArtifactsAndCannotResurrect()
    {
        var item = New(); item.AcquireLease("worker", Now, Now.AddMinutes(1));
        Action shortcut = () => item.Transition("worker", item.CurrentAttemptId, AiVideoWorkState.Ingested, Now);
        shortcut.Should().Throw<InvalidOperationException>();
        foreach (var state in new[] { AiVideoWorkState.Admitted, AiVideoWorkState.Running, AiVideoWorkState.ArtifactsReady, AiVideoWorkState.Ingested })
            item.Transition("worker", item.CurrentAttemptId, state, Now);
        Action resurrect = () => item.AcquireLease("worker", Now, Now.AddMinutes(1));
        Action retry = () => item.AdvanceAttempt(Guid.NewGuid(), "{}", Now);
        resurrect.Should().Throw<InvalidOperationException>(); retry.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RetryRequiresExplicitNewAttemptAndClearsOldLease()
    {
        var item = New(); var old = item.CurrentAttemptId;
        item.AcquireLease("worker", Now, Now.AddMinutes(1));
        item.Transition("worker", old, AiVideoWorkState.RetryableFailed, Now, errorCode: "TRANSPORT_FAILED");
        Action retrySame = () => item.AdvanceAttempt(old, "{}", Now);
        retrySame.Should().Throw<ArgumentException>();
        Action auto = () => item.AcquireLease("worker", Now, Now.AddMinutes(1));
        auto.Should().Throw<InvalidOperationException>();
        item.AdvanceAttempt(Guid.NewGuid(), "{\"attempt\":2}", Now);
        item.State.Should().Be(AiVideoWorkState.Pending); item.CurrentAttemptId.Should().NotBe(old);
        item.LeaseOwner.Should().BeNull(); item.ErrorCode.Should().BeNull();
    }

    [Theory]
    [InlineData(AiVideoWorkState.TerminalFailed)]
    [InlineData(AiVideoWorkState.Cancelled)]
    public void TerminalFailureAndCancellationCannotRetry(AiVideoWorkState state)
    {
        var item = New(); item.AcquireLease("worker", Now, Now.AddMinutes(1));
        item.Transition("worker", item.CurrentAttemptId, state, Now, errorCode: state == AiVideoWorkState.TerminalFailed ? "INVALID_INPUT" : null);
        Action retry = () => item.AdvanceAttempt(Guid.NewGuid(), "{}", Now);
        retry.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ArtifactRegistrationIsStableAndPathsAreNotAccepted()
    {
        var artifact = Artifact(); var file = Guid.NewGuid();
        artifact.MarkRegistered(file); artifact.MarkRegistered(file);
        artifact.FileId.Should().Be(file);
        Action remap = () => artifact.MarkRegistered(Guid.NewGuid());
        Action path = () => Artifact("../../private");
        Action emptyBytes = () => Artifact(bytes: 0);
        Action badFrame = () => Artifact(kind: "FRAME", width: null, height: null);
        remap.Should().Throw<InvalidOperationException>(); path.Should().Throw<ArgumentException>();
        emptyBytes.Should().Throw<ArgumentException>(); badFrame.Should().Throw<ArgumentException>();
    }

    private static AiVideoWorkItem New(Guid? dataset = null, Guid? video = null, string config = "config-1", string json = "{}")
        => AiVideoWorkItem.Create(Guid.NewGuid(), dataset ?? Guid.NewGuid(), video ?? Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), config, json, Now);

    private static AiWorkerArtifact Artifact(string? artifactId = null, long bytes = 3, string kind = "RAW_RESULT", int? width = null, int? height = null)
        => AiWorkerArtifact.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), artifactId ?? Guid.NewGuid().ToString("D"), new string('a', 64), bytes, kind, width, height);
}
