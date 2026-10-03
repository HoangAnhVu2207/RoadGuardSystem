using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Storage;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.Repositories.Implementations.Files;

public sealed class UploadPersistenceService : IUploadRepository
{
    private const string CreateOperation = "UploadSessionCreated";
    private const string CompleteOperation = "UploadSessionCompleted";
    private readonly RoadGuardDbContext _context;
    private readonly IdempotencyOperationService _idempotency;
    private readonly IUploadObjectStorage _storage;

    public UploadPersistenceService(
        RoadGuardDbContext context,
        IdempotencyOperationService idempotency,
        IUploadObjectStorage storage)
    {
        _context = context;
        _idempotency = idempotency;
        _storage = storage;
    }

    public Task<bool> IsCurrentSurveyOperatorAsync(Guid actorUserId, Guid projectId, Guid taskId, bool forUpload, CancellationToken cancellationToken = default)
        => (from task in _context.SurveyRequests.AsNoTracking()
            join assignment in _context.SurveyAssignments.AsNoTracking() on task.Id equals assignment.SurveyRequestId
            where task.Id == taskId && task.ProjectId == projectId && assignment.OperatorUserId == actorUserId && assignment.EndedAt == null
                && (!forUpload || ((task.Status == SurveyRequestStatus.Accepted || task.Status == SurveyRequestStatus.InProgress)
                    && !_context.SurveyRequests.Any(childTask => childTask.ParentTaskId == task.Id)))
            select task.Id).AnyAsync(cancellationToken);

    public async Task<UploadMutationPersistenceResult> CreateAsync(
        UploadCreatePersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SizeBytes <= 0 || UploadAdmissionPolicy.MaximumBytes(request.Purpose, request.MediaType) is not { } maximum || request.SizeBytes > maximum)
        {
            return new(UploadPersistenceStatus.InvalidInput, null);
        }

