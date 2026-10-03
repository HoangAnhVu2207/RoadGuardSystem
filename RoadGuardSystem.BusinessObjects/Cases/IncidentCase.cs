namespace RoadGuardSystem.BusinessObjects.Cases;

public sealed class IncidentCase
{
    private List<Guid> _activeReportIds = [];
    private List<CaseConclusion> _conclusions = [];
    private List<CasePublication> _publications = [];
    private List<CaseReportLinkHistory> _linkHistory = [];

    private IncidentCase()
    {
    }

    public Guid Id { get; private set; }
    public Guid? ProjectId { get; private set; }
    public IncidentCaseStatus Status { get; private set; }
    public CaseVerificationMethod? VerificationMethod { get; private set; }
    public string? TriageReason { get; private set; }
    public DateTimeOffset? TriagedAt { get; private set; }
    public Guid? LinkedTargetCaseId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<Guid> ActiveReportIds => _activeReportIds.AsReadOnly();
    public IReadOnlyList<CaseConclusion> Conclusions => _conclusions.AsReadOnly();
    public IReadOnlyList<CasePublication> Publications => _publications.AsReadOnly();
    public IReadOnlyList<CaseReportLinkHistory> LinkHistory => _linkHistory.AsReadOnly();

    public static IncidentCase CreateUnassigned(Guid id, Guid reportId, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty || reportId == Guid.Empty)
        {
            throw new ArgumentException("Case and report identifiers must not be empty.");
        }

