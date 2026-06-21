using Client.HttpClients;

namespace Client.Helpers;

public static class ScreenRunner
{
    public static async Task RunSafeAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (SessionExpiredException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorDisplayHelper.HandleException(ex);
        }
    }

    public static async Task<bool> TryRunMenuActionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (SessionExpiredException)
        {
            return false;
        }
    }
}
