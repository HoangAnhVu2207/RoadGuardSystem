namespace RoadGuardSystem.BusinessObjects.Labels;

public sealed class TrainingLabelRevision
{
    private TrainingLabelRevision()
    {
    }

    public Guid Id { get; private set; }

    public Guid LabelId { get; private set; }

    public int Revision { get; private set; }

    public Guid SourceId { get; private set; }

    public string SourceVersion { get; private set; } = string.Empty;

    public Guid FileId { get; private set; }

    public decimal X { get; private set; }

    public decimal Y { get; private set; }

    public decimal Width { get; private set; }

    public decimal Height { get; private set; }

    public string DefectTypeCode { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public TrainingLabelReviewStatus Status { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public string? ReviewReason { get; private set; }

    public static TrainingLabelRevision Create(
        Guid id,
        Guid labelId,
        int revision,
        Guid sourceId,
        string sourceVersion,
        Guid fileId,
        decimal x,
        decimal y,
        decimal width,
        decimal height,
        string defectTypeCode,
        string reason)
    {
        if (id == Guid.Empty || labelId == Guid.Empty || sourceId == Guid.Empty || fileId == Guid.Empty)
        {
            throw new ArgumentException("Training label, source, and file identifiers must not be empty.");
        }

        if (revision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        ValidateNormalizedBbox(x, y, width, height);
        return new TrainingLabelRevision
        {
            Id = id,
            LabelId = labelId,
            Revision = revision,
            SourceId = sourceId,
            SourceVersion = NormalizeRequired(sourceVersion, nameof(sourceVersion), 200),
            FileId = fileId,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            DefectTypeCode = NormalizeRequired(defectTypeCode, nameof(defectTypeCode), 80),
            Reason = NormalizeRequired(reason, nameof(reason), 1_000),
            Status = TrainingLabelReviewStatus.Pending
        };
    }

    public void Review(
        Guid reviewedByUserId,
        TrainingLabelReviewStatus status,
        string reason,
        DateTimeOffset reviewedAt)
    {
        if (Status != TrainingLabelReviewStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending training label revision can be reviewed.");
        }

        if (reviewedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Reviewer identifier must not be empty.", nameof(reviewedByUserId));
        }

        if (status is not (TrainingLabelReviewStatus.Approved or TrainingLabelReviewStatus.Rejected))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        Status = status;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAt.ToUniversalTime();
        ReviewReason = NormalizeRequired(reason, nameof(reason), 1_000);
    }

    private static void ValidateNormalizedBbox(decimal x, decimal y, decimal width, decimal height)
    {
        if (x < 0 || x > 1 || y < 0 || y > 1 || width <= 0 || height <= 0 || x + width > 1 || y + height > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "A normalized bounding box must be contained within the image.");
        }
    }

    private static string NormalizeRequired(string value, string parameterName, int maximumLength)
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
