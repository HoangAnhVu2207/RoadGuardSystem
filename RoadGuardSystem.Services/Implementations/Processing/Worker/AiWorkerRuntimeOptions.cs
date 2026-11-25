using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RoadGuardSystem.Services.Processing.Worker;

/// <summary>Opt-in configuration. Catalog values must be explicitly supplied and verified by SQL readiness.</summary>
public sealed class AiWorkerRuntimeOptions
{
    public bool Enabled { get; set; }
    public string WorkerEndpoint { get; set; } = "http://127.0.0.1:8999/";
    public string ServiceTokenFile { get; set; } = "";
    public Guid ReleasedModelVersionId { get; set; }
    public string CheckpointSha256 { get; set; } = "";
    public string PreprocessingVersion { get; set; } = "ffmpeg-pts-v1";
    public string ConfigVersion { get; set; } = "ft95-cpu-v1";
    public double SampleFps { get; set; } = 1;
    public int MaxFrames { get; set; } = 120;
    public Dictionary<string, string> ClassMap { get; set; } = new(StringComparer.Ordinal);
    public string TempRoot { get; set; } = "D:/RoadGuard-AI-LocalTest/be-spool";
    public string FfprobePath { get; set; } = "ffprobe";
    public TimeSpan TransportTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(40);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public long DiskBudgetBytes { get; set; } = 40L * 1024 * 1024 * 1024;
    public long MinimumFreeBytes { get; set; } = 30L * 1024 * 1024 * 1024;

    internal AiWorkerSettings Freeze()
    {
        var map = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(ClassMap, StringComparer.Ordinal));
        if (Enabled)
        {
            var test = new AiWorkerAdmission("rg-ai-worker-v1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('0', 64),
                ReleasedModelVersionId, CheckpointSha256, PreprocessingVersion, ConfigVersion, new string('0', 64), 1, 1, 1, 1, SampleFps, MaxFrames);
            AiWorkerWireContract.Validate(test);
            string[] labels = ["alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack"];
            if (map.Count != 6 || labels.Any(label => !map.ContainsKey(label)) || map.Values.Any(v => string.IsNullOrWhiteSpace(v) || v.Length > 80) ||
                map.Values.Distinct(StringComparer.Ordinal).Count() != 6 || map["repair"] != "REPAIR_PATCH")
                throw new ArgumentException("An explicit six-class mapping is required.");
            if (!Path.IsPathFullyQualified(TempRoot) || string.IsNullOrWhiteSpace(FfprobePath) ||
                TransportTimeout <= TimeSpan.Zero || TransportTimeout >= OperationTimeout || OperationTimeout >= LeaseDuration || LeaseDuration > TimeSpan.FromHours(1) ||
                PollInterval <= TimeSpan.Zero || PollInterval > TimeSpan.FromMinutes(5) || RetryDelay <= TimeSpan.Zero || RetryDelay > TimeSpan.FromMinutes(5) ||
                DiskBudgetBytes <= 0 || DiskBudgetBytes > 40L * 1024 * 1024 * 1024 || MinimumFreeBytes < 30L * 1024 * 1024 * 1024)
                throw new ArgumentException("Invalid bounded worker runtime configuration.");
        }
        var identityJson = JsonSerializer.Serialize(new
        {
            contractVersion = "rg-ai-worker-v1",
            modelVersionId = ReleasedModelVersionId,
            checkpointSha256 = CheckpointSha256,
            preprocessingVersion = PreprocessingVersion,
            configVersion = ConfigVersion,
            sampleFps = SampleFps,
            maxFrames = MaxFrames,
            classes = map.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new { sourceClass = x.Key, typeCode = x.Value }).ToArray()
        });
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identityJson))).ToLowerInvariant();
        return new(Enabled, ReleasedModelVersionId, CheckpointSha256, PreprocessingVersion, ConfigVersion, SampleFps, MaxFrames, map,
            TempRoot, FfprobePath, OperationTimeout, LeaseDuration, PollInterval, RetryDelay, DiskBudgetBytes, MinimumFreeBytes, identity);
    }
}

internal sealed record AiWorkerSettings(bool Enabled, Guid ModelVersionId, string CheckpointSha256, string PreprocessingVersion, string ConfigVersion,
    double SampleFps, int MaxFrames, IReadOnlyDictionary<string, string> ClassMap, string TempRoot, string FfprobePath, TimeSpan OperationTimeout,
    TimeSpan LeaseDuration, TimeSpan PollInterval, TimeSpan RetryDelay, long DiskBudgetBytes, long MinimumFreeBytes, string Identity);
