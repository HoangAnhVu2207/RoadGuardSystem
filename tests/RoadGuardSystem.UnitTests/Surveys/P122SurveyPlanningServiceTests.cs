using FluentAssertions;
using RoadGuardSystem.Repositories.Surveys;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Surveys;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Surveys;

[Trait("TaskId", "P1-22")]
public sealed class P122SurveyPlanningServiceTests
{
    [Fact]
    public async Task CreatePlanAsync_AuthorizedProjectManager_MapsPersistenceResult()
    {
        var actorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var roadSectionId = Guid.NewGuid();
        var roadSectionVersionId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
        var repository = new RecordingPlanningRepository
        {
            PlanResult = new SurveyPlanPersistenceResult(
                SurveyPlanPersistenceStatus.Success,
                new SurveyPlanPersistenceView(
                    Guid.NewGuid(), projectId, roadSectionId, roadSectionVersionId, start, start.AddHours(2),
                    SurveyType.Periodic, SurveyPlanStatus.Planned, "{\"formats\":[\"video\"]}"))
        };
        var scopeGuard = new FixedProjectScopeGuard(projectId, UserRoleCode.ProjectManager);
        var service = new SurveyPlanningService(repository, scopeGuard, TimeProvider.System);

        var result = await service.CreatePlanAsync(
            actorId,
            UserRoleCode.ProjectManager,
            new CreateSurveyPlanCommand(
                projectId,
                roadSectionId,
                roadSectionVersionId,
                start,
                start.AddHours(2),
                SurveyType.Periodic,
                "{\"formats\":[\"video\"]}",
                operationId,
                Guid.NewGuid()));

        result.Status.Should().Be(SurveyPlanningServiceStatus.Success);
        result.Plan!.PlanId.Should().Be(repository.PlanResult.Plan!.PlanId);
        repository.PlanRequest!.ActorUserId.Should().Be(actorId);
        repository.PlanRequest.ProjectId.Should().Be(projectId);
        repository.PlanRequest.OperationId.Should().Be(operationId);
    }

    [Fact]
    public async Task CreateRequestAsync_WithoutDueAt_UsesStableFingerprintAcrossReplay()
    {
        var projectId = Guid.NewGuid();
        var roadSectionId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var repository = new RecordingPlanningRepository
        {
            RequestResult = new SurveyRequestPersistenceResult(SurveyRequestPersistenceStatus.Success)
        };
        var service = new SurveyPlanningService(
            repository,
            new FixedProjectScopeGuard(projectId, UserRoleCode.ProjectManager),
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero)));
        var command = new CreateSurveyRequestCommand(
            projectId,
            roadSectionId,
            Guid.NewGuid(),
            null,
            SurveyType.Periodic,
            null,
            "{\"formats\":[\"video\"]}",
            operationId,
            Guid.NewGuid());

        await service.CreateRequestAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, command);
        await service.CreateRequestAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, command);

        repository.Requests.Should().HaveCount(2);
        repository.Requests[0].RequestFingerprint.Should().Be(repository.Requests[1].RequestFingerprint);

        var changedOutputCommand = command with { OutputRequirements = "{\"formats\":[\"srt\"]}" };
        await service.CreateRequestAsync(Guid.NewGuid(), UserRoleCode.ProjectManager, changedOutputCommand);

        repository.Requests[2].RequestFingerprint.Should().NotBe(repository.Requests[0].RequestFingerprint);
    }

    private sealed class RecordingPlanningRepository : ISurveyPlanningRepository
    {
        public SurveyPlanPersistenceResult PlanResult { get; init; } = new(SurveyPlanPersistenceStatus.InvalidInput);
        public SurveyPlanCreationPersistenceRequest? PlanRequest { get; private set; }
        public SurveyRequestPersistenceResult RequestResult { get; init; } = new(SurveyRequestPersistenceStatus.InvalidInput);
        public List<SurveyRequestCreationPersistenceRequest> Requests { get; } = [];

        public Task<SurveyPlanPersistenceResult> CreatePlanAsync(
            SurveyPlanCreationPersistenceRequest request,
            CancellationToken cancellationToken = default)
        {
            PlanRequest = request;
            return Task.FromResult(PlanResult);
        }

        public Task<SurveyRequestPersistenceResult> CreateRequestAsync(
            SurveyRequestCreationPersistenceRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(RequestResult);
        }

        public Task<SurveyPlanPostponementPersistenceResult> PostponePlanAsync(
            SurveyPlanPostponementPersistenceRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new SurveyPlanPostponementPersistenceResult(SurveyPlanPostponementPersistenceStatus.InvalidInput));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class FixedProjectScopeGuard : IProjectScopeGuard
    {
        private readonly Guid _projectId;
        private readonly UserRoleCode _role;

        public FixedProjectScopeGuard(Guid projectId, UserRoleCode role)
        {
            _projectId = projectId;
            _role = role;
        }

        public Task<ProjectAccessScope?> AuthorizeAsync(
            Guid userId,
            UserRoleCode authoritativeRole,
            Guid projectId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ProjectAccessScope?>(
                projectId == _projectId && authoritativeRole == _role
                    ? new ProjectAccessScope(projectId, _role, Guid.NewGuid())
                    : null);
    }
}
