namespace RoadGuardSystem.BusinessObjects.Reports;

public sealed class ReportEvidence
{
    private ReportEvidence()
    {
    }

    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid? SupplementId { get; private set; }
    public Guid FileId { get; private set; }
    public string FileVersion { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public EvidenceVerificationState VerificationState { get; private set; }
    public EvidenceCaptureMetadata? CaptureMetadata { get; private set; }

    internal static ReportEvidence FromReference(Guid reportId, Guid? supplementId, VerifiedEvidenceReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (reference.VerificationState != EvidenceVerificationState.Verified)
        {
            throw new InvalidOperationException("Only verified evidence can be attached to a report.");
        }

        return new ReportEvidence
        {
            Id = reference.EvidenceId,
            ReportId = reportId,
            SupplementId = supplementId,
            FileId = reference.FileId,
            FileVersion = reference.FileVersion,
            OwnerUserId = reference.OwnerUserId,
            VerificationState = reference.VerificationState,
            CaptureMetadata = reference.CaptureMetadata
        };
    }
}
