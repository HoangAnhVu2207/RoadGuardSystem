using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyPlanScope
{
    private SurveyPlanScope()
    {
    }

    public Guid Id { get; private set; }

    public Guid SurveyPlanId { get; private set; }

    public Guid RouteSectionVersionId { get; private set; }

    public Guid SegmentSetId { get; private set; }

    public string SegmentIdsJson { get; private set; } = "[]";

    public string TargetBand { get; private set; } = string.Empty;

    public static SurveyPlanScope Create(
        Guid id,
        Guid surveyPlanId,
        Guid routeSectionVersionId,
        Guid segmentSetId,
        string segmentIdsJson,
        string targetBand)
    {
        Validate(id, surveyPlanId, routeSectionVersionId, segmentSetId, segmentIdsJson, targetBand);
        return new SurveyPlanScope
        {
            Id = id,
            SurveyPlanId = surveyPlanId,
            RouteSectionVersionId = routeSectionVersionId,
            SegmentSetId = segmentSetId,
            SegmentIdsJson = segmentIdsJson.Trim(),
            TargetBand = targetBand.Trim().ToUpperInvariant()
        };
    }

    private static void Validate(Guid id, Guid surveyPlanId, Guid routeSectionVersionId, Guid segmentSetId, string segmentIdsJson, string targetBand)
    {
        if (id == Guid.Empty || surveyPlanId == Guid.Empty || routeSectionVersionId == Guid.Empty || segmentSetId == Guid.Empty)
        {
            throw new ArgumentException("Survey scope identifiers must not be empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(segmentIdsJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetBand);
        using var document = JsonDocument.Parse(segmentIdsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
        {
            throw new ArgumentException("Survey scope segment ids must be a non-empty JSON array.", nameof(segmentIdsJson));
        }

        if (targetBand.Trim().ToUpperInvariant() is not ("SURFACE" or "LEFT_EDGE" or "RIGHT_EDGE"))
        {
            throw new ArgumentException("Survey scope target band is invalid.", nameof(targetBand));
        }
    }
}
