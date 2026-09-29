using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Files;

public sealed class UploadSession
{
    private UploadSession()
    {
    }

    public Guid Id { get; private set; }
    public Guid FileId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string Purpose { get; private set; } = string.Empty;
    public string MediaType { get; private set; } = string.Empty;
    public long ExpectedSizeBytes { get; private set; }
    public string ExpectedChecksumSha256 { get; private set; } = string.Empty;
    public int PartSizeBytes { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public UploadSessionStatus Status { get; private set; }
    public string? StorageUploadId { get; private set; }
    public string? FailureCode { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static UploadSession Create(
        Guid id,
        Guid fileId,
        Guid ownerUserId,
        string objectKey,
        string purpose,
        string mediaType,
        long expectedSizeBytes,
        string expectedChecksumSha256,
        int partSizeBytes,
        DateTimeOffset expiresAt)
    {
        if (id == Guid.Empty || fileId == Guid.Empty || ownerUserId == Guid.Empty || expectedSizeBytes < 1 || partSizeBytes < 1)
        {
            throw new ArgumentException("Upload session values are invalid.");
        }

        ValidateText(objectKey, nameof(objectKey), 512);
        ValidateText(purpose, nameof(purpose), 40);
        ValidateText(mediaType, nameof(mediaType), 120);
        if (!IsSha256(expectedChecksumSha256))
        {
            throw new ArgumentException("Expected checksum must be lowercase SHA-256.", nameof(expectedChecksumSha256));
        }

        return new UploadSession
        {
            Id = id,
            FileId = fileId,
            OwnerUserId = ownerUserId,
            ObjectKey = objectKey.Trim(),
            Purpose = purpose.Trim().ToUpperInvariant(),
            MediaType = mediaType.Trim().ToLowerInvariant(),
            ExpectedSizeBytes = expectedSizeBytes,
            ExpectedChecksumSha256 = expectedChecksumSha256.Trim(),
            PartSizeBytes = partSizeBytes,
            ExpiresAt = expiresAt.ToUniversalTime(),
            Status = UploadSessionStatus.Pending
        };
    }

    public void StartUploading(string storageUploadId, DateTimeOffset now)
    {
        if (Status is not (UploadSessionStatus.Pending or UploadSessionStatus.Uploading) || now.ToUniversalTime() >= ExpiresAt)
        {
            throw new InvalidOperationException("Upload session is not available for upload.");
        }

        ValidateText(storageUploadId, nameof(storageUploadId), 1024);
        StorageUploadId = storageUploadId.Trim();
        Status = UploadSessionStatus.Uploading;
    }

    public void StartVerification(string expectedVersion, DateTimeOffset now)
    {
        if (!RowVersion.SequenceEqual(Convert.FromBase64String(expectedVersion)) ||
            Status != UploadSessionStatus.Uploading || now.ToUniversalTime() >= ExpiresAt)
        {
            throw new InvalidOperationException("Upload session cannot be completed.");
        }

        Status = UploadSessionStatus.Verifying;
        FailureCode = null;
    }

    public void MarkVerified()
    {
        if (Status != UploadSessionStatus.Verifying)
        {
            throw new InvalidOperationException("Upload session is not verifying.");
        }

        Status = UploadSessionStatus.Verified;
        FailureCode = null;
    }

    public void MarkFailed(string failureCode)
    {
        if (Status != UploadSessionStatus.Verifying)
        {
            throw new InvalidOperationException("Upload session is not verifying.");
        }

        ValidateText(failureCode, nameof(failureCode), 80);
        Status = UploadSessionStatus.Failed;
        FailureCode = failureCode.Trim();
    }

    private static bool IsSha256(string value)
        => value is { Length: 64 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static void ValidateText(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
