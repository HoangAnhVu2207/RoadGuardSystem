using RoadGuardSystem.DTOs.Processing;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Processing;

public interface IProcessingV2Service
{
    Task<ProcessingV2ServiceResult> CreateAsync(Guid actorUserId, UserRoleCode role, CreateProcessingJobRequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default);
    Task<ProcessingV2ServiceResult> GetAsync(Guid actorUserId, UserRoleCode role, Guid jobId, CancellationToken cancellationToken = default);
    Task<ProcessingV2ServiceResult> RetryAsync(Guid actorUserId, UserRoleCode role, Guid jobId, RetryProcessingJobRequestDto request, string idempotencyKey, string expectedVersion, Guid? correlationId, CancellationToken cancellationToken = default);
    Task<ProcessingV2ServiceResult> ReceiveResultAsync(Guid jobId, ReceiveAiResultRequestDto request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ProcessingV2ServiceResult> CreateValidationAsync(Guid actorUserId, UserRoleCode role, Guid projectId, CreateValidationRunRequestDto request, string idempotencyKey, Guid? correlationId, CancellationToken cancellationToken = default);
    Task<ValidationResultDto?> GetValidationAsync(Guid actorUserId, UserRoleCode role, Guid runId, CancellationToken cancellationToken = default);
}
