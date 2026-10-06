using RoadGuardSystem.DTOs.Projects;
using System.Security.Cryptography;
using System.Text.Json;

namespace RoadGuardSystem.Services.Projects;

public static class GeometryMapPageEngine
{
    public static GeometryMapPage Page(GeometryMapSnapshot snapshot, string layer, string expectedHash,
        int limit, double[]? bbox = null, string? cursor = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (limit is < 1 or > 500 || string.IsNullOrWhiteSpace(layer) ||
            bbox is not null && (bbox.Length != 4 || bbox.Any(x => !double.IsFinite(x)) || bbox[0] > bbox[2] || bbox[1] > bbox[3]))
            Fail("geometry_page_invalid");
        if (expectedHash != snapshot.Manifest.ContentHash) Fail("geometry_version_mismatch");
        var descriptor = snapshot.Manifest.Layers.SingleOrDefault(x => x.Key == layer);
        if (descriptor is null) Fail("geometry_layer_not_found");
        if (descriptor.Status != "READY") Fail("geometry_layer_unavailable");
        var filterHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(bbox)));
        var offset = 0;
        if (cursor is not null)
        {
            Cursor decoded;
            try
            {
                if (cursor.Length > 4096) Fail("geometry_cursor_invalid");
                decoded = JsonSerializer.Deserialize<Cursor>(Convert.FromBase64String(cursor))
                    ?? throw new JsonException();
            }
            catch (Exception e) when (e is JsonException or FormatException) { Fail("geometry_cursor_invalid"); throw; }
            if (decoded.PublicationId != snapshot.Manifest.PublicationId || decoded.Hash != expectedHash ||
                decoded.Layer != layer || decoded.FilterHash != filterHash) Fail("geometry_version_mismatch");
            offset = decoded.Offset;
        }
        var all = snapshot.Features.Where(x => x.Layer == layer).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
        if (all.Length != descriptor.Count || all.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != all.Length ||
            all.Any(f => f.Bbox.Length != 4 || f.Bbox.Any(x => !double.IsFinite(x)))) Fail("geometry_snapshot_invalid");
        var features = bbox is null ? all : all.Where(f => f.Bbox[0] <= bbox[2] && f.Bbox[2] >= bbox[0] && f.Bbox[1] <= bbox[3] && f.Bbox[3] >= bbox[1]).ToArray();
        if (offset < 0 || offset > features.Length) Fail("geometry_cursor_invalid");
        var selected = features.Skip(offset).Take(limit).ToArray();
        var nextOffset = offset + selected.Length;
        var next = nextOffset < features.Length ? Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
            new Cursor(snapshot.Manifest.PublicationId, expectedHash, layer, filterHash, nextOffset))) : null;
        return new(snapshot.Manifest.PublicationId, expectedHash, layer, selected, next);
    }
    private sealed record Cursor(Guid PublicationId, string Hash, string Layer, string FilterHash, int Offset);
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail(string code) => throw new GeometryValidationException(code, "The pinned map request could not be completed.");
}
