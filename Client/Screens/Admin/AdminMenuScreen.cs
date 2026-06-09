using Client.Helpers;
using Client.HttpClients;
using Client.Session;

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
            Console.WriteLine("1. Manage Employees      (Phase 3+)");
            Console.WriteLine("2. Manage Projects       (Phase 4+)");
            Console.WriteLine("3. View All Allocations  (Phase 4+)");
            Console.WriteLine("4. Manage Users");
            Console.WriteLine("5. System Configuration  (Phase 4+)");
            Console.WriteLine("0. Logout");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "4":
                        await ManageUsersScreen.RunAsync(client);
                        break;
                    case "0":
                        SessionStore.Clear();
                        client.SetToken(null);
                        ConsoleHelper.PrintSuccess("Logged out.");
                        return false;
                    case "1":
                    case "2":
                    case "3":
                    case "5":
                        ConsoleHelper.PrintError("This feature is available in a later phase.");
                        break;
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
