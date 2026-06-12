using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class AdminMenuScreen
{
    public static async Task<bool> RunAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintBoxHeader(
                "ADMIN PANEL",
                $"Welcome, {SessionStore.FullName}  |  {DateTime.Now:dd-MM-yyyy HH:mm}"
            );
            Console.WriteLine("1. Manage Employees/Resources");
            Console.WriteLine("2. Manage Projects");
            Console.WriteLine("3. View All Allocations");
            Console.WriteLine("4. Manage Users");
            Console.WriteLine("5. System Configuration");
            Console.WriteLine("0. Logout");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ManageEmployeesScreen.RunAsync(clients);
                        break;
                    case "2":
                        await ManageProjectsScreen.RunAsync(clients);
                        break;
                    case "3":
                        await ViewAllAllocationsScreen.RunAsync(clients);
                        break;
                    case "4":
                        await ManageUsersScreen.RunAsync(clients);
                        break;
                    case "5":
                        await SystemConfigScreen.RunAsync(clients);
                        break;
                    case "0":
                        SessionStore.Clear();
                        clients.SetToken(null);
                        ConsoleHelper.PrintSuccess("Logged out.");
                        return false;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }
            catch (SessionExpiredException)
            {
                return false;
            }
        }
    }
}
