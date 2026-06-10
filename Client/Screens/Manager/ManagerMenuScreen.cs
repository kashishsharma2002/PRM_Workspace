using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Manager;

public static class ManagerMenuScreen
{
    public static async Task<bool> RunAsync(RestClient client)
    {
        while (true)
        {
            ConsoleHelper.PrintHeader("Manager Main Menu");
            Console.WriteLine($"Logged in as: {SessionStore.FullName}");
            ConsoleHelper.PrintDivider();
            Console.WriteLine("1. Resource Dashboard");
            Console.WriteLine("2. Allocate Resource");
            Console.WriteLine("3. My Projects");
            Console.WriteLine("4. Timesheets");
            Console.WriteLine("5. AI Assistant (Phase 8)");
            Console.WriteLine("0. Logout");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ResourceDashboardScreen.RunAsync(client);
                        break;
                    case "2":
                        await AllocateResourceScreen.RunAsync(client);
                        break;
                    case "5":
                        Console.WriteLine("AI Assistant will be available in Phase 8.");
                        break;
                    case "3":
                        await MyProjectsScreen.RunAsync(client);
                        break;
                    case "4":
                        await TimesheetsScreen.RunAsync(client);
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
