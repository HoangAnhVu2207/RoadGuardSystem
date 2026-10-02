using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.Repositories.Exports;
using RoadGuardSystem.Repositories.Storage;

namespace RoadGuardSystem.Services.Exports;

public interface IExportRenderer
{
    Task<Stream> RenderAsync(ExportSnapshotPayloadDto payload, Func<ExportFileDto, CancellationToken, Task<Stream>> openSource, Func<CancellationToken, Task> renew, CancellationToken ct);
}
public sealed class ExportRenderException(string code) : Exception(code) { public string Code { get; } = code; }

public sealed class ExportRenderer : IExportRenderer
{
    private readonly ExportOptions _options;
    private static readonly object FontLock = new();
    public ExportRenderer(IOptions<ExportOptions> options) => _options = options.Value;
    public async Task<Stream> RenderAsync(ExportSnapshotPayloadDto payload, Func<ExportFileDto, CancellationToken, Task<Stream>> openSource, Func<CancellationToken, Task> renew, CancellationToken ct)
    {
        var output = MinioAnh02ArtifactStore.Temporary();
        try
        {
            if (payload.Manifest.Format == "PDF") await RenderPdfAsync(payload, output, ct);
            else
            {
                using var zip = new ZipArchive(output, ZipArchiveMode.Create, true, Encoding.UTF8);
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ZipArchiveEntry Entry(string name)
                {
                    ExportArchive.ValidatePath(name); if (!paths.Add(name)) throw new ExportRenderException("export_archive_invalid");
                    var entry = zip.CreateEntry(name, CompressionLevel.Fastest);
                    entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero); return entry;
                }
                async Task Text(string name, string value)
                {
                    await using var stream = Entry(name).Open(); await stream.WriteAsync(Encoding.UTF8.GetBytes(value), ct);
                }
                await Text("manifest.json", JsonSerializer.Serialize(payload.Manifest, ExportSerialization.Options));
                await Text("evidence-index.json", JsonSerializer.Serialize(payload.Manifest.Files, ExportSerialization.Options));
                if (payload.Manifest.Kind == "DOSSIER")
                {
                    await using var spool = MinioAnh02ArtifactStore.Temporary(); await RenderPdfAsync(payload, spool, ct); spool.Position = 0;
                    await using var pdf = Entry("dossier.pdf").Open(); await spool.CopyToAsync(pdf, 81920, ct);
                }
                else
                {
                    await Text("labels.jsonl", string.Join("\n", (payload.Manifest.Labels ?? []).Select(l => JsonSerializer.Serialize(l, ExportSerialization.Options))) + "\n");
                    await Text("README.txt", "RoadGuard approved training snapshot; anh02.export.v1\nAs of " + payload.Manifest.SnapshotAt.ToString("O") + "\nBounding boxes: normalized x,y,width,height in [0,1]. Source coordinates are not converted to COCO or YOLO. No train/validation/test split inferred. Mode REAL/MOCK/SYNTHETIC remains explicit per label.\n");
                }
                foreach (var file in payload.Manifest.Files.Where(f => f.Included))
                {
                    if (file.ArchivePath is null) throw new ExportRenderException("export_archive_invalid");
                    await renew(ct);
                    await using var original = await openSource(file, ct);
                    await using var destination = Entry(file.ArchivePath).Open();
                    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    var buffer = new byte[81920]; long size = 0; var lastRenewal = System.Diagnostics.Stopwatch.StartNew();
                    while (true)
                    {
                        var read = await original.ReadAsync(buffer, ct); if (read == 0) break;
                        size = checked(size + read); if (size > file.SizeBytes) throw new ExportRenderException("export_source_unavailable");
                        hash.AppendData(buffer, 0, read); await destination.WriteAsync(buffer.AsMemory(0, read), ct);
                        if (lastRenewal.Elapsed > TimeSpan.FromSeconds(30)) { await renew(ct); lastRenewal.Restart(); }
                    }
                    if (size != file.SizeBytes || !string.Equals(Convert.ToHexString(hash.GetHashAndReset()), file.Sha256, StringComparison.OrdinalIgnoreCase)) throw new ExportRenderException("export_source_unavailable");
                }
            }
            output.Position = 0; return output;
        }
        catch { await output.DisposeAsync(); throw; }
    }
    private Task RenderPdfAsync(ExportSnapshotPayloadDto payload, Stream output, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (FontLock)
        {
            if (GlobalFontSettings.FontResolver is null)
            {
                if (!File.Exists(_options.UnicodeFontPath)) throw new ExportRenderException("export_font_unavailable");
                // Deployment supplies a font licensed for embedding. No proprietary font is redistributed by this package.
                GlobalFontSettings.FontResolver = new UnicodeFontResolver(File.ReadAllBytes(_options.UnicodeFontPath));
            }
        }
        using var document = new PdfDocument();
        document.Info.Title = "Hồ sơ RoadGuard";
        document.Info.Subject = "Snapshot " + payload.Manifest.SnapshotHash;
        document.Info.CreationDate = payload.Manifest.SnapshotAt.UtcDateTime; document.Info.ModificationDate = payload.Manifest.SnapshotAt.UtcDateTime;
        var font = new XFont("RoadGuardUnicode", 10, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        XGraphics? graphics = null; double y = 0; var pageNumber = 0;
        void Page()
        {
            graphics?.Dispose(); var page = document.AddPage(); page.Size = PdfSharp.PageSize.A4; pageNumber++;
            graphics = XGraphics.FromPdfPage(page); y = 44;
            graphics.DrawString($"RoadGuard | Trang {pageNumber} | {payload.Manifest.SnapshotAt:yyyy-MM-dd HH:mm} UTC", font, XBrushes.Gray, new XRect(40, page.Height.Point - 35, page.Width.Point - 80, 18), XStringFormats.TopLeft);
        }
        void Line(string text)
        {
            const double width = 515;
            foreach (var paragraph in text.Replace("\r", "", StringComparison.Ordinal).Split('\n'))
            {
                var remaining = paragraph;
                while (remaining.Length > 0)
                {
                    var length = Math.Min(remaining.Length, 110);
                    while (length > 1 && graphics!.MeasureString(remaining[..length], font).Width > width) length--;
                    if (length < remaining.Length)
                    {
                        var space = remaining[..length].LastIndexOf(' '); if (space > 0) length = space;
                    }
                    if (y > 780) Page();
                    graphics!.DrawString(remaining[..length], font, XBrushes.Black, new XRect(40, y, width, 16), XStringFormats.TopLeft);
                    y += 16; remaining = remaining[length..].TrimStart();
                }
                y += 4;
            }
        }
        try
        {
            Page(); Line("HỒ SƠ ROADGUARD – Tổng hợp và nguồn dữ liệu");
            Line("Dự án: " + payload.Manifest.ProjectId); Line("Thời điểm snapshot: " + payload.Manifest.SnapshotAt.ToString("O"));
            Line("Snapshot: " + payload.Manifest.SnapshotId); Line("SHA-256: " + payload.Manifest.SnapshotHash);
            Line("Định nghĩa: " + string.Join(", ", payload.Manifest.DefinitionVersions));
            Line("TỔNG HỢP | Giá trị | Đơn vị | Tình trạng nguồn");
            foreach (var metric in payload.Dossier?.Summary.Metrics ?? [])
                Line($"{metric.Code}: {(metric.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "UNKNOWN")} | {metric.Unit} | {metric.Availability}" + (metric.ReasonCodes.Length == 0 ? "" : " | " + string.Join(", ", metric.ReasonCodes)));
            if (payload.Dossier?.Items.Length == 0) Line("Chưa có dữ liệu trong phạm vi đã chọn.");
            Line("NGUỒN VÀ PHIÊN BẢN");
            foreach (var source in payload.Manifest.SourceRevisions) Line($"{source.Kind} | {source.Id} | {source.Version}");
            foreach (var f in payload.Manifest.Files) Line($"File {f.FileId} | {f.FileVersion} | {f.SizeBytes} bytes | {f.Sha256} | {f.ReasonCode}");
            Line("DIỄN BIẾN");
            foreach (var item in payload.Dossier?.Timeline ?? []) Line($"{item.OccurredAt:O} | {item.Action} | {item.Source.Type} {item.Source.Id} {item.Source.Version} | {item.Summary}");
            Line("CÁC PHẦN CHƯA CÓ NGUỒN");
            foreach (var section in payload.Manifest.Sections.Where(s => s.Availability != "AVAILABLE")) Line($"{section.Name}: {section.Availability} | {string.Join(", ", section.ReasonCodes)}");
            foreach (var l in payload.Manifest.Labels ?? []) if (l.Mode != "REAL") Line($"MOCK/SYNTHETIC | label {l.LabelId} | mode {l.Mode}");
            graphics?.Dispose(); graphics = null;
            document.Save(output, false);
            output.Seek(0, SeekOrigin.End); // PDFsharp resets a retained seekable stream to position zero after Save.
            // A legal trailing PDF comment provides uncompressed immutable identity for durable crash recovery.
            var proof = Encoding.ASCII.GetBytes("\n%RoadGuardSnapshotHash=" + payload.Manifest.SnapshotHash + "\n"); output.Write(proof);
        }
        finally { graphics?.Dispose(); }
        return Task.CompletedTask;
    }
    private sealed class UnicodeFontResolver(byte[] font) : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) => familyName == "RoadGuardUnicode" ? new("RoadGuardUnicodeFace") : null;
        public byte[]? GetFont(string faceName) => faceName == "RoadGuardUnicodeFace" ? font : null;
    }
}

