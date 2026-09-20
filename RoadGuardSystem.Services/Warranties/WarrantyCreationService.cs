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

public sealed class WarrantyCreationService : IWarrantyCreationService
{
    private readonly WarrantyPersistenceService _persistence;

    public WarrantyCreationService(WarrantyPersistenceService persistence)
    {
        _persistence = persistence;
    }

    public async Task<WarrantyCreationResult> CreateAsync(
        Guid actorUserId,
        UserRoleCode actorRole,
        Guid projectId,
        CreateWarrantyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (actorRole != UserRoleCode.Supervisor)
        {
            return new(WarrantyCreationStatus.Forbidden);
        }

        if (actorUserId == Guid.Empty || projectId == Guid.Empty || command.OperationId == Guid.Empty ||
            command.HandoverDate == default || command.WarrantyStartDate == default ||
            command.WarrantyEndDate == default ||
            IsInvalidRetainedValue(command.RetainedValue) ||
            !TryParseScope(command.Scope, out var scope) ||
            !TryParseStatus(command.Status, out var status))
        {
            return new(WarrantyCreationStatus.InvalidInput);
        }

        var result = await _persistence.CreateAsync(new WarrantyCreationPersistenceRequest(
            actorUserId,
            projectId,
            command.RoadSectionId,
            command.HandoverDocumentId,
            command.HandoverDate,
            command.WarrantyStartDate,
            command.WarrantyEndDate,
            command.RetainedValue,
            scope,
            command.Terms,
            command.SourceDocumentId,
            status,
            command.OperationId,
            command.CorrelationId), cancellationToken);

        return new WarrantyCreationResult(
            MapStatus(result.Status),
            result.Warranty is null ? null : new CreatedWarrantyView(
                result.Warranty.WarrantyId,
                result.Warranty.ProjectId,
                result.Warranty.RoadSectionId,
                result.Warranty.HandoverDocumentId,
                result.Warranty.HandoverDate,
                result.Warranty.WarrantyStartDate,
                result.Warranty.WarrantyEndDate,
                result.Warranty.RetainedValue,
                result.Warranty.Scope.ToApiCode(),
                result.Warranty.Terms,
                result.Warranty.SourceDocumentId,
                result.Warranty.Status.ToString().ToUpperInvariant()));
    }

    private static WarrantyCreationStatus MapStatus(WarrantyCreationPersistenceStatus status) => status switch
    {
        WarrantyCreationPersistenceStatus.Success => WarrantyCreationStatus.Success,
        WarrantyCreationPersistenceStatus.Replayed => WarrantyCreationStatus.Replayed,
        WarrantyCreationPersistenceStatus.ActorNotAuthorized => WarrantyCreationStatus.Forbidden,
        WarrantyCreationPersistenceStatus.ProjectNotFound => WarrantyCreationStatus.ProjectNotFound,
        WarrantyCreationPersistenceStatus.ProjectClosed => WarrantyCreationStatus.ProjectClosed,
        WarrantyCreationPersistenceStatus.RoadSectionNotFound => WarrantyCreationStatus.RoadSectionNotFound,
        WarrantyCreationPersistenceStatus.HandoverDocumentNotFound => WarrantyCreationStatus.HandoverDocumentNotFound,
        WarrantyCreationPersistenceStatus.SourceDocumentNotFound => WarrantyCreationStatus.SourceDocumentNotFound,
        WarrantyCreationPersistenceStatus.IdempotentConflict => WarrantyCreationStatus.IdempotentConflict,
        _ => WarrantyCreationStatus.InvalidInput
    };

    private static bool TryParseScope(string? value, out WarrantyScope scope)
    {
        scope = value?.Trim().ToUpperInvariant() switch
        {
            "PROJECT" => WarrantyScope.Project,
            "ROAD_SECTION" => WarrantyScope.RoadSection,
            "CONTRACT_ITEM" => WarrantyScope.ContractItem,
            "OTHER" => WarrantyScope.Other,
            _ => WarrantyScope.Unknown
        };
        return scope != WarrantyScope.Unknown;
    }

    private static bool TryParseStatus(string? value, out WarrantyStatus status)
    {
        status = value?.Trim().ToUpperInvariant() switch
        {
            "PLANNED" => WarrantyStatus.Planned,
            "ACTIVE" => WarrantyStatus.Active,
            "EXPIRED" => WarrantyStatus.Expired,
            "SUSPENDED" => WarrantyStatus.Suspended,
            _ => WarrantyStatus.Unknown
        };
        return status != WarrantyStatus.Unknown;
    }

    private static bool IsInvalidRetainedValue(decimal? retainedValue)
    {
        const decimal maximum = 99_999_999_999_999_999.99m;
        return retainedValue is decimal value &&
               (value < 0 || value > maximum || decimal.Round(value, 2) != value);
    }
}
