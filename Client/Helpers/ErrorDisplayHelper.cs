using Client.HttpClients;
using Client.Models;

namespace Client.Helpers;

public static class ErrorDisplayHelper
{
    public static void HandleException(Exception ex)
    {
        switch (ex)
        {
            case SessionExpiredException sessionExpired:
                ConsoleHelper.PrintError(sessionExpired.Message);
                break;
            case ApiClientException api:
                ConsoleHelper.PrintError(api.Message);
                break;
            default:
                ConsoleHelper.PrintError($"Unexpected error: {ex.Message}");
                break;
        }
    }
}
