using System.Globalization;

namespace Client.Helpers;

public static class DateInputHelper
{
    private static readonly string[] Formats = ["dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy"];

    public static bool TryParseToIso(string input, out string isoDate)
    {
        isoDate = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (DateOnly.TryParseExact(input.Trim(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            isoDate = date.ToString("yyyy-MM-dd");
            return true;
        }

        return false;
    }

    public static string FormatDisplay(DateOnly date) =>
        date.ToString("dd-MMM-yy", CultureInfo.InvariantCulture);

    public static DateOnly GetLastMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-daysSinceMonday);
    }

    public static DateOnly GetMostRecentCompletedWeekMonday()
    {
        return GetLastMonday().AddDays(-7);
    }

    public static bool TryParseWeekStart(string? input, out DateOnly weekStart, out string? error, DateOnly? defaultWeekStart = null)
    {
        weekStart = default;
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            weekStart = defaultWeekStart ?? GetLastMonday();
            return true;
        }

        if (!TryParseToIso(input, out var isoDate))
        {
            error = "Invalid date format. Use DD-MM-YYYY.";
            return false;
        }

        weekStart = DateOnly.Parse(isoDate, CultureInfo.InvariantCulture);
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            error = "Week start date must be a Monday.";
            return false;
        }

        return true;
    }
}
