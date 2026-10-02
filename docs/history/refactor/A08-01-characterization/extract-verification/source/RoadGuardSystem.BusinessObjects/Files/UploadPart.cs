namespace RoadGuardSystem.BusinessObjects.Files;

public sealed class UploadPart
{
    private UploadPart()
    {
    }

    public Guid Id { get; private set; }
    public Guid UploadSessionId { get; private set; }
    public int PartNumber { get; private set; }
    public string? ETag { get; private set; }
    public DateTimeOffset? UrlIssuedAt { get; private set; }
    public DateTimeOffset? UrlExpiresAt { get; private set; }

    public static UploadPart Create(Guid id, Guid uploadSessionId, int partNumber)
    {
        if (id == Guid.Empty || uploadSessionId == Guid.Empty || partNumber < 1)
        {
            throw new ArgumentException("Upload part identifiers are invalid.");
        }

        return new UploadPart { Id = id, UploadSessionId = uploadSessionId, PartNumber = partNumber };
    }

    public void RecordUrl(DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        if (expiresAt <= issuedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        }

        UrlIssuedAt = issuedAt.ToUniversalTime();
        UrlExpiresAt = expiresAt.ToUniversalTime();
    }

    public void RecordCompletion(string eTag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eTag);
        if (eTag.Length > 512)
        {
            throw new ArgumentOutOfRangeException(nameof(eTag));
        }

        ETag = eTag.Trim();
    }
}
