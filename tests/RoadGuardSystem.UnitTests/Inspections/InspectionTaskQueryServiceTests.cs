using FluentAssertions;
using RoadGuardSystem.Repositories.Inspections;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Inspections;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Inspections;

[Trait("TaskId", "V2-P1-063")]
public sealed class InspectionTaskQueryServiceTests
{
    [Fact]
    public async Task ListAssignedAsync_RepairCrewWithScope_ReturnsProjectedPage()
    {
        var actorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var item = new InspectionTaskPersistenceView(
            Guid.NewGuid(), projectId, Guid.NewGuid(), actorId,
            FieldInspectionTaskStatus.NewAssigned, 1, "FIT-001", DateTimeOffset.UtcNow);
        var secondItem = item with { Id = Guid.NewGuid(), DefectId = Guid.NewGuid(), TaskCode = "FIT-002" };
        var repository = new RecordingRepository(item, secondItem);
        var scopeGuard = new FixedScopeGuard(projectId);
        var service = new InspectionTaskQueryService(
            repository,
            scopeGuard);

        var result = await service.ListAssignedAsync(actorId, UserRoleCode.RepairCrew, null, 50);

        result.Status.Should().Be(InspectionTaskQueryStatus.Success);
        result.Page!.Items.Should().HaveCount(2);
        result.Page.Items[0].DefectIds.Should().Contain(item.DefectId);
        result.Page.Items[0].Mode.Should().Be("MEASURE_ONLY");
        result.Page.Items[0].Version.Should().Be(item.TaskCode);
        scopeGuard.Calls.Should().Be(1);
    }

    [Fact]
    public async Task ListAssignedAsync_NonCrew_IsForbiddenWithoutRepositoryRead()
    {
        var repository = new RecordingRepository();
        var service = new InspectionTaskQueryService(repository, new FixedScopeGuard(Guid.NewGuid()));

        var result = await service.ListAssignedAsync(
            Guid.NewGuid(), UserRoleCode.ProjectManager, null, 50);

        result.Status.Should().Be(InspectionTaskQueryStatus.Forbidden);
        repository.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ListAssignedAsync_InvalidCursor_IsRejected()
    {
        var repository = new RecordingRepository();
        var service = new InspectionTaskQueryService(repository, new FixedScopeGuard(Guid.NewGuid()));

        var result = await service.ListAssignedAsync(
            Guid.NewGuid(), UserRoleCode.RepairCrew, "bad-cursor", 50);

        result.Status.Should().Be(InspectionTaskQueryStatus.InvalidCursor);
        repository.Calls.Should().Be(0);
    }

    private sealed class RecordingRepository : IInspectionTaskReadRepository
    {
        private readonly InspectionTaskPersistenceView[] _items;
        public int Calls { get; private set; }

        public RecordingRepository(params InspectionTaskPersistenceView[] items)
        {
            _items = items;
        }

        public Task<InspectionTaskPagePersistenceResult> ListAssignedAsync(
            Guid assignedToUserId,
            DateTimeOffset? afterDueAt,
            Guid? afterId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            var last = _items.Length == 0 ? null : _items[^1];
            return Task.FromResult(new InspectionTaskPagePersistenceResult(
                _items, last?.DueAt, last?.Id, false, DateTimeOffset.UtcNow));
        }
    }

    private sealed class FixedScopeGuard : IProjectScopeGuard
    {
        private readonly Guid _projectId;
        public int Calls { get; private set; }

        public FixedScopeGuard(Guid projectId)
        {
            _projectId = projectId;
        }

        public Task<ProjectAccessScope?> AuthorizeAsync(
            Guid userId,
            UserRoleCode authoritativeRole,
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<ProjectAccessScope?>(
                projectId == _projectId
                    ? new ProjectAccessScope(projectId, authoritativeRole, Guid.NewGuid())
                    : null);
        }
    }
}
