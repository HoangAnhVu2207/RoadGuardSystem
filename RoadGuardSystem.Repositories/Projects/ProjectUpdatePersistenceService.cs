using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Idempotency;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

public enum ProjectUpdatePersistenceStatus
{
    Success,
    Replayed,
    InvalidInput,
    ActorNotAuthorized,
    NotFound,
    StaleConcurrency,
    IdempotentConflict
}

public sealed record ProjectUpdatePersistenceRequest(
    Guid ActorUserId,
    Guid ProjectId,
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    byte[] ExpectedRowVersion,
    Guid OperationId,
    Guid? CorrelationId);

public sealed record ProjectUpdatePersistenceView(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    string? Description,
    int? EngineeringUtmSrid,
    DateOnly? StartDate,
    DateOnly? EndDate,
    ProjectStatus Status,
    byte[] RowVersion);

public sealed record ProjectUpdatePersistenceResult(
    ProjectUpdatePersistenceStatus Status,
    ProjectUpdatePersistenceView? Project = null);

public sealed class ProjectUpdatePersistenceService
{
    private const string Operation = "ProjectUpdated";
    private static readonly string[] AuditFields =
        ["name", "description", "engineering_utm_srid", "start_date", "end_date"];
    private readonly RoadGuardDbContext _context;

    public ProjectUpdatePersistenceService(RoadGuardDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectUpdatePersistenceResult> UpdateAsync(
        ProjectUpdatePersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty ||
            request.OperationId == Guid.Empty || request.ExpectedRowVersion.Length == 0)
        {
            return new(ProjectUpdatePersistenceStatus.InvalidInput);
        }

        var actorIsSupervisor = await _context.Users.AsNoTracking().AnyAsync(user =>
            user.Id == request.ActorUserId &&
            user.Status == UserStatus.Active &&
            user.RoleCode == UserRoleCode.Supervisor,
            cancellationToken);
        if (!actorIsSupervisor)
        {
            return new(ProjectUpdatePersistenceStatus.ActorNotAuthorized);
        }

        var fingerprint = Fingerprint(request);
        var idempotencyKey = request.OperationId.ToString("N");
        var existing = await FindExistingAsync(request, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return MapExisting(existing, fingerprint);
        }

        try
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteInTransactionAsync(
                attemptCancellationToken => ExecuteAttemptAsync(
                    request,
                    idempotencyKey,
                    fingerprint,
                    attemptCancellationToken),
                async verificationCancellationToken =>
                {
                    _context.ChangeTracker.Clear();
                    var committed = await FindExistingAsync(
                        request,
                        idempotencyKey,
                        verificationCancellationToken);
                    return committed is not null &&
                           string.Equals(committed.RequestFingerprint, fingerprint, StringComparison.Ordinal);
                },
                cancellationToken);
        }
        catch (ProjectNotFoundException)
        {
            return new(ProjectUpdatePersistenceStatus.NotFound);
        }
        catch (StaleProjectException)
        {
            _context.ChangeTracker.Clear();
            existing = await FindExistingAsync(request, idempotencyKey, CancellationToken.None);
            return existing is null
                ? new(ProjectUpdatePersistenceStatus.StaleConcurrency)
                : MapExisting(existing, fingerprint);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            existing = await FindExistingAsync(request, idempotencyKey, CancellationToken.None);
            return existing is null
                ? new(ProjectUpdatePersistenceStatus.StaleConcurrency)
                : MapExisting(existing, fingerprint);
        }
        catch (ArgumentException)
        {
            return new(ProjectUpdatePersistenceStatus.InvalidInput);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.ChangeTracker.Clear();
            existing = await FindExistingAsync(request, idempotencyKey, CancellationToken.None);
            if (existing is null)
            {
                throw;
            }

            return MapExisting(existing, fingerprint);
        }
    }

    private async Task<ProjectUpdatePersistenceResult> ExecuteAttemptAsync(
        ProjectUpdatePersistenceRequest request,
        string idempotencyKey,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        var existing = await FindExistingAsync(request, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return MapExisting(existing, fingerprint);
        }

        var project = await _context.Projects
            .SingleOrDefaultAsync(candidate => candidate.Id == request.ProjectId, cancellationToken)
            ?? throw new ProjectNotFoundException();
        if (!project.RowVersion.SequenceEqual(request.ExpectedRowVersion))
        {
            throw new StaleProjectException();
        }

        var beforeSnapshot = Snapshot(project);
        project.UpdateDetails(
            request.Name,
            request.Description,
            request.EngineeringUtmSrid,
            request.StartDate,
            request.EndDate);
        var now = DateTimeOffset.UtcNow;
        _context.AuditLogs.Add(AuditLog.Create(
            Guid.NewGuid(),
            request.ActorUserId,
            now,
            "project_updated",
            "Project",
            project.Id,
            beforeSnapshot,
            Snapshot(project),
            "Supervisor updated project metadata",
            "PROJECT_API",
            request.CorrelationId,
            AuditFields));
        await _context.SaveChangesAsync(cancellationToken);

        var view = new ProjectUpdatePersistenceView(
            project.Id,
            project.ProjectCode,
            project.Name,
            project.Description,
            project.EngineeringUtmSrid,
            project.StartDate,
            project.EndDate,
            project.Status,
            project.RowVersion);
        _context.IdempotencyRecords.Add(IdempotencyRecord.Create(
            request.ActorUserId,
            request.ProjectId,
            Operation,
            idempotencyKey,
            fingerprint,
            request.OperationId,
            JsonSerializer.Serialize(view),
            now));
        await _context.SaveChangesAsync(cancellationToken);
        return new(ProjectUpdatePersistenceStatus.Success, view);
    }

    private Task<IdempotencyRecord?> FindExistingAsync(
        ProjectUpdatePersistenceRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        _context.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(record =>
            record.ActorUserId == request.ActorUserId &&
            record.ProjectId == request.ProjectId &&
            record.Operation == Operation &&
            record.IdempotencyKey == idempotencyKey,
            cancellationToken);

    private static ProjectUpdatePersistenceResult MapExisting(
        IdempotencyRecord record,
        string fingerprint)
    {
        if (!string.Equals(record.RequestFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return new(ProjectUpdatePersistenceStatus.IdempotentConflict);
        }

        var view = JsonSerializer.Deserialize<ProjectUpdatePersistenceView>(record.OutcomeJson)
            ?? throw new InvalidOperationException("Stored project update outcome is invalid.");
        return new(ProjectUpdatePersistenceStatus.Replayed, view);
    }

    private static string Snapshot(BusinessObjects.Projects.Project project) => JsonSerializer.Serialize(new
    {
        name = project.Name,
        description = project.Description,
        engineering_utm_srid = project.EngineeringUtmSrid,
        start_date = project.StartDate,
        end_date = project.EndDate
    });

    private static string Fingerprint(ProjectUpdatePersistenceRequest request)
    {
        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            project_id = request.ProjectId,
            name = request.Name.Trim(),
            description = request.Description?.Trim(),
            engineering_utm_srid = request.EngineeringUtmSrid,
            start_date = request.StartDate,
            end_date = request.EndDate,
            expected_row_version = Convert.ToBase64String(request.ExpectedRowVersion)
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

    private sealed class ProjectNotFoundException : Exception;

    private sealed class StaleProjectException : Exception;
}
