using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Processing;

public sealed class ValidationRun
{
    private ValidationRun()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ModelVersionId { get; private set; }
    public string DatasetSplitId { get; private set; } = string.Empty;
    public string MeasurementType { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public string PairsJson { get; private set; } = string.Empty;
    public ValidationRunStatus Status { get; private set; }
    public int UsedCount { get; private set; }
    public int ExcludedCount { get; private set; }
    public decimal? Bias { get; private set; }
    public decimal? Mae { get; private set; }
    public decimal? Rmse { get; private set; }
    public string ExclusionReasonsJson { get; private set; } = "[]";
    public byte[] RowVersion { get; private set; } = [];

    public static ValidationRun CreateQueued(
        Guid id,
        Guid projectId,
        Guid modelVersionId,
        string datasetSplitId,
        string measurementType,
        string unit,
        string pairsJson)
    {
        if (id == Guid.Empty || projectId == Guid.Empty || modelVersionId == Guid.Empty)
        {
            throw new ArgumentException("Validation run identity is invalid.");
        }

        ValidateNonEmptyJsonArray(pairsJson, nameof(pairsJson));
        return new ValidationRun
        {
            Id = id,
            ProjectId = projectId,
            ModelVersionId = modelVersionId,
            DatasetSplitId = Required(datasetSplitId, nameof(datasetSplitId), 120),
            MeasurementType = Required(measurementType, nameof(measurementType), 80),
            Unit = Required(unit, nameof(unit), 32),
            PairsJson = pairsJson.Trim(),
            Status = ValidationRunStatus.Queued,
            ExclusionReasonsJson = "[]"
        };
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentOutOfRangeException(parameterName);
        return normalized;
    }

    public void Complete(int usedCount, int excludedCount, decimal bias, decimal mae, decimal rmse, string exclusionReasonsJson)
    {
        if (usedCount <= 0 || excludedCount < 0 || mae < 0 || rmse < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(usedCount));
        }

        ValidateJsonArray(exclusionReasonsJson, nameof(exclusionReasonsJson));
        Status = ValidationRunStatus.Completed;
        UsedCount = usedCount;
        ExcludedCount = excludedCount;
        Bias = bias;
        Mae = mae;
        Rmse = rmse;
        ExclusionReasonsJson = exclusionReasonsJson.Trim();
    }

    private static void ValidateJsonArray(string value, string parameterName)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Value must be a JSON array.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Pairs must be valid JSON.", parameterName, exception);
        }
    }

    private static void ValidateNonEmptyJsonArray(string value, string parameterName)
    {
        ValidateJsonArray(value, parameterName);
        using var document = JsonDocument.Parse(value);
        if (document.RootElement.GetArrayLength() == 0)
        {
            throw new ArgumentException("Pairs must be a non-empty JSON array.", parameterName);
        }
    }
}
