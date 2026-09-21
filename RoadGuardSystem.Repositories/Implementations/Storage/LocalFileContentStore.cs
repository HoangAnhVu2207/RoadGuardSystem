using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.Repositories.Options;

namespace RoadGuardSystem.Repositories.Storage;

public sealed class LocalFileContentStore : IFileContentStore, IFileContentCleanup
{
    private const int BufferSize = 64 * 1024;
    private readonly string _rootPath;
    private readonly string _objectPath;
    private readonly string _temporaryPath;
    private readonly long _maximumSizeBytes;
    private readonly Func<string> _objectKeyFactory;

    public LocalFileContentStore(FileStorageOptions options)
        : this(options, () => Guid.NewGuid().ToString("N"))
    {
    }

    internal LocalFileContentStore(FileStorageOptions options, Func<string> objectKeyFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(objectKeyFactory);
        if (string.IsNullOrWhiteSpace(options.RootPath) || !Path.IsPathRooted(options.RootPath))
        {
            throw new FileStorageException(
                FileStorageErrorCodes.PathInvalid,
                "File storage root must be an absolute configured path.");
        }

        if (options.MaximumSizeBytes <= 0 || options.MaximumSizeBytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"Maximum file size must be between 1 and {int.MaxValue} bytes.");
        }

        _rootPath = Path.GetFullPath(options.RootPath);
        _objectPath = Path.Combine(_rootPath, "objects");
        _temporaryPath = Path.Combine(_rootPath, ".tmp");
        _maximumSizeBytes = options.MaximumSizeBytes;
        _objectKeyFactory = objectKeyFactory;

