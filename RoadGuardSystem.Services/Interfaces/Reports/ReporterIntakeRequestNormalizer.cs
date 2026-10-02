using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Reports;

namespace RoadGuardSystem.Services.Reports;

public sealed record NormalizedReporterEvidence(Guid FileId, string FileVersion, EvidenceCaptureMetadata CaptureMetadata);
public sealed record NormalizedReporterIntake(string Description, IReadOnlyList<NormalizedReporterEvidence> Evidence,
    string IdempotencyKey, string Fingerprint);

public static class ReporterIntakeRequestNormalizer
{
    public static bool TryNormalize(CreateReporterReportRequestDto? request, string? idempotencyKey,
        out NormalizedReporterIntake? normalized, out IReadOnlyDictionary<string, string[]>? errors)
    {
        normalized = null; errors = null;
        if (request is null)
        {
            return Invalid("request", "A request body is required.", out errors);
        }
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 1000)
        {
            return Invalid("description", "Description is required and must be at most 1000 characters.", out errors);
        }

        if (idempotencyKey is null || idempotencyKey.Any(character => character > 0x7f || char.IsControl(character)))
        {
            return Invalid("Idempotency-Key", "Idempotency-Key must contain 1 to 200 printable ASCII characters.", out errors);
        }
        var normalizedKey = idempotencyKey.Trim(' ');
        if (!IsPrintableKey(normalizedKey))
        {
            return Invalid("Idempotency-Key", "Idempotency-Key must contain 1 to 200 printable ASCII characters.", out errors);
        }
        if (request.Evidence is not { Count: > 0 })
        {
            return Invalid("evidence", "At least one evidence item is required.", out errors);
        }

        var evidence = new List<NormalizedReporterEvidence>(); var fileIds = new HashSet<Guid>();
        for (var index = 0; index < request.Evidence.Count; index++)
        {
            var item = request.Evidence[index];
            if (item is null) return Invalid($"evidence[{index}]", "Evidence item cannot be null.", out errors);
            if (item.FileId == Guid.Empty) return Invalid($"evidence[{index}].fileId", "File ID is required.", out errors);
            if (string.IsNullOrWhiteSpace(item.FileVersion) || item.FileVersion.Trim().Length > 200)
                return Invalid($"evidence[{index}].fileVersion", "File version is required and must be at most 200 characters.", out errors);
            if (!fileIds.Add(item.FileId)) return Invalid($"evidence[{index}].fileId", "Evidence file IDs must be unique.", out errors);
            if (!TryCapture(item, index, out var capture, out var field, out var message)) return Invalid(field!, message!, out errors);
            evidence.Add(new(item.FileId, item.FileVersion.Trim(), capture!));
        }
        var description = request.Description.Trim();
        normalized = new(description, evidence, normalizedKey!, Fingerprint(description, evidence));
        return true;
    }

    public static string Fingerprint(CreateReporterReportRequestDto request)
    {
        if (!TryNormalize(request, "key", out var normalized, out _)) throw new ArgumentException("Request is invalid.", nameof(request));
        return normalized!.Fingerprint;
    }

    private static bool TryCapture(ReportEvidenceInputDto input, int index, out EvidenceCaptureMetadata? metadata,
        out string? field, out string? message)
    {
        metadata = null; field = null; message = null;
        var source = input.LocationSource switch { "UNKNOWN" => EvidenceLocationSource.Unknown, "CAPTURE" => EvidenceLocationSource.Capture, "EXIF" => EvidenceLocationSource.Exif, "MANUAL" => EvidenceLocationSource.Manual, _ => (EvidenceLocationSource?)null };
        if (source is null)
        {
            field = $"evidence[{index}].locationSource"; message = "Location source must be UNKNOWN, CAPTURE, EXIF, or MANUAL."; return false;
        }
        if (input.Location is null && source != EvidenceLocationSource.Unknown)
        {
            field = $"evidence[{index}].location"; message = "Location is required for a known location source."; return false;
        }
        if (input.Location is not null && source == EvidenceLocationSource.Unknown)
        {
            field = $"evidence[{index}].location"; message = "UNKNOWN location source cannot include location metadata."; return false;
        }
        if (source == EvidenceLocationSource.Unknown)
        {
            metadata = EvidenceCaptureMetadata.Create(input.CapturedAt, source.Value);
            return true;
        }
        if (input.Location?.Latitude is null)
        {
            field = $"evidence[{index}].location.latitude"; message = "Latitude is required when location is provided."; return false;
        }
        if (input.Location?.Longitude is null)
        {
            field = $"evidence[{index}].location.longitude"; message = "Longitude is required when location is provided."; return false;
        }
        try
        {
            metadata = EvidenceCaptureMetadata.Create(input.CapturedAt, source.Value, input.Location?.Latitude,
                input.Location?.Longitude, input.Location?.AccuracyMeters);
            return true;
        }
        catch (ArgumentException)
        {
            field = $"evidence[{index}].location"; message = "Location metadata is invalid."; return false;
        }
    }

    private static bool IsPrintableKey(string? value) => value is { Length: >= 1 and <= 200 } && value.All(character => character is >= ' ' and <= '~');

    private static bool Invalid(string field, string message, out IReadOnlyDictionary<string, string[]>? errors)
    {
        errors = new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] };
        return false;
    }

    private static string Fingerprint(string description, IReadOnlyList<NormalizedReporterEvidence> evidence)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject(); writer.WriteString("description", description); writer.WritePropertyName("evidence"); writer.WriteStartArray();
            foreach (var item in evidence)
            {
                writer.WriteStartObject(); writer.WriteString("fileId", item.FileId); writer.WriteString("fileVersion", item.FileVersion); writer.WriteString("capturedAt", item.CaptureMetadata.CapturedAt?.ToUniversalTime().ToString("O")); writer.WriteString("locationSource", item.CaptureMetadata.LocationSource.ToString().ToUpperInvariant());
                if (item.CaptureMetadata.Latitude is decimal latitude) { writer.WritePropertyName("location"); writer.WriteStartObject(); writer.WriteNumber("latitude", latitude); writer.WriteNumber("longitude", item.CaptureMetadata.Longitude!.Value); if (item.CaptureMetadata.AccuracyMeters is decimal accuracy) writer.WriteNumber("accuracyMeters", accuracy); writer.WriteEndObject(); }
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return Convert.ToHexString(SHA256.HashData(buffer.WrittenSpan)).ToLowerInvariant();
    }
}
