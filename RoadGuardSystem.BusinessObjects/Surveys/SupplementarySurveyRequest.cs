using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SupplementarySurveyRequest
{
    private SupplementarySurveyRequest()
    {
    }

    public Guid Id { get; private set; }
    public Guid SurveyId { get; private set; }
    public Guid? SurveyRequestId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string RequestedScope { get; private set; } = string.Empty;
    public int RoundNo { get; private set; }
    public SupplementarySurveyRequestStatus Status { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? SourcePreservationNote { get; private set; }

    public static SupplementarySurveyRequest Create(
        Guid id,
        Guid surveyId,
        Guid? surveyRequestId,
        Guid requestedByUserId,
        string reason,
        string requestedScope,
        int roundNo,
        SupplementarySurveyRequestStatus status,
        Guid? approvedByUserId,
        DateTimeOffset? approvedAt,
        string? sourcePreservationNote)
    {
        if (id == Guid.Empty || surveyId == Guid.Empty || requestedByUserId == Guid.Empty || roundNo <= 0)
        {
            throw new ArgumentException("Supplementary request, survey, requester, and positive round number are required.");
        }

        if (!Enum.IsDefined(status) || status == SupplementarySurveyRequestStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Supplementary request status must be specified.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var normalizedReason = reason.Trim();
        if (normalizedReason.Length > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), "Supplementary request reason exceeds 1000 characters.");
        }

        if (approvedByUserId.HasValue != approvedAt.HasValue)
        {
            throw new ArgumentException("Approval actor and timestamp must be supplied together.");
        }

        ValidateJsonObject(requestedScope);
        return new SupplementarySurveyRequest
        {
            Id = id,
            SurveyId = surveyId,
            SurveyRequestId = surveyRequestId,
            RequestedByUserId = requestedByUserId,
            Reason = normalizedReason,
            RequestedScope = requestedScope.Trim(),
            RoundNo = roundNo,
            Status = status,
            ApprovedByUserId = approvedByUserId,
            ApprovedAt = approvedAt?.ToUniversalTime(),
            SourcePreservationNote = string.IsNullOrWhiteSpace(sourcePreservationNote) ? null : sourcePreservationNote.Trim()
        };
    }

    private static void ValidateJsonObject(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Requested scope must be a JSON object.", nameof(value));
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Requested scope must be valid JSON.", nameof(value), exception);
        }
    }
}
