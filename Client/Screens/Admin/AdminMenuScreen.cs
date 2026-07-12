using Client.Common;
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
            Console.WriteLine("5. View Activity Logs");
            Console.WriteLine("6. System Configuration");
            Console.WriteLine("0. Logout");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            if (choice == MenuChoices.Exit)
            {
                SessionStore.Clear();
                clients.SetToken(null);
                ConsoleHelper.PrintSuccess("Logged out.");
                return false;
            }

            if (!await ScreenRunner.TryRunMenuActionAsync(async () =>
            {
                switch (choice)
                {
                    case MenuChoices.One:
                        await ManageEmployeesScreen.RunAsync(clients);
                        break;
                    case MenuChoices.Two:
                        await ManageProjectsScreen.RunAsync(clients);
                        break;
                    case MenuChoices.Three:
                        await ViewAllAllocationsScreen.RunAsync(clients);
                        break;
                    case "4":
                        await ManageUsersScreen.RunAsync(clients);
                        break;
                    case "5":
                        await ActivityLogScreen.RunAsync(clients);
                        break;
                    case "6":
                        await SystemConfigScreen.RunAsync(clients);
                        break;
                    default:
                        ConsoleHelper.PrintError("Invalid option.");
                        break;
                }
            }))
                return false;
        }
    }
}
