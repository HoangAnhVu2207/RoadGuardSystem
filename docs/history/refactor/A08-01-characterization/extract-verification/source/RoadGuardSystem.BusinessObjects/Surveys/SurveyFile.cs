using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class SurveyFile
{
    private SurveyFile()
    {
    }

    public Guid Id { get; private set; }

    public Guid SurveyId { get; private set; }

    public Guid? FlightId { get; private set; }

    public Guid FileId { get; private set; }

    public SurveyFileType FileType { get; private set; }

    public DateTimeOffset? CaptureStartedAt { get; private set; }

    public DateTimeOffset? CaptureEndedAt { get; private set; }

    public SurveyFileSyncStatus SyncStatus { get; private set; }

    public string Checksum { get; private set; } = string.Empty;

    public static SurveyFile Create(
        Guid id,
        Guid surveyId,
        Guid? flightId,
        Guid fileId,
        SurveyFileType fileType,
        DateTimeOffset? captureStartedAt,
        DateTimeOffset? captureEndedAt,
        SurveyFileSyncStatus syncStatus,
        string checksum)
    {
        if (id == Guid.Empty || surveyId == Guid.Empty || fileId == Guid.Empty)
        {
            throw new ArgumentException("Survey file, survey, and stored file ids must not be empty.");
        }

        if (!Enum.IsDefined(fileType) || fileType == SurveyFileType.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(fileType), "Survey file type must be specified.");
        }

        if (!Enum.IsDefined(syncStatus) || syncStatus == SurveyFileSyncStatus.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(syncStatus), "Survey file sync status must be specified.");
        }

        if (captureStartedAt is not null && captureEndedAt is not null && captureEndedAt < captureStartedAt)
        {
            throw new ArgumentException("Capture end time cannot be before its start time.", nameof(captureEndedAt));
        }

        if (string.IsNullOrWhiteSpace(checksum) || checksum.Length != 64 || checksum.Any(character =>
                character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException("Checksum must be lowercase SHA-256 hexadecimal.", nameof(checksum));
        }

        return new SurveyFile
        {
            Id = id,
            SurveyId = surveyId,
            FlightId = flightId,
            FileId = fileId,
            FileType = fileType,
            CaptureStartedAt = captureStartedAt?.ToUniversalTime(),
            CaptureEndedAt = captureEndedAt?.ToUniversalTime(),
            SyncStatus = syncStatus,
            Checksum = checksum
        };
    }
}
