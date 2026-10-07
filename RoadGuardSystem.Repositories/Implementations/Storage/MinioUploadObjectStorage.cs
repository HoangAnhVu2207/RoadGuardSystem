using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RoadGuardSystem.Repositories.Options;

namespace RoadGuardSystem.Repositories.Storage;

public sealed class MinioUploadObjectStorage : IUploadObjectStorage, IMultipartRecoveryStorage
{
    private const int PrefixLength = 512;
    private const int BufferSize = 81920;
    private readonly MinioStorageOptions _options;
    private readonly Func<IAmazonS3>? _clientFactory;

    public MinioUploadObjectStorage(IOptions<MinioStorageOptions> options)
    {
        _options = options.Value;
    }

    public MinioUploadObjectStorage(IOptions<MinioStorageOptions> options, Func<IAmazonS3> clientFactory)
        : this(options) => _clientFactory = clientFactory;

    private IAmazonS3 CreateClient(bool initiation = false)
    {
        if (_clientFactory is not null) return _clientFactory();
        ValidateOptions(_options);
        return new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKey, _options.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = NormalizeEndpoint(_options.Endpoint, _options.UseSsl),
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1",
                MaxErrorRetry = initiation ? 0 : 2
            });
    }

    public async Task<string> InitiateAsync(string objectKey, string mediaType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateClient(initiation: true);
            var response = await client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                ContentType = mediaType
            }, cancellationToken);
            return response.UploadId;
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException)
        {
            throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Object storage is unavailable.", exception);
        }
    }

    public async Task<IReadOnlyList<string>> ListMultipartIdsAsync(string exactObjectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateClient();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            string? keyMarker = null, idMarker = null;
            do
            {
                var page = await client.ListMultipartUploadsAsync(new ListMultipartUploadsRequest
                {
                    BucketName = _options.BucketName,
                    Prefix = exactObjectKey,
                    MaxUploads = 1000,
                    KeyMarker = keyMarker,
                    UploadIdMarker = idMarker
                }, cancellationToken);
                foreach (var upload in page.MultipartUploads ?? [])
                    if (string.Equals(upload.Key, exactObjectKey, StringComparison.Ordinal)) ids.Add(upload.UploadId);
                if (page.IsTruncated != true) break;
                if (page.NextKeyMarker == keyMarker && page.NextUploadIdMarker == idMarker)
                    throw new IOException("Multipart pagination did not advance.");
                keyMarker = page.NextKeyMarker; idMarker = page.NextUploadIdMarker;
            } while (true);
            return ids.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }
        catch (Exception e) when (e is AmazonS3Exception or HttpRequestException or IOException)
        { throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Multipart inventory unavailable.", e); }
    }

    public async Task<bool> HasPartsAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateClient();
            string? marker = null;
            do
            {
                var page = await client.ListPartsAsync(new ListPartsRequest
                { BucketName = _options.BucketName, Key = objectKey, UploadId = uploadId, PartNumberMarker = marker, MaxParts = 1000 }, cancellationToken);
                if (page.Parts?.Count > 0) return true;
                if (page.IsTruncated != true) return false;
                var next = page.NextPartNumberMarker?.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (next is null || next == marker) throw new IOException("Part pagination did not advance.");
                marker = next;
            } while (true);
        }
        catch (Exception e) when (e is AmazonS3Exception or HttpRequestException or IOException)
        { throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Multipart parts unavailable.", e); }
    }

    public async Task AbortMultipartAsync(string objectKey, string uploadId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = CreateClient();
            await client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
            { BucketName = _options.BucketName, Key = objectKey, UploadId = uploadId }, cancellationToken);
        }
        catch (AmazonS3Exception e) when (e.ErrorCode == "NoSuchUpload") { }
        catch (Exception e) when (e is AmazonS3Exception or HttpRequestException or IOException)
        { throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Multipart abort unavailable.", e); }
    }

    public async Task<IReadOnlyList<PresignedUploadPart>> PresignPartsAsync(
        string objectKey,
        string uploadId,
        IReadOnlyList<int> partNumbers,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var client = CreateClient();
            var result = new List<PresignedUploadPart>(partNumbers.Count);
            foreach (var partNumber in partNumbers)
            {
                var url = await client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
                {
                    BucketName = _options.BucketName,
                    Key = objectKey,
                    Verb = HttpVerb.PUT,
                    Expires = expiresAt.UtcDateTime,
                    Protocol = _options.UseSsl ? Protocol.HTTPS : Protocol.HTTP,
                    PartNumber = partNumber,
                    UploadId = uploadId
                });
                result.Add(new PresignedUploadPart(partNumber, url, expiresAt));
            }

            return result;
        }
        catch (AmazonS3Exception exception)
        {
            throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Object storage is unavailable.", exception);
        }
    }

    public async Task<UploadObjectVerification> CompleteAndVerifyAsync(
        string objectKey,
        string uploadId,
        IReadOnlyList<CompletedStoragePart> parts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await CompleteAndReadAsync(objectKey, uploadId, parts, cancellationToken);
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException)
        {
            throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Object storage is unavailable.", exception);
        }
    }

    private async Task<UploadObjectVerification> CompleteAndReadAsync(
        string objectKey, string uploadId, IReadOnlyList<CompletedStoragePart> parts,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateClient();
            await client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                UploadId = uploadId,
                PartETags = parts.OrderBy(part => part.PartNumber)
                    .Select(part => new PartETag(part.PartNumber, part.ETag))
                    .ToList()
            }, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (string.Equals(exception.ErrorCode, "NoSuchUpload", StringComparison.Ordinal))
        {
            using var client = CreateClient();
            await client.GetObjectMetadataAsync(_options.BucketName, objectKey, cancellationToken);
        }
        catch (AmazonS3Exception exception)
        {
            throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Object storage is unavailable.", exception);
        }

        using var readerClient = CreateClient();
        using var response = await readerClient.GetObjectAsync(_options.BucketName, objectKey, cancellationToken);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var prefix = new byte[PrefixLength];
        var prefixLength = 0;
        var buffer = new byte[BufferSize];
        long totalBytes = 0;
        while (true)
        {
            var read = await response.ResponseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalBytes += read;
            if (prefixLength < prefix.Length)
            {
                var take = Math.Min(read, prefix.Length - prefixLength);
                buffer.AsSpan(0, take).CopyTo(prefix.AsSpan(prefixLength));
                prefixLength += take;
            }

            hash.AppendData(buffer, 0, read);
        }

        return new UploadObjectVerification(
            totalBytes,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
            DetectMimeType(prefix.AsSpan(0, prefixLength)));
    }

    public async Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetObjectAsync(_options.BucketName, objectKey, cancellationToken);
            return new S3ResponseStream(response.ResponseStream, response, client);
        }
        catch (AmazonS3Exception exception)
        {
            throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "Object storage is unavailable.", exception);
        }
    }

    private static void ValidateOptions(MinioStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint) ||
            string.IsNullOrWhiteSpace(options.BucketName) ||
            string.IsNullOrWhiteSpace(options.AccessKey) ||
            string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new FileStorageException(FileStorageErrorCodes.StorageUnavailable, "MinIO storage is not configured.");
        }
    }

    private static string NormalizeEndpoint(string endpoint, bool useSsl)
    {
        var trimmed = endpoint.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = $"{(useSsl ? "https" : "http")}://{trimmed}";
        }

        return trimmed.TrimEnd('/');
    }

    private static string DetectMimeType(ReadOnlySpan<byte> prefix)
    {
        if (prefix.StartsWith("%PDF-"u8)) return "application/pdf";
        if (prefix.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (prefix.Length >= 3 && prefix[0] == 0xFF && prefix[1] == 0xD8 && prefix[2] == 0xFF) return "image/jpeg";
        if (prefix.Length >= 8 && prefix.Slice(4, 4).SequenceEqual("ftyp"u8)) return "video/mp4";
        var text = Encoding.UTF8.GetString(prefix).TrimStart('\uFEFF');
        if (Regex.IsMatch(text, @"\A\s*\d+\r?\n\d{2}:\d{2}:\d{2},\d{3} --> \d{2}:\d{2}:\d{2},\d{3}(?:\r?\n)", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
            return "application/x-subrip";
        return "application/octet-stream";
    }
}
