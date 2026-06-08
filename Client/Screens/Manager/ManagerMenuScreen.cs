using Client.Helpers;
using Client.Session;

namespace Client.Screens.Manager;

public static class ManagerMenuScreen
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("Manager Main Menu");
        Console.WriteLine($"Logged in as: {SessionStore.FullName}");
        ConsoleHelper.PrintDivider();
        Console.WriteLine("Manager features will be available in Phase 5+.");
    }
}