        return new IncidentCase
        {
            Id = id,
            Status = IncidentCaseStatus.Unassigned,
            CreatedAt = createdAt.ToUniversalTime(),
            _activeReportIds = [reportId]
        };
    }

    public void Triage(Guid projectId, CaseVerificationMethod verificationMethod, string reason, DateTimeOffset triagedAt)
    {
        if (Status != IncidentCaseStatus.Unassigned)
        {
            throw new InvalidOperationException("Only an unassigned case can be triaged.");
        }

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project identifier must not be empty.", nameof(projectId));
        }

        if (verificationMethod == CaseVerificationMethod.Unknown || !Enum.IsDefined(verificationMethod))
        {
            throw new ArgumentOutOfRangeException(nameof(verificationMethod));
        }

        var normalizedReason = CaseConclusion.NormalizeReason(reason);
        var normalizedTriagedAt = triagedAt.ToUniversalTime();
        ProjectId = projectId;
        VerificationMethod = verificationMethod;
        TriageReason = normalizedReason;
        TriagedAt = normalizedTriagedAt;
        Status = IncidentCaseStatus.Open;
    }

    public void MarkAwaitingEvidence(string reason, DateTimeOffset changedAt)
    {
        if (Status != IncidentCaseStatus.Open)
        {
            throw new InvalidOperationException("Only an open case can await evidence.");
        }

        _ = CaseConclusion.NormalizeReason(reason);
        _ = changedAt.ToUniversalTime();
        Status = IncidentCaseStatus.AwaitingEvidence;
    }

    public void SelectVerificationMethod(CaseVerificationMethod method, string reason, DateTimeOffset changedAt)
    {
        if (ProjectId is null || Status is not (IncidentCaseStatus.Open or IncidentCaseStatus.AwaitingEvidence))
            throw new InvalidOperationException("Only an assigned active case can select its verification method.");
        if (method == CaseVerificationMethod.Unknown || !Enum.IsDefined(method)) throw new ArgumentOutOfRangeException(nameof(method));
        var normalizedReason = CaseConclusion.NormalizeReason(reason);
        VerificationMethod = method;
        TriageReason = normalizedReason;
        TriagedAt = changedAt.ToUniversalTime();
    }

    // Persistence supplies the full append-only history from both endpoints.
    public void MaterializeLinkHistory(IReadOnlyCollection<CaseReportLinkHistory> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        if (history.Any(h => h is null || h.FromCaseId != Id && h.ToCaseId != Id) || history.Select(h => h.Id).Distinct().Count() != history.Count)
            throw new ArgumentException("History must be unique and belong to this case.", nameof(history));
        _linkHistory = history.OrderBy(h => h.OccurredAt).ThenBy(h => h.Id).ToList();
    }

    public void RegisterSupplement(Guid reportId, DateTimeOffset receivedAt)
    {
        if (reportId == Guid.Empty || !_activeReportIds.Contains(reportId))
        {
            throw new InvalidOperationException("A supplement must belong to the case's active report.");
        }

        _ = receivedAt.ToUniversalTime();
        if (Status is IncidentCaseStatus.AwaitingEvidence or IncidentCaseStatus.Concluded)
        {
            Status = IncidentCaseStatus.Open;
        }
    }

    public void Conclude(CaseConclusion conclusion, CaseConclusionPrerequisites prerequisites)
    {
        ArgumentNullException.ThrowIfNull(conclusion);
        ArgumentNullException.ThrowIfNull(prerequisites);
        if (Status is not (IncidentCaseStatus.Open or IncidentCaseStatus.AwaitingEvidence))
        {
            throw new InvalidOperationException("Only an open case can record a conclusion.");
        }

        if (conclusion.Outcome == CaseConclusionOutcome.Confirmed &&
            (!prerequisites.ContainsVerifiedDefects(conclusion.DefectIds) || !prerequisites.ContainsVerifiedEvidence(conclusion.EvidenceIds)))
        {
            throw new InvalidOperationException("A confirmed conclusion requires server-verified defects and evidence.");
        }

        if (conclusion.Outcome == CaseConclusionOutcome.NoDefect &&
            (conclusion.EvidenceIds.Count == 0 || !prerequisites.ContainsVerifiedEvidence(conclusion.EvidenceIds)))
        {
            throw new InvalidOperationException("A no-defect conclusion requires server-verified evidence.");
        }

        if (conclusion.Outcome == CaseConclusionOutcome.NeedsEvidence && !prerequisites.ContainsVerifiedEvidence(conclusion.EvidenceIds))
        {
            throw new InvalidOperationException("Referenced evidence must be server-verified.");
        }

        var nextStatus = conclusion.Outcome == CaseConclusionOutcome.NeedsEvidence
            ? IncidentCaseStatus.AwaitingEvidence
            : IncidentCaseStatus.Concluded;
        _conclusions.Add(conclusion);
        Status = nextStatus;
    }

    public CasePublication Publish(
        Guid publicationId,
        IReadOnlyCollection<Guid> reportIds,
        IReadOnlyCollection<Guid> defectIds,
        IReadOnlyCollection<Guid> evidenceIds,
        string summary,
        DateTimeOffset publishedAt,
        CasePublicationPrerequisites prerequisites)
    {
        ArgumentNullException.ThrowIfNull(prerequisites);
        var publication = CasePublication.Create(publicationId, Id, reportIds, defectIds, evidenceIds, summary, publishedAt);
        if (publication.RecipientReportIds.Any(reportId => !_activeReportIds.Contains(reportId)))
        {
            throw new InvalidOperationException("A publication recipient must be an active report on this case.");
        }

        if (publication.DefectIds.Count == 0)
        {
            if (Status != IncidentCaseStatus.Concluded || _conclusions.Count == 0 || _conclusions[^1].Outcome != CaseConclusionOutcome.NoDefect)
            {
                throw new InvalidOperationException("A no-defect publication requires a concluded no-defect case.");
            }
        }
        if (!prerequisites.Authorizes(publication.RecipientReportIds, publication.DefectIds, publication.EvidenceIds))
        {
            throw new InvalidOperationException("Every selected defect and evidence reference must be server-authorized for every publication recipient.");
        }

        _publications.Add(publication);
        return publication;
    }

    public void LinkReportsFrom(
        IncidentCase sourceCase,
        IReadOnlyCollection<Guid> reportIds,
        Guid actorUserId,
        string reason,
        DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(sourceCase);
        if (ReferenceEquals(this, sourceCase) || Id == sourceCase.Id)
        {
            throw new InvalidOperationException("A case cannot link reports from itself.");
        }

        EnsureLinkable(sourceCase);
        var movedIds = CaseConclusion.ResolveIds(reportIds, nameof(reportIds));
        if (movedIds.Count == 0 || movedIds.Any(id => !sourceCase._activeReportIds.Contains(id)) || movedIds.Any(_activeReportIds.Contains))
        {
            throw new InvalidOperationException("Only active source reports can be linked once.");
        }

        var history = CaseReportLinkHistory.Create(Guid.NewGuid(), sourceCase.Id, Id, movedIds, actorUserId, reason, occurredAt);
        sourceCase._activeReportIds.RemoveAll(movedIds.Contains);
        _activeReportIds.AddRange(movedIds);
        sourceCase._linkHistory.Add(history);
        _linkHistory.Add(history);
        if (sourceCase._activeReportIds.Count == 0)
        {
            sourceCase.Status = IncidentCaseStatus.Linked;
            sourceCase.LinkedTargetCaseId = Id;
        }
    }

    public IncidentCase SplitReports(
        Guid newCaseId,
        IReadOnlyCollection<Guid> reportIds,
        Guid actorUserId,
        string reason,
        DateTimeOffset occurredAt)
    {
        if (Status is IncidentCaseStatus.Concluded or IncidentCaseStatus.Linked || ProjectId is null)
        {
            throw new InvalidOperationException("Only a non-concluded assigned case can be split.");
        }

        if (newCaseId == Id)
        {
            throw new InvalidOperationException("A split case must have a different identifier from its source case.");
        }

        var movedIds = CaseConclusion.ResolveIds(reportIds, nameof(reportIds));
        if (movedIds.Count == 0 || movedIds.Count == _activeReportIds.Count || movedIds.Any(id => !_activeReportIds.Contains(id)))
        {
            throw new InvalidOperationException("A split must move a proper non-empty subset of active reports.");
        }

        var normalizedReason = CaseConclusion.NormalizeReason(reason);
        if (newCaseId == Guid.Empty || actorUserId == Guid.Empty)
        {
            throw new ArgumentException("New case and actor identifiers must not be empty.");
        }

        var splitCase = new IncidentCase
        {
            Id = newCaseId,
            ProjectId = ProjectId,
            Status = IncidentCaseStatus.Open,
            VerificationMethod = VerificationMethod,
            CreatedAt = occurredAt.ToUniversalTime(),
            _activeReportIds = movedIds
        };
        var history = CaseReportLinkHistory.Create(Guid.NewGuid(), Id, splitCase.Id, movedIds, actorUserId, normalizedReason, occurredAt);
        _activeReportIds.RemoveAll(movedIds.Contains);
        _linkHistory.Add(history);
        splitCase._linkHistory.Add(history);
        return splitCase;
    }

    private void EnsureLinkable(IncidentCase sourceCase)
    {
        if (Status is IncidentCaseStatus.Concluded or IncidentCaseStatus.Linked || sourceCase.Status is IncidentCaseStatus.Concluded or IncidentCaseStatus.Linked)
        {
            throw new InvalidOperationException("Concluded or linked cases cannot be linked.");
        }

        if (ProjectId is null || sourceCase.ProjectId is null || ProjectId != sourceCase.ProjectId)
        {
            throw new InvalidOperationException("Cases from different projects cannot be linked.");
        }
    }
}
