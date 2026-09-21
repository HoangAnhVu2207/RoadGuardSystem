using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyPlan
{
    private SurveyPlan()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid RoadSectionId { get; private set; }

    public DateTimeOffset PlannedStartAt { get; private set; }

    public DateTimeOffset PlannedEndAt { get; private set; }

    public SurveyType SurveyType { get; private set; }

    public SurveyPlanStatus Status { get; private set; }

    public string OutputRequirements { get; private set; } = "{}";

    public static SurveyPlan Create(
        Guid id,
        Guid projectId,
        Guid roadSectionId,
        DateTimeOffset plannedStartAt,
        DateTimeOffset plannedEndAt,
        SurveyType surveyType,
        SurveyPlanStatus status,
        string outputRequirements = "{}")
    {
        if (id == Guid.Empty || projectId == Guid.Empty || roadSectionId == Guid.Empty)
        {
            throw new ArgumentException("Survey plan, project, and road section ids must not be empty.");
        }

        if (plannedEndAt < plannedStartAt)
        {
            throw new ArgumentException("Survey plan end time must not be before its start time.", nameof(plannedEndAt));
        }

        if (surveyType == SurveyType.Unknown)
        {
            throw new ArgumentException("Survey plan type must be specified.", nameof(surveyType));
        }

        if (status == SurveyPlanStatus.Unknown)
        {
            throw new ArgumentException("Survey plan status must be specified.", nameof(status));
        }

        ValidateJson(outputRequirements, nameof(outputRequirements));

        return new SurveyPlan
        {
            Id = id,
            ProjectId = projectId,
            RoadSectionId = roadSectionId,
            PlannedStartAt = plannedStartAt.ToUniversalTime(),
            PlannedEndAt = plannedEndAt.ToUniversalTime(),
            SurveyType = surveyType,
            Status = status,
            OutputRequirements = outputRequirements.Trim()
        };
    }

    public void Postpone(DateTimeOffset? newPlannedStartAt)
    {
        if (Status is SurveyPlanStatus.Completed or SurveyPlanStatus.Cancelled)
        {
            throw new InvalidOperationException("Completed or cancelled survey plans cannot be postponed.");
        }

        if (newPlannedStartAt is { } newStart)
        {
            newStart = newStart.ToUniversalTime();
            if (newStart > PlannedEndAt)
            {
                throw new ArgumentException("New planned start must not be after the planned end.", nameof(newPlannedStartAt));
            }

            PlannedStartAt = newStart;
        }

        Status = SurveyPlanStatus.Postponed;
    }

    private static void ValidateJson(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                throw new ArgumentException("Output requirements must be a JSON object or array.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Output requirements must be valid JSON.", parameterName, exception);
        }
    }
}
