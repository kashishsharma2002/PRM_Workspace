namespace Server.Common;

public static class WorkingDayHelper
{
    public static bool IsWorkingDay(DateOnly date) =>
        date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    public static DateOnly AddWorkingDays(DateOnly start, int workingDays)
    {
        if (workingDays <= 0)
            return start;

        var date = start;
        var remaining = workingDays;
        while (remaining > 0)
        {
            date = date.AddDays(1);
            if (IsWorkingDay(date))
                remaining--;
        }

        return date;
    }

    public static DateOnly GetNextWorkingDay(DateOnly date) => AddWorkingDays(date, 1);
}
