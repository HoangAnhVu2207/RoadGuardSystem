using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.Repositories.Processing;
using RoadGuardSystem.Services.Authorization;
using RoadGuardSystem.Services.Implementations.Processing;
using RoadGuardSystem.Services.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

public sealed class P234ValidationAuthorizationTests
{
    [Fact]
    public async Task CreateValidationAsync_Supervisor_IsForbiddenBeforePersistence()
    {
        var repository = new RecordingRepository();
        var service = new ProcessingV2Service(repository, new AllowScopeGuard());
        var result = await service.CreateValidationAsync(
            Guid.NewGuid(),
            UserRoleCode.Supervisor,
            Guid.NewGuid(),
            new CreateValidationRunRequestDto(
                [new ValidationPairDto(Guid.NewGuid(), Guid.NewGuid())],
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DEPRESSION_DEPTH",
                "mm"),
            "validation-pm-only-001",
            null);

        Assert.Equal(ProcessingV2ServiceStatus.Forbidden, result.Status);
        Assert.False(repository.CreateValidationCalled);
    }

    private sealed class RecordingRepository : IProcessingV2Repository
    {
        public bool CreateValidationCalled { get; private set; }

        public Task<Guid?> GetDatasetProjectIdAsync(Guid datasetId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<ProcessingJobPersistenceResult> CreateAsync(ProcessingJobCreateRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new ProcessingJobPersistenceResult(ProcessingJobPersistenceStatus.InvalidInput));
        public Task<ProcessingJobPersistenceView?> GetAsync(Guid jobId, CancellationToken cancellationToken = default) => Task.FromResult<ProcessingJobPersistenceView?>(null);
        public Task<ProcessingJobPersistenceResult> RetryAsync(ProcessingJobRetryRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new ProcessingJobPersistenceResult(ProcessingJobPersistenceStatus.InvalidInput));
        public Task<ProcessingJobPersistenceResult> ReceiveResultAsync(ProcessingAiResultRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new ProcessingJobPersistenceResult(ProcessingJobPersistenceStatus.InvalidInput));
        public Task<ValidationRunPersistenceResult> CreateValidationAsync(ValidationRunCreateRequest request, CancellationToken cancellationToken = default)
        {
            CreateValidationCalled = true;
            return Task.FromResult(new ValidationRunPersistenceResult(ProcessingJobPersistenceStatus.Success));
        }

        public Task<ValidationRunPersistenceView?> GetValidationAsync(Guid runId, CancellationToken cancellationToken = default) => Task.FromResult<ValidationRunPersistenceView?>(null);
        public Task<bool> CompleteNextValidationRunAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class AllowScopeGuard : IProjectScopeGuard
    {
        public Task<ProjectAccessScope?> AuthorizeAsync(Guid userId, UserRoleCode authoritativeRole, Guid requestedProjectId, CancellationToken cancellationToken = default)
            => Task.FromResult<ProjectAccessScope?>(new ProjectAccessScope(requestedProjectId, authoritativeRole, Guid.NewGuid()));
    }
}
