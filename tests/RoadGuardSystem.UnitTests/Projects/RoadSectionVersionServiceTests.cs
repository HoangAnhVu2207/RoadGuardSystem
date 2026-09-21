using FluentAssertions;
using NetTopologySuite.Geometries;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Projects;
using Xunit;

namespace RoadGuardSystem.UnitTests.Projects;

[Trait("TaskId", "P1-72")]
public sealed class RoadSectionVersionServiceTests
{
    [Fact]
    public async Task CreateInitialAsync_RejectsGeometryWithDifferentProjectSrid()
    {
        var projectId = Guid.NewGuid();
        var section = RoadSection.Create(Guid.NewGuid(), projectId, "SRID-ROAD");
        var geometry = new GeometryFactory(new PrecisionModel(), 32649).CreateLineString(
        [
            new Coordinate(588500, 2325000),
            new Coordinate(588600, 2325100)
        ]);
        var version = RoadSectionVersion.Create(
            Guid.NewGuid(),
            section.Id,
            1,
            true,
            geometry,
            DateTimeOffset.UtcNow,
            "Initial version");
        var repository = new RoadSectionVersionRepositoryStub(
            new RoadSectionVersionFacts(projectId, 32648, null, null));
        var service = new RoadSectionVersionService(repository);

        var action = () => service.CreateInitialAsync(section, version);

        await action.Should().ThrowAsync<ArgumentException>();
        repository.CreateInitialCalled.Should().BeFalse();
    }

    private sealed class RoadSectionVersionRepositoryStub : IRoadSectionVersionRepository
    {
        private readonly RoadSectionVersionFacts _initialFacts;

        public RoadSectionVersionRepositoryStub(RoadSectionVersionFacts initialFacts)
        {
            _initialFacts = initialFacts;
        }

        public bool CreateInitialCalled { get; private set; }

        public Task<RoadSectionVersionWriteResult> CreateInitialAsync(
            RoadSectionInitialVersionWriteRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<RoadSectionVersionWriteResult> CreateNextAsync(
            RoadSectionNextVersionWriteRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<RoadSectionVersionFacts?> GetInitialFactsAsync(
            Guid projectId,
            CancellationToken cancellationToken = default) => Task.FromResult<RoadSectionVersionFacts?>(_initialFacts);

        public Task<RoadSectionVersionFacts?> GetNextFactsAsync(
            Guid roadSectionId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task CreateInitialAsync(
            RoadSection section,
            RoadSectionVersion initialVersion,
            CancellationToken cancellationToken = default)
        {
            CreateInitialCalled = true;
            return Task.CompletedTask;
        }

        public Task AddVersionAndMakeCurrentAsync(
            Guid roadSectionId,
            RoadSectionVersion nextVersion,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
