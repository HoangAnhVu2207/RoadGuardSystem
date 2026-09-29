using FluentAssertions;
using System.Text.Json;
using RoadGuardSystem.DTOs.Surveys;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Surveys;

[Trait("TaskId", "P2-054/P2-011")]
public sealed class P2V2SurveyServiceTests
{
    [Fact]
    public async Task CreatePlanAsync_AuthorizedPm_PassesScopeAndIdempotencyToRepository()
    {
        var actor = Guid.NewGuid();
        var project = Guid.NewGuid();
        var routeVersion = Guid.NewGuid();
        var repository = new RecordingRepository
        {
            PlanResult = new(SurveyV2PersistenceStatus.Success, new(Guid.NewGuid(), project, "[]", DateTimeOffset.UtcNow, "Planned", "v1"))
        };
        var service = new SurveyV2Service(repository, new FixedScopeGuard(project, UserRoleCode.ProjectManager));

        var result = await service.CreatePlanAsync(
            actor,
            UserRoleCode.ProjectManager,
            project,
            new CreateSurveyPlanV2RequestDto([new BandScopeDto(routeVersion, Guid.NewGuid(), [Guid.NewGuid()], "SURFACE")], DateTimeOffset.UtcNow, "BASELINE"),
            "plan-key",
            null);

        result.Status.Should().Be(SurveyV2ServiceStatus.Success);
        repository.PlanRequest!.RouteVersionId.Should().Be(routeVersion);
        repository.PlanRequest.Scope.Should().ContainSingle();
        repository.PlanRequest.IdempotencyKey.Should().Be("plan-key");
    }

    [Fact]
    public async Task CreateTaskAsync_NonPm_ReturnsForbiddenWithoutPersisting()
    {
        var repository = new RecordingRepository();
        var service = new SurveyV2Service(repository, new FixedScopeGuard(Guid.NewGuid(), UserRoleCode.DroneOperator));

        var result = await service.CreateTaskAsync(
            Guid.NewGuid(),
            UserRoleCode.DroneOperator,
            Guid.NewGuid(),
            new CreateSurveyTaskV2RequestDto([new BandScopeDto(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], "SURFACE")], "BASELINE", Guid.NewGuid(), null, null),
            "task-key",
            null);

        result.Status.Should().Be(SurveyV2ServiceStatus.Forbidden);
        repository.TaskRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetTaskAsync_UsesV2CanonicalStatusAndSchema()
    {
        var actor = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var repository = new RecordingRepository
        {
            ReadTask = new(taskId, Guid.NewGuid(), "[]", actor, "NewAssigned", "version-1", DateTimeOffset.UtcNow, "{\"source\":\"MANUAL\"}")
        };
        var service = new SurveyV2Service(repository, new FixedScopeGuard(Guid.NewGuid(), UserRoleCode.DroneOperator));

        var result = await service.GetTaskAsync(actor, UserRoleCode.DroneOperator, taskId);

        result.Status.Should().Be(SurveyV2ServiceStatus.Success);
        result.Task!.Status.Should().Be("NEW_ASSIGNED");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Task));
        json.RootElement.TryGetProperty("dueAt", out _).Should().BeFalse();
        json.RootElement.TryGetProperty("accessPoint", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetTaskAsync_BindsCamelCasePersistedScope()
    {
        var actor = Guid.NewGuid();
        var route = Guid.NewGuid();
        var set = Guid.NewGuid();
        var segment = Guid.NewGuid();
        var repository = new RecordingRepository
        {
            ReadTask = new(Guid.NewGuid(), Guid.NewGuid(),
                $"{{\"scope\":[{{\"routeVersionId\":\"{route}\",\"segmentSetId\":\"{set}\",\"segmentIds\":[\"{segment}\"],\"targetBand\":\"SURFACE\"}}]}}",
                actor, "NewAssigned", "version-1", null, null)
        };
        var service = new SurveyV2Service(repository, new FixedScopeGuard(Guid.NewGuid(), UserRoleCode.DroneOperator));

        var result = await service.GetTaskAsync(actor, UserRoleCode.DroneOperator, repository.ReadTask.Id);

        result.Task!.Scope.Should().ContainSingle();
        result.Task.Scope[0].RouteVersionId.Should().Be(route);
        result.Task.Scope[0].SegmentSetId.Should().Be(set);
        result.Task.Scope[0].SegmentIds.Should().ContainSingle();
        result.Task.Scope[0].SegmentIds[0].Should().Be(segment);
        result.Task.Scope[0].TargetBand.Should().Be("SURFACE");
    }

    private sealed class RecordingRepository : ISurveyV2Repository
    {
        public SurveyV2PlanPersistenceResult PlanResult { get; init; } = new(SurveyV2PersistenceStatus.InvalidInput);
        public SurveyV2PlanCreationRequest? PlanRequest { get; private set; }
        public SurveyV2TaskCreationRequest? TaskRequest { get; private set; }
        public SurveyV2TaskPersistenceView? ReadTask { get; init; }
        public Task<SurveyV2PlanPersistenceResult> CreatePlanAsync(SurveyV2PlanCreationRequest request, CancellationToken cancellationToken = default) { PlanRequest = request; return Task.FromResult(PlanResult); }
        public Task<SurveyV2PlanPersistenceResult> PostponePlanAsync(SurveyV2PlanPostponementRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new SurveyV2PlanPersistenceResult(SurveyV2PersistenceStatus.InvalidInput));
        public Task<SurveyV2TaskPersistenceResult> CreateTaskAsync(SurveyV2TaskCreationRequest request, CancellationToken cancellationToken = default) { TaskRequest = request; return Task.FromResult(new SurveyV2TaskPersistenceResult(SurveyV2PersistenceStatus.InvalidInput)); }
        public Task<SurveyV2TaskPersistenceView?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default) => Task.FromResult(ReadTask);
        public Task<SurveyV2TaskPagePersistenceResult> ListMyTasksAsync(Guid operatorUserId, string? cursor, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult(new SurveyV2TaskPagePersistenceResult([], null, DateTimeOffset.UtcNow));
        public Task<SurveyV2TaskPersistenceResult> MutateTaskAsync(SurveyV2TaskMutationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new SurveyV2TaskPersistenceResult(SurveyV2PersistenceStatus.InvalidInput));
        public Task<Guid?> GetPlanProjectIdAsync(Guid planId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
    }

    private sealed class FixedScopeGuard(Guid projectId, UserRoleCode role) : IProjectScopeGuard
    {
        public Task<ProjectAccessScope?> AuthorizeAsync(Guid userId, UserRoleCode authoritativeRole, Guid requestedProjectId, CancellationToken cancellationToken = default)
            => Task.FromResult<ProjectAccessScope?>(requestedProjectId == projectId && authoritativeRole == role ? new ProjectAccessScope(projectId, role, Guid.NewGuid()) : null);
    }
}
