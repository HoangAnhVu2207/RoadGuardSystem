using RoadGuardSystem.BusinessObjects.Surveys;
using Xunit;

namespace RoadGuardSystem.UnitTests.Surveys;

public sealed class SurveyWarrantyCalendarTests
{
    private static readonly SurveyWarrantyWindow Window = new(Guid.NewGuid(), new(2027, 1, 1), new(2027, 12, 31));
    private static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    [Theory]
    [InlineData(22, 2, true)]
    [InlineData(23, 1, true)]
    [InlineData(23, 2, false)]
    public void Entire_interval_must_fit_the_last_local_day(int hour, int duration, bool expected)
    {
        var start = new DateTimeOffset(2027, 12, 31, hour, 0, 0, Offset);
        Assert.Equal(expected, SurveyWarrantyCalendar.Contains(Window, start, start.AddHours(duration)));
    }

    [Fact]
    public void Monthly_series_preserves_anchor_day_after_short_months()
    {
        var anchor = new DateTimeOffset(2027, 1, 31, 8, 0, 0, Offset);
        var slots = SurveyWarrantyCalendar.Generate(Window, anchor, anchor.AddHours(1), "MONTHLY");
        Assert.Equal(12, slots.Count);
        Assert.Equal(new DateTimeOffset(2027, 2, 28, 8, 0, 0, Offset), slots[1].Start);
        Assert.Equal(new DateTimeOffset(2027, 3, 31, 8, 0, 0, Offset), slots[2].Start);
    }

    [Fact]
    public void Two_year_monthly_series_clamps_to_both_regular_and_leap_february()
    {
        var window = Window with { EndDate = new DateOnly(2028, 12, 31) };
        var start = new DateTimeOffset(2027, 1, 31, 8, 0, 0, Offset);
        var slots = SurveyWarrantyCalendar.Generate(window, start, start.AddHours(1), "MONTHLY");
        Assert.Equal(24, slots.Count);
        Assert.Equal(start.AddMonths(13), slots[13].Start);
        Assert.Equal(29, slots[13].Start.Day);
        Assert.Equal(start.AddMonths(23), slots[^1].Start);
    }

    [Fact]
    public void Weekly_series_stops_without_moving_an_invalid_anchor()
    {
        var anchor = new DateTimeOffset(2027, 1, 1, 8, 0, 0, Offset);
        var slots = SurveyWarrantyCalendar.Generate(Window, anchor, anchor.AddHours(1), "WEEKLY");
        Assert.Equal(53, slots.Count);
        Assert.Equal(anchor.AddDays(364), slots[^1].Start);
        Assert.Empty(SurveyWarrantyCalendar.Generate(Window, anchor.AddDays(-7), anchor.AddDays(-7).AddHours(1), "WEEKLY"));
    }
}
