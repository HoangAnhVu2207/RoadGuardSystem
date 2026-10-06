using System.Security.Cryptography;
using System.Text;
using RoadGuardSystem.DTOs.Inspections;
using RoadGuardSystem.DTOs.Repairs;
using Xunit;

namespace RoadGuardSystem.UnitTests.Repairs;

public sealed class H4RepairCoreHashGoldenTests
{
    private static readonly Guid TaskId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Item = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Actor = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Origin = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid FirstStart = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid AssessmentId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTimeOffset Claimed = new(2026, 10, 6, 14, 5, 6, TimeSpan.FromHours(7));
    private static string Digest(string literal) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(literal))).ToLowerInvariant();

    [Fact]
    public void AssessmentGoldenPinsPropertyOrderNullableFieldsUnicodeAndDecimalScale()
    {
        var input = new RepairMeasurementAssessmentInput(Origin, FirstStart,
            [new FieldMeasurementInput("A1", "DepressionDepth", 1.230000m, "KNOWN", null, "LENGTH", "mm", null, null,
                "nứt nền", "thước", "M")], null, null);
        const string golden = """
            {"schemaVersion":1,"kind":"REPAIR_ASSESSMENT","taskId":"11111111-1111-1111-1111-111111111111","itemId":"22222222-2222-2222-2222-222222222222","originalActorId":"33333333-3333-3333-3333-333333333333","input":{"originId":"44444444-4444-4444-4444-444444444444","fieldFirstStartId":"55555555-5555-5555-5555-555555555555","measurements":[{"sampleId":"A1","type":"DepressionDepth","value":1.230000,"state":"KNOWN","unknownReason":null,"dimension":"LENGTH","unit":"mm","longitude":null,"latitude":null,"locationReason":"n\u1EE9t n\u1EC1n","instrument":"th\u01B0\u1EDBc","method":"M","notes":null}],"evidence":null,"locationProof":null,"deviceId":null}}
            """;
        Assert.Equal(Digest(golden), RepairCoreHash.Assessment(TaskId, Item, Actor, input));
    }
    [Fact]
    public void ExecutionStartGoldenRetainsOriginalOffsetAndMandatoryAssessmentPin()
    {
        var input = new RepairExecutionStartInput(Origin, FirstStart, Claimed, AssessmentId);
        const string golden = """
            {"schemaVersion":1,"kind":"REPAIR_EXECUTION_START","taskId":"11111111-1111-1111-1111-111111111111","itemId":"22222222-2222-2222-2222-222222222222","originalActorId":"33333333-3333-3333-3333-333333333333","input":{"originId":"44444444-4444-4444-4444-444444444444","fieldFirstStartId":"55555555-5555-5555-5555-555555555555","claimedAt":"2026-10-06T14:05:06+07:00","assessmentId":"66666666-6666-6666-6666-666666666666","deviceId":null,"monotonicMilliseconds":null,"bootId":null}}
            """;
        Assert.Equal(Digest(golden), RepairCoreHash.ExecutionStart(TaskId, Item, Actor, input));
    }
    [Fact]
    public void ExecutionFinishGoldenRetainsSevenFractionDigitsAndDeviceClockClaims()
    {
        var input = new RepairExecutionFinishInput(Origin, FirstStart, Claimed.AddTicks(1234567), AssessmentId, 9000, "boot-A");
        const string golden = """
            {"schemaVersion":1,"kind":"REPAIR_EXECUTION_FINISH","taskId":"11111111-1111-1111-1111-111111111111","itemId":"22222222-2222-2222-2222-222222222222","originalActorId":"33333333-3333-3333-3333-333333333333","input":{"originId":"44444444-4444-4444-4444-444444444444","executionStartId":"55555555-5555-5555-5555-555555555555","claimedAt":"2026-10-06T14:05:06.1234567+07:00","deviceId":"66666666-6666-6666-6666-666666666666","monotonicMilliseconds":9000,"bootId":"boot-A"}}
            """;
        Assert.Equal(Digest(golden), RepairCoreHash.ExecutionFinish(TaskId, Item, Actor, input));
    }
}
