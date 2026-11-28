namespace RoadGuardSystem.BusinessObjects.Surveys;

public sealed record SurveyWarrantyWindow(Guid Id, DateOnly StartDate, DateOnly EndDate);

public sealed record SurveyScheduleSlot(DateTimeOffset Start, DateTimeOffset End);

public static class SurveyWarrantyCalendar
{
    private const long VietnamOffsetTicks = 7 * TimeSpan.TicksPerHour;

    public static bool Contains(SurveyWarrantyWindow window, DateTimeOffset start, DateTimeOffset end)
    {
        var startTicks = start.UtcTicks + VietnamOffsetTicks;
        var endTicks = end.UtcTicks + VietnamOffsetTicks;
        var firstTick = (long)window.StartDate.DayNumber * TimeSpan.TicksPerDay;
        var afterLastTick = ((long)window.EndDate.DayNumber + 1) * TimeSpan.TicksPerDay;
        return end > start && startTicks >= firstTick && endTicks <= afterLastTick;
    }

    public static IReadOnlyList<SurveyScheduleSlot> Generate(
        SurveyWarrantyWindow window,
        DateTimeOffset start,
        DateTimeOffset end,
        string frequency)
    {
        if (frequency is not ("WEEKLY" or "MONTHLY") || !Contains(window, start, end))
            return [];

        var anchor = start.ToOffset(TimeSpan.FromTicks(VietnamOffsetTicks));
        var duration = end - start;
        var months = (window.EndDate.Year - anchor.Year) * 12 + window.EndDate.Month - anchor.Month;
        var weeks = (window.EndDate.DayNumber - DateOnly.FromDateTime(anchor.Date).DayNumber) / 7;
        var lastIndex = frequency == "MONTHLY" ? months : weeks;
        var slots = new List<SurveyScheduleSlot>();
        for (var index = 0; index <= lastIndex; index++)
        {
            var slotStart = frequency == "MONTHLY" ? anchor.AddMonths(index) : anchor.AddDays(index * 7);
            if (slotStart.UtcTicks > DateTime.MaxValue.Ticks - duration.Ticks)
                break;
            var slotEnd = slotStart.ToUniversalTime().Add(duration);
            if (!Contains(window, slotStart, slotEnd))
                break;
            slots.Add(new(slotStart, slotEnd));
        }
        return slots;
    }
}
