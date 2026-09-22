using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Inspections;

public sealed class FieldInspectionSession
{
    private FieldInspectionSession()
    {
    }

    public Guid Id { get; private set; }

    public FieldInspectionPurpose Purpose { get; private set; }

    public Guid? FieldInspectionTaskId { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid RoadSectionVersionId { get; private set; }

    public Guid? SurveyId { get; private set; }

    public string SessionCode { get; private set; } = string.Empty;

    public Guid? InspectorUserId { get; private set; }

    public string InspectorName { get; private set; } = string.Empty;

    public DateTimeOffset ConductedAt { get; private set; }

    public string? WeatherCondition { get; private set; }

    public string Method { get; private set; } = string.Empty;

    public FieldInspectionSessionStatus Status { get; private set; }

    public Guid? EvidenceFileId { get; private set; }

    public static FieldInspectionSession Create(
        Guid id,
        FieldInspectionPurpose purpose,
        Guid? fieldInspectionTaskId,
        Guid projectId,
        Guid roadSectionVersionId,
        Guid? surveyId,
        string sessionCode,
        Guid? inspectorUserId,
        string inspectorName,
        DateTimeOffset conductedAt,
        string? weatherCondition,
        string method,
        FieldInspectionSessionStatus status,
        Guid? evidenceFileId)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || roadSectionVersionId == Guid.Empty)
        {
            throw new ArgumentException("Session, project, and road version ids must not be empty.");
        }

        if (fieldInspectionTaskId == Guid.Empty || surveyId == Guid.Empty || inspectorUserId == Guid.Empty || evidenceFileId == Guid.Empty)
        {
            throw new ArgumentException("Optional session ids must be non-empty when supplied.");
        }

        if (!Enum.IsDefined(purpose) || purpose == FieldInspectionPurpose.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(purpose), "Inspection purpose must be specified.");
        }

        if (!Enum.IsDefined(status) || status == FieldInspectionSessionStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Session status must be specified.");
        }

        if (purpose == FieldInspectionPurpose.DefectVerification && (fieldInspectionTaskId is null || surveyId is null || inspectorUserId is null))
        {
            throw new ArgumentException("Defect verification requires a task, survey, and inspector.");
        }

        if (purpose == FieldInspectionPurpose.ResearchValidation && fieldInspectionTaskId is not null)
        {
            throw new ArgumentException("Research validation cannot reference a field inspection task.", nameof(fieldInspectionTaskId));
        }

        return new FieldInspectionSession
        {
            Id = id,
            Purpose = purpose,
            FieldInspectionTaskId = fieldInspectionTaskId,
            ProjectId = projectId,
            RoadSectionVersionId = roadSectionVersionId,
            SurveyId = surveyId,
            SessionCode = NormalizeRequired(sessionCode, nameof(sessionCode), 80),
            InspectorUserId = inspectorUserId,
            InspectorName = NormalizeRequired(inspectorName, nameof(inspectorName), 200),
            ConductedAt = conductedAt.ToUniversalTime(),
            WeatherCondition = NormalizeOptional(weatherCondition, nameof(weatherCondition), 100),
            Method = NormalizeRequired(method, nameof(method), 200),
            Status = status,
            EvidenceFileId = evidenceFileId
        };
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
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
        return string.IsNullOrWhiteSpace(value) ? null : NormalizeRequired(value, parameterName, maxLength);
    }
}
