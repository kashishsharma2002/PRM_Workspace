using System.Diagnostics.CodeAnalysis;

namespace Client.Helpers;

public static class ApiLoadHelper
{
    public static bool RequireLoaded<T>(
        [NotNullWhen(true)] T? value,
        string failureMessage) where T : class
    {
        if (value is not null)
            return true;

        ConsoleHelper.PrintError(failureMessage);
        return false;
    }
}
