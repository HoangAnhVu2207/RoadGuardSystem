using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Auditing;
using RoadGuardSystem.BusinessObjects.Files;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Repositories.Files;

public sealed class FileRepository : IFileRepository
{
    private const string Operation = "FileStored";
    private static readonly string[] AuditFields = ["size_bytes", "checksum"];
    private readonly RoadGuardDbContext _context;
    private readonly IFileContentStore _contentStore;
    private readonly IFileContentCleanup _cleanup;
    private readonly IdempotencyOperationService _idempotency;

    public FileRepository(
        RoadGuardDbContext context,
        IFileContentStore contentStore,
        IdempotencyOperationService idempotency)
    {
        _context = context;
        _contentStore = contentStore;
        _cleanup = contentStore as IFileContentCleanup
            ?? throw new ArgumentException("File content store must support compensating cleanup.", nameof(contentStore));
        _idempotency = idempotency;
    }

    public async Task<FileStoreResult> StoreAsync(
        StoreFileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IdempotencyKey);
        if (request.IdempotencyKey.Length > 200)
        {
            throw new ArgumentException("Idempotency key exceeds maximum length 200.", nameof(request));
        }

        if (request.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException("Correlation id must not be empty.", nameof(request));
        }

        if (request.IsSystemOriginated == request.UploadedByUserId.HasValue)
        {
            return new FileStoreResult(
                FileStoreStatus.OwnerNotFound,
                ErrorCode: FileStorageErrorCodes.OwnerNotFound);
        }

        if (request.UploadedByUserId.HasValue &&
            !await _context.Users.AsNoTracking().AnyAsync(
                user => user.Id == request.UploadedByUserId.Value,
                cancellationToken))
        {
            return new FileStoreResult(
                FileStoreStatus.OwnerNotFound,
                ErrorCode: FileStorageErrorCodes.OwnerNotFound);
        }

        var stored = await _contentStore.StoreAsync(
            request.Content,
            request.OriginalName,
            request.DeclaredMimeType,
            request.ExpectedChecksum,
            request.DeclaredSizeBytes,
            cancellationToken);

        try
        {
            var fingerprint = CreateFingerprint(request, stored);
            var operation = await _idempotency.ExecuteAsync(
                request.UploadedByUserId,
                projectId: null,
                Operation,
                request.IdempotencyKey,
                fingerprint,
                _ =>
                {
                    var fileId = Guid.NewGuid();
                    var now = DateTimeOffset.UtcNow;
                    var file = StoredFile.Create(
                        fileId,
                        stored.StorageUri,
                        stored.OriginalName,
                        stored.MimeType,
                        stored.SizeBytes,
                        stored.Checksum,
                        request.UploadedByUserId,
                        now,
                        request.RetentionUntil);
                    _context.Files.Add(file);

                    var snapshot = JsonSerializer.Serialize(new
                    {
                        size_bytes = stored.SizeBytes,
                        checksum = stored.Checksum
                    });
                    _context.AuditLogs.Add(AuditLog.Create(
                        Guid.NewGuid(),
                        request.UploadedByUserId,
                        now,
                        Operation,
                        "File",
                        fileId,
                        beforeSnapshot: null,
                        afterSnapshot: snapshot,
                        reason: "Verified immutable content stored",
                        source: request.IsSystemOriginated ? "SYSTEM" : "FILE_STORAGE",
                        correlationId: request.CorrelationId,
                        snapshotAllowedPropertyNames: AuditFields));

                    return Task.FromResult((fileId, JsonSerializer.Serialize(new
                    {
                        file_id = fileId,
                        mime_type = stored.MimeType,
                        size_bytes = stored.SizeBytes,
                        checksum = stored.Checksum
                    })));
                },
                cancellationToken);

            if (operation.Status == IdempotencyOperationStatus.Executed)
            {
                return Map(FileStoreStatus.Stored, operation.OperationId, stored);
            }

            await _cleanup.DeleteAsync(stored.StorageUri, CancellationToken.None);
            if (operation.Status == IdempotencyOperationStatus.Conflict)
            {
                return new FileStoreResult(
                    FileStoreStatus.Conflict,
                    operation.OperationId,
                    ErrorCode: FileStorageErrorCodes.IdempotencyConflict);
            }

            var existing = await _context.Files.AsNoTracking()
                .SingleAsync(file => file.Id == operation.OperationId, cancellationToken);
            return new FileStoreResult(
                FileStoreStatus.Replayed,
                existing.Id,
                existing.StorageUri,
                existing.MimeType,
                existing.SizeBytes,
                existing.Checksum);
        }
        catch
        {
            await _cleanup.DeleteAsync(stored.StorageUri, CancellationToken.None);
            throw;
        }
    }

    private static FileStoreResult Map(FileStoreStatus status, Guid fileId, StoredContent stored)
        => new(
            status,
            fileId,
            stored.StorageUri,
            stored.MimeType,
            stored.SizeBytes,
            stored.Checksum);

    private static string CreateFingerprint(StoreFileRequest request, StoredContent stored)
    {
        var canonical = string.Join('|',
            request.UploadedByUserId?.ToString("N") ?? "system",
            stored.OriginalName.Trim(),
            stored.MimeType,
            stored.SizeBytes,
            stored.Checksum,
            request.RetentionUntil?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
