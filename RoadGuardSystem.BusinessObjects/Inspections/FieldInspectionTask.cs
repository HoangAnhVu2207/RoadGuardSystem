using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldInspectionTask
{
    private FieldInspectionTask()
    {
    }

    public Guid Id { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public string TaskCode { get; private set; } = string.Empty;

    public Guid ProjectId { get; private set; }

    public Guid DefectId { get; private set; }

    // Only the separate H4 producer sets this immutable native source pin. Existing task factories remain measure-only.
    public Guid? RepairItemId { get; private set; }

    public Guid? SurveyId { get; private set; }

    public int LifecycleVersion { get; private set; } = 1;
    public string TaskMode { get; private set; } = "MEASURE_ONLY";
    public string SourceKind { get; private set; } = "SURVEY";
    public Guid? SegmentSetId { get; private set; }
    public Guid? LayoutRevisionId { get; private set; }
    public string? SlabId { get; private set; }
    public Guid? MapPublicationId { get; private set; }
    public Guid? CrsProfileRevisionId { get; private set; }
    public FieldInspectionPurpose Purpose { get; private set; } = FieldInspectionPurpose.DefectVerification;

    public Guid RoadSectionVersionId { get; private set; }

    public byte RequiredMeasurementType { get; private set; }

    public string MeasurementScope { get; private set; } = string.Empty;

    public string? Instructions { get; private set; }

    public string? MissingInformation { get; private set; }

    public DateTimeOffset DueAt { get; private set; }

    public FieldInspectionTaskStatus Status { get; private set; }

    public Guid AssignedByUserId { get; private set; }

    public FieldInspectionReviewDecision? ReviewDecision { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public string? ReviewReason { get; private set; }

    public static FieldInspectionTask Create(
        Guid id,
        string taskCode,
        Guid projectId,
        Guid defectId,
        Guid surveyId,
        Guid roadSectionVersionId,
        byte requiredMeasurementType,
        string measurementScope,
        string? instructions,
        string? missingInformation,
        DateTimeOffset dueAt,
        FieldInspectionTaskStatus status,
        Guid assignedByUserId,
        FieldInspectionReviewDecision? reviewDecision,
        Guid? reviewedByUserId,
        DateTimeOffset? reviewedAt,
        string? reviewReason)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || defectId == Guid.Empty ||
            surveyId == Guid.Empty || roadSectionVersionId == Guid.Empty || assignedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Task, project, defect, survey, road version, and assigner ids must not be empty.");
        }

        if (requiredMeasurementType == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredMeasurementType), "Measurement type must be specified.");
        }

        if (!Enum.IsDefined(status) || status == FieldInspectionTaskStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Inspection task status must be specified.");
        }

        if (reviewDecision is not null &&
            (!Enum.IsDefined(reviewDecision.Value) || reviewDecision == FieldInspectionReviewDecision.Unknown))
        {
            throw new ArgumentOutOfRangeException(nameof(reviewDecision), "Review decision must be specified when supplied.");
        }

        if ((reviewedByUserId is null) != (reviewedAt is null) ||
            (reviewDecision is null) != (reviewedByUserId is null))
        {
            throw new ArgumentException("Review decision, reviewer, and review timestamp must be supplied together.");
        }

        if (reviewDecision is not null && status != FieldInspectionTaskStatus.Completed)
        {
            throw new ArgumentException("A review decision requires a completed inspection task.", nameof(reviewDecision));
        }

        if (reviewedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Reviewer id must be non-empty when supplied.", nameof(reviewedByUserId));
        }

        return new FieldInspectionTask
        {
            Id = id,
            TaskCode = Normalize(taskCode, nameof(taskCode), 80),
            ProjectId = projectId,
            DefectId = defectId,
            SurveyId = surveyId,
            RoadSectionVersionId = roadSectionVersionId,
            RequiredMeasurementType = requiredMeasurementType,
            MeasurementScope = ValidateScope(measurementScope),
            Instructions = NormalizeOptional(instructions, nameof(instructions), 1_000),
            MissingInformation = NormalizeOptional(missingInformation, nameof(missingInformation), 1_000),
            DueAt = dueAt.ToUniversalTime(),
            Status = status,
            AssignedByUserId = assignedByUserId,
            ReviewDecision = reviewDecision,
            ReviewedByUserId = reviewedByUserId,
            ReviewedAt = reviewedAt?.ToUniversalTime(),
            ReviewReason = NormalizeOptional(reviewReason, nameof(reviewReason), 1_000)
        };
    }

    public static FieldInspectionTask CreateOperational(Guid id, string code, Guid projectId, Guid defectId,
        Guid? surveyId, string sourceKind, Guid routeVersionId, Guid? segmentSetId, Guid? layoutRevisionId,
        string? slabId, FieldInspectionPurpose purpose, byte requiredType, string scope, string? instructions,
        DateTimeOffset dueAt, Guid assigner, Guid? mapPublication = null, Guid? profile = null)
    {
        if (surveyId == Guid.Empty || segmentSetId == Guid.Empty || layoutRevisionId == Guid.Empty || mapPublication == Guid.Empty || profile == Guid.Empty ||
            sourceKind is not ("SURVEY" or "REPORTER") || (sourceKind == "SURVEY") != surveyId.HasValue ||
            purpose is not (FieldInspectionPurpose.PreMeasurement or FieldInspectionPurpose.PostRepair or FieldInspectionPurpose.Verification))
            throw new ArgumentException("Operational source and purpose must be explicit.");
        if (id == Guid.Empty || projectId == Guid.Empty || defectId == Guid.Empty || routeVersionId == Guid.Empty || assigner == Guid.Empty || requiredType == 0)
            throw new ArgumentException("Task, project, Defect, route, assigner and measurement type are required.");
        var task = new FieldInspectionTask { Id = id, TaskCode = Normalize(code, nameof(code), 80),
            ProjectId = projectId, DefectId = defectId, RoadSectionVersionId = routeVersionId,
            RequiredMeasurementType = requiredType, MeasurementScope = ValidateScope(scope),
            Instructions = NormalizeOptional(instructions, nameof(instructions), 1000), DueAt = dueAt.ToUniversalTime(),
            Status = FieldInspectionTaskStatus.NewAssigned, AssignedByUserId = assigner };
        task.SurveyId = surveyId; task.SourceKind = sourceKind; task.LifecycleVersion = 2;
        task.SegmentSetId = segmentSetId; task.LayoutRevisionId = layoutRevisionId; task.SlabId = NormalizeOptional(slabId, nameof(slabId), 160);
        task.Purpose = purpose; task.MapPublicationId = mapPublication; task.CrsProfileRevisionId = profile;
        return task;
    }

    public void Transition(FieldInspectionTaskStatus next)
    {
        var allowed = (Status, next) switch
        {
            (FieldInspectionTaskStatus.NewAssigned, FieldInspectionTaskStatus.Accepted or FieldInspectionTaskStatus.Rejected) => true,
            (FieldInspectionTaskStatus.Accepted, FieldInspectionTaskStatus.InProgress) => true,
            (FieldInspectionTaskStatus.InProgress or FieldInspectionTaskStatus.SupplementRequired, FieldInspectionTaskStatus.Submitted) => true,
            (FieldInspectionTaskStatus.Submitted, FieldInspectionTaskStatus.SupplementRequired or FieldInspectionTaskStatus.Completed) => true,
            (_, FieldInspectionTaskStatus.Cancelled) => Status is not (FieldInspectionTaskStatus.Completed or FieldInspectionTaskStatus.Cancelled),
            _ => false
        };
        if (LifecycleVersion != 2 || !allowed) throw new InvalidOperationException("Invalid FIELD task transition.");
        Status = next;
    }

    public static FieldInspectionTask CreateRepair(Guid id, string code,
        RoadGuardSystem.BusinessObjects.Repairs.RepairItem item, Guid? surveyId, string sourceKind,
        Guid routeVersionId, Guid? segmentSetId, Guid? layoutRevisionId, string? slabId,
        FieldInspectionPurpose purpose, byte requiredType, string scope, string? instructions,
        DateTimeOffset dueAt, Guid assigner, Guid? mapPublication = null, Guid? profile = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.State != RoadGuardSystem.BusinessObjects.Repairs.RepairItemState.Assigned ||
            item.AssignedBy != assigner || item.CrewId is null || item.AssignedAt is null ||
            string.IsNullOrWhiteSpace(item.ProposalPlanHash) || string.IsNullOrWhiteSpace(item.ChecklistVersion) ||
            (item.Mode == RoadGuardSystem.BusinessObjects.Repairs.RepairMode.Normal &&
                item.ApprovedPlanHash != item.ProposalPlanHash))
            throw new InvalidOperationException("A repair task requires the actual assigned, planned item and its assignment actor.");
        var task = CreateOperational(id, code, item.ProjectId, item.DefectId, surveyId, sourceKind,
            routeVersionId, segmentSetId, layoutRevisionId, slabId, purpose, requiredType, scope,
            instructions, dueAt, assigner, mapPublication, profile);
        task.RepairItemId = item.Id;
        task.TaskMode = item.Mode switch
        {
            RoadGuardSystem.BusinessObjects.Repairs.RepairMode.Normal => "NORMAL",
            RoadGuardSystem.BusinessObjects.Repairs.RepairMode.FastTrack => "CONDITIONAL_FT",
            _ => throw new InvalidOperationException("Repair mode must be explicit.")
        };
        return task;
    }

    public void Reassign()
    {
        if (LifecycleVersion != 2 || Status is FieldInspectionTaskStatus.Completed or FieldInspectionTaskStatus.Cancelled)
            throw new InvalidOperationException("Historical terminal tasks cannot be reassigned.");
        Status = FieldInspectionTaskStatus.NewAssigned;
    }

    private static string ValidateScope(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Measurement scope must be a JSON object.", nameof(value));
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Measurement scope must be valid JSON.", nameof(value), exception);
        }

        return value.Trim();
    }

    private static string Normalize(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds maximum length {maxLength}.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Normalize(value, parameterName, maxLength);
    }
}
