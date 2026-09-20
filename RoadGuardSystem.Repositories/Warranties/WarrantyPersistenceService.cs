using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Warranties;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Warranties;

public enum WarrantyCreationPersistenceStatus
{
    Success,
    Replayed,
    InvalidInput,
    ActorNotAuthorized,
    ProjectNotFound,
    ProjectClosed,
    RoadSectionNotFound,
    HandoverDocumentNotFound,
    SourceDocumentNotFound,
    IdempotentConflict
}

public sealed record WarrantyCreationPersistenceRequest(
    Guid ActorUserId,
    Guid ProjectId,
    Guid? RoadSectionId,
    Guid? HandoverDocumentId,
    DateOnly HandoverDate,
    DateOnly WarrantyStartDate,
    DateOnly WarrantyEndDate,
    decimal? RetainedValue,
    WarrantyScope Scope,
    string? Terms,
    Guid? SourceDocumentId,
    WarrantyStatus Status,
    Guid OperationId,
    Guid? CorrelationId);

public sealed record WarrantyCreationPersistenceView(
    Guid WarrantyId,
    Guid ProjectId,
    Guid? RoadSectionId,
    Guid? HandoverDocumentId,
    DateOnly HandoverDate,
    DateOnly WarrantyStartDate,
    DateOnly WarrantyEndDate,
    decimal? RetainedValue,
    WarrantyScope Scope,
    string? Terms,
    Guid? SourceDocumentId,
    WarrantyStatus Status);

public sealed record WarrantyCreationPersistenceResult(
    WarrantyCreationPersistenceStatus Status,
    WarrantyCreationPersistenceView? Warranty = null);

