using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoadGuardSystem.Services.Processing.Worker;

public sealed record AiWorkerAdmission(string ContractVersion, Guid ProjectId, Guid JobId, Guid AttemptId,
    Guid DatasetId, Guid SourceVideoFileId, string ManifestHash, Guid ModelVersionId, string CheckpointSha256,
    string PreprocessingVersion, string ConfigVersion, string InputSha256, long InputBytes,
    int InputWidth, int InputHeight, int InputDurationMs, double SampleFps, int MaxFrames);
public sealed record AiWorkerArtifact(Guid ArtifactId, string Sha256, long Bytes, string MediaType, int? Width = null, int? Height = null);
public sealed record AiWorkerDetection(Guid DetectionId, Guid FrameArtifactId, int TimestampMs, string SourceClass, double Confidence, IReadOnlyList<double> Bbox);
public sealed record AiWorkerTelemetry(string Source, string Outcome, string SourceVideoSha256, int? StreamIndex, string ExtractorVersion, AiWorkerArtifact? SrtArtifact);
public sealed record AiWorkerResult(AiWorkerAdmission Admission, Guid ResultId, AiWorkerArtifact RawArtifact,
    IReadOnlyList<AiWorkerArtifact> FrameArtifacts, IReadOnlyList<AiWorkerDetection> Detections,
    AiWorkerTelemetry Telemetry, string RoadMaskStatus, IReadOnlyList<string> Limitations);
public sealed record AiWorkerStatus(Guid AttemptId, string State, bool CancellationRequested, string? ErrorCode);

