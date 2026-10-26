namespace RoadGuardSystem.BusinessObjects.Cases;

public static class PublicPublicationPolicy
{
    public static string? Category(string code, bool active)
        => active && code is "CRACK" or "POTHOLE" ? "ROAD_SURFACE_DAMAGE" : null;
}

// Independent sanitized snapshot. All source references stay private in persistence.
public sealed class PublicPublication
{
    private PublicPublication() { }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DefectId { get; private set; }
    public Guid CaseId { get; private set; }
    public Guid ConclusionId { get; private set; }
    public Guid ApprovedBy { get; private set; }
    public string DamageCategory { get; private set; } = "";
    public DateTimeOffset PublishedAt { get; private set; }
    public DateTimeOffset? WithdrawnAt { get; private set; }
    public Guid? WithdrawnBy { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public static PublicPublication Create(Guid id, Guid project, Guid defect, Guid incident, Guid conclusion, Guid actor,
        string code, bool active, DateTimeOffset now)
    {
        var category = PublicPublicationPolicy.Category(code, active);
        if (category is null || new[] { id, project, defect, incident, conclusion, actor }.Contains(Guid.Empty))
            throw new ArgumentException("Publication prerequisites are required.");
        return new()
        {
            Id = id,
            ProjectId = project,
            DefectId = defect,
            CaseId = incident,
            ConclusionId = conclusion,
            ApprovedBy = actor,
            DamageCategory = category,
            PublishedAt = now.ToUniversalTime()
        };
    }
    public bool Withdraw(Guid actor, DateTimeOffset now)
    {
        if (actor == Guid.Empty) throw new ArgumentException("Actor required.");
        if (WithdrawnAt is not null) return false;
        WithdrawnAt = now.ToUniversalTime(); WithdrawnBy = actor; return true;
    }
}
