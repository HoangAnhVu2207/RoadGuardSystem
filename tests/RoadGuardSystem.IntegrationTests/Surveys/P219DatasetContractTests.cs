using FluentAssertions;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Surveys;

[Trait("TaskId", "P2-019")]
public sealed class P219DatasetContractTests
{
    [Fact(DisplayName = "P2-019: submitted dataset keeps verified source manifest and declared scope")]
    public void CreateSubmitted_ValidInput_PreservesSubmissionFacts()
    {
        var recordedAt = new DateTimeOffset(2026, 9, 29, 8, 0, 0, TimeSpan.FromHours(7));

        var dataset = SurveyDataVersion.CreateSubmitted(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            recordedAt,
            recordedAt.AddMinutes(3),
            Guid.NewGuid(),
            "[{\"fileId\":\"00000000-0000-0000-0000-000000000001\",\"kind\":\"VIDEO\",\"checksumSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"}]",
            "[{\"routeVersionId\":\"00000000-0000-0000-0000-000000000002\",\"segmentSetId\":\"00000000-0000-0000-0000-000000000003\",\"segmentIds\":[\"00000000-0000-0000-0000-000000000004\"],\"targetBand\":\"SURFACE\"}]");

        dataset.Status.Should().Be(SurveyDataVersionStatus.ServerConfirmed);
        dataset.IntegrityStatus.Should().Be(SurveyDataIntegrityStatus.Passed);
        dataset.RecordedAt.Should().Be(recordedAt.ToUniversalTime());
        dataset.DeviceId.Should().NotBe(Guid.Empty);
        dataset.ScopeManifest.Should().Contain("targetBand");
    }
}
