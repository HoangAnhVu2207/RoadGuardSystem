using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Transactions;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Projects;

/// <summary>
/// Persists the initial and replacement road geometry versions without exposing a transient second current version.
/// </summary>
public sealed class RoadSectionVersionPersistenceService : IRoadSectionVersionRepository
{
    private const string InitialOperation = "RoadSectionCreated";
    private const string NextOperation = "RoadSectionVersionCreated";
    private static readonly string[] InitialAuditFields = ["road_section_id", "road_section_version_id", "code", "version_no"];
    private static readonly string[] NextAuditFields = ["road_section_id", "road_section_version_id", "previous_version_id", "version_no"];
    private readonly RoadGuardDbContext _context;
    private readonly RoadGuardTransactionService _transactionService;
    private readonly IdempotencyOperationService _idempotency;

    public RoadSectionVersionPersistenceService(
        RoadGuardDbContext context,
        RoadGuardTransactionService transactionService,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _transactionService = transactionService;
        _idempotency = idempotency;
    }

    public async Task<RoadSectionVersionWriteResult> CreateInitialAsync(
        RoadSectionInitialVersionWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty || request.OperationId == Guid.Empty)
        {
            return new(RoadSectionVersionWriteStatus.InvalidInput);
        }

        try
        {
            var operation = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                InitialOperation,
                request.OperationId.ToString("N"),
                InitialFingerprint(request),
                token => CreateInitialAggregateAsync(request, token),
                cancellationToken);
            if (operation.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(RoadSectionVersionWriteStatus.IdempotentConflict);
            }

