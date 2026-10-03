using RoadGuardSystem.aBusinessObjects.Commons;
using RoadGuardSystem.BusinessObjects.Candidates;
using RoadGuardSystem.BusinessObjects.Labels;
using RoadGuardSystem.Repositories.Identity;
using RoadGuardSystem.Repositories.Projects;
using RoadGuardSystem.Services.Integration;

namespace RoadGuardSystem.Services.Labels;

// Unbound domain-service policy. Facts must be read/locked by the future SQL
// repository, never deserialized from an HTTP request as authority.
public static class TrainingLabelPolicy
{
    public static TrainingLabel Create(Guid id, UserSecurityState actor, EffectiveProjectMembership? membership,
        ResolvedCandidateSourceFacts facts, Guid file, decimal x, decimal y, decimal width, decimal height, string type, string reason, DateTimeOffset now)
    {
        Authorize(actor, membership, facts.DomainFacts.ProjectId, now);
        var version = ResolveSourceFileVersion(facts, file);
        return TrainingLabel.Create(id, facts.DomainFacts.ProjectId, facts.DomainFacts.Source.Id, "REPORT", facts.DomainFacts.Source.SourceVersion,
            file, version, x, y, width, height, type, reason);
    }
    public static void Revise(TrainingLabel head, UserSecurityState actor, EffectiveProjectMembership? membership,
        ResolvedCandidateSourceFacts facts, int expectedRevision, Guid file, decimal x, decimal y, decimal width, decimal height, string type, string reason, DateTimeOffset now)
    {
        Authorize(actor, membership, head.ProjectId, now);
        if (facts.DomainFacts.ProjectId != head.ProjectId || facts.DomainFacts.Source.Id != head.SourceId || head.SourceKind != "REPORT")
            throw new InvalidOperationException("A revision must retain its authoritative source/project identity.");
        var version = ResolveSourceFileVersion(facts, file);
        head.AppendRevision(expectedRevision, facts.DomainFacts.Source.SourceVersion, file, version, x, y, width, height, type, reason);
    }
    public static void Authorize(UserSecurityState actor, EffectiveProjectMembership? membership, Guid project, DateTimeOffset now)
    {
        var date = DateOnly.FromDateTime(now.UtcDateTime);
        if (actor.Status != UserStatus.Active || actor.RoleCode != UserRoleCode.ProjectManager || actor.MustChangePassword ||
            membership is null || membership.UserId != actor.Id || membership.ProjectId != project || membership.RoleCode != UserRoleCode.ProjectManager ||
            membership.Status != ProjectMemberStatus.Active || membership.ValidFrom > date || membership.ValidTo < date)
            throw new UnauthorizedAccessException("A current project PM is required.");
    }

    public static string ResolveSourceFileVersion(ResolvedCandidateSourceFacts facts, Guid requestedFile)
    {
        if (facts.DomainFacts.Source.Kind != CandidateSourceKind.Report) throw new InvalidOperationException("AI/FIELD label provenance is not ready.");
        var evidence = facts.Evidence.OrderBy(e => e.Reference.EvidenceId).FirstOrDefault(e => e.Reference.FileId == requestedFile);
        if (evidence is null || evidence.MediaType is not ("image/jpeg" or "image/png") || evidence.Purpose != "REPORT_PHOTO" ||
            evidence.ProjectId is not null || !facts.EvidenceIds.Contains(evidence.Reference.EvidenceId))
            throw new InvalidOperationException("A verified image related to the authoritative source is required.");
        return evidence.Reference.FileVersion;
    }
    public static void Review(TrainingLabel head, UserSecurityState actor, EffectiveProjectMembership? membership,
        int expectedRevision, TrainingLabelReviewStatus decision, string reason, DateTimeOffset now)
    {
        Authorize(actor, membership, head.ProjectId, now);
        head.Review(expectedRevision, actor.Id, decision, reason, now);
    }
}
