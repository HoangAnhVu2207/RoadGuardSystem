using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public enum ProjectCreationPersistenceStatus
{
    Success,
    Replayed,
    InvalidInput,
    ActorNotAuthorized,
    ProjectManagerNotFound,
    HandoverFileNotFound,
    ProjectCodeConflict,
    IdempotentConflict
}

public sealed record ProjectCreationPersistenceRequest(
    Guid ActorUserId,
    string ProjectCode,
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid PrimaryProjectManagerUserId,
    string HandoverDocumentNo,
    DateOnly HandoverDate,
    Guid? HandoverFileId,
    string? HandoverNotes,
    Guid OperationId,
    Guid? CorrelationId);

public sealed record ProjectCreationPersistenceView(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    ProjectStatus Status,
    Guid PrimaryProjectManagerUserId,
    Guid HandoverDocumentId,
    DateOnly HandoverDate,
    byte[] RowVersion);

public sealed record ProjectCreationPersistenceResult(
    ProjectCreationPersistenceStatus Status,
    ProjectCreationPersistenceView? Project = null);

public sealed class ProjectCreationPersistenceService
{
    private const string Operation = "ProjectCreated";
    private static readonly string[] AuditFields =
        ["project_code", "primary_project_manager_user_id", "handover_document_id"];
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;

    public ProjectCreationPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _idempotency = idempotency;
    }

    public async Task<ProjectCreationPersistenceResult> CreateAsync(
        ProjectCreationPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.PrimaryProjectManagerUserId == Guid.Empty ||
            request.OperationId == Guid.Empty)
        {
            return new(ProjectCreationPersistenceStatus.InvalidInput);
        }

        var actorIsSupervisor = await _context.Users.AsNoTracking().AnyAsync(user =>
            user.Id == request.ActorUserId &&
            user.Status == UserStatus.Active &&
            user.RoleCode == UserRoleCode.Supervisor,
            cancellationToken);
        if (!actorIsSupervisor)
        {
            return new(ProjectCreationPersistenceStatus.ActorNotAuthorized);
        }

        try
        {
            var operation = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                projectId: null,
                Operation,
                request.OperationId.ToString("N"),
                Fingerprint(request),
                operationCancellationToken => CreateAggregateAsync(request, operationCancellationToken),
                cancellationToken);
            if (operation.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(ProjectCreationPersistenceStatus.IdempotentConflict);
            }

            var view = JsonSerializer.Deserialize<ProjectCreationPersistenceView>(operation.OutcomeJson)
                ?? throw new InvalidOperationException("Stored project creation outcome is invalid.");
            return new(
                operation.Status == IdempotencyOperationStatus.Executed
                    ? ProjectCreationPersistenceStatus.Success
                    : ProjectCreationPersistenceStatus.Replayed,
                view);
        }
        catch (ArgumentException)
        {
            return new(ProjectCreationPersistenceStatus.InvalidInput);
        }
        catch (ProjectManagerNotFoundException)
        {
            return new(ProjectCreationPersistenceStatus.ProjectManagerNotFound);
        }
        catch (HandoverFileNotFoundException)
        {
            return new(ProjectCreationPersistenceStatus.HandoverFileNotFound);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return new(ProjectCreationPersistenceStatus.ProjectCodeConflict);
        }
    }

    private async Task<(Guid OperationId, string OutcomeJson)> CreateAggregateAsync(
        ProjectCreationPersistenceRequest request,
        CancellationToken cancellationToken)
    {
        var projectManagerExists = await _context.Users.AsNoTracking().AnyAsync(user =>
            user.Id == request.PrimaryProjectManagerUserId &&
            user.Status == UserStatus.Active &&
            user.RoleCode == UserRoleCode.ProjectManager,
            cancellationToken);
        if (!projectManagerExists)
        {
            throw new ProjectManagerNotFoundException();
        }

        if (request.HandoverFileId is Guid fileId &&
            !await _context.Files.AsNoTracking().AnyAsync(file => file.Id == fileId, cancellationToken))
        {
            throw new HandoverFileNotFoundException();
        }

        var now = DateTimeOffset.UtcNow;
        var projectId = Guid.NewGuid();
        var handoverId = Guid.NewGuid();
        var project = Project.Create(
            projectId,
            request.ProjectCode,
            request.Name,
            request.Description,
            request.EngineeringUtmSrid,
            request.StartDate,
            request.EndDate,
            now);
        var member = ProjectMember.CreatePrimaryProjectManager(
            Guid.NewGuid(),
            projectId,
            request.PrimaryProjectManagerUserId,
            request.HandoverDate);
        var handover = HandoverDocument.Create(
            handoverId,
            projectId,
            request.HandoverDocumentNo,
            request.HandoverDate,
            request.ActorUserId,
            request.HandoverFileId,
            request.HandoverNotes);

        _context.Projects.Add(project);
        _context.ProjectMembers.Add(member);
        _context.HandoverDocuments.Add(handover);
        var snapshot = JsonSerializer.Serialize(new
        {
            project_code = project.ProjectCode,
            primary_project_manager_user_id = member.UserId,
            handover_document_id = handover.Id
        });
        _context.AuditLogs.Add(AuditLog.Create(
            Guid.NewGuid(),
            request.ActorUserId,
            now,
            "project_created",
            "Project",
            projectId,
            beforeSnapshot: null,
            afterSnapshot: snapshot,
            reason: "Supervisor created project after handover",
            source: "PROJECT_API",
            correlationId: request.CorrelationId,
            snapshotAllowedPropertyNames: AuditFields));

        await _context.SaveChangesAsync(cancellationToken);
        var view = new ProjectCreationPersistenceView(
            project.Id,
            project.ProjectCode,
            project.Name,
            project.Status,
            member.UserId,
            handover.Id,
            handover.HandoverDate,
            project.RowVersion);
        return (projectId, JsonSerializer.Serialize(view));
    }

    private static string Fingerprint(ProjectCreationPersistenceRequest request)
    {
        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            project_code = request.ProjectCode.Trim(),
            name = request.Name.Trim(),
            description = request.Description?.Trim(),
            engineering_utm_srid = request.EngineeringUtmSrid,
            start_date = request.StartDate,
            end_date = request.EndDate,
            primary_project_manager_user_id = request.PrimaryProjectManagerUserId,
            handover_document_no = request.HandoverDocumentNo.Trim(),
            handover_date = request.HandoverDate,
            handover_file_id = request.HandoverFileId,
            handover_notes = request.HandoverNotes?.Trim()
        });
        return Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }

    private sealed class ProjectManagerNotFoundException : Exception;

    private sealed class HandoverFileNotFoundException : Exception;
}