            var view = JsonSerializer.Deserialize<RoadSectionVersionWriteView>(operation.OutcomeJson)
                ?? throw new InvalidOperationException("Stored road section outcome is invalid.");
            return new(
                operation.Status == IdempotencyOperationStatus.Executed
                    ? RoadSectionVersionWriteStatus.Success
                    : RoadSectionVersionWriteStatus.Replayed,
                view);
        }
        catch (RoadSectionVersionWriteException exception)
        {
            return new(exception.Status);
        }
        catch (ArgumentException)
        {
            return new(RoadSectionVersionWriteStatus.InvalidInput);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return new(RoadSectionVersionWriteStatus.RoadSectionCodeConflict);
        }
    }

    public async Task<RoadSectionVersionWriteResult> CreateNextAsync(
        RoadSectionNextVersionWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ActorUserId == Guid.Empty || request.ProjectId == Guid.Empty || request.RoadSectionId == Guid.Empty ||
            request.ExpectedCurrentVersionId == Guid.Empty || request.OperationId == Guid.Empty)
        {
            return new(RoadSectionVersionWriteStatus.InvalidInput);
        }

        try
        {
            var operation = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                NextOperation,
                request.OperationId.ToString("N"),
                NextFingerprint(request),
                token => CreateNextAggregateAsync(request, token),
                cancellationToken);
            if (operation.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(RoadSectionVersionWriteStatus.IdempotentConflict);
            }

            var view = JsonSerializer.Deserialize<RoadSectionVersionWriteView>(operation.OutcomeJson)
                ?? throw new InvalidOperationException("Stored road section version outcome is invalid.");
            return new(
                operation.Status == IdempotencyOperationStatus.Executed
                    ? RoadSectionVersionWriteStatus.Success
                    : RoadSectionVersionWriteStatus.Replayed,
                view);
        }
        catch (RoadSectionVersionWriteException exception)
        {
            return new(exception.Status);
        }
        catch (ArgumentException)
        {
            return new(RoadSectionVersionWriteStatus.InvalidInput);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return new(RoadSectionVersionWriteStatus.StaleConcurrency);
        }
    }

    public Task<RoadSectionVersionFacts?> GetInitialFactsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return _context.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => new RoadSectionVersionFacts(
                project.Id,
                project.EngineeringUtmSrid,
                null,
                null))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<RoadSectionVersionFacts?> GetNextFactsAsync(
        Guid roadSectionId,
        CancellationToken cancellationToken = default)
    {
        return (
                from section in _context.RoadSections.AsNoTracking()
                where section.Id == roadSectionId
                join version in _context.RoadSectionVersions.AsNoTracking()
                    on section.Id equals version.RoadSectionId
                where version.IsCurrent
                select new RoadSectionVersionFacts(
                    section.ProjectId,
                    _context.Projects
                        .Where(project => project.Id == section.ProjectId)
                        .Select(project => project.EngineeringUtmSrid)
                        .SingleOrDefault(),
                    version.Id,
                    version.VersionNo))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task CreateInitialAsync(
        RoadSection section,
        RoadSectionVersion initialVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(initialVersion);
        await _transactionService.ExecuteAsync(
            async operationCancellationToken =>
            {
                _context.RoadSections.Add(section);
                await _context.SaveChangesAsync(operationCancellationToken);
                _context.RoadSectionVersions.Add(initialVersion);
            },
            cancellationToken);
    }

    private async Task<(Guid OperationId, string OutcomeJson)> CreateInitialAggregateAsync(
        RoadSectionInitialVersionWriteRequest request,
        CancellationToken cancellationToken)
    {
        var project = await _context.Projects.SingleOrDefaultAsync(
            candidate => candidate.Id == request.ProjectId,
            cancellationToken);
        if (project is null)
        {
            throw new RoadSectionVersionWriteException(RoadSectionVersionWriteStatus.ProjectNotFound);
        }

        if (project.Status == ProjectStatus.Closed)
        {
            throw new RoadSectionVersionWriteException(RoadSectionVersionWriteStatus.ProjectClosed);
        }

        if (project.EngineeringUtmSrid is not int projectSrid)
        {
            throw new RoadSectionVersionWriteException(RoadSectionVersionWriteStatus.InvalidInput);
        }

        Spatial.SpatialValidation.EnsureProjectEngineeringGeometry(request.Geometry, projectSrid);
        var roadSection = RoadSection.Create(Guid.NewGuid(), request.ProjectId, request.Code, request.Name);
        var version = RoadSectionVersion.Create(
            Guid.NewGuid(),
            roadSection.Id,
            1,
            true,
            request.Geometry,
            request.EffectiveFrom,
            request.ChangeReason);
        var now = DateTimeOffset.UtcNow;
        var snapshot = JsonSerializer.Serialize(new
        {
            road_section_id = roadSection.Id,
            road_section_version_id = version.Id,
            code = roadSection.Code,
            version_no = version.VersionNo
        });
        _context.RoadSections.Add(roadSection);
        _context.RoadSectionVersions.Add(version);
        _context.AuditLogs.Add(AuditLog.Create(
            Guid.NewGuid(),
            request.ActorUserId,
            now,
            "road_section_created",
            "RoadSection",
            roadSection.Id,
            beforeSnapshot: null,
            afterSnapshot: snapshot,
            reason: "ROAD_SECTION_CREATED",
            source: "PROJECT_API",
            correlationId: request.CorrelationId,
            snapshotAllowedPropertyNames: InitialAuditFields));
        await _context.SaveChangesAsync(cancellationToken);
        var view = new RoadSectionVersionWriteView(
            request.ProjectId,
            roadSection.Id,
            version.Id,
            roadSection.Code,
            version.VersionNo,
            version.IsCurrent,
            version.EffectiveFrom,
            version.ChangeReason);
        return (version.Id, JsonSerializer.Serialize(view));
    }

    private async Task<(Guid OperationId, string OutcomeJson)> CreateNextAggregateAsync(
        RoadSectionNextVersionWriteRequest request,
        CancellationToken cancellationToken)
    {
        var section = await _context.RoadSections.SingleOrDefaultAsync(
            candidate => candidate.Id == request.RoadSectionId && candidate.ProjectId == request.ProjectId,
            cancellationToken);
        if (section is null)
        {
            throw new RoadSectionVersionWriteException(RoadSectionVersionWriteStatus.RoadSectionNotFound);
        }

        var project = await _context.Projects.SingleOrDefaultAsync(
            candidate => candidate.Id == request.ProjectId,
            cancellationToken);
        if (project is null)
        {
            throw new RoadSectionVersionWriteException(RoadSectionVersionWriteStatus.ProjectNotFound);
        }

        if (project.Status == ProjectStatus.Closed)
        {
            throw new RoadSectionVersionWriteException(RoadSectionVersionWriteStatus.ProjectClosed);
        }

        if (project.EngineeringUtmSrid is not int projectSrid)
        {
            throw new RoadSectionVersionWriteException(RoadSectionVersionWriteStatus.InvalidInput);
        }

        Spatial.SpatialValidation.EnsureProjectEngineeringGeometry(request.Geometry, projectSrid);
        var current = await _context.RoadSectionVersions.SingleOrDefaultAsync(
            candidate => candidate.RoadSectionId == request.RoadSectionId && candidate.IsCurrent,
            cancellationToken);
        if (current is null || current.Id != request.ExpectedCurrentVersionId)
        {
            throw new RoadSectionVersionWriteException(RoadSectionVersionWriteStatus.StaleConcurrency);
        }

        var version = RoadSectionVersion.Create(
            Guid.NewGuid(),
            request.RoadSectionId,
            current.VersionNo + 1,
            true,
            request.Geometry,
            request.EffectiveFrom,
            request.ChangeReason);
        current.ClearCurrent();
        await _context.SaveChangesAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var snapshot = JsonSerializer.Serialize(new
        {
            road_section_id = request.RoadSectionId,
            road_section_version_id = version.Id,
            previous_version_id = current.Id,
            version_no = version.VersionNo
        });
        _context.RoadSectionVersions.Add(version);
        _context.AuditLogs.Add(AuditLog.Create(
            Guid.NewGuid(),
            request.ActorUserId,
            now,
            "road_section_version_created",
            "RoadSectionVersion",
            version.Id,
            beforeSnapshot: null,
            afterSnapshot: snapshot,
            reason: "ROAD_SECTION_VERSION_CREATED",
            source: "PROJECT_API",
            correlationId: request.CorrelationId,
            snapshotAllowedPropertyNames: NextAuditFields));
        await _context.SaveChangesAsync(cancellationToken);
        var view = new RoadSectionVersionWriteView(
            request.ProjectId,
            request.RoadSectionId,
            version.Id,
            section.Code,
            version.VersionNo,
            version.IsCurrent,
            version.EffectiveFrom,
            version.ChangeReason);
        return (version.Id, JsonSerializer.Serialize(view));
    }

    private static string InitialFingerprint(RoadSectionInitialVersionWriteRequest request)
    {
        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            project_id = request.ProjectId,
            code = request.Code.Trim(),
            name = request.Name?.Trim(),
            srid = request.Geometry.SRID,
            coordinates = request.Geometry.Coordinates.Select(coordinate => new { x = coordinate.X, y = coordinate.Y }),
            effective_from = request.EffectiveFrom.ToUniversalTime(),
            change_reason = request.ChangeReason.Trim()
        });
        return Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
    }

    private static string NextFingerprint(RoadSectionNextVersionWriteRequest request)
    {
        var canonical = JsonSerializer.SerializeToUtf8Bytes(new
        {
            project_id = request.ProjectId,
            road_section_id = request.RoadSectionId,
            expected_current_version_id = request.ExpectedCurrentVersionId,
            srid = request.Geometry.SRID,
            coordinates = request.Geometry.Coordinates.Select(coordinate => new { x = coordinate.X, y = coordinate.Y }),
            effective_from = request.EffectiveFrom.ToUniversalTime(),
            change_reason = request.ChangeReason.Trim()
        });
        return Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }
        }

        return false;
    }

    private sealed class RoadSectionVersionWriteException : Exception
    {
        public RoadSectionVersionWriteException(RoadSectionVersionWriteStatus status) => Status = status;

        public RoadSectionVersionWriteStatus Status { get; }
    }

    public async Task AddVersionAndMakeCurrentAsync(
        Guid roadSectionId,
        RoadSectionVersion nextVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nextVersion);
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var currentVersion = await _context.RoadSectionVersions
                    .SingleOrDefaultAsync(candidate => candidate.RoadSectionId == roadSectionId && candidate.IsCurrent, cancellationToken)
                    ?? throw new InvalidOperationException("Road section has no current version.");
                if (nextVersion.VersionNo <= currentVersion.VersionNo)
                {
                    throw new InvalidOperationException("Road section version number must increase.");
                }

                currentVersion.ClearCurrent();
                await _context.SaveChangesAsync(cancellationToken);
                _context.RoadSectionVersions.Add(nextVersion);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }

}
