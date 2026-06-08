using Client.Helpers;
using Client.Session;

namespace Client.Screens.Employee;

public static class EmployeeMenuScreen
{
    public static void Run()
    {
        ConsoleHelper.PrintHeader("Employee Main Menu");
        Console.WriteLine($"Logged in as: {SessionStore.FullName}");
        ConsoleHelper.PrintDivider();
        Console.WriteLine("Employee features will be available in Phase 6+.");
    }
}
