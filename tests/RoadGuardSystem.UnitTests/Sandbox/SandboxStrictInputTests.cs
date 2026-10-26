using System.Text.Json;
using RoadGuardSystem.DTOs.Exports;
using RoadGuardSystem.DTOs.Offline;
using RoadGuardSystem.DTOs.Repairs;
using RoadGuardSystem.DTOs.Defects;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Reports;
using Xunit;

namespace RoadGuardSystem.UnitTests.Sandbox;

// These are serializer boundaries, not authentication, workflow or SQL acceptance.
public sealed class SandboxStrictInputTests
{
    public static IEnumerable<object[]> StrictCommands()
    {
        yield return [typeof(CreateExportRequestDto), "{\"kind\":\"DOSSIER\",\"format\":\"ZIP\"}"];
        yield return [typeof(OfflineGrantRevokeInput), "{\"reason\":\"Synthetic\"}"];
        yield return [typeof(OfflineStartReconcileInput), "{\"batchId\":\"11111111-1111-1111-1111-111111111111\",\"originId\":\"22222222-2222-2222-2222-222222222222\"}"];
        yield return [typeof(RepairCancellationInput), "{\"reason\":\"Synthetic\",\"handover\":null}"];
        yield return [typeof(RepairNormalContinuationInput), "{\"repairPlan\":\"Synthetic\",\"checklistVersion\":\"v1\",\"reason\":\"Synthetic\",\"handover\":null}"];
        yield return [typeof(FieldTaskActionInput), "{\"reason\":\"Synthetic\"}"];
        yield return [typeof(CreateReporterReportRequestDto), "{\"description\":\"Synthetic\",\"evidence\":[]}"];
        yield return [typeof(CandidateClassificationDto), "{\"defectTypeCode\":\"CRACK\",\"causeCategoryCode\":null,\"severity\":null,\"roadSectionVersionId\":\"11111111-1111-1111-1111-111111111111\"}"];
    }

    [Theory]
    [MemberData(nameof(StrictCommands))]
    public void CurrentCommandCanDeserializeButForgedAuthorityMemberIsRejected(Type type, string json)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Assert.NotNull(JsonSerializer.Deserialize(json, type, options));
        var forged = json[..^1] + ",\"isSupervisor\":true}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(forged, type, options));
    }
}