/// <summary>
/// Validates project ownership across optional Warranty references before persisting the aggregate.
/// </summary>
public sealed class WarrantyPersistenceService
{
    private const string Operation = "WarrantyCreated";
    private static readonly string[] AuditFields =
    [
        "project_id",
        "road_section_id",
        "handover_document_id",
        "handover_date",
        "warranty_start_date",
        "warranty_end_date",
        "scope",
        "source_document_id",
        "status"
    ];
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public WarrantyPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService? idempotency = null)
    {
        _context = context;
        _idempotency = idempotency ?? new IdempotencyOperationService(context);
    }

    public async Task<WarrantyCreationPersistenceResult> CreateAsync(
        WarrantyCreationPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty || request.OperationId == Guid.Empty)
        {
            return new(WarrantyCreationPersistenceStatus.InvalidInput);
        }

        var actorIsSupervisor = await _context.Users.AsNoTracking().AnyAsync(user =>
            user.Id == request.ActorUserId &&
            user.Status == UserStatus.Active &&
            user.RoleCode == UserRoleCode.Supervisor,
            cancellationToken);
        if (!actorIsSupervisor)
        {
            return new(WarrantyCreationPersistenceStatus.ActorNotAuthorized);
        }

        try
        {
            var operation = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                Operation,
                request.OperationId.ToString("N"),
                Fingerprint(request),
                operationCancellationToken => CreateAggregateAsync(request, operationCancellationToken),
                cancellationToken);
            if (operation.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(WarrantyCreationPersistenceStatus.IdempotentConflict);
            }

            var view = JsonSerializer.Deserialize<WarrantyCreationPersistenceView>(operation.OutcomeJson)
                ?? throw new InvalidOperationException("Warranty idempotency outcome is invalid.");
            return new(
                operation.Status == IdempotencyOperationStatus.Executed
                    ? WarrantyCreationPersistenceStatus.Success
                    : WarrantyCreationPersistenceStatus.Replayed,
                view);
        }
        catch (ArgumentException)
        {
            return new(WarrantyCreationPersistenceStatus.InvalidInput);
        }
        catch (ProjectNotFoundException)
        {
            return new(WarrantyCreationPersistenceStatus.ProjectNotFound);
        }
        catch (ProjectClosedException)
        {
            return new(WarrantyCreationPersistenceStatus.ProjectClosed);
        }
        catch (RoadSectionNotFoundException)
        {
            return new(WarrantyCreationPersistenceStatus.RoadSectionNotFound);
        }
        catch (HandoverDocumentNotFoundException)
        {
            return new(WarrantyCreationPersistenceStatus.HandoverDocumentNotFound);
        }
        catch (SourceDocumentNotFoundException)
        {
            return new(WarrantyCreationPersistenceStatus.SourceDocumentNotFound);
        }
    }

    public async Task CreateAsync(Warranty warranty, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(warranty);

        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == warranty.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new InvalidOperationException("Warranty project was not found.");
        }

        if (warranty.RoadSectionId is Guid roadSectionId)
        {
            var roadMatchesProject = await _context.RoadSections
                .AsNoTracking()
                .AnyAsync(
                    section => section.Id == roadSectionId && section.ProjectId == warranty.ProjectId,
                    cancellationToken);
            if (!roadMatchesProject)
            {
                throw new InvalidOperationException("Warranty road section must belong to the warranty project.");
            }
        }

        if (warranty.HandoverDocumentId is Guid handoverDocumentId)
        {
            var handoverMatchesProject = await _context.HandoverDocuments
                .AsNoTracking()
                .AnyAsync(
                    document => document.Id == handoverDocumentId && document.ProjectId == warranty.ProjectId,
                    cancellationToken);
            if (!handoverMatchesProject)
            {
                throw new InvalidOperationException("Warranty handover document must belong to the warranty project.");
            }
        }

        _context.Warranties.Add(warranty);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Guid OperationId, string OutcomeJson)> CreateAggregateAsync(
        WarrantyCreationPersistenceRequest request,
        CancellationToken cancellationToken)
    {
        var projectStatus = await _context.Projects.AsNoTracking()
            .Where(project => project.Id == request.ProjectId)
            .Select(project => (ProjectStatus?)project.Status)
            .SingleOrDefaultAsync(cancellationToken);
        if (projectStatus is null)
        {
            throw new ProjectNotFoundException();
        }
        if (projectStatus == ProjectStatus.Closed)
        {
            throw new ProjectClosedException();
        }

        if (request.RoadSectionId is Guid roadSectionId &&
            !await _context.RoadSections.AsNoTracking().AnyAsync(
                section => section.Id == roadSectionId && section.ProjectId == request.ProjectId,
                cancellationToken))
        {
            throw new RoadSectionNotFoundException();
        }

        if (request.HandoverDocumentId is Guid handoverDocumentId &&
            !await _context.HandoverDocuments.AsNoTracking().AnyAsync(
                document => document.Id == handoverDocumentId && document.ProjectId == request.ProjectId,
                cancellationToken))
        {
            throw new HandoverDocumentNotFoundException();
        }

        if (request.SourceDocumentId is Guid sourceDocumentId &&
            !await _context.Files.AsNoTracking().AnyAsync(file => file.Id == sourceDocumentId, cancellationToken))
        {
            throw new SourceDocumentNotFoundException();
        }

        var warranty = Warranty.Create(
            Guid.NewGuid(),
            request.ProjectId,
            request.RoadSectionId,
            request.HandoverDocumentId,
            request.HandoverDate,
            request.WarrantyStartDate,
            request.WarrantyEndDate,
            request.RetainedValue,
            request.Scope,
            request.Terms,
            request.SourceDocumentId,
            request.Status);
        _context.Warranties.Add(warranty);

        var snapshot = JsonSerializer.Serialize(new
        {
            project_id = warranty.ProjectId,
            road_section_id = warranty.RoadSectionId,
            handover_document_id = warranty.HandoverDocumentId,
            handover_date = warranty.HandoverDate,
            warranty_start_date = warranty.WarrantyStartDate,
            warranty_end_date = warranty.WarrantyEndDate,
            scope = warranty.Scope.ToApiCode(),
            source_document_id = warranty.SourceDocumentId,
            status = warranty.Status.ToString().ToUpperInvariant()
        });
        _context.AuditLogs.Add(AuditLog.Create(
            Guid.NewGuid(),
            request.ActorUserId,
            DateTimeOffset.UtcNow,
            "warranty_created",
            "Warranty",
            warranty.Id,
            beforeSnapshot: null,
            afterSnapshot: snapshot,
            reason: "Supervisor recorded warranty terms after handover",
            source: "PROJECT_API",
            correlationId: request.CorrelationId,
            snapshotAllowedPropertyNames: AuditFields));

        var view = new WarrantyCreationPersistenceView(
            warranty.Id,
            warranty.ProjectId,
            warranty.RoadSectionId,
            warranty.HandoverDocumentId,
            warranty.HandoverDate,
            warranty.WarrantyStartDate,
            warranty.WarrantyEndDate,
            warranty.RetainedValue,
            warranty.Scope,
            warranty.Terms,
            warranty.SourceDocumentId,
            warranty.Status);
        return (warranty.Id, JsonSerializer.Serialize(view));
    }

    private static string Fingerprint(WarrantyCreationPersistenceRequest request)
    {
        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            request.ProjectId,
            request.RoadSectionId,
            request.HandoverDocumentId,
            request.HandoverDate,
            request.WarrantyStartDate,
            request.WarrantyEndDate,
            request.RetainedValue,
            request.Scope,
            terms = request.Terms?.Trim(),
            request.SourceDocumentId,
            request.Status
        });
        return Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
    }

    private sealed class ProjectNotFoundException : Exception;
    private sealed class ProjectClosedException : Exception;
    private sealed class RoadSectionNotFoundException : Exception;
    private sealed class HandoverDocumentNotFoundException : Exception;
    private sealed class SourceDocumentNotFoundException : Exception;
}
