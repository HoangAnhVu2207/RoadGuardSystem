using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace RoadGuardSystem.Services.Processing.Worker;

public sealed class AiWorkerHttpException(HttpStatusCode statusCode, string code) : Exception(code)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public bool Retryable => StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout || (int)StatusCode >= 500;
}

/// <summary>Private fixed-destination transport. Caller owns HttpClient and input/output streams.
/// The supplied HttpClient MUST disable automatic redirects to prevent bearer credential forwarding.</summary>
public sealed class AiWorkerClient
{
    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private readonly string _token;
    private readonly TimeSpan _timeout;
    public AiWorkerClient(HttpClient http, Uri endpoint, string token, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != "http" || endpoint.AbsolutePath != "/" ||
            endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 || endpoint.UserInfo.Length != 0 || !PrivateHost(endpoint.Host))
            throw new ArgumentException("A private HTTP worker origin is required.", nameof(endpoint));
        if (token is null || token.Length < 32 || token.Any(c => c <= 32 || c >= 127))
            throw new ArgumentException("Invalid service token.", nameof(token));
        _timeout = timeout ?? TimeSpan.FromMinutes(10);
        if (_timeout <= TimeSpan.Zero || _timeout > TimeSpan.FromHours(1)) throw new ArgumentOutOfRangeException(nameof(timeout));
        _http = http; _endpoint = endpoint; _token = token;
    }
    public static HttpClient CreateHttpClient() => new(new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false, ConnectTimeout = TimeSpan.FromSeconds(15) }) { Timeout = Timeout.InfiniteTimeSpan };
    private static bool PrivateHost(string host)
    {
        if (host == "localhost" || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (!IPAddress.TryParse(host.Trim('[', ']'), out var ip)) return !host.Contains('.') || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase);
        if (IPAddress.IsLoopback(ip)) return true;
        var b = ip.GetAddressBytes();
        return b.Length == 4 ? b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168) : (b[0] & 0xfe) == 0xfc;
    }
    private static string Path(Guid attempt, string suffix = "")
    {
        if (attempt == Guid.Empty) throw new ArgumentException("Invalid attempt identity.", nameof(attempt));
        return "internal/v1/jobs/" + attempt.ToString("D") + suffix;
    }
    public Task<AiWorkerStatus> AdmitAsync(AiWorkerAdmission admission, CancellationToken cancellationToken = default)
    {
        AiWorkerWireContract.Validate(admission);
        return StatusAsync(HttpMethod.Post, "internal/v1/jobs", admission.AttemptId,
            new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(admission, AiWorkerWireContract.JsonOptions)), cancellationToken);
    }
    public Task<AiWorkerStatus> GetStatusAsync(Guid attemptId, CancellationToken cancellationToken = default)
        => StatusAsync(HttpMethod.Get, Path(attemptId), attemptId, null, cancellationToken);
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        using var deadline = Deadline(cancellationToken);
        using var response = await SendAsync(HttpMethod.Get, "internal/v1/health", null, deadline.Token);
        using var json = await ReadJsonAsync(response, 1024, deadline.Token);
        var root = json.RootElement;
        return root.ValueKind == JsonValueKind.Object && root.EnumerateObject().Count() == 1 &&
            root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String && status.GetString() == "ok";
    }
    public Task<AiWorkerStatus> CancelAsync(Guid attemptId, CancellationToken cancellationToken = default)
        => StatusAsync(HttpMethod.Post, Path(attemptId, "/cancel"), attemptId, null, cancellationToken);
    public Task<AiWorkerStatus> UploadInputAsync(AiWorkerAdmission admission, Stream input, CancellationToken cancellationToken = default)
    {
        AiWorkerWireContract.Validate(admission);
        var content = new VerifiedStreamContent(input, admission.InputBytes, admission.InputSha256);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Headers.Add("X-Content-SHA256", admission.InputSha256);
        return StatusAsync(HttpMethod.Put, Path(admission.AttemptId, "/input"), admission.AttemptId, content, cancellationToken);
    }
    private async Task<AiWorkerStatus> StatusAsync(HttpMethod method, string path, Guid attempt, HttpContent? content, CancellationToken ct)
    {
        using var deadline = Deadline(ct);
        using var response = await SendAsync(method, path, content, deadline.Token);
        using var json = await ReadJsonAsync(response, 64 * 1024, deadline.Token);
        return AiWorkerWireContract.ParseStatus(json.RootElement, attempt);
    }
    public async Task<AiWorkerResult> GetResultAsync(AiWorkerAdmission admission, CancellationToken cancellationToken = default)
    {
        AiWorkerWireContract.Validate(admission);
        using var deadline = Deadline(cancellationToken);
        using var response = await SendAsync(HttpMethod.Get, Path(admission.AttemptId, "/result"), null, deadline.Token);
        using var json = await ReadJsonAsync(response, 64 * 1024 * 1024, deadline.Token);
        return AiWorkerWireContract.ParseResult(json.RootElement, admission);
    }
    public async Task DownloadArtifactAsync(AiWorkerAdmission admission, AiWorkerResult result, Guid artifactId, Stream destination, CancellationToken cancellationToken = default)
    {
        AiWorkerWireContract.Validate(admission);
        if (result.Admission != admission) throw new InvalidDataException("INVALID_WORKER_CONTRACT");
        var artifact = result.FrameArtifacts.Append(result.RawArtifact).Concat(result.Telemetry.SrtArtifact is { } srt ? [srt] : [])
            .SingleOrDefault(a => a.ArtifactId == artifactId) ?? throw new InvalidDataException("UNKNOWN_WORKER_ARTIFACT");
        using var deadline = Deadline(cancellationToken);
        using var response = await SendAsync(HttpMethod.Get, Path(admission.AttemptId, "/artifacts/" + artifactId.ToString("D")), null, deadline.Token);
        if (response.Content.Headers.ContentLength is { } length && length != artifact.Bytes || response.Content.Headers.ContentType?.MediaType != artifact.MediaType)
            throw new InvalidDataException("INVALID_WORKER_ARTIFACT");
        await using var source = await response.Content.ReadAsStreamAsync(deadline.Token);
        await CopyVerifiedAsync(source, destination, artifact.Bytes, artifact.Sha256, deadline.Token);
    }
    private CancellationTokenSource Deadline(CancellationToken ct)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(ct); source.CancelAfter(_timeout); return source;
    }
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, new Uri(_endpoint, path)) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (content is ByteArrayContent) content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.OK || (method == HttpMethod.Post && path == "internal/v1/jobs" && response.StatusCode == HttpStatusCode.Accepted)) return response;
            response.Dispose(); throw new InvalidDataException("INVALID_WORKER_HTTP_STATUS");
        }
        using (response)
        {
            var code = "WORKER_HTTP_ERROR";
            try
            {
                using var json = await ReadJsonAsync(response, 64 * 1024, ct);
                var root = json.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.EnumerateObject().Count() == 1 && root.TryGetProperty("code", out var value) &&
                    value.ValueKind == JsonValueKind.String && AiWorkerWireContract.SafeCode(value.GetString())) code = value.GetString()!;
            }
            catch (Exception e) when (e is JsonException or InvalidDataException) { }
            throw new AiWorkerHttpException(response.StatusCode, code);
        }
    }
    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, int maximum, CancellationToken ct)
    {
        if (response.Content.Headers.ContentLength > maximum) throw new InvalidDataException("WORKER_RESPONSE_TOO_LARGE");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var memory = new MemoryStream(); var buffer = new byte[16384]; int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (memory.Length + read > maximum) throw new InvalidDataException("WORKER_RESPONSE_TOO_LARGE");
            await memory.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        try { return JsonDocument.Parse(memory.ToArray(), new JsonDocumentOptions { MaxDepth = 32 }); }
        catch (JsonException) { throw new InvalidDataException("INVALID_WORKER_CONTRACT"); }
    }
    private static async Task CopyVerifiedAsync(Stream source, Stream destination, long bytes, string sha256, CancellationToken ct)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var buffer = new byte[64 * 1024]; long count = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer.AsMemory(0, count == bytes ? 1 : (int)Math.Min(buffer.Length, bytes - count)), ct);
            if (read == 0) break;
            if (read > bytes - count) throw new InvalidDataException("WORKER_CHECKSUM_MISMATCH");
            count += read; hash.AppendData(buffer, 0, read); await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        if (count != bytes || !string.Equals(Convert.ToHexString(hash.GetHashAndReset()), sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("WORKER_CHECKSUM_MISMATCH");
    }
    private sealed class VerifiedStreamContent(Stream source, long bytes, string sha256) : HttpContent
    {
        protected override bool TryComputeLength(out long length) { length = bytes; return true; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => CopyVerifiedAsync(source, stream, bytes, sha256, CancellationToken.None);
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) => CopyVerifiedAsync(source, stream, bytes, sha256, cancellationToken);
    }
}