        try
        {
            var outcome = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                CreateOperation,
                request.IdempotencyKey,
                request.RequestFingerprint,
                async token =>
                {
                    if (request.ProjectId is null) await GuardPrivateAsync(request.ActorUserId, null, token);
                    var now = DateTimeOffset.UtcNow;
                    var fileId = Guid.NewGuid();
                    var objectKey = $"uploads/{fileId:N}";
                    var file = StoredFile.Create(
                        fileId,
                        objectKey,
                        request.FileName,
                        request.MediaType,
                        request.SizeBytes,
                        request.ChecksumSha256,
                        request.ActorUserId,
                        now,
                        retentionUntil: null);
                    var session = UploadSession.Create(
                        Guid.NewGuid(),
                        fileId,
                        request.ActorUserId,
                        objectKey,
                        request.Purpose,
                        request.MediaType,
                        request.SizeBytes,
                        request.ChecksumSha256,
                        request.PartSizeBytes,
                        request.ExpiresAt);
                    _context.Files.Add(file);
                    _context.FileScopes.Add(request.ProjectId is { } projectId
                        ? FileScope.Create(Guid.NewGuid(), fileId, projectId, request.TargetId, request.ActorUserId, request.Purpose, now)
                        : request.Purpose == "REPORT_PHOTO" && request.TargetId is null
                            ? FileScope.CreatePrivate(Guid.NewGuid(), fileId, request.ActorUserId, now)
                            : throw new ArgumentException("Invalid private scope."));
                    _context.UploadSessions.Add(session);
                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(),
                        request.ActorUserId,
                        now,
                        "upload_session_created",
                        "UploadSession",
                        session.Id,
                        null,
                        JsonSerializer.Serialize(new { session.Id, session.FileId, request.ProjectId, request.Purpose }),
                        "Upload session created",
                        "p2-023",
                        request.CorrelationId,
                        ["id", "fileId", "projectId", "purpose"]));
                    await _context.SaveChangesAsync(token);
                    return (session.Id, JsonSerializer.Serialize(ToView(session, request.ProjectId, request.TargetId)));
                },
                cancellationToken,
                receiptAccessGuard: request.ProjectId is null
                    ? token => GuardPrivateAsync(request.ActorUserId, null, token) : null);

            if (outcome.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(UploadPersistenceStatus.Conflict, null);
            }

            var view = JsonSerializer.Deserialize<UploadSessionPersistenceView>(outcome.OutcomeJson)
                ?? throw new InvalidOperationException("Upload session replay payload is invalid.");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? UploadPersistenceStatus.Replayed : UploadPersistenceStatus.Success, view);
        }
        catch (UploadNotFoundException)
        {
            return new(UploadPersistenceStatus.NotFound, null);
        }
        catch (ArgumentException)
        {
            return new(UploadPersistenceStatus.InvalidInput, null);
        }
    }

    public async Task<UploadSessionPersistenceView?> GetSessionAsync(Guid uploadId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from session in _context.UploadSessions.AsNoTracking()
            join scope in _context.FileScopes.AsNoTracking() on session.FileId equals scope.FileId
            where session.Id == uploadId
            select new { Session = session, scope.ProjectId, scope.TargetId })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : ToView(row.Session, row.ProjectId, row.TargetId);
    }

    public async Task<UploadPartUrlsPersistenceResult> GetPartUrlsAsync(
        Guid actorUserId,
        Guid? projectId,
        Guid uploadId,
        IReadOnlyList<int> partNumbers,
        string idempotencyKey,
        string requestFingerprint,
        DateTimeOffset now,
        DateTimeOffset urlExpiresAt,
        CancellationToken cancellationToken = default)
    {
        // Session-owned SQL application lock serializes initialization across processes without
        // holding a SQL transaction over a storage call. Connection loss releases the claim.
        await _context.Database.OpenConnectionAsync(cancellationToken);
        var resource = $"anh01.upload:{uploadId:N}";
        var acquired = false;
        try
        {
            var result = await _context.Database.SqlQueryRaw<int>(
                "DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource={0}, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=15000; SELECT @r AS [Value]", resource)
                .ToListAsync(cancellationToken);
            acquired = result.Single() >= 0;
            if (!acquired) return new(UploadPersistenceStatus.StorageUnavailable, []);
            _context.ChangeTracker.Clear();
            return await GetPartUrlsCoreAsync(actorUserId, projectId, uploadId, partNumbers,
                idempotencyKey, requestFingerprint, now, urlExpiresAt, cancellationToken);
        }
        finally
        {
            if (acquired)
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sys.sp_releaseapplock @Resource={0}, @LockOwner='Session'", [resource], CancellationToken.None);
            await _context.Database.CloseConnectionAsync();
        }
    }

    private async Task<UploadPartUrlsPersistenceResult> GetPartUrlsCoreAsync(
        Guid actorUserId, Guid? projectId, Guid uploadId, IReadOnlyList<int> partNumbers,
        string idempotencyKey, string requestFingerprint, DateTimeOffset now,
        DateTimeOffset urlExpiresAt, CancellationToken cancellationToken)
    {
        if (partNumbers.Count == 0 || partNumbers.Distinct().Count() != partNumbers.Count || partNumbers.Any(number => number < 1))
        {
            return new(UploadPersistenceStatus.InvalidInput, []);
        }

        var session = await _context.UploadSessions
            .SingleOrDefaultAsync(candidate => candidate.Id == uploadId, cancellationToken);
        if (session is null)
        {
            return new(UploadPersistenceStatus.NotFound, []);
        }

        if (session.Status is UploadSessionStatus.Verifying or UploadSessionStatus.Verified or UploadSessionStatus.Failed || now >= session.ExpiresAt)
        {
            return new(UploadPersistenceStatus.Conflict, []);
        }

        var partCount = (session.ExpectedSizeBytes - 1) / session.PartSizeBytes + 1;
        if (partNumbers.Any(number => number > partCount))
        {
            return new(UploadPersistenceStatus.InvalidInput, []);
        }

        try
        {
            var existingReceipt = await _context.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(
                receipt => receipt.ActorUserId == actorUserId && receipt.ProjectId == projectId &&
                    receipt.Operation == "UploadPartUrlsIssued" && receipt.IdempotencyKey == idempotencyKey, cancellationToken);
            if (projectId is not null && existingReceipt is not null && existingReceipt.RequestFingerprint != requestFingerprint)
                return new(UploadPersistenceStatus.Conflict, []);
            if (projectId is not null && string.IsNullOrWhiteSpace(session.StorageUploadId))
            {
                var uploadStorageId = await _storage.InitiateAsync(session.ObjectKey, session.MediaType, cancellationToken);
                session.StartUploading(uploadStorageId, now);
            }

            await _context.SaveChangesAsync(cancellationToken);
            // The durable private claim commits BEFORE external I/O. Never put
            // initiation in a SQL execution-strategy/receipt delegate. If the process
            // loses the storage acknowledgement, later requests fail closed on the
            // claim; they cannot create an unbounded succession of orphan multiparts.
            string? privateStorageId = null;
            if (projectId is null)
            {
                var claim = $"multipart_initiating:{Guid.NewGuid():N}";
                var strategy = _context.Database.CreateExecutionStrategy();
                var canInitiate = await strategy.ExecuteAsync(async () =>
                {
                    _context.ChangeTracker.Clear();
                    await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                    await GuardPrivateAsync(actorUserId, session.FileId, cancellationToken);
                    var receipt = await _context.IdempotencyRecords.AsNoTracking().AnyAsync(r =>
                        r.ActorUserId == actorUserId && r.ProjectId == null && r.Operation == "UploadPartUrlsIssued"
                        && r.IdempotencyKey == idempotencyKey, cancellationToken);
                    var current = await _context.UploadSessions.SingleAsync(s => s.Id == uploadId, cancellationToken);
                    if (receipt || !string.IsNullOrWhiteSpace(current.StorageUploadId))
                    {
                        await transaction.CommitAsync(cancellationToken);
                        return false;
                    }
                    if (current.FailureCode is not null && current.FailureCode != claim)
                        throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Multipart initiation outcome requires reconciliation.");
                    if (current.FailureCode is null)
                    {
                        current.ClaimMultipartInitiation(claim, now);
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                });
                if (canInitiate)
                    privateStorageId = await _storage.InitiateAsync(session.ObjectKey, session.MediaType, cancellationToken);
            }
            var outcome = await _idempotency.ExecuteAsync(
                actorUserId,
                projectId,
                "UploadPartUrlsIssued",
                idempotencyKey,
                requestFingerprint,
                async token =>
                {
                    if (projectId is null)
                    {
                        await GuardPrivateAsync(actorUserId, session.FileId, token);
                        // A competing operation may have won while the authority lock
                        // was acquired. Do not initiate another multipart before recovery.
                        var won = await _context.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(r =>
                            r.ActorUserId == actorUserId && r.ProjectId == null && r.Operation == "UploadPartUrlsIssued"
                            && r.IdempotencyKey == idempotencyKey, token);
                        if (won is not null)
                        {
                            if (won.RequestFingerprint != requestFingerprint) throw new UploadPartConflictException();
                            return (won.OperationId, won.OutcomeJson);
                        }
                    }
                    var current = await _context.UploadSessions.SingleOrDefaultAsync(candidate => candidate.Id == uploadId, token)
                        ?? throw new UploadNotFoundException();
                    if (projectId is null && string.IsNullOrWhiteSpace(current.StorageUploadId))
                    {
                        if (privateStorageId is null)
                            throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Multipart initiation outcome requires reconciliation.");
                        // This acknowledged ID survives all SQL retries in this call.
                        current.StartUploading(privateStorageId, now);
                    }
                    foreach (var partNumber in partNumbers)
                    {
                        var row = await _context.UploadParts.SingleOrDefaultAsync(
                            candidate => candidate.UploadSessionId == current.Id && candidate.PartNumber == partNumber,
                            token);
                        if (row is null)
                        {
                            row = UploadPart.Create(Guid.NewGuid(), current.Id, partNumber);
                            _context.UploadParts.Add(row);
                        }

                        row.RecordUrl(now, urlExpiresAt);
                    }

                    await _context.SaveChangesAsync(token);
                    return (Guid.NewGuid(), JsonSerializer.Serialize(new UploadPartUrlReceipt(
                        current.ObjectKey,
                        current.StorageUploadId!,
                        partNumbers.OrderBy(number => number).ToArray(),
                        urlExpiresAt)));
                },
                cancellationToken,
                receiptAccessGuard: projectId is null ? token => GuardPrivateAsync(actorUserId, session.FileId, token) : null);
            if (outcome.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(UploadPersistenceStatus.Conflict, []);
            }

            var receipt = JsonSerializer.Deserialize<UploadPartUrlReceipt>(outcome.OutcomeJson)
                ?? throw new InvalidOperationException("Upload part URL replay payload is invalid.");
            if (now >= receipt.ExpiresAt)
                return new(UploadPersistenceStatus.Conflict, []);
            var parts = await _storage.PresignPartsAsync(receipt.ObjectKey, receipt.StorageUploadId, receipt.PartNumbers, receipt.ExpiresAt, cancellationToken);
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? UploadPersistenceStatus.Replayed : UploadPersistenceStatus.Success, parts);
        }
        catch (FileStorageException)
        {
            return new(UploadPersistenceStatus.StorageUnavailable, []);
        }
        catch (UploadNotFoundException)
        {
            return new(UploadPersistenceStatus.NotFound, []);
        }
        catch (UploadPartConflictException)
        {
            return new(UploadPersistenceStatus.Conflict, []);
        }
    }

    public async Task<UploadMutationPersistenceResult> CompleteAsync(
        UploadCompletePersistenceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var outcome = await _idempotency.ExecuteAsync(
                request.ActorUserId,
                request.ProjectId,
                CompleteOperation,
                request.IdempotencyKey,
                request.RequestFingerprint,
                async token =>
                {
                    var session = await _context.UploadSessions.SingleOrDefaultAsync(candidate => candidate.Id == request.UploadId, token)
                        ?? throw new UploadNotFoundException();
                    var scope = await _context.FileScopes.AsNoTracking().SingleAsync(candidate => candidate.FileId == session.FileId, token);
                    if (request.ProjectId is null) await GuardPrivateAsync(request.ActorUserId, session.FileId, token);
                    if (!string.Equals(session.ExpectedChecksumSha256, request.ChecksumSha256, StringComparison.Ordinal))
                    {
                        throw new ArgumentException("Upload checksum does not match session expectation.");
                    }

                    var expectedPartCount = (session.ExpectedSizeBytes - 1) / session.PartSizeBytes + 1;
                    if (request.Parts.Count != expectedPartCount || request.Parts.Select(part => part.PartNumber).Distinct().Count() != expectedPartCount || request.Parts.Any(part => part.PartNumber < 1 || part.PartNumber > expectedPartCount))
                    {
                        throw new ArgumentException("Upload parts are incomplete.");
                    }

                    if (!session.RowVersion.SequenceEqual(Convert.FromBase64String(request.ExpectedVersion)))
                    {
                        throw new UploadConcurrencyException();
                    }

                    session.StartVerification(request.ExpectedVersion, DateTimeOffset.UtcNow);
                    foreach (var completedPart in request.Parts)
                    {
                        var part = await _context.UploadParts.SingleOrDefaultAsync(
                            candidate => candidate.UploadSessionId == session.Id && candidate.PartNumber == completedPart.PartNumber,
                            token) ?? throw new ArgumentException("Upload part was not issued.");
                        part.RecordCompletion(completedPart.ETag);
                    }

                    var now = DateTimeOffset.UtcNow;
                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(), request.ActorUserId, now, "upload_verification_requested", "UploadSession", session.Id,
                        null, JsonSerializer.Serialize(new { session.Id, session.FileId }), "Upload verification requested", "p2-025", request.CorrelationId,
                        ["id", "fileId"]));
                    await _context.SaveChangesAsync(token);
                    return (Guid.NewGuid(), JsonSerializer.Serialize(ToView(session, scope.ProjectId)));
                },
                cancellationToken,
                receiptAccessGuard: request.ProjectId is null
                    ? async token =>
                    {
                        var fileId = await _context.UploadSessions.AsNoTracking().Where(s => s.Id == request.UploadId)
                            .Select(s => (Guid?)s.FileId).SingleOrDefaultAsync(token);
                        if (fileId is null) throw new UploadNotFoundException();
                        await GuardPrivateAsync(request.ActorUserId, fileId, token);
                    } : null);

            if (outcome.Status == IdempotencyOperationStatus.Conflict)
            {
                return new(UploadPersistenceStatus.Conflict, null);
            }

            var view = JsonSerializer.Deserialize<UploadSessionPersistenceView>(outcome.OutcomeJson)
                ?? throw new InvalidOperationException("Upload completion replay payload is invalid.");
            return new(outcome.Status == IdempotencyOperationStatus.Replayed ? UploadPersistenceStatus.Replayed : UploadPersistenceStatus.Success, view);
        }
        catch (UploadNotFoundException)
        {
            return new(UploadPersistenceStatus.NotFound, null);
        }
        catch (FormatException)
        {
            return new(UploadPersistenceStatus.ConcurrencyConflict, null);
        }
        catch (UploadConcurrencyException)
        {
            return new(UploadPersistenceStatus.ConcurrencyConflict, null);
        }
        catch (InvalidOperationException)
        {
            return new(UploadPersistenceStatus.Conflict, null);
        }
        catch (ArgumentException)
        {
            return new(UploadPersistenceStatus.InvalidInput, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return new(UploadPersistenceStatus.ConcurrencyConflict, null);
        }
    }

    public async Task<FileMetadataPersistenceView?> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from file in _context.Files.AsNoTracking()
            join scope in _context.FileScopes.AsNoTracking() on file.Id equals scope.FileId
            join session in _context.UploadSessions.AsNoTracking() on file.Id equals session.FileId
            where file.Id == fileId
            select new { File = file, Scope = scope, Session = session })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new FileMetadataPersistenceView(
                row.File.Id,
                row.Scope.OwnerUserId,
                row.Scope.ProjectId,
                row.File.StorageUri,
                row.Session.Status.ToString().ToUpperInvariant(),
                row.File.Checksum,
                row.File.MimeType,
                row.File.SizeBytes,
                Convert.ToBase64String(row.Session.RowVersion), row.Scope.Purpose, row.Scope.TargetId);
    }

    private async Task GuardPrivateAsync(Guid actor, Guid? fileId, CancellationToken token)
    {
        if (_context.Database.CurrentTransaction is null) throw new InvalidOperationException("Private receipt requires its scoped transaction.");
        await Anh02ReceiptAuthority.LockAsync(_context, actor, null, token);
        if (!await new RoadGuardSystem.Repositories.Integration.AnhHuyFactsRepository(_context)
            .IsCurrentActorAsync(actor, UserRoleCode.Reporter, token)) throw new UploadNotFoundException();
        if (fileId is not { } id) return;
        await _context.Files.FromSqlInterpolated($"SELECT * FROM [Files] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id}").AsNoTracking().ToListAsync(token);
        var scope = await _context.FileScopes.FromSqlInterpolated($"SELECT * FROM [FileScopes] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={id}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        var session = await _context.UploadSessions.FromSqlInterpolated($"SELECT * FROM [UploadSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FileId]={id}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (scope is null || session is null || scope.OwnerUserId != actor || session.OwnerUserId != actor
            || scope.ProjectId is not null || scope.TargetId is not null || scope.Purpose != "REPORT_PHOTO") throw new UploadNotFoundException();
    }

    public Task<Stream> OpenFileAsync(string objectKey, CancellationToken cancellationToken = default)
        => _storage.OpenReadAsync(objectKey, cancellationToken);

    public async Task<UploadPersistenceStatus> VerifyNextAsync(CancellationToken cancellationToken = default)
    {
        var session = await _context.UploadSessions
            .OrderBy(candidate => candidate.ExpiresAt)
            .FirstOrDefaultAsync(candidate => candidate.Status == UploadSessionStatus.Verifying, cancellationToken);
        if (session is null)
        {
            return UploadPersistenceStatus.NotFound;
        }

        var parts = await _context.UploadParts
            .Where(part => part.UploadSessionId == session.Id)
            .OrderBy(part => part.PartNumber)
            .ToListAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(session.StorageUploadId) || parts.Count == 0 || parts.Any(part => string.IsNullOrWhiteSpace(part.ETag)))
        {
            session.MarkFailed("upload_parts_missing");
            await _context.SaveChangesAsync(cancellationToken);
            return UploadPersistenceStatus.InvalidInput;
        }

        try
        {
            var verification = await _storage.CompleteAndVerifyAsync(
                session.ObjectKey,
                session.StorageUploadId,
                parts.Select(part => new CompletedStoragePart(part.PartNumber, part.ETag!)).ToArray(),
                cancellationToken);
            if (UploadAdmissionPolicy.MaximumBytes(session.Purpose, verification.MimeType) is not { } maximum || verification.SizeBytes > maximum)
            {
                session.MarkFailed("file_limit_or_type_mismatch");
            }
            else if (verification.SizeBytes != session.ExpectedSizeBytes)
            {
                session.MarkFailed("file_size_mismatch");
            }
            else if (!CryptographicOperations.FixedTimeEquals(
                         Convert.FromHexString(verification.ChecksumSha256),
                         Convert.FromHexString(session.ExpectedChecksumSha256)))
            {
                session.MarkFailed("file_checksum_mismatch");
            }
            else if (!string.Equals(verification.MimeType, session.MediaType, StringComparison.OrdinalIgnoreCase))
            {
                session.MarkFailed("file_mime_mismatch");
            }
            else
            {
                session.MarkVerified();
            }

            await _context.SaveChangesAsync(cancellationToken);
            return UploadPersistenceStatus.Success;
        }
        catch (FileStorageException)
        {
            _context.ChangeTracker.Clear();
            return UploadPersistenceStatus.StorageUnavailable;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another worker persisted the terminal outcome. Never overwrite it or report a
            // transient storage/read failure as invalid evidence.
            _context.ChangeTracker.Clear();
            return UploadPersistenceStatus.Conflict;
        }
    }

    private static UploadSessionPersistenceView ToView(UploadSession session, Guid? projectId, Guid? targetId = null)
        => new(
            session.Id,
            session.FileId,
            session.OwnerUserId,
            projectId,
            session.ObjectKey,
            session.Status.ToString().ToUpperInvariant(),
            session.PartSizeBytes,
            session.ExpiresAt,
            Convert.ToBase64String(session.RowVersion), session.Purpose, targetId);

    private sealed class UploadNotFoundException : Exception;

    private sealed class UploadConcurrencyException : Exception;
    private sealed class UploadPartConflictException : Exception;

    private sealed record UploadPartUrlReceipt(
        string ObjectKey,
        string StorageUploadId,
        IReadOnlyList<int> PartNumbers,
        DateTimeOffset ExpiresAt);
}
