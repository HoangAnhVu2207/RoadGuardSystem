using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.Services.Reports;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace RoadGuardSystem.UnitTests.Reports;

public sealed class ReporterIntakeRequestNormalizerTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

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

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData("{}", "evidence[0].location.latitude")]
    [InlineData("{\"latitude\": 10}", "evidence[0].location.longitude")]
    [InlineData("{\"longitude\": 106}", "evidence[0].location.latitude")]
    public void Normalization_RejectsLocationWhenLatitudeOrLongitudeIsMissingFromJson(string locationJson, string field)
    {
        var request = JsonSerializer.Deserialize<CreateReporterReportRequestDto>($$"""
            {
              "description": "report",
              "evidence": [
                {
                  "fileId": "11111111-1111-1111-1111-111111111111",
                  "fileVersion": "version",
                  "locationSource": "CAPTURE",
                  "location": {{locationJson}}
                }
              ]
            }
            """, WebJson)!;

        ReporterIntakeRequestNormalizer.TryNormalize(request, "key", out _, out var errors).Should().BeFalse();
        errors.Should().ContainKey(field);
    }

    [Fact]
    [Trait("Package", "HUY-01")]
    public void Normalization_AcceptsExplicitZeroCoordinatesAndUsesTrimmedIdempotencyKey()
    {
        var request = new CreateReporterReportRequestDto("report", [
            new(Guid.NewGuid(), "version", "CAPTURE", null, new(0, 0, null))
        ]);

        ReporterIntakeRequestNormalizer.TryNormalize(request, "  normalized-key  ", out var normalized, out _).Should().BeTrue();
        normalized!.IdempotencyKey.Should().Be("normalized-key");
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData("case 01", "case 01")]
    [InlineData("  case 01  ", "case 01")]
    public void Normalization_AcceptsPrintableAsciiSpacesInsideIdempotencyKey(string key, string expectedKey)
    {
        var request = new CreateReporterReportRequestDto("report", [
            new(Guid.NewGuid(), "version", "UNKNOWN", null, null)
        ]);

        ReporterIntakeRequestNormalizer.TryNormalize(request, key, out var normalized, out _).Should().BeTrue();
        normalized!.IdempotencyKey.Should().Be(expectedKey);
    }

    [Theory]
    [Trait("Package", "HUY-01")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t ")]
    [InlineData("key\n")]
    [InlineData("k\u00e9y")]
    [InlineData("\u00a0key\u00a0")]
    public void Normalization_RejectsInvalidIdempotencyKeyWithHeaderFieldError(string key)
    {
        var request = new CreateReporterReportRequestDto("report", [
            new(Guid.NewGuid(), "version", "UNKNOWN", null, null)
        ]);

        ReporterIntakeRequestNormalizer.TryNormalize(request, key, out _, out var errors).Should().BeFalse();
        errors.Should().ContainKey("Idempotency-Key");
    }

    private static string LegacyFingerprint(CreateReporterReportRequestDto request)
        => request.Description + "|" + string.Join(";", request.Evidence!.Select(item =>
            $"{item!.FileId:D}|{item.FileVersion!.Trim()}|{item.LocationSource!.Trim()}"));
}
