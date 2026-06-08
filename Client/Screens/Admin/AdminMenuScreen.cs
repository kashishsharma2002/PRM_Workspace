using Client.Helpers;
using Client.Session;

namespace Client.Screens.Admin;

public static class AdminMenuScreen
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("Admin Main Menu");
        Console.WriteLine($"Logged in as: {SessionStore.FullName}");
        ConsoleHelper.PrintDivider();
        Console.WriteLine("1. Manage Employees      (Phase 2+)");
        Console.WriteLine("2. Manage Projects       (Phase 4+)");
        Console.WriteLine("3. View All Allocations  (Phase 4+)");
        Console.WriteLine("4. Manage Users          (Phase 2+)");
        Console.WriteLine("5. System Configuration  (Phase 4+)");
        Console.WriteLine("0. Logout");
        ConsoleHelper.PrintDivider();
        Console.WriteLine("Phase 1 complete — full menus coming in later phases.");
    }
}
