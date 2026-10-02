namespace RoadGuardSystem.BusinessObjects.Reports;

public sealed class EvidenceCaptureMetadata
{
    private EvidenceCaptureMetadata()
    {
    }

    public DateTimeOffset? CapturedAt { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public decimal? AccuracyMeters { get; private set; }
    public EvidenceLocationSource LocationSource { get; private set; }

    public static EvidenceCaptureMetadata Create(
        DateTimeOffset? capturedAt,
        EvidenceLocationSource locationSource,
        decimal? latitude = null,
        decimal? longitude = null,
        decimal? accuracyMeters = null)
    {
        if (!Enum.IsDefined(locationSource))
        {
            throw new ArgumentOutOfRangeException(nameof(locationSource));
        }

        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ArgumentException("Latitude and longitude must be supplied together.");
        }

        if (locationSource == EvidenceLocationSource.Unknown && (latitude.HasValue || accuracyMeters.HasValue))
        {
            throw new ArgumentException("Unknown location source cannot assert location metadata.");
        }

        if (locationSource != EvidenceLocationSource.Unknown && !latitude.HasValue)
        {
            throw new ArgumentException("A known location source requires a location.");
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180 || accuracyMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Capture metadata is outside its valid range.");
        }

        return new EvidenceCaptureMetadata
        {
            CapturedAt = capturedAt?.ToUniversalTime(),
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = accuracyMeters,
            LocationSource = locationSource
        };
    }
}
