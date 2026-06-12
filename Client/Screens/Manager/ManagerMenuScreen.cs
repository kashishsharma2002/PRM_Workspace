using Client.Helpers;
using Client.HttpClients;

namespace Client.Screens.Manager;

public static class ManagerMenuScreen
{
    public static async Task<bool> RunAsync(AppClients clients)
    {
        while (true)
        {
            ConsoleHelper.PrintBoxHeader(
                "MANAGER PANEL",
                $"Welcome, {SessionStore.FullName}  |  {DateTime.Now:dd-MM-yyyy HH:mm}"
            );
            Console.WriteLine("1. Resource Dashboard");
            Console.WriteLine("2. Allocate Resource");
            Console.WriteLine("3. My Projects");
            Console.WriteLine("4. Timesheets");
            Console.WriteLine("5. AI Assistant");
            Console.WriteLine("0. Logout");
            ConsoleHelper.PrintDivider();
            Console.Write("Enter option: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ResourceDashboardScreen.RunAsync(clients);
                        break;
                    case "2":
                        await AllocateResourceScreen.RunAsync(clients);
                        break;
                    case "5":
                        await AiAssistantScreen.RunAsync(clients);
                        break;
                    case "3":
                        await MyProjectsScreen.RunAsync(clients);
                        break;
                    case "4":
                        await TimesheetsScreen.RunAsync(clients);
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
