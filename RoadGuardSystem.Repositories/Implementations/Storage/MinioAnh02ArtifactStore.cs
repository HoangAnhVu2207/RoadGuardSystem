using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;

namespace RoadGuardSystem.Repositories.Storage;

public sealed class MinioAnh02ArtifactStore : IAnh02ArtifactStore
{
    private readonly MinioStorageOptions _options;
    private readonly Func<IAmazonS3>? _factory;
    public MinioAnh02ArtifactStore(IOptions<MinioStorageOptions> options) => _options = options.Value;
    public MinioAnh02ArtifactStore(IOptions<MinioStorageOptions> options, Func<IAmazonS3> factory) : this(options) => _factory = factory;
    private IAmazonS3 Client()
    {
        if (_factory is not null) return _factory();
        if (new[] { _options.Endpoint, _options.BucketName, _options.AccessKey, _options.SecretKey }.Any(string.IsNullOrWhiteSpace)) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Artifact storage is not configured.");
        var endpoint = _options.Endpoint.Contains("://", StringComparison.Ordinal) ? _options.Endpoint : $"{(_options.UseSsl ? "https" : "http")}://{_options.Endpoint}";
        return new AmazonS3Client(new BasicAWSCredentials(_options.AccessKey, _options.SecretKey), new AmazonS3Config { ServiceURL = endpoint.TrimEnd('/'), ForcePathStyle = true, AuthenticationRegion = "us-east-1" });
    }
    public async Task<Anh02ArtifactMetadata> WriteAsync(string key, Stream content, long? sizeBytes, string mediaType, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await using var spool = Temporary();
        var before = await CopyHashAsync(content, spool, cancellationToken);
        if (before.Size <= 0 || sizeBytes.HasValue && before.Size != sizeBytes.Value) throw new IOException("Artifact size mismatch.");
        spool.Position = 0;
        try
        {
            using var client = Client();
            try
            {
                var request = new PutObjectRequest { BucketName = _options.BucketName, Key = key, InputStream = spool, ContentType = mediaType, AutoCloseStream = false, IfNoneMatch = "*" };
                request.Metadata["anh02-sha256"] = before.Hash;
                request.Metadata["anh02-size"] = before.Size.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await client.PutObjectAsync(request, cancellationToken);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                // Another lease can finish this same immutable key. Never overwrite a previously durable object.
            }
            await using var actual = await OpenReadAsync(key, cancellationToken);
            if (actual.Metadata.Sha256 != before.Hash || actual.Metadata.SizeBytes != before.Size || actual.Metadata.MediaType != mediaType) throw new IOException("Durable artifact verification failed.");
            return actual.Metadata;
        }
        catch (Exception ex) when (ex is AmazonS3Exception or HttpRequestException)
        { throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Artifact storage is unavailable.", ex); }
    }
    public async Task<Anh02ArtifactRead> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        FileStream? spool = null;
        try
        {
            using var client = Client();
            using var response = await client.GetObjectAsync(_options.BucketName, key, cancellationToken);
            spool = Temporary();
            var actual = await CopyHashAsync(response.ResponseStream, spool, cancellationToken);
            // These values were atomically written with the object, so a crash before the SQL attach cannot erase the expected hash.
            if (response.Metadata["anh02-sha256"] != actual.Hash || response.Metadata["anh02-size"] != actual.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)) throw new IOException("Generated artifact durable metadata does not match actual bytes.");
            spool.Position = 0;
            return new(spool, new(key, actual.Size, actual.Hash, response.Headers.ContentType));
        }
        catch (Exception ex)
        {
            if (spool is not null) await spool.DisposeAsync();
            if (ex is AmazonS3Exception s3 && (s3.StatusCode == System.Net.HttpStatusCode.NotFound || s3.ErrorCode is "NoSuchKey" or "NotFound")) throw new FileNotFoundException("Generated artifact is absent.", key, ex);
            if (ex is AmazonS3Exception or HttpRequestException) throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Artifact storage is unavailable.", ex);
            throw;
        }
    }
    public static FileStream Temporary() => new(Path.Combine(Path.GetTempPath(), "roadguard-anh02-" + Guid.NewGuid().ToString("N")), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
    public static async Task<(long Size, string Hash)> CopyHashAsync(Stream input, Stream output, CancellationToken ct)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920]; long size = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, ct); if (read == 0) break;
            size = checked(size + read); hash.AppendData(buffer, 0, read); await output.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        return (size, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }
    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 512 || !key.StartsWith("anh02/", StringComparison.Ordinal) || key.Split('/').Any(s => s.Length == 0 || s is "." or ".." || s.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.')))) throw new ArgumentException("Invalid generated artifact key.", nameof(key));
    }
}
