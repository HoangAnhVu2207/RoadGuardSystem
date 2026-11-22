using RoadGuardSystem.aBusinessObjects.Commons;
using System.Text.Json;
using RoadGuardSystem.BusinessObjects.Surveys;
using RoadGuardSystem.Repositories.Surveys;
using Xunit;

namespace RoadGuardSystem.UnitTests.Surveys;

public sealed class SurveyReadbackCompatibilityTests
{
    [Theory]
    [InlineData(SurveyType.Original, "BASELINE")]
    [InlineData(SurveyType.Periodic, "PERIODIC")]
    [InlineData(SurveyType.Supplementary, "AD_HOC")]
    [InlineData(SurveyType.Unknown, "UNKNOWN")]
    [InlineData((SurveyType)99, "UNKNOWN")]
    public void PersistedTypeHasExplicitWireWithoutFallbackToBaseline(SurveyType type, string expected)
        => Assert.Equal(expected, SurveyTypeWire.Code(type));

    [Fact]
    public void HistoricalReceiptWithoutTypeDoesNotInventBaselineOrCompletion()
    {
        const string planJson = "{\"Id\":\"00000000-0000-0000-0000-000000000001\",\"ProjectId\":\"00000000-0000-0000-0000-000000000002\",\"ScopeJson\":\"[]\",\"PlannedAt\":\"2026-10-02T00:00:00Z\",\"Status\":\"Planned\",\"Version\":\"AAAAAA==\"}";
        var plan = JsonSerializer.Deserialize<SurveyV2PlanPersistenceView>(planJson)!;
        Assert.Equal("UNKNOWN", plan.SurveyType);
        Assert.Null(plan.Completion);
        const string taskJson = "{\"Id\":\"00000000-0000-0000-0000-000000000003\",\"ProjectId\":\"00000000-0000-0000-0000-000000000002\",\"ScopeJson\":\"[]\",\"OperatorId\":\"00000000-0000-0000-0000-000000000004\",\"Status\":\"Submitted\",\"Version\":\"AAAAAA==\"}";
        var task = JsonSerializer.Deserialize<SurveyV2TaskPersistenceView>(taskJson)!;
        Assert.Equal("UNKNOWN", task.SurveyType);
        Assert.Null(task.ParentTaskId);
    }
}
