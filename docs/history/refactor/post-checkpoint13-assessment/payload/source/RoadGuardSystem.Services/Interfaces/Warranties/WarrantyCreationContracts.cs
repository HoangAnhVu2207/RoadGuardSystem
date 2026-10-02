using RoadGuardSystem.Repositories.Warranties;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Services.Warranties;

public enum WarrantyCreationStatus
{
    Success,
    Replayed,
    InvalidInput,
    Forbidden,
    ProjectNotFound,
    ProjectClosed,
    RoadSectionNotFound,
    HandoverDocumentNotFound,
    SourceDocumentNotFound,
    IdempotentConflict
}

public sealed record CreateWarrantyCommand(
    Guid? RoadSectionId,
    Guid? HandoverDocumentId,
    DateOnly HandoverDate,
    DateOnly WarrantyStartDate,
    DateOnly WarrantyEndDate,
    decimal? RetainedValue,
    string Scope,
    string? Terms,
    Guid? SourceDocumentId,
    string Status,
    Guid OperationId,
    Guid? CorrelationId);

public sealed record CreatedWarrantyView(
    Guid WarrantyId,
    Guid ProjectId,
    Guid? RoadSectionId,
    Guid? HandoverDocumentId,
    DateOnly HandoverDate,
    DateOnly WarrantyStartDate,
    DateOnly WarrantyEndDate,
    decimal? RetainedValue,
    string Scope,
    string? Terms,
    Guid? SourceDocumentId,
    string Status);

public sealed record WarrantyCreationResult(
    WarrantyCreationStatus Status,
    CreatedWarrantyView? Warranty = null);

public interface IWarrantyCreationService
{
    Task<WarrantyCreationResult> CreateAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        Guid projectId,
        CreateWarrantyCommand command,
        CancellationToken cancellationToken = default);
}
