namespace RoadGuardSystem.BusinessObjects.Files;

public sealed class FileScope
{
    private FileScope()
    {
    }

    public Guid Id { get; private set; }
    public Guid FileId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? TargetId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static FileScope CreatePrivate(Guid id, Guid fileId, Guid ownerUserId, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty || fileId == Guid.Empty || ownerUserId == Guid.Empty)
            throw new ArgumentException("Private file scope identifiers must not be empty.");
        return new FileScope { Id = id, FileId = fileId, OwnerUserId = ownerUserId,
            Purpose = "REPORT_PHOTO", ProjectId = null, TargetId = null, CreatedAt = createdAt.ToUniversalTime() };
    }

    public static FileScope Create(
        Guid id,
        Guid fileId,
        Guid projectId,
        Guid? targetId,
        Guid ownerUserId,
        string purpose,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty || fileId == Guid.Empty || projectId == Guid.Empty || ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("File scope identifiers must not be empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        if (purpose.Length > 40)
        {
            throw new ArgumentOutOfRangeException(nameof(purpose));
        }

        return new FileScope
        {
            Id = id,
            FileId = fileId,
            ProjectId = projectId,
            TargetId = targetId,
            OwnerUserId = ownerUserId,
            Purpose = purpose.Trim().ToUpperInvariant(),
            CreatedAt = createdAt.ToUniversalTime()
        };
    }
}
