using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class QualityCheck
{
    private QualityCheck()
    {
    }

    public Guid Id { get; private set; }
    public QualityCheckScope Scope { get; private set; }
    public QualityCheckExecutionStage ExecutionStage { get; private set; }
    public Guid? SurveyFileId { get; private set; }
    public Guid? SurveyDataVersionId { get; private set; }
    public QualityCheckType CheckType { get; private set; }
    public QualityCheckStatus Status { get; private set; }
    public string? MeasuredValue { get; private set; }
    public string? Threshold { get; private set; }
    public string? Message { get; private set; }
    public DateTimeOffset CheckedAt { get; private set; }
    public QualityCheckActor CheckedBy { get; private set; }
    public Guid? InitiatedByUserId { get; private set; }

    public static QualityCheck Create(
        Guid id,
        QualityCheckScope scope,
        QualityCheckExecutionStage executionStage,
        Guid? surveyFileId,
        Guid? surveyDataVersionId,
        QualityCheckType checkType,
        QualityCheckStatus status,
        string? measuredValue,
        string? threshold,
        string? message,
        DateTimeOffset checkedAt,
        QualityCheckActor checkedBy,
        Guid? initiatedByUserId)
    {
        if (id == Guid.Empty || !Enum.IsDefined(scope) || scope == QualityCheckScope.Unknown ||
            !Enum.IsDefined(executionStage) || executionStage == QualityCheckExecutionStage.Unknown ||
            !Enum.IsDefined(checkType) || checkType == QualityCheckType.Unknown ||
            !Enum.IsDefined(status) || status == QualityCheckStatus.Unknown ||
            !Enum.IsDefined(checkedBy) || checkedBy == QualityCheckActor.Unknown)
        {
            throw new ArgumentException("Quality check identity, target, status, and actor must be specified.");
        }

        var isFileTarget = surveyFileId.HasValue && !surveyDataVersionId.HasValue;
        var isDatasetTarget = !surveyFileId.HasValue && surveyDataVersionId.HasValue;
        if ((scope == QualityCheckScope.SurveyFile && !isFileTarget) ||
            (scope == QualityCheckScope.SurveyDataset && !isDatasetTarget))
        {
            throw new ArgumentException("Quality check scope must have exactly its canonical target.");
        }

        if ((executionStage == QualityCheckExecutionStage.ClientPrecheck && checkedBy != QualityCheckActor.DroneApp) ||
            (executionStage == QualityCheckExecutionStage.ServerValidation &&
             (checkedBy != QualityCheckActor.Backend || initiatedByUserId is not null)))
        {
            throw new ArgumentException("Quality check stage and actor are incompatible.");
        }

        ValidateOptionalJson(measuredValue, nameof(measuredValue));
        ValidateOptionalJson(threshold, nameof(threshold));
        return new QualityCheck
        {
            Id = id,
            Scope = scope,
            ExecutionStage = executionStage,
            SurveyFileId = surveyFileId,
            SurveyDataVersionId = surveyDataVersionId,
            CheckType = checkType,
            Status = status,
            MeasuredValue = measuredValue?.Trim(),
            Threshold = threshold?.Trim(),
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            CheckedAt = checkedAt.ToUniversalTime(),
            CheckedBy = checkedBy,
            InitiatedByUserId = initiatedByUserId
        };
    }

    private static void ValidateOptionalJson(string? value, string parameterName)
    {
        if (value is null)
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Value must be valid JSON.", parameterName, exception);
        }
    }
}
