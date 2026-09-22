using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldInspectionTask
{
    private FieldInspectionTask()
    {
    }

    public Guid Id { get; private set; }

    public string TaskCode { get; private set; } = string.Empty;

    public Guid ProjectId { get; private set; }

    public Guid DefectId { get; private set; }

    public Guid SurveyId { get; private set; }

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