/// <summary>Strict private wire parser. Errors deliberately exclude remote values and bodies.</summary>
public static class AiWorkerWireContract
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string[] AdmissionFields = ["contractVersion", "projectId", "jobId", "attemptId", "datasetId", "sourceVideoFileId", "manifestHash", "modelVersionId", "checkpointSha256", "preprocessingVersion", "configVersion", "inputSha256", "inputBytes", "inputWidth", "inputHeight", "inputDurationMs", "sampleFps", "maxFrames"];
    private static readonly HashSet<string> Classes = ["alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack"];
    private static void Require(bool valid) { if (!valid) throw new InvalidDataException("INVALID_WORKER_CONTRACT"); }
    internal static bool SafeCode(string? value) => value is not null && Regex.IsMatch(value, "\\A[A-Z][A-Z0-9_]{0,127}\\z", RegexOptions.CultureInvariant);
    private static void Keys(JsonElement e, IEnumerable<string> fields)
    {
        Require(e.ValueKind == JsonValueKind.Object);
        var names = e.EnumerateObject().Select(p => p.Name).ToArray();
        var expected = fields.ToHashSet(StringComparer.Ordinal);
        Require(names.Length == expected.Count && names.Distinct(StringComparer.Ordinal).Count() == names.Length && names.All(expected.Contains));
    }
    private static string Str(JsonElement e) { Require(e.ValueKind == JsonValueKind.String); return e.GetString()!; }
    private static Guid Id(JsonElement e)
    {
        var s = Str(e); Require(Guid.TryParseExact(s, "D", out var id) && id != Guid.Empty && id.ToString("D") == s); return id;
    }
    private static string Hash(JsonElement e) { var s = Str(e); Require(Regex.IsMatch(s, "\\A[0-9a-f]{64}\\z")); return s; }
    private static long Integer(JsonElement e, long min, long max)
    {
        Require(e.ValueKind == JsonValueKind.Number && e.TryGetInt64(out var unused));
        var n = e.GetInt64(); Require(n >= min && n <= max); return n;
    }
    private static double Number(JsonElement e, double min, double max)
    {
        Require(e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out var unused));
        var n = e.GetDouble(); Require(double.IsFinite(n) && n >= min && n <= max); return n;
    }
    private static string Version(JsonElement e) { var s = Str(e); Require(Regex.IsMatch(s, "\\A[A-Za-z0-9][A-Za-z0-9_.-]{0,127}\\z")); return s; }
    private static string Choice(JsonElement e, params string[] choices) { var s = Str(e); Require(choices.Contains(s, StringComparer.Ordinal)); return s; }
    private static JsonElement[] Array(JsonElement e, int maximum) { Require(e.ValueKind == JsonValueKind.Array && e.GetArrayLength() <= maximum); return e.EnumerateArray().ToArray(); }
    public static void Validate(AiWorkerAdmission admission)
    {
        Require(admission is not null && double.IsFinite(admission.SampleFps));
        ParseAdmission(JsonSerializer.SerializeToElement(admission, JsonOptions));
    }
    public static AiWorkerAdmission ParseAdmission(JsonElement e)
    {
        Keys(e, AdmissionFields); return ReadAdmission(e);
    }
    private static AiWorkerAdmission ReadAdmission(JsonElement e)
    {
        Require(Str(e.GetProperty("contractVersion")) == "rg-ai-worker-v1");
        var fps = Number(e.GetProperty("sampleFps"), 0, 30); Require(fps > 0);
        return new("rg-ai-worker-v1", Id(e.GetProperty("projectId")), Id(e.GetProperty("jobId")), Id(e.GetProperty("attemptId")),
            Id(e.GetProperty("datasetId")), Id(e.GetProperty("sourceVideoFileId")), Hash(e.GetProperty("manifestHash")), Id(e.GetProperty("modelVersionId")),
            Hash(e.GetProperty("checkpointSha256")), Version(e.GetProperty("preprocessingVersion")), Version(e.GetProperty("configVersion")), Hash(e.GetProperty("inputSha256")),
            Integer(e.GetProperty("inputBytes"), 1, long.MaxValue), (int)Integer(e.GetProperty("inputWidth"), 1, 16384),
            (int)Integer(e.GetProperty("inputHeight"), 1, 16384), (int)Integer(e.GetProperty("inputDurationMs"), 1, int.MaxValue), fps,
            (int)Integer(e.GetProperty("maxFrames"), 1, 10000));
    }
    private static AiWorkerArtifact Artifact(JsonElement e, string media, bool frame = false)
    {
        Keys(e, frame ? ["artifactId", "sha256", "bytes", "mediaType", "width", "height"] : ["artifactId", "sha256", "bytes", "mediaType"]);
        return new(Id(e.GetProperty("artifactId")), Hash(e.GetProperty("sha256")), Integer(e.GetProperty("bytes"), 1, media == "application/x-subrip" ? 16777216 : long.MaxValue),
            Choice(e.GetProperty("mediaType"), media), frame ? (int)Integer(e.GetProperty("width"), 1, 16384) : null, frame ? (int)Integer(e.GetProperty("height"), 1, 16384) : null);
    }
    public static AiWorkerResult ParseResult(JsonElement e, AiWorkerAdmission expected)
    {
        Validate(expected);
        Keys(e, AdmissionFields.Concat(["resultId", "rawArtifact", "frameArtifacts", "detections", "telemetry", "roadMaskStatus", "limitations"]));
        Require(ReadAdmission(e) == expected);
        var raw = Artifact(e.GetProperty("rawArtifact"), "application/json");
        var frames = Array(e.GetProperty("frameArtifacts"), expected.MaxFrames).Select(f => Artifact(f, "image/jpeg", true)).ToArray();
        var ids = new HashSet<Guid> { raw.ArtifactId }; foreach (var f in frames) Require(ids.Add(f.ArtifactId));
        var frameIds = frames.Select(f => f.ArtifactId).ToHashSet(); var detectionIds = new HashSet<Guid>();
        var detections = Array(e.GetProperty("detections"), expected.MaxFrames * 1000).Select(d =>
        {
            Keys(d, ["detectionId", "frameArtifactId", "timestampMs", "sourceClass", "confidence", "bbox"]);
            var id = Id(d.GetProperty("detectionId")); Require(detectionIds.Add(id));
            var frame = Id(d.GetProperty("frameArtifactId")); Require(frameIds.Contains(frame));
            var sourceClass = Str(d.GetProperty("sourceClass")); Require(Classes.Contains(sourceClass));
            var box = Array(d.GetProperty("bbox"), 4).Select(v => Number(v, 0, 1)).ToArray();
            Require(box.Length == 4 && box[2] > 0 && box[3] > 0 && box[0] + box[2] <= 1 && box[1] + box[3] <= 1);
            return new AiWorkerDetection(id, frame, (int)Integer(d.GetProperty("timestampMs"), 0, expected.InputDurationMs), sourceClass, Number(d.GetProperty("confidence"), 0, 1), box);
        }).ToArray();
        var t = e.GetProperty("telemetry"); Keys(t, ["source", "outcome", "sourceVideoSha256", "streamIndex", "extractorVersion", "srtArtifact"]);
        var source = Choice(t.GetProperty("source"), "EMBEDDED", "PAIRED_SRT", "NONE");
        var outcome = Choice(t.GetProperty("outcome"), "EXTRACTED_VALID", "NO_SUBTITLE_STREAM", "UNSUPPORTED_STREAM", "INVALID_TELEMETRY", "TELEMETRY_AMBIGUOUS", "EXTRACTION_FAILED");
        var hash = Hash(t.GetProperty("sourceVideoSha256")); Require(hash == expected.InputSha256);
        var version = Str(t.GetProperty("extractorVersion")); Require(version.Length is >= 1 and <= 128);
        var srt = t.GetProperty("srtArtifact").ValueKind == JsonValueKind.Null ? null : Artifact(t.GetProperty("srtArtifact"), "application/x-subrip");
        if (srt is not null) Require(ids.Add(srt.ArtifactId));
        Require(outcome != "EXTRACTED_VALID" || (srt is not null && source != "NONE"));
        var telemetry = new AiWorkerTelemetry(source, outcome, hash, t.GetProperty("streamIndex").ValueKind == JsonValueKind.Null ? null : (int)Integer(t.GetProperty("streamIndex"), 0, int.MaxValue), version, srt);
        var limitations = Array(e.GetProperty("limitations"), 32).Select(Str).ToArray(); foreach (var code in limitations) Require(SafeCode(code));
        return new(expected, Id(e.GetProperty("resultId")), raw, frames, detections, telemetry, Choice(e.GetProperty("roadMaskStatus"), "AVAILABLE", "NOT_AVAILABLE"), limitations);
    }
    public static AiWorkerStatus ParseStatus(JsonElement e, Guid expected)
    {
        Keys(e, ["attemptId", "state", "cancellationRequested", "errorCode"]); var id = Id(e.GetProperty("attemptId")); Require(id == expected);
        var c = e.GetProperty("cancellationRequested"); Require(c.ValueKind is JsonValueKind.True or JsonValueKind.False);
        var error = e.GetProperty("errorCode").ValueKind == JsonValueKind.Null ? null : Str(e.GetProperty("errorCode")); Require(error is null || SafeCode(error));
        return new(id, Choice(e.GetProperty("state"), "AWAITING_INPUT", "QUEUED", "RUNNING", "COMPLETED", "FAILED", "CANCELLED", "INTERRUPTED"), c.GetBoolean(), error);
    }
}
