namespace RoadGuardSystem.BusinessObjects.Defects;

public sealed class Defect
{
    private Defect()
    {
    }

    public Guid Id { get; private set; }

    public string DefectTypeCode { get; private set; } = string.Empty;

    public string? CauseCategoryCode { get; private set; }

    public static Defect Create(Guid id, string defectTypeCode, string? causeCategoryCode = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Defect id must not be empty.", nameof(id));
        }

        return new Defect
        {
            Id = id,
            DefectTypeCode = ValidateCode(defectTypeCode, nameof(defectTypeCode)),
            CauseCategoryCode = string.IsNullOrWhiteSpace(causeCategoryCode)
                ? null
                : ValidateCode(causeCategoryCode, nameof(causeCategoryCode))
        };
    }

    private static string ValidateCode(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > 80)
        {
            throw new ArgumentException("Catalog code exceeds maximum length 80.", parameterName);
        }

        return normalized;
    }
}
