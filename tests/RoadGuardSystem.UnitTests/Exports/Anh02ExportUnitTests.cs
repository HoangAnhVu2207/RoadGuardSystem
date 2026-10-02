using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PdfSharp.Pdf.IO;
using RoadGuardSystem.BusinessObjects.Exports;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.DTOs.Reporting;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.UnitTests.Exports;

public sealed class Anh02ExportUnitTests
{
    [Theory]
    [InlineData("TRAINING", "PDF", false)]
    [InlineData("DOSSIER", "PDF", true)]
    [InlineData("UNKNOWN", "ZIP", false)]
    [InlineData("DOSSIER", "TAR", false)]
    public void Invalid_combinations_reject(string kind, string format, bool originals) => Assert.Null(ExportService.Normalize(new(kind, format, IncludeOriginalFiles: originals)));
    [Fact]
    public void Normalization_makes_set_order_and_dates_equivalent_but_not_filters()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var first = ExportService.Normalize(new("dossier", "zip", [b, a, b], From: new(2026, 1, 1, 7, 0, 0, TimeSpan.FromHours(7))))!;
        var second = ExportService.Normalize(new("DOSSIER", "ZIP", [a, b], From: new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)))!;
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.NotEqual(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second with { IncludeOriginalFiles = true }));
    }
    [Fact]
    public void Lease_expiry_and_completedAt_define_exact_thirty_day_download_boundary()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); var job = ExportJob.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "DOSSIER", "PDF", now);
        var first = Guid.NewGuid(); Assert.True(job.TryClaim(first, now, TimeSpan.FromMinutes(5)));
        Assert.False(job.TryClaim(Guid.NewGuid(), now.AddMinutes(4), TimeSpan.FromMinutes(5)));
        var second = Guid.NewGuid(); Assert.True(job.TryClaim(second, now.AddMinutes(5), TimeSpan.FromMinutes(5)));
        Assert.Throws<InvalidOperationException>(() => job.Complete(first, Guid.NewGuid(), now.AddMinutes(6)));
        job.Complete(second, Guid.NewGuid(), now.AddMinutes(6));
        Assert.Equal(now.AddMinutes(6).AddDays(30), job.ExpiresAt); Assert.False(job.TryClaim(Guid.NewGuid(), now.AddDays(50), TimeSpan.FromMinutes(5)));
    }
    [Fact]
    public void Transient_storage_fault_keeps_same_snapshot_and_respects_configured_backoff()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); var snapshot = Guid.NewGuid();
        var job = ExportJob.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), snapshot, "DOSSIER", "ZIP", now); var lease = Guid.NewGuid();
        Assert.True(job.TryClaim(lease, now, TimeSpan.FromMinutes(5))); job.Fail(lease, now, "export_storage_unavailable", false, TimeSpan.FromSeconds(90));
        Assert.Equal("QUEUED", job.Status); Assert.Equal(snapshot, job.SnapshotId); Assert.Null(job.CompletedAt); Assert.Null(job.ExpiresAt);
        Assert.False(job.TryClaim(Guid.NewGuid(), now.AddSeconds(89), TimeSpan.FromMinutes(5)));
        Assert.True(job.TryClaim(Guid.NewGuid(), now.AddSeconds(90), TimeSpan.FromMinutes(5)));
    }
    [Theory]
    [InlineData("../private.jpg")]
    [InlineData("/private.jpg")]
    [InlineData("images\\private.jpg")]
    [InlineData("C:/private.jpg")]
    [InlineData("images//private.jpg")]
    public void Unsafe_archive_names_reject(string name) => Assert.Throws<ExportRenderException>(() => ExportArchive.ValidatePath(name));
    [Fact]
    public async Task Streaming_zip_manifest_hash_and_recovery_proof_use_actual_bytes()
    {
        const int size = 4 * 1024 * 1024; var fileId = Guid.NewGuid(); var hash = HashRepeated(size); var read = new RepeatingStream(size);
        var payload = Payload("TRAINING", "ZIP", [new(fileId, "v1", hash, size, "image/jpeg", ExportArchive.FilePath(fileId, "image/jpeg"), true, null)]);
        var renewals = 0;
        await using var output = await Renderer().RenderAsync(payload, (_, _) => Task.FromResult<Stream>(read), _ => { renewals++; return Task.CompletedTask; }, default);
        Assert.True(read.MaxRead <= 81920); Assert.True(renewals > 0); Assert.True(await ExportArchive.ProvesSnapshotAsync(output, payload.Manifest, default));
        using var zip = new ZipArchive(output, ZipArchiveMode.Read, true);
        Assert.NotNull(zip.GetEntry("manifest.json")); Assert.NotNull(zip.GetEntry("labels.jsonl")); Assert.Equal(size, zip.GetEntry(payload.Manifest.Files[0].ArchivePath!)!.Length);
        Assert.DoesNotContain(zip.Entries, x => x.FullName.Contains("private", StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task Source_checksum_mismatch_fails_instead_of_partial_success()
    {
        var payload = Payload("TRAINING", "ZIP", [new(Guid.NewGuid(), "v1", new string('a', 64), 100, "image/jpeg", "images/safe.jpg", true, null)]);
        var error = await Assert.ThrowsAsync<ExportRenderException>(() => Renderer().RenderAsync(payload, (_, _) => Task.FromResult<Stream>(new RepeatingStream(100)), _ => Task.CompletedTask, default));
        Assert.Equal("export_source_unavailable", error.Code);
    }
    [Fact]
    public void Snapshot_retains_admitted_facts_and_detects_mutated_canonical_bytes()
    {
        var payload = Payload("DOSSIER", "PDF", []); var bytes = JsonSerializer.Serialize(payload, ExportSerialization.Options);
        var snapshot = ExportSnapshot.Create(payload.Manifest.SnapshotId, payload.Manifest.ProjectId, bytes, ExportSerialization.Hash(bytes), payload.Manifest.SnapshotAt);
        Assert.Equal(payload.Manifest.SnapshotId, ExportSerialization.Read(snapshot).Manifest.SnapshotId);
        var corrupt = ExportSnapshot.Create(payload.Manifest.SnapshotId, payload.Manifest.ProjectId, bytes.Replace("DOSSIER", "TRAINING", StringComparison.Ordinal), snapshot.Hash, payload.Manifest.SnapshotAt);
        Assert.Throws<InvalidDataException>(() => ExportSerialization.Read(corrupt));
    }
    [Fact]
    public async Task Unicode_font_embedded_pdf_has_pages_and_recovery_identity()
    {
        var payload = Payload("DOSSIER", "PDF", []);
        var sources = Enumerable.Range(0, 100).Select(i => new ExportSourceRevisionDto("Dữ liệu đường", Guid.NewGuid(), "phiên bản " + i)).ToArray();
        payload = payload with { Manifest = payload.Manifest with { SourceRevisions = sources } };
        await using var rendered = await Renderer().RenderAsync(payload, (_, _) => throw new InvalidOperationException("No originals selected."), _ => Task.CompletedTask, default);
        Assert.True(await ExportArchive.ProvesSnapshotAsync(rendered, payload.Manifest, default));
        using var pdf = PdfReader.Open(rendered, PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 1); Assert.Equal("Hồ sơ RoadGuard", pdf.Info.Title);
        var path = Path.Combine(Path.GetTempPath(), "roadguard-anh02-unicode-sample.pdf"); rendered.Position = 0;
        await using var target = File.Create(path); await rendered.CopyToAsync(target);
    }
    private static ExportRenderer Renderer() => new(Options.Create(new ExportOptions { UnicodeFontPath = Environment.GetEnvironmentVariable("ANH02_TEST_FONT_PATH") ?? "C:/Windows/Fonts/arial.ttf" }));
    internal static ExportSnapshotPayloadDto Payload(string kind, string format, ExportFileDto[] files)
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var manifest = new ExportManifestV1Dto("anh02.export.v1", Guid.NewGuid(), Guid.NewGuid(), kind, format, Guid.NewGuid(), now, now, ["fixture.v1"], new(kind, format), [], files, [new("reporterEvidence", "UNAVAILABLE", ["REPORTER_EVIDENCE_ACCESS_NOT_AVAILABLE"])], null, new string('a', 64));
        return new(manifest, null);
    }
    private static string HashRepeated(long length)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var buffer = new byte[81920]; Array.Fill(buffer, (byte)23);
        while (length > 0) { var count = (int)Math.Min(length, buffer.Length); hash.AppendData(buffer, 0, count); length -= count; }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
    private sealed class RepeatingStream(long remaining) : Stream
    {
        public int MaxRead { get; private set; }
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException(); public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) { MaxRead = Math.Max(MaxRead, count); var take = (int)Math.Min(remaining, count); Array.Fill(buffer, (byte)23, offset, take); remaining -= take; return take; }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) { MaxRead = Math.Max(MaxRead, buffer.Length); var take = (int)Math.Min(remaining, buffer.Length); buffer.Span[..take].Fill(23); remaining -= take; return ValueTask.FromResult(take); }
        public override void Flush() => throw new NotSupportedException(); public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(); public override void SetLength(long value) => throw new NotSupportedException(); public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
