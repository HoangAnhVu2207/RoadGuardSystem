using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Files;
using RoadGuardSystem.Repositories.Idempotency;
using RoadGuardSystem.Repositories.Offline;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Repositories.Implementations.Files;

public sealed partial class UploadPersistenceService
{
    public async Task<bool> IsCurrentOfflineFileActorAsync(Guid actor, RoadGuardSystem.aBusinessObjects.Commons.UserRoleCode role,
        Guid fileId, CancellationToken token = default)
    {
        async Task<bool> Read(CancellationToken ct)
        {
            var capture = await _context.Set<RoadGuardSystem.BusinessObjects.Offline.OfflineEvidenceCaptureReference>()
                .AsNoTracking().SingleOrDefaultAsync(row => row.FileId == fileId && row.ActualUploaderId == actor, ct);
            if (capture is null) return false;
            var session = await _context.UploadSessions.AsNoTracking().SingleOrDefaultAsync(row => row.Id == capture.UploadSessionId, ct);
            var file = await _context.Files.AsNoTracking().SingleOrDefaultAsync(row => row.Id == fileId, ct);
            if (session is null || file is null) return false;
            var request = new UploadCreatePersistenceRequest(actor, capture.ProjectId, capture.TaskId, capture.Purpose,
                file.OriginalName, capture.MediaType, file.SizeBytes, capture.Checksum, session.PartSizeBytes, session.ExpiresAt, "read", "read", null);
            try
            {
                await OfflineUploads.GuardCommittedUploadAsync(new(capture.ProjectId, capture.TaskId, capture.AdmissionId,
                    capture.CaptureOriginId, actor, role, request), fileId, session.Id, ct); return true;
            }
            catch (OfflineAdmissionRejectedException) { return false; }
        }
        return _context.Database.CurrentTransaction is not null ? await Read(token) : await MultipartTransactionAsync(Read, token);
    }
    private IOfflineUploadAdmissionValidator OfflineUploads => _offlineUploads ?? throw new InvalidOperationException("Offline upload admission is not configured.");
    private readonly IOfflineUploadAdmissionValidator? _offlineUploads;
    public UploadPersistenceService(RoadGuardDbContext db, IdempotencyOperationService receipts,
        IUploadObjectStorage storage, IOptions<UploadSessionOptions> options, TimeProvider clock,
        IOfflineUploadAdmissionValidator offline) : this(db, receipts, storage, options, clock) => _offlineUploads = offline;

    private async Task GuardOfflineUploadReceiptAsync(OfflineUploadCaptureRequest request, CancellationToken token)
    {
        var receipt = await _context.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(row =>
            row.ActorUserId == request.CurrentActorId && row.ProjectId == request.ProjectId &&
            row.Operation == "OfflineUploadSessionCreated" && row.IdempotencyKey == request.Upload.IdempotencyKey, token);
        if (receipt is null) { await OfflineUploads.GuardNewUploadAsync(request, token); return; }
        var view = JsonSerializer.Deserialize<UploadSessionPersistenceView>(receipt.OutcomeJson)
            ?? throw new InvalidOperationException("Stored offline upload outcome is invalid.");
        await OfflineUploads.GuardCommittedUploadAsync(request, view.FileId, view.Id, token);
    }
}
