namespace Server.Common;

public static class WeekDateHelper
{
    public static DateOnly GetCurrentWeekMonday(DateOnly? referenceDate = null)
    {
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);
        var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-daysSinceMonday);
    }

    public static DateOnly GetMostRecentCompletedWeekMonday(DateOnly? referenceDate = null)
    {
        return GetCurrentWeekMonday(referenceDate).AddDays(-7);
    }

    public static DateOnly GetWeekEnd(DateOnly weekStart) =>
        weekStart.AddDays(6);

    public static bool IsFutureWeek(DateOnly weekStart, DateOnly? referenceDate = null) =>
        weekStart > GetCurrentWeekMonday(referenceDate);
}
