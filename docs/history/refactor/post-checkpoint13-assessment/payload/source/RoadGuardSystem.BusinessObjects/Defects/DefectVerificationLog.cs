using System.Text.Json;
using RoadGuardSystem.aBusinessObjects.Commons;

namespace RoadGuardSystem.BusinessObjects.Defects;

public sealed class DefectVerificationLog
{
    private DefectVerificationLog()
    {
    }

    public Guid Id { get; private set; }

    public Guid? DefectId { get; private set; }

    public Guid? AIDetectionId { get; private set; }

    public DefectVerificationAction Action { get; private set; }

    public string? BeforeSnapshot { get; private set; }

    public string? AfterSnapshot { get; private set; }

    public Guid? SeverityRuleVersionId { get; private set; }

    public Guid? FieldInspectionTaskId { get; private set; }

    public Guid VerifiedByUserId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public static DefectVerificationLog Create(
        Guid id,
        Guid? defectId,
        Guid? aiDetectionId,
        DefectVerificationAction action,
        string? beforeSnapshot,
        string? afterSnapshot,
        Guid? severityRuleVersionId,
        Guid? fieldInspectionTaskId,
        Guid verifiedByUserId,
        string reason)
    {
        if (id == Guid.Empty || verifiedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Verification log and verifier ids must not be empty.");
        }

        if ((defectId is null) == (aiDetectionId is null))
        {
            throw new ArgumentException("Verification log must target exactly one defect or AI detection.");
        }

        if (defectId == Guid.Empty || aiDetectionId == Guid.Empty ||
            severityRuleVersionId == Guid.Empty || fieldInspectionTaskId == Guid.Empty)
        {
            throw new ArgumentException("Optional verification references must be non-empty when supplied.");
        }

        if (!Enum.IsDefined(action) || action == DefectVerificationAction.Unknown)
        {
            throw new ArgumentOutOfRangeException(nameof(action), "Verification action must be specified.");
        }

        return new DefectVerificationLog
        {
            Id = id,
            DefectId = defectId,
            AIDetectionId = aiDetectionId,
            Action = action,
            BeforeSnapshot = ValidateOptionalJson(beforeSnapshot, nameof(beforeSnapshot)),
            AfterSnapshot = ValidateOptionalJson(afterSnapshot, nameof(afterSnapshot)),
            SeverityRuleVersionId = severityRuleVersionId,
            FieldInspectionTaskId = fieldInspectionTaskId,
            VerifiedByUserId = verifiedByUserId,
            Reason = NormalizeReason(reason)
        };
    }

    private static string? ValidateOptionalJson(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                throw new ArgumentException("Snapshot must be a JSON object or array.", parameterName);
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Snapshot must be valid JSON.", parameterName, exception);
        }

        return value.Trim();
    }

    private static string NormalizeReason(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        var normalized = value.Trim();
        if (normalized.Length > 1_000)
        {
            throw new ArgumentException("Verification reason exceeds maximum length 1000.", nameof(value));
        }

        return normalized;
    }
}