public static class ExportArchive
{
    public static string SurveyFilePath(Guid fileId, string mediaType) => "files/" + fileId.ToString("D") + (mediaType switch { "video/mp4" => ".mp4", "application/x-subrip" => ".srt", "image/png" => ".png", "image/jpeg" => ".jpg", _ => ".bin" });
    public static string FilePath(Guid fileId, string mediaType) => "images/" + fileId.ToString("D") + (mediaType == "image/png" ? ".png" : mediaType == "image/jpeg" ? ".jpg" : throw new ExportRenderException("export_archive_invalid"));
    public static void ValidatePath(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.Contains('\\') || name.Contains(':') || name.StartsWith('/') || name.Split('/').Any(s => s.Length == 0 || s is "." or ".." || s.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.')))) throw new ExportRenderException("export_archive_invalid");
    }
    public static async Task<bool> ProvesSnapshotAsync(Stream content, ExportManifestV1Dto manifest, CancellationToken ct)
    {
        if (!content.CanSeek || content.Length == 0) return false;
        if (manifest.Format == "PDF")
        {
            content.Position = 0; var signature = new byte[5]; await content.ReadExactlyAsync(signature, ct);
            if (!signature.SequenceEqual("%PDF-"u8.ToArray())) return false;
            var proof = Encoding.ASCII.GetBytes("\n%RoadGuardSnapshotHash=" + manifest.SnapshotHash + "\n");
            if (content.Length < proof.Length) return false;
            content.Seek(-proof.Length, SeekOrigin.End); var actual = new byte[proof.Length]; await content.ReadExactlyAsync(actual, ct); content.Position = 0; return actual.SequenceEqual(proof);
        }
        using var zip = new ZipArchive(content, ZipArchiveMode.Read, true);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in zip.Entries) { ValidatePath(e.FullName); if (!names.Add(e.FullName)) return false; }
        var expected = new HashSet<string>(manifest.Kind == "DOSSIER" ? ["manifest.json", "evidence-index.json", "dossier.pdf"] : new[] { "manifest.json", "evidence-index.json", "labels.jsonl", "README.txt" }, StringComparer.OrdinalIgnoreCase);
        foreach (var f in manifest.Files.Where(f => f.Included)) if (f.ArchivePath is null || !expected.Add(f.ArchivePath)) return false;
        if (!names.SetEquals(expected)) return false;
        var entry = zip.GetEntry("manifest.json"); if (entry is null || entry.Length > 64 * 1024 * 1024) return false;
        await using var stream = entry.Open(); var stored = await JsonSerializer.DeserializeAsync<ExportManifestV1Dto>(stream, ExportSerialization.Options, ct);
        if (stored is null || JsonSerializer.Serialize(stored, ExportSerialization.Options) != JsonSerializer.Serialize(manifest, ExportSerialization.Options)) return false;
        var indexEntry = zip.GetEntry("evidence-index.json")!;
        if (indexEntry.Length > 64 * 1024 * 1024) return false;
        await using (var indexStream = indexEntry.Open())
        {
            var index = await JsonSerializer.DeserializeAsync<ExportFileDto[]>(indexStream, ExportSerialization.Options, ct);
            if (JsonSerializer.Serialize(index, ExportSerialization.Options) != JsonSerializer.Serialize(manifest.Files, ExportSerialization.Options)) return false;
        }
        if (manifest.Kind == "DOSSIER")
        {
            await using var pdfInput = zip.GetEntry("dossier.pdf")!.Open(); await using var pdf = MinioAnh02ArtifactStore.Temporary();
            await pdfInput.CopyToAsync(pdf, 81920, ct); pdf.Position = 0;
            if (!await ProvesSnapshotAsync(pdf, manifest with { Format = "PDF" }, ct)) return false;
        }
        else
        {
            var labelEntry = zip.GetEntry("labels.jsonl")!; if (labelEntry.Length > 64 * 1024 * 1024) return false;
            await using var labelInput = labelEntry.Open(); using var labelReader = new StreamReader(labelInput, Encoding.UTF8);
            var lines = await labelReader.ReadToEndAsync(ct);
            if (lines != string.Join("\n", (manifest.Labels ?? []).Select(l => JsonSerializer.Serialize(l, ExportSerialization.Options))) + "\n") return false;
        }
        foreach (var file in manifest.Files.Where(f => f.Included))
        {
            var original = zip.GetEntry(file.ArchivePath!); if (original is null || original.Length != file.SizeBytes) return false;
            await using var input = original.Open(); var actual = await MinioAnh02ArtifactStore.CopyHashAsync(input, Stream.Null, ct);
            if (actual.Hash != file.Sha256 || actual.Size != file.SizeBytes) return false;
        }
        content.Position = 0; return true;
    }
}
