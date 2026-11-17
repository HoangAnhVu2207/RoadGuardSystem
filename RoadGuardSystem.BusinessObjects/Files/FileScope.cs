namespace RoadGuardSystem.BusinessObjects.Files;

public sealed class FileScope
{
    public const string ProjectHandoverStaging = "PROJECT_HANDOVER_STAGING";

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
        return new FileScope
        {
            Id = id,
            FileId = fileId,
            OwnerUserId = ownerUserId,
            Purpose = "REPORT_PHOTO",
            ProjectId = null,
            TargetId = null,
            CreatedAt = createdAt.ToUniversalTime()
        };
    }

    public static FileScope CreateHandoverStaging(Guid id, Guid fileId, Guid owner, DateTimeOffset createdAt)
    {
        var scope = CreatePrivate(id, fileId, owner, createdAt);
        scope.Purpose = ProjectHandoverStaging;
        return scope;
    }

    public void AttachStagedHandover(Guid projectId)
    {
        if (ProjectId is not null || TargetId is not null || Purpose != ProjectHandoverStaging || projectId == Guid.Empty)
            throw new InvalidOperationException("File scope is not an unused project handover staging scope.");
        ProjectId = projectId;
        TargetId = projectId;
        Purpose = "DOCUMENT";
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
