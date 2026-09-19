namespace RoadGuardSystem.BusinessObjects.Files;

public sealed class StoredFile
{
    private StoredFile()
    {
    }

    public Guid Id { get; private set; }

    public string StorageUri { get; private set; } = string.Empty;

    public string OriginalName { get; private set; } = string.Empty;

    public string MimeType { get; private set; } = string.Empty;

    public int SizeBytes { get; private set; }

    public string Checksum { get; private set; } = string.Empty;

    public Guid? UploadedByUserId { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public DateOnly? RetentionUntil { get; private set; }

    public static StoredFile Create(
        Guid id,
        string storageUri,
        string originalName,
        string mimeType,
        int sizeBytes,
        string checksum,
        Guid? uploadedByUserId,
        DateTimeOffset uploadedAt,
        DateOnly? retentionUntil)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("File id must not be empty.", nameof(id));
        }

        ValidateRequired(storageUri, nameof(storageUri), 2048);
        ValidateRequired(originalName, nameof(originalName), 255);
        ValidateRequired(mimeType, nameof(mimeType), 120);
        if (Path.IsPathRooted(originalName) ||
            originalName is "." or ".." ||
            originalName.Contains('/') ||
            originalName.Contains('\\') ||
            originalName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Original name must not contain a path.", nameof(originalName));
        }

        if (!IsMimeType(mimeType))
        {
            throw new ArgumentException("MIME type is malformed.", nameof(mimeType));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "File content must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(checksum) || checksum.Length != 64 || checksum.Any(character =>
                character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "Checksum must be lowercase SHA-256 hexadecimal.",
                nameof(checksum));
        }

        return new StoredFile
        {
            Id = id,
            StorageUri = storageUri.Trim(),
            OriginalName = originalName.Trim(),
            MimeType = mimeType.Trim().ToLowerInvariant(),
            SizeBytes = sizeBytes,
            Checksum = checksum,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = uploadedAt.ToUniversalTime(),
            RetentionUntil = retentionUntil
        };
    }

    private static void ValidateRequired(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }
    }

    private static bool IsMimeType(string value)
    {
        var separator = value.IndexOf('/');
        return separator > 0 &&
               separator == value.LastIndexOf('/') &&
               separator < value.Length - 1 &&
               value.All(character => char.IsAsciiLetterOrDigit(character) || character is '/' or '.' or '+' or '-');
    }
}
