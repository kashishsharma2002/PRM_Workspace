using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Admin;

public static class AdminMenuScreen
{
    public static async Task<bool> RunAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Admin Main Menu");
            Console.WriteLine($"Logged in as: {SessionStore.FullName}");
            ConsoleHelper.PrintDivider();
            Console.WriteLine("1. Manage Employees");
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
                        await ManageEmployeesScreen.RunAsync(client);
                        break;
                    case "2":
                        await ManageProjectsScreen.RunAsync(client);
                        break;
                    case "3":
                        await ViewAllAllocationsScreen.RunAsync(client);
                        break;
                    case "4":
                        await ManageUsersScreen.RunAsync(client);
                        break;
                    case "5":
                        await SystemConfigScreen.RunAsync(client);
                        break;
                    case "0":
                        SessionStore.Clear();
                        client.SetToken(null);
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
