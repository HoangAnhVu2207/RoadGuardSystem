using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Reports;
using RoadGuardSystem.DTOs.Reports;

namespace RoadGuardSystem.Services.Reports;

public sealed record NormalizedReporterEvidence(Guid FileId, string FileVersion, EvidenceCaptureMetadata CaptureMetadata);
public sealed record NormalizedReporterIntake(string Description, IReadOnlyList<NormalizedReporterEvidence> Evidence, string Fingerprint);

public static class ReporterIntakeRequestNormalizer
{
    public static bool TryNormalize(CreateReporterReportRequestDto? request, string? idempotencyKey,
        out NormalizedReporterIntake? normalized, out string? error)
    {
        normalized = null; error = null;
        if (request is null || string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 1000) { error = "description"; return false; }
        if (!IsPrintableKey(idempotencyKey)) { error = "idempotencyKey"; return false; }
        if (request.Evidence is not { Count: > 0 }) { error = "evidence"; return false; }
        var evidence = new List<NormalizedReporterEvidence>(); var fileIds = new HashSet<Guid>();
        foreach (var item in request.Evidence)
        {
            if (item is null || item.FileId == Guid.Empty || string.IsNullOrWhiteSpace(item.FileVersion) || item.FileVersion.Trim().Length > 200 || !fileIds.Add(item.FileId)) { error = "evidence"; return false; }
            if (!TryCapture(item, out var capture)) { error = "evidence.locationSource"; return false; }
            evidence.Add(new(item.FileId, item.FileVersion.Trim(), capture!));
        }
        var description = request.Description.Trim();
        normalized = new(description, evidence, Fingerprint(description, evidence)); return true;
    }

    public static string Fingerprint(CreateReporterReportRequestDto request)
    {
        if (!TryNormalize(request, "key", out var normalized, out _)) throw new ArgumentException("Request is invalid.", nameof(request));
        return normalized!.Fingerprint;
    }

    private static bool TryCapture(ReportEvidenceInputDto input, out EvidenceCaptureMetadata? metadata)
    {
        metadata = null;
        var source = input.LocationSource switch { "UNKNOWN" => EvidenceLocationSource.Unknown, "CAPTURE" => EvidenceLocationSource.Capture, "EXIF" => EvidenceLocationSource.Exif, "MANUAL" => EvidenceLocationSource.Manual, _ => (EvidenceLocationSource?)null };
        if (source is null || (input.Location is null && source != EvidenceLocationSource.Unknown) || (input.Location is not null && source == EvidenceLocationSource.Unknown)) return false;
        try { metadata = EvidenceCaptureMetadata.Create(input.CapturedAt, source.Value, input.Location?.Latitude, input.Location?.Longitude, input.Location?.AccuracyMeters); return true; }
        catch (ArgumentException) { return false; }
    }

    private static bool IsPrintableKey(string? value) => value is { Length: >= 1 and <= 200 } && value.All(character => character is >= '!' and <= '~');

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
