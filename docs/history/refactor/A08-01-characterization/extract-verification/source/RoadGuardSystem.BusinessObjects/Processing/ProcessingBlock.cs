using System.Text.Json;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class ProcessingBlock
{
    private ProcessingBlock()
    {
    }

    public Guid Id { get; private set; }
    public Guid SurveyDataVersionId { get; private set; }
    public int BlockNo { get; private set; }
    public string RangeMetadata { get; private set; } = string.Empty;

    public static ProcessingBlock Create(
        Guid id,
        Guid surveyDataVersionId,
        int blockNo,
        string rangeMetadata)
    {
        if (id == Guid.Empty || surveyDataVersionId == Guid.Empty || blockNo <= 0)
        {
            throw new ArgumentException("Processing block, source version, and positive block number are required.");
        }

        ValidateJsonObject(rangeMetadata, nameof(rangeMetadata));
        return new ProcessingBlock
        {
            Id = id,
            SurveyDataVersionId = surveyDataVersionId,
            BlockNo = blockNo,
            RangeMetadata = rangeMetadata.Trim()
        };
    }

    private static void ValidateJsonObject(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Range metadata must be a JSON object.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Range metadata must be valid JSON.", parameterName, exception);
        }
    }
}
