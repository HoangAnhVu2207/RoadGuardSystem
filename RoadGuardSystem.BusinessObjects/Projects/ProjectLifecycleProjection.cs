namespace RoadGuardSystem.BusinessObjects.Projects;

/// <summary>Read facts only. An accepted notification, membership or data-handover grant cannot manufacture these transfer facts.</summary>
public sealed record ProjectObligationTransferFact(Guid GrantId, Guid ReceiverId, bool AuthorityConfirmed,
    bool ReceiverConfirmed, bool ExactObligationScope);
public sealed record ProjectLifecycleObligationFact(Guid Id, bool Mandatory, bool Resolved, ProjectObligationTransferFact? Transfer);

public sealed record ProjectLifecycleProjection(bool ConstructionCompleted, bool OperationallyClosed, bool WarrantyExists,
    bool CanOperationallyClose, bool ClosureBasisInvalidated, IReadOnlyList<Guid> OutstandingMandatoryObligationIds)
{
    public bool AcceptsNewReports { get; } = true;

    public static ProjectLifecycleProjection Evaluate(bool constructionCompleted, bool recordedOperationalClosure,
        bool warrantyExists, IReadOnlyList<ProjectLifecycleObligationFact> obligations)
    {
        ArgumentNullException.ThrowIfNull(obligations);
        if (obligations.Any(fact => fact is null || fact.Id == Guid.Empty) ||
            obligations.Select(fact => fact.Id).Distinct().Count() != obligations.Count)
            throw new ArgumentException("Lifecycle projection requires distinct actual obligation identities.", nameof(obligations));
        foreach (var fact in obligations)
            if (fact.Transfer is { } transfer && (transfer.GrantId == Guid.Empty || transfer.ReceiverId == Guid.Empty))
                throw new ArgumentException("Transfer facts require actual grant and receiver identities.", nameof(obligations));
        var outstanding = obligations.Where(fact => fact.Mandatory && !fact.Resolved &&
            fact.Transfer is not { AuthorityConfirmed: true, ReceiverConfirmed: true, ExactObligationScope: true })
            .Select(fact => fact.Id).Order().ToArray();
        var permitted = outstanding.Length == 0;
        return new(constructionCompleted, recordedOperationalClosure && permitted, warrantyExists, permitted,
            recordedOperationalClosure && !permitted, Array.AsReadOnly(outstanding));
    }
}
