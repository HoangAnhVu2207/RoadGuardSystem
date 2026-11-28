using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Projects;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Integration;
using RoadGuardSystem.BusinessObjects.Warranties;
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
    HandoverRequired,
    HandoverFileNotReady,
    HandoverVersionConflict,
    ProjectCodeConflict,
    IdempotentConflict,
    ProjectDatesInvalid,
    WarrantyDatesInvalid
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
    Guid? CorrelationId,
    DateOnly? WarrantyEndDate = null,
    IReadOnlyList<Guid>? HandoverFileIds = null,
    string? IdempotencyKey = null,
    string? HandoverFileVersion = null);

public sealed record ProjectCreationPersistenceView(
    Guid ProjectId,
    string ProjectCode,
    string Name,
    ProjectStatus Status,
    Guid PrimaryProjectManagerUserId,
    Guid HandoverDocumentId,
    DateOnly HandoverDate,
    DateOnly WarrantyEndDate,
    byte[] RowVersion);

public sealed record ProjectCreationPersistenceResult(
    ProjectCreationPersistenceStatus Status,
    ProjectCreationPersistenceView? Project = null);

public sealed class ProjectCreationPersistenceService : IProjectCreationRepository
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

    public async Task<ProjectCreationPersistenceResult?> TryGetReplayAsync(
        ProjectCreationPersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = await _context.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate =>
                candidate.ActorUserId == request.ActorUserId &&
                candidate.ProjectId == null &&
                candidate.Operation == Operation &&
                candidate.IdempotencyKey == request.OperationId.ToString("N"),
                cancellationToken);
        if (record is null)
        {
            return null;
        }

        if (!string.Equals(record.RequestFingerprint, Fingerprint(request), StringComparison.Ordinal))
        {
            return new(ProjectCreationPersistenceStatus.IdempotentConflict);
        }

        var view = JsonSerializer.Deserialize<ProjectCreationPersistenceView>(record.OutcomeJson)
            ?? throw new InvalidOperationException("Stored project creation outcome is invalid.");
        return new(ProjectCreationPersistenceStatus.Replayed, view);
    }

    public async Task<ProjectCreationFacts> GetFactsAsync(
        Guid primaryProjectManagerUserId,
        IReadOnlyList<Guid>? handoverFileIds,
        CancellationToken cancellationToken = default)
    {
        var primaryProjectManagerIsEligible = await _context.Users.AsNoTracking().AnyAsync(user =>
            user.Id == primaryProjectManagerUserId &&
            user.Status == UserStatus.Active &&
            user.RoleCode == UserRoleCode.ProjectManager,
            cancellationToken);
        var fileIds = handoverFileIds ?? [];
        var handoverFileExists = fileIds.Count <= 1 &&
            (fileIds.Count == 0 || await _context.Files.AsNoTracking()
                .AnyAsync(file => file.Id == fileIds[0], cancellationToken));
        return new(primaryProjectManagerIsEligible, handoverFileExists);
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

        try
        {
            var operation = await _idempotency.ExecuteSerializableAsync(
                request.ActorUserId,
                projectId: null,
                Operation,
                request.OperationId.ToString("N"),
                Fingerprint(request),
                operationCancellationToken => CreateAggregateAsync(request, operationCancellationToken),
                cancellationToken,
                receiptAccessGuard: token => GuardSupervisorAsync(request.ActorUserId, token));
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
        catch (ExistingProjectReceipt)
        {
            _context.ChangeTracker.Clear();
            return await CreateAsync(request, cancellationToken);
        }
        catch (ProjectCreationDenied error)
        {
            _context.ChangeTracker.Clear();
            return new(error.Status);
        }
        catch (ArgumentException exception) when (exception.ParamName == "endDate")
        {
            return new(ProjectCreationPersistenceStatus.ProjectDatesInvalid);
        }
        catch (ArgumentException)
        {
            return new(ProjectCreationPersistenceStatus.InvalidInput);
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
        await GuardSupervisorAsync(request.ActorUserId, cancellationToken);
        // The actor lock closes the gap between the receipt service's preflight and consuming the file.
        var receiptKey = request.OperationId.ToString("N");
        if (await _context.IdempotencyRecords.AsNoTracking().AnyAsync(row =>
            row.ActorUserId == request.ActorUserId && row.ProjectId == null &&
            row.Operation == Operation && row.IdempotencyKey == receiptKey, cancellationToken))
            throw new ExistingProjectReceipt();
        if (request.WarrantyEndDate < request.HandoverDate)
            throw new ProjectCreationDenied(ProjectCreationPersistenceStatus.WarrantyDatesInvalid);
        if (!await _context.Users.AnyAsync(user => user.Id == request.PrimaryProjectManagerUserId &&
            user.Status == UserStatus.Active && user.RoleCode == UserRoleCode.ProjectManager, cancellationToken))
            throw new ProjectCreationDenied(ProjectCreationPersistenceStatus.ProjectManagerNotFound);
        var now = DateTimeOffset.UtcNow;
        var scope = await RequireStagedHandoverAsync(request, now, cancellationToken);
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
            request.HandoverFileIds is { Count: > 0 } ? request.HandoverFileIds[0] : null,
            request.HandoverNotes);
        var warranty = Warranty.Create(
            Guid.NewGuid(),
            projectId,
            roadSectionId: null,
            handoverDocumentId: handoverId,
            handoverDate: request.HandoverDate,
            warrantyStartDate: request.HandoverDate,
            warrantyEndDate: request.WarrantyEndDate ?? request.HandoverDate,
            retainedValue: null,
            scope: WarrantyScope.Project,
            terms: null,
            sourceDocumentId: request.HandoverFileIds is { Count: > 0 } ? request.HandoverFileIds[0] : null,
            status: WarrantyStatus.Active);

        _context.Projects.Add(project);
        _context.ProjectMembers.Add(member);
        _context.HandoverDocuments.Add(handover);
        _context.Warranties.Add(warranty);
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
        scope.AttachStagedHandover(projectId);
        await _context.SaveChangesAsync(cancellationToken);
        var view = new ProjectCreationPersistenceView(
            project.Id,
            project.ProjectCode,
            project.Name,
            project.Status,
            member.UserId,
            handover.Id,
            handover.HandoverDate,
            warranty.WarrantyEndDate,
            project.RowVersion);
        return (projectId, JsonSerializer.Serialize(view));
    }

    private async Task GuardSupervisorAsync(Guid actor, CancellationToken token)
    {
        await Anh02ReceiptAuthority.LockAsync(_context, actor, null, token);
        if (!await new AnhHuyFactsRepository(_context).IsCurrentActorAsync(actor, UserRoleCode.Supervisor, token))
            throw new ProjectCreationDenied(ProjectCreationPersistenceStatus.ActorNotAuthorized);
    }

    private async Task<FileScope> RequireStagedHandoverAsync(ProjectCreationPersistenceRequest request,
        DateTimeOffset now, CancellationToken token)
    {
        if (request.HandoverFileIds is not { Count: 1 } || string.IsNullOrWhiteSpace(request.HandoverFileVersion))
            throw new ProjectCreationDenied(ProjectCreationPersistenceStatus.HandoverRequired);

        var id = request.HandoverFileIds[0];
        var file = await _context.Files.FromSqlInterpolated($"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        var scope = await _context.FileScopes.FromSqlInterpolated($"SELECT * FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={id}")
            .SingleOrDefaultAsync(token);
        var session = await _context.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={id}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (file is null || scope is not { ProjectId: null, TargetId: null, Purpose: FileScope.ProjectHandoverStaging } ||
            session is not { Purpose: "DOCUMENT" } || scope.OwnerUserId != request.ActorUserId ||
            session.OwnerUserId != request.ActorUserId || file.StorageUri != session.ObjectKey ||
            file.Checksum != session.ExpectedChecksumSha256 || file.MimeType != "application/pdf")
            throw new ProjectCreationDenied(ProjectCreationPersistenceStatus.HandoverFileNotFound);
        if (session.Status != UploadSessionStatus.Verified || now >= session.ExpiresAt)
            throw new ProjectCreationDenied(ProjectCreationPersistenceStatus.HandoverFileNotReady);
        if (request.HandoverFileVersion != Convert.ToBase64String(session.RowVersion))
            throw new ProjectCreationDenied(ProjectCreationPersistenceStatus.HandoverVersionConflict);
        return scope;
    }

    private sealed class ExistingProjectReceipt : Exception;

    private sealed class ProjectCreationDenied(ProjectCreationPersistenceStatus status) : Exception
    {
        public ProjectCreationPersistenceStatus Status { get; } = status;
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
            warranty_end_date = request.WarrantyEndDate,
            handover_file_ids = request.HandoverFileIds ??
                (request.HandoverFileId is Guid legacyFileId ? [legacyFileId] : []),
            handover_notes = request.HandoverNotes?.Trim()
        });
        var digest = Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
        return request.HandoverFileVersion is null ? digest : Convert.ToHexString(SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{digest}|{request.HandoverFileVersion}"))).ToLowerInvariant();
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

}
