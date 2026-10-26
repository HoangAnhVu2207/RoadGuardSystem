using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.Services.Exports;
using Xunit;

namespace RoadGuardSystem.UnitTests.Sandbox;

public sealed class SandboxExportFilterTests
{
    [Theory]
    [InlineData("TRAINING", "PDF", false)]
    [InlineData("DOSSIER", "PDF", true)]
    [InlineData("unknown", "ZIP", false)]
    [InlineData("DOSSIER", "CSV", false)]
    public void UnsupportedKindFormatOrOriginalCombinationIsRejected(string kind, string format, bool originals)
        => Assert.Null(ExportService.Normalize(new(kind, format, IncludeOriginalFiles: originals)));

    [Fact]
    public void UnsupportedDefectFilterCannotSilentlyBroadenExport()
        => Assert.Null(ExportService.Normalize(new("DOSSIER", "ZIP", DefectIds: [Guid.NewGuid()])));

    [Fact]
    public void EmptyIdentifiersAndOversizedSelectionAreRejected()
    {
        Assert.Null(ExportService.Normalize(new("DOSSIER", "ZIP", SegmentIds: [Guid.Empty])));
        Assert.Null(ExportService.Normalize(new("DOSSIER", "ZIP", SegmentIds: Enumerable.Range(0, 1001).Select(_ => Guid.NewGuid()).ToArray())));
    }

    [Fact]
    public void CanonicalizationDoesNotMutateCallerOrLoseSelectedScope()
    {
        var a = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var b = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid[] selection = [b, a, b];
        var input = new CreateExportRequestDto(" dossier ", " zip ", SegmentIds: selection);
        var result = ExportService.Normalize(input);
        Assert.NotNull(result);
        Assert.Equal("DOSSIER", result.Kind); Assert.Equal("ZIP", result.Format);
        Assert.Equal(new[] { a, b }, result.SegmentIds);
        Assert.Equal(new[] { b, a, b }, input.SegmentIds);
        var again = ExportService.Normalize(result);
        Assert.NotNull(again);
        Assert.Equal(result.Kind, again.Kind); Assert.Equal(result.Format, again.Format);
        Assert.Equal(result.SegmentIds, again.SegmentIds);
        Assert.Equal(result.DefectIds, again.DefectIds);
    }
}
