using Client.Helpers;
using Client.HttpClients;
using Client.Session;

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
            Console.WriteLine("1. Resource Dashboard (Phase 4)");
            Console.WriteLine("2. Allocate Resource (Phase 4)");
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
                    case "2":
                    case "5":
                        Console.WriteLine("This feature will be available in a later phase.");
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
