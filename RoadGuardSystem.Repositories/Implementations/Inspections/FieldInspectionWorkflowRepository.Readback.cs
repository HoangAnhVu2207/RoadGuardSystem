using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RoadGuardSystem.BusinessObjects.Inspections;
using RoadGuardSystem.BusinessObjects.PersistenceFacts.Inspections;

namespace RoadGuardSystem.Repositories.Implementations.Inspections;

public sealed partial class FieldInspectionWorkflowRepository
{
    private static void ValidateCaptureCoordinates(FieldCaptureCoordinatesFact? coordinates)
    {
        if (coordinates is null) return;
        if (coordinates.Longitude is not double lon || coordinates.Latitude is not double lat ||
            !double.IsFinite(lon) || !double.IsFinite(lat) || lon is < -180 or > 180 || lat is < -90 or > 90 ||
            coordinates.AccuracyMeters is double accuracy && (!double.IsFinite(accuracy) || accuracy < 0) ||
            coordinates.Source?.Length > 120)
            throw new ArgumentException("Capture WGS84 coordinates require a finite pair and nonnegative accuracy in metres.");
    }

    // Payload is the hashed declaration used by readiness/review. The separate proof is an
    // immutable persistence projection, not a fallback for a missing declaration.
    private async Task<FieldPositionProofInputFact?> SubmissionProofAsync(FieldInspectionSubmission row,
        FieldSubmissionInputFact payload, CancellationToken token)
    {
        var proofs = await db.Set<FieldInspectionLocationProof>().AsNoTracking()
            .Where(x => x.SubmissionId == row.Id).ToArrayAsync(token);
        if (proofs.Length > 1 || proofs.Any(x => x.ProjectId != row.ProjectId || x.TaskId != row.TaskId))
            Deny(409, "field_submission_snapshot_inconsistent");
        if (payload.LocationProof is null)
        {
            if (proofs.Length == 0) return null; // legacy payload without a proof
            using var facts = JsonDocument.Parse(proofs[0].FactsJson);
            if (proofs[0].Kind != "UNKNOWN" || !facts.RootElement.TryGetProperty("reason", out var reason) ||
                reason.GetString() != "No location proof declared" || facts.RootElement.EnumerateObject().Count() != 1)
                Deny(409, "field_submission_snapshot_inconsistent");
            return null;
        }
        if (proofs.Length != 1 || proofs[0].Kind != payload.LocationProof.Kind ||
            Decode<FieldPositionProofInputFact>(proofs[0].FactsJson) != payload.LocationProof)
            Deny(409, "field_submission_snapshot_inconsistent");
        return payload.LocationProof;
    }

    private async Task<FieldEvidenceReferenceFact[]> EvidenceReferencesAsync(FieldInspectionSubmission row,
        FieldSubmissionInputFact payload, CancellationToken token)
    {
        var links = await db.Set<FieldInspectionEvidenceLink>().AsNoTracking()
            .Where(x => x.SubmissionId == row.Id).ToArrayAsync(token);
        var declarations = payload.Evidence ?? [];
        if (links.Length != declarations.Length) Deny(409, "field_submission_snapshot_inconsistent");
        var references = new List<FieldEvidenceReferenceFact>();
        foreach (var declaration in declarations)
        {
            var matches = links.Where(x => x.CaptureOriginId == declaration.CaptureOriginId).ToArray();
            if (matches.Length != 1) Deny(409, "field_submission_snapshot_inconsistent");
            var link = matches[0];
            using var snapshot = JsonDocument.Parse(link.CaptureFactsJson);
            var facts = snapshot.RootElement;
            if (link.ProjectId != row.ProjectId || link.TaskId != row.TaskId || link.AssignmentId != row.AssignmentId ||
                link.Purpose != declaration.Purpose || !string.Equals(link.DeclaredChecksum, declaration.ChecksumSha256, StringComparison.OrdinalIgnoreCase) ||
                link.MediaType != declaration.MediaType || declaration.FileId is Guid declaredFile && link.FileId != declaredFile ||
                !facts.TryGetProperty("declared", out var declared) || Decode<FieldEvidenceDeclarationFact>(declared.GetRawText()) != declaration)
                Deny(409, "field_submission_snapshot_inconsistent");
            if (facts.TryGetProperty("actualFileId", out var actualFile) && actualFile.GetGuid() != link.FileId)
                Deny(409, "field_submission_snapshot_inconsistent");
            Guid? admission = null, grant = null;
            if (facts.TryGetProperty("offlineAdmission", out var offline))
            {
                if (offline.TryGetProperty("admissionId", out var a)) admission = a.GetGuid();
                if (offline.TryGetProperty("grantId", out var g) && g.ValueKind != JsonValueKind.Null) grant = g.GetGuid();
            }
            references.Add(new(link.Id, link.CaptureOriginId, link.FileId,
                facts.TryGetProperty("fileVersion", out var version) ? version.GetString() : null,
                link.DeclaredChecksum, row.OriginalActorId, payload.DeviceId,
                facts.TryGetProperty("actualUploaderId", out var uploader) ? uploader.GetGuid() : null, admission, grant));
        }
        return references.ToArray();
    }
}
