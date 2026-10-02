namespace RoadGuardSystem.BusinessObjects.Reports;

public sealed class VerifiedEvidenceReference
{
    private VerifiedEvidenceReference()
    {
    }

    public Guid EvidenceId { get; private set; }
    public Guid FileId { get; private set; }
    public string FileVersion { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public EvidenceVerificationState VerificationState { get; private set; }
    public EvidenceCaptureMetadata? CaptureMetadata { get; private set; }

    public static VerifiedEvidenceReference Create(
        Guid evidenceId,
        Guid fileId,
        string fileVersion,
        Guid ownerUserId,
        EvidenceCaptureMetadata? captureMetadata = null)
    {
        if (evidenceId == Guid.Empty || fileId == Guid.Empty || ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("Evidence, file, and owner identifiers must not be empty.");
        }

        return new VerifiedEvidenceReference
        {
            EvidenceId = evidenceId,
            FileId = fileId,
            FileVersion = Normalize(fileVersion, nameof(fileVersion), 200),
            OwnerUserId = ownerUserId,
            VerificationState = EvidenceVerificationState.Verified,
            CaptureMetadata = captureMetadata
        };
    }

    private static string Normalize(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maximumLength}.", parameterName);
        }

        return normalized;
    }
}
