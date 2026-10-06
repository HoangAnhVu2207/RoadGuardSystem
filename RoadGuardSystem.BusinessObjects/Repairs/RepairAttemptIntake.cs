using RoadGuardSystem.BusinessObjects.Inspections;

namespace RoadGuardSystem.BusinessObjects.Repairs;

/// <summary>Consumes actual H3 intake facts, never manufactures a submission or resets its original review origin.</summary>
public sealed class RepairAttemptIntake
{
    public RepairAttempt OriginalAttempt { get; private set; } = null!;
    public FieldInspectionSubmission OriginalSubmission { get; private set; } = null!;
    public FieldInspectionSubmission FormalIntakeRoot { get; private set; } = null!;
    public IReadOnlyList<FieldInspectionSubmission> Submissions => _submissions.AsReadOnly();
    private readonly List<FieldInspectionSubmission> _submissions = [];
    public FieldInspectionSubmission EffectiveSubmission => _submissions[^1];
    public DateTimeOffset OriginalReviewOrigin => FormalIntakeRoot.ServerReceivedAt;
    public bool IsReady => EffectiveSubmission.Readiness == "READY";
    private RepairAttemptIntake() { }
    public static RepairAttemptIntake Create(RepairAttempt attempt, FieldInspectionSubmission source)
    {
        ArgumentNullException.ThrowIfNull(attempt); ArgumentNullException.ThrowIfNull(source);
        if (source.ParentId is not null || source.Revision != 1 || source.RootId != source.Id ||
            source.ProjectId != attempt.ProjectId || source.TaskId != attempt.TaskId || source.AssignmentId != attempt.AssignmentId ||
            source.OriginalActorId != attempt.CrewId || source.OriginId != attempt.OriginId || source.ContentHash != attempt.PayloadHash ||
            source.ServerReceivedAt != attempt.ServerReceivedAt)
            throw new InvalidOperationException("Repair intake requires its actual original H3 source and original actor.");
        EnsureClaim(attempt, source);
        var intake = new RepairAttemptIntake { OriginalAttempt = attempt, OriginalSubmission = source, FormalIntakeRoot = source };
        intake._submissions.Add(source); return intake;
    }
    public static RepairAttemptIntake Create(RepairAttempt attempt, IReadOnlyList<FieldInspectionSubmission> actualLineage)
    {
        ArgumentNullException.ThrowIfNull(attempt); ArgumentNullException.ThrowIfNull(actualLineage);
        if (actualLineage.Count == 0 || actualLineage.Any(source => source is null))
            throw new InvalidOperationException("Actual formal intake lineage is required.");
        var root = actualLineage[0]; var physical = actualLineage[^1];
        if (root.Revision != 1 || root.ParentId is not null || root.RootId != root.Id ||
            physical.ProjectId != attempt.ProjectId || physical.TaskId != attempt.TaskId ||
            physical.AssignmentId != attempt.AssignmentId || physical.OriginalActorId != attempt.CrewId ||
            physical.OriginId != attempt.OriginId || physical.ContentHash != attempt.PayloadHash || physical.ServerReceivedAt != attempt.ServerReceivedAt ||
            actualLineage.Select(source => source.Id).Distinct().Count() != actualLineage.Count ||
            actualLineage.Select(source => source.OriginId).Distinct().Count() != actualLineage.Count)
            throw new InvalidOperationException("Physical attempt must reference its genuine formal intake lineage and original facts.");
        for (var index = 0; index < actualLineage.Count; index++)
        {
            var source = actualLineage[index];
            if (source.ProjectId != physical.ProjectId || source.TaskId != physical.TaskId || source.RootId != root.Id ||
                source.StartOriginId != root.StartOriginId || source.Revision != index + 1 ||
                index > 0 && (source.ParentId != actualLineage[index - 1].Id || source.ServerReceivedAt < actualLineage[index - 1].ServerReceivedAt))
                throw new InvalidOperationException("Physical intake cannot skip, fork, backdate or replace the actual first start.");
        }
        EnsureClaim(attempt, physical);
        var intake = new RepairAttemptIntake { OriginalAttempt = attempt, OriginalSubmission = physical, FormalIntakeRoot = root };
        intake._submissions.AddRange(actualLineage); return intake;
    }
    public void Supplement(FieldInspectionSubmission source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var previous = EffectiveSubmission;
        if (source.ProjectId != OriginalSubmission.ProjectId || source.TaskId != OriginalSubmission.TaskId ||
            source.RootId != OriginalSubmission.RootId || source.ParentId != previous.Id || source.Revision != previous.Revision + 1 ||
            source.StartOriginId != OriginalSubmission.StartOriginId || source.ServerReceivedAt < previous.ServerReceivedAt ||
            _submissions.Any(existing => existing.Id == source.Id || existing.OriginId == source.OriginId))
            throw new InvalidOperationException("Supplement must continue the actual immutable intake head and first start.");
        EnsureClaim(OriginalAttempt, source);
        _submissions.Add(source);
    }
    private static void EnsureClaim(RepairAttempt attempt, FieldInspectionSubmission source)
    {
        using var facts = System.Text.Json.JsonDocument.Parse(source.PayloadJson);
        var payload = facts.RootElement;
        if (!payload.TryGetProperty("captureType", out var capture) || capture.GetString() != "REPAIR_CLAIM" ||
            !payload.TryGetProperty("repaired", out var performed) || performed.ValueKind is not
                (System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False) || performed.GetBoolean() != attempt.Performed)
            throw new InvalidOperationException("Evidence supplement cannot rewrite the original physical-work claim; subsequent work needs its own attempt.");
        if (!attempt.Performed && (!payload.TryGetProperty("unrepairedReason", out var reason) || reason.GetString() != attempt.UnperformedReason))
            throw new InvalidOperationException("Original unperformed reason must remain attached to its physical-work claim.");
    }
}
