using System.Text.Json;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.DTOs.Projects;
using RoadGuardSystem.DTOs.Reports;
using RoadGuardSystem.DTOs.Cases;
using RoadGuardSystem.DTOs.Surveys;
using Xunit;

namespace RoadGuardSystem.ApiTests.Projects;

// DTO boundary tests complement the real SQL/HTTP controller regression cases.
// They do not represent a workflow or authorization acceptance result.
public sealed class OptionalResponseObjectCompatibilityTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static IEnumerable<object[]> Cases()
    {
        yield return [typeof(InternalCaseDto), "conclusion", "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"outcome\":\"NO_DEFECT\",\"reason\":\"Verified\",\"concludedAt\":\"2026-10-24T00:00:00+00:00\",\"defectIds\":[],\"evidenceIds\":[]}"];
        yield return [typeof(SurveyTaskWorkPackage), "accessPoint", "{\"point\":{\"longitude\":106.5,\"latitude\":10.5},\"source\":\"GPS\",\"accuracyMeters\":2,\"capturedAt\":null}"];
        yield return [typeof(DefectViewDto), "geometry", "{\"srid\":32648,\"wkt\":\"POINT (500000 1200000)\"}"];
        yield return [typeof(RoadGeometryVersionView), "wgs84Centerline", Shape];
        yield return [typeof(RoadGeometryVersionView), "chainageCalibration", Calibration];
        yield return [typeof(SegmentDefinition), "nativeAlignment", "{\"crsProfileRevisionId\":\"11111111-1111-1111-1111-111111111111\",\"primitives\":[],\"spatialSrid\":32648}"];
        yield return [typeof(SegmentDefinition), "chainageCalibration", Calibration];
        yield return [typeof(SegmentGeometryView), "metricGeometry", Shape];
        yield return [typeof(SegmentGeometryView), "wgs84Geometry", Shape];
        yield return [typeof(OwnReportEvidenceDto), "location", "{\"latitude\":10.5,\"longitude\":106.5,\"accuracyMeters\":2}"];
    }

    private const string Shape = "{\"type\":\"LineString\",\"coordinates\":[[500000,1200000],[500100,1200000]]}";
    private const string Calibration = "{\"sourceReference\":\"owned fixture\",\"sourceChecksum\":\"abc\",\"controls\":[],\"selectedReason\":\"survey\"}";

    [Theory]
    [MemberData(nameof(Cases))]
    public void OptionalOutputObjectOmitsNullAndPreservesPopulatedValue(Type type, string property, string populated)
    {
        var empty = JsonSerializer.Deserialize("{\"" + property + "\":null}", type, Options)!;
        var nullWire = JsonSerializer.SerializeToElement(empty, type, Options);
        Assert.False(nullWire.TryGetProperty(property, out _));

        var value = JsonSerializer.Deserialize("{\"" + property + "\":" + populated + "}", type, Options)!;
        var actual = JsonSerializer.SerializeToElement(value, type, Options).GetProperty(property);
        Assert.Equal(JsonValueKind.Object, actual.ValueKind);
        Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(populated),
            System.Text.Json.Nodes.JsonNode.Parse(actual.GetRawText())));
    }

    [Fact]
    public void OtherNullablePropertiesAndInputContractsRemainUnchanged()
    {
        var defect = JsonSerializer.Deserialize<DefectViewDto>("{}", Options)!;
        Assert.Equal(JsonValueKind.Null, JsonSerializer.SerializeToElement(defect, Options).GetProperty("causeCategoryCode").ValueKind);
        var input = JsonSerializer.Deserialize<GeometryDraftInput>("{}", Options)!;
        var wire = JsonSerializer.SerializeToElement(input, Options);
        Assert.Equal(JsonValueKind.Null, wire.GetProperty("nativeAlignment").ValueKind);
        Assert.Equal(JsonValueKind.Null, wire.GetProperty("chainageCalibration").ValueKind);
        var reportInput = JsonSerializer.Deserialize<ReportEvidenceInputDto>("{}", Options)!;
        Assert.Equal(JsonValueKind.Null, JsonSerializer.SerializeToElement(reportInput, Options).GetProperty("location").ValueKind);
    }
}
