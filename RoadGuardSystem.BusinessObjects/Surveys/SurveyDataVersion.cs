using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyDataVersion
{
    private SurveyDataVersion()
    {
    }

    public Guid Id { get; private set; }
    public Guid SurveyId { get; private set; }
    public int VersionNo { get; private set; }
    public SurveyDataVersionStatus Status { get; private set; }
    public SurveyDataIntegrityStatus IntegrityStatus { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public SurveyDataConfirmationActor? ConfirmedBy { get; private set; }
    public string SourceManifest { get; private set; } = string.Empty;

    public static SurveyDataVersion Create(
        Guid id,
        Guid surveyId,
        int versionNo,
        SurveyDataVersionStatus status,
        SurveyDataIntegrityStatus integrityStatus,
        DateTimeOffset? confirmedAt,
        SurveyDataConfirmationActor? confirmedBy,
        string sourceManifest)
    {
        if (id == Guid.Empty || surveyId == Guid.Empty || versionNo <= 0)
        {
            throw new ArgumentException("Dataset version, survey, and positive version number are required.");
        }

        if (!Enum.IsDefined(status) || status == SurveyDataVersionStatus.Unknown ||
            !Enum.IsDefined(integrityStatus) || integrityStatus == SurveyDataIntegrityStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Dataset status and integrity status must be specified.");
        }

        if (status == SurveyDataVersionStatus.ServerConfirmed)
        {
            if (integrityStatus != SurveyDataIntegrityStatus.Passed || confirmedAt is null || confirmedBy != SurveyDataConfirmationActor.Backend)
            {
                throw new ArgumentException("Server confirmation requires passed integrity, a timestamp, and the Backend actor.");
            }
        }
        else if (confirmedAt is not null || confirmedBy is not null)
        {
            throw new ArgumentException("Only server-confirmed datasets may carry confirmation metadata.");
        }

        ValidateManifest(sourceManifest);
        return new SurveyDataVersion
        {
            Id = id,
            SurveyId = surveyId,
            VersionNo = versionNo,
            Status = status,
            IntegrityStatus = integrityStatus,
            ConfirmedAt = confirmedAt?.ToUniversalTime(),
            ConfirmedBy = confirmedBy,
            SourceManifest = sourceManifest.Trim()
        };
    }

    private static void ValidateManifest(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Source manifest must be a JSON array.", nameof(value));
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Source manifest must be valid JSON.", nameof(value), exception);
        }
    }
}
