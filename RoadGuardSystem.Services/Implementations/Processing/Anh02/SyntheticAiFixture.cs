using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Processing;

namespace RoadGuardSystem.Services.Processing.Anh02;

public sealed class SyntheticAiFixture(IOptions<Anh02AiOptions> options)
{
    public const string VideoHash = "152c804d61a49960bb3307501c68da4baccb68b264416d657b2533df0476da15";
    public const string FrameHash = "755358d3fc71b2543e4a3ce50b2887e4b64ecdf18db7dce500a5250fc379e82e";
    public const long VideoBytes = 1510;
    public const long FrameBytes = 1021;
    public const long DurationMilliseconds = 2000;
    public const long TimestampMilliseconds = 500;
    public static bool Supported(string version) => version is "synthetic-road-v1" or "synthetic-empty-v1";
    public byte[] Frame()
    {
        var bytes = File.ReadAllBytes(Path.Combine(options.Value.FixtureDirectory, "synthetic-road-v1-frame-1.png"));
        if (bytes.LongLength != FrameBytes || AiManifestCanonicalizer.Hash(bytes) != FrameHash
            || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            throw new AiRequestException(422, "ai_contract_invalid");
        return bytes;
    }
    public static void CheckSources(AiDatasetSourceFacts facts, string version)
    {
        if (!Supported(version)) throw new AiRequestException(422, "ai_contract_invalid");
        var videos = facts.Files.Where(f => f.Purpose == "SURVEY_VIDEO").ToArray();
        if (videos.Length == 0 || videos.Any(v => v.Sha256 != VideoHash || v.SizeBytes != VideoBytes || v.MediaType != "video/mp4"))
            throw new AiRequestException(422, "mock_fixture_source_mismatch");
    }
    public static Guid Identity(Guid run, string component)
        => new(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"anh02.ai.v1:{run:D}:{component}"))[..16]);
}
