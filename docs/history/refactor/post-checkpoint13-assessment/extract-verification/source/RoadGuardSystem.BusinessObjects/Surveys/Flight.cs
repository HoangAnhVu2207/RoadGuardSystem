namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed class Flight
{
    private Flight()
    {
    }

    public Guid Id { get; private set; }

    public Guid SurveyId { get; private set; }

    public Guid? DroneDeviceId { get; private set; }

    public Guid OperatorUserId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public string FlightNo { get; private set; } = string.Empty;

    public static Flight Create(
        Guid id,
        Guid surveyId,
        Guid? droneDeviceId,
        Guid operatorUserId,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        string flightNo)
    {
        if (id == Guid.Empty || surveyId == Guid.Empty || operatorUserId == Guid.Empty)
        {
            throw new ArgumentException("Flight, survey, and operator ids must not be empty.");
        }

        if (endedAt is not null && endedAt < startedAt)
        {
            throw new ArgumentException("Flight end time cannot be before its start time.", nameof(endedAt));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(flightNo);
        var normalizedFlightNo = flightNo.Trim();
        if (normalizedFlightNo.Length > 80)
        {
            throw new ArgumentOutOfRangeException(nameof(flightNo), "Flight number exceeds 80 characters.");
        }

        return new Flight
        {
            Id = id,
            SurveyId = surveyId,
            DroneDeviceId = droneDeviceId,
            OperatorUserId = operatorUserId,
            StartedAt = startedAt.ToUniversalTime(),
            EndedAt = endedAt?.ToUniversalTime(),
            FlightNo = normalizedFlightNo
        };
    }
}
