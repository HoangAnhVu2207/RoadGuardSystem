using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using RoadGuardSystem.Services.Processing.Worker;
using Xunit;

namespace RoadGuardSystem.UnitTests.Processing;

/// <summary>Mock transport/fixture evidence only; does not call a production worker.</summary>
public sealed class AiWorkerClientTests
{
    private static string Fixture(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(System.IO.Path.Combine(dir.FullName, "tests", "fixtures", "ai-worker-v1"))) dir = dir.Parent;
        return File.ReadAllText(System.IO.Path.Combine(dir!.FullName, "tests", "fixtures", "ai-worker-v1", name + ".json"));
    }
    private static AiWorkerAdmission Admission()
    {
        using var doc = JsonDocument.Parse(Fixture("admission")); return AiWorkerWireContract.ParseAdmission(doc.RootElement);
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => action(request, cancellationToken);
    }
    private static HttpResponseMessage Json(string value, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(value, Encoding.UTF8, "application/json") };
    private static AiWorkerClient Client(Handler handler) => new(new HttpClient(handler), new Uri("http://127.0.0.1:8999/"), new string('t', 32));
    private static string Status(Guid id) => JsonSerializer.Serialize(new { attemptId = id, state = "QUEUED", cancellationRequested = false, errorCode = (string?)null });
    [Fact]
    public void GoldenResultPreservesAllSixClassesAndRepairPatch()
    {
        using var doc = JsonDocument.Parse(Fixture("result"));
        var result = AiWorkerWireContract.ParseResult(doc.RootElement, Admission());
        result.Detections.Select(d => d.SourceClass).Should().BeEquivalentTo("alligator crack", "longitudinal crack", "oblique crack", "pothole", "repair", "transverse crack");
    }
    [Fact]
    public void SharedNegativeAdmissionsAreRejected()
    {
        var cases = JsonNode.Parse(Fixture("negative-admissions"))!.AsArray();
        foreach (var test in cases)
        {
            var admission = JsonNode.Parse(Fixture("admission"))!;
            admission[test!["field"]!.GetValue<string>()] = test["value"]!.DeepClone();
            using var doc = JsonDocument.Parse(admission.ToJsonString());
            Action act = () => AiWorkerWireContract.ParseAdmission(doc.RootElement);
            act.Should().Throw<InvalidDataException>();
        }
    }
    [Theory]
    [InlineData("wrong_identity")]
    [InlineData("unknown_class")]
    [InlineData("missing_frame")]
    [InlineData("duplicate_detection")]
    [InlineData("duplicate_artifact")]
    [InlineData("outside_box")]
    [InlineData("zero_box")]
    [InlineData("late_timestamp")]
    [InlineData("telemetry_hash")]
    [InlineData("unknown_field")]
    [InlineData("infinite")]
    public void InvalidResultIsRejected(string kind)
    {
        var root = JsonNode.Parse(Fixture("result"))!.AsObject();
        var d = root["detections"]![0]!;
        switch (kind)
        {
            case "wrong_identity": root["jobId"] = Guid.NewGuid().ToString(); break;
            case "unknown_class": d["sourceClass"] = "CRACK"; break;
            case "missing_frame": d["frameArtifactId"] = Guid.NewGuid().ToString(); break;
            case "duplicate_detection": root["detections"]![1]!["detectionId"] = d["detectionId"]!.DeepClone(); break;
            case "duplicate_artifact": root["frameArtifacts"]![0]!["artifactId"] = root["rawArtifact"]!["artifactId"]!.DeepClone(); break;
            case "outside_box": d["bbox"] = new JsonArray(0.9, 0, 0.2, 0.1); break;
            case "zero_box": d["bbox"] = new JsonArray(0, 0, 0, 0.1); break;
            case "late_timestamp": d["timestampMs"] = Admission().InputDurationMs + 1; break;
            case "telemetry_hash": root["telemetry"]!["sourceVideoSha256"] = new string('f', 64); break;
            case "unknown_field": root["url"] = "http://outside.invalid"; break;
            case "infinite": d["confidence"] = JsonNode.Parse("1e999"); break;
        }
        using var doc = JsonDocument.Parse(root.ToJsonString());
        Action act = () => AiWorkerWireContract.ParseResult(doc.RootElement, Admission()); act.Should().Throw<InvalidDataException>();
    }
    [Theory]
    [InlineData("http://example.com/")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("http://127.0.0.1/else")]
    [InlineData("http://user:secret@127.0.0.1/")]
    public void PublicOrNonOriginDestinationsAreRejected(string url)
    {
        Action act = () => _ = new AiWorkerClient(new HttpClient(), new Uri(url), new string('t', 32)); act.Should().Throw<ArgumentException>();
    }
    [Fact]
    public async Task AdmissionSendsDedicatedBearerAndRejectsWrongAttemptStatus()
    {
        var admission = Admission();
        var client = Client(new Handler(async (request, ct) =>
        {
            request.Method.Should().Be(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.Should().Be("/internal/v1/jobs");
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            using var doc = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            AiWorkerWireContract.ParseAdmission(doc.RootElement).Should().Be(admission);
            return Json(Status(Guid.NewGuid()), HttpStatusCode.Accepted);
        }));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.AdmitAsync(admission));
    }
    [Fact]
    public async Task UploadStreamsExactBytesAndHashWithoutClosingInput()
    {
        var bytes = Encoding.UTF8.GetBytes("synthetic video");
        var admission = Admission() with { InputBytes = bytes.Length, InputSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() };
        using var input = new MemoryStream(bytes);
        var client = Client(new Handler(async (request, ct) =>
        {
            request.Method.Should().Be(HttpMethod.Put);
            request.Content!.Headers.ContentLength.Should().Be(bytes.Length);
            request.Content.Headers.GetValues("X-Content-SHA256").Single().Should().Be(admission.InputSha256);
            (await request.Content.ReadAsByteArrayAsync(ct)).Should().Equal(bytes);
            return Json(Status(admission.AttemptId));
        }));
        (await client.UploadInputAsync(admission, input)).State.Should().Be("QUEUED"); input.CanRead.Should().BeTrue();
    }
    [Theory]
    [InlineData("short")]
    [InlineData("long")]
    [InlineData("hash")]
    public async Task UploadRejectsMismatch(string kind)
    {
        var bytes = Encoding.UTF8.GetBytes("test");
        var admission = Admission() with { InputBytes = kind == "short" ? 5 : kind == "long" ? 3 : 4, InputSha256 = new string('0', 64) };
        var client = Client(new Handler(async (request, ct) => { await request.Content!.CopyToAsync(Stream.Null, ct); return Json(Status(admission.AttemptId)); }));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.UploadInputAsync(admission, new MemoryStream(bytes)));
    }
    [Fact]
    public async Task ErrorBodyDoesNotLeakArbitraryRemoteText()
    {
        var client = Client(new Handler((request, ct) => Task.FromResult(Json("{\"code\":\"secret with spaces\"}", HttpStatusCode.ServiceUnavailable))));
        var exception = await Assert.ThrowsAsync<AiWorkerHttpException>(() => client.GetStatusAsync(Guid.NewGuid()));
        exception.Code.Should().Be("WORKER_HTTP_ERROR"); exception.Retryable.Should().BeTrue();
    }
    [Fact]
    public void DuplicateAndNoncanonicalAdmissionPropertiesAreRejected()
    {
        var text = Fixture("admission"); using var doc = JsonDocument.Parse(text.Replace("{", "{\"sampleFps\":1,", StringComparison.Ordinal));
        Action duplicate = () => AiWorkerWireContract.ParseAdmission(doc.RootElement); duplicate.Should().Throw<InvalidDataException>();
        var node = JsonNode.Parse(text)!; node["attemptId"] = Admission().AttemptId.ToString().ToUpperInvariant();
        // A letter-containing UUID is needed even if a fixture UUID uses only digits.
        node["attemptId"] = "AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA";
        using var upperDoc = JsonDocument.Parse(node.ToJsonString());
        Action uppercase = () => AiWorkerWireContract.ParseAdmission(upperDoc.RootElement); uppercase.Should().Throw<InvalidDataException>();
    }
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(double.NaN)]
    public void InvalidSampleRateRejected(double fps)
    {
        Action act = () => AiWorkerWireContract.Validate(Admission() with { SampleFps = fps }); act.Should().Throw<InvalidDataException>();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ArtifactDownloadChecksHashAndLength(bool corrupt)
    {
        using var doc = JsonDocument.Parse(Fixture("result")); var admission = Admission();
        var result = AiWorkerWireContract.ParseResult(doc.RootElement, admission);
        var bytes = Encoding.UTF8.GetBytes("mock artifact");
        result = result with { RawArtifact = result.RawArtifact with { Bytes = bytes.Length, Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() } };
        var client = Client(new Handler((request, ct) =>
        {
            request.RequestUri!.AbsolutePath.Should().EndWith("/artifacts/" + result.RawArtifact.ArtifactId);
            var content = new ByteArrayContent(corrupt ? Encoding.UTF8.GetBytes("evil artifact") : bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }));
        using var output = new MemoryStream();
        if (corrupt) await Assert.ThrowsAsync<InvalidDataException>(() => client.DownloadArtifactAsync(admission, result, result.RawArtifact.ArtifactId, output));
        else { await client.DownloadArtifactAsync(admission, result, result.RawArtifact.ArtifactId, output); output.ToArray().Should().Equal(bytes); }
        output.CanWrite.Should().BeTrue();
    }
    [Fact]
    public async Task ResultAndCancellationUseOnlyFixedAttemptRoutes()
    {
        var admission = Admission(); var paths = new List<string>();
        var client = Client(new Handler((request, ct) =>
        {
            paths.Add(request.Method + " " + request.RequestUri!.AbsolutePath);
            return Task.FromResult(Json(request.RequestUri.AbsolutePath.EndsWith("/result", StringComparison.Ordinal) ? Fixture("result") : Status(admission.AttemptId)));
        }));
        (await client.GetResultAsync(admission)).Admission.Should().Be(admission);
        await client.GetStatusAsync(admission.AttemptId); await client.CancelAsync(admission.AttemptId);
        paths.Should().Equal($"GET /internal/v1/jobs/{admission.AttemptId}/result", $"GET /internal/v1/jobs/{admission.AttemptId}", $"POST /internal/v1/jobs/{admission.AttemptId}/cancel");
    }
    [Fact]
    public async Task CallerCancellationReachesTransport()
    {
        var client = Client(new Handler(async (request, ct) => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return Json("{}"); }));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetStatusAsync(Guid.NewGuid(), cancellation.Token));
    }
    [Fact]
    public async Task OversizedControlBodyIsRejected()
    {
        var client = Client(new Handler((request, ct) => Task.FromResult(Json(new string(' ', 65537)))));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.GetStatusAsync(Guid.NewGuid()));
    }
}