        try
        {
            RejectReparsePointIfPresent(_rootPath, "configured storage root");
            Directory.CreateDirectory(_rootPath);
            Directory.CreateDirectory(_objectPath);
            Directory.CreateDirectory(_temporaryPath);
            EnsureStorageDirectoriesSafe();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileStorageException(
                FileStorageErrorCodes.StorageUnavailable,
                "File storage root is unavailable.",
                exception);
        }
    }

    public async Task<StoredContent> StoreAsync(
        Stream content,
        string originalName,
        string declaredMimeType,
        string expectedChecksum,
        long? declaredSizeBytes,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(content, originalName, declaredMimeType, expectedChecksum, declaredSizeBytes);
        EnsureStorageDirectoriesSafe();

        var objectKey = CreateObjectKey();
        var temporaryFile = ResolveContainedPath(_temporaryPath, $"{objectKey}.tmp");
        var finalFile = ResolveContainedPath(_objectPath, objectKey);
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        var prefix = new byte[512];
        var prefixLength = 0;
        long totalBytes = 0;

        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var output = new FileStream(
                             temporaryFile,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             BufferSize,
                             FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough))
            {
                while (true)
                {
                    var read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (read == 0)
                    {
                        break;
                    }

                    totalBytes += read;
                    if (totalBytes > _maximumSizeBytes)
                    {
                        throw new FileStorageException(FileStorageErrorCodes.TooLarge, "File exceeds configured size limit.");
                    }

                    if (prefixLength < prefix.Length)
                    {
                        var take = Math.Min(read, prefix.Length - prefixLength);
                        buffer.AsSpan(0, take).CopyTo(prefix.AsSpan(prefixLength));
                        prefixLength += take;
                    }

                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                await output.FlushAsync(cancellationToken);
            }

            if (totalBytes == 0)
            {
                throw new FileStorageException(FileStorageErrorCodes.Empty, "Empty file content is not accepted.");
            }

            if (declaredSizeBytes.HasValue && declaredSizeBytes.Value != totalBytes)
            {
                throw new FileStorageException(FileStorageErrorCodes.SizeMismatch, "Declared file size does not match received content.");
            }

            var checksum = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(checksum),
                    Convert.FromHexString(expectedChecksum)))
            {
                throw new FileStorageException(FileStorageErrorCodes.ChecksumMismatch, "File checksum does not match received content.");
            }

            var observedMimeType = DetectMimeType(prefix.AsSpan(0, prefixLength));
            if (!string.Equals(observedMimeType, declaredMimeType, StringComparison.OrdinalIgnoreCase))
            {
                throw new FileStorageException(FileStorageErrorCodes.MimeMismatch, "Declared MIME type does not match received content.");
            }

            EnsureStorageDirectoriesSafe();
            File.Move(temporaryFile, finalFile, overwrite: false);
            return new StoredContent(objectKey, originalName, observedMimeType, checked((int)totalBytes), checksum);
        }
        catch (OperationCanceledException)
        {
            DeleteTemporaryFile(temporaryFile);
            throw;
        }
        catch (FileStorageException)
        {
            DeleteTemporaryFile(temporaryFile);
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            DeleteTemporaryFile(temporaryFile);
            throw new FileStorageException(
                FileStorageErrorCodes.StorageUnavailable,
                "File storage operation failed.",
                exception);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public Task<Stream> OpenReadAsync(string storageUri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveObjectPath(storageUri);
        try
        {
            Stream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Task.FromResult(stream);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileStorageException(
                FileStorageErrorCodes.StorageUnavailable,
                "Stored content is unavailable.",
                exception);
        }
    }

    Task IFileContentCleanup.DeleteAsync(string storageUri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveObjectPath(storageUri);
        try
        {
            File.Delete(path);
            return Task.CompletedTask;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileStorageException(
                FileStorageErrorCodes.StorageUnavailable,
                "Stored content could not be cleaned up.",
                exception);
        }
    }

    private void ValidateRequest(
        Stream content,
        string originalName,
        string declaredMimeType,
        string expectedChecksum,
        long? declaredSizeBytes)
    {
        if (content is null || !content.CanRead)
        {
            throw new FileStorageException(
                FileStorageErrorCodes.ContentInvalid,
                "File content must be a readable stream.");
        }

        if (string.IsNullOrWhiteSpace(originalName) ||
            originalName.Length > 255 ||
            Path.IsPathRooted(originalName) ||
            originalName is "." or ".." ||
            originalName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0 ||
            originalName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new FileStorageException(FileStorageErrorCodes.PathInvalid, "Original file name is invalid.");
        }

        if (string.IsNullOrWhiteSpace(declaredMimeType) || declaredMimeType.Length > 120)
        {
            throw new FileStorageException(FileStorageErrorCodes.MimeMismatch, "Declared MIME type is invalid.");
        }

        if (expectedChecksum is null || expectedChecksum.Length != 64 || expectedChecksum.Any(character =>
                character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
        {
            throw new FileStorageException(FileStorageErrorCodes.ChecksumMismatch, "Expected checksum must be lowercase SHA-256 hexadecimal.");
        }

        if (declaredSizeBytes is <= 0)
        {
            throw new FileStorageException(FileStorageErrorCodes.SizeMismatch, "Declared size must be positive when supplied.");
        }

        if (declaredSizeBytes > _maximumSizeBytes)
        {
            throw new FileStorageException(FileStorageErrorCodes.TooLarge, "Declared file size exceeds configured limit.");
        }
    }

    private string CreateObjectKey()
    {
        var objectKey = _objectKeyFactory();
        if (!IsOpaqueObjectKey(objectKey))
        {
            throw new FileStorageException(FileStorageErrorCodes.PathInvalid, "Generated object key is invalid.");
        }

        return objectKey;
    }

    private string ResolveObjectPath(string storageUri)
    {
        EnsureStorageDirectoriesSafe();
        if (!IsOpaqueObjectKey(storageUri))
        {
            throw new FileStorageException(FileStorageErrorCodes.PathInvalid, "Storage identity is invalid.");
        }

        var path = ResolveContainedPath(_objectPath, storageUri);
        if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new FileStorageException(
                FileStorageErrorCodes.PathInvalid,
                "Symbolic links and other reparse points are not valid stored objects.");
        }

        return path;
    }

    private static bool IsOpaqueObjectKey(string? value)
        => value is { Length: 32 } && value.All(character =>
            character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));

    private static string ResolveContainedPath(string parent, string name)
    {
        var parentPath = Path.GetFullPath(parent) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(parentPath, name));
        if (!candidate.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new FileStorageException(FileStorageErrorCodes.PathInvalid, "Storage path escaped the configured root.");
        }

        return candidate;
    }

    private void EnsureStorageDirectoriesSafe()
    {
        EnsureDirectoryIsSafe(_rootPath, "configured storage root");
        EnsureDirectoryIsSafe(_objectPath, "object directory");
        EnsureDirectoryIsSafe(_temporaryPath, "temporary directory");
    }

    private static void EnsureDirectoryIsSafe(string path, string description)
    {
        if (!Directory.Exists(path))
        {
            throw new FileStorageException(
                FileStorageErrorCodes.StorageUnavailable,
                $"The {description} is unavailable.");
        }

        RejectReparsePointIfPresent(path, description);
    }

    private static void RejectReparsePointIfPresent(string path, string description)
    {
        if (Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new FileStorageException(
                FileStorageErrorCodes.PathInvalid,
                $"The {description} must not be a symbolic link, junction, or other reparse point.");
        }
    }

    private static string DetectMimeType(ReadOnlySpan<byte> prefix)
    {
        if (prefix.StartsWith("%PDF-"u8))
        {
            return "application/pdf";
        }

        if (prefix.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "image/png";
        }

        if (prefix.Length >= 3 && prefix[0] == 0xFF && prefix[1] == 0xD8 && prefix[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (prefix.Length >= 8 && prefix.Slice(4, 4).SequenceEqual("ftyp"u8))
        {
            return "video/mp4";
        }

        if (IsUtf8Text(prefix))
        {
            return "text/plain";
        }

        return "application/octet-stream";
    }

    private static bool IsUtf8Text(ReadOnlySpan<byte> content)
    {
        if (content.IsEmpty || content.Contains((byte)0))
        {
            return false;
        }

        try
        {
            var text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(content);
            return text.All(character => !char.IsControl(character) || character is '\t' or '\r' or '\n');
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private void DeleteTemporaryFile(string path)
    {
        EnsureStorageDirectoriesSafe();
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new FileStorageException(
                FileStorageErrorCodes.StorageUnavailable,
                "Incomplete temporary content could not be cleaned up.",
                exception);
        }
    }
}
