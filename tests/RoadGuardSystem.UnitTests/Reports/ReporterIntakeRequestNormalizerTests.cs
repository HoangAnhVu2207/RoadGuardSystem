using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Services.Reports;
using FluentAssertions;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reports;

public sealed class ReporterIntakeRequestNormalizerTests
{
    [Fact]
    [Trait("Package", "HUY-01")]
    public void Normalization_RejectsDelimiterInjectionThatCollidesWithLegacyEvidenceBoundaries()
    {
        var first = new CreateReporterReportRequestDto("same", [
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "a", "UNKNOWN;22222222-2222-2222-2222-222222222222|b|UNKNOWN", null, null)
        ]);
        var second = new CreateReporterReportRequestDto("same", [
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "a", "UNKNOWN", null, null),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "b", "UNKNOWN", null, null)
        ]);

        LegacyFingerprint(first).Should().Be(LegacyFingerprint(second));
        ReporterIntakeRequestNormalizer.TryNormalize(first, "same-key", out _, out _).Should().BeFalse();
        ReporterIntakeRequestNormalizer.TryNormalize(second, "same-key", out _, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("UNKNOWN", true)]
    [InlineData("CAPTURE", false)]
    [InlineData("capture", false)]
    [InlineData("GPS", false)]
    public void Validate_RequiresExplicitLocationSourceWireValue(string source, bool valid)
    {
        var request = new CreateReporterReportRequestDto("report", [
            new(Guid.NewGuid(), "version", source, null, null)
        ]);

        ReporterIntakeRequestNormalizer.TryNormalize(request, "key", out _, out _).Should().Be(valid);
    }

    private static string LegacyFingerprint(CreateReporterReportRequestDto request)
        => request.Description + "|" + string.Join(";", request.Evidence!.Select(item =>
            $"{item!.FileId:D}|{item.FileVersion!.Trim()}|{item.LocationSource!.Trim()}"));
}
