using System.Security.Cryptography;
using FluentAssertions;
using RoadGuardSystem.Repositories.Options;
using RoadGuardSystem.Repositories.Storage;
using Xunit;

namespace RoadGuardSystem.IntegrationTests.Files;

[Trait("TaskId", "P2-04")]
public sealed class LocalFileContentStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"roadguard-p204-{Guid.NewGuid():N}");

    [Fact]
    public async Task StoreAsync_TraversalName_IsRejectedWithoutWritingContent()
    {
        var store = CreateStore();
        var bytes = "%PDF-1.7 test"u8.ToArray();
        var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        var invoke = () => store.StoreAsync(
            new MemoryStream(bytes),
            "../escape.pdf",
            "application/pdf",
            checksum,
            bytes.Length);

        var exception = await invoke.Should().ThrowAsync<FileStorageException>();
        exception.Which.Code.Should().Be(FileStorageErrorCodes.PathInvalid);
        Directory.Exists(_root).Should().BeTrue();
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task OpenReadAsync_SymbolicLinkToOutsideRoot_IsRejected()
    {
        var store = CreateStore();
        var outside = Path.Combine(Path.GetTempPath(), $"roadguard-p204-outside-{Guid.NewGuid():N}.pdf");
        var key = new string('a', 32);
        var link = Path.Combine(_root, "objects", key);
        await File.WriteAllBytesAsync(outside, "%PDF-1.7 outside"u8.ToArray());
        File.CreateSymbolicLink(link, outside);

        Stream? escapedStream = null;
        FileStorageException? rejection = null;
        try
        {
            try
            {
                escapedStream = await store.OpenReadAsync(key);
            }
            catch (FileStorageException exception)
            {
                rejection = exception;
            }
        }
        finally
        {
            if (escapedStream is not null)
            {
                await escapedStream.DisposeAsync();
            }
            File.Delete(link);
            File.Delete(outside);
        }

        rejection.Should().NotBeNull("a symbolic link must not bypass the configured root");
        rejection!.Code.Should().Be(FileStorageErrorCodes.PathInvalid);
    }

    [Fact]
    public void Constructor_ConfiguredRootSymbolicLink_IsRejected()
    {
        var outside = CreateOutsideDirectory();
        Directory.CreateSymbolicLink(_root, outside);
        try
        {
            var create = () => CreateStore();

            create.Should().Throw<FileStorageException>()
                .Which.Code.Should().Be(FileStorageErrorCodes.PathInvalid);
            Directory.EnumerateFileSystemEntries(outside).Should().BeEmpty();
        }
        finally
        {
            DeleteDirectoryLink(_root);
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public async Task StoreAsync_TemporaryDirectorySymbolicLink_IsRejectedBeforeOutsideWrite()
    {
        var store = CreateStore();
        var outside = CreateOutsideDirectory();
        var temporaryDirectory = Path.Combine(_root, ".tmp");
        ReplaceDirectoryWithLink(temporaryDirectory, outside);
        var bytes = "%PDF-1.7 temporary parent"u8.ToArray();
        try
        {
            var storeFile = () => store.StoreAsync(
                new MemoryStream(bytes), "evidence.pdf", "application/pdf", Hash(bytes), bytes.Length);

            var exception = await storeFile.Should().ThrowAsync<FileStorageException>();
            exception.Which.Code.Should().Be(FileStorageErrorCodes.PathInvalid);
            Directory.EnumerateFileSystemEntries(outside).Should().BeEmpty();
        }
        finally
        {
            DeleteDirectoryLink(temporaryDirectory);
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public async Task StoreAsync_ObjectDirectorySymbolicLink_IsRejectedBeforeOutsideWrite()
    {
        var store = CreateStore();
        var outside = CreateOutsideDirectory();
        var objectDirectory = Path.Combine(_root, "objects");
        ReplaceDirectoryWithLink(objectDirectory, outside);
        var bytes = "%PDF-1.7 object parent"u8.ToArray();
        try
        {
            var storeFile = () => store.StoreAsync(
                new MemoryStream(bytes), "evidence.pdf", "application/pdf", Hash(bytes), bytes.Length);

            var exception = await storeFile.Should().ThrowAsync<FileStorageException>();
            exception.Which.Code.Should().Be(FileStorageErrorCodes.PathInvalid);
            Directory.EnumerateFileSystemEntries(outside).Should().BeEmpty();
        }
        finally
        {
            DeleteDirectoryLink(objectDirectory);
            Directory.Delete(outside, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAndCleanup_ObjectDirectorySymbolicLink_CannotTouchOutsideFile()
    {
        var store = CreateStore();
        var cleanup = (IFileContentCleanup)store;
        var outside = CreateOutsideDirectory();
        var objectDirectory = Path.Combine(_root, "objects");
        var key = new string('c', 32);
        var outsideFile = Path.Combine(outside, key);
        await File.WriteAllBytesAsync(outsideFile, "%PDF-1.7 outside parent"u8.ToArray());
        ReplaceDirectoryWithLink(objectDirectory, outside);
        try
        {
            var read = async () =>
            {
                await using var stream = await store.OpenReadAsync(key);
            };
            var remove = () => cleanup.DeleteAsync(key);

            var readFailure = await read.Should().ThrowAsync<FileStorageException>();
            readFailure.Which.Code.Should().Be(FileStorageErrorCodes.PathInvalid);
            var cleanupFailure = await remove.Should().ThrowAsync<FileStorageException>();
            cleanupFailure.Which.Code.Should().Be(FileStorageErrorCodes.PathInvalid);
            File.Exists(outsideFile).Should().BeTrue();
        }
        finally
        {
            DeleteDirectoryLink(objectDirectory);
            Directory.Delete(outside, recursive: true);
        }
    }

    [Theory]
    [InlineData("checksum", "application/pdf", 1024, "file_checksum_mismatch")]
    [InlineData("valid", "image/png", 1024, "file_mime_mismatch")]
    [InlineData("valid", "application/pdf", 8, "file_too_large")]
    public async Task StoreAsync_InvalidIntegrityInput_CleansTemporaryContent(
        string checksumMode,
        string mimeType,
        long maximumSize,
        string expectedCode)
    {
        var store = CreateStore(maximumSize);
        var bytes = "%PDF-1.7 test payload"u8.ToArray();
        var checksum = checksumMode == "valid"
            ? Hash(bytes)
            : new string('0', 64);

        var storeFile = () => store.StoreAsync(
            new MemoryStream(bytes),
            "evidence.pdf",
            mimeType,
            checksum,
            bytes.Length);

        var exception = await storeFile.Should().ThrowAsync<FileStorageException>();
        exception.Which.Code.Should().Be(expectedCode);
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAsync_Cancellation_CleansTemporaryContent()
    {
        var store = CreateStore();
        var bytes = "%PDF-1.7 cancelled"u8.ToArray();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var storeFile = () => store.StoreAsync(
            new MemoryStream(bytes),
            "evidence.pdf",
            "application/pdf",
            Hash(bytes),
            bytes.Length,
            cancellation.Token);

        await storeFile.Should().ThrowAsync<OperationCanceledException>();
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAsync_MidStreamCancellation_CleansPartialTemporaryContent()
    {
        var store = CreateStore(maximumSizeBytes: 256 * 1024);
        var bytes = PdfBytes(130 * 1024);
        using var cancellation = new CancellationTokenSource();
        await using var stream = new CancelAfterFirstReadStream(bytes, cancellation);

        var storeFile = () => store.StoreAsync(
            stream,
            "evidence.pdf",
            "application/pdf",
            Hash(bytes),
            bytes.Length,
            cancellation.Token);

        await storeFile.Should().ThrowAsync<OperationCanceledException>();
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAsync_EmptyAndDeclaredSizeMismatch_AreRejectedWithoutContent()
    {
        var store = CreateStore();
        var empty = Array.Empty<byte>();
        var emptyStore = () => store.StoreAsync(
            new MemoryStream(empty), "empty.pdf", "application/pdf", Hash(empty), null);
        var emptyFailure = await emptyStore.Should().ThrowAsync<FileStorageException>();
        emptyFailure.Which.Code.Should().Be(FileStorageErrorCodes.Empty);

        var bytes = "%PDF-1.7 size"u8.ToArray();
        var mismatch = () => store.StoreAsync(
            new MemoryStream(bytes), "size.pdf", "application/pdf", Hash(bytes), bytes.Length + 1L);
        var mismatchFailure = await mismatch.Should().ThrowAsync<FileStorageException>();
        mismatchFailure.Which.Code.Should().Be(FileStorageErrorCodes.SizeMismatch);
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public void Constructor_RootOccupiedByFile_ReturnsStorageUnavailable()
    {
        var rootAsFile = Path.Combine(Path.GetTempPath(), $"roadguard-p204-file-{Guid.NewGuid():N}");
        File.WriteAllText(rootAsFile, "occupied");
        try
        {
            var create = () => new LocalFileContentStore(new FileStorageOptions
            {
                RootPath = rootAsFile,
                MaximumSizeBytes = 1024
            });

            create.Should().Throw<FileStorageException>()
                .Which.Code.Should().Be(FileStorageErrorCodes.StorageUnavailable);
        }
        finally
        {
            File.Delete(rootAsFile);
        }
    }

    [Fact]
    public async Task StoreAsync_NullOrUnreadableStream_ReturnsStableContentError()
    {
        var store = CreateStore();
        var nullStore = () => store.StoreAsync(
            null!, "evidence.pdf", "application/pdf", new string('a', 64), 1);
        var nullFailure = await nullStore.Should().ThrowAsync<FileStorageException>();
        nullFailure.Which.Code.Should().Be("file_content_invalid");

        var disposed = new MemoryStream([1]);
        disposed.Dispose();
        var unreadableStore = () => store.StoreAsync(
            disposed, "evidence.pdf", "application/pdf", new string('a', 64), 1);
        var unreadableFailure = await unreadableStore.Should().ThrowAsync<FileStorageException>();
        unreadableFailure.Which.Code.Should().Be("file_content_invalid");
        Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAsync_Collision_DoesNotOverwriteCommittedBytes()
    {
        var key = new string('b', 32);
        var store = new LocalFileContentStore(
            new FileStorageOptions { RootPath = _root, MaximumSizeBytes = 1024 },
            () => key);
        var original = "%PDF-1.7 original"u8.ToArray();
        var replacement = "%PDF-1.7 replacement"u8.ToArray();
        await store.StoreAsync(
            new MemoryStream(original), "original.pdf", "application/pdf", Hash(original), original.Length);

        var collide = () => store.StoreAsync(
            new MemoryStream(replacement), "replacement.pdf", "application/pdf", Hash(replacement), replacement.Length);

        var exception = await collide.Should().ThrowAsync<FileStorageException>();
        exception.Which.Code.Should().Be(FileStorageErrorCodes.StorageUnavailable);
        await using var read = await store.OpenReadAsync(key);
        using var copied = new MemoryStream();
        await read.CopyToAsync(copied);
        copied.ToArray().Should().Equal(original);
        Directory.EnumerateFiles(Path.Combine(_root, ".tmp")).Should().BeEmpty();
    }

    [Fact]
    public async Task StoreAsync_ValidPdf_RoundTripsVerifiedMetadataWithBoundedReads()
    {
        var store = CreateStore(maximumSizeBytes: 256 * 1024);
        var bytes = "%PDF-1.7 verified content"u8.ToArray();
        await using var source = new ReadSizeTrackingStream(bytes);

        var stored = await store.StoreAsync(
            source,
            "handover.pdf",
            "application/pdf",
            Hash(bytes),
            bytes.Length);

        stored.StorageUri.Should().MatchRegex("^[0-9a-f]{32}$");
        stored.StorageUri.Should().NotContain(_root);
        stored.OriginalName.Should().Be("handover.pdf");
        stored.MimeType.Should().Be("application/pdf");
        stored.SizeBytes.Should().Be(bytes.Length);
        stored.Checksum.Should().Be(Hash(bytes));
        source.MaximumRequestedBytes.Should().BeLessThanOrEqualTo(64 * 1024);

        await using var read = await store.OpenReadAsync(stored.StorageUri);
        using var copied = new MemoryStream();
        await read.CopyToAsync(copied);
        copied.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public async Task StoreAsync_ValidMp4_UsesServerObservedVideoMime()
    {
        var store = CreateStore();
        var bytes = new byte[]
        {
            0x00, 0x00, 0x00, 0x18,
            0x66, 0x74, 0x79, 0x70,
            0x69, 0x73, 0x6F, 0x6D,
            0x00, 0x00, 0x02, 0x00,
            0x69, 0x73, 0x6F, 0x6D
        };

        var stored = await store.StoreAsync(
            new MemoryStream(bytes),
            "survey.mp4",
            "video/mp4",
            Hash(bytes),
            bytes.Length);

        stored.MimeType.Should().Be("video/mp4");
    }

    [Fact]
    public async Task StoreAsync_ValidSrt_UsesServerObservedTextMime()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("1\r\n00:00:01,000 --> 00:00:02,000\r\nRoad section\r\n");
        var store = CreateStore();

        var stored = await store.StoreAsync(
            new MemoryStream(bytes),
            "flight.srt",
            "text/plain",
            Hash(bytes),
            bytes.Length);

        stored.MimeType.Should().Be("text/plain");
    }

    private LocalFileContentStore CreateStore(long maximumSizeBytes = 1024)
        => new(new FileStorageOptions
        {
            RootPath = _root,
            MaximumSizeBytes = maximumSizeBytes
        });

    private static string CreateOutsideDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"roadguard-p204-link-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void ReplaceDirectoryWithLink(string path, string target)
    {
        Directory.Delete(path);
        Directory.CreateSymbolicLink(path, target);
    }

    private static void DeleteDirectoryLink(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path);
        }
    }

    private static string Hash(byte[] content)
        => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static byte[] PdfBytes(int length)
    {
        var bytes = new byte[length];
        "%PDF-1.7"u8.CopyTo(bytes);
        return bytes;
    }

    private sealed class ReadSizeTrackingStream : MemoryStream
    {
        public ReadSizeTrackingStream(byte[] buffer)
            : base(buffer)
        {
        }

        public int MaximumRequestedBytes { get; private set; }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            MaximumRequestedBytes = Math.Max(MaximumRequestedBytes, buffer.Length);
            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    private sealed class CancelAfterFirstReadStream : MemoryStream
    {
        private readonly CancellationTokenSource _cancellation;
        private bool _hasRead;

        public CancelAfterFirstReadStream(byte[] buffer, CancellationTokenSource cancellation)
            : base(buffer)
        {
            _cancellation = cancellation;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_hasRead)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            _hasRead = true;
            var read = base.ReadAsync(buffer, cancellationToken);
            _cancellation.Cancel();
            return read;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
