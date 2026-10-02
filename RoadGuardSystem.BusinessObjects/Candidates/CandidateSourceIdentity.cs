namespace RoadGuardSystem.BusinessObjects.Candidates;

public sealed record CandidateSourceIdentity
{
    private CandidateSourceIdentity(CandidateSourceKind kind, Guid id, string sourceVersion)
    {
        Kind = kind;
        Id = id;
        SourceVersion = sourceVersion;
    }

    public CandidateSourceKind Kind { get; }
    public Guid Id { get; }
    public string SourceVersion { get; }

    public static CandidateSourceIdentity Create(CandidateSourceKind kind, Guid id, string sourceVersion)
    {
        if (kind == CandidateSourceKind.Unknown || !Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Source identifier must not be empty.", nameof(id));
        }

        return new CandidateSourceIdentity(kind, id, NormalizeVersion(sourceVersion, nameof(sourceVersion)));
    }

    internal static string NormalizeVersion(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > 200)
        {
            throw new ArgumentException("Version exceeds maximum length 200.", parameterName);
        }

        return normalized;
    }
}
