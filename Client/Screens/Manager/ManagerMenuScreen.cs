using Client.Common;
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
                        await ResourceDashboardScreen.RunAsync(clients);
                        break;
                    case MenuChoices.Two:
                        await AllocateResourceScreen.RunAsync(clients);
                        break;
                    case "5":
                        await AiAssistantScreen.RunAsync(clients);
                        break;
                    case MenuChoices.Three:
                        await MyProjectsScreen.RunAsync(clients);
                        break;
                    case "4":
                        await TimesheetsScreen.RunAsync(clients);
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
