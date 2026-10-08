using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadGuardSystem.BusinessObjects.Messaging;
using RoadGuardSystem.BusinessObjects.Processing;
using RoadGuardSystem.Repositories;
using RoadGuardSystem.Repositories.Messaging;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Processing;

public sealed class P231ProcessingPersistenceTests
{
    [Fact(DisplayName = "P2-31: processing entity factories validate JSON and preserve UTC metadata")]
    public void ProcessingEntities_ValidateAndNormalizeMetadata()
    {
        var block = ProcessingBlock.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "{\"startFrame\":1,\"endFrame\":10}");
        var model = AIModelVersion.Create(
            Guid.NewGuid(),
            "roadguard-mock",
            "v1",
            "file:///models/mock-v1",
            "{\"precision\":1}",
            "{\"confidence\":0.5}",
            AIModelVersionStatus.Released,
            new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.FromHours(7)),
            Guid.NewGuid());
        var attempt = ProcessingAttempt.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.FromHours(7)),
            null,
            ProcessingAttemptErrorType.None,
            "worker-a");

        block.RangeMetadata.Should().Contain("startFrame");
        model.ReleasedAt.Should().Be(new DateTimeOffset(2026, 10, 1, 1, 0, 0, TimeSpan.Zero));
        attempt.StartedAt.Offset.Should().Be(TimeSpan.Zero);
    }

}
