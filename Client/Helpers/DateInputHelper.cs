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
}
